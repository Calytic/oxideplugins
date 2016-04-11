PLUGIN.Title = "AntiSuicide"
PLUGIN.Description = "Block the suicide command"
PLUGIN.Version = V(1, 0, 0)
PLUGIN.Author = "copper"



function PLUGIN:Init()
    self.Data = datafile.GetDataTable( "AntiSuicide(pl)" ) or {}
	command.AddChatCommand( "sactive", self.Plugin, "cmdactive")
	command.AddChatCommand( "sactivepl", self.Plugin, "cmdblocks")
	command.AddChatCommand( "sdeactivepl", self.Plugin, "cmdunblocks")
	command.AddChatCommand( "sdeactive", self.Plugin, "cmddeactive")
	permission.RegisterPermission("canantisuicide", self.Plugin);
	self.isActive = 0
end

function PLUGIN:flagged(netuser)
if(netuser:CanAdmin()) then return true end
	return permission.UserHasPermission(rust.UserIDFromPlayer(netuser), "canantisuicide")
end

function PLUGIN:cmdblocks( netuser, cmd, args )
    if (args.Length == 0) then
        return
    end
    if (not(self:flagged(netuser))) then
        rust.Notice( netuser, "You're not admin!" )
        return
    end
    
    local targetuser = rust.FindPlayer( args[0] )
    if (targetuser == nil) then
		rust.Notice( netuser, "No players found with that name!" )
		return
    end
    local userID = rust.UserIDFromPlayer( targetuser )
	local pp = self.Data[ rust.UserIDFromPlayer( targetuser ) ]
	if(pp) then
	rust.Notice( netuser, "suicide block is already activated on that current player: "..targetuser.displayName)
	return
	end
	data = {}
	data.ID = targetuser.networkPlayer.externalIP
	data.Name = targetuser.displayName
	data.isblocked = true
	self.Data[rust.UserIDFromPlayer( targetuser )] = data
	self:Save()
    rust.SendChatMessage( netuser,"Antisuicide", "You have blocked suicide for " .. rust.QuoteSafe(targetuser.displayName) )
end

function PLUGIN:Save()
	datafile.SaveDataTable("AntiSuicide(pl)")
end

function PLUGIN:cmdunblocks( netuser, cmd, args )
    if (args.Length == 0) then
        return
    end
    if (not(self:flagged(netuser))) then
        rust.Notice( netuser, "You're not admin!" )
        return
    end
    local targetuser = rust.FindPlayer( args[0] )
    if (targetuser == nil) then
		rust.Notice( netuser, "No players found with that name!" )
		return
    end
    local userID = rust.UserIDFromPlayer( targetuser )
    self.Data[userID] = null
	self:Save()
    rust.Notice( targetuser, "You have been set free!" )
    rust.SendChatMessage( netuser,"Antisuicide", "you have been given freedom of action: " .. rust.QuoteSafe(targetuser.displayName) )
end

function PLUGIN:cmddeactive( netuser, cmd, args )
    if (args.Length == 1) then rust.Notice(netuser, "wrong syntax /sactive")
        return
    end
	if (not(self:flagged(netuser))) then  rust.Notice( netuser, "you are not alowed to use this command" )
	   return
	end
	self.isActive = 0
	rust.SendChatMessage( netuser,"Antisuicide", "antisuicide is now at slumber ")
	end

function PLUGIN:cmdactive( netuser, cmd, args )
    if (args.Length == 1) then rust.Notice(netuser, "wrong syntax /sactive")
        return
    end
	if (not(self:flagged(netuser))) then  rust.Notice( netuser, "you are not alowed to use this command" )
	   return
	end
	self.isActive = 1
	rust.SendChatMessage( netuser,"Antisuicide", "antisuicide is now active ")
	end

function PLUGIN:OnRunCommand(arg, wantreply)
	local cmd = arg.Function
	if (cmd == "suicide") then
	    local netuser = arg.argUser
		local data = self.Data[ rust.UserIDFromPlayer( netuser ) ]
	    if (data) then
		rust.Notice(netuser, "you are not allowed to suicide!")
		return false
		end
		if (self.isActive == 1) then
	    rust.Notice(netuser, "Suicide is blocked!")
	    return false
		end
	end
end