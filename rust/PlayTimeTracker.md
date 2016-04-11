This plugin tracks a player's playtime and time spent AFK, and saves it to the file store. It can be dropped into a live server and does not require a restart (players already in the server will not be tracked until they reconnect).


**Warnings**

Time spend AFK is calculated based on the players position in the world. PlayTime and AfkTime are logged separately; therefore **time spend AFK is NOT automatically subtracted from time played.
**

Re uploading the plugin (for instance, to update it) **may cause errors** on player disconnection, until the players rejoin. This will be fixed in the next update.

**Config**

Afk Check Interval (seconds): How often should the script check if a player has not changed position.

Cycles Until Afk (number): How many cycles of the above should happen until the player is considered AFK.

Track AFK Time? (true/false): Enables/Disables AFK time tracking. Disabling this may reduce server load by a small amount.

**Chat Commands**

/playtime: Prints the player's total playtime to chat, up to the second, in human readable form.

/afktime: Prints the player's afk time in chat, in human readable form.

/lastseen: Prints the last date and time the player has been seen.


You can add a target after each of those commands (/playtime <player_name>).

**Permissions**

PlayTimeTracker.CanCheckPlayTime: For use with /playtime <target>

PlayTimeTracker.CanCheckAfkTime: For use with /afktime <target>

PlayTimeTracker.CanCheckLastSeen: For use with /lastseen<target>

PlayTimeTracker.CanCheckSelfPlayTime: For use with /playtime

PlayTimeTracker.CanCheckSelfAfkTime: For use with /afktime

PlayTimeTracker.CanCheckSelfLastSeen: For use with /lastseen

**Saved Data**

SteamID

Name: Uses name when first joined

PlayTime (seconds)

AfkTime (seconds)

HumanPlayTime (d.hh:mm:ss format)

HumanAfkTime (d.hh:mm:ss format)

LastSeen (dd.mm.yyyy T hh:mm:ss format)


Feel free to leave questions, comments or improvement you would like to see!


Like it? Give a like back!