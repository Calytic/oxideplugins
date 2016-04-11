**VisionPVP ****version 0.4.2**

A PVP / PVE Controller for Rust

**Big Update: Player Punishment Removed!**

VisionPVP removes punishment from PVE and allows PVP and PVE to be controlled by time of day, event, randomly and more.


Currently VisionPVP features 7 different configurable modes and player broadcasts that can be edited by server admins.

**Important**

visionPVP no longer uses the Rust server.pve variable to change PVP and PVE mode. Instead, VisionPVP forces the server in PVP mode and then overrides the PVE switch when needed. This makes PVE mode slightly less weak. Before this update, players were punished for attacking other players, buildings or even their own homes and bases. Leaving most of the Rust population alienated from PVE mode all together because even building your own base could kill you if you needed to take down a wall. VisionPVP uses a softer approach and only negates damage from the attacked player when in PVE mode without to damage to the attacker.


Some other mods have tried thing similar ideas with bad side effects such as not being able to gather, or a full-on god mode protecting players from all damage, or only protecting against Melee damage. VisionPVP still allows all other types of damage (falling, starving, drowning, bear attacks, etc.) and only protects players against other players.


If you prefer the legacy mode you can change the configuration file

**Legacy Mode**

Default Rust Server Behavior and VisionPVP before v0.4.x


````
"pveMode": "server"
````


**VisionPVE Mode**

A more accepted Behavior; based on feedback and forums


````
"pveMode": "vision"
````


**Available Modes**

visionPVP currently supports 7 modes:



* pvp
* pve
* pvp-night
* pvp-day
* time
* random
* event


**PVP Mode**

Console Command:


````
visionPVP.pvp pvp
````

Players may hurt other players and buildings. This mode will be on all of the time. PVP mode works the same as Vanilla Rust with default settings. VisionPVP is not required to run your server this way, but was added simply for consistency and convenience.

**PVE Mode**

Console Command:


````
visionPVP.pvp pve
````

Players may not hurt other players. This differs from Rust PVE mode whereas VisionPVP does not punish players for attempting to attack each other. Punishment for damaging structures is also removed so raiding is still possible with VisionPVP even when in PVE mode.

**Night Mode**

Console Command:


````
visionPVP.pvp pvp-night
````

PVE mode during the day and PVP mode at night. VisionPVP will toggle PVP to be on only at night. During the day PVE is enforced and Players cannot hurt each other during the daytime hours. Daytime and Nighttime hours are determined by the server's Sun and cannot be controlled by VisionPVP. For more control over the time of day, use Time-Mode.

**Day Mode**

Console Command:


````
visionPVP.pvp pvp-day
````

PVE mode during the night and PVP mode during the day. VisionPVP will toggle PVE to only be at night. During the day, PVP mode is enforced and Players may hurt and kill other players. Leaving night for peace. Daytime and Nighttime hours are determined by the server's Sun and cannot be controlled by VisionPVP. For more control over the time of day, use Time-Mode.

**Random Mode**

Console Command:


````
visionPVP.pvp random
````

Random mode toggles PVP and PVE mode on randomly. This is controlled by two settings in the configuration that determine the parameters for when and for how long. Optionally a player warning can be used before the random change occurs giving players a heads up to prepare for the change. More about the settings for random mode can be found below in the configuration section.

**Time Mode**

Console Command:


````
visionPVP.pvp time
````

Time mode lets you set a start and stop time for PVP mode. The rest of the time the server remains in PVE mode. This gives you more control then the day/night cycles which are set by the server. The times are relative to in-game server time and in 24 hour format (1 to 24).

**Event Mode**

Console Command:


````
visionPVP.pvp event
````

Event mode allows you to set a default state of PVE or PVP and then when an event happens (currently only airdrops), VisionPVP will switch to the opposite mode for a certain duration which can be set in the configuration file. The switch will happen for a certain duration, also set in the configuration file. More on these settings in the configuration section below.


Event mode is also the default mode. Keeping the server in PVE mode until an event happens, then switching to PVP mode for 2 (in-game) hours.

**WARNING**

PVE does not prevent players from being looted or prevent buildings from being damaged.


