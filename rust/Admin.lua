PLUGIN.Title       = "Admin"
PLUGIN.Description = "Admin"
PLUGIN.Version     = V( 1, 0, 2 )
PLUGIN.HasConfig   = false
PLUGIN.Author      = "mvrb"
PLUGIN.ResourceId   = 1378

StatusData = {}
MetabolismData = {}
LoadoutData = {}
MasterData = {}
InventoryData = {}

function PLUGIN:Init()
	
	-- Chat Commands
	command.AddChatCommand( "admin", self.Object, "cmdAdmin" )
	
	--self:LoadDefaultConfig()
	self:RegisterPermissions()
	
	-- Data
	
	StatusData = datafile.GetDataTable( "UltimateAdmin/Status" )
	StatusData = StatusData or {}
	
	MetabolismData = datafile.GetDataTable( "UltimateAdmin/Metabolism" )
	MetabolismData = MetabolismData or {}
	
	LoadoutData = datafile.GetDataTable( "UltimateAdmin/Loadout" )
	LoadoutData = LoadoutData or {}
	
	MasterData = datafile.GetDataTable( "UltimateAdmin/Master" )	
	MasterData = MasterData or {}
	
	InventoryData = datafile.GetDataTable( "UltimateAdmin/Inventory" )	
	InventoryData = InventoryData or {}
	
end

function PLUGIN:LoadDefaultConfig()

	self.Config.Settings 										= self.Config.Settings 											or {}
    self.Config.Settings.RequiredAuthLevel 						= self.Config.Settings.RequiredAuthLevel 						or 2
    self.Config.Settings.UseAuthLevelPermission 				= self.Config.Settings.UseAuthLevelPermission 					or true
    self.Config.Settings.AdminsCanOpenAllDoors 					= self.Config.Settings.AdminsCanOpenAllDoors 					or true
    self.Config.Settings.AdminsCanUseGodMode 					= self.Config.Settings.AdminsCanUseGodMode 						or true
    self.Config.Settings.DisableAdmin_vs_PlayerDamageIfGodmode 	= self.Config.Settings.DisableAdmin_vs_PlayerDamageIfGodmode 	or true
    self.Config.Settings.DisableAdmin_vs_EntityDamageIfGodmode 	= self.Config.Settings.DisableAdmin_vs_EntityDamageIfGodmode 	or true
    self.Config.Settings.NotifyAdminWhenDamaged 				= self.Config.Settings.NotifyAdminWhenDamaged 					or true
    self.Config.Settings.NotifyPlayerWhenAttacking 				= self.Config.Settings.NotifyPlayerWhenAttacking 				or false
    self.Config.Settings.AdminsNoDurability 					= self.Config.Settings.AdminsNoDurability 						or true
    self.Config.Settings.AdminsBypassBuildingBlocked 			= self.Config.Settings.AdminsBypassBuildingBlocked 				or true
						
    self.Config.Permissions		 								= self.Config.Permissions 										or {}
    self.Config.Permissions.CanUseAdmin		 					= self.Config.Permissions.CanUseAdmin 							or "admin.use"
    self.Config.Permissions.Godmode		 						= self.Config.Permissions.Godmode 								or "admin.god"
    self.Config.Permissions.CanOpenAllDoors						= self.Config.Permissions.CanOpenAllDoors 						or "admin.door"
    self.Config.Permissions.CanUseCustomLoadout					= self.Config.Permissions.CanUseCustomLoadout 					or "admin.loadout"
    self.Config.Permissions.CanSaveMaster						= self.Config.Permissions.CanSaveMaster 						or "admin.master"
    self.Config.Permissions.LockedInventory						= self.Config.Permissions.LockedInventory 						or "admin.lock"
    self.Config.Permissions.BypassBuildingBlocked				= self.Config.Permissions.BypassBuildingBlocked 				or "admin.bypass"
    self.Config.Permissions.CanKickPlayers						= self.Config.Permissions.CanKickPlayers 						or "admin.kick"
    self.Config.Permissions.NoDurability						= self.Config.Permissions.NoDurability 							or "admin.durability"
						
    self.Config.Messages 										= self.Config.Messages 											or {}
    self.Config.Messages.InventorySaved 						= self.Config.Messages.InventorySaved 							or "<color=#99CC32>You have saved your current inventory.</color>"
    self.Config.Messages.AdminEnabled 							= self.Config.Messages.AdminEnabled 							or "<color=#99CC32>You have enabled Admin Mode.</color>"
    self.Config.Messages.AdminDisabled 							= self.Config.Messages.AdminDisabled 							or "<color=#FF3D0D>You have disabled Admin Mode.</color>"
    self.Config.Messages.NoMasterSaved 							= self.Config.Messages.NoMasterSaved 							or "<color=#FF3D0D>No master inventory found! Ask the owner of the server to save a Master inventory.</color>"
    self.Config.Messages.NoPermission 							= self.Config.Messages.NoPermission 							or "<color=#FF3D0D>You do not have permission to use this command.</color>"
    self.Config.Messages.CantLootAdmin 							= self.Config.Messages.CantLootAdmin 							or "<color=#FF3D0D>You are not allowed to open an admin's inventory!.</color>"
    self.Config.Messages.TriedToDamage 							= self.Config.Messages.TriedToDamage 							or " tried to damage you!"
    self.Config.Messages.CantDamageAdmin 						= self.Config.Messages.CantDamageAdmin 							or "<color=#FF3D0D>This player is in admin mode and can't be damaged!</color>"
	
    self:SaveConfig()
	
