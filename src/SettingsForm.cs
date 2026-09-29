// MuroSOC - SettingsForm
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace MuroSoc
{
    internal sealed class SettingsForm : Form
    {
        private static readonly string[] PermissionNames = new string[]
        {
            "camera", "microphone", "geolocation", "notifications", "midi", "clipboard", "sensors", "downloads", "files", "autoplay", "fonts", "window-management", "storage", "*"
        };

        private readonly TextBox homeUrl = new TextBox();
        private readonly CheckBox fullscreen = new CheckBox();
        private readonly CheckBox popupsAsTabs = new CheckBox();
        private readonly CheckBox devTools = new CheckBox();
        private readonly CheckBox downloads = new CheckBox();
        private readonly NumericUpDown cursorSeconds = new NumericUpDown();
        private readonly CheckBox autostart = new CheckBox();

        private readonly CheckBox clockEnabled = new CheckBox();
        private readonly CheckBox clockAllMonitors = new CheckBox();
        private readonly ComboBox clockZone = new ComboBox();
        private readonly ComboBox clockCorner = new ComboBox();
        private readonly CheckBox clockSeconds = new CheckBox();
        private readonly CheckBox clockShowZone = new CheckBox();
        private readonly NumericUpDown clockSize = new NumericUpDown();

        private readonly CheckBox allowlist = new CheckBox();
        private readonly TextBox allowedDomains = new TextBox();
        private readonly TextBox loginPatterns = new TextBox();
        private readonly DataGridView permissions = new DataGridView();

        private readonly ListBox profiles = new ListBox();
        private readonly DataGridView shortcuts = new DataGridView();

        private readonly List<TimeZoneInfo> zones = new List<TimeZoneInfo>();

        public SettingsForm()
        {
            Text = "Configuración - Muro SOC";
            StartPosition = FormStartPosition.CenterParent;
            ShowInTaskbar = false;
            MinimizeBox = false;
            AutoScaleMode = AutoScaleMode.Dpi;
            Font = SystemFonts.MessageBoxFont;
            ClientSize = new Size(720, 540);
            MinimumSize = new Size(620, 480);

            TabControl tabs = new TabControl();
            tabs.Dock = DockStyle.Fill;
            tabs.TabPages.Add(BuildGeneralPage());
            tabs.TabPages.Add(BuildClockPage());
            tabs.TabPages.Add(BuildSecurityPage());
            tabs.TabPages.Add(BuildProfilesPage());
            tabs.TabPages.Add(BuildShortcutsPage());

            FlowLayoutPanel buttons = new FlowLayoutPanel();
            buttons.Dock = DockStyle.Bottom;
            buttons.FlowDirection = FlowDirection.RightToLeft;
            buttons.Height = 44;
            buttons.Padding = new Padding(8);

            Button cancel = new Button();
            cancel.Text = "Cancelar";
            cancel.DialogResult = DialogResult.Cancel;
            cancel.AutoSize = true;

            Button save = new Button();
            save.Text = "Guardar";
            save.AutoSize = true;
            save.Click += delegate
            {
                if (Save())
                {
                    DialogResult = DialogResult.OK;
                    Close();
                }
            };

            buttons.Controls.Add(cancel);
            buttons.Controls.Add(save);
            CancelButton = cancel;

            Controls.Add(tabs);
            Controls.Add(buttons);
            LoadValues();
        }

        private TabPage BuildGeneralPage()
        {
            TableLayoutPanel table = NewTable();
            homeUrl.Dock = DockStyle.Fill;
            AddRow(table, "Página inicial", homeUrl);
            AddCheck(table, fullscreen, "Pantalla completa sin bordes en cada monitor");
            AddCheck(table, popupsAsTabs, "Abrir los popups como pestaña en vez de ventana flotante");
            AddCheck(table, devTools, "Permitir herramientas de desarrollo (F12)");
            AddCheck(table, downloads, "Permitir descargas (van a %LOCALAPPDATA%\\MuroSOC\\downloads)");
            cursorSeconds.Minimum = 0;
            cursorSeconds.Maximum = 3600;
            cursorSeconds.Width = 80;
            AddRow(table, "Ocultar cursor tras (s, 0 = nunca)", cursorSeconds);
            AddCheck(table, autostart, "Abrir Muro SOC al iniciar sesión en Windows");
            return NewPage("General", table);
        }

        private TabPage BuildClockPage()
        {
            TableLayoutPanel table = NewTable();
            AddCheck(table, clockEnabled, "Mostrar reloj");
            AddCheck(table, clockAllMonitors, "En todos los monitores (si no, solo en el principal)");
            clockZone.DropDownStyle = ComboBoxStyle.DropDownList;
            clockZone.Dock = DockStyle.Fill;
            clockZone.Items.Add("Hora local del equipo");
            foreach (TimeZoneInfo zone in TimeZoneInfo.GetSystemTimeZones())
            {
                zones.Add(zone);
                clockZone.Items.Add(zone.DisplayName);
            }
            AddRow(table, "Zona horaria", clockZone);
            clockCorner.DropDownStyle = ComboBoxStyle.DropDownList;
            clockCorner.Items.AddRange(new object[] { "Arriba a la derecha", "Arriba a la izquierda", "Abajo a la derecha", "Abajo a la izquierda" });
            AddRow(table, "Esquina", clockCorner);
            AddCheck(table, clockSeconds, "Mostrar segundos");
            AddCheck(table, clockShowZone, "Mostrar desfase UTC");
            clockSize.Minimum = 10;
            clockSize.Maximum = 96;
            clockSize.Width = 80;
            AddRow(table, "Tamaño (px)", clockSize);
            return NewPage("Reloj", table);
        }

        private TabPage BuildSecurityPage()
        {
            TableLayoutPanel table = NewTable();
            AddCheck(table, allowlist, "Usar lista de dominios permitidos (los de login de Microsoft siempre se permiten)");
            SetupMultiline(allowedDomains, 80);
            AddRow(table, "Dominios permitidos (uno por línea)", allowedDomains);
            SetupMultiline(loginPatterns, 80);
            AddRow(table, "Páginas de login (dominio o dominio/ruta)", loginPatterns);

            permissions.Dock = DockStyle.Fill;
            permissions.Height = 150;
            permissions.AllowUserToAddRows = true;
            permissions.AllowUserToDeleteRows = true;
            permissions.RowHeadersWidth = 24;
            permissions.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            DataGridViewTextBoxColumn domain = new DataGridViewTextBoxColumn();
            domain.HeaderText = "Dominio";
            domain.FillWeight = 50;
            DataGridViewComboBoxColumn permission = new DataGridViewComboBoxColumn();
            permission.HeaderText = "Permiso";
            permission.Items.AddRange(PermissionNames);
            permission.FillWeight = 35;
            DataGridViewCheckBoxColumn allow = new DataGridViewCheckBoxColumn();
            allow.HeaderText = "Permitir";
            allow.FillWeight = 15;
            permissions.Columns.Add(domain);
            permissions.Columns.Add(permission);
            permissions.Columns.Add(allow);
            AddRow(table, "Excepciones de permisos", permissions);
            return NewPage("Seguridad", table);
        }

        private TabPage BuildProfilesPage()
        {
            TableLayoutPanel table = NewTable();
            profiles.Height = 160;
            profiles.Dock = DockStyle.Fill;
            AddRow(table, "Perfiles", profiles);

            FlowLayoutPanel actions = new FlowLayoutPanel();
            actions.AutoSize = true;
            actions.Dock = DockStyle.Fill;
            Button add = new Button();
            add.Text = "Agregar...";
            add.AutoSize = true;
            add.Click += delegate { AddProfile(); };
            Button remove = new Button();
            remove.Text = "Quitar";
            remove.AutoSize = true;
            remove.Click += delegate { RemoveProfile(); };
            Button signOut = new Button();
            signOut.Text = "Cerrar sesión en todo...";
            signOut.AutoSize = true;
            signOut.Click += delegate
            {
                string selected = profiles.SelectedItem as string;
                if (selected != null)
                {
                    App.SignOutProfile(this, selected);
                }
            };
            actions.Controls.Add(add);
            actions.Controls.Add(remove);
            actions.Controls.Add(signOut);
            AddRow(table, string.Empty, actions);

            Label help = new Label();
            help.AutoSize = true;
            help.MaximumSize = new Size(640, 0);
            help.Text = "Cada perfil guarda sus propias cookies y sesiones. Sirve para estar logueado en tenants distintos a la vez. " +
                "El perfil de cada celda se elige en modo edición o en el menú de la celda. Quitar un perfil de la lista no borra sus datos: usa antes \"Cerrar sesión en todo\".";
            AddRow(table, string.Empty, help);
            return NewPage("Perfiles", table);
        }

        private TabPage BuildShortcutsPage()
        {
            TableLayoutPanel table = NewTable();
            shortcuts.Dock = DockStyle.Fill;
            shortcuts.Height = 330;
            shortcuts.AllowUserToAddRows = false;
            shortcuts.AllowUserToDeleteRows = false;
            shortcuts.RowHeadersVisible = false;
            shortcuts.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            DataGridViewTextBoxColumn action = new DataGridViewTextBoxColumn();
            action.HeaderText = "Acción";
            action.ReadOnly = true;
            action.FillWeight = 60;
            DataGridViewTextBoxColumn key = new DataGridViewTextBoxColumn();
            key.HeaderText = "Tecla (ej. F10, Ctrl+Shift+L; vacío = sin atajo)";
            key.FillWeight = 40;
            shortcuts.Columns.Add(action);
            shortcuts.Columns.Add(key);
            AddRow(table, "Atajos", shortcuts);
            Label help = new Label();
            help.AutoSize = true;
            help.MaximumSize = new Size(640, 0);
            help.Text = "Los atajos solo funcionan dentro de Muro SOC. Fijos: Esc (restaurar celda o salir de edición) y Ctrl+1…9 (cambiar de layout).";
            AddRow(table, string.Empty, help);
            return NewPage("Atajos", table);
        }

        private void LoadValues()
        {
            AppConfig config = App.Config;
            homeUrl.Text = config.HomeUrl;
            fullscreen.Checked = config.Fullscreen;
            popupsAsTabs.Checked = config.PopupsAsTabs;
            devTools.Checked = config.DevToolsEnabled;
            downloads.Checked = config.DownloadsEnabled;
            cursorSeconds.Value = Math.Max(0, Math.Min(3600, config.CursorHideSeconds));
            autostart.Checked = StartupShortcut.IsEnabled();

            clockEnabled.Checked = config.ClockEnabled;
            clockAllMonitors.Checked = config.ClockAllMonitors;
            clockZone.SelectedIndex = 0;
            for (int i = 0; i < zones.Count; i++)
            {
                if (zones[i].Id == config.ClockTimeZone)
                {
                    clockZone.SelectedIndex = i + 1;
                }
            }
            string[] corners = new string[] { "topright", "topleft", "bottomright", "bottomleft" };
            clockCorner.SelectedIndex = Math.Max(0, Array.IndexOf(corners, (config.ClockCorner ?? string.Empty).ToLowerInvariant()));
            clockSeconds.Checked = config.ClockShowSeconds;
            clockShowZone.Checked = config.ClockShowZone;
            clockSize.Value = Math.Max(10, Math.Min(96, config.ClockFontSize));

            allowlist.Checked = config.AllowlistEnabled;
            allowedDomains.Text = string.Join("\r\n", config.AllowedDomains.ToArray());
            loginPatterns.Text = string.Join("\r\n", config.LoginUrlPatterns.ToArray());
            foreach (PermissionRule rule in config.PermissionRules)
            {
                string permission = Array.IndexOf(PermissionNames, rule.Permission) >= 0 ? rule.Permission : "*";
                permissions.Rows.Add(rule.Domain, permission, rule.Allow);
            }

            foreach (string profile in config.Profiles)
            {
                profiles.Items.Add(profile);
            }

            foreach (KeyValuePair<string, string> pair in config.KeyBindings)
            {
                int row = shortcuts.Rows.Add(Shortcuts.Describe(pair.Key), pair.Value);
                shortcuts.Rows[row].Tag = pair.Key;
            }
        }

        private bool Save()
        {
            string home = UrlInput.Normalize(homeUrl.Text);
            if (home == null)
            {
                MessageBox.Show(this, "La página inicial no es una URL válida.", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            Dictionary<string, string> bindings = new Dictionary<string, string>();
            Dictionary<Keys, string> used = new Dictionary<Keys, string>();
            foreach (DataGridViewRow row in shortcuts.Rows)
            {
                string action = row.Tag as string;
                string value = Convert.ToString(row.Cells[1].Value).Trim();
                if (action == null)
                {
                    continue;
                }
                if (value.Length > 0)
                {
                    Keys keys;
                    if (!Shortcuts.TryParse(value, out keys))
                    {
                        MessageBox.Show(this, "El atajo \"" + value + "\" no es válido.", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return false;
                    }
                    if (used.ContainsKey(keys))
                    {
                        MessageBox.Show(this, "El atajo " + Shortcuts.Format(keys) + " está repetido.", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return false;
                    }
                    used[keys] = action;
                    value = Shortcuts.Format(keys);
                }
                bindings[action] = value;
            }
            string lockKey;
            if (!bindings.TryGetValue(Shortcuts.LockWall, out lockKey) || string.IsNullOrEmpty(lockKey))
            {
                MessageBox.Show(this, "El atajo de bloqueo no puede quedar vacío: es la única forma de desbloquear el muro.", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            List<PermissionRule> rules = new List<PermissionRule>();
            foreach (DataGridViewRow row in permissions.Rows)
            {
                if (row.IsNewRow)
                {
                    continue;
                }
                string domain = Convert.ToString(row.Cells[0].Value).Trim();
                string permission = Convert.ToString(row.Cells[1].Value).Trim();
                if (domain.Length == 0 || permission.Length == 0)
                {
                    continue;
                }
                PermissionRule rule = new PermissionRule();
                rule.Domain = domain;
                rule.Permission = permission;
                rule.Allow = row.Cells[2].Value is bool && (bool)row.Cells[2].Value;
                rules.Add(rule);
            }

            try
            {
                if (autostart.Checked != StartupShortcut.IsEnabled())
                {
                    StartupShortcut.SetEnabled(autostart.Checked);
                }
            }
            catch (Exception ex)
            {
                Log.Error("No se pudo cambiar el inicio automático", ex);
                MessageBox.Show(this, "No se pudo cambiar el inicio automático.\r\n\r\n" + ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            AppConfig config = App.Config;
            config.HomeUrl = home;
            config.Fullscreen = fullscreen.Checked;
            config.PopupsAsTabs = popupsAsTabs.Checked;
            config.DevToolsEnabled = devTools.Checked;
            config.DownloadsEnabled = downloads.Checked;
            config.CursorHideSeconds = (int)cursorSeconds.Value;
            config.ClockEnabled = clockEnabled.Checked;
            config.ClockAllMonitors = clockAllMonitors.Checked;
            config.ClockTimeZone = clockZone.SelectedIndex > 0 ? zones[clockZone.SelectedIndex - 1].Id : string.Empty;
            config.ClockCorner = new string[] { "TopRight", "TopLeft", "BottomRight", "BottomLeft" }[Math.Max(0, clockCorner.SelectedIndex)];
            config.ClockShowSeconds = clockSeconds.Checked;
            config.ClockShowZone = clockShowZone.Checked;
            config.ClockFontSize = (int)clockSize.Value;
            config.AllowlistEnabled = allowlist.Checked;
            config.AllowedDomains = Lines(allowedDomains.Text);
            config.LoginUrlPatterns = Lines(loginPatterns.Text);
            config.PermissionRules = rules;
            List<string> profileNames = new List<string>();
            foreach (object item in profiles.Items)
            {
                profileNames.Add((string)item);
            }
            config.Profiles = profileNames;
            config.KeyBindings = bindings;
            config.Normalize();
            App.SaveConfig();
            Log.Info("Configuración guardada desde la ventana de configuración");
            return true;
        }

        private void AddProfile()
        {
            string name = InputDialog.Prompt(this, "Agregar perfil", "Nombre del perfil (letras, números, - y _):", string.Empty, false);
            if (name == null)
            {
                return;
            }
            name = name.Trim();
            if (!AppConfig.IsValidProfileName(name))
            {
                MessageBox.Show(this, "Usa solo letras, números, - y _ (máximo 64).", "Agregar perfil", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            foreach (object item in profiles.Items)
            {
                if (string.Equals((string)item, name, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
            }
            profiles.Items.Add(name);
        }

        private void RemoveProfile()
        {
            string selected = profiles.SelectedItem as string;
            if (selected == null)
            {
                return;
            }
            if (selected == BrowserEnvironment.DefaultProfile)
            {
                MessageBox.Show(this, "El perfil default no se puede quitar.", "Perfiles", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            foreach (Cell cell in Wall.AllCells())
            {
                if (cell.ProfileName == selected)
                {
                    MessageBox.Show(this, "Hay celdas usando el perfil \"" + selected + "\". Cámbialas de perfil antes de quitarlo.", "Perfiles", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
            }
            profiles.Items.Remove(selected);
        }

        private static List<string> Lines(string text)
        {
            List<string> lines = new List<string>();
            foreach (string line in text.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string trimmed = line.Trim();
                if (trimmed.Length > 0)
                {
                    lines.Add(trimmed);
                }
            }
            return lines;
        }

        private static TableLayoutPanel NewTable()
        {
            TableLayoutPanel table = new TableLayoutPanel();
            table.Dock = DockStyle.Fill;
            table.ColumnCount = 2;
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            table.AutoScroll = true;
            table.Padding = new Padding(8);
            return table;
        }

        private static TabPage NewPage(string title, Control content)
        {
            TabPage page = new TabPage(title);
            page.Controls.Add(content);
            return page;
        }

        private static void AddRow(TableLayoutPanel table, string label, Control control)
        {
            int row = table.RowCount;
            table.RowCount = row + 1;
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            Label caption = new Label();
            caption.Text = label;
            caption.AutoSize = true;
            caption.Margin = new Padding(0, 6, 8, 6);
            control.Margin = new Padding(0, 3, 0, 3);
            table.Controls.Add(caption, 0, row);
            table.Controls.Add(control, 1, row);
        }

        private static void AddCheck(TableLayoutPanel table, CheckBox box, string text)
        {
            box.Text = text;
            box.AutoSize = true;
            AddRow(table, string.Empty, box);
        }

        private static void SetupMultiline(TextBox box, int height)
        {
            box.Multiline = true;
            box.ScrollBars = ScrollBars.Vertical;
            box.AcceptsReturn = true;
            box.Height = height;
            box.Dock = DockStyle.Fill;
        }
    }
}
