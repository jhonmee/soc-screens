// MuroSOC - AppPaths
using System;
using System.IO;

namespace MuroSoc
{
    internal static class AppPaths
    {
        public static string Root
        {
            get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MuroSOC"); }
        }

        public static string ProfileFolder
        {
            get { return Path.Combine(Root, "Profile"); }
        }

        public static string LayoutsFolder
        {
            get { return Path.Combine(Root, "layouts"); }
        }

        public static string LogsFolder
        {
            get { return Path.Combine(Root, "logs"); }
        }

        public static string DownloadsFolder
        {
            get { return Path.Combine(Root, "downloads"); }
        }

        public static string ConfigFile
        {
            get { return Path.Combine(Root, "config.json"); }
        }

        public static string StateFile
        {
            get { return Path.Combine(Root, "state.json"); }
        }

        public static void EnsureCreated()
        {
            Directory.CreateDirectory(Root);
            Directory.CreateDirectory(ProfileFolder);
            Directory.CreateDirectory(LayoutsFolder);
            Directory.CreateDirectory(LogsFolder);
        }
    }
}
