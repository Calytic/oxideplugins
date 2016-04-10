PLUGIN.Title        = "Chat Handler"
PLUGIN.Description  = "Chat modification and moderation suite"
PLUGIN.Author       = "#Domestos"
PLUGIN.Version      = V(3, 1, 4)
PLUGIN.ResourceId   = 707

local debugMode = false

-- --------------------------------
-- declare some plugin wide vars
-- --------------------------------
local spamData = {}
local SpamList = "chathandler-spamlist"
local LogFile = "Log.ChatHandler.txt"
local AntiSpam, ChatHistory, AdminMode = {}, {}, {}
-- external plugin references
local eRanksAndTitles, eIgnoreAPI, eChatMute
-- --------------------------------
-- initialise all settings and data
-- --------------------------------
function PLUGIN:Init()
    self:LoadDefaultConfig()
    self:LoadCommands()
    self:LoadDataFiles()
    self:RegisterPermissions()
    if debugMode then print("ChatHandler is in debug mode") end
end
-- --------------------------------
-- Debug reporting
-- --------------------------------
local function Debug(msg)
    if not debugMode then return end
    global.ServerConsole.PrintColoured(System.ConsoleColor.Yellow, msg)
    ConVar.Server.Log("Debug.ChatHandler.txt", msg.."\n")
end
-- --------------------------------
-- permission check
-- --------------------------------
local function HasPermission(player, perm)
    local steamID = rust.UserIDFromPlayer(player)
    if permission.UserHasPermission(steamID, "admin") then
        return true
    end
    if permission.UserHasPermission(steamID, perm) then
        return true
    end
    return false
end
-- --------------------------------
-- builds output messages by replacing wildcards
-- --------------------------------
local function BuildOutput(str, tags, replacements)
    for i = 1, #tags do
        str = str:gsub(tags[i], replacements[i])
    end
    return str
end
-- --------------------------------
-- print functions
-- --------------------------------
local function PrintToConsole(msg)
    global.ServerConsole.PrintColoured(System.ConsoleColor.Cyan, msg)
end
local function PrintToFile(msg)
    ConVar.Server.Log(LogFile, msg.."\n")
end
-- --------------------------------
-- splits chat messages longer than charlimit characters into multilines
-- --------------------------------
local function SplitLongMessages(msg, charlimit)
    local length = msg:len()
    local msgTbl = {}
    if length > 128 then
        msg = msg:sub(1, 128)
    end
    if length > charlimit then
        while length > charlimit do
            local subStr = msg:sub(1, charlimit)
            local first, last = subStr:reverse():find(" ")
            if first then
                subStr = subStr:sub(1, -first)
            end
            table.insert(msgTbl, subStr)
            msg = msg:sub(subStr:len() + 1)
            length = msg:len()
        end
        table.insert(msgTbl, msg)
    else
        table.insert(msgTbl, msg)
    end
    return msgTbl
