PLUGIN.Name = "r-Lanterns"
PLUGIN.Title = "r-Lanterns"
PLUGIN.Version = V(1, 0, 3)
PLUGIN.Description = "Manage Lanterns On/Off by Building Privilege zones"
PLUGIN.Author = "Reneb & Mughisi"
PLUGIN.HasConfig = true


function PLUGIN:Init()
	------------------------------------------------------------------------
	-- Debug Config
	------------------------------------------------------------------------
	--self.Config = {}
	--self:LoadDefaultConfig()
	------------------------------------------------------------------------
	
	PlayerLanternZones = {} 
	LaternsZones = {}
	
	command.AddChatCommand( "lanterns_on", self.Plugin, "cmdLanternsOn" )
	command.AddChatCommand( "lanterns_off", self.Plugin, "cmdLanternsOff" )
end

-- -----------------------------------------------------------------------------
-- PLUGIN:LoadDefaultConfig()
-- auto-creation of the config file if: PLUGIN.HasConfig = true
-- -----------------------------------------------------------------------------
function PLUGIN:LoadDefaultConfig()
	self.Config.Settings = {}
	--self.Config.nightTime = 18
	--self.Config.dayTime = 6
	self.Config.Settings.authLevel = 1
	self.Config.Settings.chatName = "Auto-Light"
end

-- -----------------------------------------------------------------------------
-- add_ZoneLight( triggerbase , lantern )
-- add a lantern inside a zone
-- -----------------------------------------------------------------------------
local function add_ZoneLight(triggerbase,lantern)
	if(not LaternsZones[lantern]) then LaternsZones[lantern] = {} end
	LaternsZones[lantern][triggerbase] = true
end

-- -----------------------------------------------------------------------------
-- hasPlayer(triggerbase)
-- check if a zone has a connected player or not
-- -----------------------------------------------------------------------------

local function hasPlayer(triggerbase)
	if(not PlayerLanternZones[triggerbase]) then PlayerLanternZones[triggerbase] = {} end
	for k,v in pairs(PlayerLanternZones[triggerbase]) do
		return true
	end
	return false
end

-- -----------------------------------------------------------------------------
-- countPlayers(triggerbase)
-- count the current online players inside a zone
-- -----------------------------------------------------------------------------

local function countPlayers(triggerbase)
	if(not PlayerLanternZones[triggerbase]) then PlayerLanternZones[triggerbase] = {} end
	count = 0
	for k,v in pairs(PlayerLanternZones[triggerbase]) do
		count = count + 1
	end
	return count
end

-- -----------------------------------------------------------------------------
-- start_sphereCastLights(triggerbase,pos,radius)
-- Start all lanterns in a zone
-- -----------------------------------------------------------------------------
local function start_sphereCastLights(triggerbase,pos,radius)
	arr = util.TableToArray( { pos , radius } )
	util.ConvertAndSetOnArray(arr, 1, radius, System.Single._type)
	hits = UnityEngine.Physics.OverlapSphere["methodarray"][1]:Invoke(nil,arr)
	local it = hits:GetEnumerator()
	while (it:MoveNext()) do
		if tostring(it.Current):find("lantern") then
			it.Current:GetComponent(global.BaseEntity._type):SetFlag(global["BaseEntity+Flags"].On, true)
			add_ZoneLight(triggerbase,it.Current:GetComponent(global.BaseEntity._type))
		end
	end
end

-- -----------------------------------------------------------------------------
-- canSwitchOff(lantern)
-- Check if a lantern can be turned off, check if one of the zone where he is in has a player or not.
-- -----------------------------------------------------------------------------
local function canSwitchOff(lantern)
	if(not LaternsZones[lantern]) then return true end
	for triggerbase,abool in pairs(LaternsZones[lantern]) do
		if(hasPlayer(triggerbase)) then
			return false
		end
	end
	return true
end

-- -----------------------------------------------------------------------------
-- stop_sphereCastLights(triggerbase,pos,radius)
-- Stops all lanterns in a zone, uses canSwitchOff(lantern)
-- -----------------------------------------------------------------------------
local function stop_sphereCastLights(triggerbase,pos,radius)
	arr = util.TableToArray( { pos , radius } )
	util.ConvertAndSetOnArray(arr, 1, radius, System.Single._type)
	hits = UnityEngine.Physics.OverlapSphere["methodarray"][1]:Invoke(nil,arr)
	local it = hits:GetEnumerator()
	while (it:MoveNext()) do
		if tostring(it.Current):find("lantern") then
			if canSwitchOff(it.Current:GetComponent(global.BaseEntity._type)) then
				it.Current:GetComponent(global.BaseEntity._type):SetFlag(global["BaseEntity+Flags"].On, false)
			end
		end
	end
end

-- -----------------------------------------------------------------------------
-- startLights(triggerbase)
-- calls: start_sphereCastLights(triggerbase,pos,radius)
-- -----------------------------------------------------------------------------
local function startLights(triggerbase)
	start_sphereCastLights(triggerbase,triggerbase:GetComponent(UnityEngine.Transform._type).transform.position,triggerbase:GetComponent(UnityEngine.SphereCollider._type).radius)
end

-- -----------------------------------------------------------------------------
-- stopLights(triggerbase)
-- calls: stop_sphereCastLights(triggerbase,pos,radius)
-- -----------------------------------------------------------------------------
local function stopLights(triggerbase)
	stop_sphereCastLights(triggerbase,triggerbase:GetComponent(UnityEngine.Transform._type).transform.position,triggerbase:GetComponent(UnityEngine.SphereCollider._type).radius)
end

