PLUGIN.Title = "PaiN TimedExecute"
PLUGIN.Description = "Execute commands every (x) seconds."
PLUGIN.Author = "PaiN"
PLUGIN.Version = V(2, 0, 0)
PLUGIN.ResourceId = 1100

function PLUGIN:LoadDefaultConfig()
    self.Config.EnableTimerOnceCommands = self.Config.EnableTimerOnceCommands or "true"
	self.Config.EnableTimerRepeatCommands = self.Config.EnableTimerRepeatCommands or "true"
    self.Config.RepeaterCommands = self.Config.RepeaterCommands or { 
        { command = "server.save", seconds = 300 },
        { command = "say 'hello world'", seconds = 60 }
    }
	self.Config.OnceCommands = self.Config.OnceCommands or {
        { command = "say 'Restart in 1 minute'", seconds = 60 },
        { command = "say 'Restart in 30 seconds'", seconds = 90 },
		{ command = "say 'Restart NOW'", seconds = 120 },
		{ command = "restart", seconds = 120 },
		{ command = "reset.oncetimer", seconds = 121 }
    }
self:SaveConfig()
end

function PLUGIN:Init()
if self.Config.EnableTimerOnceCommands == "true" then
print("[" .. self.Title .. "] Timer-Once is ON ")
else
print("[" .. self.Title .. "] Timer-Once is OFF")
end
if self.Config.EnableTimerRepeatCommands == "true" then
print("[" .. self.Title .. "] Timer-Repeat is ON ")
else
print("[" .. self.Title .. "] Timer-Repeat is OFF")
end
command.AddConsoleCommand("reset.oncetimer", self.Plugin, "cmdResetOnceTimer")
self:LoadDefaultConfig()
	 self.repeattimer = {}
	 self.oncetimer = {}
	self:RepeaterTimedCommands()
	self:OnceTimedCommands()
end

function PLUGIN:Unload()
    self:ResetRepeatTimer()
	self:ResetOnceTimer()
end

function PLUGIN:ResetRepeatTimer()
for i, item in ipairs(self.repeattimer) do
self.repeattimer[i]:Destroy()
	end
end
function PLUGIN:ResetOnceTimer()
for i, item in ipairs(self.oncetimer) do
self.oncetimer[i]:Destroy()
	end
end
function PLUGIN:cmdResetOnceTimer()
self:OnceTimedCommands()
end

function PLUGIN:RepeaterTimedCommands()
if self.Config.EnableTimerRepeatCommands == "true" then
self:ResetRepeatTimer()
for i, item in ipairs(self.Config.RepeaterCommands) do
self.repeattimer[i] = timer.Repeat(item.seconds, 0, function()
            print("[" .. self.Title .. "] Ran command: " .. item.command)
            rust.RunServerCommand(item.command)
        end, self.Plugin)
    end
end
end

function PLUGIN:OnceTimedCommands()
if self.Config.EnableTimerOnceCommands == "true" then
	self:ResetOnceTimer()
for i, item in ipairs(self.Config.OnceCommands) do
self.oncetimer[i] = timer.Once(item.seconds, function ()
            print("[" .. self.Title .. "] Ran command: " .. item.command)
            rust.RunServerCommand(item.command)
        end, self.Plugin)
    end
end
end
