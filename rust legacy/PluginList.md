**Plugin List** allows players to see all of the installed plugins on a server, and obtain additional information about those plugins if its provided by each plugin. If a plugin is missing a description or resourceId (used for the URL), you may encourage plugin authors to add the information to their plugins.
**Chat Commands**


* 
**/plugins**
Shows all installed plugins in a list ordered by filename.



* 
**/plugin PluginName**
Shows plugin version, description, and URL if available.


**Configuration**

You can configure the settings and messages in the PluginList.json file under the serverdata/oxide/config directory.
**Default Configuration**

````
{

  "Messages": {

    "InstalledPlugins": "Installed plugin(s):",

    "NoPluginFound": "No plugin found with that name!"

  }

}
````

The configuration file will update automatically if new options are added or removed. I'll do my best to preserve any existing settings and messages with each new version.