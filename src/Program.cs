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

            AppPaths.EnsureCreated();

            string startUrl = args.Length > 0 ? args[0] : "https://www.microsoft.com/es-co/security";
            Application.Run(new WallWindow(startUrl));
        }
    }
}
