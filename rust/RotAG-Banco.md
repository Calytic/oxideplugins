A Bank System with its own API to use with Economics from Bombardir.

* Required: [Economics](http://forum.rustoxide.com/plugins/economics.717/)

- Optional: [HelpText ](http://forum.rustoxide.com/plugins/helptext.676/)and [Updater](http://forum.rustoxide.com/plugins/updater.681/)


I know that it doesn't seems to be a necessary plugin, but I created it to use with RotAG-Robou, my robbery system.

**Console Commands**



* 
**bnc.c setbb <steamid\name> <money> **- set [money] balance for [steamid/name] player bank account *****
* 
****bnc.c deposit** <steamid\name> <money> **- deposit [money] to [steamid/name] player bank account *
* 
****bnc.c withdraw** <steamid\name> <money>** - withdraw [money] from [steamid/name] player bank account *
* 
****bnc.c save** **- saves plugin data *****
* 
****bnc.c **balance <steamid\name> **- check [steamid/name] player bank account balance *****


**Chat Commands**



* 
**/bb **- check your bank account balance
* 
**/tb <name> <money> **- transfer [money] to [name] player bank account
* 
**/bb <name>** - check [name] player bank account balance (only admins)*****
* 
**/deposit <name> <money> **- withdraws money from your wallet (from Economics plugin) and deposit [money] to [name] player bank account  (you be your own name)
* 
**/withdraw <name> <money> **- withdraw [money] from your bank account to your wallet (from Economics plugin)
* 
**/setmoney <name> <money> **- set [money] balance for [name] player bank account  *

Basic Config to be changed as you wish after the file is created in the first plugin load.

````

    self.Config.Start = 0

    self.Config.Limit = 10000000

    self.Config.DepositFee = 5

    self.Config.WithdrawFee = 5

    self.Config.AllowTrans = 1
````


***** These commands only work for admins.

-- The plugin autogenerates both, the data and the config file.

-- Its possible to work with offline players if you use their Steam ID with the console commands.

**Credits to Bombardir, since I fully used its plugin as a example.**


Consider donating if you think that I deserve it [](https://www.paypal.com/cgi-bin/webscr?cmd=_donations&business=NMSAMT36VTNTS&lc=GB&item_name=TheRotAG&currency_code=USD&bn=PP%2dDonationsBF%3abtn_donateCC_LG%2egif%3aNonHosted)