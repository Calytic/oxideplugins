**OPTIONAL:
[Share Database](http://oxidemod.org/plugins/share-database.935/) 1.0.1

Features:**

- SetHome to teleport to

- Choose to allow only buildings or not

- Choose to allow only foundation on buildings on not

- Choose to allow only on own buildings or/and shared buildings ([Share Database](http://oxidemod.org/plugins/share-database.935/) 1.0.1)

- Choose max homes allowed

- Choose cooldown

- Choose to cancel teleportations when hit
**Commands:**

- /sethome XXX => sethome on your position

- /sethome remove XX => remove a home point

- /home => get the list of your homes

- /home XXX => teleport to your home

Config file: SetHome.json

````
{

  "Home: Cancel teleport when hurt": true,

  "Home: Teleportations Cooldown": 60,

  "Home: Time to teleport": 30,

  "Messages: Home Doesn't exist": "This home doesn't exist.",

  "Messages: Home Erased": "{0} home point was erased",

  "Messages: Homes List": "Homes list:",

  "Messages: Max Home Reached": "You've reached the maximum homes allowed",

  "Messages: New home": "You've set a new home named {0} @ {1}",

  "Messages: No Homes Set": "You dont have any homes set.",

  "Messages: Only Buildings": "You are only allowed to set home on buildings on this server.",

  "Messages: Only Foundations": "On buildings, you are only allowed to sethome on foundations.",

  "Messages: Only Self Buildings": "On buildings, you are only allowed to sethome on your home.",

  "Messages: Only Self Or Friends Buildings": "On buildings, you are only allowed to sethome on your home or shared homes.",

  "Messages: Restricted command": "You are not allowed to use this command.",

  "Messages: Restricted Location": "You are not allowed to sethome here.",

  "Messages: Sethome Help 1": "/sethome XXX => to set home where you stand",

  "Messages: Sethome Help 2": "/sethome remove XXX => to remove a home",

  "Messages: Teleportation Accepted": "You will be teleported in {0} seconds.",

  "Messages: Teleportation cancelled": "Teleportation was cancelled",

  "Messages: Teleportation Cooldown": "You must wait {0} seconds before requesting another home teleportation.",

  "Messages: Teleportation Pending": "You are already waiting for a home teleportation.",

  "Messages: Teleportation Restricted": "You are not allowed to teleport from where you are.",

  "Sethome: allow Share Plugin": true,

  "Sethome: If on building, only on foundations": true,

  "Sethome: If on building, only on own house (or shared house)": true,

  "Sethome: Max Allowed Homes": 3,

  "Sethome: Only on buildings": true

}
````


**For plugins devs:**
**Refuse teleportations for players with:**

(this will refuse teleportations for ALL users, but you can edit it easilly to prevent players from teleportation in or out of certain area, etc)

````
object canTeleport(NetUser netuser)

{

    return false;

}
````