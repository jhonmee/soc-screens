// MuroSOC - HoverBar
using System;
using System.Drawing;
using System.Windows.Forms;

namespace MuroSoc
{
    internal sealed class HoverBar : FlowLayoutPanel
    {
        private readonly Label countdown;
        private readonly ToolTip tip;

        public HoverBar(Cell cell)
        {
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            WrapContents = false;
            BackColor = Color.FromArgb(34, 34, 34);
            Padding = new Padding(2);
            Visible = false;
            tip = new ToolTip();

            countdown = new Label();
            countdown.AutoSize = true;
            countdown.ForeColor = Color.FromArgb(80, 200, 120);
            countdown.Margin = new Padding(6, 6, 4, 0);
            Controls.Add(countdown);

            AddButton("Interfaz", "Mostrar interfaz (" + Shortcuts.Display(Shortcuts.ToggleUi) + ")", delegate { Wall.SetUiVisible(true); });
            AddButton("⛶", "Pantalla completa (" + Shortcuts.Display(Shortcuts.ToggleFullscreen) + ")", delegate { Wall.ToggleFullscreen(); });
            AddButton("☰", "Menú de la celda", delegate(object sender, EventArgs e)
            {
                Control button = (Control)sender;
                MenuEntry.ShowAt(button, new Point(0, 0), Wall.BuildCellMenu(cell, cell.ActiveTab));
            });
            ApplyScale();
        }

        public bool ContainsCursor
        {
            get { return Visible && RectangleToScreen(ClientRectangle).Contains(Cursor.Position); }
        }

        public void SetCountdown(string text)
        {
            string value = text ?? string.Empty;
            if (countdown.Text != value)
            {
                countdown.Text = value;
            }
            bool show = value.Length > 0;
            if (countdown.Visible != show)
            {
                countdown.Visible = show;
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

        private void AddButton(string text, string tooltip, EventHandler click)
        {
            Button button = new Button();
            button.Text = text;
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.ForeColor = Color.Gainsboro;
            button.BackColor = Color.FromArgb(50, 50, 52);
            button.AutoSize = true;
            button.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            button.Margin = new Padding(2);
            button.TabStop = false;
            button.Click += click;
            tip.SetToolTip(button, tooltip);
            Controls.Add(button);
        }

        private void ApplyScale()
        {
            Font = new Font("Segoe UI", Dpi.ScaleF(this, 12f), FontStyle.Regular, GraphicsUnit.Pixel);
        }
    }
}
