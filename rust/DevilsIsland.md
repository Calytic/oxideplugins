Devil's Island is a complete game mode plug-in, designed to provide a bit more endgame content, and some structure to player interactions.


Players compete to be the Boss, who can place a tax on resource gathering. Being Boss has its obvious advantages, but he also has a huge target painted on his back; everyone knows where he is (to within about 150 meters). Kill the Boss to become the Boss - it's that simple.


Players can also rebel against paying taxes, but the Boss will then know their location (dangerous to rebel alone, not so much if everyone does ). The Boss can order a helicopter patrol to any rebel's position, but for a price.

**Player Commands**

/rules Displays summary of commands to the players
/status Displays a players status, and the current Boss and tax rate
/claim If there is currently no Boss, any player can claim the title. If no player claims the title, someone is randomly chosen after a few minutes
/loot while looking at a box. Allows the Boss to choose a box for his collected taxes to be paid in to.
/tax n Allows the Boss to set the tax rate, from 3% to 45%. Most gathering is taxed.
/helo player Allows the Boss to send a helicopter patrol to any rebel's location. Costs 25 High Quality Metal
/rebel Player becomes an Outlaw, and doesn't pay any tax. However, the Boss is informed of the rebels' locations.
/where Shows the player their current grid location
/decoy player Allows an Outlaw to use another player has his decoy, for a few minutes. Costs 5 High Quality Metal, by default

**Admin Commands (Console)**

devilsisland.reset Resets the game state back to the starting state (no Boss, no Outlaws, 10% tax rate).

**Config File
**

I will get around to documenting this fully in due course, but I think its probably clear enough. Anything that is related to time (xxInterval, xxDelay) is expressed in seconds. The Decoy and Evade related items are placeholders, and do nothing at the moment.

**Current Status**


As of 0.3.0, all of the above is currently functional in the game mode, and running on our test server. We have a lot of more ideas to add, and the mod will be frequently updated.


I'm very interested in feedback from anybody who runs this plug-in. It's also running on my test server, if you want to try it out, or maybe even stick around a bit and help play test it (The server is called "Devil's Island|Game Mode|PVP|02.10|Friendly" (The server is in Frankfurt. Admins may be online during the day, and are online during the evening, CET).

**Current Backlog**


The following features will be implemented in this order:

* "Evade" command for rebels - their location is reported with increased error. Cheap.
* Introduce "bounty" mechanism for (against) Outlaws.
* Chat channel for Outlaws only.
* Switch some player information from chat messages to GUI panels