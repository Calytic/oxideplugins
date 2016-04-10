**RGive** allows you to randomly give out items.
**-------------------------- [CHANGELOG v2.0.0] --------------------------**

* completely rewritten from scratch in C#

* completely changed config file

+ added console command support (RCon, Player Console & Server Console)

+ added more stuff, seen in the commands

+ the plugin now also gives the items with a random amount (Min and Max can be set in the config)

+ the plugin now works with th item categories found under **F1 -> Item List**

+ added blacklist for items which should not be given out randomly

- removed item list
**--------------------------**----------------------------------------------------**---**
**Permissions:**


* **rgive.use**


**Commands:**


* 
**/rgive player <playername>** give random item to specific player
* 
**/rgive item <itemname>** give specific item to random player
* 
**/rgive all **give random item to all players


**Console Commands are the same, just without the slash (/)

Default Configfile:

````
{

  "Categories": {

    "Ammunition": {

      "Enabled": true,

      "Maximal Amount": 64,

      "Minimal Amount": 5

    },

    "Attire": {

      "Enabled": true,

      "Maximal Amount": 2,

      "Minimal Amount": 1

    },

    "Construction": {

      "Enabled": true,

      "Maximal Amount": 5,

      "Minimal Amount": 1

    },

    "Food": {

      "Enabled": false,

      "Maximal Amount": 10,

      "Minimal Amount": 5

    },

    "Items": {

      "Enabled": true,

      "Maximal Amount": 5,

      "Minimal Amount": 1

    },

    "Medical": {

      "Enabled": true,

      "Maximal Amount": 5,

      "Minimal Amount": 1

    },

    "Misc": {

      "Enabled": false,

      "Maximal Amount": 5,

      "Minimal Amount": 1

    },

    "Resources": {

      "Enabled": false,

      "Maximal Amount": 10000,

      "Minimal Amount": 500

    },

    "Tool": {

      "Enabled": true,

      "Maximal Amount": 2,

      "Minimal Amount": 1

    },

    "Traps": {

      "Enabled": true,

      "Maximal Amount": 3,

      "Minimal Amount": 1

    },

    "Weapon": {

      "Enabled": true,

      "Maximal Amount": 2,

      "Minimal Amount": 1

    }

  },

  "Settings": {

    "Item Blacklist": [

      "autoturret",

      "mining.quarry",

      "mining.pumpjack",

      "cctv.camera",

      "targeting.computer"

    ]

  }
}
````


**
**Known issues:**


* none


**Future Updates:**


* **your suggestions**

Thanks to: 4seti and Mughisi for the first versions of this plugin