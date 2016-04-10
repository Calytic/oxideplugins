**Optional plugins:
**[Enhanced Ban System](http://oxidemod.org/resources/enhanced-ban-system.693/)**
**[Jail](http://oxidemod.org/plugins/jail.794/)**

Strongly recommended plugin:
**[DeadPlayersList](http://oxidemod.org/plugins/deadplayerslist.696/)**

PROTECT EVEN MORE YOUR SERVER WITH:
**[RustDB](http://oxidemod.org/plugins/rustdb.749/)**


Console Commands:**

ac.fps => will show you how much time it took for the plugin to check all the players

ac.check NAME/STEAMID => manually check a player for fly/speed hack, this is mainly for testing purpoise as players are already checked 100% of time
**

Chat Commands:**

- /ac => to reset your current logs

- /ac player NAME/STEAMID => check if the target player has hack detections, and if so gives you a visual on the detections

- /ac radius XXXX => check if any hacks were made around you, and if so gives you a visual on all detections in your current radius

- /ac_list => list of all hack detections by steamid

- /ac_remove NAME/STEAMID => remove detections

- /ac_reset => reset all detections
**

Features:**

- anti **SpeedHacks**

- anti **Flyhack**

- anti **Wallhack**

- anti **Wallhack Kills**

- anti **Melee Speed**

- **C#** speed

- Checks players **every second** now

- Lag was taken in count in the plugin to **prevent false detections**.

- Ban also the owner of the** family share account**

**Default Configs:

WARNING: Config is in: oxide/config/AntiCheat.json**

````

{

  "Flyhack: activated": true,

  "Flyhack: Log": true,

  "Flyhack: Punish": true,

  "Flyhack: Punish Detections": 3,

  "MeleeOverRange Hack: activated": true,

  "MeleeSpeed Hack: activated": true,

  "Settings: Ban Also Family Owner": true,

  "Settings: FPS Ignore": 5,

  "Settings: Ignore Hacks for authLevel": 1,

  "Settings: Punish - 0 = Kick, 1 = Ban, 2 = Jail": 1,

  "SpeedHack: activated": true,

  "SpeedHack: Log": true,

  "SpeedHack: Punish": true,

  "SpeedHack: Punish Detections": 3,

  "SpeedHack: Speed Detection": 10.0,

  "Wallhack Kills: activated": true,

  "Wallhack Kills: Log": true,

  "Wallhack: activated": true,

  "Wallhack: Log": true,

  "Wallhack: Protect - Doors": true,

  "Wallhack: Protect - Floors": true,

  "Wallhack: Protect - Walls": true,

  "Wallhack: Punish": true,

  "Wallhack: Punish Detections": 2

}

 
````


**Anti Wallhack:**

Detects players that try to go through walls or doors

floors detect wallhackers that go UP not DOWN


**Anti Wallhack Kills:**

Should detect kills that were made through wallhack, detecting will show admins the detection & cancel the kill


**Anti Speedhack:**

Detects players that are moving too fast >10m/s as default

**Anti Flyhack:**

Detects players that are in the air and moving freely (so not falling)

**Anti** **Melee Speed:**

detect players that hit too fast with melee weapons

Cancels Damage
**
Anti **Melee Over Range:****

FIXED BY GARRY

**Visual Logs:**

Glitchy for the moment as you have to spam the commands to keep the logs going, i'll work on something to keep it showing when i find out how

I recommend that you daily check all your hack detections and remove/reset the logs to prevent the logs from getting too big

to easily get player detections by name i STRONGLY recommend you to install: **[DeadPlayersList](http://oxidemod.org/plugins/deadplayerslist.696/) 2.0.4**


**Please give me feedback on errors and fail/good bans


Know Issues:

1) HELPPP everybody is getting Punished!!!!**

- Check if you can still build on your server

If not, it means you've reached the max allowed entities from unity engine, a wipe is necessary, nothing else can be done.

- Check your colliders ("colliders" in the SERVER console, not player), if it's over 260 000, a wipe is needed, nothing else can be done.

- If you don't enter in both categories above, unload the plugin, seems like you have a bug on your server (or a lot of cheaters). Try using the plugin on next server wipe.