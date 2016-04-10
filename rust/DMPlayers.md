This **Damage Mod: Players** plugin allows admin to change the effects of all major damage types against players in the game via the config file.

Works well with my other plugins :

Damage Mod : Buildings

Damage Mod : Deployables

**Not recommended** to use with my Global Damage Modifier !

**Install -** Place the plugin in your oxide/plugins folder. It should then create a new Config file for you in oxide/config folder.

**Editing Config File -** Open the config file located in your oxide/config folder.

there you will see the following with all values of 1.0.


I have modified them here to show you** my preferred PVE setup**. All player damage is nulled except Suicide, so players can kill themselves.

And Generic, If your server is set to server.pve true, then when a player shoots someone's building while under the "not authorized" to build flag, they will receive damage back to themselves.

**My Preferred PVE server setup:**

````
{

"Player_Multipliers": {

"Bite": 0.0,

"Bleeding": 0.0,

"Blunt": 0.0,

"Bullet": 0.0,

"Cold": 0.0,

"ColdExposure": 0.0,

"Decay": 0.0,

"Drowned": 0.0,

"ElectricShock": 0.0,

"Explosion": 0.0,

"Fall": 0.0,

"Generic": 1.0,

"Heat": 0.0,

"Hunger": 0.0,

"Poison": 0.0,

"Radiation": 0.0,

"RadiationExposure": 0.0,

"Slash": 0.0,

"Stab": 0.0,

"Suicide": 1.0,

"Thirst": 0.0

}

}
````

Initial Configuration values of 1.0 - Normal Damage in game.

**To remove damage type from game:** Set desired damage type to 0.0

Example 1 : Setting Drowned to 0.0, will allow players to take no damage from drowning.

**To Increase Damage:** Set desired damage type in configuration file to more than 1.0

**To Decrease Damage :** Set desired damage type in configuration file to less than 1.0 but more than 0.0

Example 1 : Normal fall damage from a 10 block high jump resulted in a 65 health loss. I changed the Fall damage to 0.25, in turn I only received 7 health loss next time.