This is a simple plugin that allows you to send some basic information to the in-game chat. Each function of the plugin can be disabled or enabled through the configuration file, by default all the components are enabled.


The plugin consists of 3 components:

- Join/Leave messages: Each time a player joins or leaves a message is printed in the chat for everyone to see.

- Broadcasts: Allows you to broadcast a line of text to all players on a pre-defined interval.

- Rules: Adds a /rules command which shows all the set rules to the player using the command.


Also because I named the plugin Information Announcer I've added a /version command that prints the current Oxide version and RoK version in chat.

**Using the ExcludePlayers option**

Since version 1.1 it is possible to hide the join/leave messages for specific players, to do this you simply add their SteamId to this option in the Announcer.json configuration file.

Example:

````
...

  "ConnectionSettings": {

    "Enabled": true,

    "ExcludePlayers": [ "76561198112743227", "76561198112743228" ],

    "Log": true,

    "ShowChatPrefix": true,

    "ShowJoinMessages": true,

    "ShowLeaveMessages": true

  },

...
````


**Using custom commands**

In version 1.1 the option to add custom commands to display information in the chat to a player using that command has been added, you simply add your custom command with the message you want it to display to the config file.

Example:

````
...

  "CustomCommands": {

    "Commands": {

        "website": [ "You can visit our website at http://www.oxidemod.org" ],

        "donate": [ "We are currently accepting any donations made to use so that we can keep the server running", "You can donate on our PayPal account .....", "Or you could send us BitCoins to ....." ]

    },

    "ShowChatPrefix": true

  },

...

 
````


**Default config**

````
{

  "BroadcasterSettings": {

    "Enabled": true,

    "Interval": 120,

    "Messages": [

      "You can visit our forums at www.oxidemod.org!",

      "Don't forget to bring your friends!",

      "Type /rules to see our server's rules!"

    ],

    "ShowChatPrefix": true,

    "ShowInRandomOrder": false

  },

  "ConnectionSettings": {

    "Enabled": true,

    "ExcludePlayers": [],

    "Log": true,

    "ShowChatPrefix": true,

    "ShowJoinMessages": true,

    "ShowLeaveMessages": true

  },

  "CustomCommands": {

    "Commands": {},

    "ShowChatPrefix": true

  },

  "Messages": {

    "PlayerJoined": "{0} has joined the server!",

    "PlayerLeft": "{0} has left the server"

  },

  "RulesSettings": {

    "Enabled": true,

    "Rules": [

      "1. Do not cheat on our server.",

      "2. Speak English in chat at all times.",

      "3. When Mughisi says jump, you ask how high.",

      "Not complying with these rules may result in a ban at any given time."

    ],

    "ShowChatPrefix": true

  },

  "Settings": {

    "ChatPrefix": "Server",

    "ChatPrefixColor": "950415"

  }

}
````