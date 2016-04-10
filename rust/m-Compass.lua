
-- -----------------------------------------------------------------------------------
-- Simple In-Game Compass                                                Version 1.0.0
-- -----------------------------------------------------------------------------------
-- Filename:          m-Compass.lua
-- Last Modification: 11-02-2014
-- -----------------------------------------------------------------------------------
-- Description:
--
-- This plugin is developed for Rust servers with the Oxide Server Mod and will offer
-- players to check in which direction they're running.
-- -----------------------------------------------------------------------------------


PLUGIN.Title = "Compass"
PLUGIN.Description = "Allows a player to check his/her direction."
PLUGIN.Version = V( 1, 0, 0 )
PLUGIN.HasConfig = true
PLUGIN.Author = "Mughisi"


-- -----------------------------------------------------------------------------------
-- PLUGIN:Init()
-- -----------------------------------------------------------------------------------
-- On plugin initialisation the required in-game chat command is registered and the
-- configuration file is checked.
-- -----------------------------------------------------------------------------------
function PLUGIN:Init()
    -- Set the chat command.
    command.AddChatCommand("compass", self.Object, "cmdCompass")
end


-- -----------------------------------------------------------------------------------
-- PLUGIN:LoadDefaultConfig()
-- -----------------------------------------------------------------------------------
-- The plugin uses a configuration file to save certain settings and uses it for
-- localized messages that are send in-game to the admins. When this file doesn't
-- exist a new one will be created with these default values.
-- -----------------------------------------------------------------------------------
function PLUGIN:LoadDefaultConfig()
    -- General Setting:
    self.Config.Settings = {
        ChatName = "Compass",
        Version = "1.0.0"
    }

    -- Plugin Messages:
    self.Config.Messages = {
        OnUse = "You are facing {direction}"
    }

    self.Config.Directions = {
        N = "North",
        NE = "Northeast",
        E = "East",
        SE = "Southeast",
        S = "South",
        SW = "Southwest",
        W = "West",
        NW = "Northwest"
    }
end

-- -----------------------------------------------------------------------------------
-- PLUGIN:cmdCompass( player, cmd, args )
-- -----------------------------------------------------------------------------------
-- In-game '/compass' command for players to check the direction they're heading in.
-- -----------------------------------------------------------------------------------
function PLUGIN:cmdCompass(player, cmd, args)
    -- Grab the look rotation of the player, do this twice to prevent issues with the 
    -- rotation.
    local lookRotation = player.eyes.rotation
    lookRotation = player.eyes.rotation.eulerAngles.y
    local direction = nil

    -- Translate the angle into a position.
    if lookRotation > 337.5 or lookRotation < 22.5 then 
        direction = self.Config.Directions.N 
    elseif lookRotation > 22.5 and lookRotation < 67.5 then
        direction = self.Config.Directions.NE 
    elseif lookRotation > 67.5 and lookRotation < 112.5 then
        direction = self.Config.Directions.E 
    elseif lookRotation > 112.5 and lookRotation < 157.5 then
        direction = self.Config.Directions.SE 
    elseif lookRotation > 157.5 and lookRotation < 202.5 then
        direction = self.Config.Directions.S 
    elseif lookRotation > 202.5 and lookRotation < 247.5 then
        direction = self.Config.Directions.SW
    elseif lookRotation > 247.5 and lookRotation < 292.5 then
        direction = self.Config.Directions.W
    elseif lookRotation > 292.5 and lookRotation < 337.5 then
        direction = self.Config.Directions.NW 
    end

    self:SendMessage( player, self:Parse( self.Config.Messages.OnUse, { direction = direction } ) )
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
