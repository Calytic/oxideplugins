**THIS PLUGIN IS FOR LEGACY OXIDE 2.0!


Commands: (0)**

- /zone_add => Add a zone

- /zone_edit XXXX => Edit a zone

- /zone option1 value1 option2 value2 option3 value3 etc => set the zone as you want it.

- /zone_list => Gets the list of all current zones

- /zone_remove XXXX=> deletes a specific zone

- /zone_reset => resets all zones

**Options:**

eject true/false => all players will be kicked out of the zone when trying to enter it (1)

enter_message "XXXXX"/false => set a message that will be sent to players when they enter the zone

leave_message "XXXXXX"/false=> set a message that will be sent to players when they leave the zone

pvpgod true/false => all players have PVP god mode in the zone

pvegod true/false => all players have PVE god mode in the zone

sleepgod true/false => all sleepers have god mode in the zone

undestr true/false => all buildings can't be destroyed in the zone

name XXX => set a name for the zone

radius XXX => set the radius of the zone (default is 20)

nochat true/false => prevent players from chatting in this zone

nobuild true/false => no buildings can be built in the zone (2)

nodeploy true/false => items will not be allowed to be deployed (3)

notp true/false => no teleportation commands can be used to get in or out of the zone (compatible: [Teleportation Requests](http://oxidemod.org/plugins/teleportation-requests.941/) & [SetHome](http://oxidemod.org/plugins/sethome.951/))

nokits true/false => no kits can be requested inside the zone (Compatible:
[Kits](http://oxidemod.org/plugins/kits.668/) plugin)

nosuicide true/false => can't write "suicide" in the console, to commit suicide

killsleepers true/false => all sleepers will be killed when they go to sleep here

radiation XX/false => add radiation to the zone. Radiation amount is set by minute. So setting 5000 will get will 5000 radiation in 1min.


(0): Commands are accessible for admins and for oxide permission: "**zone**"

(1): Wont be ejected are admins and players in the whitelist (only editable via outer plugins, so yeah this plugin can now be used easily by outer plugins)

(2): can build admins and oxide permission: "**canbuild**"

(3): can deploy admins and oxide permission: "**candeploy**"

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
**Plugin Developpers**

**Hooks:**

````
void OnEnterZone(string ZoneID, PlayerClient player)
````

Called when a player enters a zone

no return


````
void OnExitZone(string ZoneID, PlayerClient player)
````

Called when a player leaves a zone

no return

**External Calls to this plugin:**

````
bool CreateOrUpdateZone(string ZoneID, string[] args, Vector3 position = default(Vector3))
````

Create or Update a Zone from an external plugin

Option 1: is the Zone ID that you want (can be a name)

Option 2: are the options as you would put them in /zone ex:

args[0] = "name"

args[1] = "Jail"

args[2] = "eject"

args[3] = "true"

args[4] = "enter_message"

args[5] = "Welcome to the jail"

args[6] = "radius"

args[7] = "120"

Option 3 (optional): is to set or edit the location of the zone


The plugin returns TRUE if the zone is valid

and FALSE if it was saved but NOT created (only reason would be that no position for the zone was set)


````
bool EraseZone(string ZoneID)
````

Erase a zone by ZoneID (can be a name)

returns TRUE if the zone was deleted

returns FALSE if the zone was already deleted


````
List<BasePlayer> GetPlayersInZone(string ZoneID)
````

Get the list of players in the zone


````
bool isPlayerInZone(string ZoneID, PlayerClient player)
````

returns TRUE if the player is in the zone

returns FALSE if the player is not in the zone


````
bool AddPlayerToZoneWhitelist(string ZoneID, PlayerClient player)
````

Requires: EJECT TRUE

Will allow a specific player to enter a zone

returns TRUE if everything went OK

returns FALSE if the zone doesn't exist


````
bool RemovePlayerFromZoneWhitelist(string ZoneID, PlayerClient player)
````

Requires: EJECT TRUE

Will revoke access to a zone (the player will need to get out first before being ejected)

returns TRUE if everything went OK

returns FALSE if the zone doesn't exist


````
bool AddPlayerToZoneKeepinlist(string ZoneID, PlayerClient player)
````

Will JAIL this player inside the zone, he will not be able to get out

returns TRUE if everything went OK

returns FALSE if the zone doesn't exist

(it does NOT teleport the player to the zone, so you will need to place the player inside the zone first, and if you have eject TRUE, make sure you also allow them to get inside  or something fun will happen)


````
bool RemovePlayerFromZoneKeepinlist(string ZoneID, PlayerClient player)
````

Will unJAIL a player inside the zone

returns TRUE if everything went OK

returns FALSE if the zone doesn't exist

(this does not teleport the player out or anything, just allows him to get out)



If you have any questions, or need new hooks, etc, feel free to ask.