SuicideBomber will allow players to use a Targeting Computer and Timed Explosives to explode.

**How To** 

* Move a targeting computer to quickbar
* Select the computer from your quickbar
* Press E
* Count down briefly (6s)
* Explode**Configuration** 


* damage (default 1200)
The amount of damage done to objects closest to you
* radius (default 12)
The maximum radius of the explosive damage
* explosives (default 10)
The number of Explosives required to use the Targeting Computer
* c4 (default 1)
The number of Timed Explosives required to use the Targeting Computer

* flare (default true)
Whether or not a flare is required

* scream (default true)
Whether or not the bomber screams before exploding

**Notes** 


* You do the most damage to the players/buildings closest to you.  The damage will disperse the farther away the object is.
* The scream sound may be disabled but is enabled by default
* Localization is supported so you may change the message language

**Default Configuration** 

````

{

Â  "c4": 1,

Â  "damage": 1200.0,

Â  "explosives": 10,

Â  "flare": true,

Â  "messages": {

Â  Â  "You lack the required {0} explosives": "You lack the required {0} explosives",

Â  Â  "You lack the required {0} timed explosives": "You lack the required {0} timed explosives",

Â  Â  "You lack the required flare": "You lack the required flare",

Â  Â  "You will explode shortly..": "You will explode shortly.."

Â  },

Â  "radius": 12.0,

Â  "scream": true

}

Â 
````