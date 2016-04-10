**Introduction**

This plugin was inspired by a suggestion I read on the forums.  I was looking for a new project and decided to create it.  Since this plugin intercepts player chat, it may or may not play well with other plugins that do so.  For this reason, player name and message colors and player tags are included with this plugin.

**Description**

Allows players to create, join and manage chat channels.  Also allows players to chat locally.

**Notes**:


* Setting "ShowNewChannels", if true, will display a configurable message to all players when a new channel is created.  A message is not shown for channels created with a password.
* Setting "PrintToConsole", if true, will print all chat messages, channel messages and radius messages to the server console, which is logged.
* Setting "GlobalAdmin", if true, will show administrators all messages in all channels.

* Radius settings "GlobalOnly", if true, will only allow players in global chat (not in a channel) to send and receive radius messages.
* Players with permission "chan.admin" can create official channels by using the create command and using "official" as the password.  All players will be able to join official channels.  The purpose of official channels is to show players which channels are sponsored by the server.  Official channels also show as green in the channel list.  Official channels can be removed by setting the password to "none".
* Only channel owners (and admins) can delete channels, mod and unmod players.  Owners cannot use chat commands on admins.
* Moderators of channels (and admins) can kick, ban and change the channel password.  Moderators cannot use chat commands on owners or admins.
* Channel owners can join their channels without providing a password even if a password is set.
* Players (not admins) are limited to owning one channel at a time.
* All players are limited to joining one channel at a time.
* Setting "UsePermissions" applies to "chan.join" and "chan.create" only.
* Since this plugin and my other plugin "Radius" both have local chat, if using both plugins, disable local chat in one or the other to avoid conflicts.


**Permissions**

This plugin uses oxide permissions.


chan.join - Allows players to join channels

chan.create - Allow players to create channels

chan.admin - Grants players administrator control

chan.gread - Allows players to read global chat while in a channel

chan.gsend - Allows players to send global chat messages while in a channel

chan.tag_admin - Displays the admin tag prefix in front of players names

chan.tag_mod - Displays the mod tag prefix in front of players names

chan.tag_player - Displays the player tag prefix in front of players names

````
oxide.grant <group|user> <name|id> chan.join

oxide.revoke <group|user> <name|id> chan.join
````


**Usage for players**


Chat Channels

/chan - View help

/chan list - List currently active channels

/chan create <channel> [password] - Create a channel with optional password

/chan delete <reason> - Delete your channel (cannot be undone)

/chan password <password> - Create, change or remove a channel password ('none' to remove)

/chan join <channel> [password] | leave - Join or leave a channel

/chan kick | ban | unban | mod | unmod <player> - Kick, (un)ban or (un)mod a player in your channel

/chan unbanall | banlist | unmodall | modlist | info - Unban/unmod all players, view the ban/mod list or channel information

**Note**: With the exception of list, create and join, you must be in your channel to use commands.


Radius Messages

/w <message> - Send a whisper message (50m)

/l <message> - Send a local message (150m)

/y <message> - Send a yell message (300m)

**Note**: Radius chat commands and radius range can be configured.  The figures above are default values.

**Usage for administrators
**

/chan <enable | disable> - Enable or disable chat channels system

/chan global <channel | *> <message> - Send a message to channel or all channels


* Use any command listed above on any channel or player
* Create unlimited channels
* Create official channels **(see notes above)**
* Join password protected channels
* Cannot be kicked or banned from channels


**Note**: The enable and disable command applies to chat channels only.  Radius messages and player tags must be toggled in the configuration file.  When chat channels is disabled, all players currently in a channel will be kicked to global chat.

**Configuration file**

