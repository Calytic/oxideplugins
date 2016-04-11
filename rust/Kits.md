Kits plugin, with only command chat support

Features:

Choose a limit of usage for kits

Choose a cooldown for kits

Create kits for admins

Create kits for moderators

Create kits for VIPs

Choose 1 Autokit on your server

Supports GUI

Supports NPC

Supports Skins
[Create kits not accessible by players but accessible via the Give plugin](http://forum.rustoxide.com/resources/give.666/)
No more json, too hard for a lot of admins, all kits are configuration from ingame

Players Commands:

- /kit => see the full list of available kits for you

- /kit KITNAME => choose a kit

Admin Commands:

- /kit list => see the full list of kits

- /kit add "KITNAME" => add a new kit

- /kit remove "KITNAME" => remove a kit from the database

- /kit edit "KITNAME" => edit a kit

- /kit resetkits => delete all kits and player data

- /kit resetdata => reset player data

- /kit option1 value1 option2 value2 option3 value3 => set the options for a kit you are currently editing

How to create kits:

1) Empty your inventory

2) Add in your inventory the kit that you want players to have (blueprints, weapons in the belt, armors in the clothing, etc)

3) use /kit add "kitname"

4) set the options via: /kit option1 value1 option2 value2 etc

ex:

/kit items max 10 cooldown 3600 description "Every hour kit, max usage: 10"



Options:
max XXX/false => set the max usage of a kit (false will deactivate it)
cooldown XXXX/false => set the cooldown in seconds of a kit (false will deactivate it)
authlevel X => level needed to redeem the kit
npconly true/false => only avaible via an NPC
permission CUSTOMPERMISSION/false => only players with the custom permission oxide permission will be able to redeem those (doesn't work on autokits). See under.

description "XXXX XXX"/false => set a description for a kit
image "URL" => set an image for a kit in the GUI.
hide true/false => hide a kit from the list: /kit (won't hide from the admin command /kit list)
items => no values here, this will copy the items in your inventory to set it in the kit.

Custom Permissions:

when you created a kit or edited a kit do:

/kit permission PERMISSIONNAME

the permissionname can be a new permission or an existing permission.


Custom permissions are oxide permissions:

````
oxide.grant user "PLAYERNAME" PERMISSIONNAME
````

You can grant a user permission by using:
oxide.grant user <username> <permission>

To create a group:
oxide.group add <groupname>

To assign permission to a group:
oxide.grant group <groupname> <permission>

To add users to a group:
oxide.usergroup add <username> <groupname>

To remove users permission:
oxide.revoke <userid/username> <group> <permission>Click to expand...Auto Kits:

1) /kit add "autokit"

2) /kit authlevel 2 hide true => this will set the kit only manually redeemable for admins, and hide will hide it from the list in /kit.

3) /kit items => this will copy the items in your inventory to set it as the new kit. you don't need to do it seperatly you can do it in the previous line: /kit authlevel 2 items hide true

NPC GUI:

When you create a NPC Kit, you can (or not) use -npconly

When you created your npc, do /npc_list to get the NPC ID.

Then in the config you can add what this npc has:

````
"NPC - GUI Kits": {

"1235439": {

      "description": "Welcome on this server, Here is a list of free kits that you can get <color=red>only once each</color>\n\n                      <color=green>Enjoy your stay</color>",

      "kits": [

        "kit1",

        "kit2"

      ]

    },

    "8753201223": {

      "description": "<color=red>VIPs Kits</color>",

      "kits": [

        "kit1",

        "kit3"

      ]

    }

}

 
````

Chat GUI:

By default there are no chat gui.

But you may replace the default /kit chat command by a gui.

in the NPC - GUI Kits, instead of putting an NPC id, put: "chat".


ex:

````
{

  "NPC - GUI Kits": {

    "chat": {

      "description": "<color=green>Chat Kits</color>",

      "kits": [

        "lotwood",

        "wood",

    "autokit"

      ]

    }

  }

}
````

Default Example Configs in:

oxide/config/Kits.json

````

{

  "NPC - GUI Kits": {

    "1235439": {

      "description": "Welcome on this server, Here is a list of free kits that you can get <color=red>only once each</color>\n\n                      <color=green>Enjoy your stay</color>",

      "kits": [

        "kit1",

        "kit2"

      ]

    },

    "8753201223": {

      "description": "<color=red>VIPs Kits</color>",

      "kits": [

        "kit1",

        "kit3"

      ]

    }

}

 
````

Settings authLevel is the level needed to use the admin commands

note that a level 1 can't remove a kit from a level 2.

For Plugin Devs

To refuse a kit to be given (arena, specific player moderation, etc)

you may do this:

````
function PLUGIN:canRedeemKit(player)

    if(ArenaPlayers[player]) then

        return "You are currently in an Arena, you may not redeem any kit"

    end

    -- don't return anything if you want to let the kit to be redeemed

end
````

By returning ANYTHING it will refuse the kit to be given, return a text to specify the reason.


Check if the kit exists:

````
object theanswer = Interface.CallHook("isKit", KITNAME);
````