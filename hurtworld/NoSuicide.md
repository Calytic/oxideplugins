**NoSuicide** stops players from suiciding/killing themselves.

**Permissions**

This plugin uses Oxide's permission system. To assign a permission, use **grant user <username|steamid> <permission>**. To remove a permission, use **revoke user <username|steamid> <permission>**.


* 
**nosuicide.excluded** (allows player to use 'kill')
**Ex.** grant user Wulf nosuicide.excluded
**Ex.** revoke user Wulf nosuicide.excluded
**Ex.** grant group moderator nosuicide.excluded


**Localization**

The default messages are in the NoSuicide.en.json under the oxide/lang directory, or create a language file for another language using the 'en' file as a default.

````
{

  "SuicideNotAllowed": "Sorry, suicide is not an option!"

}
````