**Version 0.2.0 is out! More commands, permissions, etc...


Presentation:**

Authentication is a plugin designed with private servers in mind. It makes it possible to setup one without having to worry about Steam Groups nor SteamIDs. Just choose a password of your preference and you're ready to go.

**Commands:

/auth [password]**

Usage: Authenticates players;
**/auth help**

Usage: Shows in-game commands;
**/auth password**

Usage: Shows current password;
**/auth password [new password]**

Usage: Sets new password;

**Permissions:
auth.edit**
Usage: Allows usage of **/auth help** and **/auth password**;
**
How it works:**

After wake up, players will be requested to enter the /auth command along with the password the server owner should have shared with them.

There's a limited time to enter the command after the wake up, if players fail to authenticate before the time runs they're kicked from the server.

**Installation and Setup:**


* Place Authenticator.cs in the plugins folder.
* If/When your server is open, open Authenticator.json in the config folder.
* Replace "changeme" with a password of your choice. Remember to always surround it with quotes, even if the value is numeric.
* Restart the server.


**Config:**
Variable: "ALREADY_AUTHED"

Usage: The message that will be displayed if player try to authenticate twice;


Variable: "AUTHENTICATION_SUCCESSFUL"

Usage: The message that will be displayed AFTER authentication;

Variable: "AUTHENTICATION_TIMED_OUT"

Usage: The error message that appears on the kicked screen;

Variable: "HELP"
Usage: Help message displayed in some situations;


Variable: "INCORRECT_PASSWORD"

Usage: The message that will be displayed if wrong password is used;

Variable: "INVALID_COMMAND
Usage: The message that will be displayed if command is not recognized;


Variable: "PASSWORD"

Usage: The argument of the /auth command;


Variable: "PASSWORD_REQUEST"

Usage: The message that will be displayed BEFORE authentication. {TIMEOUT} is automatically with the TIMEOUT variable below.


Variable: "SYNTAX_ERROR"

Usage: The message that will be displayed if invalid password is used (e.g blank);


Variable: "TIMEOUT"

Usage: Maximum amount of time that players have to authenticate before being kicked.

**Default Config:**

````
{

  "ALREADY_AUTHED": "You're already authed.",

  "AUTHENTICATION_SUCCESSFUL": "Authentication sucessful.",

  "AUTHENTICATION_TIMED_OUT": "You took too long to authenticate",

  "HELP": "Type /help for all available commands.",

  "INCORRECT_PASSWORD": "Incorrect password. Please try again.",

  "INVALID_COMMAND": "Invalid command or you must be authed to do this.",

  "PASSWORD": "changeme",

  "PASSWORD_REQUEST": "Type /auth [password] in the following {TIMEOUT} seconds to authenticate or you'll be kicked.",

  "SYNTAX_ERROR": "Correct syntax: /auth [password]",

  "TIMEOUT": 30

}
````


**That's it for now, hope you like it!**