PLUGIN.Title        = "Administrator Spawn"
PLUGIN.Description  = "Manage administrator spawns and messages."
PLUGIN.Author       = "InSaNe8472"
PLUGIN.Version      = V(1,1,0)
PLUGIN.ResourceID   = 1644

local popupApi

function PLUGIN:Init()
	permission.RegisterPermission("adminspawn.hide", self.Plugin)
	permission.RegisterPermission("adminspawn.warn", self.Plugin)
	permission.RegisterPermission("adminspawn.blacklist", self.Plugin)
	permission.RegisterPermission("adminspawn.all", self.Plugin)
	permission.RegisterPermission("adminspawn.ammunition", self.Plugin)
	permission.RegisterPermission("adminspawn.attire", self.Plugin)
	permission.RegisterPermission("adminspawn.construction", self.Plugin)
	permission.RegisterPermission("adminspawn.food", self.Plugin)
	permission.RegisterPermission("adminspawn.items", self.Plugin)
	permission.RegisterPermission("adminspawn.medical", self.Plugin)
	permission.RegisterPermission("adminspawn.misc", self.Plugin)
	permission.RegisterPermission("adminspawn.resources", self.Plugin)
	permission.RegisterPermission("adminspawn.tool", self.Plugin)
	permission.RegisterPermission("adminspawn.traps", self.Plugin)
	permission.RegisterPermission("adminspawn.weapon", self.Plugin)
	command.AddChatCommand("giveme", self.Plugin, "cmdAdminSpawn")
	command.AddChatCommand("give", self.Plugin, "cmdAdminSpawn")
	command.AddChatCommand("giveall", self.Plugin, "cmdAdminSpawn")
	self:LoadDefaultConfig()
end

function PLUGIN:LoadDefaultConfig()
	self.Config.Settings = self.Config.Settings or {}
	self.Config.Messages = self.Config.Messages or {}
	self.Config.Blacklist = self.Config.Blacklist or {}
	self.Config.Settings.Console = self.Config.Settings.Console or "true"
	self.Config.Settings.Log = self.Config.Settings.Log or "true"
	self.Config.Settings.Popup = self.Config.Settings.Popup or "true"
	self.Config.Settings.Blacklist = self.Config.Settings.Blacklist or "false"
	self.Config.Settings.WarnChat = self.Config.Settings.WarnChat or "false"
	self.Config.Settings.WarnUser = self.Config.Settings.WarnUser or "true"
	self.Config.Settings.WarnGiveTo = self.Config.Settings.WarnGiveTo or "true"
	self.Config.Settings.OneHundred = self.Config.Settings.OneHundred or "100"
	self.Config.Settings.OneThousand = self.Config.Settings.OneThousand or "1000"
	self.Config.Settings.MessageSize = self.Config.Settings.MessageSize or "12"
	self.Config.Settings.Prefix = self.Config.Settings.Prefix or "[<color=#cd422b> Admin Spawn </color>]"
	self.Config.Messages.NoPermission = self.Config.Messages.NoPermission or "You do not have permission to spawn <color=#cd422b>{item}</color>."
	self.Config.Messages.NoPlayer = self.Config.Messages.NoPlayer or "Player not found.  Please try again."
	self.Config.Messages.MultiPlayer = self.Config.Messages.MultiPlayer or "Multiple players found.  Provide a more specific username."
	self.Config.Messages.Invalid = self.Config.Messages.Invalid or "Invalid item <color=#ffd479>{item}</color>."
	self.Config.Messages.AdminSpawn = self.Config.Messages.AdminSpawn or "<color=#cd422b>{player}</color> gave <color=#ffd479>{amount} {item}</color> to <color=#cd422b>{target}</color>."
	self.Config.Messages.GiveTo = self.Config.Messages.GiveTo or "Administrator gave you <color=#ffd479>{amount} {item}</color>."
	self.Config.Messages.GivePlayer = self.Config.Messages.GivePlayer or "<color=#cd422b>{player}</color> received <color=#ffd479>{amount} {item}</color>."
	self.Config.Blacklist.Items = self.Config.Blacklist.Items or {
		"rifle.ak",
		"supply.signal"
	}
	if not tonumber(self.Config.Settings.OneHundred) or tonumber(self.Config.Settings.OneHundred) < 1 then self.Config.Settings.OneHundred = "100" end
	if not tonumber(self.Config.Settings.OneThousand) or tonumber(self.Config.Settings.OneThousand) < 1 then self.Config.Settings.OneThousand = "1000" end
	if not tonumber(self.Config.Settings.MessageSize) or tonumber(self.Config.Settings.MessageSize) < 1 then self.Config.Settings.MessageSize = "12" end
	self:SaveConfig()
