Store player IPs, Names, position, played time, etc

**Chat commands:**

/lastips STEAMID/NAME => show the last ips used by this player

/ipowners XX.XX.XX.XX => show players that had the same ip

/lastnames STEAMID/NAME => show the names that this user had

/lastseen STEAMID/NAME => show when the player was last seen

/played STEAMID/NAME => show how much time a player has played on the server

/firstconnection STEAMID/NAME => show when the player was first seen on the server

/lastposition STEAMID/NAME => show where the player was last seen (doesn't work on online players)

**Default Configs:**

````
{

  "First Connection - activated": true,

  "First Connection - allow players": true,

  "First Connection - Permission - authlevel": 1,

  "IP Logs - activated": true,

  "IP Logs - allow players": false,

  "IP Logs - Max Logs per player": 5,

  "IP Logs - Permission - authlevel": 1,

  "Last Position - activated": true,

  "Last Position - allow players": false,

  "Last Position - Permission - authlevel": 1,

  "Last Seen - activated": true,

  "Last Seen - allow players": true,

  "Last Seen - Permission - authlevel": 1,

  "Names Logs - activated": true,

  "Names Logs - allow players": true,

  "Names Logs - Max Logs per player": 5,

  "Names Logs - Permission - authlevel": 1,

  "Time Played - activated": true,

  "Time Played - allow players": true,

  "Time Played - Permission - authlevel": 0

}
````