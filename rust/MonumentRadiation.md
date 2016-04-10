Basic plugin to create zones of radiation at designated monuments. Has a optional built in radiation timer to turn it on/off, and message's for if a player walks into a radiation zone

**Options**

Radiation Zones - Turn on or off radiation at this monument

Zone Size - Adjust zone size

**Chat Commands**

/mr_clearall - Removes all zones

/mr_list - List all zones into console

/mr - Displays time left until radiation is activated/deactivated

**Console Commands**

mr_clearall - Removes all zones

mr_list - List all zones into console


** To use this with HapisIsland you must set "Using Hapis Island" to true in the config

**Config**

````

{

  "Options - Message - Use enter message": true,

  "Options - Message - Use leave message": false,

  "Options - Radiation amount - Airfield": 100.0,

  "Options - Radiation amount - Lighthouses": 100.0,

  "Options - Radiation amount - Military Tunnels": 100.0,

  "Options - Radiation amount - Powerplant": 100.0,

  "Options - Radiation amount - Rad-towns": 100.0,

  "Options - Radiation amount - Satellite": 100.0,

  "Options - Radiation amount - Sphere Tank": 100.0,

  "Options - Radiation amount - Trainyard": 100.0,

  "Options - Radiation amount - Warehouses": 100.0,

  "Options - Radiation amount - Water Treatment Plant": 100.0,

  "Options - Radiation Zones - Airfield": false,

  "Options - Radiation Zones - Lighthouses": false,

  "Options - Radiation Zones - Military Tunnels": false,

  "Options - Radiation Zones - Powerplant": false,

  "Options - Radiation Zones - Rad-towns": true,

  "Options - Radiation Zones - Satellite": false,

  "Options - Radiation Zones - Sphere Tank": false,

  "Options - Radiation Zones - Trainyard": false,

  "Options - Radiation Zones - Warehouses": false,

  "Options - Radiation Zones - Water Treatment Plant": false,

  "Options - Timers - Amount of time activated (mins)": 45,

  "Options - Timers - Amount of time deactivated (mins)": 15,

  "Options - Timers - Broadcast radiation status": true,

  "Options - Timers - Random - Off maximum (mins)": 60,

  "Options - Timers - Random - Off minimum (mins)": 25,

  "Options - Timers - Random - On maximum (mins)": 5,

  "Options - Timers - Random - On minimum (mins)": 30,

  "Options - Timers - Use radiation activation/deactivation timers": true,

  "Options - Timers - Use random timers": false,

  "Options - Using Hapis Island": false,

  "Options - Zone Size - Airfield": "85",

  "Options - Zone Size - Lighthouses": "15",

  "Options - Zone Size - Military Tunnels": "90",

  "Options - Zone Size - Powerplant": "120",

  "Options - Zone Size - Rad-towns": "60",

  "Options - Zone Size - Satellite": "60",

  "Options - Zone Size - Sphere Tank": "50",

  "Options - Zone Size - Trainyard": "100",

  "Options - Zone Size - Warehouses": "15",

  "Options - Zone Size - Water Treatment Plant": "120"

}

 
````