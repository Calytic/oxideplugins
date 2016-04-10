**Permissions:**

giveitem.use

**Using Permissions:**

As an example, to grant the permission to a user:

/grant user Player giveitem.use


To grant the permission to all admins (if you have an admin group) you can use:

/grant group admin giveitem.use

**Commands:**

/giveitem Player ItemID Amount

/giveall ItemID Amount

/itemid Name

**Usage:**

Example: /giveitem Noviets 19 100

Result: Would give Noviets 100 Stones


Example: /giveall 19 100

Result: Would give Everyone 100 Stones

**Editing:**

If you need/want to edit the messages, you can do so in the oxide/lang/GiveItem json file.

**Notes: **

A full list of Item's and associated ItemIDs can be found in the FAQ section, or you can use /itemid Name in game.


Partial Player names are accepted. So those with special characters are no long a pain. Still try to write as much of their name as you can to avoid multiple matches.