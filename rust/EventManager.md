**REQUIRES**
**[Spawns Database](http://oxidemod.org/plugins/spawns-database.720/) **
**[Kits](http://oxidemod.org/plugins/kits.925/) **
**[Zones Manager](http://oxidemod.org/plugins/zones-manager.739/)**

**Compatible:**
[Rust:IO FriendlyFire](http://oxidemod.org/plugins/rust-io-friendlyfire.840/) 

**THIS IS A BETA VERSION

STUFF MIGHT BE MISSING (MESSAGES, COMMANDS)

I NEED FEEDBACK SO I CAN IMPROVE THE PLUGIN!!**

**Current Mods:**
**[Event - Deathmatch](http://oxidemod.org/plugins/arena-deathmatch.741/)**
**[Event - Battlefield](http://oxidemod.org/plugins/event-battlefield.1311/)**
**[Team Deathmatch](http://oxidemod.org/plugins/team-deathmatch.1484/)**

**Console Commands

Configure:**

- event.game GAMENAME => currently the only one avaible is: Deathmatch (requires: [Arena - Deathmatch](http://oxidemod.org/plugins/arena-deathmatch.741/))

- event.spawnfile SPAWNFILENAME => your arena spawnfile that you made with the spawns database plugin.

-  event.minplayers XX => Sets the minimum players needed to start the event (usefull only if using the auto event)

-  event.maxplayers XX => Sets the maximum players allowed (auto event doesnt need to be used for this feature)

**Manage your arena:**

-  event.open => let players register for the arena

-  event.start => start the arena

-  event.close => close registration for the arena

-  event.end => end the arena

-  event.launch => launchs the auto event

-  event.kit XX => sets a new kit for the current game

**Manages Tokens & Rewards:**

- event.reward add/list/remove => to manage rewards

- event.reward give/take/set/check => to manage tokens of players

- event.reward clear yes => to clear all tokens from players

**Chat Commands**

Players Commands:

- /event_join => join the event if it's openned

- /event_leave => leave the event

- /event => show the current status of the event

**Installation:**

- Install & Configure a mod, **load it AFTER the event manager  plugin**.

- use event.game GAMENAME to activate the mod (ex: Deathmatch)

- use event.spawnfile FILENAME to activate the spawn points

- look under Zone Management to know how to make/edit a zone

- Open your event for inscriptions: event.open

- When you have enough players do: event.start

**Zone Management:**

1) /zone_add ZONENAME

2) Then when the zone when created use: /zone_edit Deathmatch

This will let you edit the zone and put what ever you want as option.

recommended options are as follow:

/zone eject true undestr true autolights true nobuild true nodeploy true nokits true notp true killsleepers true nosuicide true nocorpse true nowounded true

3) Then only the Zone Management plugin will take care of saving options, you will not have to edit the config file for it anymore

4) In the Event plugins you will always find a place where to put the zone name. REMEMBER TO DO IT!!

In Deathmatch it is inside: "DeathMatch - Zone - Name"

**Features (what should be working):**

-  Save & TP Back players home

-  Save & Redeem players inventory

-  Most hooks for external plugins

-  Give Kits

- Auto matic reward system

**EXPERIMENTAL AUTO EVENTS!!!!!!!!**

To start the auto events use: event.launch

You should be able to use any of the management commands WHILE the auto event is on (event.start event.end, etc), the plugin should adapt to what you use ... SHOULD (experimental again!)


Default config (totally random, just to show how to make one), as the Battlefield gametype doesn't exist yet, you will have to erase it from the config ^^ (even if it doesn't matter, the plugin will detect if the configs are not right and if not will ignore the game and try another one)


Dont forgot to set an order when you create a new auto event: "0", "1", "2" (here is only 0 and 1)

````
"AutoEvents - Activated": true,

  "AutoEvents - Announce Open Interval": 30,

"AutoEvents - Config": {

    "0": {

      "closeonstart": "false",

      "gametype": "Deathmatch",

      "maxplayers": "10",

      "minplayers": "1",

      "spawnfile": "deathmatchspawnfile",

      "timelimit": "1200",

      "timetojoin": "30"

    },

    "1": {

      "closeonstart": "false",

      "gametype": "Battlefield2",

      "maxplayers": "30",

      "minplayers": "0",

      "spawnfile": "battlefieldspawnfile",

      "timelimit": null,

      "timetojoin": "0"

    }

  },

  ]
````

Announce Open Interval = how often the plugin should announce that the event is opened
closeonstart = if the plugin should close joining when the game has already started
maxplayers ...
minplayers = minimum players needed for the event to run

spawnfile is the spawnfile ...
timelimit = time until the event will shutdown (if a match is taking too long)
timetojoin = when the minimum players are reached, the plugin will give XXX more time to join before it starts

**Rewards:**

You can add item rewards or kit rewards,

event.reward add REWARDNAME COST ITEM/KITNAME AMOUNT

here are 2 examples:

event.reward add "Hunting Equipment" 10 "huntingkit" 1 => this will 1 hunting kit to the player for 10 tokens

event.reward add "Lanterns" 10 lantern 10 => this will give 10 lanterns for 10 tokens.


**What still needs to be done:**

nothing much now XD just fixing it

**Configs**

````

{

  "AutoEvents - Activate": false,

  "AutoEvents - Announce Open Interval": 30,

  "AutoEvents - Config": {

    "0": {

      "closeonstart": "false",

      "gametype": "Deathmatch",

      "maxplayers": "10",

      "minplayers": "1",

      "spawnfile": "deathmatchspawnfile",

      "timelimit": "1800",

      "timetojoin": "30"

    },

    "1": {

      "closeonstart": "false",

      "gametype": "Battlefield",

      "maxplayers": "30",

      "minplayers": "0",

      "spawnfile": "battlefieldspawnfile",

      "timelimit": null,

      "timetojoin": "0"

    }

  },

  "AutoEvents - Interval between 2 events": 600,

  "Default - Game": "Deathmatch",

  "Default - Spawnfile": "deathmatchspawns",

  "Messages - Error - Multiple players found": "Multiple players found",

  "Messages - Error - No players found": "No players found",

  "Messages - Event - Begin": "Event: {0} is about to begin!",

  "Messages - Event - Cancelled": "The Event was cancelled!",

  "Messages - Event - Closed": "The Event entrance is now closed!",

  "Messages - Event - End": "Event: {0} is now over!",

  "Messages - Event - Join": "{0} has joined the Event!  (Total Players: {1})",

  "Messages - Event - Left": "{0} has left the Event! (Total Players: {1})",

  "Messages - Event - MaxPlayersReached": "The Event {0} has reached max players. You may not join for the moment",

  "Messages - Event - MinPlayersReached": "The Event {0} has reached min players and will start in {1} seconds",

  "Messages - Event - Opened": "The Event is now open for : {0} !  Type /event_join to join!",

  "Messages - Event Error - Already Closed": "The Event is already closed.",

  "Messages - Event Error - Already Joined": "You are already in the Event.",

  "Messages - Event Error - Already Opened": "The Event is already open.",

  "Messages - Event Error - Already Started": "An Event game has already started.",

  "Messages - Event Error - Close&End": "The Event needs to be closed and ended before using this command.",

  "Messages - Event Error - No Games Undergoing": "An Event game is not underway.",

  "Messages - Event Error - No SpawnFile": "A spawn file must first be loaded.",

  "Messages - Event Error - Not In Event": "You are not currently in the Event.",

  "Messages - Event Error - Not Registered Event": "This Game {0} isn't registered, did you reload the game after loading Event - Core?",

  "Messages - Event Error - Not Set": "An Event game must first be chosen.",

  "Messages - Event Error - SpawnFile Is Null": "The spawnfile can't be set to null",

  "Messages - Permissions - Not Allowed": "You are not allowed to use this command",

  "Messages - Reward - Current": "You have {0} tokens",

  "Messages - Reward - Doesnt Exist": "This reward doesn't exist",

  "Messages - Reward - GUI Message": "You currently have <color=green>{0}</color> tokens.",

  "Messages - Reward - Help": "/reward \"RewardName\" Amount",

  "Messages - Reward - Message": "You currently have {0} for the /reward shop",

  "Messages - Reward - Negative Amount": "The amount to buy can't be 0 or negative.",

  "Messages - Reward - Not Enough Tokens": "You don't have enough tokens to buy {1} of {0}.",

  "Messages - Reward - Reward Description": "Reward Name: {0} - Cost: <color={4}>{1}</color> - {2} - Amount: {3}",

  "Messages - Status - Closed & End": "There is currently no event",

  "Messages - Status - Closed & Started": "The Event {0} has already started, it's too late to join.",

  "Messages - Status - Open": "The Event {0} is currently opened for registration: /event_join",

  "Messages - Status - Open & Started": "The Event {0} has started, but is still opened: /event_join",

  "Settings - authLevel": 1

}

 
````


**Please concentrate on giving me errors, error messages and WHEN the error occurred.**