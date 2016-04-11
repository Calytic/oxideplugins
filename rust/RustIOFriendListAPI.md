This plugin allows players to add/remove friends using Rust:IO.

Please note **this mod is not an official Rust:IO mod**!

**Description:**

This mod was designed to allow player to control there Rust:IO friends list via in game chat commands so players did not have to use the website, This is useful for people who don't play in windowed mode or don't have two screens.
**Reason:**

So for me the reason I made this mod is not to long ago there where two friendly fire mods, one was a lua and used the friend api and the other a C# for Rust:IOs official friendly fire mod. Because they where different types they didnt interfere, then the lua version got rewriten into C# meaning you could only have one, But other mods I used required FriendAPI. This caused a few issues as players had to manage two friends list now as friendly fire was only through Rust:IO so clans where automatic.
**Solution:**

Make a mod that called the available hooks for Rust:IO to allow players to add,remove and check people as friends
**Compatibility For non supported mods:**

If you have a mod that supports another friends mod but want to have compatibility with Rust:IO to consolidate check the FAQ page for details.
**Commands:**


* /friend add/+ name
* /friend remove/- name
* /friend check/? name


**Developers:**

I have designed this mod to be used with other mods, this saves your plugin a decent amount of space, this is what you can use below.

I will need to check the input when I return home, at the moment it accepts a string so I believe its passing the userIDstring to Rust:IO. (I am not at home to confirm)

Code (C#):
````
        [PluginReference]

        Plugin RustIOFriendListAPI;

Ulong

        ANDFriends( player, player2);

        ORFriends( player, player2);

String Variation

        ANDFriendsS( player, player2);

        ORFriendsS( player, player2);

 
````


**Functions:**

ANDFriends: Only returns true if BOTH players are friends

ORFriends: Returns true if at least one person is a friend of the other.

**Planned:**

THEYFriends: only true if person2 has added person1

YOUFriends: only true if person1 has added person2