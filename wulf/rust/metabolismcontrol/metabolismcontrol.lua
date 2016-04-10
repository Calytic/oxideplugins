PLUGIN.Title = "Metabolism Control"
PLUGIN.Description = "Allows control of player metabolism stats and rates."
PLUGIN.Author = "Wulfspider"
PLUGIN.Version = V(1, 1, 1)
PLUGIN.ResourceId = 680
PLUGIN.HasConfig = true

local lastCalories = 1000
local lastHydration = 1000
local lastHealth = 1

function PLUGIN:Init()
    self:LoadDefaultConfig()
end

function PLUGIN:LoadDefaultConfig()
    self.Config.Settings = self.Config.Settings or {}
    -- Health
    self.Config.Settings.Health = self.Config.Settings.Health or {}
    self.Config.Settings.Health.MaxValue = tonumber(self.Config.Settings.Health.MaxValue) or tonumber(self.Config.Settings.Health.maxValue) or 100
    self.Config.Settings.Health.SpawnValue = self.Config.Settings.Health.SpawnValue or self.Config.Settings.Health.spawnValue or "default"
    self.Config.Settings.Health.GainRate = self.Config.Settings.Health.GainRate or self.Config.Settings.Health.gainRate or "default"
    -- Calories
    self.Config.Settings.Calories = self.Config.Settings.Calories or {}
    self.Config.Settings.Calories.MaxValue = tonumber(self.Config.Settings.Calories.MaxValue) or tonumber(self.Config.Settings.Calories.maxValue) or 1000
    self.Config.Settings.Calories.SpawnValue = self.Config.Settings.Calories.SpawnValue or self.Config.Settings.Calories.spawnValue or "default"
    self.Config.Settings.Calories.LossRate = self.Config.Settings.Calories.LossRate or self.Config.Settings.Calories.loseRate or "default"
    -- Hydration
    self.Config.Settings.Hydration = self.Config.Settings.Hydration or {}
    self.Config.Settings.Hydration.MaxValue = tonumber(self.Config.Settings.Hydration.MaxValue) or tonumber(self.Config.Settings.Hydration.maxValue) or 1000
    self.Config.Settings.Hydration.SpawnValue = self.Config.Settings.Hydration.SpawnValue or self.Config.Settings.Hydration.spawnValue or "default"
    self.Config.Settings.Hydration.LossRate = self.Config.Settings.Hydration.LossRate or self.Config.Settings.Hydration.loseRate or "default"
    -- Remove old
    self.Config.Settings.Health.maxValue = nil -- Removed in 0.1.1
    self.Config.Settings.Health.spawnValue = nil -- Removed in 0.1.1
    self.Config.Settings.Health.gainRate = nil -- Removed in 0.1.1
    self.Config.Settings.Calories.maxValue = nil -- Removed in 0.1.1
    self.Config.Settings.Calories.spawnValue = nil -- Removed in 0.1.1
    self.Config.Settings.Calories.loseRate = nil -- Removed in 0.1.1
    self.Config.Settings.Hydration.maxValue = nil -- Removed in 0.1.1
    self.Config.Settings.Hydration.spawnValue = nil -- Removed in 0.1.1
    self.Config.Settings.Hydration.loseRate = nil -- Removed in 0.1.1
    self:SaveConfig()
end

function PLUGIN:OnPlayerInit(player)
    self:SetMetabolismValues(player)
end

function PLUGIN:OnPlayerSpawn(player)
    self:SetMetabolismValues(player)
end

-- ----------------------------
-- Rust default rates
-- ----------------------------
-- healthgain = 0.03
-- caloriesloss = 0 - 0.05
-- hydrationloss = 0 - 0.025
-- ----------------------------
function PLUGIN:OnRunPlayerMetabolism(metabolism)
    local caloriesLossRate = self.Config.Settings.Calories.LossRate
    local hydrationLossRate = self.Config.Settings.Hydration.LossRate
    local healthGainRate = self.Config.Settings.Health.GainRate
    local heartRate = metabolism.heartrate.value
    if caloriesLossRate ~= "default" then
        if calorieLossRate == 0 or calorieLossRate == "0" then
            metabolism.calories.value = metabolism.calories.value
        else
            metabolism.calories.value = metabolism.calories.value - (tonumber(caloriesLossRate) + (heartRate / 10))
        end
    end
    if hydrationLossRate ~= "default" then
        if hydrationLossRate == 0 or hydrationLossRate == "0" then
            metabolism.hydration.value = metabolism.hydration.value
        else
            metabolism.hydration.value = metabolism.hydration.value - (tonumber(hydrationLossRate) + (heartRate / 10))
        end
    end
    if healthGainRate ~= "default" then
        if healthGainRate == 0 or healthGainRate == "0" then
            metabolism.health = metabolism.health
        else
            metabolism.health = metabolism.health + tonumber(healthGainRate) - 0.03
        end
    end
end

function PLUGIN:SetMetabolismValues(player)
    local maxHydration = tonumber(self.Config.Settings.Hydration.MaxValue)
    local maxCalories = tonumber(self.Config.Settings.Calories.MaxValue)
    local maxHealth = tonumber(self.Config.Settings.Health.MaxValue)
    local hydrationValue, caloriesValue, healthValue = false, false, false
    if self.Config.Settings.Hydration.SpawnValue ~= "default" then
        hydrationValue = tonumber(self.Config.Settings.Hydration.SpawnValue)
    end
    if self.Config.Settings.Calories.SpawnValue ~= "default" then
        caloriesValue = tonumber(self.Config.Settings.Calories.SpawnValue)
    end
    if self.Config.Settings.Health.SpawnValue ~= "default" then
        healthValue = tonumber(self.Config.Settings.Health.SpawnValue)
    end
    player.metabolism.calories.max = maxCalories
    player.health = maxHealth
    player.metabolism.hydration.max = maxHydration
    if healthValue then
        player.health = healthValue
    else
        player.health = maxHealth
    end
    if caloriesValue then
        player.metabolism.calories.value = caloriesValue
    end
    if hydrationValue then
        player.metabolism.hydration.value = hydrationValue
    end
end
