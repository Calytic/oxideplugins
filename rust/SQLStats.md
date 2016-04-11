This plugin collects various data from in game, and saves it to MySQL database. This allows server owners to have nice statistics of whats happening in their server, and also allows them to display those statistics on their website.

Donations would motivate me to do few more plugins: SQLRanks and SQLAchievements. Which would allow you to display such details in game using GUI. So even beginner server owners could make use of SQLStats plugin.

Working examples:


* [[EU] BEST](http://eubestservers.com/stats.php)


Plugin logs these events (so far):


* Player login [Table: stats_player]

This table saves latest player name he joined server with [Column: name].

How long he was online in server (in seconds), this column is updated every time player disconnects. [Column: online_seconds].

Player steam id [Column: id].

Player last used IP [Column: ip].

Player state [Column: online].
Click to expand...

* Player animal kills [Table: stats_player_animal_kill]

This table saves what player (his steam id) [Column: player].

Killed what animal [Column: animal].

On what date and time [Column: date].

From what range [Column: distance].

With which weapon [Column: weapon].
Click to expand...

* Player crafted items list and count for each day for each player. [Table: stats_player_craft_item]

This tables saves information about player crafted things for each day. For example it shows how many Timed Explosives has been crafted on 2015-12-12 by certain player. Also with power of MySQL you can select how many Timed Explosives have been crafted through whole week, but i'll not explain how to do it here, expect details on F.A.Q. page.

Table saves player steam id [Column: player].

Item which have been crafted name [Column: item].

Date when this item has been crafted [Column: date].

Count of how many items were crafted that day, by that player [Column: count].
Click to expand...

* Player death information for each day. Statistics such as how many times player died from cold/bullet/fall/etc on 2015-12-10 for example. [Table: stats_player_death]

This table allows you to see who died the most, what cause made most deaths and etc.

Table saves player (who died) steam id [Column: player].

Cause of player death (BITE/SUICIDE/BULLET/etc) [Column: cause].

Date when death happened. [Column: date]

Count of how many times played died on that day, from that cause. [Column: count].
Click to expand...

* Statistics about player destroyed buildings. [Table: stats_player_destroy_building]

This table allows you to check how many building objects player destroyed and what weapon was used last to destroy that object.

Table saves player (who destroyed) steam id [Column: player].

Building object which was destroyed name. [Column: building].

Date and time when building was destroyed. [Column: date].

Tier and max health of building object. [Column: tier].

Weapon which was used when building was destroyed. [Column: weapon].
Click to expand...

* Statistics about player bullet shots for each day. [Table: stats_player_fire_bullet]

This table allows you to check how many bullets from which weapon player shot at certain day.

Table saves player (who was shooting) steamid [Column: player].

Bullet which was shot name. [Column: bullet].

Weapon which shot that bullet. [Column: weapon].

Date when bullet was shot. [Column: date].

Count how many certain bullets were shot that way by that player with  that weapon. [Column: count].
Click to expand...

* Statistic about resources player gathered certain day. [Table: stats_player_gather_resource]

This tables shows you how many certain resources player gathered on certain day.

Table saves player (who gathered resources) steamid [Column: player].

Resource which was gathered [Column: resource].

Count of how many resources were gathered that day [Column: count].

Date when resources were gathered. [Column: date].
Click to expand...

* Statistics about player kills in PvP. [Table: stats_player_kill]

This table shows you what player killed what victim player, from what distance, on what date and time and on which body part killing blow happened.

Table saves killer steam id [Column: killer].

Victim's steam id [Column: victim].

Weapon which was used to kill [Column: weapon].

Bodypart on which killing blow was landed. [Column: bodypart].

Exact time and date when killing blow was made. [Column: date].

Distance how far away killer was away from victim. [Column: distance].
Click to expand...

* Statistic about player placed building objects. [Table: stats_player_place_building]

This table shows how many and which building objects player has placed on certain date.

Table saves player steam id (who placed) [Column: player].

Building object name which was placed [Column: building].

Date when building object was placed [Column: date].

Count of how many such building objects were placed on that date [Column: date].
Click to expand...

* Statistic about player placed deployable objects(such as Campfires, Auto Turrets, etc). [Table: stats_player_place_deployable]

This table shows how many and which building objects player has placed on certain date.

Table saves player steam id (who placed) [Column: player].

Deployable name which was placed [Column: deployable].

Date when deployable was placed [Column: date].

Count of how many such deployables were placed on that date [Column: date].
Click to expand...Here is config file for the plugin:

````
{

  "dbConnection": {

    "Database": "RustDB",

    "Host": "127.0.0.1",

    "Password": "RustPW",

    "Port": 3306,

    "Username": "RustUSR"

  }

 
````

Here is SQL structure which is used in my plugin:

````
CREATE TABLE `stats_player` (

  `id` bigint(20) NOT NULL,

  `name` varchar(256) CHARACTER SET utf8 NOT NULL,

  `online_seconds` bigint(20) NOT NULL DEFAULT '0',

  `ip` varchar(50) COLLATE utf8_unicode_ci NOT NULL,

  `online` bit(1) NOT NULL DEFAULT b'0',

  PRIMARY KEY (`id`)

) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci;


CREATE TABLE `stats_player_animal_kill` (

  `id` bigint(20) NOT NULL AUTO_INCREMENT,

  `player` bigint(20) NOT NULL,

  `animal` varchar(32) NOT NULL,

  `date` datetime NOT NULL,

  `distance` int(11) DEFAULT NULL,

  `weapon` varchar(128) DEFAULT NULL,

  PRIMARY KEY (`id`)

) ENGINE=InnoDB AUTO_INCREMENT=1 DEFAULT CHARSET=utf8;


CREATE TABLE `stats_player_craft_item` (

  `id` bigint(20) NOT NULL AUTO_INCREMENT,

  `player` bigint(20) NOT NULL,

  `item` varchar(32) NOT NULL,

  `date` date NOT NULL,

  `count` int(11) NOT NULL DEFAULT '1',

  PRIMARY KEY (`id`),

  UNIQUE KEY `PlayerItemDate` (`player`,`item`,`date`)

) ENGINE=InnoDB AUTO_INCREMENT=1 DEFAULT CHARSET=utf8;


CREATE TABLE `stats_player_death` (

  `id` bigint(20) NOT NULL AUTO_INCREMENT,

  `player` bigint(20) NOT NULL,

  `cause` varchar(32) NOT NULL,

  `date` date NOT NULL,

  `count` int(11) NOT NULL DEFAULT '1',

  PRIMARY KEY (`id`),

  UNIQUE KEY `PlayerCauseDate` (`player`,`cause`,`date`)

) ENGINE=InnoDB AUTO_INCREMENT=1 DEFAULT CHARSET=utf8;


CREATE TABLE `stats_player_destroy_building` (

  `id` bigint(20) NOT NULL AUTO_INCREMENT,

  `player` bigint(20) NOT NULL,

  `building` varchar(128) NOT NULL,

  `date` datetime NOT NULL,

  `tier` varchar(20) DEFAULT NULL,

  `weapon` varchar(128) DEFAULT NULL,

  PRIMARY KEY (`id`)

) ENGINE=InnoDB AUTO_INCREMENT=1 DEFAULT CHARSET=utf8;


CREATE TABLE `stats_player_fire_bullet` (

  `id` bigint(20) NOT NULL AUTO_INCREMENT,

  `player` bigint(20) NOT NULL,

  `bullet` varchar(32) NOT NULL,

  `weapon` varchar(128) NOT NULL,

  `date` date NOT NULL,

  `count` int(11) NOT NULL DEFAULT '1',

  PRIMARY KEY (`id`),

  UNIQUE KEY `PlayerBulletWeaponDate` (`player`,`bullet`,`weapon`,`date`)

) ENGINE=InnoDB AUTO_INCREMENT=1 DEFAULT CHARSET=utf8;


CREATE TABLE `stats_player_gather_resource` (

  `id` bigint(20) NOT NULL AUTO_INCREMENT,

  `player` bigint(20) NOT NULL,

  `resource` varchar(32) NOT NULL,

  `count` bigint(20) NOT NULL,

  `date` date NOT NULL,

  PRIMARY KEY (`id`),

  UNIQUE KEY `PlayerResourceCountDate` (`player`,`resource`,`date`)

) ENGINE=InnoDB AUTO_INCREMENT=1 DEFAULT CHARSET=utf8;


CREATE TABLE `stats_player_kill` (

  `id` bigint(20) NOT NULL AUTO_INCREMENT,

  `killer` bigint(20) NOT NULL,

  `victim` bigint(20) NOT NULL,

  `weapon` varchar(128) NOT NULL,

  `bodypart` varchar(2000) NOT NULL DEFAULT '',

  `date` datetime NOT NULL,

  `distance` int(11) DEFAULT NULL,

  PRIMARY KEY (`id`)

) ENGINE=InnoDB AUTO_INCREMENT=1 DEFAULT CHARSET=utf8;


CREATE TABLE `stats_player_place_building` (

  `id` bigint(20) NOT NULL AUTO_INCREMENT,

  `player` bigint(20) NOT NULL,

  `building` varchar(128) NOT NULL,

  `date` date NOT NULL,

  `count` int(11) NOT NULL DEFAULT '1',

  PRIMARY KEY (`id`),

  UNIQUE KEY `PlayerBuildingDate` (`player`,`building`,`date`)

) ENGINE=InnoDB AUTO_INCREMENT=1 DEFAULT CHARSET=utf8;


CREATE TABLE `stats_player_place_deployable` (

  `id` bigint(20) NOT NULL AUTO_INCREMENT,

  `player` bigint(20) NOT NULL,

  `deployable` varchar(128) NOT NULL,

  `date` date NOT NULL,

  `count` int(11) NOT NULL DEFAULT '1',

  PRIMARY KEY (`id`),

  UNIQUE KEY `PlayerDeployableDate` (`player`,`deployable`,`date`)

) ENGINE=InnoDB AUTO_INCREMENT=1 DEFAULT CHARSET=utf8;

 
````