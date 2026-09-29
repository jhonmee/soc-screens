// MuroSOC - Cell
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MuroSoc
{
    internal sealed class Cell : Panel, ITabHost
    {
        public static readonly Color LoginColor = Color.FromArgb(255, 140, 0);
        public static readonly Color ErrorColor = Color.FromArgb(210, 40, 40);
        public static readonly Color RecoveringColor = Color.FromArgb(0, 150, 200);
        public static readonly Color AccentColor = Color.FromArgb(0, 120, 212);
        public static readonly Color ChromeBack = Color.FromArgb(28, 28, 28);

        private readonly List<BrowserTab> tabs = new List<BrowserTab>();
        private readonly TabStrip strip;
        private readonly AddressBar address;
        private readonly EditBar editBar;
        private readonly NoticeBar notices;
        private readonly Label status;
        private readonly Panel content;
        private readonly EmptyCellPanel empty;
        private readonly HoverBar hoverBar;
        private BrowserTab active;
        private bool dropTarget;

        public Cell(string profileName)
        {
            ProfileName = AppConfig.IsValidProfileName(profileName) ? profileName : BrowserEnvironment.DefaultProfile;
            BackColor = Color.Black;

            content = new Panel();
            content.Dock = DockStyle.Fill;
            content.BackColor = Color.Black;
            content.Resize += delegate { UpdateBrowserBounds(); };

            empty = new EmptyCellPanel(this);
            empty.Dock = DockStyle.Fill;
            content.Controls.Add(empty);

            hoverBar = new HoverBar(this);
            content.Controls.Add(hoverBar);
            content.Resize += delegate { PlaceHoverBar(); };

            status = new Label();
            status.Dock = DockStyle.Bottom;
            status.TextAlign = ContentAlignment.MiddleLeft;
            status.ForeColor = Color.Black;
            status.Visible = false;

            notices = new NoticeBar();
            editBar = new EditBar(this);
            address = new AddressBar(this);
            strip = new TabStrip(this);

            Controls.Add(content);
            Controls.Add(status);
            Controls.Add(notices);
            Controls.Add(editBar);
            Controls.Add(address);
            Controls.Add(strip);

            ApplyScale();
            UpdateChrome();
        }

        public PaneNode Node { get; set; }

        public string ProfileName { get; set; }

        public IList<BrowserTab> Tabs
        {
            get { return tabs.AsReadOnly(); }
        }

        public BrowserTab ActiveTab
        {
            get { return active; }
        }

        public Control ContentHost
        {
            get { return content; }
        }

        public WallWindow Window
        {
            get { return FindForm() as WallWindow; }
        }

        public bool IsDropTarget
        {
            get { return dropTarget; }
            set
            {
                if (dropTarget != value)
                {
                    dropTarget = value;
                    UpdateChrome();
                }
            }
        }

        public async Task<BrowserTab> CreateTabAsync(TabModel settings, bool activate)
        {
            TabModel model = settings == null ? new TabModel() : settings.Clone();
            if (!AppConfig.IsValidProfileName(model.Profile) || settings == null)
            {
                model.Profile = ProfileName;
            }
            model.Normalize();
            BrowserTab tab = await BrowserTab.CreateAsync(this, model, null, false);
            if (IsDisposed)
            {
                tab.Close();
                return null;
            }
            Insert(tab, tabs.Count, activate);
            string url = string.IsNullOrEmpty(model.Url) ? App.Config.HomeUrl : model.Url;
            tab.Navigate(url);
            return tab;
        }

        public void Insert(BrowserTab tab, int index, bool activate)
        {
            if (tab.Host != this)
            {
                tab.MoveTo(this);
            }
            if (tabs.Contains(tab))
            {
                int current = tabs.IndexOf(tab);
                tabs.RemoveAt(current);
                if (index > current)
                {
                    index--;
                }
            }
            index = Math.Max(0, Math.Min(index, tabs.Count));
            tabs.Insert(index, tab);
            Wall.MarkDirty();
            if (activate || active == null)
            {
                Activate(tab);
            }
            else
            {
                tab.SetVisible(false);
                RefreshAll();
            }
        }

        public void Detach(BrowserTab tab)
        {
            int index = tabs.IndexOf(tab);
            if (index < 0)
            {
                return;
            }
            tabs.RemoveAt(index);
            Wall.MarkDirty();
            if (active == tab)
            {
                active = null;
                if (tabs.Count > 0)
                {
                    Activate(tabs[Math.Min(index, tabs.Count - 1)]);
                    return;
                }
                Activate(null);
                return;
            }
            RefreshAll();
        }

        public void Activate(BrowserTab tab)
        {
            active = tab;
            foreach (BrowserTab item in tabs)
            {
                if (item != tab)
                {
                    item.SetVisible(false);
                }
            }
            empty.Visible = tab == null;
            if (tab != null)
            {
                UpdateBrowserBounds();
                tab.SetVisible(true);
            }
            RefreshAll();
        }

        public void ActivateOffset(int offset)
        {
            if (tabs.Count < 2 || active == null)
            {
                return;
            }
            int index = (tabs.IndexOf(active) + offset + tabs.Count) % tabs.Count;
            Activate(tabs[index]);
            tabs[index].Focus();
        }

        public void CloseTab(BrowserTab tab, bool remember)
        {
            if (!tabs.Contains(tab))
            {
                return;
            }
            if (remember)
            {
                Wall.RememberClosedTab(tab.Snapshot());
            }
            Detach(tab);
            tab.Close();
        }

        public void CloseAllTabs(bool remember)
        {
            foreach (BrowserTab tab in new List<BrowserTab>(tabs))
            {
                CloseTab(tab, remember);
            }
        }

        public void UpdateRefreshBadge()
        {
            BrowserTab tab = active;
            bool show = tab != null
                && App.Config.ShowHoverBar
                && !Wall.UiVisible
                && !Wall.EditMode
                && !Wall.IsLocked
                && !Wall.IsCursorHidden
                && (tab.IsHovered || hoverBar.ContainsCursor);
            if (show)
            {
                hoverBar.SetCountdown(tab.RefreshCountdownText);
                if (!hoverBar.Visible)
                {
                    hoverBar.Visible = true;
                }
                PlaceHoverBar();
                hoverBar.BringToFront();
            }
            else if (hoverBar.Visible)
            {
                hoverBar.Visible = false;
            }
            if (strip.Visible)
            {
                foreach (BrowserTab item in tabs)
                {
                    if (item.Settings.AutoRefreshSeconds > 0)
                    {
                        strip.Invalidate();
                        break;
                    }
                }
            }
            if (tab != null && tab.ErrorText != null)
            {
                UpdateChrome();
            }
        }

        public void FocusAddress()
        {
            address.FocusInput();
        }

        public void NotifyPositionChanged()
        {
            foreach (BrowserTab tab in tabs)
            {
                tab.NotifyPositionChanged();
            }
        }

        public void UpdateChrome()
        {
            bool ui = Wall.UiVisible;
            bool edit = Wall.EditMode;
            strip.Visible = ui || edit;
            address.Visible = ui;
            editBar.Visible = edit;

            Color border = Color.Empty;
            int thickness = 0;
            string text = null;
            BrowserTab tab = active;
            if (dropTarget)
            {
                border = AccentColor;
                thickness = 3;
            }
            else if (tab != null && tab.ErrorText != null)
            {
                border = ErrorColor;
                thickness = 3;
                text = tab.ErrorText;
            }
            else if (tab != null && tab.IsRecovering)
            {
                border = RecoveringColor;
                thickness = 3;
                text = "Recuperando la pestaña...";
            }
            else if (tab != null && tab.IsAtLogin)
            {
                border = LoginColor;
                thickness = 3;
                text = "Requiere login";
            }
            else if (edit)
            {
                border = Wall.ActiveCell == this ? AccentColor : Color.FromArgb(90, 90, 90);
                thickness = 1;
            }
            else if (ui && Wall.ActiveCell == this && Wall.CellCount > 1)
            {
                border = AccentColor;
                thickness = 1;
            }

            int scaled = thickness == 0 ? 0 : Math.Max(1, Dpi.Scale(this, thickness));
            Padding padding = new Padding(scaled);
            if (Padding != padding)
            {
                Padding = padding;
            }
            BackColor = thickness == 0 ? Color.Black : border;
            if (text != null)
            {
                status.BackColor = border;
                status.ForeColor = border == ErrorColor || border == RecoveringColor ? Color.White : Color.Black;
                status.Text = "  " + text;
                status.Visible = true;
            }
            else
            {
                status.Visible = false;
            }
        }

        public void RefreshAll()
        {
            strip.Invalidate();
            address.Bind(active);
            editBar.RefreshInfo();
            UpdateChrome();
        }

        public Task<BrowserTab> OpenScriptTabAsync(BrowserTab opener)
        {
            return OpenScriptTabCoreAsync(opener);
        }

        public void CloseScriptTab(BrowserTab tab)
        {
            CloseTab(tab, false);
        }

        public void OnTabStateChanged(BrowserTab tab)
        {
            if (tab == active)
            {
                address.Bind(tab);
                editBar.RefreshInfo();
                UpdateChrome();
            }
            strip.Invalidate();
        }

        public void ShowNotice(BrowserTab source, NoticeEventArgs notice)
        {
            notices.ShowNotice(notice);
        }

        public void FocusHost()
        {
            Form form = FindForm();
            if (form != null)
            {
                form.Activate();
            }
            Wall.SetActiveCell(this);
        }

        public void OnTabFocused(BrowserTab tab)
        {
            Wall.SetActiveCell(this);
        }

        public List<MenuEntry> BuildMenu(BrowserTab tab)
        {
            return Wall.BuildCellMenu(this, tab);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            Wall.SetActiveCell(this);
            base.OnMouseDown(e);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                foreach (BrowserTab tab in new List<BrowserTab>(tabs))
                {
                    tab.Close();
                }
                tabs.Clear();
            }
            base.Dispose(disposing);
        }

        private async Task<BrowserTab> OpenScriptTabCoreAsync(BrowserTab opener)
        {
            TabModel model = new TabModel();
            model.Profile = opener.ProfileName;
            BrowserTab tab = await BrowserTab.CreateAsync(this, model, opener, false);
            Insert(tab, active == null ? tabs.Count : tabs.IndexOf(active) + 1, true);
            return tab;
        }

        private void PlaceHoverBar()
        {
            int margin = Dpi.Scale(this, 8);
            Point location = new Point(
                Math.Max(0, content.ClientSize.Width - hoverBar.Width - margin),
                Math.Max(0, content.ClientSize.Height - hoverBar.Height - margin));
            if (hoverBar.Location != location)
            {
                hoverBar.Location = location;
            }
        }

        private void ApplyScale()
        {
            status.Height = Dpi.Scale(this, 22);
            status.Font = new Font("Segoe UI", Dpi.ScaleF(this, 12f), FontStyle.Bold, GraphicsUnit.Pixel);
        }

        private void UpdateBrowserBounds()
        {
            if (active != null)
            {
                active.SetBounds(content.ClientRectangle);
            }
        }
    }
}
