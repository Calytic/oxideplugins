**Description**

This plugin helps you handle your bans. It gives you the possebility to ban per name, steamID or per IP and considers all three of them when someone banned trying to join the server. So even if someone uses another steam account his IP will still be blocked.

**NEW:**

Now when a banned IP / Steam connects with a new Steam / IP this steam/IP will be added to the banlist!

**Usage**

Chat Commands:
**/ban <name/steamID/IP> <reason optional> <time in seconds optional>**

If no time is given the ban is permanently.

Examples:
/ban Domestos noob - will ban Domestos permanently

/ban Domestos noob 1200 - will ban Domestos for 20 minutes

/ban Domestos noob 10800 - will ban Domestos for 3 hours

/ban Domestos noob 864000 - will ban Domestos for 10 days

**/unban <name/steamID/IP>** - obviously
**/kick <name/steamID/IP> <reason> **- does what it says
**/checkban <name/steamid/ip>**

Console Commands:

Syntax is exactly the same like the chat commands!
**player.ban <name/steamID/IP> <reason> <time[m|h|d] optional>

player.unban <name/steamID/IP>

player.kick <name/steamID/IP>

player.banlist

player.checkban <name/steamid/ip>
**
**Permissions:**

The config offers options to set different permissions for every command so you can control which user can use which commands.

Default permissions are:

ban and unban - "enhancedbansystem.ban"

kick - "enhancedbansystem.kick"

bancheck - "enhancedbansystem.bancheck"

**IMPORT BANS FROM LAST EBS LIST:**

In the SERVER console: ebs.import


Configs:

````

{

  "Ban - Broadcast Chat": true,

  "Ban - Default Ban Reason": "Hacking",

  "Ban - Message - Broadcast": "An admin banned {0} from the server for {1}",

  "Ban - Message - Deny Connection - Permanent": "You are banned on this server",

  "Ban - Message - Deny Connection - Temp": "You are temp-banned on this server",

  "Ban - Message - Player": "An admin banned you for {0}",

  "Ban - permission": "enhancedbansystem.ban",

  "Kick - Broadcast Chat": true,

  "Kick - Message - Broadcast": "{0} was kicked from the server for {1}",

  "Kick - Message - Player": "An admin kicked you for {0}",

  "Kick - permission": "enhancedbansystem.kick",

  "Setting - Chat Name": "<color=orange>SERVER</color>",

  "Setting - Message - No Player Found": "No player found",

  "Unban - Broadcast Chat": true

}

 
````


TO BE MADE:

- some code improvement