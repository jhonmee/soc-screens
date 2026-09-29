// MuroSOC - LayoutPanel
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace MuroSoc
{
    internal sealed class PaneNode
    {
        public PaneNode()
        {
            Ratio = 0.5;
            Direction = PaneModel.Columns;
        }

        public PaneNode Parent { get; set; }

        public Cell Cell { get; set; }

        public string Direction { get; set; }

        public double Ratio { get; set; }

        public PaneNode First { get; set; }

        public PaneNode Second { get; set; }

        public Divider Divider { get; set; }

        public Rectangle Bounds { get; set; }

        public bool IsLeaf
        {
            get { return Cell != null; }
        }

        public Cell FirstCell()
        {
            PaneNode node = this;
            while (!node.IsLeaf)
            {
                node = node.First;
            }
            return node.Cell;
        }

        public void CollectCells(List<Cell> into)
        {
            if (IsLeaf)
            {
                into.Add(Cell);
                return;
            }
            First.CollectCells(into);
            Second.CollectCells(into);
        }
    }

    internal sealed class LayoutPanel : Panel
    {
        private PaneNode root;
        private Cell maximized;

        public LayoutPanel()
        {
            Dock = DockStyle.Fill;
            BackColor = Color.FromArgb(14, 14, 14);
        }

        public PaneNode Root
        {
            get { return root; }
        }

        public Cell MaximizedCell
        {
            get { return maximized; }
        }

        public List<Cell> Cells
        {
            get
            {
                List<Cell> cells = new List<Cell>();
                if (root != null)
                {
                    root.CollectCells(cells);
                }
                return cells;
            }
        }

        public void SetRoot(PaneNode node)
        {
            SuspendLayout();
            root = node;
            maximized = null;
            AddControls(node);
            ResumeLayout(true);
        }

        public void SetMaximized(Cell cell)
        {
            maximized = cell;
            PerformLayout();
        }

        public Cell Split(Cell cell, string direction)
        {
            PaneNode node = cell.Node;
            Cell added = new Cell(cell.ProfileName);
            PaneNode first = new PaneNode();
            first.Cell = cell;
            first.Parent = node;
            cell.Node = first;
            PaneNode second = new PaneNode();
            second.Cell = added;
            second.Parent = node;
            added.Node = second;
            node.Cell = null;
            node.First = first;
            node.Second = second;
            node.Direction = direction == PaneModel.Rows ? PaneModel.Rows : PaneModel.Columns;
            node.Ratio = 0.5;
            node.Divider = new Divider(this, node);
            SuspendLayout();
            Controls.Add(added);
            Controls.Add(node.Divider);
            ResumeLayout(true);
            return added;
        }

        public Cell RemoveCell(Cell cell)
        {
            PaneNode node = cell.Node;
            PaneNode parent = node == null ? null : node.Parent;
            if (parent == null)
            {
                return null;
            }
            PaneNode sibling = parent.First == node ? parent.Second : parent.First;
            if (maximized == cell)
            {
                maximized = null;
            }
            SuspendLayout();
            Controls.Remove(cell);
            if (parent.Divider != null)
            {
                Controls.Remove(parent.Divider);
                parent.Divider.Dispose();
            }
            parent.Cell = sibling.Cell;
            parent.First = sibling.First;
            parent.Second = sibling.Second;
            parent.Direction = sibling.Direction;
            parent.Ratio = sibling.Ratio;
            parent.Divider = sibling.Divider;
            if (parent.Divider != null)
            {
                parent.Divider.Node = parent;
            }
            if (parent.IsLeaf)
            {
                parent.Cell.Node = parent;
            }
            else
            {
                parent.First.Parent = parent;
                parent.Second.Parent = parent;
            }
            ResumeLayout(true);
            return parent.FirstCell();
        }

        public void RefreshChrome()
        {
            foreach (Control control in Controls)
            {
                Divider divider = control as Divider;
                if (divider != null)
                {
                    divider.UpdateStyle();
                }
            }
            PerformLayout();
        }

        protected override void OnLayout(LayoutEventArgs levent)
        {
            base.OnLayout(levent);
            if (root == null)
            {
                return;
            }
            if (maximized != null && !maximized.IsDisposed)
            {
                foreach (Control control in Controls)
                {
                    control.Visible = control == maximized;
                }
                maximized.Bounds = ClientRectangle;
                return;
            }
            Arrange(root, ClientRectangle);
        }

        private void AddControls(PaneNode node)
        {
            if (node.IsLeaf)
            {
                node.Cell.Node = node;
                Controls.Add(node.Cell);
                return;
            }
            node.First.Parent = node;
            node.Second.Parent = node;
            if (node.Divider == null)
            {
                node.Divider = new Divider(this, node);
            }
            Controls.Add(node.Divider);
            AddControls(node.First);
            AddControls(node.Second);
        }

        private void Arrange(PaneNode node, Rectangle rect)
        {
            node.Bounds = rect;
            if (node.IsLeaf)
            {
                node.Cell.Bounds = rect;
                node.Cell.Visible = true;
                return;
            }
            int thickness = Divider.Thickness(this);
            Rectangle first;
            Rectangle second;
            Rectangle split;
            if (node.Direction == PaneModel.Rows)
            {
                int available = Math.Max(0, rect.Height - thickness);
                int size = (int)Math.Round(available * node.Ratio);
                first = new Rectangle(rect.Left, rect.Top, rect.Width, size);
                split = new Rectangle(rect.Left, rect.Top + size, rect.Width, thickness);
                second = new Rectangle(rect.Left, rect.Top + size + thickness, rect.Width, available - size);
            }
            else
            {
                int available = Math.Max(0, rect.Width - thickness);
                int size = (int)Math.Round(available * node.Ratio);
                first = new Rectangle(rect.Left, rect.Top, size, rect.Height);
                split = new Rectangle(rect.Left + size, rect.Top, thickness, rect.Height);
                second = new Rectangle(rect.Left + size + thickness, rect.Top, available - size, rect.Height);
            }
            node.Divider.Bounds = split;
            node.Divider.Visible = true;
            node.Divider.BringToFront();
            Arrange(node.First, first);
            Arrange(node.Second, second);
        }
    }

    internal sealed class Divider : Control
    {
        private readonly LayoutPanel panel;
        private bool dragging;

        public Divider(LayoutPanel panel, PaneNode node)
        {
            this.panel = panel;
            Node = node;
            SetStyle(ControlStyles.Selectable, false);
            UpdateStyle();
        }

        public PaneNode Node { get; set; }

        public static int Thickness(Control reference)
        {
            if (Wall.EditMode)
            {
                return Dpi.Scale(reference, 8);
            }
            return Wall.UiVisible ? Dpi.Scale(reference, 4) : Math.Max(1, Dpi.Scale(reference, 2));
        }

        public void UpdateStyle()
        {
            BackColor = Wall.EditMode ? Cell.AccentColor : Color.FromArgb(14, 14, 14);
            Cursor = Wall.EditMode ? (Node.Direction == PaneModel.Rows ? Cursors.HSplit : Cursors.VSplit) : Cursors.Default;
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left && Wall.EditMode)
            {
                dragging = true;
                Capture = true;
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (!dragging)
            {
                return;
            }
            Point point = panel.PointToClient(Cursor.Position);
            Rectangle rect = Node.Bounds;
            double ratio;
            if (Node.Direction == PaneModel.Rows)
            {
                ratio = rect.Height <= 0 ? 0.5 : (point.Y - rect.Top) / (double)rect.Height;
            }
            else
            {
                ratio = rect.Width <= 0 ? 0.5 : (point.X - rect.Left) / (double)rect.Width;
            }
            ratio = Math.Max(0.05, Math.Min(0.95, ratio));
            if (Math.Abs(ratio - Node.Ratio) > 0.001)
            {
                Node.Ratio = ratio;
                panel.PerformLayout();
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (dragging)
            {
                dragging = false;
                Capture = false;
                Wall.MarkDirty();
            }
        }
    }
}
