// MuroSOC - Shortcuts
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace MuroSoc
{
    internal static class Shortcuts
    {
        public const string ToggleUi = "MostrarInterfaz";
        public const string MaximizeCell = "MaximizarCelda";
        public const string EditLayout = "EditarLayout";
        public const string LockWall = "BloquearMuro";
        public const string NewTab = "PestanaNueva";
        public const string CloseTab = "CerrarPestana";
        public const string ReopenTab = "ReabrirPestana";
        public const string NextTab = "PestanaSiguiente";
        public const string PreviousTab = "PestanaAnterior";
        public const string FocusAddress = "BarraDirecciones";
        public const string ReloadWall = "RecargarMuro";
        public const string OpenSettings = "Configuracion";
        public const string PickElement = "AislarElemento";
        public const string SaveLayout = "GuardarLayout";

        public static Dictionary<string, string> Defaults()
        {
            Dictionary<string, string> map = new Dictionary<string, string>();
            map[ToggleUi] = "F10";
            map[MaximizeCell] = "F11";
            map[EditLayout] = "F2";
            map[LockWall] = "Ctrl+Shift+L";
            map[NewTab] = "Ctrl+T";
            map[CloseTab] = "Ctrl+W";
            map[ReopenTab] = "Ctrl+Shift+T";
            map[NextTab] = "Ctrl+Tab";
            map[PreviousTab] = "Ctrl+Shift+Tab";
            map[FocusAddress] = "Ctrl+L";
            map[ReloadWall] = "Ctrl+Shift+F5";
            map[OpenSettings] = "Ctrl+Oemcomma";
            map[PickElement] = "Ctrl+Shift+E";
            map[SaveLayout] = "Ctrl+Shift+S";
            return map;
        }

        public static string Describe(string action)
        {
            switch (action)
            {
                case ToggleUi: return "Mostrar u ocultar la interfaz";
                case MaximizeCell: return "Maximizar la celda activa";
                case EditLayout: return "Editar layout";
                case LockWall: return "Bloquear o desbloquear el muro";
                case NewTab: return "Pestaña nueva";
                case CloseTab: return "Cerrar pestaña";
                case ReopenTab: return "Reabrir pestaña cerrada";
                case NextTab: return "Pestaña siguiente";
                case PreviousTab: return "Pestaña anterior";
                case FocusAddress: return "Ir a la barra de direcciones";
                case ReloadWall: return "Recargar todo el muro";
                case OpenSettings: return "Configuración";
                case PickElement: return "Aislar elemento";
                case SaveLayout: return "Guardar layout";
                default: return action;
            }
        }

        public static bool TryParse(string text, out Keys keys)
        {
            keys = Keys.None;
            if (string.IsNullOrEmpty(text))
            {
                return false;
            }
            Keys result = Keys.None;
            Keys code = Keys.None;
            foreach (string raw in text.Split('+'))
            {
                string part = raw.Trim();
                if (part.Length == 0)
                {
                    return false;
                }
                string lower = part.ToLowerInvariant();
                if (lower == "ctrl" || lower == "control")
                {
                    result |= Keys.Control;
                }
                else if (lower == "shift" || lower == "mayus")
                {
                    result |= Keys.Shift;
                }
                else if (lower == "alt")
                {
                    result |= Keys.Alt;
                }
                else
                {
                    if (code != Keys.None)
                    {
                        return false;
                    }
                    if (part.Length == 1 && char.IsDigit(part[0]))
                    {
                        part = "D" + part;
                    }
                    else if (lower == "esc")
                    {
                        part = "Escape";
                    }
                    try
                    {
                        code = (Keys)Enum.Parse(typeof(Keys), part, true);
                    }
                    catch (ArgumentException)
                    {
                        return false;
                    }
                    if ((code & Keys.Modifiers) != 0)
                    {
                        return false;
                    }
                }
            }
            if (code == Keys.None)
            {
                return false;
            }
            keys = result | code;
            return true;
        }

        public static string Format(Keys keys)
        {
            List<string> parts = new List<string>();
            if ((keys & Keys.Control) != 0)
            {
                parts.Add("Ctrl");
            }
            if ((keys & Keys.Shift) != 0)
            {
                parts.Add("Shift");
            }
            if ((keys & Keys.Alt) != 0)
            {
                parts.Add("Alt");
            }
            Keys code = keys & Keys.KeyCode;
            string name = code.ToString();
            if (code >= Keys.D0 && code <= Keys.D9)
            {
                name = name.Substring(1);
            }
            else if (code == Keys.Oemcomma)
            {
                name = ",";
            }
            parts.Add(name);
            return string.Join("+", parts.ToArray());
        }

        public static string Display(string action)
        {
            string text;
            Keys keys;
            if (App.Config.KeyBindings.TryGetValue(action, out text) && TryParse(text, out keys))
            {
                return Format(keys);
            }
            return string.Empty;
        }

        public static string Match(Keys keys)
        {
            foreach (KeyValuePair<string, string> pair in App.Config.KeyBindings)
            {
                Keys bound;
                if (TryParse(pair.Value, out bound) && bound == keys)
                {
                    return pair.Key;
                }
            }
            return null;
        }
    }
}
