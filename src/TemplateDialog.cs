// MuroSOC - TemplateDialog
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace MuroSoc
{
    internal sealed class TemplateDialog : Form
    {
        private readonly List<LayoutTemplate> templates = LayoutTemplates.All();
        private readonly List<TemplateTile> tiles = new List<TemplateTile>();
        private readonly Panel preview;
        private readonly Label summary;
        private readonly ComboBox target;
        private readonly Button apply;
        private readonly List<WallWindow> windows;
        private LayoutTemplate selected;
        private LayoutTemplate hovered;

        public TemplateDialog(WallWindow current)
        {
            Text = "Plantillas de layout";
            StartPosition = FormStartPosition.CenterParent;
            ShowInTaskbar = false;
            MinimizeBox = false;
            MaximizeBox = false;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            AutoScaleMode = AutoScaleMode.Dpi;
            Font = SystemFonts.MessageBoxFont;
            BackColor = Color.FromArgb(32, 32, 32);
            ForeColor = Color.Gainsboro;
            ClientSize = new Size(900, 560);

            windows = new List<WallWindow>(Wall.OpenWindows);

            Label title = new Label();
            title.Text = "Elige una plantilla. La vista previa muestra dónde queda cada pestaña; nada cambia hasta que pulses Aplicar.";
            title.SetBounds(16, 12, 868, 22);

            FlowLayoutPanel grid = new FlowLayoutPanel();
            grid.SetBounds(16, 40, 500, 460);
            grid.AutoScroll = true;
            grid.BackColor = Color.FromArgb(24, 24, 24);
            foreach (LayoutTemplate template in templates)
            {
                TemplateTile tile = new TemplateTile(template);
                tile.Click += OnTileClick;
                tile.DoubleClick += delegate { ApplyAndClose(); };
                tile.MouseEnter += OnTileEnter;
                tile.MouseLeave += delegate
                {
                    hovered = null;
                    RefreshPreview();
                };
                tiles.Add(tile);
                grid.Controls.Add(tile);
            }

            preview = new PreviewPanel();
            preview.SetBounds(532, 40, 352, 230);
            preview.BackColor = Color.FromArgb(18, 18, 18);
            preview.Paint += OnPreviewPaint;

            summary = new Label();
            summary.SetBounds(532, 280, 352, 120);

            Label targetLabel = new Label();
            targetLabel.Text = "Aplicar en:";
            targetLabel.SetBounds(532, 410, 352, 20);

            target = new ComboBox();
            target.DropDownStyle = ComboBoxStyle.DropDownList;
            target.SetBounds(532, 432, 352, 24);
            int currentIndex = 0;
            for (int i = 0; i < windows.Count; i++)
            {
                Screen screen = windows[i].Screen;
                target.Items.Add("Monitor " + MonitorMapper.ShortName(screen.DeviceName) + " (" + screen.Bounds.Width + "x" + screen.Bounds.Height + (screen.Primary ? ", principal" : string.Empty) + ")");
                if (windows[i] == current)
                {
                    currentIndex = i;
                }
            }
            if (windows.Count > 1)
            {
                target.Items.Add("Todos los monitores");
            }
            if (target.Items.Count > 0)
            {
                target.SelectedIndex = currentIndex;
            }
            target.SelectedIndexChanged += delegate { RefreshPreview(); };

            apply = new Button();
            apply.Text = "Aplicar";
            apply.FlatStyle = FlatStyle.Flat;
            apply.BackColor = Cell.AccentColor;
            apply.ForeColor = Color.White;
            apply.SetBounds(708, 512, 84, 32);
            apply.Enabled = false;
            apply.Click += delegate { ApplyAndClose(); };

            Button cancel = new Button();
            cancel.Text = "Cancelar";
            cancel.FlatStyle = FlatStyle.Flat;
            cancel.DialogResult = DialogResult.Cancel;
            cancel.SetBounds(800, 512, 84, 32);

            CancelButton = cancel;
            Controls.Add(title);
            Controls.Add(grid);
            Controls.Add(preview);
            Controls.Add(summary);
            Controls.Add(targetLabel);
            Controls.Add(target);
            Controls.Add(apply);
            Controls.Add(cancel);
            RefreshPreview();
        }

        private List<WallWindow> TargetWindows()
        {
            List<WallWindow> list = new List<WallWindow>();
            int index = target.SelectedIndex;
            if (index >= 0 && index < windows.Count)
            {
                list.Add(windows[index]);
            }
            else if (index == windows.Count)
            {
                list.AddRange(windows);
            }
            return list;
        }

        private void OnTileClick(object sender, EventArgs e)
        {
            TemplateTile tile = (TemplateTile)sender;
            selected = tile.Template;
            foreach (TemplateTile item in tiles)
            {
                item.Selected = item == tile;
            }
            apply.Enabled = true;
            RefreshPreview();
        }

        private void OnTileEnter(object sender, EventArgs e)
        {
            hovered = ((TemplateTile)sender).Template;
            RefreshPreview();
        }

        private void RefreshPreview()
        {
            preview.Invalidate();
            LayoutTemplate shown = hovered ?? selected;
            List<WallWindow> targets = TargetWindows();
            if (shown == null)
            {
                summary.Text = "Pasa el mouse por una plantilla para ver cómo quedaría.";
                return;
            }
            int tabs = 0;
            foreach (WallWindow window in targets)
            {
                foreach (Cell cell in window.Panel.Cells)
                {
                    tabs += cell.Tabs.Count;
                }
            }
            string text = shown.Name + ": " + shown.CellCount + (shown.CellCount == 1 ? " celda" : " celdas") + " por monitor. ";
            text += tabs == 0 ? "No hay pestañas abiertas; las celdas quedan vacías para escribir una URL." :
                "Las " + tabs + " pestañas abiertas se reparten en orden, sin recargarse ni perder la sesión. Si sobran, quedan juntas en la última celda; si faltan, las celdas restantes quedan vacías.";
            if (hovered != null && selected != null && hovered != selected)
            {
                text += "\r\n\r\nSeleccionada: " + selected.Name + ".";
            }
            summary.Text = text;
        }

        private void OnPreviewPaint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(preview.BackColor);
            LayoutTemplate shown = hovered ?? selected;
            List<WallWindow> targets = TargetWindows();
            if (shown == null || targets.Count == 0)
            {
                TextRenderer.DrawText(g, "Vista previa", Font, preview.ClientRectangle, Color.Gray, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                return;
            }
            Screen screen = targets[0].Screen;
            double aspect = screen.Bounds.Height == 0 ? 0.5625 : screen.Bounds.Height / (double)screen.Bounds.Width;
            int width = preview.ClientSize.Width - 16;
            int height = (int)(width * aspect);
            if (height > preview.ClientSize.Height - 16)
            {
                height = preview.ClientSize.Height - 16;
                width = (int)(height / aspect);
            }
            Rectangle area = new Rectangle((preview.ClientSize.Width - width) / 2, (preview.ClientSize.Height - height) / 2, width, height);
            using (Pen frame = new Pen(Color.FromArgb(70, 70, 70), 2))
            {
                g.DrawRectangle(frame, Rectangle.Inflate(area, 3, 3));
            }
            List<string> labels = Wall.PreviewLabels(targets[0], shown.Shape);
            LayoutTemplates.Draw(g, area, shown.Shape, labels, Font, shown == selected);
        }

        private void ApplyAndClose()
        {
            if (selected == null)
            {
                return;
            }
            List<WallWindow> targets = TargetWindows();
            DialogResult = DialogResult.OK;
            Close();
            foreach (WallWindow window in targets)
            {
                Wall.ApplyTemplate(window, selected.Shape, selected.Name);
            }
        }
    }

    internal sealed class PreviewPanel : Panel
    {
        public PreviewPanel()
        {
            DoubleBuffered = true;
            ResizeRedraw = true;
        }
    }

    internal sealed class TemplateTile : Control
    {
        private bool hover;
        private bool selected;

        public TemplateTile(LayoutTemplate template)
        {
            Template = template;
            Size = new Size(150, 118);
            Margin = new Padding(6);
            Cursor = Cursors.Hand;
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            BackColor = Color.FromArgb(36, 36, 36);
        }

        public LayoutTemplate Template { get; private set; }

        public bool Selected
        {
            get { return selected; }
            set
            {
                selected = value;
                Invalidate();
            }
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            hover = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            hover = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(selected ? Color.FromArgb(0, 70, 130) : (hover ? Color.FromArgb(52, 52, 52) : BackColor));
            Rectangle thumb = new Rectangle(10, 8, Width - 20, (int)((Width - 20) * 0.5625));
            LayoutTemplates.Draw(g, thumb, Template.Shape, null, null, selected || hover);
            Rectangle text = new Rectangle(4, thumb.Bottom + 4, Width - 8, Height - thumb.Bottom - 6);
            TextRenderer.DrawText(g, Template.Name, Font, text, Color.Gainsboro, TextFormatFlags.HorizontalCenter | TextFormatFlags.Top | TextFormatFlags.WordBreak | TextFormatFlags.EndEllipsis);
        }
    }
}
