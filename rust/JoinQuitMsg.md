This is a very basic plugin that simply let's players know when players join and disconnect form your server. I created this plugin for my server because other plugins with the same functionality where a bit overkill.



**Config:

Admin Colour:** The colour of admin names. it's **#AAFF55** green by default. If you want to hide the fact that you are an admin just use the players colour.
**Player Colour:** The colour of player names. it's **#55AAFF** blue by default.
**Only Show Admins:** when this is true only admins will show up when they connect or disconnect.
**Player join message:** This is the format of the message when someone joins. {1} is replaced with the colour for admins or players and {0} is replaced with the name of the player.
**Player leave message:** This is the format of the message when someone quits the server {1} and {0} are the same as above but the {2} is replaced with the reason why they where disconnected. You can remove the {2} if you want.
**Show Connections: **Set this to false if you do not want to let players know when someone joins.
**Show Disconnections:** The same as Show Connections but for Disconnections.


{

  "Admin Colour": "#AAFF55",

  "Only Show Admins": false,

  "Player Colour": "#55AAFF",

  "Player join message": "<color={1}>{0}</color> has joined the game.",

  "Player leave message": "<color={1}>{0}</color> has left the game.",

  "Show Connections": true,

  "show Disconnections": true

}