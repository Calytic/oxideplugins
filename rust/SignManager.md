**Introduction**

My own take on tracking sign changes. If you have a problem with inappropriate content on your signs, now you'll know who did it.

**Description**

Track sign changes and allows administrator access to all signs.

**Notes**


* "AllowSigns", when true, will allow new signs to be created.  If false, no new signs will be allowed.  Administrators will always be allowed to create signs.   Players with permission "sign.create" will bypass this setting if "UsePermissions" is true.

* "Radius" determines how close to a sign you must be to use commands on it.

* "LogAdmin", when true, will log signs for players with permission "sign.admin".
* "LogHistory", when true, will keep a history of all sign edits.  This must true for this information to show in sign info.

* "MaxHistory, determines how many entries in history are kept.  It is recommended you keep this low to avoid chat limit problems.

* "AdminWarn", when true, will display are message to all players with permission "sign.admin" when a player edits a sign, along with the location of the sign.
* "PlayerWarn", when true, will display a configurable message to players when they edit a sign.
* Players with permission "sign.admin" will not trigger warn messages, if enabled.
* "AdminOwnClear". When true, administrators will be allowed to clear their own sign data. Otherwise, another administrator must do so.
* "UsePermissions". When true, permission "sign.create" will be active, otherwise, inactive. Meaning, if sign creation is false and "UserPermissions" is also false, players with this permission also cannot create new signs.


**Sign Artist Notes**
You must use the edited SignArtist plugin file located [here](http://oxidemod.org/threads/sign-manager.12009/#post-134135) if you wish to track edits made with SignArtist.


* "EnableBlacklist", when true, will monitor for sign edits for blacklisted keywords, urls, etc.
* "Blacklist" contains all the blacklisted entries.
* "AdminWarn", when true, will warn administrators when a blacklisted url is used.
* "DeleteBlacklist", when true, will delete the sign when a blacklisted url is used.
* Setting "Enabled" must be true to track changes and monitor for blacklisted urls.



**Permissions**

This plugin uses oxide permissions.


sign.create - Grants players access to create new signs when new sign creation is disabled

sign.nobl - Grants players immunity to sign artist blacklist check

sign.admin - Grants players access to plugin commands

````
oxide.grant <group|user> <name|id> sign.admin

oxide.revoke <group|user> <name|id> sign.admin
````


**Usage for administrators**


/sign - View help

/sign toggle - Enable or disable creation of new signs

/sign stats - View database statistics

/sign lockall <true | false> - Lock or unlock all signs

/sign deleteall - Delete all signs (This cannot be undone and there are no second chances!  Use with caution!)

/sign info - View data for sign

/sign unlock - Unlock sign for editing

/sign clear <sign | edits | player | all> [player] - Clear data for sign, player or edit count (cannot be undone)

**Configuration file**

````
{

  "Messages": {

  "AdminWarn": "<color=#cd422b>{player}</color> edited sign. (<color=#ffd479>{location}</color>)",

  "AdminWarnBL": "<color=#cd422b>{player}</color> edited sign with possible blacklist url <color=#cd422b>{url}</color>. (<color=#ffd479>{location}</color>)",

  "AdminWarnURL": "<color=#cd422b>{player}</color> edited sign with sign artist, url <color=#cd422b>{url}</color>. (<color=#ffd479>{location}</color>)",

  "ChangedStatus": "New sign creation <color=#cd422b>{status}</color>.",

  "ClearPlayer": "Data for <color=#ffd479>{player}</color> cleared. (<color=#ffd479>{count}</color> entries)",

  "ClearSign": "Data for sign cleared.",

  "DataCleared": "Data for <color=#ffd479>{count}</color> sign(s) cleared.",

  "DeleteBlacklist": "Your sign possibly contained inappropriate content and has been deleted.  If you believe this is an error, please contact an administrator.",

  "EditCountError": "Sign edit count is already zero.",

  "EditCountReset": "Sign edit count has been reset.",

  "GlobalDelete": "<color=#ffd479>{count}</color> sign(s) have been deleted.",

  "GlobalLock": "<color=#ffd479>{count}</color> sign(s) have been <color=#ffd479>{status}</color>.",

  "HistoryInfo": "History: {history}",

  "Info": "Last edit: [<color=#ffd479>{timestamp}</color>] <color=#cd422b>{player}</color> (<color=#ffd479>{playerid}</color>)",

  "Link": "Image URL: <color=#ffd479>{url}</color>",

  "MultiPlayer": "Multiple players found.  Provide a more specific username.",

  "NoClearPlayer": "No stored sign data found for <color=#ffd479>{player}</color>.",

  "NoData": "No stored sign data found.",

  "NoEdit": "No edit entries found.",

  "NoHistory": "No history entries found.",

  "NoInfo": "No data found for sign.",

  "NoOwner": "No owner entry found.",

  "NoPermission": "You do not have permission to use this command.",

  "NoPlayer": "Player not found.  Please try again.",

  "NoSign": "No sign found.  You must be looking at a sign.",

  "NoSigns": "No signs found.",

  "NoSignsAllowed": "You are not allowed to create new signs.",

  "NoStats": "No database entries found.",

  "NotLocked": "Sign is not locked.",

  "Owner": "Owner: <color=#cd422b>{player}</color> (<color=#ffd479>{playerid}</color>)",

  "PlayerWarn": "Sign updated.  All signs are monitored for inappropriate content.",

  "Self": "You cannot clear your own sign data.",

  "TooFar": "Too far from sign.  You must be within <color=#ffd479>{radius}m</color>.",

  "Unlocked": "Sign has been unlocked.",

  "WrongArgs": "Syntax error.  Use <color=#cd422b>/sign</color> for help."

  },

  "Settings": {

  "AdminOwnClear": "false",

  "AdminWarn": "false",

  "AllowSigns": "true",

  "LogAdmin": "true",

  "LogHistory": "true",

  "LogToFile": "true",

  "MaxHistory": "10",

  "PlayerWarn": "true",

  "Prefix": "[ <color=#cd422b>Sign Manager</color> ]",

  "Radius": "2",

  "Timestamp": "MM/dd/yyyy @ h:mm tt",

  "UsePermissions": "true"

  },

  "SignArtist": {

  "AdminWarn": "true",

  "Blacklist": "porn,naked,naughty",

  "DeleteBlacklist": "false",

  "EnableBlacklist": "false",

  "Enabled": "true"

  }

}
````

Configuration file will be created and updated automatically.

**More to come**


* Your suggestions


**Known Issues**


* None