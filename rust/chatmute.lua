PLUGIN.Title        = "ChatMute"
PLUGIN.Description  = "Helps moderating chat by muting players"
PLUGIN.Author       = "#Domestos"
PLUGIN.Version      = V(1, 1, 6)
PLUGIN.ResourceId   = 1053

local debugMode = false

function PLUGIN:Init()
    self:LoadDefaultConfig()
    self:LoadCommands()
    self:LoadDataFiles()
    self:RegisterPermissions()
end
-- --------------------------------
-- generates default config
-- --------------------------------
local settings, messages
function PLUGIN:LoadDefaultConfig()
    self.Config.Settings                    = self.Config.Settings or {}
    settings = self.Config.Settings
    -- General Settings
    settings.General                        = settings.General or {}
    settings.General.BroadcastMutes         = settings.General.BroadcastMutes or "true"
    settings.General.LogToConsole           = settings.General.LogToConsole or "true"
    -- Chat commands
    settings.ChatCommands                   = settings.ChatCommands or {}
    settings.ChatCommands.Mute              = settings.ChatCommands.Mute or {"mute"}
    settings.ChatCommands.Unmute            = settings.ChatCommands.Unmute or {"unmute"}
    settings.ChatCommands.GlobalMute        = settings.ChatCommands.GlobalMute or {"globalmute"}
    -- command permissions
    settings.Permissions                    = settings.Permissions or {}
    settings.Permissions.Mute               = settings.Permissions.Mute or "chat.mute"
    settings.Permissions.GlobalMute         = settings.Permissions.GlobalMute or "chat.globalmute"
    settings.Permissions.AntiGlobalMute     = settings.Permissions.AntiGlobalMute or "chat.notglobalmuted"
    -- Messages
    self.Config.Messages                    = self.Config.Messages or {}
    messages = self.Config.Messages
    -- admin messages
    messages.Admin                          = messages.Admin or {}
    messages.Admin.NoPermission             = messages.Admin.NoPermission or "You dont have permission to use this command"
    messages.Admin.PlayerNotFound           = messages.Admin.PlayerNotFound or "Player not found"
    messages.Admin.MultiplePlayerFound      = messages.Admin.MultiplePlayerFound or "Found more than one player, be more specific:"
    messages.Admin.AlreadyMuted             = messages.Admin.AlreadyMuted or "{name} is already muted"
    messages.Admin.PlayerMuted              = messages.Admin.PlayerMuted or "{name} has been muted"
    messages.Admin.InvalidTimeFormat        = messages.Admin.InvalidTimeFormat or "Invalid time format"
    messages.Admin.PlayerMutedTimed         = messages.Admin.PlayerMutedTimed or "{name} has been muted for {time}"
    messages.Admin.MutelistCleared          = messages.Admin.MutelistCleared or "Cleared {count} entries from mutelist"
    messages.Admin.PlayerUnmuted            = messages.Admin.PlayerUnmuted or "{name} has been unmuted"
    messages.Admin.PlayerNotMuted           = messages.Admin.PlayerNotMuted or "{name} is not muted"
    -- player messages
    messages.Player                         = messages.Player or {}
    messages.Player.GlobalMuteEnabled       = messages.Player.GlobalMuteEnabled or "Chat is now globally muted"
    messages.Player.GlobalMuteDisabled      = messages.Player.GlobalMuteDisabled or "Global chat mute disabled"
    messages.Player.BroadcastMutes          = messages.Player.BroadcastMutes or "{name} has been muted"
    messages.Player.Muted                   = messages.Player.Muted or "You have been muted"
    messages.Player.BroadcastMutesTimed     = messages.Player.BroadcastMutesTimed or "{name} has been muted for {time}"
    messages.Player.MutedTimed              = messages.Player.MutedTimed or "You have been muted for {time}"
    messages.Player.BroadcastUnmutes        = messages.Player.BroadcastUnmutes or "{name} has been unmuted"
    messages.Player.Unmuted                 = messages.Player.Unmuted or "You have been unmuted"
    messages.Player.IsMuted                 = messages.Player.IsMuted or "You are muted"
    messages.Player.IsTimeMuted             = messages.Player.IsTimeMuted or "You are muted for {timeMuted}"
    messages.Player.GlobalMuted             = messages.Player.GlobalMuted or "Chat is globally muted by an admin"

    self:SaveConfig()
