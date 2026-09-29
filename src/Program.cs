// MuroSOC - Program
using System;
using System.Windows.Forms;

namespace MuroSoc
{
    internal static class Program
    {
        private static ApplicationContext pendingContext;
        private static string pendingUrl;

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

            string startUrl = args.Length > 0 ? UrlInput.Normalize(args[0]) : null;
            ApplicationContext context = new ApplicationContext();
            Application.Idle += StartOnce;
            pendingContext = context;
            pendingUrl = startUrl;
            Application.Run(context);
        }

        private static void StartOnce(object sender, EventArgs e)
        {
            Application.Idle -= StartOnce;
            try
            {
                Wall.Start(pendingContext, pendingUrl);
            }
            catch (Exception ex)
            {
                Log.Error("No se pudo iniciar el muro", ex);
                MessageBox.Show("No se pudo iniciar Muro SOC.\r\n\r\n" + ex.Message, "Muro SOC", MessageBoxButtons.OK, MessageBoxIcon.Error);
                pendingContext.ExitThread();
            }
        }
    }
}
