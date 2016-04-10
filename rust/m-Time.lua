
-- -----------------------------------------------------------------------------------
-- Rust Day & Night System                                               Version 1.0.3
-- -----------------------------------------------------------------------------------
-- Filename:          m-Time.lua
-- Last Modification: 01-21-2015
-- -----------------------------------------------------------------------------------
-- Description:
--
-- This plugin is developed for Rust servers with the Oxide Server Mod and will offer
-- server admins the option to change the current in-game time, modify the length of
-- the days and even freeze the time.
-- -----------------------------------------------------------------------------------


PLUGIN.Title       = "Day & Night System"
PLUGIN.Description = "Change the current time, change the daylength or even freeze the time!"
PLUGIN.Version     = V( 1, 0, 3 )
PLUGIN.HasConfig   = true
PLUGIN.Author      = "Mughisi"
PLUGIN.ResourceId  = 671


-- -----------------------------------------------------------------------------------
-- Globals
-- -----------------------------------------------------------------------------------
-- Some globals that are used in multiple functions.
-- -----------------------------------------------------------------------------------
local Sky  = nil
local Time = nil

local TimeData = nil

local Sunrise        = 6
local Sunset         = 18
local TimeTickUpdate = false

local DayNightTimer  = nil
local DayNightUpdate = nil
local FrozenTimer    = nil
local UpdateInterval = nil

-- -----------------------------------------------------------------------------------
-- PLUGIN:Init()
-- -----------------------------------------------------------------------------------
-- On plugin initialisation the required in-game chat commands are registered and data
-- from the DataTable file is loaded.
-- -----------------------------------------------------------------------------------
function PLUGIN:Init()
    -- Add the admin chat commands.
    command.AddChatCommand("settime",        self.Plugin, "cmdSetTime")
    command.AddChatCommand("setdaylength",   self.Plugin, "cmdSetDayLength")
    command.AddChatCommand("setnightlength", self.Plugin, "cmdSetNightLength")
    command.AddChatCommand("freezetime",     self.Plugin, "cmdFreezeTime")
    command.AddChatCommand("unfreezetime",   self.Plugin, "cmdUnfreezeTime")

    -- Add the player commands.
    command.AddChatCommand("time",  self.Plugin, "cmdTime" )

    -- Add the admin console commands.
    command.AddConsoleCommand("env.freeze",      self.Plugin, "ccmdEnvTime")
    command.AddConsoleCommand("env.unfreeze",    self.Plugin, "ccmdEnvTime")
    command.AddConsoleCommand("env.daylength",   self.Plugin, "ccmdEnvTime")
    command.AddConsoleCommand("env.nightlength", self.Plugin, "ccmdEnvTime")

    -- Load the saved data.
    self:LoadSavedData()

    -- Check the config.
    self:CheckConfig()
end

-- -----------------------------------------------------------------------------------
-- PLUGIN:Unload()
-- -----------------------------------------------------------------------------------
-- On plugin unloading all the running timers are destroyed to prevent problems.
-- -----------------------------------------------------------------------------------
function PLUGIN:Unload()
    -- Destroy the day/night modifier timer if it is running.
    if DayNightTimer then
        DayNightTimer:Destroy()
    end

    -- Destroy the freeze timer if it is running.
    if FrozenTimer then
        FrozenTimer:Destroy()
    end
end

-- -----------------------------------------------------------------------------------
-- PLUGIN:LoadSavedData()
-- -----------------------------------------------------------------------------------
-- Load the DataTable file into a table or create a new table when the file doesn't
-- exist yet.
-- -----------------------------------------------------------------------------------
function PLUGIN:LoadSavedData()
    -- Open the datafile if it exists, otherwise we'll create a new one.
    TimeData = datafile.GetDataTable("m-Time")
    TimeData = TimeData or { }
end

