**Features:**

- Choose how many crates an Airdrop can drop (Min to Max)

- Choose where the airdrop will land Supply Crates

- Automatically randomise where airdrops go (no more airdrops only in center of the map)

- Send an airdrop to selected location

- Send an airdrop to a selected player

- Send mass airdrops

- Show drop locations (or not)
**Console Commands:**

- airdrop.massdrop NUMBER => call mass airdrops

- airdrop.topos X Y Z => send an airdrop to that location (carefull with the Y, if you set it too low the airdrop will be close to the ground , or might even drop under the map)

- airdrop.toplayer PLAYER => send an airdrop to that player
**Configs:**

````
{

  "Drop": {

    "MinX": "-500",

    "MaxX": "500",

    "MinZ": "-500",

    "MaxZ": "500",

    "MinY": "200",

    "MaxY": "300",

    "MinCrates": "1",

    "MaxCrates": "3",

    "MinDropCratesInterval": 3,

    "MaxDropCratesInterval": 10,

    "ShowDropLocation": true

  },

  "Airdrop": {

    "Speed": "40"

  }

}
````

Airdrop Speed => 40 is the default airdrop speed, you can make them go faster or slower.

Drop MinX,MaxX,MinZ,MaxZ => those are set automatically on creation of your config file, it corresponds to: (server.worldsize/2 - 500).

So a worldsize of 6000, will have the limits between -2500 and 2500.

This was made like that so airdrops don't drop in the ocean.

You can freely edit this if you want, just be carefull not to drop outside the map 
Drop MinY, MaxY => default are 200 and 300, it's the height where airdrops should fly between.

Drop MinCrates, MaxCrates => number of crates that each airdrop should throw. (on rust default is 1 for min and max ). So be carefull if you put Max to more then 1, try to give less loots maybe with the Airdrop Settings, unless you want a mass loot.

Drop MinDropCratesInterval, MaxDropCratesInterval => time for the airdrop to wait before sending another crate (if maxcrates > 1).

Drop ShowDropLocation => everytime an airdrop is sent, show where it will drop.