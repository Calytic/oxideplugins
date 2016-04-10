PLUGIN.Title        = "Door Share"
PLUGIN.Description  = "Allows players to share all doors they own with other players."
PLUGIN.Author       = "InSaNe8472"
PLUGIN.Version      = V(1,1,2)
PLUGIN.ResourceID   = 1251

local DataFile_PL = "DoorShare_Players"
local DataFile_DR = "DoorShare_Doors"
local Data_PL = {}
local Data_DR = {}
local CoolDown = {}
local MasterKey

function PLUGIN:Init()
	permission.RegisterPermission("share.use", self.Plugin)
	permission.RegisterPermission("share.owner", self.Plugin)
	permission.RegisterPermission("share.nocd", self.Plugin)
	permission.RegisterPermission("share.admin", self.Plugin)
	command.AddChatCommand("share", self.Plugin, "cmdShareDoor")
	self:LoadDataFile()
	self:LoadDefaultConfig()
end

function PLUGIN:LoadDefaultConfig()
	self.Config.Settings = self.Config.Settings or {}
	self.Config.Messages = self.Config.Messages or {}
	self.Config.Settings.Prefix = self.Config.Settings.Prefix or "[ <color=#cd422b>Door Share</color> ]"
	self.Config.Settings.UsePermissions = self.Config.Settings.UsePermissions or "true"
	self.Config.Settings.Enabled = self.Config.Settings.Enabled or "true"
	self.Config.Settings.ShowAccessMessage = self.Config.Settings.ShowAccessMessage or "true"
	self.Config.Settings.MaxShare = self.Config.Settings.MaxShare or "25"
	self.Config.Settings.Cooldown = self.Config.Settings.Cooldown or "10"
	self.Config.Messages.NoPermission = self.Config.Messages.NoPermission or "You do not have permission to use this command."
	self.Config.Messages.ChangedStatus = self.Config.Messages.ChangedStatus or "Door sharing system <color=#cd422b>{status}</color>."
	self.Config.Messages.Disabled = self.Config.Messages.Disabled or "Door sharing is currently disabled."
	self.Config.Messages.CoolDown = self.Config.Messages.CoolDown or "You must wait <color=#cd422b>{cooldown} seconds</color> before using this command again."
	self.Config.Messages.MaxShare = self.Config.Messages.MaxShare or "You may only share your doors with <color=#cd422b>{limit} player(s)</color> at one time."
	self.Config.Messages.WrongArgs = self.Config.Messages.WrongArgs or "Syntax error.  Use <color=#cd422b>/share</color> for help."
	self.Config.Messages.NoPlayer = self.Config.Messages.NoPlayer or "Player not found.  Please try again."
	self.Config.Messages.MultiPlayer = self.Config.Messages.MultiPlayer or "Multiple players found.  Provide a more specific username."
	self.Config.Messages.Self = self.Config.Messages.Self or "You cannot share your doors with yourself."
	self.Config.Messages.PlayerExists = self.Config.Messages.PlayerExists or "You already share your doors with <color=#cd422b>{player}</color>."
	self.Config.Messages.PlayerAdded = self.Config.Messages.PlayerAdded or "You now share all your doors with <color=#cd422b>{player}</color>."
	self.Config.Messages.NewShareAdd = self.Config.Messages.NewShareAdd or "You have been granted access to all doors owned by <color=#cd422b>{player}</color>."
	self.Config.Messages.PlayerDeleted = self.Config.Messages.PlayerDeleted or "You no longer share all your doors with <color=#cd422b>{player}</color>."
	self.Config.Messages.NewShareDel = self.Config.Messages.NewShareDel or "You no longer have access to all doors owned by <color=#cd422b>{player}</color>."
	self.Config.Messages.DeleteAll = self.Config.Messages.DeleteAll or "You no longer share your doors with anyone. (<color=#cd422b>{entries}</color> players removed)"
	self.Config.Messages.PlayerNotExists = self.Config.Messages.PlayerNotExists or "You do not share your doors with <color=#cd422b>{player}</color>."
	self.Config.Messages.NoShares = self.Config.Messages.NoShares or "You do not share your doors with anyone."
	self.Config.Messages.DoorOwner = self.Config.Messages.DoorOwner or "The owner of this door is <color=#cd422b>{player}</color> ({id})."
	self.Config.Messages.DoorAccess = self.Config.Messages.DoorAccess or "You were granted access to this door by <color=#cd422b>{player}</color> ({id})."
	self.Config.Messages.Auth = self.Config.Messages.Auth or "You have been temporarily authorized on all nearby cupboards."
	if not tonumber(self.Config.Settings.MaxShare) or tonumber(self.Config.Settings.MaxShare) < 1 then self.Config.Settings.MaxShare = "25" end
	if not tonumber(self.Config.Settings.Cooldown) or tonumber(self.Config.Settings.Cooldown) < 1 then self.Config.Settings.Cooldown = "10" end
	self:SaveConfig()
