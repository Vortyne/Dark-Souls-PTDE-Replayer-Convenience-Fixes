// Visual style modelled on Dark Souls 1's menus: near-black grainy stone, thin gold double
// frames, and a rust-orange highlight for the focused entry; text uses sans-serif UI fonts
// for readability. Dark only.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Linq;
using System.Windows.Forms;

namespace ParamPatcher
{
    static class Theme
    {
        // Surfaces
        public static readonly Color Window = Color.FromArgb(14, 13, 12);
        public static readonly Color Panel = Color.FromArgb(24, 23, 21);
        public static readonly Color Card = Color.FromArgb(31, 29, 26);
        public static readonly Color Input = Color.FromArgb(10, 10, 9);
        // Gold trim
        public static readonly Color Gold = Color.FromArgb(196, 165, 104);
        public static readonly Color GoldDim = Color.FromArgb(111, 92, 55);
        public static readonly Color GoldFaint = Color.FromArgb(62, 53, 36);
        // Text
        public static readonly Color Text = Color.FromArgb(233, 225, 207);
        public static readonly Color Muted = Color.FromArgb(160, 151, 132);
        public static readonly Color Faint = Color.FromArgb(105, 99, 88);
        // The orange-brown highlight of a selected save slot
        public static readonly Color Rust = Color.FromArgb(132, 58, 22);
        public static readonly Color RustDark = Color.FromArgb(92, 38, 14);
        public static readonly Color Hover = Color.FromArgb(44, 39, 33);
        // Warnings
        public static readonly Color WarnBack = Color.FromArgb(46, 26, 14);
        public static readonly Color WarnText = Color.FromArgb(236, 186, 138);

        public static readonly float Scale = GetScale();
        public static float S(float v) => v * Scale;
        public static int Si(float v) => (int)Math.Round(v * Scale);

        static float GetScale()
        {
            using (var g = Graphics.FromHwnd(IntPtr.Zero)) return g.DpiX / 96f;
        }

        public static (Color back, Color fore) Pill(Status s)
        {
            switch (s)
            {
                case Status.Applied: return (Color.FromArgb(30, 44, 26), Color.FromArgb(170, 204, 142));
                case Status.Partial: return (Color.FromArgb(56, 44, 18), Color.FromArgb(226, 186, 98));
                case Status.Conflict: return (Color.FromArgb(62, 22, 18), Color.FromArgb(236, 140, 122));
                case Status.NotApplied: return (Color.FromArgb(33, 37, 46), Color.FromArgb(172, 186, 210));
                default: return (Color.FromArgb(40, 38, 35), Color.FromArgb(143, 135, 122));
            }
        }

        // ------------------------------------------------------------- fonts

        static readonly Dictionary<(string, float, FontStyle), Font> fontCache = new Dictionary<(string, float, FontStyle), Font>();
        static HashSet<string> installed;

        static string FirstInstalled(params string[] names)
        {
            if (installed == null)
                using (var fonts = new InstalledFontCollection())
                    installed = new HashSet<string>(fonts.Families.Select(f => f.Name), StringComparer.OrdinalIgnoreCase);
            return names.FirstOrDefault(installed.Contains) ?? "Segoe UI";
        }

        /// <summary>Sans-serif family for the current language (Windows' own UI fonts, for readability).</summary>
        public static string Family
        {
            get
            {
                switch (Strings.Current)
                {
                    case Lang.Japanese: return FirstInstalled("Yu Gothic UI", "Meiryo UI", "Meiryo", "MS UI Gothic");
                    case Lang.Korean: return FirstInstalled("Malgun Gothic", "Gulim");
                    case Lang.TChinese: return FirstInstalled("Microsoft JhengHei UI", "Microsoft JhengHei", "PMingLiU");
                    default: return FirstInstalled("Segoe UI", "Tahoma", "Arial");
                }
            }
        }

        /// <summary>A font in the current language's family, in points (scaled by Windows like any font).</summary>
        public static Font F(float size, FontStyle style = FontStyle.Regular)
        {
            var key = (Family, size, style);
            if (!fontCache.TryGetValue(key, out var f))
                fontCache[key] = f = new Font(key.Item1, size, style);
            return f;
        }

        // ------------------------------------------------------------- texture

        static TextureBrush grain;

        /// <summary>A subtle stone-like grain, tiled behind every surface.</summary>
        public static Brush Grain(Color baseColor)
        {
            if (grain == null)
            {
                var bmp = new Bitmap(160, 160);
                var rng = new Random(1337);
                for (int y = 0; y < bmp.Height; y++)
                    for (int x = 0; x < bmp.Width; x++)
                    {
                        int n = rng.Next(-7, 8) + (rng.Next(40) == 0 ? rng.Next(-10, 6) : 0);
                        bmp.SetPixel(x, y, n >= 0 ? Color.FromArgb(n * 3, 255, 255, 255) : Color.FromArgb(-n * 6, 0, 0, 0));
                    }
                grain = new TextureBrush(bmp);
            }
            return grain;
        }

        public static void FillSurface(Graphics g, Rectangle r, Color baseColor)
        {
            using (var b = new SolidBrush(baseColor)) g.FillRectangle(b, r);
            g.FillRectangle(Grain(baseColor), r);
        }

        // ------------------------------------------------------------- frames