end
-- --------------------------------
-- generates default config
-- --------------------------------
local settings, wordfilter, chatgroups, messages
function PLUGIN:LoadDefaultConfig() 
    self.Config.Settings                    = self.Config.Settings or {}
    settings = self.Config.Settings
    -- General Settings
    settings.General                        = settings.General or {}
    settings.General.MaxCharsPerLine        = settings.General.MaxCharsPerLine or 80
    settings.General.BlockServerAds         = settings.General.BlockServerAds or "true"
    settings.General.AllowedIPsToPost       = settings.General.AllowedIPsToPost or {}
    settings.General.EnableChatHistory      = settings.General.EnableChatHistory or "true"
    settings.General.ChatHistoryMaxLines    = settings.General.ChatHistoryMaxLines or 10
    settings.General.EnableChatGroups       = settings.General.EnableChatGroups or "true"
    -- Wordfilter settings
    settings.Wordfilter                     = settings.Wordfilter or {}
    settings.Wordfilter.EnableWordfilter    = settings.Wordfilter.EnableWordfilter or "false"
    settings.Wordfilter.ReplaceFullWord     = settings.Wordfilter.ReplaceFullWord or "true"
    settings.Wordfilter.AllowPunish         = settings.Wordfilter.AllowPunish or "false"
    -- Chat commands
    settings.ChatCommands                   = settings.ChatCommands or {}
    settings.ChatCommands.AdminMode         = settings.ChatCommands.AdminMode or {"admin"}
    settings.ChatCommands.ChatHistory       = settings.ChatCommands.ChatHistory or {"history", "h"}
    settings.ChatCommands.Wordfilter        = settings.ChatCommands.Wordfilter or {"wordfilter"}
    -- command permissions
    settings.Permissions                    = settings.Permissions or {}
    settings.Permissions.AdminMode          = settings.Permissions.AdminMode or "chathandler.adminmode"
    settings.Permissions.EditWordFilter     = settings.Permissions.EditWordFilter or "chathandler.wordfilter"
    -- Logging settings
    settings.Logging                        = settings.Logging or {}
    settings.Logging.LogToConsole           = settings.Logging.LogToConsole or "true"
    settings.Logging.LogBlockedMessages     = settings.Logging.LogBlockedMessages or "true"
    settings.Logging.LogToFile              = settings.Logging.LogToFile or "false"
    -- Admin mode settings
    settings.AdminMode                      = settings.AdminMode or {}
    settings.AdminMode.ChatName             = settings.AdminMode.ChatName or "[Server Admin]"
    settings.AdminMode.NameColor            = settings.AdminMode.NameColor or "#ff8000"
    settings.AdminMode.TextColor            = settings.AdminMode.TextColor or "#ff8000"
    -- Antispam settings
    settings.AntiSpam                       = settings.AntiSpam or {}
    settings.AntiSpam.EnableAntiSpam        = settings.AntiSpam.EnableAntiSpam or "true"
    settings.AntiSpam.MaxLines              = settings.AntiSpam.MaxLines or 4
    settings.AntiSpam.TimeFrame             = settings.AntiSpam.TimeFrame or 6

    -- Chatgroups
    self.Config.ChatGroups = self.Config.ChatGroups or {
        ["Donator"] = {
            ["Permission"] = "donator",
            ["Prefix"] = "[$$$]",
            ["PrefixPosition"] = "left",
            ["PrefixColor"] = "#06DCFB",
            ["NameColor"] = "#5af",
            ["TextColor"] = "#ffffff",
            ["PriorityRank"] = 4,
            ["ShowPrefix"] = true
        },
        ["VIP"] = {
            ["Permission"] = "vip",
            ["Prefix"] = "[VIP]",
            ["PrefixPosition"] = "left",
            ["PrefixColor"] = "#59ff4a",
            ["NameColor"] = "#5af",
            ["TextColor"] = "#ffffff",
            ["PriorityRank"] = 3,
            ["ShowPrefix"] = true,
        },
        ["Admin"] = {
            ["Permission"] = "admin",
            ["Prefix"] = "[Admin]",
            ["PrefixPosition"] = "left",
            ["PrefixColor"] = "#FF7F50",
            ["NameColor"] = "#5af",
            ["TextColor"] = "#ffffff",
            ["PriorityRank"] = 5,
            ["ShowPrefix"] = true,
        },
        ["Moderator"] = {
            ["Permission"] = "moderator",
            ["Prefix"] = "[Mod]",
            ["PrefixPosition"] = "left",
            ["PrefixColor"] = "#FFA04A",
            ["NameColor"] = "#5af",
            ["TextColor"] = "#ffffff",
            ["PriorityRank"] = 2,
            ["ShowPrefix"] = true,
        },
        ["Player"] = {
            ["Permission"] = "player",
            ["Prefix"] = "[Player]",
            ["PrefixPosition"] = "left",
            ["PrefixColor"] = "#ffffff",
            ["NameColor"] = "#5af",
            ["TextColor"] = "#ffffff",
            ["PriorityRank"] = 1,
            ["ShowPrefix"] = false,
        }
    }
    chatgroups = self.Config.ChatGroups

    -- Wordfilter
    self.Config.WordFilter = self.Config.WordFilter or {
        ["bitch"] = "sweety",
        ["fucking hell"] = "lovely heaven",
        ["cunt"] = "****",
        ["nigger"] = {"mute", "mute reason"},
        ["son of a bitch"] = {"kick", "kick reason"}
    }
    wordfilter = self.Config.WordFilter
    -- Check wordfilter for conflicts
    if settings.Wordfilter.EnableWordfilter == "true" then
        for key, value in pairs(wordfilter) do
            if type(value) ~= "table" then
                local first, _ = string.find(value:lower(), key:lower())
                if first then
                    wordfilter[key] = nil
                    print("Config error in wordfilter: [\""..key.."\":\""..value.."\"] both contain the same word")
                    print("[\""..key.."\":\""..value.."\"] was removed from word filter")
                end
            end
        end
    end

    -- message settings
    self.Config.Messages                = self.Config.Messages or {}
    messages = self.Config.Messages
    -- player messages
    messages.Player                     = messages.PlayerNotifications or messages.Player or {}
    messages.Player.AutoMuted           = messages.Player.AutoMuted or "You got {punishTime} auto muted for spam"
    messages.Player.SpamWarning         = messages.Player.SpamWarning or "If you keep spamming your punishment will raise"
    messages.Player.BroadcastAutoMutes  = messages.Player.BroadcastAutoMuted or "{name} got {punishTime} auto muted for spam"
    messages.Player.AdWarning           = messages.Player.AdWarning or "Its not allowed to advertise other servers"
    messages.Player.NoChatHistory       = messages.Player.NoChatHistory or "No chat history found"
    messages.Player.WordfilterList      = messages.Player.WordfilterList or "Blacklisted words: {wordFilterList}"
    -- admin messages
    messages.Admin                      = messages.AdminNotifications or messages.Admin or {}
    messages.Admin.NoPermission         = messages.Admin.NoPermission or "You dont have permission to use this command"
    messages.Admin.AdminModeEnabled     = messages.Admin.AdminModeEnabled or "You are now in admin mode"
    messages.Admin.AdminModeDisabled    = messages.Admin.AdminModeDisabled or "Admin mode disabled"
    messages.Admin.WordfilterError      = messages.Admin.WordfilterError or "Error: {replacement} contains the word {word}"
    messages.Admin.WordfilterAdded      = messages.Admin.WordfilterAdded or "WordFilter added. {word} will now be replaced with {replacement}"
    messages.Admin.WordfilterRemoved    = messages.Admin.WordfilterRemoved or "successfully removed {word} from the wordfilter"
    messages.Admin.WordfilterNotFound   = messages.Admin.WordfilterNotFound or "No filter for {word} found to remove"
    -- helptext messages
    messages.Helptext                   = messages.Helptext or {}
    messages.Helptext.Wordfilter        = messages.Helptext.Wordfilter or "Use /wordfilter list to see blacklisted words"
    messages.Helptext.ChatHistory       = messages.Helptext.ChatHistory or "Use /history or /h to view recent chat history"

    -- remove old entries
    messages.PlayerNotifications = nil
    messages.AdminNotifications = nil
    
    self:SaveConfig()
