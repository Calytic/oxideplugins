The plugin provides the ability to list all the connected players, with support to have multiple players listed per line and have it limited by length.

**Command**

/players
**In game

**
**Config**

````
{

  "Settings": {

    "AdminMode": false,

    "ChatName": "Oxide",

    "MaxLength": 80,

    "OnlineMessage": "{0}/50 Players Online",

    "PlayersPerLine": 5,

    "SinglePlayerMessage": "You're the only one on you silly sap."

  }

}
````



* MaxLength: The length of the players list
* OnlineMessage: n/50 OnlineMessage
* SinglePlayerMessage: The message displayed if one player is online
* PlayersPerLine: The amount of desired players on a line, will be limited by MaxLength
* ChatName: The name in the chat next to the message
* AdminMode: Determines if /players is available to admins only. Meaning if its set to true only admins can see the player list and regular players will just see the amount of players online



**Updates to come**

You tell me


If there are any suggestions please post, thanks.

**Donate**

All donations are welcome.