Simple plugin that allows players to spawn (or not) items with the F1 item menu, console or external rcon program such as rusty.  **With or without a message shown to players.**

**Notes**


* Spawning a blueprint will create a blueprint of the item in your inventory.  You will not automatically learn it.
* Chat and console commands accept item short names only.  To spawn a blueprint, include "bp" at the end.  For example, /giveme "rifle.ak bp".
* Server console commands do not check for permissions.

* Use item short names when adding to blacklist.
* Players with no permissions will not know the plugin even exists on the server, unless you want them to via plugin list, etc. It will show default rust error messages or act like default rust when trying to use commands.


**Configurations**

Console - Show item spawns in console.

Log - Log all spawns to a text file.

Blacklist - Enable the item blacklist.

Popup - Show a popup to player when given an item.

WarnChat - Warn all players that an item was spawned.

WarnUser - Warn players with permissions "adminspawn.warn" that an item was spawned. This will only trigger when "WarnChat" is false.

"WarnGiveTo" - Warn players the console gave them an item.

OneHundred - Replaces the 100 button on item spawns with this number.

OneThousand - Replaces the 1k button on item spawns with this number.

**Permissions**

This plugin uses oxide permissions.


adminspawn.hide - Player will not trigger any chat messages

adminspawn.warn - Warn the player an item was spawned when configurations "WarnUser" is true and "WarnChat" is false.

adminspawn.blacklist - Allows player to spawn blacklisted items

adminspawn.all - Allows player to spawn all items

adminspawn.ammunition - Allows player to spawn ammunition only

adminspawn.attire - Allows player to spawn attire only

adminspawn.construction - Allows player to spawn construction only

adminspawn.food - Allows player to spawn food only

adminspawn.items - Allows player to spawn items only

adminspawn.medical - Allows player to spawn medical only

adminspawn.misc - Allows player to spawn misc only

adminspawn.resources - Allows player to spawn resources only

adminspawn.tool - Allows player to spawn tools only

adminspawn.traps - Allows player to spawn traps only

adminspawn.weapon - Allows player to spawn weapons only

````
grant <group|user> <name|id> adminspawn.all

revoke <group|user> <name|id> adminspawn.all
````


**Usage for players**


Chat

/giveme <item> [quantity] - Give yourself an item

/give <player> <item> [quantity] - Give player an item

/giveall <item> [quantity] - Give all players (sleepers included) an item


F1 Console

give <item> [quantity] - Give yourself an item

giveto <player> <item> [quantity] - Give player an item

giveall <item> [quantity] - Give all players (sleepers included) an item


Server Console

giveto <player> <item> [quantity] - Give player an item

giveall <item> [quantity] - Give all players (sleepers included) an item

**Configuration file**

````
{

  "Blacklist": {

  "Items": [

  "rifle.ak",

  "supply.signal"

  ]

  },

  "Messages": {

  "AdminSpawn": "<color=#cd422b>{player}</color> gave <color=#ffd479>{amount} {item}</color> to <color=#cd422b>{target}</color>.",

  "GivePlayer": "<color=#cd422b>{player}</color> received <color=#ffd479>{amount} {item}</color>.",

  "GiveTo": "Administrator gave you <color=#ffd479>{amount} {item}</color>.",

  "Invalid": "Invalid item <color=#ffd479>{item}</color>.",

  "MultiPlayer": "Multiple players found.  Provide a more specific username.",

  "NoPermission": "You do not have permission to spawn <color=#cd422b>{item}</color>.",

  "NoPlayer": "Player not found.  Please try again."

  },

  "Settings": {

  "Blacklist": "false",

  "Console": "true",

  "Log": "true",

  "MessageSize": "12",

  "OneHundred": "100",

  "OneThousand": "1000",

  "Popup": "false",

  "Prefix": "[<color=#cd422b> Admin Spawn </color>]",

  "WarnChat": "false",

  "WarnGiveTo": "true",

  "WarnUser": "true"

  }

}
````

Configuration file will be created and updated automatically.

**More to come**


* Your suggestions


**Known Issues**


* None