**PLEASE CONSIDER DONATING TO SUPPORT DEVELOPMENT**

Checks votes on [Home - Top Game Servers](http://game-servers.top) & [Reign Of Kings Server List | Reign Of Kings Multiplayer Servers](http://reign-of-kings.net) and rewards users for their votes. The more they vote the more they receive.

IF YOU ARE UPDATING FROM A PRE VERSION 2, please delete your config file and recreate it via chat commands.

**To setup your api key use**

/rewardconf api <tgs/listforge> youServerApiKey

**To setup your serverID use**

/rewardconf serverid yourServerID

**To setup your reward interval use**

/rewardconf <week/month/year> <period>

- Example: using /rewardconf week 1 = This will only count the votes on a weekly basis, so max amount of votes would be 7

**To setup autorewarding use**

/rewardconf <true/false> - If true it will automatically give rewards when a user connects.


Please also set your server id found here -> [http://reign-of-kings.net/server/](http://reign-of-kings.net/server/)**1017**/

This will be used to automatically show the voting url to users.

**To add new rewards use**

/addreward itemname rewardamount votecountneeded

- if votecountneeded is set to -1 it will give that reward for each vote

- Please add double quotes around names with spaces. EX: "steel crest"

**To clear your rewards file and defaults use**

This is recommended for your first setup, otherwise the default rewards will be given

/clearrewards

**User commands**

/getreward - Get your unclaimed reward (Automatically given when connected)

/rewards - View all rewards


Special thanks to Scorpyon for helping me with the dreaded lists