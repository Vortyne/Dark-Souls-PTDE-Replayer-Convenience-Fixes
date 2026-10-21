// UI text in the ten languages Dark Souls 1 ships with. The command line stays English.
// Counts are written as "Label: n" so no language needs plural rules.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace ParamPatcher
{
    // Same order as the columns of each entry in Strings.Table.
    enum Lang { English, French, German, Italian, Japanese, Korean, Polish, Russian, Spanish, TChinese }

    static class Strings
    {
        public static Lang Current = Lang.English;
        public static event Action Changed;

        public static readonly (Lang lang, string native, string code)[] Languages =
        {
            (Lang.English, "English", "en"), (Lang.French, "Français", "fr"), (Lang.German, "Deutsch", "de"),
            (Lang.Italian, "Italiano", "it"), (Lang.Japanese, "日本語", "ja"), (Lang.Korean, "한국어", "ko"),
            (Lang.Polish, "Polski", "pl"), (Lang.Russian, "Русский", "ru"), (Lang.Spanish, "Español", "es"),
            (Lang.TChinese, "繁體中文", "zh-TW"),
        };

        public static string Native(Lang l) => Array.Find(Languages, x => x.lang == l).native;

        /// <summary>Code of the current language ("en", "ja", "zh-TW", ...), as used in mod.json.</summary>
        public static string Code => Array.Find(Languages, x => x.lang == Current).code;

        public static void Set(Lang l)
        {
            Current = l;
            SaveSetting(l);
            Changed?.Invoke();
        }

        /// <summary>Saved choice, else Windows' display language if DS1 has it, else English.</summary>
        public static Lang Initial()
        {
            try
            {
                string code = File.ReadAllText(SettingsPath).Trim();
                foreach (var x in Languages) if (x.code == code) return x.lang;
            }
            catch (Exception) { /* no saved choice */ }
            var ui = CultureInfo.CurrentUICulture;
            if (ui.TwoLetterISOLanguageName == "zh")
                return ui.Name.Contains("TW") || ui.Name.Contains("HK") || ui.Name.Contains("MO") || ui.Name.Contains("Hant") ? Lang.TChinese : Lang.English;
            foreach (var x in Languages) if (x.code == ui.TwoLetterISOLanguageName) return x.lang;
            return Lang.English;
        }

        static string SettingsPath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DS1ParamPatcher", "language.txt");

        static void SaveSetting(Lang l)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath));
                File.WriteAllText(SettingsPath, Array.Find(Languages, x => x.lang == l).code);
            }
            catch (Exception) { /* not important enough to bother the user */ }
        }

        public static string T(string key, params object[] args)
        {
            if (!Table.TryGetValue(key, out var row)) return key;
            string s = row[(int)Current];
            return args.Length > 0 ? string.Format(s, args) : s;
        }

        // en, fr, de, it, ja, ko, pl, ru, es, zh-TW
        static readonly Dictionary<string, string[]> Table = new Dictionary<string, string[]>
        {
            ["subtitle"] = new[] {
                "Tick the mods you want, then patch your game's param file. Only the values each mod changes are written, so your other edits are kept.",
                "Cochez les mods souhaités, puis patchez le fichier de paramètres du jeu. Seules les valeurs modifiées par chaque mod sont écrites : vos autres modifications sont conservées.",
                "Wähle die gewünschten Mods aus und patche dann die Parameterdatei deines Spiels. Es werden nur die Werte geschrieben, die ein Mod ändert – deine anderen Änderungen bleiben erhalten.",
                "Seleziona le mod che desideri, poi applica la patch al file dei parametri del gioco. Vengono scritti solo i valori modificati da ciascuna mod, quindi le tue altre modifiche restano intatte.",
                "使用したいMODにチェックを入れ、ゲームのパラメータファイルにパッチを適用します。各MODが変更する値だけが書き込まれるため、他の変更はそのまま残ります。",
                "원하는 모드를 선택한 다음 게임의 파라미터 파일에 패치를 적용하세요. 각 모드가 변경하는 값만 기록되므로 다른 수정 사항은 그대로 유지됩니다.",
                "Zaznacz wybrane mody, a następnie załataj plik parametrów gry. Zapisywane są tylko wartości zmieniane przez dany mod, więc Twoje pozostałe zmiany zostają zachowane.",
                "Отметьте нужные моды и примените их к файлу параметров игры. Записываются только значения, которые изменяет мод, поэтому другие ваши правки сохраняются.",
                "Marca los mods que quieras y aplica el parche al archivo de parámetros del juego. Solo se escriben los valores que cambia cada mod, así que tus demás cambios se conservan.",
                "勾選想要的模組，然後修補遊戲的參數檔。只會寫入各模組變更的數值，因此您的其他修改都會保留。" },
            ["game"] = new[] { "Game", "Jeu", "Spiel", "Gioco", "ゲーム", "게임", "Gra", "Игра", "Juego", "遊戲" },
            ["game.ptde"] = new[] {
                "Prepare to Die Edition (recommended)", "Prepare to Die Edition (recommandé)", "Prepare to Die Edition (empfohlen)",
                "Prepare to Die Edition (consigliata)", "Prepare to Die Edition（推奨）", "Prepare to Die Edition (권장)",
                "Prepare to Die Edition (zalecane)", "Prepare to Die Edition (рекомендуется)", "Prepare to Die Edition (recomendado)",
                "Prepare to Die Edition（建議）" },
            ["game.dsr"] = new[] {
                "Remastered (offline only)", "Remastered (hors ligne uniquement)", "Remastered (nur offline)", "Remastered (solo offline)",
                "Remastered（オフライン専用）", "Remastered (오프라인 전용)", "Remastered (tylko offline)", "Remastered (только офлайн)",
                "Remastered (solo sin conexión)", "Remastered（僅限離線）" },
            ["warn.dsr"] = new[] {
                "Remastered: only play offline with these mods. They change game balance, and playing online with modified game data isn't fair to other players and can get your account flagged by the game's servers.",
                "Remastered : jouez uniquement hors ligne avec ces mods. Ils modifient l'équilibrage du jeu, et jouer en ligne avec des données modifiées est injuste envers les autres joueurs et peut faire signaler votre compte par les serveurs du jeu.",
                "Remastered: Spiele mit diesen Mods nur offline. Sie verändern die Spielbalance, und online mit veränderten Spieldaten zu spielen ist unfair gegenüber anderen Spielern und kann dazu führen, dass dein Konto von den Servern markiert wird.",
                "Remastered: gioca solo offline con queste mod. Modificano il bilanciamento del gioco, e giocare online con dati modificati è scorretto verso gli altri giocatori e può far segnalare il tuo account dai server del gioco.",
                "Remastered：これらのMODを使う場合はオフラインでのみプレイしてください。ゲームバランスが変わるため、改変したデータでオンラインプレイをすると他のプレイヤーに不公平であり、サーバーによってアカウントにフラグが付けられる可能性があります。",
                "Remastered: 이 모드는 오프라인에서만 사용하세요. 게임 밸런스가 바뀌므로, 수정된 데이터로 온라인 플레이를 하면 다른 플레이어에게 불공정하며 게임 서버에 의해 계정이 플래그될 수 있습니다.",
                "Remastered: graj z tymi modami wyłącznie offline. Zmieniają one balans gry, a gra online ze zmodyfikowanymi danymi jest nieuczciwa wobec innych graczy i może skończyć się oznaczeniem konta przez serwery gry.",
                "Remastered: играйте с этими модами только офлайн. Они меняют баланс игры, а игра по сети с изменёнными данными нечестна по отношению к другим игрокам и может привести к пометке вашей учётной записи серверами игры.",
                "Remastered: juega solo sin conexión con estos mods. Cambian el equilibrio del juego, y jugar en línea con datos modificados es injusto para otros jugadores y puede hacer que los servidores marquen tu cuenta.",
                "Remastered：使用這些模組時請僅在離線狀態下遊玩。它們會改變遊戲平衡，以修改過的資料進行線上遊戲對其他玩家不公平，且您的帳號可能會被遊戲伺服器標記。" },
            ["file"] = new[] {
                "Your game's param file", "Fichier de paramètres du jeu", "Parameterdatei deines Spiels", "File dei parametri del gioco",
                "ゲームのパラメータファイル", "게임 파라미터 파일", "Plik parametrów gry", "Файл параметров игры",
                "Archivo de parámetros del juego", "遊戲的參數檔" },
            ["browse"] = new[] { "Browse...", "Parcourir...", "Durchsuchen...", "Sfoglia...", "参照...", "찾아보기...", "Przeglądaj...", "Обзор...", "Examinar...", "瀏覽..." },
            ["file.note"] = new[] {
                "This file will be automatically backed up before you patch it",
                "Ce fichier sera automatiquement sauvegardé avant d'être patché",
                "Diese Datei wird vor dem Patchen automatisch gesichert",
                "Prima della patch verrà creata automaticamente una copia di backup di questo file",
                "このファイルはパッチ適用前に自動でバックアップされます",
                "이 파일은 패치 전에 자동으로 백업됩니다",
                "Przed załataniem zostanie automatycznie utworzona kopia zapasowa tego pliku",
                "Перед применением патча будет автоматически создана резервная копия этого файла",
                "Se hará una copia de seguridad automática de este archivo antes de aplicar el parche",
                "修補前會自動備份此檔案" },
            ["where.ptde"] = new[] {
                "It's DATA\\param\\GameParam\\GameParam.parambnd in your game folder, after unpacking the game with UXM Selective Unpack.",
                "Il se trouve dans DATA\\param\\GameParam\\GameParam.parambnd dans le dossier du jeu, après avoir décompressé le jeu avec UXM Selective Unpack.",
                "Sie liegt unter DATA\\param\\GameParam\\GameParam.parambnd im Spielordner, nachdem das Spiel mit UXM Selective Unpack entpackt wurde.",
                "Si trova in DATA\\param\\GameParam\\GameParam.parambnd nella cartella del gioco, dopo aver estratto il gioco con UXM Selective Unpack.",
                "UXM Selective Unpackでゲームを展開した後、ゲームフォルダ内の DATA\\param\\GameParam\\GameParam.parambnd にあります。",
                "UXM Selective Unpack으로 게임 압축을 푼 뒤 게임 폴더의 DATA\\param\\GameParam\\GameParam.parambnd에 있습니다.",
                "Znajduje się w DATA\\param\\GameParam\\GameParam.parambnd w folderze gry, po rozpakowaniu gry za pomocą UXM Selective Unpack.",
                "Он находится в DATA\\param\\GameParam\\GameParam.parambnd в папке игры после распаковки игры с помощью UXM Selective Unpack.",
                "Está en DATA\\param\\GameParam\\GameParam.parambnd dentro de la carpeta del juego, tras desempaquetar el juego con UXM Selective Unpack.",
                "使用 UXM Selective Unpack 解包遊戲後，位於遊戲資料夾中的 DATA\\param\\GameParam\\GameParam.parambnd。" },
            ["where.dsr"] = new[] {
                "It's param\\GameParam\\GameParam.parambnd.dcx in your DARK SOULS REMASTERED folder.",
                "Il se trouve dans param\\GameParam\\GameParam.parambnd.dcx dans le dossier DARK SOULS REMASTERED.",
                "Sie liegt unter param\\GameParam\\GameParam.parambnd.dcx im Ordner DARK SOULS REMASTERED.",
                "Si trova in param\\GameParam\\GameParam.parambnd.dcx nella cartella DARK SOULS REMASTERED.",
                "DARK SOULS REMASTERED フォルダ内の param\\GameParam\\GameParam.parambnd.dcx にあります。",
                "DARK SOULS REMASTERED 폴더의 param\\GameParam\\GameParam.parambnd.dcx에 있습니다.",
                "Znajduje się w param\\GameParam\\GameParam.parambnd.dcx w folderze DARK SOULS REMASTERED.",
                "Он находится в param\\GameParam\\GameParam.parambnd.dcx в папке DARK SOULS REMASTERED.",
                "Está en param\\GameParam\\GameParam.parambnd.dcx dentro de la carpeta DARK SOULS REMASTERED.",
                "位於 DARK SOULS REMASTERED 資料夾中的 param\\GameParam\\GameParam.parambnd.dcx。" },
            ["empty.select"] = new[] {
                "Select your {0} param file above (Browse...).", "Sélectionnez ci-dessus le fichier de paramètres de {0} (Parcourir...).",
                "Wähle oben die Parameterdatei von {0} aus (Durchsuchen...).", "Seleziona qui sopra il file dei parametri di {0} (Sfoglia...).",
                "上で {0} のパラメータファイルを選択してください（参照...）。", "위에서 {0} 파라미터 파일을 선택하세요 (찾아보기...).",
                "Wybierz powyżej plik parametrów {0} (Przeglądaj...).", "Выберите выше файл параметров {0} (Обзор...).",
                "Selecciona arriba el archivo de parámetros de {0} (Examinar...).", "請在上方選擇 {0} 的參數檔（瀏覽...）。" },
            ["loading"] = new[] {
                "Loading your param file...", "Chargement du fichier de paramètres...", "Parameterdatei wird geladen...",
                "Caricamento del file dei parametri...", "パラメータファイルを読み込んでいます...", "파라미터 파일을 불러오는 중...",
                "Wczytywanie pliku parametrów...", "Загрузка файла параметров...", "Cargando el archivo de parámetros...", "正在載入參數檔..." },
            ["nocsv"] = new[] {
                "No mods found in {0}.\n\nPut each mod in its own folder inside the \"mods\" folder next to this program.",
                "Aucun mod trouvé dans {0}.\n\nPlacez chaque mod dans son propre dossier à l'intérieur du dossier « mods » situé à côté de ce programme.",
                "Keine Mods in {0} gefunden.\n\nLege jeden Mod in einen eigenen Ordner im Ordner „mods“ neben diesem Programm.",
                "Nessuna mod trovata in {0}.\n\nMetti ogni mod in una propria cartella dentro la cartella \"mods\" accanto a questo programma.",
                "{0} にMODが見つかりません。\n\n各MODを、このプログラムと同じ場所にある「mods」フォルダ内の個別のフォルダに入れてください。",
                "{0}에서 모드를 찾을 수 없습니다.\n\n각 모드를 이 프로그램 옆의 \"mods\" 폴더 안에 개별 폴더로 넣으세요.",
                "Nie znaleziono modów w {0}.\n\nUmieść każdy mod w osobnym folderze w folderze „mods” obok tego programu.",
                "В {0} не найдено модов.\n\nПоместите каждый мод в отдельную папку внутри папки «mods» рядом с этой программой.",
                "No se encontraron mods en {0}.\n\nPon cada mod en su propia carpeta dentro de la carpeta «mods» junto a este programa.",
                "在 {0} 中找不到模組。\n\n請將每個模組放在此程式旁「mods」資料夾內各自的資料夾中。" },
            ["mod.together"] = new[] { "Applied together", "Appliqués ensemble", "Werden zusammen angewendet", "Applicati insieme", "まとめて適用", "함께 적용됨", "Stosowane razem", "Применяются вместе", "Se aplican juntos", "一併套用" },
            ["mod.extra"] = new[] { "Needs extra files", "Fichiers supplémentaires requis", "Benötigt zusätzliche Dateien", "Richiede file aggiuntivi", "追加ファイルが必要", "추가 파일 필요", "Wymaga dodatkowych plików", "Нужны доп. файлы", "Requiere archivos adicionales", "需要額外檔案" },
            ["extra.title"] = new[] {
                "This mod needs extra files that the patcher can't install:", "Ce mod nécessite des fichiers supplémentaires que le patcheur ne peut pas installer :",
                "Dieser Mod benötigt zusätzliche Dateien, die der Patcher nicht installieren kann:", "Questa mod richiede file aggiuntivi che il patcher non può installare:",
                "このMODには、パッチャーではインストールできない追加ファイルが必要です：", "이 모드에는 패처가 설치할 수 없는 추가 파일이 필요합니다:",
                "Ten mod wymaga dodatkowych plików, których patcher nie może zainstalować:", "Этому моду нужны дополнительные файлы, которые патчер не может установить:",
                "Este mod necesita archivos adicionales que el parcheador no puede instalar:", "此模組需要修補程式無法安裝的額外檔案：" },
            ["extra.after"] = new[] {
                "Don't forget the extra files these mods need:", "N'oubliez pas les fichiers supplémentaires nécessaires à ces mods :",
                "Vergiss nicht die zusätzlichen Dateien, die diese Mods benötigen:", "Non dimenticare i file aggiuntivi richiesti da queste mod:",
                "これらのMODに必要な追加ファイルを忘れずにコピーしてください：", "이 모드에 필요한 추가 파일을 잊지 마세요:",
                "Nie zapomnij o dodatkowych plikach wymaganych przez te mody:", "Не забудьте про дополнительные файлы, нужные этим модам:",
                "No olvides los archivos adicionales que necesitan estos mods:", "別忘了這些模組需要的額外檔案：" },
            ["files.row"] = new[] { "Game files", "Fichiers du jeu", "Spieldateien", "File di gioco", "ゲームファイル", "게임 파일", "Pliki gry", "Файлы игры", "Archivos del juego", "遊戲檔案" },
            ["btn.changes"] = new[] { "Full List of Changes", "Liste complète des changements", "Vollständige Liste der Änderungen", "Elenco completo delle modifiche", "変更点の一覧", "전체 변경 사항 목록", "Pełna lista zmian", "Полный список изменений", "Lista completa de cambios", "完整變更清單" },
            ["files.installed"] = new[] { "Game files: installed", "Fichiers du jeu : installés", "Spieldateien: installiert", "File di gioco: installati", "ゲームファイル：インストール済み", "게임 파일: 설치됨", "Pliki gry: zainstalowane", "Файлы игры: установлены", "Archivos del juego: instalados", "遊戲檔案：已安裝" },
            ["files.missing"] = new[] { "Game files to install: {0}", "Fichiers du jeu à installer : {0}", "Zu installierende Spieldateien: {0}", "File di gioco da installare: {0}", "インストールするゲームファイル：{0}", "설치할 게임 파일: {0}", "Pliki gry do zainstalowania: {0}", "Файлов игры к установке: {0}", "Archivos del juego por instalar: {0}", "待安裝的遊戲檔案：{0}" },
            ["confirm.files"] = new[] {
                "Game files will also be installed for: {0}", "Des fichiers du jeu seront aussi installés pour : {0}", "Außerdem werden Spieldateien installiert für: {0}",
                "Verranno installati anche file di gioco per: {0}", "次のMODのゲームファイルもインストールされます：{0}", "다음 모드의 게임 파일도 설치됩니다: {0}",
                "Zostaną też zainstalowane pliki gry dla: {0}", "Также будут установлены файлы игры для: {0}", "También se instalarán archivos del juego para: {0}",
                "也會為以下模組安裝遊戲檔案：{0}" },
            ["files.done"] = new[] {
                "Game files installed: {0} (replaced files were backed up next to them as .bak)",
                "Fichiers du jeu installés : {0} (les fichiers remplacés ont été sauvegardés à côté en .bak)",
                "Installierte Spieldateien: {0} (ersetzte Dateien wurden daneben als .bak gesichert)",
                "File di gioco installati: {0} (i file sostituiti sono stati salvati accanto come .bak)",
                "インストールしたゲームファイル：{0}（置き換えたファイルは .bak として同じ場所にバックアップしました）",
                "설치한 게임 파일: {0} (교체된 파일은 같은 위치에 .bak으로 백업됨)",
                "Zainstalowane pliki gry: {0} (zastąpione pliki zapisano obok jako .bak)",
                "Установлено файлов игры: {0} (заменённые файлы сохранены рядом как .bak)",
                "Archivos del juego instalados: {0} (los archivos reemplazados se guardaron al lado como .bak)",
                "已安裝的遊戲檔案：{0}（被取代的檔案已在原處備份為 .bak）" },
            ["files.noRoot"] = new[] {
                "Game files weren't installed: the param file isn't inside the game's param\\GameParam folder, so the game folder is unknown.",
                "Les fichiers du jeu n'ont pas été installés : le fichier de paramètres n'est pas dans le dossier param\\GameParam du jeu, le dossier du jeu est donc inconnu.",
                "Spieldateien wurden nicht installiert: Die Parameterdatei liegt nicht im Ordner param\\GameParam des Spiels, daher ist der Spielordner unbekannt.",
                "I file di gioco non sono stati installati: il file dei parametri non si trova nella cartella param\\GameParam del gioco, quindi la cartella del gioco è sconosciuta.",
                "ゲームファイルはインストールされませんでした：パラメータファイルがゲームの param\\GameParam フォルダ内にないため、ゲームフォルダが特定できません。",
                "게임 파일을 설치하지 않았습니다: 파라미터 파일이 게임의 param\\GameParam 폴더에 없어 게임 폴더를 알 수 없습니다.",
                "Nie zainstalowano plików gry: plik parametrów nie znajduje się w folderze param\\GameParam gry, więc folder gry jest nieznany.",
                "Файлы игры не установлены: файл параметров находится не в папке param\\GameParam игры, поэтому папка игры неизвестна.",
                "No se instalaron los archivos del juego: el archivo de parámetros no está en la carpeta param\\GameParam del juego, así que se desconoce la carpeta del juego.",
                "未安裝遊戲檔案：參數檔不在遊戲的 param\\GameParam 資料夾內，因此無法確定遊戲資料夾。" },
            ["files.onlyPatch"] = new[] {
                "Game files are only installed by \"Patch game file\"; this copy doesn't include them.",
                "Les fichiers du jeu ne sont installés que par « Patcher le fichier du jeu » ; cette copie ne les inclut pas.",
                "Spieldateien werden nur mit „Spieldatei patchen“ installiert; diese Kopie enthält sie nicht.",
                "I file di gioco vengono installati solo con \"Applica patch al file del gioco\"; questa copia non li include.",
                "ゲームファイルは「ゲームファイルにパッチ適用」でのみインストールされます。このコピーには含まれません。",
                "게임 파일은 \"게임 파일 패치\"로만 설치됩니다. 이 사본에는 포함되지 않습니다.",
                "Pliki gry są instalowane tylko przez „Załataj plik gry”; ta kopia ich nie zawiera.",
                "Файлы игры устанавливаются только через «Пропатчить файл игры»; в эту копию они не входят.",
                "Los archivos del juego solo se instalan con «Parchear archivo del juego»; esta copia no los incluye.",
                "遊戲檔案只會透過「修補遊戲檔案」安裝；此副本不包含它們。" },
            ["author"] = new[] { "By {0}", "Par {0}", "Von {0}", "Di {0}", "作者：{0}", "제작자: {0}", "Autor: {0}", "Автор: {0}", "Por {0}", "作者：{0}" },
            ["n.files"] = new[] { "Files: {0}", "Fichiers : {0}", "Dateien: {0}", "File: {0}", "ファイル数：{0}", "파일: {0}", "Pliki: {0}", "Файлов: {0}", "Archivos: {0}", "檔案：{0}" },
            ["n.selected"] = new[] { "Selected: {0}", "Sélectionnés : {0}", "Ausgewählt: {0}", "Selezionati: {0}", "選択中：{0}", "선택됨: {0}", "Zaznaczone: {0}", "Выбрано: {0}", "Seleccionados: {0}", "已選擇：{0}" },
            ["n.values"] = new[] { "Values: {0}", "Valeurs : {0}", "Werte: {0}", "Valori: {0}", "変更値：{0}", "값: {0}", "Wartości: {0}", "Значений: {0}", "Valores: {0}", "數值：{0}" },
            ["n.applied"] = new[] { "Applied: {0}", "Appliquées : {0}", "Angewendet: {0}", "Applicati: {0}", "適用済み：{0}", "적용됨: {0}", "Zastosowane: {0}", "Применено: {0}", "Aplicados: {0}", "已套用：{0}" },
            ["n.conflicts"] = new[] { "Conflicts: {0}", "Conflits : {0}", "Konflikte: {0}", "Conflitti: {0}", "競合：{0}", "충돌: {0}", "Konflikty: {0}", "Конфликтов: {0}", "Conflictos: {0}", "衝突：{0}" },
            ["status.notApplied"] = new[] { "Not applied", "Non appliqué", "Nicht angewendet", "Non applicata", "未適用", "미적용", "Nie zastosowano", "Не применён", "No aplicado", "未套用" },
            ["status.partial"] = new[] { "Partially applied", "Partiellement appliqué", "Teilweise angewendet", "Applicata in parte", "一部適用済み", "일부 적용됨", "Częściowo zastosowano", "Применён частично", "Aplicado en parte", "部分套用" },
            ["status.applied"] = new[] { "Already applied", "Déjà appliqué", "Bereits angewendet", "Già applicata", "適用済み", "이미 적용됨", "Już zastosowano", "Уже применён", "Ya aplicado", "已套用" },
            ["status.conflict"] = new[] { "Conflicts", "Conflits", "Konflikte", "Conflitti", "競合あり", "충돌", "Konflikty", "Конфликты", "Conflictos", "有衝突" },
            ["status.noEffect"] = new[] { "No changes", "Aucun changement", "Keine Änderungen", "Nessuna modifica", "変更なし", "변경 없음", "Brak zmian", "Нет изменений", "Sin cambios", "無變更" },
            ["status.error"] = new[] { "Error", "Erreur", "Fehler", "Errore", "エラー", "오류", "Błąd", "Ошибка", "Error", "錯誤" },
            ["details"] = new[] { "Details", "Détails", "Details", "Dettagli", "詳細", "세부 정보", "Szczegóły", "Подробности", "Detalles", "詳細資訊" },
            ["details.hint"] = new[] {
                "Click a mod or file to see exactly which values it changes.",
                "Cliquez sur un mod ou un fichier pour voir précisément les valeurs qu'il modifie.",
                "Klicke auf einen Mod oder eine Datei, um genau zu sehen, welche Werte geändert werden.",
                "Fai clic su una mod o su un file per vedere esattamente quali valori modifica.",
                "MODまたはファイルをクリックすると、変更される値を正確に確認できます。",
                "모드나 파일을 클릭하면 어떤 값이 변경되는지 정확히 볼 수 있습니다.",
                "Kliknij mod lub plik, aby zobaczyć dokładnie, które wartości zmienia.",
                "Щёлкните мод или файл, чтобы увидеть, какие именно значения он меняет.",
                "Haz clic en un mod o archivo para ver exactamente qué valores cambia.",
                "點選模組或檔案即可查看它變更的確切數值。" },
            ["col.name"] = new[] { "Name", "Nom", "Name", "Nome", "名前", "이름", "Nazwa", "Название", "Nombre", "名稱" },
            ["col.field"] = new[] { "Field", "Champ", "Feld", "Campo", "項目", "필드", "Pole", "Поле", "Campo", "欄位" },
            ["col.original"] = new[] { "Original", "Original", "Original", "Originale", "元の値", "원본", "Oryginał", "Исходное", "Original", "原始值" },
            ["col.yours"] = new[] { "Your file", "Votre fichier", "Deine Datei", "Tuo file", "現在の値", "내 파일", "Twój plik", "Ваш файл", "Tu archivo", "您的檔案" },
            ["col.mod"] = new[] { "Mod", "Mod", "Mod", "Mod", "MOD", "모드", "Mod", "Мод", "Mod", "模組" },
            ["col.state"] = new[] { "State", "État", "Status", "Stato", "状態", "상태", "Stan", "Состояние", "Estado", "狀態" },
            ["col.row"] = new[] { "Row", "Ligne", "Zeile", "Riga", "行", "행", "Wiersz", "Строка", "Fila", "列" },
            ["col.param"] = new[] { "Param", "Paramètre", "Parameter", "Parametro", "パラメータ", "파라미터", "Parametr", "Параметр", "Parámetro", "參數" },
            ["state.conflict"] = new[] { "Conflict", "Conflit", "Konflikt", "Conflitto", "競合", "충돌", "Konflikt", "Конфликт", "Conflicto", "衝突" },
            ["state.will"] = new[] { "Will change", "Sera modifié", "Wird geändert", "Verrà modificato", "変更予定", "변경 예정", "Zostanie zmienione", "Будет изменено", "Se cambiará", "將變更" },
            ["val.newRow"] = new[] { "(new row)", "(nouvelle ligne)", "(neue Zeile)", "(nuova riga)", "（新規行）", "(새 행)", "(nowy wiersz)", "(новая строка)", "(fila nueva)", "（新列）" },
            ["val.missing"] = new[] { "(missing)", "(absent)", "(fehlt)", "(assente)", "（なし）", "(없음)", "(brak)", "(нет)", "(no existe)", "（不存在）" },
            ["more"] = new[] { "...and {0} more", "... et {0} de plus", "... und {0} weitere", "... e altri {0}", "...ほか {0} 件", "...외 {0}개", "... i {0} więcej", "... и ещё {0}", "... y {0} más", "...以及其他 {0} 項" },
            ["clash"] = new[] {
                "Two selected mods change the same value differently (the one lower in the list wins):",
                "Deux mods sélectionnés modifient la même valeur différemment (celui placé le plus bas dans la liste l'emporte) :",
                "Zwei ausgewählte Mods ändern denselben Wert unterschiedlich (der weiter unten in der Liste gewinnt):",
                "Due mod selezionate modificano lo stesso valore in modo diverso (prevale quella più in basso nell'elenco):",
                "選択した2つのMODが同じ値を異なる値に変更します（リストの下にある方が優先されます）：",
                "선택한 두 모드가 같은 값을 다르게 변경합니다 (목록에서 아래에 있는 모드가 우선합니다):",
                "Dwa zaznaczone mody zmieniają tę samą wartość inaczej (wygrywa ten niżej na liście):",
                "Два выбранных мода по-разному меняют одно и то же значение (побеждает тот, что ниже в списке):",
                "Dos mods seleccionados cambian el mismo valor de forma distinta (gana el que está más abajo en la lista):",
                "兩個已選擇的模組將同一數值改為不同的值（以清單中較下方者為準）：" },
            ["footer.none"] = new[] { "Nothing selected", "Aucune sélection", "Nichts ausgewählt", "Nessuna selezione", "何も選択されていません", "선택된 항목 없음", "Nic nie zaznaczono", "Ничего не выбрано", "Nada seleccionado", "未選擇任何項目" },
            ["footer.mods"] = new[] {
                "Mods selected: {0} (files: {1})", "Mods sélectionnés : {0} (fichiers : {1})", "Ausgewählte Mods: {0} (Dateien: {1})",
                "Mod selezionate: {0} (file: {1})", "選択中のMOD：{0}（ファイル：{1}）", "선택한 모드: {0} (파일: {1})",
                "Zaznaczone mody: {0} (pliki: {1})", "Выбрано модов: {0} (файлов: {1})", "Mods seleccionados: {0} (archivos: {1})",
                "已選擇的模組：{0}（檔案：{1}）" },
            ["footer.conflicts"] = new[] {
                "Conflicting with your edits: {0}", "En conflit avec vos modifications : {0}", "Konflikte mit deinen Änderungen: {0}",
                "In conflitto con le tue modifiche: {0}", "あなたの変更との競合：{0}", "내 수정 사항과 충돌: {0}",
                "W konflikcie z Twoimi zmianami: {0}", "Конфликтов с вашими правками: {0}", "En conflicto con tus cambios: {0}", "與您的修改衝突：{0}" },
            ["footer.clashes"] = new[] {
                "Set differently by two mods: {0}", "Définies différemment par deux mods : {0}", "Von zwei Mods unterschiedlich gesetzt: {0}",
                "Impostati in modo diverso da due mod: {0}", "2つのMODで異なる値：{0}", "두 모드가 다르게 설정: {0}",
                "Różnie ustawione przez dwa mody: {0}", "По-разному задано двумя модами: {0}", "Definidos de forma distinta por dos mods: {0}", "兩個模組設定不同：{0}" },
            ["keep"] = new[] {
                "Keep my own edits where they conflict with a mod", "Conserver mes propres modifications en cas de conflit avec un mod",
                "Eigene Änderungen behalten, wenn sie mit einem Mod in Konflikt stehen", "Mantieni le mie modifiche in caso di conflitto con una mod",
                "MODと競合する場合は自分の変更を残す", "모드와 충돌할 때 내 수정 사항 유지", "Zachowaj moje zmiany, gdy kolidują z modem",
                "Сохранять мои правки при конфликте с модом", "Conservar mis propios cambios si entran en conflicto con un mod", "與模組衝突時保留我自己的修改" },
            ["btn.pending"] = new[] {
                "Select all not yet applied", "Sélectionner tout ce qui n'est pas appliqué", "Alle noch nicht angewendeten auswählen",
                "Seleziona tutte quelle non applicate", "未適用をすべて選択", "미적용 항목 모두 선택", "Zaznacz wszystkie niezastosowane",
                "Выбрать все неприменённые", "Seleccionar todo lo no aplicado", "選擇所有尚未套用的項目" },
            ["btn.clear"] = new[] { "Clear", "Effacer", "Leeren", "Deseleziona", "クリア", "선택 해제", "Wyczyść", "Сбросить", "Borrar", "清除" },
            ["btn.save"] = new[] {
                "Save patched copy...", "Enregistrer une copie patchée...", "Gepatchte Kopie speichern...", "Salva copia con patch...",
                "パッチ適用済みのコピーを保存...", "패치된 사본 저장...", "Zapisz załataną kopię...", "Сохранить копию с патчем...",
                "Guardar copia parcheada...", "儲存已修補的副本..." },
            ["btn.patch"] = new[] {
                "Patch game file", "Patcher le fichier du jeu", "Spieldatei patchen", "Applica patch al file del gioco",
                "ゲームファイルにパッチ適用", "게임 파일 패치", "Załataj plik gry", "Пропатчить файл игры", "Parchear archivo del juego", "修補遊戲檔案" },
            ["confirm"] = new[] {
                "Files to apply: {0}\nGame: {1}\n{2}\n\nYour current file will be backed up next to it first.",
                "Fichiers à appliquer : {0}\nJeu : {1}\n{2}\n\nVotre fichier actuel sera d'abord sauvegardé à côté.",
                "Anzuwendende Dateien: {0}\nSpiel: {1}\n{2}\n\nDeine aktuelle Datei wird zuerst daneben gesichert.",
                "File da applicare: {0}\nGioco: {1}\n{2}\n\nIl file attuale verrà prima salvato come backup accanto ad esso.",
                "適用するファイル数：{0}\nゲーム：{1}\n{2}\n\n現在のファイルは先に同じ場所へバックアップされます。",
                "적용할 파일: {0}\n게임: {1}\n{2}\n\n현재 파일은 먼저 같은 폴더에 백업됩니다.",
                "Pliki do zastosowania: {0}\nGra: {1}\n{2}\n\nNajpierw obok zostanie utworzona kopia zapasowa obecnego pliku.",
                "Файлов к применению: {0}\nИгра: {1}\n{2}\n\nСначала рядом будет создана резервная копия текущего файла.",
                "Archivos que se aplicarán: {0}\nJuego: {1}\n{2}\n\nPrimero se hará una copia de seguridad de tu archivo actual junto a él.",
                "要套用的檔案：{0}\n遊戲：{1}\n{2}\n\n會先在同一位置備份您目前的檔案。" },
            ["confirm.dsr"] = new[] {
                "Remember: only play Remastered offline with these mods.", "Rappel : avec ces mods, jouez à Remastered uniquement hors ligne.",
                "Denk daran: Spiele Remastered mit diesen Mods nur offline.", "Ricorda: con queste mod gioca a Remastered solo offline.",
                "注意：これらのMODを使う場合、Remasteredはオフラインでのみプレイしてください。", "주의: 이 모드를 사용할 때는 Remastered를 오프라인에서만 플레이하세요.",
                "Pamiętaj: z tymi modami graj w Remastered wyłącznie offline.", "Помните: с этими модами играйте в Remastered только офлайн.",
                "Recuerda: con estos mods, juega a Remastered solo sin conexión.", "提醒：使用這些模組時，Remastered 請僅離線遊玩。" },
            ["done.changed"] = new[] { "Values changed: {0}", "Valeurs modifiées : {0}", "Geänderte Werte: {0}", "Valori modificati: {0}", "変更した値：{0}", "변경된 값: {0}", "Zmienione wartości: {0}", "Изменено значений: {0}", "Valores cambiados: {0}", "已變更的數值：{0}" },
            ["done.kept"] = new[] { "Kept as your own edits: {0}", "Conservées comme vos modifications : {0}", "Als eigene Änderungen behalten: {0}", "Mantenuti come tue modifiche: {0}", "自分の変更として保持：{0}", "내 수정 사항으로 유지: {0}", "Zachowane jako Twoje zmiany: {0}", "Оставлено ваших правок: {0}", "Conservados como tus cambios: {0}", "保留為您的修改：{0}" },
            ["done.patched"] = new[] {
                "Your game file is patched.\nBackup of the previous file:\n{0}", "Le fichier du jeu a été patché.\nSauvegarde du fichier précédent :\n{0}",
                "Deine Spieldatei wurde gepatcht.\nSicherung der vorherigen Datei:\n{0}", "La patch è stata applicata al file del gioco.\nBackup del file precedente:\n{0}",
                "ゲームファイルにパッチを適用しました。\n以前のファイルのバックアップ：\n{0}", "게임 파일이 패치되었습니다.\n이전 파일 백업:\n{0}",
                "Plik gry został załatany.\nKopia zapasowa poprzedniego pliku:\n{0}", "Файл игры пропатчен.\nРезервная копия прежнего файла:\n{0}",
                "Se ha parcheado el archivo del juego.\nCopia de seguridad del archivo anterior:\n{0}", "已修補遊戲檔案。\n先前檔案的備份：\n{0}" },
            ["done.saved"] = new[] { "Saved to:\n{0}", "Enregistré dans :\n{0}", "Gespeichert unter:\n{0}", "Salvato in:\n{0}", "保存先：\n{0}", "저장 위치:\n{0}", "Zapisano w:\n{0}", "Сохранено в:\n{0}", "Guardado en:\n{0}", "已儲存至：\n{0}" },
            ["err.read"] = new[] { "Couldn't read the param file:", "Impossible de lire le fichier de paramètres :", "Die Parameterdatei konnte nicht gelesen werden:", "Impossibile leggere il file dei parametri:", "パラメータファイルを読み込めませんでした：", "파라미터 파일을 읽을 수 없습니다:", "Nie udało się odczytać pliku parametrów:", "Не удалось прочитать файл параметров:", "No se pudo leer el archivo de parámetros:", "無法讀取參數檔：" },
            ["err.patch"] = new[] { "Patching failed:", "Échec du patch :", "Patchen fehlgeschlagen:", "Patch non riuscita:", "パッチの適用に失敗しました：", "패치에 실패했습니다:", "Łatanie nie powiodło się:", "Не удалось применить патч:", "Error al aplicar el parche:", "修補失敗：" },
            ["err.missing"] = new[] { "That file doesn't exist.", "Ce fichier n'existe pas.", "Diese Datei existiert nicht.", "Il file non esiste.", "そのファイルは存在しません。", "해당 파일이 없습니다.", "Ten plik nie istnieje.", "Этот файл не существует.", "Ese archivo no existe.", "該檔案不存在。" },
            ["ok"] = new[] { "OK", "OK", "OK", "OK", "OK", "확인", "OK", "ОК", "Aceptar", "確定" },
            ["cancel"] = new[] { "Cancel", "Annuler", "Abbrechen", "Annulla", "キャンセル", "취소", "Anuluj", "Отмена", "Cancelar", "取消" },
            ["open.title"] = new[] { "Select your {0} param file", "Sélectionnez le fichier de paramètres de {0}", "Parameterdatei von {0} auswählen", "Seleziona il file dei parametri di {0}", "{0} のパラメータファイルを選択", "{0} 파라미터 파일 선택", "Wybierz plik parametrów {0}", "Выберите файл параметров {0}", "Selecciona el archivo de parámetros de {0}", "選擇 {0} 的參數檔" },
            ["save.title"] = new[] { "Save patched param file", "Enregistrer le fichier de paramètres patché", "Gepatchte Parameterdatei speichern", "Salva il file dei parametri con patch", "パッチ適用済みパラメータファイルを保存", "패치된 파라미터 파일 저장", "Zapisz załatany plik parametrów", "Сохранить пропатченный файл параметров", "Guardar archivo de parámetros parcheado", "儲存已修補的參數檔" },
            ["filter.param"] = new[] { "Param file", "Fichier de paramètres", "Parameterdatei", "File dei parametri", "パラメータファイル", "파라미터 파일", "Plik parametrów", "Файл параметров", "Archivo de parámetros", "參數檔" },
            ["filter.all"] = new[] { "All files", "Tous les fichiers", "Alle Dateien", "Tutti i file", "すべてのファイル", "모든 파일", "Wszystkie pliki", "Все файлы", "Todos los archivos", "所有檔案" },
            ["tip.min"] = new[] { "Minimize", "Réduire", "Minimieren", "Riduci a icona", "最小化", "최소화", "Minimalizuj", "Свернуть", "Minimizar", "最小化" },
            ["tip.max"] = new[] { "Maximize", "Agrandir", "Maximieren", "Ingrandisci", "最大化", "최대화", "Maksymalizuj", "Развернуть", "Maximizar", "最大化" },
            ["tip.restore"] = new[] { "Restore", "Restaurer", "Wiederherstellen", "Ripristina", "元に戻す", "이전 크기로 복원", "Przywróć", "Восстановить", "Restaurar", "還原" },
            ["tip.close"] = new[] { "Close", "Fermer", "Schließen", "Chiudi", "閉じる", "닫기", "Zamknij", "Закрыть", "Cerrar", "關閉" },
        };
    }
}
