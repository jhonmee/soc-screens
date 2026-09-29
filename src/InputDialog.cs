// MuroSOC - InputDialog
using System;
using System.Drawing;
using System.Windows.Forms;

namespace MuroSoc
{
    internal sealed class InputDialog : Form
    {
        private readonly TextBox input;

        private InputDialog(string title, string label, string initial, bool multiline)
        {
            Text = title;
            FormBorderStyle = multiline ? FormBorderStyle.Sizable : FormBorderStyle.FixedDialog;
            MaximizeBox = multiline;
            MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            AutoScaleMode = AutoScaleMode.Dpi;
            Font = SystemFonts.MessageBoxFont;
            ClientSize = multiline ? new Size(620, 380) : new Size(520, 120);

            Label caption = new Label();
            caption.Text = label;
            caption.AutoSize = true;
            caption.Location = new Point(12, 12);

            input = new TextBox();
            input.Text = initial ?? string.Empty;
            input.Multiline = multiline;
            input.AcceptsReturn = multiline;
            input.AcceptsTab = multiline;
            input.ScrollBars = multiline ? ScrollBars.Both : ScrollBars.None;
            input.WordWrap = false;
            if (multiline)
            {
                input.Font = new Font(FontFamily.GenericMonospace, 10f);
                input.SetBounds(12, 36, ClientSize.Width - 24, ClientSize.Height - 88);
                input.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            }
            else
            {
                input.SetBounds(12, 36, ClientSize.Width - 24, 24);
                input.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            }

            Button ok = new Button();
            ok.Text = "Aceptar";
            ok.DialogResult = DialogResult.OK;
            ok.SetBounds(ClientSize.Width - 184, ClientSize.Height - 40, 80, 28);
            ok.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;

            Button cancel = new Button();
            cancel.Text = "Cancelar";
            cancel.DialogResult = DialogResult.Cancel;
            cancel.SetBounds(ClientSize.Width - 92, ClientSize.Height - 40, 80, 28);
            cancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;

            if (!multiline)
            {
                AcceptButton = ok;
            }
            CancelButton = cancel;
            Controls.Add(caption);
            Controls.Add(input);
            Controls.Add(ok);
            Controls.Add(cancel);
            Shown += delegate
            {
                input.Focus();
                if (!multiline)
                {
                    input.SelectAll();
                }
            };
        }

        public static string Prompt(IWin32Window owner, string title, string label, string initial, bool multiline)
        {
            using (InputDialog dialog = new InputDialog(title, label, initial, multiline))
            {
                return dialog.ShowDialog(owner) == DialogResult.OK ? dialog.input.Text : null;
            }
        }

        public static int? PromptNumber(IWin32Window owner, string title, string label, int initial, int min, int max)
        {
            string text = Prompt(owner, title, label, initial.ToString(), false);
            if (text == null)
            {
                return null;
            }
            int value;
            if (!int.TryParse(text.Trim(), out value) || value < min || value > max)
            {
                MessageBox.Show(owner, "Escribe un número entre " + min + " y " + max + ".", title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return null;
            }
            return value;
        }
    }
}
