Kits plugin, with only command chat support
**Features:**

Choose a **limit** of usage for kits

Choose a **cooldown** for kits

Create kits for **admins**

Create kits for **VIPs**
**Players Commands:**

- /kit => see the full list of available kits for you

- /kit KITNAME => choose a kit
**Admin Commands:**

- /kit list => see the full list of kits

- /kit add "KITNAME" => add a new kit

- /kit remove "KITNAME" => remove a kit from the database

- /kit edit "KITNAME" => edit a kit

- /kit resetkits => delete all kits and player data

- /kit resetdata => reset player data

- /kit option1 value1 option2 value2 option3 value3 => set the options for a kit you are currently editing
**How to create kits:**

1) Empty your inventory

2) Add in your inventory the kit that you want players to have

3) use /kit add "kitname"

4) set the options via: /kit option1 value1 option2 value2 etc

ex:

/kit items max 10 cooldown 3600 description "Every hour kit, max usage: 10"

**Options:**
max XXX/false => set the max usage of a kit (false will deactivate it)
cooldown XXXX/false => set the cooldown in seconds of a kit (false will deactivate it)
authlevel 1/0 => 1 is for admin only, 0 for players
permission CUSTOMPERMISSION/false => only players with the custom permission oxide permission will be able to redeem those (doesn't work on autokits). See under.

description "XXXX XXX"/false => set a description for a kit
hide true/false => hide a kit from the list: /kit (won't hide from the admin command /kit list)
**items** => no values here, this will copy the items in your inventory to set it in the kit.
**Autokits:**

Create a kit names: "autokit"

1) /kit add "autokit"

2) /kit items authlevel 1 hide true => so no one can redeem the autokit, and the autokit wont show in the list
**Custom Permissions:**

when you created a kit or edited a kit do:

/kit permission PERMISSIONNAME

the permissionname can be a new permission or an existing permission.

Custom permissions are oxide permissions:

````
oxide.grant user "PLAYERNAME" PERMISSIONNAME
````

You can grant a user permission by using:
**oxide.grant user <username> <permission>**

To create a group:
**oxide.group add <groupname>**

To assign permission to a group:
**oxide.grant group <groupname> <permission>**

To add users to a group:
**oxide.usergroup add <username> <groupname>**

To remove users permission:
**oxide.revoke <userid/username> <group> <permission>**Click to expand...
**For Plugin Devs**

To refuse a kit to be given

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