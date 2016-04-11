**HeliControl** is a plugin that allows you to tweak various settings of Helicopters on your server.




Chat Commands:
**/callheli 

/callheli <playername>

/callheli <playername> <amount of heli' to call>

/killheli

/killheli forced

/killgibs

/killfireballs

/getshortname <item name (doesn't need quotes)> 

/strafe **- Tells the heli to strafe your position.
**/strafe <playername> **- Tells the heli to strafe the target's position.
**/helidest <playername>** - Tells the heli to start flying to this player's position.
**/updatehelis **- Updated all Helicopters to the current config's settings. Please note that you still need to reload the plugin after any config changes, **then** run this command.



Server Console Commands: (no slash)
**callheli

callheli <playername>
**callheli <playername> <amount of heli' to call>**

killheli
**getshortname <item name (doesn't need quotes)> ****

**getshortname **will give you the name of the item to use when setting up custom loot spawns. You can find the spawns file in the /oxide/data directory. More info on lootspawns below.

**killheli **kills ALL helicopters instantly. They will not drop any box loot.


Config settings with description:

````

UseCustomLoot - enable/disable custom loot in boxes dropped by helicopters.

DisableHeli - set to true to disable helicopters all together.

UseGlobalDamageModifier - use only one multiplier for all weapons (no need to change individual weapons if using global modifier)

GlobalDamageMultiplier - all damage will by multiplied by this number if UseGlobalDamageModifier is true. (can be fraction to make damage less ex: 0.5 will only do half damage to helicopters)

HeliBulletDamageAmount - damage dealt by each bullet from the helicopter to the player. Setting to zero will deal no damage, but helicopters still shoot at players. (default is 20)

MaxLootCratesToDrop - lets helicopters drop more than the default 4 loot crates

ModifyDamageToHeli - You can decide not to modify damage to helicopters by changing to false.

HeliSpeed - Changes the speed of the helicopter, making it too fast can make it look very choppy and hard to kill (if it moves faster than bullets) (default = 25.0) (100 =  too much for me)

HeliAccuracy - Changes the accuracy of the helicopter when shooting at players. A lower number means more accuracy. Changes make a drastic difference. This value is basically a radius that the helicopter's bullets will land around you. Changing this to a number close to zero makes things interesting and difficult. (default = 2.0)

BaseHealth - health of the main helicopter's base. (default 10000)

MainRotorHealth - health of the main rotor (weak point) (default 750)

TailRotorHealth - health of the tail rotor (weak point) (default 375)

MaxHeliRockets - determines the number of rockets the heli shoots on one attack pass. Set to 0 to disable rockets. (default 12)

TimeBetweenRockets - this is the time (in seconds) between each rocket, when the heli is strafing. Default is 0.2. (200 ms)

DisableGibs - this option toggles whether or not the Helicopter has any harvestable parts after it is destroyed.

DisableNapalm - this options toggles whether or not the Helicopter can drop napalm with it's Rockets.

LockedCrates - this option toggles whether or not crates that drop from the Helicopter are locked. (This automatically destroys the fire surrounding the crates.)

TimeBeforeUnlockingCrates - this option sets how long (in seconds) before the crates are unlocked, currently, however, you can only make it shorter, rather than a longer duration. LockedCrates must be set to false for this to have any effect.

BulletSpeed - how fast the bullet travels from the Helicopter to the target.

TurretBurstLength - how long a "burst" of bullets from the turret lasts. Default is 3.0

TurretFireRate - time between each bullet from the helicopter's turrets. Lower is faster. Default is 0.125.

TurretMaxTargetRange - How far away a turret can target a player.

TurretTimeBetweenBursts - How much time in-between the helicopter's turrets' bursts.

 
````


Permissions are as follows:

helicontrol.callheli (gives you access /callheli)

helicontrol.killheli (gives you access /killheli)

helicontrol.killgibs (gives you access to /killgibs)

helicontrol.killfireballs (gives you access to /killfireballs)

helicontrol.strafe (gives you access to /strafe)

helicontrol.update (gives you access to /updatehelis)

helicontrol.destination (gives you access to /helidest)

helicontrol.admin (gives you permission to everything)


Good guide on permissions:
[Using Oxide's permission system | Oxide](http://oxidemod.org/threads/using-oxides-permission-system.8296/)


P.S, it is worth noting that by default, **HeliControl should not change any of the Helicopter's behavior**, and will only add chat/console commands.



This config file will be automatically generated and located in /oxide/config/HeliControl.json

````

{

  "BaseHealth": 10000.0,

  "BulletSpeed": 250.0,

  "DisableGibs": false,

  "DisableHeli": false,

  "DisableNapalm": false,

  "GibsTooHotLength": 480.0,

  "GlobalDamageMultiplier": 1.0,

  "HeliAccuracy": 2.0,

  "HeliBulletDamageAmount": 20.0,

  "HeliSpeed": 25.0,

  "LifeTimeMinutes": 15,

  "LockedCrates": true,

  "MainRotorHealth": 750.0,

  "MaxHeliRockets": 12,

  "MaxLootCratesToDrop": 4,

  "ModifyDamageToHeli": false,

  "TailRotorHealth": 375.0,

  "TimeBeforeUnlockingCrates": 0.0,

  "TimeBetweenRockets": 0.2,

  "TurretBurstLength": 3.0,

  "TurretFireRate": 0.125,

  "TurretMaxTargetRange": 300.0,

  "TurretTimeBetweenBursts": 3.0,

  "UseCustomLoot": true,

  "UseGlobalDamageModifier": false

}

 
````



**Spawn System:**

There is no weight system as of yet, so if you want one lootbox to be rarer, add more of the others, by copying and pasting. Duplicates are definitely okay.
**I made a list of a bunch of the default loot drops in a paste here:**
[[JSON] HeliControlData - Pastebin.com](http://pastebin.com/kMb1N857)


Example HeliControlData.json - located in /oxide/data/HeliControlData.json:

Use [http://www.jsonlint.com/](http://jsonlint.com/) to validate your JSON

Use the command getshortname <item name> to get the names used in this data file.

````

{

  "HeliInventoryLists": [

    {

      "lootBoxContents": [

        {

          "name": "rifle.ak",

          "amount": 1,

          "isBP": false

        },

        {

          "name": "ammo.rifle.hv",

          "amount": 128,

          "isBP": false

        }

      ]

    },

    {

      "lootBoxContents": [

        {

          "name": "rifle.bolt",

          "amount": 1,

          "isBP": false

        },

        {

          "name": "ammo.rifle.hv",

          "amount": 128,

          "isBP": false

        }

      ]

    },

    {

      "lootBoxContents": [

        {

          "name": "explosive.timed",

          "amount": 3,

          "isBP": false

        },

        {

          "name": "ammo.rocket.hv",

          "amount": 3,

          "isBP": false

        }

      ]

    },

    {

      "lootBoxContents": [

        {

          "name": "lmg.m249",

          "amount": 1,

          "isBP": false

        },

        {

          "name": "ammo.rifle",

          "amount": 100,

          "isBP": false

        }

      ]

    }

  ]

}

 
````


Default HeliControl weapons data file (oxide\data\HeliControlWeapons.json):

````

{

  "WeaponList": {

    "Assault Rifle": 1.0,

    "Bolt Action Rifle": 1.0,

    "Hunting Bow": 1.0,

    "Crossbow": 1.0,

    "M249": 1.0,

    "Eoka Pistol": 1.0,

    "Revolver": 1.0,

    "Semi-Automatic Pistol": 1.0,

    "Semi-Automatic Rifle": 1.0,

    "Pump Shotgun": 1.0,

    "Waterpipe Shotgun": 1.0,

    "Custom SMG": 1.0,

    "Thompson": 1.0

  }

}

 
````

Default HeliControl localization file (oxide/lang/HeliControl.en.json):

````

{

  "noPerms": "You do not have permission to use this command!",

  "invalidSyntax": "Invalid Syntax, usage example: {0} {1}",

  "invalidSyntaxMultiple": "Invalid Syntax, usage example: {0} {1} or {2} {3}",

  "heliCalled": "Helicopter Inbound!",

  "helisCalledPlayer": "{0} Helicopter(s) called on: {1}",

  "entityDestroyed": "{0} {1}(s) were annihilated!",

  "helisForceDestroyed": "{0} Helicopter(s) were forcefully destroyed!",

  "heliAutoDestroyed": "Helicopter auto-destroyed because config has it disabled!",

  "playerNotFound": "Could not find player: {0}",

  "noHelisFound": "No active helicopters were found!",

  "cannotBeCalled": "This can only be called on a single Helicopter, there are: {0} active.",

  "strafingYourPosition": "Helicopter is now strafing your position.",

  "strafingOtherPosition": "Helicopter is now strafing {0}'s position.",

  "destinationYourPosition": "Helicopter's destination has been set to your position.",

  "destinationOtherPosition": "Helicopter's destination has been set to {0}'s position.",

  "IDnotFound": "Could not find player by ID: {0}",

  "updatedHelis": "{0} helicopters were updated successfully!",

  "itemNotFound": "Item not found!"

}

 
````