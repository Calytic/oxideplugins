**About:**


* This plugin handles every players gather rate. This plugin is very much like the well known Gather plugin that handles all gather rate. The difference between these two plugins is that one handles every players gather rate, and one handles the servers gather rate essentially.
* This now includes groups which can be added with different gather rates. Ex: VIP1 can be created with x, x and x gather rates.
* This also includes various different configuration options and various different permissions available to you.


**Features:**


* Fully functional permissions system(See Permissions)
* Fully functional config system(See Config)
* Fully functional and custom group system(See Groups)
* Fully functional gather rate changer, changes all 3 types of gather rates(Quarry, Consumable/Pickup and Resources such as rocks)
* Fully functional language system(Can change to any language and edit the different words in language(See your oxide folder)
* Saves Data when the server Saves to reduce lag.
* And more!


**Permissions:**


* igather.admin: Gives you permission to every command.

* igather.groupadmin: Gives you permission to group commands.
* igather.gatheradmin: Gives you permission to gather commands.



**Config:**


* ChatColor: Gives the ability to change the color of chat(plugin chat).
* ChatPrefix: The plugin prefix in chat.
* ChatPrefixColor: Color of the chat prefix.
* DefaultGroupPickupX: The default group(regular)'s pickup rate.
* DefaultGroupQuarryX: The default group(regular)'s quarry rate.
* DefaultGroupResourceX: The default group(regular)'s resource rate.


**Groups:**


* This version of IGather introduces a new ability which is groups.
* Groups can be created using the commands(Usage) seen below.
* Every player is automatically added to (regular) when they join for the first time.
* The group (regular) can have its resource rate set in the Configuration.
* Groups can be created and have players added using the commands.
* You may not set a players gather rate but you can set a groups gather rate to something else(Up coming).
* Groups have their own ID which can be seen by typing trygroup in console(Dev command mostly)
* If you create a group with a ID of 3, you must create a group with a ID of 4 next or it will not allow you too.
* You may also add a player to a group while sleeping, this is why I included a function to change their rates upon join if it has been changed!

**Usage:**


* /igath addtogroup (player) (groupID) - Adds a player to a group!
* /igath removefromgroup (player) (groupID) - Removes a player from the selected group.
* /igath creategroup (groupID) (groupName) (ResourceX) (PickupX) (QuarryX) - Creates a group.
* /igath gather - Shows your gather rate.
* /igath gatherp (target) - Shows a targets current gather rate!
* /igath groupbaseplayers (groupID) - Shows the players in a group if the total players is less then 12.

**Folder Paths:**


* Config: my_server_identity\oxide\config\
* Data: my_server_identity\oxide\data\
* Lang: my_server_identity\oxide\lang\
* These can be in different locations dependent on how you installed your sever.

**To-Do List:**


* If you want the option to set individual gather rates.
* Remove groups.
* Add a group wipe system.
* Change group gather rates after creation of a group.


**Known Bugs:**


* **None as of this current date!**