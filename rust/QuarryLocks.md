This plugin allows players to use [x] codelocks(Defined in config) to create a scripted lock which players can use using various different commands such as seen below!!!

**Notes:**


* When a player accesses a quarry and has his cursor stuck, and he starts to complain. Just tell him to open his inventor and close it! As this is currently the only way I can find to end a players looting.
* For you developers that are looking at this plugin - player.EndLooting() does not seem to work with quarry's for some reason.


**Usage:**


* /qlock friends - Returns anyone who knows your quarry code.
* /qlock blocked - Returns anyone who is blocked from your code.
* /qlock block (player) - Will block a player.
* /qlock unblock (player) - Will unblock a player.
* /qlock code - Returns your quarry code.
* /qlock createcode - Creates a code for your quarrys if you have the correct locks(must do setcode afterwards).
* /qlock disablecode - Disables your code and refunds the code locks.
* /qlock setcode (newcode) - Sets your code to (NewCode) if your code system is enabled.
* /qlock entercode (player) (code) - Unlocks a quarry of (player) if you used the right code.
* /qlock rmessage - Returns all of your current messages.
* /qlock clearmessages - Clears all of your logs|messages.
* /qlock setmaxlogs (#) - Sets your max logs allowed to (#).
* /qlock setmaxlogs(p) (#) - Sets your max logs per player allowed to (#).


**Features:**


* Loggings systems for players that log anyone who accesses or tries to access they're quarry. This can be limited using the setmaxlogs(p) and setmaxlogs command.
* Virtual Code Locks - Meaning these locks can not be destroyed and must be accesses via code as in with a command.
* Health loss if a player enters the code wrong(Configurable).
* Auto Save(Saves upon your server saving)
* Chat Prefix(Configurable)
* Chat Prefix Color(Configurable)
* Chat Color(Configurable)
* Code Locks Needed to create a lock for a quarry(Virtual and Configurable!)
* Configurable language messages.
* Configurable help messages(Via data file named "quarrylocks_messages")
* And more!


**Config:**


* ChatPrefix : The chat prefix the plugin uses -Default is QLock,
* ChatPrefixColor : The color the chat prefix gets -Default is #6f60c9,
* ChatColor : The color chat commands are colored - Default is #6f60c9,
* HealthWrong : The amount of health a player looses when they enter a code wrong -Default is 5,
* CodeLocksNeeded : Code Locks needed to create a virtual lock -Default is 5


**Whats To Come?**


* Your suggestions!


**Any Known Bugs:**


* None as of now!