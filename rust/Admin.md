**Ultimate Admin** allows admins or users with permission to easily go from User to Admin mode which saves and restores inventories, enables godmode, cancels damage done by admins in godmode and allows the admin to open all doors. All of the admin abilities can be configured in the config file.


The server owner (or anyone with the permission **admin.master **(default permission)) must save a Master set before other admins can use it.


Changes while being in Admin Mode with permission:

- Automatically save and revert inventory when toggling /admin

- Godmode is enabled

- Lock inventory when sleeping

- Bypass building restriction

- No durability on items

- Damage dealt while in godmode is cancelled

- Open any locked door

**Chat Commands

Toggle Admin Mode: /**admin
**Save Master Set: /**admin master
**Save Custom Set: /**admin save (admin.loadout permission required)
**Change Preferred Set: /**admin set master** OR /**admin set custom

**ToDo List**

- /kick

- /ban

- /airdrop

- Update Config in-game

- Your suggestions


All Admin Data is saved when a player disconnects and through server restarts.

**Default Config**

````

{

  "Messages": {

    "AdminDisabled": "<color=#FF3D0D>You have disabled Admin Mode.</color>",

    "AdminEnabled": "<color=#99CC32>You have enabled Admin Mode.</color>",

    "CantDamageAdmin": "<color=#FF3D0D>This player is in admin mode and can't be damaged!</color>",

    "CantLootAdmin": "<color=#FF3D0D>You are not allowed to open an admin's inventory!.</color>",

    "InventorySaved": "<color=#99CC32>You have saved your current inventory.</color>",

    "NoMasterSaved": "<color=#FF3D0D>No master inventory found! Ask the owner of the server to save a Master inventory.</color>",

    "NoPermission": "<color=#FF3D0D>You do not have permission to use this command.</color>",

    "TriedToDamage": " tried to damage you!"

  },

  "Permissions": {

    "BypassBuildingBlocked": "admin.bypass",

    "CanKickPlayers": "admin.kick",

    "CanOpenAllDoors": "admin.door",

    "CanSaveMaster": "admin.master",

    "CanUseAdmin": "admin.use",

    "CanUseCustomLoadout": "admin.loadout",

    "Godmode": "admin.god",

    "LockedInventory": "admin.lock",

    "NoDurability": "admin.durability"

  },

  "Settings": {

    "AdminsBypassBuildingBlocked": "true",

    "AdminsCanOpenAllDoors": "true",

    "AdminsCanUseGodMode": "true",

    "AdminsNoDurability": "true",

    "DisableAdmin_vs_EntityDamageIfGodmode": "true",

    "DisableAdmin_vs_PlayerDamageIfGodmode": "true",

    "NotifyAdminWhenDamaged": "true",

    "NotifyPlayerWhenAttacking": "false",

    "RequiredAuthLevel": 2,

    "UseAuthLevelPermission": "true"

  }

}

 
````