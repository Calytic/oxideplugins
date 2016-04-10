**Respawner** is a plugin that automatically respawns players after they die, either at the same location where they died, one of their sleeping bags, custom spawn, or a random location. You can set players to automatically wake up via the configuration as well. To use a custom spawn location, set other location options to "false" and set "CustomSpawn" to something such as "10, 12, -14" or similar.
**Configuration**

You can configure the settings and messages in the Respawner.json file under the server/identity/oxide/config directory.
**Default Configuration**

````
{

  "Messages": {

    "CustomSpawn": "You've respawned at {location}",

    "SameLocation": "You've respawned at the same location",

    "SleepingBag": "You've respawned at your sleeping bag"

  },

  "Settings": {

    "AutoWakeUp": "true",

    "CustomSpawn": "false",

    "SameLocation": "false",

    "ShowMessages": "true",

    "SleepingBags": "false"

  }

}
````

The configuration file will update automatically if new options are added or removed. I'll do my best to preserve any existing settings and messages with each new version.