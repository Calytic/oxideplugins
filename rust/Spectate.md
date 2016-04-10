**Spectate** allows only players with permission to spectate, and stop spectating without dying or respawning. This also blocks usage of the default 'spectate' console command functionality in Rust, and instead passes it through this plugin.

**Permissions**

This plugin uses Oxide's permission system. To assign a permission, use **grant user <username|steamid> <permission>**. To remove a permission, use **revoke user <username|steamid> <permission>**.


* 
**spectate.allowed** (allows player to use the /spectate and spectate commands)
**Ex.** grant user Wulf spectate.allowed
**Ex.** revoke user Wulf spectate.allowed
**Ex.** grant group moderator spectate.allowed


**Chat Command**


* 
**/spectate <name>**
Toggles the spectate mode for player, with optional target.


**Console Command**


* 
**spectate <name>**
Toggles the spectate mode for player, with optional target.


**Configuration**

You can configure the settings and messages in the Spectate.json file under the server/<identity>/oxide/config directory.

**Default Configuration**

````
{

  "ChatCommand": "spectate",

  "NoPermission": "Sorry, you can't use 'spectate' right now",

  "SpectateStart": "Started spectating",

  "SpectateStop": "Stopped spectating"

}
````