end

function PLUGIN:RegisterPermissions()

	permissions = 
	{
		self.Config.Permissions.CanUseAdmin,
		self.Config.Permissions.Godmode,
		self.Config.Permissions.CanOpenAllDoors,
		self.Config.Permissions.CanUseCustomLoadout,
		self.Config.Permissions.CanSaveMaster,
		self.Config.Permissions.LockedInventory,
		self.Config.Permissions.BypassBuildingBlocked,
		self.Config.Permissions.CanKickPlayers,
		self.Config.Permissions.NoDurability
	}
	for i=1,#permissions do
		if not permission.PermissionExists( permissions[i] ) then
			permission.RegisterPermission( permissions[i], self.Object )
		end
	end
	
end

function PLUGIN:HasPerm( player, perm )

	local steamID = rust.UserIDFromPlayer( player )
	
	if self.Config.Settings.UseAuthLevelPermission and perm ~= self.Config.Permissions.CanSaveMaster then
		if not player then return end
		if not player.net then return end
		if not player.net.connection then return end
		if player.net.connection.authLevel >= self.Config.Settings.RequiredAuthLevel then return true end
	else
		if permission.UserHasPermission( steamID, perm ) then return true end
	end
	
	return false
	
end


function PLUGIN:CanLootPlayer( inv, player )
	
	local targetID = rust.UserIDFromPlayer( inv )
	if not StatusData[targetID] then return end	
	StatusData[targetID]["status"] = StatusData[targetID]["status"] or false
	
	if self:HasPerm( inv, self.Config.Permissions.CanSaveMaster ) and StatusData[targetID] then
		rust.SendChatMessage( player, self.Config.Messages.LockedInventory )
		return false
	end
	
end

function PLUGIN:OnEntityEnter( trigger, entity )

	if not self.Config.Settings.AdminsBypassBuildingBlocked then return end

	if entity:ToPlayer() then
        local player = entity:ToPlayer()
		if not self:HasPerm( player, self.Config.Permissions.BypassBuildingBlocked ) then return end
		if trigger:GetType() == global.BuildPrivilegeTrigger._type then		
			timer.Once(0.1, function() player:SetPlayerFlag( global.BasePlayer.PlayerFlags.HasBuildingPrivilege, true ) end, self.Plugin)
            return			
		end		
	end

end

function PLUGIN:OnLoseCondition( item, amount )
    
	if not self.Config.Settings.AdminsNoDurability then return end
	local player = item:GetOwnerPlayer()
	if not player then return end
	if not self:HasPerm( player, "admin.durability" ) then return end
	item.condition = item.maxCondition
	
end


function PLUGIN:CanUseDoor( player, door )

	if not self.Config.Settings.AdminsCanOpenAllDoors then return end
	
	local steamID = rust.UserIDFromPlayer( player )	
	if not StatusData[steamID] then return end	
	StatusData[steamID]["status"] = StatusData[steamID]["status"] or false
	
	if StatusData[steamID]["status"] == true and self:HasPerm( player, "door" ) then 
		door:SetFlag( global["BaseEntity+Flags"].Locked, false )
		timer.Once( 0.1, function() door:SetFlag(global["BaseEntity+Flags"].Locked, true ) end )
	end
	
