**PLEASE CONSIDER DONATING TO SUPPORT DEVELOPMENT**


This is a very beta release of the banking system. This will add the function to bank your cash (Economy) into banks for safe guarding and earn interest on your money. While carrying cash you will lose it (if enabled) to your killer.


If the economy plugin is installed, we will use that system as "Cash on hand", if it isn't installed we will use an inbuilt cash system.


Earning cash on killing animals will also be implemented in a future version.


This plugin can't evolve without some suggestions. So all are invited.

**Commands:

/bank** - This will give you all avail commands
**/bank balance** - This shows your wallet balance and account balance.
**/bank deposit <amount>** - How much you want to deposit from your cash to your bank account.
**/bank withdraw <amount>** - How much you want to withdraw from your bank account to cash.
**/bank transfer <amount> <player>** - How much you want to withdraw from your bank account to cash.
**/bank top players **- See top 5 richest players
**
Clan commands are:

/bank withdraw** <amount> clan
**/bank deposit** <amount> clan

**Setup Commands:

/bank setup interest rate <interest>** - Where <interest> is the digit of %, current is 6
**/bank setup interest interval <interval>** - Where <interval> is the digit of minutes, current is every 30
**/bank setup deathdrop <true/false>** - If set to true (default), the users cash will drop at death and be rewarded to the murderer.
**/bank setup playtime <interval> <cash>** - Gives players cash every interval minutes
**/bank setup fee <withdrawel/deposit/transfer> <fee>** - Sets the fees for the relevant transactions, fee will be a percentage of the amount
**/bank setup stakebanking <true/false> **- default false: if set to true user can only bank when near their stake.
**/bank setup clanbanking <true/false> **- default true: if set to true clans will have bank accounts. Needs HW CLans
**/vault** - Checks the vaults balance.
**/wipe all [backup] **- backup is optional, if supplied it will attempt to backup the accounts before clearing data.

**FOR DEVELOPERS**

Basic example of using EconomyBanks withing your plugin.

````
using Oxide.Core.Plugins;


namespace Oxide.Plugins

{

    [Info("Example", "Pho3niX90", "1.0.0", ResourceId = 0)]

    class Example: HurtworldPlugin

    {

[PluginReference]

        Plugin EconomyBanks;


void Loaded()

{

    if (EconomyBanks != null)

    {

        MoneySym = (string)EconomyBanks.Call("GetMsg", "moneySymbol");

        Puts("EcconomyBanks has now loaded, and "+this.Title+" will now function");

    }

}



        double CashBalance(PlayerSession player)

        {

            return double.Parse(EconomyBanks.Call("Wallet", player).ToString());

        }

        double AccountBalance(PlayerSession player)

        {

            return double.Parse(EconomyBanks.Call("Balance", player).ToString());

        }

        void AddCash(PlayerSession player, double Amount)

        {

            EconomyBanks.Call("AddCash", player, Amount);

        }

        void RemoveCash(PlayerSession player, double Amount)

        {

            EconomyBanks.Call("RemoveCash", player, Amount);

        }

    }

}

 
````


**SPECIAL THANKS**

@[kpl35m](http://oxidemod.org/members/kpl35mm-nl.93043/)m-NL

For helping to beta test the plugin. You assistance is much appreciated.