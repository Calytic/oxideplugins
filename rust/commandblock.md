**Command Block** is a simple plugin that blocks configured console commands sent by a client to the server. This is useful for preventing users from suiciding, getting your server's map seed, and more.
**Configuration**

Most of the client to server console commands for Rust should work fine, just add them in the commandblock.json file under the oxide/config directory. You can see the available console commands at [Server commands for Rust | Oxide](http://forum.rustoxide.com/threads/6404/).

Do not include any value, only the base command itself. Keep in mind that this will only work with commands that are sent to the server. Client specific commands will not be blocked, as it is never sent to the server being as it is a purely a client-side command.
**Default Configuration**

````
{

  "Settings": {

    "ChatName": "SERVER",

    "Commands": [

      "kill",

      "server.seed"

    ]

  },

  "Messages": {

    "CommandBlocked": "Sorry, that command is blocked!"

  }

}
````

The configuration file will update automatically if there are new options available. I'll do my best to preserve any existing settings, message strings, etc with each new version.