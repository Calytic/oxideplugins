PLUGIN.Title        = "Players list"
PLUGIN.Description  = "Allows users to see who or how many people are online"
PLUGIN.Author       = "#Domestos"
PLUGIN.Version      = V(1, 4, 0)
PLUGIN.HasConfig    = true
PLUGIN.ResourceID   = 661


function PLUGIN:Init()
    --command.AddChatCommand("who", self.Object, "cmdWho")
    self:LoadDefaultConfig()
    for _, cmd in pairs(self.Config.Settings.ChatCommands) do
        command.AddChatCommand(cmd, self.Object, "cmdWho")
    end
end

function PLUGIN:LoadDefaultConfig()
    -- Settings
    self.Config.Settings = self.Config.Settings or {}
    self.Config.Settings.OnlyShowPlayerCount = self.Config.Settings.OnlyShowPlayerCount or "true"
    self.Config.Settings.MaxPlayersPerLine = self.Config.Settings.MaxPlayersPerLine or 8
    self.Config.Settings.SeparateAdmins = self.Config.Settings.SeparateAdmins or "false"
    self.Config.Settings.OnlyShowAdminCount = self.Config.Settings.OnlyShowAdminCount or "false"
    self.Config.Settings.ChatCommands = self.Config.Settings.ChatCommands or {"who", "online"}
    -- Messages
    self.Config.Messages = self.Config.Messages or {}
    self.Config.Messages.OnePlayerMessage = self.Config.Messages.OnePlayerMessage or "You're the only one online"
    self.Config.Messages.PlayerCountMessage = self.Config.Messages.PlayerCountMessage or "{count} players online"
    self.Config.Messages.PlayerNameMessage = self.Config.Messages.PlayerNameMessage or "{count} players online: "
    self.Config.Messages.NoAdminMessage = self.Config.Messages.NoAdminMessage or "No admin online"
    self.Config.Messages.AdminCountMessage = self.Config.Messages.AdminCountMessage or "{count} admins online"
    self.Config.Messages.AdminNameMessage = self.Config.Messages.AdminNameMessage or "{count} admins online: "
    self.Config.Messages.HelpText = self.Config.Messages.HelpText or "use /who or /online to show who's online"

    self:SaveConfig()
end

-- --------------------------------
-- admin permission check
-- --------------------------------
local function IsAdmin(player)
    return player:GetComponent("BaseNetworkable").net.connection.authLevel > 0
end

function PLUGIN:cmdWho(player)
    if not player then return end
    local playerList = player.activePlayerList
    local enum = playerList:GetEnumerator()
    local playerCount, adminCount, allCount = 0, 0, 0
    local playerString, adminString = "", ""
    local playerStringTbl, adminStringTbl = {}, {}
    local i = 0
    local maxPlayersPerLine = self.Config.Settings.MaxPlayersPerLine
    while enum:MoveNext() do
        if self.Config.Settings.SeparateAdmins == "true" and IsAdmin(enum.Current) then
            adminCount = adminCount + 1
            allCount = allCount + 1
            adminString = adminString..enum.Current.displayName..", "
            if adminCount == maxPlayersPerLine then
                adminStringTbl[i + 1] = adminString
                adminString = ""
                i = i + 1
            end
        else
            playerCount = playerCount + 1
            allCount = allCount + 1
            playerString = playerString..enum.Current.displayName..", "
            if playerCount == maxPlayersPerLine then
                playerStringTbl[i + 1] = playerString
                playerString = ""
                i = i + 1
            end
        end
    end
    if allCount == 1 then
        rust.SendChatMessage(player, self.Config.Messages.OnePlayerMessage)
        return
    end
    -- remove comma at the end
    if string.sub(playerString, -2, -2) == "," then
        playerString = string.sub(playerString, 1, -3)
    end
    if string.sub(adminString, -2, -2) == "," then
        adminString = string.sub(adminString, 1, -3)
    end
    -- Build admin message
    if self.Config.Settings.SeparateAdmins == "true" then
        if adminCount == 0 then
            rust.SendChatMessage(player, self.Config.Messages.NoAdminMessage)
        else
            if self.Config.Settings.OnlyShowAdminCount == "true" then
                local msg = string.gsub(self.Config.Messages.AdminCountMessage, "{count}", tostring(adminCount))
                rust.SendChatMessage(player, msg)
            else
                if #adminStringTbl >= 1 then
                    local msg = string.gsub(self.Config.Messages.AdminNameMessage, "{count}", tostring(adminCount))
                    rust.SendChatMessage(player, msg)
                    for i = 1, #adminStringTbl, 1 do
                        rust.SendChatMessage(player, adminStringTbl[i])
                    end
                    rust.SendChatMessage(player, adminString)
                else
                    local msg = string.gsub(self.Config.Messages.AdminNameMessage, "{count}", tostring(adminCount))
                    rust.SendChatMessage(player, msg..adminString)
                end
            end
        end
    end
    -- Build player message
    if self.Config.Settings.OnlyShowPlayerCount == "true" then
        local msg = string.gsub(self.Config.Messages.PlayerCountMessage, "{count}", tostring(playerCount))
        rust.SendChatMessage(player, msg)
    else
        if #playerStringTbl >= 1 then
            local msg = string.gsub(self.Config.Messages.PlayerNameMessage, "{count}", tostring(playerCount))
            rust.SendChatMessage(player, msg)
            for i = 1, #playerStringTbl, 1 do
                rust.SendChatMessage(player, playerStringTbl[i])
            end
            rust.SendChatMessage(player, playerString)
        else
            local msg = string.gsub(self.Config.Messages.PlayerNameMessage, "{count}", tostring(playerCount))
            rust.SendChatMessage(player, msg..playerString)
        end
    end
end

function PLUGIN:SendHelpText(player)
    rust.SendChatMessage(player, self.Config.Messages.HelpText)
end