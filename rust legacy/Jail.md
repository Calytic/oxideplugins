REQUIRES:
[Spawns Database](http://oxidemod.org/plugins/spawns-database.952/) 1.0.0
[Zone Manager](http://oxidemod.org/plugins/zone-manager.943/) 1.0.0

Features:

- Send players to jail

- Free players from jail

- Timed Jail option

- Configure the spawn points

- Inmates can't leave the jail zone radius (they will be automatically teleported back)

- Players can't enter the jail zone radius (they will be automatically teleported away)

- Admins can enter the jail zone radius

- Inmates can't do any damage

- Inmates can die from anything (exept each other)

- Inmates will be teleported back in the jail on death

- Inmates will be teleported back in the jail on reconnection (if they aren't in the jail)

- Inmates can't request Kits

- Inmates can't Teleport out (or in) (works with SetHome & Teleportation Requests)

- Inmates can't build / deploy

- Jail is undestructable

- Inmates have 1 spawn point (they will always spawn back there if they die), but can be shared with others as the spawn points are randomly chosen.

Oxide Permissions: "canjail"
You can grant a user permission by using:
oxide.grant user <username> <permission>

To create a group:
oxide.group add <groupname>

To assign permission to a group:
oxide.grant group <groupname> <permission>

To add users to a group:
oxide.usergroup add <username> <groupname>

To remove users permission:
oxide.revoke <userid/username> <group> <permission>
Click to expand...Commands:

- /jail PLAYER optional:XXX => send a player to jail for XX amount of time

- /free PLAYER => free a player from jail

- /jail_config spawnfile NAME => select the spawnfile where players will be freed at (you can set 1 spawnpoint or billions)

- /jail_config zone RADIUS => set the zone center (your position) and zone radius of the jail.

Config file:

Jail.json

````
{

  "Message: Freed": "You were freed from jail",

  "Message: Jail Created": "You successfully created/updated the jail zone, use /zone_list for more informations",

  "Message: KeepIn": "You are not allowed to leave the Jail",

  "Message: KeepOut": "Keep out, no visitors allowed in the jail",

  "Message: Loaded Cells": "Jail Plugin: {0} cell spawns were detected and loaded",

  "Message: No Permission": "You don't have the permission to use this command",

  "Message: No Player Found": "No Online player with this name was found",

  "Message: No SpawnData": "No spawns set or no spawns database found http://forum.rustoxide.com/resources/spawns-database.720",

  "Message: No SpawnDatabase": "No spawns set or no spawns database found http://forum.rustoxide.com/resources/spawns-database.720",

  "Message: No SpawnFile": "No SpawnFile - You must configure your spawnfile first: /jail_config spawnfile FILENAME",

  "Message: No ZoneManager": "You can't use the Jail plugin without ZoneManager",

  "Message: Sent In Jail": "You were arrested and sent to jail",

  "Message: Welcome ADMIN": "Welcome to the jail {0}",

  "spawnfile": "jailinmate"

}
````