end

function PLUGIN:OnServerInitialized()
	popupApi = plugins.Find("PopupNotifications") or false
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

local function StripMessage(message)
	local message = message:gsub("<color=%p*%w*>", "")
	message = message:gsub("</color>", "")
	return message
end

local function StripSpace(message)
	local message = message:gsub(" ", "")
	return message
end

function PLUGIN:cmdAdminSpawn(player, cmd, args)
	if cmd:lower() == "giveme" then
		local silent = self:CheckSilent(player)
		if silent then
			rust.SendChatMessage(player, "Unknown command: "..cmd)
			return
		end
		if args.Length < 1 then
			self:RustMessage(player, self.Config.Settings.Prefix.." Usage: /giveme <item> [quantity]")
			return
		end
		local ItemName, SpawnAmt = args[0]:lower(), 1
		local ItemID = self:GetItemID(ItemName)
		local auth = self:CheckAuth(player, ItemID, ItemName, 1)
		if not auth then return false end
		if args.Length > 1 and tonumber(args[1]) then SpawnAmt = args[1] end
		local isBP = false
		if string.sub(ItemName, -3) == " bp" then isBP = true end
		local item = global.ItemManager.CreateByItemID(ItemID, tonumber(SpawnAmt), isBP)
		item:MoveToContainer(player.inventory.containerMain, -1)
		if self.Config.Settings.WarnChat ~= "true" then
			local message = FormatMessage(self.Config.Messages.GivePlayer, { player = player.displayName, amount = SpawnAmt, item = item.info.displayName.translated })
			self:RustMessage(player, self.Config.Settings.Prefix.." "..message)
		end
		self:WarnPlayers(player, player.displayName, SpawnAmt, item, player.displayName)
	end
	if cmd:lower() == "give" then
		local silent = self:CheckSilent(player)
		if silent then
			rust.SendChatMessage(player, "Unknown command: "..cmd)
			return
		end
		if args.Length < 2 then
			self:RustMessage(player, self.Config.Settings.Prefix.." Usage: /give <player> <item> [quantity]")
			return
		end
		local target, ItemName, SpawnAmt = args[0], args[1]:lower(), 1
		local found, targetplayer, targetname = self:CheckPlayer(player, target, 1)
		if not found then return false end
		local ItemID = self:GetItemID(ItemName)
		local auth = self:CheckAuth(player, ItemID, ItemName, 1)
		if not auth then return false end
		if args.Length > 2 and tonumber(args[2]) then SpawnAmt = args[2] end
		local isBP = false
		if string.sub(ItemName, -3) == " bp" then isBP = true end
		local item = global.ItemManager.CreateByItemID(ItemID, tonumber(SpawnAmt), isBP)
		item:MoveToContainer(targetplayer.inventory.containerMain, -1)
		if self.Config.Settings.WarnChat ~= "true" then
			local message = FormatMessage(self.Config.Messages.GivePlayer, { player = targetname, amount = SpawnAmt, item = item.info.displayName.translated })
			self:RustMessage(player, self.Config.Settings.Prefix.." "..message)
		end
		self:WarnPlayers(player, player.displayName, SpawnAmt, item, targetname)
		if player ~= targetplayer and self.Config.Settings.WarnChat ~= "true" and self.Config.Settings.WarnGiveTo == "true" then
			local message = FormatMessage(self.Config.Messages.GiveTo, { amount = SpawnAmt, item = item.info.displayName.translated })
			self:RustMessage(targetplayer, self.Config.Settings.Prefix.." "..message)
			self:ShowPopup(targetplayer, message)
		end
	end
	if cmd:lower() == "giveall" then
		local silent = self:CheckSilent(player)
		if silent then
			rust.SendChatMessage(player, "Unknown command: "..cmd)
			return
		end
		if args.Length < 1 then
			self:RustMessage(player, self.Config.Settings.Prefix.." Usage: /giveall <item> [quantity]")
			return
		end
		local ItemName, SpawnAmt = args[0]:lower(), 1
		local ItemID = self:GetItemID(ItemName)
		local auth = self:CheckAuth(player, ItemID, ItemName, 1)
		if not auth then return false end
		if args.Length > 1 and tonumber(args[1]) then SpawnAmt = args[1] end
		local isBP = false
		if string.sub(ItemName, -3) == " bp" then isBP = true end
		local item_ = global.ItemManager.CreateByItemID(ItemID, tonumber(SpawnAmt), isBP)
		local message = FormatMessage(self.Config.Messages.GiveTo, { amount = SpawnAmt, item = item_.info.displayName.translated })
		local players = global.BasePlayer.activePlayerList:GetEnumerator()
		local item
		while players:MoveNext() do
			item = global.ItemManager.CreateByItemID(ItemID, tonumber(SpawnAmt), isBP)
			item:MoveToContainer(players.Current.inventory.containerMain, -1)
			if player ~= players.Current and self.Config.Settings.WarnChat ~= "true" and self.Config.Settings.WarnGiveTo == "true" then
				self:RustMessage(players.Current, self.Config.Settings.Prefix.." "..message)
				self:ShowPopup(players.Current, message)
			end
		end
		local players = global.BasePlayer.sleepingPlayerList:GetEnumerator()
		while players:MoveNext() do
			item = global.ItemManager.CreateByItemID(ItemID, tonumber(SpawnAmt), isBP)
			item:MoveToContainer(players.Current.inventory.containerMain, -1)
		end
		if self.Config.Settings.WarnChat ~= "true" then
			local message = FormatMessage(self.Config.Messages.GivePlayer, { player = "All players", amount = SpawnAmt, item = item.info.displayName.translated })
			self:RustMessage(player, self.Config.Settings.Prefix.." "..message)
		end
		self:WarnPlayers(player, player.displayName, SpawnAmt, item, "all players")
	end
	return
