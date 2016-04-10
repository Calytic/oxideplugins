**Economy** adds an economy system to the game.

**NOTE: **This plugin just adds the possibility to HAVE money. It does NOT add a possibility to get or pay with money. I am hoping to have other developers use this to make plugins like a shop.


Fun Fact: The plugin has exactly 500 lines of code.

**Plugins for Economy:**


* [Playtime Money for Hurtworld | Oxide](http://oxidemod.org/plugins/playtime-money.1610/)
* [HurtStore for Hurtworld | Oxide](http://oxidemod.org/plugins/hurtstore.1646/)


**Permissions:**


* 
**economy.admin **for all admin commands


**How to grant permissions:**

Use following CHAT commands to grant permissions


* 
**/grant user <player> <permission>** grant permission to player
* 
**/grant group <group> <permission>** grant permission to group


**Chat Commands:**


* 
**/money **show all available commands
* 
**/money balance** show your balance
* 
**/money balance <player>** show balance of another player
* 
**/money transfer <player> <amount> **transfer money from yourself to somebody else
* 
**/money transfer <player> <target> <amount> **transfer money from one player to another **(Admin Only!)**
* 
**/money add <player> <amount>** add money to a player's account **(Admin Only!)**
* 
**/money substract <player> <amount>** substract money from a player's account **(Admin Only!)**


**Config file:**

````
{

  "Settings": {

    "Command": "money",

    "Default Amount": 100.0

  }
}
````


**Language file:**

````
{

  "No Permission": "You don't have permission to use this command.",

  "Invalid Amount Argument": "Invalid amount argument! Amount must be a number!",

  "Too Low Amount": "The amount must not be below 0!",

  "Not Enough Money": "You do not have enough money to do that!",

  "Balance": "Your Balance: {balance}$",

  "Admin Balance": "{player}'s Balance: {balance}$",

  "Command Description - Balance": "show your balance",

  "Command Description - Admin Balance": "show balance of a player",

  "Command Description - Transfer": "transfer money to another player",

  "Command Description - Admin Transfer": "transfer money from one player to another",

  "Command Description - Add": "give money to a player",

  "Command Description - Substract": "substract money from a player",

  "Admin Transfer - To Admin": "{amount}$ from {player} was given to {target}.",

  "Admin Transfer - To Player": "{amount}$ from {player} was given to you by {admin}.",

  "Admin Transfer - To Target": "{amount}$ from you was given to {target} by {admin}.",

  "Transfer - To Player": "{player} has transfered {amount}$ to you.",

  "Transfer - To Target": "You have transfered {amount}$ to {target}.",

  "Add - To Admin": "You have added {amount}$ to {target}'s account.",

  "Add - To Target": "{admin} has added {amount}$ to your account.",

  "Substract - To Admin": "You have substracted {amount}$ from {target}'s account.",

  "Substract - To Target": "{admin} has substracted {amount}$ from your account."
}
````