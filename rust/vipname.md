This plugin will auto-rename any player connecting to your server found in the VIP or Admin list.


Here an example Config file. Make sure that you add your Admin's and your VIP's SteamID's to the lists below.

````
{

  "Admins": [

  "99999999999999991",

  "99999999999999992"

  ],

  "Vips": [

  "99999999999999991",

  "99999999999999992"

  ],

  "Settings": {

  "AddVipTag": "true",

  "AdminTag": "(Admin) ",

  "VipTag": " ]ViP[",

  "AddAdminTag": "true"

  }

}
````

eg.: **TheDoc** will become ->** (Admin) TheDoc**


Assuming my SteamID is listed under the ADMINS in the config file


or


eg.: **TheDoc** will become ->** TheDoc ]VIP[**


Assuming my SteamID is listed under the VIPS in the config file


Enjoy and let me know what you guys think.