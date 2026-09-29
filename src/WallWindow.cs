// MuroSOC - WallWindow
using System;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;

namespace MuroSoc
{
    internal sealed class WallWindow : Form
    {
        private readonly string startUrl;
        private readonly Panel host;
        private CoreWebView2Controller controller;

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
            Controls.Add(host);

            Move += delegate
            {
                if (controller != null)
                {
                    controller.NotifyParentWindowPositionChanged();
                }
            };
        }

        protected override async void OnShown(EventArgs e)
        {
            base.OnShown(e);
            try
            {
                controller = await BrowserEnvironment.CreateControllerAsync(host.Handle, BrowserEnvironment.DefaultProfile);
                controller.DefaultBackgroundColor = Color.Black;
                UpdateBrowserBounds();
                controller.IsVisible = true;
                controller.CoreWebView2.DocumentTitleChanged += delegate
                {
                    Text = controller.CoreWebView2.DocumentTitle + " - Muro SOC";
                };
                controller.CoreWebView2.Navigate(startUrl);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "No se pudo iniciar WebView2.\r\n\r\n" + ex.Message, "Muro SOC", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Close();
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            if (controller != null)
            {
                controller.Close();
                controller = null;
            }
            base.OnFormClosed(e);
        }

        private void UpdateBrowserBounds()
        {
            if (controller != null)
            {
                controller.Bounds = host.ClientRectangle;
            }
        }
    }
}
