PLUGIN.Title = "Sleep"
PLUGIN.Version = V(0, 1, 2)
PLUGIN.Description = "Allows players with permission to get a well-rested sleep."
PLUGIN.Author = "Wulf / Luke Spragg"
PLUGIN.Url = "http://oxidemod.org/plugins/1156/"
PLUGIN.ResourceId = 1156

local debug = false

--[[ Do NOT edit the config here, instead edit Sleep.json in oxide/config ! ]]

local messages, settings
function PLUGIN:LoadDefaultConfig()
    self.Config.Messages = self.Config.Messages or {}
    messages = self.Config.Messages
    messages.CantSleep = messages.CantSleep or "You can't go to sleep right now!"
    messages.ChatHelp = messages.ChatHelp or "Use '/sleep' to go to sleep and rest"
    messages.Dirty = messages.Dirty or "You seem to be a bit dirty, go take a dip!"
    messages.Hungry = messages.Hungry or "You seem to be a bit hungry, eat something!"
    messages.Rested = messages.Rested or "You have awaken restored and rested!"
    messages.Thirsty = messages.Thirsty or "You seem to be a bit thirsty, drink something!"

    self.Config.Settings = self.Config.Settings or {}
    settings = self.Config.Settings
    settings.Command = settings.Command or "sleep"
    settings.Cure = settings.Cure or "false"
    settings.CurePercent = tonumber(settings.CurePercent) or 5
    settings.Heal = settings.Heal or "true"
    settings.HealPercent = tonumber(settings.HealPercent) or 5
    settings.Realism = settings.Realism or "true"
    settings.RealismPercent = tonumber(settings.RealismPercent) or 5
    settings.Restore = settings.Restore or "true"
    settings.RestorePercent = tonumber(settings.RestorePercent) or 5
    settings.UpdateRate = tonumber(settings.UpdateRate) or 10

    self:SaveConfig()
end

local function Print(self, message) print("[" .. self.Title .. "] " .. message) end

local function HasPermission(steamId, perm)
    if permission.UserHasPermission(steamId, perm) then return true end
    return false
end

function PLUGIN:Init()
    self:LoadDefaultConfig()
    command.AddChatCommand(settings.Command, self.Plugin, "ChatCommand")
    permission.RegisterPermission("sleep.allowed", self.Plugin)
end

local function Sleep(player) player:StartSleeping() end

local function WakeUp(player) player:EndSleeping() end

local function Cure(self, player, percent)
    -- Poison -- Default: 0, Min: 0, Max: 100
    local poison = player.metabolism.poison.value
    if poison > 0 then
        poison = poison - (poison / percent)
    end
    if debug then Print(self, player.displayName .. " poison: " .. poison) end

    -- Radiation level -- Default: 0, Min: 0, Max: 100
    local radLevel = player.metabolism.radiation_level.value
    if radLevel > 0 then
        radLevel = radLevel - (radLevel / percent)
    end
    if debug then Print(self, player.displayName .. " radiation level: " .. radLevel) end

    -- Radiation poison -- Default: 0, Min: 0, Max: 500
    local radPoison = player.metabolism.radiation_poison.value
    if radPoison > 0 then
        radPoison = radPoison - (radPoison / percent)
    end
    if debug then Print(self, player.displayName .. " radiation poison: " .. radPoison) end
end

local function Heal(self, player, percent)
    -- Bleeding -- Default: 0, Min: 0, Max: 1
    local bleeding = player.metabolism.bleeding.value
    if bleeding == 1 then bleeding = 0 end
    if debug then Print(self, player.displayName .. " bleeding: " .. bleeding) end

    -- Health -- Default: 50-60, Min: 0, Max: 100
    local health = player.health
    if health < 100 then
        health = health + (health / percent)
    end
    if debug then Print(self, player.displayName .. " health: " .. health) end

end

local function Realism(self, player, percent)
    -- Calories -- Default: 75-100, Min: 0, Max: 1000
    local calories = player.metabolism.calories.value
    if calories < 1000 then
        calories = calories - (calories / percent)
    end
    if debug then Print(self, player.displayName .. " calories: " .. calories) end

    -- Dirtyness -- Default: 0, Min: 0, Max: 100
    local dirtyness = player.metabolism.dirtyness.value
    if dirtyness < 100 then
        dirtyness = dirtyness + (dirtyness / percent)
    end
    if debug then Print(self, player.displayName .. " dirtyness: " .. dirtyness) end

    -- Hydration -- Default: 75-100, Min: 0, Max: 1000
    local hydration = player.metabolism.hydration.value
    if hydration >= 1 then
        hydration = hydration - (hydration / percent)
    end
    if debug then Print(self, player.displayName .. " hydration: " .. hydration) end
end

local function Restore(self, player, percent)
    -- Comfort -- Default: 0.5, Min: 0, Max: 1
    local comfort = player.metabolism.comfort.value
    if comfort < 0.5 then
        comfort = comfort + (comfort / percent)
    end
    if debug then Print(self, player.displayName .. " comfort: " .. comfort) end

    -- Heartrate -- Default 0.5, Min: 0, Max: 1
    local heartrate = player.metabolism.heartrate.value
    if heartrate > 0.5 then
        heartrate = heartrate - (heartrate / percent)
    end
    if debug then Print(self, player.displayName .. " heartrate: " .. heartrate) end

    -- Temperature -- Default: 20, Min: -100, Max: 100
    local temperature = player.metabolism.temperature.value
    if temperature ~= 20 then
        if temperature < 20 then
            temperature = temperature + (temperature / percent)
        elseif temperature > 20 then
            temperature = temperature - (temperature / percent)
        end
    end
    if debug then Print(self, player.displayName .. " temperature: " .. temperature) end
end

local sleepTimer = {}

function PLUGIN:ChatCommand(player, cmd, args)
    local steamId = rust.UserIDFromPlayer(player)

    if not HasPermission(steamId, "sleep.allowed") then
        rust.SendChatMessage(player, messages.CantSleep)
        return
    end

    Sleep(player)

    sleepTimer[steamId] = timer.Repeat(settings.UpdateRate, 0, function()
        if player:IsSleeping() then
            if settings.Cure == "true" then
                Cure(self, player, settings.CurePercent)
            end
            if settings.Heal == "true" then
                Heal(self, player, settings.HealPercent)
            end

            if settings.Realism == "true" then
                Realism(self, player, settings.RealismPercent)
            end

            if settings.Restore == "true" then
                Restore(self, player, settings.RestorePercent)
            end

            player.metabolism:SendChangesToClient()
        end
    end, self.Plugin)
end

function PLUGIN:OnPlayerSleepEnded(player)
    local steamId = rust.UserIDFromPlayer(player)

    if sleepTimer[steamId] then
        sleepTimer[steamId]:Destroy()

        rust.SendChatMessage(player, messages.Rested)
    end

    if player.metabolism.calories.value < 40 then
        rust.SendChatMessage(player, messages.Hungry)
    end

    if player.metabolism.dirtyness.value > 0 then
        rust.SendChatMessage(player, messages.Dirty)
    end

    if player.metabolism.hydration.value < 40 then
        rust.SendChatMessage(player, messages.Thirsty)
    end
end

function PLUGIN:SendHelpText(player)
    if HasPermission(rust.UserIDFromPlayer(player), "sleep.allowed") then
        rust.SendChatMessage(player, messages.ChatHelp)
    end
end
