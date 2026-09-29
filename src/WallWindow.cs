// MuroSOC - WallWindow
using System;
using System.Drawing;
using System.Windows.Forms;

namespace MuroSoc
{
    internal sealed class WallWindow : Form
    {
        private readonly LayoutPanel panel;
        private readonly ClockLabel clock;
        private Screen screen;
        private bool? appliedFullscreen;
        private LockOverlay lockOverlay;

        public WallWindow(Screen screen)
        {
            this.screen = screen;
            Text = "Muro SOC";
            BackColor = Color.Black;
            StartPosition = FormStartPosition.Manual;
            KeyPreview = true;
            ShowIcon = true;

            panel = new LayoutPanel();
            clock = new ClockLabel();
            Controls.Add(clock);
            Controls.Add(panel);

            Move += delegate { NotifyPositionChanged(); };
            Resize += delegate
            {
                if (lockOverlay != null)
                {
                    lockOverlay.Bounds = Bounds;
                }
            };
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

        public void UpdateClock(bool showClock, bool locked)
        {
            if (showClock)
            {
                clock.UpdateClock(App.Config, locked);
            }
            else if (clock.Visible)
            {
                clock.Visible = false;
            }
        }

        public void SetLocked(bool locked, bool activate)
        {
            if (locked)
            {
                if (lockOverlay == null)
                {
                    lockOverlay = new LockOverlay(this);
                    lockOverlay.ShowOverlay();
                }
                if (activate)
                {
                    lockOverlay.Activate();
                }
                return;
            }
            if (lockOverlay != null)
            {
                LockOverlay closing = lockOverlay;
                lockOverlay = null;
                closing.Close();
                closing.Dispose();
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
                if (!Wall.IsLocked)
                {
                    BeginInvoke((MethodInvoker)delegate { Wall.RequestExit(this); });
                }
                return;
            }
            base.OnFormClosing(e);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            SetLocked(false, false);
            base.OnFormClosed(e);
        }
    }
}
