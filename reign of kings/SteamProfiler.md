This plugin will allow you to automatically kick players that connect with an account that has a VAC ban, is using a shared game or has a private profile.


Everything can be modified in the config file, including the option to exclude certain players from these checks. To do this you simply add their SteamID to the Whitelist in the config.

Example:

````
...

  "Options": {

    "Whitelist": ["12345678901234567", "76561198112743227"]

  },

...
````


You can also allow players with a specific amount of VAC bans or with their last VAC ban being x days ago.


This plugin uses webrequests to the Steam Web API to check if a player has VAC bans so you need to get an API key to use this here:
[http://steamcommunity.com/dev/apikey](http://steamcommunity.com/dev/apikey)

**Default config:**

````
{

  "FamilyShareBlocker": {

    "Enabled": true

  },

  "Messages": {

    "FamilyShareKickMessage": "Family Shared accounts are not allowed.",

    "FamilyShareServerAnnouncement": "{0} was not allowed on the server because of a shared game.",

    "PrivateProfileKickMessage": "Accounts with private profiles are not allowed.",

    "PrivateProfileKickServerAnnouncement": "{0} was not allowed on the server because of a private profile.",

    "VACBanKickMessage": "VAC banned accounts are not allowed.",

    "VACBanKickServerAnnouncement": "{0} was not allowed on the server because of one or more VAC bans."

  },

  "Options": {

    "Whitelist": []

  },

  "PrivateProfileBlocker": {

    "Enabled": true

  },

  "Settings": {

    "AnnounceToServer": true,

    "ChatPrefix": "Server",

    "ChatPrefixColor": "950415",

    "LogToConsole": true,

    "SteamAPIKey": "STEAM_API_KEY"

  },

  "VACBanBlocker": {

    "Enabled": true,

    "MinimumDaysSinceLastBan": 365,

    "NumberOfAllowedVACBans": 0

  }

}

 
````