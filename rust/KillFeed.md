« Features »


* Displays self-inflicted deaths!
* Displays players who either have been wounded or killed while not being wounded by default!
* Displays the bone that has been hit!
* Displays the distance the wounding/killing projectile traveled!
* Works with animals!
* Players can disable the Kill Feed through a customizable chat command!
* Position, icons and more can be customized through a configuration file!
* Items can be disabled individually! Do as you see fit!
* Icons can be hosted locally, online or using a combination of both!
* New items can be added easily using the configuration file! No more waiting for the plugin to be updated!
* Existing items should have their icons correctly updated if you use the default configuration file.
* This plugin comes completely functioning. Usually no setup is required!

« Commands »

/killfeed

Shows information about the possible arguments that can trail this command!

/killfeed enable

Enables the Kill Feed for the player who entered the command!

/killfeed disable

Disables the Kill Feed for the player who entered the command!

/killfeed status

Displays whether the Kill Feed is enabled or disabled for the player who entered the command!

« Default Configuration File »


You can find the configuration file under .../server/<identity>/oxide/config.

If you edit the configuration file, make sure to save it and then reload this plugin!

Make sure to only ever edit the right hand string of each line!

````
{

  "1. General": {

    "1.1 chat icon": "76561198263554080",

    "1.2 Text": {

      "1.2.1 font": "robotocondensed-bold.ttf",

      "1.2.2 outline": false,

      "1.2.3 font size": 18,

      "1.2.4 number of characters": 12,

      "1.2.5 remove special characters": true,

      "1.2.6 remove tags": false

    },

    "1.3 Eligible For Entry": {

      "1.3.1 animals": true,

      "1.3.2 player deaths": false

    },

    "1.4 Monitoring": {

      "1.4.1 log entries": false,

      "1.4.2 print entries to console": false,

      "1.4.3 debugging": 0

    }

  },

  "2. Kill Feed": {

    "2.1 formatting": "{initiator}          {hitBone}{weapon}{distance}          {hitEntity}",

    "2.2 number of entries": 3,

    "2.3 destroy after": 30.0,

    "2.4 Action-On-Key-Use": [

      {

        "key": "tab",

        "action": "inventory.toggle;killfeed.action add destroy",

        "defaultAction": "inventory.toggle"

      },

      {

        "key": "escape",

        "action": "killfeed.action add",

        "defaultAction": ""

      }

    ],

    "2.5 Dimensions": {

      "2.5.1 width": 0.3,

      "2.5.2 icon half-height": 0.5

    },

    "2.6 Position": {

      "2.6.1 x": 0.175,

      "2.6.2 y": 0.95

    },

    "2.7 Spacing": {

      "2.7.1 horizontal": 0.0,

      "2.7.2 vertical": -0.005

    },

    "2.8 Fade": {

      "2.8.1 in": 0.0,

      "2.8.2 out": 0.0

    },

    "2.9 Colors": {

      "2.9.1 inititator": "#336699",

      "2.9.2 info": "#b38600",

      "2.9.3 hit entity": "#800000",

      "2.9.4 npc": "#267326"

    }

  },

  "3. Data": {

    "3.1 file directory": "http://vignette1.wikia.nocookie.net/play-rust/images/",

    "3.2 Files": {

      "autoturret": "f/f9/Auto_Turret_icon.png",

      "axe.salvaged": "c/c9/Salvaged_Axe_icon.png",

      "barricade.metal": "b/bb/Metal_Barricade_icon.png",

      "barricade.wood": "e/e5/Wooden_Barricade_icon.png",

      "barricade.woodwire": "7/7b/Barbed_Wooden_Barricade_icon.png",

      "bone.club": "1/19/Bone_Club_icon.png",

      "bow.hunting": "2/25/Hunting_Bow_icon.png",

      "crossbow": "2/23/Crossbow_icon.png",

      "explosive.timed": "6/6c/Timed_Explosive_Charge_icon.png",

      "gates.external.high.stone": "8/85/High_External_Stone_Gate_icon.png",

      "gates.external.high.wood": "5/53/High_External_Wooden_Gate_icon.png",

      "grenade.beancan": "b/be/Beancan_Grenade_icon.png",

      "grenade.f1": "5/52/F1_Grenade_icon.png",

      "hammer.salvaged": "f/f8/Salvaged_Hammer_icon.png",

      "hatchet": "0/06/Hatchet_icon.png",

      "icepick.salvaged": "e/e1/Salvaged_Icepick_icon.png",

      "knife.bone": "c/c7/Bone_Knife_icon.png",

      "landmine": "8/83/Land_Mine_icon.png",

      "lmg.m249": "c/c6/M249_icon.png",

      "lock.code": "0/0c/Code_Lock_icon.png",

      "longsword": "3/34/Longsword_icon.png",

      "mace": "4/4d/Mace_icon.png",

      "machete": "3/34/Machete_icon.png",

      "pickaxe": "8/86/Pick_Axe_icon.png",

      "pistol.eoka": "b/b5/Eoka_Pistol_icon.png",

      "pistol.revolver": "5/58/Revolver_icon.png",

      "pistol.semiauto": "6/6b/Semi-Automatic_Pistol_icon.png",

      "rifle.ak": "d/d1/Assault_Rifle_icon.png",

      "rifle.bolt": "5/55/Bolt_Action_Rifle_icon.png",

      "rifle.semiauto": "8/8d/Semi-Automatic_Rifle_icon.png",

      "rock": "f/ff/Rock_icon.png",

      "rocket.launcher": "0/06/Rocket_Launcher_icon.png",

      "salvaged.cleaver": "7/7e/Salvaged_Cleaver_icon.png",

      "salvaged.sword": "7/77/Salvaged_Sword_icon.png",

      "shotgun.pump": "6/60/Pump_Shotgun_icon.png",

      "shotgun.waterpipe": "1/1b/Waterpipe_Shotgun_icon.png",

      "smg.2": "9/95/Custom_SMG_icon.png",

      "smg.thompson": "4/4e/Thompson_icon.png",

      "spear.stone": "0/0a/Stone_Spear_icon.png",

      "spear.wooden": "f/f2/Wooden_Spear_icon.png",

      "spikes.floor": "f/f7/Wooden_Floor_Spikes_icon.png",

      "stone.pickaxe": "7/77/Stone_Pick_Axe_icon.png",

      "stonehatchet": "9/9b/Stone_Hatchet_icon.png",

      "surveycharge": "9/9a/Survey_Charge_icon.png",

      "torch": "4/48/Torch_icon.png",

      "trap.bear": "b/b0/Snap_Trap_icon.png",

      "wall.external.high": "9/96/High_External_Wooden_Wall_icon.png",

      "wall.external.high.stone": "b/b6/High_External_Stone_Wall_icon.png"

    },

    "3.3 Damagetype Files": {

      "bite": "1/17/Bite_icon.png",

      "bleeding": "e/e5/Bleeding_icon.png",

      "blunt": "8/83/Blunt_icon.png",

      "bullet": "5/5a/Bullet_icon.png",

      "cold": "7/74/Freezing_icon.png",

      "drowned": "8/81/Drowning_icon.png",

      "electricShock": "a/af/Electric_icon.png",

      "explosion": "5/50/Explosion_icon.png",

      "fall": "f/ff/Fall_icon.png",

      "generic": "b/be/Missing_icon.png",

      "heat": "e/e4/Ignite_icon.png",

      "hunger": "8/84/Hunger_icon.png",

      "poison": "8/84/Poison_icon.png",

      "radiation": "4/44/Radiation_icon.png",

      "slash": "5/50/Slash_icon.png",

      "stab": "3/3e/Stab_icon.png",

      "suicide": "b/be/Missing_icon.png",

      "thirst": "8/8e/Thirst_icon.png"

    },

    "3.4 NPC Names": {

      "bear": "Bear",

      "boar": "Boar",

      "chicken": "Chicken",

      "horse": "Horse",

      "patrolhelicopter": "Helicopter",

      "stag": "Stag",

      "wolf": "Wolf"

    },

    "3.5 Bone Names": {

      "body": "Body",

      "chest": "Chest",

      "groin": "Groin",

      "head": "Head",

      "hip": "Hip",

      "jaw": "Jaw",

      "left arm": "Arm",

      "left eye": "Eye",

      "left foot": "Foot",

      "left forearm": "Forearm",

      "left hand": "Hand",

      "left knee": "Knee",

      "left ring finger": "Finger",

      "left shoulder": "Shoulder",

      "left thumb": "Thumb",

      "left toe": "Toe",

      "left wrist": "Wrist",

      "lower spine": "Spine",

      "neck": "Neck",

      "pelvis": "Pelvis",

      "right arm": "Arm",

      "right eye": "Eye",

      "right foot": "Foot",

      "right forearm": "Forearm",

      "right hand": "Hand",

      "right knee": "Knee",

      "right ring finger": "Finger",

      "right shoulder": "Shoulder",

      "right thumb": "Thumb",

      "right toe": "Toe",

      "right wrist": "Wrist",

      "stomach": "Stomach"

    },

    "3.6 Allowed Special Characters": [

      ".",

      " ",

      "[",

      "]",

      "(",

      ")",

      "<",

      ">"

    ]

  }
}
````

