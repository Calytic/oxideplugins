**WARNING THIS PLUGIN CAN'T BE USED ALONE**

This is a plugin for futur plugins that will require a spawn database.

**Commands:**

- /spawns_new => start making a new spawns file

- /spawns_add => add a spawn point

- /spawns_remove NUMBER => remove a spawn point

- /spawns_save FILENAME => save the spawns in a file and close

- /spawns_open FILENAME => Open an existing spawns file

- /spawns_close => close your current spawns without saving

- /spawns_help => get in-game help informations

**For Plugin developpers:**


Here are exemples on how to use this plugin with **lua**, but of course this **would work with C# plugins**


To get this plugin use in PLUGIN:Init()

````
local pluginList = plugins.GetAll()

    for i = 0, pluginList.Length - 1 do

        local pluginTitle = pluginList[i].Object.Title

        if pluginTitle == "Spawns Database" then

            spawnsplugin = pluginList[i].Object

            break

        end

    end
````

To get the **max number of spawns **in a spawnfile use (use this to initialise your spawnfile, if it returns false, then dont use the spawnfile):

````
local count, err = spawnsplugin:GetSpawnsCount( spawnfile )

if(not count) then print(err) return end
````

To get a **random spawn point **use:

````
local spawnpoint, err = spawnsplugin:GetRandomSpawn( spawnfile)

if(not spawnpoint) then print(err) return end

player.lastKnownPosition.x = spawnpoint.x

player.lastKnownPosition.y = spawnpoint.y

player.lastKnownPosition.z = spawnpoint.z
````

To get a **random spawn point from range **use:

````
local spawnpoint, err = spawnsplugin:GetRandomSpawnRange( spawnfile,min,max)

if(not spawnpoint) then print(err) return end

player.lastKnownPosition.x = spawnpoint.x

player.lastKnownPosition.y = spawnpoint.y

player.lastKnownPosition.z = spawnpoint.z
````

To get a **specific spawn point** use:

````
local spawnpoint, err = spawnsplugin:GetSpawn( spawnfile, number )

if(not spawnpoint) then print(err) return end

player.lastKnownPosition.x = spawnpoint.x

player.lastKnownPosition.y = spawnpoint.y

player.lastKnownPosition.z = spawnpoint.z
````