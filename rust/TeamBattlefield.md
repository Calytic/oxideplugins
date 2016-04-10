This plugin was originally created by [@BodyweightEnergy](http://oxidemod.org/members/72939/)


Similar to the current game styles on Battlefield-type servers, but with teams:


* Endless, not Event-based, no scores kept (for now)
* Two teams only
* Players can choose their team upon startup, with a maximum team difference before auto-assigning players
* Individual kit types for each team
* Team-specific wearable items
* Friendly-fire damage is configurable (default is 0.0)
* GUI Team selection
* Player stats save on disconnect so they can reconnect and continue from where they left off, with a automatic wipe cycle for sleeping players (default 5 mins)
* Should be compatible with all other plugins (except those that modify damage)
* Spawn Database integration
* Spectator mode (GUI Selection)
* Admin mode that only admins can see (GUI Selection)
* Scoreboard GUI
* Plugin based death notifications


**Console Commands:**
**tbf.assign** - <partial player name> <"a"/"b"/"spectator">      (example: tbf.assign k1lly0u a)
**tbf.purge** - Removes inactive sleepers saved data
**tbf.list** - Shows what teams players are on, and when they last disconnected
**tbf.version** - Prints plugin version for debugging purposes
**tbf.clearscore** - Resets the scoreboard

**Chat Commands:**
**/switchteam** - Opens the team selection GUI and allows player to change teams/spectate

**In-plugin Chat:**

This plugin has a inbuilt chat and death notification system. If you do not wish to use these you may disable them in the config!
** Using other chat based handlers can result in plugin malfunction **

**Spawns Database Integration:**

You must create 2 spawnfiles, one for each team. Failure to do so will result in the plugin turning itself off.

Use SpawnsDatabase to create the spawnfile and set the file names in the config

Spawn file names are adjustable in the config, default names are;


* For Team A, save the spawn file as "team_a_spawns"
* For Team A, save the spawn file as "team_b_spawns"


**Config**


* AdminGear, TeamAGear and TeamBGear are the individual gear lists for each team
* CommonGear and StartingWeapons is the gear that everyone gets

* MaximumTeamCountDifference is the maximum amount of player one team can have over the other before new players will be auto assigned
* RemoveSleepers_Timer is the amount of time (in minutes) before a players stored kills and team is removed after disconnecting
* FF_DamageScale is the amount to scale damage too between teammates (0.0 is nothing, 1.0 is normal)
* Admin, TeamA and TeamB_Chat_Color changes the players chat color
* Admin, TeamA and TeamB_Chat_Prefix changes the team tag before the player name

* UsePluginChatControl enables and disables the in-plugin chat system
* BroadcastDeath enables and disables the in-plugin death notification system


````

{

  "Admin_Chat_Color": "<color=#00ff04>",

  "Admin_Chat_Prefix": "[Admin] ",

  "BroadcastDeath": true,

  "FF_DamageScale": 0.0,

  "MaximumTeamCountDifference": 4,

  "RemoveSleeper_Timer": 5,

  "Spectator_Chat_Color": "<color=white>",

  "Spectator_Chat_Prefix": "[Spectator] ",

  "TeamA_Chat_Color": "<color=#0066ff>",

  "TeamA_Chat_Prefix": "[Team A] ",

  "TeamA_Spawnfile": "team_a_spawns",

  "TeamB_Chat_Color": "<color=#ff0000>",

  "TeamB_Chat_Prefix": "[Team B] ",

  "TeamB_Spawnfile": "team_b_spawns",

  "UsePluginChatControl": true,

  "z_Admin_Gear": [

    {

      "amount": 1,

      "container": "wear",

      "name": "Hoodie",

      "shortname": "hoodie",

      "skin": 10129

    },

    {

      "amount": 1,

      "container": "wear",

      "name": "Pants",

      "shortname": "pants",

      "skin": 10078

    },

    {

      "amount": 1,

      "container": "wear",

      "name": "Gloves",

      "shortname": "burlap.gloves",

      "skin": 10128

    },

    {

      "amount": 1,

      "container": "wear",

      "name": "Boots",

      "shortname": "shoes.boots",

      "skin": 10023

    }

  ],

  "z_CommonGear": [

    {

      "amount": 1,

      "container": "belt",

      "name": "Machete",

      "shortname": "machete",

      "skin": 0

    },

    {

      "amount": 2,

      "container": "belt",

      "name": "Medical Syringe",

      "shortname": "syringe.medical",

      "skin": 0

    },

    {

      "amount": 1,

      "container": "belt",

      "name": "Bandage",

      "shortname": "bandage",

      "skin": 0

    },

    {

      "amount": 1,

      "container": "belt",

      "name": "Paper Map",

      "shortname": "map",

      "skin": 0

    },

    {

      "amount": 1,

      "container": "wear",

      "name": "Metal ChestPlate",

      "shortname": "metal.plate.torso",

      "skin": 0

    }

  ],

  "z_StartingWeapons": [

    {

      "ammo": 120,

      "ammoType": "ammo.rifle.hv",

      "amount": 1,

      "container": "belt",

      "contents": [

        "weapon.mod.holosight"

      ],

      "name": "AssaultRifle",

      "shortname": "rifle.ak",

      "skin": 0

    },

    {

      "ammo": 120,

      "ammoType": "ammo.pistol.hv",

      "amount": 1,

      "container": "belt",

      "contents": [

        "weapon.mod.silencer"

      ],

      "name": "SemiAutoPistol",

      "shortname": "pistol.semiauto",

      "skin": 0

    }

  ],

  "z_TeamA_Gear": [

    {

      "amount": 1,

      "container": "wear",

      "name": "Hoodie",

      "shortname": "hoodie",

      "skin": 14178

    },

    {

      "amount": 1,

      "container": "wear",

      "name": "Pants",

      "shortname": "pants",

      "skin": 10020

    },

    {

      "amount": 1,

      "container": "wear",

      "name": "Gloves",

      "shortname": "burlap.gloves",

      "skin": 10128

    },

    {

      "amount": 1,

      "container": "wear",

      "name": "Boots",

      "shortname": "shoes.boots",

      "skin": 10023

    }

  ],

  "z_TeamB_Gear": [

    {

      "amount": 1,

      "container": "wear",

      "name": "Hoodie",

      "shortname": "hoodie",

      "skin": 0

    },

    {

      "amount": 1,

      "container": "wear",

      "name": "Pants",

      "shortname": "pants",

      "skin": 10019

    },

    {

      "amount": 1,

      "container": "wear",

      "name": "Gloves",

      "shortname": "burlap.gloves",

      "skin": 10128

    },

    {

      "amount": 1,

      "container": "wear",

      "name": "Boots",

      "shortname": "shoes.boots",

      "skin": 10023

    }

  ]

}

 
````



**(For Developers) Cross-Plugin Functions:**


* Code (C#):
````
string GetPlayerTeams(ulong playerID) // Returns the players team (A,B,SPECTATOR,ADMIN)
````


* Code (C#):
````
Dictionary<ulong, string> GetTeams() // Returns a dictionary with player ID's and their corresponding team names
````




**Known Incompatible Plugins:**


* (confirmed) HeliController (it uses a global damage modifier, disables friendly fire functions)