end
-- --------------------------------
-- load all chat commands, depending on settings
-- --------------------------------
function PLUGIN:LoadCommands()
    for _, cmd in pairs(settings.ChatCommands.AdminMode) do
        command.AddChatCommand(cmd, self.Object, "cmdAdminMode")
    end
    if settings.General.EnableChatHistory == "true" then
        for _, cmd in pairs(settings.ChatCommands.ChatHistory) do
            command.AddChatCommand(cmd, self.Object, "cmdHistory")
        end
    end
    if settings.Wordfilter.EnableWordfilter == "true" then
        for _, cmd in pairs(settings.ChatCommands.Wordfilter) do
            command.AddChatCommand(cmd, self.Object, "cmdEditWordFilter")
        end
    end
    command.AddConsoleCommand("chathandler.debug", self.Object, "ccmdDebug")
end
-- --------------------------------
-- handles all data files
-- --------------------------------
function PLUGIN:LoadDataFiles()
    spamData = datafile.GetDataTable(SpamList) or {}
end
-- --------------------------------
-- register all permissions
-- --------------------------------
function PLUGIN:RegisterPermissions()
    -- command permissions
    for _, perm in pairs(settings.Permissions) do
        if not permission.PermissionExists(perm) then
            permission.RegisterPermission(perm, self.Object)
        end
    end
    -- group permissions
    if settings.General.EnableChatGroups == "true" then
        for key, _ in pairs(chatgroups) do
            if not permission.PermissionExists(chatgroups[key].Permission) then
                permission.RegisterPermission(chatgroups[key].Permission, self.Object)
            end
        end
        -- grant default groups default permissions
        local defaultGroups = {"Player", "Moderator", "Admin"}
        for i = 1, 3, 1 do
            if not permission.GroupHasPermission(defaultGroups[i]:lower(), chatgroups[defaultGroups[i]].Permission) then
                permission.GrantGroupPermission(defaultGroups[i]:lower(), chatgroups[defaultGroups[i]].Permission, self.Object)
            end
        end
    end
