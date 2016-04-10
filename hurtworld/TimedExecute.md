It's a very simple plugin that can Execute many commands every (X) seconds!

**NOTE(CONFIG): **For the TimerOnce configuration always at the end u must execute the command(Only if you want to start again from the top to the bottom of the TimerOnce chain "reset.oncetimer" **ALSO **remember that this **TIMER IS WORKING UNTIL THE RESET COMMAND.

NOTE2: You can see in my config the command (for the TimerOnce) will get executed after 60 seconds so the second command will get executed after 60 seconds too! (60+60=120) this timer is a chain so u have to add an additional seconds for the second command!

NOTE(RealTime-Timer): **The time **MUST** look like "HH:mm:ss" ex. "18:30:00".

**Default Configuration**

````
{

  "EnabledRealTime-Timer": true,

  "EnableTimerOnce": true,

  "EnableTimerRepeat": true,

  "RealTime-Timer": {

    "16:00:00": "adminmessage 'The gate for the event is open!'",

    "16:00:10": "settime 10",

    "16:30:00": "adminmessage 'The gate for the event just closed'",

    "17:00:00": "adminmessage 'Restart in 1 HOUR'",

    "18:00:00": "adminmessage 'The server is restarting NOW.'"

  },

  "TimerOnce": {

    "adminmessage 'Dont forget to like our fanpage!'": 60,

    "adminmessage 'Follow us on Twitter!'": 120,

    "adminmessage 'You can donate via PayPal!'": 180

  },

  "TimerRepeat": {

    "saveserver": 300

  }

}
````