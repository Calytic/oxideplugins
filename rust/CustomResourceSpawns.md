Create custom resource spawn points. All placed resources are on a refresh timer, and will respawn in the same place when you restart the server.

**Chat Commands**
/crs - Displays commands
/crs resources - Puts available resource types and ID's in your console
/crs add ## - Activates the resource tool. Press fire to create new spawn locations for resource ID ##. Once done type /crs to end
/crs remove - Removes the resource you are looking at
/crs near ## - Shows all custom resource's in a radius of ##
/crs wipe - Remove all custom resource's

**Permission**
customresourcespawns.admin - Permission if no auth access

**Config**

````
{

  "Options - Authlevel required": 1,

  "Options - Resource refresh timer (mins)": 15

}
````