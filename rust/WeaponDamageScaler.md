**Weapon Damage Scaler **is a plugin that allows you to scale a player's damage per weapon, and/or ammo type.


The plugin itself is pretty simple, here's how to scale damages:

Example of console command usage: **weapon.setscale "Assault Rifle" "0.923"**. This will scale the damage of the Assault Rifle to 0.923.


Chat command works the same way: **/setscale <weaponnname> <x.x>**

Setting ammo is just as easy: **/setscale "HV 5.56 Rifle Ammo" 1.2**


To scale a body part, use: /**scalebp weapon <weaponname> <body part name> <scale>
**

Example: **/scalebp weapon "Custom SMG" neck 1.1 **- This will scale the weapon's damage to the neck by 1.1.
**
**

Both commands require the permission **"weapondamagescaler.setscale"
**

Scaling body part damage requires permission "**weapondamagescaler.setscalebp**"

**NOTE:** Melee weapons currently do not scale up or down, I really don't know why. I'll try to fix it some time soon.

**NOTE 2:** When scaling the pump shotgun, remember that it is scaled for each pellet if buckshot ammo is used.


For clarification, the config option "PlayersOnly" toggles whether or not the damage will be scaled on things like buildings, barrels, npcs, etc. If set to true (default), it only scales damage against other players.

**Config**

````

{

  "AllowAuthLevel": false,

  "AuthLevel": 2,

  "GlobalDamageScaler": 1.0,

  "PlayersOnly": true,

  "UseGlobalDamageScaler": false

}

 
````

Localization

````

{

  "noPerms": "You do not have permission to use this command!",

  "invalidSyntax": "Invalid Syntax, usage example: setscale <weaponname> <x.x>",

  "itemNotFound": "Item: \"{item}\" does not exist, syntax example: setscale <weaponname> <x.x>",

  "alreadySameValue": "This is already the value for the selected item!",

  "scaledItem": "Scaled \"{engName}\" ({shortName}) to: {scaledValue}"

}

 
````

Please post any bugs/suggestions in the thread. Thanks!