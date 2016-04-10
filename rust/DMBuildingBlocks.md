Allows you to change whether specific building parts are protected from being damaged.

**Update:** Added in game chat command to change all config variables to true or false for authorized users.

**Note:** As requested by some, this plugin will allow you to protect Foundations, Pillars and Floors from being damaged. Just as in legacy. Just set them to true. And they take no damage.


By default, all Building blocks are not protected. Change False to True to protect them.

**Default Config:**

````
{

  "ProtectDoorway": false,

  "ProtectFloor": false,

  "ProtectFloorFrame":false,

  "ProtectFloorTriangle": false,

  "ProtectFoundation": false,

  "ProtectFoundationSteps": false,

  "ProtectFoundationTriangle": false,

  "ProtectLowWall": false,

  "ProtectPillar": false,

  "ProtectRoof": false,

  "ProtectStairsLShaped": false,

  "ProtectStairsUShaped": false,

  "ProtectWall": false,

  "ProtectWallFrame": false

  "ProtectWindowWall": false

}
````


**Permissions to use Chat Commands:**

dmbuildingblocks.admin


````
oxide.grant <group|user> <name|id>  dmbuildingblocks.admin


Example: To allow the admin group to use it.

oxide.grant group admin dmbuildingblocks.admin


Example 2 : To just allow a person to use it:

oxide.grant user "colon blow" dmbuildingblocks.admin
````


**Chat Commands:**

To set any of the config variables. all you need to do is type /variable [true or false].

Example 1: type /ProtectRoof True

Example 2: type /ProtectRoof False

Example 3: type /ProtectDoorway true


you will get a in game reply that you have set it to the desired true or false.