Since Garry has implemented 10 minute rotation after upgrading a building part a couple weeks ago, this mod now only allows you to demolish a building part after upgrading it from **twigs**. This is by default turned off for doors, but can be turned on in the configuration file.

In addition this mod allows admins to rotate locked building parts for roughly one minute by hitting it once using **left mouse-button** with their hammer. This feature can also be turned off in the configuration file.

You can edit the plugin to your preferences by editing the following lines in RotateOnUpgrade.json:

````

  "allowAdminRotate": true,

  "allowDemolish": true,

  "allowDemolishDoors": false,

  "amountOfMinutesAfterUpgrade": 10,

 
````

For example: If you don't want admins to be able to rotate building parts then simply change the value of allowAdminRotate from "true" to "false".  You can also change the amount of time people have after upgrading by editing the "amountOfMinutesAfterUpgrade" field. You can also have infintie rotation of building blocks if you set this value to 0 but we highly discourage using this on your server.


*Also be sure to reload the mod after editing the configuration file*

*****Version 1.3.0 requires deletion of your old configuration file*****