// MuroSOC - UrlSanitizer
using System;
using System.Collections.Generic;

namespace MuroSoc
{
    internal static class UrlSanitizer
    {
        private static readonly string[] SensitiveKeys = new string[]
        {
            "code", "id_token", "access_token", "refresh_token", "token", "client_info", "session_state", "saml", "samlresponse", "password", "pwd"
        };

        public static string ForStorage(string url)
        {
            Uri uri;
            if (string.IsNullOrEmpty(url) || !Uri.TryCreate(url, UriKind.Absolute, out uri))
            {
                return url ?? string.Empty;
            }
            if (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp)
            {
                return url;
            }
            string query = Filter(uri.Query.TrimStart('?'));
            string fragment = uri.Fragment.TrimStart('#');
            if (ContainsSensitive(fragment))
            {
                fragment = string.Empty;
            }
            UriBuilder builder = new UriBuilder(uri);
            builder.UserName = string.Empty;
            builder.Password = string.Empty;
            builder.Query = query;
            builder.Fragment = fragment;
            return builder.Uri.AbsoluteUri;
        }

        private static string Filter(string query)
        {
            if (string.IsNullOrEmpty(query))
            {
                return string.Empty;
            }
            List<string> kept = new List<string>();
            foreach (string part in query.Split('&'))
            {
                if (part.Length == 0)
                {
                    continue;
                }
                string key = part;
                int equals = part.IndexOf('=');
                if (equals >= 0)
                {
                    key = part.Substring(0, equals);
                }
                if (!IsSensitive(Uri.UnescapeDataString(key)))
                {
                    kept.Add(part);
                }
            }
            return string.Join("&", kept.ToArray());
        }

        private static bool ContainsSensitive(string fragment)
        {
            if (string.IsNullOrEmpty(fragment) || fragment.IndexOf('=') < 0)
            {
                return false;
            }
            foreach (string part in fragment.Split('&'))
            {
                int equals = part.IndexOf('=');
                if (equals > 0 && IsSensitive(Uri.UnescapeDataString(part.Substring(0, equals))))
                {
                    return true;
                }
            }
            return false;
        }

        private static bool IsSensitive(string key)
        {
            string lower = key.Trim().ToLowerInvariant();
            foreach (string sensitive in SensitiveKeys)
            {
                if (lower == sensitive)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
