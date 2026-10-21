// The "Full List of Changes" window: shows a mod's CHANGES.md (headings, bullet points, paragraphs, tables).
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using static ParamPatcher.Theme;

namespace ParamPatcher
{
    /// <summary>Renders simple Markdown: "#" headings, "-"/"*" bullets, paragraphs and "|" tables.
    /// Bold markers are dropped; "<br>" in a table cell starts a new line.</summary>
    class MarkdownView : DsScrollView
    {
        enum Kind { Heading, Bullet, Text, Table }

        class Block
        {
            public Kind Kind;
            public int Level;
            public string Text;
            public Rectangle Bounds;
            // Tables
            public string[] Header;
            public List<string[]> Rows;
            public int[] Widths;
            public int HeaderHeight;
            public int[] RowHeights;
        }

        readonly List<Block> blocks = new List<Block>();
        int contentHeight;
        const TextFormatFlags Wrap = TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding;

        protected override int ContentHeight => contentHeight;

        static string Inline(string text) =>
            Regex.Replace(text, @"\*\*(.+?)\*\*|__(.+?)__", m => m.Groups[1].Value + m.Groups[2].Value)
                 .Replace("<br>", "\n").Replace("<br/>", "\n").Replace("<br />", "\n").Trim();

        static string[] Cells(string line)
        {
            string t = line.Trim();
            if (t.StartsWith("|")) t = t.Substring(1);
            if (t.EndsWith("|")) t = t.Substring(0, t.Length - 1);
            return t.Split('|').Select(Inline).ToArray();
        }

        static bool IsSeparator(string line) => Regex.IsMatch(line.Trim(), @"^\|?\s*:?-{3,}:?\s*(\|\s*:?-{3,}:?\s*)*\|?$");

