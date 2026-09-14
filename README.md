Dark Souls PTDE Replayer's Convenience Fixes (RCF)

This has been tested with Prepare to Die Edition only. Since this tweaks aspects of the game such as weapon stats a bit, don't recommend applying to Remastered.

How to apply these mods:

1. Unpack Dark Souls data files with an unpacker, such as UXM Selective Unpack. Download here: https://github.com/Nordgaren/UXM-Selective-Unpack/releases/latest

2. After unpacking, you must patch the game to actually use these unpacked files. If you're using UXM Selective Unpack, click "Patch" from the program to do this.

IF YOU WANT ALL MODS AT ONCE (simplest method)

3. Paste the contents of "All Changes At Once" folder into your game's DATA folder replace all files that already exist. Any mod that changes the game's parameter file that you may have been using will be overwritten. Boot up the game and you should have all these changes active. 

IF YOU ONLY WANT SPECIFIC CHANGES

For the great hollow collision fix, just paste it into your data folder. No parameter changes are needed. For all others, you need to modify your parameter file with new parameters stored in the .csv files enclosed.

3. Get software that can edit the parameters in the game files. DSMapStudio should work perfectly for this. Download it and run DSMapStudio. https://github.com/soulsmods/DSMapStudio/releases/latest

4. Create a new project (file -> new project) doesn't matter what you call it, just select the correct DS1 .exe file that you use to run the game.

5. Select "Param Editor" tab. Then, depending on which mod .csv file you want to install, click one the following categories that should show up on the leftmost column. Which category to use is indicated in the .csv's filename.

6. Now that you've selected the category of parameter to edit, click Edit -> Import CSV -> From file... -> All Fields and select the .csv file you want to apply. It should apply the changes to the game's parameters automatically.

For some of the mods, you will need to import multiple .csv files for different categories. To get the mod fully working you must import all the files of that name to their respective categories.

7. Go back to step 5 if you want to apply other .csv files. You can apply all of them one after the other if you want, or any combination of mods. When you're done, click "File -> Save All"

8. Now go to the directory of the project you created at step 4, and get your newly generated project params file. Should be named "GameParam.parambnd"

9. Go to your DS1 DATA directory, and find the original game's GameParam.parambnd file. Should be unpacked to "DATA/param/GameParam" directory. Rename the file to something like GameParam.parambnd.bak to back up the original version of your game's params so you can undo any modifications later.

10. Paste in the newly modified "GameParam.parambnd" that you made with DSMapStudio to your DATA/param/GameParam folder for Dark Souls PTDE's installation directory. You can back up the old file by renaming it if you want.

11. Have fun!

???? What do they change? ????

==== Less Grindy Offline Covenant Items =====

- Drakes in the valley of drakes will have an increased 50% chance to drop 3 dragon scales
- Balder knights in the undead burg have a newly added 8% chance to drop 1 sunlight medal
- Painting Guardians have a newly added 15% chance to drop 2 souvenirs of reprisal
- Basilisks (depths or great hollow) have an increased 25% chance to drop 3 eyes of death
- The Fog Ring can't be obtained without doing darkroot garden battles, so it now also is found alongside the Soul of a Brave Warrior in Darkroot Garden.

In case you play offline, and want to be able to get covenant items around when you have access to the covenant.

===== No Killing friendly NPCs for Gear =====

- Andre will sell his hammer
- Vamos will sell his helmet and hammer
- Giant Blacksmith will sell his hammer
- Gough will sell his armor
- Gough will sell the Lord's Blade set
- Chester will sell his armor
- Shiva will sell his shield

In case you want to do a run with such equipment without murdering your friends in cold blood.

===== No Painful Equipment drop rates =====

Basically all <5% armor or weapon drop rates were raised to 5%, unless you could get them in some other easier way (shops/items in the overworld), or they were not particularly uncommon despite a low drop rate (broken straight sword, etc.)

