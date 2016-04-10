PLUGIN.Title = "Admin / Player"
PLUGIN.Description = "Easily switch between player and admin mod"
PLUGIN.Author = "Reneb"
PLUGIN.Version = V(2, 0, 1)

function PLUGIN:Init()
	self.Admin = datafile.GetDataTable( "admin" ) or {}
	
	self.Timer = {}
	
	command.AddChatCommand( "admin", self.Plugin, "cmdAdmin")
	self.AdminInv = {}

	permission.RegisterPermission("admin", self.Plugin);
end

local arr = util.TableToArray( { global["Inventory+Slot+Kind"].Armor, false, global["Inventory+Slot+KindFlags"].Armor }  )
util.ConvertAndSetOnArray( arr, 1, false, System.Boolean._type )
local armorpref = global["Inventory+Slot+Preference"].Define.methodarray[11]:Invoke( nil, arr )
local arrr = util.TableToArray( { global["Inventory+Slot+Kind"].Belt, false, global["Inventory+Slot+KindFlags"].Belt } )
util.ConvertAndSetOnArray( arr, 1, false, System.Boolean._type )
local beltpref = global["Inventory+Slot+Preference"].Define.methodarray[11]:Invoke( nil, arrr )
local arrrr = util.TableToArray( { global["Inventory+Slot+Kind"].Default, false, global["Inventory+Slot+KindFlags"].Belt } )
util.ConvertAndSetOnArray( arr, 1, false, System.Boolean._type )
local defaultpref = global["Inventory+Slot+Preference"].Define.methodarray[11]:Invoke( nil, arrrr )

function PLUGIN:LoadDefaultConfig() 
	self.Config.BagPack = {
		[1]={name="Arrow",amount=50},
	}
	self.Config.Armor = {
		[1]="Invisible Helmet",
		[2]="Invisible Vest",
		[3]="Invisible Pants",
		[4]="Invisible Boots"	
	}
	self.Config.FastBar = {
		[1]={name="Uber Hatchet",amount=1},
		[2]={name="Uber Hunting Bow",amount=1},
		[3]={name="Cooked Chicken Breast",amount=5},
		[4]={name="Large Medkit",amount=5},
		[5]={name="Stone Hatchet",amount=1}
	}
	self.Config.Version = "1.0"
end
function PLUGIN:OnPlayerChat(netuser,msg)
	if (msg:sub( 1, 1 ) ~= "/") then
		local userID = rust.UserIDFromPlayer(netuser)
		if(self.Admin[userID] and self.Admin[userID].hideName) then
			name = self.Admin[userID].AdminName
			global.ConsoleNetworker.Broadcast("chat.add "..name.." "..rust.QuoteSafe(msg).."")
			return true
		end
	end
end
function PLUGIN:cmdAdmin(netuser,cmd,args)
	if not self:isAdmin(netuser) then
		return
	end
	self:AdminCMD(netuser,args)
end
function PLUGIN:SetGodMode(netuser,to)
	local char = netuser.playerClient.rootControllable.rootCharacter
	if(not char) then return end
	if(tostring(type(char.takeDamage)) ~= "userdata") then return end
	char.takeDamage:SetGodMode(to)
