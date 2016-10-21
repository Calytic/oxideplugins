PLUGIN.Title        = "Bank Manager"
PLUGIN.Description  = "Allows players to deposit and withdraw items from a bank."
PLUGIN.Author       = "InSaNe8472"
PLUGIN.Version      = V(1,1,2)
PLUGIN.ResourceID   = 1331

local DataFile_PB = "BankManager_PlayerBank"
local DataFile_PS = "BankManager_PlayerConfig"
local DataFile_CB = "BankManager_ClanBank"
local DataFile_CC = "BankManager_ClanConfig"
local Data_PB = {}
local Data_PS = {}
local Data_CB = {}
local Data_CC = {}
local Bank = {}
local BankOpened = {}
local Shared = {}
local BankUser = {}
local BankItem = {}
local CoolDown = {}
local ClanPlugin = "Clans"
local clans
local ClanBank = {}
local ClanName = {}
local ClanUser = {}
local Owner = {}
local ProximityPlayer = {}
local ProximityClan = {}

function PLUGIN:Init()
	permission.RegisterPermission("bankmanager.use", self.Plugin)
	permission.RegisterPermission("bankmanager.share", self.Plugin)
	permission.RegisterPermission("bankmanager.admin", self.Plugin)
	command.AddChatCommand("bank", self.Plugin, "cmdBank")
	self:LoadDataFile()
	self:LoadDefaultConfig()
	self:LoadDefaultLang()
end

function PLUGIN:LoadDefaultConfig()
	self.Config.Settings = self.Config.Settings or {}
	self.Config.Clan = self.Config.Clan or {}
	self.Config.NPC = self.Config.NPC or {}
	self.Config.Defaults = self.Config.Defaults or {}
	self.Config.Items = self.Config.Items or {}
	self.Config.Settings.Enabled = self.Config.Settings.Enabled or "true"
	self.Config.Settings.ShareEnabled = self.Config.Settings.ShareEnabled or "true"
	self.Config.Settings.MessageSize = self.Config.Settings.MessageSize or "12"
	self.Config.Settings.Ground = self.Config.Settings.Ground or "true"
	self.Config.Settings.Tier = self.Config.Settings.Tier or "-1"
	self.Config.Settings.BuildingBlocked = self.Config.Settings.BuildingBlocked or "true"
	self.Config.Settings.PerformItemCheck = self.Config.Settings.PerformItemCheck or "true"
	self.Config.Settings.UsePermissions = self.Config.Settings.UsePermissions or "true"
	self.Config.Settings.MaxBank = self.Config.Settings.MaxBank or "15"
	self.Config.Settings.MaxShare = self.Config.Settings.MaxShare or "10"
	self.Config.Settings.KeepDurability = self.Config.Settings.KeepDurability or "true"
	self.Config.Settings.Cooldown = self.Config.Settings.Cooldown or "3"
	self.Config.Settings.Radius = self.Config.Settings.Radius or "5"
	self.Config.Clan.Enabled = self.Config.Clan.Enabled or "true"
	self.Config.Clan.MinMembers = self.Config.Clan.MinMembers or "3"
	self.Config.Clan.MaxBank = self.Config.Clan.MaxBank or "15"
	self.Config.Clan.KeepDurability = self.Config.Clan.KeepDurability or "true"
	self.Config.Clan.Cooldown = self.Config.Clan.Cooldown or "3"
	self.Config.NPC.Enabled = self.Config.NPC.Enabled or "false"
	self.Config.NPC.MustInteract = self.Config.NPC.MustInteract or "true"
	self.Config.NPC.PlayerBankName = self.Config.NPC.PlayerBankName or "Player Bank"
	self.Config.NPC.ClanBankName = self.Config.NPC.ClanBankName or "Clan Bank"
	self.Config.NPC.CheckBuildingBlock = self.Config.NPC.CheckBuild or "false"
	self.Config.NPC.CheckOnGround = self.Config.NPC.CheckGround or "false"
	self.Config.NPC.CheckRadius = self.Config.NPC.CheckRadius or "false"
	self.Config.Defaults.ForceUpdate = self.Config.Defaults.ForceUpdate or "false"
	self.Config.Defaults.Items = self.Config.Defaults.Items or {
		"Ammunition:0:2:1000:0:2:1000",
		"Attire:0:2:1000:0:2:1000",
		"Construction:0:2:1000:0:2:1000",
		"Food:0:2:1000:0:2:1000",
		"Items:0:2:1000:0:2:1000",
		"Medical:0:2:1000:0:2:1000",
		"Misc:0:2:1000:0:2:1000",
		"Resources:1:2:1000:1:2:1000",
		"Tool:0:2:1000:0:2:1000",
		"Traps:0:2:1000:0:2:1000",
		"Unknown:0:2:1000:0:2:1000",
		"Weapon:0:2:1000:0:2:1000"
	}
	self.Config.CustomPermissions = self.Config.CustomPermissions or {
		{["Permission"] = "bankmanager.vip1", ["MaxBank"] = "20", ["MaxShare"] = "20", ["Items"] = {"wood:1:3:2000"}},
		{["Permission"] = "bankmanager.vip2", ["MaxBank"] = "30", ["MaxShare"] = "30", ["Items"] = {"wood:1:3:3000"}}
	}
	if not tonumber(self.Config.Settings.MessageSize) or tonumber(self.Config.Settings.MessageSize) < 1 then self.Config.Settings.MessageSize = "12" end
	if not tonumber(self.Config.Settings.Tier) or tonumber(self.Config.Settings.Tier) < -1 or tonumber(self.Config.Settings.Tier) > 4 then self.Config.Settings.Tier = "-1" end
	if not tonumber(self.Config.Settings.MaxBank) or tonumber(self.Config.Settings.MaxBank) < 1 or tonumber(self.Config.Settings.MaxBank) > 30 then self.Config.Settings.MaxBank = "15" end
	if not tonumber(self.Config.Settings.MaxShare) or tonumber(self.Config.Settings.MaxShare) < 1 then self.Config.Settings.MaxShare = "10" end
	if not tonumber(self.Config.Settings.Cooldown) or tonumber(self.Config.Settings.Cooldown) < 3 then self.Config.Settings.Cooldown = "3" end
	if not tonumber(self.Config.Settings.Radius) or tonumber(self.Config.Settings.Radius) < 5 then self.Config.Settings.Cooldown = "5" end
	if not tonumber(self.Config.Clan.MinMembers) or tonumber(self.Config.Clan.MinMembers) < 1 then self.Config.Clan.MinMembers = "3" end
	if not tonumber(self.Config.Clan.MaxBank) or tonumber(self.Config.Clan.MaxBank) < 1 or tonumber(self.Config.Clan.MaxBank) > 30 then self.Config.Clan.MaxBank = "15" end
	if not tonumber(self.Config.Clan.Cooldown) or tonumber(self.Config.Clan.Cooldown) < 3 then self.Config.Clan.Cooldown = "3" end
	self:SaveConfig()
	if self.Config.CustomPermissions then
		for current, data in pairs(self.Config.CustomPermissions) do
			permission.RegisterPermission(data.Permission, self.Plugin)
		end
	end
end

