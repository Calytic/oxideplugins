PLUGIN.Title = "Natives"
PLUGIN.Version = V(0, 1, 8)
PLUGIN.Description = "Allows only players from the server's country to join."
PLUGIN.Author = "Wulfspider"
PLUGIN.Url = "http://forum.rustoxide.com/plugins/678/"
PLUGIN.ResourceId = 678
PLUGIN.HasConfig = true

local debug = false

function PLUGIN:Init()
    self:LoadDefaultConfig()
end

local function GetIp(ip)
    return ("%s.%s.%s.%s"):format(bit32.rshift(ip, 24), bit32.band(bit32.rshift(ip, 16), 0xff), bit32.band(bit32.rshift(ip, 8), 0xff), bit32.band(ip, 0xff))
end

local homeland = "undefined"
function PLUGIN:OnServerInitialized()
    self.ipTimer = timer.Once(5, function()
        local serverIp
        serverIp = GetIp(Steamworks.SteamGameServer.GetPublicIP())
        if debug then print("[" .. self.Title .. "] Server's IP: " .. serverIp) end
        webrequests.EnqueueGet("http://ipinfo.io/" .. serverIp .. "/country", function(code, response)
            homeland = response:gsub("\n", "")
            if homeland == "undefined" and code ~= "200" then print("[" .. self.Title .. "] Getting country for server failed!"); return end
            print("[" .. self.Title .. "] Server's country: " .. homeland)
        end, self.Plugin)
        if serverIp == "" or serverIp == "0.0.0.0" then print("[" .. self.Title .. "] Getting IP for server failed!") end
    end)
end

function PLUGIN:CanClientLogin(connection)
    local country = "undefined"
    local playerIp = connection.ipaddress:match("([^:]*):")
    if debug then playerIp = "8.8.8.8"; print("[" .. self.Title .. "] Player's IP: " .. playerIp) end
    if playerIp ~= "127.0.0.1" then
        local steamId = rust.UserIDFromConnection(connection)
        local url = "http://ipinfo.io/" .. playerIp .. "/country"
        webrequests.EnqueueGet(url, function(code, response)
            country = response:gsub("\n", "")
            print("[" .. self.Title .. "] " .. connection.username .. " connected from " .. country)
            if country == "undefined" or code ~= 200 then
                print("[" .. self.Title .. "] Checking country for " .. connection.username .. " failed!")
                self:Deport(connection, country)
                return
            end
            if country ~= homeland then self:Deport(connection, country) end
        end, self.Plugin)
    end
end

function PLUGIN:Deport(connection, country)
    Network.Net.sv:Kick(connection, self.Config.Messages.Rejected)
    local kicked = self.Config.Messages.Kicked:gsub("{player}", connection.username); local kicked = kicked:gsub("{country}", homeland)
    print("[" .. self.Title .. "] " .. kicked)
    if self.Config.Settings.Broadcast ~= "false" then rust.BroadcastChat(self.Config.Settings.ChatName, kicked) end
end

function PLUGIN:Unload()
    if self.ipTimer then self.ipTimer:Destroy(); self.ipTimer = nil end
end

function PLUGIN:LoadDefaultConfig()
    self.Config.Settings = self.Config.Settings or {}
    self.Config.Settings.Broadcast = self.Config.Settings.Broadcast or "true"
    self.Config.Settings.ChatName = self.Config.Settings.ChatName or "SERVER"
    self.Config.Messages = self.Config.Messages or {}
    self.Config.Messages.Kicked = self.Config.Messages.Kicked or "{player} kicked for not being from the homeland, {country}!"
    self.Config.Messages.Rejected = self.Config.Messages.Rejected or "Sorry, this server allows only players from its native country!"
    self:SaveConfig()
end
