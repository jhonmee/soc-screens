// MuroSOC - LayoutStore
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MuroSoc
{
    internal static class LayoutStore
    {
        public static List<string> ListNames()
        {
            List<string> names = new List<string>();
            foreach (KeyValuePair<string, string> pair in Index())
            {
                names.Add(pair.Key);
            }
            names.Sort(StringComparer.CurrentCultureIgnoreCase);
            return names;
        }

        public static bool Exists(string name)
        {
            return FindPath(name) != null;
        }

        public static LayoutModel Load(string name)
        {
            string path = FindPath(name);
            if (path == null)
            {
                return null;
            }
            LayoutModel layout = JsonFile.Read<LayoutModel>(path);
            layout.Normalize();
            layout.Name = name;
            return layout;
        }

        public static void Save(LayoutModel layout)
        {
            Directory.CreateDirectory(AppPaths.LayoutsFolder);
            string path = FindPath(layout.Name) ?? Path.Combine(AppPaths.LayoutsFolder, FileNameFor(layout.Name));
            JsonFile.Write(path, Sanitized(layout));
        }

        public static void Delete(string name)
        {
            string path = FindPath(name);
            if (path != null)
            {
                File.Delete(path);
            }
        }

        public static void Export(LayoutModel layout, string path)
        {
            JsonFile.Write(path, Sanitized(layout));
        }

        public static LayoutModel Import(string path)
        {
            LayoutModel layout = JsonFile.Read<LayoutModel>(path);
            if (layout == null || layout.Monitors == null || layout.Monitors.Count == 0)
            {
                throw new InvalidDataException("El archivo no contiene un layout de Muro SOC.");
            }
            layout.Normalize();
            if (string.IsNullOrEmpty(layout.Name) || layout.Name == "Sin nombre")
            {
                layout.Name = Path.GetFileNameWithoutExtension(path);
            }
            return layout;
        }

        public static SessionState LoadState()
        {
            if (!File.Exists(AppPaths.StateFile))
            {
                return null;
            }
            try
            {
                SessionState state = JsonFile.Read<SessionState>(AppPaths.StateFile);
                if (state != null && state.Layout != null)
                {
                    state.Layout.Normalize();
                }
                return state;
            }
            catch (Exception ex)
            {
                Log.Error("state.json no se pudo leer; se inicia con el layout por defecto", ex);
                try
                {
                    File.Copy(AppPaths.StateFile, AppPaths.StateFile + ".error-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".json", true);
                }
                catch (Exception)
                {
                }
                return null;
            }
        }

        public static void SaveState(SessionState state)
        {
            if (state.Layout != null)
            {
                state.Layout = Sanitized(state.Layout);
            }
            JsonFile.Write(AppPaths.StateFile, state);
        }

        public static bool IsValidName(string name)
        {
            return !string.IsNullOrEmpty(name) && name.Trim().Length > 0 && name.Trim().Length <= 60;
        }

        private static LayoutModel Sanitized(LayoutModel layout)
        {
            List<PaneModel> cells = new List<PaneModel>();
            foreach (MonitorModel monitor in layout.Monitors)
            {
                if (monitor.Root != null)
                {
                    monitor.Root.CollectCells(cells);
                }
            }
            foreach (PaneModel cell in cells)
            {
                foreach (TabModel tab in cell.Tabs)
                {
                    tab.Url = UrlSanitizer.ForStorage(tab.Url);
                }
            }
            return layout;
        }

        private static Dictionary<string, string> Index()
        {
            Dictionary<string, string> index = new Dictionary<string, string>(StringComparer.CurrentCultureIgnoreCase);
            if (!Directory.Exists(AppPaths.LayoutsFolder))
            {
                return index;
            }
            foreach (string path in Directory.GetFiles(AppPaths.LayoutsFolder, "*.json"))
            {
                try
                {
                    LayoutModel layout = JsonFile.Read<LayoutModel>(path);
                    string name = layout == null || string.IsNullOrEmpty(layout.Name) ? Path.GetFileNameWithoutExtension(path) : layout.Name;
                    if (!index.ContainsKey(name))
                    {
                        index[name] = path;
                    }
                }
                catch (Exception ex)
                {
                    Log.Error("Layout ilegible: " + Path.GetFileName(path), ex);
                }
            }
            return index;
        }

        private static string FindPath(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return null;
            }
            string path;
            return Index().TryGetValue(name, out path) ? path : null;
        }

        private static string FileNameFor(string name)
        {
            StringBuilder builder = new StringBuilder();
            char[] invalid = Path.GetInvalidFileNameChars();
            foreach (char c in name.Trim())
            {
                builder.Append(Array.IndexOf(invalid, c) >= 0 || c == '.' ? '_' : c);
            }
            string stem = builder.ToString();
            if (stem.Length == 0)
            {
                stem = "layout";
            }
            string candidate = stem + ".json";
            int counter = 2;
            while (File.Exists(Path.Combine(AppPaths.LayoutsFolder, candidate)))
            {
                candidate = stem + " (" + counter + ").json";
                counter++;
            }
            return candidate;
        }
    }
}
