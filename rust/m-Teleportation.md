**Latest config:**

````
{

  "Settings": {

    "InterruptTPOnHurt": true,

    "HomesEnabled": true,

    "ChatName": "Teleportation",

    "ConfigVersion": "1.4.15",

    "TPREnabled": true

  },

  "AdminTP": {

    "AnnounceTeleportToTarget": false,

    "TeleportNearDefaultDistance": 30,

    "UseableByModerators": true,

    "LocationRadius": 25

  },

  "Homes": {

    "Countdown": 5,

    "UseFriendsAPI": true,

    "CheckFoundationForOwner": true,

    "LocationRadius": 25,

    "ForceOnTopOfFoundation": true,

    "Cooldown": 300,

    "DailyLimit": 5,

    "HomesLimit": 2

  },

  "TPR": {

    "Countdown": 5,

    "RequestDuration": 30,

    "DailyLimit": 5,

    "BlockTPAOnCeiling": true,

    "Cooldown": 300

  },

  "Messages": {

    "AdminTPTargetCoordinatesTarget": "{admin} teleported you to {coordinates}!",

    "HomeList": "The following homes are available:",

    "SyntaxConsoleCommandToPos": [

      "A Syntax Error Occurred!",

      "You can only use the teleport.topos console command as follows:",

      " > teleport.topos \"player\" x y z"

    ],

    "AdminTPLocation": "You teleported to {location}!",

    "TPRLimitReached": "You have reached the daily limit of {limit} teleport requests today!",

    "LocationNotFound": "Couldn't find a location with that name!",

    "Accept": "{player} has accepted your teleport request! Teleporting in {countdown} seconds!",

    "SyntaxCommandTPSave": [

      "A Syntax Error Occurred!",

      "You can only use the /tpsave command as follows:",

      "/tpsave <location name> - Saves your current position as 'location name'."

    ],

    "Request": "You've requested a teleport to {player}!",

    "TimedOutTarget": "You did not answer {player}'s teleport request in time!",

    "CantTeleportPlayerToSelf": "You can't teleport a player to himself!",

    "SyntaxCommandListHomes": [

      "A Syntax Error Occurred!",

      "You can only use the /listhomes command as follows:",

      "/listhomes - Shows you a list of all your saved home locations."

    ],

    "AdminTPBoundaries": "X and Z values need to be between -{boundary} and {boundary} while the Y value needs to be between -100 and 2000!",

    "AdminTPBack": "You've teleported back to your previous location!",

    "InvalidCoordinates": "The coordinates you've entered are invalid!",

    "HomeSave": "You have saved the current location as your home!",

    "TargetDisconnected": "{player} has disconnected, your teleport was cancelled!",

    "TeleportPendingTarget": "You can't request a teleport to someone who's about to teleport!",

    "InterruptedTarget": "{player}'s teleport was interrupted!",

    "HomeExistsNearby": "A home location with the name {name} already exists near this position!",

    "AdminTPLocationSave": "You have saved the current location!",

    "HomeFoundationNotOwned": "You can't set your home on someone else's house.",

    "HomesListWiped": "You have wiped all the saved home locations!",

    "HomeTPStarted": "Teleporting to your home {home} in {countdown} seconds!",

    "SyntaxCommandTP": [

      "A Syntax Error Occurred!",

      "You can only use the /tp command as follows:",

      "/tp <targetplayer> - Teleports yourself to the target player.",

      "/tp <player> <targetplayer> - Teleports the player to the target player.",

      "/tp <x> <y> <z> - Teleports you to the set of coordinates.",

      "/tp <player> <x> <y> <z> - Teleports the player to the set of coordinates."

    ],

    "HomeTPLimitReached": "You have reached the daily limit of {limit} teleports today!",

    "TPHelp": {

      "General": [

        "Please specify the module you want to view the help of. ",

        "The available modules are: "

      ],

      "tpr": [

        "With these commands you can request to be teleported to a player or accept someone else's request:",

        "/tpr <player name> - Sends a teleport request to the player.",

        "/tpa - Accepts an incoming teleport request."

      ],

      "home": [

        "With the following commands you can set your home location to teleport back to:",

        "/sethome <home name> - Saves your current position as the location name.",

        "/listhomes - Shows you a list of all the locations you have saved.",

        "/removehome <home name> - Removes the location of your saved homes.",

        "/home <home name> - Teleports you to the home location."

      ],

      "admintp": [

        "As an admin you have access to the following commands:",

        "/tp <targetplayer> - Teleports yourself to the target player.",

        "/tp <player> <targetplayer> - Teleports the player to the target player.",

        "/tp <x> <y> <z> - Teleports you to the set of coordinates.",

        "/tpl - Shows a list of saved locations.",

        "/tpl <location name> - Teleports you to a saved location.",

        "/tpsave <location name> - Saves your current position as the location name.",

        "/tpremove <location name> - Removes the location from your saved list.",

        "/tpb - Teleports you back to the place where you were before teleporting."

      ]

    },

    "Interrupted": "Your teleport was interrupted!",

    "HomeTPCooldown": "Your teleport is currently on cooldown. You'll have to wait {time} for your next teleport.",

    "AdminLocationListEmpty": "You haven't saved any locations!",

    "AcceptTarget": "You've accepted the teleport request of {player}!",

    "SyntaxCommandTPN": [

      "A Syntax Error Occurred!",

      "You can only use the /tpn command as follows:",

      "/tpn <targetplayer> - Teleports yourself the default distance behind the target player.",

      "/tpn <targetplayer> <distance> - Teleports you the specified distance behind the target player."

    ],

    "TeleportPending": "You can't initiate another teleport while you have a teleport pending!",

    "HomeExists": "You have already saved a home location by this name!",

    "AdminTPTargetCoordinates": "You teleported {player} to {coordinates}!",

    "HomeNotFound": "Couldn't find your home with that name!",

    "AdminTPCoordinates": "You teleported to {coordinates}!",

    "HomeTP": "You teleported to your home '{home}'!",

    "TPSettings": {

      "home": [

        "Home System as the current settings enabled: ",

        "Time between teleports: {cooldown}",

        "Daily amount of teleports: {limit}",

        "Amount of saved Home locations: {amount}"

      ],

      "tpr": [

        "TPR System as the current settings enabled: ",

        "Time between teleports: {cooldown}",

        "Daily amount of teleports: {limit}"

      ],

      "General": [

        "Please specify the module you want to view the settings of. ",

        "The available modules are: "

      ]

    },

    "AcceptOnRoof": "You can't accept a teleport while you're on a ceiling, get to ground level!",

    "RequestTarget": "{player} requested to be teleported to you! Use '/tpa' to accept!",

    "SyntaxCommandTPR": [

      "A Syntax Error Occurred!",

      "You can only use the /tpr command as follows:",

      "/tpr <player name> - Sends out a teleport request to 'player name'."

    ],

    "LocationExistsNearby": "A location with the name {name} already exists near this position!",

    "AdminTP": "You teleported to {player}!",

    "SyntaxCommandTPRemove": [

      "A Syntax Error Occurred!",

      "You can only use the /tpremove command as follows:",

      "/tpremove <location name> - Removes the location with the name 'location name'."

    ],

    "SyntaxCommandRemoveHome": [

      "A Syntax Error Occurred!",

      "You can only use the /removehome command as follows:",

      "/removehome <home name> - Removes the home location with the name 'location name'."

    ],

    "SyntaxCommandHome": [

      "A Syntax Error Occurred!",

      "You can only use the /home command as follows:",

      "/home <home name> - Teleports yourself to your home with the name 'home name'."

    ],

    "LocationExists": "A location with this name already exists at {location}!",

    "HomeMaxLocations": "Unable to set your home here, you have reached the maximum of {amount} homes!",

    "AdminTPConsoleTP": "You were teleported to {destination}",

    "AdminTPPlayerTarget": "{admin} teleported {player} to you!",

    "AdminTPPlayers": "You teleported {player} to {target}!",

    "SyntaxCommandSetHome": [

      "A Syntax Error Occurred!",

      "You can only use the /sethome command as follows:",

      "/sethome <home name> - Saves the current location as your home with the name 'home name'."

    ],

    "TPRCooldown": "Your teleport requests are currently on cooldown. You'll have to wait {time} to send your next teleport request.",

    "InvalidCharacter": "You have used an invalid character, please limit yourself to the letters a to z and numbers.",

    "PlayerNotFound": "The specified player couldn't be found please try again!",

    "AdminTPTarget": "{player} teleported to you!",

    "InvalidHelpModule": "Invalid module supplied!",

    "MultiplePlayersFound": "Found multiple players with that name!",

    "SuccessTarget": "{player} teleported to you!",

    "AdminLocationList": "The following locations are available:",

    "AdminTPBackSave": "Your previous location has been saved, use /tpb to teleport back!",

    "PendingRequest": "You already have a request pending, cancel that request or wait until it gets accepted or times out!",

    "HomeFoundationNotFriendsOwned": "You need to be in your own or in a friend's house to set your home!",

    "SyntaxCommandTPA": [

      "A Syntax Error Occurred!",

      "You can only use the /tpa command as follows:",

      "/tpa - Accepts an incoming teleport request."

    ],

    "HomeSaveFoundationOnly": "You can only save a home location on a foundation!",

    "Success": "You teleported to {player}!",

    "HomeListEmpty": "You haven't saved any homes!",

    "SyntaxConsoleCommandToPlayer": [

      "A Syntax Error Occurred!",

      "You can only use the teleport.toplayer console command as follows:",

      " > teleport.toplayer \"player\" \"target player\""

    ],

    "AdminTPPlayer": "{admin} teleported you to {player}!",

    "NoPendingRequest": "You have no pending teleport request!",

    "AdminTPConsoleTPPlayer": "You were teleported to {player}",

    "PendingRequestTarget": "The player you wish to teleport to already has a pending request, try again later!",

    "NoPreviousLocationSaved": "No previous location saved!",

    "AdminTPOutOfBounds": "You tried to teleport to a set of coordinates outside the map boundaries!",

    "AdminTPLocationRemove": "You have removed the location {location}!",

    "SyntaxCommandTPL": [

      "A Syntax Error Occurred!",

      "You can only use the /tpl command as follows:",

      "/tpl - Shows a list of saved locations.",

      "/tpl <location name> - Teleports you to a saved location."

    ],

    "CantTeleportToSelf": "You can't teleport to yourself!",

    "TimedOut": "{player} did not answer your request in time!",

    "HomeRemove": "You have removed your home {home}!"

  }

}
````