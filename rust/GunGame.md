The rules of GunGame are simple. Each kill you get will advance you 1 rank, each rank comes with a new weapon. The weapons get harder to kill with the higher you go. The first person to get a kill on rank 15(default) wins. Kills with a machete will lower the victim's rank. Reward points are issued to the winner (see event manager reward system)

**Setup for GunGame**

* Create your arena
* Create a Zone around your Arena (Zone Manager)
* Create a spawn file (Spawns)
* Start a game (Event Manager)
**Game Config Options**


"Use Armour" - Give players a Metal Chestplate (default true)

  "Use Machete" - Kills using the Machete will lower the victims rank (default true)

  "Use Meds" - Give players some Medical Syringes (default true)

"Meds Amount" - The amount of meds the players recieve each spawn (default 1)

"Rank Limit" - Change the maximum amount of ranks, you can set this to as many entries as you have for ranks in the config

"Weapons" - This is your rank/weapon entry.

"Meds" - Select what medical supplies go into the medical kit

"PlayerGear" - This is the attire the player will be given

"DowngradeWeapon" - This is the weapon that will lower a victims rank (default: machete)

**Chat Commands**
/gg rank <rank##> <opt:ammo amount> - Sets a new weapon for the rank specified.

-- ex. /gg rank 10 120 - Will set the current weapon in my hands to rank 10 with 120 ammo

** To use this command you must have a weapon ready and in your hands.

** It will save the ammo type, mods and skin.

** You can add extra ranks using this method

<rank##> is the rank number you want to assign the weapon to.

<opt:ammo amount> is the amount of ammo you want to give with the weapon (if the weapon needs ammo)

/gg kit - Will copy your current inventory and save it as the kit the player spawns in with

** This will only save attire and medical items. There is no need to save anything else

**Configuration**

````

