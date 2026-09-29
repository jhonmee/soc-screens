// MuroSOC - MonitorMapper
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace MuroSoc
{
    internal static class MonitorMapper
    {
        public static List<KeyValuePair<Screen, PaneModel>> Map(LayoutModel layout, List<string> warnings)
        {
            Screen[] screens = Screen.AllScreens;
            List<KeyValuePair<Screen, PaneModel>> result = new List<KeyValuePair<Screen, PaneModel>>();
            List<MonitorModel> pending = new List<MonitorModel>(layout.Monitors);
            List<Screen> free = new List<Screen>(screens);
            Dictionary<MonitorModel, Screen> assigned = new Dictionary<MonitorModel, Screen>();

            foreach (MonitorModel monitor in new List<MonitorModel>(pending))
            {
                Screen match = free.Find(delegate(Screen s) { return string.Equals(s.DeviceName, monitor.DeviceName, StringComparison.OrdinalIgnoreCase); });
                if (match != null)
                {
                    Assign(monitor, match, pending, free, assigned);
                }
            }
            foreach (MonitorModel monitor in new List<MonitorModel>(pending))
            {
                Screen match = free.Find(delegate(Screen s) { return s.Bounds.Width == monitor.Width && s.Bounds.Height == monitor.Height; });
                if (match != null)
                {
                    Assign(monitor, match, pending, free, assigned);
                }
            }
            foreach (MonitorModel monitor in new List<MonitorModel>(pending))
            {
                if (free.Count == 0)
                {
                    break;
                }
                Screen match = free.Find(delegate(Screen s) { return s.Primary == monitor.Primary; }) ?? free[0];
                Assign(monitor, match, pending, free, assigned);
            }

            foreach (MonitorModel monitor in layout.Monitors)
            {
                Screen screen;
                if (assigned.TryGetValue(monitor, out screen))
                {
                    result.Add(new KeyValuePair<Screen, PaneModel>(screen, monitor.Root));
                }
            }

            if (result.Count == 0)
            {
                PaneModel root = PaneModel.NewCell(App.Config.HomeUrl);
                if (pending.Count > 0)
                {
                    root = pending[0].Root;
                    pending.RemoveAt(0);
                }
                result.Add(new KeyValuePair<Screen, PaneModel>(Screen.PrimaryScreen, root));
            }

            if (pending.Count > 0)
            {
                int lastIndex = result.Count - 1;
                KeyValuePair<Screen, PaneModel> last = result[lastIndex];
                PaneModel merged = last.Value;
                List<string> names = new List<string>();
                foreach (MonitorModel monitor in pending)
                {
                    PaneModel split = new PaneModel();
                    split.Type = PaneModel.SplitType;
                    split.Direction = PaneModel.Columns;
                    split.Ratio = 0.5;
                    split.First = merged;
                    split.Second = monitor.Root;
                    merged = split;
                    names.Add(ShortName(monitor.DeviceName) + " (" + monitor.Width + "x" + monitor.Height + ")");
                }
                result[lastIndex] = new KeyValuePair<Screen, PaneModel>(last.Key, merged);
                warnings.Add("Faltan monitores del layout: " + string.Join(", ", names.ToArray()) + ". Sus celdas se movieron a " + ShortName(last.Key.DeviceName) + ".");
            }
            return result;
        }

        public static string ShortName(string deviceName)
        {
            return string.IsNullOrEmpty(deviceName) ? "monitor" : deviceName.Replace(@"\\.\", string.Empty);
        }

        private static void Assign(MonitorModel monitor, Screen screen, List<MonitorModel> pending, List<Screen> free, Dictionary<MonitorModel, Screen> assigned)
        {
            assigned[monitor] = screen;
            pending.Remove(monitor);
            free.Remove(screen);
        }
    }
}
