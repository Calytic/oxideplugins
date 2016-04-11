[\/\/\/\/\/\/\/\/\/\/\/\/\/\/\


Requires: **Player Database !**](http://oxidemod.org/plugins/player-database-mysql-support.927/)


/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\

**If you update from 1.0.0 or lower you need to restart server** **before plugin works**


This plugin will check if player has voted at toprustservers.com, rust-server.net or rust-serverlists.net


If player has voted this plugin will give him reward points, which can be used to claim different rewards.

**Commands**

/vote                       -         Shows a help message to user
/vote check            -        Check if player has voted | Sends message if yes/or and adds points    -

/reward                 -      List all rewards and your current points
/reward *name*  -      Tries to claim named reward

**How to Install**


Just drop it to plugins folder

Change API key in config to your own

**rslAPIKey** = Rust-serverlist.net ApiKey
**rslServerID** = Your server id at Rust-serverlist.net
**rssAPIKey** = rust-servers.net ApiKey
**trsAPIKey** = toprustservers.com ApiKey

**How to change rewards**


You Add/Modify kits on its config file. [http://jsoneditoronline.org/](http://jsoneditoronline.org/) is a handy tool to check if JSON is valid.

**Default Config** - vote.json

````

{

  "Messages": {

  "Broadcast": "[color #ff0000]You don't have enough points! Need: ",

  "HelpMessage": "Vote us at http://toprustservers and earn some sweet voter rewards today!",

  "HelpMessage2": "type [color cyan]/vote check[/color] to claim your points and [color cyan]/reward[/color] to see all possible rewards",

  "NotEnoughpoints": "[color #ff0000]You don't have enough points! Need: ",

  "rslAlreadyVoted": "You have already voted us at Rust-serverlist.net! (You can vote every 24h)",

  "rslHasVoted": "Thanks for voting us at Rust-serverlist.net",

  "rslNotVoted": "You have not voted us yet at Rust-serverlist.net",

  "rssAlreadyVoted": "You have already voted us at Rust-servers.net! (You can vote every 24h)",

  "rssHasVoted": "Thanks for voting us at Rust-servers.net",

  "rssNotVoted": "You have not voted us yet at Rust-servers.net",

  "trsHasVoted": "Thanks for voting us at TopRustServers",

  "trsNotVoted": "You have not voted us yet at TopRustServer"

  },

  "PointsPerVote": 1,

  "Rewards": {

  "guns": {

  "desc": "Go shoot your enemies",

  "items": {

  "a": {

  "Amount": 1,

  "ItemName": "9mm Pistol"

  },

  "b": {

  "Amount": 100,

  "ItemName": "9mm Ammo"

  }

  },

  "name": "guns",

  "price": 1

  },

  "materials": {

  "desc": "Material pack (100 x WoodPlanks and 50 x Low Quality Metal)",

  "items": {

  "a": {

  "Amount": 100,

  "ItemName": "Wood Planks"

  },

  "b": {

  "Amount": 50,

  "ItemName": "Low Quality Metal"

  }

  },

  "name": "materials",

  "price": 1

  },

  "supply": {

  "desc": "Your very own Supply Signal",

  "items": {

  "a": {

  "Amount": 1,

  "ItemName": "Supply Signal"

  }

  },

  "name": "supply",

  "price": 1

  }

  },

  "rslAPIKey": "xxx",

  "rslServerID": "",

  "rssAPIKey": "xxx",

  "trsAPIKey": "xxx"

}

 
````


**Todo:**

Configurable messages

Code cleaning/optimizing

Command to add points manually to players (Admin only)