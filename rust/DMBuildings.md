This **Damage Mod: Buildings** plugin allows admin to change the effects of all major damage types against Buildings in the game via the config file.

Works well with my other plugins :

Damage Mod : Players

Damage Mod : Deployables

**Not recommended** to use with my Global Damage Modifier !

**Install -** Place the plugin in your oxide/plugins folder. It should then create a new Config file for you in oxide/config folder.

**Editing Config File -** Open the config file located in your oxide/config folder.

there you will see the following with all values of 1.0.


I have modified them here to show you** my prefered PVE setup**. All Building damage is nulled, except Decay. Helps on cleaning up the server a little if they eventually decay.

**My Preferred PVE setup** for buildings

````
{

"Buildings_Multipliers": {

"Bite": 0.0,

"Bleeding": 0.0,

"Blunt": 0.0,

"Bullet": 0.0,

"Cold": 0.0,

"ColdExposure": 0.0,

"Decay": 1.0,

"Drowned": 0.0,

"ElectricShock": 0.0,

"Explosion": 0.0,

"Fall": 0.0,

"Generic": 0.0,

"Heat": 0.0,

"Hunger": 0.0,

"Poison": 0.0,

"Radiation": 0.0,

"RadiationExposure": 0.0,

"Slash": 0.0,

"Stab": 0.0,

"Suicide": 0.0,

"Thirst": 0.0

}

}
````

Initial Configuration values of 1.0 - Normal Damage in game.

**To remove damage type from game:** Set desired damage type to 0.0
Example 1 : Setting Explosive Damage to 0.0 will prevent players from using C4 against buildings.


**To Increase Damage:** Set desired damage type in configuration file to more than 1.0
Example 1 : Setting Decay to anything over 1.0, will increase the rate at which it decays.

**To Decrease Damage :** Set desired damage type in configuration file to less than 1.0 but more than 0.0
Example 1 : Setting say...explosives to 0.25 will decrease the damage done by C4 on buildings.