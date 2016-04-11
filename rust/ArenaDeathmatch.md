REQUIRED
**[Event Manager](http://oxidemod.org/plugins/event-manager.740/)**
**[Kits](http://oxidemod.org/plugins/kits.668/)**


**THIS IS A BETA VERSION**
**STUFF MIGHT BE MISSING (MESSAGES, COMMANDS)**
**I NEED FEEDBACK SO I CAN IMPROVE THE PLUGIN!!**

**DEATHMATCH - Free for All**

**How do use this mod:**

1) you need to create your arena

2) you need to create at least 1 kit for your arena (with Kits plugin) default name that you should use is: deathmatch

remember to do /kit add deathmatch "Deathmatch Kit" -authlevel2

with -authlevel2 to prevent players from redeeming this kit!

3) you need to create your spawnpoints for this mod, default name is: DeathmatchSpawnfile (but you can edit in the configs)

4) you need to configure the Event Manager (see event manager to know how) and then start the event

**Zone Management:**

1) /zone_add ZONENAME

2) Then when the zone when created use: /zone_edit Deathmatch

This will let you edit the zone and put what ever you want as option.

recommended options are as follow:

/zone eject true undestr true autolights true nobuild true nodeploy true nokits true notp true killsleepers true nosuicide true nocorpse true nowounded true

3) Then only the Zone Management plugin will take care of saving options, you will not have to edit the config file for it anymore

4) In the Event plugins you will always find a place where to put the zone name. REMEMBER TO DO IT!!

In Deathmatch it is inside: "DeathMatch - Zone - Name"

**Configs:**

Auto generated