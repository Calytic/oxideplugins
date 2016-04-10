The plugin allows you to change the time to any hour (0 to 24) with an in-game command or a console command, please note that it is impossible to call the update function at this time so it will take a few seconds before the time update reflects in the game.


With the commands to freeze time you can change your server to constant day or night. And when you want the time to run again you use the unfreeze command as listed below.


The commands /setdaylength and /setnightlength will allow you to have a modified day & night cycle, so instead of having it 50/50 day & night you can have for example 50 minutes of daytime and 10 minutes of night. Keep in mind that you need to set both for this to work and time cannot be frozen 
In-game commands:

````
Shows the current time info:

/settime


Modifies the time:

/settime <hour (0-24)>


Freeze the time:

/freezetime


Unfreeze the time:

/unfreezetime


Set the day length:

/setdaylength <length in minutes (5-720)>


Set the night length:

/setnightlength <length in minutes (5-720)>
````

Console commands:

````
Shows the current time info:

env.time


Modifies the time:

env.time <hour (0-24)>


Freeze the time:

env.freeze


Unfreeze the time:

env.unfreeze


Set the day length:

env.daylength <length in minutes (5-720)>


Set the night length:

env.nightlength <length in minutes (5-720)>
````