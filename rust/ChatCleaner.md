**Chat Cleaner** is used to work around the nasty Unity/Rust chat disappearing bug. This will clear a player's chat on join, and on /clear command use. Chat history restoration is also available when using the optional Chat Handler plugin.
**Chat Command**


* 
**/clear**
Clears the chat of all previous messages.


**Configuration**

You can configure the settings and messages in the ChatCleaner.json file under the server/identity/oxide/config directory.
**

Default Configuration**

````
{

  "Messages": {

    "Cleared": "<color=orange><size=18><b><i>Chat Cleared!</i></b></size></color>",

    "Welcome": "<color=orange><size=20><b>Welcome to {server}!</b></size></color>"

  },

  "Settings": {

    "ChatCommand": "clear",

    "ClearedMessage": "true",

    "RestoreChat": "false",

    "WelcomeMessage": "true"

  }

}
````

The configuration file will update automatically if new options are added or removed. I'll do my best to preserve any existing settings and messages with each new version.