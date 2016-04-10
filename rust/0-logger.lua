PLUGIN.Title = "Logger"
PLUGIN.Version = V(0, 2, 8)
PLUGIN.Description = "Logs all chat commands used by players with auth level to the server log and/or chat."
PLUGIN.Author = "Wulfspider"
PLUGIN.Url = "http://forum.rustoxide.com/plugins/670/"
PLUGIN.ResourceId = 670
PLUGIN.HasConfig = true

local debug = false

local compatible = true
function PLUGIN:Init()
    self:LoadDefaultConfig()
    if plugins.Exists("chathandler") then compatible = false end
end

function PLUGIN:OnRunCommand(arg)
    if not arg then return end
    if not arg.connection then return end
    if not arg.connection.player then return end
    if not arg.cmd then return end
    if not arg.cmd.namefull then return end
    local player = arg.connection.player
    local console = arg.cmd.namefull
    local chat = arg:GetString(0, "text")
    if self.Config.Settings.CommandLogging ~= "false" then
        if self:PermissionsCheck(player) then
            local excluded
            for key, value in pairs(self.Config.Settings.Exclusions) do if value == console then excluded = true; break end end
            if not excluded then
                print("[" .. self.Title .. "] " .. player.displayName .. " ran console command: " .. console)
                if self.Config.Settings.Broadcast ~= "false" then
                    rust.BroadcastChat(self.Config.Settings.ChatName, player.displayName .. " ran console command: " .. console)
                end
            elseif chat:sub(1, 1) == "/" then
                print("[" .. self.Title .. "] " .. player.displayName .. " ran chat command: " .. chat)
                if self.Config.Settings.Broadcast ~= "false" then
                    rust.BroadcastChat(self.Config.Settings.ChatName, player.displayName .. " ran chat command: " .. chat)
                end
            end
        end
    end
end

function PLUGIN:OnPlayerChat(arg)
    if not arg then return end
    if not arg.connection then return end
    if not arg.connection.player then return end
    local player = arg.connection.player
    local chat = arg:GetString(0, "text")
    if not chat or chat == "" or chat:sub(1, 1) == "/" then return end
    if self.Config.Settings.ChatLogging ~= "false" and compatible ~= "false" then
        print(player.displayName .. ": " .. chat)
    end
end

function PLUGIN:PermissionsCheck(player)
    local authLevel
    if player then authLevel = player.net.connection.authLevel else authLevel = 2 end
    local neededLevel = tonumber(self.Config.Settings.AuthLevel) or 2
    if debug then print(player.displayName .. " has auth level: " .. tostring(authLevel)) end
    if authLevel and authLevel >= neededLevel then return true else return false end
end

function PLUGIN:LoadDefaultConfig()
    self.Config.Settings = self.Config.Settings or {}
    self.Config.Settings.AuthLevel = tonumber(self.Config.Settings.AuthLevel) or 2
    self.Config.Settings.Broadcast = self.Config.Settings.Broadcast or "true"
    self.Config.Settings.ChatName = self.Config.Settings.ChatName or "LOGGER"
    self.Config.Settings.ChatLogging = self.Config.Settings.ChatLogging or "false"
    self.Config.Settings.CommandLogging = self.Config.Settings.CommandLogging or "true"
    self.Config.Settings.Exclusions = self.Config.Settings.Exclusions or {
        "chat.say", "craft.add", "craft.cancel", "global.kill", "global.respawn", "global.respawn_sleepingbag", "global.status", "global.wakeup", "inventory.endloot"
    }
    self:SaveConfig()
end