function PLUGIN:LoadDefaultLang()
	lang.RegisterMessages(util.TableToLangDict({
		["AdminMenu"] = "\n	<color=#ffd479>/bank toggle <bank | clan | share | npc></color> - Enable or disable bank system\n"..
		"	<color=#ffd479>/bank admin <bank | clan> <player | clan></color> - Open player or clan bank",
		["BankBox"] = "This box is a bank owned by another player and cannot be opened or destroyed.",
		["BankClosed"] = "Bank closed for <color=#cd422b>{player}</color>.",
		["BankDisabled"] = "Your open bank has been saved and closed. The bank system has been reloaded, unloaded or disabled by an administrator.",
		["BankOpened"] = "Bank opened for <color=#cd422b>{player}</color>.",
		["BuildingBlocked"] = "You cannot access a bank in building blocked areas.",
		["ChangedClanStatus"] = "Clan <color=#cd422b>{clan}'s</color> group <color=#ffd479>{group}</color> bank access <color=#cd422b>{status}</color>.",
		["ChangedFeature"] = "Bank feature <color=#cd422b>{group}</color> now <color=#cd422b>{status}</color>.",
		["ChangedStatus"] = "Bank group <color=#cd422b>{group}</color> now <color=#cd422b>{status}</color>.",
		["CheckGround"] = "You may only access a bank while standing on the ground.",
		["CheckRadius"] = "You cannot access a bank within <color=#cd422b>{range} meters</color> of another online player. Current nearest range is <color=#cd422b>{current} meters</color>.",
		["CheckTier"] = "You may only access a bank while standing on the ground or on tier <color=#cd422b>{tier}</color> or highier foundations.",
		["ClanBankClosed"] = "Bank closed for clan <color=#cd422b>{clan}</color>.",
		["ClanBankOpened"] = "Bank opened for clan <color=#cd422b>{clan}</color>.",
		["ClanError"] = "An error occured while retrieving your clan information.",
		["ClanNoPermission"] = "You do not have permission to access <color=#cd422b>{clan}'s</color> bank.",
		["ClanOccupied"] = "Clan <color=#cd422b>{clan}'s</color> bank is currently occupied by <color=#cd422b>{player}</color> ({id}).",
		["ClanOwner"] = "Your clan, <color=#cd422b>{clan}</color>, currently has <color=#ffd479>{members} member(s)</color>. You must have minimum <color=#cd422b>{required} members</color> to use clan bank. As owner, you may access existing banked items. They will be returned to you upon closing your inventory.",
		["CoolDown"] = "You must wait <color=#cd422b>{cooldown} seconds</color> before trying that.",
		["DeleteAll"] = "You no longer share your bank with anyone. (<color=#cd422b>{entries}</color> player(s) removed)",
		["Disabled"] = "disabled",
		["Enabled"] = "enabled",
		["GroupPlayer"] = "player",
		["GroupClan"] = "clan",
		["GroupShare"] = "sharing",
		["GroupNPC"] = "npc",
		["GroupMember"] = "member",
		["GroupModerator"] = "moderator",
		["Help"] = "<color=#ffd479>/bank</color> - Allows players to deposit and withdraw items from a bank",
		["InfoClan"] = "\n	Clan: <color=#ffd479>{i1}</color>\n"..
		"	Rank: <color=#ffd479>{i2}</color>\n"..
		"	Members: <color=#ffd479>{i3}</color>",
		["InfoItem"] = "\n	Your limits for <color=#cd422b>{i1}</color>:\n"..
		"	[Player] Bankable: <color=#ffd479>{i2}</color>\n"..
		"	[Player] Maximum Deposit: <color=#ffd479>{i3}</color>\n"..
		"	[Player] Maximum Stack: <color=#ffd479>{i4}</color>\n"..
		"	[Clan] Bankable: <color=#ffd479>{i5}</color>\n"..
		"	[Clan] Maximum Deposit: <color=#ffd479>{i6}</color>\n"..
		"	[Clan] Maximum Stack: <color=#ffd479>{i7}</color>",
		["Initialize1"] = "{prefix} Item check not performed, items may be missing or invalid",
		["Initialize2"] = "{prefix} Performing item check...",
		["Initialize3"] = "{prefix} Force update configuration found, updating items...",
		["Initialize4"] = "{prefix} New item(s) added: {items}",
		["Initialize5"] = "{prefix} No new items added",
		["Initialize6"] = "{prefix} Invalid item(s) removed: {items}",
		["Initialize7"] = "{prefix} No invalid items removed",
		["Initialize8"] = "{prefix} Duplicate item(s) removed: {items}",
		["Initialize9"] = "{prefix} No duplicate items removed",
		["LangError"] = "Language error: ",
		["LimitsBank"] = "\n	Player Bank Enabled: <color=#ffd479>{l1}</color>\n"..
		"	Building Blocked: <color=#ffd479>{l2}</color>\n"..
		"	Your Max Bank: <color=#ffd479>{l3} items</color>\n"..
		"	Your Max Share: <color=#ffd479>{l4} players</color>\n"..
		"	Keep Durability: <color=#ffd479>{l5}</color>\n"..
		"	Cooldown: <color=#ffd479>{l6} seconds</color>",
		["LimitsClan"] = "\n	Clan Bank Enabled: <color=#ffd479>{l1}</color>\n"..
		"	Minimum Members: <color=#ffd479>{l2} members</color>\n"..
		"	Max Bank: <color=#ffd479>{l3} items</color>\n"..
		"	Keep Durability: <color=#ffd479>{l4}</color>\n"..
		"	Cooldown: <color=#ffd479>{l5} seconds</color>",
		["MaxShare"] = "You may only share your bank with <color=#cd422b>{limit} player(s)</color> at one time.",
		["Menu"] = "\n	<color=#ffd479>/bank limits <bank | clan></color> - View bank limits\n"..
		"	<color=#ffd479>/bank info <item | clan></color> - View item information (first inventory slot) or clan information\n"..
		"	<color=#ffd479>/bank <bank | clan></color> - Open personal or clan bank\n"..
		"	<color=#ffd479>/bank share <player></color> - Open bank of shared player\n"..
		"	<color=#ffd479>/bank add <player></color> - Share your bank with player\n"..
		"	<color=#ffd479>/bank remove <player></color> - Unshare your bank with player\n"..
		"	<color=#ffd479>/bank removeall</color> - Unshare your bank with all players\n"..
		"	<color=#ffd479>/bank list <player></color> - List players sharing your bank\n"..
		"	<color=#ffd479>/bank clan toggle <moderator | member></color> - Toggle group bank access",
		["MinClanMembers"] = "Your clan, <color=#cd422b>{clan}</color>, currently has <color=#ffd479>{members} member(s)</color>. You must have minimum <color=#cd422b>{required} members</color> to use clan bank.",
		["MustInteract"] = "You must interact with a Banking NPC to access your bank.",
		["NoClan"] = "You do not belong to a clan.",
		["NoClanExists"] = "Clan <color=#cd422b>{clan}</color> does not exist.",
		["NoItem"] = "No item found in first slot of inventory to check for information.",
		["NoPermission"] = "You do not have permission to use this command.",
		["NoPlayer"] = "Player not found or multiple players found.  Provide a more specific username.",
		["NoPlugin"] = "The <color=#cd422b>{plugin} plugin</color> is not installed.",
		["NoShares"] = "You do not share your bank with anyone.",
		["NotEnabled"] = "Bank group <color=#cd422b>{group}</color> is <color=#cd422b>disabled</color>.",
		["NotShareEnabled"] = "Bank sharing is <color=#cd422b>disabled</color>.",
		["NotShared"] = "<color=#cd422b>{player}</color> does not share their bank with you.",
		["Occupied"] = "<color=#cd422b>{target}'s</color> bank is currently occupied by <color=#cd422b>{player}</color> ({id}).",
		["PlayerAdded"] = "You now share your bank with <color=#cd422b>{player}</color>.",
		["PlayerDeleted"] = "You no longer share your bank with <color=#cd422b>{player}</color>.",
		["PlayerExists"] = "You already share your bank with <color=#cd422b>{player}</color>.",
		["PlayerNotExists"] = "You do not share your bank with <color=#cd422b>{player}</color>.",
		["Prefix"] = "[<color=#cd422b> Bank Manager </color>] ",
		["Proximity"] = "You must be within close proximity of a Banking NPC to access your bank.",
		["RequiredPermission"] = "You cannot share your bank with <color=#cd422b>{player}</color> or open their bank. They do not have the required permissions.",
		["Returned"] = "One or more items have been returned to you for the following reason(s): <color=#cd422b>{reason}</color>",
		["ReturnReason1"] = "Insufficent clan members, ",
		["ReturnReason2"] = "Item cannot be banked, ",
		["ReturnReason3"] = "Item reached max deposit, ",
		["ReturnReason4"] = "Max bank reached, ",
		["ReturnReason5"] = "Max item stack reached, ",
		["Self"] = "You cannot use commands on yourself.",
		["ShareList"] = "Bank shared with <color=#cd422b>{count} player(s)</color>:\n{players}",
		["WrongArgs"] = "Syntax error. Use <color=#cd422b>/bank</color> for help.",
		["WrongRank"] = "You may only toggle access for ranks lower than your own."		
	}), self.Plugin)
end

function PLUGIN:LoadDataFile(call)
	local data = datafile.GetDataTable(DataFile_PB)
	Data_PB = data or {}
	data = datafile.GetDataTable(DataFile_PS)
	Data_PS = data or {}
	data = datafile.GetDataTable(DataFile_CB)
	Data_CB = data or {}
	data = datafile.GetDataTable(DataFile_CC)
	Data_CC = data or {}
end

function PLUGIN:SaveDataFile(call)
	if call == 1 then datafile.SaveDataTable(DataFile_PB) end
	if call == 2 then datafile.SaveDataTable(DataFile_PS) end
	if call == 3 then datafile.SaveDataTable(DataFile_CB) end
	if call == 4 then datafile.SaveDataTable(DataFile_CC) end
end

function PLUGIN:Unload()
	datafile.SaveDataTable(DataFile_PB)
	datafile.SaveDataTable(DataFile_PS)
	datafile.SaveDataTable(DataFile_CB)
	datafile.SaveDataTable(DataFile_CC)
	self:CloseBanks(1)
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

