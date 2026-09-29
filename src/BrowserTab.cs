// MuroSOC - BrowserTab
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;

namespace MuroSoc
{
    internal sealed class BrowserTab
    {
        private ITabHost host;
        private CoreWebView2Controller controller;
        private Image favicon;
        private bool applyingZoom;

        private BrowserTab(ITabHost host, CoreWebView2Controller controller, TabModel settings, BrowserTab opener, bool isPopup)
        {
            this.host = host;
            this.controller = controller;
            Settings = settings;
            ProfileName = settings.Profile;
            Opener = opener;
            IsPopup = isPopup;
        }

        public TabModel Settings { get; private set; }

        public string ProfileName { get; private set; }

        public BrowserTab Opener { get; private set; }

        public bool IsPopup { get; private set; }

        public bool OpenedByScript
        {
            get { return Opener != null; }
        }

        public bool IsAtLogin { get; private set; }

        public string ErrorText { get; private set; }

        public bool IsRecovering { get; private set; }

        public string RefreshCountdownText
        {
            get { return null; }
        }

        public string RefreshStatusText
        {
            get { return null; }
        }

        public ITabHost Host
        {
            get { return host; }
        }

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
            get { return Core == null ? Settings.Url : Core.Source; }
        }

        public string Title
        {
            get { return Core == null ? Settings.Title : Core.DocumentTitle; }
        }

        public string DisplayTitle
        {
            get
            {
                string title = Title;
                if (!string.IsNullOrEmpty(title))
                {
                    return title;
                }
                string host = DomainMatcher.HostOf(Url);
                return host.Length > 0 ? host : "Pestaña nueva";
            }
        }

        public Image Favicon
        {
            get { return favicon; }
        }

        public bool CanGoBack
        {
            get { return Core != null && Core.CanGoBack; }
        }

        public bool CanGoForward
        {
            get { return Core != null && Core.CanGoForward; }
        }

        public static async Task<BrowserTab> CreateAsync(ITabHost host, TabModel settings, BrowserTab opener, bool isPopup)
        {
            TabModel model = settings == null ? new TabModel() : settings.Clone();
            model.Normalize();
            CoreWebView2Controller created = await BrowserEnvironment.CreateControllerAsync(host.ContentHost.Handle, model.Profile);
            BrowserTab tab = new BrowserTab(host, created, model, opener, isPopup);
            tab.Attach();
            App.RegisterTab(tab);
            return tab;
        }

        public TabModel Snapshot()
        {
            TabModel model = Settings.Clone();
            string url = Url;
            if (!string.IsNullOrEmpty(url))
            {
                model.Url = url;
            }
            model.Title = Title ?? string.Empty;
            model.Profile = ProfileName;
            if (controller != null && Settings.VirtualWidth == 0)
            {
                model.Zoom = controller.ZoomFactor;
            }
            return model;
        }

        public void MoveTo(ITabHost newHost)
        {
            host = newHost;
            if (controller != null)
            {
                controller.ParentWindow = newHost.ContentHost.Handle;
                controller.NotifyParentWindowPositionChanged();
            }
        }

        public void SetVisible(bool visible)
        {
            if (controller != null && controller.IsVisible != visible)
            {
                controller.IsVisible = visible;
            }
        }