end
-- --------------------------------
-- broadcasts chat messages
-- --------------------------------
function PLUGIN:BroadcastChat(player, name, msg)
    Debug("-- BroadcastChat() --")
    local senderSteamID = rust.UserIDFromPlayer(player)
    Debug("senderSteamID: "..tostring(senderSteamID))
    if AdminMode[senderSteamID] then
        Debug("AdminMode on")
        senderSteamID = 0
        global.ConsoleSystem.Broadcast("chat.add", senderSteamID, name..msg)
        return
    end
    -- only send chat to people not ignoring sender
    eIgnoreAPI = plugins.Find("0ignoreAPI") or false
    if eIgnoreAPI then
        Debug("IgnoreAPI found")
        local playerList = global.BasePlayer.activePlayerList:GetEnumerator()
        while playerList:MoveNext() do
            if playerList.Current then
                Debug("playerList.Current: "..tostring(playerList.Current).." ("..tostring(type(playerList.Current))..")")
                local targetPlayer = playerList.Current
                local targetSteamID = rust.UserIDFromPlayer(targetPlayer)
                Debug("targetSteamID: "..tostring(targetSTeamID))
                local hasIgnored = eIgnoreAPI:Call("HasIgnored", targetSteamID, senderSteamID)
                if not hasIgnored then
                    Debug("targetPlayer not ignored")
                    targetPlayer:SendConsoleCommand("chat.add", senderSteamID, name..msg)
                end
            end
        end
        return
    end
    -- broadcast chat
    global.ConsoleSystem.Broadcast("chat.add", senderSteamID, name..msg)
end
-- --------------------------------
-- returns args as a table
-- --------------------------------
function PLUGIN:ArgsToTable(args, src)
    local argsTbl = {}
    if src == "chat" then
        local length = args.Length
        for i = 0, length - 1, 1 do
            argsTbl[i + 1] = args[i]
        end
        return argsTbl
    end
    if src == "console" then
        local i = 1
        while args:HasArgs(i) do
            argsTbl[i] = args:GetString(i - 1)
            i = i + 1
        end
        return argsTbl
    end
    return argsTbl
end
-- --------------------------------
-- handles chat command /admin
-- --------------------------------
function PLUGIN:cmdAdminMode(player)
    if not HasPermission(player, settings.Permissions.AdminMode) then
        rust.SendChatMessage(player, messages.Admin.NoPermission)
        return
    end
    local steamID = rust.UserIDFromPlayer(player)
    if AdminMode[steamID] then
        AdminMode[steamID] = nil
        rust.SendChatMessage(player, messages.Admin.AdminModeDisabled)
    else
        AdminMode[steamID] = true
        rust.SendChatMessage(player, messages.Admin.AdminModeEnabled)
    end
end
-- --------------------------------
-- activate/deactivate debug mode
-- --------------------------------
function PLUGIN:ccmdDebug(arg)
    if arg.connection then return end -- terminate if not server console
    local args = self:ArgsToTable(arg, "console")
    if args[1] == "true" then
        debugMode = true
        PrintToConsole("[ChatHandler]: debug mode activated")
    elseif args[1] == "false" then
        debugMode = false
        PrintToConsole("[ChatHandler]: debug mode deactivated")
    else
        PrintToConsole("Syntax: chathandler.debug true/false")
    end
