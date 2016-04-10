This plugin allows people with the right permission to use /wartime to toggle the allowance to use siege weapons on the server.

You can also use realtime and set in the config file when to activate wartime and when to activate peacetime. The plugin will the automatically switch between wartime and peacetime depending on the time.

You can let the mod kick or ban people who siege during peace times.

**Commands

/wartime **- Toggles WarTime/PeaceTime. Only av
**/checkwartime** - Tells the caller whether it's a time of Peace or War.

**Permissions

wartime.toggle** - People with this permission can use the wartime toggle when realtime is not being used.
**wartime.exception** - People with this permission will be left unharmed when AdminSiegeException is turned on.

**Confirugation

AdminSiegeException **- Toggle if the plugin should skip people with the wartime.exception permission.
**BanTime **- Set how many days a player should be banned when they siege during peace time (if punish is set to ban).
**Peacetime **- Set the hour (in 24-hour notation) at which peacetime should be activated.
**Punish **- Choose whether to "ban" or "kick" players when they siege during peace times.
**UsingRealtime **- Toggle for the usage of realtime.
**Wartime **- Set the hour (in 24-hour notation) at which wartime should be activated.
**WarOn **- Whether it's currently peacetime or wartime.

**Lang config**

Set the messages the plugin will show.

Add multiple of these with different languages to sent players messages in their own language.