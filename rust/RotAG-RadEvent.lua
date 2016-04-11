PLUGIN.Name = "RotAG-RadEvent"
PLUGIN.Title = "RotAG-RadEvent"
PLUGIN.Version = V(1, 0, 0)
PLUGIN.Description = "RadTowns Event plugin for Oxide 2.0"
PLUGIN.Author = "TheRotAG"
PLUGIN.HasConfig = true

local run = {}

function PLUGIN:Init()
	self.Config.NoRadTime = self.Config.NoRadTime or 1800
	self.Config.RadTime = self.Config.RadTime or 7200
	self.Config.ChatName = self.Config.ChatName or "[RadBroadcast]"
	self.Config.EventStart = self.Config.EventStart or "The radiations seems to be down! Now its a good time to loot everything we can!"
	self.Config.EventStop = self.Config.EventStop or "GET OUT OF THE RAD LOCATIONS NOW! THE RADIATION IS REACHING ITS PEAK AGAIN!!!"
	self:SaveConfig()
	self:RunEvent()
end

function PLUGIN:RunEvent()
	timer.Once(self.Config.RadTime, function() self:TurnRad() return end )
end

function PLUGIN:TurnRad()
	if global.server.radiation == true then
		global.server.radiation = false
		print("Rad is now disabled - RadEvent started!")
		global.ConsoleSystem.Broadcast("chat.add \"" .. self.Config.ChatName .. "\" \"" .. self.Config.EventStart .. "\"")
		timer.Once(self.Config.NoRadTime, function()
			global.server.radiation = true
			global.ConsoleSystem.Broadcast("chat.add \"" .. self.Config.ChatName .. "\" \"" .. self.Config.EventStop .. "\"")
			self:RunEvent()
			return
		end)
	else
		global.server.radiation = true
		self:RunEvent()
		return
	end
end