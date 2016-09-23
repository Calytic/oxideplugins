PLUGIN.Title        = "Door Share"
PLUGIN.Description  = "Allows players to share selected claimed doors with other players"
PLUGIN.Author       = "InSaNe8472"
PLUGIN.Version      = V(1,1,5)
PLUGIN.ResourceID   = 1251

local DataFile_PL = "DoorShare_Players"
local DataFile_DR = "DoorShare_Doors"
local Data_PL = {}
local Data_DR = {}
local CoolDown = {}
local CoolDownOwner = {}
local Config = {}
local ConfigVars = {}
local DoorData = {}
local MasterKey

function PLUGIN:Init()
	permission.RegisterPermission("doorshare.use", self.Plugin)
	permission.RegisterPermission("doorshare.owner", self.Plugin)
	permission.RegisterPermission("doorshare.nocd", self.Plugin)
	permission.RegisterPermission("doorshare.admin", self.Plugin)
	command.AddChatCommand("share", self.Plugin, "cmdShareDoor")
	self:LoadDataFile()
	self:LoadDefaultConfig()
	self:LoadDefaultLang()
end

function PLUGIN:LoadDefaultConfig()
	self.Config.Settings = self.Config.Settings or {}
	self.Config.Settings.UsePermissions = self.Config.Settings.UsePermissions or "true"
	self.Config.Settings.BuildingBlocked = self.Config.Settings.BuildingBlocked or "true"
	self.Config.Settings.Enabled = self.Config.Settings.Enabled or "true"
	self.Config.Settings.OpenOnly = self.Config.Settings.OpenOnly or "true"
	self.Config.Settings.ShowAccessMessage = self.Config.Settings.ShowAccessMessage or "true"
	self.Config.Settings.MaxShare = self.Config.Settings.MaxShare or "25"
	self.Config.Settings.CommandCooldown = self.Config.Settings.CommandCooldown or "10"
	self.Config.Settings.OwnerCooldown = self.Config.Settings.OwnerCooldown or "15"
	self.Config.Settings.MessageSize = self.Config.Settings.MessageSize or "12"
	if not tonumber(self.Config.Settings.MaxShare) or tonumber(self.Config.Settings.MaxShare) < 1 then self.Config.Settings.MaxShare = "25" end
	if not tonumber(self.Config.Settings.CommandCooldown) or tonumber(self.Config.Settings.CommandCooldown) < 1 then self.Config.Settings.CommandCooldown = "10" end
	if not tonumber(self.Config.Settings.OwnerCooldown) or tonumber(self.Config.Settings.OwnerCooldown) < 1 then self.Config.Settings.OwnerCooldown = "15" end
	if not tonumber(self.Config.Settings.MessageSize) or tonumber(self.Config.Settings.MessageSize) < 1 then self.Config.Settings.MessageSize = "12" end
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

