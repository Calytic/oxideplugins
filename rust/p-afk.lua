PLUGIN.Title        = "PaiN Afk"
PLUGIN.Description  = "Basic AFK System"
PLUGIN.Author       = "PaiN"
PLUGIN.Version      = V(3, 0, 0)
PLUGIN.HasConfig    = true
PLUGIN.ResourceId = 976

function PLUGIN:Init()
  command.AddChatCommand("afk", self.Plugin, "cmdAfk")
    afkData = datafile.GetDataTable("p-afklist")
    self:LoadDefaultConfig()
 end
 function PLUGIN:LoadDefaultConfig()
    self.Config.Settings = self.Config.Settings or {}
	self.Config.Settings.PluginPrefix = self.Config.Settings.PluginPrefix or "<color=cyan>**AFK**</color>"
    self.Config.Messages = self.Config.Messages or {}
    self.Config.Messages.IsAfk = self.Config.Messages.IsAfk or "{player} went AFK"
    self.Config.Messages.IsNotAfk = self.Config.Messages.IsNotAfk or "{player} is no longer AFK"
    self:SaveConfig()
end

 function PLUGIN:cmdAfk(player,  command, arg)
    local steamId = rust.UserIDFromPlayer(player)
	local prefixandname = self.Config.Settings.PluginPrefix.." "..player.displayName
    if afkData[steamId] then
        afkData[steamId] = nil
        local message = string.gsub(self.Config.Messages.IsNotAfk, "{player}", prefixandname)
        rust.BroadcastChat(message)
    else
	    afkData[steamId] = true
        local message = string.gsub(self.Config.Messages.IsAfk, "{player}", prefixandname)
        rust.BroadcastChat(message)
        player:StartSleeping()
    end
    datafile.SaveDataTable("p-afklist")

end
function PLUGIN:IsAFK(steamId)
    if not steamId then
        return false
    end

    if afkData[steamId] then

        return true
	end
	return false
end

function PLUGIN:SetAFK(steamId, afk)

    if not afk then
  
        afkData[steamId] = nil
   end
  
   if afk then

        afkData[steamId] = true
end
end
 function PLUGIN:OnPlayerSleepEnded(player)
    local userID = rust.UserIDFromPlayer(player)
	local message = string.gsub(self.Config.Messages.IsNotAfk, "{player}", self.Config.Settings.PluginPrefix.." "..player.displayName)
	if afkData[userID] then
	rust.BroadcastChat(message)
	afkData[userID] = nil
	end
	datafile.SaveDataTable("p-afklist")
end
