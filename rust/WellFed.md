Who said you have to be near starving when you get thrown out in the world? Not me... this plugin will fatten you up before you get tossed out to the world of Rust. Players will start with full health, hunger and thirst instead of default values.

**Note:** Players will still lose health, hunger and thirst as normal, it does not prevent damage or anything.

**Permissions :**

wellfed.onlogin

wellfed.onspawn


````

oxide.grant <group|user> <name|id> wellfed.onlogin

oxide.grant <group|user> <name|id> wellfed.onspawn


//example to allow all players

oxide.grant group player wellfed.onlogin

oxide.grant group player wellfed.onspawn

 
````


**Default Config :**

````
{

  "Enable WellFed On Login": true,

  "Enable WellFed On Spawn": true,

  "Login Health": 100,

  "Login Hunger": 1000,

  "Login Thirst": 1000,

  "Spawn Health": 100,

  "Spawn Hunger": 1000,

  "Spawn Thirst": 1000

}
````