end

local GlobalMute = false
local muteList = "chatmute"
local muteData = {}
function PLUGIN:LoadDataFiles()
    muteData = datafile.GetDataTable(muteList) or {}
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
-- load all commands, depending on settings
-- --------------------------------
function PLUGIN:LoadCommands()
    for _, cmd in pairs(settings.ChatCommands.Mute) do
        command.AddChatCommand(cmd, self.Object, "cmdMute")
    end
    for _, cmd in pairs(settings.ChatCommands.Unmute) do
        command.AddChatCommand(cmd, self.Object, "cmdUnMute")
    end
    for _, cmd in pairs(settings.ChatCommands.GlobalMute) do
        command.AddChatCommand(cmd, self.Object, "cmdGlobalMute")
    end
    -- Console commands
    command.AddConsoleCommand("player.mute", self.Object, "ccmdMute")
    command.AddConsoleCommand("player.unmute", self.Object, "ccmdUnMute")
    command.AddConsoleCommand("chatmute.debug", self.Object, "ccmdDebug")
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
-- prints to server console
-- --------------------------------
local function PrintToConsole(msg)
    --global.ServerConsole.PrintColoured(System.ConsoleColor.Cyan, msg)
    UnityEngine.Debug.Log.methodarray[0]:Invoke(nil, util.TableToArray({msg}))
end
-- --------------------------------
-- Debug reporting
-- --------------------------------
local function Debug(msg)
    if not debugMode then return end
    --global.ServerConsole.PrintColoured(System.ConsoleColor.Yellow, msg)
    UnityEngine.Debug.Log.methodarray[0]:Invoke(nil, util.TableToArray({"[Debug] "..msg}))
end
-- --------------------------------
-- register all permissions for group system
-- --------------------------------
function PLUGIN:RegisterPermissions()
    for _, perm in pairs(settings.Permissions) do
        if not permission.PermissionExists(perm) then
            permission.RegisterPermission(perm, self.Object)
        end
    end
end
-- --------------------------------
-- try to find a BasePlayer
-- returns (int) numFound, (table) playerTbl
-- --------------------------------
local function FindPlayer(NameOrIpOrSteamID, checkSleeper)
    local playerTbl = {}
    local enumPlayerList = global.BasePlayer.activePlayerList:GetEnumerator()
    while enumPlayerList:MoveNext() do
        local currPlayer = enumPlayerList.Current
        local currSteamID = rust.UserIDFromPlayer(currPlayer)
        local currIP = ""
        if currPlayer.net ~= nil and currPlayer.net.connection ~= nil then
            currIP = currPlayer.net.connection.ipaddress
        end 
        if currPlayer.displayName == NameOrIpOrSteamID or currSteamID == NameOrIpOrSteamID or currIP == NameOrIpOrSteamID then
            table.insert(playerTbl, currPlayer)
            return #playerTbl, playerTbl
        end
        local matched, _ = currPlayer.displayName:lower():find(NameOrIpOrSteamID:lower(), 1, true)
        if matched then
            table.insert(playerTbl, currPlayer)
        end
    end
    if checkSleeper then
        local enumSleeperList = global.BasePlayer.sleepingPlayerList:GetEnumerator()
        while enumSleeperList:MoveNext() do
            local currPlayer = enumSleeperList.Current
            local currSteamID = rust.UserIDFromPlayer(currPlayer)
            if currPlayer.displayName == NameOrIpOrSteamID or currSteamID == NameOrIpOrSteamID then
                table.insert(playerTbl, currPlayer)
                return #playerTbl, playerTbl
            end
            local matched, _ = currPlayer.displayName:lower():find(NameOrIpOrSteamID:lower(), 1, true)
            if matched then
                table.insert(playerTbl, currPlayer)
            end
        end
    end
    return #playerTbl, playerTbl