end

function PLUGIN:OnEntityTakeDamage( entity, info )
	
	if self.Config.Settings.AdminsCanUseGodMode == false then return end	
	
	local nullify = false
		
	if entity:ToPlayer() then
	
		local victim = entity:ToPlayer()
		if victim then
			local victimID = rust.UserIDFromPlayer( victim )		
			if StatusData[victimID] then
				if StatusData[victimID]["status"] then					
					if StatusData[victimID]["status"] == true and self:HasPerm( victim, self.Config.Permissions.Godmode ) then
						self:Heal( victim )
						nullify = true
						if info then
							if info.Initiator then
								if info.Initiator:ToPlayer() then
									local attacker = info.Initiator:ToPlayer()
									if attacker then
										if self.Config.Settings.NotifyAdminWhenDamaged == true then
											rust.SendChatMessage( victim, attacker.displayName .. self.Config.Messages.TriedToDamage )
										end
										if self.Config.Settings.NotifyPlayerWhenAttacking == true then
											rust.SendChatMessage( attacker, self.Config.Messages.CantDamageAdmin )
										end
									end
								end
							end
						end
					end
				end
			end
		end
		
		if info then
			if info.Initiator then
				if info.Initiator:ToPlayer() ~= nil then
					local attacker = info.Initiator:ToPlayer()
					if attacker then
						local attackerID = rust.UserIDFromPlayer( attacker )	
						if StatusData[attackerID] then
							if StatusData[attackerID]["status"] then	
								if StatusData[attackerID]["status"] == true then nullify = true	end
							end
						end
					end			
				end
			end
		end
		
	else
		if info then
			if info.Initiator then
				if info.Initiator:ToPlayer() ~= nil then
					local attacker = info.Initiator:ToPlayer()
					if attacker then
						attackerID = rust.UserIDFromPlayer( attacker )
						if StatusData[attackerID] then
							if StatusData[attackerID]["status"] then
								if StatusData[attackerID]["status"] == true then
									if self.Config.Settings.DisableAdmin_vs_EntityDamageIfGodmode then nullify = true end
								end
							end
						end	
					end
				end
			end
		end
	end
	
	if nullify then return true end
	
end

function PLUGIN:cmdKick( player, cmd, args )

	if self:HasPerm( player, self.Config.Permissions.CanKickPlayers ) then
		
	else
		rust.SendChatMessage( player, self.Config.Messages.NoPermission )
	end

end

function PLUGIN:cmdAdmin( player, cmd, args )

	if not self:HasPerm( player, self.Config.Permissions.CanUseAdmin ) then
		rust.SendChatMessage( player, self.Config.Messages.NoPermission )
		return
	end
	
	local steamID = rust.UserIDFromPlayer( player )
	
	if args.Length == 0 then	
		self:ToggleAdmin( player, steamID )		
	elseif args.Length == 1 then	
		local largs0 = string.lower( args[0] )		
		if largs0 == "save" then		
			if self:HasPerm( player, self.Config.Permissions.CanUseCustomLoadout ) then
				self:SaveSet( player, "loadout" )
				rust.SendChatMessage( player, self.Config.Messages.InventorySaved )
			else
				rust.SendChatMessage( player, self.Config.Messages.NoPermission )
			end			
		elseif largs0 == "master" then		
			if self:HasPerm( player, largs0 ) then
				self:SaveSet( player, "master" )
				rust.SendChatMessage( player, "<color=#99CC32>You have successfully updated the Master set.</color>" )
			else
				rust.SendChatMessage( player, self.Config.Messages.NoPermission )
			end			
		else		
			if not self:HasPerm( player, self.Config.Permissions.CanSaveMaster ) then
				rust.SendChatMessage( player, "<color=#FF3D0D>Wrong option! Use /admin save</color>" )
				return
			else
				rust.SendChatMessage( player, "<color=#FF3D0D>Wrong option! Use /admin save or /admin master</color>" )
				return
			end			
		end		
	elseif args.Length == 2 then	
		local largs0 = string.lower( args[0] )
		local largs1 = string.lower( args[1] )		
		if largs0 == "set" then		
			if largs1 == "master" then			
				StatusData[steamID]					= StatusData[steamID] 					or {}
				StatusData[steamID]["preferredSet"]	= StatusData[steamID]["preferredSet"]	or "master"
				StatusData[steamID]["preferredSet"]	= "master"				
				StatusData[steamID]["playerName"]	= player.displayName			
				rust.SendChatMessage( player, "<color=#99CC32>You have updated your preferred set to the master set.</color>" )				
			elseif largs1 == "custom" then			
				if self:HasPerm( player, self.Config.Permissions.CanUseCustomLoadout ) then				
					StatusData[steamID]					= StatusData[steamID] 					or {}
					StatusData[steamID]["preferredSet"]	= StatusData[steamID]["preferredSet"]	or "loadout"
					StatusData[steamID]["preferredSet"] = "loadout"
					StatusData[steamID]["playerName"]	= player.displayName
					rust.SendChatMessage( player, "<color=#99CC32>You have updated your preferred set to your own custom set.</color>" )					
				else
					rust.SendChatMessage( player, self.Config.Messages.NoPermission )
				end				
			else
				rust.SendChatMessage( player, "<color=#FF3D0D>Wrong option! Use /admin set custom or /admin set master</color>" )
				return
			end			
		end		
	end
	
