
-- -----------------------------------------------------------------------------------
-- Anti-Airdrop                                                          Version 1.0.0
-- -----------------------------------------------------------------------------------
-- Filename:          m-AntiParachutes.lua
-- Last Modification: 01-22-2015
-- -----------------------------------------------------------------------------------
-- Description:
--
-- This plugin is developed for Rust servers with the Oxide Server Mod and will
-- allow a server admin to remove active parachutes from the world.
--
-- This is just a simple snippet and not an actual plugin.
-- -----------------------------------------------------------------------------------
 
PLUGIN.Title       = "Remove Parachutes"
PLUGIN.Description = "Removes parachutes that are stuck in place."
PLUGIN.Version     = V( 1, 0, 0 )
PLUGIN.HasConfig   = false
PLUGIN.Author      = "Mughisi"
 
-- -----------------------------------------------------------------------------------
-- PLUGIN:Init()
-- -----------------------------------------------------------------------------------
-- On plugin initialisation the required in-game chat command is registered.
-- -----------------------------------------------------------------------------------
function PLUGIN:Init()
    -- Add the chat command:
    command.AddChatCommand( "remparachutes", self.Object, "cmdRemoveParachutes" )
end
 
-- -----------------------------------------------------------------------------------
-- PLUGIN:cmdRemoveParachutes( player, cmd, args )                       Admin Command
-- -----------------------------------------------------------------------------------
-- In-game '/remparachutes' command that allows the server admins to remove all 
-- parachutes from the world.
-- -----------------------------------------------------------------------------------
function PLUGIN:cmdRemoveParachutes( player, cmd, args )
    -- Check if the player is an admin, if this is not the case exit the function.
    if player.net.connection.authLevel == 0 then
        return
    end
 
    parachutes = UnityEngine.Object.FindObjectsOfTypeAll( global.BaseEntity._type )
    for i = 0, parachutes.Length - 1 do
        if parachutes[i].model then
            if tostring( parachutes[i] ):find("parachute") then
                parachutes[i]:KillMessage()
            end
        end
    end
end