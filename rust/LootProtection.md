**LootProtection** prevents players with permission from being looted by other players. This only applies to a player's inventory, not boxes or other storage.
**Permissions**

This plugin uses Oxide's permission system. To assign a permission, use **oxide.grant user <username|steamid> <permission>**. To remove a permission, use **oxide.revoke user <username|steamid> <permission>**.


* 
**loot.protection** (prevents player from being looted)
**Ex.** oxide.grant user Wulf loot.protection
**Ex.** oxide.revoke user Wulf loot.protection
**Ex.** oxide.grant group admin loot.protection



* 
**loot.dead** (allows player to loot dead players)



* 
**loot.sleepers** (allows player to loot sleeping players)


**Configuration**

You can configure the settings and messages in the LootProtection.json file under the server/identity/oxide/config directory.
**Default Configuration**

````
{

  "Messages": {

    "CantBeLooted": "{player} can't be looted!"

  }

}
````

The configuration file will update automatically if new options are added or removed. I'll do my best to preserve any existing settings and messages with each new version.