end
-- --------------------------------
-- handles chat messages
-- --------------------------------
function PLUGIN:OnPlayerChat(arg)
    Debug("--- OnPlayerChat() ---")
    local msg = arg:GetString(0, "text")
    local player = arg.connection.player
    if msg:sub(1, 1) == "/" or msg == "" then return end
    local steamID = rust.UserIDFromPlayer(player)
    Debug("Player: "..player.displayName.." ("..steamID..")")
    eChatMute = plugins.Find("chatmute") or false
    if eChatMute then
        Debug("ChatMute found")
        local isMuted = eChatMute:Call("IsMuted", player)
        -- if muted abort chat handling and let chatmute handle chat canceling
        if isMuted then Debug("player muted") return end
    end
    -- Spam prevention
    if eChatMute and settings.AntiSpam.EnableAntiSpam == "true" then
        local isSpam, punishTime = self:AntiSpamCheck(player)
        if isSpam then
            rust.SendChatMessage(player, BuildOutput(messages.Player.AutoMuted, {"{punishTime}"}, {punishTime}))
            timer.Once(4, function() rust.SendChatMessage(player, messages.Player.SpamWarning) end)
            if settings.General.BroadcastMutes == "true" then
                rust.BroadcastChat(BuildOutput(messages.Player.BroadcastAutoMuted, {"{name}", "{punishTime}"}, {player.displayName, punishTime}))
            end
            if settings.Logging.LogToConsole == "true" then
                PrintToConsole("[ChatHandler] "..player.displayName.." got a "..punishTime.." auto mute for spam")
            end
            if settings.Logging.LogToFile == "true" then
                PrintToFile(player.displayName.." got a "..punishTime.." auto mute for spam")
            end
            return false
        end
    end
    -- Parse message to filter stuff and check if message should be blocked
    local canChat, msg, errorMsg, errorPrefix = self:ParseChat(player, msg)
    -- Chat is blocked
    if not canChat then
        if settings.Logging.LogBlockedMessages == "true" then
            if settings.Logging.LogToConsole == "true" then
                global.ServerConsole.PrintColoured(System.ConsoleColor.Cyan, errorPrefix, System.ConsoleColor.DarkYellow, " "..player.displayName..": ", System.ConsoleColor.DarkGreen, msg)
            end
            if settings.Logging.LogToFile == "true" then
                PrintToFile(errorPrefix.." "..steamID.."/"..player.displayName..": "..msg.."\n")
            end
        end
        if errorMsg then
            rust.SendChatMessage(player, errorMsg)
        end
        return false
    end
    -- Chat is ok and not blocked
    local charlimit = tonumber(settings.General.MaxCharsPerLine)
    msg = SplitLongMessages(msg, charlimit) -- msg is a table now
    local i = 1
    while msg[i] do
        local username, message, logUsername, logMessage = self:BuildNameMessage(player, msg[i])
        Debug("username: "..username)
        Debug("message: "..message)
        Debug("logUsername: "..logUsername)
        Debug("logMessage: "..logMessage)
        self:SendChat(player, username, message, logUsername, logMessage)
        i = i + 1
    end
    return false
end
-- --------------------------------
-- checks for chat spam
-- returns (bool)IsSpam, (string)punishTime
-- --------------------------------
function PLUGIN:AntiSpamCheck(player)
    local steamID = rust.UserIDFromPlayer(player)
    local now = time.GetUnixTimestamp()
    eChatMute = plugins.Find("chatmute") or false
    if eChatMute:Call("muteData", steamID) then return false, false end
    if AdminMode[steamID] then return false, false end
    if AntiSpam[steamID] then
        local firstMsg = AntiSpam[steamID].timestamp
        local msgCount = AntiSpam[steamID].msgcount
        if msgCount < settings.AntiSpam.MaxLines then
            AntiSpam[steamID].msgcount = AntiSpam[steamID].msgcount + 1
            return false, false
        else
            if now - firstMsg <= settings.AntiSpam.TimeFrame then
                -- punish
                local punishCount = 1
                local expiration, punishTime, newEntry
                if spamData[steamID] then
                    newEntry = false
                    punishCount = spamData[steamID].punishcount + 1
                    spamData[steamID].punishcount = punishCount
                    datafile.SaveDataTable(SpamList)
                end
                if punishCount == 1 then
                    expiration =  now + 300
                    punishTime = "5 minutes"
                elseif punishCount == 2 then
                    expiration = now + 3600
                    punishTime = "1 hour"
                else
                    expiration = 0
                    punishTime = "permanent"
                end
                if newEntry ~= false then
                    spamData[steamID] = {}
                    spamData[steamID].steamID = steamID
                    spamData[steamID].punishcount = punishCount
                    table.insert(spamData, spamData[steamID])
                    datafile.SaveDataTable(SpamList)
                end
                local apimuted = eChatMute:Call("APIMute", steamID, expiration)
                AntiSpam[steamID] = nil
                return true, punishTime
            else
                AntiSpam[steamID].timestamp = now
                AntiSpam[steamID].msgcount = 1
                return false, false
            end
        end
    else
        AntiSpam[steamID] = {}
        AntiSpam[steamID].timestamp = now
        AntiSpam[steamID].msgcount = 1
        return false, false
    end
