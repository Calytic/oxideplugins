**REQUIRES
[Spawns Database](http://oxidemod.org/plugins/spawns-database.720/) **


This plugin will let you **edit the rust spawn points**, so you can decide where players can spawn by default. This will not interfere with players spawns to sleeping bags.

**Config:**

1) Create your spawn file with Spawns Database

2) in the console do:  spawns.config SpawnsDatabaseFile


Then the plugin will take care in using the custom spawn points.

**Spawn Fix:**

Rust uses a spawn fix variable, basically it checks from 32m up to the ground to see if something is built or on the way (rocks, etc). So i added that.

Default is only 1m up to 1m under the spawn location. But you can change that to what ever you guys want.

rust is something like: 32m up to 1m down

If the plugin doesnt find a ground position, it will just spawn to where you've set the spawn point (so if it's high up in the air, it will stay high up in the air, unless you put 1000 down)

using 0 down will result in deactivating the spawn fix


````
{

  "Messages - Permissions - Not Allowed": "You are not allowed to use this command",

  "Settings - Spawn Database Name": "spawnfile",

  "Spawn Fix - Check from Xm up": "1.0",

  "Spawn Fix - Check to Xm down": "1.0"

}
````