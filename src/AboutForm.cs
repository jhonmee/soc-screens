// MuroSOC - AboutForm
using System;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace MuroSoc
{
    internal sealed class AboutForm : Form
    {
        public AboutForm()
        {
            Text = "Acerca de Muro SOC";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            AutoScaleMode = AutoScaleMode.Dpi;
            Font = SystemFonts.MessageBoxFont;
            ClientSize = new Size(520, 270);

            string edge = RuntimeInfo.GetEdgeVersion();
            string[,] rows = new string[,]
            {
                { "Muro SOC", RuntimeInfo.AppVersion },
                { "WebView2 Runtime", App.RuntimeVersion + " (Evergreen)" },
                { "Microsoft Edge", edge ?? "No detectado" },
                { "SDK WebView2", RuntimeInfo.SdkVersion },
                { "Carpeta de datos", AppPaths.Root },
                { "Perfiles", string.Join(", ", App.Config.Profiles.ToArray()) }
            };

            TableLayoutPanel table = new TableLayoutPanel();
            table.SetBounds(16, 16, 488, 190);
            table.ColumnCount = 2;
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            StringBuilder copy = new StringBuilder();
            for (int i = 0; i < rows.GetLength(0); i++)
            {
                Label name = new Label();
                name.Text = rows[i, 0];
                name.AutoSize = true;
                name.Font = new Font(Font, FontStyle.Bold);
                name.Margin = new Padding(0, 4, 8, 4);
                TextBox value = new TextBox();
                value.Text = rows[i, 1];
                value.ReadOnly = true;
                value.BorderStyle = BorderStyle.None;
                value.BackColor = SystemColors.Control;
                value.Dock = DockStyle.Fill;
                value.Margin = new Padding(0, 4, 0, 4);
                table.Controls.Add(name, 0, i);
                table.Controls.Add(value, 1, i);
                copy.AppendLine(rows[i, 0] + ": " + rows[i, 1]);
            }

            Button copyButton = new Button();
            copyButton.Text = "Copiar";
            copyButton.SetBounds(332, 226, 80, 28);
            string copyText = copy.ToString();
            copyButton.Click += delegate
            {
                try
                {
                    Clipboard.SetText(copyText);
                }
                catch (Exception)
                {
                }
            };

            Button close = new Button();
            close.Text = "Cerrar";
            close.DialogResult = DialogResult.OK;
            close.SetBounds(424, 226, 80, 28);

            AcceptButton = close;
            CancelButton = close;
            Controls.Add(table);
            Controls.Add(copyButton);
            Controls.Add(close);
        }
    }
}