end

function PLUGIN:ToggleAdmin( player, steamID )
	
	MasterData							= MasterData 							or {}
	StatusData[steamID]					= StatusData[steamID]					or {}
	StatusData[steamID]["status"]		= StatusData[steamID]["status"] 		or false
	StatusData[steamID]["preferredSet"]	= StatusData[steamID]["preferredSet"]	or "master"
	StatusData[steamID]["playerName"]	= player.displayName
	
	if StatusData[steamID]["status"] == false then		
		
		 if StatusData[steamID]["preferredSet"] == "loadout" then
			if LoadoutData[steamID] then 
				StatusData[steamID]["status"] = true
				self:SaveSet( player, "inventory" )	
				self:RestoreSet( player, "loadout" )
				--self:SaveMetabolism( player )
				self:Heal( player )
				rust.SendChatMessage( player, self.Config.Messages.AdminEnabled )				
			else
				rust.SendChatMessage( player, 
					"<color=#FF3D0D>No saved inventory found!</color> \n" ..
					"<color=#FF3D0D>Spawn the items you wish to save and save it by typing \n</color><color=#59ff4a>/admin save</color>"			
				)
				return
			end
		 else
			if MasterData then
				StatusData[steamID]["status"] = true
				self:SaveSet( player, "inventory" )	
				self:RestoreSet( player, "master" )
				--self:SaveMetabolism( player )
				self:Heal( player )
				rust.SendChatMessage( player, self.Config.Messages.AdminEnabled )				
			else
				if self:HasPerm( player, self.Config.Permissions.CanSaveMaster ) then
					rust.SendChatMessage( player, 
						"<color=#FF3D0D>No master inventory found!</color> \n" ..
						"<color=#FF3D0D>Spawn the items you wish to save and save it by typing \n</color><color=#59ff4a>/admin master</color>"			
					)
				else
					rust.SendChatMessage( player, self.Config.Messages.NoMasterSaved )
				end
				return
			end
		 end
			
	else
		StatusData[steamID]["status"] = false
		self:RestoreSet( player, "inventory" )
		--self:RestoreMetabolism( player )
		rust.SendChatMessage( player, self.Config.Messages.AdminDisabled )
	end
	
	datafile.SaveDataTable( "UltimateAdmin/Status" )
	
end

function PLUGIN:Heal( player )

	player.health = 100
	player.metabolism.bleeding.max = 0
	player.metabolism.bleeding.value = 0
	player.metabolism.calories.min = 1000
	player.metabolism.calories.value = 1000
	player.metabolism.dirtyness.max = 0
	player.metabolism.dirtyness.value = 0
	player.metabolism.heartrate.min = 0.5
	player.metabolism.heartrate.max = 0.5
	player.metabolism.heartrate.value = 0.5
	player.metabolism.hydration.min = 1000
	player.metabolism.hydration.value = 1000
	player.metabolism.oxygen.min = 1
	player.metabolism.oxygen.value = 1
	player.metabolism.poison.max = 0
	player.metabolism.poison.value = 0
	player.metabolism.radiation_level.max = 0
	player.metabolism.radiation_level.value = 0
	player.metabolism.radiation_poison.max = 0
	player.metabolism.radiation_poison.value = 0
	player.metabolism.temperature.min = 32
	player.metabolism.temperature.max = 32
	player.metabolism.temperature.value = 32
	player.metabolism.wetness.max = 0
	player.metabolism.wetness.value = 0
	