{

  "ArmourType": "metal.plate.torso",

  "CloseEventAtStart": true,

  "DowngradeWeapon": {

    "amount": 1,

    "container": "belt",

    "name": "Machete",

    "shortname": "machete",

    "skin": 0

  },

  "EventName": "GunGame",

  "Meds": [

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

    }

  ],

  "PlayerGear": [

    {

      "amount": 1,

      "container": "wear",

      "name": "Boots",

      "shortname": "shoes.boots",

      "skin": 0

    },

    {

      "amount": 1,

      "container": "wear",

      "name": "Hide Pants",

      "shortname": "attire.hide.pants",

      "skin": 0

    },

    {

      "amount": 1,

      "container": "wear",

      "name": "Bone Armour Pants",

      "shortname": "bone.armor.pants",

      "skin": 0

    },

    {

      "amount": 1,

      "container": "wear",

      "name": "Riot Helmet",

      "shortname": "riot.helmet",

      "skin": 0

    }

  ],

  "RankLimit": 15,

  "SpawnFile": "ggspawnfile",

  "StartHealth": 100.0,

  "TokensOnWin": 5,

  "TokensPerKill": 1,

  "UseArmour": true,

  "UseMachete": true,

  "UseMeds": true,

  "Weapons": {

    "1": {

      "ammo": 120,

      "ammoType": "ammo.rifle",

      "amount": 1,

      "container": "belt",

      "contents": [

        "weapon.mod.holosight"

      ],

      "name": "AssaultRifle",

      "shortname": "rifle.ak",

      "skin": 0

    },

    "10": {

      "ammo": 40,

      "ammoType": "arrow.hv",

      "amount": 1,

      "container": "belt",

      "contents": [],

      "name": "HuntingBow",

      "shortname": "bow.hunting",

      "skin": 0

    },

    "11": {

      "ammo": 40,

      "ammoType": "ammo.handmade.shell",

      "amount": 1,

      "container": "belt",

      "contents": [],

      "name": "EokaPistol",

      "shortname": "pistol.eoka",

      "skin": 0

    },

    "12": {

      "ammo": 0,

      "ammoType": null,

      "amount": 2,

      "container": "belt",

      "contents": [],

      "name": "StoneSpear",

      "shortname": "spear.stone",

      "skin": 0

    },

    "13": {

      "ammo": 0,

      "ammoType": null,

      "amount": 2,

      "container": "belt",

      "contents": [],

      "name": "SalvagedCleaver",

      "shortname": "salvaged.cleaver",

      "skin": 0

    },

    "14": {

      "ammo": 0,

      "ammoType": null,

      "amount": 2,

      "container": "belt",

      "contents": [],

      "name": "Mace",

      "shortname": "mace",

      "skin": 0

    },

    "15": {

      "ammo": 0,

      "ammoType": null,

      "amount": 2,

      "container": "belt",

      "contents": [],

      "name": "BoneClub",

      "shortname": "bone.club",

      "skin": 0

    },

    "16": {

      "ammo": 0,

      "ammoType": null,

      "amount": 1,

      "container": "belt",

      "contents": [],

      "name": "BoneKnife",

      "shortname": "knife.bone",

      "skin": 0

    },

    "17": {

      "ammo": 0,

      "ammoType": null,

      "amount": 1,

      "container": "belt",

      "contents": [],

      "name": "LongSword",

      "shortname": "longsword",

      "skin": 0

    },

    "18": {

      "ammo": 0,

      "ammoType": null,

      "amount": 1,

      "container": "belt",

      "contents": [],

      "name": "SalvagedSword",

      "shortname": "salvaged.sword",

      "skin": 0

    },

    "19": {

      "ammo": 0,

      "ammoType": null,

      "amount": 1,

      "container": "belt",

      "contents": [],

      "name": "SalvagedIcepick",

      "shortname": "icepick.salvaged",

      "skin": 0

    },

    "2": {

      "ammo": 120,

      "ammoType": "ammo.pistol",

      "amount": 1,

      "container": "belt",

      "contents": [

        "weapon.mod.holosight"

      ],

      "name": "Thompson",

      "shortname": "smg.thompson",

      "skin": 0

    },

    "20": {

      "ammo": 0,

      "ammoType": null,

      "amount": 1,

      "container": "belt",

      "contents": [],

      "name": "SalvagedAxe",

      "shortname": "axe.salvaged",

      "skin": 0

    },

    "21": {

      "ammo": 0,

      "ammoType": null,

      "amount": 1,

      "container": "belt",

      "contents": [],

      "name": "Pickaxe",

      "shortname": "pickaxe",

      "skin": 0

    },

    "22": {

      "ammo": 0,

      "ammoType": null,

      "amount": 1,

      "container": "belt",

      "contents": [],

      "name": "Hatchet",

      "shortname": "hatchet",

      "skin": 0

    },

    "23": {

      "ammo": 0,

      "ammoType": null,

      "amount": 1,

      "container": "belt",

      "contents": [],

      "name": "Rock",

      "shortname": "rock",

      "skin": 0

    },

    "24": {

      "ammo": 0,

      "ammoType": null,

      "amount": 1,

      "container": "belt",

      "contents": [],

      "name": "Torch",

      "shortname": "torch",

      "skin": 0

    },

    "25": {

      "ammo": 40,

      "ammoType": "arrow.hv",

      "amount": 1,

      "container": "belt",

      "contents": [

        "weapon.mod.holosight"

      ],

      "name": "Crossbow",

      "shortname": "crossbow",

      "skin": 0

    },

    "26": {

      "ammo": 120,

      "ammoType": "ammo.rifle",

      "amount": 1,

      "container": "belt",

      "contents": [

        "weapon.mod.holosight"

      ],

      "name": "M249",

      "shortname": "lmg.m249",

      "skin": 0

    },

    "27": {

      "ammo": 0,

      "ammoType": null,

      "amount": 20,

      "container": "belt",

      "contents": [],

      "name": "TimedExplosive",

      "shortname": "explosive.timed",

      "skin": 0

    },

    "28": {

      "ammo": 0,

      "ammoType": null,

      "amount": 20,

      "container": "belt",

      "contents": [],

      "name": "SurveyCharge",

      "shortname": "surveycharge",

      "skin": 0

    },

    "29": {

      "ammo": 0,

      "ammoType": null,

      "amount": 20,

      "container": "belt",

      "contents": [],

      "name": "F1Grenade",

      "shortname": "grenade.f1",

      "skin": 0

    },

    "3": {

      "ammo": 60,

      "ammoType": "ammo.shotgun",

      "amount": 1,

      "container": "belt",

      "contents": [

        "weapon.mod.holosight"

      ],

      "name": "PumpShotgun",

      "shortname": "shotgun.pump",

      "skin": 0

    },

    "30": {

      "ammo": 20,

      "ammoType": "ammo.rocket.basic",

      "amount": 1,

      "container": "belt",

      "contents": [],

      "name": "RocketLauncher",

      "shortname": "rocket.launcher",

      "skin": 0

    },

    "4": {

      "ammo": 120,

      "ammoType": "ammo.pistol",

      "amount": 1,

      "container": "belt",

      "contents": [

        "weapon.mod.holosight"

      ],

      "name": "SMG",

      "shortname": "smg.2",

      "skin": 0

    },

    "5": {

      "ammo": 120,

      "ammoType": "ammo.rifle",

      "amount": 1,

      "container": "belt",

      "contents": [

        "weapon.mod.holosight"

      ],

      "name": "BoltAction",

      "shortname": "rifle.bolt",

      "skin": 0

    },

    "6": {

      "ammo": 120,

      "ammoType": "ammo.rifle",

      "amount": 1,

      "container": "belt",

      "contents": [

        "weapon.mod.holosight"

      ],

      "name": "SemiAutoRifle",

      "shortname": "rifle.semiauto",

      "skin": 0

    },

    "7": {

      "ammo": 120,

      "ammoType": "ammo.pistol",

      "amount": 1,

      "container": "belt",

      "contents": [

        "weapon.mod.holosight"

      ],

      "name": "SemiAutoPistol",

      "shortname": "pistol.semiauto",

      "skin": 0

    },

    "8": {

      "ammo": 120,

      "ammoType": "ammo.pistol",

      "amount": 1,

      "container": "belt",

      "contents": [

        "weapon.mod.holosight"

      ],

      "name": "Revolver",

      "shortname": "pistol.revolver",

      "skin": 0

    },

    "9": {

      "ammo": 40,

      "ammoType": "ammo.handmade.shell",

      "amount": 1,

      "container": "belt",

      "contents": [

        "weapon.mod.holosight"

      ],

      "name": "WaterpipeShotgun",

      "shortname": "shotgun.waterpipe",

      "skin": 0

    }

  },

  "ZoneName": "GunGame"

}

 
````


Credit to [@Reneb](http://oxidemod.org/members/20031/), this is essentially a modified ArenaDeathmatch