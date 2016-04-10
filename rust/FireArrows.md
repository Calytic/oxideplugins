Allows authorized players the ability to shoot arrows with fire. Once player is authorized and has a Bow or Crossbow equipped.

**3 Tiers of Arrows currently:**

Fire Arrow - standard fire arrows. Need Cloth and Low grade fuel.

Fire Ball Arrow - better fire arrow. Need Cloth, Fuel and Crude oil.

Fire Bomb Arrow - best fire arrow. Need Cloth, Fuel, Oil and Explosives.

**How to Toggle Arrows:**

Press the mouse wheel down will toggle Arrows if you are holding a bow. And allowed to use them. Each toggle will move to next tier. It will tell you if you can use them or not.

**Commands: /firearrow**

Or.. You can type /firearrow in chat. This will also toggle arrow tiers if you are holding a bow or crossbow.


A fire arrow icon appears at bottom left when its enabled, and goes away when normal arrows are selected. Icon can be not show via config file.



**Permissions:**

firearrows.allowed

firearrows.ball.allowed

firearrows.bomb.allowed


````
oxide.grant <group|user> <name|id>  firearrows.allowed

oxide.grant <group|user> <name|id>  firearrows.ball.allowed

oxide.grant <group|user> <name|id>  firearrows.bomb.allowed


Example: To allow the group player to use it. (all players)

oxide.grant group player firearrows.allowed

oxide.grant group player firearrows.ball.allowed

oxide.grant group player firearrows.bomb.allowed


Example 2 : To just allow a person to use it:

oxide.grant user "colon blow" firearrows.allowed
````


**Configuration file: **(located in /servername/oxide/config)

````
{

  "Damage - Fire Arrow": 50.0,

  "Damage - Fire Ball Arrow": 200.0,

  "Damage - Fire Bomb Arrow": 500.0,

  "Damage - Radius": 1.0,

  "Duration - Fire Arrow": 10.0,

  "Duration - Fire Ball Arrow": 10.0,

  "Duration - Fire Bomb Arrow": 10.0,

  "Effects - Show Fire on Arrow Draw": true,

  "Icon - Fire Arrow": "http://i.imgur.com/3e8FWvt.png",

  "Icon - Fire Ball Arrow": "http://i.imgur.com/USdpXGT.png",

  "Icon - Fire Bomb Arrow": "http://i.imgur.com/0DpAHMn.png",

  "Icon - Show Arrow Type": true,

  "Required - All Arrows - Cloth Amount": 5,

  "Required - All Arrows- Low Grade Fuel Amount": 5,

  "Required - FireBall & FireBomb Arrows - Crude Oil": 5,

  "Required - FireBomb Arrows - Explosives": 5

}
````


**Language File default: **(located in /servername/oxide/lang)

````
{

  "firearrowtxt": "Your Arrows are set for Fire.",

  "fireballarrowtxt": "Your Arrows are set for FireBall.",

  "firebombarrowtxt": "Your Arrows are set for FireBomb.",

  "defaultarrowtxt": "Your Arrows are set for Normal.",

  "deniedarrowtxt": "No Access to This Arrow Tier.",

  "doesnothavemattxt": "You don't have required materials..."

}
````


**Future:**

Working on pre-fire animation / fire on arrow.