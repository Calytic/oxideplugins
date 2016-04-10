
-- ----------------------------------------------------------------------------
-- Teleportation System                                          Version 1.4.22
-- ----------------------------------------------------------------------------
-- Filename:          m-Teleportation.lua
-- Last Modification: 10-18-2015
-- ----------------------------------------------------------------------------
-- Description:
-- This plugin is developed for Rust servers with the Oxide Server Mod and 
-- offers the following teleportation methods: Admin Teleports, Homes and TPR.
-- ----------------------------------------------------------------------------

PLUGIN.Title       = "Teleportation System"
PLUGIN.Description = "Multiple teleportation systems for admins and players."
PLUGIN.Version     = V( 1, 4, 23)
PLUGIN.HasConfig   = true
PLUGIN.Author      = "Mughisi"
PLUGIN.ResourceId  = 660

-- ----------------------------------------------------------------------------
-- Globals
-- ----------------------------------------------------------------------------
-- Some globals that are used in multiple functions.
-- ----------------------------------------------------------------------------
local PendingRequests = {}
local PlayersRequests = {}
local TeleportTimers  = {}

local TeleportData    = {}

local FriendsAPI      = nil

local OverlapSphere   = UnityEngine.Physics.OverlapSphere.methodarray[2]
local RaycastAll      = UnityEngine.Physics.RaycastAll["methodarray"][7]
local Raycast         = UnityEngine.Physics.Raycast.methodarray[12]
local LayerMask       = 2097152 -- Construction layer mask

-- ----------------------------------------------------------------------------
-- PLUGIN:Init()
-- ----------------------------------------------------------------------------
-- On plugin initialisation the required in-game chat commands are registered
-- and data from the DataTable file is loaded. Also running a temporary player
-- check to prevent bugs with `player.transform.position` after reloading the
-- plugin.
-- ----------------------------------------------------------------------------
function PLUGIN:Init()
    -- Add the chat commands for the Admin TP System:
    command.AddChatCommand( "tp",       self.Plugin, "cmdTeleport" )
    command.AddChatCommand( "tpl",      self.Plugin, "cmdTeleportLocation" )
    command.AddChatCommand( "tpsave",   self.Plugin, "cmdSaveTeleportLocation" )
    command.AddChatCommand( "tpremove", self.Plugin, "cmdRemoveTeleportLocation" )
    command.AddChatCommand( "tpb",      self.Plugin, "cmdTeleportBack" )
    command.AddChatCommand( "tpn",      self.Plugin, "cmdTeleportNear" )

    -- Add the chat commands for the Homes System:
    command.AddChatCommand( "sethome",    self.Plugin, "cmdSetHome" )
    command.AddChatCommand( "removehome", self.Plugin, "cmdRemoveHome" )
    command.AddChatCommand( "home",       self.Plugin, "cmdTeleportHome" )
    command.AddChatCommand( "listhomes",  self.Plugin, "cmdListHomes" )

    -- Add the chat commands for the TPR System:
    command.AddChatCommand( "tpr", self.Plugin, "cmdTeleportRequest" )
    command.AddChatCommand( "tpa", self.Plugin, "cmdTeleportAccept" )

    -- Add the Help commands:
    command.AddChatCommand( "tphelp",   self.Plugin, "cmdTeleportHelp" )
    command.AddChatCommand( "tplimits", self.Plugin, "cmdTeleportLimits" )

    -- Add the admin wipe command to remove all saved homes.
    command.AddChatCommand( "wipehomes", self.Plugin, "cmdWipeHomes" )

    -- Add the Console commands:
    command.AddConsoleCommand("teleport.toplayer", self.Plugin, "ccmdTeleport")
    command.AddConsoleCommand("teleport.topos", self.Plugin, "ccmdTeleport")

    -- Load the teleport datatable file.
    self:LoadSavedData()

    -- Check if the configuration file is up to date.
    if self.Config.Settings.ConfigVersion ~= "1.4.19" then
        -- The configuration file needs an update.
        self:UpdateConfig()
    end
end

-- ----------------------------------------------------------------------------
-- PLUGIN:OnServerInitialized()
-- ----------------------------------------------------------------------------
-- When the server has finished the startup process we will check if the 
-- required API plugins are available and get a few variables.
-- ----------------------------------------------------------------------------
function PLUGIN:OnServerInitialized()
    -- Check if the CheckFoundationForOwner configuration value is set to true.
    -- If it is true we need the plugin `Building Owners`.
    if self.Config.Homes.CheckFoundationForOwner then
        -- The setting CheckFoundationForOwner is true, check if
        -- `Building Owners` is installed.
        if not plugins.Exists( "BuildingOwners" ) then
            -- The plugin isn't installed, print a message in the console and
            -- disable the owner check.
            print( "m-Teleportation: To limit Homes to only be set on foundations owned by the player the plugin `Building Owners` is required!" )
            print( "                 This plugin can be downloaded at http://forum.rustoxide.com/plugins/building-owners.682/" )
            print( "                 The owner check has temporarily been disabled to prevent problems." )
            self.Config.Homes.CheckFoundationForOwner = false
        else		
	            BuildingOwners = plugins.Find( "BuildingOwners" )
		end
    end
    
    -- Check if the UseFriendsAPI configuration value is set to true. If it  is
    -- true we need the plugin `FriendsAPI`.
    if self.Config.Homes.UseFriendsAPI then
        -- The setting CheckFoundationForOwner is true, check if `Friends API`
        -- is installed.
        if not plugins.Exists( "0friendsAPI" ) then
            -- The plugin isn't installed, print a message in the console and
            -- disable the owner check.
            print( "m-Teleportation: To allow Homes to be set on foundations owned by friends of the player the plugin `Friends API` is required!" )
            print( "                 This plugin can be downloaded at http://forum.rustoxide.com/plugins/friends-api.686/" )
            print( "                 The friends check has temporarily been disabled to prevent problems." )
            self.Config.Homes.UseFriendsAPI = false
        else
            -- Grab the plugin.
            FriendsAPI = plugins.Find( "0friendsAPI" )
        end
    end
    
    -- "Fix" for the GetCurrentTime() problem that is causing it to throw an 
    -- "AmibiguousMatchException: Ambiguous matching in method resolution"
    -- error when called for the first time.
    status, err = pcall( time.GetCurrentTime )
    date = time.GetCurrentTime():ToString( "d" )
end

-- ----------------------------------------------------------------------------
-- PLUGIN:LoadDefaultConfig()
-- ----------------------------------------------------------------------------
-- The plugin uses a configuration file to save certain settings and uses it
-- for localized messages that are send in-game to the players. When this file
-- doesn't exist a new one will be created with these default values.
-- ----------------------------------------------------------------------------
function PLUGIN:LoadDefaultConfig()
    -- General Settings:
    self.Config.Settings = {
        ChatName          = "Teleportation",
        ConfigVersion     = "1.4.19",
        HomesEnabled      = true,
        TPREnabled        = true,
        InterruptTPOnHurt = true
    }

    -- Admin TP System Settings:
    self.Config.AdminTP = {
        AnnounceTeleportToTarget    = false,
        UseableByModerators         = true,
        LocationRadius              = 25,
        TeleportNearDefaultDistance = 30
    }
    
    -- Homes System Settings:
    self.Config.Homes = {
        HomesLimit              = 2,
        Cooldown                = 600,
        Countdown               = 15,
        DailyLimit              = 5,
        LocationRadius          = 25,
        ForceOnTopOfFoundation  = true,
        CheckFoundationForOwner = true,
        UseFriendsAPI           = true
    }

    -- TPR System Settings: 
    self.Config.TPR = {
        Cooldown          = 600,
        Countdown         = 15,
        DailyLimit        = 5,
        RequestDuration   = 30,
        BlockTPAOnCeiling = true
    }

    -- Plugin Messages:
    self.Config.Messages = {
        -- Admin TP System:
        AdminTP                        = "You teleported to {player}!",
        AdminTPTarget                  = "{player} teleported to you!",
        AdminTPPlayers                 = "You teleported {player} to {target}!",
        AdminTPPlayer                  = "{admin} teleported you to {player}!",
        AdminTPPlayerTarget            = "{admin} teleported {player} to you!",
        AdminTPCoordinates             = "You teleported to {coordinates}!",
        AdminTPTargetCoordinates       = "You teleported {player} to {coordinates}!",
        AdminTPOutOfBounds             = "You tried to teleport to a set of coordinates outside the map boundaries!",
        AdminTPBoundaries              = "X and Z values need to be between -{boundary} and {boundary} while the Y value needs to be between -100 and 2000!",
        AdminTPLocation                = "You teleported to {location}!",
        AdminTPLocationSave            = "You have saved the current location!",
        AdminTPLocationRemove          = "You have removed the location {location}!",
        AdminLocationList              = "The following locations are available:",
        AdminLocationListEmpty         = "You haven't saved any locations!",
        AdminTPBack                    = "You've teleported back to your previous location!",
        AdminTPBackSave                = "Your previous location has been saved, use /tpb to teleport back!",
        AdminTPTargetCoordinatesTarget = "{admin} teleported you to {coordinates}!",
        AdminTPConsoleTP               = "You were teleported to {destination}",
        AdminTPConsoleTPPlayer         = "You were teleported to {player}",

        -- Homes System:
        HomeTP                        = "You teleported to your home '{home}'!",
        HomeSave                      = "You have saved the current location as your home!",
        HomeSaveFoundationOnly        = "You can only save a home location on a foundation!",
        HomeFoundationNotOwned        = "You can't set your home on someone else's house.",
        HomeFoundationNotFriendsOwned = "You need to be in your own or in a friend's house to set your home!",
        HomeRemove                    = "You have removed your home {home}!",
        HomeList                      = "The following homes are available:",
        HomeListEmpty                 = "You haven't saved any homes!",
        HomeMaxLocations              = "Unable to set your home here, you have reached the maximum of {amount} homes!",
        HomeTPStarted                 = "Teleporting to your home {home} in {countdown} seconds!",
        HomeTPCooldown                = "Your teleport is currently on cooldown. You'll have to wait {time} for your next teleport.",
        HomeTPLimitReached            = "You have reached the daily limit of {limit} teleports today!",
        HomesListWiped                = "You have wiped all the saved home locations!",
        HomesBuildingBlocked          = "You can't set your home if you are not allowed to build in this zone!",

        -- TPR System:
        Request              = "You've requested a teleport to {player}!",
        RequestTarget        = "{player} requested to be teleported to you! Use '/tpa' to accept!",
        PendingRequest       = "You already have a request pending, cancel that request or wait until it gets accepted or times out!",
        PendingRequestTarget = "The player you wish to teleport to already has a pending request, try again later!",
        NoPendingRequest     = "You have no pending teleport request!",
        AcceptOnRoof         = "You can't accept a teleport while you're on a ceiling, get to ground level!",
        Accept               = "{player} has accepted your teleport request! Teleporting in {countdown} seconds!",
        AcceptTarget         = "You've accepted the teleport request of {player}!",
        Success              = "You teleported to {player}!",
        SuccessTarget        = "{player} teleported to you!",
        TimedOut             = "{player} did not answer your request in time!",
        TimedOutTarget       = "You did not answer {player}'s teleport request in time!",
        TargetDisconnected   = "{player} has disconnected, your teleport was cancelled!",
        TPRCooldown          = "Your teleport requests are currently on cooldown. You'll have to wait {time} to send your next teleport request.",
        TPRLimitReached      = "You have reached the daily limit of {limit} teleport requests today!",
        TPRBuildingBlockedA  = "You can't accept a TPR while in a building blocked zone!",
        TPRBuildingBlockedT  = "Teleport aborted because your target moved into a building blocked zone!",

        -- General Messages:
        Interrupted          = "Your teleport was interrupted!",
        InterruptedTarget    = "{player}'s teleport was interrupted!",

        -- Help Messages:
        TPHelp = {
            General = {
                "Please specify the module you want to view the help of. ",
                "The available modules are: ",
            },
            admintp = {
                "As an admin you have access to the following commands:",
                "/tp <targetplayer> - Teleports yourself to the target player.",
                "/tp <player> <targetplayer> - Teleports the player to the target player.",
                "/tp <x> <y> <z> - Teleports you to the set of coordinates.",
                "/tpl - Shows a list of saved locations.",
                "/tpl <location name> - Teleports you to a saved location.",
                "/tpsave <location name> - Saves your current position as the location name.",
                "/tpremove <location name> - Removes the location from your saved list.",
                "/tpb - Teleports you back to the place where you were before teleporting."
            },

            home = {
                "With the following commands you can set your home location to teleport back to:",
                "/sethome <home name> - Saves your current position as the location name.",
                "/listhomes - Shows you a list of all the locations you have saved.",
                "/removehome <home name> - Removes the location of your saved homes.",
                "/home <home name> - Teleports you to the home location."
            },
            tpr = {
                "With these commands you can request to be teleported to a player or accept someone else's request:",
                "/tpr <player name> - Sends a teleport request to the player.",
                "/tpa - Accepts an incoming teleport request."
            }
        },

        -- Settings Messages:
        TPSettings = {
            General = {
                "Please specify the module you want to view the settings of. ",
                "The available modules are: ",
            },
            home = {
                "Home System as the current settings enabled: ",
                "Time between teleports: {cooldown}",
                "Daily amount of teleports: {limit}",
                "Amount of saved Home locations: {amount}"
            },
            tpr = {
                "TPR System as the current settings enabled: ",
                "Time between teleports: {cooldown}",
                "Daily amount of teleports: {limit}"
            }
        },

        -- Error Messages:
        PlayerNotFound           = "The specified player couldn't be found please try again!",
        MultiplePlayersFound     = "Found multiple players with that name!",
        CantTeleportToSelf       = "You can't teleport to yourself!",
        CantTeleportPlayerToSelf = "You can't teleport a player to himself!",
        TeleportPending          = "You can't initiate another teleport while you have a teleport pending!",
        TeleportPendingTarget    = "You can't request a teleport to someone who's about to teleport!",
        LocationExists           = "A location with this name already exists at {location}!",
        LocationExistsNearby     = "A location with the name {name} already exists near this position!",
        LocationNotFound         = "Couldn't find a location with that name!",
        NoPreviousLocationSaved  = "No previous location saved!",
        HomeExists               = "You have already saved a home location by this name!",
        HomeExistsNearby         = "A home location with the name {name} already exists near this position!",
        HomeNotFound             = "Couldn't find your home with that name!",
        InvalidCoordinates       = "The coordinates you've entered are invalid!",
        InvalidHelpModule        = "Invalid module supplied!",
        InvalidCharacter         = "You have used an invalid character, please limit yourself to the letters a to z and numbers.",

        -- Syntax Errors Admin TP System:
        SyntaxCommandTP = {
            "A Syntax Error Occurred!",
            "You can only use the /tp command as follows:",
            "/tp <targetplayer> - Teleports yourself to the target player.",
            "/tp <player> <targetplayer> - Teleports the player to the target player.",
            "/tp <x> <y> <z> - Teleports you to the set of coordinates.",
            "/tp <player> <x> <y> <z> - Teleports the player to the set of coordinates."
        },
        SyntaxCommandTPL = {
            "A Syntax Error Occurred!",
            "You can only use the /tpl command as follows:",
            "/tpl - Shows a list of saved locations.",
            "/tpl <location name> - Teleports you to a saved location."
        },
        SyntaxCommandTPSave = {
            "A Syntax Error Occurred!",
            "You can only use the /tpsave command as follows:",
            "/tpsave <location name> - Saves your current position as 'location name'."
        },
        SyntaxCommandTPRemove = {
            "A Syntax Error Occurred!",
            "You can only use the /tpremove command as follows:",
            "/tpremove <location name> - Removes the location with the name 'location name'."
        },
        SyntaxCommandTPN = {
            "A Syntax Error Occurred!",
            "You can only use the /tpn command as follows:",
            "/tpn <targetplayer> - Teleports yourself the default distance behind the target player.",
            "/tpn <targetplayer> <distance> - Teleports you the specified distance behind the target player."
        },

        -- Syntax Errors Home System:
        SyntaxCommandSetHome = {
            "A Syntax Error Occurred!",
            "You can only use the /sethome command as follows:",
            "/sethome <home name> - Saves the current location as your home with the name 'home name'."
        },
        SyntaxCommandRemoveHome = {
            "A Syntax Error Occurred!",
            "You can only use the /removehome command as follows:",
            "/removehome <home name> - Removes the home location with the name 'location name'."
        },
        SyntaxCommandHome = {
            "A Syntax Error Occurred!",
            "You can only use the /home command as follows:",
            "/home <home name> - Teleports yourself to your home with the name 'home name'."
        },
        SyntaxCommandListHomes = {
            "A Syntax Error Occurred!",
            "You can only use the /listhomes command as follows:",
            "/listhomes - Shows you a list of all your saved home locations."
        },

        -- Syntax Errors TPR System:
        SyntaxCommandTPR = {
            "A Syntax Error Occurred!",
            "You can only use the /tpr command as follows:",
            "/tpr <player name> - Sends out a teleport request to 'player name'."
        },
        SyntaxCommandTPA = {
            "A Syntax Error Occurred!",
            "You can only use the /tpa command as follows:",
            "/tpa - Accepts an incoming teleport request."
        },
        SyntaxConsoleCommandToPos = { 
            "A Syntax Error Occurred!", 
            "You can only use the teleport.topos console command as follows:", 
            " > teleport.topos \"player\" x y z" 
        },
        SyntaxConsoleCommandToPlayer = { 
            "A Syntax Error Occurred!", 
            "You can only use the teleport.toplayer console command as follows:", 
            " > teleport.toplayer \"player\" \"target player\"" 
        }
    }
