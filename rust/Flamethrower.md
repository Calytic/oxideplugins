This plugin is mostly for fun. Use as you wish, change it, make it better, fix it.. whatever.  its for fun  at least until the official version is out.


Allows authorized players to use the Thompson as a makeshift flamethrower.  Flames and Blast heat damage shoot out approx. to 12 meters, then flames fall to ground and does damage over time. Flames will start small secondary fires that will last longer.



**Fuel:**

uses 5 Low grade fuel and 5 crude oil every time you hit the use key.

This can be adjusted in Config file.

**Permissions:**

flamethrower.allowed - grants access to use flamethrower


````
oxide.grant <group|user> <name|id> flamethrower.allowed


Example: To allow the group player to use it. (all players)

oxide.grant group player flamethrower.allowed


Example 2 : To just allow a person to use it:

oxide.grant user "colon blow" flamethrower.allowed
````


**Config:  (**Default numbers below)


````
{

  "AmountRequired-CrudeOil": 5,

  "AmountRequired-LowGradeFuel": 5,

  "BlastDamageEffectedByProtectionValue": true,

  "BlastDamageRadius": 1.0,

  "BlastDamagetoBuilding": 10.0,

  "BlastDamagetoNPC": 10.0,

  "BlastDamagetoPlayer": 10.0,

  "EnableChanceOfWeaponFailure": false,

  "FlamethrowerReloadTime": 4.0,

  "GroundFlameDamagePerSecond": 2.0,

  "GroundFlameDamageRadius": 1.0,

  "GroundFlameMaximumDuration": 10.0,

  "GroundFlameMinimumDuration": 10.0,

  "GroundFlameTimetoSpread": 8.0

}
````


**Configuration Files Explained**

"AmountRequired-CrudeOil": 5,  - Amount of Crude Oil needed per shot.

"AmountRequired-LowGradeFuel": 5,  - Amount of Low Grade Fuel needed per shot.

"BlastDamageEffectedByProtectionValue": true,  - All items have resistance against damage types, setting to false allow Initial Flame Blast damage to deal 100% of value no matter what.

"BlastDamageRadius": 1.0, - Initial Flame Blast has a quick damage cone. this is the radius from the flame shot it effects as it goes out to approx. 12 meters.

"BlastDamagetoBuilding": 10.0, - Intital Flame Blast effects Buildings this amount per hit.

"BlastDamagetoNPC": 10.0, - Intital Flame Blast effects NPC's this amount per hit.

"BlastDamagetoPlayer": 10.0, - Intital Flame Blast effects Players this amount per hit.

"EnableChanceOfWeaponFailure": false, - Setting to True, will give your weapon a slight chance to explode on use.

"FlamethrowerReloadTime": 4.0, - Amount of time need to pull trigger again after using.

"GroundFlameDamagePerSecond": 2.0, - Flames that drop on ground, this is how much DPS it does to things.

"GroundFlameDamageRadius": 1.0, - Flames that drop on ground, this is the radius amount the DPS effects.

"GroundFlameMaximumDuration": 10.0, - Once Flames are out and on ground, this is the minimum time they will stay in world. Setting to lower than max, flames will randomly despawn starting at this time.

"GroundFlameMinimumDuration": 10.0, - Once Flames are out and on ground, this is the maximum time they will stay in world.

"GroundFlameTimetoSpread": 8.0 - Once Flames are on ground, they have chance to 'spread' themselves starting after this many seconds. setting to higher than time max will prevent this.

**Use:**

Equip a Thompson gun, have the required fuel in inventory, make sure the Thompson is highlighted in belt, then press the use key (the E key).

**Note:** testing with 20 plus players at a time do not seem to have a big performance hit. But of course if everyone is spamming the use key it will cause framerate degradation.


thanks to the other plugin developers for there great coding inspiration