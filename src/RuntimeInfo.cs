// MuroSOC - RuntimeInfo
using System;
using System.Diagnostics;
using System.Reflection;
using Microsoft.Web.WebView2.Core;
using Microsoft.Win32;

namespace MuroSoc
{
    internal static class RuntimeInfo
    {
        public const string RuntimeDownloadUrl = "https://developer.microsoft.com/microsoft-edge/webview2/";

        private const string EdgeClientKey = @"SOFTWARE\Microsoft\EdgeUpdate\Clients\{56EB18F8-B008-4CBD-B6D2-8C97FE7E9062}";
        private const string EdgeClientKeyWow = @"SOFTWARE\WOW6432Node\Microsoft\EdgeUpdate\Clients\{56EB18F8-B008-4CBD-B6D2-8C97FE7E9062}";

        public static string AppVersion
        {
            get
            {
                object[] attributes = typeof(RuntimeInfo).Assembly.GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), false);
                if (attributes.Length > 0)
                {
                    return ((AssemblyInformationalVersionAttribute)attributes[0]).InformationalVersion;
                }
                return typeof(RuntimeInfo).Assembly.GetName().Version.ToString();
            }
        }

        public static string SdkVersion
        {
            get
            {
                FileVersionInfo info = FileVersionInfo.GetVersionInfo(typeof(CoreWebView2Environment).Assembly.Location);
                return info.FileVersion;
            }
        }

        public static string GetRuntimeVersion()
        {
            try
            {
                string version = CoreWebView2Environment.GetAvailableBrowserVersionString();
                return string.IsNullOrEmpty(version) ? null : version;
            }
            catch (WebView2RuntimeNotFoundException)
            {
                return null;
            }
            catch (DllNotFoundException)
            {
                return null;
            }
            catch (BadImageFormatException)
            {
                return null;
            }
        }

        public static string GetEdgeVersion()
        {
            string version = ReadVersion(Registry.LocalMachine, EdgeClientKeyWow);
            if (version == null)
            {
                version = ReadVersion(Registry.LocalMachine, EdgeClientKey);
            }
            if (version == null)
            {
                version = ReadVersion(Registry.CurrentUser, EdgeClientKey);
            }
            return version;
        }

        private static string ReadVersion(RegistryKey hive, string path)
        {
            try
            {
                using (RegistryKey key = hive.OpenSubKey(path, false))
                {
                    if (key == null)
                    {
                        return null;
                    }
                    string value = key.GetValue("pv") as string;
                    if (string.IsNullOrEmpty(value) || value == "0.0.0.0")
                    {
                        return null;
                    }
                    return value;
                }
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
