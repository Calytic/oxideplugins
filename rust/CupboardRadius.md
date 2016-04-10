The **August 27th, 2015** update changed the cupboard build zone shape to a mesh instead of a sphere so that it's sometimes possible to place another cupboard bellow an existing one, in some configurations resulting in a situation where a player is able to demolish lower levels of a building.


This plugin changes the cupboard build zone shape back to a sphere and also makes its radius configurable (defaults to cupboardRadius = 25).


Also, the **October 29th, 2015** update restricted cupboard placement within each others influence. There's now also a config option for that (optional, defaults to ignoreInfluenceRestriction = false which is the new behavior).


Hence, this plugin might give your players some additional time to prepare for the (potentially game breaking) changes. But note that Facepunch wants to get rid of cupboards eventually, so this won't last forever.


Maybe consider a donation if this saved your day 

**Default configuration:**

````
{

  "cupboardRadius": 25,

  "ignoreInfluenceRestriction": false

}
````