Better Chat lets you change the name colors and prefixes as you want to, by using oxide permissions.

If you have this installed and you see somebody called [Oxide Plugin Dev] LaserHydra, thats me visiting your server. 

How to set up a custom group:

* Create a group in the config as shown in the preset groups
* Save the configfile
* Reload the plugin - Type /reload BetterChat
* Add yourself to the group - Type /usergroup add <Your Name> <Group Name>Now lets do that with the example of the V.I.P. group.

* Create the group: 

````
  "vip": {

    "ConsoleFormatting": "{Title} {Name}: {Message}",

    "Formatting": "{Title} {Name}<color={TextColor}>:</color> {Message}",

    "NameColor": "yellow",

    "Permission": "vip",

    "Rank": 2,

    "TextColor": "white",

    "Title": "[V.I.P.]",

    "TitleColor": "orange"

  }
````


* so it should look like this:

````
{

  "mod": {

    "ConsoleFormatting": "{Title} {Name}: {Message}",

    "Formatting": "{Title} {Name}<color={TextColor}>:</color> {Message}",

    "NameColor": "blue",

    "Permission": "color_mod",

    "Rank": 3,

    "TextColor": "white",

    "Title": "[Mod]",

    "TitleColor": "yellow"

  },

  "owner": {

    "ConsoleFormatting": "{Title} {Name}: {Message}",

    "Formatting": "{Title} {Name}<color={TextColor}>:</color> {Message}",

    "NameColor": "blue",

    "Permission": "color_owner",

    "Rank": 4,

    "TextColor": "white",

    "Title": "[Owner]",

    "TitleColor": "red"

  },

  "vip": {

    "ConsoleFormatting": "{Title} {Name}: {Message}",

    "Formatting": "{Title} {Name}<color={TextColor}>:</color> {Message}",

    "NameColor": "yellow",

    "Permission": "color_vip",

    "Rank": 2,

    "TextColor": "white",

    "Title": "[V.I.P.]",

    "TitleColor": "orange"

  },

  "player": {

    "ConsoleFormatting": "{Title} {Name}: {Message}",

    "Formatting": "{Title} {Name}<color={TextColor}>:</color> {Message}",

    "NameColor": "blue",

    "Permission": "color_player",

    "Rank": 1,

    "TextColor": "white",

    "Title": "[Player]",

    "TitleColor": "blue"

  },

  "WordFilter": {

    "Enabled": false,

    "FilterList": [

      "fuck",

      "bitch",

      "faggot"

    ]

  }
}
````


* Save the configfile
* Reload the plugin - Type /reload BetterChat
* Add yourself to the group - Type /usergroup add LaserHydra vipTo set priority ( means if someone has multiple groups, setting which color is actually used ) just change the rank of it ( the higher the value the higher the rank )

Commands:


