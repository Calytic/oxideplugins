**Whitelist**  restricts access to your server, only allowing players who have the 'whitelist.allowed' permission from Oxide.

**Permissions** 

This plugin uses Oxide's permission system. To assign a permission, use **grant user <username|steamid> <permission>** . To remove a permission, use **revoke user <username|steamid> <permission>** .


* 
**whitelist.allowed**  (allows player to connect to the server)
**Ex.**  grant user Wulf whitelist.allowed
**Ex.**  revoke user Wulf whitelist.allowed
**Ex.**  grant group admin whitelist.allowed



**Localization** 

There is currently no way to change the message kicked players see. I may have to change to a different hook to allow custom messages.