PLUGIN.Title = "Authenticator"
PLUGIN.Description = "Allow users to register their name on your server"
PLUGIN.Author = "#Domestos"
PLUGIN.Version = V(1, 1, 0)
PLUGIN.HasConfig = true
PLUGIN.ResourceID = 701

local DataFile = "authenticator"
local needIdentify = {}
local kickTimer = {}
local dataTable = {}


function PLUGIN:Init()
    command.AddChatCommand("auth", self.Object, "cmdAuth")
    command.AddChatCommand("authhelp", self.Object, "cmdAuthHelp")
    self:LoadDataFile()
    self:LoadDefaultConfig()
end

function PLUGIN:LoadDefaultConfig()
    -- Settings
    self.Config.Settings = self.Config.Settings or {}
    self.Config.Settings.LogToConsole = self.Config.Settings.LogToConsole or "false"
    self.Config.Settings.NotifyOnConnect = self.Config.Settings.NotifyOnConnect or "true"
    self.Config.Settings.TimeToIdentify = self.Config.Settings.TimeToIdentify or 40
    -- Messages
    self.Config.Messages = self.Config.Messages or {}
    self.Config.Messages.NotRegistered = self.Config.Messages.NotRegistered or "You dont have registered a name yet"
    self.Config.Messages.NotifyOnConnect = self.Config.Messages.NotifyOnConnect or "You can register your name on this server by using {command} so nobody can pretend to be you"
    self.Config.Messages.SuccessfullRegistered = self.Config.Messages.SuccessfullRegistered or "Your name is now registered to your steam account"
    self.Config.Messages.SuccessfullUnregistered = self.Config.Messages.SuccessfullUnregistered or "You have successfully unregistered your name"
    self.Config.Messages.SuccessfullIdentified = self.Config.Messages.SuccessfullIdentified or "Successfully identified"
    self.Config.Messages.NoIdentifyNeeded = self.Config.Messages.NoIdentifyNeeded or "You dont need to identify on this account"
    self.Config.Messages.WrongPassword = self.Config.Messages.WrongPassword or "Wrong password!"
    self.Config.Messages.KickMessage = self.Config.Messages.KickMessage or "You didnt identify your name. Please choose a different one"
    self.Config.Messages.NameAlreadyRegistered = self.Config.Messages.NameAlreadyRegistered or "The name you're using is registered to another steam account. Please identify by using {command}. You have {time} seconds before getting kicked"
    self.Config.Messages.RegisteredAnotherNameAlready = self.Config.Messages.RegisteredAnotherNameAlready or "Your steam account has already registered the name {name}. Use {command} to unregister"
    -- Save
    self:SaveConfig()
end

function PLUGIN:LoadDataFile()
    dataTable = datafile.GetDataTable(DataFile) or {}
end
function PLUGIN:SaveDataFile()
    datafile.SaveDataTable(DataFile)
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
-- handles chat command /auth
-- --------------------------------
function PLUGIN:cmdAuth(player, cmd, args)
    local args = self:ArgsToTable(args, "chat")
    local func, password = args[1], args[2]
    local playerSteamID = rust.UserIDFromPlayer(player)
    local playerName = player.displayName
    if not password or func ~= "register" and func ~= "unregister" and func ~= "identify" then
        self:ChatMessage(player, "Syntax: \"/auth <register/unregister/identify> <password>\"")
        return
    end
    if func == "register" then
        if needIdentify[playerSteamID] then
            local msg = string.gsub(self.Config.Messages.NameAlreadyRegistered, "{time}", self.Config.Settings.TimeToIdentify)
            msg = string.gsub(msg, "{command}", "\"/auth identify <password>\"")
            self:ChatMessage(player, msg)
            return
        end
        if not dataTable[playerSteamID] then
            dataTable[playerSteamID] = {}
            dataTable[playerSteamID].steamid = playerSteamID
            dataTable[playerSteamID].name = playerName
            dataTable[playerSteamID].password = password
            self:SaveDataFile()
            self:ChatMessage(player, self.Config.Messages.SuccessfullRegistered)
            if self.Config.Settings.LogToConsole == "true" then
                print("SteamID: "..tostring(playerSteamID).." registered the name "..playerName)
            end
            return
        end
        local msg = string.gsub(self.Config.Messages.RegisteredAnotherNameAlready, "{name}", dataTable[playerSteamID].name)
        msg = string.gsub(msg, "{command}", "\"/auth unregister <password>\"")
        self:ChatMessage(player, msg)
        return
    end
    if func == "unregister" then
        if not dataTable[playerSteamID] then
            self:ChatMessage(player, self.Config.Messages.NotRegistered)
            return
        end
        if password == dataTable[playerSteamID].password then
            dataTable[playerSteamID] = nil
            self:SaveDataFile()
            self:ChatMessage(player, self.Config.Messages.SuccessfullUnregistered)
            if self.Config.Settings.LogToConsole == "true" then
                print("SteamID: "..tostring(playerSteamID).." unregistered the name "..playerName)
            end
            return
        end
        self:ChatMessage(player, self.Config.Messages.WrongPassword)
        return
    end
    if func == "identify" then
        if not needIdentify[playerSteamID] then
            self:ChatMessage(player, self.Config.Messages.NoIdentifyNeeded)
            return
        end
        for key, _ in pairs(dataTable) do
            if dataTable[key].name == playerName then
                if dataTable[key].password == tostring(password) then
                    needIdentify[playerSteamID] = nil
                    kickTimer[playerSteamID] = nil
                    self:ChatMessage(player, self.Config.Messages.SuccessfullIdentified)
                    if self.Config.Settings.LogToConsole == "true" then
                        print("SteamID: "..tostring(playerSteamID).." identified as "..playerName.." (original account: "..dataTable[key].steamID..")")
                    end
                    return
                end
                self:ChatMessage(player, self.Config.Messages.WrongPassword)
                return
            end
        end
    end