-- -----------------------------------------------------------------------------------
-- PLUGIN:SaveData()
-- -----------------------------------------------------------------------------------
-- Saves the table with all the teleportdata to a DataTable file.
-- -----------------------------------------------------------------------------------
function PLUGIN:SaveData()
    -- Save the DataTable
    datafile.SaveDataTable("m-Time")
end

-- -----------------------------------------------------------------------------------
-- PLUGIN:OnServerInitialized()
-- -----------------------------------------------------------------------------------
-- When the server has finished the startup process the plugin will grab the world's
-- TOD_Sky and TOD_Time components. At this point saved values will also be loaded.
-- -----------------------------------------------------------------------------------
function PLUGIN:OnServerInitialized()
    -- Get the Sky and Time components of the world.
    Sky = global.TOD_Sky.get_Instance()
    Time = Sky:GetComponent( "TOD_Components" ):GetComponent( "TOD_Time" )

    -- Disable the time curve to be able to handle time better. This is also more 
    -- realistic, a minute during the night should be as long as a minute during
    -- the day!
    Time.UseTimeCurve = false

    -- Set the DayLengthInMinutes if durations are specified for both day and night
    -- and when time is not frozen.
    if TimeData.DayDuration and TimeData.NightDuration and not TimeData.Frozen then
        -- Time is not frozen, check if the time matches day or night and set the
        -- DayLengthInMinutes accordingly.
        if Sky.Cycle.Hour >= Sunrise and Sky.Cycle.Hour <= Sunset then
            -- Day
            Time.DayLengthInMinutes = TimeData.DayDuration * 2
        else
            -- Night
            Time.DayLengthInMinutes = TimeData.NightDuration * 2
        end
    end

    -- Determine the updateInterval by looking at the DayLengthInMinutes field of
    -- TOD_Time.
    UpdateInterval = Time.DayLengthInMinutes * 60 / 24

    -- Check if the time should be frozen, if it should be then we need to set the
    -- current time to the frozen time and instantiate a timer to keep the time more
    -- or less the same over time. If the time isn't frozen check if the duration has
    -- been set for both day and night and instantiate a timer to check the time each
    -- in-game hour to be able to determine when to adjust the DayLengthInMinutes and
    -- to modify the delay of the timer for switches between day and night.
    if TimeData.Hour and TimeData.Frozen then
        -- Set the time after a few seconds, this won't work if we load it directly 
        -- because TOD_Sky data is still being loaded.
        timer.Once( 3, function() Sky.Cycle.Hour = TimeData.Hour end, self.Plugin )
        
        -- Modify DayLengthInMinutes and the update interval.
        Time.DayLengthInMinutes = 60
        UpdateInterval = Time.DayLengthInMinutes * 60 / 24 / 2

        -- Start the FrozenTimer.
        FrozenTimer = timer.Repeat( UpdateInterval, 0, function() Sky.Cycle.Hour = TimeData.Hour end )
    elseif TimeData.DayDuration and TimeData.NightDuration then
        -- Start the DayNightTimer.
        DayNightTimer = timer.Repeat( UpdateInterval, 0, function() self:TimeTick() end )
    end
end

