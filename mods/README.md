# Making mods for Param Patcher

Param Patcher applies every mod it finds in this `mods` folder. Each mod is a folder with a
`mod.json` and one or more CSV files, each CSV editing one param table:

```
mods/
  My Mod/
    mod.json
    CHANGES.md          (optional: the list of changes players can read in the app)
    EquipParamWeapon.csv
    SpEffectParam.csv
    game files/         (optional: files to install into the game, e.g. parts/, msg/)
```

To share your mod, send people the `My Mod` folder; they drop it into their `mods` folder.

## mod.json

```json
{
  "name": "My Mod",
  "author": "Your name",
  "description": "One or two sentences shown when the mod is selected.",
  "games": ["ptde", "remastered"],
  "installFiles": "game files",
  "requireAll": true,
  "patches": [
    { "file": "EquipParamWeapon.csv", "param": "EquipParamWeapon" },
    { "file": "SpEffectParam.csv", "param": "SpEffectParam" }
  ]
}
```

| Key | Required | Meaning |
|---|---|---|
| `patches` | yes, unless `installFiles` is set | The CSVs, in the order they're applied. A mod that only installs game files (e.g. a map fix) can leave it out; it's listed with a single "Game files" entry. `file` is relative to the mod folder; `param` is the param table it edits (defaults to the file name without `.csv`). |
| `name` | no | Shown in the list. Defaults to the folder name. |
| `author` | no | Shown under the description. |
| `description` | no | Shown in the details panel. |
| `games` | no | `"ptde"` and/or `"remastered"`: the games the mod works on. Leave it out if it works on both. The mod is hidden from the list when the other game is loaded. |
| `installFiles` | no | A folder in the mod whose contents are copied into the game when the player clicks "Patch game file": into `DATA` for Prepare to Die Edition, the game folder for Remastered, keeping the folder structure (`game files/parts/X.partsbnd` goes to `DATA/parts/X.partsbnd`). Files it replaces are backed up next to themselves as `.bak`. The mod's card shows whether the files are installed. Editor backups (`*.prev`, `*.bak`) are skipped. |
| `changes` | no | A Markdown file with the mod's list of changes, opened in the app with "Full List of Changes". Defaults to `CHANGES.md` if the mod has one. Headings, bullet lists and tables are shown; add `CHANGES.ja.md` etc. for translations. |
| `additionalFiles` | no | A message for mods that need something the patcher can't do itself (prefer `installFiles` for game files): say what to do. The mod's card shows "Needs extra files", the message appears in the details panel, and it's repeated after patching. |
| `requireAll` | no | `true` if the patches only work together (e.g. a new weapon row in `EquipParamWeapon` and the effect it uses in `SpEffectParam`): ticking any of its files ticks the whole mod, and if one file has an error the whole mod is blocked. Leave it out (or `false`) to let players pick files one by one. |

`name`, `description` and `additionalFiles` can also be translated, using Dark Souls' languages:

```json
"name": { "en": "My Mod", "ja": "私のMOD", "fr": "Mon mod" }
```

Codes: `en`, `fr`, `de`, `it`, `ja`, `ko`, `pl`, `ru`, `es`, `zh-TW`. Missing languages fall back to `en`.

Without a `mod.json`, every CSV in the folder is a patch for the param it's named after
(`EquipParamWeapon.csv`, or `Anything (EquipParamWeapon).csv`), and the folder name is the mod name.

### Param names

The `param` value is the param's file name inside `GameParam.parambnd` (case doesn't matter):
`AtkParam_Npc`, `AtkParam_Pc`, `BehaviorParam`, `BehaviorParam_PC`, `Bullet`, `CalcCorrectGraph`,
`CharaInitParam`, `EquipMtrlSetParam`, `EquipParamAccessory`, `EquipParamGoods`, `EquipParamProtector`,
`EquipParamWeapon`, `FaceGenParam`, `GameAreaParam`, `HitMtrlParam`, `ItemLotParam`, `KnockBackParam`,
`LockCamParam`, `Magic`, `MenuColorTableParam`, `MoveParam`, `NpcParam`, `NpcThinkParam`, `ObjActParam`,
`ObjectParam`, `QwcChange`, `QwcJudge`, `RagdollParam`, `ReinforceParamProtector`, `ReinforceParamWeapon`,
`ShopLineupParam`, `SkeletonParam`, `SpEffectParam`, `SpEffectVfxParam`, `TalkParam`, `ThrowParam`,
`default_AIStandardInfoBank`, `default_EnemyBehaviorBank`.

## CSV files

The format is what DSMapStudio's param editor exports (Edit -> Export CSV), so you can make a mod
by exporting your changed rows from there:

```
ID,Name,weight,correctStrength,correctAgility
1205000,Darkmoon Bow [Short Bow],,,50
306000,Stone Greatsword,12,,
```

- The first column is the row `ID`. A `Name` column is optional and only used for rows your mod adds.
- The other columns are field names, exactly as DSMapStudio shows them. Use only the fields you change.
- **A blank cell means "leave this value alone".** Only cells with a value are written.
- Rows whose ID doesn't exist yet are added as new rows; give them every field they need. Fields
  you leave out are copied from the existing row just before it.
- Floats use a dot (`0.5`); padding/array fields use `[0|0|0]`.

You don't need to strip out values that are the same as the original game: Param Patcher compares every
value with the unmodified game, and only writes the ones your mod actually changes. Players' own edits
elsewhere in the file are kept, and the patcher shows them which of your values conflict with theirs.

## Testing

Run `ParamPatcher.exe --list` in a terminal to see every mod, its status and any error in your
`mod.json` or CSVs (`ParamPatcher.exe --help` for more options).