end

function PLUGIN:SaveMetabolism( player )

		local steamID = rust.UserIDFromPlayer( player )
		
		MetabolismData[steamID]											= {}
		MetabolismData[steamID]["playerName"]							= player.displayName								    
		MetabolismData[steamID]["health"]								= player.health								    
		MetabolismData[steamID]["metabolism.bleeding.max"]				= player.metabolism.bleeding.max 			    
		MetabolismData[steamID]["metabolism.bleeding.value"]			= player.metabolism.bleeding.value		    
		MetabolismData[steamID]["metabolism.calories.min"]				= player.metabolism.calories.min 			    
		MetabolismData[steamID]["metabolism.calories.value"]			= player.metabolism.calories.value 		    
		MetabolismData[steamID]["metabolism.dirtyness.max "]			= player.metabolism.dirtyness.max 		    
		MetabolismData[steamID]["metabolism.dirtyness.value"]			= player.metabolism.dirtyness.value 		    
		MetabolismData[steamID]["metabolism.heartrate.min"]				= player.metabolism.heartrate.min 		    
		MetabolismData[steamID]["metabolism.heartrate.max"]				= player.metabolism.heartrate.max 		    
		MetabolismData[steamID]["metabolism.heartrate.value"]			= player.metabolism.heartrate.value 		    
		MetabolismData[steamID]["metabolism.hydration.min"]				= player.metabolism.hydration.min 		    
		MetabolismData[steamID]["metabolism.hydration.value"]			= player.metabolism.hydration.value 		    
		MetabolismData[steamID]["metabolism.oxygen.min"]				= player.metabolism.oxygen.min 			    
		MetabolismData[steamID]["metabolism.oxygen.value"]				= player.metabolism.oxygen.value 			    
		MetabolismData[steamID]["metabolism.poison.max"]				= player.metabolism.poison.max 			    
		MetabolismData[steamID]["metabolism.poison.value "]				= player.metabolism.poison.value 			    
		MetabolismData[steamID]["metabolism.radiation_level.max"]		= player.metabolism.radiation_level.max 	    
		MetabolismData[steamID]["metabolism.radiation_level.value"]		= player.metabolism.radiation_level.value     
		MetabolismData[steamID]["metabolism.radiation_poison.max"]		= player.metabolism.radiation_poison.max     	
		MetabolismData[steamID]["metabolism.radiation_poison.value"]	= player.metabolism.radiation_poison.value    
		MetabolismData[steamID]["metabolism.temperature.min"]			= player.metabolism.temperature.min 		    
		MetabolismData[steamID]["metabolism.temperature.max"]			= player.metabolism.temperature.max 		    
		MetabolismData[steamID]["metabolism.temperature.value"]			= player.metabolism.temperature.value 	    
		MetabolismData[steamID]["metabolism.wetness.max"]				= player.metabolism.wetness.max 			    
		MetabolismData[steamID]["metabolism.wetness.value"]				= player.metabolism.wetness.value 
		
		datafile.SaveDataTable( "UltimateAdmin/Metabolism" )
		
end