-- -----------------------------------------------------------------------------------
-- PLUGIN:TimeTick()
-- -----------------------------------------------------------------------------------
-- Timer callback to modify the server TOD_Time.DayLengthInMinutes value to allow the
-- server admin to individually modify the day and night lengths.
-- -----------------------------------------------------------------------------------
function PLUGIN:TimeTick()
    -- Round the hour so that we have an exact value to work with.
    Sky.Cycle.Hour = math.floor( Sky.Cycle.Hour + 0.5 )

    -- Check if the current time is day or night. Need to subtract 1 of both sunrise
    -- and sunset because we need to modify the delay of the timer 1 in-game hour
    -- before we change the length of the day or night.
    if Sky.Cycle.Hour >= ( Sunrise - 1 ) and Sky.Cycle.Hour < ( Sunset - 1 ) then
        -- Current time is day, check if all values are set or modify them where
        -- needed.

        -- Check if the DayLengthInMinutes needs to be updated to increase the
        -- length of the day.
        if DayNightUpdate then
            Time.DayLengthInMinutes = TimeData.DayDuration * 2
            DayNightUpdate = false
        end

        -- Check if the interval of the timer needs to be modified to change
        -- the time between timer ticks during the day.
        if Time.DayLengthInMinutes ~= TimeData.DayDuration * 2 then
            DayNightUpdate = true
            UpdateInterval = TimeData.DayDuration * 2 * 60 / 24
            DayNightTimer.Delay = UpdateInterval
        end
    else
        -- Current time is night, check if all values are set or modify them where
        -- needed.

        -- Check if the DayLengthInMinutes needs to be updated to increase the
        -- length of the night.
        if DayNightUpdate then
            Time.DayLengthInMinutes = TimeData.NightDuration * 2
            DayNightUpdate = false
        end
        
        -- Check if the interval of the timer needs to be modified to change
        -- the time between timer ticks during the night.
        if Time.DayLengthInMinutes ~= TimeData.NightDuration * 2 then
            DayNightUpdate = true
            UpdateInterval = TimeData.NightDuration * 2 * 60 / 24
            DayNightTimer.Delay = UpdateInterval
        end
    end
end

-- -----------------------------------------------------------------------------------
-- PLUGIN:LoadDefaultConfig()
-- -----------------------------------------------------------------------------------
-- The plugin uses a configuration file to save certain settings and uses it for
-- localized messages that are send in-game to the admins. When this file doesn't
-- exist a new one will be created with these default values.
-- -----------------------------------------------------------------------------------
function PLUGIN:LoadDefaultConfig()
    -- General Settings:
    self.Config.Settings = {
        ChatName           = "Time",
        Version            = "1.0"
    }

    -- Plugin Messages:
    self.Config.Messages = {
        -- Messages involving /settime and env.time
        SetTimeSuccess        = "Changing the time to {hour}, please wait a moment.",
        SyntaxCommandSetTime  = {
            "A Syntax Error Occurred!",
            "You can only use the /settime command as follows:",
            "/settime <hour> - Sets the in-game time to the specified hour, 0 to 24."
        },
        SyntaxCCommandSetTime = {
            "A Syntax Error Occurred!",
            "You can only use the env.time command as follows:",
            "env.time <hour> - Sets the in-game time to the specified hour, 0 to 24."
        },

        -- Messages involving /freezetime and env.freeze
        FreezeTimeSuccess       = "You have stopped the time from progressing.",
        FreezeTimeAlreadyFrozen = "Time is already frozen!",

        -- Messages involving /unfreezetime and env.unfreeze
        UnfreezeTimeSuccess        = "You have started time progression.",
        UnfreezeTimeAlreadyRunning = "Time is not frozen!",

        -- Messages involving /setdayduration and env.daylength
        SetDayLengthSuccess        = "You have modified the length of an in-game day to {minutes} minutes.",
        SyntaxCommandSetDayLength  = {
            "A Syntax Error Occurred!",
            "You can only use the /setdaylength command as follows:",
            "/setdaylength <length> - Changes the duration of an in-game day to length.",
            "Length is in minutes and needs to be a value between 5 and 720!"
        },
        SyntaxCCommandSetDayLength = {
            "A Syntax Error Occurred!",
            "You can only use the env.daylength command as follows:",
            "env.daylength <length> - Changes the duration of an in-game day to length.",
            "Length is in minutes and needs to be a value between 5 and 720!"
        },

        -- Messages involving /setnightduration and env.nightlength
        SetNightLengthSuccess        = "You have modified the length of an in-game night to {minutes} minutes.",
        SyntaxCommandSetNightLength  = {
            "A Syntax Error Occurred!",
            "You can only use the /setnightlength command as follows:",
            "/setdaylength <length> - Changes the duration of an in-game day to length.",
            "Length is in minutes and needs to be a value between 5 and 720!"
        },
        SyntaxCCommandSetNightLength = {
            "A Syntax Error Occurred!",
            "You can only use the env.nightlength command as follows:",
            "env.nightlength <length> - Changes the duration of an in-game day to length.",
            "Length is in minutes and needs to be a value between 5 and 720!"
        },

        -- Messages involving /time
        Time = "It's currently {time}",
        TimeFrozen = "Time is currently frozen."
    }
