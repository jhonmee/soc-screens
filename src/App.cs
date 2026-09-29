// MuroSOC - App
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;

namespace MuroSoc
{
    internal static class App
    {
        private static readonly List<BrowserTab> OpenTabs = new List<BrowserTab>();
        private static ConfigStore store;
        private static bool signOutRunning;

        public static event EventHandler<ConfigReloadedEventArgs> ConfigReloaded;

        public static string RuntimeVersion { get; private set; }

        public static AppConfig Config
        {
            get { return store.Current; }
        }

        public static IList<BrowserTab> Tabs
        {
            get { return OpenTabs.AsReadOnly(); }
        }

        public static void Initialize(string runtimeVersion)
        {
            RuntimeVersion = runtimeVersion;
            AppPaths.EnsureCreated();
            Log.Info("Inicio Muro SOC " + RuntimeInfo.AppVersion + " | WebView2 Runtime " + runtimeVersion + " | SDK " + RuntimeInfo.SdkVersion);
            store = new ConfigStore(AppPaths.ConfigFile);
            store.Load();
            store.Reloaded += OnConfigReloaded;
        }

        public static void SaveConfig()
        {
            store.Save();
            ApplyConfigToTabs();
        }

        public static void RegisterTab(BrowserTab tab)
        {
            if (!OpenTabs.Contains(tab))
            {
                OpenTabs.Add(tab);
            }
        }

        public static void UnregisterTab(BrowserTab tab)
        {
            OpenTabs.Remove(tab);
        }

        public static void ShowAbout(IWin32Window owner)
        {
            using (AboutForm form = new AboutForm())
            {
                form.ShowDialog(owner);
            }
        }

        public static async void SignOutProfile(IWin32Window owner, string profileName)
        {
            if (signOutRunning)
            {
                return;
            }
            DialogResult answer = MessageBox.Show(owner,
                "Se borrarán las cookies, la caché y los datos de sitios del perfil \"" + profileName + "\".\r\n\r\n" +
                "Todas las pestañas con ese perfil tendrán que iniciar sesión otra vez. " +
                "Si el equipo usa SSO con la cuenta de Windows, algunos sitios pueden volver a entrar solos.\r\n\r\n¿Continuar?",
                "Cerrar sesión en todo", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
            if (answer != DialogResult.Yes)
            {
                return;
            }
            signOutRunning = true;
            try
            {
                await ClearProfileAsync(profileName);
                Log.Info("Datos de navegación borrados en el perfil " + profileName);
                foreach (BrowserTab tab in new List<BrowserTab>(OpenTabs))
                {
                    if (string.Equals(tab.ProfileName, profileName, StringComparison.OrdinalIgnoreCase))
                    {
                        tab.Reload();
                    }
                }
                MessageBox.Show(owner, "Listo. Se cerró la sesión en el perfil \"" + profileName + "\".", "Cerrar sesión en todo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                Log.Error("No se pudieron borrar los datos del perfil " + profileName, ex);
                MessageBox.Show(owner, "No se pudieron borrar los datos del perfil.\r\n\r\n" + ex.Message, "Cerrar sesión en todo", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                signOutRunning = false;
            }
        }

        private static async Task ClearProfileAsync(string profileName)
        {
            foreach (BrowserTab tab in OpenTabs)
            {
                if (!tab.IsClosed && string.Equals(tab.ProfileName, profileName, StringComparison.OrdinalIgnoreCase))
                {
                    await tab.Core.Profile.ClearBrowsingDataAsync();
                    return;
                }
            }
            using (Form hidden = new Form())
            {
                hidden.ShowInTaskbar = false;
                IntPtr handle = hidden.Handle;
                CoreWebView2Controller temporary = await BrowserEnvironment.CreateControllerAsync(handle, profileName);
                try
                {
                    await temporary.CoreWebView2.Profile.ClearBrowsingDataAsync();
                }
                finally
                {
                    temporary.Close();
                }
            }
        }

        private static void OnConfigReloaded(object sender, ConfigReloadedEventArgs e)
        {
            if (e.Error == null)
            {
                ApplyConfigToTabs();
            }
            EventHandler<ConfigReloadedEventArgs> handler = ConfigReloaded;
            if (handler != null)
            {
                handler(null, e);
            }
        }

        private static void ApplyConfigToTabs()
        {
            foreach (BrowserTab tab in new List<BrowserTab>(OpenTabs))
            {
                try
                {
                    tab.ApplyConfig(Config);
                }
                catch (Exception ex)
                {
                    Log.Error("No se pudo aplicar la configuración a una pestaña", ex);
                }
            }
        }
    }
}
