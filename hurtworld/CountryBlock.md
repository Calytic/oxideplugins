**CountryBlock** blocks or allows players only from configured countries. This is useful for keeping your player-base somewhat localized.

**Permissions**

This plugin uses Oxide's permission system. To assign a permission, use **grant user <username|steamid> <permission>**. To remove a permission, use **revoke user <username|steamid> <permission>**.


* 
**countryblock.bypass** (allows player to bypass block)
**Ex.** grant user Wulf countryblock.bypass
**Ex.** revoke user Wulf countryblock.bypass
**Ex.** grant group moderator countryblock.bypass


**Configuration**

You can configure the settings in the CountryBlock.json file under the oxide/config directory.

````
{

  "CountryList": [

    "CN",

    "RU"

  ],

  "Whitelist": false

}
````


**Localization**

The default messages are in the CountryBlock.en.json under the oxide/lang directory, or create a language file for another language using the 'en' file as a default.

````
{

  "PlayerRejected": "This server doesn't allow players from {country}"

}
````