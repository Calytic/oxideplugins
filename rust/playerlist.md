**Description**

This plugin allows your users to see what players or only how many players are currently online.

**Usage**

/who or /online - shows either how many or what players are online

**Default config**

````

{

  "Settings": {

      "ChatCommands": [

         "who",

         "online"

      ],

    "SeparateAdmins": "false",

    "OnlyShowAdminCount": "false",

    "OnlyShowPlayerCount": "true",

    "MaxPlayersPerLine": 8

  },

  "Messages": {

    "AdminCountMessage": "{count} admins online",

    "PlayerCountMessage": "{count} players online",

    "OnePlayerMessage": "You're the only one online",

    "NoAdminMessage": "No admin online",

    "PlayerNameMessage": "{count} players online: ",

    "AdminNameMessage": "{count} admins online: "

  }

}

}
````


**Settings**

ChatCommands - configure the chat commands

SeparateAdmins - separates admin and player if set to true

MaxPlayersPerLine - sets the number of players shown per line if OnlyShowPlayerCount set to false

OnlyShowPlayerCount - true shows only a player count, false shows all player names

OnlyShowAdminCount - true shows only a admin count, false shows all admin names
**Messages**

You can edit all the messages displayed to the user. {count} is the placeholder for the usercount