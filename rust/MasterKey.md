**MasterKey** allows players with permission to unlock doors, storage boxes, furnaces, and gates, and even authorizing building automatically when entering restricted cupboards' radius. Usage is logged by default to the dated masterkeys_##-##-####.txt file in the oxide/logs directory.

**Permissions**

This plugin uses Oxide's permission system. To assign a permission, use **grant user <username|steamid> <permission>**. To remove a permission, use **revoke user <username|steamid> <permission>**.


* 
**masterkey.all** (allows player to unlock boxes and doors, and authorize cupboards)
**Ex.** grant user Wulf masterkey.all
**Ex.** revoke user Wulf masterkey.all
**Ex.** grant group moderator masterkey.all



* 
**masterkey.boxes** (allows player to unlock other players' boxes)
* 
**masterkey.cells **(allows player to unlock other players' cells)
* 
**masterkey.cupboards** (authorizes player when entering cupboard radius)
* 
**masterkey.doors** (allows player to unlock other players' doors)
* 
**masterkey.gates** (allows player to unlock other players' gates)
* 
**masterkey.shops **(allows player to unlock other players' shops)


**Configuration**

You can configure the settings in the MasterKey.json file under the oxide/config directory.

````
{

  "LogUsage": true,

  "ShowMessages": true

}
````


**Localization**

The default messages are in the MasterKey.en.json under the oxide/lang directory, or create a language file for another language using the 'en' file as a default.

````
{

  "ChatCommand": "masterkey",

  "Disabled": "MasterKey access is now disabled",

  "Enabled": "MasterKey access is now enabled",

  "MasterKeyUsed": "{0} ({1}) used master key at {2}",

  "UnlockedWith": "Unlocked {0} with master key!"

}
````