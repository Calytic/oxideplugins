THIS IS FOR LEGACY OXIDE 2.0


Complete rewrite in C# for Oxide 2.0

= MUCH faster then Lua 
= You can try to keep the plugin set to permanent


( For Oxide 1.18 latest compatible version is: 1.5.4
[R-AntiCheat for Rust Legacy - Version History | Oxide](http://oxidemod.org/plugins/r-anticheat.632/history) )

Features:

- Choose to Kick or/and Ban

- Supports EnhancedBanSystem from oxide 1.18 (dunno if it's relevent ...)

- Anti Speedhack

- Anti Walkspeedhack

- Anti FlyHack

- Anti Superjump

- Anti Autoloot

- Anti MassRadiation (see under)

- Anti BlueprintUnlocker

- Anti CeilingHack Removal

- Anti OverKill (Long Range kills)

- Anti Wallhack

- Anti SleepingBag through Walls

- Fully configurable (for the anti cheat part)

Anti Speedhack/WalkSpeedhack/Superjump:


CONSOLE Commands:

ac.check Player/SteamID => Checks a specific player (can be an admin)

ac.checkall => Checks all online players

ac.reset => Reset the database and checks all players

Permissions / SpeedDetection Ignore:

Players with admin permissions will be ignored by the Speedhack detections

Admins are: rcon.login & oxide permission: cananticheat
You can grant a user permission by using:
oxide.grant user <username> <permission>

To create a group:
oxide.group add <groupname>

To assign permission to a group:
oxide.grant group <groupname> <permission>

To add users to a group:
oxide.usergroup add <username> <groupname>

To remove users permission:
oxide.revoke <userid/username> <group> <permission>
Click to expand...Anti Speedhack:
Permanent Check: optional

Detect players that run too fast in a ROW (everything is in m/s)

Min detection speed default is: 11.0

Max detection speed default is 25.0 (facepunch blocks at 20.0 so no need to go higher as facepunch will kick the guy before this plugin does)

Max height difference allow: 8.0 (this is usefull for players that slide on mountains)

Punish is set at 3 or more detections. (4 seconds as the first one it just informative and doesn't trigger a detection)

Anti Walkspeedhack:
Permanent Check: optional

Detect players that walk too fast in a ROW (everything is in m/s)

Min detection speed default is: 5.0

Max detection speed default is 15.0

Max height difference allow: 8.0 (this is usefull for players that slide on mountains)

Punish is set at 3 or more detections. (4 seconds as the first one it just informative and doesn't trigger a detection)

Anti Superjump:
Permanent Check: optional

Detect players that makes superjumps (plugin detects if there is ground under him or not to prevent teleportation fails)

Minimum Height default is: 4.0

Maximum Distance default: 25.0 (to prevent teleportations fails also)

Time before the superjump detections gets reseted: 300.0

Punish is set at 2 or more detections.

Anti Wallhack:
Permanent Check: YES

When a player gets killed, the anti wallhack checks if it is a valid kill. If a building (wall or ceiling) are detected on the way it will negate the kill. If that player gets killed AGAIN through a wall/ceiling in a short period of time, the attacker will get punished.

As i was aware that my previous anti wallhack didnt take in count boxes and stuff like that, some hackers would just go high up in the sky, and start shooting at all boxes destroying them, this wont be possible now as it checks for players & deployables.

No Recoil:
Permanent Check: YES

Checks when players do kills, if when they do those kills the recoil is still the same during the kill and right after it, then it detects the no recoil.

There is a minimum distance for that (40m)

You can choose the minimum kills for punishement, and also the minimum ratio norecoil detections / total kills that were checked


AntiMassRadiation: 
Permanent Check: YES

Deactivated atm

AntiFlyhack:
Permanent Check: optional

Flyhack detects when a player is not on the ground and when he is not it checks that the player is falling correctly.

Max Drop Speed before ignoring (m/s): is the the fall speed until the plugin ignores players (it's set very low, as it doesn't really matter as now the anti no falldamage dont use a reduced speed of fall, but actually deactivate the falldamage, this anti flyhack just detects players that flyhack,  and not NoFallDamage)

BlueprintUnlocker:
Permanent Check: YES

Punish players that try to use blueprints that they didn't learn.


Autoloot:
Permanent Check: YES

Punish a player that loots an airdrop 2+ times in less then 1sec from a big distance.

CeilingHack:
Permanent Check: YES

Checks if a player suddently drops under a ceiling when connecting.

This is probably the easiest hack to use, so a lot of people use it, and will stay undetectable by VAC until the end XD

Sleeping Bag Hack:
Permanent Check: YES

Checks if players spawns sleeping bags (and beds) threw walls or doors.

It will always delete the sleeping bag,

and as optional you can punish the player.


OverKill:
Permanent Check: YES

Checks all players kills and see if the distances for the kills are too big or not.

I've set a default max range that should be allowed for every weapons, it's my opinion, and you may choose any distances you want. (Of course i didnt set the admin weapons ^^)

Default weapon distances are:
"9mm Pistol": 80.0,

    "Bolt Action Rifle": 250.0,

    "HandCannon": 20.0,

    "Hatchet": 1.0,

    "Hunting Bow": Inifinity ,

    "M4": 140.0,

    "MP5A4": 80.0,

    "P250": 120.0,

    "Pick Axe": 1.0,

    "Pipe Shotgun": 80.0,

    "Revolver": 80.0,

    "Rock": 1.0,

    "Shotgun": 30.0,

    "Stone Hatchet": 1.0
Click to expand...Config file: AntiCheat.json

````
{

  "AntiMassRadiation: activated": true,

  "AutoGather: activated": true,

  "AutoGather: Punish ": true,

  "Autoloot: activated": true,

  "Autoloot: Punish": true,

  "BlueprintUnlocker: activated": true,

  "BlueprintUnlocker: Punish": true,

  "CeilingHack: activated": true,

  "CeilingHack: Punish ": true,

  "FlyHack: activated": true,

  "FlyHack: Detections needed before punishment": 3.0,

  "FlyHack: Max Drop Speed before ignoring (m/s)": 5.0,

  "FlyHack: Punish": true,

  "Messages: All players being checked": "AntiCheat: Now checking all players",

  "Messages: Broadcast Message to Player on Hacker Punishement": "[color #FFD630] {0} [color red]tried to cheat on this server!",

  "Messages: Data Reseted": "AntiCheat: Data was resetted, all players are now being checked again",

  "Messages: No Access": "AntiCheat: You dont have access to this command",

  "Messages: No player found": "AntiCheat: No player found with this name or steamid",

  "Messages: Player being checked": "AntiCheat: {0} is now being checked",

  "NoRecoil: activated": true,

  "NoRecoil: Min Distance For Check ": 40.0,

  "NoRecoil: Punish ": true,

  "NoRecoil: Punish Min Kills": 5,

  "NoRecoil: Punish Min Ratio in %": 33,

  "OverKill: activated": true,

  "OverKill: Detections before punish": 2.0,

  "OverKill: Max Distances": {

    "9mm Pistol": 85.0,

    "Bolt Action Rifle": 255.0,

    "HandCannon": 25.0,

    "Hatchet": 10.0,

    "Hunting Bow": 255.0,

    "M4": 145.0,

    "MP5A4": 85.0,

    "P250": 125.0,

    "Pick Axe": 10.0,

    "Pipe Shotgun": 85.0,

    "Revolver": 85.0,

    "Rock": 10.0,

    "Shotgun": 35.0,

    "Stone Hatchet": 10.0

  },

  "OverKill: Punish ": true,

  "OverKill: Reset Timer ": 600.0,

  "Settings: Broadcast Bans to Players": true,

  "Settings: Broadcast Detections to Admins": true,

  "Settings: Check Time (seconds)": 3600.0,

  "Settings: Permanent Check": true,

  "Settings: Punish by Ban": true,

  "Settings: Punish by Kick": true,

  "Sleeping Bag Hack: activated": true,

  "Sleeping Bag Hack: Punish ": true,

  "SpeedHack: activated": true,

  "SpeedHack: Detections needed in a row before Punishment": 3.0,

  "SpeedHack: Max Height difference allowed (m/s)": 8.0,

  "SpeedHack: Maximum Speed (m/s)": 25.0,

  "SpeedHack: Minimum Speed (m/s)": 11.0,

  "SpeedHack: Punish": true,

  "SuperJump: activated": true,

  "SuperJump: Detections needed before punishment": 2.0,

  "SuperJump: Maximum Distance before ignore (m/s)": 25.0,

  "SuperJump: Minimum Height (m/s)": 4.0,

  "SuperJump: Punish": false,

  "SuperJump: Time before the superjump detections gets reseted": 300.0,

  "WalkSpeedHack: activated": true,

  "WalkSpeedHack: Detections needed in a row before Punishment": 3.0,

  "WalkSpeedHack: Max Height difference allowed (m/s)": 8.0,

  "WalkSpeedHack: Maximum Speed (m/s)": 15.0,

  "WalkSpeedHack: Minimum Speed (m/s)": 6.0,

  "WalkSpeedHack: Punish": true,

  "Wallhack: activated": true,

  "Wallhack: Punish ": true

}
````

Anti Mass Radiation

Now included inside oxide


Before:

After:


Near future / ToDo List:

- Aimbot?

- Anti Nofall damage

- sleep kill

- box destroy