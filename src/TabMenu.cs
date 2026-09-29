// MuroSOC - TabMenu
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace MuroSoc
{
    internal static class TabMenu
    {
        private static readonly int[] RefreshOptions = new int[] { 30, 60, 120, 300, 600, 900, 1800 };

        public static List<MenuEntry> Build(Cell cell, BrowserTab tab)
        {
            List<MenuEntry> menu = new List<MenuEntry>();
            menu.Add(MenuEntry.Sub("Autorefresh", BuildRefreshMenu(cell, tab)));
            return menu;
        }

        private static List<MenuEntry> BuildRefreshMenu(Cell cell, BrowserTab tab)
        {
            List<MenuEntry> menu = new List<MenuEntry>();
            int current = tab.Settings.AutoRefreshSeconds;
            menu.Add(MenuEntry.Check("Apagado", current == 0, delegate { tab.SetAutoRefresh(0); }));
            bool matched = current == 0;
            foreach (int option in RefreshOptions)
            {
                int seconds = option;
                if (seconds == current)
                {
                    matched = true;
                }
                menu.Add(MenuEntry.Check("Cada " + BrowserTab.FormatInterval(seconds), seconds == current, delegate { tab.SetAutoRefresh(seconds); }));
            }
            string custom = matched ? "Personalizado..." : "Personalizado (" + BrowserTab.FormatInterval(current) + ")...";
            menu.Add(MenuEntry.Check(custom, !matched, delegate
            {
                int? value = InputDialog.PromptNumber(cell.FindForm(), "Autorefresh", "Segundos entre recargas (mínimo 30):", current > 0 ? current : 300, 30, 86400);
                if (value.HasValue)
                {
                    tab.SetAutoRefresh(value.Value);
                }
            }));
            string status = tab.RefreshStatusText;
            if (!string.IsNullOrEmpty(status))
            {
                menu.Add(MenuEntry.Separator());
                MenuEntry info = MenuEntry.Item(status, null);
                info.Enabled = false;
                menu.Add(info);
            }
            return menu;
        }
    }
}
