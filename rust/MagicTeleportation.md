**What is this?**

Have you ever wanted a home system that actually makes living entities (beds etc) etc as homes? That when destroyed the the home teleport is removed? Then this is the plugin for you.


Homes are destroyed when bed (entity) is destroyed.

**Features:**

* Unique Home System (Homes are living entities).

* Public Teleport System

* Health Checks

* ...Much more.

**How do players set homes?**

Set a bed.

**Commands:**

* Player:

/t home (id), if id is left blank it will show you a list of your homes.

/t list (id) to teleport to a public teleport (if left blank, shows list)


* Admin:

/t create (Create a public teleport)

/t remove (Remove a republic teleport)

/t entity "Item" (etc /t entity "Sleeping Bag" to make sleeping bag a  home entity.) (If item param is left blank it will show a list of current entities.)

**API:**

You can use MagicTeleportation's inner workings in your own plugin to have features such as, sleep god when teleporting, health checks etc.

Example:
Code (C#):
````
[PluginReference]

Plugin MagicTeleportation;

bool success = Convert.ToBoolean(MagicTeleportation.CallHook("InitTeleport", player, x, y, z, false, true, "Bank" + count.ToString(), null, count, Convert.ToInt32(Config["Teleport", "Wait"])));
if (!success) { PrintToChat(player, "You <color=red>can't</color> teleport currently.", false); }

//InitTeleport(BasePlayer player, float init_x, float init_y, float init_z, bool type = true, bool printtoplayer = true, string title = "", string description = "", int count = -1, int seconds = 0)


 
````


**

How do I add more entities?**

They are at the bottom of the MagicTeleportation.json data file, at this moment only beds are used by default. Not sleeping bags. You can add them easily though if you want though via the data file. It will technically work with any deployable (bag type, if they add any more in future), if you add it into the datafile.


Will write more as development progresses...

This is an idea I've had for a while, there's a lot lacking in it currently so bare with me. This is the bones for hopefully something decent.

**To-do:**

Optimization, count of how many beds per building.

Rest of ideas...

**Default Configuration:**

````

{

  "Commands": {

    "CreateTeleport": "create",

    "Home": "home",

    "Main": "t",

    "Public": "list",

    "RemoveTeleport": "remove"

  },

  "Dependencies": {

    "BuildingOwners": true,

    "DeadPlayersList": true,

    "PopupNotifications": false

  },

  "General": {

    "AuthLevel": 2,

    "Protocol": 1336,

    "ShowPluginName": false

  },

  "GeneralMessages": {

    "AdminCmd": "<color=yellow>ADMIN:</color> /{command} <{createtp} | {remove} | clean>",

    "CreateTeleport": "<color=yellow>USAGE:</color> /{command} {subcommand}\n<color=red>title</color> | <color=red>description</color> | <color=yellow>sleepgod</color> (<color=green>true</color>/<color=red>false</color>) | <color=yellow>authlevel</color> | <color=yellow>enabled</color> (<color=green>true</color>/<color=red>false</color>).",

    "DBCleared": "You have <color=#FF3300>cleared</color> the Magic Homes database.",

    "HomeDestroyed": "You have <color=#FF0000>destroyed</color> your home (<color=#FFFF00>{home}</color>).",

    "HomeInfo": "[ <color=#33CCFF>{id}</color> ] <color=#FFFF00>{title}</color>, HP: <color=#FF0000>{hp}</color>.",

    "HomeNoExist": "That home does <color=red>not</color> exist. [<color=#FFFF00>{id}</color>]",

    "HomeTP": "<color=yellow>USAGE:</color> /{command} {subcommand} <id>.",

    "MaxHomes": "You have reached your maximum allowed homes. ({max_homes})",

    "NoAuthLevel": "You <color=#FF3300>do not</color> have access to this command.",

    "NoHomes": "You have <color=red>no</color> homes.",

    "NoTeleports": "There is currently <color=red>nowhere</color> to teleport to.",

    "PlayerNoAwake": "You <color=red>cannot</color> attack someone who has not woken up from teleporting.",

    "PublicTP": "<color=yellow>USAGE:</color> /{command} {subcommand} <id>.",

    "RemoveTeleport": "<color=yellow>USAGE:</color> /{command} {subcommand} <id>",

    "SetupHome": "You have setup a new home! (Use /t <home> at any time).",

    "TeleportCreated": "Created teleport <color=#FF0000>{id}</color> at your current location!\nTitle: {title} : <color=yellow>Description:</color> {description},\n<color=yellow>Sleep God:</color> {sleepgod} | <color=yellow>Auth Level:</color> {authlevel} | Enabled: <color=yellow>{enabled}</color>.",

    "TeleportInfo": "[ <color=#33CCFF>{id}</color> ] <color=yellow>Name:</color> {title}, <color=yellow>Description:</color> {description}. (<color=#33CCFF>{tpcount}</color>)",

    "TeleportInterrupted": "Your teleport has been <color=red>interrupted</color>...",

    "TeleportPending": "You already have a teleport <color=red>pending</color>.",

    "TPCooldown": "You are not currently allowed to teleport. (<color=#FF0000>{cooldown} second cooldown</color>).",

    "TPCreationFailed": "<color=yellow>ERROR:</color> Failed to create teleport.",

    "TPGeneral": "You will be teleported to {title} in <color=#FFFF00>{seconds}</color> seconds (<color=#FFFF00>{tpcount}</color>).",

    "TPHome": "You will be teleported to your home in <color=#FFFF00>{seconds}</color> seconds (<color=#FFFF00>{title}</color>).",

    "TPNoExist": "That teleport does <color=red>not</color> exist. [<color=#FFFF00>{id}</color>]",

    "TPRemoveSuccess": "<color=yellow>INFO:</color> You have removed the teleport: {id}!",

    "Usage": "<color=#33CCFF>USAGE:</color> /t <home | list>"

  },

  "HomeMessages": {

    "Alive": "You <color=#FF0000>can't</color> teleport home when you're not even <color=#FF0000>alive</color>.",

    "BuildingBlocked": "You <color=#FF0000>can't</color> teleport home when you're in a <color=#FF0000>building blocked</color> area.",

    "Failed": "<color=#FF0000>Failed</color> to teleport, contact an administrator.",

    "Fire": "You <color=#FF0000>can't</color> teleport home when you're on <color=#FF0000>fire</color>.",

    "MinHP": "Your health <color=#FF0000>needs</color> to be above <color=#FF0000>{minhp}</color> to teleport home.",

    "Swimming": "You <color=#FF0000>can't</color> teleport home when you're <color=#FF0000>swimming</color>.",

    "TooCold": "It's too <color=#FF0000>cold</color> to teleport. (<color=#00E1FF>{temperature}</color>)",

    "Wounded": "You <color=#FF0000>can't</color> teleport home when you're <color=#FF0000>wounded</color>."

  },

  "HomeSettings": {

    "BypassCold": false,

    "Cooldown": 5,

    "MaxHomes": 4,

    "MinimumHealthCheck": 30.0,

    "SanityCheck": true,

    "TPInBlockedArea": true,

    "TPWait": 8

  },

  "Settings": {

    "BypassAdmin": false,

    "EntityHeight": 2,

    "MaxEntitiesPerBuilding": 2,

    "RefundEntity": true,

    "TPSleepGod": true,

    "UpdateTimerInt": 10

  },

  "TPSettings": {

    "BypassCold": false,

    "Cooldown": 30,

    "MinimumHealthCheck": 51.0,

    "SanityCheck": true,

    "TPInBlockedArea": true,

    "TPWait": 3

  }

}

 
````

Bugs: Lots to be expected