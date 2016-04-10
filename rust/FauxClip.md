**WARNING:** Server Antihack will probably flag noclip mode. Use with caution if you are using these server settings or any plugin that detects noclip.  But if you want players to noclip... why you got them on anyhow 

**Update: (3/11/16)**
- Updated plugin to reset Antihack violation points to Zero while flying. Should prevent players from being kicked if Antihack is enabled to kick.

**Performance Note:**

FauxClip is very dependent on server FPS and Player localized FPS. Freshly wiped servers, FauxClip runs pretty smooth for many players.  Servers that are fuller, lower FPS, a lot of colliders and players with low FPS will see a lot of jitter in movement.

**Summary :**

This plugin allows authorized players to simulate the experience of the Admin NoClip mode.

Once activated, players will be immune to all damage and can move around in a simulated noclip mode.

Players are visible at all times to everyone and tool cupboard authorization check while in the NoClip mode is in place. You will get a message "Entering restricted Area!" and  the NoClip mode will be toggled off while in the zone. Unless you have permission to bypass cupboard checks.

Toggling noclip mode off, players will get a default 3 second grace landing period until they will start to take damage again.

**Permissions :

````
oxide.grant <group|user> <name|id> fauxclip.allowed

oxide.revoke <group|user> <name|id> fauxclip.allowed


oxide.grant <group|user> <name|id> fauxclip.norestriction

oxide.revoke <group|user> <name|id> fauxclip.norestriction


oxide.grant <group|user> <name|id> fauxclip.canuseturbo

oxide.revoke <group|user> <name|id> fauxclip.canuseturbo


Example: To allow players to use noclip mode:

oxide.grant group player fauxclip.allowed
````

**


Grant - **fauxclip.allowed** to group/player you want to have access to use noclip mode. Revoke to remove it.

Grant - ** **fauxclip.**norestriction** to group/player you want to noclip into a "building blocked" areas without noclip being turned off. Revoke to remove it.

Grant - **fauxclip.canuseturbo** to allow player to use the 'E' key to modify speed greatly while noclipping.


visit [http://oxidemod.org/threads/using-oxides-permission-system.8296/](http://oxidemod.org/threads/using-oxides-permission-system.8296/) for more info on setting permissions.

**Commands:** (typed in chat or keyboard keys used)
**- /noclip**     - Starts and Stops the noclip simulation mode.
**- Movement** - use standard movement keys to move around.
**- SPACEBAR** key now moves player straight up.
**- SPRINT** key increases noclip speed
**- 'E'** key increases noclip speed greatly if you have permission
**- 'R'** key (reload) cancels noclip simulation mode as well, returning player back to ground. Players do get a 3 second graceful landing time where they will still be immune to damage.

**Config:**

````
{

  "BaseNoClipSpeed": 0.12,

  "GracefulLandingTime": 3,

  "SprintNoClipSpeed": 0.24,

  "TurboNoClipSpeed": 1,

  "UseFauxClipGodMode": true

}
````


**Added UseFauxClipGodMode** - Setting to true will block all damage while player is in NoClip mode. Setting False will allow all damage to effect player unless another plugin is controlling damage to players.

**Note:** Noclipping into a unauthorized tool cupboard range will cancel noclip mode unless you are Admin, or you have been granted the norestriction permissions.

**Work in progress :**

- Smoother Noclip simulation