end

-- ----------------------------------------------------------------------------
-- PLUGIN:UpdateConfig()
-- ----------------------------------------------------------------------------
-- Updates the configuration file when required.
-- ----------------------------------------------------------------------------
function PLUGIN:UpdateConfig()
    if self.Config.Settings.ConfigVersion == "1.0.0" then
        -- Change the config version.
        self.Config.Settings.ConfigVersion = "1.1"

        -- Modify the changed config values.
        self.Config.Messages.MultiplePlayersFound = "Found multiple players with that name!"

        -- Send a console message to notify the server owner of this change.
        print( "m-Teleportation: Your config file was updated to config version 1.1, 1 new message was added to the configuration file!" )
    end

    if self.Config.Settings.ConfigVersion == "1.1" then
        -- Change the config version.
        self.Config.Settings.ConfigVersion = "1.1.0"
        
        -- Modify the changed config values.
        table.insert( self.Config.Messages.SyntaxCommandTP, "/tp <player> <x> <y> <z> - Teleports the player to the set of coordinates." )
        self.Config.Messages.SyntaxConsoleCommandToPos      = { "A Syntax Error Occurred!", "You can only use the teleport.topos console command as follows:", " > teleport.topos \"player\" x y z" }
        self.Config.Messages.SyntaxConsoleCommandToPlayer   = { "A Syntax Error Occurred!", "You can only use the teleport.toplayer console command as follows:", " > teleport.toplayer \"player\" \"target player\"" }
        self.Config.Messages.AdminTPTargetCoordinates       = "You teleported {player} to {coordinates}!"
        self.Config.Messages.AdminTPTargetCoordinatesTarget = "{admin} teleported you to {coordinates}!"
        self.Config.Messages.AdminTPConsoleTP               = "You were teleported to {destination}"
        self.Config.Messages.AdminTPConsoleTPPlayer         = "You were teleported to {player}"

        -- Send a console message to notify the server owner of this change.
        print( "m-Teleportation: Your config file was updated to config version 1.1.0, 7 new messages were added to the configuration file!" )
    end

    if self.Config.Settings.ConfigVersion == "1.1.0" then
        -- Change the config version.
        self.Config.Settings.ConfigVersion = "1.2.0"

        -- Modify the changed config values.
        self.Config.Homes.UseFriendsAPI                    = true
        self.Config.Messages.HomeFoundationNotFriendsOwned = "You need to be in your own or in a friend's house to set your home!"
        self.Config.Messages.InvalidCharacter              = "You have used an invalid character, please limit yourself to the letters a to z and numbers.",

        -- Send a console message to notify the server owner of this change.
        print( "m-Teleportation: Your config file was updated to config version 1.2.0, 1 new value and 2 new messages were added to the configuration file!" )
    end

    if self.Config.Settings.ConfigVersion == "1.2.0" then
        -- Change the config version.
        self.Config.Settings.ConfigVersion = "1.2.1"
        
        -- Modify the changed config values.
        self.Config.Messages.AdminTPBoundaries = "X and Z values need to be between -{boundary} and {boundary} while the Y value needs to be between -100 and 2000!"
        
        -- Send a console message to notify the server owner of this change.
        print( "m-Teleportation: Your config file was updated to config version 1.2.1, 1 value was modified!" )
    end
    
    if self.Config.Settings.ConfigVersion == "1.2.1" then
        -- Change the config version.
        self.Config.Settings.ConfigVersion = "1.3.0"

        -- Modify the changed config values.
        self.Config.AdminTP.TeleportNearDefaultDistance = 30
        self.Config.Messages.HomesListWiped = "You have wiped all the saved home locations!"
        self.Config.Messages.SyntaxCommandTPN = {
            "A Syntax Error Occurred!",
            "You can only use the /tpn command as follows:",
            "/tpn <targetplayer> - Teleports yourself the default distance behind the target player.",
            "/tpn <targetplayer> <distance> - Teleports you the specified distance behind the target player."
        }

        -- Send a console message to notify the server owner of this change.
        print( "m-Teleportation: Your config file was updated to config version 1.3.0, 5 new values were added!" )
    end
    
    if self.Config.Settings.ConfigVersion == "1.3.0" then
        -- Change the config version.
        self.Config.Settings.ConfigVersion = "1.4.0"

        -- Send a console message to notify the server owner of this change.
        print( "m-Teleportation: Your config file was updated to config version 1.4.0, version indication was updated!" )
    end
    
    if self.Config.Settings.ConfigVersion == "1.4.0" then
        -- Change the config version.
        self.Config.Settings.ConfigVersion = "1.4.1"

        -- Send a console message to notify the server owner of this change.
        print( "m-Teleportation: Your config file was updated to config version 1.4.1, version indication was updated!" )
    end
    
    if self.Config.Settings.ConfigVersion == "1.4.1" then
        -- Change the config version.
        self.Config.Settings.ConfigVersion = "1.4.2"

        -- Send a console message to notify the server owner of this change.
        print( "m-Teleportation: Your config file was updated to config version 1.4.2, version indication was updated!" )
    end
    
    if self.Config.Settings.ConfigVersion == "1.4.2" then
        -- Change the config version.
        self.Config.Settings.ConfigVersion = "1.4.3"

        -- Send a console message to notify the server owner of this change.
        print( "m-Teleportation: Your config file was updated to config version 1.4.3, version indication was updated!" )
    end
    
    if self.Config.Settings.ConfigVersion == "1.4.3" then
        -- Change the config version.
        self.Config.Settings.ConfigVersion = "1.4.4"

        -- Send a console message to notify the server owner of this change.
        print( "m-Teleportation: Your config file was updated to config version 1.4.4, version indication was updated!" )
    end
    
    if self.Config.Settings.ConfigVersion == "1.4.4" then
        -- Change the config version.
        self.Config.Settings.ConfigVersion = "1.4.5"

        -- Send a console message to notify the server owner of this change.
        print( "m-Teleportation: Your config file was updated to config version 1.4.5, version indication was updated!" )
    end
    
    if self.Config.Settings.ConfigVersion == "1.4.5" then
        -- Change the config version.
        self.Config.Settings.ConfigVersion = "1.4.6"

        -- Send a console message to notify the server owner of this change.
        print( "m-Teleportation: Your config file was updated to config version 1.4.6, version indication was updated!" )
    end
    
    if self.Config.Settings.ConfigVersion == "1.4.6" then
        -- Change the config version.
        self.Config.Settings.ConfigVersion = "1.4.7"

        -- Send a console message to notify the server owner of this change.
        print( "m-Teleportation: Your config file was updated to config version 1.4.7, version indication was updated!" )
    end
    
    if self.Config.Settings.ConfigVersion == "1.4.7" then
        -- Change the config version.
        self.Config.Settings.ConfigVersion = "1.4.8"

        -- Send a console message to notify the server owner of this change.
        print( "m-Teleportation: Your config file was updated to config version 1.4.8, version indication was updated!" )
    end
    
    if self.Config.Settings.ConfigVersion == "1.4.8" then
        -- Change the config version.
        self.Config.Settings.ConfigVersion = "1.4.9"

        -- Send a console message to notify the server owner of this change.
        print( "m-Teleportation: Your config file was updated to config version 1.4.9, version indication was updated!" )
    end
    
    if self.Config.Settings.ConfigVersion == "1.4.9" then
        -- Change the config version.
        self.Config.Settings.ConfigVersion = "1.4.12"

        -- Send a console message to notify the server owner of this change.
        print( "m-Teleportation: Your config file was updated to config version 1.4.12, version indication was updated!" )
    end
    
    if self.Config.Settings.ConfigVersion == "1.4.12" then
        -- Change the config version.
        self.Config.Settings.ConfigVersion = "1.4.13"

        -- Send a console message to notify the server owner of this change.
        print( "m-Teleportation: Your config file was updated to config version 1.4.13, version indication was updated!" )
    end
    
    if self.Config.Settings.ConfigVersion == "1.4.13" then
        -- Change the config version.
        self.Config.Settings.ConfigVersion = "1.4.14"

        -- Send a console message to notify the server owner of this change.
        print( "m-Teleportation: Your config file was updated to config version 1.4.14, version indication was updated!" )
    end
    
    if self.Config.Settings.ConfigVersion == "1.4.14" then
        -- Change the config version.
        self.Config.Settings.ConfigVersion = "1.4.15"

        -- Send a console message to notify the server owner of this change.
        print( "m-Teleportation: Your config file was updated to config version 1.4.15, version indication was updated!" )
    end
    
    if self.Config.Settings.ConfigVersion ~= "1.4.19" then
        -- Change the config version.
        self.Config.Settings.ConfigVersion = "1.4.19"
        
        -- Add new config entries
        self.Config.Messages.HomesBuildingBlocked = "You can't set your home if you are not allowed to build in this zone!"
        self.Config.Messages.TPRBuildingBlockedA  = "You can't accept a TPR while in a building blocked zone!"
        self.Config.Messages.TPRBuildingBlockedT  = "Teleport aborted because your target moved into a building blocked zone!"

        -- Send a console message to notify the server owner of this change.
        print( "m-Teleportation: Your config file was updated to config version 1.4.19, version indication was updated and 3 new messages were added!" )
    end

    -- Save the config.
    self:SaveConfig()
end

-- ----------------------------------------------------------------------------
-- PLUGIN:LoadSavedData()
-- ----------------------------------------------------------------------------
-- Load the DataTable file into a table or create a new table when the file
-- doesn't exist yet.
-- ----------------------------------------------------------------------------
function PLUGIN:LoadSavedData()
    -- Open the datafile if it exists, otherwise we'll create a new one.
    TeleportData           = datafile.GetDataTable( "m-Teleportation" )
    TeleportData           = TeleportData or {}
    TeleportData.AdminData = TeleportData.AdminData or {}
    TeleportData.HomeData  = TeleportData.HomeData or {}
    TeleportData.TPRData   = TeleportData.TPRData or {}
end

