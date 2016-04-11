**OPTIONAL BUT HIGHLY RECOMMENDED: **
**[Player Database](http://oxidemod.org/plugins/player-database.927/) 1.0.2+**


**Commands:**

- /house PLAYERNAME => Find all houses of all players that match partially this name (Requires PlayerDatabase)

- /house STEAMID => Find all houses of a specific steamid (doesn't require PlayerDatabase)

- /housetp XX => teleport to a houseID (got from /house)

**Permissions:**

rcon.login

oxide permission: canlocate

oxide permission: all

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
**Tutorial**: