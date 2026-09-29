// MuroSOC - AddressBar
using System;
using System.Drawing;
using System.Windows.Forms;

namespace MuroSoc
{
    internal sealed class AddressBar : Panel
    {
        private readonly Cell cell;
        private readonly Button back;
        private readonly Button forward;
        private readonly Button reload;
        private readonly Button edge;
        private readonly TextBox input;
        private BrowserTab bound;

        public AddressBar(Cell cell)
        {
            this.cell = cell;
            Dock = DockStyle.Top;
            BackColor = Cell.ChromeBack;
            Visible = false;

            input = new TextBox();
            input.Dock = DockStyle.Fill;
            input.BorderStyle = BorderStyle.FixedSingle;
            input.BackColor = Color.FromArgb(45, 45, 48);
            input.ForeColor = Color.White;
            input.KeyDown += OnInputKeyDown;
            input.Enter += delegate { BeginInvoke((MethodInvoker)delegate { input.SelectAll(); }); };

            edge = MakeButton("Edge", "Abrir esta página en Edge");
            edge.Dock = DockStyle.Right;
            edge.Click += delegate
            {
                if (bound != null)
                {
                    ExternalBrowser.OpenInEdge(bound.Url);
                }
            };

            reload = MakeButton("⟳", "Recargar");
            reload.Dock = DockStyle.Left;
            reload.Click += delegate
            {
                if (bound != null)
                {
                    bound.Reload();
                }
            };

            forward = MakeButton("→", "Adelante");
            forward.Dock = DockStyle.Left;
            forward.Click += delegate
            {
                if (bound != null)
                {
                    bound.GoForward();
                }
            };

            back = MakeButton("←", "Atrás");
            back.Dock = DockStyle.Left;
            back.Click += delegate
            {
                if (bound != null)
                {
                    bound.GoBack();
                }
            };

            Panel inputHolder = new Panel();
            inputHolder.Dock = DockStyle.Fill;
            inputHolder.Padding = new Padding(4, 4, 4, 4);
            inputHolder.Controls.Add(input);

            Controls.Add(inputHolder);
            Controls.Add(edge);
            Controls.Add(reload);
            Controls.Add(forward);
            Controls.Add(back);

            ApplyScale();
            HandleCreated += delegate { ApplyScale(); };
        }

        public void Bind(BrowserTab tab)
        {
            bound = tab;
            bool hasTab = tab != null && !tab.IsClosed;
            back.Enabled = hasTab && tab.CanGoBack;
            forward.Enabled = hasTab && tab.CanGoForward;
            reload.Enabled = hasTab;
            edge.Enabled = hasTab;
            if (!input.Focused)
            {
                input.Text = hasTab ? tab.Url : string.Empty;
            }
        }

        public void FocusInput()
        {
            input.Focus();
            input.SelectAll();
        }

        private async void OnInputKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                e.SuppressKeyPress = true;
                Bind(bound);
                if (bound != null)
                {
                    bound.Focus();
                }
                return;
            }
            if (e.KeyCode != Keys.Enter)
            {
                return;
            }
            e.SuppressKeyPress = true;
            string url = UrlInput.Normalize(input.Text);
            if (url == null)
            {
                input.SelectAll();
                return;
            }
            if (bound != null && !bound.IsClosed)
            {
                bound.Navigate(url);
                bound.Focus();
                return;
            }
            TabModel model = new TabModel();
            model.Url = url;
            model.Profile = cell.ProfileName;
            try
            {
                await cell.CreateTabAsync(model, true);
            }
            catch (Exception ex)
            {
                Log.Error("No se pudo abrir la pestaña", ex);
            }
        }

        private Button MakeButton(string text, string tooltip)
        {
            Button button = new Button();
            button.Text = text;
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.ForeColor = Color.Gainsboro;
            button.TabStop = false;
            button.AccessibleName = tooltip;
            return button;
        }

        private void ApplyScale()
        {
            Height = Dpi.Scale(this, 32);
            Font = new Font("Segoe UI", Dpi.ScaleF(this, 13f), FontStyle.Regular, GraphicsUnit.Pixel);
            int width = Dpi.Scale(this, 34);
            back.Width = width;
            forward.Width = width;
            reload.Width = width;
            edge.Width = Dpi.Scale(this, 52);
        }
    }
}
