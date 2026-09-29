// MuroSOC - LayoutModels
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace MuroSoc
{
    [DataContract]
    internal sealed class TabModel
    {
        public TabModel()
        {
            SetDefaults();
        }

        [DataMember(Order = 1)]
        public string Url { get; set; }

        [DataMember(Order = 2)]
        public string Title { get; set; }

        [DataMember(Order = 3)]
        public string Profile { get; set; }

        [DataMember(Order = 4)]
        public double Zoom { get; set; }

        [DataMember(Order = 5)]
        public int VirtualWidth { get; set; }

        [DataMember(Order = 6)]
        public int AutoRefreshSeconds { get; set; }

        [DataMember(Order = 7)]
        public bool PanEnabled { get; set; }

        [DataMember(Order = 8)]
        public int PanX { get; set; }

        [DataMember(Order = 9)]
        public int PanY { get; set; }

        [DataMember(Order = 10)]
        public string PanSelector { get; set; }

        [DataMember(Order = 11)]
        public string IsolateSelector { get; set; }

        [DataMember(Order = 12)]
        public List<string> HideSelectors { get; set; }

        [DataMember(Order = 13)]
        public string CustomCss { get; set; }

        [DataMember(Order = 14)]
        public string CustomCssPattern { get; set; }

        [DataMember(Order = 15)]
        public bool HideScrollbars { get; set; }

        [DataMember(Order = 16)]
        public int MinWidth { get; set; }

        public TabModel Clone()
        {
            TabModel copy = (TabModel)MemberwiseClone();
            copy.HideSelectors = new List<string>(HideSelectors ?? new List<string>());
            return copy;
        }

        public void Normalize()
        {
            if (Url == null)
            {
                Url = string.Empty;
            }
            if (!AppConfig.IsValidProfileName(Profile))
            {
                Profile = BrowserEnvironment.DefaultProfile;
            }
            if (Zoom < 0.25 || Zoom > 5 || double.IsNaN(Zoom))
            {
                Zoom = 1.0;
            }
            if (VirtualWidth != 0 && (VirtualWidth < 320 || VirtualWidth > 7680))
            {
                VirtualWidth = 0;
            }
            if (AutoRefreshSeconds < 0)
            {
                AutoRefreshSeconds = 0;
            }
            if (AutoRefreshSeconds > 0 && AutoRefreshSeconds < 30)
            {
                AutoRefreshSeconds = 30;
            }
            if (HideSelectors == null)
            {
                HideSelectors = new List<string>();
            }
            HideSelectors.RemoveAll(delegate(string s) { return string.IsNullOrEmpty(s); });
            if (MinWidth < 0 || MinWidth > 7680)
            {
                MinWidth = 0;
            }
        }

        [OnDeserializing]
        private void OnDeserializing(StreamingContext context)
        {
            SetDefaults();
        }

        private void SetDefaults()
        {
            Url = string.Empty;
            Title = string.Empty;
            Profile = BrowserEnvironment.DefaultProfile;
            Zoom = 1.0;
            HideSelectors = new List<string>();
            CustomCss = string.Empty;
            CustomCssPattern = string.Empty;
            IsolateSelector = string.Empty;
            PanSelector = string.Empty;
        }
    }

    [DataContract]
    internal sealed class PaneModel
    {
        public const string CellType = "cell";
        public const string SplitType = "split";
        public const string Columns = "columns";
        public const string Rows = "rows";

        public PaneModel()
        {
            SetDefaults();
        }

        [DataMember(Order = 1)]
        public string Type { get; set; }

        [DataMember(Order = 2)]
        public string Direction { get; set; }

        [DataMember(Order = 3)]
        public double Ratio { get; set; }

        [DataMember(Order = 4)]
        public PaneModel First { get; set; }

        [DataMember(Order = 5)]
        public PaneModel Second { get; set; }

        [DataMember(Order = 6)]
        public string Profile { get; set; }

        [DataMember(Order = 7)]
        public List<TabModel> Tabs { get; set; }

        [DataMember(Order = 8)]
        public int ActiveTab { get; set; }

        public bool IsSplit
        {
            get { return Type == SplitType && First != null && Second != null; }
        }

        public static PaneModel NewCell(string url)
        {
            PaneModel cell = new PaneModel();
            if (!string.IsNullOrEmpty(url))
            {
                TabModel tab = new TabModel();
                tab.Url = url;
                cell.Tabs.Add(tab);
            }
            return cell;
        }

        public void Normalize()
        {
            if (Type == SplitType && First != null && Second != null)
            {
                if (Direction != Rows)
                {
                    Direction = Columns;
                }
                if (Ratio < 0.05 || Ratio > 0.95 || double.IsNaN(Ratio))
                {
                    Ratio = 0.5;
                }
                First.Normalize();
                Second.Normalize();
                Tabs = new List<TabModel>();
                return;
            }
            Type = CellType;
            First = null;
            Second = null;
            if (!AppConfig.IsValidProfileName(Profile))
            {
                Profile = BrowserEnvironment.DefaultProfile;
            }
            if (Tabs == null)
            {
                Tabs = new List<TabModel>();
            }
            Tabs.RemoveAll(delegate(TabModel t) { return t == null; });
            foreach (TabModel tab in Tabs)
            {
                tab.Normalize();
            }
            if (ActiveTab < 0 || ActiveTab >= Tabs.Count)
            {
                ActiveTab = 0;
            }
        }

        public void CollectCells(List<PaneModel> into)
        {
            if (IsSplit)
            {
                First.CollectCells(into);
                Second.CollectCells(into);
            }
            else
            {
                into.Add(this);
            }
        }

        [OnDeserializing]
        private void OnDeserializing(StreamingContext context)
        {
            SetDefaults();
        }

        private void SetDefaults()
        {
            Type = CellType;
            Direction = Columns;
            Ratio = 0.5;
            Profile = BrowserEnvironment.DefaultProfile;
            Tabs = new List<TabModel>();
        }
    }

    [DataContract]
    internal sealed class MonitorModel
    {
        [DataMember(Order = 1)]
        public string DeviceName { get; set; }

        [DataMember(Order = 2)]
        public int Width { get; set; }

        [DataMember(Order = 3)]
        public int Height { get; set; }

        [DataMember(Order = 4)]
        public int X { get; set; }

        [DataMember(Order = 5)]
        public int Y { get; set; }

        [DataMember(Order = 6)]
        public bool Primary { get; set; }

        [DataMember(Order = 7)]
        public PaneModel Root { get; set; }
    }

    [DataContract]
    internal sealed class LayoutModel
    {
        public const int CurrentSchemaVersion = 1;

        public LayoutModel()
        {
            SetDefaults();
        }

        [DataMember(Order = 1)]
        public int SchemaVersion { get; set; }

        [DataMember(Order = 2)]
        public string Name { get; set; }

        [DataMember(Order = 3)]
        public List<MonitorModel> Monitors { get; set; }

        public void Normalize()
        {
            if (SchemaVersion <= 0)
            {
                SchemaVersion = CurrentSchemaVersion;
            }
            if (string.IsNullOrEmpty(Name))
            {
                Name = "Sin nombre";
            }
            if (Monitors == null)
            {
                Monitors = new List<MonitorModel>();
            }
            Monitors.RemoveAll(delegate(MonitorModel m) { return m == null; });
            foreach (MonitorModel monitor in Monitors)
            {
                if (monitor.Root == null)
                {
                    monitor.Root = new PaneModel();
                }
                monitor.Root.Normalize();
                if (monitor.DeviceName == null)
                {
                    monitor.DeviceName = string.Empty;
                }
            }
        }

        [OnDeserializing]
        private void OnDeserializing(StreamingContext context)
        {
            SetDefaults();
        }

        private void SetDefaults()
        {
            SchemaVersion = CurrentSchemaVersion;
            Name = string.Empty;
            Monitors = new List<MonitorModel>();
        }
    }

    [DataContract]
    internal sealed class SessionState
    {
        public SessionState()
        {
            SetDefaults();
        }

        [DataMember(Order = 1)]
        public string LayoutName { get; set; }

        [DataMember(Order = 2)]
        public bool UiVisible { get; set; }

        [DataMember(Order = 3)]
        public LayoutModel Layout { get; set; }

        [DataMember(Order = 4)]
        public List<TabModel> ClosedTabs { get; set; }

        [DataMember(Order = 5)]
        public bool Locked { get; set; }

        [OnDeserializing]
        private void OnDeserializing(StreamingContext context)
        {
            SetDefaults();
        }

        private void SetDefaults()
        {
            LayoutName = string.Empty;
            UiVisible = true;
            ClosedTabs = new List<TabModel>();
        }
    }
}
