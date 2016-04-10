Simple plugin that prevents moderators (or owners depending on how you edit configuration) from spawning items via F1.

**Configuration:**

````

{

  "Admin": {

    "GodEnabled": false,

    "KickAdmin": false,

    "MaxLevel": 2,

    "MinLevel": 1,

    "OnlyMaxCanSpawn": true,

    "PrintToConsole": true

  },

  "Messages": {

    "Disabled": "Spawning items has been <color=red>disabled</color> by the server owner.",

    "NoGod": "God Mode has been <color=red>disabled</color> by the server owner.",

    "NoGodAllowed": "God Mode is not allowed, even for administrators."

  }

}

 
````