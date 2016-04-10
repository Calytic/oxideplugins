**Permissions:**

admintools.kick

admintools.ban

admintools.tempban

admintools.godmode

admintools.mute

admintools.freeze

admintools.all

**Using Permissions:**

As an example, to grant the permission to a user:

/grant user Player admintools.kick


To grant the permission to all admins (if you have an admin group) you can use:

/grant group admin admintools.kick


To grant permission to all commands you can use: admintools.all

**Usage:

/**godmode on|off

/kick Player Reason here

/ban Player

/tempban Player Duration (in minutes)

/checktempban Player

/removetempban Player

/unban SteamID

/mute Player Time(in seconds)

/unmute Player

/freeze Player

/unfreeze Player

/heal

/heal Player
**

Usage Note:**

Player Name, SteamID, or IP Address are accepted for all commands. (Except /unban)

SteamID and IP must match exactly.

**Messages:**

If you need/want to edit the messages, you can do so in the oxide/lang/AdminTools.en.json file.

**Config File:

````

{

  "KillingInGodBan": false,

  "KillingInGodKick": false,

  "ShowConsoleMsg": false

}

 
````

Notes:**

Players that are muted will automatically increase their mute length by 20 seconds whenever they attempt to send messages while muted.


Partial Player names are accepted. So those with special characters are no long a pain. Still try to write as much of their name as you can to avoid multiple matches.