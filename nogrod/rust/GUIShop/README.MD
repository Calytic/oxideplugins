GUI Based Shop



**How To**:

USE [JSONLint - The JSON Validator.](http://jsonlint.com/) TO MAKE SURE YOUR CONFIG FILE IS CORRECT


In **Shop - Shop Categories**:

You must place all the items that you want here (all shops included)

````
"NAME OF THE ITEM": {

      "buy": "BUYPRICE",

      "img": "URLOFTHEIMAGE",

      "item": "RUSTITEMNAME",

      "cooldown": "0",

      "sell": "SELLPRICE"

    },
````


**NEW: Commands:**

````
"NAME OF THE ITEM": {

      "buy": "BUYPRICE",

      "img": "URLOFTHEIMAGE",

      "cmd": ["cmd1", "cmd2", "cmd3"],

      "cooldown": "0",

      "sell": "SELLPRICE"

    }
````

Ofc for commands you can't sell commands ... well u can  but you would be just giving free coins to players ^^

Like zone commands you can put $player.id, $player.name, $player.x, $player.y, $player.z

You can make warps with this by placing for exemple:

"cmd": ["teleport.topos $player.id 45 20 1022"],

(45 20 1022 being a specific location for your warp)


Then in
**Shop - Shop List**

You may add what items you want inside the shops. You can have multiple shops with same items, or not. You can choose if in 1 shop you can buy or/and sell the items and what items.

````
"NPC USERID": {

      "buy": [

        "ITEM1",

        "ITEM2",

        "ITEM3",

        "ETC"

      ],

      "description": "MARKET DESCRIPTION",

      "name": "MARKET NAME",

      "sell": [

        "ITEM1",

        "ITEM2",

        "ETC"

      ]

    }
````

in NPC USERID:

you must use /npc_list to get the list of the NPC IDs and place the id of the NPC that you want to be this market.

You may use "chat" as ID, this will ACTIVATE the /shop command.

if NO "chat" markets are set, the /shop will not work and the shops will only be avaible via NPCs.

**Video Tutorial:**


(If you see well i actually fucked buy/sell, and made it that you buy cheaper then what you sell it for ... should be the opposite )

**Default Configs & Exemple**

````
{

  "Message - Bought": "You've successfully bought {0}x {1}",

  "Message - Error - Item Doesnt Exist": "WARNING: The item you are trying to buy doesn't seem to exist",

  "Message - Error - Item Not Set Properly": "WARNING: The admin didn't set this item properly! (item)",

  "Message - Error - Item Not Valid": "WARNING: It seems like it's not a valid item",

  "Message - Error - No Action In Shop": "You are not allowed to {0} in this shop",

  "Message - Error - No Action Item": "You are not allowed to {0} this item here",

  "Message - Error - No Buy Price": "WARNING: No buy price was given by the admin, you can't buy this item",

  "Message - Error - No Chat Shop": "You may not use the chat shop. You might need to find the NPC Shops.",

  "Message - Error - No Econonomics": "Couldn't get informations out of Economics. Is it installed?",

  "Message - Error - No NPC": "The NPC owning this shop was not found around you",

  "Message - Error - No Sell Price": "WARNING: No sell price was given by the admin, you can't sell this item",

  "Message - Error - No Shop": "This shop doesn't seem to exist.",

  "Message - Error - Not Enough Items": "You don't have enough of this item.",

  "Message - Error - Not Enough Money": "You need {0} coins to buy {1} of {2}",

  "Message - Error - Redeem Kit": "WARNING: There was an error while giving you this kit",

  "Message - Sold": "You've successfully sold {0}x {1}",

  "Shop - Shop Categories": {


"Apple": {

      "buy": "1",

      "img": "http://vignette2.wikia.nocookie.net/play-rust/images/d/dc/Apple_icon.png/revision/latest/scale-to-width-down/100?cb=20150405103640",

      "item": "apple",

      "sell": "1"

    },

"Airdrop": {

      "buy": "1000",

      "img": "http://vignette2.wikia.nocookie.net/play-rust/images/d/dc/Apple_icon.png/revision/latest/scale-to-width-down/100?cb=20150405103640",

      "cmd": ["airdrop.toplayer $player.id", "say $player.name bought an airdrop. Kill him!!!"],

      "sell": "1000"

    },

    "Assault Rifle": {

      "buy": "10",

      "img": "http://vignette3.wikia.nocookie.net/play-rust/images/d/d1/Assault_Rifle_icon.png/revision/latest/scale-to-width-down/100?cb=20150405105940",

      "item": "assault rifle",

      "sell": "8"

    },

    "BlueBerries": {

      "buy": "1",

      "img": "http://vignette1.wikia.nocookie.net/play-rust/images/f/f8/Blueberries_icon.png/revision/latest/scale-to-width-down/100?cb=20150405111338",

      "item": "blueberries",

      "sell": "1"

    },

    "Bolt Action Rifle": {

      "buy": "10",

      "img": "http://vignette1.wikia.nocookie.net/play-rust/images/5/55/Bolt_Action_Rifle_icon.png/revision/latest/scale-to-width-down/100?cb=20150405111457",

      "item": "bolt action rifle",

      "sell": "8"

    },

    "Build Kit": {

      "buy": "10",

      "img": "http://oxidemod.org/data/resource_icons/0/715.jpg?1425682952",

      "item": "kitbuild",

      "sell": "8"

    }

  },

  "Shop - Shop List": {

    "1234567": {

      "buy": [

        "Apple",

        "BlueBerries",

        "Assault Rifle",

        "Bolt Action Rifle"

      ],

      "description": "You currently have {0} coins to spend in this farmers market",

      "name": "Fruit Market",

      "sell": [

        "Apple",

        "BlueBerries",

        "Assault Rifle",

        "Bolt Action Rifle"

      ]

    },

    "5498734": {

      "buy": [

        "Assault Rifle",

        "Bolt Action Rifle"

      ],

      "description": "You currently have {0} coins to spend in this weapons shop",

      "name": "Weaponsmith Shop",

      "sell": [

        "Assault Rifle",

        "Bolt Action Rifle"

      ]

    },

    "chat": {

      "buy": [

        "Build Kit",

        "Airdrop"

      ],

      "description": "You currently have {0} coins to spend in this builders shop",

      "name": "Build"

    }

  }

}
````