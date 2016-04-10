**TwigsDecay**  is a replacement for the default decay system. It steadily decays buildings made of twigs in an attempt to reduce the overall entity count on servers. Also works for other tiers if configured to do so.

**Configuration (edit config/TwigsDecay.json):** 


* damage - The amount of damage a block of a specific tier takes per timespan (default: 1 for "Twigs", 0 for everything else)
* timespan - Timespan in minutes for damage updates (default: 288)
* blocks - Contains a list of blocks to decay (default: all but "pillar")

With the default configuration, twigs buildings will fully decay in 24 hours after being built and higher grades will not decay.

**Barricades and Ladders** 


By default, TwigsDecay does not decay barricades or ladders. You can, however, configure this. Barricades have 350 HP (Wooden), 400 HP (Barbed) respectively 500 HP (Metal) and ladders have 50 HP (Wooden).

**Translating** 


There is a config file located at config/TwigsDecay.json which contains all translatable strings. Simply edit the right hand side of the translations, but always keep the %PLACEHOLDERS% intact and untranslated.


````
{

  "damage": {

    "Barricade": 0,

    "Ladder": 0,

    "Metal": 0,

    "Stone": 0,

    "TopTier": 0,

    "Twigs": 1,

    "Wood": 0

  },

  "timespan": 288,

  "blocks": [

    "block.halfheight.prefab",

    "block.halfheight.slanted.prefab",

    "block.stair.lshape.prefab",

    "block.stair.ushape.prefab",

    "floor.prefab",

    "floor.triangle.prefab",

    "foundation.prefab",

    "foundation.steps.prefab",

    "foundation.triangle.prefab",

    "gates.external.high.prefab",

    "roof.prefab",

    "wall.prefab",

    "wall.doorway.prefab",

    "wall.external.high.prefab",

    "wall.external.high.stone.prefab",

    "wall.low.prefab",

    "wall.window.prefab",

    "wall.window.bars.prefab"

  ],

  "messages": {

    "%GRADE% buildings decay by %DAMAGE% HP per %TIMESPAN% minutes.": "%GRADE% buildings decay by %DAMAGE% HP per %TIMESPAN% minutes.",

    "%GRADE% buildings do not decay.": "%GRADE% buildings do not decay.",

    "Barricade": "Barricade",

    "Ladder": "Ladder",

    "Metal": "Metal",

    "Stone": "Stone",

    "TopTier": "TopTier",

    "Twigs": "Twigs",

    "Wood": "Wood"

  }

}
````