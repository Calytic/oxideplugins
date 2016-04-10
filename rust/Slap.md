**Slap** allows players with permission to slap other players around a bit. Also features random flinching, suitable sound effects, and plenty of hurt for a premium slap experience.

**Permissions**

This plugin uses Oxide's permission system. To assign a permission, use **grant user <username|steamid> <permission>**. To remove a permission, use **revoke user <username|steamid> <permission>**.


* 
**slap.allowed** (allows player to slap other players)
**Ex.** grant user Wulf slap.allowed
**Ex.** revoke user Wulf slap.allowed
**Ex.** grant group moderator slap.allowed


**Chat Command**


* 
**/slap name** (replace 'name' with a player name)
Slaps the specified player around a bit.


**Configuration**

You can configure the settings and messages in the Slap.json file under the server/<identity>/oxide/config directory.

**Default Configuration**

````
{

  "ChatCommand": "slap",

  "CommandUsage": "Usage:\n /slap name (replace 'name' with a player name)",

  "DamageAmount": 5,

  "NoPermission": "Sorry, you can't use 'slap' right now",

  "PlayerNotFound": "Player '{player}' not found!",

  "PlayerSlapped": "{player} got slapped!",

  "SlapsPerUse": 3

}
````