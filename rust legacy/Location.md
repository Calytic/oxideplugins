**OPTIONAL: **
**[Share Database](http://oxidemod.org/plugins/share-database.935/) 1.0.0+**
**

Features:**

- Set your own** location points** and **names**

- use **/location** to know the coordinates of your location and the name

- for admins only or everyone

- optional: use** Share plugin** for players to share their locations

**Commands:**

- /location => Get your location

- /location TARGET => Get a target's location (for admins or players that use Share)

**Configs Location.json:**

````

{

  "Locations: List": [

    [

      "Hacker Valley South",

      5907,

      -1848

    ],

    [

      "Hacker Mountain South",

      5268,

      -1961

    ],

    [

      "Hacker Valley Middle",

      5268,

      -2700

    ],

    [

      "Hacker Mountain North",

      4529,

      -2274

    ],

    [

      "Hacker Valley North",

      4416,

      -2813

    ],

    [

      "Wasteland North",

      3208,

      -4191

    ],

    [

      "Wasteland South",

      6433,

      -2374

    ],

    [

      "Wasteland East",

      4942,

      -2061

    ],

    [

      "Wasteland West",

      3827,

      -5682

    ],

    [

      "Sweden",

      3677,

      -4617

    ],

    [

      "Everust Mountain",

      5005,

      -3226

    ],

    [

      "North Everust Mountain",

      4316,

      -3439

    ],

    [

      "South Everust Mountain",

      5907,

      -2700

    ],

    [

      "Metal Valley",

      6825,

      -3038

    ],

    [

      "Metal Mountain",

      7185,

      -3339

    ],

    [

      "Metal Hill",

      5055,

      -5256

    ],

    [

      "Resource Mountain",

      5268,

      -3665

    ],

    [

      "Resource Valley",

      5531,

      -3552

    ],

    [

      "Resource Hole",

      6942,

      -3502

    ],

    [

      "Resource Road",

      6659,

      -3527

    ],

    [

      "Beach",

      5494,

      -5770

    ],

    [

      "Beach Mountain",

      5108,

      -5875

    ],

    [

      "Coast Valley",

      5501,

      -5286

    ],

    [

      "Coast Mountain",

      5750,

      -4677

    ],

    [

      "Coast Resource",

      6120,

      -4930

    ],

    [

      "Secret Mountain",

      6709,

      -4730

    ],

    [

      "Secret Valley",

      7085,

      -4617

    ],

    [

      "Factory Radtown",

      6446,

      -4667

    ],

    [

      "Small Radtown",

      6120,

      -3452

    ],

    [

      "Big Radtown",

      5218,

      -4800

    ],

    [

      "Hangar",

      6809,

      -4304

    ],

    [

      "Tanks",

      6859,

      -3865

    ],

    [

      "Civilian Forest",

      6659,

      -4028

    ],

    [

      "Civilian Mountain",

      6346,

      -4028

    ],

    [

      "Civilian Road",

      6120,

      -4404

    ],

    [

      "Ballzack Mountain",

      4316,

      -5682

    ],

    [

      "Ballzack Valley",

      4720,

      -5660

    ],

    [

      "Spain Valley",

      4742,

      -5143

    ],

    [

      "Portugal Mountain",

      4203,

      -4570

    ],

    [

      "Portugal",

      4579,

      -4637

    ],

    [

      "Lone Tree Mountain",

      4842,

      -4354

    ],

    [

      "Forest",

      5368,

      -4434

    ],

    [

      "Rad-Town Valley",

      5907,

      -3400

    ],

    [

      "Next Valley",

      4955,

      -3900

    ],

    [

      "Silk Valley",

      5674,

      -4048

    ],

    [

      "French Valley",

      5995,

      -3978

    ],

    [

      "Ecko Valley",

      7085,

      -3815

    ],

    [

      "Ecko Mountain",

      7348,

      -4100

    ],

    [

      "Zombie Hill",

      6396,

      -3428

    ]

  ],

  "Messages: Help": "Show your current location: /location",

  "Messages: Location Message, 0 is the playername, 1 2 3 are x, y, z coordinates, and 4 is the name of the location": "{0} is located @ {1} {2} {3} - near {4}",

  "Messages: Not Allowed": "You are not allowed to use this command.",

  "Messages: Not Allowed To Target This Player": "You are not allowed to get the target's location",

  "Settings: Allow Chat Command For Players": true,

  "Settings: Allow players to target other players (Share Plugin needed)": true

}

 
````