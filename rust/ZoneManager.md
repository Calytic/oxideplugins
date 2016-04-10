**

Commands: (0)**

- /zone_add => Add a zone

- /zone_edit [XXXX] => Edit a zone

- /zone option1 value1 option2 value2 option3 value3 etc => set the zone as you want it.

- /zone_list => Gets the list of all current zones

- /zone_remove XXXX=> deletes a specific zone

- /zone_reset => resets all zones

- /zone_player [playerName] => show player zones & flags

- /zone_stats => shows known entity counts
**Options:**

autolights true/false => autolights on or off depending on the time of the day (settable in the configs)

eject true/false => all players will be kicked out of the zone when trying to enter it (1)

enter_message "XXXXX"/false => set a message that will be sent to players when they enter the zone

leave_message "XXXXXX"/false=> set a message that will be sent to players when they leave the zone

location here/"x y z" => change location of zone

pvpgod true/false => all players have PVP god mode in the zone

pvegod true/false => all players have PVE god mode in the zone

sleepgod true/false => all sleepers have god mode in the zone

undestr true/false => all buildings can't be destroyed in the zone

name XXX => set a name for the zone

radius XXX => set the radius of the zone (default is 20)

nochat true/false => prevent players from chatting in this zone

nobleed true/false => prevent players from taking damage from bleeding

nobuild true/false => no buildings can be built in the zone (2)

noboxloot true/false => prevent players from looting boxes

nodecay true/false => remove decay from all buildings and deployables in the area

nodeploy true/false => items will not be allowed to be deployed (3)

nocorpse true/false => remove players corpse if they die in this zone

nogather true/false => prevent people from gathering in this zone

notp true/false => no teleportation commands can be used to get in or out of the zone (m-Teleportation)

nokits true/false => no kits can be requested inside the zone (Kits plugin)

noplayerloot true/false => prevent players from looting other players or sleepers

nopve true/false => animals will be invulnerable

noremove true/false => block players from using the remover tool in here. default is set to true.

nosuicide true/false => can't write "kill" in the console, to commit suicide

nowounded true/false => when a player is supposed to die, he dies, doesn't go by the wounded state

npcfreeze true/false => freeze the NPC (animals won't move any more in the zone)

killsleepers true/false => all sleepers will be killed when they go to sleep here

radiation XX/false => add radiation to the zone

nodrown true/false => disables drowning

nostability true/false => stability is turned off

noupgrade true/false => buildings can't be upgraded

ejectsleepers true/false => sleepers will be moved out of the zone

nopickup true/false => block picking up items

nocollect true/false => block picking up collectables

(0): Commands are accessible for level 2 and for  oxide permission: "zone"

(1): Wont be ejected level 2 and players in the whitelist (only editable via outer plugins, so yeah this plugin can now be used easily by outer plugins)

(2): can build level 2 and oxide permission: "canbuild"

(3): can deploy level 2 and oxide permission: "candeploy"
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
**Example for an Admin House:**

/zone_add

/zone nobuild true nodeploy true name "Admin House" undestr true
**Configs:**

````
{

  "AutoLights": {

    "Lights Off Time": "8.0",

    "Lights On Time": "18.0"

  }

}
````


**Plugin Developpers**
**Hooks:**

````
void OnEnterZone(string ZoneID, BasePlayer player)
````

Called when a player enters a zone

no return

````
void OnExitZone(string ZoneID, BasePlayer player)
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
bool isPlayerInZone(string ZoneID, BasePlayer player)
````

returns TRUE if the player is in the zone

returns FALSE if the player is not in the zone

````
bool AddPlayerToZoneWhitelist(string ZoneID, BasePlayer player)
````

Requires: EJECT TRUE

Will allow a specific player to enter a zone

returns TRUE if everything went OK

returns FALSE if the zone doesn't exist

````
bool RemovePlayerFromZoneWhitelist(string ZoneID, BasePlayer player)
````

Requires: EJECT TRUE

Will revoke access to a zone (the player will need to get out first before being ejected)

returns TRUE if everything went OK

returns FALSE if the zone doesn't exist

````
bool AddPlayerToZoneKeepinlist(string ZoneID, BasePlayer player)
````

Will JAIL this player inside the zone, he will not be able to get out

returns TRUE if everything went OK

returns FALSE if the zone doesn't exist

(it does NOT teleport the player to the zone, so you will need to place the player inside the zone first, and if you have eject TRUE, make sure you also allow them to get inside  or something fun will happen)

````
bool RemovePlayerFromZoneKeepinlist(string ZoneID, BasePlayer player)
````

Will unJAIL a player inside the zone

returns TRUE if everything went OK

returns FALSE if the zone doesn't exist

(this does not teleport the player out or anything, just allows him to get out)


If you have any questions, or need new hooks, etc, feel free to ask.