# ArmorRepair
A BattleTech mod that introduces an armor repair mechanic into the game.

ArmorRepair does this by automatically generating Mech Bay work orders to replace any lost mech armor after battles, for a time and cost just as if you added armor to a mech in the mech bay. 

The mod also uses the same system to create automatic structural repair orders after battles (you can disable this in the mod.json settings).

## Requirements
* install [BattleTechModLoader](https://github.com/Mpstark/BattleTechModLoader/releases) using the [instructions here](https://github.com/Mpstark/BattleTechModLoader)
* install [ModTek](https://github.com/Mpstark/ModTek/releases) using the [instructions here](https://github.com/Mpstark/ModTek)

## Recommended Mods
* TBC pending real play testing :)

## Features
- Armor loss now matters
- No more busywork with repairs
- Automated repair work orders can be cancelled safely, and will refund any sub items not paid for.
- Scales both structure and armor costs with the mech tonnage, making Light mechs more cost effective on milk runs, and Heavy/Assault mechs more of a consideration than a go-to.

## IMPORTANT NOTES
* The vanilla SimGameConstants setting "ArmorInstallTechPoints" had to be increased in this mod by a factor of 100 to make armor repairs work correctly. 
	* This is because HBS set it as an integer (a whole number). By default, even setting it to its lowest usable integer value (1) resulted in massive armor modification / repair times, and we needed much more flexibility than that overall.
	* When tweaking this setting in SimGameConstants while using this mod, bear in mind it needs to be much higher than it would be in vanilla. For example, an ArmorTechCost setting of 100 with this mod would be equal to 1 in vanilla.
* If you disable the scaleStructureCostByTonnage and/or scaleArmorCostByTonnage functionality, remember to lower the relevant costs in the mnod's SimGameConstants.json accordingly.

## Download
Downloads can be found on [github](https://github.com/citizenSnippy/ArmorRepair/releases).

## Installation
* After installing BTML and ModTek, unpack everything from the release zip into the \BATTLETECH\Mods\ folder.
	* This must result in you having a folder called \BATTLETECH\Mods\ArmorRepair\ with the ArmorRepair.dll file in it, otherwise the mod has not been unpacked correctly!
* If you want to enable / disable mod features like automatic structure repair orders, or mech tonnage scaling, edit the Settings in the mod.json file.
* If you want to adjust the Armor / Structure costs, you'll need to edit \Mods\ArmorRepair\StreamingAssets\data\simGameConstants\SimGameConstants.json
* Start the game.

## mod.json Settings
Setting | Type | Default | Description
--- | --- | --- | ---
enableStructureRepair | boolean | default true | Whether to automatically issue structure damage repair work orders
scaleStructureCostByTonnage | bool | default true | Set this to false if you don't want to scale structure repair time/costs by mech tonnage
scaleArmorCostByTonnage | bool | default true | Set this to false if you don't want to scale armor repair time/costs by mech tonnage
debug | bool | default true | Set this to false to turn off debug logging (See ArmorRepair\Log.txt)

## SimGameConstants.json Settings
Setting | Type | Default | Description
--- | --- | --- | ---
StructureRepairTechPoints | float | default 0.9 | Number of tech points to repair structure. With mech tonnage scaling enabled, this is a maximum - you will typically only pay 30-50% of this early game.
StructureRepairCost | float | default 600 | Number of cbills to repair structure. With mech tonnage scaling enabled, this is a maximum - you will typically only pay 30-50% of this early game.
ArmorInstallTechPoints | float | default 35 | Number of tech points to repair armor (remember this is divided by 100 so this is equivalent 0.35 of StructureRepairTechPoints. With mech tonnage scaling enabled, this is a maximum - you will typically only pay 30-50% of this early game. 
ArmorInstallCost | float | default 200 | Number of cbills to repair armor. With mech tonnage scaling enabled, this is a maximum - you will typically only pay 30-50% of this early game.
MechLabRefundModifier | float | default 1.0 | This enables full refunds of work orders now that we are generating them automatically. In vanilla this is 0.9 for a 90% rebate on cancelling work orders.
    

## Credits
All credit to [Beaglerush](https://www.twitch.tv/beagsandjam) for the whole mod concept, and for the interesting mechanics discussions & help along the way.

Many thanks to everyone over at the [BattleTech Discord](https://discord.gg/zRptMZD) - in particular Morphyum, LadyAlekto and mpstark for their assistance.

Also cheers to HBS for another damn fine game!