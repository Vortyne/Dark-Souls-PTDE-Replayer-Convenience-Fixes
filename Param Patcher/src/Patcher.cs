// Compare the repo's CSV mods against vanilla and the player's GameParam, and apply them.
//
// Only the cells where a CSV differs from vanilla count as that CSV's changes, so applying a
// CSV never reverts unrelated edits that already exist in the player's param file.
// CSV format: "ID,Name,<field>,..." (Name optional); a blank cell means "leave this field alone".
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace ParamPatcher
{
    enum Status { NotApplied, Partial, Applied, Conflict, NoEffect, Error }

    static class StatusText
    {
        public static string Of(Status s)
        {
            switch (s)
            {
                case Status.NotApplied: return "Not applied";
                case Status.Partial: return "Partially applied";
                case Status.Applied: return "Already applied";
                case Status.Conflict: return "Conflicts";
                case Status.NoEffect: return "No changes";
                default: return "Error";
            }
        }
    }

    /// <summary>Text from mod.json: a plain string, or {"en": "...", "ja": "...", ...} per language.</summary>
    class LocalText
    {
        readonly Dictionary<string, string> byLang = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public LocalText(string text) { if (text != null) byLang["en"] = text; }

        public static LocalText From(object json)
        {
            if (json is string s) return new LocalText(s);
            var t = new LocalText(null);
            if (json is Dictionary<string, object> d)
                foreach (var kv in d) if (kv.Value is string v) t.byLang[kv.Key] = v;
            return t;
        }

        public bool IsEmpty => byLang.Count == 0;

        /// <summary>The text in `langCode`, else English, else whatever is there.</summary>
        public string Get(string langCode) =>
            byLang.TryGetValue(langCode, out var s) ? s : byLang.TryGetValue("en", out s) ? s : byLang.Values.FirstOrDefault() ?? "";
    }

    /// <summary>A mod: one folder inside "mods", described by its mod.json.</summary>
    class ModInfo
    {
        public string Folder;          // folder name, used when mod.json has no name
        public string Dir;
        public LocalText Name, Description;
        public LocalText AdditionalFiles = new LocalText(null); // what else to copy into the game, shown to players
        public string Author;
        public List<Game> Games;       // null = every game
        public bool RequireAll;        // all patches are applied together or not at all
        public string Error;           // mod.json couldn't be read

        public string DisplayName(string langCode) => Name.IsEmpty ? Folder : Name.Get(langCode);
        public bool SupportsGame(Game g) => Games == null || Games.Contains(g);

        public string Changes;      // the mod's list of changes (Markdown), shown in the app; null = none
        public string InstallDir;   // folder whose contents are copied into the game folder when patching

        /// <summary>The change list in `langCode` (e.g. CHANGES.ja.md next to CHANGES.md) if there is one.</summary>
        public string ChangesPath(string langCode)
        {
            if (Changes == null) return null;
            string translated = Path.Combine(Path.GetDirectoryName(Changes),
                Path.GetFileNameWithoutExtension(Changes) + "." + langCode + Path.GetExtension(Changes));
            return File.Exists(translated) ? translated : Changes;
        }

        /// <summary>Every file to install, as (source, path relative to the game folder). Editor backups
        /// (*.prev, *.bak) are left out.</summary>
        public IEnumerable<(string source, string relative)> InstallFiles() =>
            InstallDir == null ? Enumerable.Empty<(string, string)>()
                : Directory.GetFiles(InstallDir, "*", SearchOption.AllDirectories)
                    .Where(f => !f.EndsWith(".prev", StringComparison.OrdinalIgnoreCase) && !f.EndsWith(".bak", StringComparison.OrdinalIgnoreCase))
                    .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
                    .Select(f => (f, f.Substring(InstallDir.Length).TrimStart('\\', '/')));
    }

    /// <summary>One patch of a mod: a CSV and the param table it edits.</summary>
    class ModCsv
    {
        public ModInfo Info;
        public string Path;      // the CSV
        public string RelPath;   // "Mod Folder/File.csv", for messages
        public string Param;     // e.g. "EquipParamWeapon"
        public string Error;     // set when the patch entry itself is broken

        public string Label => System.IO.Path.GetFileName(Path);
        /// <summary>Stands for a mod's game files rather than a CSV (mods that only install files).</summary>
        public bool FilesOnly => Param == null;
        public string Mod => Info.DisplayName("en");
    }

    class CsvRow
    {
        public int Id;
        public string Name;
        public Dictionary<int, Value> Values; // field index -> value
    }

    class Cell
    {
        public string Param;        // short param name in the bnd
        public int RowId;
        public string RowName;
        public Field Field;
        public Value? Vanilla;      // null when the row doesn't exist in vanilla
        public Value Target;        // value from the CSV
        public Value? Current;      // value in the player's file, null if the row is missing

        public bool Applied => Current.HasValue && Current.Value.Equals(Target);

        /// <summary>The player's file has its own, different edit of this cell.</summary>
        public bool Conflict => !Applied && Current.HasValue && !Current.Equals(Vanilla);

        public (string, int, int, int) Key => (Param, RowId, Field.Offset, Field.BitOffset);

        public string Describe()
        {
            string vanilla = Vanilla.HasValue ? Field.Format(Vanilla.Value) : "(new row)";
            string current = Current.HasValue ? Field.Format(Current.Value) : "(missing)";
            return $"{Param} row {RowId} [{RowName}] {Field.Name}: vanilla {vanilla}, your file {current}, mod {Field.Format(Target)}";
        }
    }

    class Report
    {
        public readonly ModCsv Csv;
        public readonly List<Cell> Cells;
        public readonly string Error;

        public Report(ModCsv csv, List<Cell> cells, string error = null)
        {
            Csv = csv;
            Cells = cells;
            Error = error;
        }

        /// <summary>Can be ticked and applied.</summary>
        public bool Selectable => Error == null;
        public bool FilesOnly;      // a mod's game files, not a CSV
        public int MissingFiles;    // files-only: how many aren't in the game yet (set by Session)

        public int AppliedCount => Cells.Count(c => c.Applied);
        public List<Cell> Conflicts => Cells.Where(c => c.Conflict).ToList();
        public List<Cell> Pending => Cells.Where(c => !c.Applied).ToList();

        public Status Status
        {
            get
            {
                if (Error != null) return Status.Error;
                if (FilesOnly) return MissingFiles == 0 ? Status.Applied : Status.NotApplied;
                if (Cells.Count == 0) return Status.NoEffect;
                if (Cells.Any(c => c.Conflict)) return Status.Conflict;
                int applied = AppliedCount;
                if (applied == Cells.Count) return Status.Applied;
                return applied > 0 ? Status.Partial : Status.NotApplied;
            }
        }

        public string Summary()
        {
            if (Error != null) return Error;
            if (FilesOnly) return MissingFiles == 0 ? "Game files installed" : $"Game files to install: {MissingFiles}";
            var parts = new List<string> { $"{Cells.Count} changed values", $"{AppliedCount} already in your file" };
            int conflicts = Conflicts.Count;
            if (conflicts > 0) parts.Add($"{conflicts} conflict with your own edits");
            return string.Join(", ", parts);
        }

        /// <summary>Compact version for the mod list, e.g. "1061 values · 973 applied · 88 conflicts".</summary>
        public string ShortSummary()
        {
            if (Error != null) return Error;
            string s = $"{Cells.Count} value{(Cells.Count == 1 ? "" : "s")}  ·  {AppliedCount} applied";
            int conflicts = Conflicts.Count;
            return conflicts > 0 ? s + $"  ·  {conflicts} conflict{(conflicts == 1 ? "" : "s")}" : s;
        }
    }

    class Clash
    {
        public Report A, B;
        public Cell CellA, CellB;
    }

    static class Patcher
    {
        public static byte[] Resource(string name)
        {
            using (var s = Assembly.GetExecutingAssembly().GetManifestResourceStream(name))
            using (var m = new MemoryStream())
            {
                if (s == null) throw new FileNotFoundException("Missing embedded resource " + name);
                s.CopyTo(m);
                return m.ToArray();
            }
        }

        /// <summary>The "mods" folder next to the exe (as in a release). When running a development
        /// build from inside the repo, the repo's top-level "mods" folder a few levels up.</summary>
        public static string DefaultModsDir()
        {
            string exeDir = AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\', '/');
            string dir = exeDir;
            for (int up = 0; up <= 4 && dir != null; up++, dir = Path.GetDirectoryName(dir))
            {
                string mods = Path.Combine(dir, "mods");
                if (Directory.Exists(mods)) return mods;
            }
            return Path.Combine(exeDir, "mods");
        }

        static readonly Regex ParamInName = new Regex(@"\((?<param>[^()]+)\)\s*$");

        /// <summary>Every patch of every mod in `modsDir`: one folder per mod, each with a mod.json
        /// (or, without one, CSVs named after the param they edit).</summary>
        public static List<ModCsv> Discover(string modsDir)
        {
            var found = new List<ModCsv>();
            if (!Directory.Exists(modsDir)) return found;
            foreach (var dir in Directory.GetDirectories(modsDir).OrderBy(d => d, StringComparer.OrdinalIgnoreCase))
            {
                var info = new ModInfo { Folder = Path.GetFileName(dir), Dir = dir, Name = new LocalText(null), Description = new LocalText(null) };
                var patches = new List<(string file, string param)>();
                string config = Path.Combine(dir, "mod.json");
                if (File.Exists(config))
                {
                    try { ReadModJson(config, info, patches); }
                    catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is ArgumentException
                                              || e is InvalidOperationException || e is FormatException)
                    {
                        info.Error = $"{info.Folder}/mod.json: {e.Message}";
                    }
                }
                else
                {
                    // No mod.json: each CSV edits the param it's named after ("EquipParamWeapon.csv"
                    // or "Anything (EquipParamWeapon).csv").
                    try
                    {
                        foreach (var f in Directory.GetFiles(dir, "*.csv").OrderBy(f => f, StringComparer.OrdinalIgnoreCase))
                        {
                            string stem = Path.GetFileNameWithoutExtension(f);
                            var m = ParamInName.Match(stem);
                            patches.Add((Path.GetFileName(f), m.Success ? m.Groups["param"].Value.Trim() : stem));
                        }
                    }
                    catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
                    {
                        info.Error = $"{info.Folder}: {e.Message}";
                    }
                }
                if (info.Changes == null && File.Exists(Path.Combine(dir, "CHANGES.md")))
                    info.Changes = Path.Combine(dir, "CHANGES.md");
                if (info.Error != null)
                {
                    found.Add(new ModCsv { Info = info, Path = config, RelPath = $"{info.Folder}/mod.json", Param = "mod.json", Error = info.Error });
                    continue;
                }
                if (patches.Count == 0 && info.InstallDir != null)
                    found.Add(new ModCsv { Info = info, Path = info.InstallDir, RelPath = $"{info.Folder}/{Path.GetFileName(info.InstallDir)}" });
                foreach (var (file, param) in patches)
                {
                    string path = Path.Combine(dir, file);
                    found.Add(new ModCsv
                    {
                        Info = info, Path = path, RelPath = $"{info.Folder}/{file}", Param = param,
                        Error = File.Exists(path) ? null : $"{info.Folder}: '{file}' listed in mod.json doesn't exist",
                    });
                }
            }
            return found;
        }

        static void ReadModJson(string path, ModInfo info, List<(string file, string param)> patches)
        {
            var json = new System.Web.Script.Serialization.JavaScriptSerializer().DeserializeObject(File.ReadAllText(path))
                as Dictionary<string, object>;
            if (json == null) throw new FormatException("expected a JSON object");
            object v;
            if (json.TryGetValue("name", out v)) info.Name = LocalText.From(v);
            if (json.TryGetValue("description", out v)) info.Description = LocalText.From(v);
            if (json.TryGetValue("additionalFiles", out v)) info.AdditionalFiles = LocalText.From(v);
            if (json.TryGetValue("author", out v)) info.Author = v as string;
            if (json.TryGetValue("requireAll", out v))
                info.RequireAll = v is bool b ? b : throw new FormatException("\"requireAll\" must be true or false");
            string modDir = Path.GetDirectoryName(path);
            if (json.TryGetValue("changes", out v))
            {
                info.Changes = Path.Combine(modDir, v as string ?? "");
                if (!File.Exists(info.Changes)) throw new FormatException($"\"changes\": '{v}' doesn't exist");
            }
            if (json.TryGetValue("installFiles", out v))
            {
                info.InstallDir = Path.Combine(modDir, v as string ?? "").TrimEnd('\\', '/');
                if (!Directory.Exists(info.InstallDir)) throw new FormatException($"\"installFiles\": folder '{v}' doesn't exist");
            }
            if (json.TryGetValue("games", out v) && v is object[] games)
            {
                info.Games = new List<Game>();
                foreach (var g in games.OfType<string>())
                {
                    switch (g.Trim().ToLowerInvariant())
                    {
                        case "ptde": case "prepare to die edition": info.Games.Add(Game.Ptde); break;
                        case "remastered": case "dsr": info.Games.Add(Game.Remastered); break;
                        default: throw new FormatException($"unknown game '{g}' (use \"ptde\" or \"remastered\")");
                    }
                }
            }
            if (!json.TryGetValue("patches", out v) || !(v is object[] list) || list.Length == 0)
            {
                if (info.InstallDir != null) return; // a mod that only installs game files
                throw new FormatException("needs a \"patches\" list (e.g. [{ \"file\": \"EquipParamWeapon.csv\", \"param\": \"EquipParamWeapon\" }]) or \"installFiles\"");
            }
            foreach (var entry in list)
            {
                var p = entry as Dictionary<string, object>;
                string file = p != null && p.TryGetValue("file", out v) ? v as string : null;
                string param = p != null && p.TryGetValue("param", out v) ? v as string : null;
                if (string.IsNullOrWhiteSpace(file))
                    throw new FormatException("every patch needs a \"file\"");
                patches.Add((file, string.IsNullOrWhiteSpace(param) ? Path.GetFileNameWithoutExtension(file) : param.Trim()));
            }
        }

        public static List<CsvRow> ReadCsv(string path, ParamDef def)
        {
            string file = Path.GetFileName(path);
            var lines = File.ReadAllLines(path).Where(l => l.Trim() != "").ToList();
            var rows = new List<CsvRow>();
            if (lines.Count == 0) return rows;
            var header = lines[0].Split(',');
            if (header.Length < 2 || !header[0].Trim().Equals("ID", StringComparison.OrdinalIgnoreCase))
                throw new FormatException($"{file}: header must start with 'ID'");
            bool hasName = header[1].Trim().Equals("Name", StringComparison.OrdinalIgnoreCase);
            int lead = hasName ? 2 : 1;
            var columns = header.Skip(lead).Select(h => h.Trim()).ToList();
            while (columns.Count > 0 && columns[columns.Count - 1] == "") columns.RemoveAt(columns.Count - 1);

            // Match columns to fields by name, in order, so repeated names (e.g. 'pad') map correctly.
            var used = new HashSet<int>();
            var columnFields = new List<int>();
            foreach (var col in columns)
            {
                int index = def.Fields.FindIndex(f => !used.Contains(def.Fields.IndexOf(f))
                                                      && f.Name.Equals(col, StringComparison.OrdinalIgnoreCase));
                if (index < 0) throw new FormatException($"{file}: unknown field '{col}' for {def.ParamType}");
                used.Add(index);
                columnFields.Add(index);
            }

            for (int ln = 1; ln < lines.Count; ln++)
            {
                var cells = lines[ln].Split(',').ToList();
                if (cells.Count > columns.Count + lead && cells[cells.Count - 1] == "") cells.RemoveAt(cells.Count - 1);
                int extra = cells.Count - (columns.Count + lead);
                if (extra < 0 || (extra > 0 && !hasName))
                    throw new FormatException($"{file} line {ln + 1}: expected {columns.Count + lead} values, got {cells.Count}");
                // Names aren't quoted, so commas in a name spill into extra cells.
                string name = hasName ? string.Join(",", cells.Skip(1).Take(1 + extra)) : "";
                var values = new Dictionary<int, Value>();
                try
                {
                    int id = int.Parse(cells[0].Trim());
                    for (int c = 0; c < columns.Count; c++)
                    {
                        string text = cells[lead + extra + c];
                        if (text.Trim() == "") continue; // blank = leave alone
                        // Zero padding is a no-op (new rows start zeroed) and pad sizes differ
                        // between PTDE and Remastered, so skip it.
                        var field = def.Fields[columnFields[c]];
                        if (field.IsPadding && text.Trim().Trim('[', ']').Split('|').All(p => p.Trim() == "0")) continue;
                        values[columnFields[c]] = def.Fields[columnFields[c]].Parse(text);
                    }
                    rows.Add(new CsvRow { Id = id, Name = name, Values = values });
                }
                catch (FormatException e)
                {
                    throw new FormatException($"{file} line {ln + 1}: {e.Message}");
                }
            }
            return rows;
        }

        /// <summary>Compare one CSV with vanilla and the player's GameParam.</summary>
        public static Report Analyze(ModCsv csv, GameParam vanilla, GameParam game, Game kind)
        {
            if (csv.Error != null) return new Report(csv, new List<Cell>(), csv.Error);
            if (csv.FilesOnly) return new Report(csv, new List<Cell>()) { FilesOnly = true };
            var (shortName, gameParam) = game.Get(csv.Param);
            var vanillaParam = vanilla.Get(csv.Param).param;
            if (gameParam == null || vanillaParam == null)
                return new Report(csv, new List<Cell>(), $"Param '{csv.Param}' not found in GameParam");
            List<CsvRow> rows;
            try { rows = ReadCsv(csv.Path, gameParam.Def); }
            catch (Exception e) when (e is IOException || e is FormatException) { return new Report(csv, new List<Cell>(), e.Message); }

            var cells = new List<Cell>();
            foreach (var row in rows)
            {
                var vrow = vanillaParam.Find(row.Id);
                var grow = gameParam.Find(row.Id);
                foreach (var kv in row.Values)
                {
                    var field = gameParam.Def.Fields[kv.Key];
                    Value? vval = vrow != null ? field.Read(vrow.Data) : (Value?)null;
                    if (vval.HasValue && vval.Value.Equals(kv.Value)) continue;
                    cells.Add(new Cell
                    {
                        Param = shortName, RowId = row.Id, RowName = row.Name, Field = field,
                        Vanilla = vval, Target = kv.Value,
                        Current = grow != null ? field.Read(grow.Data) : (Value?)null,
                    });
                }
            }
            return new Report(csv, cells);
        }

        public static List<Report> AnalyzeAll(IEnumerable<ModCsv> csvs, GameParam vanilla, GameParam game, Game kind)
        {
            var reports = csvs.Select(c => Analyze(c, vanilla, game, kind)).ToList();
            // A mod whose patches must all be applied can't be applied at all if one of them is broken.
            foreach (var mod in reports.GroupBy(r => r.Csv.Info).Where(g => g.Key.RequireAll))
            {
                var broken = mod.FirstOrDefault(r => r.Error != null);
                if (broken == null) continue;
                foreach (var r in mod.Where(r => r.Error == null).ToList())
                    reports[reports.IndexOf(r)] = new Report(r.Csv, r.Cells,
                        $"Can't be applied: this mod's files must all be applied together, and {broken.Csv.RelPath} has an error") { FilesOnly = r.FilesOnly };
            }
            return reports;
        }

        /// <summary>`selected` plus every other patch of mods that must be applied as a whole, in list order.</summary>
        public static List<Report> WithRequired(IEnumerable<Report> selected, IList<Report> all)
        {
            var set = new HashSet<Report>(selected);
            foreach (var r in set.ToList())
                if (r.Csv.Info.RequireAll)
                    foreach (var other in all.Where(o => o.Csv.Info == r.Csv.Info && o.Selectable)) set.Add(other);
            return all.Where(set.Contains).ToList();
        }

        /// <summary>Values that two of the given CSVs set differently.</summary>
        public static List<Clash> CrossConflicts(IEnumerable<Report> reports)
        {
            var seen = new Dictionary<(string, int, int, int), (Report, Cell)>();
            var clashes = new List<Clash>();
            foreach (var r in reports)
                foreach (var c in r.Cells)
                {
                    if (!seen.TryGetValue(c.Key, out var prev)) seen[c.Key] = (r, c);
                    else if (prev.Item1 != r && !prev.Item2.Target.Equals(c.Target))
                        clashes.Add(new Clash { A = prev.Item1, CellA = prev.Item2, B = r, CellB = c });
                }
            return clashes;
        }

        /// <summary>Write the reports' pending values into `game` (in memory). Later reports win.</summary>
        public static (int written, int skipped) Apply(IEnumerable<Report> reports, GameParam game, bool keepOwnEdits)
        {
            int written = 0, skipped = 0;
            foreach (var r in reports)
                foreach (var c in r.Pending)
                {
                    if (keepOwnEdits && c.Conflict) { skipped++; continue; }
                    var param = game.Get(c.Param).param;
                    var row = param.Find(c.RowId) ?? AddRow(param, c, r);
                    c.Field.Write(row.Data, c.Target);
                    c.Current = c.Target;
                    written++;
                }
            return (written, skipped);
        }

        /// <summary>Adds a mod's new row. Fields the CSV doesn't set (e.g. ones only Remastered has,
        /// like levelSyncCorrectID) are copied from the nearest existing row with a lower ID.</summary>
        static Row AddRow(Param param, Cell cell, Report report)
        {
            var template = param.Rows.Where(x => x.Id < cell.RowId).OrderByDescending(x => x.Id).FirstOrDefault();
            var row = param.AddRow(cell.RowId, Bin.ShiftJis.GetBytes(cell.RowName));
            if (template == null) return row;
            var setByCsv = new HashSet<Field>(report.Cells.Where(x => x.RowId == cell.RowId).Select(x => x.Field));
            foreach (var f in param.Def.Fields)
                if (!f.IsPadding && !setByCsv.Contains(f))
                    f.Write(row.Data, f.Read(template.Data));
            return row;
        }
    }

    enum Game { Ptde, Remastered }

    static class Games
    {
        public static string Name(Game g) => g == Game.Ptde ? "Prepare to Die Edition" : "Remastered";

        /// <summary>Remastered's param file is DCX-compressed; PTDE's (unpacked with UXM) is not.</summary>
        public static Game Detect(byte[] file) => Dcx.IsDcx(file) ? Game.Remastered : Game.Ptde;

        public static string FileName(Game g) => g == Game.Ptde ? "GameParam.parambnd" : "GameParam.parambnd.dcx";

        /// <summary>Where the param file sits inside the game's Steam folder.</summary>
        public static string SteamSubPath(Game g) => g == Game.Ptde
            ? @"steamapps\common\Dark Souls Prepare to Die Edition\DATA\param\GameParam\GameParam.parambnd"
            : @"steamapps\common\DARK SOULS REMASTERED\param\GameParam\GameParam.parambnd.dcx";

        // Unmodified params and layouts of each game, embedded in the exe.
        public static string ParamResource(Game g) => g == Game.Ptde ? "ptde/GameParam.parambnd" : "dsr/GameParam.parambnd.dcx";
        public static string DefResource(Game g) => g == Game.Ptde ? "ptde/paramdef.paramdefbnd" : "dsr/paramdef.paramdefbnd.dcx";
    }

    /// <summary>Loaded vanilla + game params and the analysis of every CSV.</summary>
    class Session
    {
        public readonly string GamePath;
        public readonly Game Game;
        readonly Dictionary<string, ParamDef> defs;
        readonly GameParam vanilla;
        public readonly List<Report> Reports;
        /// <summary>The folder mods' game files are installed into (DATA for PTDE, the game folder for
        /// Remastered): two folders above the param file's "GameParam" folder. Null if the param file
        /// isn't in the game's param\GameParam folder.</summary>
        public readonly string GameRoot;
        /// <summary>For mods that install files: how many of their files aren't in the game folder yet.</summary>
        public readonly Dictionary<ModInfo, int> MissingFiles = new Dictionary<ModInfo, int>();

        public Session(string gamePath, string modsDir)
        {
            GamePath = gamePath;
            var file = File.ReadAllBytes(gamePath);
            Game = Games.Detect(file);
            defs = ParamDef.LoadAll(Patcher.Resource(Games.DefResource(Game)));
            vanilla = new GameParam(Patcher.Resource(Games.ParamResource(Game)), defs);
            var game = new GameParam(file, defs);
            var mods = Patcher.Discover(modsDir).Where(c => c.Info.SupportsGame(Game));
            Reports = Patcher.AnalyzeAll(mods, vanilla, game, Game);

            var paramDir = Path.GetDirectoryName(Path.GetFullPath(gamePath));
            if (string.Equals(Path.GetFileName(paramDir), "GameParam", StringComparison.OrdinalIgnoreCase)
                && string.Equals(Path.GetFileName(Path.GetDirectoryName(paramDir)), "param", StringComparison.OrdinalIgnoreCase))
                GameRoot = Path.GetDirectoryName(Path.GetDirectoryName(paramDir));
            foreach (var info in Reports.Select(r => r.Csv.Info).Distinct().Where(i => i.InstallDir != null))
                MissingFiles[info] = GameRoot == null ? info.InstallFiles().Count()
                    : info.InstallFiles().Count(f => !SameFile(f.source, Path.Combine(GameRoot, f.relative)));
            foreach (var r in Reports.Where(r => r.FilesOnly))
                r.MissingFiles = MissingFiles[r.Csv.Info];
        }

        static bool SameFile(string a, string b)
        {
            if (!File.Exists(b)) return false;
            if (new FileInfo(a).Length != new FileInfo(b).Length) return false;
            return File.ReadAllBytes(a).SequenceEqual(File.ReadAllBytes(b));
        }

        /// <summary>Copy the mods' game files into the game folder. Files that would be overwritten (and
        /// differ) are backed up next to themselves first. Returns (files copied, files backed up).</summary>
        public (int copied, int backedUp) InstallFiles(IEnumerable<ModInfo> mods)
        {
            if (GameRoot == null) throw new InvalidOperationException("The param file isn't in the game's param\\GameParam folder, so the game folder is unknown");
            string stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
            int copied = 0, backedUp = 0;
            foreach (var info in mods.Where(m => m.InstallDir != null))
                foreach (var (source, relative) in info.InstallFiles())
                {
                    string target = Path.Combine(GameRoot, relative);
                    if (SameFile(source, target)) continue;
                    Directory.CreateDirectory(Path.GetDirectoryName(target));
                    if (File.Exists(target)) { File.Copy(target, $"{target}.{stamp}.bak"); backedUp++; }
                    File.Copy(source, target, true);
                    copied++;
                }
            return (copied, backedUp);
        }

        /// <summary>Apply the selected CSVs to a fresh copy of the game file and return the new file.</summary>
        public (byte[] data, int written, int skipped) Build(IEnumerable<Report> selected, bool keepOwnEdits)
        {
            var target = new GameParam(File.ReadAllBytes(GamePath), defs);
            var csvs = Patcher.WithRequired(selected, Reports).Select(r => r.Csv);
            var reports = Patcher.AnalyzeAll(csvs, vanilla, target, Game);
            var (written, skipped) = Patcher.Apply(reports, target, keepOwnEdits);
            return (target.ToBytes(), written, skipped);
        }

        /// <summary>Back up the game's file next to it, then write `data` over it. Returns the backup path.</summary>
        public static string BackupAndReplace(string gamePath, byte[] data)
        {
            string backup = $"{gamePath}.{DateTime.Now:yyyyMMdd-HHmmss}.bak";
            File.Copy(gamePath, backup);
            string temp = gamePath + ".tmp";
            File.WriteAllBytes(temp, data);
            File.Copy(temp, gamePath, true);
            File.Delete(temp);
            return backup;
        }

        /// <summary>Best-effort search of Steam libraries for the game's param file.</summary>
        public static string FindGameParam(Game g)
        {
            string sub = Games.SteamSubPath(g);
            var roots = new[] { "ProgramFiles(x86)", "ProgramFiles" }
                .Select(v => Environment.GetEnvironmentVariable(v))
                .Where(v => !string.IsNullOrEmpty(v))
                .Select(v => Path.Combine(v, "Steam"))
                .ToList();
            var libraries = new List<string>(roots);
            foreach (var root in roots)
            {
                try
                {
                    string vdf = File.ReadAllText(Path.Combine(root, "steamapps", "libraryfolders.vdf"));
                    foreach (Match m in Regex.Matches(vdf, "\"path\"\\s+\"([^\"]+)\""))
                        libraries.Add(m.Groups[1].Value.Replace(@"\\", @"\"));
                }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
            foreach (var lib in libraries)
            {
                string candidate = Path.Combine(lib, sub);
                if (File.Exists(candidate)) return candidate;
            }
            return "";
        }
    }
}
