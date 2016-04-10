[](http://forum.rustoxide.com/plugins/704/rate)
**Notifico** is a simple yet useful plugin that relays your server joins, quits, commands used by admin, and chat messages to the IRC world using the web to IRC bridge provided by [http://n.tkte.ch](http://n.tkte.ch).
**Initial Setup**

In order to use this plugin, there's a few steps you'll have to take to prepare.

* Register an account on http://n.tkte.ch.
* Login, and click on the button "New Project".

* Fill in a "Name" (this can be anything), uncheck "Public", and click "Create Project".

* Now that the "project" is created, click on the name of the project to open the configuration page for it.
* First we'll need to create a new hook URL that you'll be using in the plugin's configuration file. Click the "Create New Hook" button to get started.

* Select the "Plain Text" option, and then click "Create Hook".

* After you have created the hook, you'll need to add a new IRC channel and server; you do this by clicking on "Add New Channel".

* Enter your preferred IRC channel name and server, uncheck "Public" and click "Add Channel".

* Now that the site setup is completed, simply copy the "Hook URL" for the "Plain Text" hook you created, and paste that into the plugin's configuration file located under oxide/config/notifico.json.
* You can now enjoy chat messages, joins, and quits broadcasting to your IRC channel of choice! Enjoy!
**Configuration**

You can configure the general settings and messages in the notifico.json file under the oxide/config directory.
**Default Configuration**

````
{

  "Settings": {

    "AuthLevel": 2,

    "Exclusions": [

      "chat.say",

      "craft.add",

      "craft.cancel",

      "global.kill",

      "global.respawn",

      "global.respawn_sleepingbag",

      "global.status",

      "global.wakeup",

      "inventory.endloot"

    ],

    "HookUrl": "",

    "ShowChat": "true",

    "ShowCommands": "true",

    "ShowConnects": "true",

    "ShowConsoleCommands": "true",

    "ShowDisconnects": "true"

  },

  "Messages": {

    "Connected": "{player} has connected to the server",

    "Disconnected": "{player} has disconnected from the server"

  }

}
````

The configuration file will update automatically if there are new options available. I'll do my best to preserve any existing settings, message strings, etc with each new version.