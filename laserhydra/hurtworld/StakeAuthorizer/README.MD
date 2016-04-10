**Stake Authorizer** allows you to authorize yourself for every stake or for all stakes in a radius.
**Permissions:**


* 
**stakeauthorizer.use **for using the plugin


****

How permissions work:****


* 
****/grant user <player> <permission>** **grant permission to player
* 
****/grant group <group> <permission>** **grant permission to group


**Chat Commands:**


* 
**/authall <radius> **Authorize at all stakes in a radius from you
* 
**/authall** Authorize at all stakes on the server



* 
**/deauthall <radius> **Deauthorize at all stakes in a radius from you
* 
**/deauthall** Deauthorize at all stakes on the server



* 
**/undoauth **Deauthorize at all stakes you used **/authall** on before


**Language file:**

````
{

  "No Permission": "You don't have permission to use this command.",

  "Authorized": "You have been authorized for all stakes.",

  "Authorized Radius": "You have been authorized for all stakes in the radius of {radius}m."
}
````


Thanks to [@Cheeze](http://oxidemod.org/members/43678/) for testing the plugin.