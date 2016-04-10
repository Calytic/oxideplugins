This plugin allows you to record users emails and then dump that data later, you can also reward your player random item(s) that you have added to the configuration file upon them registering their email address.



**Commands:**

*** Player:**

/mail set (This is the one they will be rewarded for, technically registering their email.)

/mail update (Users can update their email at any time)

***Admin:**

/mail dump (Dumps the email database to a format that will hopefully be readable by mailing list software etc.

/mail cleardb (Clears the database of all its content)

**Default Configuration:**

````

{

  "bRewardPlayer": true,

  "bShowPluginName": true,

  "bUsePopupNotifications": false,

  "dRewardItems": {

    "sign.hanging.banner.large": 1,

    "sign.hanging.ornate": 1,

    "sign.pictureframe.landscape": 1,

    "sign.pictureframe.portrait": 1,

    "sign.pictureframe.tall": 1,

    "sign.pictureframe.xl": 1,

    "sign.pictureframe.xxl": 1,

    "sign.pole.banner.large": 1,

    "sign.post.double": 1,

    "sign.post.single": 1,

    "sign.post.town": 1,

    "sign.post.town.roof": 1,

    "sign.wooden.huge": 2,

    "sign.wooden.large": 2,

    "sign.wooden.medium": 3,

    "sign.wooden.small": 4

  },

  "iAuthLevel": 2,

  "iMaxRewardItems": 1,

  "iProtocol": 1336,

  "tDBCleared": "You have <color=#FF3300>cleared</color> the Mailing List Rewards database.",

  "tNoAuthLevel": "You <color=#FF3300>do not</color> have access to this command.",

  "tNotification": "Enter your email and receive updates, items and more! /mail set <Your e-mail address>."

}

 
````


**TODO:**


* Convert the rest of the text into configs.
* Better dumping and/or a PHP script to get readable data from my dump as I can't seem to figure out how to write plaintext atm.
* Perhaps integrate Email API.

Idea from request by [Request - Reward for email submission | Oxide](http://oxidemod.org/threads/reward-for-email-submission.12326/)