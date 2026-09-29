// MuroSOC - TabStrip
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace MuroSoc
{
    internal sealed class TabStrip : Control
    {
        private static readonly Color Back = Color.FromArgb(24, 24, 24);
        private static readonly Color ActiveBack = Color.FromArgb(58, 58, 60);
        private static readonly Color HoverBack = Color.FromArgb(42, 42, 44);
        private const int SlotMenu = 0;
        private const int SlotFullscreen = 1;
        private const int SlotTemplates = 2;
        private const int ButtonCount = 4;

        private readonly Cell cell;
        private readonly ToolTip tip;
        private int hoverIndex = -1;
        private bool hoverClose;
        private int pressIndex = -1;
        private Point pressPoint;
        private bool dragging;
        private string lastTip;

        public TabStrip(Cell cell)
        {
            this.cell = cell;
            Dock = DockStyle.Top;
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            BackColor = Back;
            ForeColor = Color.Gainsboro;
            tip = new ToolTip();
            ApplyScale();
            HandleCreated += delegate { ApplyScale(); };
        }

        public int InsertIndexAt(Point client)
        {
            int count = cell.Tabs.Count;
            for (int i = 0; i < count; i++)
            {
                Rectangle rect = TabRect(i, count);
                if (client.X < rect.Left + rect.Width / 2)
                {
                    return i;
                }
            }
            return count;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(Back);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            int count = cell.Tabs.Count;
            using (Font font = new Font("Segoe UI", Dpi.ScaleF(this, 12f), FontStyle.Regular, GraphicsUnit.Pixel))
            using (Font small = new Font("Segoe UI", Dpi.ScaleF(this, 10f), FontStyle.Regular, GraphicsUnit.Pixel))
            {
                for (int i = 0; i < count; i++)
                {
                    DrawTab(g, font, small, i, count);
                }
                for (int slot = 0; slot < ButtonCount; slot++)
                {
                    DrawButton(g, font, ButtonRect(slot), ButtonGlyph(slot), hoverIndex == ButtonCode(slot));
                }
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            Wall.SetActiveCell(cell);
            int count = cell.Tabs.Count;
            int button = ButtonAt(e.Location);
            if (button >= 0)
            {
                if (e.Button == MouseButtons.Left)
                {
                    RunButton(button);
                }
                return;
            }
            int index = IndexAt(e.Location, count);
            if (index < 0)
            {
                return;
            }
            BrowserTab tab = cell.Tabs[index];
            if (e.Button == MouseButtons.Middle)
            {
                cell.CloseTab(tab, true);
                return;
            }
            if (e.Button == MouseButtons.Right)
            {
                cell.Activate(tab);
                MenuEntry.ShowAt(this, e.Location, Wall.BuildCellMenu(cell, tab));
                return;
            }
            if (e.Button == MouseButtons.Left)
            {
                pressIndex = index;
                pressPoint = e.Location;
                if (!CloseRect(TabRect(index, count)).Contains(e.Location))
                {
                    cell.Activate(tab);
                }
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            int count = cell.Tabs.Count;
            if (pressIndex >= 0 && pressIndex < count && (e.Button & MouseButtons.Left) != 0)
            {
                Size drag = SystemInformation.DragSize;
                if (!dragging && (Math.Abs(e.X - pressPoint.X) > drag.Width || Math.Abs(e.Y - pressPoint.Y) > drag.Height))
                {
                    dragging = true;
                    Capture = true;
                    Cursor = Cursors.SizeAll;
                }
                if (dragging)
                {
                    Wall.UpdateTabDrag(Cursor.Position);
                    return;
                }
            }
            int index = IndexAt(e.Location, count);
            bool close = index >= 0 && CloseRect(TabRect(index, count)).Contains(e.Location);
            int button = ButtonAt(e.Location);
            if (button >= 0)
            {
                index = ButtonCode(button);
            }
            if (index != hoverIndex || close != hoverClose)
            {
                hoverIndex = index;
                hoverClose = close;
                Invalidate();
            }
            UpdateTip(index, count);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            int count = cell.Tabs.Count;
            int pressed = pressIndex;
            pressIndex = -1;
            if (dragging)
            {
                dragging = false;
                Capture = false;
                Cursor = Cursors.Default;
                if (pressed >= 0 && pressed < count)
                {
                    Wall.CompleteTabDrag(cell.Tabs[pressed], Cursor.Position);
                }
                else
                {
                    Wall.CancelTabDrag();
                }
                return;
            }
            if (e.Button == MouseButtons.Left && pressed >= 0 && pressed < count)
            {
                if (CloseRect(TabRect(pressed, count)).Contains(e.Location))
                {
                    cell.CloseTab(cell.Tabs[pressed], true);
                }
            }
        }

        protected override void OnMouseCaptureChanged(EventArgs e)
        {
            base.OnMouseCaptureChanged(e);
            if (dragging && !Capture)
            {
                dragging = false;
                pressIndex = -1;
                Cursor = Cursors.Default;
                Wall.CancelTabDrag();
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            hoverIndex = -1;
            hoverClose = false;
            Invalidate();
        }

        protected override void OnDoubleClick(EventArgs e)
        {
            base.OnDoubleClick(e);
            Point point = PointToClient(Cursor.Position);
            if (IndexAt(point, cell.Tabs.Count) < 0 && ButtonAt(point) < 0)
            {
                Wall.NewTab(cell);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                tip.Dispose();
            }
            base.Dispose(disposing);
        }

        private void ApplyScale()
        {
            Height = Dpi.Scale(this, 30);
        }

        private void UpdateTip(int index, int count)
        {
            string text = null;
            if (index >= 0 && index < count)
            {
                BrowserTab tab = cell.Tabs[index];
                text = tab.DisplayTitle + "\r\n" + Log.SafeUrl(tab.Url) + "\r\nPerfil: " + tab.ProfileName;
                string refresh = tab.RefreshStatusText;
                if (!string.IsNullOrEmpty(refresh))
                {
                    text += "\r\n" + refresh;
                }
            }
            else
            {
                for (int slot = 0; slot < ButtonCount; slot++)
                {
                    if (index == ButtonCode(slot))
                    {
                        text = ButtonTip(slot);
                    }
                }
            }
            if (text != lastTip)
            {
                lastTip = text;
                tip.SetToolTip(this, text);
            }
        }

        private int IndexAt(Point point, int count)
        {
            for (int i = 0; i < count; i++)
            {
                if (TabRect(i, count).Contains(point))
                {
                    return i;
                }
            }
            return -1;
        }

        private int ButtonWidth
        {
            get { return Height; }
        }

        private static int ButtonCode(int slot)
        {
            return -10 - slot;
        }

        private static string ButtonGlyph(int slot)
        {
            switch (slot)
            {
                case SlotMenu: return "☰";
                case SlotFullscreen: return "⛶";
                case SlotTemplates: return "⊞";
                default: return "+";
            }
        }

        private static string ButtonTip(int slot)
        {
            switch (slot)
            {
                case SlotMenu: return "Menú de la celda";
                case SlotFullscreen: return (App.Config.Fullscreen ? "Salir de pantalla completa" : "Pantalla completa") + " (" + Shortcuts.Display(Shortcuts.ToggleFullscreen) + ")";
                case SlotTemplates: return "Plantillas de layout (" + Shortcuts.Display(Shortcuts.Templates) + ")";
                default: return "Pestaña nueva (" + Shortcuts.Display(Shortcuts.NewTab) + ")";
            }
        }

        private void RunButton(int slot)
        {
            switch (slot)
            {
                case SlotMenu:
                    Rectangle menu = ButtonRect(SlotMenu);
                    MenuEntry.ShowAt(this, new Point(menu.Left, menu.Bottom), Wall.BuildCellMenu(cell, cell.ActiveTab));
                    break;
                case SlotFullscreen:
                    Wall.ToggleFullscreen();
                    break;
                case SlotTemplates:
                    Wall.OpenTemplates();
                    break;
                default:
                    Wall.NewTab(cell);
                    break;
            }
        }

        private Rectangle ButtonRect(int slot)
        {
            return new Rectangle(Width - ButtonWidth * (slot + 1), 0, ButtonWidth, Height);
        }

        private int ButtonAt(Point point)
        {
            for (int slot = 0; slot < ButtonCount; slot++)
            {
                if (ButtonRect(slot).Contains(point))
                {
                    return slot;
                }
            }
            return -1;
        }

        private Rectangle TabRect(int index, int count)
        {
            int available = Math.Max(0, Width - ButtonWidth * ButtonCount);
            int max = Dpi.Scale(this, 240);
            int min = Dpi.Scale(this, 60);
            int width = count == 0 ? max : Math.Max(min, Math.Min(max, available / count));
            return new Rectangle(index * width, 0, width - 1, Height);
        }

        private Rectangle CloseRect(Rectangle tab)
        {
            int size = Dpi.Scale(this, 18);
            return new Rectangle(tab.Right - size - Dpi.Scale(this, 6), tab.Top + (tab.Height - size) / 2, size, size);
        }

        private void DrawTab(Graphics g, Font font, Font small, int index, int count)
        {
            BrowserTab tab = cell.Tabs[index];
            Rectangle rect = TabRect(index, count);
            bool isActive = tab == cell.ActiveTab;
            Color back = isActive ? ActiveBack : (index == hoverIndex ? HoverBack : Back);
            using (SolidBrush brush = new SolidBrush(back))
            {
                g.FillRectangle(brush, rect);
            }
            if (isActive)
            {
                using (SolidBrush accent = new SolidBrush(Cell.AccentColor))
                {
                    g.FillRectangle(accent, rect.Left, rect.Bottom - Dpi.Scale(this, 2), rect.Width, Dpi.Scale(this, 2));
                }
            }

            int pad = Dpi.Scale(this, 8);
            int icon = Dpi.Scale(this, 16);
            int x = rect.Left + pad;
            Image favicon = tab.Favicon;
            if (favicon != null)
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.DrawImage(favicon, new Rectangle(x, rect.Top + (rect.Height - icon) / 2, icon, icon));
                x += icon + Dpi.Scale(this, 6);
            }

            Color dot = Color.Empty;
            if (tab.ErrorText != null)
            {
                dot = Cell.ErrorColor;
            }
            else if (tab.IsRecovering)
            {
                dot = Cell.RecoveringColor;
            }
            else if (tab.IsAtLogin)
            {
                dot = Cell.LoginColor;
            }
            if (dot != Color.Empty)
            {
                int d = Dpi.Scale(this, 8);
                using (SolidBrush brush = new SolidBrush(dot))
                {
                    g.FillEllipse(brush, x, rect.Top + (rect.Height - d) / 2, d, d);
                }
                x += d + Dpi.Scale(this, 6);
            }

            if (tab.Settings.AutoRefreshSeconds > 0)
            {
                bool paused = tab.IsRefreshPaused;
                string glyph = paused ? "⏸" : "⟳";
                Size glyphSize = TextRenderer.MeasureText(g, glyph, small, Size.Empty, TextFormatFlags.NoPadding);
                Rectangle glyphRect = new Rectangle(x, rect.Top, glyphSize.Width, rect.Height);
                TextRenderer.DrawText(g, glyph, small, glyphRect, paused ? Color.Gray : Color.FromArgb(80, 200, 120), TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.NoPadding);
                x += glyphSize.Width + Dpi.Scale(this, 5);
            }

            Rectangle close = CloseRect(rect);
            bool showClose = isActive || index == hoverIndex;
            int textRight = showClose ? close.Left - Dpi.Scale(this, 4) : rect.Right - pad;

            string countdown = Wall.EditMode || index == hoverIndex ? tab.RefreshCountdownText : null;
            if (!string.IsNullOrEmpty(countdown))
            {
                Size size = TextRenderer.MeasureText(g, countdown, small);
                Rectangle countRect = new Rectangle(textRight - size.Width, rect.Top, size.Width, rect.Height);
                TextRenderer.DrawText(g, countdown, small, countRect, Color.DarkGray, TextFormatFlags.VerticalCenter | TextFormatFlags.Right | TextFormatFlags.NoPadding);
                textRight = countRect.Left - Dpi.Scale(this, 4);
            }

            Rectangle textRect = new Rectangle(x, rect.Top, Math.Max(0, textRight - x), rect.Height);
            TextRenderer.DrawText(g, tab.DisplayTitle, font, textRect, isActive ? Color.White : Color.Silver,
                TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine);

            if (showClose)
            {
                if (hoverClose && index == hoverIndex)
                {
                    using (SolidBrush brush = new SolidBrush(Color.FromArgb(90, 90, 90)))
                    {
                        g.FillEllipse(brush, close);
                    }
                }
                TextRenderer.DrawText(g, "✕", small, close, Color.Gainsboro, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
            }
        }

        private void DrawButton(Graphics g, Font font, Rectangle rect, string text, bool hover)
        {
            if (hover)
            {
                using (SolidBrush brush = new SolidBrush(HoverBack))
                {
                    g.FillRectangle(brush, rect);
                }
            }
            TextRenderer.DrawText(g, text, font, rect, Color.Gainsboro, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
        }
    }
}
