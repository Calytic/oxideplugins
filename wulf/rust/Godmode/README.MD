This plugin allows players with the correct permissions to be invulnerable to damage.

**Required Permissions** 

For players to be able to use the commands that this plugin offers the permission **godmode.allowed** is required.

**Default config** 

	Code ( (Unknown Language)):
	
````
{

  "Messages": {

    "Disabled": "You have disabled godmode.",

    "DisabledPlayer": "Your godmode has been disabled by {0}.",

    "DisablePlayer": "You have disabled godmode for {0}.",

    "Enabled": "You have enabled godmode.",

    "EnabledPlayer": "Your godmode has been enabled by {0}.",

    "EnablePlayer": "You have enabled godmode for {0}.",

    "GodList": "The following players currently have godmode enabled:",

    "InformAttacker": "{0} is currently in godmode and can't take any damage.",

    "InformVictim": "{0} just tried to deal damage to you.",

    "InvalidPlayerSelected": "Invalid number, use the number in front of the player's name. Use /viewinv list to check the list again",

    "MultiplePlayersFound": "Multiple players were found with that name, select one of these players by using /god list <number>:",

    "NoGodLoot": "You are not allowed to loot a player that is in Godmode.",

    "NoListAvailable": "You don't have a player list available, use '/god <name>' instead.",

    "NotAllowed": "You are not allowed to use this command.",

    "PlayerNotFound": "No players were found with that name."

  },

  "Options": {

    "GodDamageEnabled": false,

    "InformPlayersOnAttack": true,

    "PlayerPrefixEnabled": true

  },

  "Settings": {

    "ChatPrefix": "God",

    "ChatPrefixColor": "008800",

    "PlayerPrefix": "[God]"

  }

}

 
````