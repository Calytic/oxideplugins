This plugin allows you to spawn helicopters on a timer along with setting how long they last before they leave.


The idea of this plugin is to allow Choppers to spawn on a regular or less regular basis and set how long before they leave, when someone engage's the chopper (Hits) if the lifetime of the chopper is about to expire it extends its life to ensure the players attacking it don't lose it half way through.

**Permissions**

canCoptorControl - allows for use of spawning and killing helis.

**ChatCommands**

NextHeli - advises how long untill a heli comes and if its up how long before it leaves.


KillAllHelis - Removes all helis from the map - requires permissions (canCoptorControl)


SpawnHeli - Spawns a heli - requires permissions (canCoptorControl)

**Config**

````
{

  "CoptorLifetimeInMins": 7.5, -- Duration that Chopper is on the map

  "CoptorRespawnTimeInSeconds": 7200 -- Spawn Timer

}
````