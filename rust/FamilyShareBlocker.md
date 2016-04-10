This plugin will kick players that don't own the game and are playing Rust as a family shared game.


If you would like certain people with a family shared account to connect you can add them to the whitelist in the configuration file.


Keep in mind that this plugin uses a webrequest to use the Steam API to check if a player is playing on a shared account, and so for this to work a Steam API key is required. You can obtain your own API key here: [http://steamcommunity.com/dev/apikey](http://steamcommunity.com/dev/apikey)

**This is the Rust Experimental version of the plugin, for the Rust Legacy version go to [http://oxidemod.org/plugins/family-share-blocker.994/](http://oxidemod.org/plugins/family-share-blocker.994/)**


**Default Config:**

````
{

  "Settings": {

    "SteamAPIKey": "STEAM_API_KEY"

  },

  "Options": {

    "LogToConsole": true,

    "Whitelist": []

  }

}
````


**Whitelist Config example:**

````
{

  "Settings": {

    "SteamAPIKey": "STEAM_API_KEY"

  },

  "Options": {

    "LogToConsole": true,

    "Whitelist": ["76561198112743227", "76561198112743229"]

  }

}
````