VisionPVP is still in development. You may experience bugs as updates are added. For faster turnaround, please report problems here: [Build software better, together](https://github.com/VisionMise/visionPVP/issues/new)


Available at [visionmise.github.io/visionPVP](http://visionmise.github.io/visionPVP/)


A Plugin for OxideMode for Rust

[oxidemod.org](http://oxidemod.org/)

**Configuration**

For advanced reading:


Configurations now support upgrades and no longer wipe your current settings when VisionPVP or it's config are file is updated. Below is a sample configuration file. The settings are explained below the example.

**Sample Config**

````

    {

      "settings": {

        "version":      0.4.2,

        "config":       1.6.5,

        "pvpMode":      "event",

        "pveMode":      "vision",

        "random":       {

            "minimum":          "1",

            "maximum":          "12",

            "player_warning":   "2"

        },

        "pvptime":      {

            'pvp_start_time':   "18",

            'pvp_stop_time':    "6"

        },

        "event":    {

          "pvp_event_mode": "pvp",

          "pvp_duration":   "2"

        }

      }

    }

 
````


**Configuration Settings**
**pvpMode **

The current setting for VisionPVP. This value can be any of the valid 7 modes.


pvp

pve

pvp-night

pvp-day

time

random

event

**pveMode**

The PVE behavior. When set to vision the server will not punish players for engaging in player attacks or raiding. When set to server VisionPVP will act according to the rule of the Rust Dedicated Server.

**random**

When using a random time, you can specify a window of time in which a random hour may be picked to switch to PVP. The settings minimum and maximum define that window. If the current (in-game) time is noon, and the min and max are set to 1 and 12 respectively. Then a random hour to change to PVP will be chosen between 1 and 12 hours from now. That will continue in that fashion until you reset the random generator. The only values supported for each setting are 1 to 24.


To reset the random generator, use the below console command:


Console Command:


````
VisionPVP.rnd
````

A player warning can also be set to notify players that the change will occur. Set player_warning to anything from 0 to 24. It stores the number of hours before the change that the players are warned. The only values support are 0 to 24.



* 0 Gives no Warning
* 2 Gives a 2 hour warning
* 24 Gives a day's notice


**pvptime**

The time mode requires that a start and stop time be set for changing in to PVP mode. PVE mode will be enacted during the rest of the time. Setting pvp_start_time to 18 will start PVP at 6pm (in-game time). Setting pvp_stop_time to 6 will end PVP at 6am (in-game time). Values supported are 1 to 24.

**event**

The event mode is the setting that should be enacted when an event happens. The inverse will be enforced when an event is not happening and/or the duration limit is reached. The duration is set in (in-game) hours and starts counting from the start of an event, not the end.

**Resources**

Resources are strings used in messages sent to players. So when VisionPVP changes to PVP mode, it broadcasts that to chat. These messages are stored in the resources of your config. Resources are generated when the VisionPVP is added or upgrade on your server. Currently resources are not upgradeable so the latest changes will wipe your current resources. It is recommended that if you make changes here, you make a backup before starting you server. If you needed to replace or changes these to get them in you own language or just for the sake of customization, make sure you understand how to edit JSON config files. If you need help in this area, learn JSON before attempting changes to resources.

**Furture Development**

The future development is almost

````
(at an end) ? "sad panda" : "happy face";
````

Version 0.4.2 is the first Release Canidant. It meets all of my original goals and addresses all of the abiltiy I have (so far) in making a PVP controller. It may still contain bugs and needs more testing by you folks. Your feedback is how I get a bug-free release 1.0


Some of the remaining features include:



* Upgradable Resources, so their not wiped upon an update to the Mod or Config
* Bringing back optional Player Chat command to see what the current mode is (configurable by admins)
* Possible API for other modders. Not sure if there is interest here, or even what is required, but and API would allow other mods to take control of VisionPVP for whatever reason.
* Better code documentation and comments

Of course, as OxideMod2 and Rust change through their development, other changes may be needed to VisionPVP. But I hope to have full release (v1.0.0) to you guys before 2016.


Also working a new mod, more for modders then server admins called protorust. Its just for us JavaScript Modders. Though few of us, in the OxideMod/Rust community, I hope to bring a set of tools to make it easier to build mods in JavaScript. And if not, I'll use it to make another mod 
Thanks to my supporters, other modders for their examples. The folks on the forum, and a huge THANK YOU to Wulf.


--VisionMise