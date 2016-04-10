Create custom loot spawn points, using boxes of your choosing. Boxes are on a refresh timer, and will respawn in the same place when you restart the server.

**Chat Commands**
/cls - Displays commands
/cls boxes - Puts available loot box types and ID's in your console
/cls add ## - Create a new spawn location for loot box ID ##

/cls create name ## - Creates a new loot kit called 'name' using boxid '##' ex. /cls create testbox 15

/cls remove - Removes the box you are looking at
/cls near ## - Shows all custom loot boxes in a radius of ##
/cls wipe - Remove all custom loot boxes
/cls wipeall - Removes all loot boxes and loot kits

**Permission**
customlootspawn.admin - Permission if no auth access

**Config**

````
{

  "Options - Authlevel required": 1,

  "Options - Box refresh timer (mins)": 15

}
````