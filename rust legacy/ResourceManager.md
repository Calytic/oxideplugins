This will edit rust server resources, so everything will be managed by rust.

With it you will be able to remove some resources or add some more.

The configs are as annoying as the lootspawnlist, or spawn list manager,

but it's the best way to do it to make everything automatic
**How to:**

1) Load the plugin on your server

2) SAVE YOUR NEW: oxide/config/ResourceManager.json TO KEEP AS A BACKUP

3) Edit your ResourceManager.json

the "0" "1" before every resources can be anything as long as they are not duplicate, here they are numbers as it's easier to keep count, but they DONT need to be in a row (just different)

"positions_x" "positions_y" "positions_z" are the positions in the map (you can use any locater for that), you dont have to put an exact y height as it will be automatically detected by rust (but i recommend setting one close to the real height)

"radius" is the radius of spawn of the resources, if you set 10 you will have all the spawns close to each other, and if you set it high they will all be distant

"thinkDelay" default is 60-70, you should keep that way, basically it's the time for the resource to spawn (basically ... as other stuff also edits the time of resource)

"spawnList" is the spawnlist that will be inside this spawner. they can all be different or you can have the same spawns, as you wish

- "numToSpawnPerTick" is the max number that can be spawned during think (so every 60-70secs by default)

- "prefabName" is the prefab name to be spawned (see under to have the full list)

- "targetPopulation" is the max number of resources to be spawned

so if you set numToSpawnPerTick to 2, and targetPopulation to 4, you will need 2 think (so 120secs) for all of them to spawn

4) Use [http://jsonlint.com/](http://jsonlint.com/) to check if your json file is valid then SAVE

5) Restart your server

6) you should see a: XX custom resource spawns where loaded!
**If it crashes? well you did something wrong, make sure all the names are correctly set ex: it's thinkDelay, not thinkdelay

Video Tutorial:


RELOADING THE PLUGIN:

Will result in multiplying the resources (old resources will NOT respawn back, but until they are harvest/killed they will stay ingame!)**