This plugin helps you handle your bans. It gives you the possibility to ban per name, SteamID or per IP and considers all three of them when someone banned trying to join the server. So even if someone uses another steam account his IP will still be blocked.

When a banned IP / Steam connects with a new Steam / IP this steam/IP will be added to the banlist!

You may use **[Player Database](http://oxidemod.org/plugins/player-database.1496/) to easily **ban offline players
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
**Permissions:**

The config offers options to set different permissions for every command so you can control which user can use which commands.

Default permissions are:

ban and unban - "enhancedbansystem.ban"

kick - "enhancedbansystem.kick"

bancheck - "enhancedbansystem.bancheck"