function PLUGIN:RestoreMetabolism( player )

		local steamID = rust.UserIDFromPlayer( player )
		
		player.health 								= MetabolismData[steamID]["health"]
		player.metabolism.bleeding.max 				= MetabolismData[steamID]["metabolism.bleeding.max"]
		player.metabolism.bleeding.value			= MetabolismData[steamID]["metabolism.bleeding.value"]
		player.metabolism.calories.min 				= MetabolismData[steamID]["metabolism.calories.min"]
		player.metabolism.calories.value 			= MetabolismData[steamID]["metabolism.calories.value"]
		player.metabolism.dirtyness.max 			= MetabolismData[steamID]["metabolism.dirtyness.max "]
		player.metabolism.dirtyness.value 			= MetabolismData[steamID]["metabolism.dirtyness.value"]
		player.metabolism.heartrate.min 			= MetabolismData[steamID]["metabolism.heartrate.min"]
		player.metabolism.heartrate.max 			= MetabolismData[steamID]["metabolism.heartrate.max"]
		player.metabolism.heartrate.value 			= MetabolismData[steamID]["metabolism.heartrate.value"]
		player.metabolism.hydration.min 			= MetabolismData[steamID]["metabolism.hydration.min"]
		player.metabolism.hydration.value 			= MetabolismData[steamID]["metabolism.hydration.value"]
		player.metabolism.oxygen.min 				= MetabolismData[steamID]["metabolism.oxygen.min"]
		player.metabolism.oxygen.value 				= MetabolismData[steamID]["metabolism.oxygen.value"]
		player.metabolism.poison.max 				= MetabolismData[steamID]["metabolism.poison.max"]
		player.metabolism.poison.value 				= MetabolismData[steamID]["metabolism.poison.value "]
		player.metabolism.radiation_level.max 		= MetabolismData[steamID]["metabolism.radiation_level.max"]
		player.metabolism.radiation_level.value 	= MetabolismData[steamID]["metabolism.radiation_level.value"]
		player.metabolism.radiation_poison.max 		= MetabolismData[steamID]["metabolism.radiation_poison.max"]
		player.metabolism.radiation_poison.value 	= MetabolismData[steamID]["metabolism.radiation_poison.value"]
		player.metabolism.temperature.min 			= MetabolismData[steamID]["metabolism.temperature.min"]
		player.metabolism.temperature.max 			= MetabolismData[steamID]["metabolism.temperature.max"]
		player.metabolism.temperature.value 		= MetabolismData[steamID]["metabolism.temperature.value"]
		player.metabolism.wetness.max 				= MetabolismData[steamID]["metabolism.wetness.max"]
		player.metabolism.wetness.value 			= MetabolismData[steamID]["metabolism.wetness.value"]
		
end

function PLUGIN:RestoreSet( player, set )

	local steamID = rust.UserIDFromPlayer( player )
	local belt = player.inventory.containerBelt
	local main = player.inventory.containerMain
	local main = player.inventory.containerMain
	local wear = player.inventory.containerWear
	local Inventory = {}
	Inventory["belt"] = belt
	Inventory["main"] = main
	Inventory["wear"] = wear
	player.inventory:Strip()
	
	if set == "master" then	
		timer.Once ( 1, function ()
			for slot, items in pairs( MasterData ) do
				for i, item in pairs( items ) do
					local itemEntity = global.ItemManager.CreateByName( item.name, item.amount )
					if item.bp then
						itemEntity:SetFlag( global.Item.Flag.Blueprint, true )
					elseif item.condition then
						itemEntity.condition = item.condition       
					end
					player.inventory:GiveItem( itemEntity, Inventory[slot] )
				end
			end
		end )
	elseif set == "loadout" then
		timer.Once ( 1, function ()
			for slot, items in pairs( LoadoutData[steamID] ) do
				for i, item in pairs( items ) do
					local itemEntity = global.ItemManager.CreateByName( item.name, item.amount )
					if item.bp then
						itemEntity:SetFlag( global.Item.Flag.Blueprint, true )
					elseif item.condition then
						itemEntity.condition = item.condition       
					end
					player.inventory:GiveItem( itemEntity, Inventory[slot] )
				end
			end
		end )
	elseif set == "inventory" then
		timer.Once ( 1, function ()
			for slot, items in pairs( InventoryData[steamID] ) do
				for i, item in pairs( items ) do
					local itemEntity = global.ItemManager.CreateByName( item.name, item.amount )
					if item.bp then
						itemEntity:SetFlag( global.Item.Flag.Blueprint, true )
					elseif item.condition then
						itemEntity.condition = item.condition       
					end
					player.inventory:GiveItem( itemEntity, Inventory[slot] )
				end
			end
		end )
	end

end