-- ----------------------------------------------------------------------------
-- PLUGIN:SaveData()
-- ----------------------------------------------------------------------------
-- Saves the table with all the teleportdata to a DataTable file.
-- ----------------------------------------------------------------------------
function PLUGIN:SaveData()  
    -- Save the DataTable
    datafile.SaveDataTable( "m-Teleportation" )
end

-- ----------------------------------------------------------------------------
-- PLUGIN:cmdTeleport( player, cmd, args )                        Admin Command
-- ----------------------------------------------------------------------------
-- In-game '/tp' command for server admins to be able to teleport to players,
-- teleport players to other players and to teleport to a set of coordinates on
-- the map.
-- ----------------------------------------------------------------------------
function PLUGIN: cmdTeleport( player, cmd, args )
    -- Check if the player is allowed to run the command.
    if not self:IsAllowed( player ) then return end

    -- Determine what the player is trying to do depending on the amount of
    -- arguments that the player has supplied.
    if args.Length == 1 then
        -- The player supplied one argument, assuming that the player is 
        -- attempting to teleport himself/herself to another online player.

        -- Search for the BasePlayer for the given (partial) name.
        local targetPlayer = self:FindPlayerByName( args[0] )

        -- Check if we found the targetted player.
        if #targetPlayer == 0 then
            -- The targetted player couldn't be found, send a message to the 
            -- player.
            self:SendMessage( player, self.Config.Messages.PlayerNotFound )

            return
        end

        -- Check if we found multiple players with that partial name.
        if #targetPlayer > 1 then
            -- Multiple players were found, send a message to the player.
            self:SendMessage( player, self.Config.Messages.MultiplePlayersFound )

            return
        else
            -- Only one player was found, modify the targetPlayer variable 
            -- value.
            targetPlayer = targetPlayer[1]
        end

        -- Check if the targetted player isn't the player that is running the
        -- command.
        if player == targetPlayer then
            -- The player and the targetted player are the same, send a message
            -- to the player.
            self:SendMessage( player, self.Config.Messages.CantTeleportToSelf )
            
            return
        end

        -- The targetted player was found and is a valid target. Save the
        -- player his current location for the '/tpb' command and initiate a
        -- teleport to the target.
        self:SaveLocation( player )
        self:TeleportToPlayer( player, targetPlayer )

        -- Show a message to the player.
        self:SendMessage( player, self:Parse( self.Config.Messages.AdminTP, { player = targetPlayer.displayName } ) ) 

        -- Check the settings if we're supposed to send a message to the
        -- targetted player or not.
        if self.Config.AdminTP.AnnounceTeleportToTarget then
            -- Send the message to the targetted player.
            self:SendMessage( targetPlayer, self:Parse( self.Config.Messages.AdminTPTarget, { player = player.displayName } ) )
        end
    elseif args.Length == 2 then
        -- The player supplied two arguments, assuming that the player is
        -- attempting to teleport a different player to another player.

        -- Search for the BasePlayer for the given (partial) names.
        local originPlayer = self:FindPlayerByName( args[0] )
        local targetPlayer = self:FindPlayerByName( args[1] )

        -- Check if we found the targetted player.
        if #originPlayer == 0 or #targetPlayer == 0 then
            -- One or both players couldn't be found, send a message to the
            -- player.
            self:SendMessage( player, self.Config.Messages.PlayerNotFound )

            return
        end

        -- Check if we found multiple players with that partial name.
        if #originPlayer > 1 or #targetPlayer > 1 then
            -- Multiple players were found, send a message to the player.
            self:SendMessage( player, self.Config.Messages.MultiplePlayersFound )

            return
        else
            -- Only one player was found, modify the targetPlayer variable
            -- value.
            originPlayer = originPlayer[1]
            targetPlayer = targetPlayer[1]
        end

        -- Check if the origin player is different from the targetted player.
        if originPlayer == targetPlayer then
            -- Both players are the same, send a message to the player.
            self:SendMessage( player, self.Config.Messages.CantTeleportPlayerToSelf )

            return
        end

        -- Check if the player is teleporting himself.
        if self:IsAllowed( originPlayer ) then
            -- The player is teleporting himself so we need to save his current
            -- location.
            self:SaveLocation( player )
        end

        -- Both players were found and are valid. Initiate a teleport for the
        -- origin player to the targetted player.
        self:TeleportToPlayer( originPlayer, targetPlayer )

        -- Show a message to the player, origin player and targetted player.
        self:SendMessage( player, self:Parse( self.Config.Messages.AdminTPPlayers, { player = originPlayer.displayName, target = targetPlayer.displayName } ) )
        self:SendMessage( originPlayer, self:Parse( self.Config.Messages.AdminTPPlayer, { admin = player.displayName, player = targetPlayer.displayName } ) )
        self:SendMessage( targetPlayer, self:Parse( self.Config.Messages.AdminTPPlayerTarget, { admin = player.displayName, player = originPlayer.displayName } ) )
    elseif args.Length == 3 then
        -- The player supplied three arguments, assuming that the player is
        -- attempting to teleport himself/herself to a set of coordinates.

        -- Store the coordinates as numbers into variables.
        local x = tonumber( args[0] )
        local y = tonumber( args[1] )
        local z = tonumber( args[2] )

        -- Validate the three coordinates, first check if all three are numbers
        -- and then check if the coordinates are within the map boundaries.
        if x and y and z then
            -- The three supplied axis values are numbers, check if they are
            -- within the boundaries of the map.
            local boundary = global.TerrainMeta.get_Size().x / 2

            if ( x <= boundary and x >= -boundary ) and ( y < 2000 and y >= -100 ) and ( z <= boundary and z >= -boundary ) then
                -- A valid location was specified, save the player his current
                -- location for the '/tpb' command and initiate a teleport.
                self:SaveLocation( player )
                self:TeleportToPosition( player, x, y, z )

                -- Show a message to the player.
                self:SendMessage(player, self:Parse( self.Config.Messages.AdminTPCoordinates, { coordinates = x .. " " .. y .. " " .. z } ) )
            else
                -- One or more axis values are out of bounds, show a message to
                -- the player.
                self:SendMessage( player, self.Config.Messages.AdminTPOutOfBounds )
                self:SendMessage( player, self:Parse( self.Config.Messages.AdminTPBoundaries, { boundary = boundary } ) )
            end
        else
            -- One or more axis values are not a number and are invalid, show a
            -- a message to the player.
            self:SendMessage( player, self.Config.Messages.InvalidCoordinates )
        end
    elseif args.Length == 4 then
        -- The player supplied four arguments, assuming that the player is
        -- attempting to teleport a player to a set of coordinates.
        
        -- Search for the BasePlayer for the given (partial) name.
        local targetPlayer = self:FindPlayerByName( args[0] )

        -- Check if we found the targetted player.
        if #targetPlayer == 0 then
            -- The targetted player couldn't be found, send a message to the
            -- player.
            self:SendMessage( player, self.Config.Messages.PlayerNotFound )

            return
        end

        -- Check if we found multiple players with that partial name.
        if #targetPlayer > 1 then
            -- Multiple players were found, send a message to the player.
            self:SendMessage( player, self.Config.Messages.MultiplePlayersFound )

            return
        else
            -- Only one player was found, modify the targetPlayer variable
            -- value.
            targetPlayer = targetPlayer[1]
        end

        -- Store the coordinates as numbers into variables.
        local x = tonumber( args[1] )
        local y = tonumber( args[2] )
        local z = tonumber( args[3] )

        -- Validate the three coordinates, first check if all three are numbers
        -- and then check if the coordinates are within the map boundaries.
        if x and y and z then
            -- The three supplied axis values are numbers, check if they are
            -- within the boundaries of the map.
            local boundary = global.TerrainMeta.get_Size().x / 2

            if ( x <= boundary and x >= -boundary ) and ( y < 2000 and y >= -100 ) and ( z <= boundary and z >= -boundary ) then
                -- A valid location was specified, save the player his/her
                -- current location for the '/tpb' command if necessary and
                -- initiate a teleport.
                if self:IsAllowed( targetPlayer ) then
                    -- The player is an admin so we need to save his current
                    -- location.
                    self:SaveLocation( targetPlayer )
                end

                self:TeleportToPosition( targetPlayer, x, y, z )

                -- Show a message to the player.
                if player == targetPlayer then
                    self:SendMessage(player, self:Parse( self.Config.Messages.AdminTPCoordinates, { coordinates = x .. " " .. y .. " " .. z } ) )
                else
                    self:SendMessage(player, self:Parse( self.Config.Messages.AdminTPTargetCoordinates, { player = targetPlayer.displayName, coordinates = x .. " " .. y .. " " .. z } ) )
                    self:SendMessage(targetPlayer, self:Parse( self.Config.Messages.AdminTPTargetCoordinatesTarget, { admin = player.displayName, coordinates = x .. " " .. y .. " " .. z } ) )
                end
            else
                -- One or more axis values are out of bounds, show a message to
                -- the player.
                self:SendMessage( player, self.Config.Messages.AdminTPOutOfBounds )
                self:SendMessage( player, self:Parse( self.Config.Messages.AdminTPBoundaries, { boundary = boundary } ) )
            end
        else
            -- One or more axis values are not a number and are invalid, show a
            -- a message to the player.
            self:SendMessage( player, self.Config.Messages.InvalidCoordinates )
        end
    else
        -- No arguments or an invalid amount of arguments were supplied, send a
        -- message to the player with the available command possibilities.
        self:SendMessage( player, self.Config.Messages.SyntaxCommandTP )
    end
end

-- ----------------------------------------------------------------------------
-- PLUGIN:cmdTeleportNear( player, cmd, args )                    Admin Command
-- ----------------------------------------------------------------------------
-- In-game '/tpn' command for server admins to be able to teleport near a
-- player.
-- ----------------------------------------------------------------------------
function PLUGIN: cmdTeleportNear( player, cmd, args )
    -- Check if the player is allowed to run the command.
    if not self:IsAllowed( player ) then return end

    -- Determine what the player is trying to do depending on the amount of
    -- arguments that the player has supplied.
    if args.Length == 1 or args.Length == 2 then
        -- The player supplied one argument, assuming that the player is
        -- attempting to teleport himself/herself to another online player.

        -- Search for the BasePlayer for the given (partial) name.
        local targetPlayer = self:FindPlayerByName( args[0] )

        -- Check if we found the targetted player.
        if #targetPlayer == 0 then
            -- The targetted player couldn't be found, send a message to the
            -- player.
            self:SendMessage( player, self.Config.Messages.PlayerNotFound )

            return
        end

        -- Check if we found multiple players with that partial name.
        if #targetPlayer > 1 then
            -- Multiple players were found, send a message to the player.
            self:SendMessage( player, self.Config.Messages.MultiplePlayersFound )

            return
        else
            -- Only one player was found, modify the targetPlayer variable
            -- value.
            targetPlayer = targetPlayer[1]
        end

        -- Check if the targetted player isn't the player that is running the
        -- command.
        if player == targetPlayer then
            -- The player and the targetted player are the same, send a message
            -- to the player.
            self:SendMessage( player, self.Config.Messages.CantTeleportToSelf )
            
            return
        end

        -- Determine the distance behind the player.
        local distance = tonumber( self.Config.AdminTP.TeleportNearDefaultDistance )

        -- Check if a different distance was supplied.
        if args.Length == 2 then
            if tonumber( args[1] ) then
                distance = tonumber( args[1] )
            end
        end

        -- Generate a random x value between 0 and the distance required.
        local targetX = math.random( -distance, distance )
        local targetZ = math.sqrt( math.pow( distance, 2 ) - math.pow( targetX, 2 ) )

        local destination = targetPlayer.transform.position
        destination.x = destination.x - targetX
        destination.y = 5000
        destination.z = destination.z - targetZ

        -- Calculate the height at the new coordinates.
        destination = self:GetGround( destination )

        -- The targetted player was found and is a valid target. Save the
        -- player his/her current location for the '/tpb' command and initiate
        -- a teleport to the target.
        self:SaveLocation( player )
        self:TeleportToPosition( player, destination.x, destination.y, destination.z )
        
        -- Show a message to the player.
        self:SendMessage( player, self:Parse( self.Config.Messages.AdminTP, { player = targetPlayer.displayName } ) ) 

        -- Check the settings if we're supposed to send a message to the
        -- targetted player or not.
        if self.Config.AdminTP.AnnounceTeleportToTarget then
            -- Send the message to the targetted player.
            self:SendMessage( targetPlayer, self:Parse( self.Config.Messages.AdminTPTarget, { player = player.displayName } ) )
        end
    else
        -- No arguments or an invalid amount of arguments were supplied, send a
        -- message to the player with the available command possibilities.
        self:SendMessage( player, self.Config.Messages.SyntaxCommandTPN )
    end
end

