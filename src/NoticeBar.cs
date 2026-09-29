// MuroSOC - NoticeBar
using System;
using System.Drawing;
using System.Windows.Forms;

namespace MuroSoc
{
    internal sealed class NoticeBar : Panel
    {
        private readonly Label message;
        private readonly Button action;
        private readonly Button close;
        private readonly Timer hideTimer;
        private MethodInvoker currentAction;

        public NoticeBar()
        {
            Dock = DockStyle.Top;
            Height = 32;
            Visible = false;
            Padding = new Padding(8, 4, 4, 4);

            close = new Button();
            close.Text = "✕";
            close.FlatStyle = FlatStyle.Flat;
            close.FlatAppearance.BorderSize = 0;
            close.Dock = DockStyle.Right;
            close.Width = 32;
            close.Click += delegate { HideNotice(); };

            action = new Button();
            action.FlatStyle = FlatStyle.Flat;
            action.AutoSize = true;
            action.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            action.Dock = DockStyle.Right;
            action.Click += delegate
            {
                MethodInvoker run = currentAction;
                HideNotice();
                if (run != null)
                {
                    run();
                }
            };

            message = new Label();
            message.Dock = DockStyle.Fill;
            message.TextAlign = ContentAlignment.MiddleLeft;
            message.AutoEllipsis = true;

            Controls.Add(message);
            Controls.Add(action);
            Controls.Add(close);

            hideTimer = new Timer();
            hideTimer.Tick += delegate { HideNotice(); };
        }

        public void ShowNotice(NoticeEventArgs notice)
        {
            hideTimer.Stop();
            message.Text = notice.Text;
            currentAction = notice.Action;
            action.Text = notice.ActionText ?? string.Empty;
            action.Visible = notice.Action != null && !string.IsNullOrEmpty(notice.ActionText);

            Color back;
            switch (notice.Level)
            {
                case NoticeLevel.Error:
                    back = Color.FromArgb(160, 30, 30);
                    break;
                case NoticeLevel.Warning:
                    back = Color.FromArgb(200, 110, 0);
                    break;
                default:
                    back = Color.FromArgb(30, 80, 150);
                    break;
            }
            BackColor = back;
            ForeColor = Color.White;
            action.ForeColor = Color.White;
            close.ForeColor = Color.White;
            Visible = true;

            if (notice.Action == null)
            {
                hideTimer.Interval = 15000;
                hideTimer.Start();
            }
        }

        public void HideNotice()
        {
            hideTimer.Stop();
            currentAction = null;
            Visible = false;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                hideTimer.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
