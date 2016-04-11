Make sure you delete the previews plug-in and configuration file.


When a player re-spawn from dead it starts with only a Rock and a Torch. This plug-in use a configuration file where you can set the items you want the players to have after they re-spawn from dead. The configuration file by default is set for admins to start with a Rock, Hunting Bow, 25 HV Arrows, Hide Pant, and Hide Boot as an example but you can remove this on the config file.


Example of adding an item in the config file:

[ {

   "Amount": 20,

   "Container": "Main",

   "item_shortname": "arrow.wooden",

},

{

   "Amount": 1,

   "Container": "Wear",

   "item_shortname": "bow.hunting",

}

] ,[/CODE]

The Amount is self explanatory.


The Container is where the item will be place and this can be Main, Wear, Belt or default to let the game decide for you. The position of the item is base on what item is first from top to bottom on the config file.


item_shortname is the short name for the item and you can find this on the link below:
[Oxide API for Rust](http://docs.oxidemod.org/rust/#item-list)


After you modify the config file make sure you reload the plug-in by typing on the console <reload StartupItems> without the <>.


You can now set startup items to groups using the Oxide's permission system. Please check out [Using Oxide's permission system | Oxide](http://oxidemod.org/threads/using-oxides-permission-system.8296/)


You need to add the group name to the configuration file and then add the items there and the name must be exactly the same on the configuration file as the group name you created on the console.


If after you modify the plug-in it does not work make sure you check the config file again and check the syntax of the Jason file because a missing comma, indentation etc... will make the plug-in not work.

NOTE: For this update to work please delete the old config file because there is a new version.


Thanks to [BlackStone](http://oxidemod.org/members/blackstone.117432/) for the plug-in icon .