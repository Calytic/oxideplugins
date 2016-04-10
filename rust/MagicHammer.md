Magic Hammer is a simple plugin that allows users with permission to use the hammer as more than just a building tool.


It can turn the hammer into a one-hit repair tool, or a one hit destroy tool.

**Default Configuration:**

````
{

  "bDestroyCupboardCheck": true,

  "bMessagesEnabled": true,

  "bUsePopupNotifications": false,

  "iProtocol": 1324,

  "tHammerEnabled": "Status: {hammer_status}.",

  "tHammerMode": "You have switched to: {hammer_mode} mode.",

  "tHammerModeText": "Choose your mode: 1 = <color=#2EFE64>repair</color>, 2 = <color=#FF4000>destroy</color>.",

  "tMessageDestroyed": "Entity: <color=#F2F5A9>{entity_name}</color> <color=#FF4000>destroyed</color>.",

  "tMessageRepaired": "Entity: <color=#F2F5A9>{entity_name}</color> health <color=#2EFE64>updated</color> from <color=#FF4000>{current_hp}</color>/<color=#2EFE64>{new_hp}</color>.",

  "tMessageUsage": "/mh <enabled/mode>.",

  "tNoAccessCupboard": "You <color=#FF4000>don't</color> have access to all the tool cupboards around you."

}
````


**Commands:**

/mh

**To-do:**

Make it detect if user has the amount it would cost to repair to maximum, if so then remove that amount from inventory.


Right now this would be best used on PVE servers, or administrators only as it doesn't charge anything to repair currently.

**Permission:**

````
can.mh

grant user "Name" can.mh
````