end

function PLUGIN:LoadDataFile()
	local data = datafile.GetDataTable(DataFile_PL)
	Data_PL = data or {}
	data = datafile.GetDataTable(DataFile_DR)
	Data_DR = data or {}
	if not Data_DR.Doors then
		Data_DR.Doors = {}
		self:SaveDataFile(2)
	end
end

function PLUGIN:SaveDataFile(call)
	if call == 1 then datafile.SaveDataTable(DataFile_PL) end
	if call == 2 then datafile.SaveDataTable(DataFile_DR) end
end

function PLUGIN:Unload()
	datafile.SaveDataTable(DataFile_PL)
	datafile.SaveDataTable(DataFile_DR)
end

function PLUGIN:OnServerInitialized()
	MasterKey = plugins.Find("MasterKey") or false
end

function PLUGIN:GetPlayerData(playerSteamID, addNewEntry)
	local playerData = Data_PL[playerSteamID]
	if not playerData and addNewEntry then
		playerData = {}
		playerData.Shared = {}
		Data_PL[playerSteamID] = playerData
		self:SaveDataFile(1)
	end
	return playerData
end

local function FindPlayer(NameOrIpOrSteamID, checkSleeper)
	local playerTbl = {}
	local enumPlayerList = global.BasePlayer.activePlayerList:GetEnumerator()
	while enumPlayerList:MoveNext() do
		if enumPlayerList.Current then 
			local currPlayer = enumPlayerList.Current
			local currSteamID = rust.UserIDFromPlayer(currPlayer)
			local currIP = ""
			if currPlayer.net ~= nil and currPlayer.net.connection ~= nil then
				currIP = currPlayer.net.connection.ipaddress
			end
			if currPlayer.displayName == NameOrIpOrSteamID or currSteamID == NameOrIpOrSteamID or currIP == NameOrIpOrSteamID then
				table.insert(playerTbl, currPlayer)
				return #playerTbl, playerTbl
			end
			local matched, _ = string.find(currPlayer.displayName:lower(), NameOrIpOrSteamID:lower(), 1, true)
			if matched then
				table.insert(playerTbl, currPlayer)
			end
		end
	end
	if checkSleeper then
		local enumSleeperList = global.BasePlayer.sleepingPlayerList:GetEnumerator()
		while enumSleeperList:MoveNext() do
			local currPlayer = enumSleeperList.Current
			local currSteamID = rust.UserIDFromPlayer(currPlayer)
			if currPlayer.displayName == NameOrIpOrSteamID or currSteamID == NameOrIpOrSteamID then
				table.insert(playerTbl, currPlayer)
				return #playerTbl, playerTbl
			end
			local matched, _ = string.find(currPlayer.displayName:lower(), NameOrIpOrSteamID:lower(), 1, true)
			if matched then
				table.insert(playerTbl, currPlayer)
			end
		end
	end
	return #playerTbl, playerTbl
end

local function FormatMessage(message, values)
	for key, value in pairs(values) do message = message:gsub("{" .. key .. "}", value) end
	return message
end

