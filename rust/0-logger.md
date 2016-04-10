[](http://forum.rustoxide.com/plugins/670/rate)
**Logger** is a plugin that logs all chat commands used by players with auth level to the server log and/or chat if desired. Each type of logging can be enabled/disabled via the configuration file.
**Configuration**

You can configure the chat command, messages, and other settings in the logger.json file under the oxide/config directory.
**Default Configuration**

````
{

  "Settings": {

    "AuthLevel: 2,

    "Broadcast": "true",

    "ChatLogging": "false",

    "CommandLogging": "true",

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

    ]

  }

}
````

The configuration file will update automatically if there are new options available. I'll do my best to preserve any existing settings, message strings, etc with each new version.