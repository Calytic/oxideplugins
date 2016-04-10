**Alias System** gives you the opportunity to create an alias for an existing command.
**Permissions:**


* **commandalias.admin**
* ****commandalias**.use**


**Chat Commands:**


* 
**/alias add <alias> "<command>" **(Add alias for a command (arguments can be set in command but must be in quotes))
* 
**/alias remove <alias>** (Removes alias)


**For a Chatcommand, add a / in front of it. For a Consolecommand just use the command, don't add a /**

[I][/I]
**Example:**
to add /kill as alias for the kill console command:


* **/alias add /kill kill**

or if you'd like to have a console command for /players (Notifier, Notifications or Playerlist Plugin):


* **/alias add players /players**

or maybe you'd just like a shorter command for /backpack (Backpacks Plugin):


* **/alias add /bp /backpack**

you can also do that with console commands (Give Plugin):


* **/alias add giveto inv.giveplayer**