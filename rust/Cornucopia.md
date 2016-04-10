The cornucopia (from Latin cornu copiae) or horn of plenty is a symbol of abundance and nourishment, commonly a large horn-shaped container overflowing with produce, flowers or nuts.


This plugin allows admin to keep a minimum of everything on the map so nobody ever complains he can't find animals, barrels, loot crates or mineral nodes ever again.

What you need to know:


· Yes, it's compatible with BetterLoot, barrels and crates contents are set on spawn

· The default config file is very conservative and similar to what you will typically see on a normal, unmodded server, you need to review the values and increase them as you see fit.

· You can set any of the config values to zero if you do not wish that specific resource to be affected (do not set the timer to zero though!)

· The plugin replaces existing collectibles (ore lumps, wood parts, mushroom, hemp, etc.) to ease with placement and distribution.

· On server restart the number of initial collectibles will be very low, so if you have very high numbers in the config, it could take a while before enough collectibles are available to fulfill your target

· The mod is on a 15 min timer by default. The first pass can take several seconds and lag the server out (the more extras you demand, the worse this will be). The spawn cycle is NOT triggered on start, so you need to either manually run the command or wait 15 min to see any effect.

· The command /cdump displays current numbers of everything to RCON (also available as console command cornu.dump)

· The command /cspawn runs the cycle, overriding the timer (it does not affect the next timer occurrence) - also available as console command cornu.spawn

· Both of the above commands need the player to be admin to use

· Errors and some warnings are thrown to RCON, let me know if they are intrusive

· Very important: Stuff will spawn anywhere! Barrels are not limited to monuments/roads, loot boxes are not limited to rad towns, etc.

· This plugin also deletes stacked barrels and rad town crates by default (you can turn this off by setting fixloot to false in the config file)

· Chat command /cpurge and console command cornu.purge. This will delete ALL spawnable elements supported by Cornucopia from the map

· Config param treatMinimumAsMaximum (false by default) allows you to LIMIT the amount of resources on your server to the same amount

Default config:

````
{

  "Animals": {

    "minBears": 10,

    "minBoars": 25,

    "minChickens": 5,

    "minHorses": 60,

    "minStags": 35,

    "minWolves": 5

  },

  "Barrels": {

    "minGoodBarrels": 25,

    "minNormalBarrels": 15,

    "minTrashCans": 45

  },

  "Crates": {

    "minBoxCrates": 20,

    "minWeaponCrates": 15

  },

  "General": {

    "refreshIntervalSeconds": 900,

    "fixloot": true

  },

  "Minerals": {

    "minMetalNodes": 70,

    "minStoneNodes": 70,

    "minSulfurNodes": 70

  }

}
````

Duplicate loot fix only:

````
{


  "Animals": {

    "minBears": 0,

    "minBoars": 0,

    "minChickens": 0,

    "minHorses": 0,

    "minStags": 0,

    "minWolves": 0

  },

  "Barrels": {

    "minGoodBarrels": 0,

    "minNormalBarrels": 0,

    "minTrashCans": 0

  },

  "Crates": {

    "minBoxCrates": 0,

    "minWeaponCrates": 0

  },

  "General": {

    "refreshIntervalSeconds": 300,

    "fixloot": true

  },

  "Minerals": {

    "minMetalNodes": 0,

    "minStoneNodes": 0,

    "minSulfurNodes":0

  }

}
````