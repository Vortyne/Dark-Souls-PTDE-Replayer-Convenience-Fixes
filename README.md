Made for Prepare to Die Edition, which is offline. The parameter changes can also be applied to Remastered with Param Patcher (see below), but since they tweak game balance, only play Remastered offline with them: playing online with modified game data isn't fair to other players and can get your account flagged.

Download the latest release zip (from the GitHub "Releases" page) and unzip it. It contains:

- "Param Patcher": ParamPatcher.exe and, in its "mods" folder, every mod that changes the game's parameters (one folder per mod).
- "Manual Install": the "All Changes At Once" bundle, for copying every mod into the game yourself.

(The repository itself holds only the sources; see "Building" at the end of this section.)

How to apply these mods:

1. Unpack Dark Souls data files with an unpacker, such as UXM Selective Unpack. Download here: https://github.com/Nordgaren/UXM-Selective-Unpack/releases/latest

2. After unpacking, you must patch the game to actually use these unpacked files. If you're using UXM Selective Unpack, click "Patch" from the program to do this.

IF YOU WANT ALL MODS AT ONCE (simplest method)

3. Paste the contents of the "Manual Install/All Changes At Once" folder into your game's DATA folder replace all files that already exist. Any mod that changes the game's parameter file that you may have been using will be overwritten. Boot up the game and you should have all these changes active. 

IF YOU ONLY WANT SPECIFIC CHANGES (Param Patcher)

The "Param Patcher" folder contains ParamPatcher.exe, which applies the mods in its "mods" folder straight onto your game's parameter file. Nothing to install: it runs on any Windows 10 or 11 PC (it uses .NET Framework 4.8, which comes with Windows).

3. Keep ParamPatcher.exe next to its "mods" folder and run it. Choose which mods you want and click apply for the version of dark souls you choose.

4. Some mods also install game files: "Add Unused Content" (text, armor models, NPC scripts) and the great hollow fix (a map file). The patcher copies them into your game for you when you click "Patch game file", backing up any file it replaces. "Add Unused Content" is for Prepare to Die Edition only, so the patcher doesn't offer it for Remastered; the great hollow fix works on both. Click a mod and "Full List of Changes" to see everything it changes.

5. Have fun! To undo, delete the patched file and rename the .bak file back to its original name (GameParam.parambnd, or GameParam.parambnd.dcx on Remastered).

About the mods: each mod folder has a mod.json (name, author, description, and which .csv edits which param table) and .csv files that only list the values the mod changes (blank cells mean "leave alone"). They are made for Param Patcher and can't be imported with DSMapStudio's "Import CSV -> All Fields".

Making your own mods: Param Patcher isn't tied to these mods. Put any mod folder in "Param Patcher/mods" and it shows up; see the README.md in that "mods" folder for the format. You're welcome to ship ParamPatcher.exe with your own mods.

ParamPatcher.exe also has a command line: run `ParamPatcher.exe --help`. With `--game` it detects the game from the file; without it, add `--remastered` to look for Remastered instead of Prepare to Die Edition.

Building (for the repository): the repo contains only sources:

- "mods": the mods (each folder: mod.json, .csv files, a CHANGES.md list of changes, and any game files the mod installs) and the mod format guide.
- "Manual Install": the files players copy into the game themselves.
- "Param Patcher/src": the patcher's C# source. "Param Patcher/data": the unmodified params of both games, built into the exe so it can compare against them.

