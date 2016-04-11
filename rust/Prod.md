Compatible with: **[DeadPlayerList](http://oxidemod.org/resources/deadplayerlist.696/) & **Building Owners****

**Features:**

- Get Owners **Steams ID & Names**

- **Building Blocks**

- **Tool Cupboard **whitelist

- whitelist of **CodeLocks from Boxes**

- **code numbers** of **CodeLocks**

- Deployers of **Deployables**

- Fully configurable

**Command:**
- /prod => will show you all the owners of stuff that you see in front of you

**Config-file:**

````

{

  "Messages": {

    "boxNeedsCode": "Can't find owners of an item without a Code Lock",

    "Code": "Code is: {0}",

    "codeLockList": "CodeLock whitelist:",

    "helpProd": "/prod on a building or tool cupboard to know who owns it.",

    "noAccess": "You don't have access to this command",

    "noBlockOwnerfound": "No owner found for this building block",

    "noCodeAccess": "No players has access to this Lock",

    "noCupboardPlayers": "No players has access to this cupboard",

    "noTargetfound": "You must look at a tool cupboard or building",

    "Toolcupboard": "Tool Cupboard"

  },

  "Plugin Dev": {

    "Are you are plugin dev?": false,

    "Dump all components of all entities that you are looking at? (false will do only the closest one)": false

  },

  "Prod": {

    "authLevel": 1

  }

}

 
````