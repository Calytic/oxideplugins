
-- --------------------------------------------------------------------------------------
-- m-RustBanSaver.lua
-- --------------------------------------------------------------------------------------
-- This plugin will save your bans to the bans.cfg server file.
-- --------------------------------------------------------------------------------------

PLUGIN.Title       = "Rust Ban Saver"
PLUGIN.Description = "Saves the bans of players banned with the command banid."
PLUGIN.Version     = V( 1, 0, 1 )
PLUGIN.HasConfig   = false
PLUGIN.Author      = "Mughisi"

-- --------------------------------------------------------------------------------------
-- PLUGIN:Init()
-- --------------------------------------------------------------------------------------
-- This will set the ingame chat command.
-- --------------------------------------------------------------------------------------
function PLUGIN:Init()
    command.AddChatCommand( "savebans", self.Object, "cmdSaveBans" )
end

-- --------------------------------------------------------------------------------------
-- PLUGIN:cmdSaveBans( player, cmd, args )
-- --------------------------------------------------------------------------------------
-- In-game '/savebans' command that allows an admin to save the executed bans.
-- --------------------------------------------------------------------------------------
function PLUGIN:cmdSaveBans( player, cmd, args )
    if not player:IsAdmin() then return end

    global.ServerUsers.Save()
end

function PLUGIN:OnRunCommand( arg )
    -- Check if the command is being send from an in-game player.
    if ( not arg ) then return end
    if ( not arg.cmd ) then return end
    if ( not arg.cmd.name ) then return end
    
    -- Check if the wakeup command was used.
    if arg.cmd.name == "banid" then
        if arg.connection then
            player = arg.connection.player
        end

        if player and not player:IsAdmin() then
            return true
        end

        timer.Once(1, function() global.ServerUsers.Save() end )

        arg:ReplyWith("Executed command banid!")
    end	
end