Run `.\build.ps1` in PowerShell (needs the .NET SDK: https://dotnet.microsoft.com/download). It builds ParamPatcher.exe and puts the release together in "dist": a "Dark-Souls-PTDE-Replayer-Convenience-Fixes.v<version>" folder laid out as above, and the same as a zip to upload. Add `-Version 1.2` to name the version; otherwise it uses `git describe`. While developing you can also run the exe straight from "Param Patcher/build": it finds the repo's "mods" folder by itself.

The change lists below are generated from each mod's CHANGES.md: edit those files, not this README. `build.ps1` refreshes them here (as collapsible sections) and writes them out in full into the README in the zip; run `.\update-readme.ps1` to refresh this README without building.

## ???? What do they change? ????

<!-- changes:start -->

Each mod's full list of changes. Click a mod to expand it; they can also be read in Param Patcher with "Full List of Changes".

<details>
<summary><b>Add Unused Content + Check Covenant Rank Menu Option</b></summary>

Adds a bunch of unused content to the game, and when you talk to a covenant NPC you can select a new option to check your current rank / how many offerings you've made. Some of the unused content had no names or text, so I had to invent some of them. They should be somewhat lore-friendly though.

| Item | Effect | Where to get it |
|---|---|---|
| Mage Smith armor | The gauntlets give the same effect as the East Wood Grain Ring | Sold by Rickert of Vinheim in New Londo Ruins |
| Elite Cleric armor | | Sold by Rhea after she's been saved |
| Undead King armor (Jareel's armor) | | Lost Izalith (chest with the Soul of a Great Hero) |
| Barbarian armor | | Blighttown's poison swamp, where you get the Server |
| Fine Zweihander | | Bought from Andre |
| Spiked Catch Pole | Giant's Halberd moveset | The cages of Sen's Fortress |
| Darkwraith Dirk | | Sold by Darkstalker Kaathe |
| Ring of Condemnation | Karmic Justice effect | Bought from Oswald of Carim |
| Ring of the Blind Ghosts | Fall control effect | The Depths (replaces the Spider Shield pickup above the giant rat, since there's another Spider Shield in the Valley of Drakes) |
| Charred Estus Ring (Ring of Displacement) | 20% boost to healing, -10% fire resistance | With the dragon scale in the log in Ash Lake |

<sub>Source: <a href="mods/Add%20Unused%20Content%20%2B%20Check%20Covenant%20Rank%20Menu%20Option/CHANGES.md">mods/Add%20Unused%20Content%20%2B%20Check%20Covenant%20Rank%20Menu%20Option/CHANGES.md</a></sub>
</details>

<details>
<summary><b>Fix Armor that Looks Like it should Have some Poise</b></summary>

Increases poise on armor pieces that look like they should have some poise but don't.

| Armor | Poise |
|---|---|
| Painting Guardian Legs | 10 |
| Crimson Mask | 6 |
| Brigand Armor | 8 |
| Brigand Gauntlets | 8 |
| Big Hat | 4 |
| Mask of Velka | 4 |
| Crown of the Dark Sun | 6 |
| Crown of the Great Lord | 8 |
| Robe of the Great Lord | 20 |
| Bracelet of the Great Lord | 6 |
| Anklet of the Great Lord | 12 |
| Shadow Gauntlets | 3 |
| Shadow Leggings | 5 |
| Hollow Thief's Leather Armor | 6 |
| Black Leather Armor | 6 |
| Snickering Top Hat | 4 |
| Chester's Long Coat | 4 |
| Chester's Trousers | 6 |
| Wanderer Coat | 7 |
| Wanderer Manchette | 3 |
| Wanderer Boots | 6 |
| Leather Armor | 6 |
| Hard Leather Armor | 8 |
| Hard Leather Boots | 8 |
| Hard Leather Gauntlets | 3 |
| Witch Hat | 4 |
| Xanthous Crown | 12 |
| Xanthous Gloves | 5 |

<sub>Source: <a href="mods/Fix%20Armor%20that%20Looks%20Like%20it%20should%20Have%20some%20Poise/CHANGES.md">mods/Fix%20Armor%20that%20Looks%20Like%20it%20should%20Have%20some%20Poise/CHANGES.md</a></sub>
</details>

<details>
<summary><b>Fix Great Hollow Invisible Wall Issue</b></summary>

Fixes the root that blocks you from easily going back up the Great Hollow, by moving a model in the map of the central roots up a small amount. This makes it so you can easily walk past the "lip" that caused the issue in the original game.

| File | Change |
|---|---|
| map/MapStudio/m13_02_00_00.msb (the Great Hollow and Ash Lake map) | One of the central roots moved up a small amount |

It doesn't change the game's parameters, so it works together with any other mod. It works on Remastered too: Remastered uses the same map format.

<sub>Source: <a href="mods/Fix%20Great%20Hollow%20Invisible%20Wall%20Issue/CHANGES.md">mods/Fix%20Great%20Hollow%20Invisible%20Wall%20Issue/CHANGES.md</a></sub>
</details>

<details>
<summary><b>Less Grindy Offline Covenant Items</b></summary>

| Enemy | Drop | Chance |
|---|---|---|
| Drakes (Valley of Drakes) | 3 Dragon Scales | 50% (increased) |
| Balder Knights (Undead Burg) | 1 Sunlight Medal | 8% (new) |
| Painting Guardians | 2 Souvenirs of Reprisal | 15% (new) |
| Basilisks (Depths, Great Hollow) | 3 Eyes of Death | 25% (increased) |

Since Prepare to Die Edition has no online support, getting these covenant items is a massive pain without some sort of tweak like these ones.

<sub>Source: <a href="mods/Less%20Grindy%20Offline%20Covenant%20Items/CHANGES.md">mods/Less%20Grindy%20Offline%20Covenant%20Items/CHANGES.md</a></sub>
</details>

<details>
<summary><b>Less Useless Rings</b></summary>

| Ring | Change |
|---|---|
| Ring of the Evil Eye | Regenerates 50 HP per kill instead of 30 |
| East Wood Grain Ring | Changed effect: regenerates 3 weapon durability per second for your equipment while equipped (useful if you're using weapons with durability attacks); does not repair crystal weapons |
| Ring of the Sun Princess | Added effect: a sunlight maggot light (allows for earlier, less painful exploration of the Tomb of the Giants) |
| Tiny Being's Ring | Added effect: also gives a 10% boost to healing (estus, other sources of healing) |
| Blue Tearstone Ring | Triggers when you have 35% health remaining instead of 20%, so you can actually get some mileage out of the defensive boost |
| White Seance Ring | Added effect: 4 HP regen per second |
| Dusk Crown Ring | -25% HP instead of -50% HP |
| Darkmoon Seance Ring | Added effect: permanent Magic Shield spell while the ring is equipped |
| Old Witch's Ring | Added effect: if you have less than 10% HP, +2 humanity every 2 minutes |
| Covenant of Artorias | Added effect: regain 40 extra stamina per second when you have less than 50% max health (a "green tearstone ring", the same sort of aggression upgrade as Artorias) |
| Orange Charred Ring | Added effect: +20% pyromancy damage and 50% less fire damage taken (the flame kick effect was removed to add these) |
| Calamity Ring | Added effect: all player animations are 25% faster. You still take double damage though. A late game ring that can change things up quite a bit for fun |

A couple of rings have negligible effects that are almost useless or have no effect at all. Luckily there aren't that many of them, but these changes make them a bit more useful.

<sub>Source: <a href="mods/Less%20Useless%20Rings/CHANGES.md">mods/Less%20Useless%20Rings/CHANGES.md</a></sub>
</details>

<details>
<summary><b>Less Useless Shields</b></summary>

Increased the stats of a bunch of shields:

| Shield | Physical block | Stability | Deflection | Other |
|---|---|---|---|---|
| Target Shield | 90% | 52 (base) | mid tier | |
| Effigy Shield | 100% | 58 (base) | mid tier | |
| Crystal Ring Shield | 100% | 69 | mid tier | 135 base magic damage, B int scaling |
| Caduceus Round Shield | 95% | 56 (base) | mid tier | |
| Gargoyle's Shield | 99% (can't be 100% to stay lore accurate) | 58 (base) | heavy tier | |
| Large Leather Shield | 100% | | | |
| Pierce Shield | 100% | 68 | mid tier | |
| Spiked Shield | 100% | 68 | mid tier | |
| Sanctus | 98% | 59 (base) | heavy tier | |
| Bonewheel Shield | 95% | 78 | | |
| Eagle Shield | 98% | | | |
| Cleansing Greatshield | 95% | | | |
| Sunlight Shield | | | heavy tier | |
| Crystal Shield | | | heavy tier | |
| Giant Shield | | 73 (base) | | Really heavy, should have stability to match |
| Knight Shield | | 60 (base) | | 65 magic, 65 lightning and 65 fire defense (due to its higher weight, it has better stats than lighter shields now) |
| Bloodshield | | 62 (base) | | 50 magic defense (obtained late in the game, may as well be a bit stronger overall) |
| Caduceus Kite Shield | | | | 70 magic, 50 fire and 30 thunder defense (differentiates it from the Tower Kite Shield a bit) |
| Dark Hand | 97% | 60 | 50 (medium shield level) | B strength and dex scaling added (now it's a pretty good shield) |

"Base stability" means upgrading will increase the stability higher than the base. Some shields do not increase stability with upgrades.

Mostly just for some fun with playing with shields for fashion.

<sub>Source: <a href="mods/Less%20Useless%20Shields/CHANGES.md">mods/Less%20Useless%20Shields/CHANGES.md</a></sub>
</details>

<details>
<summary><b>Less Useless Spells</b></summary>

| Spell | Change |
|---|---|
| Gravelord spells | 120 casts, damage increased |
| Emit Force | 12 casts |
| Fire Surge | Damage per tick increased from 55 to 80 |

<sub>Source: <a href="mods/Less%20Useless%20Spells/CHANGES.md">mods/Less%20Useless%20Spells/CHANGES.md</a></sub>
</details>

<details>
<summary><b>Less Useless Weapons</b></summary>

Some weapons just have many issues or are greatly outclassed, especially some that appear only late in the game. These changes improve them a bit.

#### Scaling, damage, weight and stat requirements

| Weapon | Change |
|---|---|
| Ghost Blade | A dex scaling, buffable |
| Jagged Ghost Blade | B dex scaling, buffable |
| Dark Hand | B strength and dex scaling added, guard stability increased to 60, deflection increased to 50 (medium shield level), physical guard reduction increased to 97 (now it's a pretty good shield) |
| Stone Greatsword | A strength and A int scaling at +5, weight decreased to 12, required strength decreased to 18, weapon weight rate decreased to 0.5, physical damage decreased to 124, magic damage increased to 124 |
| Server | A dex scaling at +15 |
| Giant's Halberd | Base physical damage increased to 180, weight decreased to 12, required strength decreased to 24, base poise damage increased to 35, B faith scaling added |
| Sniper Crossbow | Base damage increased to 75, weight decreased to 5.5 |
| Pike | Weight decreased to 6.5, required strength decreased to 16 |
| Butcher Knife | Weight decreased to 6, required strength decreased to 16 |
| Four-Pronged Plow | Weight decreased to 2.5 |
| Man-Serpent Greatsword | Weight decreased to 7.5 |
| Stone Greataxe | Base damage increased to 250 |
| Demon's Greathammer | Base damage increased to 155 |
| Dragon Greatsword | Base damage increased to 450 |
| Dragon King Greataxe | Base damage increased to 420 |
| Smough's Hammer | Base damage increased to 400, buffable |
| Lucerne | Base damage increased to 118 |
| Titanite Catch Pole | Base int scaling increased to B |
| Hammer of Vamos | Base physical damage increased to 150, base fire damage increased to 120, A faith and A int scaling added |
| Blacksmith Hammer | S strength scaling at +15 |
| Morning Star | C dex scaling added, B at +15 |
| Warpick | C strength and C dex scaling, double B at +15 |
| Dragon Bone Fist | Base damage increased to 156, weight decreased to 4, base poise damage increased to 40 |
| Mail Breaker | Base damage increased to 66, critical multiplier increased to 200 |
| Astora's Straight Sword | Faith scaling increased to ~A when fully upgraded |
| Crescent Axe | Faith scaling increased to ~A when fully upgraded |
| Silver Knight Straight Sword | 17 required dex |
| Silver Knight Spear | Buffable, 17 required dex |
| Golem Axe | Buffable |
| Great Lord Greatsword | Buffable |
| Darkmoon Bow | Int scaling added (same scaling as faith, so mages in general can get use out of this bow, not just faith builds) |
| Darksword | Base damage increased by 3 in every infusion, on every damage type. More worth using despite its 6 weight and how hard it is to obtain |

#### Stamina cost

| Weapon | Change |
|---|---|
| Pike | Attack stamina cost reduced to be in line with the base spear |
| Whip | Attack stamina cost heavily reduced |
| Notched Whip | Attack stamina cost heavily reduced |
| Guardian Tail | Attack stamina cost heavily reduced |
| Four-Pronged Plow | Attack stamina cost heavily reduced |
| Dragon Bone Fist | Heavy attack stamina cost reduced |
| Titanite Catch Pole | Heavy attack stamina cost reduced |

#### Guard piercing

| Weapon | Change |
|---|---|
| All whips | Attacks ignore shields |
| Mail Breaker | Heavy attacks go through shields |

#### Channeler's Staff

| Change |
|---|
| The buff lasts 60 seconds and also buffs the user, the same length as the buff the actual Channelers give. It now also applies to magic and miracles |

<sub>Source: <a href="mods/Less%20Useless%20Weapons/CHANGES.md">mods/Less%20Useless%20Weapons/CHANGES.md</a></sub>
</details>

<details>
<summary><b>More Useful Armor Effects</b></summary>

| Armor | Effect |
|---|---|
| Ornstein's Helm | +10% lightning damage on all sources |
| Brass Helm | +10% magic damage on all sources |
| Black Knight Helm | +10% fire damage on all sources |
| Paladin Helmet | +20% miracle damage, -20% magic resistance |
| Mask of the Sealer | +20% magic damage on sorceries, -20% fire resistance |
| Six-Eyed Helm of the Channelers | +20% magic damage on sorceries, -20% lightning resistance |
| Gold-Hemmed Black Hood | +20% fire spell damage, -20% lightning resistance |
| Big Hat | +10% all spell damage (same effect as the Crown of the Dark Sun) |
| Witch Hat | +10% all spell damage (same effect as the Crown of the Dark Sun) |
| Maiden Hood | +10% miracle damage |
| Cleric Helm | +10% miracle damage |
| Elite Cleric Helm | +10% miracle damage |
| Royal Helm | Mask of the Father effect |
| Gough's Helm | Mask of the Mother effect |
| Porcelain Mask | Mask of the Child effect |
| Pharis's Hat | Hawk Ring effect (does not stack with the ring) |
| Dark Mask | Ring of the Evil Eye effect (does not stack with the ring) |
| Snickering Top Hat | Hornet Ring effect (does not stack with the ring) |
| Artorias's Helm | Leo Ring effect (does not stack with the ring) |
| Boots of the Explorer | Fall control effect |

<sub>Source: <a href="mods/More%20Useful%20Armor%20Effects/CHANGES.md">mods/More%20Useful%20Armor%20Effects/CHANGES.md</a></sub>
</details>

<details>
<summary><b>No Killing Useful NPCs for Gear</b></summary>

| NPC | Now sells |
|---|---|
| Andre | His hammer |
| Vamos | His helmet and hammer |
| Giant Blacksmith | His hammer |
| Gough | His armor, and the Lord's Blade set |
| Chester | His armor |
| Shiva | His shield |

In case you want to do a run with such equipment without murdering your friends in cold blood.

<sub>Source: <a href="mods/No%20Killing%20Useful%20NPCs%20for%20Gear/CHANGES.md">mods/No%20Killing%20Useful%20NPCs%20for%20Gear/CHANGES.md</a></sub>
</details>

<details>
<summary><b>No Painful Equipment Drop Rates</b></summary>

Basically all armor or weapon drop rates under 5% were raised to 5%, unless you could get the item some other easier way (shops, items in the overworld), or it wasn't particularly uncommon despite a low drop rate (Broken Straight Sword, etc.).

| Equipment | Original drop rate | New drop rate |
|---|---|---|
| Ghost Blade (banshees) | 1% | 35% |
| Titanite Slabs (all colors, all drops) | 0.2% | 5% |
| Titanite Chunks (the rarer second chunk drop of the same enemies) | 0.3% | 10% |
| Titanite Catch Pole (all drops) | 2% | 15% |
| Channeler's Trident (all drops) | 1% | 10% |
| Stone Greatshield (all drops) | 2% | 5% |
| Dark Hand | 1% | 5% |
| Painting Guardian Sword | 2% | 5% |
| Silver Knight Straight Sword | 2% | 5% |
| Silver Knight Shield | 1% | 5% |
| Silver Knight Spear | 2% | 5% |
| Bandit's Knife | 2% | 5% |
| Hollow Thief's set (hood, armor, tights) | 1% each | 5% each |
| Balder Side Sword | 1% | 5% |
| Balder Shield | 1% | 5% |
| Tower Shield | 2% | 5% |
| Jagged Ghost Blade | 1% | 5% |
| Man-Serpent Greatsword | 2% | 5% |
| Large Club | 2% | 5% |
| Murakumo (giant skeletons) | 1% (2% from the one near Nito) | 5% |
| Bonewheel Shield | 1% | 5% |
| Mask of the Mother / Father / Child (pinwheels near Nito) | 2% each | 5% each |
| Stone Greataxe | 2% | 5% |
| Four-Pronged Plow | 1% | 5% |
| Bloated Head | 0.5% | 5% |
| Bloated Sorcerer Head | 1% | 10% |
| Oolacile Catalyst | 0.5% | 5% |

Just to make things less painful if you want to do a run with the rarer pieces of equipment or collect/upgrade everything.

<sub>Source: <a href="mods/No%20Painful%20Equipment%20Drop%20Rates/CHANGES.md">mods/No%20Painful%20Equipment%20Drop%20Rates/CHANGES.md</a></sub>
</details>

<!-- changes:end -->

## Recommended mods to install alongside these ones

- DSFix (60fps on DS PTDE) (https://www.nexusmods.com/darksouls/mods/19?tab=description)
- Dark Souls Overhaul (Fixes ladders, lava eyesore button toggle, fixes bonfire softlock, fixes quantity selectors for covenant offerings/soul usages, and more...) (https://www.nexusmods.com/darksouls/mods/1474)
- Better Crystal Effect FPS (Seath - White Dragon Breath - Crystal Golems) (https://www.nexusmods.com/darksouls/mods/1633)
- Dark Souls Input Customizer (If you are using a keyboard, or want to customize inputs like adding a dedicated kick input button) (https://www.nexusmods.com/darksouls/mods/1273)
