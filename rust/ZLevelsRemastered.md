This plugin, is remastered version of [ZLevels plugin](http://oxidemod.org/plugins/zlevels.1199/). It was long time since it was updated, and I had many crazy ideas about it, so I decided, that i should develop it on my own, taking ZLevels as base, because I really loved the work plugin developer did there.

**My plugin includes 4 skills:**


* Mining
* Woodcutting
* Skinning
* Crafting

**Any of them can be disabled via configuration file.**

All of those skills works kinda the same like in ZLevels, I've just introduced new skill called Crafting, which boosts your crafting speed depending on your Crafting skill level, to increase it, you must craft things obviously. The more time you spend on crafting, the more XP you get. Also when player reaches a point, where he has 100% faster crafting, all his items are instant crafted, if you craft more than 10 items at once, then items are [MagicCrafted](http://oxidemod.org/plugins/magic-craft.1347/), I've loved how this plugin solved that huge queue of "instantly crafted" items, by simple creating them from plugin.


I rewritten XP loss formula, so now you loose percentage of your current level XP instead of some fixed amount if you are level 10 and have 80% exp, and you should loose 60% exp on death, that means you'll be level 10 and 20% experience.

Every hour spent alive, you'll loss less experience when killed. For example if you set XPPercentToLoose to 100%, after 5 hours you'll only loose 50% instead of 100%, hours counts even when you are offline. There is also 10 mins grace period, that when you die within first 10 mins you don't loose any experience. You can check how much XP you'll loose by typing /stats.


The biggest thing I'm proud of that I've implemented user interface, which looks like this:

This makes you to avoid typing /stats every second and check how much XP is left till you level up and etc.


I also made this plugin to save stats into MySQL database, but it can also be saved into game files, just like the old plugin, since I always used MySQL, i did some quick rewriting to make usable for guys, who doesn't have MySQL server.


I'll write more information later, when i'll get some feedback and questions.

**Thanks to people who helped me testing:**


* leeter (Owner of Rusty Dallas server)
* meatcircus (Owner of 100PlaneAirdrops server)
* killsontact


**Incompatible plugins**:


* [Hunt RPG for Rust Experimental | Oxide](http://oxidemod.org/plugins/hunt-rpg.841/) (To make them work together, you must disable crafting in my plugin settings)



**Current commands are:**


* /stats - displays stats.
* /statsui - toggle's stats interface.
* /topskills - display's top player skills (only working if you are using MySQL)
* /statinfo [Woodcutting/Crafting/Mining/ Skinning] - Displays information about certain skill, including server configuration.


**You can disable certain skills, by setting LevelCaps value for that skill to -1.


Example of configuration file:**

````
{

  "CraftingDetails": {

    "PercentFasterPerLevel": 5,

    "TimeSpent": 1,

    "XPPerTimeSpent": 3

  },

  "dbConnection": {

    "Database": "db",

    "GameProtocol": 1336,

    "Host": "127.0.0.1",

    "Password": "password",

    "Port": 3306,

    "UseMySQL": false,

    "Username": "user"

  },

  "LevelCaps": {

    "C": 20,

    "M": 0,

    "S": 0,

    "WC": 0

  },

  "Messages": {

    "CSkill": "Crafting",

    "LevelUpText": "{0} Level up\nLevel: {1} (+{4}% bonus) \nXP: {2}/{3}",

    "MSkill": "Mining",

    "SSkill": "Skinning",

    "StatsHeadline": "Level stats (/statinfo [statname] - To get more information about skill)",

    "StatsText": "-{0}\nLevel: {1} (+{4}% bonus) \nXP: {2}/{3} [{5}].\n<color=red>-{6} XP loose on death.</color>",

    "WCSkill": "Woodcutting"

  },

  "PercentLostOnDeath": {

    "C": 50,

    "M": 100,

    "S": 50,

    "WC": 100

  },

  "PointsPerHit": {

    "M": 30,

    "S": 30,

    "WC": 30

  },

  "ResourcePerLevelMultiplier": {

    "M": 20.0,

    "S": 20.0,

    "WC": 20.0

  }

}
````


**If you are going to use MySQL here's a table this plugin use:**

````
CREATE TABLE `RPG_User` (

  `UserID` bigint(20) NOT NULL,

  `Name` varchar(50) DEFAULT NULL,

  `WCLevel` int(11) DEFAULT NULL,

  `WCPoints` bigint(20) DEFAULT NULL,

  `MLevel` int(11) DEFAULT NULL,

  `MPoints` bigint(20) DEFAULT NULL,

  `SLevel` int(11) DEFAULT NULL,

  `SPoints` bigint(20) DEFAULT NULL,

  `CLevel` int(11) DEFAULT NULL,

  `CPoints` bigint(20) DEFAULT NULL,

  `LastDeath` int(11) DEFAULT NULL,

  `LastLoginDate` int(11) DEFAULT NULL,

  `XPMultiplier` int(11) NOT NULL DEFAULT '100',

  PRIMARY KEY (`UserID`),

  UNIQUE KEY `UserID_UNIQUE` (`UserID`)

) ENGINE=InnoDB DEFAULT CHARSET=latin1;
````