end

-- -----------------------------------------------------------------------------------
-- PLUGIN:CheckConfig()
-- -----------------------------------------------------------------------------------
-- This function checks if the configuration file is up to date and starts an update
-- if this is not the case.
-- -----------------------------------------------------------------------------------
function PLUGIN:CheckConfig() 
    -- Check if the current plugin version is the latest.
    if self.Config.Settings.Version ~= "1.0" then
        -- Different configuration version, update it now.
        self:UpdateConfig()
    end
end

-- -----------------------------------------------------------------------------------
-- PLUGIN:UpdateConfig()
-- -----------------------------------------------------------------------------------
-- This function updates the configuration file.
-- -----------------------------------------------------------------------------------
function PLUGIN:UpdateConfig()
    self:LoadDefaultConfig()
    self:SaveConfig()
    print( "m-Time.lua : Default Config Loaded" )
end

-- -----------------------------------------------------------------------------------
-- PLUGIN:cmdSetTime( player, cmd, args )                                Admin Command
-- -----------------------------------------------------------------------------------
-- In-game '/settime' command for server admins to be able to modify the current time.
-- -----------------------------------------------------------------------------------
function PLUGIN:cmdSetTime( player, cmd, args )
    -- Check if the player is allowed to run the command.
    if player.net.connection.authLevel == 0 then 
        return 
    end

    -- Check if the player specified an argument.
    if args.Length == 1 then
        -- The player specified an argument, checking if this is a number.
        local newHour = tonumber( args[0] )

        -- Checking if the new hour is valid.
        if newHour >= 0 and newHour <= 24 then
            -- The new hour is valid, modify the time and inform the player.
            self:ModifyTime( math.floor( newHour ) )
            self:SendMessage( player, self:Parse( self.Config.Messages.SetTimeSuccess, { hour = math.floor( newHour ) } ) )

            return
        end
    end

    -- Something went wrong, show a syntax error to the player.
    self:SendMessage( player, self.Config.Messages.SyntaxCommandSetTime )
end

-- -----------------------------------------------------------------------------------
-- PLUGIN:cmdFreezeTime( player, cmd, args )                             Admin Command
-- -----------------------------------------------------------------------------------
-- In-game '/freezetime' command for server admins to be able to stop the time from
-- progressing.
-- -----------------------------------------------------------------------------------
function PLUGIN:cmdFreezeTime( player, cmd, args )
    -- Check if the player is allowed to run the command.
    if player.net.connection.authLevel == 0 then 
        return 
    end

    -- Check if the time is already frozen or not.
    if TimeData.Frozen then
        -- Time is already frozen, send the player a message.
        self:SendMessage( player, self.Config.Messages.FreezeTimeAlreadyFrozen )
    else
        -- Time isn't frozen, freeze it and send the player a message.
        self:ModifyProgression( true )
        self:SendMessage( player, self.Config.Messages.FreezeTimeSuccess )
    end
end

-- -----------------------------------------------------------------------------------
-- PLUGIN:cmdUnfreezeTime( player, cmd, args )                           Admin Command
-- -----------------------------------------------------------------------------------
-- In-game '/unfreezetime' command for server admins to be able to start the time 
-- progression again after freezing it.
-- -----------------------------------------------------------------------------------
function PLUGIN:cmdUnfreezeTime( player, cmd, args )
    -- Check if the player is allowed to run the command.
    if player.net.connection.authLevel == 0 then 
        return 
    end

    -- Check if the time is frozen or not.
    if TimeData.Frozen then
        -- Time is already frozen, unfreeze it and send the player a message.
        self:ModifyProgression( false )
        self:SendMessage( player, self.Config.Messages.UnfreezeTimeSuccess )
    else
        -- Time isn't frozen, send the player a message.
        self:SendMessage( player, self.Config.Messages.UnfreezeTimeAlreadyRunning )
    end
