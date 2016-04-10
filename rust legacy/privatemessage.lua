PLUGIN.Title        = "Private Messaging"
PLUGIN.Description  = "Allows users to chat private with each other"
PLUGIN.Author       = "#Domestos"
PLUGIN.Version      = V(1, 0, 0)
PLUGIN.ResourceId   = _


local pmHistory = {}
function PLUGIN:Init()
    command.AddChatCommand("pm", self.Object, "cmdPm")
    command.AddChatCommand("r", self.Object, "cmdReply")
end

-- --------------------------------
-- temporary until rust.SendChatMessage() supports chat names
-- --------------------------------
function PLUGIN:SendChatMessage(netuser, ToOrFrom, chatName, message)
    local color = "#ff00ff"
    local chatName = "PM "..ToOrFrom.." "..chatName
    local message = "[Color "..color.."]"..message
    global.ConsoleNetworker.SendClientCommand(netuser.networkPlayer, "chat.add "..rust.QuoteSafe(chatName).." "..rust.QuoteSafe(message).."")
end
-- --------------------------------
-- Chat command for pm
-- --------------------------------
function PLUGIN:cmdPm(netuser, _, args)
    local args = self:ArgsToTable(args, "chat")
    local target, message = args[1], ""
    local i = 2
    while args[i] do
        message = message..args[i].." "
        i = i + 1
    end
    if not target or message == "" then
        -- no target or no message is given
        rust.SendChatMessage(netuser, "Syntax: /pm <name> <message>")
        return
    end
    local targetNetuser = rust.FindPlayer(target)
    if not targetNetuser then
        rust.SendChatMessage(netuser, "Player not found")
        return
    end
    local senderName = netuser.displayName
    local senderSteamID = rust.UserIDFromPlayer(netuser)
    local targetName = targetNetuser.displayName
    local targetSteamID = rust.UserIDFromPlayer(targetNetuser)
    self:SendChatMessage(targetNetuser, "from", senderName, message)
    self:SendChatMessage(netuser, "to", targetName, message)
    pmHistory[targetSteamID] = senderSteamID
end
-- --------------------------------
-- Chat command for reply
-- --------------------------------
function PLUGIN:cmdReply(netuser, _, args) 
    local senderName = netuser.displayName
    local senderSteamID = rust.UserIDFromPlayer(netuser)
    local args = self:ArgsToTable(args, "chat")
    local message = ""
    local i = 1
    while args[i] do
        message = message..args[i].." "
        i = i + 1
    end
    if message == "" then
        -- no args given
        rust.SendChatMessage(netuser, "Syntax: /r <message> to reply to last pm")
        return
    end
    if pmHistory[senderSteamID] then
        local targetNetuser = rust.FindPlayer(pmHistory[senderSteamID])
        if not targetNetuser then
            rust.SendChatMessage(netuser, "Player is offline")
            return
        end
        local targetName = targetNetuser.displayName
        self:SendChatMessage(targetNetuser, "from", senderName, message)
        self:SendChatMessage(netuser, "to", targetName, message)
    else
        rust.SendChatMessage(netuser, "No PM found to reply to")
        return
    end
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

function PLUGIN:OnPlayerDisconnected(networkPlayer)
    local netuser = networkPlayer:GetLocalData()
    local steamID = rust.UserIDFromPlayer(netuser)
    if pmHistory[steamID] then
        pmHistory[steamID] = nil
    end
end

function PLUGIN:SendHelpText(netuser)
    rust.SendChatMessage(netuser, "use /pm <name> <message> to pm someone")
    rust.SendChatMessage(netuser, "use /r <message> to reply to the last pm")
end