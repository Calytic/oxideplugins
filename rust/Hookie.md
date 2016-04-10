**What is hookie?**

Hookie is a useful commands plugin as well as an API for other plugins to call.


Small useful features added regularly hopefully.


Will write more later.

**Internals:**

    [1] IsPlayerInArea(player, MinX, MinY, MaxX, MaxY); (Bool)

    [2] SetPlayerHealth(player, amount); (Void)

    [3] Slap(BasePlayer player, amount = 12); (Bool)

    [4] GivePlayerHealth(player, amount); (Void)

    [5] RemovePlayerHealth(player, amount); (Void)

    [6] Explode(BasePlayer player, damage = 60, times = 1); (Void)

    [7] IsPlayerInWater(player); (Bool)

    [8] HealAll(); (Void)

    [9] MoveEveryPlayerToPlayer(player); (Void)

    [10] GetGroundPosition(sourcepos); (Vecto3)

    [11] FindPlayer(stringtofind); (BasePlayer); [Reneb]

    [12] FindPlayerByID(id); (BasePlayer); [Reneb]

**Commands:**

        Slap, Explode, Heal, HealAll, TpAll
**

Default Configuration:**

````

{

  "Admin": {

    "MaxLevel": 2,

    "MinLevel": 1

  },

  "Commands": {

    "Slap": true

  },

  "General": {

    "Commands": true

  },

  "Messages": {

    "Explode": "<color=yellow>INFO:</color> You have exploded <color=red>{name}</color>!",

    "Exploded": "<color=yellow>INFO:</color> You have been exploded by <color=red>{name}</color>!",

    "Heal": "<color=yellow>INFO:</color> You have healed <color=green>{name}</color>!",

    "Healed": "<color=yellow>INFO:</color> You have been healed by <color=green>{name}</color>!",

    "MultiplePlayers": "<color=yellow>ERROR:</color> Multiple players found.",

    "NoAuth": "<color=yellow>ERROR:</color> You don't have access to this command.",

    "NoPlayersFound": "<color=yellow>ERROR:</color> No players found.",

    "Slap": "<color=yellow>INFO:</color> You have slapped <color=red>{name}</color>!",

    "Slapped": "<color=yellow>INFO:</color> You have been slapped by <color=red>{name}</color>!"

  }

}

 
````