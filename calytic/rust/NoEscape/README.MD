**Description** 


NoEscape prevents teleportation (and other commands) when a raid or combat is detected.

**Blocked Commands** 


* /home        (m-Teleportation)
* /tpr            (m-Teleportation)
* /remove     (Remover Tool)
* /t home #   (Magic Teleportation)
* /bgrade     (Automatic Build Grades)

**Permissions** 

This plugin uses Oxide's permission system. To assign a permission, use **grant user <username|steamid> <permission>** . To remove a permission, use **revoke user <username|steamid> <permission>** .


* noescape.raid.tpblock (blocks home/tpr near raid)
Ex. grant user Calytic noescape.raid.tpblock
Ex. revoke user Calytic noescape.raid.tpblock
Ex. grant group player noescape.raid.tpblock
* noescape.raid.removeblock (blocks remove near raid)
Ex. grant user Calytic noescape.raid.removeblock
Ex. revoke user Calytic noescape.raid.removeblock
Ex. grant group player noescape.raid.removeblock
* noescape.raid.bgradeblock (blocks bgrade near raid)
Ex. grant user Calytic noescape.raid.bgradeblock
Ex. revoke user Calytic noescape.raid.bgradeblock
Ex. grant group player noescape.raid.bgradeblock
* noescape.combat.tpblock (blocks home/tpr in combat)
Ex. grant user Calytic noescape.combat.tpblock
Ex. revoke user Calytic noescape.combat.tpblock
Ex. grant group player noescape.combat.tpblock
* noescape.combat.removeblock (blocks remove in combat)
 Ex. grant user Calytic noescape.combat.removeblock
 Ex. revoke user Calytic noescape.combat.removeblock
Ex. grant group player noescape.combat.removeblock
* noescape.combat.bgradeblock (blocks bgrade in combat)
 Ex. grant user Calytic noescape.combat.bgradeblock
 Ex. revoke user Calytic noescape.combat.bgradeblock
Ex. grant group player noescape.combat.bgradeblock

**Raid Block** 

Detects when a raid is occurring and block players from using teleport (or remover tool).


The raid "participants" whom may be blocked:

* The owner of the base
* The nearby clan-mates of the owner
* The nearby friends of the owner
* The raid initiator
* The nearby clan-mates of the raid initiator
* The nearby friends of the raid initiator
* Anyone nearby (including raiders and bystanders)

* raidBlock will detect raids and dispense command blocks depending on a variety of circumstances.
* ownerBlock will check if the owner of the damaged block is within the configured distance, they are blocked for the configured duration.
* clanShare will check if any clan-mates within configured distance and block them for the configured duration.
* friendShare will check if any friends within configured distance and block them for the configured duration.
* clanCheck will prevent clan-mates from blocking eachother accidentally.
* friendCheck will prevent friends from blocking eachother accidentally.
* blockAll will block everyone within configured distance for the configured duration. Note: this option overrides ownerBlock, friendShare, clanShare and raiderBlock entirely.

**Combat Block** 

Detects when a player has given or received damage to/from another player, then blocks one or both parties from using teleport (or remover tool) for the configured duration.

**Configuration** 


* raidBlock (default true)
Whether or not raiding will perform blocking
* raidDistance (default 100)
The distance the owner must be from where the raid is occurring (default 100 meters)

* raidDuration (default 300)
The length of time before the raid block is lifed (default 5 minutes)
* raidBlockNotify (default true)
Whether or not player is notified when they are raid blocked.

**Raid Block Options** 


* ownerBlock (default true)
Blocks owner of target block (if nearby)

* blockAll (default false)
Blocks everyone within configured distance (including raiders and bystanders).

* friendShare (default false)
Block nearby friends of owner

* friendCheck (default false)
Prevents friends from blocking eachother

* clanShare (default false)
Block nearby clan-mates of owner
* clanCheck (default false)
Prevents clan-mates from blocking eachother
* damageTypes
Choose what types of structure damage will block escape
* raiderBlock (default false)
Blocks player who initiated raid

* raiderClanShare (default false)
Blocks nearby clan-mates of raid initiator
* raiderFriendShare (default false)
Blocks nearby friends of raid initiator

**Combat Block Options** 


* combatBlock (default false)
Whether or not raiding will perform blocking
* combatDuration (default 180)
The length of time in seconds before the raid block is lifted (default 3 minutes)
* combatOnHitPlayer (default true)
Whether successfully hitting another player blocks you

* combatOnTakeDamage (default true)
Whether successfully being hit by another player blocks you
* combatBlockNotify (default false)
Whether or not a player is notified when they are combat blocked

**Misc. Settings** 


* cacheMinutes (default 1)
The number of minutes before friend and clan relationships are refreshed

**Default Configuration** 


````

{

Â  "blockAll": false,

Â  "blockOnDamage": true,

Â  "blockOnDestroy": false,

Â  "cacheMinutes": 1.0,

Â  "clanCheck": false,

Â  "clanShare": false,

Â  "combatBlock": false,

Â  "combatBlockNotify": false,

Â  "combatDuration": 180.0,

Â  "combatOnHitPlayer": true,

Â  "combatOnTakeDamage": true,

Â  "damageTypes": [

Â  Â  "Bullet",

Â  Â  "Blunt",

Â  Â  "Stab",

Â  Â  "Slash",

Â  Â  "Explosion"

Â  ],

Â  "friendCheck": false,

Â  "friendShare": false,

Â  "ownerBlock": true,

Â  "raidBlock": true,

Â  "raidBlockNotify": true,

Â  "raidDistance": 100.0,

Â  "raidDuration": 300.0,

Â  "raiderBlock": false,

Â  "raiderClanShare": false,

Â  "raiderFriendShare": false,

Â  "unblockOnDeath": true,

Â  "unblockOnRespawn": true,

Â  "unblockOnWakeup": false

}

Â 
````

**API** 

````
bool IsEscapeBlocked(BasePlayer target)

bool IsRaidBlocked(BasePlayer target)

bool IsRaidBlockedS(string target)

bool IsCombatBlocked(BasePlayer target)

bool IsCombatBlockedS(string target)


void StartRaidBlocking(string target)

void StopRaidBlocking(string target)

void StartCombatBlocking(string target)

void StopCombatBlocking(string target)

void StopBlocking(string target)
````