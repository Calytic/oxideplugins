**Description**

Shows clan members, mutual friends, attackers and hit mark locations to other players.

**Notes**


* Settings "ForceClan", ForceFriend", "ForceAttack" and "ForceHit". When true, these radar systems will be enabled for players when the plugin is loaded or a player connects. The radar system you wish to force must also be enabled under their own section. These options will obey user permissions, if enabled.
* Setting "AllowForceChange", when true, will allow players to toggle on or off a radar system that is forced on.
* Setting "NotifyForced", when true, will notify the player that one or more radar systems have been forced on for them.
* Setting "RememberPlayerConfig", when true, will remember players radar config (on or off) when they disconnect and connect. This information is cleared when the plugin or server is restarted.
* Setting "EnforceHours", when true, hour restrictions will be enforced.  This feature will use settings "StartHour" and "EndHour".
* Setting "UsePermissions" applies to "radarmanager.clan", "radarmanager.friend", "radarmanager.attack" and "radarmanager.hit" only.


**What it does**

Clan Radar

Will show clan members each others locations.


Friend Radar

Will show mutual friends each others locations.


Attack Radar

Will show when someone fires or throws a weapon.


Hit Radar

Will show when you hit someone with any weapon.

**Radar Systems**

Each radar system has a "Tag" setting which allows you to define the tag how you wish. The following variables are available for use.


{player} - shows the players name

{location} - shows the players location

{range} - shows the range between you and the player (this variable cannot be used in admin radar tags)


Each radar system also allows you to define "TagOffset". For example, 2 will show the tag above the players head while 1 will show it in the middle of their body.


The setting "Radius" defines in what range a player must be within to show on a radar system.


The setting "Refresh" defines how often that radar system is updated for players.

**Attack and Hit Radar Systems**

Settings "EnableFirearm", "EnableRocket" and "EnableThrow" define what actions are detected by radar. Set to true to enable.


Settings "ShowClan" and "ShowFriend" define if clan members or mutual friends are shown to clan mates or other friends. If true and other radar systems are enabled, tags may overlap. Change tag offsets to fix this.


Setting "AttackTimeout" defines how often a tag is shown for a player when they fire or throw a weapon.


Setting "TagTimeout" defines how long the tag is shown for the player to other players. This setting should always be less than the AttackTimeout setting.


Setting "ShowOnlineOnly", when true, will only show hit tags for players that are online.

**Permissions**

This plugin uses oxide permissions.


"radarmanager.all" - Grants players access to clan, friend, attack and hit only

"radarmanager.clan" - Grants players access to clan radar system

"radarmanager.friend" - Grants players access to friend radar system

"radarmanager.attack" - Grants players access to attack radar system

"radarmanager.hit" - Grants players access to hit radar system

"radarmanager.hide" - Players will not show in any radar system

"radarmanager.immune" - Players are immune to time restrictions

"radarmanager.admin" - Grants players access to admin commands and immunities

````
oxide.grant <group|user> <name|id> radarmanager.all

oxide.revoke <group|user> <name|id> radarmanager.all
````


**Usage for players**


* /rm - View help
* /rm limits <system | clan | friend | attack | hit> - View radar limits
* /rm active <clan | friend | attack | hit> - Toggle radar system


**Usage for administrators**


* /rm toggle <clan | friend | attack | hit> - Enable or disable radar system
* /rm admin <off> | <radius> <refresh> <sleepers (0 | 1)> - Toggle global radar


**Configuration file**

````

