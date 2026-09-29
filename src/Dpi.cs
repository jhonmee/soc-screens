// MuroSOC - Dpi
using System;
using System.Windows.Forms;

namespace MuroSoc
{
    internal static class Dpi
    {
        public static int Of(Control control)
        {
            try
            {
                if (control != null && control.IsHandleCreated)
                {
                    int dpi = (int)NativeMethods.GetDpiForWindow(control.Handle);
                    if (dpi > 0)
                    {
                        return dpi;
                    }
                }
                int system = (int)NativeMethods.GetDpiForSystem();
                return system > 0 ? system : 96;
            }
            catch (EntryPointNotFoundException)
            {
                return 96;
            }
            catch (DllNotFoundException)
            {
                return 96;
            }
        }

        public static int Scale(Control control, int value)
        {
            return (int)Math.Round(value * Of(control) / 96.0);
        }

        public static float ScaleF(Control control, float value)
        {
            return value * Of(control) / 96f;
        }
    }
}
