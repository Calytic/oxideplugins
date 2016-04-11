Adds a basic economy with money and transfers to Rust. Owners/moderators (authlevel 1/2) can give money for free.

Or groups/players with the permission eco.give if usepermissions is set to true in config!

**CONFIGURATION FILE:**

(server root path)/server/(serveridentity)/oxide/data/Reconomy.json
NOT THE CONFIG DIRECTORY!

**Beta testings:**

Betas are avaible on: [||JMTeams Betas||](http://jmnet.servegame.com/rust/beta/)
Report errors in beta trough email not on this site!

email: [jmgamerzzz@gmail.com](mailto:jmgamerzzz@gmail.com)

**Chat Command:**


* /eco - Use this for help


**API Reference:**
Code (C#):
````
[PluginReference]

Plugin Economy;
//Add money or test if player can pay
float money = Economy.Call("Money", BasePlayer player);

Boolean hasenoughmoney = Economy.Call("CanPay", BasePlayer player, double amount, Boolean NotifyWhenPlayerCantPay);

Economy.Call("GiveMoney", BasePlayer ply, double amount);
````

if you want to remove money you just use:

Economy.Call("GiveMoney", BasePlayer ply, -1,50); (this will make the user pay 1,50)
**DEVELOPPER WARNING:**

I do recommend you to use the Beta versions of EconomyCore for your coding! This source is only weekly updated!!
[Link to EconomyCore Beta](http://jmnet.servegame.com/rust/beta/)

**ConfigShare:**

Do you have a rust network?

You think of MySql?

Well, i did not.

This plugin provides configshare.

go ahead and open your data folder

rusterserver/server/myserverid/oxide/data/Reconomy.json

you will see these values:

"ConnectionAPI": false,

"ConnectionAPIKey": "xxx",

"ConnectionAPIPassword": "xxx"


Change the APIKey to whatever you want. if the console returns an error it means the key is already claimed with another password.

If you see config created youre good to go.

This COULD lagg, 1 player could lose some data. like 1 payment.

because it updates and reads. it may update and overwrite a save.

this risk is once in the 0,1 second. if 1 player gets money on one server and also one on another then there can be a conflict. but the chances are small 
**READ THE UPDATE:
**Critical fix:****

Decrased change of losing data when using configshare,

Updatetime reducable.

If the config server crashes then i need to put a cooldown on it (belive me you dont want that)

So dont have a too high update rate!

**Current plugins with Economy:**
[LevelAPI ](http://oxidemod.org/plugins/levelapi.1450/)(Gather resources and level up) [Approved]
[LevelAPI ](http://oxidemod.org/plugins/levelapi.1450/)Sreenshots: