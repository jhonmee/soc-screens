// MuroSOC - MenuEntry
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;

namespace MuroSoc
{
    internal sealed class MenuEntry
    {
        private MenuEntry()
        {
            Enabled = true;
            Children = new List<MenuEntry>();
        }

        public string Text { get; private set; }

        public MethodInvoker Action { get; private set; }

        public List<MenuEntry> Children { get; private set; }

        public bool IsSeparator { get; private set; }

        public bool Checked { get; set; }

        public bool Enabled { get; set; }

        public string Shortcut { get; set; }

        public static MenuEntry Item(string text, MethodInvoker action)
        {
            MenuEntry entry = new MenuEntry();
            entry.Text = text;
            entry.Action = action;
            return entry;
        }

        public static MenuEntry Item(string text, string shortcut, MethodInvoker action)
        {
            MenuEntry entry = Item(text, action);
            entry.Shortcut = shortcut;
            return entry;
        }

        public static MenuEntry Check(string text, bool isChecked, MethodInvoker action)
        {
            MenuEntry entry = Item(text, action);
            entry.Checked = isChecked;
            return entry;
        }

        public static MenuEntry Sub(string text, List<MenuEntry> children)
        {
            MenuEntry entry = new MenuEntry();
            entry.Text = text;
            entry.Children = children ?? new List<MenuEntry>();
            return entry;
        }

        public static MenuEntry Separator()
        {
            MenuEntry entry = new MenuEntry();
            entry.IsSeparator = true;
            entry.Text = string.Empty;
            return entry;
        }

        public static ContextMenuStrip ToContextMenu(List<MenuEntry> entries)
        {
            ContextMenuStrip menu = new ContextMenuStrip();
            AddTo(menu.Items, entries);
            return menu;
        }

        public static void ShowAt(Control control, Point location, List<MenuEntry> entries)
        {
            ContextMenuStrip menu = ToContextMenu(entries);
            menu.Closed += delegate { control.BeginInvoke((MethodInvoker)delegate { menu.Dispose(); }); };
            menu.Show(control, location);
        }

        public static void AddToWebView(CoreWebView2Environment environment, IList<CoreWebView2ContextMenuItem> target, List<MenuEntry> entries, Control invoker)
        {
            foreach (MenuEntry entry in entries)
            {
                if (entry.IsSeparator)
                {
                    target.Add(environment.CreateContextMenuItem(string.Empty, null, CoreWebView2ContextMenuItemKind.Separator));
                    continue;
                }
                if (entry.Children.Count > 0)
                {
                    CoreWebView2ContextMenuItem sub = environment.CreateContextMenuItem(entry.Text, null, CoreWebView2ContextMenuItemKind.Submenu);
                    AddToWebView(environment, sub.Children, entry.Children, invoker);
                    sub.IsEnabled = entry.Enabled;
                    target.Add(sub);
                    continue;
                }
                CoreWebView2ContextMenuItemKind kind = entry.Checked ? CoreWebView2ContextMenuItemKind.CheckBox : CoreWebView2ContextMenuItemKind.Command;
                CoreWebView2ContextMenuItem item = environment.CreateContextMenuItem(entry.Text, null, kind);
                item.IsEnabled = entry.Enabled && entry.Action != null;
                if (entry.Checked)
                {
                    item.IsChecked = true;
                }
                MethodInvoker action = entry.Action;
                if (action != null)
                {
                    item.CustomItemSelected += delegate
                    {
                        if (!invoker.IsDisposed)
                        {
                            invoker.BeginInvoke(action);
                        }
                    };
                }
                target.Add(item);
            }
        }

        private static void AddTo(ToolStripItemCollection items, List<MenuEntry> entries)
        {
            foreach (MenuEntry entry in entries)
            {
                if (entry.IsSeparator)
                {
                    items.Add(new ToolStripSeparator());
                    continue;
                }
                ToolStripMenuItem item = new ToolStripMenuItem(entry.Text);
                item.Enabled = entry.Enabled;
                item.Checked = entry.Checked;
                if (!string.IsNullOrEmpty(entry.Shortcut))
                {
                    item.ShortcutKeyDisplayString = entry.Shortcut;
                }
                if (entry.Children.Count > 0)
                {
                    AddTo(item.DropDownItems, entry.Children);
                }
                else if (entry.Action != null)
                {
                    MethodInvoker action = entry.Action;
                    item.Click += delegate { action(); };
                }
                else
                {
                    item.Enabled = false;
                }
                items.Add(item);
            }
        }
    }
}
