Place bounty's on other players using ingame items with optional FriendsAPI and Clans integration to stop claim abuse. Also support for popup notifications and Economics

**Chat Commands**

"/bounty" - Will show help
"/bounty add Playername" - Will open the bounty box to deposit your items. when closed the bounty will be placed
"/bounty add PlayerName money ##" - For use with economics, replace ## with the money amount
"/bounty check" - Will check the users current bounty's
"/bounty check PLAYERNAME" - Will show you the players current bounty's
"/bounty claim" - Will list your current rewards with ID number
"/bounty claim ##" - Claims the reward with ID number ##
"/bounty top" - Will display the top 5 bounty hunters and top 5 total wanted times
"/bounty wanted" - Will display the top 5 wanted players and top 5 current wanted times
-------- Admin only -------- 

"/bounty clear PLAYERNAME" - Clears the players bounty's
"/bounty wipe" - Wipes all bounty data

**Console Commands**
"bounty.wipe" - Wipes all data
"bounty.list" - Prints all bounty data to console

**Permissions**
bounty.use - Player bounty usage
bounty.admin - Admin commands

**Config**

````

{

  "Authlevel to access admin commands": 1,

  "Economics - Use money as bounty": true,

  "Options - Authlevel to access admin commands": 1,

  "Popup Notification time": 30,

  "Popups - Popup Notification time": 30,

  "Popups - Use Popup Notifications": true,

  "Reminders - Timer": 1,

  "Reminders - Timer (mins)": 20,

  "Reminders - Use reminders": true,

  "Use Clans": true,

  "Use FriendsAPI": true,

  "Use Popup Notifications": true

}

 
````