-- ----------------------------------------------------------------------------
-- PLUGIN:cmdTeleportLocation( player, cmd, args )                Admin Command
-- ----------------------------------------------------------------------------
-- In-game '/tpl' command that allows a server admin to teleport to a
-- previously saved location or to get a list of all saved locations.
-- ----------------------------------------------------------------------------
function PLUGIN:cmdTeleportLocation( player, cmd, args )
    -- Check if the player is allowed to run the command.
    if not self:IsAllowed( player ) then return end

    -- Grab the player his/her SteamID.
    local playerID = rust.UserIDFromPlayer( player )

    -- Determine what the player is trying to do depending on the amount of
    -- arguments that the player has supplied.
    if args.Length == 0 then
        -- The player didn't supply any arguments, assuming that the player
        -- wants to get a list of all his available teleport locations.

        -- Check if there is data saved for the player, after that check if the
        -- data contains any saved locations for the player.
        if TeleportData.AdminData[playerID] then
            if self:Count( TeleportData.AdminData[playerID].SavedLocations ) > 0 then
                -- The player has one or more save locations available, show a
                -- message to the player and show him/her all the available
                -- saved locations.
                self:SendMessage( player, self.Config.Messages.AdminLocationList )

                -- Loop through all the saved locations and print them one by
                -- one.
                for location, coordinates in pairs( TeleportData.AdminData[playerID].SavedLocations ) do
                    self:SendMessage( player, location .. ": " .. math.floor( coordinates.x ) .. " " .. math.floor( coordinates.y ) .. " " .. math.floor( coordinates.z ) )
                end

                return
            end
        end

        -- The player has no saved locations available, show a message to
        -- him/her. 
        self:SendMessage( player, self.Config.Messages.AdminLocationListEmpty )
    elseif args.Length == 1 then
        -- The player supplied one argument, assuming that the player is
        -- attempting to teleport himself/herself to a saved location.

        -- Check if there is data saved for the player, after that check if the
        -- data contains any saved locations for the player.
        if TeleportData.AdminData[playerID] then
            if self:Count( TeleportData.AdminData[playerID].SavedLocations ) > 0 then
                -- The player has one or more save locations available, check
                -- if the location that the player specified is a saved
                -- location.

                -- Set some variables to store the location in when we find it.
                local locationFound       = false
                local locationCoordinates = nil

                -- Loop through all the saved locations for the player and
                -- check for a match against the entered location.
                for location, coordinates in pairs( TeleportData.AdminData[playerID].SavedLocations ) do
                    if string.lower( args[0] ) == string.lower( location ) then
                        -- We found a match for the entered location.
                        locationFound       = true
                        locationCoordinates = coordinates

                        -- Exit the loop.
                        break
                    end
                end
                
                if not locationFound then
                    self:SendMessage( player, self.Config.Messages.LocationNotFound )

                    return
                end
                -- A valid location was specified, save the player his current
                -- location for the '/tpb' command and initiate a teleport.
                self:SaveLocation( player )
                self:TeleportToPosition( player, locationCoordinates.x, locationCoordinates.y, locationCoordinates.z ) 

                -- Send a message to the player.
                self:SendMessage( player, self:Parse( self.Config.Messages.AdminTPLocation, { location = args[0] } ) )

                return
            end
        end

        -- The player has no saved locations available, show a message to
        -- him/her. 
        self:SendMessage( player, self.Config.Messages.AdminLocationListEmpty )
    else
        -- An invalid amount of arguments were supplied, send a message to the
        -- player with the available command possibilities.
        self:SendMessage( player, self.Config.Messages.SyntaxCommandTPL )
    end
end

-- ----------------------------------------------------------------------------
-- PLUGIN:cmdSaveTeleportLocation( player, cmd, args )            Admin command
-- ----------------------------------------------------------------------------
-- In-game '/tpsave' command that allows a server admin to save his current
-- location to be able to teleport to it later.
-- ----------------------------------------------------------------------------
function PLUGIN:cmdSaveTeleportLocation( player, cmd, args )
     -- Check if the player is allowed to run the command.
    if not self:IsAllowed( player ) then return end

    -- Grab the player his/her SteamID.
    local playerID = rust.UserIDFromPlayer( player )

    -- Determine what the player is trying to do depending on the amount of
    -- arguments that the player has supplied.
    if args.Length == 1 then
        -- The player supplied one argument, assuming that the player is
        -- attempting to save his/her current location.
        
        -- Check if the tables to save the location to exist, if they don't we
        -- have to create them first or an error will occur.
        TeleportData.AdminData[playerID]                = TeleportData.AdminData[playerID] or {}
        TeleportData.AdminData[playerID].SavedLocations = TeleportData.AdminData[playerID].SavedLocations or {}

        -- Set some variables that we'll use to check if there is already a
        -- location saved near the current position or with the same name.
        local locationFound             = false
        local locationFoundNearby       = false
        local locationName              = nil
        local positionCoordinates       = player.transform.position
        local locationCoordinates       = new( UnityEngine.Vector3._type, nil )
        local locationCoordinatesString = nil

        -- Loop through all the saved locations for the player to check if a
        -- location with the same name or near that position already exists.
        for location, coordinates in pairs( TeleportData.AdminData[playerID].SavedLocations ) do
            locationName          = location
            locationCoordinates.x = coordinates.x
            locationCoordinates.y = coordinates.y
            locationCoordinates.z = coordinates.z

            -- Check if the player already has a location with the same name.
            if args[0] == locationName then
                -- A location with this name already exists.
                locationFound = true

                -- Exit the loop.
                break
            end

            -- Check if the player already has a location nearby this position
            -- with a different name.
            if UnityEngine.Vector3.Distance( positionCoordinates, locationCoordinates ) < self.Config.AdminTP.LocationRadius then
                -- A location was found near the current position.
                locationFound = true

                -- Exit the loop.
                break
            end
        end

        -- If the location can't be created because a location with the same
        -- name or near the current position already exist we'll setup a string
        -- with the coordinates of that location to show to the player.
        if locationFound or locationFoundNearby then
            locationCoordinatesString = math.floor( locationCoordinates.x ) .. " " .. math.floor( locationCoordinates.y ) .. " " .. math.floor( locationCoordinates.z )
        end

        -- Determine the next step depending on the existance of the location.
        if not locationFound and not locationFoundNearby then
            -- No saved locations found near this position or with the same
            -- name, so this position can be saved with the supplied name.

            -- Set the data and save it.
            TeleportData.AdminData[playerID].SavedLocations[args[0]] = { x = positionCoordinates.x, y = positionCoordinates.y, z = positionCoordinates.z }
            self:SaveData()

            -- Show a message to the player.
            self:SendMessage( player, self.Config.Messages.AdminTPLocationSave )
        elseif locationFound and not locationFoundNearby then
            -- A saved location was found with the same name, send a message to
            -- the player with the coordinates of the location.
            self:SendMessage( player, self:Parse( self.Config.Messages.LocationExists, { location = locationCoordinatesString } ) )
        elseif not locationFound and locationFoundNearby then
            -- A saved location was found near the current position, send a
            -- message to the player with the name of the location.
            self:SendMessage( player, self:Parse( self.Config.Messages.LocationExistsNearby, { name = locationName } ) )
        end
    else
        -- No arguments or an invalid amount of arguments were supplied, send a
        -- message to the player with the available command possibilities.
        self:SendMessage( player, self.Config.Messages.SyntaxCommandTPSave )
    end
end

-- ----------------------------------------------------------------------------
-- PLUGIN:cmdRemoveTeleportLocation( player, cmd, args )          Admin Command
-- ----------------------------------------------------------------------------
-- In-game '/tpremove' command that allows a server admin to remove a saved
-- location.
-- ----------------------------------------------------------------------------
function PLUGIN:cmdRemoveTeleportLocation( player, cmd, args )
     -- Check if the player is allowed to run the command.
    if not self:IsAllowed( player ) then return end

    -- Grab the player his/her SteamID.
    local playerID = rust.UserIDFromPlayer( player )

    -- Determine what the player is trying to do depending on the amount of
    -- arguments that the player has supplied.
    if args.Length == 1 then
        -- The player supplied one argument, assuming that the player is
        -- attempting to remove the specified location from his/her saved
        -- locations list.
        
        -- Check if there is data saved for the player, after that check if the
        -- data contains any saved locations for the player.
        if TeleportData.AdminData[playerID] then
            if self:Count( TeleportData.AdminData[playerID].SavedLocations ) > 0 then
                -- The player has one or more save locations available, check
                -- if the location that the player specified is a saved
                -- location.

                -- Set some variables to store the location in when we find it.
                local locationFound       = false

                -- Loop through all the saved locations for the player and
                -- check for a match against the entered location.
                for location, coordinates in pairs( TeleportData.AdminData[playerID].SavedLocations ) do
                    if args[0] == location then
                        -- We found a match for the entered location.
                        locationFound       = true

                        -- Exit the loop.
                        break
                    end
                end

                -- Check if we found a match while comparing the specified
                -- location to the list of saved locations.    
                if locationFound then
                    -- We have found a location with the specified name so we
                    -- can now remove this from the DataTable and save it.
                    TeleportData.AdminData[playerID].SavedLocations[args[0]] = nil
                    self:SaveData()

                    -- Show a message to the player.
                    self:SendMessage( player, self:Parse( self.Config.Messages.AdminTPLocationRemove, { location = args[0] } ) )

                else
                    -- We haven't found a location with the specified name,
                    -- send a message to the player.
                    self:SendMessage( player, self.Config.Messages.LocationNotFound )
                end

                return
            end
        end

        -- The player has no saved locations available, show a message to the
        -- player.
        self:SendMessage( player, self.Config.Messages.AdminLocationListEmpty )
    else
        -- No arguments or an invalid amount of arguments were supplied, send a
        -- message to the player with the available command possibilities.
        self:SendMessage( player, self.Config.Messages.SyntaxCommandTPRemove )
    end
end

-- ----------------------------------------------------------------------------
-- PLUGIN:cmdTeleportBack( player, cmd, args )                    Admin Command
-- ----------------------------------------------------------------------------
-- In-game '/tpb' command that allows a server admin to teleport back to his
-- previous location after teleporting.
-- ----------------------------------------------------------------------------
function PLUGIN:cmdTeleportBack( player, cmd, args )
     -- Check if the player is allowed to run the command.
    if not self:IsAllowed( player ) then return end

    -- Grab the player his/her SteamID.
    local playerID = rust.UserIDFromPlayer( player )

    -- Determine what the player is trying to do depending on the amount of
    -- arguments that the player has supplied.
    if args.Length == 0 then
        -- The player supplied no arguments, assuming that the player is
        -- attempting to teleport back to his previous location.
        
        -- Check if there is data saved for the player, after that check if the
        -- data contains a previous location for the player.
        if TeleportData.AdminData[playerID] then
            if TeleportData.AdminData[playerID].PreviousLocation then
                -- A previous location was found, teleport the player back to
                -- that
                -- position.
                self:TeleportToPosition( player, TeleportData.AdminData[playerID].PreviousLocation.x, TeleportData.AdminData[playerID].PreviousLocation.y, TeleportData.AdminData[playerID].PreviousLocation.z )

                -- Remove the saved location from the DataTable and save it.
                TeleportData.AdminData[playerID].PreviousLocation = nil
                self:SaveData()

                -- Show a message to the player.
                self:SendMessage( player, self.Config.Messages.AdminTPBack )
            end
        end

        -- There is no previously saved position available, show a message to
        -- the player.
        self:SendMessage( player, self.Config.Messages.NoPreviousLocationSaved )
    else
        -- An invalid amount of arguments was supplied, send a message to the
        -- player with the available command possibilities.
        self:SendMessage( player, self.Config.Messages.SyntaxCommandTPB )
    end
end

