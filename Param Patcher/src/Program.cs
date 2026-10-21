// DS1 Param Patcher: apply this repo's CSV mods to your GameParam (Prepare to Die Edition or Remastered).
// No arguments: opens the window. With arguments: command line (run with --help).
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ParamPatcher
{
    static class Program
    {
        [DllImport("kernel32.dll")] static extern bool AttachConsole(int processId);
        [DllImport("user32.dll")] static extern bool SetProcessDPIAware();

        const string Help = @"Usage: ParamPatcher.exe [options]
  (no options)          open the window
  --game PATH           your game's param file (PTDE: DATA\param\GameParam\GameParam.parambnd,
                        Remastered: param\GameParam\GameParam.parambnd.dcx); found automatically if omitted
  --remastered          when --game is omitted, look for Remastered instead of PTDE
  --mods DIR            folder of mods to read (default: the ""mods"" folder next to this exe)
  --list                show every CSV and its status, then exit
  --apply CSV...        CSVs to apply, by number from --list or part of their path
  --all                 apply every CSV
  --output PATH         where to write the patched file
  --replace             back up the game's file and replace it with the patched one
  --keep-own-edits      don't overwrite values you already changed to something else
  -v, --verbose         list conflicting values";

        [STAThread]
        static int Main(string[] args)
        {
            if (args.Length > 0)
            {
                AttachConsole(-1); // print to the console we were started from, if any
                try { return RunCli(args); }
                catch (Exception e) { Console.Error.WriteLine("Error: " + e.Message); return 1; }
            }
            if (Environment.OSVersion.Version.Major >= 6) SetProcessDPIAware();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm(Patcher.DefaultModsDir()));
            return 0;
        }

        static int RunCli(string[] args)
        {
            string game = null, mods = null, output = null;
            bool list = false, all = false, replace = false, keepOwn = false, verbose = false, remastered = false;
            var apply = new List<string>();
            for (int i = 0; i < args.Length; i++)
            {
                string a = args[i];
                string Next() => i + 1 < args.Length ? args[++i] : throw new ArgumentException($"{a} needs a value");
                switch (a)
                {
                    case "--game": game = Next(); break;
                    case "--mods": case "--root": mods = Next(); break;
                    case "--output": output = Next(); break;
                    case "--list": list = true; break;
                    case "--remastered": remastered = true; break;
                    case "--all": all = true; break;
                    case "--replace": replace = true; break;
                    case "--keep-own-edits": keepOwn = true; break;
                    case "-v": case "--verbose": verbose = true; break;
                    case "--apply":
                        while (i + 1 < args.Length && !args[i + 1].StartsWith("--")) apply.Add(args[++i]);
                        break;
                    case "-h": case "--help": case "/?": Console.WriteLine(Help); return 0;
                    default: Console.Error.WriteLine($"Unknown option {a}\n\n{Help}"); return 2;
                }
            }
            game = game ?? Session.FindGameParam(remastered ? Game.Remastered : Game.Ptde);
            if (string.IsNullOrEmpty(game) || !File.Exists(game))
            {
                Console.Error.WriteLine("Could not find the game's param file; pass it with --game");
                return 2;
            }
            var session = new Session(game, mods ?? Patcher.DefaultModsDir());
            Console.WriteLine($"Game params ({Games.Name(session.Game)}): {game}\n");
            for (int i = 0; i < session.Reports.Count; i++)
            {
                var r = session.Reports[i];
                Console.WriteLine($"{i + 1,3}. [{StatusText.Of(r.Status)}] {r.Csv.RelPath}\n       {r.Summary()}");
                if (verbose)
                    foreach (var c in r.Conflicts) Console.WriteLine("         conflict: " + c.Describe());
            }
            if (list || (!all && apply.Count == 0)) return 0;

            var selected = new List<Report>();
            if (all) selected.AddRange(session.Reports.Where(r => r.Selectable));
            foreach (var token in apply)
            {
                var matches = session.Reports.Where((r, i) => token == (i + 1).ToString()
                    || r.Csv.RelPath.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
                if (matches.Count == 0) { Console.Error.WriteLine($"No CSV matches '{token}'"); return 2; }
                selected.AddRange(matches.Where(m => !selected.Contains(m)));
            }
            var bad = selected.FirstOrDefault(r => !r.Selectable);
            if (bad != null) { Console.Error.WriteLine($"Can't apply {bad.Csv.RelPath}: {bad.Error}"); return 2; }

            var asked = new HashSet<Report>(selected);
            selected = Patcher.WithRequired(selected, session.Reports);
            Console.WriteLine("\nSelected:");
            foreach (var r in selected)
                Console.WriteLine($"  - {r.Csv.RelPath} ({StatusText.Of(r.Status)})"
                                  + (asked.Contains(r) ? "" : " - added: this mod's files are applied together"));
            foreach (var c in Patcher.CrossConflicts(selected))
                Console.WriteLine($"  ! {c.A.Csv.Label} and {c.B.Csv.Label} disagree: {c.CellA.Describe()} vs {c.CellB.Field.Format(c.CellB.Target)} (later one wins)");

            var (data, written, skipped) = session.Build(selected, keepOwn);
            output = output ?? Path.Combine(Path.GetTempPath(), "patched." + Games.FileName(session.Game));
            File.WriteAllBytes(output, data);
            Console.WriteLine($"\nWrote {output}: {written} values changed" + (skipped > 0 ? $", {skipped} kept as your own edits" : ""));
            if (replace)
            {
                string backup = Session.BackupAndReplace(game, data);
                Console.WriteLine($"Replaced {game}\nBackup saved as {backup}");
                var withFiles = selected.Select(r => r.Csv.Info).Distinct().Where(m => m.InstallDir != null).ToList();
                if (withFiles.Count > 0 && session.GameRoot == null)
                    Console.WriteLine("Game files weren't installed: the param file isn't in the game's param\\GameParam folder, so the game folder is unknown.");
                else if (withFiles.Count > 0)
                {
                    var (copied, backedUp) = session.InstallFiles(withFiles);
                    Console.WriteLine($"Installed game files into {session.GameRoot}: {copied} copied, {backedUp} replaced files backed up (.bak)");
                }
            }
            foreach (var mod in selected.Select(r => r.Csv.Info).Distinct().Where(m => !m.AdditionalFiles.IsEmpty))
                Console.WriteLine($"\nAlso needed for {mod.DisplayName("en")}:\n  {mod.AdditionalFiles.Get("en").Replace("\n", "\n  ")}");
            return 0;
        }
    }
}