end

function PLUGIN:OnRunCommand(arg)
	if arg and arg.cmd then
		local cmd = arg.cmd.name
		if cmd == "givebp" or cmd == "givearm" or cmd == "giveid" then
			if not arg.connection then
				UnityEngine.Debug.LogWarning.methodarray[0]:Invoke(nil, util.TableToArray({ StripMessage(StripSpace(self.Config.Settings.Prefix)).." "..cmd..": not accessible from server console" }))
				return false
			end
			local player = arg.connection.player
			local silent = self:CheckSilent(player)
			if silent then return false end
			if arg.Args then
				local ItemID = arg.Args[0]:lower()
				if not tonumber(ItemID) then ItemID = self:GetItemID(ItemID) end
				local auth, ItemName = self:CheckAuth(player, ItemID, arg.Args[0], 3)
				if not auth then return false end
				local SpawnAmt = 1
				local item
				if cmd == "givebp" then
					item = global.ItemManager.CreateByItemID(tonumber(ItemID), SpawnAmt, true)
					item:MoveToContainer(player.inventory.containerMain, -1)
					ItemName = ItemName.." bp"
				end
				if cmd == "givearm" then
					item = global.ItemManager.CreateByItemID(tonumber(ItemID), SpawnAmt, false)
					local Belt = player.inventory.containerBelt.itemList:GetEnumerator()
					local BeltItems = 0
					while Belt:MoveNext() do
						if Belt.Current.position then
							BeltItems = BeltItems + 1
						end
					end
					if BeltItems < 6 then
						item:MoveToContainer(player.inventory.containerBelt, -1)
						else
						item:MoveToContainer(player.inventory.containerMain, -1)
					end
				end
				if cmd == "giveid" then
					if arg.Args.Length > 1 then SpawnAmt = tonumber(arg.Args[1]) end
					if SpawnAmt == 100 then SpawnAmt = tonumber(self.Config.Settings.OneHundred) end
					if SpawnAmt == 1000 then SpawnAmt = tonumber(self.Config.Settings.OneThousand) end
					item = global.ItemManager.CreateByItemID(tonumber(ItemID), SpawnAmt, false)
					item:MoveToContainer(player.inventory.containerMain, -1)
				end
				self:WarnPlayers(player, player.displayName, SpawnAmt, item, player.displayName)
				return false
				else
				if cmd == "givebp" then player:SendConsoleCommand("echo "..self.Config.Settings.Prefix.." Usage: givebp <item>") end
				if cmd == "givearm" then player:SendConsoleCommand("echo "..self.Config.Settings.Prefix.." Usage: givearm <item>") end
				if cmd == "giveid" then player:SendConsoleCommand("echo "..self.Config.Settings.Prefix.." Usage: giveid <item> [quantity]") end
				return false
			end
		end
		if cmd == "give" then
			if arg.connection then
				local player = arg.connection.player
				local silent = self:CheckSilent(player)
				if silent then return false end
				if not arg.Args or arg.ArgsStr == nil or arg.Args.Length < 1 then
					player:SendConsoleCommand("echo "..self.Config.Settings.Prefix.." Usage: give <item> [quantity]")
					return false
				end
				local ItemName, SpawnAmt = arg.Args[0]:lower(), 1
				local ItemID = self:GetItemID(ItemName)
				local auth = self:CheckAuth(player, ItemID, arg.Args[0], 2)
				if not auth then return false end
				if arg.Args.Length > 1 and tonumber(arg.Args[1]) then SpawnAmt = arg.Args[1] end
				local isBP = false
				if string.sub(ItemName, -3) == " bp" then isBP = true end
				local item = global.ItemManager.CreateByItemID(ItemID, tonumber(SpawnAmt), isBP)
				item:MoveToContainer(player.inventory.containerMain, -1)
				local message = FormatMessage(self.Config.Messages.GivePlayer, { player = player.displayName, amount = SpawnAmt, item = item.info.displayName.translated })
				player:SendConsoleCommand("echo "..self.Config.Settings.Prefix.." "..message)
				self:WarnPlayers(player, player.displayName, SpawnAmt, item, player.displayName)
				return false
				else
				UnityEngine.Debug.LogWarning.methodarray[0]:Invoke(nil, util.TableToArray({ StripMessage(StripSpace(self.Config.Settings.Prefix)).." give: not accessible from server console" }))
				return false
			end
		end
		if cmd == "giveto" then
			if arg.connection then
				local player = arg.connection.player
				local silent = self:CheckSilent(player)
				if silent then return false end
				if not arg.Args or arg.ArgsStr == nil or arg.Args.Length < 2 then
					player:SendConsoleCommand("echo "..self.Config.Settings.Prefix.." Usage: giveto <player> <item> [quantity]")
					return false
				end
				local target, ItemName, SpawnAmt = arg.Args[0], arg.Args[1]:lower(), 1
				local found, targetplayer, targetname = self:CheckPlayer(player, target, 2)
				if not found then return false end
				local ItemID = self:GetItemID(ItemName)
				local auth = self:CheckAuth(player, ItemID, arg.Args[1], 2)
				if not auth then return false end
				if arg.Args.Length > 2 and tonumber(arg.Args[2]) then SpawnAmt = arg.Args[2] end
				local isBP = false
				if string.sub(ItemName, -3) == " bp" then isBP = true end
				local item = global.ItemManager.CreateByItemID(ItemID, tonumber(SpawnAmt), isBP)
				item:MoveToContainer(targetplayer.inventory.containerMain, -1)
				local message = FormatMessage(self.Config.Messages.GivePlayer, { player = targetname, amount = SpawnAmt, item = item.info.displayName.translated })
				player:SendConsoleCommand("echo "..self.Config.Settings.Prefix.." "..message)
				self:WarnPlayers(player, player.displayName, SpawnAmt, item, targetname)
				if player ~= targetplayer and self.Config.Settings.WarnChat ~= "true" and self.Config.Settings.WarnGiveTo == "true" then
					local message = FormatMessage(self.Config.Messages.GiveTo, { amount = SpawnAmt, item = item.info.displayName.translated })
					self:RustMessage(targetplayer, self.Config.Settings.Prefix.." "..message)
					self:ShowPopup(targetplayer, message)
				end
				else
				if not arg.Args or arg.ArgsStr == nil or arg.Args.Length < 2 then
					UnityEngine.Debug.LogWarning.methodarray[0]:Invoke(nil, util.TableToArray({ StripMessage(StripSpace(self.Config.Settings.Prefix)).." Usage: giveto <player> <item> [quantity]" }))
					return false
				end
				local target, ItemName, SpawnAmt = arg.Args[0], arg.Args[1]:lower(), 1
				local found, targetplayer, targetname = self:CheckPlayer(nil, target, 3)
				if not found then return false end
				local ItemID = self:GetItemID(ItemName)
				if not ItemID then
					local message = FormatMessage(self.Config.Messages.Invalid, { item = ItemName })
					UnityEngine.Debug.LogWarning.methodarray[0]:Invoke(nil, util.TableToArray({ StripMessage(StripSpace(self.Config.Settings.Prefix).." "..message) }))
					return false
				end
				if arg.Args.Length > 2 and tonumber(arg.Args[2]) then SpawnAmt = arg.Args[2] end
				local isBP = false
				if string.sub(ItemName, -3) == " bp" then isBP = true end
				local item = global.ItemManager.CreateByItemID(ItemID, tonumber(SpawnAmt), isBP)
				item:MoveToContainer(targetplayer.inventory.containerMain, -1)
				local message = FormatMessage(self.Config.Messages.GivePlayer, { player = targetname, amount = SpawnAmt, item = item.info.displayName.translated })
				UnityEngine.Debug.LogWarning.methodarray[0]:Invoke(nil, util.TableToArray({ StripMessage(StripSpace(self.Config.Settings.Prefix).." "..message) }))
				self:WarnPlayers(nil, "Console", SpawnAmt, item, targetname)
				if self.Config.Settings.WarnChat ~= "true" and self.Config.Settings.WarnGiveTo == "true" then
					local message = FormatMessage(self.Config.Messages.GiveTo, { amount = SpawnAmt, item = item.info.displayName.translated })
					self:RustMessage(targetplayer, self.Config.Settings.Prefix.." "..message)
					self:ShowPopup(targetplayer, message)
				end
			end
			return false
		end
		if cmd == "giveall" then
			if arg.connection then
				local player = arg.connection.player
				local silent = self:CheckSilent(player)
				if silent then return false end
				if not arg.Args or arg.ArgsStr == nil or arg.Args.Length < 1 then
					player:SendConsoleCommand("echo "..self.Config.Settings.Prefix.." Usage: giveall <item> [quantity]")
					return false
				end
				local ItemName, SpawnAmt = arg.Args[0]:lower(), 1
				local ItemID = self:GetItemID(ItemName)
				local auth = self:CheckAuth(player, ItemID, arg.Args[0], 2)
				if not auth then return false end
				if arg.Args.Length > 1 and tonumber(arg.Args[1]) then SpawnAmt = arg.Args[1] end
				local isBP = false
				if string.sub(ItemName, -3) == " bp" then isBP = true end
				local item_ = global.ItemManager.CreateByItemID(ItemID, tonumber(SpawnAmt), isBP)
				local message = FormatMessage(self.Config.Messages.GiveTo, { amount = SpawnAmt, item = item_.info.displayName.translated })
				local players = global.BasePlayer.activePlayerList:GetEnumerator()
				local item
				while players:MoveNext() do
					item = global.ItemManager.CreateByItemID(ItemID, tonumber(SpawnAmt), isBP)
					item:MoveToContainer(players.Current.inventory.containerMain, -1)
					if player ~= players.Current and self.Config.Settings.WarnChat ~= "true" and self.Config.Settings.WarnGiveTo == "true" then
						self:RustMessage(players.Current, self.Config.Settings.Prefix.." "..message)
						self:ShowPopup(players.Current, message)
					end
				end
				local players = global.BasePlayer.sleepingPlayerList:GetEnumerator()
				while players:MoveNext() do
					item = global.ItemManager.CreateByItemID(ItemID, tonumber(SpawnAmt), isBP)
					item:MoveToContainer(players.Current.inventory.containerMain, -1)
				end
				local message = FormatMessage(self.Config.Messages.GivePlayer, { player = "All players", amount = SpawnAmt, item = item.info.displayName.translated })
				player:SendConsoleCommand("echo "..self.Config.Settings.Prefix.." "..message)
				self:WarnPlayers(player, player.displayName, SpawnAmt, item, "all players")
				else
				if not arg.Args or arg.ArgsStr == nil or arg.Args.Length < 1 then
					UnityEngine.Debug.LogWarning.methodarray[0]:Invoke(nil, util.TableToArray({ StripMessage(StripSpace(self.Config.Settings.Prefix)).." Usage: giveall <item> [quantity]" }))
					return false
				end
				local ItemName, SpawnAmt = arg.Args[0]:lower(), 1
				local ItemID = self:GetItemID(ItemName)
				if not ItemID then
					local message = FormatMessage(self.Config.Messages.Invalid, { item = ItemName })
					UnityEngine.Debug.LogWarning.methodarray[0]:Invoke(nil, util.TableToArray({ StripMessage(StripSpace(self.Config.Settings.Prefix).." "..message) }))
					return false
				end
				if arg.Args.Length > 1 and tonumber(arg.Args[1]) then SpawnAmt = arg.Args[1] end
				local isBP = false
				if string.sub(ItemName, -3) == " bp" then isBP = true end
				local item_ = global.ItemManager.CreateByItemID(ItemID, tonumber(SpawnAmt), isBP)
				local message = FormatMessage(self.Config.Messages.GiveTo, { amount = SpawnAmt, item = item_.info.displayName.translated })
				local players = global.BasePlayer.activePlayerList:GetEnumerator()
				local item
				while players:MoveNext() do
					item = global.ItemManager.CreateByItemID(ItemID, tonumber(SpawnAmt), isBP)
					item:MoveToContainer(players.Current.inventory.containerMain, -1)
					if self.Config.Settings.WarnChat ~= "true" and self.Config.Settings.WarnGiveTo == "true" then
						self:RustMessage(players.Current, self.Config.Settings.Prefix.." "..message)
						self:ShowPopup(players.Current, message)
					end
				end
				local players = global.BasePlayer.sleepingPlayerList:GetEnumerator()
				while players:MoveNext() do
					item = global.ItemManager.CreateByItemID(ItemID, tonumber(SpawnAmt), isBP)
					item:MoveToContainer(players.Current.inventory.containerMain, -1)
				end
				local message = FormatMessage(self.Config.Messages.GivePlayer, { player = "All players", amount = SpawnAmt, item = item.info.displayName.translated })
				UnityEngine.Debug.LogWarning.methodarray[0]:Invoke(nil, util.TableToArray({ StripMessage(StripSpace(self.Config.Settings.Prefix).." "..message) }))
				self:WarnPlayers(nil, "Console", SpawnAmt, item, "all players")
			end
			return false
		end
	end
