**AFK** automatically kicks players that are AFK (away from keyboard) for too long. The default AFK time limit is set to 600 seconds (10 minutes).
**Permissions**

This plugin uses Oxide's permission system. To assign a permission, use **grant user <username|steamid> <permission>**. To remove a permission, use **revoke user <username|steamid> <permission>**.


* 
**afk.excluded** (excludes player from being kicked when AFK)
**Ex.** grant user Wulf afk.excluded
**Ex.** revoke user Wulf afk.excluded
**Ex.** grant group moderator afk.excluded


**Configuration**

You can configure the settings in the AFK.json file under the oxide/config directory.

````
{

  "AfkLimitMinutes": 10,

  "KickAfkPlayers": true

}
````


**Localization**

The default messages are in the AFK.en.json under the oxide/lang directory, or create a language file for another language using the 'en' file as a default.

````
{

  "KickedForAfk": "You were kicked for being AFK for {0} minutes"

}
````