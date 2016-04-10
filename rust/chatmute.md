**If you'd like to support my work you can [Donate here](https://www.paypal.com/cgi-bin/webscr?cmd=_s-xclick&hosted_button_id=FFWZBZBCPWY2G)**

**Usage__________________________________________
Chat commands**

/mute <name/steamID> <time[m/h] (optional)>

/unmute <name|steamID> or /unmute all to clear mutelist

/globalmute to mute the whole chat and only allow admins and whitelisted players to chat
**Console commands**

player.mute <name/steamID> <time[m/h] (optional)>

player.unmute <name/steamID> or player.unmute all to clear mutelist

**Config__________________________________________**

Chat commands can be edited in the config. If you want to use more than one chat command for the same command simply seperate them by comma.

To whitelist people to write while globalmute is active give them or their group the permission set in the config.


Default config:

````

{

  "Messages": {

    "Admin": {

      "AlreadyMuted": "{name} is already muted",

      "InvalidTimeFormat": "Invalid time format",

      "MultiplePlayerFound": "Found more than one player, be more specific:",

      "MutelistCleared": "Cleared {count} entries from mutelist",

      "NoPermission": "You dont have permission to use this command",

      "PlayerMuted": "{name} has been muted",

      "PlayerMutedTimed": "{name} has been muted for {time}",

      "PlayerNotFound": "Player not found",

      "PlayerNotMuted": "{name} is not muted",

      "PlayerUnmuted": "{name} has been unmuted"

    },

    "Player": {

      "BroadcastMutes": "{name} has been muted",

      "BroadcastMutesTimed": "{name} has been muted for {time}",

      "BroadcastUnmutes": "{name} has been unmuted",

      "GlobalMuted": "Chat is globally muted by an admin",

      "GlobalMuteDisabled": "Global chat mute disabled",

      "GlobalMuteEnabled": "Chat is now globally muted",

      "IsMuted": "You are muted",

      "IsTimeMuted": "You are muted for {timeMuted}",

      "Muted": "You have been muted",

      "MutedTimed": "You have been muted for {time}",

      "Unmuted": "You have been unmuted"

    }

  },

  "Settings": {

    "ChatCommands": {

      "GlobalMute": [

        "globalmute"

      ],

      "Mute": [

        "mute"

      ],

      "Unmute": [

        "unmute"

      ]

    },

    "General": {

      "BroadcastMutes": "true",

      "LogToConsole": "true"

    },

    "Permissions": {

      "AntiGlobalMute": "chat.notglobalmuted",

      "GlobalMute": "chat.globalmute",

      "Mute": "chat.mute"

    }

  }

}

 
````



**Permissions______________________________________**

ChatMute uses [Oxide's permission system](http://oxidemod.org/threads/using-oxides-permission-system.8296/) to handle all permissions needed for specific features.

All permissions used can be changed in the config file.
Default permissions are:
**chat.mute** to mute and unmute players
**chat.globalmute** to use the globalmute feature
**chat.notglobalmuted** to not be affected by the global mute

Players with the permission "admin" can use everything regardless of other permissions.

**For other plugin developers__________________________**

If you're developing a plugin that handles chat messages itself and cancels default chat you will probably notice muted players can still chat when using both plugins together.

To work around that Chatmute also has an API function to call by external plugins.

If Chatmute is present you can call the function IsMuted(player) to check if the player sending the chat message is muted. If so simply abort your chat handling to let Chatmute handle it.
Code (Lua):
````
local ChatMute
function PLUGIN:OnServerInitialized()

    ChatMute = plugins.Find("chatmute")
end
function PLUGIN:OnPlayerChat(arg)

    if ChatMute then

        local isMuted = ChatMute:Call("IsMuted", player)

        if isMuted then return end

    end

    -- your chat handling stuff
end
````