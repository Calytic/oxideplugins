Have you wanted your users to be able to view the time? What about you wanting to be able to set the time without typing env.time in console? Well I did and the plugins on here about time did not satisfy how i wanted it so i made my own.
**

Commands:**
/time - Displays the time.
/time help - Displays a ingame syntax help.
/time (1-24) - Sets the time to the define number.
/time freeze (1-23) - Freezes the time at the number given.
/time freeze - Freezes the time at the current time.
/time unfreeze - Unfreezes the time.
/day - Sets the time to 6am.
/night - Sets the time to 6pm.

**Permission: **timeset.use

**Hooks:**
(string)TimeSet?.Call("whatisthetime");

This returns the time with a meridiem(am/pm)

**Language File:**

````
["time_get"] = "The current time is {0}.",

["time_noperm"] = "You do not have the TimeSet.use permission!",

["time_help"] = "/time - Tells what time it is.\n/time help - Bring up this menu.\n/time <0-24/day/night/freeze/unfreeze> - Sets the time or freezes time if you have the permission.\n/day - Sets the time to 6am if you have permission.\n/night - Sets the time to 7:30pm if you have permission.",

["time_error"] = "{0} is not a valid parameter!",

["time_freeze"] = "{1} has frooze the time to {0}.",

["time_freezeerror"] = "Please enter a valid time to freeze.",

["time_unfreeze"] = "{1} has unfrooze the time.",

["time_set"] = "{1} has set the time to {0}."
````


**Planned:**

Saving data such as freeze time etc.

Day/night Length changing