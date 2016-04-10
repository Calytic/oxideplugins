**Robbery** allows players to steal Economics money from other players by killing them. The percent stolen can be customized based on if the victim was awake or sleeping at time of death/when pickpocketed.


This plugin also supports Zone Manager/Event Manager, whereas it will disable robbery if the victim is in an event or a zone where they have the "noplayerloot" flag.

**Permissions**

This plugin uses Oxide's permission system. To assign a permission, use **grant user <username|steamid> <permission>**. To remove a permission, use **revoke user <username|steamid> <permission>**.


* 
**robbery.allowed** (allows player to steal money from other players)
**Ex.** grant user Wulf robbery.allowed
**Ex.** revoke user Wulf robbery.allowed
**Ex.** grant group moderator robbery.allowed


**Configuration**

You can configure the settings and messages in the Robbery.json file under the server/<identity>/oxide/config directory.

**Default Configuration**

````
{

  "AllowMugging": true,

  "AllowPickpocket": true,

  "MoneyStolen": "You stole ${amount} from {player}!",

  "NothingStolen": "You stole pocket lint from {player}!",

  "PercentAwake": 100.0,

  "PercentSleeping": 50.0

}
````


**Credits**


* 
**TheRotAG**, for the original RotAG-Roubo plugin in Lua.