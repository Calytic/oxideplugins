**Introduction**

This is my take of timed events that uses local server time instead of timers.  Add as many or as few events as you want.  Multiple events can share the same time.

**Description**

Allows automatic events to take place depending on server time.

**Notes**:


* Multiple events can share the same time slot, however, it is best to keep it limited to reduce any potential lag.  Events that share the time time will run in top to bottom order as listed in the config file.

* When adding a new event, the config file can be manually edited or in-game commands may be used.  Do not use ":" other than to separate the time and event.  In-game commands have error checking.  Manual editing does not.
* Setting "SayEnabled".  Set this to true if you use the say command in events and have trouble with formatting if using another plugin that modifies the say command. When true, the new setting "SayPrefix" will also be used. When using "SayEnabled", all commands are still logged to console.

* The airdrop commands included in the config file require the Airdrop Controller plugin.


**Permissions**

This plugin uses oxide permissions.


event.admin - Allows players to configure timed events

````
oxide.grant <group|user> <name|id> event.admin

oxide.revoke <group|user> <name|id> event.admin
````


**Usage for administrators
**

/te toggle <system | local | server | repeat> - Enable or disable system or event group

/te time - View current server time

/te list <l |s | r> - View current events

Local time: /te add <l> <00.00.PM:Event> - Add new event

Server time: /te add <s> <0-23:Event> - Add new event

/te delete <l |s | r> <ID> - Delete existing event (list for ID's)

/te clear <l |s | r> - Delete all existing events (cannot be undone)

/te run <l |s | r> <ID> - Manually run an event ID (list for ID's)

**Configuration file**

````
{

  "Events": {

  "Local": [

  "12.00.PM:say Time for an airdrop!",

  "12.00.PM:airdrop.massdrop 1",

  "01.00.PM:say Welcome to my server!  Enjoy your stay!",

  "02.00.PM:say Play fair!  No Cheating!",

  "03.00.PM:say Time for another airdrop!",

  "03.00.PM:airdrop.massdrop 1"

  ],

  "Repeat": [

  "3600:say Saving server data.",

  "3600:server.save"

  ],

  "Server": [

  "8:say Time for an airdrop!",

  "8:airdrop.massdrop 1",

  "9:say Welcome to my server!  Enjoy your stay!",

  "10:say Play fair!  No Cheating!",

  "11:say Time for another airdrop!",

  "11:airdrop.massdrop 1"

  ]

  },

  "Messages": {

  "ChangedStatus": "Timed events <color=#cd422b>{status}</color>.",

  "EventAdded": "Event <color=#cd422b>{event}</color> with time <color=#cd422b>{etime}</color> (<color=#cd422b>{group}</color>) successfully added.",

  "EventDeleted": "Event with ID <color=#cd422b>{id}</color> (<color=#cd422b>{group}</color>) successfully deleted.",

  "EventRun": "Event with ID <color=#cd422b>{id}</color> (<color=#cd422b>{event}</color>) with original run time <color=#cd422b>{etime}</color> (<color=#cd422b>{group}</color>) successfully run.",

  "EventsCleared": "All <color=#cd422b>{group}</color> events successfully deleted.",

  "InvalidFormat": "Invalid event format <color=#cd422b>{eformat}</color> (<color=#cd422b>{group}</color>).  Use <color=#cd422b>/te</color> for help.",

  "InvalidID": "Invalid event ID <color=#cd422b>{id}</color> for <color=#cd422b>{group} group</color>.  Use <color=#cd422b>/te</color> for help.",

  "NoEvents": "No events found for <color=#cd422b>{group} group</color>.",

  "NoPermission": "You do not have permission to use this command.",

  "ServerTime": "Server time: <color=#cd422b>{stime}</color>",

  "TimeChangedStatus": "Event group <color=#cd422b>{group}</color> now <color=#cd422b>{status}</color>.",

  "WrongArgs": "Syntax error.  Use <color=#cd422b>/te</color> for help."

  },

  "Settings": {

  "Enabled": "true",

  "LocalTime": "true",

  "Prefix": "[<color=#cd422b>Timed Events</color>]",

  "Repeat": "true",

  "SayEnabled": "false",

  "SayPrefix": "[<color=#cd422b> SERVER </color>]",

  "ServerTime": "true"

  }

}
````

Configuration file will be created and updated automatically.

**More to come**


* Your suggestions


**Known Issues**


* None