-- ----------------------------------------------------------------------------
-- PLUGIN:cmdSetHome( player, cmd, args )
-- ----------------------------------------------------------------------------
-- In-game '/sethome' command that allows any player to save his/her current
-- position as a home that can be used to teleport back to.
-- ----------------------------------------------------------------------------
function PLUGIN:cmdSetHome( player, cmd, args )
    -- Check if the Home module is enabled.
    if not self.Config.Settings.HomesEnabled then return end
    -- Check if the player is allowed to use the command.
    canTeleport, err = self:CanPlayerTeleport( player )
    if not canTeleport then
        -- The player isn't allowed to teleport right now, send him a message
        -- from the plugin that is blocking the teleport and cancel the
        -- teleport process.
        self:SendMessage( player, err )

        return
    end

    -- Grab the player his/her SteamID.
    local playerID = rust.UserIDFromPlayer( player )

    -- Determine what the player is trying to do depending on the amount of
    -- arguments that the player has supplied.
    if args.Length == 1 then
        -- The player supplied one arguments, assuming that the player is
        -- attempting to save his/her current position as a home location.
        
        -- Check if the tables to save the location to exist, if they don't we
        -- have to create them first or an error will occur.
        TeleportData.HomeData[playerID]               = TeleportData.HomeData[playerID] or {}
        TeleportData.HomeData[playerID].HomeLocations = TeleportData.HomeData[playerID].HomeLocations or {}

        -- Set some variables that we'll use to check if there is already a
        -- location saved near the current position or with the same name.
        local locationFound             = false
        local locationFoundNearby       = false
        local locationName              = nil
        local positionCoordinates       = player.transform.position
        local locationCoordinates       = new( UnityEngine.Vector3._type, nil )
        local locationCoordinatesString = nil

        -- Check if the player already has saved home locations before.
        if self:Count( TeleportData.HomeData[playerID].HomeLocations ) > 0 then
            -- Check if the amount of homes is limited and if it is then check
            -- if the amount of saved homes will not go over the limit.
			local HomesLimit = self.Config.Homes.HomesLimit
            if HomesLimit > 0 and self:Count( TeleportData.HomeData[playerID].HomeLocations ) >= HomesLimit then
                -- The player has reached the maximum amount of saved homes,
                -- show a message to the player.
                self:SendMessage( player, self:Parse( self.Config.Messages.HomeMaxLocations, { amount = HomesLimit } ) )

                return
            end

            -- Loop through all the saved locations for the player to check if
            -- a location with the same name or near that position already
            -- exists.
            for location, coordinates in pairs( TeleportData.HomeData[playerID].HomeLocations ) do
                locationName          = location
                locationCoordinates.x = coordinates.x
                locationCoordinates.y = coordinates.y
                locationCoordinates.z = coordinates.z

                -- Check if the player already has a location with the same
                -- name.
                if args[0] == locationName then
                    -- A location with this name already exists.
                    locationFound = true

                    -- Exit the loop.
                    break
                end

                -- Check if the player already has a location nearby this
                -- position with a different name.
                if UnityEngine.Vector3.Distance( positionCoordinates, locationCoordinates ) < self.Config.Homes.LocationRadius then
                    -- A location was found near the current position.
                    locationFoundNearby = true

                    -- Exit the loop.
                    break
                end
            end
        end

        -- If the location can't be created because a location with the same
        -- name or near the current position already exist we'll setup a string
        -- with the coordinates of that location to show to the player.
        if locationFound or locationFoundNearby then
            locationCoordinatesString = math.floor( locationCoordinates.x ) .. " " .. math.floor( locationCoordinates.y ) .. " " .. math.floor( locationCoordinates.z )
        end
        
        if not player:CanBuild() then
            self:SendMessage( player, self.Config.Messages.HomesBuildingBlocked )
            return
        end

        -- Determine the next step depending on the existance of the location.
        if not locationFound and not locationFoundNearby then
            -- No saved locations found near this position or with the same
            -- name, so this position can be saved with the supplied name.

            -- Check if the player is standing on a foundation if required.
            if self.Config.Homes.ForceOnTopOfFoundation then
                -- Modify the local position, add 2 to the y coordinate in case
                -- the terrain (grass) goes through the foundation.
                local position = player.transform.position
                position.y = position.y + 1
                
                -- Create a local variable to store the BuildingBlock.
                local block = nil

                -- Create a Ray from the player to the ground to detect what the player is standing on
                local ray = new( UnityEngine.Ray._type, util.TableToArray { player.transform.position, UnityEngine.Vector3.get_down() } )
                         
                local arr = util.TableToArray { ray, new( UnityEngine.RaycastHit._type, nil ), 1.5, LayerMask }
                util.ConvertAndSetOnArray(arr, 2, 1.5, System.Int64._type)
                util.ConvertAndSetOnArray(arr, 3, LayerMask, System.Int32._type)
                 
                if Raycast:Invoke( nil, arr ) then
                    local hitEntity = global.RaycastHitEx.GetEntity(arr[1])
                    if hitEntity then
                        if hitEntity:GetComponentInParent(global.BuildingBlock._type) then
                            local buildingBlock = hitEntity:GetComponentInParent(global.BuildingBlock._type)
                            if buildingBlock.name:find( "foundation", 1, true) then
                                block = buildingBlock
                            end
                        end
                    end                    
                end     

                -- Check if we have a BuildingBlock for the player.
                if not block then
                    -- The player is not standing on a foundation, send
                    -- the player a message.
                    self:SendMessage( player, self.Config.Messages.HomeSaveFoundationOnly )

                    return
                end

                -- Check if a player is on top of a door.
                if UnityEngine.Vector3.Distance( player.transform.position, block.transform.position ) > 2 then
                    -- The player is not standing on a foundation, send
                    -- the player a message.
                    self:SendMessage( player, self.Config.Messages.HomeSaveFoundationOnly )

                    return
                end

                -- Check if the player owns the foundation if required.
                if self.Config.Homes.CheckFoundationForOwner then
                    if playerID ~= self:GetOwner( block ) then
                        -- The player is not standing on a owned foundation, 
                        -- send the player a message if the foundation is also
                        -- not owned by a friend if this is enabled in the
                        -- settings.
                        if self.Config.Homes.UseFriendsAPI and not FriendsAPI.Object:areFriends( self:GetOwner( block ), playerID ) then
                            -- The player is not standing on a foundation owned
                            -- by a friend.
                            self:SendMessage( player, self.Config.Messages.HomeFoundationNotFriendsOwned )

                            return
                        end

                        if not self.Config.Homes.UseFriendsAPI then
                            self:SendMessage( player, self.Config.Messages.HomeFoundationNotOwned )

                            return 
                        end                       
                    end
                end

            end

            -- Check the location name for invalid characters.
            if args[0]:match("%W") then
                -- Send the player a message.
                self:SendMessage( player, self.Config.Messages.InvalidCharacter )

                return
            end

            -- Set the data and save it.
            TeleportData.HomeData[playerID].HomeLocations[args[0]] = { x = positionCoordinates.x, y = positionCoordinates.y, z = positionCoordinates.z }
            self:SaveData()

            -- Show a message to the player.
            self:SendMessage( player, self.Config.Messages.HomeSave )
        elseif locationFound and not locationFoundNearby then
            -- A saved location was found with the same name, send a message to
            -- the player with the coordinates of the location.
            self:SendMessage( player, self.Config.Messages.HomeExists )
        elseif not locationFound and locationFoundNearby then
            -- A saved location was found near the current position, send a
            -- message to the player with the name of the location.
            self:SendMessage( player, self:Parse( self.Config.Messages.HomeExistsNearby, { name = locationName } ) )
        end
    else
        -- An invalid amount of arguments was supplied, send a message to the
        -- player with the available command possibilities.
        self:SendMessage( player, self.Config.Messages.SyntaxCommandSetHome)
    end
end

-- ----------------------------------------------------------------------------
-- PLUGIN:cmdRemoveHome( player, cmd, args )
-- ----------------------------------------------------------------------------
-- In-game '/removehome' command that allows a player to remove a previously
-- saved home location.
-- ----------------------------------------------------------------------------
function PLUGIN:cmdRemoveHome( player, cmd, args )
    -- Check if the Home module is enabled.
    if not self.Config.Settings.HomesEnabled then return end

    -- Grab the player his/her SteamID.
    local playerID = rust.UserIDFromPlayer( player )

    -- Determine what the player is trying to do depending on the amount of
    -- arguments that the player has supplied.
    if args.Length == 1 then
        -- The player supplied one arguments, assuming that the player is
        -- attempting to save his/her current position as a home location.
        -- Check if there is data saved for the player, after that check if the
        -- data contains any saved locations for the player.
        if TeleportData.HomeData[playerID] then
            if self:Count( TeleportData.HomeData[playerID].HomeLocations ) > 0 then
                -- The player has one or more save locations available, check
                -- if the location that the player specified is a saved
                -- location.

                -- Set some variables to store the location in when we find it.
                local locationFound       = false

                -- Loop through all the saved locations for the player and
                -- check for a match against the entered location.
                for location, coordinates in pairs( TeleportData.HomeData[playerID].HomeLocations ) do
                    if args[0] == location then
                        -- We found a match for the entered location.
                        locationFound       = true

                        -- Exit the loop.
                        break
                    end
                end

                -- Check if we found a match while comparing the specified
                -- location to the list of saved locations.    
                if locationFound then
                    -- We have found a location with the specified name so we
                    -- can now remove this from the DataTable and save it.
                    TeleportData.HomeData[playerID].HomeLocations[args[0]] = nil
                    self:SaveData()

                    -- Show a message to the player.
                    self:SendMessage( player, self:Parse( self.Config.Messages.HomeRemove, { home = args[0] } ) )

                else
                    -- We haven't found a location with the specified name,
                    -- send a message to the player.
                    self:SendMessage( player, self.Config.Messages.HomeNotFound )
                end

                return
            end
        end

        -- The player has no saved locations available, show a message to the
        -- player.
        self:SendMessage( player, self.Config.Messages.HomeListEmpty )
    else
        -- An invalid amount of arguments was supplied, send a message to the
        -- player with the available command possibilities.
        self:SendMessage( player, self.Config.Messages.SyntaxCommandRemoveHome)
    end
end

-- ----------------------------------------------------------------------------
-- PLUGIN:cmdTeleportHome( player, cmd, args )
-- ----------------------------------------------------------------------------
-- In-game '/home' command that allows a player to a previously saved home
-- location.
-- ----------------------------------------------------------------------------
function PLUGIN:cmdTeleportHome( player, cmd, args )
    -- Check if the Home module is enabled.
    if not self.Config.Settings.HomesEnabled then return end

    -- Check if the player is allowed to use the command.
    canTeleport, err = self:CanPlayerTeleport( player )
    if not canTeleport then
        -- The player isn't allowed to teleport right now, send him a message
        -- from the plugin that is blocking the teleport and cancel the
        -- teleport proces.
        self:SendMessage( player, err )

        return
    end

    -- Grab the player his/her SteamID.
    local playerID = rust.UserIDFromPlayer( player )

    -- Determine what the player is trying to do depending on the amount of
    -- arguments that the player has supplied.
    if args.Length == 1 then
        -- The player supplied one argument, assuming that the player is 
        -- attempting to teleport himself/herself to a saved home location.

        -- Check if there is data saved for the player, after that check if the
        -- data contains any saved locations for the player.
        if TeleportData.HomeData[playerID] then
            if self:Count( TeleportData.HomeData[playerID].HomeLocations ) > 0 then
                -- The player has one or more save locations available, check
                -- if the location that the player specified is a saved
                -- location.

                -- Setup variables with todays date and the current timestamp.
                local timestamp   = time.GetUnixTimestamp()
                local currentDate = tostring( time.GetCurrentTime():ToString("d") )

                -- Check if there is saved teleport data available for the
                -- player.
                if TeleportData.HomeData[playerID].Teleports then
                    if TeleportData.HomeData[playerID].Teleports.date ~= currentDate then
                        TeleportData.HomeData[playerID].Teleports = nil
                    end
                end

                -- Grab the user his/her teleport data.
                TeleportData.HomeData[playerID].Teleports = TeleportData.HomeData[playerID].Teleports or {}
                TeleportData.HomeData[playerID].Teleports.amount = TeleportData.HomeData[playerID].Teleports.amount or 0
                TeleportData.HomeData[playerID].Teleports.date = currentDate
                TeleportData.HomeData[playerID].Teleports.timestamp = TeleportData.HomeData[playerID].Teleports.timestamp or 0

                -- Check if the cooldown option is enabled and if it is make
                -- sure that the cooldown time has passed.
                if self.Config.Homes.Cooldown > 0 and ( timestamp - TeleportData.HomeData[playerID].Teleports.timestamp ) < self.Config.Homes.Cooldown then
                    -- Get the remaining time.
                    local remainingTime = self:ParseRemainingTime( self.Config.Homes.Cooldown - ( timestamp - TeleportData.HomeData[playerID].Teleports.timestamp ) )
                    -- Teleport is on cooldown, show a message to the player.
                    self:SendMessage( player, self:Parse( self.Config.Messages.HomeTPCooldown, { time = remainingTime } ) )

                    return
                end
				
				local DailyLimit = self.Config.Homes.DailyLimit
                -- Check if the teleports daily limit is enabled and make sure
                -- that the player has not yet reached the limit.
                if DailyLimit > 0 and TeleportData.HomeData[playerID].Teleports.amount >= DailyLimit then
                    -- The player has reached the limit, show a message to the
                    -- player.
                    self:SendMessage( player, self:Parse( self.Config.Messages.HomeTPLimitReached, { limit = DailyLimit } ) )

                    return
                end

                -- Check if the player already has a teleport pending.
                if TeleportTimers[playerID] then
                    -- Send a message to the player.
                    self:SendMessage( player, self.Config.Messages.TeleportPending )

                    return
                end

                -- Set some variables to store the location in when we find it.
                local locationFound       = false
                local locationCoordinates = nil

                -- Loop through all the saved locations for the player and
                -- check for a match against the entered location.
                for location, coordinates in pairs( TeleportData.HomeData[playerID].HomeLocations ) do
                    if args[0] == location then
                        -- We found a match for the entered location.
                        locationFound       = true
                        locationCoordinates = coordinates

                        -- Exit the loop.
                        break
                    end
                end

                -- Check if we found a match while comparing the specified
                -- location to the list of saved locations.    
                if locationFound then

                    -- Location was found and no limits were reached so we ca
                    -- teleport the player to his home after a short delay.
                    TeleportTimers[playerID]              = {}
                    TeleportTimers[playerID].originPlayer = player
                    TeleportTimers[playerID].timer        = timer.Once ( self.Config.Homes.Countdown, 
                        function()
                            -- Teleport the player to his home location.
                            self:TeleportToPosition( player, locationCoordinates.x, locationCoordinates.y + 1, locationCoordinates.z )
                            
                            -- Modify the teleport amount and last teleport
                            -- timestamp.
                            TeleportData.HomeData[playerID].Teleports.amount = TeleportData.HomeData[playerID].Teleports.amount + 1
                            TeleportData.HomeData[playerID].Teleports.timestamp = timestamp
                            self:SaveData()

                            -- Show a message to the player.
                            self:SendMessage( player, self:Parse( self.Config.Messages.HomeTP, { home = args[0] } ) )

                            -- Remove the pending timer info.
                            TeleportTimers[playerID] = nil
                        end )

                    -- Send a message to the player.
                    self:SendMessage( player, self:Parse( self.Config.Messages.HomeTPStarted, { home = args[0], countdown = self.Config.Homes.Countdown } ) )

                    return
                else
                    -- Couldn't find a home location with the specified name.
                    self:SendMessage( player, self.Config.Messages.HomeNotFound )

                    return
                end
            end
        end

        -- The player has no saved locations available, show a message to
        -- him/her. 
        self:SendMessage( player, self.Config.Messages.HomeListEmpty )
    else
        -- An invalid amount of arguments were supplied, send a message to the
        -- player with the available command possibilities.
        self:SendMessage( player, self.Config.Messages.SyntaxCommandHome )
    end
end

-- ----------------------------------------------------------------------------
-- PLUGIN:cmdListHomes( player, cmd, args )
-- ----------------------------------------------------------------------------
-- In-game '/listhomes' command that allows a player to look at his saved home
-- locations.
-- ----------------------------------------------------------------------------
function PLUGIN:cmdListHomes( player, cmd, args )
    -- Check if the Home module is enabled.
    if not self.Config.Settings.HomesEnabled then return end

    -- Grab the player his/her SteamID.
    local playerID = rust.UserIDFromPlayer( player )

    -- Determine what the player is trying to do depending on the amount of
    -- arguments that the player has supplied.
    if args.Length == 0 then
        -- The player didn't supply any arguments, assuming that the player
        -- wants to get a list of all his available home locations.

        -- Check if there is data saved for the player, after that check if the
        -- data contains any saved locations for the player.
        if TeleportData.HomeData[playerID] then
            if self:Count( TeleportData.HomeData[playerID].HomeLocations ) > 0 then
                -- The player has one or more save locations available, show a
                -- message to the player and show him/her all the available
                -- saved locations.
                self:SendMessage( player, self.Config.Messages.HomeList )

                -- Loop through all the saved locations and print them one by
                -- one.
                for location, coordinates in pairs( TeleportData.HomeData[playerID].HomeLocations ) do
                    self:SendMessage( player, location .. ": " .. math.floor( coordinates.x ) .. " " .. math.floor( coordinates.y ) .. " " .. math.floor( coordinates.z ) )
                end

                return
            end
        end

        -- The player has no saved locations available, show a message to
        -- him/her. 
        self:SendMessage( player, self.Config.Messages.HomeListEmpty )
    else
        -- An invalid amount of arguments were supplied, send a message to the
        -- player with the available command possibilities.
        self:SendMessage( player, self.Config.Messages.SyntaxCommandListHomes)
    end