end
-- --------------------------------
-- Function to call by external plugins to check mute status
-- --------------------------------
-- return values:
-- true (bool) if muted permanent
-- expirationDate (timestamp) if muted for specific time
-- false (bool) if not muted
-- --------------------------------
function PLUGIN:IsMuted(player)
    local now = time.GetUnixTimestamp()
    local targetSteamID = rust.UserIDFromPlayer(player)
    if GlobalMute and not HasPermission(player, settings.Permissions.AntiGlobalMute) then
        return true
    end
    if not muteData[targetSteamID] then
        return false
    end
    if muteData[targetSteamID].expiration < now and muteData[targetSteamID].expiration ~= 0 then
        muteData[targetSteamID] = nil
        datafile.SaveDataTable(muteList)
        player:SetPlayerFlag(global.BasePlayer.PlayerFlags.VoiceMuted, false)
        return false
    end
    if muteData[targetSteamID].expiration == 0 then
        return true
    else
        return muteData[targetSteamID].expiration
    end
    return false
end
function PLUGIN:muteData(steamID)
    return muteData[steamID] ~= nil
end
function PLUGIN:APIMute(steamID, expiration)
    if muteData[steamID] then return false end
    muteData[steamID] = {}
    muteData[steamID].steamID = steamID
    muteData[steamID].expiration = expiration
    table.insert(muteData, muteData[steamID])
    datafile.SaveDataTable(muteList)
    local numFound, targetPlayerTbl = FindPlayer(steamID, false)
    targetPlayerTbl[1]:SetPlayerFlag(global.BasePlayer.PlayerFlags.VoiceMuted, true)
    return true
end
-- --------------------------------
-- handles chat command /globalmute
-- --------------------------------
function PLUGIN:cmdGlobalMute(player)
    if not HasPermission(player, settings.Permissions.GlobalMute) then
        rust.SendChatMessage(player, messages.Admin.NoPermission)
        return
    end
    if not GlobalMute then
        GlobalMute = true
        rust.BroadcastChat(messages.Player.GlobalMuteEnabled)
    else
        GlobalMute = false
        rust.BroadcastChat(messages.Player.GlobalMuteDisabled)
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
        PrintToConsole("[ChatMute]: debug mode activated")
    elseif args[1] == "false" then
        debugMode = false
        PrintToConsole("[ChatMute]: debug mode deactivated")
    else
        PrintToConsole("Syntax: chatmute.debug true/false")
    end
end
-- --------------------------------
-- handles chat command /mute
-- --------------------------------
function PLUGIN:cmdMute(player, cmd, args)
    if not HasPermission(player, settings.Permissions.Mute) then
        rust.SendChatMessage(player, messages.Admin.NoPermission)
        return
    end
    local args = self:ArgsToTable(args, "chat")
    local target, duration = args[1], args[2]
    if not target then
        rust.SendChatMessage(player, "Syntax: /mute <name/steamID> <time[m/h] (optional)>")
        return
    end
    local numFound, targetPlayerTbl = FindPlayer(target, false)
    if numFound == 0 then
        rust.SendChatMessage(player, messages.Admin.PlayerNotFound)
        return
    end
    if numFound > 1 then
        local targetNameString = ""
        for i = 1, numFound do
            targetNameString = targetNameString..targetPlayerTbl[i].displayName..", "
        end
        rust.SendChatMessage(player, messages.Admin.MultiplePlayerFound)
        rust.SendChatMessage(player, targetNameString)
        return
    end
    local targetPlayer = targetPlayerTbl[1]
    self:Mute(player, targetPlayer, duration, nil)