````
{

  "Messages": {

  "Admin": "You cannot use channel commands on administrators.",

  "AdminForceJoined": "<color=#cd422b>{player}</color> has been forced into channel <color=#cd422b>{channel}</color>.  (reason: <color=#ffd479>{reason}</color>, channel has password: <color=#ffd479>{password}</color>, player is banned: <color=#ffd479>{banned}</color>)",

  "AlreadyBanned": "<color=#cd422b>{player}</color> is already banned from channel.",

  "AlreadyInChannel": "You are already in channel <color=#cd422b>{channel}</color>.  You must leave before joining another.",

  "AlreadyInForceChannel": "<color=#cd422b>{player}</color> is already in channel <color=#cd422b>{channel}</color>.",

  "AlreadyInGlobal": "You are already in global chat.  You may chat normally without commands.",

  "AlreadyInJoinChannel": "You are already in channel <color=#cd422b>{channel}</color>.",

  "AlreadyMod": "<color=#cd422b>{player}</color> is already a moderator in channel.",

  "AlreadyStatus": "Chat channels is already <color=#cd422b>{status}</color>.",

  "Banned": "You have been banned from channel <color=#cd422b>{channel}</color> by <color=#cd422b>{player}</color>.  You are now in global chat.",

  "ChanBan": "<color=#cd422b>{player}</color> has banned <color=#cd422b>{target}</color> from the channel.",

  "ChangedStatus": "Chat channels <color=#cd422b>{status}</color>.",

  "ChanJoin": "<color=#cd422b>{player}</color> has joined the channel.",

  "ChanKick": "<color=#cd422b>{player}</color> has kicked <color=#cd422b>{target}</color> from the channel.",

  "ChanMod": "<color=#cd422b>{player}</color> has moderated <color=#cd422b>{target}</color> in the channel.",

  "ChannelCreated": "Channel <color=#cd422b>{channel}</color> successfully created.  To join your channel, use <color=#cd422b>/chan join {channel}</color>.",

  "ChannelDeleted": "Channel <color=#cd422b>{channel}</color> successfully deleted.  You are now in global chat.",

  "ChannelExists": "Channel <color=#cd422b>{channel}</color> already exists.  Choose another name.",

  "ChannelNotExists": "Channel <color=#cd422b>{channel}</color> does not exist.",

  "ChanPart": "<color=#cd422b>{player}</color> has left the channel.",

  "ChanUnban": "<color=#cd422b>{player}</color> has unbanned <color=#cd422b>{target}</color> from the channel.",

  "ChanUnmod": "<color=#cd422b>{player}</color> has unmoderated <color=#cd422b>{target}</color> from the channel.",

  "ForceJoined": "Administrator has forced you into channel <color=#cd422b>{channel}</color>.  (reason: <color=#ffd479>{reason}</color>)  Use <color=#cd422b>/chan</color> for help.",

  "GlobalChatError": "Global chat is already <color=#cd422b>{status}</color>.",

  "GlobalChatOff": "Global chat is <color=#cd422b>disabled</color>.  Use <color=#cd422b>/chan globalchat enable</color> to enable global chat.",

  "GlobalChatOn": "Global chat is <color=#cd422b>enabled</color>.  Use <color=#cd422b>/{command} <message></color> to chat in global chat.",

  "GlobalSend": "Your message, <color=#ffd479>{message}</color>, has been sent to channel <color=#cd422b>{channel}</color>.",

  "GlobalSendAll": "Your message, <color=#ffd479>{message}</color>, has been sent to <color=#cd422b>{count}</color> channel(s).",

  "InvalidChannel": "Invalid channel name.  Cannot contain restricted words.",

  "InvalidPassword": "Invalid channel password.  Must be at least five characters long and cannot contain restricted words.",

  "JoinBanned": "You are banned from channel <color=#cd422b>{channel}</color>.",

  "Kicked": "You have been kicked from channel <color=#cd422b>{channel}</color> by <color=#cd422b>{player}</color>.  You are now in global chat.",

  "MatchPassword": "Password for channel is already <color=#cd422b>{password}</color>.",

  "MultiPlayer": "Multiple players found.  Provide a more specific username.",

  "NewOfficialCreated": "Official channel <color=#cd422b>{channel}</color> successfully created.  To join your channel, use <color=#cd422b>/chan join {channel}</color>.",

  "NoBans": "No bans found for channel.",

  "NoChannel": "You are not in a channel.",

  "NoChannels": "No channels found.",

  "NoMessage": "You must provide a message.",

  "NoMods": "No moderators found for channel.",

  "NoPassword": "Channel does not have a password.",

  "NoPermission": "You do not have permission to use this command.",

  "NoPlayer": "Player not found.  Please try again.",

  "NotBanned": "<color=#cd422b>{player}</color> is not banned in channel.",

  "NotEnabled": "Chat channels is <color=#cd422b>disabled</color>.",

  "NotInChannel": "Player <color=#cd422b>{player}</color> is not in channel.",

  "NotMod": "<color=#cd422b>{player}</color> is not a moderator in channel.",

  "NotOwner": "Only the owner of this channel can use this command.",

  "NotOwnerMod": "You have no access to this channel.",

  "OfficialChannelCreated": "<color=#cd422b>{player}</color> created new official channel <color=#cd422b>{channel}</color>.",

  "OfficialCreated": "Channel is now an official channel.",

  "OfficialRemoved": "Channel is no longer an official channel.",

  "Owner": "You cannot use channel commands on channel owners.",

  "OwnerDeleted": "You have been kicked from <color=#cd422b>{channel}</color>.  <color=#cd422b>{owner}</color> has deleted the channel (reason: <color=#cd422b>{reason}</color>).  You are now in global chat.",

  "OwnerExists": "You may only own one channel at a time.",

  "PartChannel": "You have left channel <color=#cd422b>{channel}</color>.  You are now in global chat.",

  "PasswordChanged": "Password for channel successfully set to <color=#cd422b>{password}</color>.",

  "PasswordRemoved": "Password for channel successfully removed.",

  "PlayerChannelCreated": "<color=#cd422b>{player}</color> created new channel <color=#cd422b>{channel}</color>.",

  "RadiusGlobalOnly": "Radius chat may only be used while in global chat.",

  "Self": "You cannot use channel commands on yourself.",

  "SystemDisabled": "You have been kicked from <color=#cd422b>{channel}</color>.  Administrator has disable Chat Channels system.  You are now in global chat.",

  "UnbannedAll": "All channel bans successfully removed.",

  "UnmodAll": "All channel moderators successfully removed.",

  "WrongArgs": "Syntax error.  Use <color=#cd422b>/chan</color> for help.",

  "WrongPassword": "Password provided for channel <color=#cd422b>{channel}</color> is incorrect."

  },

  "Radius": {

  "Enable": "true",

  "GlobalOnly": "false",

  "LocalColor": "red",

  "LocalCommand": "l",

  "LocalRadius": "300.0",

  "Prefix": "<color=white>[</color> <color={color}>{radius} ({meters}m)</color> <color=white>]</color>",

  "WhisperColor": "yellow",

  "WhisperCommand": "w",

  "WhisperRadius": "50.0",

  "YellColor": "orange",

  "YellCommand": "y",

  "YellRadius": "150.0"

  },

  "Settings": {

  "AdminPrefix": "[<color=#cd422b>ADMIN</color>]",

  "ChanPrefix": "<color=white>[</color> <color=#f9169f>{channel}</color> <color=white>]</color>",

  "ConsolePrint": "[{chat}] {tag} {player}: {message}",

  "DefaultGlobalChat": "false",

  "Enabled": "true",

  "GlobalAdmin": "false",

  "GlobalChatCommand": "g",

  "MessageColor": "white",

  "PlayerColor": "teal",

  "Prefix": "[<color=#cd422b>Chat Channels</color>]",

  "PrintToConsole": "true",

  "ShowNewChannels": "true",

  "TagsEnabled": "true",

  "UsePermissions": "true"

  },

  "Tags": [

  "chan.tag_admin:<color=#cd422b>[ADMIN]</color>:teal:white",

  "chan.tag_mod:<color=orange>[MOD]</color>:teal:white",

  "chan.tag_player:<color=yellow>[PLAYER]</color>:teal:white"

  ]

}
````

Configuration file will be created and updated automatically.

**More to come**


* Your suggestions


**Known Issues**


* None