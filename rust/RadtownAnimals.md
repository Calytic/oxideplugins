Spawns animals at monuments to make looting a little harder. Animals will respawn when killed or if they travel too far away from the monument.


** Note to be careful when adjusting the maximum amount of animals, if you spawn too many the lag will be real

**Options**

"Options - Animals - Spawn *AnimalName*" - Use these to select what animals you want to spawn

"Options - Spawnpoints -  Spread" - The amount to spread the spawnpoints out from the original spawn point

"Options - Animals - Maximum distance from monument": 100, - The amount of meters away from a monument a animal can travel before getting respawned back

"Options - Timers - Distance check timer (minutes)": 5.0 - Checks the distance the animal is from the monument every X minutes, if it is greater than the distance set in the config the animal will be killed and will respawn

  "Options - Timers - Respawn (minutes)": 10.0 - The amount of time from when one of the animals is killed until it can respawn

**Chat Commands**

/ra_killall - Use this if you accidentally spawned too many animals

**Console Commands**

ra_killall - Same as above

**Config**

````

{

  "Options - Animals - Maximum Amount (per monument)": 10,

  "Options - Animals - Maximum Amount (total)": 50,

  "Options - Animals - Maximum distance from monument": 100,

  "Options - Animals - Spawn Bears": true,

  "Options - Animals - Spawn Boars": false,

  "Options - Animals - Spawn Chickens": false,

  "Options - Animals - Spawn Horses": false,

  "Options - Animals - Spawn Stags": true,

  "Options - Animals - Spawn Wolfs": true,

  "Options - Spawns - Airfield": true,

  "Options - Spawns - Lighthouses": false,

  "Options - Spawns - Powerplant": false,

  "Options - Spawns - Rad-towns": true,

  "Options - Spawns - Satellite": true,

  "Options - Spawns - Sphere Tank": false,

  "Options - Spawns - Trainyard": false,

  "Options - Spawns - Warehouses": false,

  "Options - Spawns - Water Treatment Plant": false,

  "Options - Spawnpoints - Spread": 20.0,

  "Options - Timers - Distance check timer (minutes)": 5.0,

  "Options - Timers - Respawn (minutes)": 10.0

}

 
````