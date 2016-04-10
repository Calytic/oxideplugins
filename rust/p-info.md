Its a plugin that took me a bit of more time than all the others but i got it to work properly with some APIs from other plugins. You can check some information that the plugin currently supports like(Muted, Afk, IP, Ping, SteamID and Position) for now.

**NEEDED PLUGINS** **(Otherwise the plugin wont work well and will throw some errors)


1. PaiN AFK  **[http://oxidemod.org/plugins/pain-afk.976/](http://oxidemod.org/plugins/pain-afk.976/)
**2. ChatMute **[http://oxidemod.org/plugins/chatmute.1053/](http://oxidemod.org/plugins/chatmute.1053/)

**NOTES:**

- Every message is configurable except of one.

**Commands:
/myinfo **--> Shows your information
**/ainfo [target]** --> Shows some more information about your target than the self check.** [NEEDS PERMISSION]**
**/pinfo** --> Not Available right now. This command will allow players to check some not serious info.

**Permissions:**
"cancheckinfo" **=** Permission for the /ainfo command


**Default Config:**

````

{

  "AinfoSettings": {

    "DisplayAfk": "true",

    "DisplayIP": "true",

    "DisplayMute": "true",

    "DisplayName": "true",

    "DisplayPing": "true",

    "DisplayPosition": "true",

    "DisplaySteamId": "true",

    "PrintToF1Console": "true",

    "PrintToRconConsole": "true"

  },

  "Messages": {

    "ChatPrefix": "P-INFO",

    "HisIPis": "His/Her IP is:",

    "HisName": "Player Name:",

    "HisPingIs": "His/Her PING is:",

    "HisPositionIs": "His/Her Position is:",

    "HisSteamId": "His/Her Steam ID is:",

    "MultiplePlayers": "Multiple users found: ",

    "NoPermission": "You do not have permission to use this command!",

    "NoTarget": "Syntax: /ainfo <player>",

    "PlayerIsAfk": "This player is AFK !",

    "PlayerIsMuted": "This player is MUTED!",

    "PlayerIsNotAfk": "This player is NOT AFK! ",

    "PlayerIsNotMuted": "This player is NOT MUTED!",

    "PlayerNotFound": "Player not found!",

    "ResearchForUser": "[Research For User]",

    "YouAreMuted": "You are MUTED!",

    "YouAreNotMuted": "You are NOT MUTED!",

    "YouAreVip": "You are a VIP",

    "YourIpIs": "Your IP is:",

    "YourPingIs": "Your PING is:",

    "YourPositionIs": "Your Position is:",

    "YourSteamIdIs": "Your Steam ID is:"

  },

  "MyInfoSettings": {

    "DisplayIP": "true",

    "DisplayMute": "true",

    "DisplayPing": "true",

    "DisplayPosition": "true",

    "DisplaySteamId": "true"

  }

}

 
````


**TODO LIST:

-IP** for /ainfo = **DONE**

**Uknown ISSUES:

PlayerIsAfk** = I'm not sure if it works well if only could someone test it :/

**Admin Info Command (/ainfo) Images** from Chat, Rcon Console and "F1" Console!

**Chat Message Image:**
[http://screenshooter.net/101978843/vxfroyd](http://screenshooter.net/101978843/vxfroyd)

**Rcon Console Image:**
[http://screenshooter.net/101978843/fkrxtsq](http://screenshooter.net/101978843/fkrxtsq)
**

In-Game "F1" Console:**
[http://screenshooter.net/101978843/urfkosl](http://screenshooter.net/101978843/urfkosl)