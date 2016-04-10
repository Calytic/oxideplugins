**Chat Commands:**

- /airdrop => call 1 random airdrop

- /airdrop NUMBER => call X random airdrops

- /airdrop PLAYERNAME/STEAMID => call 1 airdrop to the target player

- /airdrop X Z => call 1 airdrop to the target position

- /airdrop X Y Z => call 1 airdrop to the target position

- /airdrop cancel => destroy all planes to cancel all current airdrop calls

- /airdrop destroy => destroy all supply crates.

**Permissions:

rcon.login**

oxide permission: "**all**"

oxide permission: "**canairdrop**"

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
**Configs AdminAirdrop.json**

````
{

  "Messages: Airdrop on player position": "Airdrop called @ {0} - {1}'s position",

  "Messages: Airdrop on position": "Airdrop called @ {0}",

  "Messages: Called 1 airdrop": "Called airdrop...",

  "Messages: Cancelled the airdrop calls": "{0} Airdrops cancelled...",

  "Messages: Destroyed all crates": "{0} Supply crates destroyed...",

  "Messages: Mass airdrop call": "Calling {0} airdrops ...",

  "Messages: Not Allowed": "You are not allowed to use this command.",

  "Messages: Wrong Arguments": "Wrong arguments, or target player doesn't exist"

}
````