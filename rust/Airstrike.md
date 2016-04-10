Call an airstrike either by throwing a supply signal or using a chat command (for players with permission &/or admin)

**Chat Commands**
/callstrike - Will call a strike on your position
/callstrike "playername" - Will call a strike on a player
/togglestrike "on" or "off" - Turn on/off the supply signal strike funtion
/buystrike "option" - Allows a player to purchase a airstrike to their location using HQ metal, a Targeting Computer and some Flares, or if you use Economics, money. ** options are "metal" or "computer" or "money"
/buystrike squad "option" - Same as above but purchasing a squadron strike
/squadstrike - Will call 3 planes flying in a V formation

**Console Commands**

airstrike - Will call a strike to a random location

airstrike "playername" - Will call a strike on a player

airstrike x y z - Will call a strike to co-ordinates X Y Z

squadstrike- Will call a squadron strike to a random location

squadstrike "playername" - Will call a squadron strike on a player

squadstrike x y z - Will call a squadron strike to co-ordinates X Y Z

**Permissions**

airstrike.canuse - Will allow players to use the Supply Signal airstrike

airstrike.buystrike - Will allow players to purchase a airstrike

airstrike.admin - Will allow player to use the airstrike chat command

airstrike.mass - Will allow player to use the squadstrike chat command

**Using Cooldown**

To use a cooldown, these are the options your must modify

"Options - Use Cooldown" - Turns the function on or off // default false (off)

"Options - Cooldown timer" - The amount of time in seconds // default 3600 (1 hour)

"Options - Admin exempt from cooldown": - Whether admins are exempt from the cooldown funtion // default true
**

Calls for other devs**

````
bool isStrikePlane(CargoPlane plane) // returns true if plane is a strike plane


bool checkStrikeDrop(SupplyDrop drop) // returns true if a supply drop come from a strike plane
````


**Configuration**

````

{

  "Buy - Buy strike cost - Economics": 500,

  "Buy - Buy strike cost - Flare": 2,

  "Buy - Buy strike cost - HQ Metal": 1000,

  "Buy - Buy strike cost - Targeting Computer": 1,

  "Buy - Can purchase airstrike": true,

  "Buy - Can purchase squadstrike": true,

  "Buy - Use Cooldown": false,

  "Cooldown - Admin exempt from cooldown": true,

  "Cooldown - Cooldown timer": 3600,

  "Messages - Broadcast strike to all players": true,

  "Options - Minimum Authlevel": 1,

  "Options - Use Economics": true,

  "Plane - Plane distance before firing": 900,

  "Plane - Plane speed": 105,

  "Rockets - Accuracy of rockets": 1.5,

  "Rockets - Amount of rockets to fire": 15,

  "Rockets - Chance of fire rocket - 1 in... ": 4,

  "Rockets - Damage Modifier": 1.0,

  "Rockets - Default rocket type": "ammo.rocket.basic",

  "Rockets - Interval between rockets (seconds)": 0.6,

  "Rockets - Speed of rockets": 110.0,

  "Rockets - Use both rocket types": false,

  "Supply Signals - Change the supply strike to call a Squadstrike": false,

  "Supply Signals - Use supply signals to call strike": true

}

 
````


**Squadron Strike**