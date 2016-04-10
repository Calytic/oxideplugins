This plugin only supports the RustDB part

For the moment only calling rustDB when a player joins a server is possible. (with auto warning / kick features)

Don't forget to write the correct serverPort and serverIP (join Port, not the queryIp)
**Commands:**
- /rustdb => This command will upload your **server admin to rustDB.** You may set that it is not accessible to the public in your config files.

using KickBan plugin, or ingame commands: banid, ban, removeid (NOT UNBANALL) will automatically trigger RustDB also;

This command will at the same time debug your server and tell you if there was a problem with your server or not (and if so, will tell you what to do / look for)
**RustDB Config file:**

````
{

  "serverIP": "XX.XX.XX.XX",

  "onJoin": {

    "logBanned": false,

    "sendToAdminsBanned": false,

    "autoKick": {

      "minBansRequired": 1,

      "activated": true

    },

    "broadcastBanned": true

  },

  "Messages": {

    "serverOwnerIsXXX": "You need to set the serverOwner SteamID64 in the configs",

    "NotAllowed": "You are not allowed to use this command",

    "tryAgain": "Couldn't contact RustDB, please try again",

    "lookIntoConsole": "Please look into your server console to see RustDB's answer",

    "serverOwnerIsWrong": "You didn't set a proper SteamID64."

  },

  "RustDB": {

    "allowRustDBtoShowOwner": true,

    "serverOwner": "XXXXXXXXXXXXXXXXX"

  },

  "showRustDBAnswers": true,

  "serverPort": "XXXXX"

}
````