end

function PLUGIN:GetItemID(ItemName)
	if string.sub(ItemName, -3) == " bp" then ItemName = string.sub(ItemName, 1, -4) end
	local ItemDefinition = global.ItemManager.FindItemDefinition.methodarray[1]
	local TableArray = util.TableToArray({ItemName})
	Item = ItemDefinition:Invoke(nil, TableArray)
	if Item then return Item.itemid end
	return nil
end

function PLUGIN:CheckSilent(player)
	local playerSteamID = rust.UserIDFromPlayer(player)
	if permission.UserHasPermission(playerSteamID, "adminspawn.hide") then return false end
	if permission.UserHasPermission(playerSteamID, "adminspawn.warn") then return false end
	if permission.UserHasPermission(playerSteamID, "adminspawn.blacklist") then return false end
	if permission.UserHasPermission(playerSteamID, "adminspawn.all") then return false end
	if permission.UserHasPermission(playerSteamID, "adminspawn.ammunition") then return false end
	if permission.UserHasPermission(playerSteamID, "adminspawn.attire") then return false end
	if permission.UserHasPermission(playerSteamID, "adminspawn.construction") then return false end
	if permission.UserHasPermission(playerSteamID, "adminspawn.food") then return false end
	if permission.UserHasPermission(playerSteamID, "adminspawn.items") then return false end
	if permission.UserHasPermission(playerSteamID, "adminspawn.medical") then return false end
	if permission.UserHasPermission(playerSteamID, "adminspawn.misc") then return false end
	if permission.UserHasPermission(playerSteamID, "adminspawn.resources") then return false end
	if permission.UserHasPermission(playerSteamID, "adminspawn.tool") then return false end
	if permission.UserHasPermission(playerSteamID, "adminspawn.traps") then return false end
	if permission.UserHasPermission(playerSteamID, "adminspawn.weapon") then return false end
	return true