1.1 chat icon

Specifies the chat icon that will appear if players use the /killfeed chat command!

The chat icon has to be a valid 64 bit Steam ID formatted as a string!

1.2.1 font

Specifies the font that will be used!

available fonts:


* "daubmark.ttf"
* "droidsansmono.ttf"
* "robotocondensed-bold.ttf"
* "robotocondensed-regular.ttf"

1.2.2 outline

Specifies whether the text should have a black outline.

1.2.3 font size

Specifies the size of the font used.

1.2.4 number of characters

Specifies the maximum number of characters a name displayed in a Kill Feed entry can have!

If a player's username is longer than the specified number it will be trimmed!

1.2.5 remove special characters

Specifies if names containing special characters should be stripped of special characters.

Disabling this should allow Asian characters to be displayed.

exceptions:


* all characters contained in 3.6 AllowedSpecialCharacters
* all lowercase letters contained in the English alphabet
* all uppercase letters contained in the English alphabet
* all Arabic numerals

1.2.6 remove tags

Specifies if leading tags should be removed. Will only remove leading tags that start with '[' and end with ']'.

examples:


* input: "[tag] Tuntenfisch" | output: "Tuntenfisch"
* input: "Tuntenfisch [tag] Tuntenfisch" | output: "Tuntenfisch [tag] Tuntenfisch"

