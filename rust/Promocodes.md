**Promocodes** allows you to set up promocodes which run a command on the player who redeemed a code


You can either use custom codes or generate some using the console command: **promocode.generate**. THIS IS CURRENTLY NOT IN!


You can set up own codes, by copying the default/example and customizing it.


This was for example used on UK Wasteland's Birthday. 

**Chat Commands:**


* 
**/redeem<code> **to try to redeem a code


**
**Console Commands: THIS IS CURRENTLY NOT IN!****


* 
****promocode.generate <amount> ****- generate a specific amount of codes


**Permissions: THIS IS CURRENTLY NOT IN!**


* **promocode.generate**



**Configfile:**

````
{
"Promocodes": [

    {

      "availableCodes": [

        // 10 random codes

      ],

      "command": "oxide.usergroup add {steamid} vip"

    }

  ]
}
````

in **"command"** you can use following tags:


* 
**{steamid} **represents the players steamID
* 
**{name}** represents the players name