end

-- --------------------------------
-- checks if player needs to identify
-- --------------------------------
function PLUGIN:OnPlayerInit(player)
    local playerSteamID = rust.UserIDFromPlayer(player)
    local playerName = player.displayName
    for key, _ in pairs(dataTable) do
        if dataTable[key].name == playerName then
            if dataTable[key].steamid ~= playerSteamID then
                local msg = string.gsub(self.Config.Messages.NameAlreadyRegistered, "{time}", self.Config.Settings.TimeToIdentify)
                msg = string.gsub(msg, "{command}", "\"/auth identify <password>\"")
                self:ChatMessage(player, msg)
                needIdentify[playerSteamID] = true
                kickTimer[playerSteamID] = timer.Once(self.Config.Settings.TimeToIdentify, function() self:CheckIdentify(player) end)
                if self.Config.Settings.LogToConsole == "true" then
                    print("SteamID: "..tostring(playerSteamID).." is forced to identify his name "..playerName)
                end
                return
            end
            return
        end
    end
    if not dataTable[playerSteamID] then
        if self.Config.Settings.NotifyOnConnect == "true" then
            local msg = string.gsub(self.Config.Messages.NotifyOnConnect, "{command}", "\"/auth register <password>\"")
            self:ChatMessage(player, msg)
        end
    end
end

-- --------------------------------
-- kicks player if not identified in time
-- --------------------------------
function PLUGIN:CheckIdentify(player)
    local playerSteamID = rust.UserIDFromPlayer(player)
    local playerName = player.displayName
    if not needIdentify[playerSteamID] then return end
    needIdentify[playerSteamID] = nil
    kickTimer[playerSteamID] = nil
    if self.Config.Settings.LogToConsole == "true" then
        print("SteamID: "..tostring(playerSteamID).." kicked for not identifying his name ("..playerName..")")
    end
    Network.Net.sv:Kick(player.net.connection, self.Config.Messages.KickMessage)
end

-- --------------------------------
-- remove data when disconnected before identified
-- --------------------------------
function PLUGIN:OnPlayerDisconnected(player)
    local playerSteamID = rust.UserIDFromPlayer(player)
    if needIdentify[playerSteamID] then
        needIdentify[playerSteamID] = nil
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

function PLUGIN:SendHelpText(player)
    self:ChatMessage(player, "Use \"/authhelp\" to get infos about the name authentication")
end
function PLUGIN:cmdAuthHelp(player)
    self:ChatMessage(player, "This server allows you to register your name so nobody can pretend to be you")
    self:ChatMessage(player, "Use \"/auth register <password>\" to register")
    self:ChatMessage(player, "If someone connects with your name from another steam account now he will be kicked")
end

function PLUGIN:Unload()
    if kickTimer then kickTimer = nil end
end