Hey all,


First of all I would to thank **Evanonian **for the inspiration for this plugin. As well as this, I do wish to forewarn you all that this is my first plugin so although I have checked and double checked there maybe some issues. Now that is out of the way.

**About:**


Basically this plugin will  allow admins and players with permissions to plan announcements in advance. This will make use of the [PopupNotifications  plugin for the GUI pop ups. Don't worry, if you don't have it, this will use chat instead. If by chance a notification is missed, when the plugin is next run, it will see that it has not run and then run it. Hope it works well for you all!](http://oxidemod.org/plugins/popup-notifications.1252/)

**Perms:**


canplanevent

**Default Config:**



````
{

  "Events": {

    "AllowedDays": "Mon,Tue,Wed,Thu,Fri,Sat,Sun"

  },

  "Plugin": {

    "EnablePopUps": true,

    "PopUpTime": 20,

    "TimeCheck": 10,

    "Version": "0.0.1"

  }

}
````


**AllowedDays** - Using Days with a 3 letter code, you can block any notifications on any certain day. Just remove them from this list.
**EnablePopups** - If enabled, it will use the optional dependency - otherwise it will use the chatl og
**PopUpTime **- The time in seconds the pop up will stay up (if unclosed)
**TimeCheck** - Time in seconds to check for any events
**Version** - Just the version 

**Commands:**

**/notification (add|hourly|daily|weekly|monthly|yearly) <DD/MM/YY> <HH:MM> "<MESSAGE>"** - To schedule a notification for the specified time
**/notification (addcmd|hourlycmd|dailycmd|weeklycmd|monthlycmd|yearlycmd) <DD/MM/YY> <HH:MM> "<COMMAND>"** - To schedule a console command for the specified time
**/notification list** - List Future Events
**/notification reset** - Remove Current and Past Events
**/notification remove <ID>** - Removes specified event - IDs will alter on removal

**TODO List:**


* Notification Removal - can use reset, edit the MyEvents data file for the meantime)
* Hooks (if any dev has idea for how they wish to hook in send me a PM)
* Run custom commands at the time and date specified.
* Console commands