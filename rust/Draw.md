**Draw** uses the game's ddraw tools (what the devs use to debug projectile paths for example) to draw primitives directly into a player's world and exposes that as a simple to use API for other plugins to utilize:


* 
**Lines**

Draw.Line(player:BasePlayer, from:UnityEngine.Vector3, to:UnityEngine.Vector3, color:UnityEngine.Color, duration:float)


* 
**Arrows**

Draw.Arrow(player:BasePlayer, from:UnityEngine.Vector3, to:UnityEngine.Vector3, headSize:float, color:UnityEngine.Color, duration:float)


* 
**Spheres**

Draw.Sphere(player:BasePlayer, pos:UnityEngine.Vector3, radius:float, color:UnityEngine.Color, duration:float)


* 
**Texts**

Draw.Text(player:BasePlayer, pos:UnityEngine.Vector3, text:string, color:UnityEngine.Color, duration:float)


* 
**Boxes**

Draw.Box(player:BasePlayer, pos:UnityEngine.Vector3, size:float, color:UnityEngine.Color, duration:float)



**Example (drawing a red line into a player's look direction):**

````
using UnityEngine;

...

[PluginReference] Plugin Draw;

...

Draw.Call(

    "Line",

    player,

    player.eyes.position,

    player.eyes.position + player.eyes.rotation * Vector3.forward * 4f,

    Color.red,

    10f

);
````