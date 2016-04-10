**Special thanks to mughisi, he pretty much made the hardest part of the plugin 

Chat Commands:**

- /ban PLAYER/STEAMID REASON => bans a player, if the player is connected he will get kicked also

- /kick PLAYER/STEAMID REASON => kicks a player from the server

- /unban PLAYERNAME/STEAMID => unban a player by name or steamid

- /banlist XX => see the banlist by with a specific index (1 is default)
**Permissions:**

/ban => **canban **& all

/unban => **canunban **& all

/kick => **cankick **& all

/banlist => canban & canunban & all
Oxide permission system pretty much works like legacy's Flags plugin.

You can also create groups, add permissions to that groups and assign users to it so every user in that group has the permission of that group.
You can grant a user permission by using:
**oxide.grant user <username> <permission>**
To create a group:
**oxide.group add <groupname>**
To assign permission to a group:
**oxide.grant group <groupname> <permission>**
To add users to a group:
**oxide.usergroup add <username> <groupname>**
To remove users permission:
**oxide.revoke <userid/username> <group> <permission>**
Click to expand...
**Console Commands (rust commands):**

banid STEAMID "NAME" "REASON" => ban a steamid

ban PLAYERNAME REASON => ban a player

removeid STEAMID => unban a player

unbanall => clear the banlist

kick PLAYER REASON => kick a player

banlistex => see the full banlist

Config file: KickBan.json

````
{

  "Messages: Ban ({0} is the userid, {1} the name, {2} the reason": "{0} - {1} was banned from the server - {2}",

  "Messages: Kick ({0} is the userid, {1} the name, {2} the reason": "{0} - {1} was kicked from the server - {2}",

  "Messages: No player found": "Couldn't find the target user",

  "Messages: Not Allowed": "You are not allowed to use this command.",

  "Messages: Unban ({0} is the userid, {1} the name": "{0} - {1} was unbanned from the server",

  "Settings: Broadcast Bans": true,

  "Settings: Broadcast Kicks": true

}
````