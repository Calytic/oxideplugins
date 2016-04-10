**Global Anti Glitch plugin,**

You maybe **choose **in the options what to use and not use if you only want part of the plugin!

If you see any other kind of glitchs or want this one to be tweaked you may as in this resources post 
1) **Anti Pillar-Barricade**

Prevents players from using Pillar-Barricade (some servers consider it as a glitch, others dont. Personnally i didn't concider it as a glitch as it would mean banning 50% of my players ... but i'll use this now)


2) **Foundation Glitch (Sleeping bags/Boxes/etc placed under the foundations)**

Prevents players from placing anything under a foundation


3) **Anti Pillar-Stash**

Prevents players from placing a pillar if they placed a small stash under it.


4) **Anti Ramp Stacks**

Prevent players from stacking ramps (you can choose how many they can)

with the new oxide 2.0 version this feature will not create lags


5) **Anti Rock Glitch**

Will detect players that respawn under a rock, kill them and destroy the sleeping bag (and yes it's THAT easy )


6) **Anti Ramp Glitch**

Prevents players from building anything under a ramp (or over themselves)


7) **Anti Storage-Door Glitch**

Prevent players from being able to loot on other sides of doors

The plugin will give back the item


8)** WallLoot:**

Checks if the player has a pillar/wall/doorway a bit on his left and a pillar/wall/doorway a bit on his right, if BOTH are closer then the box he is looter = WallLoot detection.

**Config:** AntiGlitch.json

````

{

  "anti FoundationGlitch: activated": true,

  "anti Pillar-Barricade: activated": true,

  "anti Pillar-Stash: activated": true,

  "anti RampGlitch: activated": true,

  "anti RampStack: activated": true,

  "anti RampStack: max allowed": 2.0,

  "anti RockGlitch: activated": true,

  "anti RockGlitch: Destroy Sleeping Bag": true,

  "anti RockGlitch: Kill Player": true,

  "anti StorageBox-Door Glitch: activated": true,

  "anti Wall-Loot: activated": true,

  "anti Wall-Loot: Punish By Ban": true,

  "anti Wall-Loot: Punish By Kick": true,

  "Messages: Glitch Broadcast": "[color #FFD630] {0} [color red]tried to glitch on this server!"

}

 
````