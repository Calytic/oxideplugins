This plugin enables and disables radiation in an interval of minutes set in the configuration, to act as an server event, where your server players may loot radiation zones without radiation for a certain amount of time.

**WARNING:**

Since Rust developers have disabled Radiation in the game due to Rust development, it's now required to install Zones Manager plugin and manually add radiaton zones throghout the map for RAD-Line to work properly, just take in mind this is a temporary work-aroundClick to expand...
**[ DEFAULT CONFIGURATION ]**
Code (Java):
````
{

  "General Settings": {

    "Enable Icon Profile": false,

    "Enable Plugin Prefix": true,

    "Icon Profile": "76561198248442828",

    "Prefix": "[ <cyan>NOTIFIER<end> ]",

    "Radiation Disabled Interval (In Minutes)": 10,

    "Radiation Enabled Interval (In Minutes)": 30

  }
}
````


**[ DEFAULT COMMANDS ]**

**/rad** - Tells the state of Radiation and the time remaining to change
**/radline** - Shows information about the plugin version
Click to expand...
**[ USAGE NOTES ]**


* In order for any configuration changes take effect in game you must reload the plugin. Simply type **oxide.reload RadLine** in your server's console.
* Make sure you respect the configuration's file quotation marks, commas, braces, square brackets and line indentation, or else you may cause plugin malfunctions. For further help validate your file in [jsonlint.com](http://jsonlint.com)