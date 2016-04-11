**
Features:**

- **Player** remove

- **Refund** option

- **Pay **option

- **Admin** remove & remove **all**

- Use **Tool Cupboard** or/and **Building Owners**

- Supports **RustIO **Friends **if using Building Owners**

- **GUI**

-** Raid Blocker** (blocks remove while a raid is undergoing around your position, needs to be **explosives **that destroy a **structure**)

**Chat Commands:**
- /remove admin optional:TIME => Activate Remove Admin Tool
- /remove all optional:TIME => will remove an entire building with all it's deployables
- /remove optional:TIME => this will work if you choose

- /remove target TargetPlayer optional:TIME => give remove to a player

**Console Commands:**
remove.give PLAYER optional:time

remove.allow false/true => overrides the remover tool for players, they will not be able to use the remover tool if you set it to allow false. this is NOT saved after a server restart or plugin restart!!! This is for use with **Timed Executed **if you want your server to have the remover tool only during a certain period of time. If you don't want players to ever have access to the remover tool, just set it in the configs.

**Configs:**

````

{

  "GUI - Position - X Max": "0.4",

  "GUI - Position - X Min": "0.1",

  "GUI - Position - Y Max": "0.90",

  "GUI - Position - Y Min": "0.65",

  "Message - Admin Override Disabled the Remover Tool": "The remover tool was disabled for the time being.",

  "Message - Cant Use Remove With Item": "You can't use the remover tool while you have an active item",

  "Message - External Plugin Blocking Remove": "You are not allowed use the remover tool at the moment",

  "Message - Multiple Players Found": "Multiple players found",

  "Message - No Players Found": "No players found",

  "Message - No Rights To Remove This": "You have no rights to remove this",

  "Message - Not Allowed": "You are not allowed to use this command",

  "Message - Not Enough To Pay": "You don't have enough to pay for this remove",

  "Message - Nothing To Remove": "Couldn't find anything to remove. Are you close enough?",

  "Message - Raid Blocked": "RaidBlocker: You need to wait for {0}s before being allowed to remove again",

  "Message - Remover Tool Ended": "{0}: Remover Tool Deactivated",

  "Message - Target Remover Tool Ended": "The Remover Tool for {0} has ended",

  "Remove - Access - Use Building Owners": true,

  "Remove - Access - Use RustIO & BuildingOwners (Building Owners needs to be true)": true,

  "Remove - Access - Use ToolCupboards": true,

  "Remove - Auth - AuthLevel - Admin Commands": 1,

  "Remove - Auth - AuthLevel - Normal Remove": 0,

  "Remove - Auth - Permission - Admin Remove": "canremoveadmin",

  "Remove - Auth - Permission - All Remove": "canremoveall",

  "Remove - Auth - Permission - Normal Remove": "canremove",

  "Remove - Auth - Permission - Target Remove": "canremovetarget",

  "Remove - Default Time": 30,

  "Remove - Distance - Admin": 20,

  "Remove - Distance - All": 300,

  "Remove - Distance - Player": 3,

  "Remove - Max Remove Time": 120,

  "Remove - Pay": true,

  "Remove - Pay - Costs": {

    "0": {

      "wood": "1"

    },

    "1": {

      "wood": "100"

    },

    "2": {

      "stone": "150",

      "wood": "100"

    },

    "3": {

      "metal fragments": "75",

      "stone": "50",

      "wood": "100"

    },

    "4": {

      "high quality metal": "25",

      "metal fragments": "75",

      "stone": "350",

      "wood": "250"

    },

    "deployable": {

      "wood": "50"

    }

  },

  "Remove - Pay - Deployables": true,

  "Remove - Pay - Structures": true,

  "Remove - RaidBlocker": true,

  "Remove - RaidBlocker - Radius To Block": 80,

  "Remove - RaidBlocker - Time To Block": 300,

  "Remove - Refund": true,

  "Remove - Refund - Deployables": true,

  "Remove - Refund - Percentage (Structures Only)": {

    "0": "100.0",

    "1": "80.0",

    "2": "60.0",

    "3": "40.0",

    "4": "20.0"

  },

  "Remove - Refund - Structures": true

}

 
````


**How Basic Remove Works:**

1 - Checks if you are the owner of the building (via Building Owners) if it does find you => remove ok

2 - If you allowed the Tool Cupboard it will look if you have Access to ALL the tool cupboards in range!!! If you have access to 10 of them, but 1 of them you dont => NO REMOVE, but if you have access to all of them => it will check if you are in range (max 3m) to allow the remove