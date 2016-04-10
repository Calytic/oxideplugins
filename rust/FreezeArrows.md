Allows authorized players to shoot a Freeze Arrow. When the arrow hits something, everyone within a certain radius will be frozen in place for 10 seconds (default) and will have the frozen overlay on there screen until timer is up.

******* Since these arrows tend to really really piss people off when they get frozen.. LOL (perfect for rust). I will try to come up with a good way to limit its uses easily if wanted.

**Usage:** Player will type /freezearrow. only the next arrow shot from bow or crossbow will be a Freeze Arrow. After its shot, arrows will become normal again. 



**Chat Command:** /freezearrow

This will enable next shot to be a Freeze Arrow. Or will turn it off.

**Permissions:** freezearrows.allowed

````
oxide.grant <group|user> <name|id>  freezearrows.allowed


Example: To allow the group player to use it. (all players)

oxide.grant group player freezearrows.allowed


Example 2 : To just allow a person to use it:

oxide.grant user "colon blow" freezearrows.allowed
````


**Configuration** (located in servername/oxide/config)

````
{

  "Effects - Show hit explosion effect": true,

  "Overlay - How long frozen overlay is shown when player is frozen": 10.0,

  "Overlay - Show freeze overlay when player is frozen": true,

  "Radius - The distance from impact players are effeted": 5,

  "Targets - Arrows will freeze NPCs": true,

  "Targets - Arrows will freeze players": true,

  "Time - Cooldown for freezing same player again": 10,

  "Time - How long player is frozen when hit": 10

}
````


**Language File** (located in servername/oxide/lang)

````
{

  "onnextshottxt": "Your next shot will be a Freeze Arrow",

  "offnextshottxt": "Your next shot will a Normal Arrow",

  "yourfrozetxt": "You are frozen in place....",

  "unfrozetxt": "You are now unfrozen...."

}
````


**Future Ideas:**

- add ability to break freeze if player takes damage. turn off/on via config.

- add ability to have  set number of arrows to use per rust day.

- Admin or authorized person can give arrows to players. or reset players counts.