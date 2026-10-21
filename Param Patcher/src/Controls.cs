// Themed building blocks: textured panels, buttons, checkbox, text box, scroll view,
// the custom title bar and a dialog that replaces the Windows message box.
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using static ParamPatcher.Theme;

namespace ParamPatcher
{
    /// <summary>A grainy dark surface, optionally framed in gold. Children should use a transparent BackColor.</summary>
    class DsPanel : Panel
    {
        public Color Surface = Theme.Panel;
        public bool Framed;

        public DsPanel()
        {
            DoubleBuffered = true;
            ResizeRedraw = true;
            BackColor = Theme.Panel;
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            FillSurface(e.Graphics, ClientRectangle, Surface);
            if (Framed) Frame(e.Graphics, ClientRectangle);
        }
    }

    /// <summary>A table layout that paints nothing itself, so the textured parent shows through.</summary>
    class ClearTable : TableLayoutPanel
    {
        public ClearTable()
        {
            BackColor = Color.Transparent;
            Margin = new Padding(0);
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
        }
    }

    class DsButton : Control
    {
        public bool Primary;   // rust highlight, for the main action
        bool toggled, hover, pressed;

        /// <summary>Shows as selected (rust highlight), for toggle groups like the game choice.</summary>
        public bool Toggled { get => toggled; set { toggled = value; Invalidate(); } }

