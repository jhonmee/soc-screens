// MuroSOC - BrowserTab
using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;

namespace MuroSoc
{
    internal sealed class BrowserTab
    {
        private readonly Control host;
        private CoreWebView2Controller controller;

        private BrowserTab(Control host, CoreWebView2Controller controller, string profileName)
        {
            this.host = host;
            this.controller = controller;
            ProfileName = profileName;
        }

        public event EventHandler<NoticeEventArgs> NoticeRequested;

        public event EventHandler TitleChanged;

        public string ProfileName { get; private set; }

        public CoreWebView2Controller Controller
        {
            get { return controller; }
        }

        public CoreWebView2 Core
        {
            get { return controller == null ? null : controller.CoreWebView2; }
        }

        public bool IsClosed
        {
            get { return controller == null; }
        }

        public string Url
        {
            get { return Core == null ? string.Empty : Core.Source; }
        }

        public string Title
        {
            get { return Core == null ? string.Empty : Core.DocumentTitle; }
        }

        public static async Task<BrowserTab> CreateAsync(Control host, string profileName)
        {
            string profile = AppConfig.IsValidProfileName(profileName) ? profileName : BrowserEnvironment.DefaultProfile;
            CoreWebView2Controller created = await BrowserEnvironment.CreateControllerAsync(host.Handle, profile);
            BrowserTab tab = new BrowserTab(host, created, profile);
            tab.Attach();
            App.RegisterTab(tab);
            return tab;
        }

        public void Navigate(string url)
        {
            if (Core == null || string.IsNullOrEmpty(url))
            {
                return;
            }
            try
            {
                Core.Navigate(url);
            }
            catch (ArgumentException)
            {
                RaiseNotice(new NoticeEventArgs(NoticeLevel.Warning, "La dirección no es válida: " + url));
            }
        }

        public void Reload()
        {
            if (Core != null)
            {
                Core.Reload();
            }
        }

        public void SetBounds(Rectangle bounds)
        {
            if (controller != null)
            {
                controller.Bounds = bounds;
            }
        }

        public void NotifyPositionChanged()
        {
            if (controller != null)
            {
                controller.NotifyParentWindowPositionChanged();
            }
        }

        public void ApplyConfig(AppConfig config)
        {
            if (Core == null)
            {
                return;
            }
            CoreWebView2Settings settings = Core.Settings;
            settings.AreDevToolsEnabled = config.DevToolsEnabled;
            settings.AreDefaultContextMenusEnabled = true;
            settings.AreDefaultScriptDialogsEnabled = true;
            settings.IsPasswordAutosaveEnabled = false;
            settings.IsGeneralAutofillEnabled = false;
            settings.IsReputationCheckingRequired = true;
            settings.AreHostObjectsAllowed = false;
            settings.IsBuiltInErrorPageEnabled = true;

            CoreWebView2Profile profile = Core.Profile;
            profile.PreferredTrackingPreventionLevel = CoreWebView2TrackingPreventionLevel.Basic;
            profile.IsPasswordAutosaveEnabled = false;
            profile.IsGeneralAutofillEnabled = false;
            profile.DefaultDownloadFolderPath = AppPaths.DownloadsFolder;
        }

        public void Close()
        {
            if (controller == null)
            {
                return;
            }
            App.UnregisterTab(this);
            CoreWebView2Controller closing = controller;
            controller = null;
            closing.Close();
        }

        private void Attach()
        {
            controller.DefaultBackgroundColor = Color.Black;
            ApplyConfig(App.Config);
            CoreWebView2 core = controller.CoreWebView2;
            core.PermissionRequested += OnPermissionRequested;
            core.DownloadStarting += OnDownloadStarting;
            core.NavigationStarting += OnNavigationStarting;
            core.LaunchingExternalUriScheme += OnLaunchingExternalUriScheme;
            core.ContextMenuRequested += OnContextMenuRequested;
            core.DocumentTitleChanged += delegate { RaiseTitleChanged(); };
            core.SourceChanged += delegate { RaiseTitleChanged(); };
        }

        private void OnPermissionRequested(object sender, CoreWebView2PermissionRequestedEventArgs e)
        {
            bool allowed = SecurityPolicy.IsPermissionAllowed(App.Config, e.Uri, e.PermissionKind);
            e.SavesInProfile = false;
            e.State = allowed ? CoreWebView2PermissionState.Allow : CoreWebView2PermissionState.Deny;
            e.Handled = true;
            Log.Info("Permiso " + SecurityPolicy.PermissionName(e.PermissionKind) + (allowed ? " permitido" : " denegado") + " para " + Log.SafeUrl(e.Uri));
        }

