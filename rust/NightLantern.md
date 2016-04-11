NightLantern automatically turns ON lanterns after sunset and then turns them OFF after sunrise.

(It doesn't require wood in lanterns to function ).


Sunset and sunrise hours can be set in configuration file, default values are 7 and 18, now is also possible to disable automatic control of lanterns completely, giving you the possibility to control them via chat command.

You can now choose to include Jack-o'-lanterns and campfires in the "turn on/off" cycle.

**CONFIGURATION FILE:**

````

{

  "Settings": {

    "AutoTurnLanterns": true,

    "includeCampfires": false,

    "includeJackOLanterns": true,

    "sunriseHour": "7",

    "sunsetHour": "18"

  }

}

 
````


**CHAT COMMANDS:**

The plugin also includes a chat command to manually turn ON or OFF all lanterns:

**/lant <ON|OFF>** -> Turn lanterns ON or OFF


*Chat commands require permission "CanControlLanterns" assigned by default to admins.

**PERMISSIONS:**

Only the permission **CanControlLanterns** is used at the moment.

This is automatically assigned to admins, you can revoke it or assign it to groups/users you want through Oxide permission system.