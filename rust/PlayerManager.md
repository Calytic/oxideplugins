It's in beta test, just to see how it works, i forgot to do the configs, but i'll do them soon enough

**Features:**

- Show all online players

- Show all sleeping players

- Show all all time players (Need Player Database for this)

- Show all banned players (Enhanced ban system & Rust banlist)

- Teleport to a player

- Ban a player => you must specify a reason in the check box

- Kick a player => you must specify a reason in the check box

- Mute / unmute a player (need chatmute for this)

- See information about this player: played, first connection, last seen (You need Player Information for this)
**Permissions:**

Use the GUI: playermanager.gui

Use the GUI Kick: playermanager.kick

Use the GUI Ban: playermanager.ban

Use the GUI To Teleport: playermanager.tp

Use the GUI To See IPS: playermanager.ips
**Commands:**

/playermanager => open the player manager

/playermanager NAME => search for a player in the SEARCH section

Add your own commands:

This is a default example to use with chat mute:

````
"External Commands": [

    {

      "commands": [

        {

          "cmd": "player.mute {steamid}",

          "color": "1 0 0 0.4",

          "text": "mute"

        },

        {

          "cmd": "player.unmute {steamid}",

          "color": "0 1 0 0.4",

          "text": "unmute"

        }

      ],

      "name": "Mute",

      "permission": "canmute"

    }

  ]
````

Imagine you would want to add like troll chat things you could do something like:

````
"External Commands": [

    {

      "commands": [

        {

          "cmd": "player.mute {steamid}",

          "color": "1 0 0 0.4",

          "text": "mute"

        },

        {

          "cmd": "player.unmute {steamid}",

          "color": "0 1 0 0.4",

          "text": "unmute"

        }

      ],

      "name": "Mute",

      "permission": "canmute"

    },

    {

      "commands": [

        {

          "cmd": "say {name} is drunk",

          "color": "0 1 0 0.4",

          "text": "drunk"

        },

        {

          "cmd": "say {name} is gay",

          "color": "0 1 0 0.4",

          "text": "gay"

        },

        {

          "cmd": "player.kill {steamid}",

          "color": "0 1 0 0.4",

          "text": "kill"

        },

      ],

      "name": "Fun",

      "permission": "canfun"

    },

  ]
````

This player.kill i dont even know if it exists, but it's just an example 
**Configs:**

````
{

  "External Commands": [

    {

      "commands": [

        {

          "cmd": "player.mute {steamid}",

          "color": "1 0 0 0.4",

          "text": "mute"

        },

        {

          "cmd": "player.unmute {steamid}",

          "color": "0 1 0 0.4",

          "text": "unmute"

        }

      ],

      "name": "Mute",

      "permission": "canmute"

    }

  ],

  "Permission - GUI": "playermanager.gui",

  "Permission - GUI - Ban": "playermanager.ban",

  "Permission - GUI - IPs": "playermanager.ips",

  "Permission - GUI - Kick": "playermanager.kick",

  "Permission - GUI - TP": "playermanager.tp"

}
````