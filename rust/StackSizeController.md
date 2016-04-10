**Feedback**

Any feedback is welcome, whether it be ideas and suggestions, bug reports, or just to tell me I coded it wrong. Tell me what I did wrong, and I'll fix it. Want something new? If I can do it I will.

**Notes**


* This plugin was created because the other Stack Sizes plugin is no longer being managed (as far as I can tell) and has a lot of confusion around the Config file. Something this plugin eliminates.
* Shout out to the ItemConfig plugin which helped me get a list of all game items.
* Even though all items are in the config, some won't stack, usually things with a durability, such as Assault Rifles, this is unavoidable.
* Some item names have a /n in them, this is intentional, DO NOT change any item names, or that will break the item stacking.
* Stacks over 65536 will split partially when you move them in your inventory, this is a Rust bug, and I have spent over 4 hours looking into a solution and it is simply beyond my reach.


**Features**


* Allows you to set the stack size of every item in the game.
* Grabs all items and puts it into the config file automatically. (No adding items manually!)
* On update, new items are added automatically without a need to reset the config file. (No updating or resetting the config, woo!)


**Commands**


* /stack item_short_name stacksize (EX: /stack ammo_rocket_hv 64)
* /stackall 65000 (Sets the stack size to 65000)
* Console command "stack" and "stackall" also work same way as the above.


**Permissions**


* stacksizecontroller.canChangeStackSize - Allows user to use /stack and /stackall in-game. (Admins have access automatically)


**To-Do List**


* Open to suggestions.