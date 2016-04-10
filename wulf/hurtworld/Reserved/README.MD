**Reserved**  sets aside a configurable number of slots so that select players can connect even when the server has the maximum number of regular players connected.

**Permissions** 

This plugin uses Oxide's permission system. To assign a permission, use **grant user <username|steamid> <permission>** . To remove a permission, use **revoke user <username|steamid> <permission>** .


* reserved.slot (allows player to use a reserved slot)
Ex. grant user Wulf reserved.slot
Ex. revoke user Wulf reserved.slot
Ex. grant group admin reserved.slot

**Configuration** 

You can configure the settings in the Reserved.json file under the oxide/config directory.

````
{

  "ReservedSlots": 5

}
````

**Localization** 

The default messages are in the Reserved.en.json under the oxide/lang directory, or create a language file for another language using the 'en' file as a default.

````
{

  "ReservedSlotsOnly": "Only reserved slots available"

}
````