end
-- --------------------------------
-- handles console command player.mute
-- --------------------------------
function PLUGIN:ccmdMute(arg)
    local player, F1Console
    if arg.connection then
        player = arg.connection.player
    end
    if player then F1Console = true end
    if player and not HasPermission(player, settings.Permissions.Mute) then
        arg:ReplyWith(messages.Admin.NoPermission)
        return true
    end
    local args = self:ArgsToTable(arg, "console")
    local target, duration = args[1], args[2]
    if not target then
        if F1Console then
            arg:ReplyWith("Syntax: player.mute <name/steamID> <time[m/h] (optional)>")
        else
            PrintToConsole("Syntax: player.mute <name/steamID> <time[m/h] (optional)>")
        end
        return
    end
    local numFound, targetPlayerTbl = FindPlayer(target, false)
    if numFound == 0 then
        if F1Console then
            arg:ReplyWith(messages.Admin.PlayerNotFound)
        else
            PrintToConsole(messages.Admin.PlayerNotFound)
        end
        return
    end
    if numFound > 1 then
        local targetNameString = ""
        for i = 1, numFound do
            targetNameString = targetNameString..targetPlayerTbl[i].displayName..", "
        end
        if F1Console then
            arg:ReplyWith(messages.Admin.MultiplePlayerFound)
            for i = 1, numFound do
                arg:ReplyWith(targetPlayerTbl[i].displayName)
            end
        else
            PrintToConsole(messages.Admin.MultiplePlayerFound)
            for i = 1, numFound do
                PrintToConsole(targetPlayerTbl[i].displayName)
            end
        end
        return
    end
    local targetPlayer = targetPlayerTbl[1]
    self:Mute(player, targetPlayer, duration, arg)
