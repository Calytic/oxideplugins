PLUGIN.Title = "Analytics"
PLUGIN.Version = V(0, 2, 2)
PLUGIN.Description = "Real-time collection and reporting of player locations to Google Analytics."
PLUGIN.Author = "Wulfspider"
PLUGIN.Url = "http://oxidemod.org/plugins/679/"
PLUGIN.ResourceId = 679

local debug = false

function PLUGIN:LoadDefaultConfig()
    self.Config.TrackingId = self.Config.TrackingId or self.Config.TrackingID or "UA-XXXXXXXX-Y"

    self.Config.TrackingID = nil -- Removed in 0.2.2

    self:SaveConfig()
end

function PLUGIN:Init()
    self:LoadDefaultConfig()
end

local function Print(self, message) print("[" .. self.Title .. "] " .. message) end

function PLUGIN:OnPlayerConnected(packet)
    if not packet then return end
    if not packet.connection then return end

    self:CollectAnalytics(packet.connection, "start")
end

function PLUGIN:OnPlayerDisconnected(player)
    if not player then return end
    if not player.net then return end
    if not player.net.connection then return end

    self:CollectAnalytics(player.net.connection, "end")
end

function PLUGIN:CollectAnalytics(connection, session)
    local url = "https://ssl.google-analytics.com/collect?v=1"
    local data = "&tid=" .. self.Config.TrackingId
    .. "&sc=" .. session
    .. "&cid=" .. rust.UserIDFromConnection(connection)
    .. "&uip=" .. connection.ipaddress:match("([^:]*):")
    .. "&ua=" .. Oxide.Core.OxideMod.Version:ToString()
    .. "&dp=" .. ConVar.Server.hostname
    .. "&t=pageview"

    webrequests.EnqueuePost(url, data, function(code, response)
        if debug then
            Print(self, "Request URL: " .. url)
            Print(self, "Request data: " .. data)
            Print(self, "Response: " .. response)
            Print(self, "HTTP code: " .. code)
        end
    end, self.Plugin)
end
