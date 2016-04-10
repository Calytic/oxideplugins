**EventBox API** allows you to set up spots for boxes which other plugins can use.

**Chat Commands:**


* 
**/eventbox add <name> **- to add a box spot to specific name (arena for example) at your current location
* 
**/eventbox build <name> **- to build all boxes from specific name (arena for example)
* 
**/eventbox destroy <name> **- to destroy all boxes from specific name (arena for example)


**Permissions:**


* **eventbox.use**


**

Default Config:**

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

    ],

    "Max Items": 8,

    "Min Items": 1

  }
}
````


**For Developers:**

Usable Methods & Variables:


* List<string> names
* void AddBox(string name, Vector3 vector)     
* BaseEntity BuildBox(string name, Location location, bool items)
* string BuildBoxes(string name)
* string DestroyBoxes(string name)