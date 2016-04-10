This plugin allows you to create completely custom chat commands to display custom info messages.

**Important**

Be sure you don't create commands that are already used by other installed plugins! To use the helptext you need the [HelpText](http://oxidemod.org/resources/helptext.676/) plugin!

**Usage**

Everything is handled in the config file.

**Permission**

You can use Oxide's permissions to allow specific commands only to specific users. Set permission to false to allow everyone to use a command.

**Default config**

````
{

    "Settings": {

        "ChatName": "SERVER"

    },

    "ChatCommands": {

        "command2": {

            "text": [

                "This is an example text for admins only",

                "You can also use multiline messages"

            ],

            "helptext": "This is an example text",

            "permission": "admin"

        },

        "command1": {

            "text": [

                "This is an example text"

            ],

            "helptext": false,

            "permission": false

        }

    }

}
````


**Example config**

This creates 3 commands: /rules, /website and /givehelp

/givehelp can only be used by moderators and owners.

````
{

    "Settings": {

        "ChatName": "SERVER"

    },

    "ChatCommands": {

        "rules": {

            "text": [

                "1 - Dont cheat",

                "2 - No discrimination",

                "3 - Be nice"

            ],

            "helptext": "use /rules to display our rules",

            "permission": false

        },

        "website": {

            "text": [

                "Our website is www.example.com"

            ],

            "helptext": "use /website to get our homepage url",

            "permission": false

        },

        "givehelp": {

            "text": [

                "The syntax for /give is /give name item amount"

            ],

            "helptext": false,

            "permission": "admin"

        }

    }

}
````