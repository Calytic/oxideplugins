**Description**

Allows players to share all doors they own with other players.

**Notes**


* When players place a key or code lock on doors, they become the "owner" of that door regardless what the code is, if any. Meaning, if the code is changed by anyone else, the original player that placed the lock will still have access. To prevent this, the lock must be removed and a new one placed by another player.
* Setting "ShowAccessMessage", when true, will show a message to players every time they open a shared door.

* Setting "UsePermissions" applies to "share.use" only.



**Permissions**

This plugin uses oxide permissions.


share.use - Allows players to share their doors with other players

share.owner - Allow players to check the owner of doors

share.nocd - Allows players to bypass command cooldown

share.admin - Allow players access to admin commands

````
oxide.grant <group|user> <name|id> share.use

oxide.revoke <group|user> <name|id> share.use
````


**Usage for players**


/share - View help

/share limits - View door share limits

/share add <player> - Share all doors with player

/share remove <player> - Unshare all doors with player

/share removeall - Unshare all doors with all players (cannot be undone)

/share list - List players sharing your doors

**Usage for administrators**


/share toggle - Enable or disable door sharing system

/share auth - Temporarily authorize on all nearby cupboards

**Configuration file**

````
{

  "Messages": {

  "Auth": "You have been temporarily authorized on all nearby cupboards.",

  "ChangedStatus": "Door sharing system <color=#cd422b>{status}</color>.",

  "CoolDown": "You must wait <color=#cd422b>{cooldown} seconds</color> before using this command again.",

  "DeleteAll": "You no longer share your doors with anyone. (<color=#cd422b>{entries}</color> players deleted)",

  "Disabled": "Door sharing is currently disabled.",

  "DoorAccess": "You were granted access to this door by <color=#cd422b>{player}</color> ({id}).",

  "DoorOwner": "The owner of this door is <color=#cd422b>{player}</color> ({id}).",

  "MaxShare": "You may only share your doors with <color=#cd422b>{limit} player(s)</color> at one time.",

  "MultiPlayer": "Multiple players found.  Provide a more specific username.",

  "NewShareAdd": "You have been granted access to all doors owned by <color=#cd422b>{player}</color>.",

  "NewShareDel": "You no longer have access to all doors owned by <color=#cd422b>{player}</color>.",

  "NoPermission": "You do not have permission to use this command.",

  "NoPlayer": "Player not found.  Please try again.",

  "NoShares": "You do not share your doors with anyone.",

  "PlayerAdded": "You now share all your doors with <color=#cd422b>{player}</color>.",

  "PlayerDeleted": "You no longer share all your doors with <color=#cd422b>{player}</color>.",

  "PlayerExists": "You already share your doors with <color=#cd422b>{player}</color>.",

  "PlayerNotExists": "You do not share your doors with <color=#cd422b>{player}</color>.",

  "Self": "You cannot share your doors with yourself.",

  "WrongArgs": "Syntax error.  Use <color=#cd422b>/share</color> for help."

  },

  "Settings": {

  "Cooldown": "10",

  "Enabled": "true",

  "MaxShare": "25",

  "Prefix": "[ <color=#cd422b>Door Share</color> ]",

  "ShowAccessMessage": "true",

  "UsePermissions": "true"

  }

}
````

Configuration file will be created and updated automatically.

**More to come**


* Your suggestions


**Known Issues**


* None