// The list of mods: each mod is a gold-framed card with a header checkbox, each CSV a row in it.
// The focused entry gets the rust highlight of a selected DS1 save slot.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using static ParamPatcher.Theme;

namespace ParamPatcher
{
    static class UiText
    {
        public static string Status(Status s)
        {
            switch (s)
            {
                case ParamPatcher.Status.NotApplied: return Strings.T("status.notApplied");
                case ParamPatcher.Status.Partial: return Strings.T("status.partial");
                case ParamPatcher.Status.Applied: return Strings.T("status.applied");
                case ParamPatcher.Status.Conflict: return Strings.T("status.conflict");
                case ParamPatcher.Status.NoEffect: return Strings.T("status.noEffect");
                default: return Strings.T("status.error");
            }
        }

        /// <summary>A patch's name in the list: its param, or "Game files" for a mod's files.</summary>
        public static string PatchName(ModCsv csv) => csv.FilesOnly ? Strings.T("files.row") : csv.Param;

        /// <summary>"Values: n · Applied: n · Conflicts: n" (conflicts only when there are some).</summary>
        public static string Summary(Report r)
        {
            if (r.Error != null) return r.Error;
            if (r.FilesOnly)
                return r.MissingFiles == 0 ? Strings.T("files.installed") : Strings.T("files.missing", r.MissingFiles);
            var parts = new List<string> { Strings.T("n.values", r.Cells.Count), Strings.T("n.applied", r.AppliedCount) };
            int conflicts = r.Conflicts.Count;
            if (conflicts > 0) parts.Add(Strings.T("n.conflicts", conflicts));
            return string.Join("   ·   ", parts);
        }
    }

    class ModList : DsScrollView
    {
        class Item
        {
            public bool IsHeader;
            public string Mod;
            public List<int> Reports; // header: all its CSVs; row: just one
            public List<int> Toggles; // what its checkbox ticks: a row in a "requireAll" mod ticks the whole mod
            public Rectangle Bounds;
            public Rectangle Card;    // header only: the whole card
            public int TitleHeight;   // header only
            public int TagWidth;      // width reserved for the status tag
        }

        readonly List<Item> items = new List<Item>();
        List<Report> reports = new List<Report>();
        Item hover, focused;
        int contentHeight;

        public readonly HashSet<int> Checked = new HashSet<int>();
        public event EventHandler CheckedChanged;
        /// <summary>Raised when the user picks a mod or CSV; carries the report indices to show.</summary>
        public event Action<List<int>> FocusChanged;
        public string EmptyText = "";
        /// <summary>For mods that install game files: how many aren't in the game folder yet.</summary>
        public Dictionary<ModInfo, int> MissingFiles = new Dictionary<ModInfo, int>();

        protected override int ContentHeight => contentHeight;

        public void SetReports(List<Report> list)
        {
            reports = list ?? new List<Report>();
            Checked.Clear();
            focused = hover = null;
            ScrollTo(0);
            Relayout();
        }

        public void SetChecked(IEnumerable<int> indices)
        {
            Checked.Clear();
            foreach (var i in indices) if (i < reports.Count && reports[i].Selectable) Checked.Add(i);
            // Mods whose files are applied together are ticked as a whole.
            foreach (var i in Checked.ToList())
                for (int j = 0; j < reports.Count; j++)
                    if (reports[i].Csv.Info.RequireAll && reports[j].Csv.Info == reports[i].Csv.Info && reports[j].Selectable) Checked.Add(j);
            Invalidate();
            CheckedChanged?.Invoke(this, EventArgs.Empty);
        }

