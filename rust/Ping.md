**Ping** provides automatic kicking of players with high pings. This plugin will allow you to only keep players on your server with lower pings, hopefully preventing "laggy" players that make gameplay frustrating for some.

**Permissions**

This plugin uses Oxide's permission system. To assign a permission, use **grant user <username|steamid> <permission>**. To remove a permission, use **revoke user <username|steamid> <permission>**.


* 
**ping.bypass** (excludes player from having their ping checked)
**Ex.** grant user Wulf ping.bypass
**Ex.** revoke user Wulf ping.bypass
**Ex.** grant group moderator ping.bypass


**Configuration**

You can configure the settings in the Ping.json file under the oxide/config directory.

````
{

  "BroadcastKick": true,

  "HighPingKick": true,

  "PingLimit": 200

}
````


**Localization**

The default messages are in the Ping.en.json under the oxide/lang directory, or create a language file for another language using the 'en' file as a default.

````
{

  "PlayerKicked": "{name} kicked for high ping ({ping}ms)",

  "PingTooHigh": "Ping is too high: {ping}ms"

}
````