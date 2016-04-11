PLUGIN.Title = "Airdrop Arrow"
PLUGIN.Description = "Draws an arrow on airdrop"
PLUGIN.Version = V(1, 0, 1)
PLUGIN.Author = "Dezito"
PLUGIN.ResourceId = 1187

function PLUGIN:LoadDefaultConfig()
    self.Config.ArrowLength = tonumber(self.Config.ArrowLength) or 15
    self.Config.ArrowSize = tonumber(self.Config.ArrowSize) or 4
    self.Config.ArrowTime = tonumber(self.Config.ArrowTime) or 60

    self:SaveConfig()
end

function PLUGIN:OnEntitySpawned(Entity)
    if not Entity then return end
    if Entity:GetComponentInParent(global.SupplyDrop._type) then
        timer.Once(1, function() self:CheckSupplyLanded(Entity) end, self.Plugin)
    end
    if not Entity:GetComponentInParent(global.CargoPlane._type) then
        return
    end
end

function PLUGIN:CheckSupplyLanded(SupplyDrop)
    if SupplyDrop then
        local ParachuteField = global.SupplyDrop._type:GetField("parachute", rust.PrivateBindingFlag())

        if ParachuteField then
            local Parachute = ParachuteField:GetValue(SupplyDrop)

            if Parachute then
                timer.Once(1, function() self:CheckSupplyLanded(SupplyDrop) end, self.Plugin)
            else
                local StartPos = new(UnityEngine.Vector3._type, nil)
                StartPos.x = SupplyDrop.transform.position.x
                StartPos.y = SupplyDrop.transform.position.y + 5 + self.Config.ArrowLength
                StartPos.z = SupplyDrop.transform.position.z

                local EndPos = new(UnityEngine.Vector3._type, nil)
                EndPos.x = SupplyDrop.transform.position.x
                EndPos.y = SupplyDrop.transform.position.y + 5
                EndPos.z = SupplyDrop.transform.position.z

                local ArrowParams = util.TableToArray({ self.Config.ArrowTime, System.ConsoleColor.White, StartPos, EndPos, self.Config.ArrowSize })
                global.ConsoleSystem.Broadcast("ddraw.arrow", ArrowParams)
            end
        end
    end
end
