Automatically open doors when admins get close to doors,

and close the doors when the admin gets away.



**Why this way?**

This will free the hook on open door, the plugin will ONLY be activated when an admin uses the plugin, and as the plugin only adds informations to this player, it will not affect the speed of the server.

So yeah you wont choose what doors to open, but they will all open, and it will save some speed for the server ^^ 
It's also optimized to just detect doors, so it won't be using any resources even if 10 admins uses it at the same time 

**Command:**

- /admindoor => activate/deactivate the auto door.

**Permissions:

rcon.login**

oxide permission "**candoor**"

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