end
function PLUGIN:AdminCMD(netuser,args)
	local userID = rust.UserIDFromPlayer(netuser)
	if(not self.Admin[userID]) then 
	self.Admin[userID] = {} 
		self.Admin[userID].isAdmin = false
		self.Admin[userID].hideName = false
		self.Admin[userID].AdminName = "SERVER CONSOLE"
	end
	if(args.Length > 0) then
		if(args[0] == "hide") then
			if(not self.Admin[userID].hideName) then
				self.Admin[userID].hideName = true
				rust.SendChatMessage(netuser, "You will now have a hidden name (" .. self.Admin[userID].AdminName .. ")!" )
			else
				self.Admin[userID].hideName = false
				rust.SendChatMessage(netuser, "You will no longer have a hidden name!" )
			end
			self:Save()
			return
		elseif(args[0] == "name") then
			local adminname = ""
			if(not args[1]) then 
				adminname = "SERVER CONSOLE"
			else
				for i=1, args.Length-1, 1 do
					adminname = adminname .. args[i] .. " "
				end
			end
			self.Admin[userID].AdminName = adminname
			rust.SendChatMessage(netuser, "Your new admin name will be " .. adminname )
			self:Save()
			return
		end
	end
	if(not self.Admin[userID].isAdmin) then
		self.Admin[userID].isAdmin = true
		self:SaveInventory(netuser,userID)
		self:ClearInventory(netuser)
		self:AdminGear(netuser)
		self:ClearInjury(netuser)
		self:SetGodMode(netuser,true)
		rust.Notice( netuser, "You've transformed into an admin", 5.0 )
		
	else
		self.Admin[userID].isAdmin = false
		self:ClearInventory(netuser)
		self:RestoreInventory(netuser,userID)
		self:SetGodMode(netuser,false)
		rust.Notice( netuser, "You are a simple mortel again", 5.0 )
	end
end
function PLUGIN:ClearInjury(netuser)
	if(not netuser) then return end
	local userID = rust.UserIDFromPlayer(netuser)
    local playerClient = netuser.playerClient
    if(not playerClient) then
        return
    end 
	
    local controllable = playerClient.controllable
    if(not controllable) then
        return
    end
	if(self.Timer[netuser]) then self.Timer[netuser]:Destroy() end
	local fallDamage = controllable:GetComponent("FallDamage")
	if(fallDamage:GetLegInjury() > 0) then
		fallDamage:ClearInjury()
	end
	if(self.Admin[userID] and self.Admin[userID].isAdmin) then
		self.Timer[netuser] = timer.Once( 2, function() self:ClearInjury(netuser) end)
	end
end
function PLUGIN:OnSpawnPlayer( playerclient, usecamp, avatar )
	local netuser = playerclient.netUser
	if(not netuser) then return end
	local userID = rust.UserIDFromPlayer(netuser)
	if(self.Timer[netuser]) then self.Timer[netuser]:Destroy() end

	if(self.Admin[userID] and self.Admin[userID].isAdmin) then
		timer.Once(0.1, function()
			self:ClearInventory(playerclient.netUser)
			self:AdminGear(playerclient.netUser)
			self:SetGodMode(playerclient.netUser,true)
			self:ClearInjury(playerclient.netUser)
		end)
	else
		timer.NextFrame( function()
			self:SetGodMode(playerclient.netUser,false)
		end)
	end
