// MuroSOC - EmptyCellPanel
using System;
using System.Drawing;
using System.Windows.Forms;

namespace MuroSoc
{
    internal sealed class EmptyCellPanel : Panel
    {
        private readonly Cell cell;
        private readonly Label title;
        private readonly TextBox input;
        private readonly Button open;
        private readonly Label hint;

        public EmptyCellPanel(Cell cell)
        {
            this.cell = cell;
            BackColor = Color.FromArgb(24, 24, 24);

            title = new Label();
            title.Text = "Celda vacía";
            title.ForeColor = Color.Gainsboro;
            title.AutoSize = true;

            input = new TextBox();
            input.KeyDown += OnInputKeyDown;

            open = new Button();
            open.Text = "Abrir";
            open.FlatStyle = FlatStyle.Flat;
            open.ForeColor = Color.White;
            open.BackColor = Cell.AccentColor;
            open.Click += delegate { OpenInput(); };

            hint = new Label();
            hint.Text = "Escribe una URL o arrastra aquí una pestaña de otra celda.";
            hint.ForeColor = Color.Gray;
            hint.AutoSize = true;

            Controls.Add(title);
            Controls.Add(input);
            Controls.Add(open);
            Controls.Add(hint);
            Resize += delegate { ArrangeControls(); };
            HandleCreated += delegate { ArrangeControls(); };
        }

        private void ArrangeControls()
        {
            title.Font = new Font("Segoe UI", Dpi.ScaleF(this, 20f), FontStyle.Regular, GraphicsUnit.Pixel);
            input.Font = new Font("Segoe UI", Dpi.ScaleF(this, 14f), FontStyle.Regular, GraphicsUnit.Pixel);
            open.Font = input.Font;
            hint.Font = new Font("Segoe UI", Dpi.ScaleF(this, 12f), FontStyle.Regular, GraphicsUnit.Pixel);
            int width = Math.Min(ClientSize.Width - Dpi.Scale(this, 32), Dpi.Scale(this, 520));
            int buttonWidth = Dpi.Scale(this, 80);
            int height = Dpi.Scale(this, 28);
            int left = (ClientSize.Width - width) / 2;
            int top = ClientSize.Height / 2 - height;
            title.Location = new Point(left, top - title.PreferredHeight - Dpi.Scale(this, 8));
            input.SetBounds(left, top, Math.Max(40, width - buttonWidth - Dpi.Scale(this, 6)), height);
            open.SetBounds(left + width - buttonWidth, top, buttonWidth, input.Height);
            hint.Location = new Point(left, top + input.Height + Dpi.Scale(this, 8));
        }

        private void OnInputKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                OpenInput();
            }
        }

        private async void OpenInput()
        {
            string url = UrlInput.Normalize(input.Text);
            if (url == null)
            {
                input.SelectAll();
                input.Focus();
                return;
            }
            input.Text = string.Empty;
            TabModel model = App.Config.NewTabModel(url, cell.ProfileName);
            try
            {
                await cell.CreateTabAsync(model, true);
            }
            catch (Exception ex)
            {
                Log.Error("No se pudo abrir la pestaña", ex);
            }
        }
    }
}
