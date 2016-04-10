Booby traps boxes with a variety of different traps for players with auth/permission. Various permission levels for different types of traps. Also options to set random traps on loot boxes and supply drops when they spawn.

**Available Traps**

Grenade - drops a grenade

Beancan - drops a beancan grenade

Explosive - detonates C4

Landmine - drops a circle of landmines around the box

Beartrap - drops a circle of beartraps around the box

Radiation - create a radiation zone around the box (needs ZoneManager)

Shock - electrocutes the looter

Fire - burns the looter

**Options**

I have tried to make the config as self-explanatory as possible.
-"Autotraps - Chance" is the chance a box/drop will get a trap when it spawns (default 1 out of 5 chance)
-"Plugins - Use EntityOwner " - Checks to make sure you are the owner of the box, if you are the owner or if there is no owner you will be allowed place the trap (needs EntityOwner)
-"Trap Timer" is the amount of time from which a player opens a box until the trap goes off
-"Buy Amount" for each item is how much of that item it will cost to set the trap


The rest you should be able to work out

**Chat Commands**

/settrap 'trapname' - Set a trap on the box you are looking at (options are below)

/checktrap - Checks the box you are looking at for traps

/removetrap - Remove a trap on the box you are looking at

/traplist - Lists all boxes with traps (Auth2)

/erasealltraps - Remove all traps on the map (Auth2)


````
/settrap Trap Names

- grenade

- beancan

- explosive

- landmine

- beartrap

- radiation

- shock

- fire
````


**Permissions**

boobytraps.elements - Used to set fire/shock traps

boobytraps.explosives - Used to set grenade/beancan/explosive traps

boobytraps.deployables - Used to set landmine/beartrap traps

boobytraps.admin - Used to set any trap, also can use /removetrap and /checktrap

**Important Information**

-Traps are activated when a player loots it, or if the box gets shot/hit!

-You can set traps on any kind of wood storage box/small stash, supply drops, rubbish piles and loot containers (barrels, military boxes etc). You cannot set traps on furnaces, campfires, quarrys etc

-Traps set on player placed boxes will be saved, traps set on barrels and loot boxes will be wiped on restart/unload

-It is not recommended to set traps on your main loot boxes as the box, and everything surrounding it will take damage!

-If you hear a noise when you open a box you had better run!

**Config**

````

{

  "Options - Autotraps - Airdrops": true,

  "Options - Autotraps - Airdrops - Chance": 5,

  "Options - Autotraps - Loot Container - Chance": 5,

  "Options - Autotraps - Loot Containers": true,

  "Options - Plugins - Use EntityOwner ": true,

  "Options - Tool Cupboard - Use Building Privileges ": true,

  "Options - Traps - Trap timer": 2.0,

  "Traps - Beancan": true,

  "Traps - Beancan - Buy Amount": 2,

  "Traps - Beancan - Damage": 30.0,

  "Traps - Beancan - Radius": 4.0,

  "Traps - Beartraps": true,

  "Traps - Beartraps - Buy Amount": 10,

  "Traps - Beartraps - Radius": 2.0,

  "Traps - Explosive": true,

  "Traps - Explosive - Buy Amount": 2,

  "Traps - Explosive - Damage": 110.0,

  "Traps - Explosive - Radius": 10.0,

  "Traps - Fire": true,

  "Traps - Fire - Buy Amount - LowGrade": 50,

  "Traps - Fire - Buy Amount - Oil": 50,

  "Traps - Fire - Damage": 1.0,

  "Traps - Fire - Radius": 2.0,

  "Traps - Grenade": true,

  "Traps - Grenade - Buy Amount": 2,

  "Traps - Grenade - Damage": 75.0,

  "Traps - Grenade - Radius": 5.0,

  "Traps - Landmine": true,

  "Traps - Landmine - Buy Amount": 10,

  "Traps - Landmine - Radius": 2.0,

  "Traps - Radiation": true,

  "Traps - Radiation - Buy Amount": 75,

  "Traps - Radiation - Radius": 10,

  "Traps - Radiation - Time to keep radiation active": 60.0,

  "Traps - Shock": true,

  "Traps - Shock - Damage": 95.0,

  "Traps - Shock - Radius": 2.0

}

 
````


**TO DO**

- More work on the electrocution effects, as of right now it is quite dull

- Any ideas you may have