        public void Relayout()
        {
            items.Clear();
            int y = Si(10);
            int left = Si(10), width = Math.Max(Si(300), ViewWidth - Si(16));
            foreach (var group in reports.Select((r, i) => (r, i)).GroupBy(t => t.r.Csv.Info))
            {
                var indices = group.Select(t => t.i).ToList();
                int cardTop = y;
                int tagWidth = TagWidth(Aggregate(indices.Select(i => reports[i])));
                // Long mod names wrap onto extra lines instead of being cut off.
                string modName = group.Key.DisplayName(Strings.Code);
                int titleHeight = TextRenderer.MeasureText(modName, F(13.5f), new Size(TitleWidth(width, tagWidth), 0),
                    TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding).Height;
                int headerHeight = Si(14) + titleHeight + Si(26) + Si(10);
                var header = new Item { IsHeader = true, Mod = modName, Reports = indices, Toggles = indices, TitleHeight = titleHeight, TagWidth = tagWidth, Bounds = new Rectangle(left, y, width, headerHeight) };
                items.Add(header);
                y += headerHeight;
                int rowHeight = Si(14) + TextRenderer.MeasureText("Ag", F(11.5f)).Height + TextRenderer.MeasureText("Ag", F(9.5f)).Height + Si(12);
                foreach (var i in indices)
                {
                    items.Add(new Item { Mod = modName, Reports = new List<int> { i }, Toggles = group.Key.RequireAll ? indices : new List<int> { i }, Bounds = new Rectangle(left, y, width, rowHeight) });
                    y += rowHeight;
                }
                y += Si(10);
                header.Card = new Rectangle(left, cardTop, width, y - cardTop);
                y += Si(14);
            }
            contentHeight = y;
            ScrollTo(Offset);
            Invalidate();
        }

        // Header layout: [18 pad][26 box][16 gap] title [24 gap][status tag][18 pad]
        int TitleWidth(int cardWidth, int tagWidth) => cardWidth - Si(18 + 26 + 16 + 24 + 18) - tagWidth;

        static int TagWidth(Status s) =>
            TextRenderer.MeasureText(UiText.Status(s), F(9.5f, FontStyle.Bold), Size.Empty, TextFormatFlags.NoPadding).Width + Si(18);

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            Relayout();
        }

        Item HitTest(Point p) => InScrollBar(p) ? null : items.FirstOrDefault(it => it.Bounds.Contains(p.X, p.Y + Offset));

        int StateOf(Item it)
        {
            var usable = it.Toggles.Where(i => reports[i].Selectable).ToList();
            int on = usable.Count(i => Checked.Contains(i));
            if (on == 0) return 0;
            return on == usable.Count ? 1 : 2;
        }

        void Toggle(Item it)
        {
            var usable = it.Toggles.Where(i => reports[i].Selectable).ToList();
            if (usable.Count == 0) return;
            bool turnOn = StateOf(it) != 1;
            foreach (var i in usable)
                if (turnOn) Checked.Add(i); else Checked.Remove(i);
            Invalidate();
            CheckedChanged?.Invoke(this, EventArgs.Empty);
        }

        void SetFocus(Item it)
        {
            focused = it;
            Invalidate();
            if (it == null) return;
            FocusChanged?.Invoke(it.Reports);
            EnsureVisible(it.Bounds.Top, it.Bounds.Bottom);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            var it = HitTest(e.Location);
            if (it != hover) { hover = it; Invalidate(); }
            Cursor = it != null ? Cursors.Hand : Cursors.Default;
        }

        protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); hover = null; Invalidate(); }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            var it = HitTest(e.Location);
            if (it == null || e.Button != MouseButtons.Left || !Enabled) return;
            // Clicking the row shows what it changes; only the checkbox ticks it.
            var box = BoxRect(it);
            box.Inflate(Si(8), Si(8));
            if (box.Contains(e.X, e.Y + Offset)) Toggle(it);
            SetFocus(it);
        }

        /// <summary>Where an item's checkbox is drawn (headers have a larger one, rows are indented).</summary>
        RectangleF BoxRect(Item it)
        {
            var r = it.Bounds;
            int box = it.IsHeader ? Si(26) : Si(22);
            return new RectangleF(r.X + (it.IsHeader ? Si(18) : Si(62)), r.Y + (r.Height - box) / 2f, box, box);
        }

        protected override bool IsInputKey(Keys k) => k == Keys.Up || k == Keys.Down || k == Keys.Space || base.IsInputKey(k);

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (items.Count == 0) return;
            int idx = focused == null ? -1 : items.IndexOf(focused);
            if (e.KeyCode == Keys.Down) SetFocus(items[Math.Min(items.Count - 1, idx + 1)]);
            else if (e.KeyCode == Keys.Up) SetFocus(items[Math.Max(0, idx - 1)]);
            else if (e.KeyCode == Keys.Space && focused != null) Toggle(focused);
            else return;
            e.Handled = true;
        }

        protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Invalidate(); }
        protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); Invalidate(); }
        protected override void OnEnabledChanged(EventArgs e) { base.OnEnabledChanged(e); Invalidate(); }

        static Status Aggregate(IEnumerable<Report> rs)
        {
            var s = rs.Select(r => r.Status).ToList();
            if (s.Contains(Status.Error)) return Status.Error;
            if (s.Contains(Status.Conflict)) return Status.Conflict;
            if (s.All(x => x == Status.Applied || x == Status.NoEffect)) return Status.Applied;
            if (s.All(x => x == Status.NotApplied || x == Status.NoEffect)) return Status.NotApplied;
            return Status.Partial;
        }

        protected override void PaintContent(Graphics g, Rectangle view)
        {
            if (items.Count == 0)
            {
                Text_(g, EmptyText, F(11f), new Rectangle(Si(24), Si(24), ViewWidth - Si(48), Si(400)), Muted, TextFormatFlags.WordBreak);
                return;
            }
            foreach (var it in items)
            {
                if (it.IsHeader && it.Card.IntersectsWith(view))
                {
                    FillSurface(g, it.Card, Theme.Card);
                    Frame(g, it.Card);
                }
                if (!it.Bounds.IntersectsWith(view)) continue;
                var r = it.Bounds;
                bool enabled = Enabled && it.Reports.Any(i => reports[i].Selectable);
                var inner = new Rectangle(r.X + Si(7), r.Y + Si(4), r.Width - Si(14), r.Height - Si(8));
                if (it == focused) Highlight(g, inner);
                else if (it == hover) using (var b = new SolidBrush(Theme.Hover)) g.FillRectangle(b, inner);

                if (it.IsHeader)
                {
                    var boxRect = BoxRect(it);
                    CheckBox(g, boxRect, StateOf(it), enabled);
                    var rs = it.Reports.Select(i => reports[i]).ToList();
                    var status = Aggregate(rs);
                    DrawPill(g, UiText.Status(status), status, F(9.5f, FontStyle.Bold), r.Right - Si(18), r.Y + r.Height / 2f);
                    int on = it.Reports.Count(i => Checked.Contains(i));
                    string count = Strings.T("n.files", rs.Count) + (on > 0 ? "   ·   " + Strings.T("n.selected", on) : "")
                                   + (rs[0].Csv.Info.RequireAll && rs.Count > 1 ? "   ·   " + Strings.T("mod.together") : "")
                                   + (rs[0].Csv.Info.AdditionalFiles.IsEmpty ? "" : "   ·   " + Strings.T("mod.extra"))
                                   + (MissingFiles.TryGetValue(rs[0].Csv.Info, out int missing)
                                       ? "   ·   " + (missing == 0 ? Strings.T("files.installed") : Strings.T("files.missing", missing)) : "");
                    int tx = (int)boxRect.Right + Si(16);
                    Text_(g, it.Mod, F(13.5f), new Rectangle(tx, r.Y + Si(12), TitleWidth(r.Width, it.TagWidth), it.TitleHeight + Si(2)), Theme.Text, TextFormatFlags.WordBreak);
                    Text_(g, count, F(9.5f), new Rectangle(tx, r.Y + Si(16) + it.TitleHeight, TitleWidth(r.Width, it.TagWidth), Si(24)), it == focused ? Theme.Text : Muted);
                    Rule(g, r.X + Si(14), r.Right - Si(14), r.Bottom - 1);
                }
                else
                {
                    var rep = reports[it.Reports[0]];
                    var boxRect = BoxRect(it);
                    CheckBox(g, boxRect, StateOf(it), enabled);
                    DrawPill(g, UiText.Status(rep.Status), rep.Status, F(9.5f, FontStyle.Bold), r.Right - Si(18), r.Y + r.Height / 2f);
                    int tx = (int)boxRect.Right + Si(16);
                    int width = r.Right - Si(18 + 24) - TagWidth(rep.Status) - tx;
                    int nameHeight = TextRenderer.MeasureText("Ag", F(11.5f)).Height;
                    Text_(g, UiText.PatchName(rep.Csv), F(11.5f), new Rectangle(tx, r.Y + Si(9), width, nameHeight), Theme.Text, TextFormatFlags.EndEllipsis);
                    Text_(g, UiText.Summary(rep), F(9.5f), new Rectangle(tx, r.Y + Si(11) + nameHeight, width, Si(24)),
                          it == focused ? Theme.Text : Muted, TextFormatFlags.EndEllipsis);
                }
            }
        }
    }
}