        public void Focus()
        {
            if (controller != null)
            {
                controller.MoveFocus(CoreWebView2MoveFocusReason.Programmatic);
            }
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

        public void GoBack()
        {
            if (CanGoBack)
            {
                Core.GoBack();
            }
        }

        public void GoForward()
        {
            if (CanGoForward)
            {
                Core.GoForward();
            }
        }

        public void SetBounds(Rectangle bounds)
        {
            if (controller != null)
            {
                controller.Bounds = bounds;
                ApplyVirtualWidth();
            }
        }

        public void NotifyPositionChanged()
        {
            if (controller != null)
            {
                controller.NotifyParentWindowPositionChanged();
            }
        }

        public void SetZoom(double zoom)
        {
            Settings.VirtualWidth = 0;
            Settings.Zoom = Math.Max(0.25, Math.Min(5.0, zoom));
            ApplyZoom(Settings.Zoom);
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
            settings.IsZoomControlEnabled = true;

            CoreWebView2Profile profile = Core.Profile;
            profile.PreferredTrackingPreventionLevel = CoreWebView2TrackingPreventionLevel.Basic;
            profile.IsPasswordAutosaveEnabled = false;
            profile.IsGeneralAutofillEnabled = false;
            profile.DefaultDownloadFolderPath = AppPaths.DownloadsFolder;
            UpdateLoginState();
        }

        public void UpdateLoginState()
        {
            bool atLogin = !IsPopup && LoginDetector.IsLoginUrl(Url, App.Config.LoginUrlPatterns);
            if (atLogin == IsAtLogin)
            {
                return;
            }
            IsAtLogin = atLogin;
            Log.Info(atLogin ? "Pestaña en página de login: " + Log.SafeUrl(Url) : "Pestaña salió de la página de login: " + Log.SafeUrl(Url));
            RaiseStateChanged();
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
            try
            {
                closing.Close();
            }
            catch (Exception ex)
            {
                Log.Error("Error al cerrar el controlador de WebView2", ex);
            }
            if (favicon != null)
            {
                favicon.Dispose();
                favicon = null;
            }
        }

        private void Attach()
        {
            controller.DefaultBackgroundColor = Color.White;
            controller.AcceleratorKeyPressed += OnAcceleratorKeyPressed;
            controller.GotFocus += delegate { host.OnTabFocused(this); };
            controller.ZoomFactorChanged += OnZoomFactorChanged;
            ApplyConfig(App.Config);
            ApplyZoom(Settings.Zoom);
            CoreWebView2 core = controller.CoreWebView2;
            core.PermissionRequested += OnPermissionRequested;
            core.DownloadStarting += OnDownloadStarting;
            core.NavigationStarting += OnNavigationStarting;
            core.LaunchingExternalUriScheme += OnLaunchingExternalUriScheme;
            core.ContextMenuRequested += OnContextMenuRequested;
            core.NewWindowRequested += OnNewWindowRequested;
            core.WindowCloseRequested += OnWindowCloseRequested;
            core.FaviconChanged += OnFaviconChanged;
            core.HistoryChanged += delegate { RaiseStateChanged(); };
            core.DocumentTitleChanged += delegate { RaiseStateChanged(); };
            core.SourceChanged += delegate { UpdateLoginState(); RaiseStateChanged(); };
            core.NavigationCompleted += delegate { UpdateLoginState(); RaiseStateChanged(); };
        }

        private void ApplyZoom(double zoom)
        {
            if (controller == null)
            {
                return;
            }
            applyingZoom = true;
            try
            {
                if (Math.Abs(controller.ZoomFactor - zoom) > 0.001)
                {
                    controller.ZoomFactor = zoom;
                }
            }
            finally
            {
                applyingZoom = false;
            }
        }

        private void ApplyVirtualWidth()
        {
            if (controller == null || Settings.VirtualWidth <= 0)
            {
                return;
            }
            Rectangle bounds = controller.Bounds;
            if (bounds.Width <= 0)
            {
                return;
            }
            double scale = controller.RasterizationScale <= 0 ? 1.0 : controller.RasterizationScale;
            double zoom = bounds.Width / (scale * Settings.VirtualWidth);
            ApplyZoom(Math.Max(0.25, Math.Min(5.0, zoom)));
        }

        private void OnZoomFactorChanged(object sender, object e)
        {
            if (applyingZoom || controller == null)
            {
                return;
            }
            Settings.VirtualWidth = 0;
            Settings.Zoom = controller.ZoomFactor;
            RaiseStateChanged();
        }

        private void OnAcceleratorKeyPressed(object sender, CoreWebView2AcceleratorKeyPressedEventArgs e)
        {
            if (e.KeyEventKind != CoreWebView2KeyEventKind.KeyDown && e.KeyEventKind != CoreWebView2KeyEventKind.SystemKeyDown)
            {
                return;
            }
            Keys keys = (Keys)e.VirtualKey | Control.ModifierKeys;
            if (e.PhysicalKeyStatus.WasKeyDown != 0)
            {
                if (Shortcuts.Match(keys) != null || keys == Keys.Escape)
                {
                    e.Handled = true;
                }
                return;
            }
            if (Wall.TryHandleKey(keys))
            {
                e.Handled = true;
            }
        }

        private async void OnFaviconChanged(object sender, object e)
        {
            CoreWebView2 core = Core;
            if (core == null)
            {
                return;
            }
            try
            {
                if (string.IsNullOrEmpty(core.FaviconUri))
                {
                    SetFavicon(null);
                    return;
                }
                using (Stream stream = await core.GetFaviconAsync(CoreWebView2FaviconImageFormat.Png))
                {
                    if (stream == null || IsClosed)
                    {
                        return;
                    }
                    MemoryStream copy = new MemoryStream();
                    stream.CopyTo(copy);
                    if (copy.Length == 0)
                    {
                        SetFavicon(null);
                        return;
                    }
                    copy.Position = 0;
                    using (Image image = Image.FromStream(copy))
                    {
                        SetFavicon(new Bitmap(image));
                    }
                }
            }
            catch (Exception)
            {
                SetFavicon(null);
            }
        }

        private void SetFavicon(Image image)
        {
            Image old = favicon;
            favicon = image;
            if (old != null)
            {
                old.Dispose();
            }
            RaiseStateChanged();
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
            List<MenuEntry> entries = host.BuildMenu(this);
            if (entries == null || entries.Count == 0)
            {
                return;
            }
            List<MenuEntry> root = new List<MenuEntry>();
            root.Add(MenuEntry.Separator());
            root.Add(MenuEntry.Sub("Muro SOC", entries));
            MenuEntry.AddToWebView(environment, e.MenuItems, root, host.ContentHost);
        }

        private async void OnNewWindowRequested(object sender, CoreWebView2NewWindowRequestedEventArgs e)
        {
            CoreWebView2Deferral deferral = e.GetDeferral();
            try
            {
                CoreWebView2WindowFeatures features = e.WindowFeatures;
                bool popup = features != null && features.HasSize;
                BrowserTab created;
                if (popup && !App.Config.PopupsAsTabs)
                {
                    created = await PopupWindow.OpenAsync(this, features);
                }
                else
                {
                    created = await host.OpenScriptTabAsync(this);
                }
                if (created != null && created.Core != null)
                {
                    e.NewWindow = created.Core;
                    e.Handled = true;
                    Log.Info((popup ? "Popup abierto" : "Pestaña abierta por script") + " hacia " + Log.SafeUrl(e.Uri));
                }
            }
            catch (Exception ex)
            {
                Log.Error("No se pudo abrir la ventana solicitada por la página", ex);
            }
            finally
            {
                deferral.Complete();
            }
        }

        private void OnWindowCloseRequested(object sender, object e)
        {
            if (!OpenedByScript)
            {
                Log.Info("La página pidió cerrar una pestaña principal; se ignora");
                return;
            }
            BrowserTab opener = Opener;
            host.CloseScriptTab(this);
            if (opener != null && !opener.IsClosed)
            {
                opener.host.FocusHost();
                opener.Focus();
            }
        }

        private void RaiseNotice(NoticeEventArgs notice)
        {
            host.ShowNotice(this, notice);
        }

        private void RaiseStateChanged()
        {
            if (controller != null)
            {
                host.OnTabStateChanged(this);
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