end

-- ----------------------------------------------------------------------------
-- PLUGIN:cmdTeleportRequest( player, cmd, args )
-- ----------------------------------------------------------------------------
-- In-game '/tpr' command that allows a player to send a teleport request to
-- another online player.
-- ----------------------------------------------------------------------------
function PLUGIN:cmdTeleportRequest( player, cmd, args )
    -- Check if the TPR module is enabled.
    if not self.Config.Settings.TPREnabled then return end

    -- Check if the player is allowed to use the command.
    canTeleport, err = self:CanPlayerTeleport( player )
    if not canTeleport then
        -- The player isn't allowed to teleport right now, send him a message
        -- from the plugin that is blocking the teleport and cancel the
        -- teleport proces.
        self:SendMessage( player, err )

        return
    end

    -- Determine what the player is trying to do depending on the amount of
    -- arguments that the player has supplied.
    if args.Length == 1 then
        -- The player supplied one argument, assuming that the player is
        -- attempting to request a teleport to another player.

        -- Search for the BasePlayer for the given (partial) name.
        local targetPlayer = self:FindPlayerByName( args[0] )

        -- Check if we found the targetted player.
        if #targetPlayer == 0 then
            -- The targetted player couldn't be found, send a message to the
            -- player.
            self:SendMessage( player, self.Config.Messages.PlayerNotFound )

            return
        end

        -- Check if we found multiple players with that partial name.
        if #targetPlayer > 1 then
            -- Multiple players were found, send a message to the player.
            self:SendMessage( player, self.Config.Messages.MultiplePlayersFound )

            return
        else
            -- Only one player was found, modify the targetPlayer variable
            -- value.
            targetPlayer = targetPlayer[1]
        end

        -- Check if the targetted player isn't the player that is running the
        -- command.
        if player == targetPlayer then
            -- The player and the targetted player are the same, send a message
            -- to the player.
            self:SendMessage( player, self.Config.Messages.CantTeleportToSelf )
            
            return
        end

        -- Grab the Steam ID of both players.
        originPlayerID = rust.UserIDFromPlayer( player )
        targetPlayerID = rust.UserIDFromPlayer( targetPlayer )

        -- Check if the player has not reached the limit yet and check if it's
        -- not on cooldown.

        -- Setup variables with todays date and the current timestamp.
        local timestamp   = time.GetUnixTimestamp()
        local currentDate = time.GetCurrentTime():ToString("d")

        -- Setup the player his/her TPRData table if it doesn't exist yet.
        TeleportData.TPRData[originPlayerID] = TeleportData.TPRData[originPlayerID] or {}

        -- Check if there is saved teleport data available for the player.
        if TeleportData.TPRData[originPlayerID].Teleports then
            if TeleportData.TPRData[originPlayerID].Teleports.date ~= currentDate then
                TeleportData.TPRData[originPlayerID].Teleports = nil
            end
        end

        -- Grab the user his/her teleport data.
        TeleportData.TPRData[originPlayerID].Teleports = TeleportData.TPRData[originPlayerID].Teleports or {}
        TeleportData.TPRData[originPlayerID].Teleports.amount = TeleportData.TPRData[originPlayerID].Teleports.amount or 0
        TeleportData.TPRData[originPlayerID].Teleports.date = currentDate
        TeleportData.TPRData[originPlayerID].Teleports.timestamp = TeleportData.TPRData[originPlayerID].Teleports.timestamp or 0

        -- Check if the cooldown option is enabled and if it is make sure
        -- that the cooldown time has passed.
        if self.Config.TPR.Cooldown > 0 and ( timestamp - TeleportData.TPRData[originPlayerID].Teleports.timestamp ) < self.Config.TPR.Cooldown then
            -- Get the remaining time.
            local remainingTime = self:ParseRemainingTime( self.Config.TPR.Cooldown - ( timestamp - TeleportData.TPRData[originPlayerID].Teleports.timestamp ) )
            -- Teleport is on cooldown, show a message to the player.
            self:SendMessage( player, self:Parse( self.Config.Messages.TPRCooldown, { time = remainingTime } ) )

            return
        end
		
		
		local DailyTeleportLimit = self.Config.TPR.DailyLimit
        -- Check if the teleports daily limit is enabled and make sure that
        -- the player has not yet reached the limit.
        if DailyTeleportLimit > 0 and TeleportData.TPRData[originPlayerID].Teleports.amount >= DailyTeleportLimit then
            -- The player has reached the limit, show a message to the
            -- player.
            self:SendMessage( player, self:Parse( self.Config.Messages.TPRLimitReached, { limit = DailyTeleportLimit } ) )

            return
        end

        -- Check if the player already has a teleport pending.
        if TeleportTimers[originPlayerID] then
            -- Send a message to the player.
            self:SendMessage( player, self.Config.Messages.TeleportPending )

            return
        end

        -- Check if the player his/her target already has a teleport pending.
        if TeleportTimers[targetPlayerID] then
            -- Send a message to the player.
            self:SendMessage( player, self.Config.Messages.TeleportPendingTarget )

            return
        end

        -- Check if the player or the targetted player already has a request
        -- pending.
        if PlayersRequests[originPlayerID] or PlayersRequests[targetPlayerID] then
            -- Show a message to the players.
            self:SendMessage( player, self.Config.Messages.PendingRequest )
            self:SendMessage( originPlayer, self.Config.Messages.PendingRequestTarget )

            return
        end

        -- Start a pending request for both the players.
        PlayersRequests[originPlayerID] = targetPlayer
        PlayersRequests[targetPlayerID] = player

        -- Start the teleport request timer.
        PendingRequests[targetPlayerID] = timer.Once( self.Config.TPR.RequestDuration,
            function()
                self:RequestTimedOut( player, targetPlayer )
            end )

        -- Send a message to both players.
        self:SendMessage( player, self:Parse( self.Config.Messages.Request, { player = targetPlayer.displayName } ) )
        self:SendMessage( targetPlayer, self:Parse( self.Config.Messages.RequestTarget, { player = player.displayName } ) )
    else
        -- An invalid amount of arguments were supplied, send a message to the
        -- player with the available command possibilities.
        self:SendMessage( player, self.Config.Messages.SyntaxCommandTPR)
    end
end

-- ----------------------------------------------------------------------------
-- PLUGIN:cmdTeleportAccept( player, cmd, args )
-- ----------------------------------------------------------------------------
-- In-game '/tpa' command that allows a player to accept a teleport request
-- from another player.
-- ----------------------------------------------------------------------------
function PLUGIN:cmdTeleportAccept( player, cmd, args )
    -- Check if the TPR module is enabled.
    if not self.Config.Settings.TPREnabled then return end
    
    -- Check if the player is allowed to use the command.
    canTeleport, err = self:CanPlayerTeleport( player )
    if not canTeleport then
        -- The player isn't allowed to teleport right now, send him a message
        -- from the plugin that is blocking the teleport and cancel the
        -- teleport proces.
        self:SendMessage( player, err )

        return
    end
    
    if not player:CanBuild() then
        self:SendMessage(player, self.Config.Messages.TPRBuildingBlockedA)
        return
    end
        
    -- Determine what the player is trying to do depending on the amount of
    -- arguments that the player has supplied.
    if args.Length == 0 then
        -- The player supplied no arguments, assuming that the player is
        -- attempting to accept a teleport request from another player.

        -- Grab the player his/her Steam ID.
        local playerID = rust.UserIDFromPlayer( player )

        -- Check if the player has a pending teleport request.
        if PendingRequests[playerID] then
            -- Grab the other player.
            local originPlayer   = PlayersRequests[playerID]
            local originPlayerID = rust.UserIDFromPlayer( originPlayer )
           
            -- Setup variables with todays date and the current timestamp.
            local timestamp   = time.GetUnixTimestamp()
            local currentDate = time.GetCurrentTime():ToString("d")

            -- Perform a ceiling check if enabled.
            if self.Config.TPR.BlockTPAOnCeiling then                
                -- Modify the local position, add 2 to the y coordinate.
                local position = player.transform.position
                position.y = position.y + 1

                -- Create a local variable to store the BuildingBlock.
                local ceiling = false
                local firstHit = true

                -- Create a Ray from the player to the ground to detect what the player is standing on
                local ray = new( UnityEngine.Ray._type, util.TableToArray { player.transform.position, UnityEngine.Vector3.get_down() } )
                         
                local arr = util.TableToArray { ray, new( UnityEngine.RaycastHit._type, nil ), 1.5, -5 }
                util.ConvertAndSetOnArray(arr, 2, 1.5, System.Int64._type)
                util.ConvertAndSetOnArray(arr, 3, -5, System.Int32._type)
                 
                if Raycast:Invoke( nil, arr ) then
                    local hitEntity = global.RaycastHitEx.GetEntity(arr[1])
                    if hitEntity then
                        if hitEntity:GetComponentInParent(global.BuildingBlock._type) then
                            local buildingBlock = hitEntity:GetComponentInParent(global.BuildingBlock._type)
                            if buildingBlock.name:find( "floor", 1, true) then
                                ceiling = true
                            end
                        end              
                    end      
                end     
                
                if ceiling then
                    -- The player is trying to accept a teleport on top of a
                    -- ceiling, show the player a message.
                    self:SendMessage( player, self.Config.Messages.AcceptOnRoof )

                    return
                end
            end
            
            -- The teleport request is valid and can be accepted, send a
            -- message to both the players.
            self:SendMessage( originPlayer, self:Parse( self.Config.Messages.Accept, { player = player.displayName, countdown = self.Config.TPR.Countdown } ) ) 
            self:SendMessage( player, self:Parse( self.Config.Messages.AcceptTarget, { player = originPlayer.displayName } ) )
            
            -- Initiate the teleport timer.

            TeleportTimers[originPlayerID] = {}
            TeleportTimers[originPlayerID].originPlayer = originPlayer
            TeleportTimers[originPlayerID].targetPlayer = player
            TeleportTimers[originPlayerID].timer = timer.Once( self.Config.TPR.Countdown,
                function()
                    -- Check if the player is allowed to use the command.
                    canTeleport, err = self:CanPlayerTeleport( originPlayer )
                    if not canTeleport then
                        -- The player isn't allowed to teleport right now, send
                        -- both players a message from the plugin that is
                        -- blocking the teleport and cancel the teleport.
                        self:SendMessage( originPlayer, err )
                        self:SendMessage( player, err )

                        return
                    end

                    -- Check if the target player is allowed to use this
                    -- command.
                    canTeleport, err = self:CanPlayerTeleport( player )
                    if not canTeleport then
                        -- The player isn't allowed to teleport right now, send
                        -- both players a message from the plugin that is
                        -- blocking the teleport and cancel the teleport.
                        self:SendMessage( originPlayer, err )
                        self:SendMessage( player, err )

                        return
                    end
                    
                    if not player:CanBuild() then
                        self:SendMessage(player, self.Config.Messages.TPRBuildingBlockedT)
                        TeleportTimers[originPlayerID] = nil
                        return
                    end
                    
                    -- Teleport the player.
                    local destination = self:CheckPosition( player.transform.position, originPlayer )
                    self:Teleport( originPlayer, destination )
 
                    -- Modify the teleport amount and last teleport timestamp.
                    TeleportData.TPRData[originPlayerID].Teleports.amount = TeleportData.TPRData[originPlayerID].Teleports.amount + 1
                    TeleportData.TPRData[originPlayerID].Teleports.timestamp = timestamp
                    self:SaveData()

                    -- Send a message to both players.
                    self:SendMessage( originPlayer, self:Parse( self.Config.Messages.Success, { player = player.displayName } ) )
                    self:SendMessage( player, self:Parse( self.Config.Messages.SuccessTarget, { player = originPlayer.displayName } ) )
                    
                    -- Remove the pending timer info.
                    TeleportTimers[originPlayerID] = nil
                end )

            -- Destroy the pending request timer.
            PendingRequests[playerID]:Destroy()

            -- Remove the table entries
            PendingRequests[playerID]       = nil
            PlayersRequests[playerID]       = nil
            PlayersRequests[originPlayerID] = nil
        else
            -- The player doesn't have a pending request, show him/her a
            -- message.
            self:SendMessage( player, self.Config.Messages.NoPendingRequest )
        end
    else
        -- An invalid amount of arguments were supplied, send a message to the
        -- player with the available command possibilities.
        self:SendMessage( player, self.Config.Messages.SyntaxCommandTPA)
    end
end

-- ----------------------------------------------------------------------------
-- PLUGIN:cmdWipeHomes( player, cmd, args )
-- ----------------------------------------------------------------------------
-- In-game '/wipehomes' command to wipe all the saved homes.
-- ----------------------------------------------------------------------------
function PLUGIN:cmdWipeHomes( player, cmd, args )
    -- Check if the player is allowed to run the command.
    if not self:IsAllowed( player ) then return end

    -- Clear the data.
    TeleportData.HomeData = {}

    -- Save the data.
    self:SaveData()

    -- Send a message to the player.
    self:SendMessage( player, self.Config.Messages.HomesListWiped )
