Its a very simple plugin that can Execute many commands every (X) seconds! Also it's my first plugin and without the help of Reneb and Wulfspider i think i couldn't make this plugin.
**

How this plugin can be used:**


* If you want to throw 5 airdrops every 5 mins you can set in config (for the airdrop.massdrop 5 command i'm using [Airdrop Control](http://oxidemod.org/plugins/airdrop-controller.804/) from Reneb)


**NOTE(CONFIG): **For the TimerOnce configuration always at the end u must execute the command "reset.oncetimer" **ALSO **remember that this **TIMER IS WORKING UNTILL THE RESET COMMAND.

**NOTE2: You can see in my config the first command (for the TimerOnce) will get executed after 60 seconds so the second command will get executed after 30 seconds! (90-60=30) this timer is a chain so u have to add an additional seconds for the second command!


Default Config:****

````

{

  "EnableTimerOnceCommands": "true",

  "EnableTimerRepeatCommands": "true",

  "OnceCommands": [

    {

      "command": "say 'Restart in 1 minute'",

      "seconds": 60

    },

    {

      "command": "say 'Restart in 30 seconds'",

      "seconds": 90

    },

    {

      "command": "say 'Restart NOW'",

      "seconds": 120

    },

    {

      "command": "restart",

      "seconds": 120

    },

    {

      "command": "reset.oncetimer",

      "seconds": 121

    }

  ],

  "RepeaterCommands": [

    {

      "command": "server.save",

      "seconds": 300

    },

    {

      "command": "say 'hello world'",

      "seconds": 60

    }

  ]

}

 
````