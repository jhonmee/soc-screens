// MuroSOC - DomainMatcher
using System;
using System.Collections.Generic;

namespace MuroSoc
{
    internal static class DomainMatcher
    {
        public static string Clean(string domain)
        {
            if (domain == null)
            {
                return string.Empty;
            }
            string value = domain.Trim().ToLowerInvariant();
            if (value.StartsWith("*.", StringComparison.Ordinal))
            {
                value = value.Substring(2);
            }
            value = value.TrimStart('.').TrimEnd('.', '/');
            return value;
        }

        public static string HostOf(string url)
        {
            Uri uri;
            if (!Uri.TryCreate(url, UriKind.Absolute, out uri))
            {
                return string.Empty;
            }
            return uri.Host.ToLowerInvariant();
        }

        public static bool Matches(string host, string domain)
        {
            if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(domain))
            {
                return false;
            }
            if (host == domain)
            {
                return true;
            }
            return host.EndsWith("." + domain, StringComparison.Ordinal);
        }

        public static bool MatchesAny(string host, IEnumerable<string> domains)
        {
            foreach (string domain in domains)
            {
                if (Matches(host, domain))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
