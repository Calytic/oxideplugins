**ThirdPerson** is a micro-plugin that allows any player with permission to use the third-person view.

**Permissions**

This plugin uses Oxide's permission system. To assign a permission, use **grant user <username|steamid> <permission>**. To remove a permission, use **revoke user <username|steamid> <permission>**.


* 
**thirdperson.allowed** (allows player to use /view to change their view)
**Ex.** grant user Wulf thirdperson.allowed
**Ex.** revoke user Wulf thirdperson.allowed
**Ex.** grant group moderator thirdperson.allowed


**Chat Command**


* 
**/view**
Toggles player's view to/from third-person.


**Configuration**

You can configure the settings in the ThirdPerson.json file under the server/<identity>/oxide/config directory.

**Default Configuration**

````
{

  "ChatCommand": "view",

  "NoPermission": "Sorry, you can't use 'view' right now"

}
````