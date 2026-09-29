// MuroSOC - LoginDetector
using System;
using System.Collections.Generic;

namespace MuroSoc
{
    internal static class LoginDetector
    {
        public static readonly string[] DefaultPatterns = new string[]
        {
            "login.microsoftonline.com",
            "login.microsoft.com",
            "login.windows.net",
            "login.live.com",
            "okta.com",
            "oktapreview.com"
        };

        public static string CleanPattern(string pattern)
        {
            if (pattern == null)
            {
                return string.Empty;
            }
            string value = pattern.Trim().ToLowerInvariant();
            int scheme = value.IndexOf("://", StringComparison.Ordinal);
            if (scheme >= 0)
            {
                value = value.Substring(scheme + 3);
            }
            if (value.StartsWith("*.", StringComparison.Ordinal))
            {
                value = value.Substring(2);
            }
            return value.TrimStart('.').TrimEnd('/');
        }

        public static bool IsLoginUrl(string url, IEnumerable<string> patterns)
        {
            Uri uri;
            if (string.IsNullOrEmpty(url) || !Uri.TryCreate(url, UriKind.Absolute, out uri))
            {
                return false;
            }
            if (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp)
            {
                return false;
            }
            string host = uri.Host.ToLowerInvariant();
            string path = uri.AbsolutePath.ToLowerInvariant();
            foreach (string pattern in patterns)
            {
                int slash = pattern.IndexOf('/');
                string domain = slash < 0 ? pattern : pattern.Substring(0, slash);
                string prefix = slash < 0 ? string.Empty : pattern.Substring(slash);
                if (!DomainMatcher.Matches(host, domain))
                {
                    continue;
                }
                if (prefix.Length == 0 || path.StartsWith(prefix, StringComparison.Ordinal))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