1.3.1 animals

Specifies if an animal killed by a player or a player killed by  an animal should be displayed!

1.3.2 player deaths

Specifies whether the Kill Feed should display player deaths or players being wounded!

1.4.1 log entries

Specifies whether Kill Feed entries should be logged!

If enabled, each Kill Feed entry will be logged inside the KillFeed.EntryLog.json file!


The KillFeed.EntryLog.json file will only be created/updated if this plugin is unloaded/reloaded!

The KillFeed.EntryLog.json file can be found under .../server/<identity>/oxide/data.

1.4.3 print entries to console

Specifies whether entries should be printed to console.

1.4.3 debugging

Specifies which level of debugging should be enabled!

If enabled, debug information will be output to the server console!

levels:


* 0: no debugging
* 1: first level debugging displays [www.error](http://www.error) messages

2.1 formatting

Specifies the formatting of an entry.

The available keywords are displayed below:

keywords:


* "{initiator}" will be replaced with the initiator
* "{hitBone}" will be replaced with the bone which was hit
* "{weapon}" will be replaced with the weapon image (required)
* "{distance}" will be replaced with the distance
* "{hitEntity}" will be replaced with the entity which was hit

By modifying the formatting, information can be left out or added!

2.2 number of entries

Specifies the number of entries that will be displayed at max!

2.3 destroy after

Specifies if the Kill Feed should be removed after X seconds!

Disable destroyAfter by setting the value to 0.0!

2.4 Action-On-Key-Use

You can add/remove/change keybindings here!


The keybindings contained inside the default configuration file are there to ensure the Kill Feed is properly destroyed/added if the inventory has been opened/closed!

If no default action is available for a given key leave the right hand string empty!

You will need to append a comma if you are not at the end of the list!

example key:

````
{

  "key": "tab",                                               // the key the action should be bound to

  "action": "inventory.toggle;killfeed.action add destroy",   // the action that should be bound

  "defaultAction": "inventory.toggle"                         // the default action the key normally has
}
````

2.5.1 width

Specifies the width of the entry.

2.5.2 icon half-height

Specifies the half-height and hence the size of the icon displayed in a Kill Feed entry.

2.6.1 x

Specifies the horizontal position of the first entry!

Values can range from 0.0 (left edge of the screen) to 1.0 (right edge of the screen)!

The horizontal anchor is the center of the entry!

2.6.2 y

Specifies the vertical position of the first entry!

Values can range from 0.0 (bottom edge of the screen) to 1.0 (top edge of the screen)!

The vertical anchor is the center of the entry!

2.7.1 horizontal

Specifies the horizontal spacing between individual Kill Feed entries!

Use positive/negative values to specify right/left!

2.7.2 vertical

Specifies the vertical spacing between individual Kill Feed entries!

Use positive/negative values to specify up/down!


2.8.1 in

Specifies how quickly a new entry fades in.

Disable by setting the value to 0.0!

2.8.2 out

Specifies how quickly a old entry fades out.

Disable by setting the value to 0.0!

2.9.1 initiator

Specifies the color of the initiator text if the initiator is a player!

The color can be specified in the traditional HTML format.

example: "#ff0000ff" = red

2.9.2 info

Specifies the color of the bone and distance text!

The color can be specified in the traditional HTML format.

example: "#ff0000ff" = red

2.9.3 hitEntity

Specifies the color of the hitEntity text if the hitEntity is a player!

The color can be specified in the traditional HTML format.

example: "#ff0000ff" = red

2.9.4 npc

Specifies the color of the initiator/hitEntity text if the initiator/hitEntity is a non-player character!

The color can be specified in the traditional HTML format.

example: "#ff0000ff" = red

3.1 file directory

If several files you access are in the same directory you can specify the common path here!

If you specify a fileDirectory make sure to use forward slashes and either put "http://" or "file:///" in front of your directory!

3.2 Files

You can add/remove/change or disable item icons here!


If you don't want entries with specific item icons to show up, disable/remove those items, either by completely removing them from the list or by leaving the second string of those specific items empty!


Make sure the files are of type ".png"!


The first string always has to be the shortname of the corresponding item followed by a colon, a space and the string that completes the filepath! You will need to append a comma if you are not at the end of the list!

If you haven't specified a fileDirectory you will need to use the fully qualified filepath for each individual file starting with either "http://" or "file:///"!

3.3 Damagetype Files

If no fitting item icon can be found, a damage-type icon may be displayed instead.


You can add/remove/change or disable damage type icons here!


If you don't want entries with specific damage type icons to show up, disable/remove those damage types, either by completely removing them from the list or by leaving the second string of those specific damage types empty!


Make sure the files are of type ".png"!


The first string always has to be the shortname of the corresponding item followed by a colon, a space and the string that completes the filepath! You will need to append a comma if you are not at the end of the list!

If you haven't specified a fileDirectory you will need to use the fully qualified filepath for each individual file starting with either "http://" or "file:///"!

3.4 NPC Names

You can add/remove/change the custom display name of a NPC here!


The first string always has to be the NPC name followed by a colon, a space and the string that specifies the custom display name! You will need to append a comma if you are not at the end of the list!

3.5 Bone Names

You can add/remove/change the custom display name of a bone here!


The first string always has to be the bone name followed by a colon, a space and the string that specifies the custom display name! You will need to append a comma if you are not at the end of the list!

3.6 Allowed Special Characters

You can add/remove allowed special characters here!


The string has to be a single character. You will need to append a comma if you are not at the end of the list!

« Default Language File »


You can find the language file under .../server/<identity>/oxide/lang.

If you edit the language file, make sure to save it and then reload this plugin!

Make sure to only ever edit the right hand string of each line!

````
{

  "killfeed": "<color=red>[KillFeed]</color> /killfeed disable<color=red>|</color>enable<color=red>|</color>status",

  "killfeed enable > enabled": "<color=red>[KillFeed]</color> enabled!",

  "killfeed enable > already enabled": "<color=red>[KillFeed]</color> already enabled!",

  "killfeed disable > disabled": "<color=red>[KillFeed]</color> disabled!",

  "killfeed disable > already disabled": "<color=red>[KillFeed]</color> already disabled!",

  "killfeed status > is enabled": "<color=red>[KillFeed]</color> is enabled!",

  "killfeed status > is disabled": "<color=red>[KillFeed]</color> is disabled!"
}
````

killfeed

Specifies the message that will be displayed to the player when said player enters the /killfeed command!

killfeed enable > enabled

Specifies the message that will be displayed to the player when said player enters the /killfeed enable command if the Kill Feed was disabled beforehand!

killfeed enable > already enabled

Specifies the message that will be displayed to the player when said player enters the /killfeed enable command if the Kill Feed was enabled beforehand!

killfeed disable > disabled

Specifies the message that will be displayed to the player when said player enters the /killfeed disable command if the Kill Feed was enabled beforehand!

killfeed disable > already disabled

Specifies the message that will be displayed to the player when said player enters the /killfeed disable command if the Kill Feed was disabled beforehand!

killfeed status > is enabled

Specifies the message that will be displayed to the player when said player enters the /killfeed status command if the Kill Feed is enabled!

killfeed status > is disabled

Specifies the message that will be displayed to the player when said player enters the /killfeed status command if the Kill Feed is disabled!

« Notes »


* Like many UI-based plugins, this plugin looks best with a 16:9 aspect ratio!
* Major updates will still require a plugin update!