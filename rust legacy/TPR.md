**OPTIONAL (see tokens):
**[Player Database](http://oxidemod.org/plugins/player-database.927/) 1.0.1**


Features:**

- Teleportation requests for players

- Cancel any teleportation any time

- Teleportation gets cancelled if the source or target player dies

- Blocks players that try to glitch inside a rock

- Blocks players from trying to tp next to buildings to prevent glitching

-Blocks players from being able to tp in/out of shacks

- Optional: if source player gets hit, his teleportation will be cancelled

- Optional: Token system that can be called from outer plugins

- Multiple teleportations to prevent abuse

- Teleportation request cool down

**Commands:**

- /tpr => Shows how to use the command. If you use the tokens it will show how much you have left

- /tpr PLAYER => request to teleportation to a player.

- /tpa => accept a teleportation request.

- /tpc => reject a teleportation request. If you don't have any teleportation request it will cancel all teleportations towards you or from you.

**Tokens (Optional):

TO USE THIS FEATURE YOU WILL NEED:**
**[Player Database](http://oxidemod.org/plugins/player-database.927/) 1.0.1**

- You can set a max amount of tokens per player

- You can activate the free tokens timer

- You can set the timer for players to get free tokens

- You can set the start tokens

**TPR.json**

````
{

  "Settings: Cancel teleportation if player is hurt": true,

  "Settings: Cooldown before being able to use TPR again": 60,

  "Settings: TPR Teleportation delay": 10,

  "Tokens: activated": true,

  "Tokens: give free token every": 600.0,

  "Tokens: give free tokens": true,

  "Tokens: give free tokens max": 3,

  "Tokens: start tokens": 3

}
````


**For External plugins:**

**Add Tokens to a player:**

````
TPR.Call("AddTeleportTokens",string USERID, int NUMBER);

return you show how many tokens the player now has

if tokens are deactivated it will return null

 
````


**Remove Tokens from a player:**

````
TPR.Call("RemoveTeleportTokens",string USERID, int NUMBER);

return will show how many tokens the player now has

if tokens are deactivated it will return null

 
````


**Get Tokens of a player:**

````
TPR.Call("GetTeleportTokens",string USERID);

return will show how many tokens the player has

if tokens are deactivated it will return null

 
````


**Set Tokens of a player:**

````
TPR.Call("SetTeleportTokens",string USERID, int NUMBER);

return will show how many tokens the player has

if tokens are deactivated it will return null

 
````


**Cancel ALL teleportations from a player:**

````
TPR.Call("ResetRequest",NetUser netuser);

no returns

 
````


**Refuse teleportations for players with:**

(this will refuse teleportations for ALL users, but you can edit it easilly to prevent players from teleportation in or out of certain area, etc)

````
object canTeleport(NetUser netuser)

{

    return false;

}
````


**TODO:**

- Fix some random errors in console (does not effect plugin)