function PLUGIN:cmdShareDoor(player, cmd, args)
	local playerSteamID = rust.UserIDFromPlayer(player)
	if self.Config.Settings.UsePermissions == "true" then
		if not permission.UserHasPermission(playerSteamID, "share.admin") and not permission.UserHasPermission(playerSteamID, "share.use") then
			rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.NoPermission)
			return
		end
	end
	if args.Length > 0 and args[0] == "toggle" then
		if not permission.UserHasPermission(playerSteamID, "share.admin") then
			rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.NoPermission)
			return
		end
		local message = ""
		if self.Config.Settings.Enabled == "true" then
			self.Config.Settings.Enabled = "false"
			message = FormatMessage(self.Config.Messages.ChangedStatus, { status = "disabled" })
			else
			self.Config.Settings.Enabled = "true"
			message = FormatMessage(self.Config.Messages.ChangedStatus, { status = "enabled" })
		end
		self:SaveConfig()
		rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..message)
		return
	end
	if self.Config.Settings.Enabled ~= "true" then
		rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.Disabled)
		return
	end
	if args.Length == 0 then
		if permission.UserHasPermission(playerSteamID, "share.admin") then
			rust.SendChatMessage(player,
				self.Config.Settings.Prefix.." <color=#ffd479>/share toggle</color> - Enable or disable door sharing system\n"..
				self.Config.Settings.Prefix.." <color=#ffd479>/share auth</color> - Temporarily authorize on all nearby cupboards"
			)
		end
		rust.SendChatMessage(player,
			self.Config.Settings.Prefix.." <color=#ffd479>/share limits</color> - View door share limits\n"..
			self.Config.Settings.Prefix.." <color=#ffd479>/share add <player></color> - Share all doors with player\n"..
			self.Config.Settings.Prefix.." <color=#ffd479>/share remove <player></color> - Unshare all doors with player\n"..
			self.Config.Settings.Prefix.." <color=#ffd479>/share removeall</color> - Unshare all doors with all players (cannot be undone)\n"..
			self.Config.Settings.Prefix.." <color=#ffd479>/share list</color> - List players sharing your doors"
		)
		return
		elseif args.Length > 0 then
		local func = args[0]
		if func ~= "auth" and func ~= "limits" and func ~= "add" and func ~= "remove" and func ~= "removeall" and func ~= "list" then
			rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.WrongArgs)
			return
		end
		if func == "auth" then
			if not permission.UserHasPermission(playerSteamID, "share.admin") then
				rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.NoPermission)
				return
			end
			player:SetPlayerFlag(global.BasePlayer.PlayerFlags.HasBuildingPrivilege, true)
			rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.Auth)
			return
		end
		if func == "limits" then
			rust.SendChatMessage(player,
				self.Config.Settings.Prefix.." Max Shares: <color=#ffd479>"..self.Config.Settings.MaxShare.." players</color>\n"..
				self.Config.Settings.Prefix.." Cooldown: <color=#ffd479>"..self.Config.Settings.Cooldown.." seconds</color>"
			)
			return
		end
		if func == "add" then
			if not self:CheckCooldown(player, 1) then return end
			if args.Length < 2 then
				rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.WrongArgs)
				return
			end
			local playerData = self:GetPlayerData(playerSteamID, true)
			if not permission.UserHasPermission(playerSteamID, "share.admin") then
				if tonumber(#playerData.Shared) >= tonumber(self.Config.Settings.MaxShare) then
					local message = FormatMessage(self.Config.Messages.MaxShare, { limit = self.Config.Settings.MaxShare })
					rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..message)
					return
				end
			end
			local found, targetplayer, targetname, targetid = self:CheckPlayer(player, args[1])
			if not found then return end
			if #playerData.Shared > 0 then
				for current, data in pairs(playerData.Shared) do
					if data.id == targetid then
						local message = FormatMessage(self.Config.Messages.PlayerExists, { player = targetname })
						rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..message)
						return
					end
				end
			end
			local newShare = {["player"] = targetname, ["id"] = targetid}
			table.insert(playerData.Shared, newShare)
			self:SaveDataFile(1)
			local message = FormatMessage(self.Config.Messages.PlayerAdded, { player = targetname })
			rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..message)
			if not permission.UserHasPermission(playerSteamID, "share.admin") and not permission.UserHasPermission(playerSteamID, "share.nocd") then
				CoolDown[playerSteamID] = time.GetUnixTimestamp()
			end
			if targetplayer:IsConnected() then
				local message = FormatMessage(self.Config.Messages.NewShareAdd, { player = player.displayName })
				rust.SendChatMessage(targetplayer, self.Config.Settings.Prefix.." "..message)
			end
			return
		end
		if func == "remove" then
			if not self:CheckCooldown(player, 1) then return end
			if args.Length < 2 then
				rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.WrongArgs)
				return
			end
			local found, targetplayer, targetname, targetid = self:CheckPlayer(player, args[1])
			if not found then return end
			local playerData = self:GetPlayerData(playerSteamID, true)
			if #playerData.Shared > 0 then
				for current, data in pairs(playerData.Shared) do
					if data.id == targetid then
						table.remove(playerData.Shared, current)
						self:SaveDataFile(1)
						local message = FormatMessage(self.Config.Messages.PlayerDeleted, { player = targetname })
						rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..message)
						if targetplayer:IsConnected() then
							local message = FormatMessage(self.Config.Messages.NewShareDel, { player = player.displayName })
							rust.SendChatMessage(targetplayer, self.Config.Settings.Prefix.." "..message)
						end
						return
					end
				end
			end
			local message = FormatMessage(self.Config.Messages.PlayerNotExists, { player = targetname })
			rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..message)
			if not permission.UserHasPermission(playerSteamID, "share.admin") and not permission.UserHasPermission(playerSteamID, "share.nocd") then
				CoolDown[playerSteamID] = time.GetUnixTimestamp()
			end
			return
		end
		if func == "removeall" then
			if not self:CheckCooldown(player, 1) then return end
			local playerData = self:GetPlayerData(playerSteamID, true)
			if #playerData.Shared == 0 then
				rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.NoShares)
				return
			end
			local message = FormatMessage(self.Config.Messages.DeleteAll, { entries = #playerData.Shared })
			rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..message)
			if not permission.UserHasPermission(playerSteamID, "share.admin") and not permission.UserHasPermission(playerSteamID, "share.nocd") then
				CoolDown[playerSteamID] = time.GetUnixTimestamp()
			end
			playerData.Shared = {}
			self:SaveDataFile(1)
			return
		end
		if func == "list" then
			local playerData = self:GetPlayerData(playerSteamID, true)
			if #playerData.Shared == 0 then
				rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.NoShares)
				return
			end
			local count = 0
			local players = ""
			for Share, data in pairs(playerData.Shared) do
				players = players..data.player..", "
				count = count + 1
			end
			rust.SendChatMessage(player, self.Config.Settings.Prefix.." Doors shared with <color=#cd422b>"..count.." player(s)</color>:\n"..string.sub(players, 1, -3))
			return
		end
	end