end

-- -----------------------------------------------------------------------------------
-- PLUGIN:cmdSetDayLength( player, cmd, args )                           Admin Command
-- -----------------------------------------------------------------------------------
-- In-game '/setdaylength' command for server admins to be able to change the length
-- of the day.
-- -----------------------------------------------------------------------------------
function PLUGIN:cmdSetDayLength( player, cmd, args )
    -- Check if the player is allowed to run the command.
    if player.net.connection.authLevel == 0 then 
        return 
    end

    -- Check if the player specified an argument.
    if args.Length == 1 then
        -- The player specified an argument, checking if this is a number.
        local newLength = tonumber( args[0] )

        -- Checking if the new length is valid.
        if newLength >= 5 and newLength <= 720 then
            -- The new hour is valid, modify the time and inform the player.
            self:ModifyDayLength( newLength)
            self:SendMessage( player, self:Parse( self.Config.Messages.SetDayLengthSuccess, { minutes = newLength } ) )

            return
        end
    end

    -- Something went wrong, show a syntax error to the player.
    self:SendMessage( player, self.Config.Messages.SyntaxCommandSetDayLength )
end

-- -----------------------------------------------------------------------------------
-- PLUGIN:cmdSetNightLength( player, cmd, args )                         Admin Command
-- -----------------------------------------------------------------------------------
-- In-game '/setnightlength' command for server admins to be able to change the length
-- of the night.
-- -----------------------------------------------------------------------------------
function PLUGIN:cmdSetNightLength( player, cmd, args )
    -- Check if the player is allowed to run the command.
    if player.net.connection.authLevel == 0 then 
        return 
    end

    -- Check if the player specified an argument.
    if args.Length == 1 then
        -- The player specified an argument, checking if this is a number.
        local newLength = tonumber( args[0] )

        -- Checking if the new length is valid.
        if newLength >= 5 and newLength <= 720 then
            -- The new hour is valid, modify the time and inform the player.
            self:ModifyNightLength( newLength)
            self:SendMessage( player, self:Parse( self.Config.Messages.SetNightLengthSuccess, { minutes = newLength } ) )

            return
        end
    end

    -- Something went wrong, show a syntax error to the player.
    self:SendMessage( player, self.Config.Messages.SyntaxCommandSetNightLength )
end

-- -----------------------------------------------------------------------------------
-- PLUGIN:cmdTime( player, cmd, args )
-- -----------------------------------------------------------------------------------
-- In-game '/time' command for players to check the current in-game time.
-- -----------------------------------------------------------------------------------
function PLUGIN:cmdTime( player, cmd, args )
    -- Setup a variable to store the time value in.
    local time = nil

    -- Get the hour, minutes and seconds. Depending on if the time is frozen or not we
    -- return the actual value or just the hour with a message that time is frozen or
    -- not.
    if TimeData.Frozen then
        time = TimeData.Hour .. ":00:00, " .. self.Config.Messages.TimeFrozen
    else
        time = Sky.Cycle.DateTime:ToString("HH:mm:ss")
    end

    -- Send the player a message.
    self:SendMessage( player, self:Parse( self.Config.Messages.Time, { time = time } ) )
end