- Ghost Blade drop from banshees raised to 35% chance
- all Titanite slab drops (all colors) raised to 5%
- Titanite catch pole drops all raised to 15%
- Channeler's trident drops all raised to 10%
- Stone greatshield drops all raised to 5%
- Dark hand drops raised to 5%
- Painting Guardian Sword drop rate raised to 5%
- Silver knight straight sword drop rate raised to 5%
- Silver knight shield drop rate raised to 5%
- Silver knight spear drop rate raised to 5%
- Bandit's Knife drop rate raised to 5%
- Hollow thief's set (hood, armor, tights) drop rate raised to 5%
- Balder side sword drop rate raised to 5%
- Balder shield drop rate raised to 5%
- Tower Shield drop rate raised to 5%
- Jagged Ghost Blade drop rate raised to 5%
- Man-Serpent Greatsword drop rate raised to 5%
- Large Club drop rate raised to 5%
- Murakumo drop rate (giant skeletons) raised to 5%
- Bonewheel shield drop rate raised to 5%
- Pinwheels near nito drop rate of each mask (Mother/Father/Child masks) raised to 5% each
- Stone Greataxe drop rate raised to 5%
- Four-pronged plow drop rate raised to 5%
- Bloated Head drop rate raised to 5%
- Bloated sorcerer head drop rate raised to 10%
- Oolacile catalyst drop rate raised to 5%

Just to make things less painful if you want to do a run with the rarer pieces of equipment or collect/upgrade everything.

===== Less useless shields =====

Increased the physical resistance and stability of a bunch of shields:

Target Shield
- 90% physical
- Base stability raised to 52
- mid tier deflection

Effigy Shield
- 100% physical
- Base stability raised to 58 
- mid tier deflection

Crystal Ring Shield
- 100% physical
- Stability raised to 69
- mid tier deflection
- 135 base magic damage
- B int scaling

Caduceus Round Shield
- 95% physical
- Base stability raised to 56
- mid tier deflection

