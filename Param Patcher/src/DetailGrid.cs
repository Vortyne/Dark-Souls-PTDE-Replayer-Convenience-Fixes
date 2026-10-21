// The table of values a mod changes: a sticky header, conflicts tinted red, applied rows dimmed.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using static ParamPatcher.Theme;

namespace ParamPatcher
{
    enum DetailKind { Conflict, WillChange, Applied, Note }

    class DetailRow
    {
        public string[] Cells;
        public DetailKind Kind;
    }

    class DetailGrid : DsScrollView
    {
        string[] columns = new string[0];
        List<DetailRow> rows = new List<DetailRow>();
        int[] widths = new int[0];

        int RowHeight => TextRenderer.MeasureText("Ag", F(10f)).Height + Si(10);
        int HeaderHeight => TextRenderer.MeasureText("Ag", F(10f, FontStyle.Bold)).Height + Si(14);
        protected override int ContentHeight => HeaderHeight + rows.Count * RowHeight + Si(6);

        public void SetData(string[] headers, List<DetailRow> data)
        {
            columns = headers;
            rows = data;
            ScrollTo(0);
            FitColumns();
            Invalidate();
        }

        protected override void OnResize(EventArgs e) { base.OnResize(e); FitColumns(); }

        /// <summary>Each column as wide as its widest text (sampled). When they don't fit, the
        /// least useful columns (last ones: Param, Row) are hidden first, then the widest shrink.</summary>
        void FitColumns()
        {
            int pad = Si(18);
            var w = new int[columns.Length];
            var cell = F(10f);
            for (int c = 0; c < columns.Length; c++)
            {
                w[c] = TextRenderer.MeasureText(columns[c], F(10f, FontStyle.Bold)).Width + pad;
                foreach (var row in rows.Take(400))
                    if (c < row.Cells.Length && row.Kind != DetailKind.Note)
                        w[c] = Math.Max(w[c], TextRenderer.MeasureText(row.Cells[c] ?? "", cell).Width + pad);
                w[c] = Math.Min(w[c], Si(300));
            }
            int available = ViewWidth - Si(4);
            const int alwaysShown = 6; // Name, Field, Original, Your file, Mod, State
            for (int c = w.Length - 1; c >= alwaysShown && w.Sum() > available; c--) w[c] = 0;
            int min = Si(64);
            while (w.Sum() > available)
            {
                int widest = Array.IndexOf(w, w.Max());
                if (w[widest] <= min) break;
                w[widest] = Math.Max(min, w[widest] - Math.Min(Si(10), w.Sum() - available));
            }
            // Spare room goes to the Name column.
            if (w.Length > 0 && w.Sum() < available) w[0] += available - w.Sum();
            widths = w;
        }

        protected override void PaintContent(Graphics g, Rectangle view)
        {
            int rh = RowHeight, y0 = HeaderHeight;
            int first = Math.Max(0, (view.Top - y0) / rh), last = Math.Min(rows.Count - 1, (view.Bottom - y0) / rh + 1);
            var font = F(10f);
            for (int i = first; i <= last; i++)
            {
                var row = rows[i];
                var r = new Rectangle(0, y0 + i * rh, ViewWidth, rh);
                if (row.Kind == DetailKind.Conflict) using (var b = new SolidBrush(Color.FromArgb(58, 22, 18))) g.FillRectangle(b, r);
                else if (i % 2 == 1) using (var b = new SolidBrush(Color.FromArgb(10, 255, 240, 210))) g.FillRectangle(b, r);
                if (row.Kind == DetailKind.Note)
                {
                    Text_(g, row.Cells[0], font, new Rectangle(Si(10), r.Y, ViewWidth - Si(20), rh), Muted, TextFormatFlags.VerticalCenter);
                    continue;
                }
                var color = row.Kind == DetailKind.Applied ? Faint : row.Kind == DetailKind.Conflict ? Color.FromArgb(240, 196, 184) : Theme.Text;
                int x = 0;
                for (int c = 0; c < widths.Length && c < row.Cells.Length; c++)
                {
                    if (widths[c] == 0) continue;
                    Text_(g, row.Cells[c] ?? "", font, new Rectangle(x + Si(9), r.Y, widths[c] - Si(14), rh), color,
                          TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
                    x += widths[c];
                }
            }
        }

        protected override void PaintOverlay(Graphics g)
        {
            if (columns.Length == 0) return;
            var head = new Rectangle(0, 0, ViewWidth, HeaderHeight);
            FillSurface(g, head, Theme.Card);
            int x = 0;
            int lastShown = Array.FindLastIndex(widths, x => x > 0);
            for (int c = 0; c < widths.Length; c++)
            {
                if (widths[c] == 0) continue;
                Text_(g, columns[c], F(10f, FontStyle.Bold), new Rectangle(x + Si(9), 0, widths[c] - Si(14), head.Height), Gold,
                      TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
                x += widths[c];
                if (c < lastShown)
                    using (var pen = new Pen(GoldFaint)) g.DrawLine(pen, x - 1, Si(6), x - 1, head.Height - Si(6));
            }
            Rule(g, 0, ViewWidth, head.Height - 1);
        }
    }
}
