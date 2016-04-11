This plugin allows those of you that wipe regularly to easily store and show your wipe patterns for your users along with countdowns for when the next wipe may happen.


If the option is enabled when someone says Next Wipe in chat it will send the player a message informing them of your wipe cycle and how many days until the next wipe

**Chat Command**

/NextWipe will announce your wipe cycle to the player that typed it


````
{

  "AnnounceToPlayer": true, -- Announce's to player when Next Wipe is typed by them

  "AnnounceToServer": false, (WIP Not active)

  "DatePattern": "MM/dd/yyyy", -- Date Pattern how you want to display the date

  "DaysBetweenWipes": "14" -- How many days till the next map wipe,

  "DaysBetweenWipesBP": "14" -- How many days till he next BP Wipe,

  "LastWipe": "05/11/2015 12:00:00 AM", -- The last Map Wipe (Time not used)

  "LastWipeBP": "01/09/2015 12:00:00 AM", -- The last BP Wipe (Time not used)

  "RegionDateType": "en-GB", -- Your Region for the time (en-GB, en-US etc)

  "UseBPWipes": true -- announce BP Wipe Cycle

}
````


**Features Coming:**


* Set wipe from in game
* Announce to server