end

function PLUGIN:CheckAuth(player, ItemID, ItemName, call)
	if not ItemID then
		local message = FormatMessage(self.Config.Messages.Invalid, { item = ItemName })
		if call == 1 then self:RustMessage(player, self.Config.Settings.Prefix.." "..message) end
		if call == 2 then player:SendConsoleCommand("echo "..self.Config.Settings.Prefix.." "..message) end
		if call == 3 then
			self:RustMessage(player, self.Config.Settings.Prefix.." "..message)
			player:SendConsoleCommand("echo "..self.Config.Settings.Prefix.." "..message)
		end
		return false
	end
	local ItemDefinition = global.ItemManager.FindItemDefinition.methodarray[0]
	local TableArray = util.TableToArray({ItemID})
	util.ConvertAndSetOnArray(TableArray, 0, ItemID, System.Int32._type)
	local Item = ItemDefinition:Invoke(nil, TableArray)
	if not Item then
		local message = FormatMessage(self.Config.Messages.Invalid, { item = ItemName })
		if call == 1 then self:RustMessage(player, self.Config.Settings.Prefix.." "..message) end
		if call == 2 then player:SendConsoleCommand("echo "..self.Config.Settings.Prefix.." "..message) end
		if call == 3 then
			self:RustMessage(player, self.Config.Settings.Prefix.." "..message)
			player:SendConsoleCommand("echo "..self.Config.Settings.Prefix.." "..message)
		end
		return false
	end
	local ItemName = Item.shortname
	local ItemCat = tostring(Item.category):match("([^:]+)"):lower()
	local playerSteamID = rust.UserIDFromPlayer(player)
	if not permission.UserHasPermission(playerSteamID, "adminspawn.all") and not permission.UserHasPermission(playerSteamID, "adminspawn."..ItemCat) then
		local message = FormatMessage(self.Config.Messages.NoPermission, { item = ItemName })
		if call == 1 then self:RustMessage(player, self.Config.Settings.Prefix.." "..message) end
		if call == 2 then player:SendConsoleCommand("echo "..self.Config.Settings.Prefix.." "..message) end
		if call == 3 then
			self:RustMessage(player, self.Config.Settings.Prefix.." "..message)
			player:SendConsoleCommand("echo "..self.Config.Settings.Prefix.." "..message)
		end
		return false
	end
	if self.Config.Settings.Blacklist == "true" then
		if not permission.UserHasPermission(playerSteamID, "adminspawn.blacklist") then
			if self.Config.Blacklist.Items[1] then
				local i = 1
				while self.Config.Blacklist.Items[i] do
					if ItemName:lower() == self.Config.Blacklist.Items[i]:lower() then
						local message = FormatMessage(self.Config.Messages.NoPermission, { item = ItemName })
						if call == 1 then self:RustMessage(player, self.Config.Settings.Prefix.." "..message) end
						if call == 2 then player:SendConsoleCommand("echo "..self.Config.Settings.Prefix.." "..message) end
						if call == 3 then
							self:RustMessage(player, self.Config.Settings.Prefix.." "..message)
							player:SendConsoleCommand("echo "..self.Config.Settings.Prefix.." "..message)
						end
						return false
					end
					i = i + 1
				end
			end
		end
	end
	return true, ItemName
