// MuroSOC - Notice
using System;
using System.Windows.Forms;

namespace MuroSoc
{
    internal enum NoticeLevel
    {
        Info,
        Warning,
        Error
    }

    internal sealed class NoticeEventArgs : EventArgs
    {
        public NoticeEventArgs(NoticeLevel level, string text)
        {
            Level = level;
            Text = text;
        }

        public NoticeLevel Level { get; private set; }

        public string Text { get; private set; }

        public string ActionText { get; set; }

        public MethodInvoker Action { get; set; }
    }
}
