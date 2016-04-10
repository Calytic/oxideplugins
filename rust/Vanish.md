**Vanish** allows players with permission to become completely invisible. Players, turrets, and helicopters will not be able to see, hear, or touch you! By default, admin will be able to see each other when vanished, but you can change that in the config file.

**Permissions**

This plugin uses Oxide's permission system. To assign a permission, use **grant user <username|steamid> <permission>**. To remove a permission, use **revoke user <username|steamid> <permission>**.


* 
**vanish.allowed** (allows player to use /vanish and go invisible)
**Ex.** grant user Wulf vanish.allowed
**Ex.** revoke user Wulf vanish.allowed
**Ex.** grant group moderator vanish.allowed


**Chat Command**


* 
**/vanish**
 Toggles player's invisibility on/off.


**Configuration**

You can configure the settings and messages in the Vanish.json file under the server/<identity>/oxide/config directory.

**Default Configuration**

````
{

  "CanBeHurt": false,

  "CanDamageBuilds": true,

  "CanHurtAnimals": true,

  "CanHurtPlayers": true,

  "CantDamageBuilds": "You can't damage buildings while vanished",

  "CantHurtAnimals": "You can't hurt animals while vanished",

  "CantHurtPlayers": "You can't hurt players while vanished",

  "CantUseTeleport": "You can't teleport while vanished",

  "CanUseTeleport": true,

  "ChatCommand": "vanish",

  "NoPermission": "Sorry, you can't use 'vanish' right now",

  "ShowEffect": true,

  "ShowIndicator": true,

  "ShowOverlay": false,

  "VanishDisabled": "You are no longer invisible!",

  "VanishEnabled": "You have vanished from sight...",

  "VanishTimedOut": "Vanish timeout reached!",

  "VanishTimeout": 0.0,

  "VisibleToAdmin": true

}
````


**Credits**


* 
**Nogrod**, for figuring out how to do this as well as putting up with me. Cheers!
* 
**dcode**, for the awesome icon!