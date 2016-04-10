**DeathSigns** allows you to create signs that show the total deaths on the server. This will likely be expanded in the future to cover additional things such as last death, recent deaths, etc. I'll also likely be creating additional plugins that will do the same sort of thing, but for other information.

**Permissions**

This plugin uses Oxide's permission system. To assign a permission, use **grant user <username|steamid> <permission>**. To remove a permission, use **revoke user <username|steamid> <permission>**.


* 
**deathsigns.admin** (allows player to use the /spectate and spectate commands)
**Ex.** grant user Wulf deathsigns.admin
**Ex.** revoke user Wulf deathsigns.admin
**Ex.** grant group moderator deathsigns.admin


**Chat Command**


* 
**/deathsign**
Makes the sign in front of you a death sign.


**Configuration**

You can configure the settings and messages in the DeathSigns.json file under the server/<identity>/oxide/config directory.

**Default Configuration**

````
{

  "ChatCommand": "deathsign",

  "NoPermission": "Sorry, you can't use 'deathsign' right now",

  "NoSignsFound": "No usable signs could be found"

}
````


**Credits**


* 
**Bombardir**, for the incredible Sign Artist plugin from which this is based on.