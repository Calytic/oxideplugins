**This is an API plugin that doesnt do anything alone besides managing an ignore list. To make use of that youo need another plugin using it.


Usage:**

Chat commands:

/ignore <add(+)/remove(-)> <name/steamID> to add or remove someone

/ignore list to show your ignore list

**Usage for plugin devs**

To call the functions from this API your plugin needs to get the plugin instance.
Code (C#):
````
[PluginReference]
private Plugin Ignore;

 
````

Code (Lua):
````
var added = Ignore.Call("AddIgnore", playerSteamId, targetSteamId)
````


**Available functions**
Code (Lua):
````
AddIgnore(playerSteamId, targetSteamId)
````

Adds <targetSteamId> to <playerSteamId> ignore list

returns <true> if player was added, <false> if player couldnt be added

````
RemoveIgnore(playerSteamId, targetSteamId)
````

removes <targetSteamId> from <playerSteamId> ignore list

returns <true> if removed, <false> if not
Code (Lua):
````
HasIgnored(playerSteamId, targetSteamId)
````

returns <true> if <playerSteamId> has <targetSteamId> on ignore, <false> if not
Code (Lua):
````
IsIgnoredBy(playerSteamId, targetSteamId)
````

returns <true> if <playerSteamId> is ignored by <targetSteamId>, <false> if not
Code (Lua):
````
AreIgnored(playerSteamId, targetSteamId)
````

returns <true> if both have each other on their ignore list, <false> if not

````
GetIgnorelist(playerSteamId)
````

returns a table with playerSteamId's ignore list.


For lua api just append "S" to the hook names to pass steamid as string.