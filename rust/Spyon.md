This plugin logs console and chat command usage for both ingame users and RCON, which is useful for controlling admin abuse.


The command usage is logged to oxide/logs/cmds.txt.


To enter a reason into the log to explain why a set of commands was used, use the chat command /reason <reason message>. Alternatively the console command spyon.reason <reason message> can be used as well.

**Configuration**

Spyon.json contains the configuration for this plugin.


* authlevel: Sets the authlevel of the users to log commands of. 0 means everyone, 1 means mods and admins, 2 means only admins. The authlevel also sets which users are able to use reason commands.

* public: Sets whether to report the usage of commands to the users with the authlevel specified in exposure.
* exposure: Sets the authlevel of users that are able to see that a command was used when public is enabled. 0 means everyone is able to see used commands when public is enabled, 1 means mods and admins are able to see used commands when public is enabled, 2 means only admins are able to see used commands when public is enabled.

* logdaily: Sets whether to create seperate logs each day instead of creating a single log file containing all commands (useful for people that want to use this plugin to log player commands as well).

* cmds: Sets which console commands to log the usage of. chat.say cannot be logged for ingame users as the command is filtered to identify chat commands.

* chatcmds: Sets which chat commands to log the usage of. Each entry should start with a "/".  Matching commands with subcommands is possible as well: "/foo bar" will match "/foo bar test" and "/foo bar test test2" but not "/foo baz test". This is useful when wanting to match commands like "/remove admin", but not "/remove". Partial subcommands are not matched: "/foo ba" will not match "/foo bar".


**Advice (Optional!)**

If you have root control over your server, set up a webserver and serve cmds.txt. You'll have most of the transparency of "public" while not ruining the immersion of the game with chat spam about used commands!