Add Human Non player characters ingame!

Make your cities a little bit more lively
Preview:


Before asking for help people read the overview

Tutorial videos will come soon

Optionnal:
[Minstrel](http://oxidemod.org/plugins/minstrel.981/) 0.0.6+ 
Features:

- Fully Configurable

- Can say hi when you get close to them

- Can say goodbye when you get away from them

- Can say something when you try interacting with them (USE)

- Can say ouch when you hit them

- Can say that you are a murderer when you kill them

- Multiple messages are supported (random one chosen)

- Set there name

- Set there kits (Kit plugin required)

- Set Waypoints so they can walk around the map

- Set if they are invulnerable

- Set there respawn time if they die

- NPC will defend themselves

- Set NPC Chasing speed

- Set NPC Damage

- Set NPC Max Chasing distance

- Set NPC Max View distance

- Set NPC Hostility

- Set NPC as Mistrels

- While Chasing or using Waypoints, the NPC will try to detect automatically the best ground position
Commands:

- /npc_add => create a new npc and edit it

- /npc_edit [Id] => edit the npc you are looking at or specified id

- /npc_remove [Id] => erase the npc you are looking at or specified id

- /npc_end => stop editing an npc

- /npc OPTION VALUE => set a value of an option of your npc

- /npc_reset => removes all npcs

- /npc_pathtest => follow npc path

- /npc_list => list all npcs

- /npc_way [Id] => draws path of the npc you are looking at or specified id
NPC_ADD

Creates a new NPC, and edit him. He will be created where you stand, and be looking the same way that you do

using /npc_add XXXX (npc ID from /npc_list) will clone the NPC to your position
NPC_EDIT

Edit an NPC (not needed if you just did /npc_add)

Then you can use the command: /npc
NPC_END

Stop editing an NPC
NPC

by just putting the option, you will see what value it is currently set to

Options values:

attackdistance XX => Distance between him and the target needed for the NPC to ignore the target and go back to spawn

bye reset/"TEXT" "TEXT2" etc => Dont forgot the \", this what will be said when the player gets away from the NPC

damageamount XXX => Damage done by that NPC when he hits a player

damagedistance XXX => Min distance for the NPC to hit a player (3 is default, maybe 20-30 needed for snipers?)

damageinterval XXX => Interval in seconds that the NPC have to wait before attacking again

enable true/false => Enable (default) or disable the NPC without deleting him (notice that when you are editing a bot it will stay active until you say /npc_end)

radius XXX => Radius of which the NPC will detect the player

health XXX => To set the Health of the NPC

hello reset/"TEXT" "TEXT2" etc => Dont forgot the ", this what will be said when the player gets close to the NPC

hurt reset/"TEXT" "TEXT2" etc => Dont forgot the \", set a message to tell the player when he hurts the NPC

hostile true/false => Set the NPC Hostile, will attack players on sight (radius is the sight limit)

invulnerable true/false => To set the NPC invulnerable or not

kill reset/"TEXT" "TEXT2" etc => Dont forgot the \", set a message to tell the player when he kills the NPC

kit reset/"KitName" => To set the kit of this NPC, requires the Kit plugin (see under)

lootable true/false => Set if the NPC is lootable or not

maxdistance XXX => Max distance from the spawn point that the NPC can run from (while attacking a player)

minstrel reset/"tunesong" => Give an NPC a song to play all the time, you need to create a song from the minstrel plugin ( [Minstrel](http://oxidemod.org/plugins/minstrel.981/) )

name "THE NAME" => To set a name to the NPC

respawn true/false XX => To set it to respawn on death after XX seconds, default is instant respawn

spawn new => To set the new spawn location

speed XXX => To set the NPC running speed (while chasing a player)

stopandtalk true/false XXX => To set if NPC should stop when a player talks to him, and if true for how much time.

use reset/"TEXT" "TEXT2" etc => Dont forgot the \", this what will be said when the player presses USE on the NPC

waypoints reset/"Waypoint list Name" => To set waypoints of an NPC

hitchance float => chance to hit target

fireduration float => time to fire

reloadduration float => time to reload

defend true/false => attack if attacked

needsAmmo true/false => needs to have ammo in inventory to shoot

NPC WAYPOINTS:

You need to make waypoints:
[Waypoints Database](http://oxidemod.org/plugins/waypoints-database.982/) 1.0.0

to hook to your NPC, see the waypoints post to know how to make some
NPC KIT

You need the Kit Plugin.

Create a new kit with the kit plugin like you usually do then do:

/kit add "random name" "random description" -authlevel2 (the level is set so NO players can use the kit, only admins and NPCs)

Then while editing the NPC do: /npc kit "random name" (being the same name as the kit ofc)

NPC ATTACK MOVEMENTS & PATHFINDING:

The Pathfinding is still not perfect, but it's getting there, currently the main problem isnt really coming from the Pathfinding but from the HumanNPC plugin because of the way i wrote it, so i'll need to rewrite a part of the plugin to make better movements and player attacks.

you will need to download [PathFinding for Rust Experimental | Oxide](http://oxidemod.org/resources/pathfinding.868) to make the NPC attack movements work.

If the NPC can't find any paths for 5 seconds it will stop targetting the entity and go back to his spawn with full health.
For Plugin Developpers:

Hooks were implemented to allow other plugins to interact with this one.

None of them have return values (can be edited if needed)

New hooks can be added

Note that all NPC have unique userID's, (BasePlayer.userID), so you may easily save informations of NPC by userID

Called when the NCP is getting hit

````
 OnHitNPC(BasePlayer npc, HitInfo hinfo)
````

Called when the NCP is getting used (pressed use while aiming the NPC)

````
 OnUseNPC(BasePlayer npc, BasePlayer player)
````

Called when a player gets in range of the NPC

````
 OnEnterNPC(BasePlayer npc, BasePlayer player)
````

Called when a player gets out of range of the NPC

````
 OnLeaveNPC(BasePlayer npc, BasePlayer player)
````

Called when an NPC gets killed

````
 OnKillNPC(BasePlayer npc, BasePlayer player)
````

Called when an NPC reachs a waypoint and changes to next waypoint

````
 OnNPCPosition(BasePlayer npc, Vector3 pos)
````

Called when an NPC respawns

````
 OnNPCRespawn(BasePlayer npc)
````

Called when an NPC gets looted

````
 OnLootNPC(PlayerLoot loot, BaseEntity target, string npcuserID)
````

If you want to contribute to this plugin you may ask for pull requests on github:
[Oxide2Plugins/HumanNPC.cs at master · strykes/Oxide2Plugins · GitHub](https://github.com/strykes/Oxide2Plugins/blob/master/CSharp/HumanNPC.cs)

Usage of this plugin:

- Make you server more lively, with cities and NPC that talk and interact a bit with players

- Create Epic mobs that spawn every X time and once killed gives loot (use kits plugin for that).

- Create other plugins that can give quests to players (Hunt RPG is on it's way )

- Create other plugins that uses the NPC to manage banks, quests, trades, shops.

- Only your imagination is limited, possibilities are infinite! TO DO LIST:

To Do:

- Area Call for help

- friends list not to atack
Fail:

- add bullets animation => not sure i can :/ probably controlled client side

- different radius for chat & hostile (Not going to implement that, too much checks)
Config file

````
{

  "Weapon To FX": {

    "pistol_eoka": "fx/weapons/vm_eoka_pistol/attack",

    "pistol_revolver": "fx/weapons/vm_revolver/attack",

    "rifle_ak": "fx/weapons/vm_ak47u/attack",

    "rifle_bolt": "fx/weapons/vm_bolt_rifle/attack",

    "shotgun_pump": "fx/weapons/vm_waterpipe_shotgun/attack",

    "shotgun_waterpipe": "fx/weapons/vm_waterpipe_shotgun/attack",

    "smg_thompson": "fx/weapons/vm_thompson/attack"

  }

}
````