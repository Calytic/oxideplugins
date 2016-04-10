This plugin allows you to change metabolism settings such as health, calories and water.
**Usage**

Simply edit the config to your needs, there are no chat commands.
**Rusts default values**

LossRate Calories: 0 - 0.05

LossRate Hydration: 0 - 0.025

GainRate Health: 0.03

SpawnValue: random between 50 and 200
**Rates and SpawnValues can be set to "default" to use the default game settings, otherwise only use numbers without brackets!**
**Default config**

````
{

  "Settings": {

    "Hydration": {

      "LossRate": "default",

      "MaxValue": 1000,

      "SpawnValue": "default"

    },

    "Calories": {

      "LossRate": "default",

      "MaxValue": 1000,

      "SpawnValue": "default"

    },

    "Health": {

      "GainRate": "default",

      "MaxValue": "100,

      "SpawnValue": "default"

    }

  }

}
````


**LossRate** - The rate at which players lose calories and water. Rust's default is between 0 and 0.025.
**MaxValue** - The max value.
**SpawnValue** - The value players have when they spawn (connect and when spawned).
**GainRate** - The rate at which players regenerate HP. Rust's default is 0.03.