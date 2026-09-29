// MuroSOC - Program
using System;
using System.Windows.Forms;

namespace MuroSoc
{
    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            string runtimeVersion = RuntimeInfo.GetRuntimeVersion();
            if (runtimeVersion == null)
            {
                Application.Run(new MissingRuntimeForm());
                return;
            }

            App.Initialize(runtimeVersion);

            string startUrl = args.Length > 0 ? args[0] : App.Config.HomeUrl;
            Application.Run(new WallWindow(startUrl));
        }
    }
}
