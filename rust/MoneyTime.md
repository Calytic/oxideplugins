**MoneyTime** pays players money via Economics for playing on your server.  It is useful to encourage playtime, and give something extra to your players for playing! The default payout is $10 every 600 seconds (10 minutes). You can also configure a time alive bonus (default 2x time alive) and welcome bonus (default $500) in the configuration.

**Configuration**

You can configure the settings and messages in the MoneyTime.json file under the server/<identity>/oxide/config directory.

**Default Configuration**

````
{

  "BasePayout": 10.0,

  "PayoutInterval": 600,

  "ReceivedForPlaying": "You've received ${amount} for actively playing!",

  "ReceivedForTimeAlive": "You've received ${amount} for staying alive for {time}!",

  "ReceivedWelcomeBonus": "You've received ${amount} as a welcome bonus!",

  "TimeAliveBonus": true,

  "TimeAliveMultiplier": 2.0,

  "WelcomeBonus": 500.0

}
````


**Credits**


* 
**Spiritwind**, for the original MoneyTime for Economics plugin in Lua.