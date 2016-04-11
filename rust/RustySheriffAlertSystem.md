This mod is now written in C#!  As this is the first release after conversion from Lua, please back up your config, data, and LUA plugin just in case there are issues that I haven't spotted.

   You can then easily roll back if necessary.


  Thanks for using the Rusty Sheriff Raid Alert mod, please consider a little donation if it serves you well, or if you'd like to see an iOS/Windows Phone version =)

[https://www.paypal.com/cgi-bin/webscr?cmd=_s-xclick&hosted_button_id=GMSQ4L5NFG95J](https://www.paypal.com/cgi-bin/webscr?cmd=_s-xclick&hosted_button_id=GMSQ4L5NFG95J)


  The basic premise of the mod is to be able to create a perimeter around your base and be alerted in-game and externally via a PC or Android App in the case of a breach.


  Quick Start

  -----------


   To create a perimeter, stand on a foundation and type /rs set <perimeter name>. 


You'll see the outline of the perimeter on-screen, and a test alert will be sent to your PC/Android device.


   Within 30 seconds you will see a validation code, along with your SteamID.  Put these in the PC/Android app, and your test alert will appear.


   If anyone breaches your perimeter, you will be notified in-game and on your PC/Android device.


   You can also set perimeters manually. There is a YouTube video here with a brief overview of how to do so here: (updated video coming soon)





  The PC app can be downloaded from my dropbox here:

[https://www.dropbox.com/s/n7lv4hfltz1dg50/RSRaidAlert.rar?dl=0](https://www.dropbox.com/s/n7lv4hfltz1dg50/RSRaidAlert.rar?dl=0)



  The Android app can be found here:

[https://play.google.com/store/apps/details?id=com.jvetech.rustysheriffanti_raid](https://play.google.com/store/apps/details?id=com.jvetech.rustysheriffanti_raid)



  Or search for Rusty Sheriff Raid Alert on Google Play.


  I wrote an RCON utility called Rusty Sheriff RCON which is on Google Play also, if you're looking for ways to admin your server from afar.


  User commands

  --------------

   /rs set <perimeter name> to automatically create a perimeter based on the foundations you're standing on.

  /rs view to get a list of your current alert perimeters and draw them on-screen.

  /rs delete <index> to delete a perimeter.

  /rs clear to clear all your perimeters.

   /rs test to test your perimeter and send a test alert externally


   /rs start <perimeter name> to start entering a perimeter manually

   /rs add to add a waypoint to your perimeter

   /rs undo to undo the last waypoint on your perimeter

   /rs cancel to cancel your manual perimeter entry

   /rs stop to finalise your perimeter


   /rs validate to obtain a validation code for the PC/Android app.

   /rs validatenew to obtain a new validation code for the PC/Android app.


  /rs ignoredetect to ignore any players within your perimeters so they will no longer trigger an alert.

  /rs ignore <steamid> "playername" to ignore a player manually; if they are offline for example.

   /rs ignore <steamid> / <playername> to ignore an online player.

  /rs ignores to view the players on your ignore list.

  /rs clearignores to clear your ignore list.

  /rs unignore <number> to remove a player from your ignore list.


  /rs mute to ignore your alerts in-game (they will still be sent externally)

  /rs unmute to see your alerts in-game


  Admin only commands

  -------------------

  /rs admin - display the admin menu

   /rs cupboard - toggle whether perimeters can be created without building privileges

  /rs secure - toggle whether the Raid Alert system can be used by non-authorised users

  /rs enable - enable automatic checking of players

  /rs disable - disable automatic checking of players

  /rs status - displays the values of the options below

  /rs chatmute - toggle whether player's alerts show in their chat window

  /rs sleepers - toggle trigger alert only if the player is sleeping in their perimeter

  /rs maxperim <number> - set the maximum number of perimeters a player can set

  /rs maxsize <number> - set the maximum area a perimeter can span to <number> x <number> metres

  /rs maxpoints <number> - set the maxmimum number of points a player's perimeter can contain

  /rs anon - toggle whether to display/send player's names and SteamIDs when an alert is triggered

  /rs time <number> - set the time over which each player and perimeter is checked

  /rs sync - enable/disable synchronisation with the Raid Alert server

  /rs save - save all Raid Alert data and configuration settings (handy before a server restart)

  /rs auths - display a full list of authorised players

  /rs adduser <steamid> /+ <playername> <auth level> add a player to the authorised user with auth level 0, 1 or 2

     - 0 = Basic user that's fully restricted by the options you set for perimeter size, number of perimeters etc.

     - 1 = Privileged user that's unrestricted but cannot change admin options.

     - 2 = Raid Alert admin.  No restrictions and can modify admin options.

  /rs deluser <index> - remove a player from the authorised user list


  Console Commands:


  These are versions of the above commands, that can be entered via RCON/Console


  rs.auths

  rs.adduser

  rs.deluser