end

-- ----------------------------------------------------------------------------
-- PLUGIN:SendHelpText( player )
-- ----------------------------------------------------------------------------
-- HelpText plugin support for the command /help.
-- ----------------------------------------------------------------------------
function PLUGIN:SendHelpText(player)
    if self.Config.Settings.HomesEnabled or self.Config.Settings.TPREnabled then
        self:SendMessage( player, "Use \"/tphelp\" to see the available teleport commands." )
        self:SendMessage( player, "Use \"/tplimits\" to see the teleport limits." )
    end
end

-- ----------------------------------------------------------------------------
-- PLUGIN:cmdTeleportHelp( player, cmd, args )
-- ----------------------------------------------------------------------------
-- In-game '/tphelp' command that allows players to see all the available
-- teleport commands per module.
-- ----------------------------------------------------------------------------
function PLUGIN:cmdTeleportHelp( player, cmd, args )
    -- Check if there is a player module enabled and if the player is an admin.
    if not self.Config.Settings.TPREnabled and not self.Config.Settings.HomesEnabled and not self:IsAllowed( player ) then return end

    -- Determine what the player is trying to do depending on the amount of
    -- arguments that the player has supplied.
    if args.Length == 1 then
        -- The player supplied one argument, assuming the player wants to view
        -- the help for a specific module.

        -- Grab the request help module.
        local TPModule = string.lower( args[0] )

        -- Check if a valid module was supplied.
        if self.Config.Messages.TPHelp[TPModule] then
            -- The player supplied a valid help module, show the list of
            -- commands.
            self:SendMessage( player, self.Config.Messages.TPHelp[TPModule] )
        else
            -- The player supplied an invalid help module, show an error
            -- message to the player.
            self:SendMessage( player, self.Config.Messages.InvalidHelpModule )
        end

    else
        -- The player supplied no arguments or too much arguments, assuming
        -- that the player is attempting to view the available help modules.

        -- Send the player the general help message.
        self:SendMessage( player, self.Config.Messages.TPHelp.General )

        -- If the player is allowed to access the Admin Teleport System then 
        -- he/she is allowed to see the help commands.
        if self:IsAllowed( player ) then
            self:SendMessage( player, "/tphelp AdminTP" )
        end
        
        if self.Config.Settings.HomesEnabled then
            self:SendMessage( player, "/tphelp Home" )
        end
        
        if self.Config.Settings.TPREnabled then
            self:SendMessage( player, "/tphelp TPR" )
        end
    end
end

-- ----------------------------------------------------------------------------
-- PLUGIN:cmdTeleportLimits( player, cmd, args )
-- ----------------------------------------------------------------------------
-- In-game '/tplimits' command that allows players to see all the available 
-- teleport commands per module.
-- ----------------------------------------------------------------------------
function PLUGIN:cmdTeleportLimits( player, cmd, args )
    -- Check if there is a player module enabled.
    if not self.Config.Settings.TPREnabled and not self.Config.Settings.HomesEnabled then return end

    -- Determine what the player is trying to do depending on the amount of
    -- arguments that the player has supplied.
    if args.Length == 1 then
        -- The player supplied one argument, assuming the player wants to view
        -- the help for a specific module.

        -- Grab the request help module.
        local TPModule = string.lower( args[0] )

        -- Check if a valid module was supplied.
        if self.Config.Messages.TPSettings[TPModule] then
            -- The player supplied a valid help module, show the list of
            -- commands.
            if TPModule == "home" then
                for _,message in pairs( self.Config.Messages.TPSettings[TPModule] ) do
                    self:SendMessage( player, self:Parse( message, { cooldown = self:ParseRemainingTime( self.Config.Homes.Cooldown ), limit = self.Config.Homes.DailyLimit, amount = self.Config.Homes.HomesLimit } ) )
                end
            elseif TPModule == "tpr" then
                for _,message in pairs( self.Config.Messages.TPSettings[TPModule] ) do
                    self:SendMessage( player, self:Parse( message, { cooldown = self:ParseRemainingTime( self.Config.TPR.Cooldown ), limit = self.Config.TPR.DailyLimit } ) )
                end
            end
        else
            -- The player supplied an invalid help module, show an error
            -- message to the player.
            self:SendMessage( player, self.Config.Messages.InvalidHelpModule )
        end

    else
        -- The player supplied no arguments or too much arguments, assuming
        -- that the player is attempting to view the available help modules.

        -- Send the player the general help message.
        self:SendMessage( player, self.Config.Messages.TPSettings.General )
        
        if self.Config.Settings.HomesEnabled then
            self:SendMessage( player, "/tplimits Home" )
        end
        
        if self.Config.Settings.TPREnabled then
            self:SendMessage( player, "/tplimits TPR" )
        end
    end
end

-- ----------------------------------------------------------------------------
-- PLUGIN:ccmdTeleport( arg )
-- ----------------------------------------------------------------------------
-- Console commands to mimic the old Legacy console commands teleport.topos and 
-- teleport.toplayer.
-- ----------------------------------------------------------------------------
function PLUGIN:ccmdTeleport( arg )
    local player = nil
    local command = arg.cmd.namefull

    if arg.connection then
        player = arg.connection.player
    end

    if player and not self:IsAllowed( player ) then
        return true
    end
    
    if command == "teleport.topos" then
        if not arg.Args or arg.Args.Length ~= 4 then
            local replyMessage = table.concat( self.Config.Messages.SyntaxConsoleCommandToPos, "\r\n" )
            arg:ReplyWith( replyMessage )
        else
            local playerName = arg:GetString( 0, nil )
            local x          = arg:GetFloat( 1, -10000 )
            local y          = arg:GetFloat( 2, -10000 )
            local z          = arg:GetFloat( 3, -10000 )

            -- Search for the BasePlayer for the given (partial) name.
            local targetPlayer = self:FindPlayerByName( playerName )

            -- Check if we found the targetted player.
            if #targetPlayer == 0 then
                -- The targetted player couldn't be found, send a message to
                -- the console.
                arg:ReplyWith( self.Config.Messages.PlayerNotFound )

                return
            end

            -- Check if we found multiple players with that partial name.
            if #targetPlayer > 1 then
                -- Multiple players were found, send a message to the console.
                arg:ReplyWith( player, self.Config.Messages.MultiplePlayersFound )

                return
            else
                -- Only one player was found, modify the targetPlayer variable
                -- value.
                targetPlayer = targetPlayer[1]
            end

            -- Validate the three coordinates, first check if all three are
            -- numbers and then check if the coordinates are within the map
            -- boundaries.
            if x and y and z then
                -- The three supplied axis values are numbers, check if they
                -- are within the boundaries of the map.
                local boundary = global.TerrainMeta.get_Size().x / 2

                if ( x <= boundary and x >= -boundary ) and ( y < 2000 and y >= -100 ) and ( z <= boundary and z >= -boundary ) then
                    -- A valid location was specified, save the player his/her
                    -- current location for the '/tpb' command if necessary and
                    -- initiate a teleport.
                    if self:IsAllowed( targetPlayer ) then
                        -- The player is an admin so we need to save his/her
                        -- current location.
                        self:SaveLocation( targetPlayer )
                    end

                    self:TeleportToPosition( targetPlayer, x, y, z )

                    -- Send a message to the player
                    self:SendMessage( targetPlayer, self:Parse( self.Config.Messages.AdminTPConsoleTP, { destination = x .. " " .. y .. " " .. z } ) )
                    -- Show a message in the console.
                    arg:ReplyWith( self:Parse( self.Config.Messages.AdminTPTargetCoordinates, { player = targetPlayer.displayName, coordinates = x .. " " .. y .. " " .. z } ) )

                    return
                else
                    -- One or more axis values are out of bounds, show a
                    -- message to the player.
                    arg:ReplyWith( self.Config.Messages.AdminTPOutOfBounds .. "\r\n" .. self.Config.Messages.AdminTPBoundaries )
                    
                    return
                end
            else
                -- One or more axis values are not a number and are invalid,
                -- show a message in the console.
                arg:ReplyWith( self.Config.Messages.InvalidCoordinates )

                return
            end
        end
    elseif command == "teleport.toplayer" then
        if not arg.Args or arg.Args.Length ~= 2 then
            local replyMessage = table.concat( self.Config.Messages.SyntaxConsoleCommandToPlayer, "\r\n" )
            arg:ReplyWith( replyMessage )
        else
            -- Search for the BasePlayer for the given (partial) names.
            local originPlayer = self:FindPlayerByName( arg:GetString( 0, nil ) )
            local targetPlayer = self:FindPlayerByName( arg:GetString( 1, nil ) )

            -- Check if we found both players.
            if #originPlayer == 0 or #targetPlayer == 0 then
                -- One or both players couldn't be found, send a message to the
                -- console.
                arg:ReplyWith( self.Config.Messages.PlayerNotFound )

                return
            end

            -- Check if we found multiple players with that partial names.
            if #originPlayer > 1 or #targetPlayer > 1 then
                -- Multiple players were found, send a message to the console.
                arg:ReplyWith( self.Config.Messages.MultiplePlayersFound )

                return
            else
                -- Only one player was found, modify the targetPlayer variable
                -- value.
                originPlayer = originPlayer[1]
                targetPlayer = targetPlayer[1]
            end

            -- Check if the origin player is different from the targetted
            -- player.
            if originPlayer == targetPlayer then
                -- Both players are the same, send a message to the console.
                arg:ReplyWith( self.Config.Messages.CantTeleportPlayerToSelf )

                return
            end

            -- Check if the player is an admin.
            if self:IsAllowed( originPlayer ) then
                -- The player who's being teleported is an admin, save his/her 
                -- location for the '/tpb' command.
                self:SaveLocation( originPlayer )
            end

            -- Both players were found and are valid. Initiate a teleport for
            -- the origin player to the targetted player.
            self:TeleportToPlayer( originPlayer, targetPlayer )

            -- Show a message to both the players.
            self:SendMessage( originPlayer, self:Parse( self.Config.Messages.AdminTPConsoleTPPlayer, { player = targetPlayer.displayName } ) )
            arg:ReplyWith( self:Parse( self.Config.Messages.AdminTPPlayers, { player = originPlayer.displayName, target = targetPlayer.displayName } ) )
        end
    end

    return
end

-- ----------------------------------------------------------------------------
-- PLUGIN:OnEntityAttacked( entity, hitinfo )
-- ----------------------------------------------------------------------------
-- OnEntityAttacked Oxide Hook. This hook is triggered when an entity
-- (BasePlayer or BaseNPC) is attacked. This hook is used to interrupt
-- a teleport when a player takes damage.
-- ----------------------------------------------------------------------------
function PLUGIN:OnEntityTakeDamage( entity, hitinfo )
    -- Check if the entity taking damage is a player.
    if entity:ToPlayer() then
        -- The entity taking damage is a player, grab his/her Steam ID.
        local playerID = rust.UserIDFromPlayer( entity )

        -- Check if the player has a teleport pending.
        if TeleportTimers[playerID] then
            -- Send a message to the players or to both players.
            self:SendMessage( TeleportTimers[playerID].originPlayer, self.Config.Messages.Interrupted )

            if TeleportTimers[playerID].targetPlayer then
                self:SendMessage( TeleportTimers[playerID].targetPlayer, self:Parse( self.Config.Messages.InterruptedTarget, { player = TeleportTimers[playerID].originPlayer.displayName } ) )
            end

            -- Destroy the timer.
            TeleportTimers[playerID].timer:Destroy()

            -- Remove the table entry.
            TeleportTimers[playerID] = nil
        end

    end
end

-- ----------------------------------------------------------------------------
-- PLUGIN:OnPlayerDisconnected( player )
-- ----------------------------------------------------------------------------
-- OnPlayerDisconnected Oxide Hook. This hook is triggered when a player leaves
-- the server. This hook is used to cancel pending the teleport requests and
-- pending teleports for the disconnecting player.
-- ----------------------------------------------------------------------------
function PLUGIN:OnPlayerDisconnected( player )
    -- Grab the player his/her Steam ID.
    local playerID = rust.UserIDFromPlayer( player )

    -- Check if the player has any pending requests.
    if PendingRequests[playerID] then
        -- The player has a pending request, send a message to the player that
        -- send this request, kill the timer and remove the data.

        -- Grab the target player and his/her Steam ID.
        local originPlayer   = PlayersRequests[playerID]
        local originPlayerID = rust.UserIDFromPlayer( originPlayer )

        -- Send a message to the 
        self:SendMessage( originPlayer, self.Config.Messages.RequestTargetOff )

        -- Destroy the timer and remove the table entries.
        PendingRequests[playerID]:Destroy()
        PendingRequests[playerID] = nil
        PlayersRequests[playerID] = nil
        PlayersRequests[originPlayerID] = nil
    end

    -- Check if the player has a teleport in progress.
    if TeleportTimers[playerID] then
        -- The player is about to be teleported, cancel the teleport and remove
        -- the table entry.
        TeleportTimers[playerID].timer:Destroy()
        TeleportTimers[playerID] = nil
    end
end

-- ----------------------------------------------------------------------------
-- PLUGIN:SaveLocation( player )
-- ----------------------------------------------------------------------------
-- Save the location of the player, this is used to save the location upon
-- teleporting an admin to be able to use the ingame command '/tpb'.
-- ----------------------------------------------------------------------------
function PLUGIN:SaveLocation( player )
    -- Get the player's UserID.
    local playerID = rust.UserIDFromPlayer( player )

    -- Check if we already have an entry in the saved data for the player.
    TeleportData.AdminData[playerID] = TeleportData.AdminData[playerID] or {}

    -- If the player already has a previous location saved we will preserve
    -- that one, otherwise we will save his current location to file.
    if ( not TeleportData.AdminData[playerID].PreviousLocation ) then
        local location = player.transform.position
        local x = location.x
        local y = location.y
        local z = location.z

        -- Set the data and save it.
        TeleportData.AdminData[playerID].PreviousLocation = { x = x, y = y, z = z }
        self:SaveData()

        -- Send a message to the player.
       self:SendMessage( player, self.Config.Messages.AdminTPBackSave )
    end
