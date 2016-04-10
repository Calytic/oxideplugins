What is **Start Protection**?

Start Protection is a simple plugin that gives new users a certain amount of time to play without stressing about being killed by other players, it also prevents them from attacking other players. Players can still be killed by the environment, animals and self-inflicted injuries. Only PVP is disabled.


Why is this **useful**?

Rust is full of griefers, I often read server logs and see several players rage-quit after only playing a few minutes after being gunned down by other players. This gives them a chance to move around a little and set up shop.


Default Configuration:

````

{

  "bHelicopterProtection": true,

  "bProtectionEnabled": true,

  "bSleeperProtection": true,

  "iAuthLevel": 2,

  "iInactiveDays": 0.25,

  "iPunishment": 300,

  "iTime": 1800,

  "iUpdateTimerInterval": 10

}

 
````

The default time is "1800", which equates to 30 minutes. I found this was a fair time.


Punishment time is "300" by default, which equates to 5 minutes. This is the time that will be revoked if the player tries to PVP while in SP mode.

**Ingame commands:**

/sp (Will display all the sub-commands)

**Bugs:**

None yet, but I'm sure we'll find some.