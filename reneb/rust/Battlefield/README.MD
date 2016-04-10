**Battlefield FREE FOR ALL!**

This plugin can be used alone with Event Manager, or with any other plugins.

With Event Manager you can set auto events and make it that the battlefield lasts for 30mins, then goes to another event type (before coming back again to this event at some point)

**Special Player Commands:**

- /ground XX => vote for a new battlefield ground

- /weapon XX => vote for a new weapon kit

**Special Admin Commands:**

- /ground XX => Set a new battlefield ground

- /weapon XX => Set a new weapon kit

**Creating Grounds:**

1) Create 1 ground or more

2) You will need to create your zones on your own, as i can't handle multiple zones per plugin at the moment (wasn't made like that ...)

So i recommend knowing how to use Zone Manager!!

3) in Battlefield - Grounds, use the same way i used to create the longrange ground and shortrange ground. You will need "spawnfile" and "kits".

spawnfile is where all the spawns are stored for the battle ground

kits are all the kits that you will allow the players to use (/vote for). All players will have the SAME kits!

I recommend using: [JSONLint - The JSON Validator.](http://jsonlint.com/) to make sure you didn't fuck up the configs

**Zone Management:**

In the grounds list you must have a "zone" set to the respective battlefield zone.

Exemple for the shortrange one:

- /zone_add  BattlefieldGround1

- /zone_edit BattlefieldGround1

- /zone radius 100 undestr true nobuild true nodeploy true nocorpse true nowounded true autolights true

Of course the zone can be named as you wish as long as the names match in the config and zone.


Default Config:

````

{

  "Battlefield - Default Ground": "shortrange",

  "Battlefield - Default Weapon Kit": "assault",

  "Battlefield - Event - Name": "Battlefield",

  "Battlefield - Grounds": {

    "longrange": {

      "kits": [

        "sniper",

        "pistols"

      ],

      "spawnfile": "longrangespawnfile",

      "zone": "BattlefieldGround2"

    },

    "shortrange": {

      "kits": [

        "pistols",

        "assault"

      ],

      "spawnfile": "shortrangespawnfile",

      "zone": "BattlefieldGround1"

    }

  },

  "Battlefield - Start - Health": 100.0,

  "Battlefield - Vote - % needed to win": 60,

  "Messages - Error - Not joined": "You must be in the battlefield to vote",

  "Messages - Error - Not Selected": "Battlefield isn't currently launched",

  "Messages - Error - Not Started": "You need to wait for the Battlefield to be started to use this command.",

  "Messages - Error - Vote - Ground Already This One": "The current ground is already {0}.",

  "Messages - Error - Vote - Ground Doesnt Exist": "This battlefield ground doesn't exist.",

  "Messages - Error - Vote - Weapon Already This One": "The current weapon is already {0}.",

  "Messages - Error - Vote - Weapon Doesnt Exist": "This weapon kits doesn't exist in this battleground.",

  "Messages - Kill": "{0} killed {2}. ({1} kills)",

  "Messages - Open Broadcast": "In Battlefield, it's a free for all, the goal is to kill as many players as possible!",

  "Messages - Vote - Grounds - New": "︻┳═一 New ground is now: {0}",

  "Messages - Vote - Grounds - Show Avaible List": "{0} - {1} votes",

  "Messages - Vote - Grounds - Show Avaible Title": "︻┳═一 Battlefield Grounds Avaible 一═┳︻ Votes required for an item: {0}",

  "Messages - Vote - Grounds - Show Votes": "┳═一 {0} has {1} votes",

  "Messages - Vote - Grounds - Voted": "You have voted for the ground: {0}.",

  "Messages - Vote - Weapons - New": "︻┳═一 New weapon kit is now: {0}",

  "Messages - Vote - Weapons - Show Avaible List": "{0} - {1} votes",

  "Messages - Vote - Weapons - Show Avaible Title": "︻┳═一 Avaible Weapon Kits For Current Ground 一═┳︻  Votes required for an item: {0}",

  "Messages - Vote - Weapons - Show Votes": "┳═一 {0} has {1} votes",

  "Messages - Vote - Weapons - Voted": "︻┳═一 You have voted for the weapon: {0}.",

  "Tokens - Per Kill": 1

}

 
````

This is the first version, i will add console commands later on.

And fixes also ^^