Easy to use plugin, will eventually have the ability to modify config ingame but for now it needs to be manually edited. Commands include:

/admins

/setadmin 'user'


This plugin will list all staff upon the use of /admins (those who you enter). Currently there is the following included:

1 Owner Slot

1 Co Owner Slot

1 Head Admin Slot

10 Admin Slots

10 Mod Slots


Config File:

````
{

  "Admin1: An Admin of the server": "",

  "Admin10: An Admin of the server": "",

  "Admin2: An Admin of the server": "",

  "Admin3: An Admin of the server": "",

  "Admin4: An Admin of the server": "",

  "Admin5: An Admin of the server": "",

  "Admin6: An Admin of the server": "",

  "Admin7: An Admin of the server": "",

  "Admin8: An Admin of the server": "",

  "Admin9: An Admin of the server": "",

  "CoOwner: The Coowner of the server": "Owner has yet to input this!",

  "Head Admin: The Head Admin of the server": "Owner has yet to input this!",

  "Mod1: A Mod of the server": "",

  "Mod10: A Mod of the server": "",

  "Mod2: A Mod of the server": "",

  "Mod3: A Mod of the server": "",

  "Mod4: A Mod of the server": "",

  "Mod5: A Mod of the server": "",

  "Mod6: A Mod of the server": "",

  "Mod7: A Mod of the server": "",

  "Mod8: A Mod of the server": "",

  "Mod9: A Mod of the server": "",

  "Owner: The owner of the server": "Owner has yet to input this!",

  "Settings: Number of Admins": 0,

  "Settings: Number of Moderators": 0,

  "Settings: Server has a coowner": true,

  "Settings: Server has a head admin": true,

  "Settings: Server has an admin": true

}
````

Example setup:

````
{

  "Admin1: An Admin of the server": "Drab",

  "Admin10: An Admin of the server": "Kayla",

  "Admin2: An Admin of the server": "Flap",

  "Admin3: An Admin of the server": "Frost",

  "Admin4: An Admin of the server": "BDM",

  "Admin5: An Admin of the server": "",

  "Admin6: An Admin of the server": "",

  "Admin7: An Admin of the server": "",

  "Admin8: An Admin of the server": "",

  "Admin9: An Admin of the server": "",

  "CoOwner: The Coowner of the server": "Max",

  "Head Admin: The Head Admin of the server": "MEEEEH",

  "Mod1: A Mod of the server": "James",

  "Mod10: A Mod of the server": "Frank",

  "Mod2: A Mod of the server": "Smith",

  "Mod3: A Mod of the server": "John",

  "Mod4: A Mod of the server": "Jane",

  "Mod5: A Mod of the server": "Philip",

  "Mod6: A Mod of the server": "",

  "Mod7: A Mod of the server": "",

  "Mod8: A Mod of the server": "",

  "Mod9: A Mod of the server": "",

  "Owner: The owner of the server": "Bond",

  "Settings: Number of Admins": 5,

  "Settings: Number of Moderators": 6,

  "Settings: Server has a coowner": true,

  "Settings: Server has a head admin": true,

  "Settings: Server has an admin": true

}
````