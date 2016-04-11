The Quarry Factory plugin enables you to add different items that will spawn in the quarry depending on what resource it gathered, and how common one of the items will spawn.


It can be used as an alternative for the players instead of long, tedious gathering of materials to then smelt and wait and then craft into weapons.


So they can just protect their base and wait for the quarries to gather the items, and then get straight into combat. And come back when the quarry has gathered even more weapons and armor to use. Making the game even more territorial based and that the players need to find the good quarry places and protect them at all cost.


You can change how common weapons, armor, building materials and other important items occour inside the quarry. So the players must take different strategy depending on the items that spawn.


Or you can use it to make the quarry useless and make it spawn appels and a bunch of tuna cans...

**Commands


NOTE:**

The current item list with item shortnames can be found at the oxide docs for rust experimental. link: [**Oxide API for Rust**](http://docs.oxidemod.org/rust/#item-list)


Player Commands:


- /QF => Shows the commands the player has access to with info.

- /QF (ResourceType) => Enter one of the 5 resourcetypes ( SulfurOre, MetalOre, MetalFrags, Stones, HQMetalOre ) and you get the list of items that currently spawn on that resource.


Admin Commands:


- /QF NewItem (Item ShortName) (Amount) (RandomNumberMin) (RandomNumberMax) (ResourceType) => Adds a new item.

- /QF EditItem (Item slot) (Item ShortName) (Amount)(RandomNumberMin) (RandomNumberMax) (ResourceType) => Edits a specified item that already exists.

- /QF Enable (Item slot) => Enables a item so it can spawn when called.

- /QF Disable (Item slot) => Disables a item so it can't spawn when called.

**How to assign a item to spawn:**


Use this command to add a new item:


- /QF NewItem (Item ShortName) (Amount) (RandomNumberMin) (RandomNumberMax) (ResourceType)


Or this command to edit an already existing item:


- /QF EditItem (Item slot) (Item ShortName) (Amount)(RandomNumberMin) (RandomNumberMax) (ResourceType)


(Item ShortName): The item shortname of what you want to spawn. Example: rifle.ak Item list with shortnames link: [**Oxide API for Rust**](http://docs.oxidemod.org/rust/#item-list)


(Item slot): The item slot you want to edit. Example: Item1, Item2 and so on.


(Amount): Set how many you want of that item to spawn.


(RandomNumberMin) and (RandomNumberMax):


Set the Min and Max so that the item that you want to spawn has a percentage of chance that the random number will hit between those numbers.


The number generator will generate a number between 1 and 100.


So 1 to 20 will have a 20% chance that the number will be between those two numbers.


If there's two items that has the same Min and Max, or overlaping, then both will spawn.


If the number hit at a spot that no item has been assigned to, then nothing will spawn.


(ResourceType): This will determine that when the quarry gathers the resource that you have assigned to this item, the item will spawn.


For example: the quarry has gathered sulfur ore, if the item you have created has been assigned to SulfurOre, the item will spawn.


If it's metal ore, or any other resource, then it will not spawn.


SulfurOre, MetalOre, MetalFrags, Stones and HQMetalOre is the 5 resourcetypes the quarry gathers. Type the word exactly as it is or the plugin will not recognise it.

**Config:
**

At default the normal gathering is disabled, if you want one or all of them turned on again, go to the QuarryFactory.json in the config directory, and change these to true:


SulfurOreGather = false

MetalOreGather = false

MetalFragsGather = false

StonesGather = false

HighQualityMetalOreGather = false


At default the random number will show for the items when the /QC (ResourceType) is used. If you don't want the players to know how rare the items will spawn, change this one to false:


ShowRandomNumberInItemList = true