PLUGIN.Name = "Anti-Dupe"
PLUGIN.Title = "Anti-Dupe"
PLUGIN.Description = "Anti Item Stack Duping"
PLUGIN.Version = V(1, 0, 2)
PLUGIN.Author = "SPooCK"
PLUGIN.HasConfig = true

function PLUGIN:Init()	
	self.Dupe = {}
	self.DupeTimer = {}
end

function PLUGIN:OnItemAddedToContainer(container, item)
if (not container or not container.playerOwner) then return end
local player = container.playerOwner
local PlyID = rust.UserIDFromPlayer(player)
local name, amm = item.info.shortname, item.amount
if (amm < 2) then return end
if (self.DupeTimer[PlyID]) then self.DupeTimer[PlyID]:Destroy() self.DupeTimer[PlyID] = nil end
	self.Dupe[PlyID] = {} self.Dupe[PlyID].iname = {} self.Dupe[PlyID].iamm = {}
	self.Dupe[PlyID].iname = name self.Dupe[PlyID].iamm = amm
	self.DupeTimer[PlyID] = timer.Once(1.5, function() self.Dupe[PlyID] = nil end)
end

function PLUGIN:OnItemRemovedFromContainer(container, item)
if (not container or not container.playerOwner) then return end
local player = container.playerOwner
local PlyID = rust.UserIDFromPlayer(player)
local name, amm = item.info.shortname, item.amount
if (amm < 2) then return end
local ilist = container.itemList:GetEnumerator()
	while ilist:MoveNext() do
	local ilname = ilist.Current.info.shortname
	local ilamm1 = ilist.Current.amount local ilamm2 = ilist.Current.amount-1
		if (ilist.Current ~= item and self.Dupe[PlyID] and self.Dupe[PlyID].iname == ilname) then
			if (self.Dupe[PlyID].iamm == amm) then
				if (ilamm1 == amm or ilamm2 == amm) then
				item:Remove(0)
				rust.BroadcastChat("Anti-Dupe", player.displayName.. "â is trying to DUPEâª " ..item.info.displayname)
				break
				end
			end
		end
	end
end

function PLUGIN:OnPlayerSpawn(player)
local PlyID = rust.UserIDFromPlayer(player)
if (self.Dupe[PlyID]) then self.Dupe[PlyID] = nil end
if (self.DupeTimer[PlyID]) then self.DupeTimer[PlyID]:Destroy() self.DupeTimer[PlyID] = nil end
end