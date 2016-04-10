Admin Give command
**Command:**

- /give "player" "item/kit" "optional:amount" => give to a player

- /giveme "item/kit" "optional:amount"

- inv.give "item/kit" "optional:amount" => give to self an item

- inv.giveplayer "player/steamid" "item/kit" "optional:amount" => give to a player an item

- inv.giveall "item" "optional:amount" => give item to all players
**Blueprints:**

add " BP" at the end of an item to transform it as a blueprint.

Usage:

- /give "Reneb" "Thompson BP" 1 => will give me 1 blueprint

And again don't forget the **" "**

- /giveme "Thompson BP" => will give me 1 blueprint
**
**Kits (requires [Kits](http://forum.rustoxide.com/resources/kits.668/)):****

- inv.giveplayer "Mughisi" "kit" => will show me the list of all avaible kits

- inv.giveplayer "Mughisi" "kit" "starter" => will give Mughisi a kit starter

Usable with /give and inv.give

Not yet with inv.giveall ... but soon
**Logs:**

You may choose to log all /give or inv.give etc that were made to keep track of the usage/abuse of this command.
**Usage:**

As item you may use the real names for most of the items (yes i made a list), you will convert the real name into rust item table name (called shortname).

If rust makes new items or stuff like that you can still call an item directly by it's short name:

/give "Reneb" "kit" "starter" => will give Reneb 1 kit starter from the kits plugin.

/giveme "Large Wood Storage" 1 => will give you 1 large wood storage

/giveme "box_wooden_large" 1 => will give you 1 large wood storage

/giveme "Lantern" 5 => will give 1 lantern in 5 different slots, as lanterns are not stackable.

/giveme "Metal Fragments" 5 => will give 5 metal fragments in 1 slot, as metal fragments are stackable.

inv.giveall "Thompson BP" 1 => will give all players the Thompson blueprint
**Config:**

````

{

  "authLevel": {

    "give": 1,

    "giveall": 2,

    "givekit": 1

  },

  "Give": {

    "logAdmins": true,

    "overrightStackable": false

  },

  "Messages": {

    "itemNotFound": "This item doesn't exist: ",

    "multiplePlayersFound": "Multiple Players Found",

    "noPlayersFound": "No Players Found",

    "noAccess": "You are not allowed to use this command"

  }

}

 
````