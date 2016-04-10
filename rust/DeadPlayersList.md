This plugin does not do much on its own, it is more or less an extension to Rust that stores all the players that are currently dead in a list so that other plugins can obtain some data of these players at this point.


Basically Rust has two lists available:

- BasePlayer.activePlayerList: This list keeps track of all the players that are currently in the game playing.

- BasePlayer.sleepingPlayerList: This list keeps track of all the players that are currently offline but still have the character in the world sleeping.


So once a player goes offline and starts sleeping he or she is moved from the activePlayerList to the sleepingPlayerList but when a sleeping player is killed (and you know it will happen) the player is also removed from the sleepingPlayerList leaving no way of grabbing info of that player.


This plugin was originally written by @Reneb.


When updating from a version prior to 2.x.x you will need to delete the plugin file deadPlayerList.lua before installing this one.

**Available commands**

This is just a helper plugin providing API usage to other plugins and does not offer any additional commands for the players or admins on a server.

**Default Config**

This plugin does not use a configuration file, it only stores data in a data file in the data folder for other plugins to grab information from.

**Grabbing data**

There are 4 methods available that let you grab the information of a dead player.

````
Dictionary<string, string> GetPlayerList()
````

Returns a dictionary with the SteamID as key and name as value for every dead player in the list.

````
string GetPlayerName(object userID)
````

Returns the name of the player associated with the given userID.

````
string GetPlayerDeathReason(object userID)
````

Returns the reason or the killer of the player. When the player was killed by a player this returns the name of that player. If the player was killed by an animal it will return the name of the animal. If the player died by suicide or drowning it will return that reason.

````
Vector3 GetPlayerDeathPosition(object userID)
````

Returns the position where the player died.

**Examples**
Code (Lua):
````
DeadPlayersListPlugin = plugins.Find("DeadPlayersList")
if DeadPlayersListPlugin then

  list = DeadPlayersListPlugin.Call("GetPlayerList")

  print("There are " .. list.Count .. " dead players")

  listIterator = list:GetEnumerator()

  while listIterator:MoveNext() do

    print("SteamID: " .. listIterator.Current.Key .. " - Name: " .. listIterator.Current.Name)

  end

  steamid = "76561198112743227"

  name = DeadPlayersListPlugin.Call("GetPlayerName", steamid)

  reason = DeadPlayersListPlugin.Call("GetPlayerDeathReason", steamid)

  pos = DeadPlayersListPlugin.Call("GetPlayerDeathPosition", steamid)
end
````

Code (C#):
````
using ...
using ...
...
using Oxide.Core.Plugins;

namespace Oxide.Plugins {

  [Info("Example", "Mughisi", "1.0.0")]

  class Example : RustPlugin {

    [PluginReference]

    Plugin DeadPlayersList;


    ...

    void PrintPlayerDeath(ulong steamid)

    {

      Dictionary<string, string> deadsList = DeadPlayersList?.Call("GetPlayerList") as Dictionary<string, string>;

      string name = DeadPlayersList?.Call("GetPlayerName", steamid) as string;

      string reason = DeadPlayersList?.Call("GetPlayerDeathReason", steamid) as string;

      Vector3 position = (Vector3)DeadPlayersList?.Call("GetPlayerDeathPosition", steamid);

      ...

    }

  }
}
````