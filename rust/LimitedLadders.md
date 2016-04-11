This plugin can remove bypass of building permission from ladders and they cannot be placed when building is blocked.


As an alternative it's possible to prohibit placing ladders only on the constructions where building is blocked.

For this, set DisableOnlyOnConstructions to true.


Default Configuration:

````
{

  "BuildingBlockedMsg": "Building is blocked!",

  "DisableOnlyOnConstructions": false

}
````

Detail config:
BuildingBlockedMsg - This customizable message will show up only if 'DisableOnlyOnConstructions' setting is enabled.
DisableOnlyOnConstructions - If turned on, it will prohibit placing ladders only on the constructions where building is blocked. Static objects (icebergs, rocks) will remains untouched.

If turned off the ladder will act as regular deployable object when building is blocked.

Configuration is creating automatically if not exists.