function PLUGIN:OnServerInitialized()
	clans = plugins.Find(ClanPlugin) or false
	local prefix = self:Lang(nil, "Prefix"):gsub(" ", "")
	prefix = prefix:gsub("<color=%p*%w*>", "")
	prefix = prefix:gsub("</color>", "")
	if self.Config.Defaults.ForceUpdate == "false" then
		if self.Config.Settings.PerformItemCheck ~= "true" then
			local message = FormatMessage(self:Lang(nil, "Initialize1"), { prefix = prefix })
			print(message)
			return
		end
	end
	local message = FormatMessage(self:Lang(nil, "Initialize2"), { prefix = prefix })
	print(message)
	if self.Config.Defaults.ForceUpdate == "true" then
		self.Config.Defaults.ForceUpdate = "false"
		self.Config.Items = {}
		local message = FormatMessage(self:Lang(nil, "Initialize3"), { prefix = prefix })
		print(message)
	end
	local items, acnt = global.ItemManager.GetItemDefinitions(), ""
	for i = 0, items.Count - 1 do
		local x, addtocfg = 1, true
		while self.Config.Items[x] do
			local item = tostring(self.Config.Items[x]):match("([^:]+)")
			if item == items[i].shortname then
				addtocfg = false
				break
			end
			x = x + 1
		end
		if addtocfg then
			local ItemCat, iName, iEnable, iMaxD, iMaxS, _iEnable, _iMaxD, _iMaxS = tostring(items[i].category), "", "", "", "", "", "", ""
			local y = 1
			while self.Config.Defaults.Items[y] do
				if tostring(self.Config.Defaults.Items[y]):match("([^:]+)") == tostring(ItemCat):match("([^:]+)") then
					iName, iEnable, iMaxD, iMaxS, _iEnable, _iMaxD, _iMaxS = tostring(self.Config.Defaults.Items[y]):match("([^:]+):([^:]+):([^:]+):([^:]+):([^:]+):([^:]+):([^:]+)")
					break
				end
				y = y + 1
			end
			if iName == "" then
				local y = 1
				while self.Config.Defaults.Items[y] do
					if string.match(self.Config.Defaults.Items[y], "Unknown") then
						iName, iEnable, iMaxD, iMaxS, _iEnable, _iMaxD, _iMaxS = tostring(self.Config.Defaults.Items[y]):match("([^:]+):([^:]+):([^:]+):([^:]+):([^:]+):([^:]+):([^:]+)")
						break
					end
					y = y + 1
				end
			end
			table.insert(self.Config.Items, items[i].shortname..":"..iEnable..":"..iMaxD..":"..iMaxS..":".._iEnable..":".._iMaxD..":".._iMaxS)
			acnt = acnt..items[i].shortname..", "
		end
	end
	local x, rcnt = 1, ""
	while self.Config.Items[x] do
		local item = tostring(self.Config.Items[x]):match("([^:]+)")
		local delfrmcfg = true
		for i = 0, items.Count - 1 do
			if item == items[i].shortname then
				delfrmcfg = false
				break
			end
		end
		if delfrmcfg then
			rcnt = rcnt..item..", "
			table.remove(self.Config.Items, x)
		end
		x = x + 1
	end
	local x, dcnt = 1, ""
	while self.Config.Items[x] do
		local item = tostring(self.Config.Items[x]):match("([^:]+)")
		local i, delfrmcfg = 1, false
		while self.Config.Items[i] do
			local citem = tostring(self.Config.Items[i]):match("([^:]+)")
			if item == citem and i ~= x then
				delfrmcfg = true
				break
			end
			i = i + 1
		end
		if delfrmcfg then
			dcnt = dcnt..item..", "
			table.remove(self.Config.Items, x)
		end
		x = x + 1
	end
	if acnt ~= "" then
		local message = FormatMessage(self:Lang(nil, "Initialize4"), { prefix = prefix, items = string.sub(acnt, 1, -3) })
		print(message)
		else
		local message = FormatMessage(self:Lang(nil, "Initialize5"), { prefix = prefix })
		print(message)
	end
	if rcnt ~= "" then
		local message = FormatMessage(self:Lang(nil, "Initialize6"), { prefix = prefix, items = string.sub(rcnt, 1, -3) })
		print(message)
		else
		local message = FormatMessage(self:Lang(nil, "Initialize7"), { prefix = prefix })
		print(message)
	end
	if dcnt ~= "" then
		local message = FormatMessage(self:Lang(nil, "Initialize8"), { prefix = prefix, items = string.sub(dcnt, 1, -3) })
		print(message)
		else
		local message = FormatMessage(self:Lang(nil, "Initialize9"), { prefix = prefix })
		print(message)
	end
	self:SaveConfig()
end

function PLUGIN:GetPlayerData_PB(playerSteamID, addNewEntry)
	local playerData = Data_PB[playerSteamID]
	if not playerData and addNewEntry then
		playerData = {}
		playerData.Bank = {}
		Data_PB[playerSteamID] = playerData
		self:SaveDataFile(1)
	end
	return playerData
end

function PLUGIN:GetPlayerData_PS(playerSteamID, addNewEntry)
	local playerData = Data_PS[playerSteamID]
	if not playerData and addNewEntry then
		playerData = {}
		playerData.Shared = {}
		Data_PS[playerSteamID] = playerData
		self:SaveDataFile(2)
	end
	return playerData
end

function PLUGIN:GetPlayerData_CB(clan, addNewEntry)
	local playerData = Data_CB[clan]
	if not playerData and addNewEntry then
		playerData = {}
		playerData.Bank = {}
		Data_CB[clan] = playerData
		self:SaveDataFile(3)
	end
	return playerData
end

function PLUGIN:GetPlayerData_CC(clan, addNewEntry)
	local playerData = Data_CC[clan]
	if not playerData and addNewEntry then
		playerData = {}
		playerData.Config = {}
		playerData.Config["moderator"] = "true"
		playerData.Config["member"] = "false"
		Data_CC[clan] = playerData
		self:SaveDataFile(4)
	end
	return playerData
end

function PLUGIN:OnPlayerDisconnected(player)
	local playerSteamID = rust.UserIDFromPlayer(player)
	if ProximityPlayer[playerSteamID] ~= nil and ProximityPlayer[playerSteamID] == "true" then
		ProximityPlayer[playerSteamID] = nil
	end
	if ProximityClan[playerSteamID] ~= nil and ProximityClan[playerSteamID] == "true" then
		ProximityClan[playerSteamID] = nil
	end
	BankOpened[playerSteamID] = nil
end

