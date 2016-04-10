This plugin will spawn x number of chests around the map with random loot that you define. Loot boxes will automatically despawn at the configured despawn time.


You can change the spawn location, radius etc if you want it to be more localized to a specific area. (Right now they can spawn on most of the map)

**Commands:**

/lootchests add ItemID Amount

/lootchests remove ItemID

/lootchests save

**Permission:**

lootchests.admin

**How to use:**

To add items to the list, use: /lootchest add ItemID Amount.

Or if you know what you are doing you can edit the data file ItemList.json directly located in oxide/data/LootChests/. I added a save command just for this purpose (It actually saves everything).


Default spawn time is 2 hours.

Default despawn time is 30 minutes.


````

{

  "ChestSpawnCount": 20,

  "DestroyOnEmpty": true,

  "ItemsPerChest": 1,

  "MaximumRange": 3000.0,

  "MininumRange": 0.0,

  "SecondsForSpawn": 7200,

  "SecondsTillDestroy": 1800,

  "ShowDespawnMessage": true,

  "ShowSpawnMessage": true,

  "StartPoints": [

  "-3000, 450, -3000"

  ]

}

 
````


**

Note:**

If you make any changes to the config, you need to /reload LootChests

Chests will not spawn on top of buildings or cliffs.

**Feel free to hit that purple button   >>**