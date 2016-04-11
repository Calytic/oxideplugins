Updatet for 1.0.1:

It's practically doing what the title is saying. This plugin will add chat commands to unwound you when you're wounded.


ConsoleCommands:

````
unwound.recreate -- deletes your actual config and creates a new one with standard-values

unwound.load -- if you changed the config manually this will reload it

unwound.set [1stKEY] [2ndKEY] [VALUE] -- you can change the config-values via console here (config will be loaded afterwards)
````

If any KEY was not written correctly when running unwound.set, don't worry you will just have useless entries in your config then. 
ChatCommands:

````
/aid
````

Well it is doing what the plugin is promising.

For using any of these ChatCommands the user or group needs the "canuseunwound" permission, this permission will automatically been added to the admin-group when the plugin is loaded (plugin not the config).

Optional if activated and the required plugin is installed players gan insert a number behind the ChatCommand (e.g. /aid 500) and if they have enough balance on their account the time they have to wait for the medic will get shortened or will get removed completely (won't remove the chance that the command can fail - see in config "Settings" "ChanceTheMedicSavesYou" while 100 ensures that the command will never fail).

To configure the shorteners with the Economics-Plugin see "EcoSettings"-Region in the config.

The definer (in standard "250" and "500") is the cost and the value behind is the time in seconds (in standard 5 and 0), you can edit, add or remove definers as you like (but keep in mind definers with values smaller than 0 will be ignored).

If a player uses a non existent definer or does not have enough balance it will just use the time defined under "Settings" "WaitTillMedic".

See [Using Oxide's permission system | Oxide](http://oxidemod.org/threads/using-oxides-permission-system.8296/) on how to add / remove permissions.


DefaultConfig:

````
{

  "EcoSettings": {

  "250": 5,

  "500": 0

  },

  "Localization": {

  "AboutToDie": "You are about to die, use /aid to call for a medic.",

  "DontTrollTheMedic": "How dare you call the medic and then don't wait for him before staying up again!",

  "MedicAlreadyCalled": "You already called for a medic, just wait for him.",

  "MedicIncompetent": "This incompetent troll of a medic is just to stupid to get you back up, we will get rid of him!",

  "MedicToLate": "Seems like your medic found some free beer on the way and won't come in time now ... I think we have to cut his salary!",

  "NotEnoughMoney": "You don't have enough money, how horrible ... You have {0} and you would need {1} so just wait the full {2} seconds for the medic.",

  "NotWounded": "You're not wounded, get your extra shots somewhere else!",

  "PermissionMissing": "You have no permission to use this command, if you're wounded right now it means you're probably screwed!",

  "Survived": "The claws of death failed to claim you this time!",

  "TheMedicIsComing": "The medic is coming for you ... that means if you can survive another {0} seconds."

  },

  "Settings": {

  "CanCallMedicOncePerWounded": true,

  "ChanceTheMedicSavesYou": 100,

  "EnableEconomics": false,

  "EnablePopups": false,

  "WaitTillMedic": 10

  },

  "Version": "1.0.1"

}

 
````

FuturePlans:

Well I have none for this plugin, so maybe you guys?


Bugs:

Well if you find any let me know.


PS:

And even if I still think this plugin is a bit game-breaking, I start to like it.