// MuroSOC - ExternalBrowser
using System;
using System.Diagnostics;

namespace MuroSoc
{
    internal static class ExternalBrowser
    {
        public static bool OpenDefault(string url)
        {
            if (!IsWebUrl(url))
            {
                return false;
            }
            return ShellOpen(url);
        }

        public static bool OpenInEdge(string url)
        {
            if (!IsWebUrl(url))
            {
                return false;
            }
            return ShellOpen("microsoft-edge:" + url);
        }

        private static bool IsWebUrl(string url)
        {
            Uri uri;
            if (!Uri.TryCreate(url, UriKind.Absolute, out uri))
            {
                return false;
            }
            return uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp;
        }

        private static bool ShellOpen(string target)
        {
            try
            {
                ProcessStartInfo info = new ProcessStartInfo(target);
                info.UseShellExecute = true;
                using (Process.Start(info))
                {
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
