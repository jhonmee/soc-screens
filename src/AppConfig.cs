// MuroSOC - AppConfig
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;

namespace MuroSoc
{
    [DataContract]
    internal sealed class PermissionRule
    {
        [DataMember(Order = 1)]
        public string Domain { get; set; }

        [DataMember(Order = 2)]
        public string Permission { get; set; }

        [DataMember(Order = 3)]
        public bool Allow { get; set; }
    }

    [DataContract]
    internal sealed class AppConfig
    {
        public const int CurrentSchemaVersion = 1;

        private static readonly Regex ProfileNamePattern = new Regex("^[A-Za-z0-9_-]{1,64}$");

        [DataMember(Order = 1)]
        public int SchemaVersion { get; set; }

        [DataMember(Order = 2)]
        public string HomeUrl { get; set; }

        [DataMember(Order = 3)]
        public List<string> Profiles { get; set; }

        [DataMember(Order = 4)]
        public bool DevToolsEnabled { get; set; }

        [DataMember(Order = 5)]
        public bool DownloadsEnabled { get; set; }

        [DataMember(Order = 6)]
        public bool AllowlistEnabled { get; set; }

        [DataMember(Order = 7)]
        public List<string> AllowedDomains { get; set; }

        [DataMember(Order = 8)]
        public List<PermissionRule> PermissionRules { get; set; }

        [DataMember(Order = 9)]
        public bool PopupsAsTabs { get; set; }

        [DataMember(Order = 10)]
        public List<string> LoginUrlPatterns { get; set; }

        [DataMember(Order = 11)]
        public bool Fullscreen { get; set; }

        [DataMember(Order = 12)]
        public Dictionary<string, string> KeyBindings { get; set; }

        public AppConfig()
        {
            SetDefaults();
        }

        public static bool IsValidProfileName(string name)
        {
            return name != null && ProfileNamePattern.IsMatch(name);
        }

        public void Normalize()
        {
            if (SchemaVersion <= 0)
            {
                SchemaVersion = CurrentSchemaVersion;
            }
            if (string.IsNullOrEmpty(HomeUrl))
            {
                HomeUrl = "about:blank";
            }
            List<string> profiles = new List<string>();
            profiles.Add(BrowserEnvironment.DefaultProfile);
            if (Profiles != null)
            {
                foreach (string name in Profiles)
                {
                    if (IsValidProfileName(name) && !ContainsIgnoreCase(profiles, name))
                    {
                        profiles.Add(name);
                    }
                }
            }
            Profiles = profiles;

            List<string> domains = new List<string>();
            if (AllowedDomains != null)
            {
                foreach (string domain in AllowedDomains)
                {
                    string clean = DomainMatcher.Clean(domain);
                    if (clean.Length > 0 && !ContainsIgnoreCase(domains, clean))
                    {
                        domains.Add(clean);
                    }
                }
            }
            AllowedDomains = domains;

            List<PermissionRule> rules = new List<PermissionRule>();
            if (PermissionRules != null)
            {
                foreach (PermissionRule rule in PermissionRules)
                {
                    if (rule == null || string.IsNullOrEmpty(rule.Permission))
                    {
                        continue;
                    }
                    rule.Domain = DomainMatcher.Clean(rule.Domain);
                    rule.Permission = rule.Permission.Trim().ToLowerInvariant();
                    if (rule.Domain.Length > 0)
                    {
                        rules.Add(rule);
                    }
                }
            }
            PermissionRules = rules;

            List<string> patterns = new List<string>();
            if (LoginUrlPatterns != null)
            {
                foreach (string pattern in LoginUrlPatterns)
                {
                    string clean = LoginDetector.CleanPattern(pattern);
                    if (clean.Length > 0 && !ContainsIgnoreCase(patterns, clean))
                    {
                        patterns.Add(clean);
                    }
                }
            }
            LoginUrlPatterns = patterns;

            Dictionary<string, string> bindings = Shortcuts.Defaults();
            if (KeyBindings != null)
            {
                foreach (KeyValuePair<string, string> pair in KeyBindings)
                {
                    System.Windows.Forms.Keys parsed;
                    if (bindings.ContainsKey(pair.Key) && (string.IsNullOrEmpty(pair.Value) || Shortcuts.TryParse(pair.Value, out parsed)))
                    {
                        bindings[pair.Key] = pair.Value ?? string.Empty;
                    }
                }
            }
            KeyBindings = bindings;
        }

        [OnDeserializing]
        private void OnDeserializing(StreamingContext context)
        {
            SetDefaults();
        }

        private void SetDefaults()
        {
            SchemaVersion = CurrentSchemaVersion;
            HomeUrl = "https://security.microsoft.com";
            Profiles = new List<string>();
            Profiles.Add(BrowserEnvironment.DefaultProfile);
            DevToolsEnabled = false;
            DownloadsEnabled = false;
            AllowlistEnabled = false;
            AllowedDomains = new List<string>();
            PermissionRules = new List<PermissionRule>();
            PopupsAsTabs = false;
            LoginUrlPatterns = new List<string>(LoginDetector.DefaultPatterns);
            Fullscreen = true;
            KeyBindings = Shortcuts.Defaults();
        }

        private static bool ContainsIgnoreCase(List<string> list, string value)
        {
            foreach (string item in list)
            {
                if (string.Equals(item, value, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
