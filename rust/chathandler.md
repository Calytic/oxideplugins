**If you'd like to support my work you can [Donate here](https://www.paypal.com/cgi-bin/webscr?cmd=_s-xclick&hosted_button_id=FFWZBZBCPWY2G)

Description_____________________________________________**

This plugin helps you moderate and modify your chat in many ways.

**Features_______________________________________________
**
**Anti Spam** (default enabled)
**This feature requires the [Chatmute](http://oxidemod.org/plugins/chatmute.1053/) plugin to work!**

The plugin has a anti spam feature that can be enabled/disabled in the config. Multiple messages sent in a short time frame, that can be edited in the config, will be recognized as spam and blocked.


The first time someone gets flagged for spam will auto mute him for 5 minutes, on the 2nd time he will be auto muted for 1 hour and on the 3rd time he will get a permanent mute.

The player will receive a warning that the punishment raises when he doesn't stop spamming.


ChatHandler also allows you to set a character limit sent per message. Messages longer than this limit will be cut and automatically sent in multiple messages. Its intelligent, words wont be cut in half.

**Anti Server Advertisement **(default enabled)

If enabled, every message that contains a server ip will be blocked and the user will receive a warning.

There is a whitelist in the config to allow specific ips to be posted.

**Word Filter **(default disabled)

The word filter can be used to replace bad words with something nicer or to replace bad words with symbols like * or anything else.


You can also enable the AllowPunish option in the config to automaticly mute or kick players for using blacklisted words.


There are examples in the config to see how to format it.


You can also use the chat command to add/remove words from the filterlist.

"/wordfilter add word replacement" to add a new wordfilter entry

"/wordfilter remove word" to remove a wordfilter entry

**Chat History **(default enabled)

Use /history or /h to show the chat history.

There is a config option to set the maximum number of chatlines saved.

**AdminMode**

Use "/admin" to switch yourself into admin mode. When in admin mode your chat name will be replaced with "[Server Admin]". This way you can chat as an admin without people knowing your real player name.

Chat command and chat name can be edited in the config.

Admin mode also allows you to bypass all the chat filter like anti spam, ip blocking, word filter etc.

**Ignore list**

If the [ignoreAPI](http://oxidemod.org/plugins/ignore-api.1054/) plugin is installed, players wont see chat sent by players on their ignore list.

**Chatgroups **(default enabled)

You can set multiple groups to give certain usergroups prefixes and chat colors. Pretty much everything is customizable. Players, Moderators and Admins are placed automatically into the corresponding group so they automatically have the permissions assigned to this group.

**Permissions____________________________________________**

ChatHandler uses [Oxide's permission system](http://oxidemod.org/threads/using-oxides-permission-system.8296/) to handle all permissions needed for specific features.

All permissions used can be changed in the config file.
Default permissions are:
**chathandler.adminmode** to use the adminmode feature
**chathandler.wordfilter** to use chat commands to edit the word filter

Players with the permission "admin" can use everything regardless of other permissions.


Default config file:

````

{

  "ChatGroups": {

    "Admin": {

      "NameColor": "#5af",

      "Permission": "admin",

      "Prefix": "[Admin]",

      "PrefixColor": "#FF7F50",

      "PrefixPosition": "left",

      "PriorityRank": 5,

      "ShowPrefix": true,

      "TextColor": "#ffffff"

    },

    "Donator": {

      "NameColor": "#5af",

      "Permission": "donator",

      "Prefix": "[$$$]",

      "PrefixColor": "#06DCFB",

      "PrefixPosition": "left",

      "PriorityRank": 4,

      "ShowPrefix": true,

      "TextColor": "#ffffff"

    },

    "Moderator": {

      "NameColor": "#5af",

      "Permission": "moderator",

      "Prefix": "[Mod]",

      "PrefixColor": "#FFA04A",

      "PrefixPosition": "left",

      "PriorityRank": 2,

      "ShowPrefix": true,

      "TextColor": "#ffffff"

    },

    "Player": {

      "NameColor": "#5af",

      "Permission": "player",

      "Prefix": "[Player]",

      "PrefixColor": "#ffffff",

      "PrefixPosition": "left",

      "PriorityRank": 1,

      "ShowPrefix": false,

      "TextColor": "#ffffff"

    },

    "VIP": {

      "NameColor": "#5af",

      "Permission": "vip",

      "Prefix": "[VIP]",

      "PrefixColor": "#59ff4a",

      "PrefixPosition": "left",

      "PriorityRank": 3,

      "ShowPrefix": true,

      "TextColor": "#ffffff"

    }

  },

  "Messages": {

    "Admin": {

      "AdminModeDisabled": "Admin mode disabled",

      "AdminModeEnabled": "You are now in admin mode",

      "NoPermission": "You dont have permission to use this command",

      "WordfilterAdded": "WordFilter added. {word} will now be replaced with {replacement}",

      "WordfilterError": "Error: {replacement} contains the word {word}",

      "WordfilterNotFound": "No filter for {word} found to remove",

      "WordfilterRemoved": "successfully removed {word} from the wordfilter"

    },

    "Helptext": {

      "ChatHistory": "Use /history or /h to view recent chat history",

      "Wordfilter": "Use /wordfilter list to see blacklisted words"

    },

    "Player": {

      "AdWarning": "Its not allowed to advertise other servers",

      "AutoMuted": "You got {punishTime} auto muted for spam",

      "BroadcastAutoMutes": "{name} got {punishTime} auto muted for spam",

      "NoChatHistory": "No chat history found",

      "SpamWarning": "If you keep spamming your punishment will raise",

      "WordfilterList": "Blacklisted words: {wordFilterList}"

    }

  },

  "Settings": {

    "AdminMode": {

      "ChatName": "[Server Admin]",

      "NameColor": "#ff8000",

      "TextColor": "#ff8000"

    },

    "AntiSpam": {

      "EnableAntiSpam": "true",

      "MaxLines": 4,

      "TimeFrame": 6

    },

    "ChatCommands": {

      "AdminMode": [

        "admin"

      ],

      "ChatHistory": [

        "history",

        "h"

      ],

      "Wordfilter": [

        "wordfilter"

      ]

    },

    "General": {

      "AllowedIPsToPost": [],

      "BlockServerAds": "true",

      "ChatHistoryMaxLines": 10,

      "EnableChatGroups": "true",

      "EnableChatHistory": "true",

      "MaxCharsPerLine": 80

    },

    "Logging": {

      "LogBlockedMessages": "true",

      "LogToConsole": "true",

      "LogToFile": "false"

    },

    "Permissions": {

      "AdminMode": "chathandler.adminmode",

      "EditWordFilter": "chathandler.wordfilter"

    },

    "Wordfilter": {

      "AllowPunish": "false",

      "EnableWordfilter": "false",

      "ReplaceFullWord": "true"

    }

  },

  "WordFilter": {

    "bitch": "sweety",

    "cunt": "****",

    "fucking hell": "lovely heaven",

    "nigger": [

      "mute",

      "mute reason"

    ],

    "son of a bitch": [

      "kick",

      "kick reason"

    ]

  }

}

 
````


**Chat**