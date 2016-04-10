PLUGIN.Title = "Auto Admin"
PLUGIN.Description = "Auto Login for Admins"
PLUGIN.Version = V(1, 0, 0)
PLUGIN.Author = "greyhawk, Ported by Reneb to Oxide 2.0"

local godmode = false
local permaDay = false
 
function PLUGIN:Init()
	self.Data = datafile.GetDataTable( "autoadmin" ) or {}
	command.AddChatCommand( "promote", self.Plugin, "cmdPromote")
	command.AddChatCommand( "demote", self.Plugin, "cmdDemote")
end

function PLUGIN:flagged(netuser)
    return netuser:CanAdmin()
end

function PLUGIN:cmdDemote( netuser, cmd, args )
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
    targetuser:SetAdmin(false)
    self:Save()
    rust.Notice( targetuser, "You have been demoted!" )
    rust.SendChatMessage( netuser, "You have demoted " .. rust.QuoteSafe(targetuser.displayName) )
end

function PLUGIN:cmdPromote( netuser, cmd, args )
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
	data = {}
	data.ID = userID
	data.Name = name
	data.isAdmin = true
	targetuser:SetAdmin(true)
	self.Data[ userID ] = data
	self:Save()
    rust.Notice( targetuser, "You have been promoted!" )
    rust.SendChatMessage( netuser, "You have promoted " .. rust.QuoteSafe(targetuser.displayName) )
end

function PLUGIN:OnPlayerConnected( netuser )
    local data = self:GetUserData( netuser )
	if(not data) then return end
    if (data.isAdmin) then
        netuser:SetAdmin(true)
        rust.Notice( netuser, "You are admin!" )
    end
end

function PLUGIN:SendHelpText( netuser )
    if (self:flagged(netuser)) then
        rust.SendChatMessage( netuser, "Use /promote name to add someone to the auto-admin list" )
        rust.SendChatMessage( netuser, "Use /demote name to remove someone from the auto-admin list" )
    end
end

function PLUGIN:Save()
	datafile.SaveDataTable("autoadmin")
end

function PLUGIN:GetUserData( netuser )
	local userID = rust.UserIDFromPlayer( netuser )
	return self:GetUserDataFromID( userID, netuser.displayName )
end

function PLUGIN:GetUserDataFromID( userID, name )
	local userentry = self.Data[ userID ]
	if (not userentry) then
		return false
	end
	return userentry
end