        public void SetMarkdown(string markdown)
        {
            blocks.Clear();
            var lines = markdown.Replace("\r\n", "\n").Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].TrimEnd();
                if (line.Trim() == "") { blocks.Add(null); continue; } // paragraph break
                // A table: a "|" header line followed by a "|---|" separator line.
                if (line.TrimStart().StartsWith("|") && i + 1 < lines.Length && IsSeparator(lines[i + 1]))
                {
                    var table = new Block { Kind = Kind.Table, Header = Cells(line), Rows = new List<string[]>() };
                    i += 2;
                    for (; i < lines.Length && lines[i].TrimStart().StartsWith("|"); i++)
                    {
                        var cells = Cells(lines[i]);
                        Array.Resize(ref cells, table.Header.Length);
                        table.Rows.Add(cells.Select(c => c ?? "").ToArray());
                    }
                    i--;
                    blocks.Add(table);
                    continue;
                }
                string text = Inline(line);
                var h = Regex.Match(text, @"^(#{1,6})\s+(.*)$");
                var b = Regex.Match(text, @"^\s*[-*+]\s+(.*)$");
                if (h.Success) blocks.Add(new Block { Kind = Kind.Heading, Level = h.Groups[1].Length, Text = h.Groups[2].Value });
                else if (b.Success) blocks.Add(new Block { Kind = Kind.Bullet, Text = b.Groups[1].Value });
                else blocks.Add(new Block { Kind = Kind.Text, Text = text });
            }
            ScrollTo(0);
            Relayout();
        }

        Font FontOf(Block b) => b.Kind == Kind.Heading ? F(b.Level <= 1 ? 15f : 13f) : F(10.5f);
        static Font CellFont => F(10f);
        static Font HeaderFont => F(10f, FontStyle.Bold);
        int CellPadX => Si(10);
        int CellPadY => Si(6);

        /// <summary>Column widths: each column's natural (one-line) width; if the table is too wide,
        /// the widest columns give way first, never below a minimum, and their text wraps.</summary>
        void LayoutTable(Block t, int width)
        {
            int n = t.Header.Length;
            var natural = new int[n];
            for (int c = 0; c < n; c++)
            {
                natural[c] = TextRenderer.MeasureText(t.Header[c], HeaderFont, Size.Empty, TextFormatFlags.NoPrefix).Width;
                foreach (var row in t.Rows)
                    foreach (var part in row[c].Split('\n'))
                        natural[c] = Math.Max(natural[c], TextRenderer.MeasureText(part, CellFont, Size.Empty, TextFormatFlags.NoPrefix).Width);
                natural[c] += 2 * CellPadX;
            }
            var w = (int[])natural.Clone();
            int min = Si(90);
            while (w.Sum() > width)
            {
                int widest = Array.IndexOf(w, w.Max());
                if (w[widest] <= min) break;
                w[widest] = Math.Max(min, w[widest] - Math.Min(Si(8), w.Sum() - width));
            }
            t.Widths = w;
            int Height(string text, Font font, int colWidth) =>
                TextRenderer.MeasureText(text == "" ? "Ag" : text, font, new Size(colWidth - 2 * CellPadX, 0), Wrap).Height + 2 * CellPadY;
            t.HeaderHeight = Enumerable.Range(0, n).Max(c => Height(t.Header[c], HeaderFont, w[c]));
            t.RowHeights = t.Rows.Select(row => Enumerable.Range(0, n).Max(c => Height(row[c], CellFont, w[c]))).ToArray();
        }

        void Relayout()
        {
            int y = Si(6), left = Si(14), width = Math.Max(Si(200), ViewWidth - Si(30));
            Block previous = null;
            foreach (var b in blocks)
            {
                if (b == null) { y += Si(8); previous = null; continue; }
                if (b.Kind == Kind.Heading && previous != null) y += Si(10);
                if (b.Kind == Kind.Table)
                {
                    LayoutTable(b, width);
                    int h = b.HeaderHeight + b.RowHeights.Sum();
                    b.Bounds = new Rectangle(left, y, b.Widths.Sum(), h);
                    y += h + Si(6);
                    previous = b;
                    continue;
                }
                int indent = b.Kind == Kind.Bullet ? Si(22) : 0;
                int th = TextRenderer.MeasureText(b.Text, FontOf(b), new Size(width - indent, 0), Wrap).Height;
                b.Bounds = new Rectangle(left + indent, y, width - indent, th);
                y += th + (b.Kind == Kind.Heading ? Si(8) : Si(4));
                previous = b;
            }
            contentHeight = y + Si(10);
            ScrollTo(Offset);
            Invalidate();
        }

        protected override void OnResize(EventArgs e) { base.OnResize(e); Relayout(); }

        void PaintTable(Graphics g, Block t, Rectangle view)
        {
            var r = t.Bounds;
            FillSurface(g, new Rectangle(r.X, r.Y, r.Width, t.HeaderHeight), Theme.Card);
            int x = r.X;
            for (int c = 0; c < t.Header.Length; c++)
            {
                Text_(g, t.Header[c], HeaderFont, new Rectangle(x + CellPadX, r.Y + CellPadY, t.Widths[c] - 2 * CellPadX, t.HeaderHeight - 2 * CellPadY), Gold, TextFormatFlags.WordBreak);
                x += t.Widths[c];
            }
            int y = r.Y + t.HeaderHeight;
            using (var stripe = new SolidBrush(Color.FromArgb(10, 255, 240, 210)))
            using (var line = new Pen(GoldFaint))
            {
                for (int i = 0; i < t.Rows.Count; i++)
                {
                    int h = t.RowHeights[i];
                    if (y + h >= view.Top && y <= view.Bottom)
                    {
                        if (i % 2 == 1) g.FillRectangle(stripe, r.X, y, r.Width, h);
                        x = r.X;
                        for (int c = 0; c < t.Header.Length; c++)
                        {
                            // The first column names the thing; the rest describe it.
                            var color = c == 0 ? Theme.Text : Color.FromArgb(214, 206, 188);
                            Text_(g, t.Rows[i][c], CellFont, new Rectangle(x + CellPadX, y + CellPadY, t.Widths[c] - 2 * CellPadX, h - 2 * CellPadY), color, TextFormatFlags.WordBreak);
                            x += t.Widths[c];
                        }
                        g.DrawLine(line, r.X, y, r.Right - 1, y);
                    }
                    y += h;
                }
                // Column dividers and the outer frame.
                x = r.X;
                for (int c = 0; c < t.Widths.Length - 1; c++) { x += t.Widths[c]; g.DrawLine(line, x, r.Y, x, r.Bottom - 1); }
                g.DrawRectangle(line, r.X, r.Y, r.Width - 1, r.Height - 1);
            }
            Rule(g, r.X, r.Right, r.Y + t.HeaderHeight - 1);
        }

        protected override void PaintContent(Graphics g, Rectangle view)
        {
            using (var gold = new SolidBrush(Gold))
                foreach (var b in blocks.Where(b => b != null && b.Bounds.IntersectsWith(view)))
                {
                    if (b.Kind == Kind.Table) { PaintTable(g, b, view); continue; }
                    var color = b.Kind == Kind.Heading ? Gold : Theme.Text;
                    Text_(g, b.Text, FontOf(b), b.Bounds, color, TextFormatFlags.WordBreak);
                    if (b.Kind == Kind.Bullet)
                    {
                        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                        int lineHeight = TextRenderer.MeasureText("Ag", FontOf(b)).Height;
                        Diamond(g, gold, new PointF(b.Bounds.X - S(12), b.Bounds.Y + lineHeight / 2f), S(3.5f));
                    }
                    if (b.Kind == Kind.Heading && b.Level <= 2)
                        Rule(g, b.Bounds.X, b.Bounds.X + Math.Min(b.Bounds.Width, Si(420)), b.Bounds.Bottom + Si(3));
                }
        }
    }

    /// <summary>A borderless, draggable window showing one mod's change list.</summary>
    class ChangesDialog : Form
    {
        [DllImport("user32.dll")] static extern bool ReleaseCapture();
        [DllImport("user32.dll")] static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        ChangesDialog(string title, string markdown, Size size)
        {
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterParent;
            ShowInTaskbar = false;
            KeyPreview = true;
            BackColor = Theme.Window;
            Size = size;
            Padding = new Padding(Si(24), Si(18), Si(16), Si(18));

            var layout = new ClearTable { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4, AutoSize = false };
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            var heading = new Label
            {
                Text = title, AutoSize = true, Font = F(16f), ForeColor = Theme.Text, BackColor = Color.Transparent,
                MaximumSize = new Size(size.Width - Si(60), 0), Margin = new Padding(0), UseMnemonic = false,
            };
            var rule = new Panel { Height = Si(14), Dock = DockStyle.Fill, BackColor = Color.Transparent, Margin = new Padding(0) };
            rule.Paint += (s, e) => Rule(e.Graphics, 0, rule.Width, Si(7));
            var view = new MarkdownView { Dock = DockStyle.Fill, Margin = new Padding(0, Si(4), 0, Si(12)) };
            view.SetMarkdown(markdown);
            var close = new DsButton(Strings.T("tip.close")) { Primary = true, Font = F(10.5f, FontStyle.Bold), MinimumSize = new Size(Si(120), 0), Anchor = AnchorStyles.Right, Margin = new Padding(0, 0, Si(8), 0) };
            close.Click += (s, e) => Close();
            layout.Controls.Add(heading, 0, 0);
            layout.Controls.Add(rule, 0, 1);
            layout.Controls.Add(view, 0, 2);
            layout.Controls.Add(close, 0, 3);
            Controls.Add(layout);

            KeyDown += (s, e) => { if (e.KeyCode == Keys.Escape) Close(); };
            // Drag the window by its title.
            MouseEventHandler drag = (s, e) =>
            {
                if (e.Button != MouseButtons.Left) return;
                ReleaseCapture();
                SendMessage(Handle, 0xA1 /* WM_NCLBUTTONDOWN */, (IntPtr)2 /* HTCAPTION */, IntPtr.Zero);
            };
            heading.MouseDown += drag;
            MouseDown += drag;
            Shown += (s, e) => view.Focus();
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            FillSurface(e.Graphics, ClientRectangle, Theme.Window);
            Frame(e.Graphics, ClientRectangle);
        }

        /// <summary>Show `info`'s change list (in the current language when the mod has a translation).</summary>
        public static void ShowFor(Form owner, ModInfo info)
        {
            string path = info.ChangesPath(Strings.Code);
            string text;
            try { text = File.ReadAllText(path); }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException) { text = e.Message; }
            var size = new Size(Math.Min(Si(900), (int)(owner.Width * 0.75)), Math.Min(Si(820), (int)(owner.Height * 0.85)));
            using (var d = new ChangesDialog(info.DisplayName(Strings.Code), text, size)) d.ShowDialog(owner);
        }
    }
}
