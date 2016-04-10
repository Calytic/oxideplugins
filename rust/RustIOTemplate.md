This is a template to help you get started to **develop plugins using the Rust:IO API**. This plugin is not intended to be used directly, but is here as a reference source for how to properly work with Rust:IO.


* [**Get Rust:IO!**](http://get.playrust.io)

Once your plugin is loaded and the server has been initialized, the Rust:IO API will be fully set up for you to use. From within your plugin you may then use the following methods:


* 
**IsInstalled**():bool

Returns if the extension is currently installed (i.e. not loading or updating). Always test this as the other methods will always return false if not installed.
* 
**HasFriend**(**playerSteamId**:string, **friendSteamId**:string):bool

Returns if a player has a specific friend (player shares location with friend)
* 
**AddFriend**(**playerSteamId**:string,**friendSteamId**:string):bool

Adds a friend and returns true if actually added
* 
**DeleteFriend**(**playerSteamId**:string,**friendSteamId**:string):bool

Deletes a friend and returns true if actually deleted