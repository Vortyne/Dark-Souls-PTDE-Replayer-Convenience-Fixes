using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using static ParamPatcher.Strings;
using static ParamPatcher.Theme;

namespace ParamPatcher
{
    /// <summary>Borderless main window with its own title bar, styled after Dark Souls 1's menus.</summary>
    class MainForm : Form
    {
        const string AppTitle = "Dark Souls Param Patcher";
        readonly string modsDir;
        Session session;
        List<int> shownReports = new List<int>();
        bool busy;

        // Game choice. PTDE is the default: these mods change balance, so Remastered must be played offline.
        Game game = Game.Ptde;
        readonly Dictionary<Game, string> paths = new Dictionary<Game, string>();
        readonly Dictionary<Game, DsButton> gameButtons = new Dictionary<Game, DsButton>();

        readonly TitleBar titleBar = new TitleBar();
        readonly Label subtitle = Lbl(), gameLabel = Lbl(), offlineWarning = Lbl(), fileLabel = Lbl(), fileNote = Lbl();
        readonly DsTextBox pathBox = new DsTextBox();
        DsPanel warnBox; // frames offlineWarning; shown only for Remastered
        readonly DsButton browseButton = new DsButton();
        readonly ModList modList = new ModList();
        readonly Label detailTitle = Lbl(), detailSummary = Lbl(), extraFiles = Lbl(), clashWarning = Lbl();
        readonly DetailGrid detailGrid = new DetailGrid();
        readonly DsButton changesButton = new DsButton { Visible = false };
        ModInfo shownMod; // the mod whose details are shown, for "Full List of Changes"
        readonly DsCheckBox keepOwn = new DsCheckBox();
        readonly Label statusLabel = Lbl();
        readonly DsButton pendingButton = new DsButton(), clearButton = new DsButton();
        readonly DsButton saveButton = new DsButton(), patchButton = new DsButton { Primary = true };
        readonly ContextMenuStrip languageMenu = new ContextMenuStrip { Renderer = new DsMenuRenderer(), ShowImageMargin = false };

        static Label Lbl() => new Label { AutoSize = true, BackColor = Color.Transparent, ForeColor = Theme.Text, UseMnemonic = false };

        public MainForm(string modsDir)
        {
            this.modsDir = modsDir;
            Text = AppTitle;
            FormBorderStyle = FormBorderStyle.None;
            DoubleBuffered = true;
            BackColor = Theme.Window;
            Padding = new Padding(Si(5)); // grab zone for resizing the borderless window
            var area = Screen.PrimaryScreen.WorkingArea;
            Size = new Size(Math.Min(Si(1500), (int)(area.Width * 0.88)), Math.Min(Si(960), (int)(area.Height * 0.9)));
            MinimumSize = new Size(Si(960), Si(640));
            StartPosition = FormStartPosition.CenterScreen;
            try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { /* default icon */ }

            Strings.Current = Strings.Initial();
            BuildLayout();
            ApplyLanguage();
            Strings.Changed += ApplyLanguage;
            foreach (Game g in Enum.GetValues(typeof(Game))) paths[g] = Session.FindGameParam(g);
            SelectGame(Game.Ptde);
            Shown += async (s, e) => { if (pathBox.Text != "") await LoadSession(); else ShowEmpty(); };
        }

