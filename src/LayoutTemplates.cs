// MuroSOC - LayoutTemplates
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace MuroSoc
{
    internal sealed class LayoutTemplate
    {
        public LayoutTemplate(string name, PaneModel shape)
        {
            Name = name;
            Shape = shape;
        }

        public string Name { get; private set; }

        public PaneModel Shape { get; private set; }

        public int CellCount
        {
            get
            {
                List<PaneModel> cells = new List<PaneModel>();
                Shape.CollectCells(cells);
                return cells.Count;
            }
        }
    }

    internal static class LayoutTemplates
    {
        public static List<LayoutTemplate> All()
        {
            List<LayoutTemplate> list = new List<LayoutTemplate>();
            list.Add(new LayoutTemplate("1 celda", Cell()));
            list.Add(new LayoutTemplate("2 columnas", Line(PaneModel.Columns, 2)));
            list.Add(new LayoutTemplate("2 filas", Line(PaneModel.Rows, 2)));
            list.Add(new LayoutTemplate("3 columnas", Line(PaneModel.Columns, 3)));
            list.Add(new LayoutTemplate("3 filas", Line(PaneModel.Rows, 3)));
            list.Add(new LayoutTemplate("2 × 2", Grid(2, 2)));
            list.Add(new LayoutTemplate("3 × 2", Grid(3, 2)));
            list.Add(new LayoutTemplate("2 × 3", Grid(2, 3)));
            list.Add(new LayoutTemplate("3 × 3", Grid(3, 3)));
            list.Add(new LayoutTemplate("4 × 2", Grid(4, 2)));
            list.Add(new LayoutTemplate("4 × 3", Grid(4, 3)));
            list.Add(new LayoutTemplate("4 × 4", Grid(4, 4)));
            list.Add(new LayoutTemplate("Grande + 2 a la derecha", Split(PaneModel.Columns, 0.62, Cell(), Line(PaneModel.Rows, 2))));
            list.Add(new LayoutTemplate("2 a la izquierda + grande", Split(PaneModel.Columns, 0.38, Line(PaneModel.Rows, 2), Cell())));
            list.Add(new LayoutTemplate("Grande + 3 a la derecha", Split(PaneModel.Columns, 0.62, Cell(), Line(PaneModel.Rows, 3))));
            list.Add(new LayoutTemplate("Grande arriba + 2 abajo", Split(PaneModel.Rows, 0.62, Cell(), Line(PaneModel.Columns, 2))));
            list.Add(new LayoutTemplate("Grande arriba + 3 abajo", Split(PaneModel.Rows, 0.62, Cell(), Line(PaneModel.Columns, 3))));
            list.Add(new LayoutTemplate("Grande al centro + 4", Split(PaneModel.Columns, 0.22, Line(PaneModel.Rows, 2), Split(PaneModel.Columns, 0.72, Cell(), Line(PaneModel.Rows, 2)))));
            return list;
        }

        public static List<List<T>> Distribute<T>(List<List<T>> groups, int cellCount)
        {
            List<List<T>> result = new List<List<T>>();
            foreach (List<T> group in groups)
            {
                if (group.Count > 0)
                {
                    result.Add(new List<T>(group));
                }
            }
            while (result.Count < cellCount)
            {
                int largest = -1;
                for (int i = 0; i < result.Count; i++)
                {
                    if (result[i].Count > 1 && (largest < 0 || result[i].Count > result[largest].Count))
                    {
                        largest = i;
                    }
                }
                if (largest < 0)
                {
                    break;
                }
                List<T> source = result[largest];
                T moved = source[source.Count - 1];
                source.RemoveAt(source.Count - 1);
                List<T> single = new List<T>();
                single.Add(moved);
                result.Insert(largest + 1, single);
            }
            while (result.Count > cellCount && result.Count > 1)
            {
                List<T> last = result[result.Count - 1];
                result.RemoveAt(result.Count - 1);
                result[result.Count - 1].AddRange(last);
            }
            return result;
        }

        public static void Draw(Graphics g, Rectangle bounds, PaneModel shape, IList<string> labels, Font font, bool highlight)
        {
            g.SmoothingMode = SmoothingMode.None;
            int index = 0;
            DrawNode(g, bounds, shape, labels, font, highlight, ref index);
        }

        private static void DrawNode(Graphics g, Rectangle rect, PaneModel node, IList<string> labels, Font font, bool highlight, ref int index)
        {
            int gap = Math.Max(2, rect.Width / 90);
            if (node.IsSplit)
            {
                Rectangle first;
                Rectangle second;
                if (node.Direction == PaneModel.Rows)
                {
                    int size = (int)Math.Round((rect.Height - gap) * node.Ratio);
                    first = new Rectangle(rect.Left, rect.Top, rect.Width, size);
                    second = new Rectangle(rect.Left, rect.Top + size + gap, rect.Width, rect.Height - size - gap);
                }
                else
                {
                    int size = (int)Math.Round((rect.Width - gap) * node.Ratio);
                    first = new Rectangle(rect.Left, rect.Top, size, rect.Height);
                    second = new Rectangle(rect.Left + size + gap, rect.Top, rect.Width - size - gap, rect.Height);
                }
                DrawNode(g, first, node.First, labels, font, highlight, ref index);
                DrawNode(g, second, node.Second, labels, font, highlight, ref index);
                return;
            }
            Color fill = highlight ? Color.FromArgb(96, 120, 150) : Color.FromArgb(110, 110, 110);
            using (SolidBrush brush = new SolidBrush(fill))
            {
                g.FillRectangle(brush, rect);
            }
            using (Pen pen = new Pen(highlight ? Color.FromArgb(150, 190, 235) : Color.FromArgb(150, 150, 150)))
            {
                g.DrawRectangle(pen, rect.Left, rect.Top, Math.Max(0, rect.Width - 1), Math.Max(0, rect.Height - 1));
            }
            index++;
            if (font != null && rect.Width > 24 && rect.Height > 16)
            {
                string text = index.ToString();
                if (labels != null && index - 1 < labels.Count && !string.IsNullOrEmpty(labels[index - 1]))
                {
                    text += "\r\n" + labels[index - 1];
                }
                Rectangle inner = Rectangle.Inflate(rect, -4, -4);
                TextRenderer.DrawText(g, text, font, inner, Color.White,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.WordBreak | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
            }
        }

        private static PaneModel Cell()
        {
            return PaneModel.NewCell(null);
        }

        private static PaneModel Split(string direction, double ratio, PaneModel first, PaneModel second)
        {
            PaneModel split = new PaneModel();
            split.Type = PaneModel.SplitType;
            split.Direction = direction;
            split.Ratio = ratio;
            split.First = first;
            split.Second = second;
            return split;
        }

        private static PaneModel Line(string direction, int count)
        {
            return Repeat(direction, count, delegate { return Cell(); });
        }

        private static PaneModel Grid(int columns, int rows)
        {
            return Repeat(PaneModel.Rows, rows, delegate { return Line(PaneModel.Columns, columns); });
        }

        private static PaneModel Repeat(string direction, int count, Func<PaneModel> make)
        {
            if (count <= 1)
            {
                return make();
            }
            return Split(direction, 1.0 / count, make(), Repeat(direction, count - 1, make));
        }
    }
}
