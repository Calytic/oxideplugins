This is a quick port from Oxide 2 Experimental Kit system

If someone makes a better one, i will gadly remove this one.
**Commands:**
/kit => See the list of avaible kits
/kit add "Kitname" "Kit Description" -option1 -option2 etc => Create new kits, see under for the options
/kit reset => reset all player data
/kit remove "Kitname" => remove a kit
/kit help => show the commands for admins
**Options:**

are optional (... daaa)

avaible options:

-maxXXX => max amount of time this kit is allowed

-cooldownXXX => cooldown to use this kit

-CUSTOMPERM => avaible for players that have the custom oxide permission that you can set in the configs ( -vip, -donator, are the 2 default one)

-admin => only avaible for admins
**Autokit:**

Command to add the autokit is:

/kit add "autokit" "blabla bla bla" -admin => dont forgot to add the -admin to prevent players from requesting it manually

(No there are no auto kits for vip nor for admins)
**How To:**

Add all the items that you want in your kit in your inventory

use the /kit add command,

if the kit was successfully created you will be stripped of all your items.
**Custom Permissions:**

Admins that can use the /kit add/reset/remove commands are: **rcon.login**

VIP that can use vip kits must have the  oxide permission: "**vip**"

but you can add as many permissions as you want in the configs.

default are "donator" and "vip"
You can grant a user permission by using:
**oxide.grant user <username> <permission>**

To create a group:
**oxide.group add <groupname>**

To assign permission to a group:
**oxide.grant group <groupname> <permission>**

To add users to a group:
**oxide.usergroup add <username> <groupname>**

To remove users permission:
**oxide.revoke <userid/username> <group> <permission>**
Click to expand...
**For Plugin Devs**

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

Configs: Kits.json

````

{

  "Messages: cantUseKit": "You are not allowed to use this kit",

  "Messages: itemNotFound": "Item not found: ",

  "Messages: kithelp": "/kit => get the full list of kits",

  "Messages: kitredeemed": "You've redeemed a kit",

  "Messages: kitsreset": "All kits data from players were deleted",

  "Messages: maxKitReached": "You've used all your tokens for this kit",

  "Messages: noAccess": "You are not allowed to use this command",

  "Messages: unknownKit": "This kit doesn't exist",

  "Settings: RemoveDefaultKit": true,

  "Settings: Permissions List": [

    "vip",

    "donator"

  ]

}

 
````