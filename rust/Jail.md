This was originally created by Reneb. Credit for this plugin goes to him. If you like his work please consider donating to him and hit that 'Support the Developer' button! ----------------->

Features:

- Send players to jail

- Free players from jail

- Configure the spawn points

- Inmates can't leave the jail zone radius (they will be automatically teleported back)

- Players can't enter the jail zone radius (they will be automatically teleported away)

- Admins can enter the jail zone radius

- Inmates can die from anything

- Inmates will be teleported back in the jail on death

- Inmates will be teleported back in the jail on reconnection (if they aren't in the jail)

- Inmates can't request Kits

- Inmates can't Teleport out (or in)

- Inmates can't build / deploy

- Jail is undestructable

- Support for multiple jails

- Option to select which jail to send a inmate

- Inmates are issued a cell number when they are sent to jail

- Option to give a inmate kit

- Option to turn inmate damage on/off

- Option to send inmates back to where they were before they were arrested

- Save inventory on incarceration and restores it when they leave

Jail Setup and usage instructions!

- Find a location for your jail


- Build a jail building (if you want to)


- Create a spawnfile for your jail.

Players are issued their own spawnpoint when they are incarcerated, if you run out of spawn points you will NOT be able to send any more players to Jail


- Create a jail zone using '/jail zone <radius>' 

This will create a standard jail zone, If you wish to you may further edit this zone using '/zone_edit zoneID'


- Register your jail using '/jail add <JailName> <ZoneID> <Spawnfile>'


- You can now send players to jail using '/jail send <playername> <optional:Time> <optional;PrisonName>'.


You may select how much time (in seconds) the player is incarcerated for and also which prison you would like to send them to. 

By not setting a time the player will be incarcerated indefinitely.

By not setting a prison name the player will be sent to the 1st prison that has empty cells


Oxide Permissions: "jail.admin"
You can grant a user permission by using the following in console:
grant user <username> <permission>

To create a group:
group add <groupname>

To assign permission to a group:
grant group <groupname> <permission>

To add users to a group:
usergroup add <username> <groupname>

To remove users permission:
revoke <userid/username> <group> <permission>
Click to expand...Chat Commands:

- /jail zone <radius> - Creates a jail zone on your position with the required flags.

- /jail add <JailName> <ZoneID> <Spawnfile> - Create a new Jail

- /jail remove <JailName> - Remove a jail

- /jail list - List all jails and their location

- /jail send <PlayerName> <optional:time> <optional:JailName> - Send a play to jail.

- /jail free <PlayerName> - Release a player from jail


Players in jail can type /jail to see how much time they have left of their sentence.

Console Commands:

- jail.send <PlayerName> <optional:time> <optional:JailName>

- jail.free <PlayerName>

Config file:

Jail.cs

````

{

  "Inmates - Disable damage inside the Jail": false, // Disables inmates from dealing damage

  "Inmates - Kits - Give kit to Inmates": true, // Issues a kit upon incarceration

  "Inmates - Kits - Kitname": "default", // Name of the prisoner kit

  "Inmates - Release - Return to initial position when released": true, // Sends players back to where they were before being incarcerated

  "Messages - Message color": "<color=#d3d3d3>" //Message color

}

 
````