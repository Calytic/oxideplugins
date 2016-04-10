━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
**[ DONATIONS ] IF YOU APPRECIATE MY WORK [DONATE HERE](https://www.paypal.com/cgi-bin/webscr?cmd=_s-xclick&hosted_button_id=PRAXKDYLX2VL2)!**

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

* [HollowPlays ( ](http://oxidemod.org/members/41450/)[Omnicidal Gaming](http://www.omnicidal.com/) ) - 45€
* [CHR](http://oxidemod.org/members/49339/) - 30€
* [Imchasinyou](http://oxidemod.org/members/9767/) - 25€
* [cenk](http://oxidemod.org/members/32141/) - 10€━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
**[ ALPHA - 0.0.3 - The KingsMen Update ]**

The next update will be totally focused on KingsMen, or Royal Guard if you will. The King will be able to add/remove any player they want the his Royal Guard, to protect him when he needs the most. I will also try to make it compatible with Rust:IO Clans, so once you are the King your whole clan automatically becomes your Royal Guard, so you can save time on adding each one by one. And even if you rather remove someone of the clan you should be able to.

We will still work on other ideas on where the Royal Guard can be useful, and and what other roles they can play on the kingdom. And of course as always we count on suggestions to help us develop these ideas.


━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
**[ DESCRIPTION ]**

The plugin consists on having a "King of the land", some one overpowered among all players, with privileges while holding the title. The King is stronger to beat then any one else in the kingdom, And what is a King without his Kingsmen?.. So the King will have it's on guards which should prove loyal and fearless, protecting him with their lives.



━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
**[ PLANNED FEATURES ]**
These are features that may or may not be added to the plugin, all these are ideas which are an yet work in progress.


* 
**King Taxes** - The people should have a penalty on their gathering and the King should receive a payment overtime.
* 
**Kingsmen** - To act sort of the Royal guard, loyal men who shall protect the King when he most needs.
* 
**Announce Kingsmen Loss** - This means whenever some kills the King man it announces to all online kingsmen the death and who have killed him.
* King and Kingsmen chat prefix and chat color (This might make the plugin incompatible with ChatHandler plugin)
* 
**Longest Time as King Top** - This is to make it compatible with [Rank-ME](http://oxidemod.org/plugins/rank-me.1074/) plugin and create a top list of the player who spent most time as King.
* 
**King Shield **- A sort of protection to each damage the King receives, to make him a little more "stronger to beat".
* 
**Kingdom Vault** - A vault where the taxes of the people are safely stored hard to penetrate to, but nevertheless it is not indestructible, where only the King him self can access.
* 
**Head-prizes** - The King should be able to put a prize on someone's head, where of course the killer will get a prize.

**More to be added...**


━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
**[ ORIGINAL CONFIGURATION FILE ]**
Code (Java):
````
{

  "COLORS": {

    "PREFIX": "#CECECE",

    "SYSTEM": "#CECECE"

  },

  "COMMANDS": {

    "KING": "king"

  },

  "CONFIG_VERSION": 0.1,

  "MESSAGES": {

    "ANNOUNCE NEW KING": "Sir <red>{attacker}<end> killed King <cyan>{lastking}<end> and is now our new King, all hail the new King!",

    "FIRST KING": "This land had no King until today, <#901BD4>{king}<end> is now the King of the land!",

    "NO KING YET": "No one is ruling the land yet. Kill someone to be the first to claim the land!",

    "NOTIFY KINGSHIP LOSS": "While away you were killed by <red>{attacker}<end> and lost the throne. The current king is <#901BD4>{king}<end>.",

    "TELL KINGSHIP LOST": "You were killed by <red>{attacker}<end> and lost your throne. You are no longer King of the land!",

    "WHO IS KING": "The land is ruled by King <#901BD4>{king}<end>, since <lime>{time}H<end> ago.",

    "YOU ARE KING": "You are the King of the land, since <lime>{time}H<end> ago."

  },

  "SETTINGS": {

    "BROADCAST TO CONSOLE": true,

    "ENABLE KING CMD": true,

    "PREFIX": "<#9B7E7A>Over<end><#CA1F0C>Throne<end>"

  }
}

 
````


━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
**[ COMMANDS ]**

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

/king - It shows the name of the current king and for how long he is holding the title.

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
**[ USAGE NOTES }**

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━


* In order for any Config File changes to take effect in game you must reload the plugin. Simply type **oxide.reload overthrone** in your server's console.
* Make sure you respect the Config File's quotation marks, commas, braces, square brackets and line indentation, or else you may cause plugin malfunctions.
* In order to use the /help command you must install [Domestos](http://oxidemod.org/members/3412/)'s [Help Text](http://forum.rustoxide.com/resources/help-text.676/) plugin.