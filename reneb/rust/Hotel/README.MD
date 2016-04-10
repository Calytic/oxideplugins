**Admin Commands (oxide permission: canhotel):**

- /hotel_new NAME => Create a new hotel

- /hotel_edit NAME => Edit a Hotel

- /hotel_remove NAME => Remove a Hotel

- /hotel_list => Get the lit of the hotels

- /hotel ARG1 ARG2 => Edit the hotel options

- /hotel_reset => Remove ALL hotels

- /room optional:ROOMID reset => resets the room

- /room optional:ROOMID duration XX => sets a NEW duration for the room in seconds (only affects current player's room duration)

**Hotel Options:**

- /hotel location => sets the center hotel location where you stand

- /hotel npc NPCID => sets the NPC that is hooked to this hotel (for UseNPC items)

- /hotel permission PERMISSIONNAME => sets the oxide permissions that the player needs to rent a room here

- /hotel radius XX => sets the radius of the hotel (the entire structure of the hotel needs to be covered by the zone

- /hotel rentduration XX => Sets the duration of a default rent in this hotel. 0 is infinite

- /hotel rentprice XXX => Sets the rent price, this requires **Economics**

- /hotel reset => resets the hotel data (all players and rooms but keeps the hotel

- /hotel roomradius XX => sets the radius of the rooms

- /hotel rooms => refreshs the rooms (detects new rooms, deletes rooms if they don't exist anymore, if rooms are in use they won't get taken in count)

**Creating a New Hotel:**



1) Create your hotel and your rooms.

You must note that all deployables must be seen by the door

You must place a CODE LOCK on the door for it to be recognised as a room.

If more then 1 door with a code lock is detectable by a deployable, this deployable will NOT be saved as inside a room (so you may place items in corridors)

2) /hotel_new NAMEOFHOTEL

3) Go to the center of your hotel and type: /hotel location

4) Set the radius of the hotel zone: /hotel radius XX

5) You may want to set the radius of the rooms if you have very very big rooms you may want to increase it. Other then that you should keep it to 10. /hotel roomradius 10

6) Set the duration of the rent: /hotel rentduration XX (default is 86400 = 1day)

7) /hotel rooms. This will detect all the rooms and all the default deployables that will remain when the room resets. Make sure that all the deployables that you have placed are correctly detected in everyroom. If they are not, you might want to make sure that the deployable can be seen by the door.

8) /hotel_save => DONT FORGET THIS!!!


Now people may use the hotel. Everything is automated so you shouldn't have to manage it.

**Add NPC:**

create a new NPC with /npc_add

get his ID with /npc_list

and in /hotel_edit NAMEOFHOTEL

do /hotel npc NPCID

**Zone Management:**

You will HAVE to use the zone manager tool to do what ever you want to the zone.

Use /zone_edit HOTELNAME to edit the hotel's zone.
**i STRONGLLLYYY recommend using:

"nobuild" and "nodeploy" and "undestr"**

**Rooms for Permissions Only:**


in /hotel_edit NAMEOFHOTEL

use /hotel permission PERMISSIONNAME (you may create a new one or use an existing one)

**Oxide Permissions Usage:**

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
**oxide.revoke <userid/username> <group> <permission>**Click to expand...Config File:

````

{

  "AdminMessage - Hotel - Edit - Confirm": "You are editing the hotel named: {0}. Now say /hotel to continue configuring your hotel. Note that no one can register/leave the hotel while you are editing it.",

  "AdminMessage - Hotel - Edit - Help": "You must select the name of the hotel you want to edit: /hotel_edit HOTELNAME",

  "AdminMessage - Hotel - Error - Already Editing Hotel": "You are already editing a hotel. You must close or save it first.",

  "AdminMessage - Hotel - Error - Already Exist": "{0} is already the name of a hotel",

  "AdminMessage - Hotel - Error - Doesnt Exist": "The hotel \"{0}\" doesn't exist",

  "AdminMessage - Hotel - Error - Not Allowed": "You are not allowed to use this command",

  "AdminMessage - Hotel - New - Confirm": "You've created a new Hotel named: {0}. Now say /hotel to continue configuring your hotel.",

  "AdminMessage - Hotel - New - Help": "You must select a name for the new hotel: /hotel_new HOTELNAME",

  "Configure - Level Required": 2,

  "GUI - Admin - Board Message": "                             <color=green>HOTEL MANAGER</color> \n\nHotel Name:      {name} \n\nHotel Location: {loc} \nHotel Radius:     {hrad} \n\nRooms Radius:   {rrad} \nRooms:                {rnum} \n<color=red>Occupied:            {onum}</color>\nRent Price:                  {rp}",

  "GUI - Admin - maxX": "1.0",

  "GUI - Admin - maxY": "0.9",

  "GUI - Admin - minX": "0.65",

  "GUI - Admin - minY": "0.6",

  "GUI - Player - Board Message": "                             <color=green>{name}</color> \n\nRooms:        <color=green>{fnum}</color>/{rnum} ",

  "GUI - Player - Board Remove Timer": 10,

  "GUI - Player - Maintenance Board Message": "                             <color=green>{name}</color> \n\nHotel is under maintenance. Please wait couple seconds/minutes until the admin is finished.",

  "GUI - Player - maxX": "0.6",

  "GUI - Player - maxY": "0.95",

  "GUI - Player - minX": "0.3",

  "GUI - Player - minY": "0.7",

  "GUI - Player - Room Board Message ": "\n\n                        Your Room\nJoined:         {jdate}\nTimeleft:      {timeleft}.",

  "GUI - Player - Show Board When Entering Hotel Zone": false,

  "GUI - Player - Show Board When Opening Room Door": true,

  "GUI - Player - Show Board When Talking To NPC": true,

  "GUI - Player - Show Room When Entering Hotel Zone": false,

  "GUI - Player - Show Room When Opening Room Door": false,

  "GUI - Player - Show Room When Talking To NPC": true,

  "PlayerMessage - Error - Already have a Room": "You already have a room in this hotel!",

  "PlayerMessage - Error - Need Permissions": "You must have the {0} permission to rent a room here",

  "PlayerMessage - Error - Not Enough Coins": "This room costs {0} coins. You only have {1} coins",

  "PlayerMessage - Error - Restricted": "You are not allowed to enter this room, it's already been used my someone else",

  "PlayerMessage - Error - Unavaible Room": "This room is unavaible, seems like it wasn't set correctly",

  "PlayerMessage - Hotel Maintenance": "This Hotel is under maintenance by the admin, you may not open this door at the moment",

  "PlayerMessage - Limited Access": "You now have access to this room. You are allowed to keep this room for {0}",

  "PlayerMessage - Payd Rent": "You payd for this room {0} coins",

  "PlayerMessage - Unlimited Access": "You now have access to this room for an unlimited time"

}

 
````