**PlayerRankings** is a plugin that automatically gives players ranks based on playtime on a server.
**

This plugin requires the plugin ConnectionDB to in order to track playtime.**

**Commands:**
/ranks -Provides a list of ranks, playtime required, and the player's own playtime.

**Example:**

Check the** FAQ** for an example that you can use on your server.

**Config Info:**


* For playtime, it counts the hours, meaning that a playtime of 1.0 means 1 hour.
* Simply edit the permissions, playtime, and rank that you want, and also edit the permissions only in BetterChat's configs. You can also add more ranks if you'd like

* PLAYER IS THE DEFAULT RANK, it has already been added by default through betterchat, however, you need to keep it so it doesn't give 2 ranks instead of 1.
* If you want to use a different plugin such as chathandler, do chathandler.player when it gets updated, otherwise, keep it as color_"permission".


**Configs:**

````

{

  "Player": {

  "Permission": "betterchat.player",

  "Playtime": "1.0"

  },

  "Pro": {

  "Permission": "betterchat.pro",

  "Playtime": "25.0"

  },

  "Regular": {

  "Permission": "betterchat.regular",

  "Playtime": "10.0"

  }

}

 
````


**Installation:**

-This plugin requires BetterChat, you must edit the configs in order for the correct permissions to get sent out.

-Through the BetterChat configs, you are able to edit the Title, colors, and the permissions. (As long as you edit the permission in the Player Rankings config also)

**For example:**
Code (C#):
````

  },

  "Regular": {

  "ConsoleFormatting": "{Title} {Name}: {Message}",

  "Formatting": "{Title} {Name}<color={TextColor}>:</color> {Message}",

  "NameColor": "yellow",

  "Permission": "regular",

  "Rank": 2,

  "TextColor": "white",

  "Title": "[Regular]",

  "TitleColor": "lime"

  },

  "Pro": {

  "ConsoleFormatting": "{Title} {Name}: {Message}",

  "Formatting": "{Title} {Name}<color={TextColor}>:</color> {Message}",

  "NameColor": "yellow",

  "Permission": "pro",

  "Rank": 2,

  "TextColor": "white",

  "Title": "[Pro]",

  "TitleColor": "lime"

 
````


**Known Bugs:**


* None


**Future Features:**


* Your suggestions


**Credit:**


* Credit goes to **LaserHydra** for helping me fix up the codes to make the plugin function properly and make it more dynamic.