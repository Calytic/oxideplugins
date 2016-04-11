Once this event has started, no one will be able to enter (/event_join) If a player dies in the event, gets Eeliminated and kicked from the event. The last player alive wins. Earn Tokens (Event Manager) for killing, surviving and winning! That's all! ^^ And as I said **Nobody **can enter the event once started!

**EVENT SETUP**

To setup the event, type the following commands in the server console:


event.game "Last Man Standing" (Sets Last Man Standing as next event)

event.kit "kitname" (Set the kit as game kit)

event.maxplayers xx (Replace xx by number of max players)

event.spawnfile "spawnfile name" (Select spawnfile to be used in the event)

event.open (Open the event so players can join)

event.start (Start the event, No body will be able to enter the event from this moment)

event.end (End the event manually (It auto ends when there is only 1 player remaining!) Type this command only to force event to stop!)
**
ZONE SETUP


Creating Grounds:**
1) Create 1 ground or more

2) You will need to create your zones on your own.

So I recommend knowing how to use Zone Manager!!

3) You will need "spawnfile" and "kits".

Spawnfile is where all the spawns are stored for the battle ground. (Create it with /spawns_new and add spawns to that file with /spawns_add. Save it with /spawns_save "name")

All players will have the SAME kit!

**Zone Management:**
Exemple zone:

- /zone_add LMS

- /zone_edit LMS

- /zone radius 50 undestr true nobuild true nodeploy true nocorpse true notp true noremove true nokits true

**COMMANDS
**
Chat:

/lms stats: See your stats in Last Man Standing Events


/lms reset: Reset all players stats!
Console: 


lms.reset: Reset all players stats!

**PRMISSIONS
**
lastmanstanding.admin: Let the player with this permission reset stored data with /lms reset

**TODO**: TOP Stats, ranking and suggetions!


Default Config:

````
{

  "Options - Default kit": "lmskit",

  "Options - Default spawnfile": "lmsspawns",

  "Options - Zone name": "lmszone",

  "Player - Starting Health": 100.0,

  "Scoring - Tokens given to alive players when a players gets eliminated": 1,

  "Scoring - Tokens given to player when killing another player": 2,

  "Scoring - Winner tokens": 5

}
````

Default Lang File:

````
{

  "noEvent": "Event plugin doesn't exist",

  "statsreset": "<color=orange>All Players Stats were Reset!</color>",

  "statsresetconsole": "All Players Stats were Reset!",

  "noConfig": "Creating a new config file",

  "title": "<color=orange>Last Man Standing</color> : ",

  "titleconsole": "Last Man Standing: ",

  "noPlayers": "Last Man Standing has no more players, auto-closing.",

  "openBroad": "Kill other players to survive! Last Player Standing Wins!",

  "eventWon": "{0} has won the event!",

  "eventDeath": "{0} has died!",

  "noperm": "<color=red>You don't have permission to run this command!</color>",

  "notEnough": "Not enough players to start the event",

  "tokensadded": "You got {0} Tokens for surviving!",

  "tokenswin": "You got {0} Tokens for winning!",

  "started": "Event has started! Last player alive wins!",

  "playersremaining": "{0} Players remaining!",

  "stats0": "<size=25><color=orange>----Last Man Standing Stats----</color></size>",

  "stats4": "Stats from: <color=#FFAA00>{0}</color>",

  "stats1": "Total Games Won: <color=#FFAA00>{0}</color>",

  "stats2": "Total Kills: <color=green>{0}</color>",

  "stats3": "Total Deaths: <color=red>{0}</color>"

}
````

MY SERVER (THAT USES THIS PLUGIN):
[[ES/EU] ThePurge | x10(+Levels) | Kits | Homes/TP | Money/Jobs | Active Admin | Airdrops | +++ | Rust server](http://rust-servers.net/server/52041)