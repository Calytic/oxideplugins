**PLEASE CONSIDER DONATING TO SUPPORT DEVELOPMENT**

Checks votes on [Home - Top Game Servers](http://game-servers.top) & hurtworld-servers.net and rewards users for their votes. The more they vote the more they receive.

IF YOU ARE UPDATING FROM A PRE VERSION 2, please delete your config file and recreate it via chat commands.

**To setup your api key use**

/rewardconf api <tgs/listforge> youServerApiKey

**To setup your ListForge serverID use**

/rewardconf serverid yourServerID

**To setup your reward interval use**

/rewardconf tracking <period> <week/month/year>

- Example: using /rewardconf tracking 1 week = This will only count the votes on a weekly basis, so max amount of votes would be 7


Please also set your server id found here -> [http://hurtworld-servers.net/server/**4607**/](http://hurtworld-servers.net/server/4607/)

This will be used to automatically show the voting url to users.

**To add new rewards use**

/addreward itemid rewardamount votecountneeded

- if votecountneeded is set to -1 it will give that reward for each vote

- If you need to get the itemid, type itemlist in the console.

- To reward money instead of items, use "**money**" as itemid

**To clear your rewards file and defaults use**

This is recommended for your first setup, otherwise the default rewards will be given

/clearrewards

**User commands**

/getreward - Get your unclaimed reward (Automatically given when connected)

/rewards - View all rewards