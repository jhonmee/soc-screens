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
        public string Domain;

        [DataMember(Order = 2)]
        public string Permission;

        [DataMember(Order = 3)]
        public bool Allow;
    }

    [DataContract]
    internal sealed class AppConfig
    {
        public const int CurrentSchemaVersion = 1;

        private static readonly Regex ProfileNamePattern = new Regex("^[A-Za-z0-9_-]{1,64}$");

        [DataMember(Order = 1)]
        public int SchemaVersion;

        [DataMember(Order = 2)]
        public string HomeUrl;

        [DataMember(Order = 3)]
        public List<string> Profiles;

        [DataMember(Order = 4)]
        public bool DevToolsEnabled;

        [DataMember(Order = 5)]
        public bool DownloadsEnabled;

        [DataMember(Order = 6)]
        public bool AllowlistEnabled;

        [DataMember(Order = 7)]
        public List<string> AllowedDomains;

        [DataMember(Order = 8)]
        public List<PermissionRule> PermissionRules;

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
