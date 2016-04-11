This plugin will allow you to remove alot of objects ( check list in Entity ), it all started as a bawNg's twig remover, then i modified it as the community wanted.

**Chat:**
/object entity count optional:time=> Will display how many entities are outside cupboard area.
/object entity remove optional:time=> Will remove all entities outside of cupboard area
optional:time => Will execute the action after X seconds.

/deployable_list => Displays the supported deployables. To add more, just edit the config.

**Console: **Same as the above, just a command in console

**How add deployables to the list:**


In the config you can see the deployables have 2 values:

"KEY": "VALUE"

The key is what you will type in the command (/object key remove) and the value is the deployable's name. It is not rocket science, its simple.

**Entity:**
all => Will count/remove ALL entitys outside cupboard area.
deployable => Will count/remove a specific deployable from /deployable_list

**Examples:**
/object barricade count =>  Displays how many barricades are outside cupboard range.
/object furnace remove => Removes all furnaces outside cupboard range.

**Credits:** bawNg for his twig remover 
If you enjoy my plugins, donate to support new updates/plugins!