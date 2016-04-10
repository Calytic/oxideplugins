[](http://forum.rustoxide.com/plugins/657/rate)
**Hurt Me** is a simple plugin that allows admin to hurt players on command, with optional amount.
**Chat Commands**


* 
**/hurt "player" amount** (default amount is 100)
Hurts target player on command, with optional amount


**Configuration**

You can configure the chat name, chat command, and messages in the hurtme.json file under the oxide/config directory.
**Default Configuration**

````
{

  "Settings": {

    "ChatName": "HURT",

    "ChatCommand": "hurt"

  },

  "Messages": {

    "NoPermission": "You do not have permission to use this command!",

    "HelpText": "Use /hurt player amount (amount being optional, default is 100)",

    "InvalidTarget": "Invalid target! Please try again",

    "TargetHurt": "You have been hurt {amount} by admin",

    "AdminHurt": "{player} has been hurt {amount}"

  }

}
````

The configuration file will update automatically if there are new options available. I'll do my best to preserve any existing settings, message strings, etc with each new version.