{

  "Admin": {

  "ActiveTag": "<size=12><color=#cd422b>{player}</color> (<color=#ffd479>{location}</color>)</size>",

  "ActiveTagOffset": "2",

  "SleepTag": "(SLEEP) <size=12><color=#cd422b>{player}</color> (<color=#ffd479>{location}</color>)</size>",

  "SleepTagOffset": "1"

  },

  "Attack": {

  "AttackTimeout": "10",

  "Enabled": "false",

  "EnabledFirearm": "true",

  "EnabledRocket": "true",

  "EnabledThrow": "false",

  "Radius": "300",

  "ShowClan": "false",

  "ShowFriend": "false",

  "Tag": "<size=12><color=#41A317>{player}</color> - <color=#ffd479>{location}</color> ({range}m)</size>",

  "TagOffset": "2",

  "TagTimeout": "2"

  },

  "Clan": {

  "Enabled": "true",

  "Radius": "300",

  "Refresh": "3",

  "Tag": "<size=12><color=#cd422b>{player}</color> - <color=#ffd479>{location}</color> ({range}m)</size>",

  "TagOffset": "2"

  },

  "CustomPermissions": [

  {

  "AttackRadius": "400",

  "ClanRadius": "400",

  "FriendRadius": "400",

  "HitRadius": "400",

  "Permission": "radarmanager.vip1",

  "Tag": "<size=13><color=#FF00FF>{player}</color> - <color=#ffd479>{location}</color> ({range}m)</size>"

  },

  {

  "AttackRadius": "500",

  "ClanRadius": "500",

  "FriendRadius": "500",

  "HitRadius": "500",

  "Permission": "radarmanager.vip2",

  "Tag": "<size=13><color=#FF00FF>{player}</color> - <color=#ffd479>{location}</color> ({range}m)</size>"

  }

  ],

  "Friend": {

  "Enabled": "true",

  "Radius": "300",

  "Refresh": "3",

  "Tag": "<size=12><color=#2B60DE>{player}</color> - <color=#ffd479>{location}</color> ({range}m)</size>",

  "TagOffset": "2"

  },

  "Hit": {

  "Enabled": "false",

  "HitTimeout": "5",

  "Radius": "300",

  "ShowClan": "false",

  "ShowFriend": "false",

  "ShowOnlineOnly": "false",

  "Tag": "<size=12><color=#F87217>{player}</color> - <color=#ffd479>{location}</color> ({range}m)</size>",

  "TagOffset": "2",

  "TagTimeout": "0.5"

  },

  "Messages": {

  "AdminDisabled": "The <color=#cd422b>{radar}</color> radar has been disabled by an administrator.  Your <color=#cd422b>{radar}</color> radar has been disabled.",

  "AdminRadarDisabled": "Administrator radar disabled.",

  "AdminRadarEnabled": "Administrator radar enabled: <color=#cd422b>{radius} meter</color> radius, <color=#cd422b>{refresh} second</color> refresh, show sleepers <color=#cd422b>{sleepers}</color>",

  "AdminRadarNotEnabled": "Administrator radar is not enabled.",

  "ChangedStatus": "Radar system {radar} <color=#cd422b>{status}</color>.",

  "Forced": "Radar system <color=#cd422b>{radar}</color> is forced enabled and cannot be toggled.",

  "HourDisabled": "Radar systems may only be used between in-game hours of <color=#cd422b>{shour}</color> and <color=#cd422b>{ehour}</color>.  Current time is <color=#cd422b>{current}</color>.  All your active radar systems have been disabled.",

  "NoPermission": "You do not have permission to use this command.",

  "NotEnabled": "All radar systems are <color=#cd422b>disabled</color>.",

  "NotifyForced": "One or more radar systems are force enabled.  Use <color=#cd422b>/rm</color> for help.",

  "PlayerRadarDisabled": "You have <color=#cd422b>disabled</color> {radar} radar.",

  "PlayerRadarEnabled": "You have <color=#cd422b>enabled</color> {radar} radar.  Players will be visible within a <color=#cd422b>{radius} meter</color> radius.",

  "TypeDisabled": "Radar system <color=#cd422b>{radar}</color> is currently disabled.",

  "TypeDisabledPlugin": "Radar system <color=#cd422b>{radar}</color> cannot be toggled.  The required plugin <color=#cd422b>{plugin}</color> is not installed.",

  "WrongArgs": "Syntax error.  Use <color=#cd422b>/rm</color> for help.",

  "WrongHour": "Radar systems may only be used between in-game hours of <color=#cd422b>{shour}</color> and <color=#cd422b>{ehour}</color>.  Current time is <color=#cd422b>{current}</color>."

  },

  "Settings": {

  "AllowForceChange": "false",

  "EnableLimits": "true",

  "EndHour": "23",

  "EnforceHours": "false",

  "ForceAttack": "false",

  "ForceClan": "false",

  "ForceFriend": "false",

  "ForceHit": "false",

  "NotifyForced": "true",

  "Prefix": "[ <color=#cd422b>Radar Manager</color> ]",

  "RememberPlayerConfig": "false",

  "StartHour": "0",

  "UsePermissions": "true"

  }

}

 
````

Configuration file will be created and updated automatically.

**More to come**


* Your suggestions


**Known Issues**


* None