This is the Rust version of GTA5's Slasher game mode.

**The Game**

In slasher, 1 player spawns with a shotgun and flashlight, everyone else spawns with a torch. The objective for the players is to avoid the slasher for 2.5 minutes, at which time they will be given weapons and then will have 1.5 minutes to hunt the slasher. (Times are adjustable in the config)

If the slasher kills all the players, or if the slasher survives the whole round he wins.

The event will open at dusk and start at night fall. Rounds will continue throughout the night, night time will be extended depending on how long you set your rounds to go for and how many there are. Default setting is pretty much 1 standard night cycle.

**IMPORTANT INFORMATION BELOW! READ ALL OF IT**


- This event is automated providing you have it setup correctly, and will only run at night time (the dark is part of the game).

- When a player dies he is sent to the waiting area until the round finishes.

- A new slasher is chosen every round, once everyone has been the slasher it will start over.

- Torch's have a damage increase to level out the fact the slasher has a shotgun

- A good tip for this is to have a big arena, with hiding spots etc. Be creative with it

**Spawnfile Setup**

Create 2 spawn files, 1 for player spawns, and another to send dead players too until the round has finished

**Console Commands**
'slasher.spawnfile spawnfilename' - Sets the player spawn file for the game.

'slasher.deadspawnfile' - Sets the spawn file for the players who are killed and are waiting for the next round

'slasher.toggle' - Toggles Auto start on/off

**Config**

````
{


  "Skins - Player - Boots": 10044, // Skin Id's for each item.

  "Skins - Player - Pants": 10078,

  "Skins - Player - TShirt": 10039,

  "Skins - Slasher - Bandana": 10064,

  "Skins - Slasher - Boots": 10088,

  "Skins - Slasher - Pants": 10048,

  "Skins - Slasher - TShirt": 10038,

  "Slasher - AutoStart - Time event will end": 4.0,

  "Slasher - AutoStart - Time event will open": 18.0,

  "Slasher - AutoStart - Time event will start": 19.5,

  "Slasher - AutoStart - Use auto start": true, // Recommended

  "Slasher - Players - Friendly fire damage modifier": 0.0,

  "Slasher - Players - Starting health": 100.0,

  "Slasher - Players - Torch damage modifier": 2.2,

  "Slasher - Round Timers - Play timer (seconds)": 90, // Amount of time the players have to kill the slasher

  "Slasher - Round Timers - Slasher timer (seconds)": 150,// Amount of time the slasher has to kill the players before they get weapons

  "Slasher - Rounds to play per night cycle": 2,

  "Slasher - Spawnfile": "slasherspawns",

"Slasher - Dead player spawnfile": "deadplayerspawns",

  "Slasher - Weapon - Ammo type": "ammo.shotgun",

  "Slasher - Weapon - Slasher weapon shortname": "shotgun.pump",

  "Slasher - Zone name": "Slasher",

  "Tokens - On Win": 10,

  "Tokens - Per Kill": 1,

  "Tokens - Per Slasher Kill": 3

}
````