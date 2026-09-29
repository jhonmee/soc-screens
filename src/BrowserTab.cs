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
        private string lastNonLoginUrl;
        private string scriptId;
        private DateTime lastInteraction = DateTime.MinValue;
        private DateTime nextRefreshAt = DateTime.MinValue;
        private DateTime autoReloadAt = DateTime.MinValue;
        private bool refreshCheckRunning;
        private bool scriptDialogOpen;
        private string pauseReason;
        private string pickerMode;
        private bool closed;
        private bool recovering;
        private Rectangle lastBounds;
        private bool wantVisible;
        private string lastKnownUrl;
        private string lastKnownTitle;
        private string attemptedUrl;
        private bool navigating;
        private DateTime navigationStartedAt = DateTime.MinValue;
        private string errorReason;
        private bool errorRetryable;
        private int retryAttempt;
        private DateTime retryAt = DateTime.MinValue;
        private DateTime nextWatchdogAt = DateTime.MinValue;
        private bool watchdogRunning;
        private int watchdogFailures;

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

        public string ErrorText
        {
            get
            {
                if (errorReason == null)
                {
                    return null;
                }
                if (!errorRetryable)
                {
                    return errorReason;
                }
                int seconds = Math.Max(0, (int)Math.Ceiling((retryAt - DateTime.UtcNow).TotalSeconds));
                return errorReason + " · reintento en " + FormatSpan(seconds);
            }
        }

        public bool IsRecovering
        {
            get { return recovering; }
        }

        public bool IsHovered { get; private set; }

        public bool IsScriptDialogOpen
        {
            get { return scriptDialogOpen; }
        }

        public string RefreshCountdownText
        {
            get
            {
                if (Settings.AutoRefreshSeconds <= 0 || IsClosed)
                {
                    return null;
                }
                if (pauseReason != null)
                {
                    return "⏸";
                }
                return "⟳ " + FormatSpan(SecondsToRefresh());
            }
        }

        public string RefreshStatusText
        {
            get
            {
                if (Settings.AutoRefreshSeconds <= 0)
                {
                    return null;
                }
                string every = "Autorefresh cada " + FormatInterval(Settings.AutoRefreshSeconds);
                if (pauseReason != null)
                {
                    return every + " · en pausa: " + pauseReason;
                }
                return every + " · próximo en " + FormatSpan(SecondsToRefresh());
            }
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
            get { return closed; }
        }

        public string Url
        {
            get
            {
                CoreWebView2 core = Core;
                if (core != null)
                {
                    try
                    {
                        return core.Source;
                    }
                    catch (Exception)
                    {
                    }
                }
                return lastKnownUrl ?? Settings.Url;
            }
        }

        public string Title
        {
            get
            {
                CoreWebView2 core = Core;
                if (core != null)
                {
                    try
                    {
                        return core.DocumentTitle;
                    }
                    catch (Exception)
                    {
                    }
                }
                return lastKnownTitle ?? Settings.Title;
            }
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
            try
            {
                await tab.RegisterScriptAsync();
            }
            catch (Exception ex)
            {
                Log.Error("No se pudo registrar el script de la app en la pestaña", ex);
            }
            return tab;
        }

        public async void UpdatePageSettings()
        {
            Settings.Normalize();
            try
            {
                await RegisterScriptAsync();
                if (Core != null)
                {
                    await Core.ExecuteScriptAsync("window.__muroSoc && window.__muroSoc.apply(" + PageScript.ConfigJson(Settings) + ")");
                }
            }
            catch (Exception ex)
            {
                Log.Error("No se pudieron aplicar los ajustes de página", ex);
            }
            ApplyVirtualWidth();
            RaiseStateChanged();
            Wall.MarkDirty();
        }

        public void SetAutoRefresh(int seconds)
        {
            Settings.AutoRefreshSeconds = seconds <= 0 ? 0 : Math.Max(30, seconds);
            nextRefreshAt = Settings.AutoRefreshSeconds > 0 ? DateTime.UtcNow.AddSeconds(Settings.AutoRefreshSeconds) : DateTime.MinValue;
            pauseReason = null;
            RaiseStateChanged();
            Wall.MarkDirty();
        }

        public void Tick(DateTime now)
        {
            if (closed || recovering)
            {
                return;
            }
            if (errorReason != null && errorRetryable && now >= retryAt)
            {
                RetryNavigation();
            }
            if (now >= nextWatchdogAt && !watchdogRunning && !scriptDialogOpen && controller != null)
            {
                bool longNavigation = navigating && (now - navigationStartedAt).TotalSeconds < 60;
                if (!longNavigation)
                {
                    RunWatchdog();
                }
            }
            TickRefresh(now);
        }

        private void TickRefresh(DateTime now)
        {
            if (Settings.AutoRefreshSeconds <= 0 || IsClosed || IsPopup)
            {
                pauseReason = null;
                return;
            }
            if (nextRefreshAt == DateTime.MinValue)
            {
                nextRefreshAt = now.AddSeconds(Settings.AutoRefreshSeconds);
            }
            string reason = null;
            if (IsAtLogin)
            {
                reason = "página de login";
            }
            else if (ErrorText != null || IsRecovering)
            {
                reason = "sin conexión";
            }
            else if (scriptDialogOpen)
            {
                reason = "diálogo abierto";
            }
            else if ((now - lastInteraction).TotalSeconds < 120)
            {
                reason = "en uso";
            }
            if (reason != null)
            {
                pauseReason = reason;
                if (now >= nextRefreshAt)
                {
                    nextRefreshAt = now.AddSeconds(10);
                }
                return;
            }
            if (pauseReason != null && pauseReason != "texto sin enviar" && pauseReason != "página ocupada" && pauseReason != "cambios sin guardar")
            {
                pauseReason = null;
            }
            if (now < nextRefreshAt || refreshCheckRunning)
            {
                return;
            }
            CheckAndRefresh();
        }

        public TabModel Snapshot()
        {
            TabModel model = Settings.Clone();
            string url = IsAtLogin && !string.IsNullOrEmpty(lastNonLoginUrl) ? lastNonLoginUrl : Url;
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
                try
                {
                    controller.ParentWindow = newHost.ContentHost.Handle;
                    controller.NotifyParentWindowPositionChanged();
                }
                catch (Exception ex)
                {
                    Log.Error("No se pudo mover el controlador de la pestaña", ex);
                }
            }
        }

        public void SetVisible(bool visible)
        {
            wantVisible = visible;
            if (controller == null)
            {
                return;
            }
            try
            {
                if (controller.IsVisible != visible)
                {
                    controller.IsVisible = visible;
                }
            }
            catch (Exception ex)
            {
                Log.Error("No se pudo cambiar la visibilidad de la pestaña", ex);
            }
        }

        public void Focus()
        {
            if (controller != null)
            {
                try
                {
                    controller.MoveFocus(CoreWebView2MoveFocusReason.Programmatic);
                }
                catch (Exception)
                {
                }
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
            if (Core == null)
            {
                return;
            }
            try
            {
                if (errorReason != null && !string.IsNullOrEmpty(attemptedUrl))
                {
                    Core.Navigate(attemptedUrl);
                }
                else
                {
                    Core.Reload();
                }
            }
            catch (Exception ex)
            {
                Log.Error("No se pudo recargar la pestaña", ex);
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
            lastBounds = bounds;
            if (controller == null)
            {
                return;
            }
            try
            {
                controller.Bounds = bounds;
                ApplyVirtualWidth();
            }
            catch (Exception ex)
            {
                Log.Error("No se pudo ajustar el tamaño de la pestaña", ex);
            }
        }

        public void NotifyPositionChanged()
        {
            if (controller == null)
            {
                return;
            }
            try
            {
                controller.NotifyParentWindowPositionChanged();
            }
            catch (Exception)
            {
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
            settings.AreDefaultScriptDialogsEnabled = false;
            settings.IsWebMessageEnabled = true;
            settings.IsScriptEnabled = true;
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
            if (!atLogin && !string.IsNullOrEmpty(Url) && Url != "about:blank")
            {
                lastNonLoginUrl = Url;
            }
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
            if (closed)
            {
                return;
            }
            closed = true;
            App.UnregisterTab(this);
            CoreWebView2Controller closing = controller;
            controller = null;
            if (closing == null)
            {
                return;
            }
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
            controller.GotFocus += delegate
            {
                lastInteraction = DateTime.UtcNow;
                host.OnTabFocused(this);
            };
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
            core.WebMessageReceived += OnWebMessageReceived;
            core.ScriptDialogOpening += OnScriptDialogOpening;
            core.ProcessFailed += OnProcessFailed;
            core.NavigationCompleted += OnNavigationCompleted;
            core.HistoryChanged += delegate { RaiseStateChanged(); };
            core.DocumentTitleChanged += delegate
            {
                lastKnownTitle = Title;
                RaiseStateChanged();
            };
            core.SourceChanged += delegate
            {
                string source = Url;
                if (!string.IsNullOrEmpty(source) && source != "about:blank")
                {
                    lastKnownUrl = source;
                }
                UpdateLoginState();
                RaiseStateChanged();
            };
        }

        private void OnNavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            navigating = false;
            if (e.IsSuccess)
            {
                if (errorReason != null)
                {
                    Log.Info("Pestaña recuperada tras error de red: " + Log.SafeUrl(Url));
                }
                errorReason = null;
                errorRetryable = false;
                retryAttempt = 0;
                watchdogFailures = 0;
            }
            else
            {
                ClassifyError(e.WebErrorStatus);
            }
            UpdateLoginState();
            RaiseStateChanged();
        }

        private void ClassifyError(CoreWebView2WebErrorStatus status)
        {
            string reason = null;
            bool retry = true;
            switch (status)
            {
                case CoreWebView2WebErrorStatus.Disconnected:
                    reason = "sin red";
                    break;
                case CoreWebView2WebErrorStatus.HostNameNotResolved:
                    reason = "no se resuelve el nombre (DNS)";
                    break;
                case CoreWebView2WebErrorStatus.CannotConnect:
                case CoreWebView2WebErrorStatus.ServerUnreachable:
                    reason = "no se puede conectar al servidor";
                    break;
                case CoreWebView2WebErrorStatus.Timeout:
                    reason = "tiempo de espera agotado";
                    break;
                case CoreWebView2WebErrorStatus.ConnectionAborted:
                case CoreWebView2WebErrorStatus.ConnectionReset:
                    reason = "la conexión se cortó";
                    break;
                case CoreWebView2WebErrorStatus.ErrorHttpInvalidServerResponse:
                    reason = "respuesta inválida del servidor";
                    break;
                case CoreWebView2WebErrorStatus.CertificateCommonNameIsIncorrect:
                case CoreWebView2WebErrorStatus.CertificateExpired:
                case CoreWebView2WebErrorStatus.ClientCertificateContainsErrors:
                case CoreWebView2WebErrorStatus.CertificateRevoked:
                case CoreWebView2WebErrorStatus.CertificateIsInvalid:
                    reason = "certificado no válido";
                    retry = false;
                    break;
            }
            if (reason == null)
            {
                return;
            }
            errorRetryable = retry;
            if (!retry)
            {
                errorReason = "Error: " + reason;
                Log.Warn("Error de certificado en " + Log.SafeUrl(attemptedUrl));
                return;
            }
            int[] delays = new int[] { 10, 30, 60, 300 };
            int delay = delays[Math.Min(retryAttempt, delays.Length - 1)];
            retryAttempt++;
            errorReason = "Sin conexión: " + reason;
            retryAt = DateTime.UtcNow.AddSeconds(delay);
            Log.Warn("Error de red (" + status + ") en " + Log.SafeUrl(attemptedUrl) + "; reintento " + retryAttempt + " en " + delay + " s");
        }

        private void RetryNavigation()
        {
            retryAt = DateTime.UtcNow.AddSeconds(300);
            string url = !string.IsNullOrEmpty(attemptedUrl) ? attemptedUrl : Url;
            if (Core == null || string.IsNullOrEmpty(url))
            {
                return;
            }
            try
            {
                Core.Navigate(url);
            }
            catch (Exception ex)
            {
                Log.Error("No se pudo reintentar la navegación", ex);
            }
        }

        private async void RunWatchdog()
        {
            watchdogRunning = true;
            nextWatchdogAt = DateTime.UtcNow.AddSeconds(30);
            try
            {
                CoreWebView2 core = Core;
                if (core == null)
                {
                    return;
                }
                Task<string> probe = core.ExecuteScriptAsync("1");
                Task finished = await Task.WhenAny(probe, Task.Delay(30000));
                if (closed || recovering || core != Core)
                {
                    return;
                }
                if (finished == probe && !probe.IsFaulted)
                {
                    watchdogFailures = 0;
                    return;
                }
                if (scriptDialogOpen)
                {
                    return;
                }
                watchdogFailures++;
                Log.Warn("La pestaña no responde (" + watchdogFailures + "): " + Log.SafeUrl(Url));
                if (watchdogFailures == 1)
                {
                    Reload();
                }
                else
                {
                    Recover("la página no responde");
                }
            }
            catch (Exception ex)
            {
                Log.Error("Fallo el watchdog de la pestaña", ex);
            }
            finally
            {
                watchdogRunning = false;
                nextWatchdogAt = DateTime.UtcNow.AddSeconds(30);
            }
        }

        private void OnProcessFailed(object sender, CoreWebView2ProcessFailedEventArgs e)
        {
            CoreWebView2ProcessFailedKind kind = e.ProcessFailedKind;
            switch (kind)
            {
                case CoreWebView2ProcessFailedKind.BrowserProcessExited:
                    Log.Warn("Se cerró el proceso del navegador (" + e.ExitCode + "); recreando pestaña");
                    BrowserEnvironment.Reset(BrowserEnvironment.Current);
                    Recover("se cerró el proceso del navegador");
                    break;
                case CoreWebView2ProcessFailedKind.RenderProcessExited:
                    Log.Warn("Se cerró el proceso de la página (" + e.ExitCode + "); recreando pestaña");
                    Recover("se cerró el proceso de la página");
                    break;
                case CoreWebView2ProcessFailedKind.RenderProcessUnresponsive:
                    Log.Warn("La página no responde; recreando pestaña");
                    Recover("la página no responde");
                    break;
                default:
                    Log.Warn("Proceso de WebView2 terminado: " + kind + " (" + e.ExitCode + ")");
                    break;
            }
        }

        private async void Recover(string reason)
        {
            if (recovering || closed)
            {
                return;
            }
            recovering = true;
            string url = lastNonLoginUrl ?? lastKnownUrl ?? Settings.Url;
            host.OnTabStateChanged(this);
            CoreWebView2Controller old = controller;
            controller = null;
            scriptId = null;
            if (old != null)
            {
                try
                {
                    old.Close();
                }
                catch (Exception)
                {
                }
            }
            for (int attempt = 0; !closed; attempt++)
            {
                try
                {
                    await Task.Delay(attempt == 0 ? 1000 : Math.Min(30000, 3000 * attempt));
                    if (closed)
                    {
                        return;
                    }
                    CoreWebView2Controller created = await BrowserEnvironment.CreateControllerAsync(host.ContentHost.Handle, ProfileName);
                    if (closed)
                    {
                        created.Close();
                        return;
                    }
                    controller = created;
                    Attach();
                    await RegisterScriptAsync();
                    created.Bounds = lastBounds;
                    created.IsVisible = wantVisible;
                    ApplyVirtualWidth();
                    errorReason = null;
                    watchdogFailures = 0;
                    nextWatchdogAt = DateTime.UtcNow.AddSeconds(60);
                    Log.Info("Pestaña recreada (" + reason + "): " + Log.SafeUrl(url));
                    Navigate(string.IsNullOrEmpty(url) ? App.Config.HomeUrl : url);
                    break;
                }
                catch (Exception ex)
                {
                    Log.Error("No se pudo recrear la pestaña (intento " + (attempt + 1) + ")", ex);
                    if (controller != null)
                    {
                        CoreWebView2Controller failed = controller;
                        controller = null;
                        try
                        {
                            failed.Close();
                        }
                        catch (Exception)
                        {
                        }
                    }
                    if (attempt == 2)
                    {
                        BrowserEnvironment.Reset(BrowserEnvironment.Current);
                    }
                }
            }
            recovering = false;
            if (!closed)
            {
                host.OnTabStateChanged(this);
            }
        }

        private async Task RegisterScriptAsync()
        {
            CoreWebView2 core = Core;
            if (core == null)
            {
                return;
            }
            if (scriptId != null)
            {
                core.RemoveScriptToExecuteOnDocumentCreated(scriptId);
                scriptId = null;
            }
            scriptId = await core.AddScriptToExecuteOnDocumentCreatedAsync(PageScript.Build(Settings));
        }

        private async void CheckAndRefresh()
        {
            refreshCheckRunning = true;
            try
            {
                CoreWebView2 core = Core;
                if (core == null)
                {
                    return;
                }
                Task<string> check = core.ExecuteScriptAsync("window.__muroSoc ? window.__muroSoc.busy() : null");
                Task finished = await Task.WhenAny(check, Task.Delay(5000));
                DateTime now = DateTime.UtcNow;
                if (finished != check)
                {
                    pauseReason = "página ocupada";
                    nextRefreshAt = now.AddSeconds(30);
                    return;
                }
                BusyState busy = PageScript.Parse<BusyState>(check.Result);
                if (busy != null && (busy.Typed || busy.Dialog))
                {
                    pauseReason = busy.Typed ? "texto sin enviar" : "diálogo abierto";
                    nextRefreshAt = now.AddSeconds(30);
                    return;
                }
                if (IsClosed || IsAtLogin || scriptDialogOpen || (now - lastInteraction).TotalSeconds < 120)
                {
                    return;
                }
                pauseReason = null;
                autoReloadAt = now;
                nextRefreshAt = now.AddSeconds(Settings.AutoRefreshSeconds);
                Core.Reload();
            }
            catch (Exception ex)
            {
                Log.Error("Fallo el autorefresh", ex);
                nextRefreshAt = DateTime.UtcNow.AddSeconds(Math.Max(30, Settings.AutoRefreshSeconds));
            }
            finally
            {
                refreshCheckRunning = false;
            }
        }

        private int SecondsToRefresh()
        {
            if (nextRefreshAt == DateTime.MinValue)
            {
                return Settings.AutoRefreshSeconds;
            }
            return Math.Max(0, (int)Math.Ceiling((nextRefreshAt - DateTime.UtcNow).TotalSeconds));
        }

        private static string FormatSpan(int seconds)
        {
            return (seconds / 60) + ":" + (seconds % 60).ToString("00");
        }

        public static string FormatInterval(int seconds)
        {
            if (seconds % 60 == 0)
            {
                int minutes = seconds / 60;
                return minutes == 1 ? "1 minuto" : minutes + " minutos";
            }
            return seconds + " s";
        }

        private void OnWebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            PageMessage message = PageScript.Parse<PageMessage>(e.WebMessageAsJson);
            if (message == null || message.Type == null)
            {
                return;
            }
            switch (message.Type)
            {
                case "activity":
                    lastInteraction = DateTime.UtcNow;
                    break;
                case "hover":
                    if (IsHovered != message.Value)
                    {
                        IsHovered = message.Value;
                        RaiseStateChanged();
                    }
                    break;
                default:
                    HandlePageMessage(message);
                    break;
            }
        }

        private void OnScriptDialogOpening(object sender, CoreWebView2ScriptDialogOpeningEventArgs e)
        {
            CoreWebView2Deferral deferral = e.GetDeferral();
            scriptDialogOpen = true;
            CoreWebView2ScriptDialogKind kind = e.Kind;
            string message = e.Message ?? string.Empty;
            string defaultText = e.DefaultText ?? string.Empty;
            string origin = DomainMatcher.HostOf(e.Uri);
            bool automatic = (DateTime.UtcNow - autoReloadAt).TotalSeconds < 10;
            host.ContentHost.BeginInvoke((MethodInvoker)delegate
            {
                try
                {
                    IWin32Window owner = host.ContentHost.FindForm();
                    string title = "Mensaje de " + (origin.Length > 0 ? origin : "la página");
                    switch (kind)
                    {
                        case CoreWebView2ScriptDialogKind.Alert:
                            MessageBox.Show(owner, message, title, MessageBoxButtons.OK, MessageBoxIcon.Information);
                            e.Accept();
                            break;
                        case CoreWebView2ScriptDialogKind.Confirm:
                            if (MessageBox.Show(owner, message, title, MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == DialogResult.OK)
                            {
                                e.Accept();
                            }
                            break;
                        case CoreWebView2ScriptDialogKind.Prompt:
                            string text = InputDialog.Prompt(owner, title, message, defaultText, false);
                            if (text != null)
                            {
                                e.ResultText = text;
                                e.Accept();
                            }
                            break;
                        case CoreWebView2ScriptDialogKind.Beforeunload:
                            if (automatic)
                            {
                                pauseReason = "cambios sin guardar";
                                Log.Info("Autorefresh cancelado: la página tiene cambios sin guardar");
                                break;
                            }
                            if (MessageBox.Show(owner, "¿Salir de esta página? Los cambios que no guardaste se pueden perder.", title, MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) == DialogResult.Yes)
                            {
                                e.Accept();
                            }
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Log.Error("Error mostrando un diálogo de la página", ex);
                }
                finally
                {
                    scriptDialogOpen = false;
                    lastInteraction = DateTime.UtcNow;
                    deferral.Complete();
                }
            });
        }

        public async void StartPicker(string mode)
        {
            if (Core == null)
            {
                return;
            }
            pickerMode = mode == "hide" ? "hide" : "isolate";
            try
            {
                Focus();
                await Core.ExecuteScriptAsync("window.__muroSoc && window.__muroSoc.pick(" + (pickerMode == "hide" ? "'hide'" : "'isolate'") + ")");
            }
            catch (Exception ex)
            {
                pickerMode = null;
                Log.Error("No se pudo iniciar el selector de elementos", ex);
            }
        }

        public async void CapturePan()
        {
            if (Core == null)
            {
                return;
            }
            try
            {
                string result = await Core.ExecuteScriptAsync("window.__muroSoc ? window.__muroSoc.capturePan() : null");
                PanState pan = PageScript.Parse<PanState>(result);
                if (pan == null)
                {
                    RaiseNotice(new NoticeEventArgs(NoticeLevel.Warning, "No se pudo leer la posición de la página."));
                    return;
                }
                Settings.PanEnabled = true;
                Settings.PanX = Math.Max(0, pan.X);
                Settings.PanY = Math.Max(0, pan.Y);
                Settings.PanSelector = pan.Selector ?? string.Empty;
                UpdatePageSettings();
                RaiseNotice(new NoticeEventArgs(NoticeLevel.Info, "Encuadre fijado. Se restaura después de cada carga."));
            }
            catch (Exception ex)
            {
                Log.Error("No se pudo fijar el encuadre", ex);
            }
        }

        public void ClearPan()
        {
            Settings.PanEnabled = false;
            Settings.PanX = 0;
            Settings.PanY = 0;
            Settings.PanSelector = string.Empty;
            UpdatePageSettings();
        }

        public void SetVirtualWidth(int width)
        {
            Settings.VirtualWidth = width;
            if (width <= 0)
            {
                Settings.VirtualWidth = 0;
                ApplyZoom(Settings.Zoom);
            }
            ApplyVirtualWidth();
            RaiseStateChanged();
            Wall.MarkDirty();
        }

        public async void SetCursorHidden(bool hidden)
        {
            if (Core == null)
            {
                return;
            }
            try
            {
                await Core.ExecuteScriptAsync("window.__muroSoc && window.__muroSoc.cursor(" + (hidden ? "true" : "false") + ")");
            }
            catch (Exception)
            {
            }
        }

        private void HandlePageMessage(PageMessage message)
        {
            if (message.Type == "pickcancel")
            {
                pickerMode = null;
                return;
            }
            if (message.Type != "picked" || pickerMode == null || message.Mode != pickerMode)
            {
                return;
            }
            string selector = (message.Selector ?? string.Empty).Trim();
            string mode = pickerMode;
            pickerMode = null;
            if (selector.Length == 0 || selector.Length > 1000 || selector.IndexOfAny(new char[] { '{', '}', '<', '>' }) >= 0)
            {
                return;
            }
            if (mode == "hide")
            {
                if (!Settings.HideSelectors.Contains(selector))
                {
                    Settings.HideSelectors.Add(selector);
                }
                RaiseNotice(new NoticeEventArgs(NoticeLevel.Info, "Elemento oculto. Para deshacerlo usa \"Mostrar elementos ocultos\"."));
            }
            else
            {
                Settings.IsolateSelector = selector;
                RaiseNotice(new NoticeEventArgs(NoticeLevel.Info, "Elemento aislado. Para deshacerlo usa \"Quitar aislamiento\"."));
            }
            Log.Info((mode == "hide" ? "Elemento oculto" : "Elemento aislado") + " en " + Log.SafeUrl(Url));
            UpdatePageSettings();
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
                navigating = true;
                navigationStartedAt = DateTime.UtcNow;
                attemptedUrl = e.Uri;
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
            if (!closed)
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
