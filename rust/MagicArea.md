Magic Area is a simple area system that uses a single point and a radius.



**Current Features:**

* Entities and structures expire after a certain amount of time (you decide).

* Auto-kit upon enter of area, inventory reset upon leave.

* No researching allowed within area (if chosen).

* Respawn inside area if killed inside.

* and much more...

**Commands:**

area.create (VIA F1 console, ingame)

**Planned Features:**

In depth spawn configuration.

Language integration for messages.

Commands/methods to edit data without unloading and reloading plugin.

**Hooks (Called Upon Enter/Exit):**

````
OnPlayerExitMagicArea(BasePlayer player, int area)

OnPlayerEnterMagicArea(BasePlayer player, int area)
````


**Other Hooks:**

````
CreateMagicArea(Vector3 position, float radius, string title = "-1", string description = "-1", bool enabled = true)

MagicAreaExists(int id)
````


**Default Configuration:**

````
{

  "Admin": {

    "MaxLevel": 2

  },

  "Settings": {

    "Debug": true,

    "DefaultExpire": 10800,

    "TimerInterval": 1

  }

}

 
````


**Example Area (Will give kit "supplies"):**

````

  "Areas": {

    "1723811619": {

      "iID": 1723811619,

      "tTitle": "Practice Zone",

      "tDescription": "Practice your mad skills.",

      "fMinX": 179.981812,

      "fMinY": 3.15574741,

      "fMinZ": 2.85190845,

      "fRadius": 50.0,

      "uEnabled": true,

      "bGod": false,

      "bResetInv": true,

      "iCount": 0,

      "tKit": "supplies",

      "bCanResearch": false,

      "bRemoveEntities": true,

      "iEntityExpire": 20,

      "Spawns": {}

    }
````

This is a work in progress and may have bugs, I hope to build this plugin up with ideas from users.