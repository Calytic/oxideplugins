Warp System is a plugin that allow any player with the required permission to create and delete Warp Points.

**Note:** All the warps that you made with the command "/warp add" can be found in your data file.

**** = NEEDS PERMISSION => "warp.admin"

Chat commands:

/warp help => **Lists all the available warp commands.
**/warp add** <WarpName> <WarpTimer> <WarpRange> <WarpMaxUses> <WarpPermissionGroup> **=>** Creates a warp point with the set name on the current location. ******
****- NOTE:****If you dont want the WarpRandomRange then set it to 0 and it will warp to the location that the warp got added.
****- NOTE:****If you dont want the WarpMaxuses then set it to 0 and it will have unlimited MaxUses.
**/warp to** <WarpName, WarpId> **=>** Use the warp and teleport the player to the Warp location.
**/warp limit** => Displays the players remaining cooldown, cooldown between warps and if cooldown is enabled/disabled.
**/warp list =>** Lists all available warps.
**/warp back** => Takes you back to the previous location. **[Permission "canback"]

/warp remove <**WarpName**> => **Deletes the warp <WarpName>.****

/warp wipe** => Wipes all the set warps.****

/warp all **<WarpName> => Teleports every online player to the warp.**

**How PermissionGroup Warps work:


Note:** If the permission group that you put in the **<WarpPermissionGroup>** field does not exists, the plugin will register it once you complete the /warp add command..
**-**If you want everyone to be able to use the warp just put in the WarpPermissionGroup argument put "all".
**Example:** /warp add SpaceShip 5 0 0 all

**-**If you want your **VIP **players to be able to use the warp just put the group that you created or simply put a one that you didnt create so the plugin will do it for you.(Auto permission group registering)

**Default Configuration:**

````

{

  "Messages": {

    "COOLDOWN_MESSAGE": "You have to wait [color cyan]{0}[color white] second(s) before you can teleport again.",

    "TELEPORTED_TO": "You have teleported to [color cyan]{0}[color white]",

    "TELEPORTED_TO_LAST_LOCATION": "You have teleported back to your last location!",

    "TELEPORTING_IN_TO": "Teleporting in [color orange]{0}[color white] second(s) to [color cyan]{1}[color white]",

    "WARP_ADDED": "Warp added with Warp Name: [color cyan]{0}[color white]",

    "WARP_EXISTS": "This warp already exists!",

    "WARP_LIST": "Warp ID: [color cyan]{2}[color white] Warp Name: [color cyan]{0}[color white] Permission:[color orange] {1} [color white] MaxUses Remaining: [color lime]{3}[color white]",

    "WARP_REMOVED": "You have removed the warp [color cyan]{0}[color white]"

  },

  "Settings": {

    "Cooldown": 120,

    "EnableCooldown": true,

    "WarpBackTimer": 5

  }

}

 
````