Players earn rewards points for time played and for referring players to your server. The players can then use those points to buy reward kits.

Setup various reward kits using the Kits plugin, and register them through ServerRewards with a reward point value. The player can then browse through the reward store and if they have enough points they can buy themselves a kit

**Chat Commands**
/claim <name> - Claims the reward kit

/refer <playername> - For use by players to acknowledge another players referral. This will give points to the player and the player who invited him/her. (points adjustable in the config)

/rewards check - Display current reward points and total time played on server
/rewards list - Lists available rewards

---- Admin commands ---
/sr add <playername> <amount> - Add points to players profile
/sr take <playername> <amount> - Subtracts points from players profile
/sr clear <playername> - Removes the players reward profile

/rewards remove <rewardname> - Removes a reward kit
/rewards add <rewardname> <kitname> <cost> <opt:description> - Setup a new reward kit

- rewardname is the name of the reward package

- kitname is the name of the kit you created

- cost is how many reward points it costs to purchase

- description is the description of what the kit contains
ex. /rewards add "AK Kit" "ak_kit" 2 "AK and ammo" ==> Creates a new reward kit called "AK Kit" using the kit "ak_kit" that will cost 2 reward points

**Console Commands**
'sr add <playername> <amount>' - See above^
'sr take <playername> <amount>'  ^^

'sr clear <playername>'                   ^^

**How the referral system works**

````
PlayerA invites PlayerB to the server through word of mouth.

PlayerB can then type /refer PlayerA which will give them both points.

PlayerB can no longer use /refer because they have already been referred to the server.

If PlayerA or PlayerB want more points they need to invite more players to the server.

The new players can then use /refer PlayerA/B, and so on and so on.
````



**Config**

````
{

  "Messages - Display message when given reward points": true, // Toggles the received point message ON/OFF

  "Messages - Display messages every X amount of points": 1, // Only displays message every x amount of points received

  "Options - Amount of reward points to give": 1, // Amount of points to give for playtime

  "Options - Data save interval (minutes)": 10,

  "Options - Time played per reward point(minutes)": 60, // 1 point per 60 mins played

  "Options - Use player referrals": true, // Enables/disables referrals

  "Options - Use time played": true, // Enables/disables points for playtime

  "Referrals - Points for the inviting player": 3, // Points for the player who referred another player

  "Refferals - Points for the invited player": 2 // Points for the player who joined on recommendation

}
````


**Datafiles **- Located in /oxide/data/
serverrewards_players.json - This stores all the players time and reward data
serverrewards_rewards.json - This stores all your reward data (names, kits, costs)
serverrewards_referrals.json - This is the list of players who have been referred. To stop abuse of the referral system it is recommended this file does not get deleted when wiping.

**External Calls**
Code (C#):
````
object AddPoints(ulong playerID, int amount) // Add reward points
object TakePoints(ulong playerID, int amount) // Take reward points

//Example
[PluginReference] Plugin ServerRewards;
void ExampleFunction(BasePlayer player)
{

     ServerRewards?.Call("AddPoints", [new](http://www.google.com/search?q=new+msdn.microsoft.com) object[] { player.userID, 5 });

     ServerRewards?.Call("TakePoints", [new](http://www.google.com/search?q=new+msdn.microsoft.com) object[] { player.userID, 5 });
}
````