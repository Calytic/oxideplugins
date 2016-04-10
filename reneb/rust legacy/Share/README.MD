Share Database

**Commands:**
- /share PLAYER/STEAMID => Share with another player (target needs to be connected)
- /unshare PLAYER/STEAMID => Unshare with target player (target doesn't need to be connected)

- /share => list of all players you share with


This plugin is useless alone, but will be usefull for door share, remove share, etc.

**Plugin developpers:**


Call this plugin with:

````
bool isSharing(string sourceID, string targetID)

 
````

exemple:

````
bool shouldopen = Share?.Call("isSharing", sourceDoorOwnerID, doorOpenerID);
````