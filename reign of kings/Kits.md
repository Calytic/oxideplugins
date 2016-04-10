The Kits plugin will allow your players to redeem sets of items by using a simple chat command. These kits can be configured how you'd like with the items and the amounts you want them to redeem. A kit can have a cooldown, a limited amount of uses and can even be made limited to a specific permission group.

**Setting up the plugin**

When you first run the plugin a data-file and a configuration file will be created in their respective folders, you don't have to do anything with the data-file but you might want/need to make some changes to the config.

In the configuration file you need to set the usergroup of the admins/owner(s) of the server so that those users can utilize the admin commands, by default the group admin is used by the plugin. Other things you can modify are the name and color of it that would appear in the chat if the plugin sends a message and all the messages can be configured as well.

The LogToConsole option is also enabled by default and will show every kit command that is used in your Oxide logs.

**Creating kits**

Creating kits can be done in different ways, one way would be editing the data-file directly (not recommended) and the other way is by using the available admin chat commands.


When using the chat commands you have two different ways of creating a kit. You can choose to simply create a kit and fill it with items afterwards through the modification commands or you create a kit that will give all the items that you currently have in your inventory.

Command syntax:

````
/kit add "<kit name>" "<kit description>" [<flag> [<flag value>]]
````

A few examples:

The following command will create a kit 'StarterKit' which can be used once a day and will contain all the items you currently have in your inventory:
/kit add "StarterKit" "A small starter kit to get you going" -uses 1 -reset 1 -inventory


The following command will create a kit for players that are in the admin group:
/kit add "AdminKit" "Admin Only Kit" -permission admin

**Available kit flags:**

To limit the use of kits multiple flags can be used:
-uses <number> : This flag allows you to limit the amount of times a player can use this kit.
-reset <number> : This flag sets a reset value in days for when the uses reset.
-permission <group> : The permission group the player needs to be in to be able to redeem this kit.
-cooldown <number> : This flag indicates the time in **seconds** that this kit will be on cooldown for after using it.
-inventory : This flag will add all the items in your inventory to the kit.

**Deleting kits:**

To delete a kit you simple run the following command:

````
/kit remove "<kit name>"
````


**Modifying kits:**

It is possible to change everything on a kit from in-game through chat commands except the name and description of the kit.

Command syntax:

````
/kit modify "<kit name>" "<action>" "<value>" ["<secondary value>"]
````


**Available actions:**
cooldown : This action allows you to modify the cooldown of the kit.
uses : This action allows you to modify the amount of uses of the kit.
reset : This action allows you to modify the time in days for the uses to reset.
permission : This action allows you to modify the permission required to redeem this kit. To remove a required a permission you would set this to "none".
additem : This action allows you to add a specific item to a kit.
removeitem : This action allows you to remove a specific item from a kit.


A few examples:

Alter the cooldown of the StarterKit kit to 10 minutes:
/kit modify StarterKit cooldown 600


Set the amount of uses on the StarterKit to 2:
/kit modify StarterKit uses 2


Reset the uses of the StarterKit after 2 days:
/kit modify StarterKit reset 2


Allow everyone to redeem the AdminKit:
/kit modify AdminKit permission none


Add 200 Wood to the AdminKit:
/kit modify AdminKit additem Wood 200


Remove the Wood from the AdminKit:
/kit modify AdminKit removeitem Wood

**Default Config**

````

{

  "AdminMessages": {

    "KitAutoKitLimit": "You can only have one kit set as an AutoKit",

    "KitCreated": "You have created the kit '{0}'.",

    "KitExists": "A kit with the name '{0}' already exists.",

    "KitItemAdded": "Added item {0} ({1}) to the kit {2}.",

    "KitItemDoesNotExist": "The item {0} does not exist. Check /itemlist for a list of available items.",

    "KitItemNotFound": "Couldn't find the item {0}.",

    "KitItemRemoved": "Removed item {0} from the kit {1}.",

    "KitRemoved": "You have removed the kit '{0}'.",

    "KitReset": "Kits data was reset for {0}",

    "KitValueUpdated": "You have set the {0} option on kit {1} to {2}."

  },

  "Dictionary": {

    "AllKits": "all kits",

    "Cooldown": "Cooldown:",

    "Day": "day",

    "Days": "days",

    "Hour": "hour",

    "Hours": "hours",

    "Minute": "minute",

    "Minutes": "minutes",

    "Permission": "Permission:",

    "RemainingCooldown": "Remaining cooldown:",

    "RemainingUses": "Remaining uses:",

    "Reset": "Reset:",

    "Second": "second",

    "Seconds": "seconds",

    "Uses": "Uses:"

  },

  "HelpMessages": {

    "RedeemKit": "You can redeem a kit by using the command [CCCCCC]/kit <name>[FFFFFF], where <name> is the kit you want to redeem.",

    "ShowKits": "You can view all the available kits by using the command [CCCCCC]/kit list"

  },

  "Messages": {

    "InvalidArguments": "Invalid arguments supplied, check /kit help for the available options.",

    "KitCantRedeemAutokit": "You can't manually redeemed an autokit.",

    "KitList": "The following kits are available:",

    "KitNoPermission": "You are not allowed to redeem this kit.",

    "KitNoRoom": "You can't redeem the kit '{0}' because you don't have enough room in your inventory ({1} slots needed).",

    "KitNotFound": "A kit with the name '{0}' does not exist.",

    "KitNoUsesLeft": "You have reached your limit for this kit.",

    "KitOnCooldown": "This kit is on cooldown. You can't use this kit for another {0}.",

    "KitRedeemed": "You have redeemed a kit: {0}.",

    "KitUsesReset": "Uses reset after {0}.",

    "KitUsesResetRemaining": "{0} until your uses reset.",

    "NoKitsAvailable": "There are no kits available right now.",

    "NotAllowed": "You are not allowed to use this command!"

  },

  "Settings": {

    "AdminPermission": "admin",

    "ChatPrefix": "Kits",

    "ChatPrefixColor": "950415",

    "LogToConsole": true

  }

}
````