Players can use Spear, Bow or Crossbow to fish up raw meat from Rivers and Open water areas. Jabbing, Throwing or Shooting Water gives players a chance for a successful catch. Bonuses are granted depending on time of day, quality of weapon, what you wear and have on you. By default, there is a very small chance to pull up a Tier 3 loot crate. argg...buried treasure mateys !!



**How to Fish:**

First, Head off and find some water. You need to be within a few meters or even standing in it. Take your Spear and jab the water floor and then you have a chance to haul in a fish. (aka...raw chicken). You can also use your Bow or Crossbow to get near the water and shoot at it.

**Note:** you do not see any actual fish swimming or anything. You just 'attack' the ground under water for a chance to get a fish.

**Current Available Catch:
[http://i.imgur.com/i6HT9oP.png](http://i.imgur.com/i6HT9oP.png) (logos for fish)


Savis Island Swordfish** - Common fish (1 raw meat)
**Hapis Island RazorJaw**  - Common fish (1 raw meat)
**Colon BlowFish**  - UnCommon fish (3 raw meat)
**Craggy Island Dorkfish** - Rare fish (5 raw meat), may or may not exist...no one knows. Legend has it, they may resemble rust developers.
**Random Item** - Now pulls up a Tier 3 loot crate.


actual raw catch will change to something else. chicken is just easy for now.

**Permissions:**

fishing.allowed - will allows authorized players / group to be able to fish.


````
oxide.grant <group|user> <name|id> fishing.allowed


Example: To allow the group player to use it. (all players)

oxide.grant group player fishing.allowed


Example 2 : To just allow a person to use it:

oxide.grant user "colon blow" fishing.allowed
````


**Default Configuration File:**

````
{

  "Allow Bonus from Attire": true,

  "Allow Bonus from Item": true,

  "Allow Bonus from Time of Day": true,

  "Allow Bonus from Weapon": true,

  "Allow Random Item Chance": true,

  "Bonus - From Attire (Percentage)": 10,

  "Bonus - From Items (Percentage)": 10,

  "Bonus - From Time of Day (Percentage)": 10,

  "Bonus - From Weapon (Percentage)": 10,

  "Chance - Default to Catch Fish (Percentage)": 10,

  "Chance - Get Random World Item (Percentage)": 1,

  "Icon - Url for Common Fish 1": "http://i.imgur.com/rBEmhpg.png",

  "Icon - Url for Common Fish 2": "http://i.imgur.com/HftxU00.png",

  "Icon - Url for Random Item": "http://i.imgur.com/y2scGmZ.png",

  "Icon - Url for Rare Fish 1": "http://i.imgur.com/jMZxGf1.png",

  "Icon - Url for UnCommon Fish 1": "http://i.imgur.com/xReDQM1.png",

  "Show Fish Catch Indicator": true

}
````


**Default Language File:**

````
{

  "missedfish": "You Missed the fish....",

  "commonfish1": "You Got a Savis Island Swordfish",

  "commonfish2": "You Got a Hapis Island RazorJaw",

  "uncommonfish1": "You Got a Colon BlowFish",

  "rarefish1": "You Got a Craggy Island Dorkfish",

  "randomitem": "You found something in the water !!!",

  "chancetext1": "Your chance to catch a fish is : ",

  "chancetext2": "% at Current time of : "

}
````


**Chat Commands:**

use /fishchance   -  returns the percentage chance you will catch a fish and the current time of day.

**Modifers to Chance to Catch fish:**

Using Wood Spear or Bow give player default chance to catch fish.

+ Bonus if using a Stone Spear or Crossbow

+ Bonus if wearing Boonie cap

+ Bonus for carrying Pookie Bear on you 
+ Bonus when Fishing around Sunrise and Sunset times.

**Coming Soon or Ideas  :**

- Adding Chances to pull up snake, gator or something bad. Might even hurt player or poison them.


- Idea : Using boxes placed in rivers / water as fish traps.

- Idea : Possibility of using mining quarry in river as a "Fish Wheel" to gather fish AFK.

- Idea : Fishing Levels? better chance to pull equipment at higher levels?

- Idea : Maps with "sunken treasure" marked on them?