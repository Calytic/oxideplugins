The Gather Manager plugin is a C# rewrite of the Increased Crafting Rate plugin. This plugin will allow you to modify the amount of resources people gain from gathering them from dispensers, the amount gained from using a Mining Quarry, the amount gained from using Survey Charges when they are succesful and the amount of the collectible object pickups (the small lootable rocks and wood logs on the ground scattered around the map).


An added feature to the plugin is also the ability to scale the amount of resources that are in the dispenser. When increasing the amount of resources gathered this will no longer increase the total amount of resources in that dispenser and you can now scale them yourself.

**Gathering**

The plugin distinguishes 4 different types of gathering, and are defined as dispenser, pickup, quarry and survey.


The type dispenser is used to change the rate you gain resources from the actual gathering nodes (trees, ore, corpses).


The type pickup will allow you to change the rate of the resources that you can find on the ground scattered all over the map (small rocks, wood logs that give a small amount of resources by default).


The type quarry is the most obvious one of the four and will allow you to scale the amount of resources that are gained by the Mining Quarry. Keep in mind that the resources gained by the Mining Quarry are zone specific, so even if you set a specific resource to give 10 times as much by example, it is still possible that nothing of that resource is given.


The type survey is the last one of the four and will allow you to modify the amount of the resources that you gain from gathering the items that spawn from the ground when you use the new Survey Charge grenade.

**Commands**

The plugin offers a series of console commands that can be used to modify the amount of resources gathered per resource per type of gathering.

Besides the console command there is a single chat command that allows for players to see all the current settings and will also show the admins which commands are available.

Increasing the gathering rate

To increase the gathering rate the console command gather.rate <type:dispenser|pickup|quarry|survey> "<resource>" <multiplier> is used.


A few examples:
gather.rate dispenser Wood 10 - Gain 10 times as much wood from hitting trees.
gather.rate dispenser Stones 5 - Gain 5 times as much stones when hitting those rocks.
gather.rate dispenser Cloth 10 - Gain 10 times as much cloth when gathering from corpses.
gather.rate pickup Stones 10 - Gain 10 times as much stones when picking up that collectible stone item.
gather.rate quarry Stones 20 - Gain 20 times as much stones from the Mining Quarry.
gather.rate survey "Sulfur Ore" 5 - Gain 5 times as much Sulfur Ore from using Survey Charges.

Keep in mind that if the name of the items has one or more spaces in it that you need to use quotes! Ex: Metal Ore -> "Metal Ore"


It is also possible to specify *** **as the item to increase the rate on all the items by that value, except those that were added manually, ex to have everything x10 and wood x2 you could use 'gather.rate dispenser wood 2' and 'gather.rate dispenser * 10'.

Increasing the amount of items in a dispenser

to increase the scale of the resources available in dispensers the console command dispenser.scale <dispenser:tree|ore|corpse> <multiplier> was made available.


A few examples:
dispenser.scale tree 5 - Trees will yield 5 times as much resources.
dispenser.scale ore 10 - Ore rocks will yield 10 times as much resources.
dispenser.scale corpse 2 - Corpses will yield 2 times as much resources.

**Things that will be added:**

Wildcard for the item argument for the command gather.rate to be able to set a rate for all the items for a single gathering type using a single command.