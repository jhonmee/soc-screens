// MuroSOC - UrlInput
using System;

namespace MuroSoc
{
    internal static class UrlInput
    {
        public static string Normalize(string text)
        {
            if (text == null)
            {
                return null;
            }
            string value = text.Trim();
            if (value.Length == 0)
            {
                return null;
            }
            if (value.Equals("about:blank", StringComparison.OrdinalIgnoreCase))
            {
                return "about:blank";
            }
            Uri uri;
            if (Uri.TryCreate(value, UriKind.Absolute, out uri) && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp))
            {
                return uri.AbsoluteUri;
            }
            if (value.IndexOf(' ') < 0 && Uri.TryCreate("https://" + value, UriKind.Absolute, out uri) && uri.Host.IndexOf('.') > 0)
            {
                return uri.AbsoluteUri;
            }
            if (value.IndexOf(' ') < 0 && Uri.TryCreate("https://" + value, UriKind.Absolute, out uri) && uri.Host == "localhost")
            {
                return uri.AbsoluteUri;
            }
            return null;
        }
    }
}
