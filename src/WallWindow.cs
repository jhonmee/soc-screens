// MuroSOC - WallWindow
using System;
using System.Drawing;
using System.Windows.Forms;

namespace MuroSoc
{
    internal sealed class WallWindow : Form
    {
        private readonly string startUrl;
        private readonly Panel host;
        private readonly NoticeBar notices;
        private BrowserTab tab;

        public WallWindow(string startUrl)
        {
            this.startUrl = startUrl;
            Text = "Muro SOC " + RuntimeInfo.AppVersion;
            BackColor = Color.Black;
            StartPosition = FormStartPosition.CenterScreen;
            Size = new Size(1280, 800);

            host = new Panel();
            host.Dock = DockStyle.Fill;
            host.BackColor = Color.Black;
            host.Resize += delegate { UpdateBrowserBounds(); };

            notices = new NoticeBar();

            Controls.Add(host);
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

        protected override async void OnShown(EventArgs e)
        {
            base.OnShown(e);
            try
            {
                tab = await BrowserTab.CreateAsync(host, BrowserEnvironment.DefaultProfile);
                tab.NoticeRequested += delegate(object sender, NoticeEventArgs notice) { notices.ShowNotice(notice); };
                tab.TitleChanged += delegate { Text = tab.Title + " - Muro SOC"; };
                UpdateBrowserBounds();
                tab.Controller.IsVisible = true;
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
