// MuroSOC - PopupWindow
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;

namespace MuroSoc
{
    internal sealed class PopupWindow : Form, ITabHost
    {
        private readonly BrowserTab opener;
        private readonly Panel content;
        private readonly Label header;
        private readonly NoticeBar notices;
        private BrowserTab tab;
        private bool closingFromScript;

        private PopupWindow(BrowserTab opener)
        {
            this.opener = opener;
            Text = "Muro SOC";
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            FormBorderStyle = FormBorderStyle.Sizable;
            MinimizeBox = false;
            BackColor = Color.FromArgb(32, 32, 32);
            KeyPreview = true;

            header = new Label();
            header.Dock = DockStyle.Top;
            header.Height = Dpi.Scale(this, 26);
            header.TextAlign = ContentAlignment.MiddleLeft;
            header.Padding = new Padding(Dpi.Scale(this, 8), 0, 0, 0);
            header.BackColor = Color.FromArgb(45, 45, 48);
            header.ForeColor = Color.Gainsboro;
            header.AutoEllipsis = true;

            notices = new NoticeBar();

            content = new Panel();
            content.Dock = DockStyle.Fill;
            content.BackColor = Color.White;
            content.Resize += delegate { UpdateBrowserBounds(); };

            Controls.Add(content);
            Controls.Add(notices);
            Controls.Add(header);

            Move += delegate
            {
                if (tab != null)
                {
                    tab.NotifyPositionChanged();
                }
            };
        }

        public Control ContentHost
        {
            get { return content; }
        }

        public static async Task<BrowserTab> OpenAsync(BrowserTab opener, CoreWebView2WindowFeatures features)
        {
            Control originHost = opener.Host.ContentHost;
            Form owner = originHost.FindForm();
            PopupWindow window = new PopupWindow(opener);
            window.Bounds = ComputeBounds(originHost, features);
            window.Show(owner);
            try
            {
                TabModel model = new TabModel();
                model.Profile = opener.ProfileName;
                window.tab = await BrowserTab.CreateAsync(window, model, opener, true);
                window.UpdateBrowserBounds();
                window.tab.SetVisible(true);
                window.tab.Focus();
                window.UpdateHeader();
                return window.tab;
            }
            catch (Exception)
            {
                window.Close();
                throw;
            }
        }

        public Task<BrowserTab> OpenScriptTabAsync(BrowserTab requester)
        {
            return OpenAsync(requester, null);
        }

        public void CloseScriptTab(BrowserTab closing)
        {
            closingFromScript = true;
            Close();
        }

        public void OnTabStateChanged(BrowserTab changed)
        {
            UpdateHeader();
        }

        public void ShowNotice(BrowserTab source, NoticeEventArgs notice)
        {
            notices.ShowNotice(notice);
        }

        public void FocusHost()
        {
            Activate();
        }

        public void OnTabFocused(BrowserTab focused)
        {
        }

        public List<MenuEntry> BuildMenu(BrowserTab source)
        {
            List<MenuEntry> menu = new List<MenuEntry>();
            string url = source.Url;
            menu.Add(MenuEntry.Item("Abrir esta página en Edge", delegate { ExternalBrowser.OpenInEdge(url); }));
            menu.Add(MenuEntry.Item("Cerrar ventana", delegate { Close(); }));
            return menu;
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            if (tab != null)
            {
                tab.Close();
                tab = null;
            }
            if (!closingFromScript)
            {
                Log.Info("Popup cerrado por el usuario");
            }
            if (!opener.IsClosed)
            {
                opener.Host.FocusHost();
                opener.Focus();
            }
            base.OnFormClosed(e);
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape)
            {
                Close();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void UpdateHeader()
        {
            if (tab == null)
            {
                return;
            }
            string url = tab.Url;
            Uri uri;
            string origin = url;
            if (Uri.TryCreate(url, UriKind.Absolute, out uri) && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp))
            {
                origin = (uri.Scheme == Uri.UriSchemeHttps ? "🔒 " : "⚠ http://") + uri.Host;
            }
            header.Text = origin;
            string title = tab.Title;
            Text = string.IsNullOrEmpty(title) ? "Muro SOC" : title + " - Muro SOC";
        }

        private void UpdateBrowserBounds()
        {
            if (tab != null)
            {
                tab.SetBounds(content.ClientRectangle);
            }
        }

        private static Rectangle ComputeBounds(Control originHost, CoreWebView2WindowFeatures features)
        {
            Rectangle origin = originHost.RectangleToScreen(originHost.ClientRectangle);
            Rectangle area = Screen.FromRectangle(origin).WorkingArea;
            double scale = Dpi.Of(originHost) / 96.0;
            int width = (int)(560 * scale);
            int height = (int)(680 * scale);
            if (features != null && features.HasSize)
            {
                width = (int)(features.Width * scale) + SystemInformation.FrameBorderSize.Width * 2;
                height = (int)(features.Height * scale) + SystemInformation.CaptionHeight + Dpi.Scale(originHost, 26);
            }
            width = Math.Max(Dpi.Scale(originHost, 360), Math.Min(width, area.Width));
            height = Math.Max(Dpi.Scale(originHost, 320), Math.Min(height, area.Height));
            int x = origin.Left + (origin.Width - width) / 2;
            int y = origin.Top + (origin.Height - height) / 2;
            x = Math.Max(area.Left, Math.Min(x, area.Right - width));
            y = Math.Max(area.Top, Math.Min(y, area.Bottom - height));
            return new Rectangle(x, y, width, height);
        }
    }
}