end
-- --------------------------------
-- parses the chat
-- returns (bool)canChat, (string)msg, (string)errorMsg, (string)errorPrefix
-- --------------------------------
function PLUGIN:ParseChat(player, msg)
    local msg = tostring(msg)
    local steamID = rust.UserIDFromPlayer(player)
    if AdminMode[steamID] then return true, msg, false, false end
    -- Check for server advertisements
    if settings.General.BlockServerAds == "true" then
        local ipCheck
        local ipString = ""
        local chunks = {msg:match("(%d+)%.(%d+)%.(%d+)%.(%d+)")}
        if #chunks == 4 then
            for _, v in pairs(chunks) do
                if tonumber(v) < 0 or tonumber(v) > 255 then
                    ipCheck = false
                    break
                end
                ipString = ipString..v.."."
                ipCheck = true
            end
            -- remove the last dot
            if ipString:sub(-1) == "." then
                ipString = ipString:sub(1, -2)
            end
        else
            ipCheck = false
        end
        if ipCheck then
            local allowedIP = false
            for key, value in pairs(settings.General.AllowedIPsToPost) do
                if settings.General.AllowedIPsToPost[key]:match(ipString) then
                    allowedIP = true
                end
            end
            if not allowedIP then
                return false, msg, messages.Player.AdWarning, "[BLOCKED]"
            end
        end
    end
    -- Make html tags useless
    msg = msg:gsub("<[cC][oO][lL][oO][rR]", "<\\color\\")
    msg = msg:gsub("[cC][oO][lL][oO][rR]>", "\\color\\>")
    msg = msg:gsub("<[sS][iI][zZ][eE]", "<\\size\\")
    msg = msg:gsub("[sS][iI][zZ][eE]>", "\\size\\>")
    msg = msg:gsub("<[mM][aA][tT][eE][rR][iI][aA][lL]", "<\\material\\")
    msg = msg:gsub("[mM][aA][tT][eE][rR][iI][aA][lL]>", "\\material\\>")
    msg = msg:gsub("<[qQ][uU][aA][dD]", "<\\quad\\")
    msg = msg:gsub("[qQ][uU][aA][dD]>", "\\quad\\>")
    msg = msg:gsub("<[bB]", "<\\b\\")
    msg = msg:gsub("[bB]>", "\\b\\>")
    msg = msg:gsub("<[iI]", "<\\i\\")
    msg = msg:gsub("[iI]>", "\\i\\>")
    -- Check for blacklisted words
    if settings.Wordfilter.EnableWordfilter == "true" then
        for key, value in pairs(wordfilter) do
            local first, last = string.find(msg:lower(), key:lower(), nil, true)
            if first then
                if type(value) == "table" then
                    if settings.Wordfilter.AllowPunish == "true" then
                        -- kick, ban or mute for word usage
                        if value[1]:lower() == "mute" then
                            eChatMute = plugins.Find("chatmute") or false
                            if eChatMute then
                                eChatMute:Call("APIMute", steamID, 0)
                                return false, msg, value[2], "[BLOCKED]"
                            end
                        end
                        if value[1]:lower() == "kick" then
                            Network.Net.sv:Kick(player.net.connection, value[2])
                            return false, msg, false, "[BLOCKED]"
                        end
                    end
                else
                    -- replace words
                    while first do
                        local before = msg:sub(1, first - 1)
                        local after = msg:sub(last + 1)
                        -- replace whole word if parts are blacklisted
                        if settings.Wordfilter.ReplaceFullWord == "true" then
                            if before:sub(-1) ~= " " and before:len() > 0 then
                                local spaceStart, spaceEnd = before:reverse():find(" ")
                                if spaceStart then
                                    before = before:sub(spaceStart + 1):reverse()
                                else
                                    before = ""
                                end
                            end
                            if after:sub(1, 1) ~= " " and after:len() > 0 then
                                local spaceStart, spaceEnd = after:find(" ")
                                if spaceStart then
                                    after = after:sub(spaceStart)
                                else
                                    after = ""
                                end
                            end
                        end
                        msg = before..value..after
                        first, last = msg:lower():find(key:lower(), nil, true)
                    end
                end
            end
        end
    end

    return true, msg, false, false
