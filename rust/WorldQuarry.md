**This mod is in early development and likely will change!**


This mod adds the ability to create a "quarry" in the open-world using deployable objects. (Currently 9x boxes)


The idea is simple: Place 9 boxes (in a 3x3), and smack the middle one with a torch. The middle box will then start gathering stone/metal/sulfur, as if it were a quarry.


You can find a small example here: [https://dl.dropboxusercontent.com/u/2068143/Share/2015-05/2015-05-27_16-37-19.png](https://dl.dropboxusercontent.com/u/2068143/Share/2015-05/2015-05-27_16-37-19.png)


This plugin also comes with some config options to make things more (or less) balanced for server owners:

````
{

  "StoneChance": 0.25,

  "MetalChance": 0.1,

  "SulfurChance": 0.05,

  "AmountToCreatePerTick": 1,

  "TicksToInclude": 0.25

}
````



* StoneChance - The chance for each tick to produce stone. (0.0-1.0 [0%-100%])
* MetalChance - Same as above, but for metal.
* SulfurChance - Same again, but this time for sulfur.
* AmountToCreatePerTick - The amount of any given resource to create per included tick (see below). The higher this value, the more resources you gain each time the quarry "mines" something.
* TicksToInclude - The percentage of ticks since the last quarry update to include. (See below for more info)

This plugin will attempt to spread out quarry updates over multiple ticks (so the server isn't hammered immediately by trying to update hundreds of quarries in 1 tick). By default, each quarry is only updated once every 10 ticks (you can chance this in the .cs file directly, there will be no setting available for it). This is due to the fact that when looking inside a chest that is actively being updated, Rust will lag on the client side. The more often you update the chest, the more noticeable the lag is. I found 10 to be a good value to use in this case.


Each quarry keeps track of how many "ticks" have passed since the last update (so you still retain 'realtime' updates, but in a batch-style setup). By default, the quarry will only include 25% of these ticks as possible resource-generating ticks. (The TicksToInclude setting) Raising this value will cause each tick since the last update, to try and generate some resources. This is an overall increase in production. Please keep this in mind when modifying the TicksToInclude value!


If you run into any bugs, or just have suggestions, please let me know in the discussion thread!


I do plan on finding a better setup for the quarry. (9 boxes seems a little plain)