function PLUGIN:cmdBank(player, cmd, args)
	local playerSteamID = rust.UserIDFromPlayer(player)
	if args.Length > 0 and args[0] == "toggle" then
		if not permission.UserHasPermission(playerSteamID, "bankmanager.admin") then
			self:RustMessage(player, self:Lang(player, "NoPermission"))
			return
		end
		if args.Length < 2 then
			self:RustMessage(player, self:Lang(player, "WrongArgs"))
			return
		end
		local sfunc = args[1]
		if sfunc ~= "bank" and sfunc ~= "clan" and sfunc ~= "share" and sfunc ~= "npc" then
			self:RustMessage(player, self:Lang(player, "WrongArgs"))
			return
		end
		local message
		if sfunc == "bank" then
			if self.Config.Settings.Enabled == "true" then
				self.Config.Settings.Enabled = "false"
				message = FormatMessage(self:Lang(player, "ChangedStatus"), { group = self:Lang(player, "GroupPlayer"), status = self:Lang(player, "Disabled") })
				self:CloseBanks(2)
				else
				self.Config.Settings.Enabled = "true"
				message = FormatMessage(self:Lang(player, "ChangedStatus"), { group = self:Lang(player, "GroupPlayer"), status = self:Lang(player, "Enabled") })
			end
		end
		if sfunc == "clan" then
			if not self:CheckPlugin(player) then return end
			if self.Config.Clan.Enabled == "true" then
				self.Config.Clan.Enabled = "false"
				message = FormatMessage(self:Lang(player, "ChangedStatus"), { group = self:Lang(player, "GroupClan"), status = self:Lang(player, "Disabled") })
				self:CloseBanks(3)
				else
				self.Config.Clan.Enabled = "true"
				message = FormatMessage(self:Lang(player, "ChangedStatus"), { group = self:Lang(player, "GroupClan"), status = self:Lang(player, "Enabled") })
			end
		end
		if sfunc == "share" then
			if self.Config.Settings.ShareEnabled == "true" then
				self.Config.Settings.ShareEnabled = "false"
				message = FormatMessage(self:Lang(player, "ChangedFeature"), { group = self:Lang(player, "GroupShare"), status = self:Lang(player, "Disabled") })
				else
				self.Config.Settings.ShareEnabled = "true"
				message = FormatMessage(self:Lang(player, "ChangedFeature"), { group = self:Lang(player, "GroupShare"), status = self:Lang(player, "Enabled") })
			end
		end
		if sfunc == "npc" then
			if self.Config.NPC.Enabled == "true" then
				self.Config.NPC.Enabled = "false"
				message = FormatMessage(self:Lang(player, "ChangedFeature"), { group = self:Lang(player, "GroupNPC"), status = self:Lang(player, "Disabled") })
				local players = global.BasePlayer.activePlayerList:GetEnumerator()
				while players:MoveNext() do
					local playerSteamID = rust.UserIDFromPlayer(players.Current)
					if ProximityPlayer[playerSteamID] ~= nil and ProximityPlayer[playerSteamID] == "true" then
						ProximityPlayer[playerSteamID] = "false"
					end
					if ProximityClan[playerSteamID] ~= nil and ProximityClan[playerSteamID] == "true" then
						ProximityClan[playerSteamID] = "false"
					end
				end
				else
				self.Config.NPC.Enabled = "true"
				message = FormatMessage(self:Lang(player, "ChangedFeature"), { group = self:Lang(player, "GroupNPC"), status = self:Lang(player, "Enabled") })
			end
		end
		self:SaveConfig()
		self:RustMessage(player, message)
		return
	end
	if self.Config.Settings.UsePermissions == "true" then
		if not permission.UserHasPermission(playerSteamID, "bankmanager.admin") and not permission.UserHasPermission(playerSteamID, "bankmanager.use") then
			self:RustMessage(player, self:Lang(player, "NoPermission"))
			return
		end
	end
	if args.Length == 0 then
		if permission.UserHasPermission(playerSteamID, "bankmanager.admin") then
			self:RustMessage(player, self:Lang(player, "AdminMenu"))
		end
		self:RustMessage(player, self:Lang(player, "Menu"))
		return
		elseif args.Length > 0 then
		local func = args[0]
		if func ~= "admin" and func ~= "limits" and func ~= "info" and func ~= "bank" and func ~= "clan" and func ~= "share" and func ~= "add" and func ~= "remove" and
			func ~= "removeall" and func ~= "list" then
			self:RustMessage(player, self:Lang(player, "WrongArgs"))
			return
		end
		if func == "admin" then
			if not permission.UserHasPermission(playerSteamID, "bankmanager.admin") then
				self:RustMessage(player, self:Lang(player, "NoPermission"))
				return
			end
			if args.Length < 2 then
				self:RustMessage(player, self:Lang(player, "WrongArgs"))
				return
			end
			local sfunc = args[1]
			if sfunc ~= "bank" and sfunc ~= "clan" then
				self:RustMessage(player, self:Lang(player, "WrongArgs"))
				return
			end
			if sfunc == "bank" then
				local found, targetplayer, targetname, targetid = self:CheckPlayer(player, args[2])
				if not found then return end
				if not self:CheckGround(player) then return end
				if self:CheckRadius(player) then return end
				self:OpenPlayerBank(player, targetplayer)
			end
			if sfunc == "clan" then
				if not self:CheckPlugin(player) then return end
				if not self:CheckGround(player) then return end
				if self:CheckRadius(player) then return end
				local ClanList = clans:Call("GetAllClans")
				for line in string.gmatch(tostring(ClanList),"\"[^\r\n]+") do
					if line:gsub("\"", "") == args[2] then
						self:OpenClanBank(player, 0, 0, args[2])
						return
					end
				end
				local message = FormatMessage(self:Lang(player, "NoClanExists"), { clan = args[2] })
				self:RustMessage(player, message)
			end
			return
		end
		if func == "limits" then
			if args.Length < 2 then
				self:RustMessage(player, self:Lang(player, "WrongArgs"))
				return
			end
			local sfunc = args[1]
			if sfunc ~= "bank" and sfunc ~= "clan" then
				self:RustMessage(player, self:Lang(player, "WrongArgs"))
				return
			end
			if sfunc == "bank" then
				local MaxBank = self.Config.Settings.MaxBank
				local MaxShare = self.Config.Settings.MaxShare
				local found, CustomMaxBank, CustomMaxShare, CustomItems = self:CheckCustomPermission(playerSteamID)
				if found then
					MaxBank = CustomMaxBank
					MaxShare = CustomMaxShare
				end
				local message = FormatMessage(self:Lang(player, "LimitsBank"), { l1 = self.Config.Settings.Enabled, l2 = self.Config.Settings.BuildingBlocked, l3 = MaxBank, l4 = MaxShare, l5 = self.Config.Settings.KeepDurability, l6 = self.Config.Settings.Cooldown })
				self:RustMessage(player, message)
			end
			if sfunc == "clan" then
				local message = FormatMessage(self:Lang(player, "LimitsClan"), { l1 = self.Config.Clan.Enabled, l2 = self.Config.Clan.MinMembers, l3 = self.Config.Clan.MaxBank, l4 = self.Config.Clan.KeepDurability, l5 = self.Config.Clan.Cooldown })
				self:RustMessage(player, message)
			end
			return
		end
		if func == "info" then
			if args.Length < 2 then
				self:RustMessage(player, self:Lang(player, "WrongArgs"))
				return
			end
			local sfunc = args[1]
			if sfunc ~= "item" and sfunc ~= "clan" then
				self:RustMessage(player, self:Lang(player, "WrongArgs"))
				return
			end
			if sfunc == "item" then
				local FindItemInfo
				local mainInv = player.inventory.containerMain
				local mainItems = mainInv.itemList:GetEnumerator()
				while mainItems:MoveNext() do
					if mainItems.Current.position == 0 then
						FindItemInfo = mainItems.Current.info.shortname
						break
					end
				end
				if not FindItemInfo then
					self:RustMessage(player, self:Lang(player, "NoItem"))
					return
				end
				local FindItem = true
				local found, CustomMaxBank, CustomMaxShare, CustomItems = self:CheckCustomPermission(playerSteamID)
				local id, bnk, maxd, maxs, id_, bnk_, maxd_, maxs_, _bnk, _maxd, _maxs
				if found and CustomItems[1] then
					local i = 1
					while CustomItems[i] do
						if tostring(CustomItems[i]):match("([^:]+)") == FindItemInfo then
							id, bnk, maxd, maxs = tostring(CustomItems[i]):match("([^:]+):([^:]+):([^:]+):([^:]+)")
							FindItem = false
							local x = 1
							while self.Config.Items[x] do
								if tostring(self.Config.Items[x]):match("([^:]+)") == FindItemInfo then
									id_, bnk_, maxd_, maxs_, _bnk, _maxd, _maxs = tostring(self.Config.Items[x]):match("([^:]+):([^:]+):([^:]+):([^:]+):([^:]+):([^:]+):([^:]+)")
									break
								end
								x = x + 1
							end
							break
						end
						i = i + 1
					end
				end
				if FindItem then
					local i = 1
					while self.Config.Items[i] do
						if tostring(self.Config.Items[i]):match("([^:]+)") == FindItemInfo then
							id, bnk, maxd, maxs, _bnk, _maxd, _maxs = tostring(self.Config.Items[i]):match("([^:]+):([^:]+):([^:]+):([^:]+):([^:]+):([^:]+):([^:]+)")
							break
						end
						i = i + 1
					end
				end
				local CanBank, _CanBank = "false", "false"
				if tonumber(bnk) == 1 then CanBank = "true" end
				if tonumber(_bnk) == 1 then _CanBank = "true" end
				local message = FormatMessage(self:Lang(player, "InfoItem"), { i1 = FindItemInfo, i2 = CanBank, i3 = maxd, i4 = maxs, i5 = _CanBank, i6 = _maxd, i7 = _maxs })
				self:RustMessage(player, message)
				return
			end
			if sfunc == "clan" then
				local found, playerClan, playerGroup, count = self:GetClanMember(player)
				if not found then return end
				local message = FormatMessage(self:Lang(player, "InfoClan"), { i1 = playerClan, i2 = playerGroup, i3 = count })
				self:RustMessage(player, message)
				return
			end
		end
		if func == "bank" then
			if not permission.UserHasPermission(playerSteamID, "bankmanager.admin") then
				if self.Config.Settings.Enabled ~= "true" then
					local message = FormatMessage(self:Lang(player, "NotEnabled"), { group = self:Lang(player, "GroupPlayer") })
					self:RustMessage(player, message)
					return
				end
				if self.Config.NPC.Enabled == "true" then
					if self.Config.NPC.MustInteract == "true" then
						self:RustMessage(player, self:Lang(player, "MustInteract"))
						return
						else
						if not self:CheckProximity(player, 1) then return end
					end
				end
			end
			if not self:CheckCooldown(player, 1) then return end
			if self:CheckBuildingBlock(player) then return end
			if not self:CheckGround(player) then return end
			if self:CheckRadius(player) then return end
			self:OpenPlayerBank(player, player)
			return
		end
		if func == "clan" then
			local sfunc
			if args.Length >= 2 then sfunc = args[1] end
			if sfunc == nil then
				if not self:CheckPlugin(player) then return end
				if not permission.UserHasPermission(playerSteamID, "bankmanager.admin") then
					if self.Config.Clan.Enabled ~= "true" then
						local message = FormatMessage(self:Lang(player, "NotEnabled"), { group = self:Lang(player, "GroupClan") })
						self:RustMessage(player, message)
						return
					end
					if self.Config.NPC.Enabled == "true" then
						if self.Config.NPC.MustInteract == "true" then
							self:RustMessage(player, self:Lang(player, "MustInteract"))
							return
							else
							if not self:CheckProximity(player, 2) then return end
						end
					end
				end
				if not self:CheckCooldown(player, 2) then return end
				if self:CheckBuildingBlock(player) then return end
				if not self:CheckGround(player) then return end
				if self:CheckRadius(player) then return end
				local found, playerClan, playerGroup, count = self:GetClanMember(player)
				if not found then return end
				if tonumber(count) < tonumber(self.Config.Clan.MinMembers) then
					if playerGroup == "owner" then 
						local playerData = self:GetPlayerData_CB(playerClan, true)
						if #playerData.Bank > 0 then
							Owner[playerSteamID] = true
							local message = FormatMessage(self:Lang(player, "ClanOwner"), { clan = playerClan, members = count, required = self.Config.Clan.MinMembers })
							self:RustMessage(player, message)
							else
							local message = FormatMessage(self:Lang(player, "MinClanMembers"), { clan = playerClan, members = count, required = self.Config.Clan.MinMembers })
							self:RustMessage(player, message)
							return
						end
						else
						local message = FormatMessage(self:Lang(player, "MinClanMembers"), { clan = playerClan, members = count, required = self.Config.Clan.MinMembers })
						self:RustMessage(player, message)
						return
					end
				end
				self:OpenClanBank(player, playerClan, playerGroup, 1)
				return
				else
				if args.Length < 3 or sfunc ~= "toggle" then
					self:RustMessage(player, self:Lang(player, "WrongArgs"))
					return
				end
				local _sfunc = args[2]
				if _sfunc ~= "moderator" and _sfunc ~= "member" then
					self:RustMessage(player, self:Lang(player, "WrongArgs"))
					return
				end
				local found, playerClan, playerGroup, count = self:GetClanMember(player)
				if not found then return end
				if playerGroup == "member" then
					self:RustMessage(player, self:Lang(player, "WrongRank"))
					return
				end
				local message
				local playerData = self:GetPlayerData_CC(playerClan, true)
				if _sfunc == "moderator" then
					if playerGroup == "owner" then
						if playerData.Config.moderator == "true" then
							playerData.Config.moderator = "false"
							message = FormatMessage(self:Lang(player, "ChangedClanStatus"), { clan = playerClan, group = self:Lang(player, "GroupModerator"), status = self:Lang(player, "Disabled") })
							else
							playerData.Config.moderator = "true"
							message = FormatMessage(self:Lang(player, "ChangedClanStatus"), { clan = playerClan, group = self:Lang(player, "GroupModerator"), status = self:Lang(player, "Enabled") })
						end
						else
						self:RustMessage(player, self:Lang(player, "WrongRank"))
						return
					end
				end
				if _sfunc == "member" then
					if playerData.Config.member == "true" then
						playerData.Config.member = "false"
						message = FormatMessage(self:Lang(player, "ChangedClanStatus"), { clan = playerClan, group = self:Lang(player, "GroupMember"), status = self:Lang(player, "Disabled") })
						else
						playerData.Config.member = "true"
						message = FormatMessage(self:Lang(player, "ChangedClanStatus"), { clan = playerClan, group = self:Lang(player, "GroupMember"), status = self:Lang(player, "Enabled") })
					end
				end
				self:RustMessage(player, message)
				self:SaveDataFile(4)
				return
			end
		end
		if func == "share" then
			if self.Config.Settings.ShareEnabled ~= "true" then
				self:RustMessage(player, self:Lang(player, "NotShareEnabled"))
				return
			end
			if not permission.UserHasPermission(playerSteamID, "bankmanager.share") and not permission.UserHasPermission(playerSteamID, "bankmanager.admin") then
				self:RustMessage(player, self:Lang(player, "NoPermission"))
				return
			end
			if args.Length < 2 then
				self:RustMessage(player, self:Lang(player, "WrongArgs"))
				return
			end
			local found, targetplayer, targetname, targetid = self:CheckPlayer(player, args[1])
			if not found then return end
			if not self:CheckCooldown(player, 1) then return end
			if not self:CheckGround(player) then return end
			if self:CheckRadius(player) then return end
			local playerData = self:GetPlayerData_PS(targetid, true)
			if #playerData.Shared > 0 then
				for current, data in pairs(playerData.Shared) do
					if data.id == playerSteamID then
						self:OpenPlayerBank(player, targetplayer)
						return
					end
				end
			end
			local message = FormatMessage(self:Lang(player, "NotShared"), { player = targetname })
			self:RustMessage(player, message)
			return
		end
		if func == "add" then
			if not permission.UserHasPermission(playerSteamID, "bankmanager.share") and not permission.UserHasPermission(playerSteamID, "bankmanager.admin") then
				self:RustMessage(player, self:Lang(player, "NoPermission"))
				return
			end
			if args.Length < 2 then
				self:RustMessage(player, self:Lang(player, "WrongArgs"))
				return
			end
			local playerData = self:GetPlayerData_PS(playerSteamID, true)
			if not permission.UserHasPermission(playerSteamID, "bankmanager.admin") then
				local MaxShare = self.Config.Settings.MaxShare
				local found, CustomMaxBank, CustomMaxShare, CustomItems = self:CheckCustomPermission(playerSteamID)
				if found then MaxShare = CustomMaxShare end
				if tonumber(#playerData.Shared) >= tonumber(MaxShare) then
					local message = FormatMessage(self:Lang(player, "MaxShare"), { limit = MaxShare })
					self:RustMessage(player, message)
					return
				end
			end
			local found, targetplayer, targetname, targetid = self:CheckPlayer(player, args[1])
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
			self:SaveDataFile(2)
			local message = FormatMessage(self:Lang(player, "PlayerAdded"), { player = targetname })
			self:RustMessage(player, message)
			return
		end
		if func == "remove" then
			if args.Length < 2 then
				self:RustMessage(player, self:Lang(player, "WrongArgs"))
				return
			end
			local found, targetplayer, targetname, targetid = self:CheckPlayer(player, args[1])
			if not found then return end
			local playerData = self:GetPlayerData_PS(playerSteamID, true)
			if #playerData.Shared > 0 then
				for current, data in pairs(playerData.Shared) do
					if data.id == targetid then
						table.remove(playerData.Shared, current)
						self:SaveDataFile(2)
						local message = FormatMessage(self:Lang(player, "PlayerDeleted"), { player = targetname })
						self:RustMessage(player, message)
						return
					end
				end
			end
			local message = FormatMessage(self:Lang(player, "PlayerNotExists"), { player = targetname })
			self:RustMessage(player, message)
			return
		end
		if func == "removeall" then
			local playerData = self:GetPlayerData_PS(playerSteamID, true)
			if #playerData.Shared == 0 then
				self:RustMessage(player, self:Lang(player, "NoShares"))
				return
			end
			local message = FormatMessage(self:Lang(player, "DeleteAll"), { entries = #playerData.Shared })
			self:RustMessage(player, message)
			playerData.Shared = {}
			self:SaveDataFile(2)
			return
		end
		if func == "list" then
			local playerData = self:GetPlayerData_PS(playerSteamID, true)
			if #playerData.Shared == 0 then
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
		return
	end
end

function PLUGIN:OpenPlayerBank(player, target)
	local playerSteamID = rust.UserIDFromPlayer(player)
	if BankOpened[playerSteamID] == "true" then return end
	local _playerSteamID = rust.UserIDFromPlayer(target)
	if BankUser[playerSteamID] and player == target then
		local user, id = tostring(BankUser[playerSteamID]):match("([^:]+):([^:]+)")
		local message = FormatMessage(self:Lang(player, "Occupied"), { target = target.displayName, player = user, id = id })
		self:RustMessage(player, message)
		return
	end
	if Bank[_playerSteamID] and Shared[_playerSteamID] == nil then
		local message = FormatMessage(self:Lang(player, "Occupied"), { target = target.displayName, player = target.displayName, id = _playerSteamID })
		self:RustMessage(player, message)
		return
	end
	if BankUser[_playerSteamID] then
		local user, id = tostring(BankUser[_playerSteamID]):match("([^:]+):([^:]+)")
		local message = FormatMessage(self:Lang(player, "Occupied"), { target = target.displayName, player = user, id = id })
		self:RustMessage(player, message)
		return
	end
	BankOpened[playerSteamID] = "true"
	timer.Once(.5, function()
		local PlayerPos = player.transform.position
		PlayerPos.y = PlayerPos.y - 1
		local box = global.GameManager.server:CreateEntity("assets/prefabs/deployable/large wood storage/box.wooden.large.prefab", PlayerPos, player.transform.rotation)
		box:SendMessage("SetDeployedBy", player, UnityEngine.SendMessageOptions.DontRequireReceiver)
		box.name = "bank:"..playerSteamID
		Bank[playerSteamID] = box
		box:Spawn()
		Shared[playerSteamID] = nil
		BankUser[_playerSteamID] = nil
		if playerSteamID ~= _playerSteamID then
			Shared[playerSteamID] = target.displayName..":".._playerSteamID
			BankUser[_playerSteamID] = player.displayName..":"..playerSteamID
		end
		local playerData = self:GetPlayerData_PB(_playerSteamID, true)
		if #playerData.Bank > 0 then
			local loot = box:GetComponent("StorageContainer").inventory
			for current, data in pairs(playerData.Bank) do
				local item = global.ItemManager.CreateByItemID(data.item, data.quantity)
				if self.Config.Settings.KeepDurability == "true" and data.durability then item.condition = data.durability end
				if data.skin then
					item.skin = data.skin
					local ent = item:GetHeldEntity()
					if ent then ent.skinID = data.skin end
				end
				if data.attachments then
					for _, _data in pairs(data.attachments) do
						if item.contents:GetSlot(0) then item.contents:GetSlot(0):Remove(0) end
						local newItem = global.ItemManager.CreateByName(_data.id, _data.quantity)
						if self.Config.Settings.KeepDurability == "true" and _data.durability then newItem.condition = _data.durability end
						timer.Once(1, function() newItem:MoveToContainer(item.contents) end)
					end
				end
				if data.ammo and data.ammo.id then
					local mag = item:GetHeldEntity().primaryMagazine
					local itemDef = global.ItemManager.FindItemDefinition.methodarray[1]:Invoke(nil, util.TableToArray({data.ammo.id}))
					mag.ammoType = itemDef
					mag.contents = data.ammo.quantity
				end
				item:MoveToContainer(loot, data.pos)
			end
		end
		local loot = box:GetComponent("StorageContainer")
		loot:PlayerOpenLoot(player)
		local message = FormatMessage(self:Lang(player, "BankOpened"), { player = target.displayName })
		self:RustMessage(player, message)
		CoolDown[playerSteamID] = time.GetUnixTimestamp()
	end)
end

function PLUGIN:OpenClanBank(player, clan, group, call)
	local playerSteamID = rust.UserIDFromPlayer(player)
	if BankOpened[playerSteamID] == "true" then return end
	local playerClan, playerGroup
	if call == 1 then
		playerClan = clan
		playerGroup = group
		local Access = false
		if playerGroup == "owner" then Access = true end
		if playerGroup == "moderator" then
			local playerData = self:GetPlayerData_CC(playerClan, true)
			if playerData.Config.moderator == "true" then Access = true end
		end
		if playerGroup == "member" then
			local playerData = self:GetPlayerData_CC(playerClan, true)
			if playerData.Config.member == "true" then Access = true end
		end
		if not Access then
			local message = FormatMessage(self:Lang(player, "ClanNoPermission"), { clan = playerClan })
			self:RustMessage(player, message)
			return
		end
		else
		playerClan = call
	end
	if ClanUser[playerClan] then
		local user, id = tostring(ClanUser[playerClan]):match("([^:]+):([^:]+)")
		local message = FormatMessage(self:Lang(player, "ClanOccupied"), { clan = playerClan, player = user, id = id })
		self:RustMessage(player, message)
		return
	end
	BankOpened[playerSteamID] = "true"
	timer.Once(.5, function()
		local PlayerPos = player.transform.position
		PlayerPos.y = PlayerPos.y - 1
		local box = global.GameManager.server:CreateEntity("assets/prefabs/deployable/large wood storage/box.wooden.large.prefab", PlayerPos, player.transform.rotation)
		box:SendMessage("SetDeployedBy", player, UnityEngine.SendMessageOptions.DontRequireReceiver)
		box.name = "bank:"..playerSteamID
		ClanBank[playerSteamID] = box
		ClanName[playerSteamID] = playerClan
		box:Spawn()
		ClanUser[playerClan] = player.displayName..":"..playerSteamID
		local playerData = self:GetPlayerData_CB(playerClan, true)
		if #playerData.Bank > 0 then
			local loot = box:GetComponent("StorageContainer").inventory
			for current, data in pairs(playerData.Bank) do
				local item = global.ItemManager.CreateByItemID(data.item, data.quantity)
				if self.Config.Clan.KeepDurability == "true" and data.durability then item.condition = data.durability end
				if data.skin then
					item.skin = data.skin
					local ent = item:GetHeldEntity()
					if ent then ent.skinID = data.skin end
				end
				if data.attachments then
					for _, _data in pairs(data.attachments) do
						if item.contents:GetSlot(0) then item.contents:GetSlot(0):Remove(0) end
						local newItem = global.ItemManager.CreateByName(_data.id, _data.quantity)
						if self.Config.Clan.KeepDurability == "true" and _data.durability then newItem.condition = _data.durability end
						timer.Once(1, function() newItem:MoveToContainer(item.contents) end)
					end
				end
				if data.ammo and data.ammo.id then
					local mag = item:GetHeldEntity().primaryMagazine
					local itemDef = global.ItemManager.FindItemDefinition.methodarray[1]:Invoke(nil, util.TableToArray({data.ammo.id}))
					mag.ammoType = itemDef
					mag.contents = data.ammo.quantity
				end
				item:MoveToContainer(loot, data.pos)
			end
		end
		local loot = box:GetComponent("StorageContainer")
		loot:PlayerOpenLoot(player)
		local message = FormatMessage(self:Lang(player, "ClanBankOpened"), { clan = playerClan })
		self:RustMessage(player, message)
		CoolDown[playerSteamID] = time.GetUnixTimestamp()
	end)
end

function PLUGIN:SaveBank(player, call)
	local playerSteamID = rust.UserIDFromPlayer(player)
	local playerData, box, playerClan, MaxBank, found, CustomMaxBank, CustomMaxShare, CustomItems
	if call == 1 then
		MaxBank = self.Config.Settings.MaxBank
		local TargetName, SteamID = "", playerSteamID
		if Shared[playerSteamID] then
			TargetName, SteamID = tostring(Shared[playerSteamID]):match("([^:]+):([^:]+)")
			found, CustomMaxBank, CustomMaxShare, CustomItems = self:CheckCustomPermission(SteamID)
			if found then MaxBank = CustomMaxBank end
			else
			found, CustomMaxBank, CustomMaxShare, CustomItems = self:CheckCustomPermission(SteamID)
			if found then MaxBank = CustomMaxBank end
		end
		playerData = self:GetPlayerData_PB(SteamID, true)
		box = Bank[playerSteamID]:GetComponent("StorageContainer")
		else
		playerClan = ClanName[playerSteamID]
		playerData = self:GetPlayerData_CB(playerClan, true)
		box = ClanBank[playerSteamID]:GetComponent("StorageContainer")
		MaxBank = self.Config.Clan.MaxBank
	end
	if #playerData.Bank > 0 then playerData.Bank = {} end
	local loot = box.inventory.itemList:GetEnumerator()
	local ItemCount, Returned = 0, "0"
	while loot:MoveNext() do
		if loot.Current.info.shortname then
			local SaveItem = true
			local MaxStk
			if not permission.UserHasPermission(playerSteamID, "bankmanager.admin") then
				local FindItem = true
				local id, bnk, maxd, maxs, _bnk, _maxd, _maxs
				if call == 1 and found and CustomItems[1] then
					local i = 1
					while CustomItems[i] do
						if tostring(CustomItems[i]):match("([^:]+)") == loot.Current.info.shortname then
							id, bnk, maxd, maxs = tostring(CustomItems[i]):match("([^:]+):([^:]+):([^:]+):([^:]+)")
							FindItem = false
							break
						end
						i = i + 1
					end
				end
				if FindItem then
					local i = 1
					while self.Config.Items[i] do
						if tostring(self.Config.Items[i]):match("([^:]+)") == loot.Current.info.shortname then
							id, bnk, maxd, maxs, _bnk, _maxd, _maxs = tostring(self.Config.Items[i]):match("([^:]+):([^:]+):([^:]+):([^:]+):([^:]+):([^:]+):([^:]+)")
							break
						end
						i = i + 1
					end
				end
				local CanBank, MaxDep
				if call == 1 then
					CanBank = bnk
					MaxDep = maxd
					MaxStk = maxs
					else
					CanBank = _bnk
					MaxDep = _maxd
					MaxStk = _maxs
					if Owner[playerSteamID] then
						SaveItem = false
						if not string.match(Returned, "1") then Returned = Returned.."1" end
						self:ReturnItem(player, loot.Current, call)
					end
				end
				if tonumber(CanBank) ~= 1 then
					SaveItem = false
					if not string.match(Returned, "2") then Returned = Returned.."2" end
					self:ReturnItem(player, loot.Current, call)
				end
				if SaveItem then
					local CurItem = loot.Current.info.itemid
					if not BankItem[CurItem] then
						BankItem[CurItem] = 1
						else
						BankItem[CurItem] = BankItem[CurItem] + 1
					end
					if tonumber(BankItem[CurItem]) > tonumber(MaxDep) then
						SaveItem = false
						if not string.match(Returned, "3") then Returned = Returned.."3" end
						self:ReturnItem(player, loot.Current, call)
					end
				end
				if SaveItem then
					ItemCount = ItemCount + 1
					if ItemCount > tonumber(MaxBank) then
						SaveItem = false
						if not string.match(Returned, "4") then Returned = Returned.."4" end
						self:ReturnItem(player, loot.Current, call)
					end
				end
			end
			if SaveItem then
				local Stack = loot.Current.amount
				if not permission.UserHasPermission(playerSteamID, "bankmanager.admin") then
					if tonumber(loot.Current.amount) > tonumber(MaxStk) then
						if not string.match(Returned, "5") then Returned = Returned.."5" end
						Stack = tonumber(MaxStk)
						local item = global.ItemManager.CreateByItemID(loot.Current.info.itemid, (loot.Current.amount - tonumber(MaxStk)))
						item:MoveToContainer(player.inventory.containerMain, -1)
					end
				end
				local skin = nil
				local alist = nil
				local ammo = nil
				if loot.Current.skin ~= 0 then skin = loot.Current.skin end
				if loot.Current.contents then
					alist = {}
					local items = loot.Current.contents.itemList:GetEnumerator()
					local i = 1
					while items:MoveNext() do
						local adur = nil
						if items.hasCondition then adur = items.condition end
						alist[tostring(i)] = {id = items.Current.info.shortname, durability = adur, quantity = items.Current.amount}
						i = i + 1
					end
				end
				if loot.Current:GetHeldEntity() then
					ammo = {}
					local magazine = loot.Current:GetHeldEntity().primaryMagazine
					if magazine.definition then
						ammo = {id = magazine.ammoType.shortname, quantity = magazine.contents}
					end
				end
				local Bank = {["item"] = loot.Current.info.itemid, ["quantity"] = Stack, ["durability"] = loot.Current.condition, ["pos"] = loot.Current.position, ["skin"] = skin, ["attachments"] = alist, ["ammo"] = ammo}
				table.insert(playerData.Bank, Bank)
			end
		end
	end
	box.inventory.itemList:Clear()
	box:Kill()
	BankItem = {}
	if call == 1 then
		Bank[playerSteamID] = nil
		if Shared[playerSteamID] then
			local TargetName, SteamID = tostring(Shared[playerSteamID]):match("([^:]+):([^:]+)")
			BankUser[SteamID] = nil
			Shared[playerSteamID] = nil
		end
		self:SaveDataFile(1)
		else
		ClanBank[playerSteamID] = nil
		ClanName[playerSteamID] = nil
		ClanUser[playerClan] = nil
		Owner[playerSteamID] = nil
		self:SaveDataFile(3)
	end
	if Returned ~= "0" then
		local Reason = ""
		if string.match(Returned, "1") then Reason = Reason..self:Lang(player, "ReturnReason1") end
		if string.match(Returned, "2") then Reason = Reason..self:Lang(player, "ReturnReason2") end
		if string.match(Returned, "3") then Reason = Reason..self:Lang(player, "ReturnReason3") end
		if string.match(Returned, "4") then Reason = Reason..self:Lang(player, "ReturnReason4") end
		if string.match(Returned, "5") then Reason = Reason..self:Lang(player, "ReturnReason5") end
		local message = FormatMessage(self:Lang(player, "Returned"), { reason = string.sub(Reason, 1, -3) })
		self:RustMessage(player, message)
	end
end

function PLUGIN:ReturnItem(player, loot, call)
	local KeepDurability = false
	if call == 1 and self.Config.Settings.KeepDurability == "true" then KeepDurability = true end
	if call == 2 and self.Config.Clan.KeepDurability == "true" then KeepDurability = true end
	local item = global.ItemManager.CreateByItemID(loot.info.itemid, loot.amount)
	if KeepDurability then item.condition = loot.condition end
	if loot.skin ~= 0 then item.skin = loot.skin end
	if loot.contents then
		local items = loot.contents.itemList:GetEnumerator()
		while items:MoveNext() do
			if item.contents:GetSlot(0) then item.contents:GetSlot(0):Remove(0) end
			local newItem = global.ItemManager.CreateByName(items.Current.info.shortname, items.Current.amount)
			if KeepDurability and items.Current.hasCondition then newItem.condition = items.Current.condition end
			timer.Once(1, function() newItem:MoveToContainer(item.contents) end)
		end
	end
	if loot:GetHeldEntity() then
		local magazine = loot:GetHeldEntity().primaryMagazine
		if magazine.definition then
			local mag = item:GetHeldEntity().primaryMagazine
			local itemDef = global.ItemManager.FindItemDefinition.methodarray[1]:Invoke(nil, util.TableToArray({magazine.ammoType.shortname}))
			mag.ammoType = itemDef
			mag.contents = magazine.contents
		end
	end
	item:MoveToContainer(player.inventory.containerMain, -1)
end

function PLUGIN:CloseBanks(call)
	local players = global.BasePlayer.activePlayerList:GetEnumerator()
	while players:MoveNext() do
		local playerSteamID = rust.UserIDFromPlayer(players.Current)
		if Bank[playerSteamID] or ClanBank[playerSteamID] then
			if call == 1 or call == 2 then
				if Bank[playerSteamID] then
					self:SaveBank(players.Current, 1)
					self:RustMessage(players.Current, self:Lang(player, "BankDisabled"))
				end
			end
			if call == 1 or call == 3 then
				if ClanBank[playerSteamID] then
					self:SaveBank(players.Current, 2)
					self:RustMessage(players.Current, self:Lang(player, "BankDisabled"))
				end
			end
		end
	end
end

function PLUGIN:OnEntityTakeDamage(entity, info)
	if string.match(entity.name, "bank:") then
		TargetName, SteamID = tostring(entity.name):match("([^:]+):([^:]+)")
		if Bank[SteamID] or ClanBank[SteamID] then return true end
	end
end

function PLUGIN:OnLootEntity(source, target)
	if string.match(target.name, "bank:") then
		local player = source:GetComponent("BasePlayer")
		local playerSteamID = rust.UserIDFromPlayer(player)
		local box, id = target.name:match("([^:]+):([^:]+)")
		if playerSteamID ~= id then
			timer.NextFrame(function() player:EndLooting() end)
			self:RustMessage(player, self:Lang(player, "BankBox"))
		end
	end
end

function PLUGIN:OnPlayerLootEnd(source)
	local player = source:GetComponent("BasePlayer")
	local playerSteamID = rust.UserIDFromPlayer(player)
	if Bank[playerSteamID] then
		local TargetName, SteamID = player.displayName
		if Shared[playerSteamID] then
			TargetName, SteamID = tostring(Shared[playerSteamID]):match("([^:]+):([^:]+)")
		end
		self:SaveBank(player, 1)
		if player:IsConnected() then
			local message = FormatMessage(self:Lang(player, "BankClosed"), { player = TargetName })
			self:RustMessage(player, message)
		end
	end
	if ClanBank[playerSteamID] then
		local TargetName = ClanName[playerSteamID]
		self:SaveBank(player, 2)
		if player:IsConnected() then
			local message = FormatMessage(self:Lang(player, "ClanBankClosed"), { clan = TargetName })
			self:RustMessage(player, message)
		end
	end
	BankOpened[playerSteamID] = "false"
end

function PLUGIN:CheckPlayer(player, target)
	local target = rust.FindPlayer(target)
	if not target then
		self:RustMessage(player, self:Lang(player, "NoPlayer"))
		return false
	end
	local targetName = target.displayName
	local targetSteamID = rust.UserIDFromPlayer(target)
	local playerSteamID = rust.UserIDFromPlayer(player)
	if playerSteamID == targetSteamID then
		self:RustMessage(player, self:Lang(player, "Self"))
		return false
	end
	if not permission.UserHasPermission(targetSteamID, "bankmanager.admin") and not permission.UserHasPermission(targetSteamID, "bankmanager.share") then
		local message = FormatMessage(self:Lang(player, "RequiredPermission"), { player = targetName })
		self:RustMessage(player, message)
		return false
	end
	return true, target, targetName, targetSteamID
end

function PLUGIN:GetClanMember(player)
	local playerSteamID = rust.UserIDFromPlayer(player)
	local playerClan = clans:Call("GetClanOf", playerSteamID)
	if playerClan then
		local ClanList = clans:Call("GetAllClans")
		for line in string.gmatch(tostring(ClanList),"\"[^\r\n]+") do
			local ClanInfo = clans:Call("GetClan", tostring(line):gsub("\"", ""))
			if ClanInfo then
				if tostring(ClanInfo.tag):match("([^:]+)") == playerClan then
					local _, count = tostring(ClanInfo.members):gsub("\n", "\n")
					count = count - 1
					if tostring(ClanInfo.owner):match("([^:]+)") == playerSteamID then
						return true, playerClan, "owner", count
					end
					if string.match(tostring(ClanInfo.moderators), playerSteamID) then
						return true, playerClan, "moderator", count
					end
					if string.match(tostring(ClanInfo.members), playerSteamID) then
						return true, playerClan, "member", count
					end
					self:RustMessage(player, self:Lang(player, "ClanError"))
					return false
				end
			end
		end
	end
	self:RustMessage(player, self:Lang(player, "NoClan"))
	return false
end

function PLUGIN:CheckCustomPermission(playerSteamID)
	if self.Config.CustomPermissions then
		for current, data in pairs(self.Config.CustomPermissions) do
			if permission.UserHasPermission(playerSteamID, data.Permission) then
				return true, data.MaxBank, data.MaxShare, data.Items
			end
		end
		return false
	end
	return false
end

function PLUGIN:CheckCooldown(player, call)
	local playerSteamID = rust.UserIDFromPlayer(player)
	if CoolDown[playerSteamID] then
		local Timestamp = time.GetUnixTimestamp()
		local Cooldown
		if call == 1 then Cooldown = tonumber(self.Config.Settings.Cooldown) end
		if call == 2 then Cooldown = tonumber(self.Config.Clan.Cooldown) end
		if Timestamp - CoolDown[playerSteamID] < Cooldown then
			local remaining = Cooldown - (Timestamp - CoolDown[playerSteamID])
			local message = FormatMessage(self:Lang(player, "CoolDown"), { cooldown = remaining })
			self:RustMessage(player, message)
			return false
		end
	end
	return true
end

function PLUGIN:CheckBuildingBlock(player)
	if self.Config.Settings.BuildingBlocked == "true" and not player:CanBuild() then
		self:RustMessage(player, self:Lang(player, "BuildingBlock"))
		return true
	end
	return false
end

function PLUGIN:CheckGround(player)
	if self.Config.Settings.Ground ~= "true" then return true end
	local Raycast = UnityEngine.Physics.Raycast.methodarray[12]
	local ray = new(UnityEngine.Ray._type, util.TableToArray { player.transform.position, UnityEngine.Vector3.get_down() })
	local arr = util.TableToArray { ray, new( UnityEngine.RaycastHit._type, nil ), 1.5, -5 }
	util.ConvertAndSetOnArray(arr, 2, 1.5, System.Int64._type)
	util.ConvertAndSetOnArray(arr, 3, -5, System.Int32._type)
	if Raycast:Invoke(nil, arr) then
		local hitEntity = global.RaycastHitEx.GetEntity(arr[1])
		if hitEntity then
			if hitEntity:GetComponentInParent(global.BuildingBlock._type) then
				if self.Config.Settings.Tier ~= "-1" then
					local Tier = self.Config.Settings.Tier
					local buildingBlock = hitEntity:GetComponentInParent(global.BuildingBlock._type)
					if tostring(buildingBlock.name) ~= "assets/prefabs/building core/foundation/foundation.prefab" then
						local message = FormatMessage(self:Lang(player, "CheckTier"), { tier = Tier })
						self:RustMessage(player, message)
						return false
					end
					local Grade = tostring(buildingBlock.grade)
					local _, _Tier = Grade:match("([^:]+):([^:]+)")
					_Tier = string.sub(_Tier, 2)
					if tonumber(_Tier) >= tonumber(Tier) then return true end
					local message = FormatMessage(self:Lang(player, "CheckTier"), { tier = Tier })
					self:RustMessage(player, message)
					return false
					else
					self:RustMessage(player, self:Lang(player, "CheckGround"))
					return false
				end
			end
		end
	end
	return true
end

function PLUGIN:CheckRadius(player)
	local players = global.BasePlayer.activePlayerList:GetEnumerator()
	while players:MoveNext() do
		if players.Current ~= player then
			if UnityEngine.Vector3.Distance(players.Current.transform.position, player.transform.position) <= tonumber(self.Config.Settings.Radius) then
				local Near = tostring(UnityEngine.Vector3.Distance(players.Current.transform.position, player.transform.position)):match("([^.]*).(.*)")
				local message = FormatMessage(self:Lang(player, "CheckRadius"), { range = self.Config.Settings.Radius, current = Near })
				self:RustMessage(player, message)
				return true
			end
		end
	end
	return false
end

function PLUGIN:CheckPlugin(player)
	if not clans then
		if self.Config.Clan.Enabled == "true" then
			self.Config.Clan.Enabled = "false"
			self:SaveConfig()
		end
		local message = FormatMessage(self:Lang(player, "NoPlugin"), { plugin = ClanPlugin })
		self:RustMessage(player, message)
		return false
	end
	return true
end

function PLUGIN:CheckProximity(player, call)
	local playerSteamID = rust.UserIDFromPlayer(player)
	if call == 1 then
		if ProximityPlayer[playerSteamID] == nil or ProximityPlayer[playerSteamID] == "false" then
			self:RustMessage(player, self:Lang(player, "Proximity"))
			return false
		end
		return true
	end
	if call == 2 then
		if ProximityClan[playerSteamID] == nil or ProximityClan[playerSteamID] == "false" then
			self:RustMessage(player, self:Lang(player, "Proximity"))
			return false
		end
		return true
	end
end

function PLUGIN:OnEnterNPC(npc, player)
	local npc = tostring(npc):match("([^%[]*)%[([^%]]*)")
	if npc:lower() == self.Config.NPC.PlayerBankName:lower() then
		local playerSteamID = rust.UserIDFromPlayer(player)
		ProximityPlayer[playerSteamID] = "true"
	end
	if npc:lower() == self.Config.NPC.ClanBankName:lower() then
		local playerSteamID = rust.UserIDFromPlayer(player)
		ProximityClan[playerSteamID] = "true"
	end
end

function PLUGIN:OnLeaveNPC(npc, player)
	local npc = tostring(npc):match("([^%[]*)%[([^%]]*)")
	if npc:lower() == self.Config.NPC.PlayerBankName:lower() then
		local playerSteamID = rust.UserIDFromPlayer(player)
		ProximityPlayer[playerSteamID] = "false"
	end
	if npc:lower() == self.Config.NPC.ClanBankName:lower() then
		local playerSteamID = rust.UserIDFromPlayer(player)
		ProximityClan[playerSteamID] = "false"
	end
end

function PLUGIN:OnUseNPC(npc, player)
	if npc and player then
		local npc = tostring(npc):match("([^%[]*)%[([^%]]*)")
		if npc:lower() == self.Config.NPC.PlayerBankName:lower() then
			local playerSteamID = rust.UserIDFromPlayer(player)
			if self.Config.Settings.Enabled ~= "true" and not permission.UserHasPermission(playerSteamID, "bankmanager.admin") then
				local message = FormatMessage(self:Lang(player, "NotEnabled"), { group = self:Lang(player, "GroupPlayer") })
				self:RustMessage(player, message)
				return
			end
			if not self:CheckCooldown(player, 1) then return end
			if self.Config.NPC.CheckBuildingBlock == "true" then
				if self:CheckBuildingBlock(player) then return end
			end
			if self.Config.NPC.CheckOnGround == "true" then
				if not self:CheckGround(player) then return end
			end
			if self.Config.NPC.CheckRadius == "true" then
				if self:CheckRadius(player) then return end
			end
			self:OpenPlayerBank(player, player)
			return
		end
		if npc:lower() == self.Config.NPC.ClanBankName:lower() then
			local playerSteamID = rust.UserIDFromPlayer(player)
			if self.Config.Clan.Enabled ~= "true" and not permission.UserHasPermission(playerSteamID, "bankmanager.admin") then
				local message = FormatMessage(self:Lang(player, "NotEnabled"), { group = self:Lang(player, "GroupClan") })
				self:RustMessage(player, message)
				return
			end
			if not self:CheckCooldown(player, 2) then return end
			if self.Config.NPC.CheckBuildingBlock == "true" then
				if self:CheckBuildingBlock(player) then return end
			end
			if self.Config.NPC.CheckOnGround == "true" then
				if not self:CheckGround(player) then return end
			end
			if self.Config.NPC.CheckRadius == "true" then
				if self:CheckRadius(player) then return end
			end
			local found, playerClan, playerGroup, count = self:GetClanMember(player)
			if not found then return end
			if tonumber(count) < tonumber(self.Config.Clan.MinMembers) then
				if playerGroup == "owner" then 
					local playerData = self:GetPlayerData_CB(playerClan, true)
					if #playerData.Bank > 0 then
						Owner[playerSteamID] = true
						local message = FormatMessage(self:Lang(player, "ClanOwner"), { clan = playerClan, members = count, required = self.Config.Clan.MinMembers })
						self:RustMessage(player, message)
						else
						local message = FormatMessage(self:Lang(player, "MinClanMembers"), { clan = playerClan, members = count, required = self.Config.Clan.MinMembers })
						self:RustMessage(player, message)
						return
					end
					else
					local message = FormatMessage(self:Lang(player, "MinClanMembers"), { clan = playerClan, members = count, required = self.Config.Clan.MinMembers })
					self:RustMessage(player, message)
					return
				end
			end
			self:OpenClanBank(player, playerClan, playerGroup, 1)
			return
		end
	end
end

function PLUGIN:RustMessage(player, message)
	rust.SendChatMessage(player, "<size="..tonumber(self.Config.Settings.MessageSize)..">"..self:Lang(player, "Prefix")..message.."</size>")
end

function PLUGIN:SendHelpText(player)
	self:RustMessage(player, self:Lang(player, "Help"))
end										