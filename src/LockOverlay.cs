// MuroSOC - LockOverlay
using System;
using System.Drawing;
using System.Windows.Forms;

namespace MuroSoc
{
    internal sealed class LockOverlay : Form
    {
        private readonly WallWindow owner;
        private readonly LockBadge badge;

        public LockOverlay(WallWindow owner)
        {
            this.owner = owner;
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            BackColor = Color.Black;
            Opacity = 0.01;
            KeyPreview = true;
            Bounds = owner.Bounds;
            badge = new LockBadge();
        }

        public void ShowOverlay()
        {
            Bounds = owner.Bounds;
            Show(owner);
            badge.Text = "🔒 Muro bloqueado · " + Shortcuts.Display(Shortcuts.LockWall) + " para desbloquear";
            badge.PlaceAt(owner.Bounds);
            badge.Show(this);
        }

        protected override bool ShowWithoutActivation
        {
            get { return false; }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            Activate();
            badge.Flash();
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            Wall.TryHandleKey(keyData);
            return true;
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            badge.Close();
            badge.Dispose();
            base.OnFormClosed(e);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (!Wall.AllowClose && Wall.IsLocked && e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                return;
            }
            base.OnFormClosing(e);
        }
    }

    internal sealed class LockBadge : Form
    {
        private readonly Label label;
        private readonly Timer flashTimer;

        public LockBadge()
        {
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            BackColor = Color.FromArgb(40, 40, 40);
            Opacity = 0.85;

            label = new Label();
            label.Dock = DockStyle.Fill;
            label.ForeColor = Color.Gainsboro;
            label.TextAlign = ContentAlignment.MiddleCenter;
            Controls.Add(label);

            flashTimer = new Timer();
            flashTimer.Interval = 600;
            flashTimer.Tick += delegate
            {
                flashTimer.Stop();
                BackColor = Color.FromArgb(40, 40, 40);
            };
        }

        public override string Text
        {
            get { return label == null ? base.Text : label.Text; }
            set
            {
                base.Text = value;
                if (label != null)
                {
                    label.Text = value;
                }
            }
        }

        protected override bool ShowWithoutActivation
        {
            get { return true; }
        }

        public void PlaceAt(Rectangle area)
        {
            label.Font = new Font("Segoe UI", Dpi.ScaleF(this, 12f), FontStyle.Regular, GraphicsUnit.Pixel);
            Size size = TextRenderer.MeasureText(label.Text, label.Font);
            int width = size.Width + Dpi.Scale(this, 24);
            int height = size.Height + Dpi.Scale(this, 12);
            int margin = Dpi.Scale(this, 12);
            Bounds = new Rectangle(area.Right - width - margin, area.Bottom - height - margin, width, height);
        }

        public void Flash()
        {
            BackColor = Color.FromArgb(200, 110, 0);
            flashTimer.Stop();
            flashTimer.Start();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                flashTimer.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
