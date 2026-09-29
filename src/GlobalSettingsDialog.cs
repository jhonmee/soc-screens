// MuroSOC - GlobalSettingsDialog
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace MuroSoc
{
    internal sealed class GlobalSettingsDialog : Form
    {
        private static readonly int[] RefreshOptions = new int[] { 0, 30, 60, 120, 300, 600, 900, 1800 };
        private static readonly int[] ZoomOptions = new int[] { 50, 67, 75, 80, 90, 100, 110, 125, 150 };

        private readonly Cell cell;
        private readonly RadioButton scopeWall = new RadioButton();
        private readonly RadioButton scopeMonitor = new RadioButton();
        private readonly RadioButton scopeCell = new RadioButton();
        private readonly CheckBox changeRefresh = new CheckBox();
        private readonly ComboBox refresh = new ComboBox();
        private readonly CheckBox changeScrollbars = new CheckBox();
        private readonly ComboBox scrollbars = new ComboBox();
        private readonly CheckBox changeZoom = new CheckBox();
        private readonly ComboBox zoom = new ComboBox();
        private readonly CheckBox forNewTabs = new CheckBox();
        private readonly Label count = new Label();

        public GlobalSettingsDialog(Cell cell)
        {
            this.cell = cell;
            Text = "Ajustes para todas las pestañas";
            StartPosition = FormStartPosition.CenterParent;
            ShowInTaskbar = false;
            MinimizeBox = false;
            MaximizeBox = false;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            AutoScaleMode = AutoScaleMode.Dpi;
            Font = SystemFonts.MessageBoxFont;
            ClientSize = new Size(520, 360);

            Label intro = new Label();
            intro.Text = "Marca lo que quieres cambiar. Se aplica a todas las pestañas del alcance elegido; después puedes ajustar cada pestaña por separado desde su menú.";
            intro.SetBounds(16, 12, 488, 36);

            GroupBox scope = new GroupBox();
            scope.Text = "Alcance";
            scope.SetBounds(16, 54, 488, 52);
            scopeWall.Text = "Todo el muro";
            scopeWall.SetBounds(12, 20, 130, 22);
            scopeWall.Checked = true;
            scopeMonitor.Text = "Este monitor";
            scopeMonitor.SetBounds(150, 20, 130, 22);
            scopeCell.Text = "Esta celda";
            scopeCell.SetBounds(290, 20, 130, 22);
            scopeCell.Enabled = cell != null;
            scopeMonitor.Enabled = cell != null && cell.Window != null;
            scope.Controls.Add(scopeWall);
            scope.Controls.Add(scopeMonitor);
            scope.Controls.Add(scopeCell);
            EventHandler updateCount = delegate { UpdateCount(); };
            scopeWall.CheckedChanged += updateCount;
            scopeMonitor.CheckedChanged += updateCount;
            scopeCell.CheckedChanged += updateCount;

            changeRefresh.Text = "Autorefresh:";
            changeRefresh.SetBounds(16, 120, 180, 24);
            refresh.DropDownStyle = ComboBoxStyle.DropDownList;
            refresh.SetBounds(200, 120, 200, 24);
            foreach (int seconds in RefreshOptions)
            {
                refresh.Items.Add(seconds == 0 ? "Apagado" : "Cada " + BrowserTab.FormatInterval(seconds));
            }
            refresh.SelectedIndex = Math.Max(0, Array.IndexOf(RefreshOptions, App.Config.NewTabAutoRefreshSeconds));
            refresh.SelectedIndexChanged += delegate { changeRefresh.Checked = true; };

            changeScrollbars.Text = "Scrollbars:";
            changeScrollbars.SetBounds(16, 154, 180, 24);
            scrollbars.DropDownStyle = ComboBoxStyle.DropDownList;
            scrollbars.SetBounds(200, 154, 200, 24);
            scrollbars.Items.Add("Mostrar");
            scrollbars.Items.Add("Ocultar");
            scrollbars.SelectedIndex = App.Config.NewTabHideScrollbars ? 1 : 0;
            scrollbars.SelectedIndexChanged += delegate { changeScrollbars.Checked = true; };

            changeZoom.Text = "Zoom:";
            changeZoom.SetBounds(16, 188, 180, 24);
            zoom.DropDownStyle = ComboBoxStyle.DropDownList;
            zoom.SetBounds(200, 188, 200, 24);
            foreach (int option in ZoomOptions)
            {
                zoom.Items.Add(option + " %");
            }
            int zoomIndex = Array.IndexOf(ZoomOptions, (int)Math.Round(App.Config.NewTabZoom * 100));
            zoom.SelectedIndex = zoomIndex >= 0 ? zoomIndex : Array.IndexOf(ZoomOptions, 100);
            zoom.SelectedIndexChanged += delegate { changeZoom.Checked = true; };

            forNewTabs.Text = "Usar también estos valores para las pestañas nuevas";
            forNewTabs.SetBounds(16, 230, 488, 24);

            count.SetBounds(16, 262, 488, 40);
            count.ForeColor = SystemColors.GrayText;

            Button apply = new Button();
            apply.Text = "Aplicar";
            apply.SetBounds(332, 316, 84, 30);
            apply.Click += delegate
            {
                if (Apply())
                {
                    DialogResult = DialogResult.OK;
                    Close();
                }
            };

            Button cancel = new Button();
            cancel.Text = "Cancelar";
            cancel.DialogResult = DialogResult.Cancel;
            cancel.SetBounds(420, 316, 84, 30);

            AcceptButton = apply;
            CancelButton = cancel;
            Controls.Add(intro);
            Controls.Add(scope);
            Controls.Add(changeRefresh);
            Controls.Add(refresh);
            Controls.Add(changeScrollbars);
            Controls.Add(scrollbars);
            Controls.Add(changeZoom);
            Controls.Add(zoom);
            Controls.Add(forNewTabs);
            Controls.Add(count);
            Controls.Add(apply);
            Controls.Add(cancel);
            UpdateCount();
        }

        private List<BrowserTab> TargetTabs()
        {
            List<BrowserTab> tabs = new List<BrowserTab>();
            if (scopeCell.Checked && cell != null)
            {
                tabs.AddRange(cell.Tabs);
            }
            else if (scopeMonitor.Checked && cell != null && cell.Window != null)
            {
                foreach (Cell item in cell.Window.Panel.Cells)
                {
                    tabs.AddRange(item.Tabs);
                }
            }
            else
            {
                tabs.AddRange(Wall.AllTabs());
            }
            return tabs;
        }

        private void UpdateCount()
        {
            int total = TargetTabs().Count;
            count.Text = total == 1 ? "Se aplicará a 1 pestaña." : "Se aplicará a " + total + " pestañas.";
        }

        private bool Apply()
        {
            if (!changeRefresh.Checked && !changeScrollbars.Checked && !changeZoom.Checked)
            {
                MessageBox.Show(this, "Marca al menos un ajuste para cambiar.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }
            int seconds = RefreshOptions[Math.Max(0, refresh.SelectedIndex)];
            bool hide = scrollbars.SelectedIndex == 1;
            double zoomValue = ZoomOptions[Math.Max(0, zoom.SelectedIndex)] / 100.0;
            List<BrowserTab> tabs = TargetTabs();
            foreach (BrowserTab tab in tabs)
            {
                if (tab.IsClosed)
                {
                    continue;
                }
                if (changeRefresh.Checked)
                {
                    tab.SetAutoRefresh(seconds);
                }
                if (changeZoom.Checked)
                {
                    tab.SetZoom(zoomValue);
                }
                if (changeScrollbars.Checked && tab.Settings.HideScrollbars != hide)
                {
                    tab.Settings.HideScrollbars = hide;
                    tab.UpdatePageSettings();
                }
            }
            if (forNewTabs.Checked)
            {
                if (changeRefresh.Checked)
                {
                    App.Config.NewTabAutoRefreshSeconds = seconds;
                }
                if (changeScrollbars.Checked)
                {
                    App.Config.NewTabHideScrollbars = hide;
                }
                if (changeZoom.Checked)
                {
                    App.Config.NewTabZoom = zoomValue;
                }
                App.SaveConfig();
            }
            Wall.MarkDirty();
            Log.Info("Ajustes globales aplicados a " + tabs.Count + " pestañas");
            return true;
        }
    }
}
