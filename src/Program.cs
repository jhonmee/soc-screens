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
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += delegate(object sender, System.Threading.ThreadExceptionEventArgs e)
            {
                Log.Error("Error no controlado en la interfaz", e.Exception);
            };
            AppDomain.CurrentDomain.UnhandledException += delegate(object sender, UnhandledExceptionEventArgs e)
            {
                Log.Error("Error no controlado", e.ExceptionObject as Exception);
            };

            string runtimeVersion = RuntimeInfo.GetRuntimeVersion();
            if (runtimeVersion == null)
            {
                Application.Run(new MissingRuntimeForm());
                return;
            }

            bool firstInstance;
            using (System.Threading.Mutex mutex = new System.Threading.Mutex(true, @"Local\MuroSOC-SingleInstance", out firstInstance))
            {
                if (!firstInstance)
                {
                    MessageBox.Show("Muro SOC ya está abierto en esta sesión.", "Muro SOC", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                App.Initialize(runtimeVersion);

                string startUrl = args.Length > 0 ? UrlInput.Normalize(args[0]) : null;
                ApplicationContext context = new ApplicationContext();
                Application.Idle += StartOnce;
                pendingContext = context;
                pendingUrl = startUrl;
                Application.Run(context);
                GC.KeepAlive(mutex);
            }
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
