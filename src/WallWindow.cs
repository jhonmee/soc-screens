// MuroSOC - WallWindow
using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MuroSoc
{
    internal sealed class WallWindow : Form, ITabHost
    {
        private static readonly Color LoginColor = Color.FromArgb(255, 140, 0);

        private readonly string startUrl;
        private readonly Panel frame;
        private readonly Panel host;
        private readonly NoticeBar notices;
        private readonly Label status;
        private BrowserTab tab;

        public WallWindow(string startUrl)
        {
            this.startUrl = startUrl;
            Text = "Muro SOC " + RuntimeInfo.AppVersion;
            BackColor = Color.Black;
            StartPosition = FormStartPosition.CenterScreen;
            Size = new Size(1280, 800);

            frame = new Panel();
            frame.Dock = DockStyle.Fill;
            frame.BackColor = Color.Black;

            host = new Panel();
            host.Dock = DockStyle.Fill;
            host.BackColor = Color.Black;
            host.Resize += delegate { UpdateBrowserBounds(); };

            status = new Label();
            status.Dock = DockStyle.Bottom;
            status.Height = Dpi.Scale(this, 22);
            status.TextAlign = ContentAlignment.MiddleLeft;
            status.ForeColor = Color.Black;
            status.Visible = false;

            frame.Controls.Add(host);
            frame.Controls.Add(status);

            notices = new NoticeBar();

            Controls.Add(frame);
            Controls.Add(notices);

            Move += delegate
            {
                if (tab != null)
                {
                    tab.NotifyPositionChanged();
                }
            };

            App.ConfigReloaded += OnConfigReloaded;
        }

        public Control ContentHost
        {
            get { return host; }
        }

        public Task<BrowserTab> OpenScriptTabAsync(BrowserTab opener)
        {
            return PopupWindow.OpenAsync(opener, null);
        }

        public void CloseScriptTab(BrowserTab closing)
        {
        }

        public void OnTabStateChanged(BrowserTab changed)
        {
            Text = changed.Title + " - Muro SOC";
            if (changed.IsAtLogin)
            {
                frame.Padding = new Padding(Dpi.Scale(this, 3));
                frame.BackColor = LoginColor;
                status.BackColor = LoginColor;
                status.Text = "  Requiere login";
                status.Visible = true;
            }
            else
            {
                frame.Padding = Padding.Empty;
                frame.BackColor = Color.Black;
                status.Visible = false;
            }
        }

        public void ShowNotice(BrowserTab source, NoticeEventArgs notice)
        {
            notices.ShowNotice(notice);
        }

        public void FocusHost()
        {
            Activate();
        }

        protected override async void OnShown(EventArgs e)
        {
            base.OnShown(e);
            try
            {
                tab = await BrowserTab.CreateAsync(this, BrowserEnvironment.DefaultProfile);
                UpdateBrowserBounds();
                tab.SetVisible(true);
                tab.Navigate(startUrl);
            }
            catch (Exception ex)
            {
                Log.Error("No se pudo iniciar WebView2", ex);
                MessageBox.Show(this, "No se pudo iniciar WebView2.\r\n\r\n" + ex.Message, "Muro SOC", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Close();
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            App.ConfigReloaded -= OnConfigReloaded;
            if (tab != null)
            {
                tab.Close();
                tab = null;
            }
            base.OnFormClosed(e);
        }

        private void OnConfigReloaded(object sender, ConfigReloadedEventArgs e)
        {
            if (e.Error == null)
            {
                notices.ShowNotice(new NoticeEventArgs(NoticeLevel.Info, "Configuración actualizada."));
                if (tab != null)
                {
                    tab.UpdateLoginState();
                }
            }
            else
            {
                notices.ShowNotice(new NoticeEventArgs(NoticeLevel.Error, "config.json tiene un error y no se aplicó: " + e.Error));
            }
        }

        private void UpdateBrowserBounds()
        {
            if (tab != null)
            {
                tab.SetBounds(host.ClientRectangle);
            }
        }
    }
}