-- -----------------------------------------------------------------------------------
-- PLUGIN:ccmdEnvTime( arg )                                     Admin Console Command
-- -----------------------------------------------------------------------------------
-- Various time related console commands for server admins.
-- env.time <hour>  - Set the current in-game time.
-- env.freezetime   - Disable time progression and freezes the current time.
-- env.unfreezetime - Enables time progression.
-- env.daylength    - Changes the length of in-game days.
-- env.nightlength  - Changes the length of in-game nights.
-- -----------------------------------------------------------------------------------
function PLUGIN:ccmdEnvTime( arg )
    -- Setup a few variables.
    local player = nil
    local command = arg.cmd.namefull

    -- Check if the command is used in the in-game console or an external console.
    if arg.connection then
        player = arg.connection.player
    end

    -- Check if the player is allowed to run the command.
    if player then
        if player.net.connection.authLevel == 0 then
            return
        end
    end
   
    -- Check which console command the user is trying to run.
    if command == "env.time" then
        -- Check if the user specified an argument.
        if arg.Args and arg.Args.Length == 1 then
            -- The player specified an argument, checking if this is a number.
            local newHour = tonumber( arg.Args[0] )

            -- Checking if the new hour is valid.
            if newHour >= 0 and newHour <= 24 then
                -- The new hour is valid, modify the time and inform the player.
                self:ModifyTime( math.floor( newHour ) )
                arg:ReplyWith( self:Parse( self.Config.Messages.SetTimeSuccess, { hour = math.floor( newHour ) } ) )

                return
            end
        end
        
        -- Something went wrong, show a syntax error to the player.
        arg:ReplyWith( table.concat( self.Config.Messages.SyntaxCommandSetTime, "\r\n" ) )

        return

    elseif command == "env.freeze" then
        if TimeData.Frozen then
            -- Time is already frozen, send the player a message.
            arg:ReplyWith( self.Config.Messages.FreezeTimeAlreadyFrozen )
            
            return
        else
            -- Time isn't frozen, freeze it and send the player a message.
            self:ModifyProgression( true )
            arg:ReplyWith( self.Config.Messages.FreezeTimeSuccess )

            return
        end

    elseif command == "env.unfreeze" then
        if TimeData.Frozen then
            -- Time is already frozen, unfreeze it and send the player a message.
            self:ModifyProgression( false )
            arg:ReplyWith( self.Config.Messages.UnfreezeTimeSuccess )

            return
        else
            -- Time isn't frozen, send the player a message.
            arg:ReplyWith( self.Config.Messages.UnfreezeTimeAlreadyRunning )

            return
        end
    
    elseif command == "env.daylength" then
        -- Check if the user specified an argument.
        if arg.Args and arg.Args.Length == 1 then
            -- The player specified an argument, checking if this is a number.
            local newLength = tonumber( arg.Args[0] )

            -- Checking if the new length is valid.
            if newLength >= 5 and newLength <= 720 then
                -- The new length is valid, modify the time and inform the player.
                self:ModifyDayLength( newLength )
                arg:ReplyWith( self:Parse( self.Config.Messages.SetDayLengthSuccess, { minutes = math.floor( newLength ) } ) )

                return
            end
        end
        
        -- Something went wrong, show a syntax error to the player.
        arg:ReplyWith( table.concat( self.Config.Messages.SyntaxCCommandSetDayLength, "\r\n" ) )

        return

    elseif command == "env.nightlength" then
        -- Check if the user specified an argument.
        if arg.Args and arg.Args.Length == 1 then
            -- The player specified an argument, checking if this is a number.
            local newLength = tonumber( arg.Args[0] )

            -- Checking if the new length is valid.
            if newLength >= 5 and newLength <= 720 then
                -- The new length is valid, modify the time and inform the player.
                self:ModifyNightLength( newLength )
                arg:ReplyWith( self:Parse( self.Config.Messages.SetNightLengthSuccess, { minutes = math.floor( newLength ) } ) )

                return
            end
        end
        
        -- Something went wrong, show a syntax error to the player.
        arg:ReplyWith( table.concat( self.Config.Messages.SyntaxCCommandSetNightLength, "\r\n" ) )

        return
    end
end

