This is a RPG system in development, I'm releasing it now to get feedback.

Currently the RPG is composed of:


* Levels
* Stats Points:

* Agility : Increases your change to dodge attacks
* Strength : Increases you Health
* Intelligence : Decreases the crafting time


* Skill Points
* Skills

* Lumberjack : Increases the gather rate for wood
* Miner : Increases the gather rate for ores and stones
* Hunter : Increases the gather rate from animals resources
* Researcher:  Allow you to research items you have to generate the Blueprint, each skill point unlocks new types of items you can research and decreases the cooldown.
* Blacksmith - Increases the melting rate, every time a furnace fuel is consumed your furnace got a chance to produce X more result (Ex: metal frag). The x depends on skill level as well as the chance %.
* BlinkToArrow - You can blink to where your arrow fell. Each skillpoint decreases the cooldown.
* Gatherer - You can gather more resources from pickup.

* [Taming - You can tame animals, thanks to @Bombardir. Level 1 allows wolf, Level 2 allows bear. ](http://oxidemod.org/threads/npc-controller.7368/)

* If the NpcController plugin isnt available the skill will be disabled.





You get exp when you gather stuff or build/upgrade buildings.

You get 1 stat point automatically assigned to each attribute when you level up, and 3 to distribute.

You get 1 skill point when you level up to distribute.

There is a in game help but is not 100% done yet, but the basics are.
**

To see the list of commands type /hunt or /h**

Each skill requires a level to get, some specific stats amounts, and a number of skillpoints to upgrade. (Still to be balanced)

Ex: Skill Researcher: Requiers level 30, 45 int, and consumes 7 skillpoints to levelup.

Also there is a max level for the skills.

Default max profile level for is 200.
**[CUSTOMIZATION]**

It's available for a lot of stuff, soon I'll explain how to customize everything that's is possible to do so.
**[ADMIN (CHAT) COMMANDS]**

hunt.lvlup <playername> <desired_level> or

/hunt lvlup <playername(opitional)> <desired_level> :

will level the admin character level or the specified player character, will give stats points and skill points too.

hunt.saverpg : save data

hunt.resetrpg : reset data

hunt.statreset <playername>

hunt.skillreset <playername>

hunt.genxptable <BaseXP(383)> <LevelMultiplier(1.105)> <LevelModule(10)> <ModuleReducer(0.005)>
Note: If you dont have permissions nothing will be shown for now.
**[INSTALLATION]**

Just copy the .cs file to the plugins folder of the server.

There is a config file and a data file, for now I recommend not changing then.
**[COMPATIBILITY]**

I suggest that you **dont** use plugins that:


* change the gather rate
* change crafting rate
* allows you to buy blueprints

It will probably work, but the RPG itself will make almost no sense.
**[IMPORTANT]**

Since I'm going to add plenty more features there may be needed to reset the RPG, so dont get to attached.

Right now it will save profiles when server saves or player levels up(not via admin chat command(/h lvlup <level>)).

You can manually save it via console command: hunt.saverpg
**[FORKING]

Please**, if you want to help with the plugin, make a push request at:
[-](https://github.com/pedrorodrigues/hunt)- currently not available --

I'll check your code and probably accept the request, if that happens I can give you my contact information so we can work together.

You are welcome to fork my code, but I would really like if you keep me in the credits. I spent a lot of time researching the assembly, not for what there is in the extension now, but for features that I wanted to do, so if you could do that, you are more than welcome to fork it.