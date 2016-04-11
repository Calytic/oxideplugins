This plugin executes **server **commands when a player is entering a zone that was set via ZonesManager plugin.

**How to use it:**


* 
**/zcmd add <zoneID> [once/perplayer] <command>** — Adds one command to a specific zone.
* 
**/zcmd add <zoneID> [once/perplayer] [command1, command2, ...]** — Adds multiple commands to a specific zone.
* 
**/zcmd remove <zoneID>** — Removes zone with commands
* 
**/zcmd list** — Shows list of all zones and their commands
* 
**/zcmd clear** — Clears data file (Deletes all zones)
* 
**/zcmd vars** — Shows all available variables



**Once/preplayer argument is optional:**


* **once **- executes commands only once.
* **perplayer **- executes commands once for every player that has entered in zone.
* Empty argument will mean that every time that a player enters a zone, commands will execute


You can use this variables in your commands:

````
$player.name - Nickname

$player.id - Steam ID

$player.x, $player.y, $player.z - position
````


**Examples:**

````
/zcmd add 12345 say Hello, $player.name!

/zcmd a 54321 once [say Rain of fire is activated! Boop!, rof.random]

/zcmd add myzone perplayer [say There is your kit!, inv.giveplayer $player.name kit starter]
````

Short commands: add = a, remove = r, list = l, clear = c, vars = v.

Short arguments: once - o, perplayer - pp