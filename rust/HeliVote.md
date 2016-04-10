Simple plugin to give players the ability to open a vote to call helicopter(s)
**

Chat Commands**
'/helivote open XX' - Will open a vote to call helicopters, with optional amount of helicopters XX
'/helivote yes' - Votes yes
'/helivote no' - Votes no

**Console Commands**

'helivote XX' - Opens a vote for xx amount of helicopters

**Permissions**

helivote.use - The permission needed when "Use permission system only" is set to true

**Config**

````

{

  "Options - Display vote progress": true,

  "Options - Maximum helicopters to call": 4,

  "Options - Required yes vote percentage": 0.5,

  "Options - Timers - Minimum time between votes (minutes)": 5,

  "Options - Timers - Open vote timer (minutes)": 4,

  "Options - Send helicopters to the initiator": false, // sends the helicopters to the initiator of the vote

  "Options - Use permission system only": false

}

 
````