function PLUGIN:LoadDefaultLang()
	lang.RegisterMessages(util.TableToLangDict({
		["Prefix"] = "[<color=#cd422b> Door Share </color>] ",
		["NoPermission"] = "You do not have permission to use this command.",
		["BuildingBlocked"] = "Doors cannot be modified in building blocked areas.  Set off to cancel.",
		["ChangedStatus"] = "Door sharing system <color=#cd422b>{status}</color>.",
		["Disabled"] = "Door sharing is currently disabled.",
		["CoolDown"] = "You must wait <color=#cd422b>{cooldown} seconds</color> before using this command again.",
		["MaxShare"] = "You may only share your doors with <color=#cd422b>{limit} player(s)</color> at one time.",
		["WrongArgs"] = "Syntax error.  Use <color=#cd422b>/share</color> for help.",
		["LangError"] = "Language Error: ",
		["NoPlayer"] = "Player not found or multiple players found.  Provide a more specific username.",
		["Self"] = "You cannot share or transfer your doors with yourself.",
		["PlayerExists"] = "You already share your doors with <color=#cd422b>{player}</color>.",
		["PlayerAdded"] = "You now share all your doors with <color=#cd422b>{player}</color>.",
		["NewShareAdd"] = "You have been granted access to all doors owned by <color=#cd422b>{player}</color>.",
		["PlayerDeleted"] = "You no longer share all your doors with <color=#cd422b>{player}</color>.",
		["NewShareDel"] = "You no longer have access to all doors owned by <color=#cd422b>{player}</color>.",
		["DeleteAll"] = "You no longer share your doors with anyone. (<color=#cd422b>{entries}</color> players removed)",
		["PlayerNotExists"] = "You do not share your doors with <color=#cd422b>{player}</color>.",
		["NoShares"] = "You do not share your doors with anyone.",
		["ShareList"] = "Doors shared with <color=#cd422b>{count} player(s)</color>:\n {players}",
		["DoorOwner"] = "The owner of this door is <color=#cd422b>{player}</color> ({id}).",
		["DoorAccess"] = "You were granted access to this door by <color=#cd422b>{player}</color> ({id}).",
		["Auth"] = "You have been temporarily authorized on all nearby cupboards.",
		["DoorConfig"] = "Door configuration set to <color=#cd422b>{status}</color>.",
		["DoorStatus"] = "Door has been <color=#cd422b>{status}</color>.",
		["AdminDoorAll"] = "All claimed doors by <color=#cd422b>{owner}</color> {status}.",
		["DoorAll"] = "All claimed doors <color=#cd422b>{status}</color>.",
		["DoorAlreadySet"] = "Door already claimed by <color=#cd422b>{owner}</color>.",
		["DoorSetNew"] = "Door set to <color=#cd422b>{owner}</color>.",
		["DoorSetExists"] = "Door claimed by <color=#cd422b>{owner}</color> set to <color=#cd422b>{newowner}</color>.",
		["AdminPending"] = "Door configuration: <color=#cd422b>{status}</color>.  Set off to cancel.",
		["AdminTransferFail"] = "You cannot transfer claimed doors of a player to themselves.",
		["AdminTransferAllOwner"] = "Administrator has transfered all your claimed doors to <color=#cd422b>{owner}</color>.",
		["AdminTransferAllNewOwner"] = "Administrator has transfered all claimed doors by <color=#cd422b>{owner}</color> to you.",
		["DoorTransferAll"] = "<color=#cd422b>{owner}</color> has transfered all claimed doors to you.",
		["AdminDoor"] = "Door claimed by <color=#cd422b>{owner}</color> {status}.",
		["Help"] = "<color=#ffd479>/share</color> - Allows players to share selected claimed doors with other players",
		["Admin"] = "\n<color=#ffd479>/share toggle</color> - Enable or disable system\n"..
		"<color=#ffd479>/share auth</color> - Temporarily authorize nearby cupboards\n"..
		"<color=#ffd479>/share adoor <set | share | unshare | remove | off> [player]</color> - Set, share, unshare and remove claimed doors\n"..
		"<color=#ffd479>/share adoor <shareall | unshareall | transferall | removeall> [player] [player]</color> - Share, unshare, transfer and remove claimed doors",
		["Menu"] = "\n<color=#ffd479>/share help</color> - View help\n"..
		"<color=#ffd479>/share limits</color> - View system limits\n"..
		"<color=#ffd479>/share door <add | share | unshare | transfer | remove | off> [player]</color> - Add, share, unshare, transfer and remove doors\n"..
		"<color=#ffd479>/share door <shareall | unshareall | transferall | removeall> [player]</color> - Share, unshare, transfer and remove all claimed doors\n"..
		"<color=#ffd479>/share add <player></color> - Share selected doors with player\n"..
		"<color=#ffd479>/share remove <player></color> - Unshare selected doors with player\n"..
		"<color=#ffd479>/share removeall</color> - Unshare selected doors with all players\n"..
		"<color=#ffd479>/share list</color> - List players sharing doors",
		["HelpMenu"] = "\n<color=#ffd479>1.</color> Once a lock is placed on a door, it must be claimed to be shared with other players.\n"..
		"<color=#ffd479>2.</color> A door can only be claimed by one player at a time.\n"..
		"<color=#ffd479>3.</color> For players, the [player] variable is only required when transfering claimed doors.\n",
		["Limits"] = "\nMax Shares: <color=#ffd479>{maxshare} players</color>\n"..
		"Command Cooldown: <color=#ffd479>{command} seconds</color>\n"..
		"Owner Message Cooldown: <color=#ffd479>{owner} seconds</color>\n"..
		"Building Blocked: <color=#ffd479>{blocked}</color>\n"..
		"Open Only Check: <color=#ffd479>{openonly}</color>",
		["DoorSet"] = "set to {owner}",
		["DoorTrans"] = "transfer to {owner}",
		["DoorAdded"] = "added",
		["DoorRemoved"] = "removed",
		["DoorShared"] = "shared",
		["DoorUnshared"] = "unshared",
		["DoorTransfer"] = "transfered to <color=#cd422b>{owner}</color>",
	}), self.Plugin)
end

function PLUGIN:OnServerInitialized()
	MasterKey = plugins.Find("MasterKey") or false
end

function PLUGIN:GetPlayerData(playerSteamID, addNewEntry)
	local playerData = Data_PL[playerSteamID]
	if not playerData and addNewEntry then
		playerData = {}
		playerData.config = "off"
		playerData.Shared = {}
		Data_PL[playerSteamID] = playerData
		self:SaveDataFile(1)
	end
	return playerData
end

function PLUGIN:OnPlayerInit(player)
	local playerSteamID = rust.UserIDFromPlayer(player)
	local playerData = self:GetPlayerData(playerSteamID, true)
	if playerData.config == "set" or playerData.config == "aremove" then
		Config[playerSteamID] = "off"
		playerData.config = "off"
		self:SaveDataFile(1)
		else
		Config[playerSteamID] = playerData.config
	end
end

function PLUGIN:OnPlayerDisconnected(player)
	local playerSteamID = rust.UserIDFromPlayer(player)
	Config[playerSteamID] = nil
	ConfigVars[playerSteamID] = nil
end

local function FormatMessage(message, values)
	for key, value in pairs(values) do message = message:gsub("{" .. key .. "}", value) end
	return message
