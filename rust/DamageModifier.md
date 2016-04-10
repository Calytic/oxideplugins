**WARNING :** As this is a global modifier, I do not recommend using other damage modifiers/changers at the same time.


This Global Damage Modifier plugin allows admin to change the effects of all major damage types in the game via the config file.

Damage modification is currently set to effect all entities in game that can be effected by changing the value.

**Install -** Place the plugin in your oxide/plugins folder. It should then create a new Config file for you in oxide/config folder.

**Editing Config File -** Open the config file located in your oxide/config folder.

there you will see the following by default.


{

  "Global_Multipliers": {

  "Bite": 1.0,

  "Bleeding": 1.0,

  "Blunt": 1.0,

  "Bullet": 1.0,

  "Cold": 1.0,

  "ColdExposure": 1.0,

  "Decay": 1.0,

  "Drowned": 0.0,

  "ElectricShock": 1.0,

  "Explosion": 1.0,

  "Fall": 1.0,

  "Generic": 1.0,

  "Heat": 1.0,

  "Hunger": 1.0,

  "Poison": 1.0,

  "Radiation": 1.0,

  "RadiationExposure": 1.0,

  "Slash": 1.0,

  "Stab": 1.0,

  "Suicide": 1.0,

  "Thirst": 1.0

  }

}


Initial Configuration values of 1.0 - Normal Damage in game.

**To remove damage type** from game: Set desired damage type to 0.0
Example 1 : Setting Drowned to 0.0, will allow players (or anything effected by damage type) to take no damage from drowning.
Example 2 : Setting Bite Damage to 0.0 will result in animals not hurting players (or anything effected by damage type) when the Bite damage is applied.
Example 3 : Setting Decay to 0.0, will remove decay damage for buildings(or anything effected by damage type).

**To Increase Damage:** Set desired damage type in configuration file to more than 1.0
Example 1 : If you want to increase damage from Fire by any source, set Fire to something more than 1.0
Note : Setting it higher will effect all things that deal any Fire damage, Incendiary rocks, Incendiary Bullets, Fire itself...etc...

**To Decrease Damage :** Set desired damage type in configuration file to less than 1.0 but more than 0.0
Example 1 : Normal fall damage from a 10 block high jump resulted in a 65 health loss. I changed the Fall damage to 0.25, in turn I only received 7 health loss next time.


Multipliers actually scale damage up or down, not multiply. So changing a value from 1.0 to 10.0 will not increase damage x10.  You will have to play with numbers to see where you want it to be.

**Future ideas for plugin:**
Target specific damage modification :

Set what type of entity is effected by changing damage type modifiers.

Such as having separate modifiers for players, buildings, NPC's, Animals, Signs and other items separately.

Preconfigured Damage Sets :

Ready to use damage sets that can be changed fast. Possible ideas,


Rust Peaceful Mode: Set all damages to 0.0

Rust Easy Mode: Set all damages to 0.25

Rust Normal Mode: Set all damages to 1.0

Rust Hard Mode: Set all damages to 10.0

Rust Extreme Mode:  Set if as high as you want!!!