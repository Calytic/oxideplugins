**This is an addon created for Rust:IO. In order to use all features of this plugin, you need to install Rust:IO first!**


* **[Get Rust:IO!](http://get.playrust.io)**


**Rust:IO Clans** provides your players with an extensive clan system. It offers:


* Create your own clan and invite your friends
* Promote members to moderators to manage the clan
* Automatically updates members to share their location on Rust:IO

* Chat internally with all clan members through **/c Message...**
* Shows the clan tag in front of all clan members' names
* Broadcasts members going online or offline to all clan members
* Turns off friendly fire for clan members when used with [Rust:IO FriendlyFire](http://oxidemod.org/resources/rust-io-friendlyfire.840/)

[ [Servers using Clans](http://playrust.io/#tags:clans) ]



**Translating**


There is a config file located at config/Clans.json which contains all translatable strings. Simply edit the **right hand side** of the translations, but always keep the %PLACEHOLDERS% intact and untranslated.

**Configuration**


Also in config/Clans.json there are a few variables to configure the plugin.:


* 
**addClanMatesAsFriends** specifies whether clan mates are automatically added to each others Rust:IO friendslist (default: true).
* 
**limit** specifies the maximum number of clan members respectively moderators (default: -1 = no limit)


**Plugin API**


This plugins provides a simple to use API for other plugins to utilize:


* 
**GetClan(tag:string):JObject**

Returns a JObject (reference Newtonsoft.Json.dll) representing the clan using the specified tag or null if there is no such clan. The JObject contains the following properties: **tag**:string, **description**:string, **owner**:string, **moderators**:JArray, **members**:JArray, **invited**:JArray. All members are represented by their SteamID as a string.


* 
**GetClanOf(player:ulong|string|BasePlayer):string**

Returns the clan tag of a player's clan or null if the player is not a member of a clan.


* 
**GetAllClans():JArray**

Returns an array of all clan tags.

The plugin also calls the following hooks (no return behavior) on changes:


* 
**OnClanCreate**(**tag:string)**

Called when a new clan has been created


* 
**OnClanUpdate**(**tag:string)**

Called when clan members or invites change. Note: Make sure not to do any clan updates within your hook method, as this would most likely result in an infinite loop.


* 
**OnClanDestroy**(**tag:string)**

Called when a clan is disbanded or deleted


````
{

  "messages": {

    "%MEMBER% invited %PLAYER% to the clan.": "%MEMBER% invited %PLAYER% to the clan.",

    "%NAME% has come online!": "%NAME% has come online!",

    "%NAME% has gone offline.": "%NAME% has gone offline.",

    "%NAME% has joined the clan!": "%NAME% has joined the clan!",

    "%NAME% has left the clan.": "%NAME% has left the clan.",

    "%NAME% kicked %MEMBER% from the clan.": "%NAME% kicked %MEMBER% from the clan.",

    "%OWNER% promoted %MEMBER% to moderator.": "%OWNER% promoted %MEMBER% to moderator.",

    "<color=\"#ffd479\">/clan help</color> - Learn how to create or join a clan": "<color=\"#ffd479\">/clan help</color> - Learn how to create or join a clan",

    "<color=\"#ffd479\">/clan</color> - Displays your current clan status": "<color=\"#ffd479\">/clan</color> - Displays your current clan status",

    "<color=#74c6ff>Moderator</color> commands:": "<color=#74c6ff>Moderator</color> commands:",

    "<color=#a1ff46>Owner</color> commands:": "<color=#a1ff46>Owner</color> commands:",

    "<color=#cd422b>Server owner</color> commands:": "<color=#cd422b>Server owner</color> commands:",

    "<color=#ffd479>/c Message...</color> - Sends a message to all online clan members": "<color=#ffd479>/c Message...</color> - Sends a message to all online clan members",

    "<color=#ffd479>/clan create \"TAG\" \"Description\"</color> - Creates a new clan you own": "<color=#ffd479>/clan create \"TAG\" \"Description\"</color> - Creates a new clan you own",

    "<color=#ffd479>/clan delete \"TAG\"</color> - Deletes a clan (no undo)": "<color=#ffd479>/clan delete \"TAG\"</color> - Deletes a clan (no undo)",

    "<color=#ffd479>/clan demote \"Name\"</color> - Demotes a moderator to member": "<color=#ffd479>/clan demote \"Name\"</color> - Demotes a moderator to member",

    "<color=#ffd479>/clan disband forever</color> - Disbands your clan (no undo)": "<color=#ffd479>/clan disband forever</color> - Disbands your clan (no undo)",

    "<color=#ffd479>/clan invite \"Player name\"</color> - Invites a player to your clan": "<color=#ffd479>/clan invite \"Player name\"</color> - Invites a player to your clan",

    "<color=#ffd479>/clan join \"TAG\"</color> - Joins a clan you have been invited to": "<color=#ffd479>/clan join \"TAG\"</color> - Joins a clan you have been invited to",

    "<color=#ffd479>/clan kick \"Player name\"</color> - Kicks a member from your clan": "<color=#ffd479>/clan kick \"Player name\"</color> - Kicks a member from your clan",

    "<color=#ffd479>/clan leave</color> - Leaves your current clan": "<color=#ffd479>/clan leave</color> - Leaves your current clan",

    "<color=#ffd479>/clan promote \"Name\"</color> - Promotes a member to moderator": "<color=#ffd479>/clan promote \"Name\"</color> - Promotes a member to moderator",

    "<color=#ffd479>/clan</color> - Displays relevant information about your current clan": "<color=#ffd479>/clan</color> - Displays relevant information about your current clan",

    "Available commands:": "Available commands:",

    "Clan tags must be 2 to 6 characters long and may contain standard letters and numbers only": "Clan tags must be 2 to 6 characters long and may contain standard letters and numbers only",

    "Members online:": "Members online:",

    "No such player or player name not unique:": "No such player or player name not unique:",

    "Pending invites:": "Pending invites:",

    "Please provide a short description of your clan.": "Please provide a short description of your clan.",

    "There is already a clan with this tag.": "There is already a clan with this tag.",

    "There is no clan with that tag:": "There is no clan with that tag:",

    "This player has already been invited to your clan:": "This player has already been invited to your clan:",

    "This player is already a member of your clan:": "This player is already a member of your clan:",

    "This player is already a moderator of your clan:": "This player is already a moderator of your clan:",

    "This player is an owner or moderator and cannot be kicked:": "This player is an owner or moderator and cannot be kicked:",

    "This player is not a member of your clan:": "This player is not a member of your clan:",

    "This player is not a moderator of your clan:": "This player is not a moderator of your clan:",

    "To invite new members, type: <color=\"#ffd479\">/clan invite \"Player name\"</color>": "To invite new members, type: <color=\"#ffd479\">/clan invite \"Player name\"</color>",

    "To join, type: <color=#ffd479>/clan join \"%TAG%\"</color>": "To join, type: <color=#ffd479>/clan join \"%TAG%\"</color>",

    "To learn more about clans, type: <color=\"#ffd479\">/clan help</color>": "To learn more about clans, type: <color=\"#ffd479\">/clan help</color>",

    "Usage: <color=\"#ffd479\">/clan create \"TAG\" \"Description\"</color>": "Usage: <color=\"#ffd479\">/clan create \"TAG\" \"Description\"</color>",

    "Usage: <color=\"#ffd479\">/clan delete \"TAG\"</color>": "Usage: <color=\"#ffd479\">/clan delete \"TAG\"</color>",

    "Usage: <color=\"#ffd479\">/clan demote \"Player name\"</color>": "Usage: <color=\"#ffd479\">/clan demote \"Player name\"</color>",

    "Usage: <color=\"#ffd479\">/clan disband forever</color>": "Usage: <color=\"#ffd479\">/clan disband forever</color>",

    "Usage: <color=\"#ffd479\">/clan invite \"Player name\"</color>": "Usage: <color=\"#ffd479\">/clan invite \"Player name\"</color>",

    "Usage: <color=\"#ffd479\">/clan join \"TAG\"</color>": "Usage: <color=\"#ffd479\">/clan join \"TAG\"</color>",

    "Usage: <color=\"#ffd479\">/clan kick \"Player name\"</color>": "Usage: <color=\"#ffd479\">/clan kick \"Player name\"</color>",

    "Usage: <color=\"#ffd479\">/clan leave</color>": "Usage: <color=\"#ffd479\">/clan leave</color>",

    "Usage: <color=\"#ffd479\">/clan promote \"Player name\"</color>": "Usage: <color=\"#ffd479\">/clan promote \"Player name\"</color>",

    "You are a member of:": "You are a member of:",

    "You are a moderator of:": "You are a moderator of:",

    "You are already a member of a clan.": "You are already a member of a clan.",

    "You are currently not a member of a clan.": "You are currently not a member of a clan.",

    "You are now the owner of your new clan:": "You are now the owner of your new clan:",

    "You are the owner of:": "You are the owner of:",

    "You have been invited to join the clan:": "You have been invited to join the clan:",

    "You have deleted the clan:": "You have deleted the clan:",

    "You have left your current clan.": "You have left your current clan.",

    "You have not been invited to join this clan.": "You have not been invited to join this clan.",

    "You need to be a moderator of your clan to use this command.": "You need to be a moderator of your clan to use this command.",

    "You need to be a server owner to delete clans.": "You need to be a server owner to delete clans.",

    "You need to be the owner of your clan to use this command.": "You need to be the owner of your clan to use this command.",

    "Your clan has been deleted by the server owner.": "Your clan has been deleted by the server owner.",

    "Your current clan has been disbanded forever.": "Your current clan has been disbanded forever."

  },

  "addClanMatesAsFriends": true,

  "limit": {

    "members": -1,

    "moderators": -1

  }

}
````