end

function PLUGIN:CheckPlayer(player, target, call)
	local numFound, targetPlayerTbl = FindPlayer(target, true)
	if numFound == 0 then
		if call == 1 then self:RustMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.NoPlayer) end
		if call == 2 then player:SendConsoleCommand("echo "..self.Config.Settings.Prefix.." "..self.Config.Messages.NoPlayer) end
		if call == 3 then UnityEngine.Debug.LogWarning.methodarray[0]:Invoke(nil, util.TableToArray({ StripMessage(StripSpace(self.Config.Settings.Prefix).." "..self.Config.Messages.NoPlayer) })) end
		return false
	end
	if numFound > 1 then
		local targetNameString = ""
		for i = 1, numFound do
			targetNameString = targetNameString..targetPlayerTbl[i].displayName..", "
		end
		if call == 1 then self:RustMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.MultiPlayer) end
		if call == 2 then player:SendConsoleCommand("echo "..self.Config.Settings.Prefix.." "..self.Config.Messages.MultiPlayer) end
		if call == 3 then UnityEngine.Debug.LogWarning.methodarray[0]:Invoke(nil, util.TableToArray({ StripMessage(StripSpace(self.Config.Settings.Prefix).." "..self.Config.Messages.MultiPlayer) })) end
		return false
	end
	local targetPlayer = targetPlayerTbl[1]
	local targetName = targetPlayer.displayName
	return true, targetPlayer, targetName