end

function PLUGIN:OnItemDeployed(deployer, entity)
	if entity.name == "assets/prefabs/locks/keypad/lock.code.prefab" or entity.name == "assets/prefabs/locks/keylock/lock.key.prefab" then
		local doorname, doorid = tostring(entity):match"([^%[]*)%[([^%]]*)"
		local playerSteamID = rust.UserIDFromPlayer(deployer.ownerPlayer)
		for current, data in pairs(Data_DR.Doors) do
			if data.door == doorid then
				data.door, data.player = doorid, playerSteamID
				self:SaveDataFile(2)
				return
			end
		end
		local newDoor = {["door"] = doorid, ["player"] = deployer.ownerPlayer.displayName, ["id"] = playerSteamID}
		table.insert(Data_DR.Doors, newDoor)
		self:SaveDataFile(2)
		return
	end
end

function PLUGIN:CanUseDoor(player, lock)
	if self.Config.Settings.Enabled == "true" then
		if MasterKey then
			local playerSteamID = rust.UserIDFromPlayer(player)
			if permission.UserHasPermission(playerSteamID, "masterkey.doors") then
				return
				else
				return self:ProcessDoor(player, lock)
			end
			else
			return self:ProcessDoor(player, lock)
		end
	end
end

function PLUGIN:ProcessDoor(player, lock)
	local playerSteamID = rust.UserIDFromPlayer(player)
	if permission.UserHasPermission(playerSteamID, "share.admin") then
		self:PlayDoorSound(player, lock)
		return true
	end
	local Access = true
	if self.Config.Settings.UsePermissions == "true" then
		if not permission.UserHasPermission(playerSteamID, "share.use") then Access = false end
	end
	if Access then
		local doorname, doorid = tostring(lock):match"([^%[]*)%[([^%]]*)"
		local foundplayer, founddoor
		for current, data in pairs(Data_DR.Doors) do
			if tonumber(data.door) == tonumber(doorid) then
				if data.id == playerSteamID then
					self:PlayDoorSound(player, lock)
					return true
				end
				foundowner = data.player
				founddoor = data.id
				break
			end
		end
		if founddoor then
			local playerData = self:GetPlayerData(founddoor, true)
			if #playerData.Shared > 0 then
				for current, data in pairs(playerData.Shared) do
					if data.id == playerSteamID then
						self:PlayDoorSound(player, lock)
						if self.Config.Settings.ShowAccessMessage == "true" then
							local message = FormatMessage(self.Config.Messages.DoorAccess, { player = foundowner, id = founddoor })
							rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..message)
						end
						return true
					end
				end
			end
		end
		self:CheckOwner(lock, player)
	end
