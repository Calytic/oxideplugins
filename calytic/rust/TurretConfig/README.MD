

		
				
	
		
		
			
				
			
				
			
				
					Configuration File:
						Yes
					
	
			
		**Description** 


TurretConfig allows you to configure the way turrets work, full option list below.

**Features** 


* Accuracy (aimCone)

* Damage (bulletDamage)

* Targeting Distance (sightRange)

* Speed of bullets (bulletSpeed)

* Ammunition type and gun sound

* Health

* Player Damage Modifier

* Animal Override (disable attacking animals)

* Animal Whitelist (disable attacking specific animals)

* Admin override (adminOverride) - automatically adds admins to authorization list of turrets

* Sleep override (sleepOverride) - avoid targeting sleeping players

* Infinite ammunition for turrets (disables looting turrets)


**Permissions** 

This plugin uses Oxide's permission system. To assign a permission, use **grant user <username|steamid> <permission>** . To remove a permission, use **revoke user <username|steamid> <permission>** .


* **turretconfig.infiniteammo** (gives turrets built by player infinite ammo)
**Ex.**  grant user Calytic turretconfig.infiniteammo
**Ex.**  revoke user Calytic turretconfig.infiniteammo
**Ex.**  grant group player turretconfig.infiniteammo


**Console Commands** 

	
	
``turrets.reload

Applies configuration to all turrets``


**Default Configuration** 

	
	
``{

  "adminOverride": true,

  "animalOverride": false,

  "sleepOverride": false,

  "aimCone": 5.0,

  "ammoType": "ammo.rifle",

  "bulletDamage": 10.0,

  "bulletSpeed": 200.0,

  "globalDamageModifier": 1.0,

  "health": 750.0,

  "sightRange": 30.0,

  "useGlobalDamageModifier": false,

  "infiniteAmmo": false,

  "animals": [

    "stag",

    "boar",

    "chicken",

    "horse"

  ]

}``

Some options for the ammoType

	
	
``ammo.pistol

ammo.shotgun

ammo.rifle.hv

ammo.rifle.incendiary

ammo.rifle.explosive

ammo.pistol.hv

ammo.pistol.fire``


Note: Multiple ammunition types are not supported.  You may only choose one type.


Feedback is appreciated.
		
	
	
	