        private void OnDownloadStarting(object sender, CoreWebView2DownloadStartingEventArgs e)
        {
            string source = e.DownloadOperation == null ? string.Empty : e.DownloadOperation.Uri;
            if (!App.Config.DownloadsEnabled)
            {
                e.Cancel = true;
                e.Handled = true;
                Log.Info("Descarga bloqueada desde " + Log.SafeUrl(source));
                RaiseNotice(new NoticeEventArgs(NoticeLevel.Info, "Descarga bloqueada. Las descargas están desactivadas en la configuración."));
                return;
            }
            try
            {
                Directory.CreateDirectory(AppPaths.DownloadsFolder);
                string name = Path.GetFileName(e.ResultFilePath);
                if (string.IsNullOrEmpty(name))
                {
                    name = "descarga";
                }
                e.ResultFilePath = UniquePath(Path.Combine(AppPaths.DownloadsFolder, name));
                Log.Info("Descarga permitida desde " + Log.SafeUrl(source));
            }
            catch (Exception ex)
            {
                e.Cancel = true;
                Log.Error("No se pudo preparar la descarga", ex);
            }
        }

        private void OnNavigationStarting(object sender, CoreWebView2NavigationStartingEventArgs e)
        {
            if (SecurityPolicy.IsNavigationAllowed(App.Config, e.Uri))
            {
                return;
            }
            e.Cancel = true;
            string url = e.Uri;
            string host = DomainMatcher.HostOf(url);
            Log.Info("Navegación fuera de la lista permitida: " + Log.SafeUrl(url));
            NoticeEventArgs notice = new NoticeEventArgs(NoticeLevel.Warning, "Este sitio no está en la lista permitida: " + (host.Length > 0 ? host : url));
            notice.ActionText = "Abrir en Edge";
            notice.Action = delegate { ExternalBrowser.OpenInEdge(url); };
            RaiseNotice(notice);
        }

        private void OnLaunchingExternalUriScheme(object sender, CoreWebView2LaunchingExternalUriSchemeEventArgs e)
        {
            e.Cancel = true;
            string scheme = e.Uri;
            int colon = scheme.IndexOf(':');
            if (colon > 0)
            {
                scheme = scheme.Substring(0, colon);
            }
            Log.Info("Bloqueado protocolo externo " + scheme + ": desde " + Log.SafeUrl(e.InitiatingOrigin));
            RaiseNotice(new NoticeEventArgs(NoticeLevel.Info, "Se bloqueó abrir una aplicación externa (" + scheme + ":)."));
        }

        private void OnContextMenuRequested(object sender, CoreWebView2ContextMenuRequestedEventArgs e)
        {
            CoreWebView2Environment environment = BrowserEnvironment.Current;
            if (environment == null)
            {
                return;
            }
            string pageUrl = Url;
            CoreWebView2ContextMenuItem root = environment.CreateContextMenuItem("Muro SOC", null, CoreWebView2ContextMenuItemKind.Submenu);
            AddCommand(environment, root, "Abrir esta página en Edge", delegate { ExternalBrowser.OpenInEdge(pageUrl); });
            CoreWebView2ContextMenuItem signOut = environment.CreateContextMenuItem("Cerrar sesión en todo", null, CoreWebView2ContextMenuItemKind.Submenu);
            foreach (string profile in App.Config.Profiles)
            {
                string target = profile;
                string label = profile == ProfileName ? "Perfil " + profile + " (esta pestaña)..." : "Perfil " + profile + "...";
                AddCommand(environment, signOut, label, delegate { App.SignOutProfile(host.FindForm(), target); });
            }
            root.Children.Add(signOut);
            AddCommand(environment, root, "Acerca de Muro SOC", delegate { App.ShowAbout(host.FindForm()); });
            e.MenuItems.Add(environment.CreateContextMenuItem(string.Empty, null, CoreWebView2ContextMenuItemKind.Separator));
            e.MenuItems.Add(root);
        }

        private void AddCommand(CoreWebView2Environment environment, CoreWebView2ContextMenuItem parent, string label, MethodInvoker action)
        {
            CoreWebView2ContextMenuItem item = environment.CreateContextMenuItem(label, null, CoreWebView2ContextMenuItemKind.Command);
            item.CustomItemSelected += delegate
            {
                if (!host.IsDisposed)
                {
                    host.BeginInvoke(action);
                }
            };
            parent.Children.Add(item);
        }

        private void RaiseNotice(NoticeEventArgs notice)
        {
            EventHandler<NoticeEventArgs> handler = NoticeRequested;
            if (handler != null)
            {
                handler(this, notice);
            }
        }

        private void RaiseTitleChanged()
        {
            EventHandler handler = TitleChanged;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        private static string UniquePath(string path)
        {
            if (!File.Exists(path))
            {
                return path;
            }
            string folder = Path.GetDirectoryName(path);
            string stem = Path.GetFileNameWithoutExtension(path);
            string extension = Path.GetExtension(path);
            for (int i = 1; i < 1000; i++)
            {
                string candidate = Path.Combine(folder, stem + " (" + i + ")" + extension);
                if (!File.Exists(candidate))
                {
                    return candidate;
                }
            }
            return Path.Combine(folder, stem + " " + Guid.NewGuid().ToString("N") + extension);
        }
    }
}
