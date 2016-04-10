**QuickSmelt** allows you to increase the speed of the furnace smelting.

The default values in the config offer roughly 2x production.

The current default config is as follows:

````
{

  "ChancePerConsumption": 0.5,

  "CharcoalChanceModifier": 1.5,

  "CharcoalProductionModifier": 1.0,

  "DontOvercookMeat": true,

  "ProductionModifier": 1.0

}
````


**ChancePerConsumption** basically means how large of a chance something has to be smelted on that fuel consumption tick. The lower the value, the less often something will be smelted in between the "vanilla" consumption ticks. Values are between 0.0 and 1.0 (0%-100%)
**CharcoalChanceModifier** lets you determine how often wood gets turned into charcoal. (The game's default value is 50%) This value is a total modifier, and by default is set to 150% (which brings up the chance to 75%).
**CharcoalProductionModifier** lets you control how much charcoal byproduct is created.
**DontOvercookMeat** (true/false) will avoid running the "QuickSmelt" logic on already-cooked meat. This lowers the chance of burning meat quickly. The game will still burn meat normally however! Please keep that in mind.
**ProductionModifier** lets you control how many of each item gets smelted each tick. By default, this is 1, but you can have your furnaces smelt 100 per consumption tick to make your furnaces crazy fast.
**Note:** this plugin does **not** cause materials to smelt into "extra". You still lose the same amount of pre-smelt items, and gain the correct amount of post-smelt items in return. This is not a count increase. Simply a speed increase.