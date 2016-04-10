Warp System is a plugin that allow any player with the set authlevel to create and delete Warp Points.

NOTE: All the warps that you made with the command "/warp add" can be found in your data file.

** = NEEDS PERMISSION => "warpsystem.admin"

Chat commands:

/warp help => Lists all the available warp commands.
/warp add <WarpName> <WarpTimer> <WarpRange> <WarpMaxUses> <WarpPermissionGroup> => Creates a warp point with the set name on the current location.**
- NOTE:If you dont want the WarpRandomRange then set it to 0 and it will warp to the location that the warp got added.
- NOTE:If you dont want the WarpMaxuses then set it to 0 and it will have unlimited MaxUses.
/warp to <WarpName, WarpId> => Use the warp and teleport the player to the Warp location.
/warp limit => Displays the players remaining cooldown, cooldown between warps and if cooldown is enabled/disabled.
/warp list => Lists all available warps.
/warp all <WarpName/WarpId> => Teleports every online player to the WarpName or WarpId that you've typed.**

/warp all sleepers <WarpName/WarpId> => Teleports every sleeping player to the Warpname or WarpId That you've typed.**

/warp back => Takes you back to the previous location.
/warp remove <WarpName> => Deletes the warp <WarpName>.**

/warp wipe => Wipes all the set warps.**

/<WarpName,WarpId> => A shorter version of /warp to <WarpName, WarpId>


Console Commands:

warp.playerto <PlayerName> <WarpName, WarpId> => Teleports the target to the warp.

How PermissionGroup Warps work:


Note: If the permission group that you put in the <WarpPermissionGroup> field does not exists, the plugin will register it once you complete the /warp add command..


-If you want everyone to be able to use the warp just put in the WarpPermissionGroup argument put "all".
Example: /warp add SpaceShip 5 0 0 all

-If you want your VIP players to be able to use the warp just put the group that you created or simply put a one that you didnt create so the plugin will do it for you.(Auto permission group registering)


Default Configuration:


````

{

  "Messages": {

    "CANT_WARP_WHILE_BUILDING_BLOCKED": "You can not warp while you are in a building blocked area!",

    "CANT_WARP_WHILE_DUCKING": "You can not warp while you are ducking!",

    "CANT_WARP_WHILE_RUNNING": "You can not warp while running!",

    "CANT_WARP_WHILE_SWIMMING": "You can not warp while you are swimming!",

    "CANT_WARP_WHILE_WOUNDED": "You can not warp while you are wounded!",

    "COOLDOWN_MESSAGE": "You have to wait <color=#91FFB5>{0}</color> second(s) before you can teleport again.",

    "TELEPORTED_TO": "You have teleported to <color=#91FFB5>{0}</color>",

    "TELEPORTED_TO_LAST_LOCATION": "You have teleported back to your last location!",

    "TELEPORTING_IN_TO": "Teleporting in <color=orange>{0}</color> second(s) to <color=#91FFB5>{1}</color>",

    "WARP_ADDED": "Warp added with Warp Name: <color=#91FFB5>{0}</color>",

    "WARP_EXISTS": "This warp already exists!",

    "WARP_LIST": "Warp ID: <color=#91FFB5>{2}</color>\nWarp Name: <color=cyan>{0}</color> \nPermission:<color=orange> {1} </color> \nMaxUses Remaining: <color=lime>{3}</color>",

    "WARP_REMOVED": "You have removed the warp <color=#91FFB5>{0}</color>"

  },

  "Settings": {

    "Cooldown": 120,

    "EnableCooldown": true,

    "Warp_Back_Atleast_Required_Authlevel": 1,

    "WarpBackTimer": 5,

    "WarpIfBuildingBlocked": true,

    "WarpIfDucking": true,

    "WarpIfRunning": true,

    "WarpIfSwimming": true,

    "WarpIfWounded": true

  }

}

 
````