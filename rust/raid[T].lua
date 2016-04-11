PLUGIN.Name = "raid[T]"
PLUGIN.Title = "Raid[Temporary]"
PLUGIN.Description = "A temporary plugin which will make bases raidable."
PLUGIN.Version = V(1, 0, 6)
PLUGIN.Author = "SPooCK"
PLUGIN.HasConfig = true

function PLUGIN:OnPlayerAttack(player, hitinfo)
if (hitinfo and hitinfo.HitEntity and hitinfo.HitEntity:GetComponent("BuildingBlock")) then
local name = tostring(hitinfo.HitEntity.name)
local Grade = tonumber(hitinfo.HitEntity.blockDefinition.grades.Length)

if (self.Config.MetalFloor["Enabled"]) then
if (name:find("floor") and Grade == 3 and hitinfo.ProjectileID and hitinfo.ProjectileID == 0) then hitinfo.HitEntity:Heal(self.Config.MetalFloor["Melee"]) 
elseif (name:find("floor") and Grade == 3) then hitinfo.HitEntity:Heal(self.Config.MetalFloor["Range"]) end
end

	if (hitinfo.ProjectileID and hitinfo.ProjectileID == 0) then
	if (Grade == 1 and self.Config.Wood["Enabled"] or Grade == 2 and self.Config.Stone["Enabled"]) then
	local type = Rust.DamageType.Generic
	local add = 0 
		if (name:find("hinged")) then
		if (Grade == 1) then add = self.Config.Wood["DoorVactor"]
		elseif (Grade == 2) then add = self.Config.Stone["DoorVactor"] end
			hitinfo:AddDamage(type, add)
			hitinfo.HitMaterial = 0
		else
		if (Grade == 1) then add = self.Config.Wood["WallsVactor"]
		elseif (Grade == 2) then add = self.Config.Stone["WallsVactor"] end
			hitinfo:AddDamage(type, add)
			hitinfo.HitMaterial = 0
		end
	end
	end
end
end

function PLUGIN:LoadDefaultConfig()
self.Config.Wood = {}
self.Config.Wood["Enabled"] = true
self.Config.Wood["DoorVactor"] = 150
self.Config.Wood["WallsVactor"] = 200
self.Config.Stone = {}
self.Config.Stone["Enabled"] = true
self.Config.Stone["DoorVactor"] = 380
self.Config.Stone["WallsVactor"] = 400
self.Config.MetalFloor = {}
self.Config.MetalFloor["Enabled"] = true
self.Config.MetalFloor["Melee"] = 9
self.Config.MetalFloor["Range"] = 39
end