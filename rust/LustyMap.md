LustyMap is a little Map & Minimap plugin developed for our rust server, [Lusty Rust](http://lustyrust.co.uk/)

**Requires the map image to be hosted on an external site somewhere, if you are using rustio then you can point to that url (details below)**


The map has two modes, the simple mode is fairly easy to setup, the complex mode has a number of steps to get it working.

**Simple Mode**

Install the plugin, then from in game type **/map url <url to your map>**

e.g. /map url [http://185.38.148.8:28025/map.jpg](http://185.38.148.8:28025/map.jpg)


If you are using rustio you can just change the IP & PORT of the above link, alternatively I recommend using **[Generate Map | PlayRust HQ - Rust News, Server List, Map Generator & Map Gallery](http://www.playrusthq.com/generate-map)** to generate a map for your seed and then host the image on your webserver.


That’s it for the simple mode, players will now have a simple minimap in the top left and can type **/map** in game for a bigger version.

**Complex Mode**

The complex mode allows for a zoomed in version of the minimap, but requires a fair bit of setup. (the steps for the simple mode are required as well).


First take your map image and split it up into 32x32 grid. I recommend using [**http://imagesplitter.net/**](http://imagesplitter.net/)

Once you have your image split up (should be 1024 files) you need to rename them so they match the following fotmat: map-x-z.jpeg

e.g. map-0-0.jpeg

map-0-1.jpeg

map-0-2.jpeg

…

map-31-31.jpeg


I used the following powershell command to rename the images created by imagesplitter.net (my starting image was name map.jpeg)


````
Dir | Rename-Item –NewName { $_.name –replace “ \[www.imagesplitter.net\]“,”” }
````

Once you have your images with the correct name you need to upload them to your hosting.


You can now set the complex url in game: **/map complex <url to split map parts (leave off the filename)>**

e.g. /map complex [http://lustyrust.co.uk/img/lustyplugins/map/](http://lustyrust.co.uk/img/lustyplugins/map/)

**Note:** Some server hosts seem to block images from loading when specifying the full url, if your minimap is just bank then try leaving off http:// in the url for complex mode

e.g. /map complex [lustyrust.co.uk/img/lustyplugins/map/](http://lustyrust.co.uk/img/lustyplugins/map/)


The last step is to Enable complex mode: **/map mode true
**

You should now have a zoomed in version of the minimap setup

***NEW* Keybinds**

Players can press M to open the in game map (pressing M again will close it).
****

Commands****

/map – Available to all players, displays the in game map

players can hide the minimap using the <<< button located at the top right of the minimap (then >>> to open it back up)

**Admin Commands**

/map url <url to full sized map image> - Sets the url for the map

/map mode <true|false> - Enables or Disables complex mode (default is disabled)

/map minimap <true|false|left|right> - Enables, disables or sets the default alignment for the minimap. (default is enabled, left)

/map startopen <true|false> - Sets if the minimap starts in open or collapsed mode when a player joins the server (default is open)

/map compass <true|false> - Enables or Disables the minimap compass (heading and position text, default is enabled)

/map complex <url to folder with split map images) – Sets the url for the split map

**Pictures**

In game map

Simple minimap

Complex minimap
****