Gargoyle's Shield
- 99% physical (can't be 100% to stay lore accurate)
- base stability raised to 58
- heavy tier deflection

Large Leather Shield
- 100% physical

Pierce Shield
- 100% physical
- stability raised to 68
- mid tier deflection

Spiked Shield
- 100% physical
- stability raised to 68
- mid tier deflection

Sanctus
- 98% physical
- base stability raised to 59
- heavy tier deflection

Bonewheel shield
- 95% physical
- stability raised to 78

Eagle Shield
- 98% physical

Cleansing Greatshield
- 95% physical

Sunlight shield
- heavy tier deflection

Crystal shield
- heavy tier deflection

Giant shield
- base stability raised to 73 (really heavy, should have stability to match)

Knight shield
- 65 magic defense, 65 lightning defense, 65 fire defense, 60 base stability (due to higher weight, it has better stats than lighter shields now)

Bloodshield
- 50 magic defense, 62 base stability (obtained late in the game, may as well be a bit stronger overall)

Caduceus Kite Shield
- 70 magic defense, 50 fire defense, 30 thunder defense (differentiate from tower kite shield a bit)

Dark Hand -> B strength and dex scaling added, guard stability increased to 60, deflection increased to 50 (medium shield level deflection), phys guard reduction increased to 97 (now it's a pretty good shield)

"Base stability" means upgrading will increase the stability higher than the base. Some shields do not increase stability with upgrades.

Mostly just for some fun with playing with shields for fashion. 

===== Less Useless Weapons =====

Weapon scaling/damage/weight/stat req modifications (EquipParamWeapon)

Ghost blade -> A dex scaling, buffable
Jagged Ghost blade -> B dex scaling, buffable
Dark Hand -> B strength and dex scaling added, guard stability increased to 60, deflection increased to 50 (medium shield level deflection), phys guard reduction increased to 97 (now it's a pretty good shield)
Stone Greatsword -> A strength scaling at +5, weight decreased to 12, required strength decreased to 18, A int scaling at +5, weapon weight rate decreased to 0.5, phys damage decreased to 124, mag damage increased to 124
Server -> A dex scaling at +15
Giant's Halberd -> Base Phys damage increased to 180, weight decreased to 12, required strength decreased to 24, base poise damage increased to 35, added B faith scaling
Sniper crossbow -> base damage increased to 75, weight decreased to 5.5
Pike -> weight decreased to 6.5, required strength decreased to 16
Butcher Knife -> weight decreased to 6, required strength decreased to 16
Four-pronged plow -> weight decreased to 2.5
Man-serpent greatsword -> weight decreased to 7.5
Stone Greataxe -> base damage increased to 250
Demon's greathammer -> base damage increased to 155
Dragon Greatsword -> base damage increased to 450
Dragon king greataxe -> base damage increased to 420
Smough's Hammer -> base damage increased to 400, buffable
Lucerne -> Base damage increased to 118
Titanite Catch Pole -> Base int scaling increased to B
Hammer of Vamos -> base phys increased to 150, base fire increased to 120, A faith scaling added, A int scaling added
Blacksmith hammer -> S strength scaling at +15
Morning Star -> C dex scaling added, B at +15
Warpick -> C strength and C dex scaling, double B at +15
Dragon Bone Fist -> Base damage increased to 156, weight decreased to 4, base poise damage increased to 40
Mail breaker -> base damage increased to 66, crit multiplier increased to 200
Astora Straight sword -> faith scaling increased to ~A at fully upgraded
Crescent Axe -> faith scaling increased to ~A at fully upgraded
Silver knight str sword -> 17 required dex
Silver knight spear -> buffable, 17 required dex
Golem Axe -> buffable
Great Lord Greatsword -> buffable
Smough's Hammer -> buffable
Darkmoon Bow -> added int scaling (same scaling as faith, allows mages in general to get use out of this bow, not just faith builds)
Darksword -> base damage increased by 3 in every infusion on every damage type. More worth using despite its 6 weight and difficulty of obtaining.

Stamina cost reduction for specific bad weapons (BehaviorParam_PC)

Pike -> Reduced stamina cost of attacking to be in line with base spear
Whip -> stamina cost of attacking heavily reduced
Notched Whip -> stamina cost of attacking heavily reduced
Guardian Tail -> stamina cost of attacking heavily reduced
Four-pronged plow -> stamina cost of attacking heavily reduced
Dragonbone fist -> stamina cost of heavy attacks reduced
Titanite catchpole -> stamina cost of heavy attacks reduced

Guard piercing for specific bad weapons (AtkParam_Pc)

All whips -> attacks ignore shields
Mail breaker -> heavy attacks go through shields

Improve Channeler's Staff buff (Bullet, SpEffectParam)
- Buff time increased to 60 seconds, also buffs the user. This is the same length of time as the buff the actual channeler gets. The buff should also apply to magic and miracles as well now.

Some weapons just have many issues or are greatly outclassed, especially some that appear only late in the game. These changes improve them a bit.

===== Less Useless Rings =====

ring of the evil eye -> regenerates 50 hp per kill instead of 30
east wood grain ring -> changed effect: regens 3 weapon durability for your right hand weapon per second when equipped (useful if you're using weapons with durability attacks), does not repair crystal weapons
ring of the sun princess -> added effect: generates a sunlight maggot light effect (allows for earlier less painful exploration of tomb of the giants)
tiny being's ring -> added effect: also gives 10% boost to healing (estus, other sources of healing)
blue tearstone ring -> triggers when you have 35% health remaining instead of 20% so you can actually get some  mileage out of the defensive boost.
white seance ring -> added effect: 4 hp regen per second
dusk crown ring -> -25% hp instead of -50% hp
darkmoon seance ring -> added effect: permanent magic shield spell while ring is equipped
old witch's ring -> added effect: if you have less than 10% HP, +2 humanity every 2 minutes
covenant of artorias -> added effect: you regain 40 extra stamina per second when you have less than 50% max health ("green tearstone ring", same sort of aggression upgrade as artorias)
orange charred ring -> added effect: +20% to pyromancy damage, reduces fire damage taken by 50% (flame kick effect removed to add these new ones)
calamity ring -> added effect: all player animations are 25% faster. You still take double damage though. Since it's essentially a ng+ ring, you can play the game in a slightly different way once you have it.


A couple of rings have negligible effects that are almost useless or have no effect at all, luckily there aren't that many of them, but these changes make them a bit more useful.

===== More useful Armor effects =====

Ornstein's helm -> +10% lightning damage on all sources
Brass helm -> +10% magic damage on all sources
Black knight helm -> +10% fire damage on all sources
Paladin helmet -> +20% miracle damage, -20% magic resistance
Mask of the sealer -> +20% magic damage on sorceries, -20% fire res
Six-eyed helm of the channeler -> +20% magic spell damage on sorceries, -20% lightning res
Gold-hemmed Black hood -> +20% fire spell damage, -20% lightning resistance
Big hat -> +10% all spell damage (same effect as crown of the dark sun)
Witch hat -> +10% all spell damage (same effect as crown of the dark sun)
Maiden Hood -> +10% miracle damage
Cleric Helm -> +10% miracle damage
Elite Cleric Helm -> +10% miracle damage
Royal helm -> father's mask effect
Gough's helm -> mask of the mother effect
Porcelain mask -> mask of the child effect
Pharis's hat -> hawk ring effect (does not stack with ring)
Dark mask -> ring of the evil eye effect (does not stack with ring)
Snickering top hat -> hornet ring effect (does not stack with ring)
Artorias's Helm -> Leo Ring effect (does not stack with ring)
Boots of the Explorer -> Fall Control effect

===== Add unused content + Check Covenant Rank Menu Option =====

adds in a bunch of unused content to the game, and when you interact with a covenant NPC, you can select a new option to check your current rank / how many offerings you've made. Some of the unused content had no names/text so I had to invent some of them. Should be somewhat lore-friendly though.

Mage smith armor -> sold by vinheim in new londo ruins, gauntlets give the same effect as the East Wood Grain Ring
Elite cleric armor -> sold by rhea after being saved
Undead king armor (jar-eel's armor) -> found in lost izalith (chest of soul of a great hero)
Barbarian armor -> found in blighttown's poison swamp where you get the server
Fine zweihander -> purchase from andre
Spiked catch pole -> found in the cages of sen's fortress (giant's halberd moveset)
Darkwraith Dirk -> sold by darkstalker kaathe

Ring of Condemnation -> karmic justice effect -> purchase from Oswald of Karim
Ring of The Blind Ghosts -> fall control effect -> found in the Depths (replaces the spider shield pickup above the giant rat, since there is another spider shield pickup available in valley of the drakes)
Charred Estus Ring (Ring of displacement) -> 20% boost to healing, -10% fire res -> Found with the dragon scale in the log in ash lake

===== Less Useless Spells =====

gravelord spells
-> 120 casts, damage increased
emit force
-> 12 casts
fire surge
-> increase damage per tick from 55 to 80

===== Fix Armor that Looks Like it should Have some Poise =====

increase poise on armor pieces that look like they should have some poise but don't
- painting guardian legs (10 poise)
- crimson mask (6 poise)
- brigand armor (8 poise)
- brigand gauntlets (8 poise)
- big hat (4 poise)
- mask of velka (4 poise)
- crown of the dark sun (6 poise)
- crown of the great lord (8 poise)
- robe of the great lord (20 poise)
- bracelet of the great lord (6 poise)
- anklet of the great lord (12 poise)
- shadow gauntlets (3 poise)
- shadow leggings (5 poise)
- hollow thief's leather armor (6 poise)
- black leather armor (6 poise)
- snickering top hat (4 poise)
- chester's long coat (4 poise)
- wanderer coat (7 poise)
- wanderer manchette (3 poise)
- wanderer boots (6 poise)
- leather armor (6 poise)
- hard leather armor (8 poise)
- hard leather boots (8 poise)
- hard leather gauntlets (3 poise)
- witch hat (4 poise)
- xanthous crown (12 poise)
- xanthous gloves (5 poise)
- chester's trousers (6 poise)

