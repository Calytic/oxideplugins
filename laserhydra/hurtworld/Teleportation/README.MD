**Teleportation **is a teleportation plugin with many different teleportation features.
**Permissions:**


* 
**teleportation.admin **for admin commands (/tp & /tphere & /setwarp & /removewarp)
* 
**teleportation.tpr **for teleport request commands (/tpr & /tpa)
* 
**teleportation.home **for home commands (/home, /sethome, /removehome & /homes)
* 
**teleportation.warp** for warp commands (/warp & /warps)


**How to grant permissions:**

Use following CHAT commands to grant permissions


* 
**/grant user <player> <permission>** grant permission to player
* 
**/grant group <group> <permission>** grant permission to group


**Chat Commands:
**

Teleportation:


* 
**/tpa **Accept teleport request
* 
**/tpr <player> **Send teleport request
* 
**/tp <player> **Teleport yourself to a player
* 
**/tp <player> <player> **Teleport a player to another
* 
**/tp <x> <y> <z> **Teleport yourself to a location (~ can be used as x, y or z to represent your x, y, z)
* 
**/tphere <player>** Teleport somebody to yourself

Homes:


* 
**/home <home> **Teleport to your home
* 
**/homes **List all your homes
* 
**/sethome <home> **Set your home
* 
**/removehome <home> **Remove your home

Warps:


* 
**/warp <warp>** Teleport to a warp
* 
**/warps** List all warps
* 
**/setwarp <warp>** Set a warp
* 
**/removewarp <warp>** Remove a warp


**What are Homes?**

This plugin allows players with the permissios 'teleportation.home' to set home locations. **/sethome <home>**

The player can teleport to his home locations. **/home <home>**

You can set up a cooldown in the configfile, so players can only teleport to a home every few minutes.
**What are Warps?**

This plugin allows admins with the permission 'teleportation.admin' to set warp locations. **/setwarp <warp>**

Players can teleport to these locations. **/warp <warp>**

You can set up a cooldown in the configfile, so players can only teleport to a warp every few minutes.
**

Configfile:**

````
{

  "Settings": {

    "Home : Check for Stake": true,

    "Home : Cooldown Enabled": true,

    "Home : Cooldown in minutes": 5.0,

    "Home : Enabled": true,

    "Home : Maximal Homes": 3,

    "Home : Stake Radius": 10.0,

    "Home : Surrender on Teleport": false,

    "Home : Teleport Timer": 15.0,

    "Home Limits": {

      "teleportation.homelimit.vip": 5

    },

    "TPR : Cooldown Enabled": true,

    "TPR : Cooldown in minutes": 5.0,

    "TPR : Enabled": true,

    "TPR : Pending Timer": 30.0,

    "TPR : Surrender on Teleport": false,

    "TPR : Teleport Timer": 15.0,

    "Warp : Cooldown Enabled": true,

    "Warp : Cooldown in minutes": 10.0,

    "Warp : Enabled": true,

    "Warp : Surrender on Teleport": false,

    "Warp : Teleport Timer": 15.0

  }
}
````


**Languagefile:**

````
{

  "No Permission": "You don't have permission to use this command.",

  "Request Ran Out": "Your pending teleport request ran out of time.",

  "Request Sent": "Teleport request sent.",

  "Request Got": "{player} would like to teleport to you. Accept by typing /tpa.",

  "Teleported": "You have been teleported to {target}.",

  "Accepted Request": "{player} has accepted your teleport request.",

  "No Pending": "You don't have a pending teleport request.",

  "Already Pending": "{player} already has a teleport request pending.",

  "Teleporting Soon": "You will be teleported in {time} seconds.",

  "Teleport To Self": "You may not teleport to yourself.",

  "No Homes": "You do not have any homes.",

  "Home Set": "You have set your home '{home}'",

  "Home Removed": "You have removed your home '{home}'",

  "Home Exists": "You already have a home called '{home}'",

  "Home Teleported": "You have been teleported to your home '{home}'",

  "Home List": "Your Homes: {homes}",

  "Max Homes": "You may not have more than {count} homes!",

  "Unknown Home": "You don't have a home called '{home}'",

  "No Stake": "You need to be close to a stake you own to set a home.",

  "Home Cooldown": "You need to wait {time} minutes before teleporting to a home again.",

  "TPR Cooldown": "You need to wait {time} minutes before sending the next teleport request.",

  "Warp Set": "You have set warp '{warp}' at your current location.",

  "Warp Remove": "You have removed warp '{warp}'",

  "Warp Teleported": "You have been teleported to warp '{warp}'",

  "Unknown Warp": "There is no warp called '{warp}'",

  "Warp List": "Available Warps: {warps}",

  "Warp Exists": "There already is a warp called '{warp}'",

  "No Warps": "There are no warps set.",

  "Warp Cooldown": "You need to wait {time} minutes before teleporting to a warp again."
}
````