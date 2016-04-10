**Permission:**

reporting.admin

**Add permission:**

/grant user Player reporting.admin

/grant group Admin reporting.admin

**Commands:**

/reportbase

/report Player description

(Report a player for something, all online admins that have permission will be notified when a report has been submitted)


/unread

(This will read all unread reports one at a time, SteamID's for the submitter and the offender will be shown, along with what they reported them for. You can use the SteamID to ban them if they are not online in the F1 console (If you're an admin), or you can use /ban SteamID if you are using AdminTools)

**ChatLogging:**

You can edit the Config file located in the Config folder called Reporting.json, where you can either turn on certain word logging, or global chat logging (every single message).

The chat will have a somewhat tidy layout with a time-stamp and a playername.

This setting is off by default.

**Config File:

````

{

  "Keywords": [

  "hack",

  "admin",

  "exploit",

  "glitch",

  "aimbot"

  ],

  "LogAllChat": false,

  "LogKeywords": false

}

 
````

**

**Note:**

A full list of reports can be found in data\Reporting\ called Reports.

Which is also where the ChatLog can be found.


When using Chat Logging, either use LogKeywords or LogAllChat, do **not** have both set to true or you will be logging keyword messages twice.

**Connection Notice:**

You will be notified of any Unread reports when you connect to the server.