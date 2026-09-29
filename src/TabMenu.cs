// MuroSOC - TabMenu
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace MuroSoc
{
    internal static class TabMenu
    {
        private static readonly int[] RefreshOptions = new int[] { 30, 60, 120, 300, 600, 900, 1800 };
        private static readonly int[] ZoomOptions = new int[] { 25, 33, 50, 67, 75, 80, 90, 100, 110, 125, 150, 175, 200 };

        public static List<MenuEntry> Build(Cell cell, BrowserTab tab)
        {
            List<MenuEntry> menu = new List<MenuEntry>();
            menu.Add(MenuEntry.Sub("Autorefresh", BuildRefreshMenu(cell, tab)));
            menu.Add(MenuEntry.Sub("Zoom y ancho virtual", BuildZoomMenu(cell, tab)));
            menu.Add(MenuEntry.Sub("Ajustes de página", BuildPageMenu(cell, tab)));
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

        private static List<MenuEntry> BuildZoomMenu(Cell cell, BrowserTab tab)
        {
            List<MenuEntry> menu = new List<MenuEntry>();
            double zoom = tab.Controller == null ? tab.Settings.Zoom : tab.Controller.ZoomFactor;
            bool virtualWidth = tab.Settings.VirtualWidth > 0;
            menu.Add(MenuEntry.Item("Acercar", "Ctrl+Rueda", delegate { tab.SetZoom(zoom * 1.1); }));
            menu.Add(MenuEntry.Item("Alejar", "Ctrl+Rueda", delegate { tab.SetZoom(zoom / 1.1); }));
            menu.Add(MenuEntry.Separator());
            foreach (int option in ZoomOptions)
            {
                double value = option / 100.0;
                bool selected = !virtualWidth && Math.Abs(zoom - value) < 0.005;
                menu.Add(MenuEntry.Check(option + " %", selected, delegate { tab.SetZoom(value); }));
            }
            menu.Add(MenuEntry.Separator());
            string label = virtualWidth ? "Ancho virtual: " + tab.Settings.VirtualWidth + " px..." : "Ancho virtual...";
            menu.Add(MenuEntry.Check(label, virtualWidth, delegate
            {
                int? value = InputDialog.PromptNumber(cell.FindForm(), "Ancho virtual",
                    "Renderizar como si la celda midiera este ancho en px (0 para desactivar):", virtualWidth ? tab.Settings.VirtualWidth : 1920, 0, 7680);
                if (value.HasValue)
                {
                    if (value.Value != 0 && value.Value < 320)
                    {
                        MessageBox.Show(cell.FindForm(), "El ancho mínimo es 320 px.", "Ancho virtual", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    tab.SetVirtualWidth(value.Value);
                }
            }));
            return menu;
        }

        private static List<MenuEntry> BuildPageMenu(Cell cell, BrowserTab tab)
        {
            TabModel settings = tab.Settings;
            List<MenuEntry> menu = new List<MenuEntry>();
            menu.Add(MenuEntry.Item("Fijar encuadre actual", delegate { tab.CapturePan(); }));
            MenuEntry clearPan = MenuEntry.Item("Quitar encuadre", delegate { tab.ClearPan(); });
            clearPan.Enabled = settings.PanEnabled;
            menu.Add(clearPan);
            menu.Add(MenuEntry.Separator());
            menu.Add(MenuEntry.Item("Aislar elemento...", Shortcuts.Display(Shortcuts.PickElement), delegate { tab.StartPicker("isolate"); }));
            MenuEntry clearIsolate = MenuEntry.Item("Quitar aislamiento", delegate
            {
                settings.IsolateSelector = string.Empty;
                tab.UpdatePageSettings();
            });
            clearIsolate.Enabled = !string.IsNullOrEmpty(settings.IsolateSelector);
            menu.Add(clearIsolate);
            menu.Add(MenuEntry.Item("Ocultar elemento...", delegate { tab.StartPicker("hide"); }));
            MenuEntry clearHidden = MenuEntry.Item("Mostrar elementos ocultos (" + settings.HideSelectors.Count + ")", delegate
            {
                settings.HideSelectors.Clear();
                tab.UpdatePageSettings();
            });
            clearHidden.Enabled = settings.HideSelectors.Count > 0;
            menu.Add(clearHidden);
            menu.Add(MenuEntry.Separator());
            menu.Add(MenuEntry.Check("CSS personalizado...", !string.IsNullOrEmpty(settings.CustomCss), delegate { EditCss(cell, tab); }));
            menu.Add(MenuEntry.Check("Ocultar scrollbars", settings.HideScrollbars, delegate
            {
                settings.HideScrollbars = !settings.HideScrollbars;
                tab.UpdatePageSettings();
            }));
            string minLabel = settings.MinWidth > 0 ? "Ancho mínimo: " + settings.MinWidth + " px..." : "Forzar ancho mínimo...";
            menu.Add(MenuEntry.Check(minLabel, settings.MinWidth > 0, delegate
            {
                int? value = InputDialog.PromptNumber(cell.FindForm(), "Ancho mínimo",
                    "Ancho mínimo de la página en px, con scroll horizontal (0 para desactivar):", settings.MinWidth > 0 ? settings.MinWidth : 1280, 0, 7680);
                if (value.HasValue)
                {
                    settings.MinWidth = value.Value;
                    tab.UpdatePageSettings();
                }
            }));
            return menu;
        }

        private static void EditCss(Cell cell, BrowserTab tab)
        {
            TabModel settings = tab.Settings;
            string pattern = InputDialog.Prompt(cell.FindForm(), "CSS personalizado",
                "Aplicar solo en URLs que coincidan con este patrón (* es comodín; vacío = todas):",
                string.IsNullOrEmpty(settings.CustomCssPattern) ? "*" : settings.CustomCssPattern, false);
            if (pattern == null)
            {
                return;
            }
            string css = InputDialog.Prompt(cell.FindForm(), "CSS personalizado", "CSS que se inyecta en la página:", settings.CustomCss, true);
            if (css == null)
            {
                return;
            }
            settings.CustomCssPattern = pattern.Trim() == "*" ? string.Empty : pattern.Trim();
            settings.CustomCss = css;
            tab.UpdatePageSettings();
        }
    }
}
