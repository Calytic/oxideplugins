**Strike System **gives you the possibility to strike players & time-ban players with a specific amount of strikes.


Command arguments inside of square brackets [] are OPTIONAL!

**Admin Commands (permission: strikesystem.admin):**


* 
**/strike <player> [reason]** strike player
* 
**/strike wipefull** wipe all data
* 
**/strike wipe [only Active Strikes: true/false]** wipe all strikes
* 
**/strike info [player]** get strike info about a player
* 
**/strike remove <player> [amount] [only Active Strikes: true/false]** remove strikes of a player
* 
**/strike reset <player> [only Active Strikes: true/false]** reset players strikes


**Player Commands:**


* 
**/strike info** get own strike info


**Permissions:**


* strikesystem.admin


**Configfile:**


* 
````
{

  "Settings": {

    "Ban Time in Seconds": 86400,

    "Permanent Ban": false,

    "Strikes Until Ban": 3

  }
}
````