**Loot Spawn Lists **allows modification of the loot tables (from animals, crates, and supply drops).

**How to use**

* Install the plugin by dropping it into the oxide/plugins directory.
* In the oxide/config directory, the file "LootSpawnLists.json" should have been created. Download it.
* This file contains the default loot tables included within rust, in JSON format. Keep a copy of this file somewhere!
* Make modifications to the loot tables file as desired (see below) and re-upload it to your server.
* Reload the plugin using "oxide.reload LootSpawnLists" or restart the server.
* Check that it's worked by opening at the latest Oxide log file and looking for the message "x custom loot tables were loaded!". If you can't find this line, check the plugin is installed correctly and check your modified loot tables file in an online JSON validator.
**Notes**


* If you have an error in your loot tables file, **the plugin may not display an error and simply just load the default loot tables.** Always check your customized loot tables in a validator ([http://jsonlint.com/](http://jsonlint.com/)) before uploading!


**Loot Tables JSON Format**

Once you've spent a while reading and understanding the loot tables, they should be pretty self evident - although the formatting of the default loot tables JSON file is quite bad. There are 6 principle tables:


* AILootList - Mutant drops

* JunkSpawnList - Wooden crate drops

* SupplyDropSpawnListMaster - Supply drops

* WeaponSpawnList - Beige/blue crate drops

* AmmoSpawnList - Green (?) crate drops

* MedicalSpawnList - Red crate drops

Each table consists of a few parameters and a number of packages. A package can reference an item or another loot table. There can be any number of packages in a table. A random number is selected between "min" and "max" - this is the number of packages that will be spawned in a drop.


* "min" - The minimum amount of packages to spawn in a drop
* "max" - The maximum amount of packages to spawn in a drop
* "oneofeach" - One of each package will be dropped. You should probably set "min" and "max" to the number of packages.
* "nodupes" - No package will be dropped twice in a single spawn.

Each package has it's own minimum and maximum, as well as a weight. The weight is relative to the total combined weight of all packages. A good way to design weights is to ensure they all add up to 100, and then they become % chance to drop. They don't have to add up to 100 though.