end
-- --------------------------------
-- mute target
-- --------------------------------
function PLUGIN:Mute(player, targetPlayer, duration, arg)
    local targetName = targetPlayer.displayName
    local targetSteamID = rust.UserIDFromPlayer(targetPlayer)
    -- define source of command trigger
    local F1Console, srvConsole, chatCmd
    if player and arg then F1Console = true end
    if not player then srvConsole = true end
    if player and not arg then chatCmd = true end
    -- Check if target is already muted
    local isMuted = self:IsMuted(targetPlayer)
    if isMuted then
        if F1Console then
            arg:ReplyWith(BuildOutput(messages.Admin.AlreadyMuted, {"{name}"}, {targetName}))
        end
        if srvConsole then
            PrintToConsole(BuildOutput(messages.Admin.AlreadyMuted, {"{name}"}, {targetName}))
        end
        if chatCmd then
            rust.SendChatMessage(player, BuildOutput(messages.Admin.AlreadyMuted, {"{name}"}, {targetName}))
        end
        return
    end
    if not duration then
        -- No time is given, mute permanently
        muteData[targetSteamID] = {}
        muteData[targetSteamID].steamID = targetSteamID
        muteData[targetSteamID].expiration = 0
        table.insert(muteData, muteData[targetSteamID])
        datafile.SaveDataTable(muteList)
        targetPlayer:SetPlayerFlag(global.BasePlayer.PlayerFlags.VoiceMuted, true)
        local isVoiceMuted = targetPlayer:HasPlayerFlag(global.BasePlayer.PlayerFlags.VoiceMuted)
        Debug(targetSteamID.." | "..targetName..": VoiceMuted "..tostring(isVoiceMuted == true))
        -- Send mute notice
        if settings.General.BroadcastMutes == "true" then
            rust.BroadcastChat(BuildOutput(messages.Player.BroadcastMutes, {"{name}"}, {targetName}))
            if F1Console then
                arg:ReplyWith(BuildOutput(messages.Admin.PlayerMuted, {"{name}"}, {targetName}))
            end
            if srvConsole then
                PrintToConsole(BuildOutput(messages.Admin.PlayerMuted, {"{name}"}, {targetName}))
            end
        else
            if F1Console then
                arg:ReplyWith(BuildOutput(messages.Admin.PlayerMuted, {"{name}"}, {targetName}))
            end
            if srvConsole then
                PrintToConsole(BuildOutput(lmessages.Admin.PlayerMuted, {"{name}"}, {targetName}))
            end
            if chatCmd then
                rust.SendChatMessage(player, BuildOutput(messages.Admin.PlayerMuted, {"{name}"}, {targetName}))
            end
            rust.SendChatMessage(targetPlayer, messages.Player.Muted)
        end
        -- Send console log
        if settings.General.LogToConsole == "true" then
            if not player then
                PrintToConsole("[ChatMute] An admin muted "..targetName)
            else
                PrintToConsole("[ChatMute] "..player.displayName.." muted "..targetName)
            end
        end
        return
    end
    -- Time is given, mute only for this timeframe
    -- Check for valid time format
    local c = string.match(duration, "^%d*[mh]$")
    if string.len(duration) < 2 or not c then
        if F1Console then
            arg:ReplyWith(messages.Admin.InvalidTimeFormat)
        end
        if srvConsole then
            PrintToConsole(messages.Admin.InvalidTimeFormat)
        end
        if chatCmd then
            rust.SendChatMessage(player, messages.Admin.InvalidTimeFormat)
        end
        return
    end
    -- Build expiration time
    local now = time.GetUnixTimestamp()
    local muteTime = tonumber(string.sub(duration, 1, -2))
    local timeUnit = string.sub(duration, -1)
    local timeMult, timeUnitLong
    if timeUnit == "m" then
        timeMult = 60
        timeUnitLong = "minutes"
    end
    if timeUnit == "h" then
        timeMult = 3600
        timeUnitLong = "hours"
    end
    local expiration = (now + (muteTime * timeMult))
    local time = muteTime.." "..timeUnitLong
    -- Mute player for given duration
    muteData[targetSteamID] = {}
    muteData[targetSteamID].steamID = targetSteamID
    muteData[targetSteamID].expiration = expiration
    table.insert(muteData, muteData[targetSteamID])
    datafile.SaveDataTable(muteList)
    targetPlayer:SetPlayerFlag(global.BasePlayer.PlayerFlags.VoiceMuted, true)
    local isVoiceMuted = targetPlayer:HasPlayerFlag(global.BasePlayer.PlayerFlags.VoiceMuted)
    Debug(targetSteamID.." | "..targetName..": VoiceMuted "..tostring(isVoiceMuted == true))
    -- Send mute notice
    if settings.General.BroadcastMutes == "true" then
        rust.BroadcastChat(BuildOutput(messages.Player.BroadcastMutesTimed, {"{name}", "{time}"}, {targetName, time}))
        if F1Console then
            arg:ReplyWith(BuildOutput(messages.Admin.PlayerMutedTimed, {"{name}", "{time}"}, {targetName, time}))
        end
        if srvConsole then
            PrintToConsole(BuildOutput(messages.Admin.PlayerMutedTimed, {"{name}", "{time}"}, {targetName, time}))
        end
    else
        rust.SendChatMessage(targetPlayer, BuildOutput(messages.Player.MutedTimed, {"{time}"}, {time}))
        if F1Console then
            arg:ReplyWith(BuildOutput(messages.Admin.PlayerMutedTimed, {"{name}", "{time}"}, {targetName, time}))
        end
        if srvConsole then
            PrintToConsole(BuildOutput(messages.Admin.PlayerMutedTimed, {"{name}", "{time}"}, {targetName, time}))
        end
        if chatCmd then
            rust.SendChatMessage(player, BuildOutput(messages.Admin.PlayerMutedTimed, {"{name}", "{time}"}, {targetName, time}))
        end
    end
    -- Send console log
    if settings.General.LogToConsole == "true" then
        if not player then
            PrintToConsole("[ChatMute] An admin muted "..targetName.." for "..muteTime.." "..timeUnitLong)
        else
            PrintToConsole("[ChatMute] "..player.displayName.." muted "..targetName.." for "..muteTime.." "..timeUnitLong)
        end
    end