end
-- --------------------------------
-- builds username and chatmessage
-- returns (string)username, (string)message, (string)logUsername, (string)logMessage
-- --------------------------------
function PLUGIN:BuildNameMessage(player, msg)
    local username, logUsername = player.displayName, player.displayName
    local message, logMessage = msg, msg
    local steamID = rust.UserIDFromPlayer(player)
    if AdminMode[steamID] then
        username = "<color="..settings.AdminMode.NameColor..">"..settings.AdminMode.ChatName.."</color>"
        message = "<color="..settings.AdminMode.TextColor..">: "..message.."</color>"
        logUsername = settings.AdminMode.ChatName..":"
        return username, message, logUsername, logMessage
    end
    if settings.General.EnableChatGroups == "true" then
        local priorityRank = 0
        local msgcolor, namecolor = "", ""
        for key, _ in pairs(chatgroups) do
            if permission.UserHasPermission(steamID, chatgroups[key].Permission) then
                if chatgroups[key].ShowPrefix then
                    if chatgroups[key].PrefixPosition == "left" then
                        username = "<color="..chatgroups[key].PrefixColor..">"..chatgroups[key].Prefix.."</color> "..username
                        logUsername = chatgroups[key].Prefix.." "..logUsername
                    else
                        username = username.." <color="..chatgroups[key].PrefixColor..">"..chatgroups[key].Prefix.."</color>"
                        logUsername = logUsername.." "..chatgroups[key].Prefix
                    end
                end
                if chatgroups[key].PriorityRank > priorityRank then
                    msgcolor = chatgroups[key].TextColor
                    namecolor = chatgroups[key].NameColor
                    priorityRank = chatgroups[key].PriorityRank
                end
            end
        end
        -- insert colors for name and message
        local first, last = username:find(player.displayName, 1, true)
        username = username:sub(1, first - 1).."<color="..namecolor..">"..player.displayName.."</color>"..username:sub(last + 1)
        message = "<color="..msgcolor..">: "..msg.."</color>"
    else
        -- Use default Rust name colors
        local namecolor = "#5af"
        if player:IsAdmin() then namecolor = "#af5" end
        if player:IsDeveloper() then namecolor = "#fa5" end
        username = "<color="..namecolor..">"..username.."</color>"
        message = ": "..msg
    end
    --[[
    -- Add title if plugin RanksAndTitles is installed
    if eRanksAndTitles then
        local title = eRanksAndTitles:Call("grabPlayerData", steamID, "Title")
        local hideTitle = eRanksAndTitles:Call("grabPlayerData", steamID, "hidden")
        local colorOn = eRanksAndTitles.Config.Settings.colorSupport
        local color = eRanksAndTitles:Call("getColor", steamID)
        if not hideTitle and title ~= "" and colorOn then
            username = username.."<color="..color.."> ["..title.."]</color>"
            logUsername = logUsername.." ["..title.."]"
        end
        if not hideTitle and title ~= "" and not colorOn then
            if username:sub(-8) == "</color>" then
                username = username:sub(1, -9).." ["..title.."]</color>"
                logUsername = logUsername.." ["..title.."]"
            else
                username = username.." ["..title.."]"
                logUsername = logUsername.." ["..title.."]"
            end
        end
    end
    ]]
    -- remove color tags from log message (possibly used in wordfilter)
    local tagStart, _ = logMessage:find("<color=", 1, true)
    if tagStart then
        local tagEnd, _ = logMessage:find(">", first, true)
        logMessage = logMessage:sub(1, tagStart -1)..logMessage:sub(tagEnd + 1)
        logMessage = logMessage:gsub("</color>", "")
    end
    return username, message, logUsername, logMessage
end
-- --------------------------------
-- sends and logs chat messages
-- --------------------------------
function PLUGIN:SendChat(player, name, msg, logName, logMsg)
    local steamID = rust.UserIDFromPlayer(player)
    -- Broadcast chat ingame
    self:BroadcastChat(player, name, msg)
    -- Log chat to console
    global.ServerConsole.PrintColoured(System.ConsoleColor.DarkYellow, logName..": ", System.ConsoleColor.DarkGreen, logMsg)
    UnityEngine.Debug.Log.methodarray[0]:Invoke(nil, util.TableToArray({"[CHAT] "..logName..": "..logMsg}))
    -- Log chat to log file
    ConVar.Server.Log("Log.Chat.txt", steamID.."/"..logName..": "..logMsg.."\n")
    -- Log chat history
    if settings.General.EnableChatHistory == "true" then
        self:InsertHistory(name, steamID, msg)
    end
