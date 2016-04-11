This is economy system for your server.

* Optional: [HelpText](http://forum.rustoxide.com/resources/helptext.676/), [Updater](http://forum.rustoxide.com/resources/updater.681/).
Console Commands


* eco.c setmoney <steamid\name> <money> - set [money] balance for [steamid/name] player *
* eco.c deposit <steamid\name> <money> - deposit [money] to [steamid/name] player *

* eco.c withdraw <steamid\name> <money> - withdraw [money] from [steamid/name] player *

* eco.c save - saves plugin data *
* eco.c balance <steamid\name> - check [steamid/name] player balance *

Chat Commands


* /balance - check your balance
* /transfer <name> <money> - transfer [money] to [name] player



* /balance <name> - check [name] player balance *
* /deposit <name> <money> - deposit [money] to [name] player *
* /withdraw <name> <money> - withdraw [money] from [name] player *

* /setmoney <name> <money> - set [money] balance for [name] player *

P.S. All chat command can be changed\deleted from config!

P.P.S You can also work with Offline players, just use console commands and steamid.

* These commands require a level of administration specified in the config.
Updating to v2

- remove old 00-economics.lua

- backup data/Economics.json

- add Economics.cs
Config

"CleanBase": true, -- Base cleaner. When you restart the server, it will delete players from database whose capital is equal to the start!

    "Transfer": "transfer", -- Chat commands, u can change their.

    "Deposit": "deposit", -- To remove a command, leave it "" (for example "Transfer": "")


  "Admin_Auth_LvL": 2, -- The level of administration that require some cmds. (2 - admin, 1 - moder, 0 - user)

  "StartMoney": 1000 -- Starting capital

  "Transfer_Fee":0.01 -- Fee for transfering money (0.01 -> 1%)

API (for developers)

````
void Set(ulong playerId, double money)

void Deposit(ulong playerId, double money)

bool Withdraw(ulong playerId, double money)

bool Transfer(ulong playerId, ulong targetId, double money)

double GetPlayerMoney(ulong playerId)

var money = Economics.CallHook("GetPlayerMoney", userId);
````


````
void SetS(string playerIdS, double money)

DepositS(string playerIdS, double money)

WithdrawS(string playerIdS, double money)

TransferS(string playerIdS, string targetIdS, double money)

double GetPlayerMoneyS(string playerIdS)

local economics = plugins.Find("Economics")

local money = economics:CallHook("GetPlayerMoney", userId)

 
````