end
function PLUGIN:RestoreInventory(netuser,userID)
	local inv = netuser.playerClient.rootControllable.idMain:GetComponent( "Inventory" )
	if(not inv or tostring(type(inv)) == "string" ) then 
		return
	end
	local pref
	local belt = 29
	local bag = -1
	for i,item in pairs(self.AdminInv[userID]) do
		pref = nil
		if(self.AdminInv[userID][i]) then
			if(item.slot >= 0 and item.slot <= 29) then
				pref = defaultpref
				bag = bag + 1
			elseif(item.slot >= 30 and item.slot <= 35) then
				pref = beltpref
				belt = belt + 1
			elseif(item.slot >= 36 and item.slot <= 39) then
				pref = armorpref
			end
		end
		
		if(pref ~= nil) then
			
			local itemdata = global.DatablockDictionary.GetByName( item.name )
			inv:AddItemAmount( itemdata, 1, pref )
			local _b, invitem = inv:GetItem( GetNewSlotFromOld(item.slot,bag,belt) )
			if(invitem and invitem ~= nil) then
				if(item.uses) then invitem:SetUses( item.uses ) end
				if(item.condition) then invitem:SetCondition( item.condition ) end
				if(item.maxcondition) then invitem:SetMaxCondition( item.maxcondition ) end
				if(item.totalModSlots) then invitem:SetTotalModSlotCount( tonumber(item.totalModSlots) ) end
				if item.ModList and tonumber( #item.ModList ) then
					for key, value in pairs( item.ModList ) do
						invitem:AddMod( value )
					end
				end
			end
		end
	end
end
function GetNewSlotFromOld(slot,bag,belt)
	if(slot >= 0 and slot <= 29) then
		return bag
	elseif(slot >= 30 and slot <= 35) then
		return belt
	else
		return slot
	end
end
function PLUGIN:AdminGear(netuser)
	local inv = netuser.playerClient.rootControllable.idMain:GetComponent( "Inventory" )
	if(not inv or tostring(type(inv)) == "string" ) then 
		return
	end
	local pref = armorpref
	for i=1, #self.Config.Armor do
		if(i > 4) then
			error("More than 4 armor parts, WTF!")
			break
		end
		inv:AddItemAmount( global.DatablockDictionary.GetByName( self.Config.Armor[i] ) , 1, pref )
	end
	pref = beltpref
	for i=1, #self.Config.FastBar do
		if(i > 6) then
			error("More than 6 items in the Bar, WTF!")
			break
		end
		inv:AddItemAmount( global.DatablockDictionary.GetByName( self.Config.FastBar[i].name ) , self.Config.FastBar[i].amount , pref )
	end

	pref = defaultpref
	for i=1, #self.Config.BagPack do
		inv:AddItemAmount( global.DatablockDictionary.GetByName( self.Config.BagPack[i].name ) , self.Config.BagPack[i].amount, pref )
	end
	
end
function PLUGIN:ClearInventory(netuser)
	local inv = netuser.playerClient.rootControllable.idMain:GetComponent( "Inventory" )
	if(not inv or tostring(type(inv)) == "string" ) then 
		return
	end
	inv:Clear()
end
function PLUGIN:Save()
	datafile.SaveDataTable("admin")
end

function PLUGIN:SaveInventory(netuser,userID)
	local inv = netuser.playerClient.rootControllable.idMain:GetComponent( "Inventory" )
	self.AdminInv[userID] = {}
	local iterator = inv.occupiedIterator
	local currentitem = 1
	while iterator:Next() do
		local item = iterator.item
		if item and item.datablock and item.datablock.name then
			local itemtosave = self:getItemSpecifics( item )
			self.AdminInv[userID][currentitem] = {}
			self.AdminInv[userID][currentitem] = itemtosave
			currentitem = currentitem + 1
		end
	end
end
function PLUGIN:getItemSpecifics( item )
	local tmp = {}
	tmp.name = item.datablock.name
	tmp.slot = item.slot
	if tonumber( item.maxUses ) then tmp.maxUses = item.maxUses end
	if tonumber( item.uses ) then tmp.uses = item.uses end
	if tonumber( item.condition ) then tmp.condition = item.condition end
	if tonumber( item.maxcondition ) then tmp.maxcondition = item.maxcondition end
	if tonumber( item.totalModSlots ) then tmp.totalModSlots = item.totalModSlots end
	if tonumber( item.usedModSlots ) and item.usedModSlots > 0 then
		tmp.usedModSlots = item.usedModSlots
		tmp.ModList = self:getItemMods( item )
	end
	return tmp
end

function PLUGIN:getItemMods( item )
	local itemModList = {}
	local itemMods = item.itemMods
	local _count = itemMods.Length - 1
	for _i = 0, _count do
		local _itemMod = itemMods[ _i ]
		if _itemMod then
			table.insert( itemModList, _itemMod )
		end
	end
	return itemModList
end

function PLUGIN:isAdmin(netuser)
	if(netuser:CanAdmin()) then return true end
	return permission.UserHasPermission(rust.UserIDFromPlayer(netuser), "admin")
end