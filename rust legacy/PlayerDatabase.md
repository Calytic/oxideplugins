**Note:** if another dev wants to create as a sqlite database you are welcomed, i'll remove this plugin after 
Core database to log all players.

Default will log only userids and names.

Outside plugins can call this plugin to get player names from userids

But also add and retrieve other informations as they see fit.

You may use the SteamAPI system to get the names of unknown players
**Commands:**

- /findname STEAMID => find the name that belong to this steamid
**USING MYSQL:

Very usefull for BIG Servers as no config files will be needed making everything lag free**

1) Create your Mysql Database with:

````

CREATE TABLE IF NOT EXISTS `playerdatabase` (

  `id` int(5) NOT NULL AUTO_INCREMENT,

  `steamid` varchar(17) CHARACTER SET latin1 NOT NULL,

  `name` varchar(255) CHARACTER SET latin1 NOT NULL,

  PRIMARY KEY (`id`)

) ENGINE=InnoDB  DEFAULT CHARSET=utf8 AUTO_INCREMENT=8 ;

 
````

2) Configure your config file (table is playerdatabase by default but you can manually edit both if you want)

3) restart the plugin

4) join the game and go see your database if you showed inside 
F**or Plugin Devs:**
Get a player data with a key:

````
object GetPlayerData(string userid, string key)

 
````

exemple:

````
var playername = PlayerDatabase.Call("GetPlayerData",structure._master.ownerID.ToString(), "name");
````

Set a player data with a key & value:

````
object SetPlayerData(string userid, string key, object value)

 
````

exemple:

````
var lastpos = new Dictionary<string,float>();

lastpos.Add("x",netUser.playerClient.lastKnownPosition.x);

lastpos.Add("y",netUser.playerClient.lastKnownPosition.y);

lastpos.Add("z",netUser.playerClient.lastKnownPosition.z);

PlayerDatabase.Call("SetPlayerData",netUser.playerClient.userID.ToString(), "last_position", lastpos );
````

then it can be called via getplayerdata "last_position"
find players steamid:

````
string[] FindAllPlayers(string name)
````

Configs: PlayerDatabase.json

````

{

  "Mysql: activated": false,

  "Mysql: database": "databasename",

  "Mysql: host": "localhost",

  "Mysql: password": "password",

  "Mysql: port": 3306,

  "Mysql: table": "playerdatabase",

  "Mysql: username": "username",

  "Settings: SteamAPI Key http://steamcommunity.com/dev/apikey": ""

}

 
````