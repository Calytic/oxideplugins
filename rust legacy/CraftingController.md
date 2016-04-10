**Crafting Controller**

A plugin which controls crafting, researching and studying.
**How to use**

* Once the plugin is installed, launch or restart the server.
* Inside your server's oxide config directory (see http://forum.rustoxide.com/threads/how-to-install-and-use-plugins-on-your-server.70/), the file "CraftingController.json" should have been created. Download it.
* Make modifications to the file as desired (see below) and reupload it to your server.
* Check that it's worked by looking at oxidelog.txt, and looking for the messages "x items have been blocked from crafting". If you can't find this line, check the plugin is installed correctly and check your modified file in an online JSON validator.
**Notes**


* If you have an error in your crafting controller file, the plugin may not display an error and simply just load the default loot tables. Always check your customised crafting controller file in a validator (http://jsonlint.com/) before uploading!


**Crafting Controller JSON Format**

The format is very simple. There are 3 sections:


* BlockedCrafting

* BlockedBlueprints

* BlockedResearching

Inside each section, you may list all items you wish to block. For example, to block players from crafting Explosive Charge, your BlockedCrafting section may look like this:

````

{

  "Blueprints: Block List": [],

  "Crafts: Block List": [

     "Metal Ceiling",

    "Metal Wall",

    "Metal Pillar"

  ],

  "Messages: Block Blueprint": "This blueprint has been disabled.",

  "Messages: Block Craft": "Crafting this item has been blocked.",

  "Messages: Block Research": "Researching this item has been blocked.",

  "Researching: Block List": []

}

 
````

The item names **are not validated, **so make sure they are correct because the plugin will not tell you if you've made a mistake. You should test your modifications by spawning in the appropriate materials and trying to craft it yourself before making your server public.