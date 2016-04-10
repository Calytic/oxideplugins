A complex ranking system for various stats of the players from PVP kills, bullets fired, resources gathered and many, many more. It's dynamic system is designed to rank any of these stats, so players can know in detail who is on top of each statistics.

**[ FEATURES ]**


* 
**VARIOUS PLAYER STATISTICS** - From PVP kills, bullets fired, resources gathered and many, many more
* 
**DYNAMIC TOP LISTS** - Known who is leading any statistic using a simple chat command
* 
**DATABASE AUTO-SAVE** - Automatically saves the database every X minutes
* 
**ADMIN COMMANDS** - Save or reset the whole database with simple commands, or delete users manually
* 
**LISTS RESTRICTIONS** - Restrict lists for players or admins



**[ DEFAULT COMMANDS ]**

**/resetme** - Resets the data of the user
**/top [stat list]** - Shows a ranked list of a certain statistic
**/top [lists]** - Prints all available lists
**/rankme** - Displays the plugin information
**/rankme <save>** - Admin command to save the database manually
**/rankme <reset>** - Admin command to reset the database
**/rankme <del> <playername>** - Admin command to manually reset a player data, even if the player is off-line
Click to expand...
**Note:** Take in mind these are the plugin's default commands, any command trigger can be changed in the configuration file, and in adition to this, it is also possible to use multiple triggers for each command.Click to expand...
**[ DEFAULT CONFIGURATION ]**
Code (Java):
````
{

  "COLORS": {

    "PREFIX": "#00EEEE",

    "SYSTEM": "white"

  },

  "COMMANDS": {

    "PLAYER RESET": "resetme",

    "RANK": "rank",

    "TOP": "top"

  },

  "LISTS": {

    "ANIMALS": "Animal Kills",

    "ARROWS": "Arrows Fired",

    "BARRELS": "Barrels Destroyed",

    "BPREVEALED": "Blueprints Revealed",

    "BUILT": "Structures Built",

    "BULLETS": "Bullets Fired",

    "CRAFTED": "Items Crafted",

    "DEATHS": "Deaths",

    "DEMOLISHED": "Structures Demolished",

    "DISTANCE": "Distance",

    "EXPLOSIVES": "Explosives Used",

    "HEALED": "Times Healed",

    "HELIS": "Helicopters Destroyed",

    "KDR": "Kill/Death Ratio",

    "NPCKILLS": "Human NPC Kills",

    "PVEDISTANCE": "PVE Distance (In Meters)",

    "PVPDISTANCE": "PVP Distance (In Meters)",

    "PVPKILLS": "PVP Kills",

    "REPAIRED": "Structures Repaired",

    "RESOURCES": "Resources Gathered",

    "ROCKETS": "Rockets Fired",

    "SDR": "Suicides/Kills Ratio",

    "SLEEPERS": "Sleepers",

    "SUICIDES": "Suicides",

    "TURRETS": "Auto-Turrets Kills",

    "UPGRADED": "Structures Upgraded",

    "WOUNDED": "Times Wounded"

  },

  "MESSAGES": {

    "ADMIN TELL RESET": "{player} data has been reset.",

    "AVAILABLE LISTS": "Available Lists (<lime>{total}<end>)",

    "CHECK CONSOLE NOTE": "Check the console (press F1) for more info.",

    "DATA RESET": "Database has been reset.",

    "DATA SAVED": "Database has been saved.",

    "LIST NOT FOUND": "No list found with that name",

    "LIST RESTRICTED": "<lime>{list}<end> is restricted by the Admins!",

    "MULTI PLAYERS FOUND": "Found multiple players with that name.",

    "MULTIPLE LISTS FOUND": "Found multiple lists close to the given name.",

    "NO PLAYERS FOUND": "No Players found with that name.",

    "NO PLAYERS TO LIST": "There are no valid players to list on Top <lime>{list}<end>",

    "PLAYER DATA RESET": "Your stats have been reset.",

    "PLAYER RESET DESC": "<orange>/resetme<end> <grey>-<end> Allows player to reset own data",

    "RANK DESC": "<orange>/rank<end> - Shows player stats information",

    "RANK INFO": "Your Personal Stats",

    "TOP DESC": "<orange>/top [list name]<end> <grey>-<end> Shows the Top PVP Kills, or any other list like Deaths, KDR, etc.",

    "TOP TITLE": "Top {list}",

    "TOP3 ADVERT": "<lime>{list}<end> Top 3: <lightblue>{top}<end>"

  },

  "SETTINGS": {

    "ANNOUNCE DATABASE RESET": true,

    "AUTO-SAVE INTERVAL": 30,

    "BROADCAST TO CONSOLE": true,

    "ENABLE AUTO-SAVE": true,

    "ENABLE PLAYER RESET": true,

    "ENABLE RANK": true,

    "ENABLE RANK WHITELIST": false,

    "ENABLE RESET DATABASE": true,

    "ENABLE TOP": true,

    "ENABLE TOP3 ADVERT": true,

    "LISTS MAX PLAYERS": 5,

    "PREFIX": "<white>[ <orange>RANK-ME<end> ]<end>",

    "RANK WHITELIST": [

      "pvpkills",

      "deaths",

      "kdr",

      "bullets"

    ],

    "RESTRICTED TO ADMINS": [],

    "RESTRICTED TO PLAYERS": [

      "sdr",

      "barrels"

    ],

    "SHOW RANK IN CHAT": true,

    "SHOW RANK IN CONSOLE": false,

    "SHOW TOP IN CHAT": true,

    "SHOW TOP IN CONSOLE": false,

    "TOP3 ADVERT INTERVAL": 15,

    "USE SEPARATOR LINES": false

  },

  "STRINGS": {

    "NAME": "Name",

    "RANK": "Rank Position"

  }
}
````

Code (Java):
````
...
"RESTRICTED TO ADMINS": ["pvpkills","deaths","kdr"],
"RESTRICTED TO PLAYERS": ["sdr","barrels"],

...
````



**[ USAGE NOTES ]**


* In order to use the **/help** command you must install **[Domestos](http://oxidemod.org/members/3412/)**'s [**Help Text**](http://forum.rustoxide.com/resources/help-text.676/) plugin.
* In order for any configuration changes take effect in game you must reload the plugin. Simply type **oxide.reload rankme** in your server's console.
* Make sure you respect the configuration's file quotation marks, commas, braces, square brackets and line indentation, or else you may cause plugin malfunctions. For further help validate your file in [jsonlint.com](http://jsonlint.com)