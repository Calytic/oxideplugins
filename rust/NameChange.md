**NameChange** allows players with permission to instantly rename other players, and keep them with that name. Names are persistent and stored by each player's Steam ID.

**Permissions**

This plugin uses Oxide's permission system. To assign a permission, use **grant user <username|steamid> <permission>**. To remove a permission, use **revoke user <username|steamid> <permission>**.


* 
**rename.players** (allows player to rename other players)
**Ex.** grant user Wulf rename.players
**Ex.** revoke user Wulf rename.players
**Ex.** grant group admin rename.players


**Chat Commands**


* 
**/rename player name**
Changes player's name to new name.



* 
**/rename player reset**
Resets player's name to original name.


**Console Commands**


* 
**rename player name**
Changes player's name to new name.



* 
**rename player reset**
Resets player's name to original name.


**Configuration**

You can configure the settings and messages in the NameChange.json file under the server/identity/oxide/config directory.
**

Default Configuration**

````
{

  "Messages": {

    "ChatHelp": "Use '/rename player name' to rename a player",

    "ConsoleHelp": "Use 'rename player name' to rename a player",

    "InvalidTarget": "Invalid player! Please try again",

    "NoPermission": "You do not have permission to use this command!",

    "PlayerRenamed": "{player} renamed to {name}!",

    "PlayerReset": "{player}'s name reset to {name}!",

    "YouRenamed": "You were renamed to {name} by {player}!",

    "YouReset": "Your name has been reset to {name}!"

  },

  "Settings": {

    "Command": "rename",

    "NotifyPlayer": "true"

  }

}
````

The configuration file will update automatically if new options are added or removed. I'll do my best to preserve any existing settings and messages with each new version.