end

-- ----------------------------------------------------------------------------
-- PLUGIN:RequestTimedOut( player, target )
-- ----------------------------------------------------------------------------
-- Sends a message to both players when the teleport request timed out.
-- ----------------------------------------------------------------------------
function PLUGIN:RequestTimedOut( player, target )
    -- Grab the UserIDs for both players.
    originUserID = rust.UserIDFromPlayer( player )
    targetUserID = rust.UserIDFromPlayer( target )

    -- Remove the table entries
    PlayersRequests[targetUserID] = nil
    PlayersRequests[originUserID] = nil
    PendingRequests[targetUserID] = nil

    -- Send a message to the players.
    self:SendMessage( player, self:Parse( self.Config.Messages.TimedOut, { player = target.displayName } ) )
    self:SendMessage( target, self:Parse( self.Config.Messages.TimedOutTarget, { player = player.displayName } ) )
end

-- ----------------------------------------------------------------------------
-- PLUGIN:IsAllowed( player )
-- ----------------------------------------------------------------------------
-- Checks if the player is allowed to run an admin (or moderator) only command.
-- ----------------------------------------------------------------------------
function PLUGIN:IsAllowed( player )
    -- Grab the player his AuthLevel and set the required AuthLevel.
    local playerAuthLevel = player:GetComponent("BaseNetworkable").net.connection.authLevel
    local requiredAuthLevel = 2
    
    -- Check if Moderators are also allowed to use the commands.
    if self.Config.AdminTP.UseableByModerators then
        -- Moderators are allowed to run the commands, reduce the required
        -- AuthLevel to 1.
        requiredAuthLevel = 1   
    end

    -- Compare the AuthLevel with the required AuthLevel, if it's higher or
    -- equal then the user is allowed to run the command.
    if playerAuthLevel >= requiredAuthLevel then
        return true
    end

    return false
end

-- ----------------------------------------------------------------------------
-- PLUGIN:ParseRemainingTime( time )
-- ----------------------------------------------------------------------------
-- Returns an amount of seconds as a nice time string.
-- ----------------------------------------------------------------------------
function PLUGIN:ParseRemainingTime( time )
    local minutes  = nil
    local seconds  = nil
    local timeLeft = nil

    -- If the amount of seconds is higher than 60 we'll have minutes too, so
    -- start with grabbing the amount of minutes and then take the remainder as
    -- the seconds that are left on the timer.
    if time >= 60 then
        minutes = math.floor( time / 60 )
        seconds = time - ( minutes * 60 )
    else
        seconds = time
    end

    -- Build a nice string with the remaining time.
    if minutes and seconds > 0 then
        timeLeft = minutes .. " min " .. seconds .. " sec "
    elseif minutes and seconds == 0 then
        timeLeft = minutes .. " min "
    else    
        timeLeft = seconds .. " sec "
    end

    -- Return the time string.
    return timeLeft        
end

-- ----------------------------------------------------------------------------
-- PLUGIN:TeleportToPlayer( player, targetPlayer )
-- ----------------------------------------------------------------------------
-- Teleports a player to the target player.
-- ----------------------------------------------------------------------------
function PLUGIN:TeleportToPlayer( player, targetPlayer )
    -- Set the destination for the player.
    local destination = targetPlayer.transform.position

    -- Teleport the player to the destination.
    self:Teleport( player, destination )
end

-- ----------------------------------------------------------------------------
-- PLUGIN:TeleportToPosition( player, x, y, z )
-- ----------------------------------------------------------------------------
-- Teleports a player to a set of coordinates.
-- ----------------------------------------------------------------------------
function PLUGIN:TeleportToPosition( player, x, y, z )
    -- set the destination for the player.
    local destination = new( UnityEngine.Vector3._type, nil )
    destination.x = x 
    destination.y = y
    destination.z = z

    -- Teleport the player to the destination.
    self:Teleport( player, destination )
end

-- ----------------------------------------------------------------------------
-- PLUGIN:Teleport( player, destination )
-- ----------------------------------------------------------------------------
-- Teleports a player to a specific location.
-- ----------------------------------------------------------------------------
function PLUGIN:Teleport( player, destination )
    -- Let the player sleep to prevent the player from falling through objects.
    player:ClientRPCPlayer(null, player, "StartLoading", nil, nil, nil, nil, nil);
    player:SetPlayerFlag(global.BasePlayer.PlayerFlags.Sleeping, true)
    if not global.BasePlayer.sleepingPlayerList:Contains(player) then
        global.BasePlayer.sleepingPlayerList:Add(player)
    end
    player:CancelInvoke("InventoryUpdate")
    player.inventory.crafting:CancelAll(true)
    
    player:MovePosition(destination);
    player:ClientRPCPlayer(nil, player, "ForcePositionTo", destination, nil, nil, nil, nil)
	player:TransformChanged();
    player:SetPlayerFlag(global.BasePlayer.PlayerFlags.ReceivingSnapshot, true);
    player:UpdateNetworkGroup();
    
    player:SendNetworkUpdateImmediate(false);
    player:ClientRPCPlayer(null, player, "StartLoading", nil, nil, nil, nil, nil);
    player:SendFullSnapshot();
end

-- ----------------------------------------------------------------------------
-- PLUGIN:SendMessage( target, message )
-- ----------------------------------------------------------------------------
-- Sends a chatmessage to a player.
-- ----------------------------------------------------------------------------
function PLUGIN:SendMessage( target, message )
    -- Check if we have an existing target to send the message to.
    if not target then return end
    if not target:IsConnected() then return end
    if not message then return end

    -- Check if the message is a table with multiple messages.
    if type( message ) == "table" then
        -- The message is a table with multiple messages, send them one by one.
        for _, message in pairs( message ) do
            self:SendMessage( target, message )
        end

        return
    end

    -- "Build" the message to be able to show it correctly.
    message = rust.QuoteSafe( message )

    -- Send the message to the targetted player.
    target:SendConsoleCommand( "chat.add \"" .. self.Config.Settings.ChatName .. "\""  .. message );
end

-- ----------------------------------------------------------------------------
-- PLUGIN:Parse( message, values )
-- ----------------------------------------------------------------------------
-- Replaces the parameters in a message with the corresponding values.
-- ----------------------------------------------------------------------------
function PLUGIN:Parse( message, values )
    for k, v in pairs( values ) do
        -- Replace the variable in the message with the specified value.
        tostring(v):gsub("(%%)", "%%%%") 
        message = message:gsub( "{" .. k .. "}", v)
    end

    return message
end

-- ----------------------------------------------------------------------------
-- PLUGIN:Count( tbl )
-- ----------------------------------------------------------------------------
-- Counts the elements of a table.
-- ----------------------------------------------------------------------------
function PLUGIN:Count( tbl ) 
    local count = 0

    if type( tbl ) == "table" then
        for _ in pairs( tbl ) do 
            count = count + 1 
        end
    end

    return count
end

-- ----------------------------------------------------------------------------
-- PLUGIN:FindPlayerByName( playerName )
-- ----------------------------------------------------------------------------
-- Searches the online players for a specific name.
-- ----------------------------------------------------------------------------
function PLUGIN:FindPlayerByName( playerName )
    -- Check if a player name was supplied.
    if not playerName then return end

    -- Set the player name to lowercase to be able to search case insensitive.
    playerName = string.lower( playerName )

    -- Setup some variables to save the matching BasePlayers with that partial
    -- name.
    local matches = {}
    local itPlayerList = global.BasePlayer.activePlayerList:GetEnumerator()
    
    -- Iterate through the online player list and check for a match.
    while itPlayerList:MoveNext() do
        -- Get the player his/her display name and set it to lowercase.
        local displayName = string.lower( itPlayerList.Current.displayName )
        
        -- Look for a match.
        if string.find( displayName, playerName, 1, true ) then
            -- Match found, add the player to the list.
            table.insert( matches, itPlayerList.Current )
        end

        if string.len( playerName ) == 17 then
            if string.find( rust.UserIDFromPlayer( itPlayerList.Current ), playerName ) then
                -- Match found, add the player to the list.
                table.insert( matches, itPlayerList.Current )
            end
        end
    end

    -- Return all the matching players.
    return matches
end

-- ----------------------------------------------------------------------------
-- PLUGIN:GetGround( position )
-- ----------------------------------------------------------------------------
-- Searches for a valid height for the given position.
-- ----------------------------------------------------------------------------
function PLUGIN:GetGround( position )
    -- Setup a raycast from high up to the ground.
    local arr  = util.TableToArray( { position, UnityEngine.Vector3.get_down() } )
    local hits = RaycastAll:Invoke( nil, arr )
    local nearestDistance = 9999
    local nearestPoint    = nil
    local hitsIterator = hits:GetEnumerator()

    -- Loop through all the hit objects.
    while hitsIterator:MoveNext() do
        if hitsIterator.Current.distance < nearestDistance then
            nearestDistance = hitsIterator.Current.distance
            nearestPoint = hitsIterator.Current.point
        end
    end

    -- Return the highest point.
    return nearestPoint
end

-- ----------------------------------------------------------------------------
-- PLUGIN:CheckPosition( position )
-- ----------------------------------------------------------------------------
-- Checks and corrects a player's position when teleporting with tpr/tpa so 
-- that the player can't glitch into a wall.
-- ----------------------------------------------------------------------------
function PLUGIN:CheckPosition( position, player )
    -- Setup an OverlapSphere to check for overlapping colliders within a 2m 
    -- radius of the player.
    local arr = util.TableToArray( { position, 2 } )
    util.ConvertAndSetOnArray( arr, 1, 2, System.Int32._type )
    local hits = OverlapSphere:Invoke( nil, arr )
    
    -- Setup some variables to use to iterate over the collider hits.
    local colliderIterator = hits:GetEnumerator()
    local colliderDistance = 5
    local colliderPosition = nil
    local buildingBlock    = nil

    -- Loop through all the colliders overlapping with the player.
    while ( colliderIterator:MoveNext() ) do
        -- Check if the collider is a building block, if this isn't the case 
        -- then we don't have to do anything.
        if colliderIterator.Current:GetComponentInParent( global.BuildingBlock._type ) then
            -- Temporarily  store the building block in a variable to work with
            -- it.
            local block = colliderIterator.Current:GetComponentInParent( global.BuildingBlock._type )

            -- The pushback shouldn't trigger on foundations and floors so we
            -- can exclude these.
            if not block.name:find( "foundation" ) and not block.name:find( "floor" ) and not block.name:find( "pillar" ) then
                -- Check if this is the nearest building block and save the
                -- values if this is the case.
                if UnityEngine.Vector3.Distance( block.transform.position, position ) < colliderDistance then
                    buildingBlock    = block
                    colliderDistance = UnityEngine.Vector3.Distance( block.transform.position, position ) 
                    colliderPosition = block.transform.position
                end
            end
        end
    end
    
    -- If a BuildingBlock was found within the specified radius we need to push
    -- the player back.
    if buildingBlock then
        -- Setup a few required variables to calculate the push-back location.
        local correctedLocations = {}
        local blockRotation      = nil
        local blockRotation      = buildingBlock.transform.rotation.eulerAngles.y
        local angles             = { 360 - blockRotation, 180 - blockRotation }
        local r                  = 1.9
        local location           = nil
        local locationDistance   = 100

        -- Calculate the two possible push-back locations.
        for _, angle in pairs( angles ) do
            local radians = math.rad( angle )
            local newX = r * math.cos( radians )
            local newZ = r * math.sin( radians )
            local newLoc = new( UnityEngine.Vector3._type, nil )

            newLoc.x = colliderPosition.x + newX
            newLoc.y = colliderPosition.y + 0.2
            newLoc.z = colliderPosition.z + newZ

            table.insert (correctedLocations, newLoc )
        end

        -- Check which position of the calculated positions should be used.
        for _, newPosition in pairs( correctedLocations ) do
            if UnityEngine.Vector3.Distance( position, newPosition ) < locationDistance then
                location         = newPosition
                locationDistance = UnityEngine.Vector3.Distance( position, newPosition )
            end
        end
        
        -- Return a new location a bit further away from the wall, at the 
        -- same height.
        if position.y - location.y > 2.5 then
            location.y = position.y
        end
        
        return location
    end

    return position
end

-- ----------------------------------------------------------------------------
-- PLUGIN:GetOwner( buildingBlock )
-- ----------------------------------------------------------------------------
-- Checks the owner of a specific building block in the Building Owners plugin
-- data.
-- ----------------------------------------------------------------------------
function PLUGIN:GetOwner( buildingBlock )
    -- Setup the array to use to invoke the FindBlockData method from the
    -- BuildingOwners plugin.
    local arr = util.TableToArray( { buildingBlock } )
    util.ConvertAndSetOnArray( arr, 0, buildingBlock, UnityEngine.Object._type )

    -- Get the owner id.
    local ownerID = plugins.CallHook( "FindBlockData", arr )

    -- Return the owner id.
    return ownerID
end

-- ----------------------------------------------------------------------------
-- PLUGIN:CanPlayerTeleport( player )
-- ----------------------------------------------------------------------------
-- Function that checks all other plugins if the player is allowed to teleport,
-- this is required for the Zones plugin by Reneb:
-- http://forum.rustoxide.com/plugins/r-zones.739/
-- ----------------------------------------------------------------------------
function PLUGIN:CanPlayerTeleport( player )
    local arr = util.TableToArray( { player } )
    util.ConvertAndSetOnArray( arr, 0, player, UnityEngine.Object._type )
    local _return = plugins.CallHook( "CanTeleport", arr )

    if not _return then
        return true
    end

    return false, tostring( _return )
end
