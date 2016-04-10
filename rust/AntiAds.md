**Anti-Advertising** automatically blocks advertising of not allowed servers, with kick and ban options. Players can be allowed to advertise if they have the "antiads.bypass" permission mentioned below.

This plugin is also the first of it's kind, supporting multiple games in a single plugin, with no real performance loss! The plugin will likely evolve as the ideas behind this become more integrated into Oxide's core, allowing for plugins to be more universal with games.
**Permissions**

This plugin uses Oxide's permission system. To assign a permission, use **oxide.grant user <username|steamid> <permission>**. To remove a permission, use **oxide.revoke user <username|steamid> <permission>**.


* 
**antiads.bypass** (allows player to advertise any server address)
**Ex.** oxide.grant user Wulf antiads.bypass
**Ex.** oxide.revoke user Wulf antiads.bypass
**Ex.** oxide.grant group moderator antiads.bypass


**Configuration**

You can configure the settings and message strings in the AntiAdvertising.json file under the server/identity/oxide/config directory.
**Default Configuration**

````
{

  "Settings": {

    "AllowedServers": [

      "84.200.193.120:28030",

      "84.200.193.120:28080"

    ],

    "Ban": "false",

    "Kick": "true"

  }

  "Messages": {

    "NoAdvertising": "Please do not advertise on this server!",

    "PlayerBanned": "{player} banned for advertising",

    "PlayerKicked": "{player} kicked for advertising"

  }

}
````

The configuration file will update automatically if new options are added or removed. I'll do my best to preserve any existing settings and messages with each new version.