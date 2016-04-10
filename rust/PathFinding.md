This plugin can only be used by other plugins.

As it's a lot of code, and probably usable by other plugins, i'll leave this plugin stand alone.

And it will be much easier to improve it

If you have any suggestions, any changes to make you are welcomed to help me 
I tried to optimize this plugin as much as i can so:

- It only takes in count coordinates such as: 241, 54, 2340
**no decimals**. (using decimals makes the code go crazy slow)

- You can easily **call this plugin** from other plugins

- The plugin is **limited to 500 Loops,** higher then that it will result as null (meaning no paths were found).

- 1000 Loops is about 0.3s of search. 500Loops is about 0.06s

200m on the ground will be around 200Loops

But if you use it in houses with multiple levels, loops will occur much more (work is probably still needed on that)
**The PathFinding:**

- **Easy path** => a simple line path that checks if there are anything blocking your way to another point, if not it will use this Easy straight path.

- **A* Pathfinding algorithm 3D **=> exemple of how the 2D works

**Commands:**
- /path => Aim where you want to go, and the plugin will try to force you to go there.

Default Configfile PathFinding.json

````
{

  "Max Loops": 500

}
````

Max Loops being the max loops allowed before the plugin timesout (500 is about 0.15s, 1000 around 0.4s and 5000 around 2s)

**Include in Other plugins:**

````
List<Vector3> FindBestPath(Vector3 sourcePosition, Vector3 targetPosition)

return the list of Vector3 accessible

or null if no path was found

this uses the Easy path first, and if it can't use the easy path, it uses the A* algorithm

 
````


````
List<Vector3> FindPath(Vector3 sourcePosition, Vector3 targetPosition)

return the list of Vector3 accessible

or null if no path was found

uses the A* algorithm

 
````


````
List<Vector3> FindLinePath(Vector3 sourcePosition, Vector3 targetPosition)

return the list of Vector3 accessible

or null if no path was found

uses the Easy Path

 
````


````
void FollowPath(BaseEntity entity, List<Vector3> pathpoints)

force an entity to follow the pathpoints, can be a BasePlayer

 
````


````
void FindAndFollowPath(BaseEntity entity, Vector3 sourcePosition, Vector3 targetPosition)

finds the best path then forces the entity to follow the pathpoints, can be a BasePlayer

 
````