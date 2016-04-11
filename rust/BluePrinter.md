Description actualised for 1.0.5

Do you ever wanted to give your chars crafting knowledge to a friend?

Well you can do this with the BluePrinter-Plugin.


ConsoleCommands:

````
blueconf.recreate -- deletes your actual config and creates a new one with standard-values

blueconf.load -- if you changed the config manually this will reload it

blueconf.set [1stKEY] [2ndKEY] [VALUE] -- you can change the config-values via console here (config will be loaded afterwards)
````

If any KEY was not written correctly when running blueconf.set, don't worry you will just have useless entries in your config then. 
ChatCommands:

````
/bluehelp -- shows the help

/blueprinter [ITEM] -- create a blueprint of the speciefied item
````

The [ITEM] is being determined by the english displayname or the set alias in the config (both not case-sensitive), so for a blueprint of a Machete you will write "/blueprinter Machete" or "/blueprinter machete".

For the usage of the /blueprinter chat-command the player or the group the player is in will need the permission "canuseblueprinter", this permission will be automatically assigned to the standard-groups "admin", "moderator" and "player" when the config is generated (also when running console-command blueconf.recreate).

See [Using Oxide's permission system | Oxide](http://oxidemod.org/threads/using-oxides-permission-system.8296/) on how to add or remove permissions.

You can't create two blueprints at the same time (only valid if the "DrawTime" for the rarity-group of the blueprint is higher than 0).

You can only create blueprints for items you actually can craft yourself (except for default craftable items, you can't create blueprints for those).

The required ammount of paper or blueprintparts will be determined by the blueprints rarity.

With the standard config you will for example need 90 paper for a rare blueprint (9k wood) or if enabled 1 blueprint book, so standard config for paper is pretty expensive I think ... feel free to change that.


DefaultConfig (if your server knwos more players than just me then there will be more under "InDrawing"):

````
{

  "BlueprintParts": {

  "Common": 20,

  "CommonType": "blueprint_fragment",

  "None": 1,

  "NoneType": "blueprint_fragment",

  "Rare": 1,

  "RareType": "blueprint_book",

  "Uncommon": 1,

  "UncommonType": "blueprint_page",

  "VeryRare": 1,

  "VeryRareType": "blueprint_library"

  },

  "DrawTime": {

  "Common": 10,

  "None": 0,

  "Rare": 60,

  "Uncommon": 30,

  "VeryRare": 120

  },

  "InDrawing": {

  "76561197996178371": ""

  },

  "Localization": {

  "AlreadyDrawing": "You are already drawing a blueprint, please wait until this is finished.",

  "BPDelivery": "You finished drawing a blueprint for {0}.",

  "BPIsDrawing": "Blueprint is drawing now, please wait {0} seconds.",

  "BPNotLearned": "You don't know the blueprint for this item, learn it yourself first.",

  "BPRemovedFromQueue": "You are dead and can't finish drawing the blueprint for {0}.",

  "Help": "Use /blueprinter [ITEM] to create a blueprint from your known blueprints.",

  "ItemNotFound": "An item with the name \"{0}\" was not found.",

  "NoBP": "No blueprint for this item possible.",

  "NoPermission": "You have no permission to use this command.",

  "NotEnoughBluePrintParts": "The required ammount of blueprintpart-type \"{0}\" to create this blueprint is {1} and you only have {2}.",

  "NotEnoughPaper": "The required ammount of paper to create this blueprint is {0} and you only have {1}."

  },

  "Paper": {

  "Common": 3,

  "None": 1,

  "Rare": 90,

  "Uncommon": 45,

  "VeryRare": 180

  },

  "Settings": {

  "BlueprintPartsUsageAllowed": false,

  "CancelBPWhenDead": true,

  "EnablePopups": false,

  "PaperUsageAllowed": true

  },

  "Version": "1.0.5",

  "ZItemAlias": {

  "ammo.pistol": "Pistol Bullet",

  "ammo.pistol.fire": "Incendiary Pistol Bullet",

  "ammo.pistol.hv": "HV Pistol Ammo",

  .

  .

  .

  "wood.armor.pants": "Wood Armor Pants"

  }

}
````

FuturePlans:

- Well my future plans where mostly implemented since 1.0.2, so if you have any ideas let me know.


Bugs:

Well if you find any let me know.


PS:

Please have mercy with me as this is my first plugin after knowing Oxide know for less than a week.

Also keep in mind that english is not my native language, so if you have suggestions on how to describe something better let me know please.