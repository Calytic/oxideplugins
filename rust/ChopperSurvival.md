Survive as long as you can against waves of enemy helicopters that get stronger each round.

**Important Information**


* Gain hitpoints every time you shoot a helicopter, extra hitpoints for shooting rotors. The **winner** is either the last player standing, or the player with the most points.
* Earn (event manager) tokens by surviving rounds
* It is preferable to build the arena for this in a secluded area (island etc) to minimize lag
* The helicopters will not drop loot when they die
* The helicopters will get stronger every round using the stat modifier
* Players gear will be restocked after every round
* The amount of helicopters per round is calculated using the maximum amount of helicopters and the maximum amount of rounds
* Helicopter rockets can be turnt on/off in the config



**Setup for Chopper Survival**

* Create your arena
* Create a Zone around your Arena (Zone Manager)
* Create a spawn file (Spawns)
* Start a game (Event Manager)
**Game Options
**
Helicopter Stats - The base stats for the helicopter, these numbers are automatically adjusted as the rounds progress.
Death limit - The amount of lives a player has, when they run out they are kicked from the arena.
Friendlyfire ratio - 0 is no friendly fire, 1 is full damage
Scoring - Body hit & Rotor hit - This is the points the player gets for making shots (if players survive to the end this is what will determine the winner)
Scoring - Round survival tokens - The amount of (event manager) token points given to a player at the end of each round
Scoring - Winner tokens - The amount of bonus tokens given to the winner
Spawning - Maximum Helicopters - This sets the maximum amount of helicopters that can be spawned at once
Spawning - Maximum waves - How many waves of helicopters in each game
Print heli stats on spawn - This will print the currents stats of the helicopters to console when they spawn. Use this to make any adjustments

**Config
**

````

"Debug - Print heli stats on spawn (for balancing)": true,

  "Helicopter - Base Stats - Health": 3200.0,

  "Helicopter - Base Stats- Accuracy": 8.0,

  "Helicopter - Base Stats- Bullet damage": 3.0,

  "Helicopter - Base Stats- Main rotor health": 320.0,

  "Helicopter - Base Stats- Speed": 24.0,

  "Helicopter - Base Stats- Tail rotor health": 180.0,

  "Helicopter - Stat modifier": 1.15,

  "Helicopter - Use rockets": true,

  "Kit - Default kit": "cskit",

  "Player - Death limit": 10,

  "Player - FriendlyFire ratio": 0.0,

  "Player - Starting Health": 100.0,

  "Scoring - Body hit points": 1,

  "Scoring - Rotor hit points": 3,

  "Scoring - Round survival tokens": 1,

  "Scoring - Winner tokens": 10,

  "Spawning - Maximum Helicopters": 4,

  "Spawning - Maximum waves": 10,

  "Spawning - Spawn distance (away from arena)": 500.0,

  "Spawning - Wave timer (between rounds)": 10.0,

  "Spawns - Default spawnfile": "csspawns",

  "Zone - Zone name": "cszone"

 
````