        public DsButton(string text = "")
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint
                     | ControlStyles.ResizeRedraw | ControlStyles.Selectable, true);
            Text = text;
            Cursor = Cursors.Hand;
            Margin = new Padding(Si(8), 0, 0, 0);
            TabStop = true;
        }

        public override Size GetPreferredSize(Size proposed)
        {
            var t = TextRenderer.MeasureText(Text, Font, Size.Empty, TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding);
            return new Size(t.Width + Si(36), Math.Max(Si(38), t.Height + Si(18)));
        }

        void Fit() { if (Parent != null) Size = GetPreferredSize(Size.Empty); }
        protected override void OnTextChanged(EventArgs e) { base.OnTextChanged(e); Fit(); Invalidate(); }
        protected override void OnFontChanged(EventArgs e) { base.OnFontChanged(e); Fit(); }
        protected override void OnParentChanged(EventArgs e) { base.OnParentChanged(e); Fit(); }
        protected override void OnEnabledChanged(EventArgs e) { base.OnEnabledChanged(e); Invalidate(); }
        protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); hover = true; Invalidate(); }
        protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); hover = pressed = false; Invalidate(); }
        protected override void OnMouseDown(MouseEventArgs e) { base.OnMouseDown(e); if (e.Button == MouseButtons.Left) { pressed = true; Focus(); Invalidate(); } }
        protected override void OnMouseUp(MouseEventArgs e) { base.OnMouseUp(e); pressed = false; Invalidate(); }
        protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Invalidate(); }
        protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); Invalidate(); }
        protected override bool IsInputKey(Keys k) => k == Keys.Enter || k == Keys.Space || base.IsInputKey(k);
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Space) { OnClick(EventArgs.Empty); e.Handled = true; }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            var r = ClientRectangle;
            FillSurface(g, r, Theme.Card);
            if (!Enabled)
            {
                Frame(g, r, corners: false, alpha: 0.5f);
                Text_(g, Text, Font, r, Faint, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                return;
            }
            if (Primary || toggled)
            {
                Highlight(g, new RectangleF(r.X, r.Y, r.Width, r.Height));
                if (hover) using (var b = new SolidBrush(Color.FromArgb(pressed ? 40 : 22, Color.White))) g.FillRectangle(b, r);
            }
            else
            {
                if (hover) using (var b = new SolidBrush(pressed ? Theme.Rust : Theme.Hover)) g.FillRectangle(b, r);
                Frame(g, r, corners: hover);
            }
            var color = Primary || toggled || hover ? Theme.Text : Color.FromArgb(206, 197, 178);
            Text_(g, Text, Font, r, color, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            if (Focused && ShowFocusCues)
                using (var pen = new Pen(Color.FromArgb(160, Gold)) { DashStyle = DashStyle.Dot })
                    g.DrawRectangle(pen, Si(5), Si(5), Width - Si(10) - 1, Height - Si(10) - 1);
        }
    }

    /// <summary>A checkbox drawn at the same large size as the ones in the mod list.</summary>
    class DsCheckBox : Control
    {
        bool isChecked, hover;

        public DsCheckBox()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint
                     | ControlStyles.SupportsTransparentBackColor | ControlStyles.Selectable, true);
            BackColor = Color.Transparent;
            Cursor = Cursors.Hand;
            TabStop = true;
        }

        public bool Checked { get => isChecked; set { isChecked = value; Invalidate(); } }

        public override Size GetPreferredSize(Size proposed)
        {
            var t = TextRenderer.MeasureText(Text, Font);
            return new Size(Si(34) + t.Width, Math.Max(Si(32), t.Height + Si(6)));
        }

        void Fit() => Size = GetPreferredSize(Size.Empty);
        protected override void OnTextChanged(EventArgs e) { base.OnTextChanged(e); Fit(); Invalidate(); }
        protected override void OnFontChanged(EventArgs e) { base.OnFontChanged(e); Fit(); }
        protected override void OnEnabledChanged(EventArgs e) { base.OnEnabledChanged(e); Invalidate(); }
        protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); hover = true; Invalidate(); }
        protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); hover = false; Invalidate(); }
        protected override void OnMouseDown(MouseEventArgs e) { base.OnMouseDown(e); Focus(); if (e.Button == MouseButtons.Left) Checked = !Checked; }
        protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Invalidate(); }
        protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); Invalidate(); }
        protected override bool IsInputKey(Keys k) => k == Keys.Space || base.IsInputKey(k);
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.KeyCode == Keys.Space) { Checked = !Checked; e.Handled = true; }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            float box = S(22);
            var rect = new RectangleF(S(2), (Height - box) / 2, box, box);
            CheckBox(g, rect, isChecked ? 1 : 0, Enabled);
            if (hover && Enabled && !isChecked)
                using (var pen = new Pen(Gold, S(1.5f))) g.DrawRectangle(pen, rect.X, rect.Y, rect.Width, rect.Height);
            var textRect = new Rectangle(Si(34), 0, Width - Si(34), Height);
            Text_(g, Text, Font, textRect, Enabled ? (hover ? Theme.Text : Muted) : Faint, TextFormatFlags.VerticalCenter);
            if (Focused && ShowFocusCues)
                using (var pen = new Pen(Color.FromArgb(160, Gold)) { DashStyle = DashStyle.Dot })
                    g.DrawRectangle(pen, textRect.X - 2, 2, textRect.Width - 1, Height - 5);
        }
    }

    /// <summary>A dark, gold-framed single-line text box.</summary>
    class DsTextBox : DsPanel
    {
        public readonly TextBox Box = new TextBox();

        public DsTextBox()
        {
            Surface = Theme.Input;
            Framed = true;
            Box.BorderStyle = BorderStyle.None;
            Box.BackColor = Theme.Input;
            Box.ForeColor = Theme.Text;
            Box.Dock = DockStyle.Fill;
            Controls.Add(Box);
            Padding = new Padding(Si(10), Si(9), Si(10), Si(6));
            Box.FontChanged += (s, e) => Height = Box.PreferredHeight + Padding.Vertical;
        }

        public override string Text { get => Box.Text; set => Box.Text = value; }
    }

    /// <summary>A control with vertical scrolling and a slim DS1-style scrollbar (gold thumb, arrow tips).</summary>
    abstract class DsScrollView : Control
    {
        int offset;
        bool dragging;
        int dragStartY, dragStartOffset;

        protected DsScrollView()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint
                     | ControlStyles.ResizeRedraw | ControlStyles.Selectable, true);
            TabStop = true;
        }

        protected abstract int ContentHeight { get; }
        protected abstract void PaintContent(Graphics g, Rectangle view);
        /// <summary>Drawn on top, unscrolled (e.g. a sticky header).</summary>
        protected virtual void PaintOverlay(Graphics g) { }
        protected int Offset => offset;
        protected int BarWidth => Si(16);
        protected int ViewWidth => Width - BarWidth;
        bool CanScroll => ContentHeight > Height;

        public void ScrollTo(int y)
        {
            int max = Math.Max(0, ContentHeight - Height);
            int v = Math.Max(0, Math.Min(max, y));
            if (v != offset) { offset = v; Invalidate(); }
        }

        protected void EnsureVisible(float top, float bottom)
        {
            int margin = Si(12);
            if (top < offset) ScrollTo((int)top - margin);
            else if (bottom > offset + Height) ScrollTo((int)bottom - Height + margin);
        }

        protected override void OnResize(EventArgs e) { base.OnResize(e); ScrollTo(offset); }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            ScrollTo(offset - e.Delta / 120 * Si(60));
        }

        Rectangle Track => new Rectangle(Width - BarWidth, Si(14), BarWidth, Math.Max(0, Height - Si(28)));

        Rectangle Thumb
        {
            get
            {
                var t = Track;
                int content = Math.Max(1, ContentHeight);
                int h = Math.Max(Si(30), t.Height * Height / content);
                int range = Math.Max(1, ContentHeight - Height);
                int y = t.Y + (t.Height - h) * offset / range;
                return new Rectangle(t.X + Si(5), y, BarWidth - Si(10), h);
            }
        }

        protected bool InScrollBar(Point p) => CanScroll && p.X >= Width - BarWidth;

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            Focus();
            if (!InScrollBar(e.Location)) return;
            var thumb = Thumb;
            if (thumb.Contains(e.Location)) { dragging = true; dragStartY = e.Y; dragStartOffset = offset; Capture = true; }
            else if (e.Y < Si(14)) ScrollTo(offset - Si(60));
            else if (e.Y > Height - Si(14)) ScrollTo(offset + Si(60));
            else ScrollTo(offset + (e.Y < thumb.Y ? -Height : Height) * 9 / 10);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (!dragging) return;
            var t = Track;
            int range = Math.Max(1, ContentHeight - Height);
            int travel = Math.Max(1, t.Height - Thumb.Height);
            ScrollTo(dragStartOffset + (e.Y - dragStartY) * range / travel);
        }

        protected override void OnMouseUp(MouseEventArgs e) { base.OnMouseUp(e); dragging = false; Capture = false; }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            FillSurface(g, ClientRectangle, Theme.Panel);
            var state = g.Save();
            g.SetClip(new Rectangle(0, 0, ViewWidth, Height));
            g.TranslateTransform(0, -offset);
            PaintContent(g, new Rectangle(0, offset, ViewWidth, Height));
            g.Restore(state);
            PaintOverlay(g);
            if (!CanScroll) return;

            g.SmoothingMode = SmoothingMode.AntiAlias;
            var t = Track;
            float cx = t.X + t.Width / 2f;
            using (var pen = new Pen(GoldFaint, Math.Max(1, S(1)))) g.DrawLine(pen, cx, t.Top, cx, t.Bottom);
            using (var b = new SolidBrush(GoldDim))
            {
                float a = S(5);
                g.FillPolygon(b, new[] { new PointF(cx, S(3)), new PointF(cx + a, S(3) + a), new PointF(cx - a, S(3) + a) });
                g.FillPolygon(b, new[] { new PointF(cx, Height - S(3)), new PointF(cx + a, Height - S(3) - a), new PointF(cx - a, Height - S(3) - a) });
            }
            var th = Thumb;
            using (var b = new LinearGradientBrush(th, Theme.Rust, Theme.RustDark, LinearGradientMode.Horizontal)) g.FillRectangle(b, th);
            using (var pen = new Pen(GoldDim, Math.Max(1, S(1)))) g.DrawRectangle(pen, th.X, th.Y, th.Width - 1, th.Height - 1);
        }
    }

    /// <summary>A borderless dialog in the app's style, replacing the Windows message box.</summary>
    class DsDialog : Form
    {
        DsDialog(string title, string message, bool cancel)
        {
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterParent;
            ShowInTaskbar = false;
            KeyPreview = true;
            BackColor = Theme.Window;
            Font = F(10.5f);
            Padding = new Padding(Si(28), Si(22), Si(28), Si(22));
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            MinimumSize = new Size(Si(420), 0);

            var layout = new ClearTable { ColumnCount = 1, Dock = DockStyle.Fill };
            layout.Controls.Add(new Label { Text = title, AutoSize = true, Font = F(15f), ForeColor = Theme.Text, BackColor = Color.Transparent, Margin = new Padding(0, 0, 0, Si(6)) });
            var rule = new Panel { Height = Si(10), Dock = DockStyle.Fill, BackColor = Color.Transparent, Margin = new Padding(0) };
            rule.Paint += (s, e) => Rule(e.Graphics, 0, rule.Width, Si(4));
            layout.Controls.Add(rule);
            layout.Controls.Add(new Label
            {
                Text = message, AutoSize = true, MaximumSize = new Size(Si(620), 0), ForeColor = Theme.Text,
                BackColor = Color.Transparent, Margin = new Padding(0, Si(8), 0, Si(20)), UseMnemonic = false,
            });
            var buttons = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Fill, BackColor = Color.Transparent, Margin = new Padding(0) };
            var ok = new DsButton(Strings.T("ok")) { Primary = true, Font = F(10.5f, FontStyle.Bold), MinimumSize = new Size(Si(110), 0) };
            ok.Click += (s, e) => { DialogResult = DialogResult.OK; Close(); };
            buttons.Controls.Add(ok);
            if (cancel)
            {
                var c = new DsButton(Strings.T("cancel")) { Font = F(10.5f), MinimumSize = new Size(Si(110), 0) };
                c.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
                buttons.Controls.Add(c);
            }
            layout.Controls.Add(buttons);
            Controls.Add(layout);
            KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Escape) { DialogResult = cancel ? DialogResult.Cancel : DialogResult.OK; Close(); }
            };
            Shown += (s, e) => ok.Focus();
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            FillSurface(e.Graphics, ClientRectangle, Theme.Window);
            Frame(e.Graphics, ClientRectangle);
        }

        public static void Info(IWin32Window owner, string title, string message) =>
            Show(owner, title, message, false);

        public static bool Confirm(IWin32Window owner, string title, string message) =>
            Show(owner, title, message, true) == DialogResult.OK;

        static DialogResult Show(IWin32Window owner, string title, string message, bool cancel)
        {
            using (var d = new DsDialog(title, message, cancel)) return d.ShowDialog(owner);
        }
    }

    /// <summary>Colours for the language drop-down menu.</summary>
    class DsMenuRenderer : ToolStripProfessionalRenderer
    {
        public DsMenuRenderer() : base(new Colors()) { RoundedEdges = false; }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            e.TextColor = e.Item.Selected ? Theme.Text : Theme.Muted;
            base.OnRenderItemText(e);
        }

        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            var r = new Rectangle(Point.Empty, e.Item.Size);
            if (e.Item.Selected) Highlight(e.Graphics, r);
            else using (var b = new SolidBrush(Theme.Panel)) e.Graphics.FillRectangle(b, r);
        }

        class Colors : ProfessionalColorTable
        {
            public override Color ToolStripDropDownBackground => Theme.Panel;
            public override Color ImageMarginGradientBegin => Theme.Panel;
            public override Color ImageMarginGradientMiddle => Theme.Panel;
            public override Color ImageMarginGradientEnd => Theme.Panel;
            public override Color MenuBorder => Theme.GoldDim;
            public override Color MenuItemBorder => Theme.Gold;
            public override Color CheckBackground => Theme.Rust;
            public override Color CheckSelectedBackground => Theme.Rust;
            public override Color CheckPressedBackground => Theme.Rust;
        }
    }

    /// <summary>The window's own title bar: drag to move, double-click to maximize, and the window buttons.</summary>
    class TitleBar : Control
    {
        enum Hot { None, Min, Max, Close }
        Hot hot = Hot.None;
        public readonly DsButton LanguageButton = new DsButton();
        readonly ToolTip tips = new ToolTip();
        Hot tipShown = Hot.None;

        [DllImport("user32.dll")] static extern bool ReleaseCapture();
        [DllImport("user32.dll")] static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        public TitleBar()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            Dock = DockStyle.Top;
            Height = Si(42);
            Controls.Add(LanguageButton);
            LanguageButton.Margin = new Padding(0);
        }

        int ButtonWidth => Si(48);
        Rectangle ButtonRect(Hot h) => new Rectangle(Width - ButtonWidth * (4 - (int)h), 0, ButtonWidth, Height);

        protected override void OnLayout(LayoutEventArgs e)
        {
            base.OnLayout(e);
            var size = LanguageButton.GetPreferredSize(Size.Empty);
            int h = Height - Si(10);
            LanguageButton.Bounds = new Rectangle(Width - ButtonWidth * 3 - Si(12) - size.Width, Si(5), size.Width, h);
        }

        Hot HitTest(Point p)
        {
            foreach (var h in new[] { Hot.Min, Hot.Max, Hot.Close })
                if (ButtonRect(h).Contains(p)) return h;
            return Hot.None;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            var h = HitTest(e.Location);
            if (h != hot) { hot = h; Invalidate(); }
            if (h != tipShown)
            {
                tipShown = h;
                var form = FindForm();
                string tip = h == Hot.Min ? Strings.T("tip.min") : h == Hot.Close ? Strings.T("tip.close")
                           : h == Hot.Max ? Strings.T(form?.WindowState == FormWindowState.Maximized ? "tip.restore" : "tip.max") : null;
                if (tip == null) tips.Hide(this); else tips.Show(tip, this, e.X, Height + Si(4), 1500);
            }
        }

        protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); hot = tipShown = Hot.None; tips.Hide(this); Invalidate(); }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left || HitTest(e.Location) != Hot.None) return;
            var form = FindForm();
            if (e.Clicks == 2) { ToggleMaximize(form); return; }
            ReleaseCapture();
            SendMessage(form.Handle, 0xA1 /* WM_NCLBUTTONDOWN */, (IntPtr)2 /* HTCAPTION */, IntPtr.Zero);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            var form = FindForm();
            switch (HitTest(e.Location))
            {
                case Hot.Min: form.WindowState = FormWindowState.Minimized; break;
                case Hot.Max: ToggleMaximize(form); break;
                case Hot.Close: form.Close(); break;
            }
        }

        static void ToggleMaximize(Form form) =>
            form.WindowState = form.WindowState == FormWindowState.Maximized ? FormWindowState.Normal : FormWindowState.Maximized;

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            FillSurface(g, ClientRectangle, Theme.Window);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (var b = new SolidBrush(Gold)) Diamond(g, b, new PointF(Si(20), Height / 2f), S(5));
            Text_(g, Text, F(11f), new Rectangle(Si(34), 0, Width / 2, Height), Muted, TextFormatFlags.VerticalCenter);

            foreach (var h in new[] { Hot.Min, Hot.Max, Hot.Close })
            {
                var r = ButtonRect(h);
                if (h == hot)
                {
                    if (h == Hot.Close) using (var b = new SolidBrush(Color.FromArgb(150, 36, 24))) g.FillRectangle(b, r);
                    else using (var b = new SolidBrush(Theme.Hover)) g.FillRectangle(b, r);
                }
                var color = h == hot ? Theme.Text : Gold;
                using (var pen = new Pen(color, Math.Max(1, S(1.3f))))
                {
                    float cx = r.X + r.Width / 2f, cy = r.Y + r.Height / 2f, s = S(5.5f);
                    g.SmoothingMode = h == Hot.Close ? SmoothingMode.AntiAlias : SmoothingMode.None;
                    if (h == Hot.Min) g.DrawLine(pen, cx - s, cy + S(1), cx + s, cy + S(1));
                    else if (h == Hot.Close) { g.DrawLine(pen, cx - s, cy - s, cx + s, cy + s); g.DrawLine(pen, cx - s, cy + s, cx + s, cy - s); }
                    else if (FindForm()?.WindowState == FormWindowState.Maximized)
                    {
                        g.DrawRectangle(pen, cx - s, cy - s + S(2.5f), s * 2 - S(2.5f), s * 2 - S(2.5f));
                        g.DrawLines(pen, new[] { new PointF(cx - s + S(2.5f), cy - s + S(2.5f)), new PointF(cx - s + S(2.5f), cy - s), new PointF(cx + s, cy - s), new PointF(cx + s, cy + s - S(2.5f)), new PointF(cx + s - S(2.5f), cy + s - S(2.5f)) });
                    }
                    else g.DrawRectangle(pen, cx - s, cy - s, s * 2, s * 2);
                }
            }
            Rule(g, 0, Width, Height - Math.Max(1, S(1)));
        }
    }
}
