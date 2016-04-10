This plugin allows server administrators to run the in-game command /corpsetime <minutes> where minutes is the length of which a player's corpse should remain active in the world.


It is also possible to use the console command corpse.time <minutes> to alter the duration using an RCON client or using the in-game console.


The duration can only be changed when the user using the command has the correct Oxide permission assigned: **corpseduration.modify
**

If a user without that permission would use the command then it will only return the duration the plugin is currently set to.


Keep in mind when a corpse is gathered the time is reset and when it's completely gathered the corpse still disappears and the items that are left on the body spawn on the ground.


The default value used by Rust is **5** minutes.