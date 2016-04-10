PLUGIN.Title = "Dice"
PLUGIN.Version = V(0, 2, 5)
PLUGIN.Description = "Feeling lucky? Roll one or multiple dice to get a random number."
PLUGIN.Author = "Wulfspider"
PLUGIN.Url = "http://forum.rustoxide.com/plugins/655/"
PLUGIN.ResourceId = 655
PLUGIN.HasConfig = true

function PLUGIN:Init()
    self:LoadDefaultConfig()
    command.AddChatCommand(self.Config.Settings.ChatCommand, self.Plugin, "cmdRollDice")
end

function PLUGIN:cmdRollDice(player, cmd, arg)
    local dice = tonumber(arg[0]) or 1
    local count, total = 0, 0
    if dice > 1000 then dice = 1 end
    while count < dice do
        local roll = math.random(6);
        total = total + roll; count = count + 1
    end
    local number = tostring(total)
    local message = self.Config.Messages.Rolled:gsub("{player}", player.displayName):gsub("{number}", number)
    rust.BroadcastChat(self.Config.Settings.ChatName, message)
end

function PLUGIN:SendHelpText(player)
    rust.SendChatMessage(player, self.Config.Settings.ChatNameHelp, self.Config.Messages.ChatHelp)
end

function PLUGIN:LoadDefaultConfig()
    self.Config.Settings = self.Config.Settings or {}
    self.Config.Settings.ChatCommand = self.Config.Settings.ChatCommand or "dice"
    self.Config.Settings.ChatName = self.Config.Settings.ChatName or "DICE"
    self.Config.Settings.ChatNameHelp = self.Config.Settings.ChatNameHelp or "HELP"
    self.Config.Messages = self.Config.Messages or {}
    self.Config.Messages.ChatHelp = self.Config.Messages.ChatHelp or "Use /dice # to roll dice (# being optional number of dice to roll)"
    self.Config.Messages.Rolled = self.Config.Messages.Rolled or "{player} rolled {number}"
    self:SaveConfig()
end
