// MuroSOC - Wall
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace MuroSoc
{
    internal static class Wall
    {
        private const int MaxClosedTabs = 25;

        private static readonly List<WallWindow> Windows = new List<WallWindow>();
        private static readonly List<TabModel> ClosedTabs = new List<TabModel>();
        private static ApplicationContext context;
        private static Timer autosaveTimer;
        private static Timer tickTimer;
        private static Cell activeCell;
        private static Cell dragTarget;
        private static bool allowClose;
        private static Point lastCursor;
        private static DateTime lastCursorMove = DateTime.UtcNow;
        private static bool cursorHidden;

        public static bool UiVisible { get; private set; }

        public static bool IsLocked { get; private set; }

        public static bool EditMode { get; private set; }

        public static bool IsExiting { get; private set; }

        public static bool IsDirty { get; private set; }

        public static string LayoutName { get; private set; }

        public static Cell ActiveCell
        {
            get { return activeCell != null && !activeCell.IsDisposed ? activeCell : null; }
        }

        public static int CellCount
        {
            get { return AllCells().Count; }
        }

        public static IList<WallWindow> OpenWindows
        {
            get { return Windows.AsReadOnly(); }
        }

        public static void Start(ApplicationContext appContext, string startUrl)
        {
            context = appContext;
            UiVisible = true;
            App.ConfigReloaded += OnConfigReloaded;
            SessionState state = LayoutStore.LoadState();
            List<string> warnings = new List<string>();
            if (state != null && state.Layout != null && state.Layout.Monitors.Count > 0)
            {
                UiVisible = state.UiVisible;
                LayoutName = state.LayoutName ?? string.Empty;
                RestoreClosedTabs(state.ClosedTabs);
                BuildWindows(state.Layout, warnings);
                if (state.Locked)
                {
                    SetLocked(true);
                }
                Log.Info("Sesión restaurada" + (LayoutName.Length > 0 ? " (layout " + LayoutName + ")" : string.Empty));
                if (!string.IsNullOrEmpty(startUrl))
                {
                    OpenInActiveCell(startUrl);
                }
            }
            else
            {
                LayoutName = string.Empty;
                BuildWindows(DefaultLayout(startUrl), warnings);
            }
            autosaveTimer = new Timer();
            autosaveTimer.Interval = 60000;
            autosaveTimer.Tick += delegate { SaveState(); };
            autosaveTimer.Start();
            tickTimer = new Timer();
            tickTimer.Interval = 1000;
            tickTimer.Tick += delegate { Tick(); };
            tickTimer.Start();
        }

        private static void Tick()
        {
            DateTime now = DateTime.UtcNow;
            foreach (WallWindow window in Windows)
            {
                foreach (Cell cell in window.Panel.Cells)
                {
                    foreach (BrowserTab tab in cell.Tabs)
                    {
                        try
                        {
                            tab.TickRefresh(now);
                        }
                        catch (Exception ex)
                        {
                            Log.Error("Error en el ciclo de la pestaña", ex);
                        }
                    }
                    cell.UpdateRefreshBadge();
                }
            }
            WallWindow main = MainWindow;
            foreach (WallWindow window in Windows)
            {
                window.UpdateClock(App.Config.ClockAllMonitors || window == main, IsLocked);
            }
            UpdateCursor(now);
        }

        public static void SetLocked(bool locked)
        {
            if (locked)
            {
                RestoreMaximized();
                SetEditMode(false);
            }
            IsLocked = locked;
            WallWindow main = MainWindow;
            foreach (WallWindow window in Windows)
            {
                window.SetLocked(locked, window == main);
            }
            Log.Info(locked ? "Muro bloqueado" : "Muro desbloqueado");
            if (!locked && main != null)
            {
                main.Activate();
            }
        }

        private static void UpdateCursor(DateTime now)
        {
            Point position = Cursor.Position;
            if (position != lastCursor)
            {
                lastCursor = position;
                lastCursorMove = now;
                if (cursorHidden)
                {
                    SetCursorHidden(false);
                }
                return;
            }
            int limit = App.Config.CursorHideSeconds;
            if (limit <= 0 || cursorHidden || (now - lastCursorMove).TotalSeconds < limit)
            {
                return;
            }
            Control under = ControlAt(position);
            Form form = under == null ? null : under.FindForm();
            if (form is WallWindow || form is LockOverlay)
            {
                SetCursorHidden(true);
            }
        }

        private static void SetCursorHidden(bool hidden)
        {
            cursorHidden = hidden;
            if (hidden)
            {
                Cursor.Hide();
            }
            else
            {
                Cursor.Show();
            }
            foreach (BrowserTab tab in AllTabs())
            {
                tab.SetCursorHidden(hidden);
            }
        }

        public static void SaveState()
        {
            if (Windows.Count == 0)
            {
                return;
            }
            try
            {
                SessionState state = new SessionState();
                state.LayoutName = LayoutName;
                state.UiVisible = UiVisible;
                state.Locked = IsLocked;
                state.Layout = Snapshot(LayoutName);
                state.ClosedTabs = ClosedTabsSnapshot();
                LayoutStore.SaveState(state);
            }
            catch (Exception ex)
            {
                Log.Error("No se pudo guardar el estado", ex);
            }
        }

        public static async void OpenInActiveCell(string url)
        {
            Cell cell = ActiveCell ?? FirstCell();
            if (cell == null || string.IsNullOrEmpty(url))
            {
                return;
            }
            TabModel model = new TabModel();
            model.Url = url;
            model.Profile = cell.ProfileName;
            try
            {
                await cell.CreateTabAsync(model, true);
            }
            catch (Exception ex)
            {
                Log.Error("No se pudo abrir la URL inicial", ex);
            }
        }

        public static void SaveLayout()
        {
            if (string.IsNullOrEmpty(LayoutName))
            {
                SaveLayoutAs();
                return;
            }
            WriteLayout(LayoutName);
        }

        public static void SaveLayoutAs()
        {
            IWin32Window owner = OwnerWindow();
            string name = InputDialog.Prompt(owner, "Guardar layout", "Nombre del layout (por ejemplo \"Turno día\" o \"Incidente\"):", LayoutName, false);
            if (name == null)
            {
                return;
            }
            name = name.Trim();
            if (!LayoutStore.IsValidName(name))
            {
                MessageBox.Show(owner, "Escribe un nombre de hasta 60 caracteres.", "Guardar layout", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (!string.Equals(name, LayoutName, StringComparison.CurrentCultureIgnoreCase) && LayoutStore.Exists(name))
            {
                DialogResult answer = MessageBox.Show(owner, "Ya existe un layout llamado \"" + name + "\". ¿Reemplazarlo?", "Guardar layout", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
                if (answer != DialogResult.Yes)
                {
                    return;
                }
            }
            WriteLayout(name);
        }

        public static void NewLayout()
        {
            if (!ConfirmLeaveLayout())
            {
                return;
            }
            LayoutName = string.Empty;
            LayoutModel layout = DefaultLayout("about:blank");
            layout.Monitors[0].Root = PaneModel.NewCell(null);
            BuildWindows(layout, new List<string>());
            SetEditMode(true);
            SaveState();
        }

        public static void LoadLayout(string name)
        {
            if (!ConfirmLeaveLayout())
            {
                return;
            }
            LayoutModel layout;
            try
            {
                layout = LayoutStore.Load(name);
            }
            catch (Exception ex)
            {
                Log.Error("No se pudo cargar el layout " + name, ex);
                MessageBox.Show(OwnerWindow(), "No se pudo cargar el layout \"" + name + "\".\r\n\r\n" + ex.Message, "Muro SOC", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (layout == null)
            {
                return;
            }
            LayoutName = name;
            List<string> warnings = new List<string>();
            BuildWindows(layout, warnings);
            Log.Info("Layout cargado: " + name);
            SaveState();
        }

        public static void LoadLayoutByIndex(int index)
        {
            List<string> names = LayoutStore.ListNames();
            if (index >= 0 && index < names.Count)
            {
                LoadLayout(names[index]);
            }
        }

        public static void DeleteLayout(string name)
        {
            IWin32Window owner = OwnerWindow();
            DialogResult answer = MessageBox.Show(owner, "¿Eliminar el layout \"" + name + "\"?", "Eliminar layout", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
            if (answer != DialogResult.Yes)
            {
                return;
            }
            try
            {
                LayoutStore.Delete(name);
                if (string.Equals(name, LayoutName, StringComparison.CurrentCultureIgnoreCase))
                {
                    SetLayoutName(string.Empty);
                }
                Log.Info("Layout eliminado: " + name);
            }
            catch (Exception ex)
            {
                Log.Error("No se pudo eliminar el layout " + name, ex);
                MessageBox.Show(owner, "No se pudo eliminar el layout.\r\n\r\n" + ex.Message, "Eliminar layout", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public static void ExportLayout()
        {
            IWin32Window owner = OwnerWindow();
            using (SaveFileDialog dialog = new SaveFileDialog())
            {
                dialog.Title = "Exportar layout";
                dialog.Filter = "Layout de Muro SOC (*.json)|*.json";
                dialog.FileName = (string.IsNullOrEmpty(LayoutName) ? "layout" : LayoutName) + ".json";
                dialog.OverwritePrompt = true;
                if (dialog.ShowDialog(owner) != DialogResult.OK)
                {
                    return;
                }
                try
                {
                    LayoutStore.Export(Snapshot(string.IsNullOrEmpty(LayoutName) ? System.IO.Path.GetFileNameWithoutExtension(dialog.FileName) : LayoutName), dialog.FileName);
                    Log.Info("Layout exportado");
                }
                catch (Exception ex)
                {
                    Log.Error("No se pudo exportar el layout", ex);
                    MessageBox.Show(owner, "No se pudo exportar el layout.\r\n\r\n" + ex.Message, "Exportar layout", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        public static void ImportLayout()
        {
            IWin32Window owner = OwnerWindow();
            string path;
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Title = "Importar layout";
                dialog.Filter = "Layout de Muro SOC (*.json)|*.json";
                if (dialog.ShowDialog(owner) != DialogResult.OK)
                {
                    return;
                }
                path = dialog.FileName;
            }
            LayoutModel layout;
            try
            {
                layout = LayoutStore.Import(path);
            }
            catch (Exception ex)
            {
                Log.Error("No se pudo importar el layout", ex);
                MessageBox.Show(owner, "El archivo no es un layout válido.\r\n\r\n" + ex.Message, "Importar layout", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            string name = InputDialog.Prompt(owner, "Importar layout", "Nombre para el layout importado:", layout.Name, false);
            if (name == null)
            {
                return;
            }
            name = name.Trim();
            if (!LayoutStore.IsValidName(name))
            {
                MessageBox.Show(owner, "Escribe un nombre de hasta 60 caracteres.", "Importar layout", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (LayoutStore.Exists(name))
            {
                DialogResult replace = MessageBox.Show(owner, "Ya existe un layout llamado \"" + name + "\". ¿Reemplazarlo?", "Importar layout", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
                if (replace != DialogResult.Yes)
                {
                    return;
                }
            }
            layout.Name = name;
            LayoutStore.Save(layout);
            Log.Info("Layout importado: " + name);
            DialogResult load = MessageBox.Show(owner, "Layout \"" + name + "\" importado. ¿Cargarlo ahora?", "Importar layout", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (load == DialogResult.Yes)
            {
                LoadLayout(name);
            }
        }

        private static void WriteLayout(string name)
        {
            try
            {
                LayoutStore.Save(Snapshot(name));
                SetLayoutName(name);
                IsDirty = false;
                SaveState();
                Log.Info("Layout guardado: " + name);
                Cell cell = ActiveCell ?? FirstCell();
                if (cell != null)
                {
                    cell.ShowNotice(null, new NoticeEventArgs(NoticeLevel.Info, "Layout \"" + name + "\" guardado."));
                }
            }
            catch (Exception ex)
            {
                Log.Error("No se pudo guardar el layout " + name, ex);
                MessageBox.Show(OwnerWindow(), "No se pudo guardar el layout.\r\n\r\n" + ex.Message, "Guardar layout", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static bool ConfirmLeaveLayout()
        {
            if (!IsDirty || string.IsNullOrEmpty(LayoutName))
            {
                return true;
            }
            DialogResult answer = MessageBox.Show(OwnerWindow(), "¿Guardar los cambios del layout \"" + LayoutName + "\" antes de cambiar?", "Muro SOC", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
            if (answer == DialogResult.Cancel)
            {
                return false;
            }
            if (answer == DialogResult.Yes)
            {
                WriteLayout(LayoutName);
            }
            return true;
        }

        private static IWin32Window OwnerWindow()
        {
            Cell cell = ActiveCell;
            if (cell != null && cell.FindForm() != null)
            {
                return cell.FindForm();
            }
            return MainWindow;
        }

        public static LayoutModel DefaultLayout(string url)
        {
            LayoutModel layout = new LayoutModel();
            layout.Name = string.Empty;
            Screen primary = Screen.PrimaryScreen;
            MonitorModel monitor = new MonitorModel();
            monitor.DeviceName = primary.DeviceName;
            monitor.Width = primary.Bounds.Width;
            monitor.Height = primary.Bounds.Height;
            monitor.X = primary.Bounds.X;
            monitor.Y = primary.Bounds.Y;
            monitor.Primary = true;
            monitor.Root = PaneModel.NewCell(string.IsNullOrEmpty(url) ? App.Config.HomeUrl : url);
            layout.Monitors.Add(monitor);
            layout.Normalize();
            return layout;
        }

        public static List<Cell> AllCells()
        {
            List<Cell> cells = new List<Cell>();
            foreach (WallWindow window in Windows)
            {
                cells.AddRange(window.Panel.Cells);
            }
            return cells;
        }

        public static List<BrowserTab> AllTabs()
        {
            List<BrowserTab> tabs = new List<BrowserTab>();
            foreach (Cell cell in AllCells())
            {
                tabs.AddRange(cell.Tabs);
            }
            return tabs;
        }

        public static WallWindow MainWindow
        {
            get
            {
                foreach (WallWindow window in Windows)
                {
                    if (window.Screen.Primary)
                    {
                        return window;
                    }
                }
                return Windows.Count > 0 ? Windows[0] : null;
            }
        }

        public static void BuildWindows(LayoutModel layout, List<string> warnings)
        {
            allowClose = true;
            try
            {
                foreach (WallWindow window in new List<WallWindow>(Windows))
                {
                    window.Close();
                    window.Dispose();
                }
            }
            finally
            {
                allowClose = false;
            }
            Windows.Clear();
            activeCell = null;

            List<KeyValuePair<Cell, PaneModel>> cells = new List<KeyValuePair<Cell, PaneModel>>();
            foreach (KeyValuePair<Screen, PaneModel> pair in MonitorMapper.Map(layout, warnings))
            {
                WallWindow window = new WallWindow(pair.Key);
                window.Panel.SetRoot(BuildNode(pair.Value, cells));
                window.UpdateTitle(LayoutName);
                Windows.Add(window);
                window.Show();
            }
            if (cells.Count > 0)
            {
                SetActiveCell(cells[0].Key);
            }
            foreach (KeyValuePair<Cell, PaneModel> pair in cells)
            {
                LoadCellTabs(pair.Key, pair.Value);
            }
            RefreshChrome();
            if (warnings.Count > 0 && cells.Count > 0)
            {
                NoticeEventArgs notice = new NoticeEventArgs(NoticeLevel.Warning, string.Join(" ", warnings.ToArray()));
                notice.ActionText = "Entendido";
                notice.Action = delegate { };
                cells[0].Key.ShowNotice(null, notice);
                foreach (string warning in warnings)
                {
                    Log.Warn(warning);
                }
            }
            IsDirty = false;
        }

        public static LayoutModel Snapshot(string name)
        {
            LayoutModel layout = new LayoutModel();
            layout.Name = name ?? string.Empty;
            foreach (WallWindow window in Windows)
            {
                MonitorModel monitor = new MonitorModel();
                Screen screen = window.Screen;
                monitor.DeviceName = screen.DeviceName;
                monitor.Width = screen.Bounds.Width;
                monitor.Height = screen.Bounds.Height;
                monitor.X = screen.Bounds.X;
                monitor.Y = screen.Bounds.Y;
                monitor.Primary = screen.Primary;
                monitor.Root = window.Panel.Root == null ? new PaneModel() : SnapshotNode(window.Panel.Root);
                layout.Monitors.Add(monitor);
            }
            layout.Normalize();
            return layout;
        }

        public static void SetActiveCell(Cell cell)
        {
            if (cell == activeCell)
            {
                return;
            }
            Cell previous = activeCell;
            activeCell = cell;
            if (previous != null && !previous.IsDisposed)
            {
                previous.UpdateChrome();
            }
            if (cell != null)
            {
                cell.UpdateChrome();
            }
        }

        public static void MarkDirty()
        {
            IsDirty = true;
        }

        public static void ClearDirty()
        {
            IsDirty = false;
        }

        public static void SetLayoutName(string name)
        {
            LayoutName = name ?? string.Empty;
            foreach (WallWindow window in Windows)
            {
                window.UpdateTitle(LayoutName);
            }
        }

        public static void SetUiVisible(bool visible)
        {
            UiVisible = visible;
            RefreshChrome();
            IsDirty = true;
        }

        public static void SetEditMode(bool edit)
        {
            EditMode = edit;
            if (edit)
            {
                foreach (WallWindow window in Windows)
                {
                    window.Panel.SetMaximized(null);
                }
            }
            RefreshChrome();
        }

        public static void RefreshChrome()
        {
            foreach (WallWindow window in Windows)
            {
                foreach (Cell cell in window.Panel.Cells)
                {
                    cell.RefreshAll();
                }
                window.Panel.RefreshChrome();
            }
        }

        public static void ToggleFullscreen()
        {
            App.Config.Fullscreen = !App.Config.Fullscreen;
            App.SaveConfig();
            foreach (WallWindow window in Windows)
            {
                window.ApplyWindowMode();
            }
        }

        public static void ToggleMaximize()
        {
            Cell cell = ActiveCell;
            if (cell == null || cell.Window == null)
            {
                return;
            }
            LayoutPanel panel = cell.Window.Panel;
            panel.SetMaximized(panel.MaximizedCell == cell ? null : cell);
        }

        public static bool RestoreMaximized()
        {
            bool restored = false;
            foreach (WallWindow window in Windows)
            {
                if (window.Panel.MaximizedCell != null)
                {
                    window.Panel.SetMaximized(null);
                    restored = true;
                }
            }
            return restored;
        }

        public static void Split(Cell cell, string direction)
        {
            if (cell == null || cell.Window == null)
            {
                return;
            }
            Cell added = cell.Window.Panel.Split(cell, direction);
            added.RefreshAll();
            SetActiveCell(added);
            RefreshChrome();
            IsDirty = true;
        }

        public static void Merge(Cell cell)
        {
            if (cell == null || cell.Window == null)
            {
                return;
            }
            PaneNode node = cell.Node;
            if (node == null || node.Parent == null)
            {
                cell.ShowNotice(null, new NoticeEventArgs(NoticeLevel.Info, "Es la única celda de este monitor."));
                return;
            }
            PaneNode sibling = node.Parent.First == node ? node.Parent.Second : node.Parent.First;
            Cell target = sibling.FirstCell();
            foreach (BrowserTab tab in new List<BrowserTab>(cell.Tabs))
            {
                MoveTab(tab, target, target.Tabs.Count, false);
            }
            cell.Window.Panel.RemoveCell(cell);
            if (activeCell == cell)
            {
                activeCell = null;
            }
            cell.Dispose();
            SetActiveCell(target);
            RefreshChrome();
            IsDirty = true;
        }

        public static void MoveTab(BrowserTab tab, Cell target, int index, bool activate)
        {
            Cell source = tab.Host as Cell;
            if (source == null || target == null || target.IsDisposed)
            {
                return;
            }
            if (source != target)
            {
                source.Detach(tab);
                Log.Info("Pestaña movida entre celdas sin recargar: " + Log.SafeUrl(tab.Url));
            }
            target.Insert(tab, index, true);
            if (activate)
            {
                SetActiveCell(target);
                tab.Focus();
            }
            IsDirty = true;
        }

        public static void UpdateTabDrag(Point screenPoint)
        {
            Cell cell = CellAt(screenPoint);
            if (cell == dragTarget)
            {
                return;
            }
            if (dragTarget != null && !dragTarget.IsDisposed)
            {
                dragTarget.IsDropTarget = false;
            }
            dragTarget = cell;
            if (cell != null)
            {
                cell.IsDropTarget = true;
            }
        }

        public static void CompleteTabDrag(BrowserTab tab, Point screenPoint)
        {
            CancelTabDrag();
            Control under = ControlAt(screenPoint);
            Cell target = CellOf(under);
            if (target == null)
            {
                return;
            }
            int index = target.Tabs.Count;
            TabStrip strip = under as TabStrip;
            if (strip != null)
            {
                index = strip.InsertIndexAt(strip.PointToClient(screenPoint));
            }
            MoveTab(tab, target, index, true);
        }

        public static void CancelTabDrag()
        {
            if (dragTarget != null && !dragTarget.IsDisposed)
            {
                dragTarget.IsDropTarget = false;
            }
            dragTarget = null;
        }

        public static async void NewTab(Cell cell)
        {
            if (cell == null)
            {
                return;
            }
            if (!UiVisible)
            {
                SetUiVisible(true);
            }
            TabModel model = new TabModel();
            model.Url = "about:blank";
            model.Profile = cell.ProfileName;
            try
            {
                BrowserTab tab = await cell.CreateTabAsync(model, true);
                if (tab != null)
                {
                    SetActiveCell(cell);
                    cell.FocusAddress();
                }
            }
            catch (Exception ex)
            {
                Log.Error("No se pudo crear la pestaña", ex);
            }
        }

        public static async void PromptUrl(Cell cell)
        {
            if (cell == null)
            {
                return;
            }
            BrowserTab current = cell.ActiveTab;
            string initial = current == null ? "https://" : current.Url;
            string text = InputDialog.Prompt(cell.FindForm(), "Abrir URL", current == null ? "URL para esta celda:" : "URL para la pestaña activa:", initial, false);
            if (text == null)
            {
                return;
            }
            string url = UrlInput.Normalize(text);
            if (url == null)
            {
                MessageBox.Show(cell.FindForm(), "La URL no es válida.", "Muro SOC", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (current != null)
            {
                current.Navigate(url);
                return;
            }
            TabModel model = new TabModel();
            model.Url = url;
            model.Profile = cell.ProfileName;
            try
            {
                await cell.CreateTabAsync(model, true);
            }
            catch (Exception ex)
            {
                Log.Error("No se pudo abrir la pestaña", ex);
            }
        }

        public static void RememberClosedTab(TabModel model)
        {
            if (model == null || string.IsNullOrEmpty(model.Url) || model.Url == "about:blank")
            {
                return;
            }
            ClosedTabs.Add(model);
            while (ClosedTabs.Count > MaxClosedTabs)
            {
                ClosedTabs.RemoveAt(0);
            }
        }

        public static List<TabModel> ClosedTabsSnapshot()
        {
            return new List<TabModel>(ClosedTabs);
        }

        public static void RestoreClosedTabs(List<TabModel> tabs)
        {
            ClosedTabs.Clear();
            if (tabs != null)
            {
                foreach (TabModel tab in tabs)
                {
                    if (tab != null)
                    {
                        tab.Normalize();
                        ClosedTabs.Add(tab);
                    }
                }
            }
        }

        public static async void ReopenClosedTab()
        {
            if (ClosedTabs.Count == 0)
            {
                return;
            }
            Cell cell = ActiveCell ?? FirstCell();
            if (cell == null)
            {
                return;
            }
            TabModel model = ClosedTabs[ClosedTabs.Count - 1];
            ClosedTabs.RemoveAt(ClosedTabs.Count - 1);
            try
            {
                await cell.CreateTabAsync(model, true);
            }
            catch (Exception ex)
            {
                Log.Error("No se pudo reabrir la pestaña", ex);
            }
        }

        public static void ReloadWall()
        {
            foreach (BrowserTab tab in AllTabs())
            {
                tab.Reload();
            }
            Log.Info("Recarga de todo el muro");
        }

        public static async void ChangeCellProfile(Cell cell, string profile)
        {
            if (cell == null || profile == cell.ProfileName)
            {
                return;
            }
            List<BrowserTab> different = new List<BrowserTab>();
            foreach (BrowserTab tab in cell.Tabs)
            {
                if (tab.ProfileName != profile)
                {
                    different.Add(tab);
                }
            }
            if (different.Count > 0)
            {
                DialogResult answer = MessageBox.Show(cell.FindForm(),
                    "Las pestañas de esta celda se van a recargar con el perfil \"" + profile + "\". Puede que tengas que iniciar sesión de nuevo.\r\n\r\n¿Continuar?",
                    "Cambiar perfil", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (answer != DialogResult.Yes)
                {
                    return;
                }
            }
            cell.ProfileName = profile;
            foreach (BrowserTab tab in different)
            {
                int index = new List<BrowserTab>(cell.Tabs).IndexOf(tab);
                bool wasActive = cell.ActiveTab == tab;
                TabModel model = tab.Snapshot();
                model.Profile = profile;
                cell.CloseTab(tab, false);
                try
                {
                    BrowserTab created = await cell.CreateTabAsync(model, wasActive);
                    if (created != null)
                    {
                        cell.Insert(created, index, wasActive);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error("No se pudo recrear la pestaña con el perfil " + profile, ex);
                }
            }
            cell.RefreshAll();
            IsDirty = true;
        }

        public static void SetMonitorEnabled(Screen screen, bool enabled)
        {
            WallWindow existing = null;
            foreach (WallWindow window in Windows)
            {
                if (window.DeviceName == screen.DeviceName)
                {
                    existing = window;
                }
            }
            if (enabled && existing == null)
            {
                WallWindow window = new WallWindow(screen);
                List<KeyValuePair<Cell, PaneModel>> cells = new List<KeyValuePair<Cell, PaneModel>>();
                window.Panel.SetRoot(BuildNode(PaneModel.NewCell(null), cells));
                window.UpdateTitle(LayoutName);
                Windows.Add(window);
                window.Show();
                RefreshChrome();
                IsDirty = true;
                return;
            }
            if (!enabled && existing != null)
            {
                if (Windows.Count == 1)
                {
                    MessageBox.Show(existing, "Debe quedar al menos un monitor.", "Muro SOC", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                WallWindow keep = Windows[0] == existing ? Windows[1] : Windows[0];
                Cell target = keep.Panel.Cells[0];
                foreach (Cell cell in existing.Panel.Cells)
                {
                    foreach (BrowserTab tab in new List<BrowserTab>(cell.Tabs))
                    {
                        MoveTab(tab, target, target.Tabs.Count, false);
                    }
                }
                Windows.Remove(existing);
                if (activeCell != null && activeCell.Window == existing)
                {
                    activeCell = null;
                }
                allowClose = true;
                try
                {
                    existing.Close();
                    existing.Dispose();
                }
                finally
                {
                    allowClose = false;
                }
                SetActiveCell(target);
                RefreshChrome();
                IsDirty = true;
            }
        }

        public static bool AllowClose
        {
            get { return allowClose || IsExiting; }
        }

        public static void RequestExit(IWin32Window owner)
        {
            DialogResult answer = MessageBox.Show(owner, "¿Cerrar Muro SOC?", "Muro SOC", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
            if (answer == DialogResult.Yes)
            {
                Exit();
            }
        }

        public static void Exit()
        {
            if (IsExiting)
            {
                return;
            }
            SaveState();
            IsExiting = true;
            if (autosaveTimer != null)
            {
                autosaveTimer.Stop();
            }
            if (tickTimer != null)
            {
                tickTimer.Stop();
            }
            Log.Info("Cierre de Muro SOC");
            foreach (WallWindow window in new List<WallWindow>(Windows))
            {
                window.Close();
            }
            Windows.Clear();
            if (context != null)
            {
                context.ExitThread();
            }
        }

        public static bool TryHandleKey(Keys keys)
        {
            if (IsLocked)
            {
                if (Shortcuts.Match(keys) == Shortcuts.LockWall)
                {
                    SetLocked(false);
                }
                return true;
            }
            if (keys == Keys.Escape)
            {
                if (RestoreMaximized())
                {
                    return true;
                }
                if (EditMode)
                {
                    SetEditMode(false);
                    return true;
                }
                return false;
            }
            Keys code = keys & Keys.KeyCode;
            if ((keys & Keys.Modifiers) == Keys.Control && code >= Keys.D1 && code <= Keys.D9)
            {
                LoadLayoutByIndex(code - Keys.D1);
                return true;
            }
            string action = Shortcuts.Match(keys);
            if (action != null)
            {
                Execute(action);
                return true;
            }
            return false;
        }

        public static void Execute(string action)
        {
            Cell cell = ActiveCell ?? FirstCell();
            switch (action)
            {
                case Shortcuts.ToggleUi:
                    SetUiVisible(!UiVisible);
                    break;
                case Shortcuts.LockWall:
                    SetLocked(!IsLocked);
                    break;
                case Shortcuts.MaximizeCell:
                    ToggleMaximize();
                    break;
                case Shortcuts.EditLayout:
                    SetEditMode(!EditMode);
                    break;
                case Shortcuts.NewTab:
                    NewTab(cell);
                    break;
                case Shortcuts.CloseTab:
                    if (cell != null && cell.ActiveTab != null)
                    {
                        cell.CloseTab(cell.ActiveTab, true);
                    }
                    break;
                case Shortcuts.ReopenTab:
                    ReopenClosedTab();
                    break;
                case Shortcuts.NextTab:
                    if (cell != null)
                    {
                        cell.ActivateOffset(1);
                    }
                    break;
                case Shortcuts.PreviousTab:
                    if (cell != null)
                    {
                        cell.ActivateOffset(-1);
                    }
                    break;
                case Shortcuts.FocusAddress:
                    if (cell != null)
                    {
                        if (!UiVisible)
                        {
                            SetUiVisible(true);
                        }
                        cell.FocusAddress();
                    }
                    break;
                case Shortcuts.ReloadWall:
                    ReloadWall();
                    break;
                case Shortcuts.SaveLayout:
                    SaveLayout();
                    break;
                case Shortcuts.PickElement:
                    if (cell != null && cell.ActiveTab != null)
                    {
                        cell.ActiveTab.StartPicker("isolate");
                    }
                    break;
            }
        }

        public static List<MenuEntry> BuildCellMenu(Cell cell, BrowserTab tab)
        {
            List<MenuEntry> menu = new List<MenuEntry>();
            menu.Add(MenuEntry.Item("Pestaña nueva", Shortcuts.Display(Shortcuts.NewTab), delegate { NewTab(cell); }));
            MenuEntry reopen = MenuEntry.Item("Reabrir pestaña cerrada", Shortcuts.Display(Shortcuts.ReopenTab), delegate { SetActiveCell(cell); ReopenClosedTab(); });
            reopen.Enabled = ClosedTabs.Count > 0;
            menu.Add(reopen);
            if (tab != null)
            {
                string url = tab.Url;
                menu.Add(MenuEntry.Item("Abrir esta página en Edge", delegate { ExternalBrowser.OpenInEdge(url); }));
                menu.Add(MenuEntry.Item("Abrir URL en esta pestaña...", delegate { PromptUrl(cell); }));
                menu.Add(MenuEntry.Separator());
                menu.AddRange(TabMenu.Build(cell, tab));
            }
            menu.Add(MenuEntry.Separator());

            List<MenuEntry> cellMenu = new List<MenuEntry>();
            cellMenu.Add(MenuEntry.Item("Dividir en columnas", delegate { Split(cell, PaneModel.Columns); }));
            cellMenu.Add(MenuEntry.Item("Dividir en filas", delegate { Split(cell, PaneModel.Rows); }));
            MenuEntry merge = MenuEntry.Item("Fusionar con la vecina", delegate { Merge(cell); });
            merge.Enabled = cell.Node != null && cell.Node.Parent != null;
            cellMenu.Add(merge);
            cellMenu.Add(MenuEntry.Sub("Perfil de la celda", BuildProfileMenu(cell)));
            menu.Add(MenuEntry.Sub("Celda", cellMenu));
            menu.Add(MenuEntry.Sub("Layout", BuildLayoutMenu()));
            menu.Add(MenuEntry.Sub("Monitores", BuildMonitorMenu()));
            menu.Add(MenuEntry.Separator());

            menu.Add(MenuEntry.Check("Mostrar interfaz", UiVisible, delegate { SetUiVisible(!UiVisible); }));
            menu[menu.Count - 1].Shortcut = Shortcuts.Display(Shortcuts.ToggleUi);
            menu.Add(MenuEntry.Check("Editar layout", EditMode, delegate { SetEditMode(!EditMode); }));
            menu[menu.Count - 1].Shortcut = Shortcuts.Display(Shortcuts.EditLayout);
            menu.Add(MenuEntry.Item("Maximizar celda", Shortcuts.Display(Shortcuts.MaximizeCell), delegate { SetActiveCell(cell); ToggleMaximize(); }));
            menu.Add(MenuEntry.Check("Pantalla completa", App.Config.Fullscreen, delegate { ToggleFullscreen(); }));
            menu.Add(MenuEntry.Check("Reloj", App.Config.ClockEnabled, delegate
            {
                App.Config.ClockEnabled = !App.Config.ClockEnabled;
                App.SaveConfig();
            }));
            menu.Add(MenuEntry.Item("Bloquear muro", Shortcuts.Display(Shortcuts.LockWall), delegate { SetLocked(true); }));
            menu.Add(MenuEntry.Item("Recargar todo el muro", Shortcuts.Display(Shortcuts.ReloadWall), delegate { ReloadWall(); }));
            menu.Add(MenuEntry.Separator());

            List<MenuEntry> signOut = new List<MenuEntry>();
            foreach (string profile in App.Config.Profiles)
            {
                string target = profile;
                signOut.Add(MenuEntry.Item("Perfil " + profile + "...", delegate { App.SignOutProfile(cell.FindForm(), target); }));
            }
            menu.Add(MenuEntry.Sub("Cerrar sesión en todo", signOut));
            menu.Add(MenuEntry.Item("Acerca de Muro SOC", delegate { App.ShowAbout(cell.FindForm()); }));
            menu.Add(MenuEntry.Item("Cerrar Muro SOC", delegate { RequestExit(cell.FindForm()); }));
            return menu;
        }

        public static List<MenuEntry> BuildLayoutMenu()
        {
            List<MenuEntry> menu = new List<MenuEntry>();
            menu.Add(MenuEntry.Check("Editar layout", EditMode, delegate { SetEditMode(!EditMode); }));
            menu[menu.Count - 1].Shortcut = Shortcuts.Display(Shortcuts.EditLayout);
            menu.Add(MenuEntry.Separator());
            string saveText = string.IsNullOrEmpty(LayoutName) ? "Guardar layout..." : "Guardar \"" + LayoutName + "\"";
            menu.Add(MenuEntry.Item(saveText, Shortcuts.Display(Shortcuts.SaveLayout), delegate { SaveLayout(); }));
            menu.Add(MenuEntry.Item("Guardar como...", delegate { SaveLayoutAs(); }));
            menu.Add(MenuEntry.Item("Layout nuevo", delegate { NewLayout(); }));
            menu.Add(MenuEntry.Separator());
            List<string> names = LayoutStore.ListNames();
            List<MenuEntry> load = new List<MenuEntry>();
            List<MenuEntry> delete = new List<MenuEntry>();
            for (int i = 0; i < names.Count; i++)
            {
                string name = names[i];
                MenuEntry entry = MenuEntry.Check(name, string.Equals(name, LayoutName, StringComparison.CurrentCultureIgnoreCase), delegate { LoadLayout(name); });
                if (i < 9)
                {
                    entry.Shortcut = "Ctrl+" + (i + 1);
                }
                load.Add(entry);
                delete.Add(MenuEntry.Item(name, delegate { DeleteLayout(name); }));
            }
            MenuEntry loadMenu = MenuEntry.Sub("Cargar", load);
            loadMenu.Enabled = names.Count > 0;
            menu.Add(loadMenu);
            menu.Add(MenuEntry.Item("Exportar...", delegate { ExportLayout(); }));
            menu.Add(MenuEntry.Item("Importar...", delegate { ImportLayout(); }));
            MenuEntry deleteMenu = MenuEntry.Sub("Eliminar", delete);
            deleteMenu.Enabled = names.Count > 0;
            menu.Add(deleteMenu);
            return menu;
        }

        public static List<MenuEntry> BuildProfileMenu(Cell cell)
        {
            List<MenuEntry> menu = new List<MenuEntry>();
            foreach (string profile in App.Config.Profiles)
            {
                string target = profile;
                menu.Add(MenuEntry.Check(profile, profile == cell.ProfileName, delegate { ChangeCellProfile(cell, target); }));
            }
            return menu;
        }

        public static List<MenuEntry> BuildMonitorMenu()
        {
            List<MenuEntry> menu = new List<MenuEntry>();
            foreach (Screen screen in Screen.AllScreens)
            {
                Screen target = screen;
                bool used = false;
                foreach (WallWindow window in Windows)
                {
                    if (window.DeviceName == screen.DeviceName)
                    {
                        used = true;
                    }
                }
                string label = screen.DeviceName.Replace(@"\\.\", string.Empty) + " (" + screen.Bounds.Width + "x" + screen.Bounds.Height + (screen.Primary ? ", principal" : string.Empty) + ")";
                bool enable = !used;
                menu.Add(MenuEntry.Check(label, used, delegate { SetMonitorEnabled(target, enable); }));
            }
            return menu;
        }

        private static void OnConfigReloaded(object sender, ConfigReloadedEventArgs e)
        {
            Cell cell = ActiveCell ?? FirstCell();
            if (cell == null)
            {
                return;
            }
            if (e.Error == null)
            {
                cell.ShowNotice(null, new NoticeEventArgs(NoticeLevel.Info, "Configuración actualizada."));
                foreach (WallWindow window in Windows)
                {
                    window.ApplyWindowMode();
                }
                RefreshChrome();
            }
            else
            {
                cell.ShowNotice(null, new NoticeEventArgs(NoticeLevel.Error, "config.json tiene un error y no se aplicó: " + e.Error));
            }
        }

        private static Cell FirstCell()
        {
            List<Cell> cells = AllCells();
            return cells.Count > 0 ? cells[0] : null;
        }

        private static PaneNode BuildNode(PaneModel model, List<KeyValuePair<Cell, PaneModel>> cells)
        {
            PaneNode node = new PaneNode();
            if (model.IsSplit)
            {
                node.Direction = model.Direction;
                node.Ratio = model.Ratio;
                node.First = BuildNode(model.First, cells);
                node.Second = BuildNode(model.Second, cells);
                node.First.Parent = node;
                node.Second.Parent = node;
                return node;
            }
            Cell cell = new Cell(model.Profile);
            node.Cell = cell;
            cell.Node = node;
            cells.Add(new KeyValuePair<Cell, PaneModel>(cell, model));
            return node;
        }

        private static PaneModel SnapshotNode(PaneNode node)
        {
            PaneModel model = new PaneModel();
            if (!node.IsLeaf)
            {
                model.Type = PaneModel.SplitType;
                model.Direction = node.Direction;
                model.Ratio = node.Ratio;
                model.First = SnapshotNode(node.First);
                model.Second = SnapshotNode(node.Second);
                return model;
            }
            Cell cell = node.Cell;
            model.Type = PaneModel.CellType;
            model.Profile = cell.ProfileName;
            int index = 0;
            foreach (BrowserTab tab in cell.Tabs)
            {
                if (tab == cell.ActiveTab)
                {
                    model.ActiveTab = index;
                }
                model.Tabs.Add(tab.Snapshot());
                index++;
            }
            return model;
        }

        private static async void LoadCellTabs(Cell cell, PaneModel model)
        {
            for (int i = 0; i < model.Tabs.Count; i++)
            {
                if (cell.IsDisposed)
                {
                    return;
                }
                try
                {
                    await cell.CreateTabAsync(model.Tabs[i], i == model.ActiveTab);
                }
                catch (Exception ex)
                {
                    Log.Error("No se pudo crear una pestaña del layout", ex);
                }
            }
            IsDirty = false;
        }

        private static Control ControlAt(Point screenPoint)
        {
            IntPtr handle = NativeMethods.WindowFromPoint(new NativeMethods.POINT(screenPoint.X, screenPoint.Y));
            return handle == IntPtr.Zero ? null : Control.FromChildHandle(handle);
        }

        private static Cell CellAt(Point screenPoint)
        {
            return CellOf(ControlAt(screenPoint));
        }

        private static Cell CellOf(Control control)
        {
            while (control != null && !(control is Cell))
            {
                control = control.Parent;
            }
            return control as Cell;
        }
    }
}