function PLUGIN:SaveSet( player, set )

	local steamID = rust.UserIDFromPlayer( player )
	
	local belt = player.inventory.containerBelt
	local main = player.inventory.containerMain
	local wear = player.inventory.containerWear
	local beltItems = belt.itemList:GetEnumerator()
	local mainItems = main.itemList:GetEnumerator()
	local wearItems = wear.itemList:GetEnumerator()
	local beltCount = 0
	local mainCount = 0
	local wearCount = 0

	self:ClearSaved( player, set )
	
	if set == "master" then
		while beltItems:MoveNext() do
			MasterData["belt"][tostring( beltCount )] = { name = tostring( beltItems.Current.info.shortname ), amount = beltItems.Current.amount, condition = beltItems.Current.condition, bp = beltItems.Current:IsBlueprint() }
			beltCount = beltCount + 1
		end

		while mainItems:MoveNext() do
			MasterData["main"][tostring( mainCount)] = { name = tostring( mainItems.Current.info.shortname ), amount = mainItems.Current.amount, condition =  mainItems.Current.condition, bp = mainItems.Current:IsBlueprint() }
			mainCount = mainCount + 1
		end

		while wearItems:MoveNext() do
			MasterData["wear"][tostring( wearCount )] = { name = tostring( wearItems.Current.info.shortname ), amount = wearItems.Current.amount, condition = wearItems.Current.condition, bp = false }
			wearCount = wearCount + 1
		end 
		datafile.SaveDataTable( "UltimateAdmin/Master" )
	elseif set == "loadout" then
		while beltItems:MoveNext() do
		LoadoutData[steamID]["belt"][tostring( beltCount )] = { name = tostring( beltItems.Current.info.shortname ), amount = beltItems.Current.amount, condition = beltItems.Current.condition, bp = beltItems.Current:IsBlueprint() }
		beltCount = beltCount + 1
		end

		while mainItems:MoveNext() do
			LoadoutData[steamID]["main"][tostring( mainCount)] = { name = tostring( mainItems.Current.info.shortname ), amount = mainItems.Current.amount, condition =  mainItems.Current.condition, bp = mainItems.Current:IsBlueprint() }
			mainCount = mainCount + 1
		end

		while wearItems:MoveNext() do
			LoadoutData[steamID]["wear"][tostring( wearCount )] = { name = tostring( wearItems.Current.info.shortname ), amount = wearItems.Current.amount, condition = wearItems.Current.condition, bp = false }
			wearCount = wearCount + 1
		end  
		datafile.SaveDataTable( "UltimateAdmin/Loadout" )
	elseif set == "inventory" then
		while beltItems:MoveNext() do
		InventoryData[steamID]["belt"][tostring( beltCount )] = { name = tostring( beltItems.Current.info.shortname ), amount = beltItems.Current.amount, condition = beltItems.Current.condition, bp = beltItems.Current:IsBlueprint() }
		beltCount = beltCount + 1
		end

		while mainItems:MoveNext() do
			InventoryData[steamID]["main"][tostring( mainCount)] = { name = tostring( mainItems.Current.info.shortname ), amount = mainItems.Current.amount, condition =  mainItems.Current.condition, bp = mainItems.Current:IsBlueprint() }
			mainCount = mainCount + 1
		end

		while wearItems:MoveNext() do
			InventoryData[steamID]["wear"][tostring( wearCount )] = { name = tostring( wearItems.Current.info.shortname ), amount = wearItems.Current.amount, condition = wearItems.Current.condition, bp = false }
			wearCount = wearCount + 1
		end  
		datafile.SaveDataTable( "UltimateAdmin/Inventory" )
	end
	
end

function PLUGIN:ClearSaved( player, set )

	local steamID = rust.UserIDFromPlayer( player )
	
	if set == "master" then
		MasterData = MasterData or {}
		MasterData["belt"] = {}
		MasterData["main"] = {}
		MasterData["wear"] = {}
		datafile.SaveDataTable( "UltimateAdmin/Master" )
	elseif set == "loadout" then
		LoadoutData[steamID] = LoadoutData[steamID] or {}
		LoadoutData[steamID]["playerName"] = player.displayName
		LoadoutData[steamID] = {}
		LoadoutData[steamID]["belt"] = {}
		LoadoutData[steamID]["main"] = {}
		LoadoutData[steamID]["wear"] = {}		
		datafile.SaveDataTable( "UltimateAdmin/Loadout" )
	elseif set == "inventory" then
		InventoryData[steamID] = InventoryData[steamID] or {}
		InventoryData[steamID]["playerName"] = player.displayName
		InventoryData[steamID] = {}
		InventoryData[steamID]["belt"] = {}
		InventoryData[steamID]["main"] = {}
		InventoryData[steamID]["wear"] = {}		
		datafile.SaveDataTable( "UltimateAdmin/Inventory" )
	end
	
end

