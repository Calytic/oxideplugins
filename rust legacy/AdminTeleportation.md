Admin ONLY Teleportation System

**Commands:**

- /tp TARGETPLAYER => Teleport yourself to a target player

- /tp LOCATION => Teleport yourself to a saved location (/tpsave)

- /tp SOURCEPLAYER TARGETPLAYER => Teleport a source player to a target player

- /tp SOURCEPLAYER LOCATION => Teleport a source player to a saved location (/tpsave)

- /tp X Z => Teleport to the coordinates X & Z (Y will be automatically detected)

- /tp SOURCEPLAYER X Z => Teleport a source player to the coordinates X & Z (Y will be automatically detected)

- /tp X Y Z => Teleport yourself to the coordinates X Y Z (if Y is set to teleport under the map, you will be automatically teleported back up)

- /tp SOURCEPLAYER X Y Z => Teleport a source player to the coordinates X Y Z (if Y is set to teleport under the map, you will be automatically teleported back up)


- /bring TARGETPLAYER => Teleport a player to you.


- /tpsave => get the list of saved locations

- /tpsave remove XX => remove a saved location

- /tpsave XXX => Add a new saved location


- /tpb => This only works for yourself, when you teleport to a player or to a position, it will save your first position for you to teleport back to with this command


- /p => Portal gun, teleport to where you are looking at


- /up XX => Go up Xm, 4 is default.

- /down XX => Go down Xm, 4 is default.

- /right XX => Go right Xm, 4 is default.

- /left XX => Go left Xm, 4 is default.

- /fw XX => Go forward Xm, 4 is default.

- /back XX => Go back Xm, 4 is default.

**Permissions:

rcon.login**

oxide permission: **canteleport**

You can grant a user permission by using:
**oxide.grant user <username> <permission>**

To create a group:
**oxide.group add <groupname>**

To assign permission to a group:
**oxide.grant group <groupname> <permission>**

To add users to a group:
**oxide.usergroup add <username> <groupname>**

To remove users permission:
**oxide.revoke <userid/username> <group> <permission>**Click to expand...
**Config file: NONE**