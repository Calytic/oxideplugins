This plugin provides a custom chat system (public and local chat).



**How do I send a local message?**

Just chat as you would normally.

**How do I send a public message? (Global)**

Start your messages with the "@" symbol.

**Commands:**

Admin:

/chat clear (Clears Database)


Player:

/chat public (Hides public chat messages)

/chat tag <new> (Sets the players tag, if left blank it toggles tag on/off)

/chat color <new> (Sets the players name color, if left blank it toggles tag on/off)

/chat icon (Hide/Show steam icon in messages)

**Info:**

Users require permission to be able to change/show/hide their tag.

Users require permission to change their name color/toggle it.

Users require permission to hide their steam icon.


I haven't got around to adding a sub-command that allows administrators to give permission, but at the moment you can do it by setting defaults in the configuration file by editing the following variables:

````
     "AllowIconHide": true,

    "DefaultCanColor": true,

    "DefaultCanTag": true,
````


**Default Configuration:**

````
{

  "Admin": {

    "MaxLevel": 2,

    "MinLevel": 1

  },

  "AdminColors": {

    "1": "#b4da73"

  },

  "Dependencies": {

    "PopupNotifications": false

  },

  "FadeGradient": {

    "1": "#E6E6E6",

    "2": "#C8C8C8",

    "3": "#AAAAAA",

    "4": "#8C8C8C",

    "5": "#6E6E6E"

  },

  "General": {

    "IconDisabled": "76561197967728661",

    "MaxColorLength": 10,

    "MaxTagLength": 15,

    "Protocol": 1336,

    "PublicPrefix": "@",

    "ShowUserIcons": true,

    "UserTagColor": "#00FFFF",

    "VoipEnabled": true

  },

  "Local": {

    "ChatPrefex": "Local",

    "Enabled": true,

    "FadeColors": true,

    "PrefixColor": "#F5A9F2",

    "PrefixEnabled": true,

    "Radius": 60.0,

    "ShowPlayerTags": true

  },

  "Messages": {

    "AuthLevel": "You <color=red>don't</color> have the required auth level.",

    "DBCleared": "You have <color=green>successfully</color> cleared the MagicChat database.",

    "PublicDisabled": "Public chat is currently <color=red>disabled</color>."

  },

  "Notifications": {

    "Enabled": true,

    "TimerInterval": 60

  },

  "Public": {

    "ChatPrefex": "Public",

    "Enabled": true,

    "PrefixColor": "#82FA58",

    "PrefixEnabled": true,

    "ShowPlayerTags": true

  },

  "UserSettings": {

    "AdminColor": "#b4da73",

    "AllowIconHide": true,

    "DefaultCanColor": true,

    "DefaultCanTag": true,

    "DefaultColor": "#81DAF5",

    "DefaultPublicChat": true,

    "DefaultTag": "O.G.",

    "DefaultWorld": 0

  }

}
````


**Updates:**

The next updates will be improving the "/chat" command.