-- -----------------------------------------------------------------------------------
-- PLUGIN:ModifyTime( hour )
-- -----------------------------------------------------------------------------------
-- Helper function to modify the current time to the specified hour and modifying the
-- DayNightTimer when required.
-- -----------------------------------------------------------------------------------
function PLUGIN:ModifyTime( hour )
    -- Set the new time value.
    Sky.Cycle.Hour = hour   

    -- Save the value to the datafile.
    TimeData.Hour = hour
    self:SaveData()

    -- Check if a DayNightTimer is running, if this would be running the timer needs
    -- to be reset so that the change of time does not mess up the day and night
    -- cycle.
    if DayNightTimer and not TimeData.Frozen then
        -- Destroy the current timer.
        DayNightTimer:Destroy()

        -- Update the DayLengthInMinutes depending on if it's day or night.
        if Sky.Cycle.Hour >= Sunrise and Sky.Cycle.Hour <= Sunset then
            -- Day
            Time.DayLengthInMinutes = TimeData.DayDuration * 2
        else
            -- Night
            Time.DayLengthInMinutes = TimeData.NightDuration * 2
        end

        -- Change the update interval of the timer.
        UpdateInterval = Time.DayLengthInMinutes * 60 / 24

        -- Start a new timer.
        DayNightTimer = timer.Repeat( UpdateInterval, 0, function() self:TimeTick() end )
    end
end

-- -----------------------------------------------------------------------------------
-- PLUGIN:ModifyProgression( IsFrozen )
-- -----------------------------------------------------------------------------------
-- Helper function to enable or disable the time progression.
-- -----------------------------------------------------------------------------------
function PLUGIN:ModifyProgression( IsFrozen )
    -- Save the value to the datafile.
    TimeData.Frozen = IsFrozen
    self:SaveData()

    -- Check if the time progression should be enabled or disabled.
    if IsFrozen then
        -- Modify DayLengthInMinutes and the update interval.
        Time.DayLengthInMinutes = 60
        UpdateInterval = Time.DayLengthInMinutes * 60 / 24 / 2

        -- Time is frozen, so to disable the time progression we will run a timer
        -- repeatedly to set the time back to the requested time.
        FrozenTimer = timer.Repeat( UpdateInterval, 0, function() Sky.Cycle.Hour = TimeData.Hour end )

        -- Check if we have a DayNightTimer running, if we have this running then it
        -- is safe to destroy this as this is not required when the time is frozen.
        if DayNightTimer then
            DayNightTimer:Destroy()
        end
    else
        -- Time is no longer frozen, so to enable the time progression again we will 
        -- destroy the timer.
        FrozenTimer:Destroy()

        -- Check if a DayNightTimer needs to run, if both the length  of days and
        -- nights are saved we will create a new DayNightTimer to use these values.
        if TimeData.DayDuration and TimeData.NightDuration then
            -- Round the hour so that we have an exact value to work with.
            Sky.Cycle.Hour = math.floor( Sky.Cycle.Hour + 0.5 )

            -- Update the DayLengthInMinutes depending on if it's day or night.
            if Sky.Cycle.Hour >= Sunrise and Sky.Cycle.Hour <= Sunset then
                -- Day
                Time.DayLengthInMinutes = TimeData.DayDuration * 2
            else
                -- Night
                Time.DayLengthInMinutes = TimeData.NightDuration * 2
            end

            -- Change the update interval of the timer.
            UpdateInterval = Time.DayLengthInMinutes * 60 / 24

            -- Start a new timer.
            DayNightTimer = timer.Repeat( UpdateInterval, 0, function() self:TimeTick() end )
        end
    end
end