end
-- --------------------------------
-- handles chat command /unmute
-- --------------------------------
function PLUGIN:cmdUnMute(player, cmd, args)
    if not HasPermission(player, settings.Permissions.Mute) then
        rust.SendChatMessage(player, messages.Admin.NoPermission)
        return
    end
    local args = self:ArgsToTable(args, "chat")
    local target = args[1]
    -- Check for valid syntax
    if not target then
        rust.SendChatMessage(player, "Syntax: /unmute <name|steamID> or /unmute all to clear mutelist")
        return
    end
    -- Check if "all" is used to clear the whole mutelist
    if target == "all" then
        local mutecount = #muteData
        muteData = {}
        datafile.SaveDataTable(muteList)
        rust.SendChatMessage(player, BuildOutput(messages.Admin.MutelistCleared, {"{count}"}, {tostring(mutecount)}))
        return
    end
    -- Try to get target netuser
    local numFound, targetPlayerTbl = FindPlayer(target, false)
    if numFound == 0 then
        rust.SendChatMessage(player, messages.Admin.PlayerNotFound)
        return
    end
    if numFound > 1 then
        local targetNameString = ""
        for i = 1, numFound do
            targetNameString = targetNameString..targetPlayerTbl[i].displayName..", "
        end
        rust.SendChatMessage(player, messages.Admin.MultiplePlayerFound)
        rust.SendChatMessage(player, targetNameString)
        return
    end
    local targetPlayer = targetPlayerTbl[1]
    self:Unmute(player, targetPlayer, nil)
end
-- --------------------------------
-- handles console command player.unmute
-- --------------------------------
function PLUGIN:ccmdUnMute(arg)
    local player, F1Console
    if arg.connection then
        player = arg.connection.player
    end
    if player then F1Console = true end
    if player and not HasPermission(player, settings.Permissions.Mute) then
        arg:ReplyWith(messages.Admin.NoPermission)
        return true
    end
    local args = self:ArgsToTable(arg, "console")
    local target = args[1]
    if not target then
        if F1Console then
            arg:ReplyWith("Syntax: player.unmute <name/steamID> or player.unmute all to clear mutelist")
        else
            PrintToConsole("Syntax: player.unmute <name/steamID> or player.unmute all to clear mutelist")
        end
        return
    end
    -- Check if "all" is used to clear the whole mutelist
    if target == "all" then
        local mutecount = #muteData
        muteData = {}
        datafile.SaveDataTable(muteList)
        if F1Console then
            arg:ReplyWith(BuildOutput(messages.Admin.MutelistCleared, {"{count}"}, {tostring(mutecount)}))
        else
            PrintToConsole(BuildOutput(messages.Admin.MutelistCleared, {"{count}"}, {tostring(mutecount)}))
        end
        return
    end
    local numFound, targetPlayerTbl = FindPlayer(target, false)
    if numFound == 0 then
        if F1Console then
            arg:ReplyWith(messages.Admin.PlayerNotFound)
        else
            PrintToConsole(messages.Admin.PlayerNotFound)
        end
        return
    end
    if numFound > 1 then
        local targetNameString = ""
        for i = 1, numFound do
            targetNameString = targetNameString..targetPlayerTbl[i].displayName..", "
        end
        if F1Console then
            arg:ReplyWith(messages.Admin.MultiplePlayerFound)
            for i = 1, numFound do
                arg:ReplyWith(targetPlayerTbl[i].displayName)
            end
        else
            PrintToConsole(messages.Admin.MultiplePlayerFound)
            for i = 1, numFound do
                PrintToConsole(targetPlayerTbl[i].displayName)
            end
        end
        return
    end
    local targetPlayer = targetPlayerTbl[1]
    self:Unmute(player, targetPlayer, arg)
