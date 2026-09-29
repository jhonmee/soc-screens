// MuroSOC - BrowserEnvironment
using System;
using System.Threading.Tasks;
using Microsoft.Web.WebView2.Core;

namespace MuroSoc
{
    internal static class BrowserEnvironment
    {
        public const string DefaultProfile = "default";

        private static Task<CoreWebView2Environment> creation;

        public static CoreWebView2Environment Current { get; private set; }

        public static Task<CoreWebView2Environment> GetAsync()
        {
            if (creation == null)
            {
                creation = CreateAsync();
            }
            return creation;
        }

        public static async Task<CoreWebView2Controller> CreateControllerAsync(IntPtr parentWindow, string profileName)
        {
            CoreWebView2Environment environment = await GetAsync();
            CoreWebView2ControllerOptions options = environment.CreateCoreWebView2ControllerOptions();
            options.ProfileName = string.IsNullOrEmpty(profileName) ? DefaultProfile : profileName;
            options.IsInPrivateModeEnabled = false;
            return await environment.CreateCoreWebView2ControllerAsync(parentWindow, options);
        }

        private static async Task<CoreWebView2Environment> CreateAsync()
        {
            CoreWebView2EnvironmentOptions options = new CoreWebView2EnvironmentOptions();
            options.AllowSingleSignOnUsingOSPrimaryAccount = true;
            options.Language = "es-CO";
            CoreWebView2Environment environment = await CoreWebView2Environment.CreateAsync(null, AppPaths.ProfileFolder, options);
            Current = environment;
            return environment;
        }
    }
}
