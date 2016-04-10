**Push API**  is an API for sending messages via Pushover and Pushalot mobile notification services. Keep in mind that alone this does nothing, as it's an API meant for plugins to utilize. To use this with a supported plugin, simply install like you would any other plugin!

**Pushover Service** 


* 7,500 messages per month free per application
* Priority: high, low, quiet
* Sounds: https://pushover.net/api#sounds
* Apps: Android, iOS, Chrome, Firefox, and Safari
* Signup at https://pushover.net/login

**Pushalot Service** 


* 500 messages per account per day,  300 messages per 300 seconds per IP address.
* Priority: high, low, quiet
* Sounds: no sound options available
* Apps: Windows 8 and Windows Phone
* Signup at https://pushalot.com/register

**Configuration** 

You need to set your Pushover or Pushalot API keys/tokens for this to work. You can configure all of the settings and messages in the PushAPI.json file under the data/oxide/config directory.

**Default Configuration** 

````
{

  "Pushalot": {

    "AuthToken": ""

  },

  "Pushover": {

    "ApiToken": "",

    "UserKey": ""

  },

  "Settings": {

    "Service": "pushover"

  },

  "Messages": {

    "SendFailed": "Notification failed to send!",

    "SendSuccess": "Notification successfully sent!",

    "SetUserKey": "User key not set! Please set it and try again.",

    "SetApiToken": "API token not set! Please set it and try again."

  }

}
````

The configuration file will update automatically if new options are added or removed. I'll do my best to preserve any existing settings and messages with each new version.

**Plugin Developers** 

To call the functions from this API your plugin needs to get the plugin instance.
Code (C):
````
[PluginReference]

Plugin PushAPI;
````

Code (Lua):
````
local pushApi = plugins.Find("PushAPI")
````

You can then use this to send notifications using the PushMessage function.
Code (C):
````
PushAPI?.Call("PushMessage", "This is a test push", "This is a test of the Push API!");
````

Code (Lua):
````
pushApi:CallHook("PushMessage", "This is a test push", "This is a test of the Push API!")
````

You can also specific an optional priority and sound at the end of the call.

**Example Plugins** 
Code (C):
````
using Oxide.Core.Plugins;


namespace Oxide.Plugins
{

    [Info("Push Test", "Wulf / Luke Spragg", 0.1)]

    [Description("Push API test plugin.")]

    class PushTest : SevenDaysPlugin

    {

        [PluginReference]

        Plugin PushAPI;


        void Loaded()

        {

            if (!PushAPI)

            {

                Puts("Push API is not loaded! http://oxidemod.org/plugins/1165/");

                return;

            }

            PushAPI.Call("PushMessage", "This is a test push", "This is a test of the Push API!");

        }

    }
}
````

Code (Lua):
````
PLUGIN.Title = "Push Test"

PLUGIN.Version = V(0, 1, 0)

PLUGIN.Description = "Push API test plugin."

PLUGIN.Author = "Wulf / Luke Spragg"

function PLUGIN:Init()

    local pushApi = plugins.Find("PushAPI")

    if not pushApi then print("Push API is not loaded! http://oxidemod.org/plugins/1165/") return end

    pushApi:CallHook("PushMessage", "This is a test push", "This is a test of the Push API!")
end
````