-- -----------------------------------------------------------------------------------
-- PLUGIN:ModifyDayLength( length )
-- -----------------------------------------------------------------------------------
-- Helper function to modify the length of an in-game day in minutes and to create
-- a new DayNightTimer.
-- -----------------------------------------------------------------------------------
function PLUGIN:ModifyDayLength( length )
    -- Save the value to the datafile.
    TimeData.DayDuration = length
    self:SaveData()
    
    -- Check if a DayNightTimer needs to run, if both the length of days and nights 
    -- are saved we will create a new DayNightTimer to use these values.
    if TimeData.DayDuration and TimeData.NightDuration and not FrozenTimer then
        -- Check if there is already a timer running, if this is the case then
        -- destroy it and start a new one.
        if DayNightTimer then
            DayNightTimer:Destroy()
        end
        
        -- Round the hour so that we have an exact value to work with.
        Sky.Cycle.Hour = math.floor( Sky.Cycle.Hour + 0.5 )

        -- Update the DayLengthInMinutes depending on if it's day or night.
        if Sky.Cycle.Hour >= Sunrise and Sky.Cycle.Hour <= Sunset then
            -- Day
            Time.DayLengthInMinutes = TimeData.DayDuration * 2
        else
            -- Night
            Time.DayLengthInMinutes = TimeData.NightDuration * 2
        end

        -- Change the update interval of the timer.
        UpdateInterval = Time.DayLengthInMinutes * 60 / 24

        -- Start a new timer.
        DayNightTimer = timer.Repeat( UpdateInterval, 0, function() self:TimeTick() end )
    end
end

-- -----------------------------------------------------------------------------------
-- PLUGIN:ModifyNightLength( length )
-- -----------------------------------------------------------------------------------
-- Helper function to modify the length of an in-game night in minutes and to create 
-- a new DayNightTimer.
-- -----------------------------------------------------------------------------------
function PLUGIN:ModifyNightLength( length )
    -- Save the value to the datafile.
    TimeData.NightDuration = length
    self:SaveData()
    
    -- Check if a DayNightTimer needs to run, if both the length of days and nights 
    -- are saved we will create a new DayNightTimer to use these values.
    if TimeData.DayDuration and TimeData.NightDuration and not FrozenTimer then
        -- Check if there is already a timer running, if this is the case then
        -- destroy it and start a new one.
        if DayNightTimer then
            DayNightTimer:Destroy()
        end
        
        -- Round the hour so that we have an exact value to work with.
        Sky.Cycle.Hour = math.floor( Sky.Cycle.Hour + 0.5 )

        -- Update the DayLengthInMinutes depending on if it's day or night.
        if Sky.Cycle.Hour >= Sunrise and Sky.Cycle.Hour <= Sunset then
            -- Day
            Time.DayLengthInMinutes = TimeData.DayDuration * 2
        else
            -- Night
            Time.DayLengthInMinutes = TimeData.NightDuration * 2
        end

        -- Change the update interval of the timer.
        UpdateInterval = Time.DayLengthInMinutes * 60 / 24

        -- Start a new timer.
        DayNightTimer = timer.Repeat( UpdateInterval, 0, function() self:TimeTick() end )
    end
end

-- -----------------------------------------------------------------------------
-- PLUGIN:SendMessage( target, message )
-- -----------------------------------------------------------------------------
-- Sends a chatmessage to a player.
-- -----------------------------------------------------------------------------
function PLUGIN:SendMessage( target, message )
    -- Check if we have an existing target to send the message to.
    if not target then return end
    if not target:IsConnected() then return end

    -- Check if the message is a table with multiple messages.
    if type( message ) == "table" then
        -- The message is a table with multiple messages, send them one by one.
        for _, message in pairs( message ) do
            self:SendMessage( target, message )
        end

        return
    end

    -- "Build" the message to be able to show it correctly.
    message = UnityEngine.StringExtensions.QuoteSafe( message )

    -- Send the message to the targetted player.
    target:SendConsoleCommand( "chat.add \"" .. self.Config.Settings.ChatName .. "\""  .. message );
end

-- -----------------------------------------------------------------------------
-- PLUGIN:Parse( message, values )
-- -----------------------------------------------------------------------------
-- Replaces the parameters in a message with the corresponding values.
-- -----------------------------------------------------------------------------
function PLUGIN:Parse( message, values )
    for k, v in pairs( values ) do
        -- Replace the variable in the message with the specified value.
        tostring(v):gsub("(%%)", "%%%%") 
        message = message:gsub( "{" .. k .. "}", v)
    end

    return message
end

