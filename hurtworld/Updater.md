**Updater** automatically checks all supported Oxide 2 plugins for updates on server start or by chat command. This is useful to see if you are using an outdated, older versions of plugins. It will also automatically check for updates every hour, or at the time configured. Updater also supports the **[Email API](http://oxidemod.org/plugins/email-api.712/)** and [**Push API**](http://oxidemod.org/plugins/push-api.705/) plugins for instant notifications!



**Permissions**

This plugin uses Oxide's permission system. To assign a permission, use **grant user <username|steamid> <permission>**. To remove a permission, use **revoke user <username|steamid> <permission>**.


* 
**updater.use **(allows player to run the update check)
**Ex.** grant user Wulf updater.use
**Ex.** revoke user Wulf updater.use
**Ex.** grant group admin updater.use


**Chat Command**


* 
**/updates**
Triggers the plugin update checking sequence.


**Console Command**


* 
**updates**
Triggers the plugin update checking sequence.


**Configuration**

You can configure the settings and messages in the Updater.json file under the server/identity/oxide/config directory.
**

Default Configuration**

````
{

  "Settings": {

    "Auto Check Interval (in Minutes)": 60.0,

    "Use EmailAPI": false,

    "Use PushAPI": false

  }
}
````


**Default Language File**

````
{

  "No Permission": "You don't have permission to use this command.",

  "Outdated Plugin List": "Following plugins are outdated: {plugins}",

  "Outdated Plugin Info": "# {title} | Installed: {installed} - Latest: {latest} | {url}"
}
````

The configuration file will update automatically if new options are added or removed. I'll do my best to preserve any existing settings and messages with each new version.

**Plugin Developers**

To add Updater support in your plugin, add the ResourceId variable and your plugin's ID from its URL.
**Ex.** [http://oxidemod.org/plugins/updater.**681**/](http://oxidemod.org/plugins/updater.681/)
Code (C):
````
namespace Oxide.Plugins
{

    [Info("Title of Plugin", "Your Name", 0.1, ResourceId = 681)]

    [Description("This is what the plugin does")]

    public class PluginName : RustPlugin

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

    ResourceId : 681


    // This is where your plugin will do its magic
}
````

Code (Lua):
````
PLUGIN.Title = "Title of Plugin"

PLUGIN.Description = "This is what the plugin does"

PLUGIN.Author = "Your Name"

PLUGIN.Version = V(0, 1, 0)

PLUGIN.ResourceId = 681

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

        self.ResourceId = 681


    # This is where your plugin will do its magic
````