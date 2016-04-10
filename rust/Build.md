Build anything the way you want it with some kind of AI.

This will basically be an improvement on my Spawn Plugin from legacy.
**Execute Command:

RIGHT CLICK**

Click 1 time at a time will spawn things 1 by 1.

Holding RIGHT CLICK for 1 sec, the mass spawning will start
**Permissions:**

set the permissions in the config file: level 1 for moderator, level 2 for admins, level 0 for simple users

you can also you oxide permission system: " **builder **"
Oxide permission system pretty much works like legacy's Flags plugin.

You can also create groups, add permissions to that groups and assign users to it so every user in that group has the permission of that group.
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
**Commands:**
**- **/buildhelp  -> To show you the full commands list
- /build foundation -> To build a foundation where you are looking at (has an AI and will try to align to other structures, see the howto)
- /spawn foundation -> To build a foundation where you are looking at (has NO AI)
- /build "structure" "Optional:HeightAdjustment"  "Optional:Grade"  "Optional:Health" -> To build the structure with AI
- /buildup "structure" "Optional:HeightAdjustment" -> To build the structure on top of another one. default heightadjustment is 3 (1 level), you may set is negative to build down

- /buildrotate => to do a rotation of the structure (rust rotations)

- /buildrotate XX => to do your own rotation of a structure

- /deploy deployable -> To deploy a deployable item (ex /deploy "Lantern")
- /buildheal Optional:all => heal all the building or only the selected part.
- /buildgrade GradeLevel Optional:all => set grade level of all the building or only the selected part
- /buildhelp animals=>  get the list of animals
- /animal name=> spawn an animal
- /buildhelp resources =>  get the list of resources
- /plant ID => Plant a resource (barrel, trees, ores)
**- **/build /plant /animal /deploy /buildheal /buildgrade  -> To Deactivate the building mode.
- /erase => Remove tool that can remove anything (don't use it on other players --')
**Notice**: NONE of the above commands (exept help commands) will work until you press the EXECUTE COMMAND (Right Click)
**HowTo:**

RIGHT SHIFT + L , to start flying in admin mode (if it doesn't work it means you are not an admin and can't use this command )

This might also help you: 
````
server.stability false
````

Buildings:

Build your first foundation or spawn it with: /spawn foundation XX (XX being the height adjustment)

Use a command like /build foundation, to start building a Grade 0 (full healthed) foundation

Aim where you want to build your next foundation (aim a side of the first foundation) then

RIGHT CLICK, to start building, you may hold right click if you want to make a lot of foundation at a time.

Then you may start using other building elements.

On a Foundation/Floor/Roof (Floor Type):

+: Walls / Doorways / Windows will place normally on the side of the foundation

+: Foundations / Floors will be placed next to the foundation

+: Blocks / Stairs will be placed on the foundation

+: Pillars will be placed on the corners of the foundation

+: You may place Roofs, might be a bit glitchy atm, nothing that can't be fixed with a simple rotation 
On a Wall/Window/Low Wall/Doorways (Wall Type):

+: You may place other wall types next to each other

+: You may place a floor on top of the wall.

+: you may place doors

-: You can't place pillars on them

Triangle foundations and floors (Floor Triangle Type):

+: You may place them agaisnt each other

+: You may place walls on them

-: You can't place normal floors/foundations on them (only manually)
**Video Tutorial:** (i need to remake it as the video shows only a part of my screen XD)


Block List:

````

foundation (floor type)

foundation.triangle (floortriange type)

foundation.steps (NOT WORKING)

block.halfheight (block type)

block.halfheight.slanted (stairs) (block type)

block.stair.lshape (block type)

block.stair.ushape (block type)

wall (wall type)

wall.low (wall type)

wall.doorway (wall type)

wall.window (wall type)

floor (floor type)

floor.triangle (floortriangle type)

pillar (support type)

roof (floor type)

door.hinged (door type)

 
````

Animal List:

````

chicken

boar

bear

stag

wolf

horse

 
````


**Config: Build.json**

````

{

  "Config": {

    "authLevel Required (1 moderator, 2 admin)": 2,

    "Pressed time before multiple spawns (seconds)": 1.0

  }

}

}
````


**Want to contribute?**

You may push pull requests on my github:
[Oxide2Plugins/Build.cs at master · strykes/Oxide2Plugins · GitHub](https://github.com/strykes/Oxide2Plugins/blob/master/CSharp/Build.cs)