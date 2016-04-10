This plugin converts the pain of filling furnaces into pure joy!


This is a first version, I have plans to make it configurable on a per-user basis since not everyone will want to fill their furnaces the same way. I went with what I use usually when playing.


So here's how it works (short version):


1. Drop a full stack of ore in an empty furnace

2. Your ore automatically splits, a stack of wood from your inventory is moved to the furnace (a metal frag is added as well for metal ore) and the furnace is started automatically.


Long version:


- Currently ore is split in 4 equal parts, sulfur in two equal parts

- I did not optimize the loadout for sulfur to drop charcoal, so it's not the "optimal" loadout (will allow different setups in a future version)

- I use the current maximum stack size as described in the item so it should work with any stack altering mods

- The plugin totally ignores the burn rate, so on a vanilla server for instance you will end up with 1000 wood and 4 stacks of 250 ore, leaving 200 unburnt ore when the wood runs out (will modify this later on)

- If a full stack of ore is not provided, plugin aborts action

- If a full stack of wood or a single metal frag for metal ore is not found, plugin aborts action

- If the furnace contains anything at all when placing the ore, plugin aborts action


Enjoy!