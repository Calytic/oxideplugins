Use teamwork to kill as many enemy players as possible and secure a victory for your team. The winning team will be issued with reward points (see event manager rewards system)

**Setup TDM**

* Create your arena! Build somewhere secluded or wall off a piece of land, I like to use islands.
* Create the kit that the players will use (Kits)

Add anything you want to the kit besides any form of t-shirt or jacket.

Save the kit as 'tdmkit', this is the default kit name
* Create a Zone around your arena (Zone Manager)
* Now you will need to create 2 spawn files, 1 for each team (Spawns)

Save the individual spawn files as 'tdmspawns_a' and 'tdmspawns_b' these are the default spawn file names.
* Start a game like you would with any other arena! (Event Manager)
**Console Commands**


* tdm.spawns.a "spawnfilename" - Changes Team A Spawnfile
* tdm.spawns.b "spawnfilename" - Changes Team B Spawnfile
* tdm.kills "XX" - Set the number of kills to win the match, can be done during a round.
* tdm.team "playername" "a -or- b" - Changes a players team to either A or B


**Editing the Scoreboard UI
**

There are options to modify the ScoreboardUI in the config. To change the color values you must use colors from [Color Values - PyMOLWiki](http://pymolwiki.org/index.php/Color_Values)

If you change these colors make note that in the config there are 4 numbers. The first 3 make the color and the last one is the alpha. Leaving it at 1

ex. Color from site "0.3 0.1 0.8", this must become " 0.3 0.1 0.8 **1.0**" in the config


All units are in percentage of screen size, 0.0 being 0% and 1.0 being 100%

PositionX is the percentage of screen from the left to the side of the scoreboard

PositionY is the percentage of screen from the bottom to the bottom of the scoreboard

Dimensions are the width and height of the scoreboard

** IF YOUR VALUES EQUAL MORE THAN 1 IT WILL NOT WORK
ex. 'positionX 0.80, dimensionX  0.25 == 1.05' -- Wrong!
**

Configuration**

````

{

  "Scoreboard - Colors - Team A kills": "0.0 0.788235294 0.0 1.0",

  "Scoreboard - Colors - Team B kills": "0.0 0.5 1.0 1.0",

  "Scoreboard - Colors - Total kills": "0.698 0.13 0.13 1.0",

  "Scoreboard - GUI - Dimensions X": 0.13,

  "Scoreboard - GUI - Dimensions Y": 0.13,

  "Scoreboard - GUI - Position X": 0.82,

  "Scoreboard - GUI - Position Y": 0.78,

  "Scoreboard - GUI - Text size": 20,

  "Scoreboard - Text - Max kills": "Kill Limit : ",

  "Scoreboard - Text - Team A kills": "Team A : ",

  "Scoreboard - Text - Team B kills": "Team B : ",

  "TeamDeathmatch - Options - Friendlyfire damage ratio": 0.0,

  "TeamDeathmatch - Options - Kills to win": 10,

  "TeamDeathmatch - Options - Kit": "tdm_kit",

  "TeamDeathmatch - Options - Start health": 100.0,

  "TeamDeathmatch - Options - Zone name": "tdm_zone",

  "TeamDeathmatch - Team A - Color": "#33CC33",

  "TeamDeathmatch - Team A - Shirt": "tshirt",

  "TeamDeathmatch - Team A - Skin": 0,

  "TeamDeathmatch - Team A - SpawnFile": "tdmspawns_a",

  "TeamDeathmatch - Team B - Color": "#003366",

  "TeamDeathmatch - Team B - Shirt": "tshirt",

  "TeamDeathmatch - Team B - Skin": 14177,

  "TeamDeathmatch - Team B - SpawnFile": "tdmspawns_b",

  "Tokens - On Win": 5,

  "Tokens - Per Kill": 1

}

 
````

Thanks to [@Reneb](http://oxidemod.org/members/20031/), this is a modified version of his Deathmatch plugin, and also for the assistance with the spawning issue.