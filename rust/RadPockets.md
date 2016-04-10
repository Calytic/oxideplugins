Turn your server into a irradiated wasteland by creating pockets of radiation randomly around your map.


The number and size of zones are random, with minimum and maximum values in the config. Zones will be automatically generated on startup and will be saved (so they are always in the same location)

****** Its recommended that you install this after a wipe, that way players will build around the radiation. If not you can remove zones that land on players buildings using a chat command.

**Chat Commands**

/rad_tpnear - Teleport to the closest zone

/rad_removenear - Remove the closest zone

/rad_remove - Remove a zone using its ID

/rad_clearall - Remove all zones from the map

/rad_list - List all the zones on the map

/rad_create - Create a zone on your position

-- Optional arguments to set custom radius and radiation level

-- /rad_create radius XX

-- /rad_create radiation XX

-- /rad_create radius XX radiation XX

**Config**

````
{

  "Options - Maximum zone radius": 60,

  "Options - Maximum zones to create": 30,

  "Options - Minimum zone radius": 15,

  "Options - Minimum zones to create": 15,

"Options - Minimum radiation level": 100,

"Options - Maximum radiation level": 1000

}
````