* /colors shows all available colors (You can also use -> [HEX Codes](http://html-color-codes.info/)!)
* /mute <player> mute somebody
* /unmute <player> unmute somebody

Permissions:


* betterchat.formatting for the usage of formatting tags like <color=orange>
* betterchat.mute to mute and unmute players

How permissions work:


* /grant user <player> <permission> grant permission to player
* /grant group <group> <permission> grant permission to group

Extra features:


* different colors and prefixes depending on oxide groups
* You can do alot with the "Formatting" of a group. you can customize it with

* {Rank} = Group Rank
* {Title} = Group Title
* {TitleColor} = Group Title Color
* {NameColor} = Group Name Color
* {TextColor} = Group Text Color
* {Name} = Player Name
* {ID} = Player SteamID
* {Message} = Message



And also just add words, letters, numbers, and symbols to it. Its everything possible. You can also just out the Title behind the name or stuff like that.

Standard Config file:

````
{

  "admin": {

    "ConsoleFormatting": "{Title} {Name}: {Message}",

    "Formatting": "{Title} {Name}<color={TextColor}>:</color> {Message}",

    "NameColor": "#DCFF66",

    "Permission": "admin",

    "Rank": 3,

    "TextColor": "white",

    "Title": "[Admin]",

    "TitleColor": "red"

  },

  "AntiSpam": {

    "Enabled": false,

    "MaxCharacters": 85

  },

  "default": {

    "ConsoleFormatting": "{Title} {Name}: {Message}",

    "Formatting": "{Title} {Name}<color={TextColor}>:</color> {Message}",

    "NameColor": "#DCFF66",

    "Permission": "player",

    "Rank": 1,

    "TextColor": "white",

    "Title": "[Player]",

    "TitleColor": "#C4FF00"

  },

  "moderator": {

    "ConsoleFormatting": "{Title} {Name}: {Message}",

    "Formatting": "{Title} {Name}<color={TextColor}>:</color> {Message}",

    "NameColor": "#DCFF66",

    "Permission": "moderator",

    "Rank": 2,

    "TextColor": "white",

    "Title": "[Mod]",

    "TitleColor": "yellow"

  },

  "Mute": {

    "Enabled": true

  },

  "WordFilter": {

    "CustomReplacement": "Unicorn",

    "Enabled": false,

    "FilterList": [

      "fuck",

      "bitch",

      "faggot"

    ],

    "UseCustomReplacement": false

  }
}

 
````

For Developers:


API methods:


* Dictionary<string, object> GetPlayerFormatting(BasePlayer player)
* Dictionary<string, object> GetGroup(string name)

* List<string> GetGroups()
* List<string> GetPlayersGroups(BasePlayer player)
* bool GroupExists(string name)
* bool AddPlayerToGroup(BasePlayer player, string name)
* bool RemovePlayerFromGroup(BasePlayer player, string name)
* bool PlayerInGroup(BasePlayer player, string name)
* bool AddGroup(string name, Dictionary<string, object> group)

Data from GetPlayerFormatting(BasePlayer player):


* Formatting = Config[Formatting]
* ConsoleFormatting = Config[ConsoleFormatting]
* GroupRank = Config[GroupRank]
* Title = Config[Title]
* TitleColor = Config[TitleColor]
* NameColor = Config[NameColor]
* TextColor = Config[TextColor]

[EXAMPLE GetPlayerFormatting(BasePlayer player)]
Code (Lua):
````
PLUGIN.Title ="Better Chat Data Grabbing"

PLUGIN.Version = V(1,0,0)

PLUGIN.Description ="Data Grabbing"

PLUGIN.Author ="LaserHydra"

function PLUGIN:Init()

    command.AddChatCommand("grab", self.Object, "cmdGrab")
end

function PLUGIN:cmdGrab(player)

   local betterChat = plugins.Find("BetterChat")

   if betterName then

        data = betterChat:Call("GetPlayerFormatting", player)

        hurt.SendChatMessage(player, "Your Prefix", data.Title)

        hurt.SendChatMessage(player, "Your Prefix Color", data.TitleColor)

        hurt.SendChatMessage(player, "Your Name Color", data.NameColor)

        hurt.SendChatMessage(player, "Your Text Color", data.TextColor)

   end
end
````

Code (C#):
````
using System.Collections.Generic;
using System.Reflection;
using System;
using System.Data;
using Oxide.Core;

namespace Oxide.Plugins
{

    [Info("Better Chat Data Grabbing", "LaserHydra", "1.0.0", ResourceId = 0)]

    [Description("Data Grabbing")]

    class ChatnameDataGrab : HurtworldPlugin

    {

        [ChatCommand("grab")]

        void cmdGrab(BasePlayer player)

        {

           Plugin betterChat = null;

           betterChat = plugins.Find("BetterChat");

           if(betterChat!= null)

           {

                var data = betterChat.Call("GetPlayerFormatting", player);

                hurt.SendChatMessage(player, "Your Prefix: " + data.Title);

                hurt.SendChatMessage(player, "Your Prefix Color: " + data.TitleColor);

                hurt.SendChatMessage(player, "Your Name Color: " + data.NameColor);

                hurt.SendChatMessage(player, "Your Text Color: " + data.TextColor);

           }

        }

    }
}
````