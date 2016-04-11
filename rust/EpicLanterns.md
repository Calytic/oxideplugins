**EpicLanterns** will automatically toggle lanterns after sunrise and sunset or in other given time. Also, it will cache list of all lanterns in order to avoid lag, which might occur while retrieving the list of all lanterns.


Future versions might support checking for fuel in lanterns to prevent abuse in the form of excessive amounts of the lanterns on the map.

Default config:

````
{

  "freeLight": true,

  "toggleOffAt": 8,

  "toggleOnAt": 18

}
````



* bool  **freeLight** (not implemented)
* int **toggleOffAt** (must be smaller than **toggleOnAt**)
* int **toggleOnAt**

Although it has method called **hasFuel() **in case that **freeLight** variable was set to false, this method will always return true, because I hadn't examine how to check the inventory of a particular entity in order to check whether it has fuel or not.

Also, I would like to thanks author of [NightLantern plugin](http://oxidemod.org/plugins/nightlantern.1182/) for an inspiration. Although my plugin is a bit different from it and probably development of **Epic Lanterns** might head in other direction (check **FAQ**), it was good to see how toggling lanterns work "under the hood". I will develop this plugin only if I have free time to spare - keep in mind it's released under zlib/png license, so you can modify it as you wish.

**EDIT: I recently have so shitty amount of FPS in RUST that I'm pretty sure that I'm not going to develop this project for the game that still have retarded optimizations problems for so long time. Yet, I've hope my code may become handy for somebody else.**