end
-- --------------------------------
-- remove data on disconnect
-- --------------------------------
function PLUGIN:OnPlayerDisconnected(player)
    local steamID = rust.UserIDFromPlayer(player)
    AntiSpam[steamID] = nil
    AdminMode[steamID] = nil
end
-- --------------------------------
-- handles chat command for chat history
-- --------------------------------
function PLUGIN:cmdHistory(player)
    if #ChatHistory > 0 then
        rust.SendChatMessage(player, "ChatHistory", "----------")
        local i = 1
        while ChatHistory[i] do
            player:SendConsoleCommand("chat.add", ChatHistory[i].steamID, ChatHistory[i].name..ChatHistory[i].msg)
            i = i + 1
        end
        rust.SendChatMessage(player, "ChatHistory", "----------")
    else
        rust.SendChatMessage(player, "ChatHistory", messages.Player.NoChatHistory)
    end
end
-- --------------------------------
-- inserts chat messages into history
-- --------------------------------
function PLUGIN:InsertHistory(name, steamID, msg)
    if #ChatHistory == settings.General.ChatHistoryMaxLines then
        table.remove(ChatHistory, 1)
    end
    table.insert(ChatHistory, {["name"] = name, ["steamID"] = steamID, ["msg"] = msg})
end
-- --------------------------------
-- handles chat command /wordfilter
-- --------------------------------
function PLUGIN:cmdEditWordFilter(player, cmd, args)
    local args = self:ArgsToTable(args, "chat")
    local func, word, replacement = args[1], args[2], args[3]
    if not func or func ~= "add" and func ~= "remove" and func ~= "list" then
        if not HasPermission(player, settings.Permissions.EditWordFilter) then
            rust.SendChatMessage(player, "Syntax /wordfilter list")
        else
            rust.SendChatMessage(player, "Syntax: /wordfilter add <word> <replacement> or /wordfilter remove <word>")
        end
        return
    end
    if func ~= "list" and not HasPermission(player, settings.Permissions.EditWordFilter) then
        rust.SendChatMessage(player, messages.Admin.NoPermission)
        return
    end
    if func == "add" then
        if not replacement then
            rust.SendChatMessage(player, "Syntax: /wordfilter add <word> <replacement>")
            return
        end
        local first, last = string.find(replacement:lower(), word:lower())
        if first then
            rust.SendChatMessage(player, BuildOutput(messages.Admin.WordfilterError, {"{replacement}", "{word}"}, {replacement, word}))
            return
        else
            wordfilter[word] = replacement
            self:SaveConfig()
            rust.SendChatMessage(player, BuildOutput(messages.Admin.WordfilterAdded, {"{word}", "{replacement}"}, {word, replacement}))
        end
        return
    end
    if func == "remove" then
        if not word then
            rust.SendChatMessage(player, "Syntax: /wordfilter remove <word>")
            return
        end
        if wordfilter[word] then
            wordfilter[word] = nil
            self:SaveConfig()
            rust.SendChatMessage(player, BuildOutput(messages.Admin.WordfilterRemoved, {"{word}"}, {word}))
        else
            rust.SendChatMessage(player, BuildOutput(messages.Admin.WordfilterNotFound, {"{word}"}, {word}))
        end
        return
    end
    if func == "list" then
        local wordFilterList = ""
        for key, _ in pairs(wordfilter) do
            wordFilterList = wordFilterList..key..", "
        end
        rust.SendChatMessage(player, BuildOutput(messages.Player.WordfilterList, {"{wordFilterList}"}, {wordFilterList}))
    end
end
-- --------------------------------
-- handles chat command /help
-- --------------------------------
function PLUGIN:SendHelpText(player)
    if settings.General.EnableChatHistory == "true" then
        rust.SendChatMessage(player, messages.Helptext.ChatHistory)
    end
    if settings.Wordfilter.EnableWordfilter == "true" then
        rust.SendChatMessage(player, messages.Helptext.Wordfilter)
    end
end