-- -----------------------------------------------------------------------------
-- add_PlayerZone(baseplayer,triggerbase)
-- add a player in a zone
-- -----------------------------------------------------------------------------
local function add_PlayerZone(baseplayer,triggerbase)
	if(not PlayerLanternZones[triggerbase]) then PlayerLanternZones[triggerbase] = {} end
	PlayerLanternZones[triggerbase][baseplayer] = true
end

-- -----------------------------------------------------------------------------
-- remove_PlayerZone(baseplayer,triggerbase)
-- remove a player from a zone
-- -----------------------------------------------------------------------------
local function remove_PlayerZone(baseplayer,triggerbase)
	if(PlayerLanternZones[triggerbase] and PlayerLanternZones[triggerbase][baseplayer]) then
		TempZones = {}
		for k,v in pairs(PlayerLanternZones[triggerbase]) do
			if(k ~= baseplayer) then
				TempZones[k] = true
			end
		end
		PlayerLanternZones[triggerbase] = TempZones
	end
end

-- -----------------------------------------------------------------------------
-- update_playersZones(player)
-- check if the player is in one or multiple zones and adds him inside them, lighting up lights in those zones
-- -----------------------------------------------------------------------------
local function update_playersZones(player)
	allBuildingZones = UnityEngine.Object.FindObjectsOfTypeAll(global.BuildPrivilegeTrigger._type)
	for i=0, allBuildingZones.Length-1 do
		if(allBuildingZones[i].entityContents.Count > 0) then
			if(allBuildingZones[i].entityContents:Contains(player:GetComponent("BaseEntity"))) then
				triggerbase = allBuildingZones[i]:GetComponent(global.TriggerBase._type)
				if(not hasPlayer(triggerbase)) then
					startLights(triggerbase)
				end
				add_PlayerZone(player,triggerbase)
			end
		end
	end
end

-- -----------------------------------------------------------------------------
-- remove_Player(player)
-- remove a player from all zones, shutting down lights in zones where he was the last one inside
-- -----------------------------------------------------------------------------
local function remove_Player(player)
	for triggerbase,players in pairs(PlayerLanternZones) do
		if(PlayerLanternZones[triggerbase][player]) then
			remove_PlayerZone(player,triggerbase)
			if(countPlayers(triggerbase) <= 1) then
				stopLights(triggerbase)
			end
		end
	end
end

-- -----------------------------------------------------------------------------
-- switchAllLanterns(light)
-- switch all server lanterns to on (true) or off (false)
-- -----------------------------------------------------------------------------
local function switchAllLanterns(light)
	allWorldItems = UnityEngine.Object.FindObjectsOfTypeAll(global.BaseEntity._type)
	for i=0, allWorldItems.Length-1 do
		if tostring(allWorldItems[i]):find("lantern") then
			allWorldItems[i]:SetFlag(global["BaseEntity+Flags"].On, light)
		end
	end
end

-- -----------------------------------------------------------------------------
-- PLUGIN:cmd*(player, cmd, args)
-- commands
-- -----------------------------------------------------------------------------
function PLUGIN:cmdLanternsOff(player, cmd, args)
	if(player:GetComponent("BaseNetworkable").net.connection.authLevel < self.Config.Settings.authLevel) then
		rust.SendChatMessage(player, "You are not allowed to use this command")
		return
	end
	switchAllLanterns(false)
	rust.SendChatMessage(player, "Lanterns were all switched off")
end

function PLUGIN:cmdLanternsOn(player, cmd, args)
	if(player:GetComponent("BaseNetworkable").net.connection.authLevel < self.Config.Settings.authLevel) then
		rust.SendChatMessage(player, "You are not allowed to use this command")
		return
	end
	switchAllLanterns(true)
	rust.SendChatMessage(player, "Lanterns were all switched on")
end

-- -----------------------------------------------------------------------------
-- HOOK
-- PLUGIN:OnEntityEnter(triggerbase,entity)
-- called when an entity (player or animal) enters a zone
-- -----------------------------------------------------------------------------

function PLUGIN:OnEntityEnter(triggerbase,entity)
	if(triggerbase:GetComponent(global.BuildPrivilegeTrigger._type)) then
		if(entity:GetComponentInParent(global.BasePlayer._type) and entity:GetComponentInParent(global.BasePlayer._type):IsConnected()) then
			if(not hasPlayer(triggerbase)) then
				startLights(triggerbase)
			end
			add_PlayerZone(entity:GetComponentInParent(global.BasePlayer._type),triggerbase)
		end
	end
end

-- -----------------------------------------------------------------------------
-- HOOK
-- PLUGIN:OnEntityLeave(triggerbase,entity)
-- called when an entity (player or animal) leaves a zone
-- -----------------------------------------------------------------------------
function PLUGIN:OnEntityLeave(triggerbase,entity)
	if(triggerbase:GetComponent(global.BuildPrivilegeTrigger._type)) then
		if(entity:GetComponentInParent(global.BasePlayer._type)) then
			remove_PlayerZone(entity:GetComponentInParent(global.BasePlayer._type),triggerbase)
			if(countPlayers(triggerbase) <= 1) then
				stopLights(triggerbase)
			end
		end
	end
end

-- -----------------------------------------------------------------------------
-- HOOK
-- PLUGIN:OnPlayerDisconnected(player,connection)
-- called when a player disconnects
-- -----------------------------------------------------------------------------
function PLUGIN:OnPlayerDisconnected(player,connection)
	remove_Player(player)
end

-- -----------------------------------------------------------------------------
-- HOOK
-- PLUGIN:OnPlayerInit(player,connection)
-- called when a player connects
-- -----------------------------------------------------------------------------
function PLUGIN:OnPlayerInit( player )
	update_playersZones(player)
end

