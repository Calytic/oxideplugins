**Permission:**

autoclean.admin

****How to Add Permission:****

/grant user UserName autoclean.admin

or

/grant group admin autoclean.admin (If you have an admin group)
**
**Commands:****

/clean
I provided this command as a manual way to do a clean interval, in case you want too 

**Information:**

This plugin will remove all objects that are **placed** outside of claimed areas automatically. This can be used to prevent the need for wipes, or to maintain high server performance.


All objects outside of a claimed area will be added to a list. All objects are checked each time it does an UpdateInterval. If that same object is still in an unclaimed area after being checked the same amount of times as your IntervalsBeforeCleaning, the object will be removed from the server. If at any point an Ownership Stake is placed down, all items within the claimed area are safe.


With the new changes of Ownership Stakes, when an ownership stake runs out of Amber, that area will become unclaimed, so this will also remove all old buildings.


As this clean works off of interval checking, if you place an object down, and 5 intervals later another is placed, there will need to be 5 interval checks after the first one is removed, before the second one will be.


This will remove all placed items, like buildings, workbenches, lockers, etc.


Cars, and Quads will not be cleaned.

**Config File:**

````
{

  "IntervalsBeforeCleaning": 24,

  "ShowConsoleMessages": true,

  "UpdateIntervalSeconds": 7200

}
````

Using the default config as an example, all placed items are checked every 2 hours. After they are checked 24 times they are removed from the game. If at any point the objects have an ownership stake placed, they will be removed from the checking list and are safe until that ownership stake either rots or is destroyed.


By default objects take 48 hours to be cleaned.

**Note:**

The permission is only used for the manual clean command (/clean) the auto cleaning will start and run automatically.