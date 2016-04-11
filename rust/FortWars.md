**Fort Wars** allows for alternating phases of build and fight. As you can probably tell from this game mode I like small maps and mass bloodshed everywhere.


This doesn't require any other plugin to run. Just install and it will be enabled with the default settings listed below.
**

Build Phase**


* Faster crafting
* More resources
* Less Planes
* Less Helicopters
* 20 Min Build Phase time (Default)
* Planes can be spawned here too, (Optional, Default Off, check config below.)


**Fight Phase**


* Slower Crafting
* Less Resources
* Normal Damage
* More Planes (1 every 5 min default)
* More Helicopters (1 every 10 min default)
* 40 Min Fight Phase time (Default)


**Commands**
Admin

/hell <number>, Spawns in <number> of Helis. Be warned these helicopters don't care for naked people, they will die, all will die.

This command cannot be used in build phase.


/drop <number>, Spawns in <number> of Air Drops's.


Both the helicopters and Airdrops come from outside the playable area, so may take a minute to arrive.

Console Admin

fw.enable 0/1, Actually enable or disable the mode.

fw.build, Start build phase.

fw.fight, Start fight phase.

**Player**

/phase, See what phase it is, and how long is left.

**Config Options
**

The messages displayed to players can be found in the (server/<my_server_identity>/oxide/lang) folder.


* "Craft - Build": 10 (Crafting Speed in %, so 10 = 10% of normal.)
* "Craft - Fight": 600, (Crafting Speed in %, so 600 = 600% of normal. or 6x.)
* "Drop - Build": 0, (0 = false, 1 = true.)
* "Drop - Fight": 1, (0 = false, 1 = true.)

* "Gather - Build": 500, (Gather rate in build phase.)
* "Gather - Fight": 25, (Gather rate in fight phase.)
* "Heli - HP": 200, (Health of Helicopter.)
* "Heli - HPRudder": 30, (Health of Rudder.)
* "Heli - Speed": 110, (Helicopter speed.)
* "Time - Build": 1200, (Time in seconds of build phase.)
* "Time - Drop Build": 300, (Time in seconds of build Air Drop interval.)
* "Time - Drop Fight": 300, (Time in seconds of fight Air Drop interval.)
* "Time - Fight": 2400, (Time in seconds of fight phase.)
* "Time - Heli": 600 (Time in seconds of Helicopter interval.)


**Permissions**


* "FortWars.UseAll" (Master override to use all commands, Server owner ideally.)
* "FortWars.UseHeli" (Perm to use /hell)
* "FortWars.UseFight" (Perm to use console fw.fight)
* "FortWars.UseBuild" (Perm to use console fw.build)
* "FortWars.UseEnable" (Perm to use console fw.Enable)
* "FortWars.UseDrop" (Perm to use /drop)


**Installation
**

Just place into "/server/my_server_identity/oxide/plugins" folder. The config and language files will be created automatically. 

"server/<my_server_identity>/oxide/lang" For the Lang file.
"server/<my_server_identity>/oxide/config" For the Config file.


To add permissions go to [http://oxidemod.org/threads/using-oxides-permission-system.8296/ ](http://oxidemod.org/threads/using-oxides-permission-system.8296/) for information how to use permissions.