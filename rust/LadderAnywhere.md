**Summary:**

This plugin allows players to **deploy ladders without building privs**, similar to what we had prior to the **August 26th 2015 update **that removed this possibility.

**Usage:**

You have to have the ladder are your **actively held item **then point to where you want it and type **/ldr **and the ladder will be placed for you by the plugin.

**Known issues:**

· The ladder will be placed at a perfectly vertical position regardless of the surface it is set on.

· The ladder is placed without the usual sanity checks and can protrude through building parts, ground, rocks, whatever.

· Ladders will be placeable where they usually would not. I haven't tested it but I assume radtowns, trees, rock nodes, large furnaces, quarries, etc.

***NEW* Default config:**

````
{

  "authLevel": 0,

  "blacklist": [

    "wall.external",

    "player",

    "ladder",

    "cupboard",

    "furnace",

    "barricade",

    "storage"

  ],

  "maxDist": 5.0,

  "radiationCheck": true

}
````


**Credits:**

Most of the code was straight out copy-pasted from the Build plugin ([Build for Rust Experimental | Oxide](http://oxidemod.org/plugins/build.715/)) by Reneb. Sorry, thanks, not sure what to say here =)


**PLEASE REPORT ANY ISSUES ASAP**, I haven't done any extensive testing with this, I'm sure some exploits and other issues will pop up, let me know.