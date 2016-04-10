**Introduction**

Simple plugin that allows players to see who is online around them within a certain radius.  Let players know who is shooting or trying to raid them.

**Description**

Allows players to view who is nearby within a radius.  Also allows players to chat locally.

**Notes**:


* Setting "PrintToConsole", if true, will print all radius messages to the server console, which is logged.
* "HostileColor", "FriendlyColor" and "UnknownColor". HostileColor will be used if you and the player found are not friends in any way and are not in the same clan. FriendlyColor will be used if you and the player found are both friends or are in the same clan. UnknownColor will be used if you and the player found are not mutual friends and are not in the same clan.

* Setting "UsePermissions" applies to "radius.use" and "radius.repeat" only.
* Since this plugin and my other plugin "Chat Channels" both have local chat, if using both plugins, disable local chat in one or the other to avoid conflicts.


**Permissions**

This plugin uses oxide permissions.


radius.use - Allows players to check radius own for players

radius.repeat - Allows players to use repeat function

radius.hide - Players will not show in radius checks

radius.admin - Allow players to check radius of other players and bypass max radius setting

````
oxide.grant <group|user> <name|id> radius.use

oxide.revoke <group|user> <name|id> radius.use
````


**Usage for players**


Chat Channels

/radius- View help

/radius <radius> - Find all online players within radius of your current location

/radius repeat <off | delay> <radius> - Automatically repeat radius command


Radius Messages

/w <message> - Send a whisper message (50m)

/l <message> - Send a local message (150m)

/y <message> - Send a yell message (300m)

**Note**: Radius chat commands and radius range can be configured.  The figures above are default values.

**Usage for administrators
**

/pradius <player> <radius> - Find all online players within radius of target player


* Administrators may also bypass the max radius setting



**Configuration file**

````
{

  "Messages": {

  "FoundRadius": "Players within <color=#cd422b>{radius}m</color> of your current location (<color=#cd422b>{count}</color>)...",

  "MaxRadius": "Radius must be between <color=#cd422b>1</color> and <color=#cd422b>{radius}</color>.",

  "MultiPlayer": "Multiple players found.  Provide a more specific username.",

  "NoMessage": "You must provide a message.",

  "NoPermission": "You do not have permission to use this command.",

  "NoPlayer": "Player not found.  Please try again.",

  "NoRadius": "No players found within <color=#cd422b>{radius}m</color> of your current location.",

  "NoRepeat": "Automatic radius check is not enabled.",

  "NotEnabled": "Radius system is <color=#cd422b>disabled</color>.",

  "PlayerFoundRadius": "Players within <color=#cd422b>{radius}m</color> of <color=#cd422b>{player}'s</color> current location (<color=#cd422b>{count}</color>)...",

  "PlayerNoRadius": "No players found within <color=#cd422b>{radius}m</color> of <color=#cd422b>{player}'s</color> current location.",

  "RepeatDisabled": "Automatic radius check <color=#cd422b>disabled</color>.",

  "RepeatEnabled": "Automatic <color=#cd422b>{radius}m</color> radius check <color=#cd422b>enabled</color> with <color=#cd422b>{delay} second</color> delay.",

  "RepeatError": "Repeat time must be between <color=#cd422b>{minrepeat}</color> and <color=#cd422b>{maxrepeat}</color>.",

  "Self": "To check your own radius, use <color=#cd422b>/radius <radius></color>.",

  "WrongArgs": "Syntax error.  Use <color=#cd422b>/radius</color> for help."

  },

  "Radius": {

  "ConsolePrint": "[{chat}] {player}: {message}",

  "Enable": "true",

  "LocalColor": "#cd422b",

  "LocalCommand": "l",

  "LocalRadius": "300",

  "MessageColor": "white",

  "PlayerColor": "teal",

  "Prefix": "<color=white>[</color> <color={color}>{radius} ({meters}m)</color> <color=white>]</color>",

  "PrintToConsole": "true",

  "WhisperColor": "yellow",

  "WhisperCommand": "w",

  "WhisperRadius": "50",

  "YellColor": "orange",

  "YellCommand": "y",

  "YellRadius": "150"

  },

  "Settings": {

  "ColorRadiusNames": "true",

  "Enabled": "true",

  "FriendlyColor": "green",

  "HostileColor": "#cd422b",

  "MaxRadius": "125",

  "Prefix": "[<color=#cd422b> Radius </color>]",

  "RepeatMax": "300",

  "RepeatMin": "60",

  "UnknownColor": "yellow",

  "UsePermissions": "true"

  }

}
````

Configuration file will be created and updated automatically.

**More to come**


* Your suggestions


**Known Issues**


* None