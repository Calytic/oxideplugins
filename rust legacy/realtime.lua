PLUGIN.Title = "RealTime"
PLUGIN.Version = V(0, 2, 0)
PLUGIN.Description = "RealTime extends the Rust day out to a full 24 hours, 12 hours of daylight and 12 hours of night of the real time."
PLUGIN.Author = "PreFiX"
PLUGIN.Url = ""

local enabled = true
local timersVariable = {}

function PLUGIN:Init()
	self:LoadDefaultConfig()
	command.AddChatCommand("realtime", self.Plugin, "cmdRealTime")
	self:StartSync()
end

function PLUGIN:LoadDefaultConfig()
    self.Config.Sync = self.Config.Sync or {}
	self.Config.Sync.SyncEveryXSec = self.Config.Sync.SyncEveryXSec or 20
    self.Config.Messages = self.Config.Messages or {}
    self.Config.Messages.ChatTag = self.Config.Messages.ChatTag  or "RealTime"
	self.Config.Messages.NoPermission = self.Config.Messages.NoPermission  or "[color red]You don't have permission for this command."
	self.Config.Messages.StartedToSync = self.Config.Messages.StartedToSync  or "[color red]RealTime started to sync with server."
	self.Config.Messages.StopToSync = self.Config.Messages.StopToSync  or "[color red]RealTime has been disabled"
	self.Config.Messages.Help = {
		[1]={name="[color red]RealTime commands"},
		[2]={name="----"},
		[3]={name="[color cyan]/realtime enable [color clear]- Start time sync if not started yet."},
		[4]={name="[color cyan]/realtime disable [color clear]- Disable time sync."}
	}
	self.Config.Version = "0.2.0"
    self:SaveConfig()
end

function PLUGIN:cmdRealTime(player, cmd, args)

	local playerID = rust.UserIDFromPlayer(player)
	
	if (not permission.UserHasPermission(playerID, "canrealtime")) then 
		rust.SendChatMessage(player, self.Config.Messages.ChatTag, self.Config.Messages.NoPermission)
		return
	end
	
	if (args[0] == "enable") then
		enabled = true
		self:StartSync()
		rust.SendChatMessage(player, self.Config.Messages.ChatTag, self.Config.Messages.StartedToSync)
	elseif (args[0] == "disable") then
		enabled = false
		self:DestroyTimer()
		rust.SendChatMessage(player, self.Config.Messages.ChatTag,  self.Config.Messages.StopToSync)
	else
	
	for i=1, #self.Config.Messages.Help do
		if(i > 5) then
			error("We have only 4 lines here yet!")
			break
		end
		rust.SendChatMessage(player, self.Config.Messages.ChatTag, self.Config.FastBar[i].name )
	end
	
		rust.SendChatMessage(player, self.Config.Messages.ChatTag, "[color red]RealTime commands")
		rust.SendChatMessage(player, self.Config.Messages.ChatTag, "----")
		rust.SendChatMessage(player, self.Config.Messages.ChatTag, "[color cyan]/realtime enable [color clear]- Start time sync if not started yet.")
		rust.SendChatMessage(player, self.Config.Messages.ChatTag, "[color cyan]/realtime disable [color clear]- Disable time sync.")
	end
end

function PLUGIN:StartSync()
	if (enabled == true) then
		self:DestroyTimer()
		
		local newvar = tostring("timer-"..time.GetUnixTimestamp().."")
			
		timersVariable[newvar] = timer.Repeat(self.Config.Sync.SyncEveryXSec, 0, function()

			local hours = time.GetCurrentTime():ToLocalTime():ToString("HH")
			local mins = tonumber(time.GetCurrentTime():ToLocalTime():ToString("mm"))
			mins = tostring((100000 / 60) * mins)
			
			local str, repl = mins:gsub("%.", "")
			local str2, repl2 = str:gsub("%,", "")
			
			local envtime = hours .. "." .. str2
			--print(chatttag .. " debug envtime  ".. envtime)
			rust.RunServerCommand("env.time " .. "\"" ..envtime .. "\"")
				
		end, self.Plugin)		
	end 
end

function PLUGIN:DestroyTimer()
	for key, value in pairs(timersVariable) do
		timersVariable[key]:Destroy()
	end
end