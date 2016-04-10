PLUGIN.Title = "Loot Protection"
PLUGIN.Version = V(0, 2, 0)
PLUGIN.Description = "Prevents players with permission from being looted by other players."
PLUGIN.Author = "Wulf / Luke Spragg"
PLUGIN.Url = "http://oxidemod.org/plugins/1150/"
PLUGIN.ResourceId = 1150

--[[ Do NOT edit the config here, instead edit LootProtection.json in oxide/config ! ]]

function PLUGIN:LoadDefaultConfig()
    self.Config.Messages = self.Config.Messages or {}
    self.Config.Messages.CantBeLooted = self.Config.Messages.CantBeLooted or "{player} can't be looted!"

    self:SaveConfig()
end

local function HasPermission(steamId, perm)
    if permission.UserHasPermission(steamId, perm) then return true end
    return false
end

function PLUGIN:Init()
    self:LoadDefaultConfig()
    permission.RegisterPermission("loot.dead", self.Plugin)
    permission.RegisterPermission("loot.protection", self.Plugin)
    permission.RegisterPermission("loot.sleepers", self.Plugin)
end

function PLUGIN:OnPlayerLoot(source, target)
    if target:GetComponent("BasePlayer") then
        local sourcePlayer = source:GetComponent("BasePlayer")
        local sourceId = rust.UserIDFromPlayer(sourcePlayer)
        local targetPlayer = target:GetComponent("BasePlayer")
        local targetId = rust.UserIDFromPlayer(targetPlayer)

        if targetPlayer:IsDead() and HasPermission(sourceId, "loot.dead") then return end

        if targetPlayer:IsSleeping() and HasPermission(sourceId, "loot.sleepers") then return end

        if HasPermission(targetId, "loot.protection") then
            timer.NextFrame(function() source:Clear() end)

            local message = self.Config.Messages.CantBeLooted:gsub("{player}", targetPlayer.displayName)
            rust.SendChatMessage(sourcePlayer, message)
        end
    end
end
