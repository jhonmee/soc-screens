// MuroSOC - Log
using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace MuroSoc
{
    internal static class Log
    {
        private const long MaxBytes = 5L * 1024 * 1024;
        private const int MaxFiles = 5;
        private const string BaseName = "murosoc";

        private static readonly object Sync = new object();

        public static void Info(string message)
        {
            Write("INFO", message);
        }

        public static void Warn(string message)
        {
            Write("WARN", message);
        }

        public static void Error(string message, Exception ex)
        {
            Write("ERROR", ex == null ? message : message + " | " + ex.GetType().Name + ": " + ex.Message);
        }

        public static string SafeUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                return string.Empty;
            }
            Uri uri;
            if (!Uri.TryCreate(url, UriKind.Absolute, out uri))
            {
                return "(url no valida)";
            }
            if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            {
                return uri.Scheme + ":";
            }
            string path = uri.AbsolutePath;
            if (path.Length > 120)
            {
                path = path.Substring(0, 120) + "...";
            }
            return uri.Scheme + "://" + uri.Authority + path;
        }

        private static void Write(string level, string message)
        {
            string line = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture) + " " + level + " " + message + Environment.NewLine;
            lock (Sync)
            {
                try
                {
                    Directory.CreateDirectory(AppPaths.LogsFolder);
                    string path = FilePath(0);
                    FileInfo info = new FileInfo(path);
                    if (info.Exists && info.Length + line.Length > MaxBytes)
                    {
                        Rotate();
                    }
                    File.AppendAllText(path, line, Encoding.UTF8);
                }
                catch (Exception)
                {
                }
            }
        }

        private static void Rotate()
        {
            string oldest = FilePath(MaxFiles - 1);
            if (File.Exists(oldest))
            {
                File.Delete(oldest);
            }
            for (int i = MaxFiles - 2; i >= 0; i--)
            {
                string source = FilePath(i);
                if (File.Exists(source))
                {
                    File.Move(source, FilePath(i + 1));
                }
            }
        }

        private static string FilePath(int index)
        {
            string name = index == 0 ? BaseName + ".log" : BaseName + "." + index.ToString(CultureInfo.InvariantCulture) + ".log";
            return Path.Combine(AppPaths.LogsFolder, name);
        }
    }
}