end

function PLUGIN:CheckPlayer(player, target)
	local numFound, targetPlayerTbl = FindPlayer(target, true)
	if numFound == 0 then
		rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.NoPlayer)
		return false
	end
	if numFound > 1 then
		local targetNameString = ""
		for i = 1, numFound do
			targetNameString = targetNameString..targetPlayerTbl[i].displayName..", "
		end
		rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.MultiPlayer)
		rust.SendChatMessage(player, targetNameString)
		return false
	end
	local targetPlayer = targetPlayerTbl[1]
	local targetName = targetPlayer.displayName
	local targetSteamID = rust.UserIDFromPlayer(targetPlayer)
	local playerSteamID = rust.UserIDFromPlayer(player)
	if playerSteamID == targetSteamID then
		rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.Self)
		return false
	end
	return true, targetPlayer, targetName, targetSteamID
end

function PLUGIN:CheckOwner(lock, player)
	local playerSteamID = rust.UserIDFromPlayer(player)
	if self:CheckCooldown(player, 2) and permission.UserHasPermission(playerSteamID, "share.owner") then
		local doorname, doorid = tostring(lock):match"([^%[]*)%[([^%]]*)"
		for current, data in pairs(Data_DR.Doors) do
			if tonumber(data.door) == tonumber(doorid) then
				local message = FormatMessage(self.Config.Messages.DoorOwner, { player = data.player, id = data.id })
				rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..message)
				if not permission.UserHasPermission(playerSteamID, "share.admin") and not permission.UserHasPermission(playerSteamID, "share.nocd") then
					CoolDown[playerSteamID] = time.GetUnixTimestamp()
				end
				break
			end
		end
	end
end

function PLUGIN:CheckCooldown(player, call)
	local playerSteamID = rust.UserIDFromPlayer(player)
	if not permission.UserHasPermission(playerSteamID, "share.admin") and not permission.UserHasPermission(playerSteamID, "share.nocd") then
		if CoolDown[playerSteamID] then
			local Timestamp = time.GetUnixTimestamp()
			if Timestamp - CoolDown[playerSteamID] < tonumber(self.Config.Settings.Cooldown) then
				if call == 1 then
					local remaining = tonumber(self.Config.Settings.Cooldown) - (Timestamp - CoolDown[playerSteamID])
					local message = FormatMessage(self.Config.Messages.CoolDown, { cooldown = remaining })
					rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..message)
				end
				return false
			end
		end
	end
	return true
end

function PLUGIN:PlayDoorSound(player, lock)
	if lock.name == "assets/prefabs/locks/keypad/lock.code.prefab" then
		global.Effect.server.Run.methodarray[3]:Invoke(nil, util.TableToArray({"assets/prefabs/locks/keypad/effects/lock.code.lock.prefab", player.transform.position, UnityEngine.Vector3.get_up(), nil, true}))
	end
end

function PLUGIN:SendHelpText(player)
	rust.SendChatMessage(player, "<color=#ffd479>/share</color> - Allows players to share all doors they own with other players")
end