As requested by [@redBDGR ~](http://oxidemod.org/members/122931/)


A simple plugin to block any item from being deployed outside of 'Building Privilege'. Blocked items are returned to the players inventory. Admins are exempt.


To add items simply add the items short name to the list in the config

You can find item names here => [Rust](http://docs.oxidemod.org/rust/#item-list)

**Config**

````
// Default blocked items

{

  "deployables": [

    "barricade.concrete",

    "barricade.metal",

    "barricade.sandbags",

    "barricade.stone",

    "barricade.wood",

    "barricade.woodwire",

    "campfire",

    "gates.external.high.stone",

    "gates.external.high.wood",

    "wall.external.high",

    "wall.external.high.stone"

  ]

}
````