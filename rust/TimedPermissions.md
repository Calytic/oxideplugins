**Timed Permissions **allows you to grant permissions for a specific time.


The <time> argument should tell for how long the player should have the permission. Ex: **/grantperm LaserHydra betterchat.vip 30d **gives LaserHydra the permission 'betterchat.vip' for 30 days.


* d = days
* h = hours
* m = minutes


**Chat Commands (permission: timedpermissions.use):**


* 
**/grantperm <player|steamid> <permission> <time> **give a player a permission for a specific time
* 
**/addgroup <player|steamid> <group> <time> **add a player to a group for a specific time


**Console Commands **(permission: timedpermissions.use)**:**


* 
**grantperm <player|steamid> <permission> <time> **give a player a permission for a specific time
* 
**addgroup <player|steamid> <group> <time> **add a player to a group for a specific time


**Permissions:**


* timedpermissions.use


**Language file:**

````
{

  "No Permission": "You don't have permission to use this command.",

  "Invalid Time Format": "Invalid Time Format: Ex: 1d12h30m | d = days, h = hours, m = minutes"
}
````