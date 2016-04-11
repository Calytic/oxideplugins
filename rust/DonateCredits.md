This plugin rewards a player with Economy-based credits from a MySQL database for donating to your server.

Disclaimer:  This plugin **absolutely** requires some web development experience.  You will have to develop your own web application to take payments, hook in Steam data, and save data to your MySQL Database.  This plugin will work right out-of-the-box once those requirements are met.  I will do my best to answer any and all questions, but I will not develop your web application for you (unless you'd like to pay me, of course).  **[You can find a template web application here](https://github.com/cPalmtrees/DonationCredits)**.

**Default Config**

This is where you put your webserver's MySQL information (make sure your Rust Server IP has access).

````
{

  "address": "127.0.0.1",

  "db_name": "my_dbname",

  "password": "password",

  "port": 3306,

  "user": "username"

}
````


**Chat Commands**

/credits - Reloads and displays a player's current donation statistics.

/getcredits - Reloads a players donation data and then gives them their donation reward credits (if available).

/refreshcredits - Phased out command.  Only ask a player to use it if they are not receiving their reward.

Admin Only

/loadcredits - Reloads all current players' donation data.

/savecredits - Saves all loaded player donation data directly to MySQL database (**WARNING:** DO NOT USE THIS BEFORE RELOADING ALL USER DATA).

**MySQL Structure**

Use these to create your MySQL Donations table:

````
CREATE TABLE IF NOT EXISTS `Donations` (

  `steam_id` bigint(20) NOT NULL,

  `user` text NOT NULL,

  `amount` bigint(20) NOT NULL DEFAULT '0',

  `total_amount` bigint(20) NOT NULL DEFAULT '0',

  `last_date` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,

  PRIMARY KEY (`steam_id`),

  UNIQUE KEY `steam_id` (`steam_id`)

) ENGINE=InnoDB DEFAULT CHARSET=utf8;
````


````
DROP TRIGGER IF EXISTS `donations_update_date`;

DELIMITER //

CREATE TRIGGER `donations_update_date` BEFORE UPDATE ON `Donations`

FOR EACH ROW SET NEW.last_date = CURRENT_TIMESTAMP

//

DELIMITER ;
````

**Demo**
**[See A Demo on Our Server](http://owd.clanservers.com/)
[Web Application Template](https://github.com/cPalmtrees/DonationCredits)**

**Future Plans**


* Create Git for boilerplate Web Application
* Remove all things related to /refreshcredits
* Item Set Rewards



**Thank Yous**

A Big Thanks to **[Visagalis](http://oxidemod.org/members/visagalis.63505/) **for making [**ZLevels**](http://oxidemod.org/threads/zlevels-remastered.12803/)... I learned a lot about Rust plugins and MySQL from his code.  And of course, **[Nogrod](http://oxidemod.org/members/nogrod.45547/) **for [**Economics **](http://oxidemod.org/threads/economics.5985/)and **[Domestos](http://oxidemod.org/members/domestos.3412/) **for [**HelpText**](http://oxidemod.org/threads/helptext.5836/).  Thank you all