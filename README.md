# ArmorRepair
A BattleTech mod that introduces an armor repair mechanic into the game.

ArmorRepair does this by automatically generating work orders to replace any lost mech armor after battles, for a time and cost just as if you added armor to a mech in the mech bay. 

The mod also uses the same functionality to create automatic structural repair orders after battles (you can disable this in the mod.json settings)

## Requirements
* install [BattleTechModLoader](https://github.com/Mpstark/BattleTechModLoader/releases) using the [instructions here](https://github.com/Mpstark/BattleTechModLoader)
* install [ModTek](https://github.com/Mpstark/ModTek/releases) using the [instructions here](https://github.com/Mpstark/ModTek)

## Recommended
* TBC pending real play testing :)

## Features
- Armor loss now matters
- No more busywork with repairs
- Automated repair work orders can be cancelled safely, and will refund any sub items not paid for.
- Scales both structure and armor costs with the mech tonnage, making Light mechs more cost effective on milk runs, and Heavy/Assault mechs more of a consideration than a go-to.

## IMPORTANT NOTE
The SimGameConstants setting "ArmorInstallTechPoints" had to be increased by a factor of 100 to make armor repairs work. This is because HBS set it as an integer (whole number). 

By default, even setting this to its lowest integer (1) resulted in massive armor modification / repair times. When tweaking this setting in SimGameConstants while using this mod, bare in mind it 
needs to be much higher than in vanilla. For example, an ArmorTechCost setting of 100 would be equal to 1 in vanilla.

## Download
Downloads can be found on [github](https://github.com/citizenSnippy/ArmorRepair/releases).

## mod.json Settings
Setting | Type | Default | Description
--- | --- | --- | ---
enableStructureRepair | boolean | default true | Whether to automatically issue structure damage repair work orders
scaleStructureCostByTonnage | bool | default true | Set this to false if you don't want to scale structure repair time/costs by mech tonnage
scaleArmorCostByTonnage | bool | default true | Set this to false if you don't want to scale armor repair time/costs by mech tonnage
debug | bool | default true | Set this to false to turn off debug logging (See ArmorRepair\Log.txt)
    
## Install
- After installing BTML and ModTek, unpack everything into \BATTLETECH\Mods\ folder.
-- You should end up with a folder called \BATTLETECH\Mods\ArmorRepair with the files in it.
- If you want to enable / disable mod features like automatic structure repair orders, or mech tonnage scaling, edit the Settings in the mod.json file.
- If you want to adjust the Armor / Structure cost and time (tech points), you will need to edit \Mods\ArmorRepair\StreamingAssets\data\simGameConstants\SimGameConstants.json
- Start the game.

## Credits
Thanks to [Beaglerush] (https://www.twitch.tv/beagsandjam) for the concept, motivation and some interesting mechanics discussions!

Also thanks to mpstark, Morphyum and LadyAlekto for everything they've done for the BT modding community and for their direct help with this mod.