PLUGIN.Name = "spectateMgr"
PLUGIN.Title = "Spectate Manager"
PLUGIN.Description = "Manage and log use of spectate on server"
PLUGIN.Author = "OHG"
PLUGIN.Version = V(0, 0, 1)
PLUGIN.HasConfig = false
PLUGIN.Debug = false

function PLUGIN:OnRunCommand( arg, player, cmd )
    if (not arg) then
		return
	end

	if (not arg.connection) then
		return
	end

	if (not arg.connection.player) then
		return
	end

	local lPlayer = arg.connection.player

	if ( arg.cmd ) then
		if ( arg.cmd.name == "spectate" ) then
			print ( ".::(TRACE - OnRunCommand) spectate: " .. tostring ( lPlayer.displayName ) )
			return true
		end
	end
end
