This plugin allows server owners to modify the time spend on crafting items by a set percentage and also allows certain items to be blocked so that players cannot craft these.


The plugin also allows you to complete the currently crafting item for every player when the server is shutdown (can be enabled in the configuration) and will automatically cancel all the queued crafts and refund their materials.

**Available commands**

[chat] /rate <rate>

[console] crafting.rate <rate>

The rate command allows players to check the current crafting speed and allows owners (players logged in with authlevel 2 (ownerid command)) to change the speed of which items are crafted.


To have the items craft faster you reduce the value, the closer to 0, the shorter the crafting time. If you would want the crafting to go slower you increase the number over 100.

A few examples:

/rate 0 results in instant-craft

/rate 50 results in half-craft

/rate 100 results in normal-craft

/rate 150 results in 50% extra crafting time

/rate 200 results in double-craft


[chat] /itemrate <item> <rate>

[console] crafting.itemrate <item> <rate>

The itemrate command allows owners (players logged in with authlevel 2 (ownerid command)) to change the speed of which the specified item is crafted.


The following example will allow the hammer to be instantly crafted and all other items take 50% of the normal crafting time:

/rate 50

/itemrate Hammer 0


[chat] /block <item name>

[console] crafting.block <item name>

The block command allows owners to disallow players from crafting certain items. For instance, if you don't want players to craft their own C4 you simply block it by using the command as follows:

/block Timed Explosive Charge


[chat] /unblock <item name>

[console] crafting.unblock <item name>

The unblock command allows owners to allow players from crafting certain items again after blocking them. For instance, if you have previously blocked C4 but you're going to wipe your servers in the next 24 hours and you want your players to have some fun and craft C4 you simply unblock it by running the command as follows:

/unblock Timed Explosive Charge

[chat] /blocked

This command can be used by everyone and will show a list of all the currently blocked items.


[console] crafting.cancelall

Console command for server admins to stop everyone from crafting and refund the resources.

**Config variables**

There are a few settings you can change in the config:


ChatPrefix

The name that appears in front of chat messages that the plugin sends.

ChatPrefixColor

The color of the prefix in chat messages.

InstantBulkCraft

When instant crafting is enabled, and this is enabled all the items will be given instantly, and not one at a time.

InstantCraftForAdmins

When set to true admins (AuthLevel 2) will always have instant crafting on everything.

InstantCraftForModerators

When set to true moderators (AuthLevel 1) will always have instant crafting on everything.

**Default Config**

````

{

  "Settings": {

    "ChatPrefix": "Crafting Controller",

    "ChatPrefixColor": "#008000ff"

  },

  "Options": {

    "BlockedItems": [],

    "CraftingRate": 100.0,

    "IndividualCraftingRates": {},

    "InstantBulkCraft": false,

    "InstantCraftForAdmins": true,

    "InstantCraftForModerators": false

  },

  "Messages": {

    "BlockedItem": "{0} has already been blocked!",

    "BlockSucces": "{0} has been blocked from crafting.",

    "CancelledCrafting": "Cancelled crafting for all players and refunded the costs.",

    "CraftBlockedItem": "{0} is blocked and can not be crafted!",

    "CurrentCraftingRate": "The crafting rate is set to {0}%.",

    "IndividualCraftingRate": "The following items have a different crafting rate:",

    "InvalidItem": "{0} is not a valid item. Please use the name of the item as it appears in the item list. Ex: Camp Fire",

    "ModifyCraftingRate": "The crafting rate is now set to {0}%.",

    "ModifyCraftingRateError": "The new crafting rate must be a number. 0 is instant craft, 100 is normal and 200 is double!",

    "ModifyCraftingRateItem": "The crafting rate for {0} is now set to {1}%.",

    "NoBlockedItems": "No items have been blocked.",

    "NoItemRate": "You need to specify an item and a new crafting rate for this command.",

    "NoItemSpecified": "You need to specify an item for this command.",

    "NoPermission": "You don't have permission to use this command.",

    "NoRoom": "You don't have enough room to craft this!",

    "ShowBlockedItems": "The following items are blocked: ",

    "UnblockItem": "{0} is not blocked!",

    "UnblockSucces": "{0} is no longer blocked from crafting."

  }

}
````


**Itemlist: **[Oxide API for Rust](http://docs.oxidemod.org/rust/#item-list)