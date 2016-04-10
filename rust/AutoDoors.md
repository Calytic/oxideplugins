**AutoDoors** automatically closes doors behind players after the default amount of seconds or as set by them. The delay is by default set to 5 seconds, but you can change that in the config file if desired.
**Chat Command**


* 
**/ad**
Disables doors automatically closing for player.
* 
**/ad #** (a number between MIN and MAX)
Sets automatic door closing delay for player.


**Configuration**

You can configure the settings in the AutoDoors.json file under the oxide/config directory.

````
{

  "DefaultDelay": 5,

  "MaximumDelay": 30,

  "MinimumDelay": 5

}
````


**Localization**

The default messages are in the AutoDoors.en.json under the oxide/lang directory, or create a language file for another language using the 'en' file as a default.

````
{

  "ChatCommand": "ad",

  "CommandUsage": "Usage:\n /ad to disable automatic doors\n /ad # (a number between 5 and 30)",

  "DelayDisabled": "Automatic door closing is now disabled",

  "DelaySetTo": "Automatic door closing delay set to {time}s"

}
````


**Credits**


* 
**Bombardir**, the original author of the original version of this plugin.