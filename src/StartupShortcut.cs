// MuroSOC - StartupShortcut
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Windows.Forms;

namespace MuroSoc
{
    internal static class StartupShortcut
    {
        private const string FileName = "Muro SOC.lnk";

        public static string ShortcutPath
        {
            get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), FileName); }
        }

        public static bool IsEnabled()
        {
            try
            {
                return File.Exists(ShortcutPath);
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static void SetEnabled(bool enabled)
        {
            if (enabled)
            {
                Create();
            }
            else if (File.Exists(ShortcutPath))
            {
                File.Delete(ShortcutPath);
                Log.Info("Inicio automático desactivado");
            }
        }

        private static void Create()
        {
            string exe = Application.ExecutablePath;
            IShellLinkW link = (IShellLinkW)new ShellLink();
            try
            {
                link.SetPath(exe);
                link.SetWorkingDirectory(Path.GetDirectoryName(exe));
                link.SetDescription("Muro SOC");
                link.SetIconLocation(exe, 0);
                ((IPersistFile)link).Save(ShortcutPath, true);
                Log.Info("Inicio automático activado con acceso directo en la carpeta Inicio del usuario");
            }
            finally
            {
                Marshal.FinalReleaseComObject(link);
            }
        }

        [ComImport]
        [Guid("00021401-0000-0000-C000-000000000046")]
        private class ShellLink
        {
        }

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("000214F9-0000-0000-C000-000000000046")]
        private interface IShellLinkW
        {
            void GetPath([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder file, int maxPath, IntPtr findData, int flags);

            void GetIDList(out IntPtr idList);

            void SetIDList(IntPtr idList);

            void GetDescription([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder name, int maxName);

            void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string name);

            void GetWorkingDirectory([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder dir, int maxPath);

            void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string dir);

            void GetArguments([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder args, int maxPath);

            void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string args);

            void GetHotkey(out short hotkey);

            void SetHotkey(short hotkey);

            void GetShowCmd(out int showCmd);

            void SetShowCmd(int showCmd);

            void GetIconLocation([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder iconPath, int maxIconPath, out int icon);

            void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string iconPath, int icon);

            void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string relativePath, int reserved);

            void Resolve(IntPtr hwnd, int flags);

            void SetPath([MarshalAs(UnmanagedType.LPWStr)] string file);
        }
    }
}
