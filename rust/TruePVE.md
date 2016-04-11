TruePVE is a plugin aimed to improve the default server PVE mode (server.pve = true) for servers who wish to truly be PVE.

Please note, TruePVE is designed to run on servers with "**server.pve = false**" - running on servers with "server.pve = true" can cause undesired behavior!

**Features:**

- Players cannot hurt other players (includes traps, mines, and barricades)

- Players cannot damage structures

- No damage reflection

- Players cannot loot sleepers (configurable)

- Players cannot loot other players' corpses (configurable)

- Turrets cannot hurt players (configurable)

- Locked doors and boxes are indestructible (like [VisionLocks](http://oxidemod.org/plugins/visionlocks.1654/)), and unlocked doors/boxes can be configured to be indestructible

- Sleeping bags and beds are indestructible (configurable)

- Prevent heli from damaging structures (configurable)

- Decay modifier - disabled on detection of [TwigsDecay](http://oxidemod.org/plugins/twigsdecay.857/)

- Damage and Loot hooks for other plugins to tie into (to override default TruePVE damage/loot handling)

**Usage **(console)**:**
tpve.set [option] [value] - Set value of specified option
tpve.get [option] - Get value of specified option
tpve.desc [option] - Describe specified option
tpve.list - List available options
tpve.def - Restore default configuration
tpve.version - Display version

**Options** (default **bold**)**:**
barricade (**true**/false) - Enable/disable damage to barricades
corpse (true/**false**) - Enable/disable looting other players' corpses
decay (float) - Decay rate modifier (default 1.0 or 100%) - 0.0 is no decay
heli (**true**/false) - Enable/disable damage to structures by heli rockets/napalm
hookdamage (**true**/false) - Enable/disable OnEntityTakeDamage hook processing - Setting to **false** will disable TruePVE damage checks!
hookloot (**true**/false) - Enable/disable loot processing hooks (CanLootPlayer, OnLootPlayer) - Setting to **false** will disable TruePVE loot checks!
sleeper (true/**false**) - Enable/disable looting sleepers
sleepingbag (true/**false**) - Enable/disable damage to sleeping bags/beds
suicide (**true**/false) - Enable/disable suicide (F1 > kill)
turret (true/**false**) - Enable/disable turret damage against players
unlocked (**true**/false) - Enable/disable damage to unlocked doors/boxes

**Hooks:**
HandleDamage - Handler for damage which cancels damage if value returned from AllowDamage is **false**
AllowDamage - Returns **true/false** regarding whether the passed entity should receive damage based on the current configuration
HandleLoot - Handler for looting which stops looting if value returned from AllowLoot is false
AllowLoot - Returns **true/false** regarding whether a player can loot the passed entity based on the current configuration

**Implementation:**

In order to implement TruePVE in a chain of damage or loot handling, set "hookdamage": false and/or "hookloot": false in the configuration - this will disable standard hooks from processing damage or looting.  Then, if TruePVE is desired to be the catch-all* handler for damage/looting, modify primary damage/loot handling plugins to call HandleDamage and/or HandleLoot.


* For example, I am using [ZonesManager](http://oxidemod.org/plugins/zones-manager.739/) to check for zone behavior, then hooking to TruePVE as "default" behavior where needed.  Zones which conflict with normal PVE behavior (like a PVP-enabled zone) can override PVE behavior by allowing ZonesManager to skip the TruePVE hooks altogether. See [this post](http://oxidemod.org/threads/truepve.16909/page-2#post-191218) for an example.

**Potential improvements:**

- [PopupNotifications](http://oxidemod.org/plugins/popup-notifications.1252/) implementation

- More options