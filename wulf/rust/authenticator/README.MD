**Description** 

This plugin allows your players to register their name to their Steam account. If someone uses a name with another steam account than the one that registered the name, he will be forced to identify or else will be kicked.

**Usage** 

/auth register <password> - registers the name to the steam account

/auth unregister <password> - removes the registration

/auth identify <password> - allows you to identify your name if yoou're logging in with another steam account

/authhelp - shows some info about the functionality

**Default coonfig** 

````
{

  "Messages": {

    "NameAlreadyRegistered": "The name you're using is registered to another steam account. Please identify by using {command}. You have {time} seconds before getting kicked",

    "SuccessfullRegistered": "Your name is now registered to your steam account",

    "NoIdentifyNeeded": "You dont need to identify on this account",

    "NotRegistered": "You dont have registered a name yet",

    "NotifyOnConnect": "You can register your name on this server by using {command} so nobody can pretend to be you",

    "SuccessfullUnregistered": "You have successfully unregistered your name",

    "WrongPassword": "Wrong password!",

    "KickMessage": "You didnt identify your name. Please choose a different one",

    "RegisteredAnotherNameAlready": "Your steam account has already registered the name {name}. Use {command} to unregister",

    "SuccessfullIdentified": "Successfully identified"

  },

  "Settings": {

    "NotifyOnConnect": "true",

    "LogToConsole": "false",

    "TimeToIdentify": 40

  }

}
````

Settings

NotifyOnConnect - sends a message on player connect to notify them about the register function

LogToConsole - shows when someone register/unregister/identify in the console

TimeToIdentify - seconds until someone gets kicked when not identified