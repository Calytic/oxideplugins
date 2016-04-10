This plugin is a gamemode/economy controller essentially. Those that have played my Exodus server in the past will be familiar with it.


I wasn't going to release this and it was written for personal use so I'm not going to go to in depth with help, please read the script if you're stuck.



**INFO**: This was coded to work as an economy for my rewrite of GUIShop among other things, you can get it [**here**](http://oxidemod.org/threads/exodus.13824/#post-155615).

**Commands:**

/p - Player info (/p qmsg to see gather queue)

/bank

/givemoney

**To-do:**

More player commands, robbing etc.

**Default Configuration:**

````
{

  "Bank": {

    "InterestRate": 2

  },

  "Defaults": {

    "Balance": 2500,

    "Wallet": 150

  },

  "Dependencies": {

    "PopupNotifications": true

  },

  "Gather": {

    "DistanceMultiplier": 5,

    "MaxResourceQueue": 25000,

    "MoneyDivide": 20,

    "MoneyGather": true,

    "MoneyRate": 2

  },

  "General": {

    "MessagesEnabled": true,

    "Protocol": 1336

  },

  "Rewards": {

    "Bear": 350,

    "Boar": 325,

    "Chicken": 75,

    "Horse": 150,

    "Stag": 125,

    "Wolf": 250

  },

  "Teleport": {

    "Wait": 3

  }

}
````


**Hooks (API):**

GetPlayerMoney(BasePlayer player, bool type = true)

PlayerDeposit(BasePlayer player, int amount, bool type = true)

PayDay()

GivePlayerMoney(BasePlayer player, int amount, bool onUser = true, bool message = true)

PlayerWithdraw(BasePlayer player, int amount, bool type = true)

**Recommended Setup:**

Exodus, HumanNPC, GUIShop, Kits, MagicTeleportation, ZLevels Rewrite

**Credits:**

LaserHydra (GUI thingy)

Whoever else I have used snippets from