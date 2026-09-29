// MuroSOC - MissingRuntimeForm
using System;
using System.Drawing;
using System.Windows.Forms;

namespace MuroSoc
{
    internal sealed class MissingRuntimeForm : Form
    {
        public MissingRuntimeForm()
        {
            Text = "Muro SOC";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            AutoScaleMode = AutoScaleMode.Dpi;
            ClientSize = new Size(460, 170);
            Font = SystemFonts.MessageBoxFont;

            Label message = new Label();
            message.Text = "No se encontró Microsoft Edge WebView2 Runtime en este equipo.\r\n\r\n" +
                "Normalmente viene con Windows 10 y 11 y se actualiza junto con Edge. " +
                "Pide a TI que lo instale desde la página oficial de Microsoft:";
            message.SetBounds(16, 16, 428, 80);

            LinkLabel link = new LinkLabel();
            link.Text = RuntimeInfo.RuntimeDownloadUrl;
            link.SetBounds(16, 96, 428, 22);
            link.LinkClicked += delegate { ExternalBrowser.OpenDefault(RuntimeInfo.RuntimeDownloadUrl); };

            Button close = new Button();
            close.Text = "Cerrar";
            close.DialogResult = DialogResult.OK;
            close.SetBounds(364, 130, 80, 28);
            close.Click += delegate { Close(); };

            AcceptButton = close;
            Controls.Add(message);
            Controls.Add(link);
            Controls.Add(close);
        }
    }
}
