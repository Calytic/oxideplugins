**Sleep**  allows players with permission to get a well-rested sleep. While sleeping, players may grow hungry, get thirsty, and even get a bit dirty depending on settings.


By default, players will heal and restore 5 percent of their current levels every 10 seconds; and get dirty, hungry, and thirsty by 5 percent of their current levels every 10 seconds. Curing is also available, but disabled by default. Everything is changeable in the config file.

**Permissions** 

This plugin uses Oxide's permission system. To assign a permission, use **oxide.grant user <username|steamid> <permission>** . To remove a permission, use **oxide.revoke user <username|steamid> <permission>** .


* sleep.allowed (allows players to go to sleep)
Ex. oxide.grant user Wulf sleep.allowed
Ex. oxide.revoke user Wulf sleep.allowed
Ex. oxide.grant group player sleep.allowed

**Chat Command** 


* /sleep
Makes player go to sleep.

**Configuration** 

You can configure the settings and messages in the Sleep.json file under the server/identity/oxide/config directory.

**Default Configuration** 

````
{

  "Messages": {

    "CantSleep": "You can't go to sleep right now!",

    "ChatHelp": "Use '/sleep' to go to sleep and rest",

    "Dirty": "You seem to be a bit dirty, go take a dip!",

    "Hungry": "You seem to be a bit hungry, eat something!",

    "Rested": "You have awaken restored and rested!",

    "Thirsty": "You seem to be a bit thirsty, drink something!"

  },

  "Settings": {

    "Command": "sleep",

    "Cure": "false",

    "CurePercent": 5,

    "Heal": "true",

    "HealPercent": 5,

    "Realism": "true",

    "RealismPercent": 5,

    "Restore": "true",

    "RestorePercent": 5,

    "UpdateRate": 10

  }

}
````

The configuration file will update automatically if new options are added or removed. I'll do my best to preserve any existing settings and messages with each new version.