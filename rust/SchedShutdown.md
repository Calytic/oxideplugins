**Introduction**

My thanks to Feramor for his HappyHour plugin, which gave me a starting point for creating this one.

**Description**

Allows a server admin to schedule a server shutdown for a set UTC time. When used with a server-side ‘keep-alive’ script or program, this will allow the admin to automatically restart the server on a daily basis.

**Usage** (server console command)
**schedule.shutdown <hh:mm:ss>** - Sets the shutdown time, enables the shutdown timer, and updates the configuration file. The time is expected to be a UTC time. For example:

````
schedule.shutdown 17:30:00
````

will schedule the shutdown for 5:30pm UTC time.

**schedule.shutdown enable** - Enables the shutdown timer if it was disabled.

**schedule.shutdown disable** - Disables the shutdown timer, without changing the shutdown time.

**schedule.shutdown** - With no parameters, the command displays the current status of the shutdown timer (enabled/disabled, and the UTC shutdown time).

**Installation Instructions**

* Download the plugin, and place in the Oxide\plugins folder.
* Once the plugin has been loaded, use the server console command to set the shutdown time.
**Configuration File**

````
{

  "status": "enabled",

  "UTC_Time": "09:30:00"

}
````

In this example, the shutdown time is set for UTC 9:30am, and the shutdown timer is enabled.

````
{

  "status": "disabled",

  "UTC_Time": ""

}
````

This is the default configuration, which is created automatically when the plugin is first loaded.