end

function PLUGIN:WarnPlayers(player, playerName, SpawnAmt, item, target)
	self:Console(FormatMessage(self.Config.Messages.AdminSpawn, { player = playerName, amount = SpawnAmt, item = item.info.displayName.translated, target = target }))
	if self.Config.Settings.Log == "true" then self:LogEvent(StripMessage(FormatMessage(self.Config.Messages.AdminSpawn, { player = playerName, amount = SpawnAmt, item = item.info.displayName.translated, target = target }))) end
	if player ~= nil and player then
		local playerSteamID = rust.UserIDFromPlayer(player)
		if permission.UserHasPermission(playerSteamID, "adminspawn.hide") then return end
	end
	if self.Config.Settings.WarnChat == "true" then
		local message = FormatMessage(self.Config.Messages.AdminSpawn, { player = playerName, amount = SpawnAmt, item = item.info.displayName.translated, target = target })
		self:RustBroadcast(self.Config.Settings.Prefix.." "..message)
		else
		if self.Config.Settings.WarnUser == "true" then
			local message = FormatMessage(self.Config.Messages.AdminSpawn, { player = playerName, amount = SpawnAmt, item = item.info.displayName.translated, target = target })
			local players = global.BasePlayer.activePlayerList:GetEnumerator()
			while players:MoveNext() do
				if players.Current ~= player then
					local targetSteamID = rust.UserIDFromPlayer(players.Current)
					if permission.UserHasPermission(targetSteamID, "adminspawn.warn") then
						self:RustMessage(players.Current, self.Config.Settings.Prefix.." "..message)
					end
				end
			end
		end
	end
end

function PLUGIN:Console(message)
	if self.Config.Settings.Console == "true" then
		UnityEngine.Debug.LogWarning.methodarray[0]:Invoke(nil, util.TableToArray({ StripMessage(StripSpace(self.Config.Settings.Prefix).." "..message) }))
	end
end

function PLUGIN:ShowPopup(player, message)
	if self.Config.Settings.Popup == "true" and popupApi then
		popupApi:CallHook("CreatePopupNotification", message, player)
	end
end

function PLUGIN:RustMessage(player, message)
	rust.SendChatMessage(player, "<size="..tonumber(self.Config.Settings.MessageSize)..">"..message.."</size>")
end

function PLUGIN:RustBroadcast(message)
	rust.BroadcastChat("<size="..tonumber(self.Config.Settings.MessageSize)..">"..message.."</size>")
end

function PLUGIN:LogEvent(logdata)
	ConVar.Server.Log("oxide/logs/AdminSpawn.txt", logdata)
end