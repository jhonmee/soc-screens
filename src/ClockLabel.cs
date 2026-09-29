// MuroSOC - ClockLabel
using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace MuroSoc
{
    internal sealed class ClockLabel : Label
    {
        private string timeZoneId;
        private TimeZoneInfo zone;

        public ClockLabel()
        {
            AutoSize = true;
            BackColor = Color.FromArgb(20, 20, 20);
            ForeColor = Color.Gainsboro;
            Padding = new Padding(6, 3, 6, 3);
            Visible = false;
        }

        public void UpdateClock(AppConfig config, bool locked, Rectangle area)
        {
            if (!config.ClockEnabled)
            {
                if (Visible)
                {
                    Visible = false;
                }
                return;
            }
            if (timeZoneId != config.ClockTimeZone)
            {
                timeZoneId = config.ClockTimeZone;
                zone = FindZone(timeZoneId);
            }
            DateTime now = zone == null ? DateTime.Now : TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zone);
            string text = (locked ? "🔒 " : string.Empty) + now.ToString(config.ClockShowSeconds ? "HH:mm:ss" : "HH:mm", CultureInfo.InvariantCulture);
            if (zone != null && config.ClockShowZone)
            {
                text += " " + ShortZoneName(zone, now);
            }
            Font font = new Font("Segoe UI", Dpi.ScaleF(this, config.ClockFontSize), FontStyle.Bold, GraphicsUnit.Pixel);
            if (Font.Size != font.Size)
            {
                Font = font;
            }
            else
            {
                font.Dispose();
            }
            if (Text != text)
            {
                Text = text;
            }
            Place(config.ClockCorner, area);
            if (!Visible)
            {
                Visible = true;
            }
            BringToFront();
        }

        private void Place(string corner, Rectangle area)
        {
            if (Parent == null)
            {
                return;
            }
            int margin = Dpi.Scale(this, 8);
            int x = area.Right - Width - margin;
            int y = area.Top + margin;
            switch ((corner ?? string.Empty).ToLowerInvariant())
            {
                case "topleft":
                    x = area.Left + margin;
                    break;
                case "bottomleft":
                    x = area.Left + margin;
                    y = area.Bottom - Height - margin;
                    break;
                case "bottomright":
                    y = area.Bottom - Height - margin;
                    break;
            }
            Point location = new Point(Math.Max(0, x), Math.Max(0, y));
            if (Location != location)
            {
                Location = location;
            }
        }

        private static TimeZoneInfo FindZone(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return null;
            }
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (TimeZoneNotFoundException)
            {
                Log.Warn("Zona horaria del reloj no encontrada: " + id);
                return null;
            }
            catch (InvalidTimeZoneException)
            {
                return null;
            }
        }

        private static string ShortZoneName(TimeZoneInfo zone, DateTime local)
        {
            TimeSpan offset = zone.GetUtcOffset(local);
            string sign = offset < TimeSpan.Zero ? "-" : "+";
            TimeSpan abs = offset.Duration();
            return "UTC" + sign + abs.Hours.ToString("00", CultureInfo.InvariantCulture) + (abs.Minutes != 0 ? ":" + abs.Minutes.ToString("00", CultureInfo.InvariantCulture) : string.Empty);
        }
    }
}
