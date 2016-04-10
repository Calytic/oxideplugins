**AutoCommands** automatically executes configured commands on server startup. This is useful if you do not have access to the startup command-line parameters for your Rust server, or your host resets it with every restart. Just add the commands you want to the config file, and done! Next time your server starts, all the commands will be run automatically!
**Permissions**

This plugin uses Oxide's permission system. To assign a permission, use **oxide.grant user <username|steamid> <permission>**. To remove a permission, use **oxide.revoke user <username|steamid> <permission>**.


* 
**auto.commands** (allows player to add/remove auto commands)
**Ex.** oxide.grant user Wulfspider auto.commands
**Ex.** oxide.revoke user Wulfspider auto.commands
**Ex.** oxide.grant group admin auto.commands


**Chat Commands**


* 
**/autocmd add command**
Adds the specified command to the auto command list.



* 
**/autocmd remove command**
Removes the specified command from the auto command list.



* 
**/autocmd list**
Lists all of the commands from the auto command list.


**Console Commands**


* The console command is not yet usable.


**Configuration**

Any of the server commands for Rust should work fine, just add them in the AutoCommands.json file under the server/identity/oxide/config directory. You can see the available commands at [http://oxidemod.org/threads/6404/](http://oxidemod.org/threads/6404/). Keep in mind that in order to use quotation marks in a command, you'll need to escape them using backslash "\".
**Default Configuration**

````
{

  "Settings": {

    "Command": "autocmd"

    "Commands": [

      "server.globalchat true",

      "server.stability false"

    ]

  },

  "Messages": {

    "AlreadyAdded": "{command} is already on the auto command list!",

    "ChatHelp": "Use /autocmd add|remove command to add or remove an auto command",

    "CommandAdded": "{command} has been added to the auto command list!",

    "CommandRemoved": "{command} has been removed from the auto command list!",

    "ConsoleHelp": "Use auto.command add|remove command to add or remove an auto command",

    "InvalidAction": "Invalid command action! Use add or remove"

    "NoPermission": "You do not have permission to use this command!",

    "NotListed": "{command} is not on the auto command list!"

  }

}
````

The configuration file will update automatically if new options are added or removed. I'll do my best to preserve any existing settings and messages with each new version.