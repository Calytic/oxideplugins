**AntiOfflineRaid**  is a structure-related plugin that reduces the amount of damage to player structures by a percentage. The percentage decreases as time passes.


There is a cooldown of 10 minutes before damage reductions will apply.  Meaning players may not necessarily prevent a raid by going offline.


Going afk for more than 5 minutes is the same as going offline.


AntiOfflineRaid determines who owns a structure based on who built it.


There is **NO REFUND**  for wasted explosives.


AOR will not know how long a player has been offline unless they go offline while this plugin is running.  There is no method to determine how long a player was offline before this plugin is installed, AOR will **not** protect bases for players that are offline since before this plugin is installed.


When unloading or updating AOR, it is recommended to save the server with server.save.

**Chat Commands** 

	
	
````
/ao "PlayerName"

Check the AntiOfflineRaid status of any active or sleeping player
````


**Permissions** 

This plugin uses Oxide's permission system. To assign a permission, use **grant user <username|steamid> <permission>** . To remove a permission, use **revoke user <username|steamid> <permission>** .


* 
**antiofflineraid.protect** (enabled protection using offline time)
**Ex.**  grant user Calytic antiofflineraid.protect
**Ex.**  revoke user Calytic antiofflineraid.protect
**Ex.**  grant group player antiofflineraid.protect


* **antiofflineraid.check** (allows access to "ao" command)
**Ex.**  grant user Calytic antiofflineraid.check
**Ex.**  revoke user Calytic antiofflineraid.check 
**Ex.**  grant group moderator antiofflineraid.check


**Configuration** 


* 
**afkMinutes**  (default 5)

The number of minutes without moving before a player is considered "offline."  Minimum is 5.

* 
**cooldownMinutes**  (default 10)

The number of minutes after going offline that damage remains default.  After cooldown has expired, damage is completely canceled up until the 60 minutes (after which damageScale is used)

* 
**damageScale** 

The scale of damage done to structures depending on how many hours since the owner was last online

* 
**interimDamage**  (default 0%)

The scale of damage between the cooldown and the first hour

* 
**clanShare**  (default false)

Checks number of clan-mates online before mitigating damage

* 
**minMembers**  (default 1)

The number of clan mates that must be online for the entire clan to be considered online

* 
**showMessage**  (default true)

Whether or not to display gui message when a base is protected

* 
**protectionMessage** 

The message to show players when they hit a protected building

* 
**playSound**  (default false)

Play a sound when damage is blocked

* 
**sound**  (default mod attachment)

Asset path of sound to play


**Default Configuration** 

	
	
````
{

  "protectionMessage": "This building is protected: {amount}%",

  "showMessage": true,

  "afkMinutes": 5,

  "clanShare": false,

  "minMembers": 1,

  "playSound": false,

  "sound": "assets/prefabs/weapon mods/silencers/effects/silencer_attach.fx.prefab",

  "prefabs": [

    "door.hinged",

    "door.double.hinged",

    "window.bars",

    "floor.ladder.hatch",

    "floor.frame",

    "wall.frame",

    "shutter",

    "wall.external",

    "gates.external"

  ]

  "cooldownMinutes": 10,

  "interimDamage": 0.0,

  "damageScale": {

    "1": 0.2,

    "3": 0.35,

    "6": 0.5,

    "12": 0.8,

    "48": 1

  }

}
````

The above configuration is intended to work as follows.


* First 10 minutes offline - 100% damage

* Between 10 and 60 minutes offline - 0% damage

* Between 1 and 3 hours offline - 20% damage

* Between 3 and 6 hours offline - 35% damage

* Between 6 and 12 hours offline - 50% damage

* Between 12 and 48 hours offline - 80% damage

* After 48 hours offline - 100% damage


**Cleaning House Option** 

With damageScale, this plugin may also potentially increase the damage beyond 100% after a certain amount of offline time, therein making it less expensive to raid.  For example, after 168 hours (1 week), raiders could do twice the damage to the structure.


This may be useful on pvp servers without decay to help clear out old bases.

	
	
````
"damageScale": {

    "1": 0.2,

    "3": 0.35,

    "6": 0.5,

    "12": 0.8,

    "48": 1,

    "168": 2

  }
````

Feedback is appreciated.  [AntiOfflineRaid | Oxide](http://oxidemod.org/threads/antiofflineraid.12824/)