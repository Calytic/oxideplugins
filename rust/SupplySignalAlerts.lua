PLUGIN.Title       = "Supply Signal Alerts"
PLUGIN.Description = "Broadcasts a message when someone throws a supply signal."
PLUGIN.Author      = "LaserHydra"
PLUGIN.Version     = V(2, 0, 1)
PLUGIN.ResourceId  = 933

function PLUGIN:Init() self:LoadDefaultConfig() end

function PLUGIN:LoadDefaultConfig()
    self.Config.Message = self.Config.Message or "{player} has thrown a Supply Signal at {location}"
    self:SaveConfig()
end

function PLUGIN:OnWeaponThrown(player, entity)
    if entity.name:match("grenade.smoke.deployed") then
        timer.Once(2.8, function()
            local position = (entity:GetEstimatedWorldPosition())
            local pos = {}

            pos.x = math.floor(position.x)
            pos.y = math.floor(position.y)
            pos.z = math.floor(position.z)

            local message = string.gsub(self.Config.Message, "{player}", "<color=orange>" .. player.displayName .. "</color>")
            message = string.gsub(message, "{location}", "<color=orange>( X: " .. pos.x .. ", Y: " .. pos.y .. ", Z: " .. pos.z .. " )</color>")

            rust.BroadcastChat(message)
            print("[" .. self.Title .. "] " .. message)
        end, self.Plugin)
    end
end
