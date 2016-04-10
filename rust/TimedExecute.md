Its a very simple plugin that can Execute many commands every (X) seconds! Also it's my first plugin and without the help of Reneb and Wulfspider i think i couldn't make this plugin.

**SOME READY CONFIGURATIONS:(OUTDATED)**
[TimedExecute for Rust Experimental - FAQ | Oxide](http://oxidemod.org/plugins/timedexecute.919/field?field=faq)

**How this plugin can be used:**


* If you want to throw 5 airdrops every 5 mins you can set in config (for the airdrop.massdrop 5 command i'm using [Airdrop Control](http://oxidemod.org/plugins/airdrop-controller.804/) from Reneb)


**NOTE(CONFIG): **For the TimerOnce configuration always at the end u must execute the command(Only if you want to start again from the top to the bottom of the TimerOnce chain "reset.oncetimer" **ALSO **remember that this **TIMER IS WORKING UNTILL THE RESET COMMAND.

NOTE2: You can see in my config the command (for the TimerOnce) will get executed after 60 seconds so the second command will get executed after 60 seconds too! (60+60=120) this timer is a chain so u have to add an additional seconds for the second command!

NOTE(RealTime-Timer): **The time **MUST** look like "HH:mm:ss" ex. "18:30:00".
**

Default Config:**

````

{

  "EnabledRealTime-Timer": true,

  "EnableTimerOnce": true,

  "EnableTimerRepeat": true,

  "RealTime-Timer": {

    "16:00:00": "say 'The gate for the event is open!'",

    "16:30:00": "say 'The gate for the event just closed'",

    "17:00:00": "say 'Restart in 1 HOUR'",

    "18:00:00": "say 'The server is restarting NOW.'"

  },

  "TimerOnce": {

    "reset.oncetimer": 181,

    "say 'Dont forget to like our fanpage!'": 60,

    "say 'Follow us on Twitter!'": 120,

    "say 'You can donate via PayPal!'": 180

  },

  "TimerRepeat": {

    "event.run": 300,

    "server.save": 300

  }

}

 
````

That's pretty much all! Again thanks to Reneb and Wulfspider!