Since cheaters are abusing the current steam refund system, we have made a little plugin for Oxide that checks if players have the required amount of Rust playtime (by default 2hours for the steam refund policy) before they can connect to the server.

Setting this up on your server is pretty easy, simply edit the following lines in MinimumPlaytime.json in the config folder:

````

{

  "minAmountOfHoursPlayed": 2,

  "steamAPIKey": "insertAPIKeyHere"

}

 
````


**Steam API key:**

This plugin requires a Steam API key, you can get one [**HERE**](http://steamcommunity.com/dev/apikey)

New in 1.1.1:

You can now customize the error messages by editing the following lines in MinimumPlaytime.json in the config folder:

````

  "errorMessageDisallowConnectionPart1": "Sorry, to join this server you need to have at least",

  "errorMessageDisallowConnectionPart2": "hours of playtime. We suggest you spend some time on a Official/Community server and hope you come back later!",

  "errorMessagePrivateProfile": "Sorry, to join this server you need to have a public Steam profile. Please change your profile settings to public and come back later!",

  "errorMessageSteamAPIUnavailable": "MinimumPlaytime was unable to check your Rust playtime because the Steam API is unavailable right now. Please try again later!",

 
````

You can now "whitelist" a player by adding their steamID to the verifiedPlayers list in MinimumPlaytime.json in the config folder:

````

  "verifiedPlayers": [

  "12345678901234567",

  "12345678901234568",

  "12345678901234569"

  ]

 
````

Take a good look at the syntax though (comma for all entries but the last one).