        /// <summary>DS1-style frame: a gold line, a darker inner line, and small diamond corners.</summary>
        public static void Frame(Graphics g, RectangleF r, bool corners = true, float alpha = 1f)
        {
            var old = g.SmoothingMode;
            g.SmoothingMode = SmoothingMode.None;
            using (var outer = new Pen(Color.FromArgb((int)(255 * alpha), GoldDim), Math.Max(1, S(1))))
            using (var inner = new Pen(Color.FromArgb((int)(255 * alpha), GoldFaint), Math.Max(1, S(1))))
            {
                g.DrawRectangle(outer, r.X, r.Y, r.Width - 1, r.Height - 1);
                float i = S(3);
                g.DrawRectangle(inner, r.X + i, r.Y + i, r.Width - 1 - 2 * i, r.Height - 1 - 2 * i);
            }
            g.SmoothingMode = SmoothingMode.AntiAlias;
            if (corners)
                using (var b = new SolidBrush(Color.FromArgb((int)(255 * alpha), Gold)))
                    foreach (var p in new[] { new PointF(r.Left, r.Top), new PointF(r.Right - 1, r.Top), new PointF(r.Left, r.Bottom - 1), new PointF(r.Right - 1, r.Bottom - 1) })
                        Diamond(g, b, p, S(3.5f));
            g.SmoothingMode = old;
        }

        public static void Diamond(Graphics g, Brush b, PointF c, float r) =>
            g.FillPolygon(b, new[] { new PointF(c.X, c.Y - r), new PointF(c.X + r, c.Y), new PointF(c.X, c.Y + r), new PointF(c.X - r, c.Y) });

        /// <summary>A gold rule that fades out at both ends, like the line under DS1 menu titles.</summary>
        public static void Rule(Graphics g, float x1, float x2, float y)
        {
            using (var b = new LinearGradientBrush(new PointF(x1, y), new PointF(x2, y), Color.Transparent, Color.Transparent))
            {
                b.InterpolationColors = new ColorBlend
                {
                    Colors = new[] { Color.FromArgb(0, GoldDim), GoldDim, GoldDim, Color.FromArgb(0, GoldDim) },
                    Positions = new[] { 0f, 0.08f, 0.85f, 1f },
                };
                g.FillRectangle(b, x1, y, x2 - x1, Math.Max(1, S(1)));
            }
        }

        /// <summary>The rust-orange gradient of a highlighted DS1 menu entry.</summary>
        public static void Highlight(Graphics g, RectangleF r)
        {
            using (var b = new LinearGradientBrush(r, Rust, RustDark, LinearGradientMode.Vertical))
                g.FillRectangle(b, r);
            using (var pen = new Pen(Color.FromArgb(170, Gold), Math.Max(1, S(1))))
                g.DrawRectangle(pen, r.X, r.Y, r.Width - 1, r.Height - 1);
        }

        /// <summary>Square checkbox with a gold frame; state 0 off, 1 on, 2 partly on.</summary>
        public static void CheckBox(Graphics g, RectangleF box, int state, bool enabled)
        {
            var old = g.SmoothingMode;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (var b = new SolidBrush(Input)) g.FillRectangle(b, box);
            using (var pen = new Pen(enabled ? (state != 0 ? Gold : Color.FromArgb(150, 126, 82)) : GoldFaint, S(1.5f)))
                g.DrawRectangle(pen, box.X, box.Y, box.Width, box.Height);
            if (state != 0 && enabled)
            {
                using (var pen = new Pen(Gold, S(2.6f)) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round })
                {
                    float x = box.X, y = box.Y, w = box.Width;
                    if (state == 1)
                        g.DrawLines(pen, new[] { new PointF(x + w * 0.22f, y + w * 0.52f), new PointF(x + w * 0.42f, y + w * 0.72f), new PointF(x + w * 0.80f, y + w * 0.28f) });
                    else
                        g.DrawLine(pen, x + w * 0.27f, y + w * 0.5f, x + w * 0.73f, y + w * 0.5f);
                }
            }
            g.SmoothingMode = old;
        }

        /// <summary>A small status tag, right-aligned at `right`.</summary>
        public static void DrawPill(Graphics g, string text, Status status, Font font, float right, float centerY)
        {
            var (back, fore) = Pill(status);
            var size = TextRenderer.MeasureText(g, text, font, Size.Empty, TextFormatFlags.NoPadding);
            var r = new Rectangle((int)(right - size.Width - S(18)), (int)(centerY - size.Height / 2f - S(4)),
                                  size.Width + Si(18), size.Height + Si(8));
            using (var b = new SolidBrush(back)) g.FillRectangle(b, r);
            using (var pen = new Pen(Color.FromArgb(110, fore), Math.Max(1, S(1)))) g.DrawRectangle(pen, r.X, r.Y, r.Width - 1, r.Height - 1);
            TextRenderer.DrawText(g, text, font, r, fore, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | KeepTransform);
        }

        public static void Text_(Graphics g, string text, Font font, Rectangle r, Color color, TextFormatFlags extra = 0) =>
            TextRenderer.DrawText(g, text, font, r, color, TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding | KeepTransform | extra);

        // TextRenderer (GDI) ignores the Graphics' scroll offset and clip unless told otherwise;
        // without these, text in scrolled views is drawn off-screen.
        const TextFormatFlags KeepTransform = TextFormatFlags.PreserveGraphicsTranslateTransform | TextFormatFlags.PreserveGraphicsClipping;
    }
}
