Simple plugin designed to give mute ability for admins.

**Commands**

/mute playername minutes - Mutes player for x minutes, if no minutes are given plugin will use default time from config
/unmute player - Unmutes player if he is muted

**Permissions**


Admins can use when logged as admin


You can give non-admin players permission to mute. Just give them oxide permission **canmute **


Example:

oxide.grant user playername canmute


If you want remove access to mute system just revoke permission with

oxide.revoke user playername canmute

**Default config:**


````
{

  "broadcastMutes": true,

  "defaultMuteTime": 5,

  "hasBeenMuted": " [color yellow]has been muted",

  "youAreMuted": "You are muted!."

}
````