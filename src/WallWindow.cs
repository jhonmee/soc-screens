// MuroSOC - WallWindow
using System;
using System.Drawing;
using System.Windows.Forms;

namespace MuroSoc
{
    internal sealed class WallWindow : Form
    {
        private readonly LayoutPanel panel;
        private Screen screen;
        private bool? appliedFullscreen;

        public WallWindow(Screen screen)
        {
            this.screen = screen;
            Text = "Muro SOC";
            BackColor = Color.Black;
            StartPosition = FormStartPosition.Manual;
            KeyPreview = true;
            ShowIcon = true;

            panel = new LayoutPanel();
            Controls.Add(panel);

            Move += delegate { NotifyPositionChanged(); };
            ApplyWindowMode();
        }

        public Screen Screen
        {
            get { return screen; }
        }

        public string DeviceName
        {
            get { return screen.DeviceName; }
        }

        public LayoutPanel Panel
        {
            get { return panel; }
        }

        public void ApplyWindowMode()
        {
            bool fullscreen = App.Config.Fullscreen;
            if (appliedFullscreen.HasValue && appliedFullscreen.Value == fullscreen)
            {
                return;
            }
            appliedFullscreen = fullscreen;
            if (fullscreen)
            {
                FormBorderStyle = FormBorderStyle.None;
                WindowState = FormWindowState.Normal;
                Bounds = screen.Bounds;
            }
            else
            {
                FormBorderStyle = FormBorderStyle.Sizable;
                Rectangle area = screen.WorkingArea;
                int insetX = area.Width / 12;
                int insetY = area.Height / 12;
                Bounds = new Rectangle(area.Left + insetX, area.Top + insetY, area.Width - insetX * 2, area.Height - insetY * 2);
            }
            NotifyPositionChanged();
        }

        public void UpdateTitle(string layoutName)
        {
            string monitor = screen.DeviceName.Replace(@"\\.\", string.Empty);
            Text = "Muro SOC - " + (string.IsNullOrEmpty(layoutName) ? "Sin layout" : layoutName) + " - " + monitor;
        }

        public void NotifyPositionChanged()
        {
            foreach (Cell cell in panel.Cells)
            {
                cell.NotifyPositionChanged();
            }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (Wall.TryHandleKey(keyData))
            {
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (!Wall.AllowClose && e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                BeginInvoke((MethodInvoker)delegate { Wall.RequestExit(this); });
                return;
            }
            base.OnFormClosing(e);
        }
    }
}
