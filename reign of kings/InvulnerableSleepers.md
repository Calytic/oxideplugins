This plugin will offer sleeper protection with a slight delay so that it can not be abused by players by just logging off when they are in danger.


Eventually the plugin will also offer admin commands to check all sleepers, and teleport to them.

**Default config:**

````
{

  "Messages": {

    "Notification": "You can't deal damage to sleepers."

  },

  "Options": {

    "AttackablePeriodInSeconds": 120,

    "GracePeriodInSeconds": 15,

    "NotifyAttacker": true,

    "TimeBetweenNotificationsInSeconds": 30

  },

  "Settings": {

    "ChatPrefix": "Server",

    "ChatPrefixColor": "950415"

  }

}
````