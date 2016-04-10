Version v2.0 brings back the simplicity chat "boards", build informational boards so your server users  can read all the common info, like wipe dates, game updates or even tutorials on how to use some command, it's really up to you.

**[ DEFAULT COMMANDS ]**

**/info** - Displays the list of all available boards
**/info <board name>** - Displays the desired board
Click to expand...
**Note:** Take in mind these are the plugin's default commands, any command trigger can be changed in the configuration file, and in adition to this, it is also possible to use multiple triggers for each command.Click to expand...
**[ DEFAULT CONFIGURATION ]**
Code (Java):
````
{

  "BOARDS": {

    "Board Example Title": {

      "DESC": "Board Example Description",

      "LINES": [

        "Line # 1",

        "Line # 2",

        "Line # 3"

      ]

    },

    "Server Info": {

      "DESC": "Displays server detailed information",

      "LINES": [

        "<green>HOSTNAME: <end><silver>{server.hostname}<end>",

        "<green>DESCRIPTION: <end><silver>{server.description}<end>",

        "<green>IP: <end><silver>{server.ip}:{server.port}<end>",

        "<green>MAP: <end><silver>{server.level}<end>",

        "<green>LOCAL TIME & DATE: <end><silver>{localtime} {localdate}<end>",

        "<green>GAME TIME & DATE: <end><silver>{gametime} {gamedate}<end>",

        "<green>PLAYERS: <silver>{players} / {server.maxplayers} ({sleepers} sleepers)<end> SEED: <silver>{server.seed}<end> WORLD SIZE: <silver>{server.worldsize}<end><end>"

      ]

    }

  },

  "COLORS": {

    "PREFIX": "#00EEEE",

    "SYSTEM": "white"

  },

  "COMMANDS": {

    "BOARDS": [

      "info",

      "boards"

    ]

  },

  "CONFIG_VERSION": 2.0,

  "MESSAGES": {

    "AVAILABLE BOARDS": "AVAILABLE BOARDS",

    "BOARDS DESC": "<orange>/info [board name]<end> <grey>-<end> Displays the list of available boards, if given a name then it will display the desired board",

    "MULTIPLE BOARDS FOUND": "<#E85858>Multiple boards found with close to '{args}'<end>",

    "NO BOARDS AVAILABLE": "<#E85858>There are't any boards available<end>",

    "NO BOARDS FOUND": "<#E85858>No boards found with the name '{args}'<end>"

  },

  "SETTINGS": {

    "BROADCAST TO CONSOLE": true,

    "ENABLE BOARDS": true,

    "PREFIX": "<white>[ <lightblue>INFO BOARDS<end> ]<end>",

    "SHOW BOARD IN CHAT": true,

    "SHOW BOARD IN CONSOLE": false

  }
}
````



**[ USAGE NOTES ]**


* In order to use the **/help** command you must install **[Domestos](http://oxidemod.org/members/3412/)**'s [**Help Text**](http://forum.rustoxide.com/resources/help-text.676/) plugin.
* In order for any configuration changes take effect in game you must reload the plugin. Simply type **oxide.reload infoboards** in your server's console.
* Make sure you respect the configuration's file quotation marks, commas, braces, square brackets and line indentation, or else you may cause plugin malfunctions. For further help validate your file in [jsonlint.com](http://jsonlint.com)