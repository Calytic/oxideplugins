**Better Chat** lets you change the name colors and prefixes as you want to, by using oxide permissions.
**If you have this installed and you see somebody called [Plugin Developer] LaserHydra, thats me visiting your server. 

Easily edit your oxide/data/BetterChat/Groups file using the BetterChat Group Manager:**
[< Download >](http://laserhydra.com/downloadfiles/BetterChatManager/BetterChat%20Group%20Manager%20Installer.exe)
**

Plugins which use BetterChat:**


* Player Challenges for Rust Experimental | Oxide
* Player Rankings for Rust Experimental | Oxide


**

All arguments inside [ ] are optional!

Commands:**


* 
**/chat group add <group>** create a new group
* 
**/chat group remove <group>** remove a group
* 
**/chat group set <group> <setting> <value>** change a group setting
* 
**/chat group list** list all groups
* 
**/chat user add <player|steamid> <group>** add a user to a group
* 
**/chat user remove <player|steamid> <group>** remove a user from a group
* 
**/chat user groups <player|steamid>** list groups of a user



* 
**/mute <player|steamid> [time]** mute a player
* 
**/unmute <player|steamid> **unmute a player



* 
**/muteall [time]** mute all players
* 
**/unmuteall** unmute all players



* 
**/ignore <player|steamid>** ignore a player
* 
**/unignore <player|steamid>** stop ignoring a player


**

Console Commands:**


* 
****chat group add <group>** **create a new group
* 
****chat group remove <group>** **remove a group
* 
****chat group set <group> <setting> <value>** **change a group setting
* 
**chat group list** list all groups

* 
****chat user add <player|steamid> <group>** **add a user to a group
* 
****chat user remove <player|steamid> <group>** **remove a user from a group
* 
****chat user groups <player|steamid>** **list groups of a user



* 
**mute <player|steamid> [time]** mute a player
* 
**unmute <player|steamid> **unmute a player



* 
**muteall [time]** mute all players
* 
**unmuteall** unmute all players


**

Permissions:**


* 
**betterchat.admin **for the /chat command
* 
**betterchat.mute** to mute and unmute players


**

Granting permissions:**

Use following CONSOLE commands to grant permissions


* 
**grant user <player> <permission>** grant permission to player
* 
**grant group <group> <permission>** grant permission to group


**

Chat-/Console Formatting :**


* different colors and prefixes depending on oxide groups
* You can do alot with the "Formatting" of a group. you can customize it with
{Title} = Group Title{Name} = Player Name{SteamID} = Player SteamID{Message} = Message{Date} = Time Stamp{Group} = Primary Group


And also just add words, letters, numbers, and symbols to it. Its everything possible. You can also just out the Title behind the name or stuff like that.
**

Default Configfile:**

````
{
"Anti Flood": {

   "Enabled": true,

   "Seconds": 1.5
},
"General": {

   "Reverse Title Order": false
},
"Word Filter": {

   "Custom Replacement": "Unicorn",

   "Enabled": false,

   "Phrases": [

     "bitch",

     "faggot",

     "fuck"

   ],

   "Replacement": "*",

   "Use Custom Replacement": false
}
}
````


**Default Languagefile:**

````
{
"No Permission": "You don't have permission to use this command.",
"Group Created": "Group '{group}' was created.",
"Group Removed": "Group '{group}' was removed.",
"Group Already Exists": "Group '{group}' already exists!",
"Group Does Not Exist": "Group '{group}' does not exist!",
"Player Added To Group": "Player {player} was added to group '{group}'.",
"Player Removed From Group": "Player {player} was removed from group '{group}'.",
"Invalid Type": "Failed to convert value: {Message}",
"Failed To Set Group Value": "Failed to set group value: {Error}",
"Muted Player": "{player} was muted!",
"Time Muted Player": "{player} was muted for {time}!",
"Unmuted Player": "{player} was unmuted!",
"Player Is Not Muted": "{player} is not muted.",
"Player Already Muted": "{player} is already muted.",
"You Are Muted": "You are muted. You may not chat.",
"You Are Time Muted": "You are muted for {time}. You may not chat.",
"Ignoring Player": "You are now ignoring {player}.",
"No Longer Ignoring Player": "You are no longer ignoring {player}.",
"Not Ignoring Player": "You are not ignoring {player}.",
"Already Ignoring Player": "You are already ignoring {player}.",
"Player Group List": "{player}'s groups: {groups}",
"Chatting Too Fast": "You're chatting too fast - try again in {time} seconds",
"Time Muted Global": "All players were muted for {time}!",
"Muted Global": "All players were muted!",
"Unmuted Global": "All players were unmuted!"
}
````


**

For Developers:**
**API calls:**


* Dictionary<string, object> API_FindGroup(string name)
* List<Dictionary<string, object>> API_GetAllGroups()
* Dictionary<string, object> API_FindPlayerPrimaryGroup(BasePlayer player)
* List<Dictionary<string, object>> API_FindPlayerGroups(BasePlayer player)
* bool API_GroupExists(string name)
* bool API_AddGroup(string name)
* bool API_IsUserInGroup(BasePlayer player, string groupName)
* bool API_RemoveUserFromGroup(BasePlayer player, string groupName)
* bool API_AddUserToGroup(BasePlayer player, string groupName)
* object API_SetGroupSetting(string groupName, string key, string value)
* bool API_IsPlayerMuted(BasePlayer player)
* bool API_PlayerIgnores(BasePlayer player1, BasePlayer player2)