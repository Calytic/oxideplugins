Collider Count can track your server's current colliders and work out an estimated amount of days till you will need to wipe, based on the average amount of colliders being added to your server daily.


To get as close an estimate as possible I ran a test server of a map, seed 24222 chosen at random, at sizes 1000, 2000, 3000, 4000, 5000, 6000, 7000 and 8000. I did this to get the starting collider count (intital colliders) on that server as a fresh map. Then I worked out the average amount of colliders per sq km on each map. There were varying results but on average most sizes gave a result of 1400-1600 per sq km.


The plugin takes your actual colliders - initial colliders / days since your last wipe giving the average colliders added per day. Then it takes the max colliders - actual colliders - inital colliders / average colliders per day, to work out a best guess, or estimate, of how many days until you may need to wipe.


Of course this isn't 100% accurate but it should be a good indication both for server admins and the players. The estimated days may go up or down depending on activity on your server.


If it's day 0 of your wipe the plugin will not attempt the calculation and instead print the message "We only just recently wiped and cannot estimate next wipe yet!".


Chat Command:

/wipeinfo - All players can do this at anytime to check the info for themselves


Console Command:

wipeinfo - Displays the same as /wipeinfo but to the whole server


Config:


{

  "Settings": {

  "LastWipe": "17.8.2015",

  "MaxColliders": 270000

  }

}


Ensure you enter your own info into the config, date format is day.month.year


Example 17th August 2015 should be entered as 17.8.2015


MaxColliders can be edited to a value of your own, 270000 is the current official Unity limit, but many get issues before that number, personally I use 260000 having hit 264000 before serious performance issues.


OPTIONAL: Use with PaiN's AutoExecute plugin to have the console command execute at timed intervals on your server! [http://oxidemod.org/plugins/timedexecute.919/](http://oxidemod.org/plugins/timedexecute.919/)


Finally, thankyou for a great deal of assistance in this plugin from Mughisi who's patience knows no bounds as I'm learning C# and also to PaiN who provided advice and a few fixes too.


To Do:


* Have the console command execute the code to a GUI popup at the side of the screen
* Your suggestions
* Customised colour settings