// MuroSOC - SecurityPolicy
using System;
using Microsoft.Web.WebView2.Core;

namespace MuroSoc
{
    internal static class SecurityPolicy
    {
        public static readonly string[] IdentityDomains = new string[]
        {
            "login.microsoftonline.com",
            "login.microsoft.com",
            "login.windows.net",
            "login.live.com",
            "account.live.com",
            "account.microsoft.com",
            "device.login.microsoftonline.com",
            "autologon.microsoftazuread-sso.com",
            "aadcdn.msauth.net",
            "aadcdn.msftauth.net",
            "msauth.net",
            "msftauth.net",
            "mysignins.microsoft.com",
            "passwordreset.microsoftonline.com"
        };

        public static string PermissionName(CoreWebView2PermissionKind kind)
        {
            switch (kind)
            {
                case CoreWebView2PermissionKind.Camera: return "camera";
                case CoreWebView2PermissionKind.Microphone: return "microphone";
                case CoreWebView2PermissionKind.Geolocation: return "geolocation";
                case CoreWebView2PermissionKind.Notifications: return "notifications";
                case CoreWebView2PermissionKind.MidiSystemExclusiveMessages: return "midi";
                case CoreWebView2PermissionKind.ClipboardRead: return "clipboard";
                case CoreWebView2PermissionKind.OtherSensors: return "sensors";
                case CoreWebView2PermissionKind.MultipleAutomaticDownloads: return "downloads";
                case CoreWebView2PermissionKind.FileReadWrite: return "files";
                case CoreWebView2PermissionKind.Autoplay: return "autoplay";
                case CoreWebView2PermissionKind.LocalFonts: return "fonts";
                case CoreWebView2PermissionKind.WindowManagement: return "window-management";
                case CoreWebView2PermissionKind.PersistentStorage: return "storage";
                default: return "unknown";
            }
        }

        public static bool DefaultAllows(string permission)
        {
            switch (permission)
            {
                case "clipboard":
                case "autoplay":
                case "storage":
                    return true;
                default:
                    return false;
            }
        }

        public static bool IsPermissionAllowed(AppConfig config, string url, CoreWebView2PermissionKind kind)
        {
            string permission = PermissionName(kind);
            string host = DomainMatcher.HostOf(url);
            bool allowed = DefaultAllows(permission);
            int bestLength = -1;
            foreach (PermissionRule rule in config.PermissionRules)
            {
                if (rule.Permission != permission && rule.Permission != "*")
                {
                    continue;
                }
                if (!DomainMatcher.Matches(host, rule.Domain))
                {
                    continue;
                }
                int length = rule.Domain.Length + (rule.Permission == "*" ? 0 : 1000);
                if (length > bestLength)
                {
                    bestLength = length;
                    allowed = rule.Allow;
                }
            }
            return allowed;
        }

        public static bool IsNavigationAllowed(AppConfig config, string url)
        {
            if (!config.AllowlistEnabled)
            {
                return true;
            }
            Uri uri;
            if (!Uri.TryCreate(url, UriKind.Absolute, out uri))
            {
                return false;
            }
            if (uri.Scheme == "about" || uri.Scheme == "blob")
            {
                return true;
            }
            if (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp)
            {
                return false;
            }
            string host = uri.Host.ToLowerInvariant();
            return DomainMatcher.MatchesAny(host, IdentityDomains) || DomainMatcher.MatchesAny(host, config.AllowedDomains);
        }
    }
}
