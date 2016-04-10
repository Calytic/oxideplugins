**Updater** automatically checks all supported Oxide 2 plugins for updates on server start or by chat command. This is useful to see if you are using an outdated, older versions of plugins. It will also automatically check for updates every hour, or at the time configured. Updater also supports the **[Email API](http://oxidemod.org/plugins/email-api.712/)** and [**Push API**](http://oxidemod.org/plugins/push-api.705/) plugins for instant notifications!
**Note:** The site I use for update checking currently caches each plugin's version for 30 minutes, so you may notice an outdated message for a brief period after updating it.
**Permissions**

This plugin uses Oxide's permission system. To assign a permission, use **oxide.grant user <username|steamid> <permission>**. To remove a permission, use **oxide.revoke user <username|steamid> <permission>**.


* 
**update.check** (allows player to run the update check)
**Ex.** oxide.grant user Wulf update.check
**Ex.** oxide.revoke user Wulf update.check
**Ex.** oxide.grant group admin update.check


**Chat Command**


* 
**/update**
Triggers the plugin update checking sequence.


**Console Command**


* 
**update**
Triggers the plugin update checking sequence.


**Configuration**

You can configure the settings and messages in the Updater.json file under the serverdata/oxide/config directory.
**

Default Configuration**

````
{

  "Settings": {

    "Command": "update",

    "CheckInterval": 3600,

    "EmailNotifications": "false",

    "PushNotifications": "false",

    "ShowUpToDate": "false"

  },

  "Messages": {

    "ChatHelp": "Use '/update' to check for plugin updates",

    "CheckFailed": "{plugin} update check failed!",

    "CheckFinished": "Update check finished!",

    "CheckStarted": "Update check started!",

    "ConsoleHelp": "Use 'update' to check for plugin updates",

    "NoPermission": "You do not have permission to use this command!",

    "NotifySubject": "{count} update(s) available on {hostname}",

    "Outdated": "{plugin} is outdated! Installed: {current}, Latest: {latest}",

    "Supported": "Supported plugin count: {count}",

    "UpToDate": "{plugin} is up-to-date, currently using version: {version}"

  }

}
````

The configuration file will update automatically if new options are added or removed. I'll do my best to preserve any existing settings and messages with each new version.
**Plugin Developers**

To add Updater support in your plugin, add the ResourceId variable and your plugin's ID from its URL.
**Ex.** [http://oxidemod.org/plugins/updater.**380**/](http://oxidemod.org/plugins/updater.380/)
Code (C):
````
namespace Oxide.Plugins
{

    [Info("Title of Plugin", "Your Name", 0.1, ResourceId = 380)]

    [Description("This is what the plugin does")]

    public class PluginName : RustLegacyPlugin

    {

        // This is where your plugin will do its magic

    }
}
````


````
var PluginName = {

    Title : "Title of Plugin",

    Description : "This is what the plugin does",

    Author : "Your Name",

    Version : V(0, 1, 0),

    ResourceId : 380

    // This is where your plugin will do its magic
}
````

Code (Lua):
````
PLUGIN.Title = "Title of Plugin"

PLUGIN.Description = "This is what the plugin does"

PLUGIN.Author = "Your Name"

PLUGIN.Version = V(0, 1, 0)

PLUGIN.ResourceId = 380
-- This is where your plugin will do its magic
````

Code (Python):
````
class PluginName:

    def __init__(self):

        self.Title = "Title of Plugin"

        self.Description = "This is what the plugin does"

        self.Author = "Your Name"

        self.Version = V(0, 1, 0)

        self.ResourceId = 380

    # This is where your plugin will do its magic
````