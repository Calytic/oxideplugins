PLUGIN.Title = "Heal, Feed and Cure"
PLUGIN.Description = "Allows you to heal, feed and cure players"
PLUGIN.Author = "#Domestos"
PLUGIN.Version = V(1, 2, 1)
PLUGIN.HasConfig = false
PLUGIN.ResourceID = 658

function PLUGIN:Init()
    command.AddChatCommand("heal", self.Object, "ChatCmd")
    command.AddChatCommand("cure", self.Object, "ChatCmd")
    command.AddChatCommand("feed", self.Object, "ChatCmd")
    command.AddConsoleCommand("player.heal", self.Object, "ConsoleCmd")
    command.AddConsoleCommand("player.cure", self.Object, "ConsoleCmd")
    command.AddConsoleCommand("player.feed", self.Object, "ConsoleCmd")
end

local function QuoteSafe(string)
    return UnityEngine.StringExtensions.QuoteSafe(string)
end

function PLUGIN:ChatMessage(targetPlayer, chatName, msg)
    if msg then
        targetPlayer:SendConsoleCommand("chat.add "..QuoteSafe(chatName).." "..QuoteSafe(msg))
    else
        msg = chatName
        targetPlayer:SendConsoleCommand("chat.add SERVER "..QuoteSafe(msg))
    end
end
-- --------------------------------
-- admin permission check
-- --------------------------------
local function IsAdmin(player)
    if player:GetComponent("BaseNetworkable").net.connection.authLevel == 0 then
        return false
    end
    return true
end

-- --------------------------------
-- Handles console commands
-- --------------------------------
function PLUGIN:ConsoleCmd(arg)
    local command = arg.cmd.namefull
    local player
    if arg.connection then
        player = arg.connection.player
    end
    -- Check permission
    if player and not IsAdmin(player) then
        arg:ReplyWith("You dont have permission to use this command")
        return true
    end
    -- Convert args
    local args = self:ArgsToTable(arg, "console")
    local target, amount = args[1], args[2]
    -- Check for target name
    if not player and not target then
        if command == "player.cure" then
            print("Syntax: \""..command.." <name>\"")
        else
            print("Syntax: \""..command.." <name> <amount (optional)>\"")
        end
        return true
    end
    -- Get target
    local targetPlayer = self:GetTargetPlayer(player, target)
    if not targetPlayer then
        if player then
            arg:ReplyWith("Player not found")
        else
            print("Player not found")
        end
        return true
    end
    -- Check for valid heal amount
    if amount and command ~= "player.cure" then
        amount = string.match(amount, "^%d*")
        if amount == "" then
            if player then
                arg:ReplyWith("<amount> needs to be a number")
            else
                print("<amount> needs to be a number")
            end
            return true
        end
    end
    if command == "player.heal" then
        amount = tonumber(amount) or 100
    elseif command == "player.feed" then
        amount = tonumber(amount) or 1000
    end
    -- Call function
    if command == "player.heal" then
        self:Heal(player, targetPlayer, amount)
    elseif command == "player.cure" then
        self:Cure(player, targetPlayer)
    elseif command == "player.feed" then
        self:Feed(player, targetPlayer, amount)
    end
    return true
end

-- --------------------------------
-- Handles chat commands
-- --------------------------------
function PLUGIN:ChatCmd(player, cmd, args)
    -- Check permission
    if not IsAdmin(player) then
        self:ChatMessage(player, "You dont have permission to use this command")
        return
    end
    local command = cmd
    local args = self:ArgsToTable(args, "chat")
    local target, amount = args[1], args[2]
    -- Check for valid heal amount
    if amount and command ~= "cure" then
        amount = string.match(amount, "^%d*")
        if amount == "" then
            self:ChatMessage(player, "<amount> needs to be a number")
            return
        end
    end
    if command == "heal" then
        amount = tonumber(amount) or 100
    elseif command == "feed" then
        amount = tonumber(amount) or 1000
    end
    -- Get target
    local targetPlayer = self:GetTargetPlayer(player, target)
    if not targetPlayer then
        self:ChatMessage(player, "Player not found")
        return
    end
    -- Call function
    if command == "heal" then
        self:Heal(player, targetPlayer, amount)
    elseif command == "cure" then
        self:Cure(player, targetPlayer)
    elseif command == "feed" then
        self:Feed(player, targetPlayer, amount)
    end
end

-- --------------------------------
-- Heal
-- --------------------------------
function PLUGIN:Heal(player, targetPlayer, amount)
    targetPlayer.metabolism.health.value = targetPlayer.metabolism.health.value + amount
    if player then
        if player ~= targetPlayer then
            self:ChatMessage(player, "You healed "..targetPlayer.displayName.." for "..tostring(amount).." HP")
            self:ChatMessage(targetPlayer, player.displayName.." healed you for "..tostring(amount).." HP")
        else
            self:ChatMessage(player, "You healed yourself for "..tostring(amount).." HP")
        end
    else
        print("You healed "..targetPlayer.displayName.." for "..tostring(amount).." HP")
        self:ChatMessage(targetPlayer, "An admin healed you for "..tostring(amount).." HP")
    end
end

-- --------------------------------
-- Cure
-- --------------------------------
function PLUGIN:Cure(player, targetPlayer)
    targetPlayer.metabolism.poison.value = 0
    targetPlayer.metabolism.radiation.value = 0
    targetPlayer.metabolism.oxygen.value = 1
    targetPlayer.metabolism.bleeding.value = 0
    targetPlayer.metabolism.wetness.value = 0
    targetPlayer.metabolism.dirtyness.value = 0
    if player then
        if player ~= targetPlayer then
            self:ChatMessage(player, "You cured "..targetPlayer.displayName)
            self:ChatMessage(targetPlayer, player.displayName.." cured you")
        else
            self:ChatMessage(player, "You cured yourself")
        end
    else
        print("You cured "..targetPlayer.displayName)
        self:ChatMessage(targetPlayer, "An admin cured you")
    end
end

-- --------------------------------
-- Feed
-- --------------------------------
function PLUGIN:Feed(player, targetPlayer, amount)
    targetPlayer.metabolism.calories.value = targetPlayer.metabolism.calories.value + amount
    targetPlayer.metabolism.hydration.value = targetPlayer.metabolism.hydration.value + amount
    if player then
        if player ~= targetPlayer then
            self:ChatMessage(player, "You fed "..targetPlayer.displayName.." for "..tostring(amount))
            self:ChatMessage(targetPlayer, player.displayName.." fed you for "..tostring(amount))
        else
            self:ChatMessage(player, "You fed yourself for "..tostring(amount))
        end
    else
        print("You fed "..targetPlayer.displayName.." for "..tostring(amount))
        self:ChatMessage(targetPlayer, "An admin fed you for "..tostring(amount))
    end
end

-- --------------------------------
-- returns targetPlayer or false
-- --------------------------------
function PLUGIN:GetTargetPlayer(player, target)
    local targetPlayer
    if not target then
        return player
    end
    local targetPlayer = global.BasePlayer.Find(target)
    if not targetPlayer then
        return false
    else
        return targetPlayer
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