end

function PLUGIN:Lang(player, lng)
	local playerSteamID
	if player and player ~= nil then playerSteamID = rust.UserIDFromPlayer(player) end
	local message = lang.GetMessage(lng, self.Plugin, playerSteamID)
	if message == lng then message = lang.GetMessage("LangError", self.Plugin, playerSteamID)..lng end
	return message
end

function PLUGIN:cmdShareDoor(player, cmd, args)
	local playerSteamID = rust.UserIDFromPlayer(player)
	if self.Config.Settings.UsePermissions == "true" then
		if not permission.UserHasPermission(playerSteamID, "doorshare.admin") and not permission.UserHasPermission(playerSteamID, "doorshare.use") then
			self:RustMessage(player, self:Lang(player, "NoPermission"))
			return
		end
	end
	if args.Length > 0 and args[0] == "toggle" then
		if not permission.UserHasPermission(playerSteamID, "doorshare.admin") then
			self:RustMessage(player, self:Lang(player, "NoPermission"))
			return
		end
		local message = ""
		if self.Config.Settings.Enabled == "true" then
			self.Config.Settings.Enabled = "false"
			message = FormatMessage(self:Lang(player, "ChangedStatus"), { status = "disabled" })
			else
			self.Config.Settings.Enabled = "true"
			message = FormatMessage(self:Lang(player, "ChangedStatus"), { status = "enabled" })
		end
		self:SaveConfig()
		self:RustMessage(player, message)
		return
	end
	if self.Config.Settings.Enabled ~= "true" then
		self:RustMessage(player, self:Lang(player, "Disabled"))
		return
	end
	if args.Length == 0 then
		if permission.UserHasPermission(playerSteamID, "doorshare.admin") then
			self:RustMessage(player, self:Lang(player, "Admin"))
		end
		self:RustMessage(player, self:Lang(player, "Menu"))
		return
		elseif args.Length > 0 then
		local func = args[0]
		if func ~= "auth" and func ~= "help" and func ~= "limits" and func ~= "adoor" and func ~= "door" and func ~= "add" and func ~= "remove" and func ~= "removeall" and func ~= "list" then
			self:RustMessage(player, self:Lang(player, "WrongArgs"))
			return
		end
		if func == "auth" then
			if not permission.UserHasPermission(playerSteamID, "doorshare.admin") then
				self:RustMessage(player, self:Lang(player, "NoPermission"))
				return
			end
			player:SetPlayerFlag(global.BasePlayer.PlayerFlags.HasBuildingPrivilege, true)
			self:RustMessage(player, self:Lang(player, "Auth"))
			return
		end
		if func == "help" then
			self:RustMessage(player, self:Lang(player, "HelpMenu"))
			return
		end
		if func == "limits" then
			local message = FormatMessage(self:Lang(player, "Limits"), { maxshare = self.Config.Settings.MaxShare, command = self.Config.Settings.CommandCooldown, owner = self.Config.Settings.OwnerCooldown, blocked = self.Config.Settings.BuildingBlocked, openonly = self.Config.Settings.OpenOnly })
			self:RustMessage(player, message)
			return
		end
		if func == "adoor" then
			if not permission.UserHasPermission(playerSteamID, "doorshare.admin") then
				self:RustMessage(player, self:Lang(player, "NoPermission"))
				return
			end
			if args.Length < 2 then
				self:RustMessage(player, self:Lang(player, "WrongArgs"))
				return
			end
			local sfunc = args[1]
			if sfunc ~= "set" and sfunc ~= "share" and sfunc ~= "unshare" and sfunc ~= "remove" and sfunc ~= "off" and sfunc ~= "shareall" and sfunc ~= "unshareall" and sfunc ~= "transferall" and sfunc ~= "removeall" then
				self:RustMessage(player, self:Lang(player, "WrongArgs"))
				return
			end
			if sfunc == "off" then
				local playerData = self:GetPlayerData(playerSteamID, true)
				Config[playerSteamID] = "off"
				playerData.config = "off"
				self:SaveDataFile(1)
				local message = FormatMessage(self:Lang(player, "DoorConfig"), { status = sfunc })
				self:RustMessage(player, message)
			end
			if sfunc == "set" then
				if args.Length < 3 then
					self:RustMessage(player, self:Lang(player, "WrongArgs"))
					return
				end
				local found, targetplayer, targetname, targetid = self:CheckPlayer(player, args[2], true)
				if not found then return end
				local playerData = self:GetPlayerData(playerSteamID, true)
				Config[playerSteamID] = "set"
				playerData.config = "set"
				self:SaveDataFile(1)
				ConfigVars[playerSteamID] = targetid.."|"..targetname
				local _message = FormatMessage(self:Lang(player, "DoorSet"), { owner = targetname.." ("..targetid..")" })
				local message = FormatMessage(self:Lang(player, "AdminPending"), { status = _message })
				self:RustMessage(player, message)
			end
			if sfunc == "share" then
				local playerData = self:GetPlayerData(playerSteamID, true)
				Config[playerSteamID] = "ashare"
				playerData.config = "ashare"
				self:SaveDataFile(1)
				local message = FormatMessage(self:Lang(player, "AdminPending"), { status = sfunc })
				self:RustMessage(player, message)
			end
			if sfunc == "unshare" then
				local playerData = self:GetPlayerData(playerSteamID, true)
				Config[playerSteamID] = "aunshare"
				playerData.config = "aunshare"
				self:SaveDataFile(1)
				local message = FormatMessage(self:Lang(player, "AdminPending"), { status = sfunc })
				self:RustMessage(player, message)
			end
			if sfunc == "remove" then
				local playerData = self:GetPlayerData(playerSteamID, true)
				Config[playerSteamID] = "aremove"
				playerData.config = "aremove"
				self:SaveDataFile(1)
				local message = FormatMessage(self:Lang(player, "AdminPending"), { status = sfunc })
				self:RustMessage(player, message)
			end
			if sfunc == "shareall" then
				if args.Length < 3 then
					self:RustMessage(player, self:Lang(player, "WrongArgs"))
					return
				end
				local found, targetplayer, targetname, targetid = self:CheckPlayer(player, args[2], true)
				if not found then return end
				for current, data in pairs(Data_DR.Doors) do
					if (data.id == targetid) then
						data.shared = "true"
					end
				end
				self:SaveDataFile(2)
				local message = FormatMessage(self:Lang(player, "AdminDoorAll"), { owner = targetname.." ("..targetid..")", status = self:Lang(player, "DoorShared") })
				self:RustMessage(player, message)
			end
			if sfunc == "unshareall" then
				if args.Length < 3 then
					self:RustMessage(player, self:Lang(player, "WrongArgs"))
					return
				end
				local found, targetplayer, targetname, targetid = self:CheckPlayer(player, args[2], true)
				if not found then return end
				for current, data in pairs(Data_DR.Doors) do
					if (data.id == targetid) then
						data.shared = "false"
					end
				end
				self:SaveDataFile(2)
				local message = FormatMessage(self:Lang(player, "AdminDoorAll"), { owner = targetname.." ("..targetid..")", status = self:Lang(player, "DoorUnshared") })
				self:RustMessage(player, message)
			end
			if sfunc == "transferall" then
				if args.Length < 4 then
					self:RustMessage(player, self:Lang(player, "WrongArgs"))
					return
				end
				local found, targetplayer, targetname, targetid = self:CheckPlayer(player, args[2], true)
				if not found then return end
				local _found, _targetplayer, _targetname, _targetid = self:CheckPlayer(player, args[3], true)
				if not _found then return end
				if targetid == _targetid then
					self:RustMessage(player, self:Lang(player, "AdminTransferFail"))
					return
				end
				for current, data in pairs(Data_DR.Doors) do
					if (data.id == targetid) then
						data.id, data.player = _targetid, _targetname
					end
				end
				self:SaveDataFile(2)
				local _message = FormatMessage(self:Lang(player, "DoorTransfer"), { owner = _targetname.." (".._targetid..")" })
				local message = FormatMessage(self:Lang(player, "AdminDoorAll"), { owner = targetname.." ("..targetid..")", status = _message })
				self:RustMessage(player, message)
				if targetplayer:IsConnected() then
					local message = FormatMessage(self:Lang(player, "AdminTransferAllOwner"), { owner = _targetname })
					self:RustMessage(targetplayer, message)
				end
				if _targetplayer:IsConnected() then
					local message = FormatMessage(self:Lang(player, "AdminTransferAllNewOwner"), { owner = targetname })
					self:RustMessage(_targetplayer, message)
				end
			end
			if sfunc == "removeall" then
				if args.Length < 3 then
					self:RustMessage(player, self:Lang(player, "WrongArgs"))
					return
				end
				local found, targetplayer, targetname, targetid = self:CheckPlayer(player, args[2], true)
				if not found then return end
				for current = #Data_DR.Doors, 1, -1 do
					if (Data_DR.Doors[current].id == targetid) then
						table.remove(Data_DR.Doors, current)
					end
				end
				self:SaveDataFile(2)
				local message = FormatMessage(self:Lang(player, "AdminDoorAll"), { owner = targetname.." ("..targetid..")", status = self:Lang(player, "DoorRemoved") })
				self:RustMessage(player, message)
			end
			return
		end
		if func == "door" then
			if not self:CheckCooldown(player) then return end
			if args.Length < 2 then
				self:RustMessage(player, self:Lang(player, "WrongArgs"))
				return
			end
			local sfunc = args[1]
			if sfunc ~= "add" and sfunc ~= "share" and sfunc ~= "unshare" and sfunc ~= "transfer" and sfunc ~= "remove" and sfunc ~= "off" and sfunc ~= "shareall" and sfunc ~= "unshareall" and sfunc ~= "transferall" and sfunc ~= "removeall" then
				self:RustMessage(player, self:Lang(player, "WrongArgs"))
				return
			end
			local playerData = self:GetPlayerData(playerSteamID, true)
			if sfunc == "off" then
				Config[playerSteamID] = "off"
				playerData.config = "off"
			end
			if sfunc == "add" then
				Config[playerSteamID] = "add"
				playerData.config = "add"
			end
			if sfunc == "share" then
				Config[playerSteamID] = "share"
				playerData.config = "share"
			end
			if sfunc == "unshare" then
				Config[playerSteamID] = "unshare"
				playerData.config = "unshare"
			end
			if sfunc == "transfer" then
				if args.Length < 3 then
					self:RustMessage(player, self:Lang(player, "WrongArgs"))
					return
				end
				local found, targetplayer, targetname, targetid = self:CheckPlayer(player, args[2], false)
				if not found then return end
				Config[playerSteamID] = "transfer"
				playerData.config = "transfer"
				ConfigVars[playerSteamID] = targetid.."|"..targetname
			end
			if sfunc == "remove" then
				Config[playerSteamID] = "remove"
				playerData.config = "remove"
			end
			if sfunc == "shareall" then
				Config[playerSteamID] = "off"
				playerData.config = "off"
				sfunc = "off"
				for current, data in pairs(Data_DR.Doors) do
					if (data.id == playerSteamID) then
						data.shared = "true"
					end
				end
				self:SaveDataFile(2)
				local message = FormatMessage(self:Lang(player, "DoorAll"), { status = self:Lang(player, "DoorShared") })
				self:RustMessage(player, message)
			end
			if sfunc == "unshareall" then
				Config[playerSteamID] = "off"
				playerData.config = "off"
				sfunc = "off"
				for current, data in pairs(Data_DR.Doors) do
					if (data.id == playerSteamID) then
						data.shared = "false"
					end
				end
				self:SaveDataFile(2)
				local message = FormatMessage(self:Lang(player, "DoorAll"), { status = self:Lang(player, "DoorUnshared") })
				self:RustMessage(player, message)
			end
			if sfunc == "transferall" then
				if args.Length < 3 then
					self:RustMessage(player, self:Lang(player, "WrongArgs"))
					return
				end
				local found, targetplayer, targetname, targetid = self:CheckPlayer(player, args[2], false)
				if not found then return end
				Config[playerSteamID] = "off"
				playerData.config = "off"
				sfunc = "off"
				for current, data in pairs(Data_DR.Doors) do
					if (data.id == playerSteamID) then
						data.id, data.player = targetid, targetname
					end
				end
				self:SaveDataFile(2)
				local _message = FormatMessage(self:Lang(player, "DoorTransfer"), { owner = targetname.." ("..targetid..")" })
				local message = FormatMessage(self:Lang(player, "DoorAll"), { status = _message })
				self:RustMessage(player, message)
				if targetplayer:IsConnected() then
					local message = FormatMessage(self:Lang(player, "DoorTransferAll"), { owner = player.displayName })
					self:RustMessage(targetplayer, message)
				end
			end
			if sfunc == "removeall" then
				Config[playerSteamID] = "off"
				playerData.config = "off"
				sfunc = "off"
				for current = #Data_DR.Doors, 1, -1 do
					if (Data_DR.Doors[current].id == playerSteamID) then
						table.remove(Data_DR.Doors, current)
					end
				end
				self:SaveDataFile(2)
				local message = FormatMessage(self:Lang(player, "DoorAll"), { status = self:Lang(player, "DoorRemoved") })
				self:RustMessage(player, message)
			end
			local message = FormatMessage(self:Lang(player, "DoorConfig"), { status = sfunc })
			self:RustMessage(player, message)
			self:SaveDataFile(1)
			if not permission.UserHasPermission(playerSteamID, "doorshare.admin") and not permission.UserHasPermission(playerSteamID, "doorshare.nocd") then
				CoolDown[playerSteamID] = time.GetUnixTimestamp()
			end
			return
		end
		if func == "add" then
			if not self:CheckCooldown(player) then return end
			if args.Length < 2 then
				self:RustMessage(player, self:Lang(player, "WrongArgs"))
				return
			end
			local playerData = self:GetPlayerData(playerSteamID, true)
			if not permission.UserHasPermission(playerSteamID, "doorshare.admin") then
				if tonumber(#playerData.Shared) >= tonumber(self.Config.Settings.MaxShare) then
					local message = FormatMessage(self:Lang(player, "MaxShare"), { limit = self.Config.Settings.MaxShare })
					self:RustMessage(player, message)
					return
				end
			end
			local found, targetplayer, targetname, targetid = self:CheckPlayer(player, args[1], false)
			if not found then return end
			if #playerData.Shared > 0 then
				for current, data in pairs(playerData.Shared) do
					if data.id == targetid then
						local message = FormatMessage(self:Lang(player, "PlayerExists"), { player = targetname })
						self:RustMessage(player, message)
						return
					end
				end
			end
			local newShare = {["player"] = targetname, ["id"] = targetid}
			table.insert(playerData.Shared, newShare)
			self:SaveDataFile(1)
			local message = FormatMessage(self:Lang(player, "PlayerAdded"), { player = targetname })
			self:RustMessage(player, message)
			if not permission.UserHasPermission(playerSteamID, "doorshare.admin") and not permission.UserHasPermission(playerSteamID, "doorshare.nocd") then
				CoolDown[playerSteamID] = time.GetUnixTimestamp()
			end
			if targetplayer:IsConnected() then
				local message = FormatMessage(self:Lang(player, "NewShareAdd"), { player = player.displayName })
				self:RustMessage(targetplayer, message)
			end
			return
		end
		if func == "remove" then
			if not self:CheckCooldown(player) then return end
			if args.Length < 2 then
				self:RustMessage(player, self:Lang(player, "WrongArgs"))
				return
			end
			local found, targetplayer, targetname, targetid = self:CheckPlayer(player, args[1], false)
			if not found then return end
			local playerData = self:GetPlayerData(playerSteamID, false)
			if playerData and #playerData.Shared > 0 then
				for current, data in pairs(playerData.Shared) do
					if data.id == targetid then
						table.remove(playerData.Shared, current)
						self:SaveDataFile(1)
						local message = FormatMessage(self:Lang(player, "PlayerDeleted"), { player = targetname })
						self:RustMessage(player, message)
						if targetplayer:IsConnected() then
							local message = FormatMessage(self:Lang(player, "NewShareDel"), { player = player.displayName })
							self:RustMessage(targetplayer, message)
						end
						return
					end
				end
			end
			local message = FormatMessage(self:Lang(player, "PlayerNotExists"), { player = targetname })
			self:RustMessage(player, message)
			if not permission.UserHasPermission(playerSteamID, "doorshare.admin") and not permission.UserHasPermission(playerSteamID, "doorshare.nocd") then
				CoolDown[playerSteamID] = time.GetUnixTimestamp()
			end
			return
		end
		if func == "removeall" then
			if not self:CheckCooldown(player) then return end
			local playerData = self:GetPlayerData(playerSteamID, false)
			if not playerData or #playerData.Shared == 0 then
				self:RustMessage(player, self:Lang(player, "NoShares"))
				return
			end
			local message = FormatMessage(self:Lang(player, "DeleteAll"), { entries = #playerData.Shared })
			self:RustMessage(player, message)
			if not permission.UserHasPermission(playerSteamID, "doorshare.admin") and not permission.UserHasPermission(playerSteamID, "doorshare.nocd") then
				CoolDown[playerSteamID] = time.GetUnixTimestamp()
			end
			playerData.Shared = {}
			self:SaveDataFile(1)
			return
		end
		if func == "list" then
			local playerData = self:GetPlayerData(playerSteamID, false)
			if not playerData or #playerData.Shared == 0 then
				self:RustMessage(player, self:Lang(player, "NoShares"))
				return
			end
			local count = 0
			local players = ""
			for Share, data in pairs(playerData.Shared) do
				players = players..data.player..", "
				count = count + 1
			end
			local message = FormatMessage(self:Lang(player, "ShareList"), { count = count, players = string.sub(players, 1, -3) })
			self:RustMessage(player, message)
			return
		end
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
	if self.Config.Settings.OpenOnly == "true" then
		local parent = lock.parentEntity:Get(true)
		if parent:IsOpen() then
			self:PlayDoorSound(player, lock)
			return true
		end
	end
	self:CheckOwner(lock, player)
	local playerSteamID = rust.UserIDFromPlayer(player)
	DoorData[playerSteamID] = lock
	if permission.UserHasPermission(playerSteamID, "doorshare.admin") then
		self:PlayDoorSound(player, lock)
		return true
	end
	local Access = true
	if self.Config.Settings.UsePermissions == "true" then
		if not permission.UserHasPermission(playerSteamID, "doorshare.use") then Access = false end
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
				if data.shared == "false" then return end
				foundowner = data.player
				founddoor = data.id
				break
			end
		end
		if founddoor then
			local playerData = self:GetPlayerData(founddoor, false)
			if playerData and #playerData.Shared > 0 then
				for current, data in pairs(playerData.Shared) do
					if data.id == playerSteamID then
						self:PlayDoorSound(player, lock)
						if self.Config.Settings.ShowAccessMessage == "true" then
							local message = FormatMessage(self:Lang(player, "DoorAccess"), { player = foundowner, id = founddoor })
							self:RustMessage(player, message)
						end
						return true
					end
				end
			end
		end
	end
end

function PLUGIN:OnDoorOpened(door, player)
	local playerSteamID = rust.UserIDFromPlayer(player)
	if DoorData[playerSteamID] and DoorData[playerSteamID] ~= nil then
		self:SaveLock(player, DoorData[playerSteamID])
		DoorData[playerSteamID] = nil
	end
end

function PLUGIN:SaveLock(player, lock)
	local playerSteamID = rust.UserIDFromPlayer(player)
	if not Config[playerSteamID] or Config[playerSteamID] == nil then
		Config[playerSteamID] = "off"
		return
	end
	if Config[playerSteamID] == "off" then return end
	local lockname, lockid = tostring(lock):match("([^%[]*)%[([^%]]*)")
	if Config[playerSteamID] == "set" then
		local newid, newowner = tostring(ConfigVars[playerSteamID]):match("([^|]+)|([^|]+)")
		for current, data in pairs(Data_DR.Doors) do
			if (tonumber(data.door) == tonumber(lockid)) then
				local owner = data.player
				local ownerid = data.id
				if (ownerid == newid) then
					local message = FormatMessage(self:Lang(player, "DoorAlreadySet"), { owner = owner.." ("..ownerid..")" })
					self:RustMessage(player, message)
					return
				end
				data.player, data.id, data.shared = newowner, newid, "false"
				self:SaveDataFile(2)
				local message = FormatMessage(self:Lang(player, "DoorSetExists"), { owner = owner.." ("..ownerid..")", newowner = newowner.." ("..newid..")" })
				self:RustMessage(player, message)
				return
			end
		end
		local newDoor = {["door"] = tonumber(lockid), ["player"] = newowner, ["id"] = newid, ["shared"] = "false"}
		table.insert(Data_DR.Doors, newDoor)
		self:SaveDataFile(2)
		local message = FormatMessage(self:Lang(player, "DoorSetNew"), { owner = newowner.." ("..newid..")" })
		self:RustMessage(player, message)
		return
	end
	if Config[playerSteamID] == "ashare" then
		for current, data in pairs(Data_DR.Doors) do
			if (tonumber(data.door) == tonumber(lockid)) then
				if (data.shared == "true") then return end
				data.shared = "true"
				self:SaveDataFile(2)
				local message = FormatMessage(self:Lang(player, "AdminDoor"), { owner = data.player.." ("..data.id..")", status = self:Lang(player, "DoorShared") })
				self:RustMessage(player, message)
				return
			end
		end
	end
	if Config[playerSteamID] == "aunshare" then
		for current, data in pairs(Data_DR.Doors) do
			if (tonumber(data.door) == tonumber(lockid)) then
				if (data.shared == "false") then return end
				data.shared = "false"
				self:SaveDataFile(2)
				local message = FormatMessage(self:Lang(player, "AdminDoor"), { owner = data.player.." ("..data.id..")", status = self:Lang(player, "DoorUnshared") })
				self:RustMessage(player, message)
				return
			end
		end
	end
	if Config[playerSteamID] == "aremove" then
		for current, data in pairs(Data_DR.Doors) do
			if (tonumber(data.door) == tonumber(lockid)) then
				table.remove(Data_DR.Doors, current)
				self:SaveDataFile(2)
				local message = FormatMessage(self:Lang(player, "AdminDoor"), { owner = data.player.." ("..data.id..")", status = self:Lang(player, "DoorRemoved") })
				self:RustMessage(player, message)
				return
			end
		end
	end
	if Config[playerSteamID] == "add" then
		for current, data in pairs(Data_DR.Doors) do
			if (tonumber(data.door) == tonumber(lockid)) then
				return
			end
		end
		if self:CheckBuildingBlocked(player) == true then return end
		local newDoor = {["door"] = tonumber(lockid), ["player"] = player.displayName, ["id"] = playerSteamID, ["shared"] = "false"}
		table.insert(Data_DR.Doors, newDoor)
		self:SaveDataFile(2)
		local message = FormatMessage(self:Lang(player, "DoorStatus"), { status = self:Lang(player, "DoorAdded") })
		self:RustMessage(player, message)
		return
	end
	if Config[playerSteamID] == "share" then
		if self:CheckBuildingBlocked(player) == true then return end
		local lockname, lockid = tostring(lock):match("([^%[]*)%[([^%]]*)")
		for current, data in pairs(Data_DR.Doors) do
			if (tonumber(data.door) == tonumber(lockid)) then
				if (data.id ~= playerSteamID) then return end
				if (data.shared == "true") then return end
				data.shared = "true"
				self:SaveDataFile(2)
				local message = FormatMessage(self:Lang(player, "DoorStatus"), { status = self:Lang(player, "DoorShared") })
				self:RustMessage(player, message)
				return
			end
		end
		local newDoor = {["door"] = tonumber(lockid), ["player"] = player.displayName, ["id"] = playerSteamID, ["shared"] = "true"}
		table.insert(Data_DR.Doors, newDoor)
		self:SaveDataFile(2)
		local message = FormatMessage(self:Lang(player, "DoorStatus"), { status = self:Lang(player, "DoorShared") })
		self:RustMessage(player, message)
		return
	end
	if Config[playerSteamID] == "unshare" then
		if self:CheckBuildingBlocked(player) == true then return end
		local lockname, lockid = tostring(lock):match("([^%[]*)%[([^%]]*)")
		for current, data in pairs(Data_DR.Doors) do
			if (tonumber(data.door) == tonumber(lockid)) then
				if (data.id ~= playerSteamID) then return end
				if (data.shared == "false") then return end
				data.shared = "false"
				self:SaveDataFile(2)
				local message = FormatMessage(self:Lang(player, "DoorStatus"), { status = self:Lang(player, "DoorUnshared") })
				self:RustMessage(player, message)
				return
			end
		end
		local newDoor = {["door"] = tonumber(lockid), ["player"] = player.displayName, ["id"] = playerSteamID, ["shared"] = "false"}
		table.insert(Data_DR.Doors, newDoor)
		self:SaveDataFile(2)
		local message = FormatMessage(self:Lang(player, "DoorStatus"), { status = self:Lang(player, "DoorUnshared") })
		self:RustMessage(player, message)
		return
	end
	if Config[playerSteamID] == "transfer" then
		if self:CheckBuildingBlocked(player) == true then return end
		local newid, newowner = tostring(ConfigVars[playerSteamID]):match("([^|]+)|([^|]+)")
		for current, data in pairs(Data_DR.Doors) do
			if (tonumber(data.door) == tonumber(lockid)) then
				if (data.id == playerSteamID) then
					data.id, data.player = newid, newowner
					self:SaveDataFile(2)
					local _message = FormatMessage(self:Lang(player, "DoorTransfer"), { owner = newowner.." ("..newid..")" })
					local message = FormatMessage(self:Lang(player, "DoorStatus"), { status = _message })
					self:RustMessage(player, message)
					return
				end
			end
		end
	end
	if Config[playerSteamID] == "remove" then
		if self:CheckBuildingBlocked(player) == true then return end
		for current, data in pairs(Data_DR.Doors) do
			if (tonumber(data.door) == tonumber(lockid)) then
				if (data.id == playerSteamID) then
					table.remove(Data_DR.Doors, current)
					self:SaveDataFile(2)
					local message = FormatMessage(self:Lang(player, "DoorStatus"), { status = self:Lang(player, "DoorRemoved") })
					self:RustMessage(player, message)
					return
				end
			end
		end
	end
end

function PLUGIN:CheckPlayer(player, target, admin)
	local target = rust.FindPlayer(target)
	if not target then
		self:RustMessage(player, self:Lang(player, "NoPlayer"))
		return false
	end
	local targetName = target.displayName
	local targetSteamID = rust.UserIDFromPlayer(target)
	local playerSteamID = rust.UserIDFromPlayer(player)
	if not admin then
		if playerSteamID == targetSteamID then
			self:RustMessage(player, self:Lang(player, "Self"))
			return false
		end
	end
	return true, target, targetName, targetSteamID
end

function PLUGIN:CheckOwner(lock, player)
	local playerSteamID = rust.UserIDFromPlayer(player)
	if self:CheckCooldownOwner(player, 2) and permission.UserHasPermission(playerSteamID, "doorshare.owner") then
		local doorname, doorid = tostring(lock):match"([^%[]*)%[([^%]]*)"
		for current, data in pairs(Data_DR.Doors) do
			if tonumber(data.door) == tonumber(doorid) then
				local message = FormatMessage(self:Lang(player, "DoorOwner"), { player = data.player, id = data.id })
				self:RustMessage(player, message)
				CoolDownOwner[playerSteamID] = time.GetUnixTimestamp()
				break
			end
		end
	end
end

function PLUGIN:CheckCooldown(player)
	local playerSteamID = rust.UserIDFromPlayer(player)
	if not permission.UserHasPermission(playerSteamID, "doorshare.admin") and not permission.UserHasPermission(playerSteamID, "doorshare.nocd") then
		if CoolDown[playerSteamID] then
			local Timestamp = time.GetUnixTimestamp()
			if Timestamp - CoolDown[playerSteamID] < tonumber(self.Config.Settings.CommandCooldown) then
				local remaining = tonumber(self.Config.Settings.CommandCooldown) - (Timestamp - CoolDown[playerSteamID])
				local message = FormatMessage(self:Lang(player, "CoolDown"), { cooldown = remaining })
				self:RustMessage(player, message)
				return false
			end
		end
	end
	return true
end

function PLUGIN:CheckCooldownOwner(player)
	local playerSteamID = rust.UserIDFromPlayer(player)
	if CoolDownOwner[playerSteamID] then
		local Timestamp = time.GetUnixTimestamp()
		if Timestamp - CoolDownOwner[playerSteamID] < tonumber(self.Config.Settings.OwnerCooldown) then
			return false
		end
	end
	return true
end

function PLUGIN:CheckBuildingBlocked(player)
	if self.Config.Settings.BuildingBlocked == "true" and not player:CanBuild() then
		local playerSteamID = rust.UserIDFromPlayer(player)
		if not permission.UserHasPermission(playerSteamID, "doorshare.admin") then
			self:RustMessage(player, self:Lang(player, "BuildingBlocked"))
			return true
		end
	end
	return false
end

function PLUGIN:PlayDoorSound(player, lock)
	if lock.name == "assets/prefabs/locks/keypad/lock.code.prefab" then
		global.Effect.server.Run.methodarray[3]:Invoke(nil, util.TableToArray({"assets/prefabs/locks/keypad/effects/lock.code.lock.prefab", player.transform.position, UnityEngine.Vector3.get_up(), nil, true}))
	end
end

function PLUGIN:RustMessage(player, message)
	rust.SendChatMessage(player, "<size="..tonumber(self.Config.Settings.MessageSize)..">"..self:Lang(player, "Prefix")..message.."</size>")
end

function PLUGIN:SendHelpText(player)
	self:RustMessage(player, self:Lang(player, "Help"))
end