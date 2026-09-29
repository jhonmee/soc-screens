// MuroSOC - EditBar
using System;
using System.Drawing;
using System.Windows.Forms;

namespace MuroSoc
{
    internal sealed class EditBar : FlowLayoutPanel
    {
        private readonly Cell cell;
        private readonly Label info;

        public EditBar(Cell cell)
        {
            this.cell = cell;
            Dock = DockStyle.Top;
            BackColor = Color.FromArgb(0, 60, 110);
            WrapContents = false;
            AutoScroll = false;
            Visible = false;

            AddButton("Columnas ⇆", "Dividir en dos columnas", delegate { Wall.Split(cell, PaneModel.Columns); });
            AddButton("Filas ⇅", "Dividir en dos filas", delegate { Wall.Split(cell, PaneModel.Rows); });
            AddButton("Fusionar", "Unir con la celda vecina", delegate { Wall.Merge(cell); });
            AddButton("URL...", "Abrir una URL en esta celda", delegate { Wall.PromptUrl(cell); });
            AddButton("Perfil", "Perfil de navegador de la celda", delegate(object sender, EventArgs e)
            {
                Control button = (Control)sender;
                MenuEntry.ShowAt(button, new Point(0, button.Height), Wall.BuildProfileMenu(cell));
            });
            AddButton("Terminar", "Salir del modo edición", delegate { Wall.SetEditMode(false); });

            info = new Label();
            info.AutoSize = true;
            info.ForeColor = Color.White;
            info.Margin = new Padding(8, 6, 0, 0);
            Controls.Add(info);

            ApplyScale();
            HandleCreated += delegate { ApplyScale(); };
        }

        public void RefreshInfo()
        {
            int count = cell.Tabs.Count;
            info.Text = "Perfil: " + cell.ProfileName + " · " + count + (count == 1 ? " pestaña" : " pestañas");
        }

        private void AddButton(string text, string tooltip, EventHandler click)
        {
            Button button = new Button();
            button.Text = text;
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderColor = Color.FromArgb(80, 140, 200);
            button.ForeColor = Color.White;
            button.AutoSize = true;
            button.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            button.Margin = new Padding(2);
            button.Click += click;
            button.Tag = tooltip;
            Controls.Add(button);
        }

        private void ApplyScale()
        {
            Font = new Font("Segoe UI", Dpi.ScaleF(this, 12f), FontStyle.Regular, GraphicsUnit.Pixel);
            Height = Dpi.Scale(this, 32);
        }
    }
}
