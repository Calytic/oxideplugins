« Features »


* Removes admin abuse announcements!
* Features individual permissions for each give command!
* Compatible with Rust's console system item list!
* Responds with a configurable error message if a user is missing the necessary permissions!

« Permissions »


removeaaa.give

Allows users or groups with that permission to use features which incorporate the give command!

removeaaa.giveall

Allows users or groups with that permission to use features which incorporate the giveall command!

removeaaa.givearm

Allows users or groups with that permission to use features which incorporate the givearm command!

This permission is necessary if you want to spawn items into your toolbelt through Rust's console system item list!

removeaaa.givebp

Allows users or groups with that permission to use features which incorporate the givebp command!

This permission is necessary if you want to learn blueprints through Rust's console system item list!

removeaaa.givebpall

Allows users or groups with that permission to use features which incorporate the givebpall command!

removeaaa.giveid

Allows users or groups with that permission to use features which incorporate the giveid command!

This permission is necessary if you want to spawn items through Rust's console system item list!

removeaaa.giveto

Allows users or groups with that permission to use features which incorporate the giveto command!

Granting a permission through the server console

grant <group|user> <name|id> <permission>

Revoking a permission through the server console

revoke <group|user> <name|id> <permission>

« Default Configuration File »


You can find the configuration file under .../server/<identity>/oxide/config.

If you edit the configuration file, make sure to save it and then reload this plugin!

Make sure to only ever edit the right hand string of each line!

````
{

  "1. Data": {

    "1.1 Item Black List": [

      "flare",

      "generator.wind.scrap"

    ]

  }
}
````

1.1 Item Black List

You can add/remove black listed items here!


If you don't want an item to be available through the give commands you can add the item's shortname here!

This will result in restricted access to the items contained in the list!

« Default Language File »


You can find the language file under .../server/<identity>/oxide/lang.

If you edit the language file, make sure to save it and then reload this plugin!

Make sure to only ever edit the right hand string of each line!

````
{

  "missing permission": "You are missing the necessary permission to do that!",

  "invalid item": "Invalid item!",

  "couldn't give item": "Couldn't give item!",

  "black listed item": "Item is black listed!",

  "couldn't find player": "Couldn't find player!"
}
````

missing permission

Specifies the error message that a user will receive if he is missing the necessary permission!

invalid item

Specifies the error message that a user will receive if he created an invalid item!

couldn't give item

Specifies the error message that a user will receive if he was unable to receive an item!

black listed item

Specifies the error message that a user will receive if he attempted to create a black listed item!

couldn't find player

Specifies the error message that user will receive if he attempts to give an item to a non-existing player!

« Notes »


* This plugin might interfere with other plugins which override the command handling system!