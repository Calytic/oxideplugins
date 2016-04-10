PLUGIN.Title = "Auto Broadcast"
PLUGIN.Version = V(0, 2, 0)
PLUGIN.Description = "Sends global broadcast messages over a timed interval"
PLUGIN.Author = "Taffy"
PLUGIN.ResourceId = 684

--[[ Do NOT edit the config here, instead edit AutoBroadcast.json in oxide/config ! ]]

function PLUGIN:LoadDefaultConfig()
    self.Config.Interval = tonumber(self.Config.Interval) or 600
    self.Config.Messages = self.Config.Messages or {
        "Please do not grief other players",
        "This is an example global broadcast",
        "Add a new line to add another message!"
    }

    self.Config.BroadCastInterval = nil -- Removed in 0.2.0
    self.Config.ChatName = nil -- Removed in 0.2.0

    self:SaveConfig()
end

function PLUGIN:Init() self:LoadDefaultConfig() end
function PLUGIN:OnServerInitialized() self:BroadcastMessages() end

function PLUGIN:BroadcastMessages()
    local count, message = 0, 1
    for _ in pairs(self.Config.Messages) do count = count + 1 end

    timer.Repeat(self.Config.Interval, 0, function()
        if message > count then message = 1 end

        print("[" .. self.Title .. "] " .. self.Config.Messages[message])
        rust.BroadcastChat(self.Config.Messages[message])

        message = message + 1
    end, self.Plugin)
end
