PLUGIN.Title = "Respawner"
PLUGIN.Version = V(0, 2, 1)
PLUGIN.Description = "Automatically respawns players after they die."
PLUGIN.Author = "Wulf / Luke Spragg"
PLUGIN.Url = "http://oxidemod.org/plugins/669/"
PLUGIN.ResourceId = 669

local debug = false

--[[ Do NOT edit the config here, instead edit Respawner.json in oxide/config ! ]]

function PLUGIN:LoadDefaultConfig()
    self.Config.Messages = self.Config.Messages or {}
    messages = self.Config.Messages
    messages.CustomSpawn = messages.CustomSpawn or "You've respawned at {location}"
    messages.SameLocation = messages.SameLocation or "You've respawned at the same location"
    messages.SleepingBag = messages.SleepingBag or "You've respawned at your sleeping bag"

    self.Config.Settings = self.Config.Settings or {}
    settings = self.Config.Settings
    settings.AutoWakeUp = settings.AutoWakeUp or "true"
    settings.CustomSpawn = settings.CustomSpawn or "false"
    settings.SameLocation = settings.SameLocation or "false"
    settings.ShowMessages = settings.ShowMessages or "true"
    settings.SleepingBags = settings.SleepingBags or "false"

    self:SaveConfig()
end

local function Print(self, message) print("[" .. self.Title .. "] " .. message) end

local FindForPlayer = global.SleepingBag.FindForPlayer.methodarray[0]
local function FindSleepingBags(steamId)
    local array = util.TableToArray({ steamId, true })
    util.ConvertAndSetOnArray(array, 0, steamId, System.UInt64._type)
    return FindForPlayer:Invoke(nil, array)
end

function PLUGIN:Init() self:LoadDefaultConfig() end

function PLUGIN:Respawn(player)
    local steamId = rust.UserIDFromPlayer(player)
    local spawnTimer = {}

    spawnTimer[steamId] = timer.Once(1, function()
        if settings.SameLocation == "true" then
            if debug then Print(self, "Original location: " .. tostring(player.transform.position)) end

            player:Respawn(false)
            if debug then Print(self, "Target location: " .. tostring(player.transform.position)) end

            if settings.ShowMessages == "true" then
                rust.SendChatMessage(player, messages.SameLocation)
            end

        elseif settings.SleepingBags == "true" then
            local sleepingBags = FindSleepingBags(steamId)

            if sleepingBags.Length >= 1 then
                if debug then
                    Print(self, "Sleeping bags: " .. tostring(sleepingBags))
                    Print(self, "# of sleeping bags: " .. sleepingBags.Length)
                end

                local sleepingBag = sleepingBags[math.random(0, sleepingBags.Length - 1)]

                if debug then
                    Print(self, "Original location: " .. tostring(player.transform.position))
                    Print(self, "Target bag: " .. tostring(sleepingBag))
                    Print(self, "Target location: " .. tostring(sleepingBag.transform.position))
                end

                player.transform.position = sleepingBag.transform.position
                player.transform.rotation = sleepingBag.transform.rotation

                player:Respawn(false)

                if settings.ShowMessages == "true" then
                    rust.SendChatMessage(player, messages.SleepingBag)
                end
            else
                player:Respawn(true)
            end

        elseif settings.CustomSpawn:match("([^,]+), ([^,]+), ([^,]+)") then
            if debug then Print(self, "Custom spawn: " .. tostring(settings.CustomSpawn)) end
            local x, y, z = settings.CustomSpawn:match("([^,]+), ([^,]+), ([^,]+)")
            local spawn = player.transform.position
            spawn.x, spawn.y, spawn.z = tonumber(x), tonumber(y), tonumber(z)

            player.transform.position = spawn
            player.transform.rotation = player.transform.rotation
            if debug then Print(self, "Target location: " .. tostring(player.transform.position)) end

            player:Respawn(false)

            if settings.ShowMessages == "true" then
                local message = messages.CustomSpawn:gsub("{location}", settings.CustomSpawn)
                rust.SendChatMessage(player, message)
            end

        else
            player:Respawn(true)
        end

        if settings.AutoWakeUp == "true" then player:EndSleeping() end
    end, self.Plugin)
end

function PLUGIN:OnEntityDeath(entity)
    local player = entity:ToPlayer()
    if player and player:IsConnected() then self:Respawn(player) end
end

function PLUGIN:OnPlayerRespawned(player)
    if debug then Print(self, "Spawn location: " .. tostring(player.transform.position)) end
    player:ClientRPCPlayer(nil, player, "ForcePositionTo", player.transform.position)
end
