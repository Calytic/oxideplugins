Suicide chat command

Admin kill chat command
**Commands:**

- /kill PlayerName/ID/IP => kill a player by name (partial name works), ID or IP

- /suicide => commit suicide

- /die => same as both above.
**Configs:**

````
{

  "Messages": {

    "MultiplePlayersFound": "Multiple Players Found",

    "PlayerWasKilled": "{username} was killed",

    "PlayerDoesntExist": "{username} doesn't exist",

    "NotAllowed": "You are not allowed to use this command on someone else"

  },

  "KillForModerators": true

}
````

KillForModerators => if set to false, only admins (level2) will be allowed to kill players.