end
-- --------------------------------
-- unmute target
-- --------------------------------
function PLUGIN:Unmute(player, targetPlayer, arg)
    local targetName = targetPlayer.displayName
    local targetSteamID = rust.UserIDFromPlayer(targetPlayer)
    -- define source of command trigger
    local F1Console, srvConsole, chatCmd
    if player and arg then F1Console = true end
    if not player then srvConsole = true end
    if player and not arg then chatCmd = true end
    -- Unmute player
    if muteData[targetSteamID] then
        muteData[targetSteamID] = nil
        datafile.SaveDataTable(muteList)
        targetPlayer:SetPlayerFlag(global.BasePlayer.PlayerFlags.VoiceMuted, false)
        local isVoiceMuted = targetPlayer:HasPlayerFlag(global.BasePlayer.PlayerFlags.VoiceMuted)
        Debug(targetSteamID.." | "..targetName..": VoiceMuted "..tostring(isVoiceMuted == true))
        -- Send unmute notice
        if settings.General.BroadcastMutes == "true" then
            rust.BroadcastChat(BuildOutput(messages.Player.BroadcastUnmutes, {"{name}"}, {targetName}))
            if F1Console then
                arg:ReplyWith(BuildOutput(messages.Admin.PlayerUnmuted, {"{name}"}, {targetName}))
            end
            if srvConsole then
                PrintToConsole(BuildOutput(messages.Admin.PlayerUnmuted, {"{name}"}, {targetName}))
            end
        else
            rust.SendChatMessage(targetPlayer, messages.Player.Unmuted)
            if F1Console then
                arg:ReplyWith(BuildOutput(messages.Admin.PlayerUnmuted, {"{name}"}, {targetName}))
            end
            if srvConsole then
                PrintToConsole(BuildOutput(messages.Admin.PlayerUnmuted, {"{name}"}, {targetName}))
            end
            if chatCmd then
                rust.SendChatMessage(player, BuildOutput(messages.Admin.PlayerUnmuted, {"{name}"}, {targetName}))
            end
        end
        -- Send console log
        if settings.General.LogToConsole == "true" then
            if player then
                PrintToConsole("[ChatMute] "..player.displayName.." unmuted "..targetName)
            else
                PrintToConsole("[ChatMute] An admin unmuted "..targetName)
            end
        end
        return
    end
    -- player is not muted
    if F1Console then
        arg:ReplyWith(BuildOutput(messages.Admin.PlayerNotMuted, {"{name}"}, {targetName}))
    end
    if srvConsole then
        PrintToConsole(BuildOutput(messages.Admin.PlayerNotMuted, {"{name}"}, {targetName}))
    end
    if chatCmd then
        rust.SendChatMessage(player, BuildOutput(messages.Admin.PlayerNotMuted, {"{name}"}, {targetName}))
    end
end
-- --------------------------------
-- capture player chat
-- --------------------------------
function PLUGIN:OnRunCommand(arg)
    if not arg.connection then return end
    if not arg.cmd then return end
    local cmd = arg.cmd.namefull
    local msg = arg:GetString(0, "text")
    local player = arg.connection.player
    if cmd == "chat.say" and msg:sub(1, 1) ~= "/" then
        if GlobalMute and not HasPermission(player, settings.Permissions.AntiGlobalMute) then
            rust.SendChatMessage(player, messages.Player.GlobalMuted)
            return true
        end
        local IsMuted = self:IsMuted(player)
        if not IsMuted then return end
        if IsMuted ~= true and IsMuted > 0 then
            local now = time.GetUnixTimestamp()
            local expiration = IsMuted
            local muteTime = expiration - now
            local hours = tostring(math.floor(muteTime / 3600)):format("%02.f")
            local minutes = tostring(math.floor(muteTime / 60 - (hours * 60))):format("%02.f")
            local seconds = tostring(math.floor(muteTime - (hours * 3600) - (minutes * 60))):format("%02.f")
            local expirationString = tostring(hours.."h "..minutes.."m "..seconds.."s")
            rust.SendChatMessage(player, BuildOutput(messages.Player.IsTimeMuted, {"{timeMuted}"}, {expirationString}))
            return true
        else
            rust.SendChatMessage(player, messages.Player.IsMuted)
            return true
        end
    end
end

function PLUGIN:OnPlayerInit(player)
    local isVoiceMuted = player:HasPlayerFlag(global.BasePlayer.PlayerFlags.VoiceMuted)
    local steamID = rust.UserIDFromPlayer(player)
    Debug(steamID.." | "..player.displayName..": VoiceMuted on join -  "..tostring(isVoiceMuted == true))
    if isVoiceMuted then
        player:SetPlayerFlag(global.BasePlayer.PlayerFlags.VoiceMuted, false)
    end
end