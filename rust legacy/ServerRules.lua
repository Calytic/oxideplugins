PLUGIN.Title = "Server Rules"
PLUGIN.Description = "Send server rules to player when command is run"
PLUGIN.Version = V(1, 0, 0)
PLUGIN.Author = "mvrb"
PLUGIN.HasConfig = true

function PLUGIN:Init()
		command.AddChatCommand( "rule", self.Plugin, "cmdRules")
		command.AddChatCommand( "rules", self.Plugin, "cmdRules")
		self:LoadDefaultConfig()
end


function PLUGIN:LoadDefaultConfig()
    self.Config.Rules = self.Config.Rules or 
		{
			"Rule 1",
			"Rule 2",
			"Rule 3"
		}
    self:SaveConfig()
end

function PLUGIN:cmdRules(player, cmd)
		for i=1, #self.Config.Rules do		rust.SendChatMessage(player, self.Config.Rules[i])	end
end