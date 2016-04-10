**Description**

This plugin doesnt do anything itself besides managing a friendlist.

It offers functions to use by other plugins to do things based on players friends.

**Usage for players**

/friend <add|+/remove|-> <name/steamID> to add or remove someone

/friend list to list your friends

**Usage for plugin devs**

To call the functions from this API your plugin needs to get the plugin instance.
Code (C#):
````
[PluginReference]
private Plugin Friends;


Friends?.CallHook("HasFriend", playerId, targetId)

 
````


**Available functions**
Code (C#):
````
bool AddFriend(ulong playerId, ulong friendId)
bool RemoveFriend(ulong playerId, ulong friendId)
bool HasFriend(ulong playerId, ulong friendId)
bool AreFriends(ulong playerId, ulong friendId)
bool IsFriend(ulong playerId, ulong friendId)
string[] GetFriendList(ulong playerId)
ulong[] IsFriendOf(ulong playerId)
````

Code (Lua):
````
bool AddFriendS(string playerS, string friendS)

bool RemoveFriendS(string playerS, string friendS)

bool HasFriendS(string playerS, string friendS)

bool AreFriendsS(string playerS, string friendS)

bool IsFriendS(string playerS, string friendS)
string[] GetFriendListS(string playerS)
string[] IsFriendOfS(string playerS)
````