        // ------------------------------------------------------------- window chrome

        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                cp.Style |= 0x20000 | 0x80000; // WS_MINIMIZEBOX | WS_SYSMENU: taskbar click minimizes, Alt+Space works
                return cp;
            }
        }

        protected override void WndProc(ref Message m)
        {
            const int WM_NCHITTEST = 0x84, HTCLIENT = 1;
            base.WndProc(ref m);
            if (m.Msg != WM_NCHITTEST || (int)m.Result != HTCLIENT || WindowState == FormWindowState.Maximized) return;
            // Edges of the borderless window resize it, like a normal window frame.
            var p = PointToClient(new Point((short)((long)m.LParam & 0xFFFF), (short)(((long)m.LParam >> 16) & 0xFFFF)));
            int grip = Si(6);
            bool l = p.X < grip, r = p.X >= Width - grip, t = p.Y < grip, b = p.Y >= Height - grip;
            int hit = t && l ? 13 : t && r ? 14 : b && l ? 16 : b && r ? 17 : l ? 10 : r ? 11 : t ? 12 : b ? 15 : HTCLIENT;
            m.Result = (IntPtr)hit;
        }

        protected override void OnResize(EventArgs e)
        {
            // Maximize to the work area (not over the taskbar), without the resize border.
            if (WindowState != FormWindowState.Maximized) MaximizedBounds = Screen.FromControl(this).WorkingArea;
            Padding = new Padding(WindowState == FormWindowState.Maximized ? 0 : Si(5));
            base.OnResize(e);
            titleBar.Invalidate();
            Invalidate();
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            FillSurface(e.Graphics, ClientRectangle, Theme.Window);
            if (WindowState != FormWindowState.Maximized)
                using (var pen = new Pen(GoldDim)) e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
        }

        // ------------------------------------------------------------- layout

        void BuildLayout()
        {
            titleBar.Text = AppTitle;
            titleBar.LanguageButton.Click += (s, e) =>
                languageMenu.Show(titleBar.LanguageButton, new Point(0, titleBar.LanguageButton.Height));
            foreach (var (lang, native, _) in Strings.Languages)
            {
                var item = new ToolStripMenuItem(native) { Tag = lang, Padding = new Padding(Si(6), Si(4), Si(6), Si(4)) };
                item.Click += (s, e) => Strings.Set((Lang)((ToolStripMenuItem)s).Tag);
                languageMenu.Items.Add(item);
            }

            var content = new DsPanel { Dock = DockStyle.Fill, Surface = Theme.Window, Padding = new Padding(Si(22), Si(14), Si(22), Si(16)) };

            // Header: title, subtitle, rule, game choice, warning, file picker
            var header = new ClearTable { Dock = DockStyle.Top, ColumnCount = 1, Padding = new Padding(0, 0, 0, Si(14)) };
            var title = new Label { Text = AppTitle, AutoSize = true, BackColor = Color.Transparent, ForeColor = Theme.Text, Margin = new Padding(0) };
            header.Controls.Add(title);
            subtitle.ForeColor = Muted;
            subtitle.Margin = new Padding(Si(2), Si(4), 0, 0);
            header.Controls.Add(subtitle);
            var rule = new Panel { Height = Si(18), Dock = DockStyle.Fill, BackColor = Color.Transparent, Margin = new Padding(0) };
            rule.Paint += (s, e) => Rule(e.Graphics, 0, rule.Width, Si(9));
            header.Controls.Add(rule);

            var gameRow = new FlowLayoutPanel { AutoSize = true, WrapContents = false, BackColor = Color.Transparent, Margin = new Padding(0, Si(4), 0, Si(10)) };
            gameLabel.Margin = new Padding(0, Si(9), Si(12), 0);
            gameRow.Controls.Add(gameLabel);
            foreach (var g in new[] { Game.Ptde, Game.Remastered })
            {
                var b = new DsButton { Margin = new Padding(0, 0, Si(6), 0) };
                var gg = g;
                b.Click += async (s, e) => { if (game != gg && !busy) await SelectGameAndLoad(gg); };
                gameButtons[g] = b;
                gameRow.Controls.Add(b);
            }
            header.Controls.Add(gameRow);

            warnBox = new DsPanel { Surface = WarnBack, Framed = true, AutoSize = true, Dock = DockStyle.Fill, Padding = new Padding(Si(16), Si(10), Si(16), Si(10)), Margin = new Padding(0, 0, 0, Si(12)) };
            offlineWarning.ForeColor = WarnText;
            offlineWarning.Location = new Point(warnBox.Padding.Left, warnBox.Padding.Top);
            warnBox.Controls.Add(offlineWarning);
            header.Controls.Add(warnBox);

            var fileRow = new ClearTable { Dock = DockStyle.Fill, ColumnCount = 3 };
            fileRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            fileRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            fileRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            fileLabel.Anchor = AnchorStyles.Left;
            fileLabel.Margin = new Padding(0, 0, Si(12), 0);
            fileRow.Controls.Add(fileLabel, 0, 0);
            pathBox.Dock = DockStyle.Fill;
            pathBox.Margin = new Padding(0);
            pathBox.Box.KeyDown += async (s, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; await LoadSession(); } };
            fileRow.Controls.Add(pathBox, 1, 0);
            browseButton.Anchor = AnchorStyles.Left;
            browseButton.Click += async (s, e) => await Browse();
            fileRow.Controls.Add(browseButton, 2, 0);
            fileNote.ForeColor = Muted;
            fileNote.Margin = new Padding(Si(2), Si(6), 0, 0);
            fileRow.Controls.Add(fileNote, 1, 1);
            header.Controls.Add(fileRow);

            // Footer: status, keep-my-edits, actions
            var footer = new ClearTable { Dock = DockStyle.Bottom, ColumnCount = 2, Padding = new Padding(0, Si(14), 0, 0) };
            footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            footer.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            var footerLeft = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, BackColor = Color.Transparent, Margin = new Padding(0), Anchor = AnchorStyles.Left };
            statusLabel.Margin = new Padding(0, 0, 0, Si(6));
            footerLeft.Controls.Add(statusLabel);
            footerLeft.Controls.Add(keepOwn);
            footer.Controls.Add(footerLeft, 0, 0);
            var buttons = new FlowLayoutPanel { AutoSize = true, WrapContents = false, BackColor = Color.Transparent, Margin = new Padding(0), Anchor = AnchorStyles.Right };
            pendingButton.Click += (s, e) => SelectPending();
            clearButton.Click += (s, e) => modList.SetChecked(new int[0]);
            saveButton.Click += (s, e) => Build(replace: false);
            patchButton.Click += (s, e) => Build(replace: true);
            buttons.Controls.AddRange(new Control[] { pendingButton, clearButton, saveButton, patchButton });
            footer.Controls.Add(buttons, 1, 0);
            var footerRule = new Panel { Height = Si(2), Dock = DockStyle.Bottom, BackColor = Color.Transparent };
            footerRule.Paint += (s, e) => Rule(e.Graphics, 0, footerRule.Width, 0);

            // Body: framed mod list | framed details
            var body = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, BackColor = Color.Transparent, Margin = new Padding(0) };
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            body.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            var listFrame = new DsPanel { Dock = DockStyle.Fill, Framed = true, Padding = new Padding(Si(8)), Margin = new Padding(0, 0, Si(8), 0) };
            modList.Dock = DockStyle.Fill;
            modList.CheckedChanged += (s, e) => UpdateStatus();
            modList.FocusChanged += ShowDetails;
            listFrame.Controls.Add(modList);
            body.Controls.Add(listFrame, 0, 0);

            var detailFrame = new DsPanel { Dock = DockStyle.Fill, Framed = true, Padding = new Padding(Si(18), Si(14), Si(10), Si(10)), Margin = new Padding(Si(8), 0, 0, 0) };
            var details = new ClearTable { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 6, AutoSize = false };
            details.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            details.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            details.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            details.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            details.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            details.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            detailTitle.Margin = new Padding(0);
            detailSummary.ForeColor = Muted;
            detailSummary.Margin = new Padding(0, Si(6), 0, Si(10));
            // A mod's "additionalFiles" message: files players must copy into the game themselves.
            extraFiles.Visible = false;
            extraFiles.BackColor = Color.FromArgb(40, 33, 20);
            extraFiles.ForeColor = Color.FromArgb(232, 206, 150);
            extraFiles.Padding = new Padding(Si(10), Si(8), Si(10), Si(8));
            extraFiles.Margin = new Padding(0, 0, 0, Si(10));
            clashWarning.Visible = false;
            clashWarning.BackColor = WarnBack;
            clashWarning.ForeColor = WarnText;
            clashWarning.Padding = new Padding(Si(10), Si(8), Si(10), Si(8));
            clashWarning.Margin = new Padding(0, 0, 0, Si(10));
            detailGrid.Dock = DockStyle.Fill;
            detailGrid.Margin = new Padding(0);
            changesButton.Margin = new Padding(0, 0, 0, Si(10));
            changesButton.Click += (s, e) => { if (shownMod != null) ChangesDialog.ShowFor(this, shownMod); };
            details.Controls.Add(detailTitle, 0, 0);
            details.Controls.Add(detailSummary, 0, 1);
            details.Controls.Add(changesButton, 0, 2);
            details.Controls.Add(extraFiles, 0, 3);
            details.Controls.Add(clashWarning, 0, 4);
            details.Controls.Add(detailGrid, 0, 5);
            details.SizeChanged += (s, e) =>
            {
                var max = new Size(Math.Max(Si(200), details.ClientSize.Width - Si(10)), 0);
                detailTitle.MaximumSize = detailSummary.MaximumSize = extraFiles.MaximumSize = clashWarning.MaximumSize = max;
            };
            detailFrame.Controls.Add(details);
            body.Controls.Add(detailFrame, 1, 0);

            content.Controls.Add(body);
            content.Controls.Add(footerRule);
            content.Controls.Add(footer);
            content.Controls.Add(header);
            header.SizeChanged += (s, e) =>
            {
                subtitle.MaximumSize = new Size(Math.Max(Si(300), header.Width - Si(10)), 0);
                offlineWarning.MaximumSize = new Size(Math.Max(Si(300), header.Width - warnBox.Padding.Horizontal - Si(10)), 0);
            };
            Controls.Add(content);
            Controls.Add(titleBar);

            localizers.Add(() =>
            {
                title.Font = F(22f);
                subtitle.Font = F(11f);
                gameLabel.Font = fileLabel.Font = F(11f, FontStyle.Bold);
                foreach (var b in gameButtons.Values) b.Font = F(10.5f);
                offlineWarning.Font = F(10.5f, FontStyle.Bold);
                pathBox.Box.Font = F(11f);
                browseButton.Font = pendingButton.Font = clearButton.Font = saveButton.Font = F(10.5f);
                patchButton.Font = F(11f, FontStyle.Bold);
                fileNote.Font = F(9.5f, FontStyle.Italic);
                statusLabel.Font = F(11f, FontStyle.Bold);
                keepOwn.Font = F(10.5f);
                detailTitle.Font = F(14f);
                detailSummary.Font = extraFiles.Font = clashWarning.Font = F(10f);
                titleBar.LanguageButton.Font = F(10f);
                changesButton.Font = F(10.5f);
            });
        }

        // ------------------------------------------------------------- language

        readonly List<Action> localizers = new List<Action>();

        void ApplyLanguage()
        {
            SuspendLayout();
            foreach (var a in localizers) a();
            titleBar.LanguageButton.Text = Native(Current) + "  ▾";
            foreach (ToolStripMenuItem item in languageMenu.Items)
            {
                var lang = (Lang)item.Tag;
                item.Checked = lang == Current;
                var old = Current;
                Current = lang; // pick a font that can show this language's own name
                item.Font = F(10.5f);
                Current = old;
            }
            subtitle.Text = T("subtitle");
            gameLabel.Text = T("game");
            gameButtons[Game.Ptde].Text = T("game.ptde");
            gameButtons[Game.Remastered].Text = T("game.dsr");
            offlineWarning.Text = T("warn.dsr");
            fileLabel.Text = T("file");
            browseButton.Text = T("browse");
            fileNote.Text = T("file.note");
            keepOwn.Text = T("keep");
            pendingButton.Text = T("btn.pending");
            changesButton.Text = "◆  " + T("btn.changes");
            clearButton.Text = T("btn.clear");
            saveButton.Text = T("btn.save");
            patchButton.Text = T("btn.patch");
            ResumeLayout(true);
            titleBar.PerformLayout();
            modList.EmptyText = session == null && !busy ? EmptyMessage() : modList.EmptyText;
            modList.Relayout();
            ShowDetails(shownReports);
            UpdateStatus();
        }

        string WhereToFind(Game g) => T(g == Game.Ptde ? "where.ptde" : "where.dsr");
        string EmptyMessage() => T("empty.select", Games.Name(game)) + "\n\n" + WhereToFind(game);

        // ------------------------------------------------------------- game + file

        void ShowEmpty()
        {
            session = null;
            modList.EmptyText = EmptyMessage();
            modList.SetReports(null);
            ShowDetails(new List<int>());
            UpdateStatus();
        }

        /// <summary>Switch the game buttons, warning banner and path box to `g`.</summary>
        void SelectGame(Game g)
        {
            if (pathBox.Text != "") paths[game] = pathBox.Text;
            game = g;
            foreach (var kv in gameButtons) kv.Value.Toggled = kv.Key == g;
            warnBox.Visible = g == Game.Remastered;
            pathBox.Text = paths.TryGetValue(g, out var p) ? p : "";
        }

        async Task SelectGameAndLoad(Game g)
        {
            SelectGame(g);
            if (File.Exists(pathBox.Text)) await LoadSession();
            else ShowEmpty();
        }

        async Task Browse()
        {
            using (var dlg = new OpenFileDialog
            {
                Title = T("open.title", Games.Name(game)),
                Filter = $"{T("filter.param")} (*.parambnd; *.parambnd.dcx)|*.parambnd;*.dcx|{T("filter.all")} (*.*)|*.*",
                FileName = Games.FileName(game),
            })
            {
                if (File.Exists(pathBox.Text)) dlg.InitialDirectory = Path.GetDirectoryName(pathBox.Text);
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                pathBox.Text = dlg.FileName;
                await LoadSession();
            }
        }

        /// <summary>Disables input while work runs on a background thread, so the window stays responsive.</summary>
        void SetBusy(bool on)
        {
            busy = on;
            UseWaitCursor = on;
            pathBox.Enabled = browseButton.Enabled = modList.Enabled = keepOwn.Enabled = !on;
            foreach (var b in gameButtons.Values) b.Enabled = !on;
            UpdateStatus();
        }

        async Task LoadSession()
        {
            string path = pathBox.Text.Trim().Trim('"');
            if (!File.Exists(path))
            {
                DsDialog.Info(this, AppTitle, T("err.missing") + "\n\n" + WhereToFind(game));
                return;
            }
            session = null;
            modList.EmptyText = T("loading");
            modList.SetReports(null);
            ShowDetails(new List<int>());
            SetBusy(true);
            try
            {
                session = await Task.Run(() => new Session(path, modsDir));
                if (session.Game != game)
                {
                    // The file belongs to the other game (detected from its format): follow it.
                    SelectGame(session.Game);
                    pathBox.Text = path;
                }
                paths[game] = path;
                if (session.Reports.Count == 0) modList.EmptyText = T("nocsv", modsDir);
                modList.MissingFiles = session.MissingFiles;
                modList.SetReports(session.Reports);
            }
            catch (Exception e)
            {
                session = null;
                ShowEmpty();
                DsDialog.Info(this, AppTitle, T("err.read") + "\n\n" + e.Message);
            }
            finally
            {
                SetBusy(false);
            }
        }

        // ------------------------------------------------------------- selection + details

        List<Report> Selected() =>
            session == null ? new List<Report>() : modList.Checked.OrderBy(i => i).Select(i => session.Reports[i]).ToList();

        void SelectPending()
        {
            if (session == null) return;
            modList.SetChecked(session.Reports.Select((r, i) => (r, i))
                .Where(t => t.r.Status == Status.NotApplied || t.r.Status == Status.Partial || t.r.Status == Status.Conflict
                            || (session.MissingFiles.TryGetValue(t.r.Csv.Info, out int missing) && missing > 0))
                .Select(t => t.i));
        }

        void UpdateStatus()
        {
            var selected = Selected();
            var parts = new List<string>();
            if (selected.Count == 0) parts.Add(T("footer.none"));
            else parts.Add(T("footer.mods", selected.Select(r => r.Csv.Info).Distinct().Count(), selected.Count));
            int conflicts = selected.Sum(r => r.Conflicts.Count);
            if (conflicts > 0) parts.Add(T("footer.conflicts", conflicts));
            var clashes = Patcher.CrossConflicts(selected);
            if (clashes.Count > 0) parts.Add(T("footer.clashes", clashes.Count));
            statusLabel.Text = string.Join("   ·   ", parts);
            patchButton.Enabled = saveButton.Enabled = selected.Count > 0 && !busy;
            pendingButton.Enabled = clearButton.Enabled = session != null && !busy;
            ShowClashes(clashes);
        }

        void ShowClashes(List<Clash> clashes)
        {
            clashWarning.Visible = clashes.Count > 0;
            if (clashes.Count == 0) return;
            var lines = clashes.Take(5).Select(c =>
                $"◆ {c.CellA.Param} #{c.CellA.RowId} {c.CellA.Field.Name}:  {c.A.Csv.Label} → {c.CellA.Field.Format(c.CellA.Target)}   ·   " +
                $"{c.B.Csv.Label} → {c.CellB.Field.Format(c.CellB.Target)}");
            clashWarning.Text = T("clash") + "\n" + string.Join("\n", lines) + (clashes.Count > 5 ? "\n" + T("more", clashes.Count - 5) : "");
        }

        void ShowDetails(List<int> indices)
        {
            shownReports = indices;
            string[] headers =
            {
                T("col.name"), T("col.field"), T("col.original"), T("col.yours"), T("col.mod"), T("col.state"), T("col.row"), T("col.param"),
            };
            extraFiles.Visible = false;
            changesButton.Visible = false;
            shownMod = null;
            if (session == null || indices.Count == 0)
            {
                detailTitle.Text = T("details");
                detailSummary.Text = T("details.hint");
                detailGrid.SetData(headers, new List<DetailRow>());
                return;
            }
            var reports = indices.Select(i => session.Reports[i]).ToList();
            var info = reports[0].Csv.Info;
            string modName = info.DisplayName(Code);
            detailTitle.Text = reports.Count == 1 ? modName + "  ·  " + UiText.PatchName(reports[0].Csv) : modName;
            shownMod = info;
            changesButton.Visible = info.Changes != null;
            // The mod's own description and author (from its mod.json), then each file's state.
            var lines = new List<string>();
            string description = info.Description.Get(Code);
            if (description != "") lines.Add(description);
            if (!string.IsNullOrWhiteSpace(info.Author)) lines.Add(T("author", info.Author));
            if (lines.Count > 0) lines.Add("");
            lines.AddRange(reports.Select(r =>
                (reports.Count > 1 ? UiText.PatchName(r.Csv) + ":  " : "") + UiText.Status(r.Status) + "  —  " + UiText.Summary(r)));
            detailSummary.Text = string.Join("\n", lines);
            if (!info.AdditionalFiles.IsEmpty)
            {
                extraFiles.Text = "◆  " + T("extra.title") + "\n" + info.AdditionalFiles.Get(Code);
                extraFiles.Visible = true;
            }
            const int limit = 3000;
            var cells = reports.SelectMany(r => r.Cells).OrderBy(c => c.Applied ? 2 : c.Conflict ? 0 : 1).ToList();
            var rows = cells.Take(limit).Select(c => new DetailRow
            {
                Kind = c.Conflict ? DetailKind.Conflict : c.Applied ? DetailKind.Applied : DetailKind.WillChange,
                Cells = new[]
                {
                    c.RowName, c.Field.Name,
                    c.Vanilla.HasValue ? c.Field.Format(c.Vanilla.Value) : T("val.newRow"),
                    c.Current.HasValue ? c.Field.Format(c.Current.Value) : T("val.missing"),
                    c.Field.Format(c.Target),
                    T(c.Conflict ? "state.conflict" : c.Applied ? "status.applied" : "state.will"),
                    c.RowId.ToString(), c.Param,
                },
            }).ToList();
            if (cells.Count > limit) rows.Add(new DetailRow { Kind = DetailKind.Note, Cells = new[] { T("more", cells.Count - limit) } });
            detailGrid.SetData(headers, rows);
        }

        // ------------------------------------------------------------- patching

        async void Build(bool replace)
        {
            var selected = Selected();
            if (session == null || selected.Count == 0 || busy) return;
            bool keep = keepOwn.Checked;
            string outPath = null;
            // Mods that install game files (whole mods, as their files are applied with the mod).
            var withFiles = Patcher.WithRequired(selected, session.Reports).Select(r => r.Csv.Info).Distinct()
                .Where(m => m.InstallDir != null).ToList();
            if (replace)
            {
                string message = T("confirm", selected.Count, Games.Name(session.Game), session.GamePath)
                                 + (withFiles.Count > 0 ? "\n\n" + T("confirm.files", string.Join(", ", withFiles.Select(m => m.DisplayName(Code)))) : "")
                                 + (session.Game == Game.Remastered ? "\n\n" + T("confirm.dsr") : "");
                if (!DsDialog.Confirm(this, T("btn.patch"), message)) return;
            }
            else
            {
                using (var dlg = new SaveFileDialog
                {
                    Title = T("save.title"),
                    FileName = Games.FileName(session.Game),
                    Filter = $"{T("filter.param")} (*.parambnd; *.parambnd.dcx)|*.parambnd;*.dcx|{T("filter.all")} (*.*)|*.*",
                })
                {
                    if (dlg.ShowDialog(this) != DialogResult.OK) return;
                    outPath = dlg.FileName;
                }
            }
            var current = session;
            string result;
            SetBusy(true);
            try
            {
                result = await Task.Run(() =>
                {
                    var (data, written, skipped) = current.Build(selected, keep);
                    string m = T("done.changed", written) + (skipped > 0 ? "\n" + T("done.kept", skipped) : "");
                    if (replace)
                    {
                        m += "\n\n" + T("done.patched", Session.BackupAndReplace(current.GamePath, data));
                        if (withFiles.Count == 0) return m;
                        if (current.GameRoot == null) return m + "\n\n" + T("files.noRoot");
                        return m + "\n\n" + T("files.done", current.InstallFiles(withFiles).copied);
                    }
                    File.WriteAllBytes(outPath, data);
                    return m + "\n\n" + T("done.saved", outPath) + (withFiles.Count > 0 ? "\n\n" + T("files.onlyPatch") : "");
                });
            }
            catch (Exception e)
            {
                SetBusy(false);
                DsDialog.Info(this, AppTitle, T("err.patch") + "\n\n" + e.Message);
                return;
            }
            SetBusy(false);
            // Remind about files the patcher can't install, for every mod that has them.
            var extras = selected.Select(r => r.Csv.Info).Distinct().Where(m => !m.AdditionalFiles.IsEmpty).ToList();
            if (extras.Count > 0)
                result += "\n\n" + T("extra.after") + string.Concat(extras.Select(m => "\n\n◆  " + m.DisplayName(Code) + "\n" + m.AdditionalFiles.Get(Code)));
            DsDialog.Info(this, replace ? T("btn.patch") : T("btn.save").TrimEnd('.'), result);
            if (replace)
            {
                var keepShown = shownReports;
                var keepChecked = modList.Checked.ToList();
                await LoadSession();
                if (session != null)
                {
                    modList.SetChecked(keepChecked);
                    ShowDetails(keepShown);
                }
            }
        }
    }
}
