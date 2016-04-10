**This is an addon created for Rust:IO. In order to use this plugin, you need to install Rust:IO first!**


* **[Get Rust:IO!](http://get.playrust.io)**

Once loaded, friendly fire will be turned off for a player's explicitly added friends.


[ [Servers using FriendlyFire](http://playrust.io/#tags:friendlyfire) ]


Please keep in mind that this is a one-way relationship, so both players have to add each other as friends to enable it mutually (just like also sharing positions works).

**Chat commands**


* 
**/ff** - Displays your current friendly fire status
* 
**/ff on|off** - Toggles friendly fire **on** or **off (default)**


**Translating**


There is a config file located at config/FriendlyFire.json which contains all translatable strings. Simply edit the right hand side of the translations, but always keep the %PLACEHOLDERS% intact and untranslated.

**Plugin API** (i.e. for use with arena plugins)


* 
**EnableBypass(playerId:ulong|string):bool**

Bypasses FriendlyFire for the specified player (player will hurt everyone including his friends)


* 
**DisableBypass(playerId:ulong|string):bool**

Disables the former (falls back to default behavior)


````
{

  "messages": {

    "%NAME% is your friend and cannot be hurt. To disable this, unshare your location with %NAME% on the live map or type: <color=\"#ffd479\">/ff on</color>": "%NAME% is your friend and cannot be hurt. To disable this, unshare your location with %NAME% on the live map or type: <color=\"#ffd479\">/ff on</color>",

    "<color=\"#ffd479\">/ff on|off</color> - Toggles friendly fire <color=#cd422b>on</color> or <color=#8acd2b>off</color>": "<color=\"#ffd479\">/ff on|off</color> - Toggles friendly fire <color=#cd422b>on</color> or <color=#8acd2b>off</color>",

    "<color=\"#ffd479\">/ff</color> - Displays your friendly fire status": "<color=\"#ffd479\">/ff</color> - Displays your friendly fire status",

    "Friendly fire for your friends is already <color=#8acd2b>disabled</color>. They are safe!": "Friendly fire for your friends is already <color=#8acd2b>disabled</color>. They are safe!",

    "Friendly fire for your friends is already <color=#cd422b>enabled</color>. Take care!": "Friendly fire for your friends is already <color=#cd422b>enabled</color>. Take care!",

    "Friendly fire is <color=#8acd2b>disabled</color> for your friends:": "Friendly fire is <color=#8acd2b>disabled</color> for your friends:",

    "Friendly fire is <color=#cd422b>enabled</color> for your friends:": "Friendly fire is <color=#cd422b>enabled</color> for your friends:",

    "To toggle friendly fire on or off, type: <color=\"#ffd479\">/ff on|off</color>": "To toggle friendly fire on or off, type: <color=\"#ffd479\">/ff on|off</color>",

    "Usage: <color=\"#ffd479\">/ff [on|off]</color>": "Usage: <color=\"#ffd479\">/ff [on|off]</color>",

    "You do not have any friends currently.": "You do not have any friends currently.",

    "You have <color=#8acd2b>disabled</color> friendly fire for your friends. They are safe!": "You have <color=#8acd2b>disabled</color> friendly fire for your friends. They are safe!",

    "You have <color=#cd422b>enabled</color> friendly fire for your friends. Take care!": "You have <color=#cd422b>enabled</color> friendly fire for your friends. Take care!",

    "You may add or delete friends using the live map.": "You may add or delete friends using the live map."

  }

}
````