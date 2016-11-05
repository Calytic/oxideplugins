PLUGIN.Title        = "Bank Manager"
PLUGIN.Description  = "Allows players to deposit and withdraw items from a bank."
PLUGIN.Author       = "InSaNe8472"
PLUGIN.Version      = V(1,1,5)
PLUGIN.ResourceId   = 1331

local ClanPlugin = "Clans"
local NPCPlugin = "HumanNPC"
local EconomicsPlugin = "Economics"
local clans, economics, npc
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
local ClanBank = {}
local ClanName = {}
local ClanUser = {}
local Owner = {}
local ProximityPlayer = {}
local ProximityClan = {}
local ConfirmPurchase = {}
local Expire = {}

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
	self.Config.Player = self.Config.Player or {}
	self.Config.Clan = self.Config.Clan or {}
	self.Config.Economics = self.Config.Economics or {}
	self.Config.NPC = self.Config.NPC or {}
	self.Config.Defaults = self.Config.Defaults or {}
	self.Config.Items = self.Config.Items or {}
	self.Config.Settings.PerformItemCheck = self.Config.Settings.PerformItemCheck or "true"
	self.Config.Settings.UsePermissions = self.Config.Settings.UsePermissions or "true"
	self.Config.Settings.GlobalAdminMessage = self.Config.Settings.GlobalAdminMessage or "true"
	self.Config.Settings.MessageSize = self.Config.Settings.MessageSize or "12"
	self.Config.Settings.Radius = self.Config.Settings.Radius or "5"
	self.Config.Settings.Ground = self.Config.Settings.Ground or "true"
	self.Config.Settings.Tier = self.Config.Settings.Tier or "-1"
	self.Config.Settings.BuildingBlocked = self.Config.Settings.BuildingBlocked or "true"
	self.Config.Player.Enabled = self.Config.Player.Enabled or "true"
	self.Config.Player.ShareEnabled = self.Config.Player.ShareEnabled or "true"
	self.Config.Player.DefaultSlots = self.Config.Player.DefaultSlots or "30"
	self.Config.Player.MaxShare = self.Config.Player.MaxShare or "10"
	self.Config.Player.KeepDurability = self.Config.Player.KeepDurability or "true"
	self.Config.Player.Cooldown = self.Config.Player.Cooldown or "3"
	self.Config.Clan.Enabled = self.Config.Clan.Enabled or "true"
	self.Config.Clan.DefaultSlots = self.Config.Clan.DefaultSlots or "30"
	self.Config.Clan.MinMembers = self.Config.Clan.MinMembers or "3"
	self.Config.Clan.KeepDurability = self.Config.Clan.KeepDurability or "true"
	self.Config.Clan.Cooldown = self.Config.Clan.Cooldown or "3"
	self.Config.Economics.Enabled = self.Config.Economics.Enabled or "false"
	self.Config.Economics.Confirmation = self.Config.Economics.Confirmation or "true"
	self.Config.Economics.ConfirmExpire = self.Config.Economics.ConfirmExpire or "30"
	self.Config.Economics.PlayerInitialSlots = self.Config.Economics.PlayerInitialSlots or "3"
	self.Config.Economics.PlayerMaxSlots = self.Config.Economics.PlayerMaxSlots or "30"
	self.Config.Economics.PlayerOpenCost = self.Config.Economics.PlayerOpenCost or "50"
	self.Config.Economics.PlayerPercentOpenCost = self.Config.Economics.PlayerPercentOpenCost or "5"
	self.Config.Economics.PlayerSlotCost = self.Config.Economics.PlayerSlotCost or "1000"
	self.Config.Economics.PlayerPercentSlotCost = self.Config.Economics.PlayerPercentSlotCost or "10"
	self.Config.Economics.ClanInitialSlots = self.Config.Economics.ClanInitialSlots or "6"
	self.Config.Economics.ClanMaxSlots = self.Config.Economics.ClanMaxSlots or "30"
	self.Config.Economics.ClanOpenCost = self.Config.Economics.ClanOpenCost or "100"
	self.Config.Economics.ClanPercentOpenCost = self.Config.Economics.ClanPercentOpenCost or "5"
	self.Config.Economics.ClanSlotCost = self.Config.Economics.ClanSlotCost or "2000"
	self.Config.Economics.ClanPercentSlotCost = self.Config.Economics.ClanPercentSlotCost or "15"
	self.Config.Economics.ClanMemberBuy = self.Config.Economics.ClanMemberBuy or "false"
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
		{
			["Permission"] = "bankmanager.vip1",
			["PlayerMaxSlots"] = "30",
			["PlayerOpenCost"] = "25",
			["PlayerPercentOpenCost"] = "1",
			["PlayerSlotCost"] = "500",
			["PlayerPercentSlotCost"] = "5",
			["MaxShare"] = "15",
			["Items"] = "wood:1:3:2000"
		}
	}
	if not tonumber(self.Config.Settings.MessageSize) or tonumber(self.Config.Settings.MessageSize) < 1 then self.Config.Settings.MessageSize = "12" end
	if not tonumber(self.Config.Settings.Radius) or tonumber(self.Config.Settings.Radius) < 5 then self.Config.Player.Cooldown = "5" end
	if not tonumber(self.Config.Settings.Tier) or tonumber(self.Config.Settings.Tier) < -1 or tonumber(self.Config.Settings.Tier) > 4 then self.Config.Settings.Tier = "-1" end
	if not tonumber(self.Config.Player.DefaultSlots) or tonumber(self.Config.Player.DefaultSlots) < 0 or tonumber(self.Config.Player.DefaultSlots) > 30 then self.Config.Player.DefaultSlots = "30" end
	if not tonumber(self.Config.Player.MaxShare) or tonumber(self.Config.Player.MaxShare) < 1 then self.Config.Player.MaxShare = "10" end
	if not tonumber(self.Config.Player.Cooldown) or tonumber(self.Config.Player.Cooldown) < 3 then self.Config.Player.Cooldown = "3" end
	if not tonumber(self.Config.Clan.DefaultSlots) or tonumber(self.Config.Clan.DefaultSlots) < 0 or tonumber(self.Config.Clan.DefaultSlots) > 30 then self.Config.Clan.DefaultSlots = "30" end
	if not tonumber(self.Config.Clan.MinMembers) or tonumber(self.Config.Clan.MinMembers) < 1 then self.Config.Clan.MinMembers = "3" end
	if not tonumber(self.Config.Clan.Cooldown) or tonumber(self.Config.Clan.Cooldown) < 3 then self.Config.Clan.Cooldown = "3" end
	if not tonumber(self.Config.Economics.ConfirmExpire) or tonumber(self.Config.Economics.ConfirmExpire) < 5 then self.Config.Economics.ConfirmExpire = "30" end
	if not tonumber(self.Config.Economics.PlayerInitialSlots) or tonumber(self.Config.Economics.PlayerInitialSlots) < 0 or tonumber(self.Config.Economics.PlayerInitialSlots) > 30 then self.Config.Economics.PlayerInitialSlots = "3" end
	if not tonumber(self.Config.Economics.PlayerMaxSlots) or tonumber(self.Config.Economics.PlayerMaxSlots) < 0 or tonumber(self.Config.Economics.PlayerMaxSlots) > 30 then self.Config.Economics.PlayerMaxSlots = "30" end
	if not tonumber(self.Config.Economics.PlayerOpenCost) or tonumber(self.Config.Economics.PlayerOpenCost) < 0 then self.Config.Economics.PlayerOpenCost = "50" end
	if not tonumber(self.Config.Economics.PlayerPercentOpenCost) or tonumber(self.Config.Economics.PlayerPercentOpenCost) < 0 then self.Config.Economics.PlayerPercentOpenCost = "5" end
	if not tonumber(self.Config.Economics.PlayerSlotCost) or tonumber(self.Config.Economics.PlayerSlotCost) < 0 then self.Config.Economics.PlayerSlotCost = "1000" end
	if not tonumber(self.Config.Economics.PlayerPercentSlotCost) or tonumber(self.Config.Economics.PlayerPercentSlotCost) < 0 then self.Config.Economics.PlayerPercentSlotCost = "10" end
	if not tonumber(self.Config.Economics.ClanInitialSlots) or tonumber(self.Config.Economics.ClanInitialSlots) < 0 or tonumber(self.Config.Economics.ClanInitialSlots) > 30 then self.Config.Economics.ClanInitialSlots = "6" end
	if not tonumber(self.Config.Economics.ClanMaxSlots) or tonumber(self.Config.Economics.ClanMaxSlots) < 0 or tonumber(self.Config.Economics.ClanMaxSlots) > 30 then self.Config.Economics.ClanMaxSlots = "30" end
	if not tonumber(self.Config.Economics.ClanOpenCost) or tonumber(self.Config.Economics.ClanOpenCost) < 0 then self.Config.Economics.ClanOpenCost = "100" end
	if not tonumber(self.Config.Economics.ClanPercentOpenCost) or tonumber(self.Config.Economics.ClanPercentOpenCost) < 0 then self.Config.Economics.ClanPercentOpenCost = "5" end
	if not tonumber(self.Config.Economics.ClanSlotCost) or tonumber(self.Config.Economics.ClanSlotCost) < 0 then self.Config.Economics.ClanSlotCost = "2000" end
	if not tonumber(self.Config.Economics.ClanPercentSlotCost) or tonumber(self.Config.Economics.ClanPercentSlotCost) < 0 then self.Config.Economics.ClanPercentSlotCost = "15" end
	self:SaveConfig()
	if self.Config.CustomPermissions then
		for current, data in pairs(self.Config.CustomPermissions) do
			permission.RegisterPermission(data.Permission, self.Plugin)
		end
	end
end

function PLUGIN:LoadDefaultLang()
	lang.RegisterMessages(util.TableToLangDict({
		["AdminMenu"] = "\n	<color=#ffd479>/bank toggle <bank | clan | share | npc | economics></color> - Enable or disable bank system\n"..
		"	<color=#ffd479>/bank admin <bank | clan> <player | clan></color> - Open player or clan bank\n"..
		"	<color=#ffd479>/bank view <bank | clan> <player | clan></color> - View current player or clan bank slots\n"..
		"	<color=#ffd479>/bank set <bank | clan> <player | * | clan> <# slots | -/+ #></color> - Set player or clan bank slots",
		["AllClans"] = "All clans",
		["AllPlayers"] = "All players",
		["BankBox"] = "This box is a bank owned by another player and cannot be opened or destroyed.",
		["BankClosed"] = "Bank closed for <color=#cd422b>{player}</color>.",
		["BankDisabled"] = "Your open bank has been saved and closed. The bank system has been reloaded, unloaded or disabled by an administrator.",
		["BankOpened"] = "Bank opened for <color=#cd422b>{player}</color>.",
		["BankOpenedCost"] = "Bank opened for <color=#cd422b>{player}</color>.  Your open cost of <color=#cd422b>${cost}</color> was withdrawn.",
		["BuildingBlocked"] = "You cannot access a bank in building blocked areas.",
		["ChangedClanStatus"] = "Clan <color=#cd422b>{clan}'s</color> group <color=#ffd479>{group}</color> bank access <color=#cd422b>{status}</color>.",
		["ChangedFeature"] = "Bank feature <color=#cd422b>{group}</color> now <color=#cd422b>{status}</color>.",
		["ChangedStatus"] = "Bank group <color=#cd422b>{group}</color> now <color=#cd422b>{status}</color>.",
		["CheckGround"] = "You may only access a bank while standing on the ground.",
		["CheckRadius"] = "You cannot access a bank within <color=#cd422b>{range} meters</color> of another online player. Current nearest range is <color=#cd422b>{current} meters</color>.",
		["CheckTier"] = "You may only access a bank while standing on the ground or on tier <color=#cd422b>{tier}</color> or highier foundations.",
		["ClanBankClosed"] = "Bank closed for clan <color=#cd422b>{clan}</color>.",
		["ClanBankOpened"] = "Bank opened for clan <color=#cd422b>{clan}</color>.",
		["ClanBankOpenedCost"] = "Bank opened for clan <color=#cd422b>{clan}</color>.  Your open cost of <color=#cd422b>${cost}</color> was withdrawn.",
		["ClanError"] = "An error occured while retrieving your clan information.",
		["ClanMaxSlots"] = "Clan <color=#ffd479>{clan}</color> already has the maximum or exceed bank slots of <color=#ffd479>{maxslots}</color>.",
		["ClanMemberBuy"] = "Only the clan owner and moderators may purchase additional bank slots for your clan.",
		["ClanNoPermission"] = "You do not have permission to access <color=#cd422b>{clan}'s</color> bank.",
		["ClanOccupied"] = "Clan <color=#cd422b>{clan}'s</color> bank is currently occupied by <color=#cd422b>{player}</color> ({id}).",
		["ClanOwner"] = "Your clan, <color=#cd422b>{clan}</color>, currently has <color=#ffd479>{members} member(s)</color>. You must have minimum <color=#cd422b>{required} members</color> to use clan bank. As owner, you may access existing banked items. They will be returned to you upon closing your inventory.",
		["CompletePurchase"] = "Successfully purchased <color=#cd422b>{slots} slot(s)</color> for <color=#cd422b>${cost}</color>.",
		["ConfirmPurchase"] = "To confirm purchase of <color=#cd422b>{slots} slot(s)</color> for <color=#cd422b>${cost}</color>, use <color=#cd422b>/bank confirm</color> within <color=#cd422b>{expire} seconds</color>.",
		["CoolDown"] = "You must wait <color=#cd422b>{cooldown} seconds</color> before trying that.",
		["Decreased"] = "decreased",
		["DeleteAll"] = "You no longer share your bank with anyone. (<color=#cd422b>{entries}</color> player(s) removed)",
		["Disabled"] = "disabled",
		["Enabled"] = "enabled",
		["GroupClan"] = "clan",
		["GroupEconomics"] = "economics",
		["GroupPlayer"] = "player",
		["GroupShare"] = "sharing",
		["GroupNPC"] = "npc",
		["GroupMember"] = "member",
		["GroupModerator"] = "moderator",
		["Help"] = "<color=#ffd479>/bank</color> - Allows players to deposit and withdraw items from a bank",
		["Increased"] = "increased",
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
		["InsufficientMoneyOpen"] = "Insufficent money to open bank.  Your open cost is <color=#ffd479>${cost}</color>.  You currently have <color=#ffd479>${current}</color>.",
		["InsufficientMoneySlot"] = "Insufficent money to purchase slot(s).  Purchase cost is <color=#ffd479>${cost}</color>.  You currently have <color=#ffd479>${current}</color>.",
		["LangError"] = "Language error: ",
		["LimitsBank"] = "\n	Player Bank Enabled: <color=#ffd479>{l1}</color>\n"..
		"	Bank Sharing Enabled: <color=#ffd479>{l2}</color>\n"..
		"	Default Bank Slots: <color=#ffd479>{l3}</color>\n"..
		"	Your Maximum Shares: <color=#ffd479>{l4}</color>\n"..
		"	Keep Durability: <color=#ffd479>{l5}</color>\n"..
		"	Cooldown: <color=#ffd479>{l6} seconds</color>",
		["LimitsClan"] = "\n	Clan Bank Enabled: <color=#ffd479>{l1}</color>\n"..
		"	Default Bank Slots: <color=#ffd479>{l2}</color>\n"..
		"	Minimum Members: <color=#ffd479>{l3} members</color>\n"..
		"	Keep Durability: <color=#ffd479>{l4}</color>\n"..
		"	Cooldown: <color=#ffd479>{l5} seconds</color>",
		["LimitsEconomics"] = "\n	Economics Enabled: <color=#ffd479>{l1}</color>\n"..
		"	Purchase Confirmation Enabled: <color=#ffd479>{l2}</color>\n"..
		"	Confirmation Expiration: <color=#ffd479>{l3} seconds</color>\n\n"..
		"	<color=#cd422b>Player Bank</color>\n"..
		"	Initial Bank Slots: <color=#ffd479>{l4}</color>\n"..
		"	Your Maximum Bank Slots: <color=#ffd479>{l5}</color>\n"..
		"	Your Open Cost: <color=#ffd479>${l6}</color>\n"..
		"	Your Percentage Cost (opening): <color=#ffd479>{l7}%</color>\n"..
		"	Your Additional Slot Cost: <color=#ffd479>${l8}</color>\n"..
		"	Your Percentage Cost (new slot): <color=#ffd479>{l9}%</color>\n\n"..
		"	<color=#cd422b>Clan Bank</color>\n"..
		"	Initial Bank Slots: <color=#ffd479>{l10}</color>\n"..
		"	Maximum Bank Slots: <color=#ffd479>{l11}</color>\n"..
		"	Open Cost: <color=#ffd479>${l12}</color>\n"..
		"	Percentage Cost (opening): <color=#ffd479>{l13}%</color>\n"..
		"	Additional Slot Cost: <color=#ffd479>${l14}</color>\n"..
		"	Percentage Cost (new slot): <color=#ffd479>{l15}%</color>",
		["LimitsSystem"] = "\n	Radius Check: <color=#ffd479>{l1}m</color>\n"..
		"	Ground Check: <color=#ffd479>{l2}</color>\n"..
		"	Foundation Tier: <color=#ffd479>{l3}</color>\n"..
		"	Building Block: <color=#ffd479>{l4}</color>",
		["MaxShare"] = "You may only share your bank with <color=#cd422b>{limit} player(s)</color> at one time.",
		["Menu"] = "\n	<color=#ffd479>/bank limits <system | bank | clan | economics></color> - View bank limits and configuration\n"..
		"	<color=#ffd479>/bank info <item | clan></color> - View item information (first inventory slot) or clan information\n"..
		"	<color=#ffd479>/bank buy <bank | clan> [# slots]</color> - Buy additional bank slots\n"..
		"	<color=#ffd479>/bank <bank | clan></color> - Open personal or clan bank\n"..
		"	<color=#ffd479>/bank share <player></color> - Open bank of shared player\n"..
		"	<color=#ffd479>/bank add <player></color> - Share your bank with player\n"..
		"	<color=#ffd479>/bank remove <player></color> - Unshare your bank with player\n"..
		"	<color=#ffd479>/bank removeall</color> - Unshare your bank with all players\n"..
		"	<color=#ffd479>/bank list <player></color> - List players sharing your bank\n"..
		"	<color=#ffd479>/bank clan toggle <moderator | member></color> - Toggle group bank access",
		["MinClanMembers"] = "Your clan, <color=#cd422b>{clan}</color>, currently has <color=#ffd479>{members} member(s)</color>. You must have minimum <color=#cd422b>{required} members</color> to use clan bank.",
		["ModGlobalAlert"] = "Administrator has {modify} all {bank} banks by <color=#cd422b>{slots}</color> slot(s).  Does not apply if already at maximum or minimum slots.",
		["ModPlayerAlert"] = "Administrator has {modify} your bank by <color=#cd422b>{slots}</color> slot(s).  Does not apply if already at maximum or minimum slots.",
		["ModPlayerSlots"] = "{bank} <color=#cd422b>{player}</color> bank slots {modify} by <color=#cd422b>{slots}</color> slot(s).",
		["MustInteract"] = "You must interact with a Banking NPC to access your bank.",
		["NoClan"] = "You do not belong to a clan.",
		["NoClanExists"] = "Clan <color=#cd422b>{clan}</color> does not exist.",
		["NoConfirmPurchase"] = "You do not have a pending purchase confirmation.  Use <color=#cd422b>/bank</color> for help.",
		["NoItem"] = "No item found in first slot of inventory to check for information.",
		["NoPermission"] = "You do not have permission to use this command.",
		["NoPlayer"] = "Player not found or multiple players found.  Provide a more specific username.",
		["NoPlugin"] = "The <color=#cd422b>{plugin} plugin</color> is not installed.",
		["NoShares"] = "You do not share your bank with anyone.",
		["NotEnabled"] = "Bank group <color=#cd422b>{group}</color> is <color=#cd422b>disabled</color>.",
		["NotNumber"] = "Bank slots must be a number between <color=#cd422b>1 and {maxnum}</color>.",
		["NotShareEnabled"] = "Bank sharing is <color=#cd422b>disabled</color>.",
		["NotShared"] = "<color=#cd422b>{player}</color> does not share their bank with you.",
		["Occupied"] = "<color=#cd422b>{target}'s</color> bank is currently occupied by <color=#cd422b>{player}</color> ({id}).",
		["PlayerAdded"] = "You now share your bank with <color=#cd422b>{player}</color>.",
		["PlayerDeleted"] = "You no longer share your bank with <color=#cd422b>{player}</color>.",
		["PlayerExists"] = "You already share your bank with <color=#cd422b>{player}</color>.",
		["PlayerMaxSlots"] = "You already have the maximum or exceed bank slots of <color=#ffd479>{maxslots}</color>.",
		["PlayerNotExists"] = "You do not share your bank with <color=#cd422b>{player}</color>.",
		["Prefix"] = "[<color=#cd422b> Bank Manager </color>] ",
		["Proximity"] = "You must be within close proximity of a Banking NPC to access your bank.",
		["RequiredPermission"] = "You cannot share your bank with <color=#cd422b>{player}</color> or open their bank. They do not have the required permissions.",
		["Returned"] = "One or more items have been returned to you for the following reason(s): <color=#cd422b>{reason}</color>",
		["ReturnReason1"] = "Insufficent clan members, ",
		["ReturnReason2"] = "Item cannot be banked, ",
		["ReturnReason3"] = "Item reached max deposit, ",
		["ReturnReason4"] = "Max item stack reached, ",
		["Self"] = "You cannot use commands on yourself.",
		["SetGlobalAlert"] = "Administrator has set all {bank} banks to <color=#cd422b>{slots}</color> slot(s).",
		["SetPlayerAlert"] = "Administrator has set your bank to <color=#cd422b>{slots}</color> slot(s).",
		["SetPlayerSlots"] = "{bank} <color=#cd422b>{player}</color> bank slots set to <color=#cd422b>{slots}</color>.",
		["ShareList"] = "Bank shared with <color=#cd422b>{count} player(s)</color>:\n{players}",
		["ViewClanSlots"] = "Clan <color=#cd422b>{clan}</color> currently has <color=#cd422b>{slots}</color> bank slot(s).",
		["ViewPlayerSlots"] = "<color=#cd422b>{player}</color> currently has <color=#cd422b>{slots}</color> bank slot(s).",
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

function comma(num)
	while true do  
		num, x = string.gsub(num, "^(-?%d+)(%d%d%d)", '%1,%2')
		if x == 0 then break end
	end
	return num
end

function PLUGIN:OnServerInitialized()
	clans = plugins.Find(ClanPlugin) or false
	npc = plugins.Find(NPCPlugin) or false
	economics = plugins.Find(EconomicsPlugin) or false
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
		playerData.Slots = self:GetInitialSlots(1)
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
		playerData.Slots = self:GetInitialSlots(2)
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
		if func ~= "toggle" and func ~= "admin" and func ~= "view" and func ~= "set" and func ~= "limits" and func ~= "info" and func ~= "buy" and func ~= "confirm" and func ~= "bank" and func ~= "clan" and func ~= "share" and func ~= "add" and func ~= "remove" and func ~= "removeall" and func ~= "list" then
			self:RustMessage(player, self:Lang(player, "WrongArgs"))
			return
		end
		if func == "toggle" then
			if not permission.UserHasPermission(playerSteamID, "bankmanager.admin") then
				self:RustMessage(player, self:Lang(player, "NoPermission"))
				return
			end
			if args.Length < 2 then
				self:RustMessage(player, self:Lang(player, "WrongArgs"))
				return
			end
			local sfunc = args[1]
			if sfunc ~= "bank" and sfunc ~= "clan" and sfunc ~= "share" and sfunc ~= "npc" and sfunc ~= "economics" then
				self:RustMessage(player, self:Lang(player, "WrongArgs"))
				return
			end
			local message
			if sfunc == "bank" then
				if self.Config.Player.Enabled == "true" then
					self.Config.Player.Enabled = "false"
					message = FormatMessage(self:Lang(player, "ChangedStatus"), { group = self:Lang(player, "GroupPlayer"), status = self:Lang(player, "Disabled") })
					self:CloseBanks(2)
					else
					self.Config.Player.Enabled = "true"
					message = FormatMessage(self:Lang(player, "ChangedStatus"), { group = self:Lang(player, "GroupPlayer"), status = self:Lang(player, "Enabled") })
				end
			end
			if sfunc == "clan" then
				if not self:CheckPlugin(player, "clans", 1) then return end
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
				if self.Config.Player.ShareEnabled == "true" then
					self.Config.Player.ShareEnabled = "false"
					message = FormatMessage(self:Lang(player, "ChangedFeature"), { group = self:Lang(player, "GroupShare"), status = self:Lang(player, "Disabled") })
					else
					self.Config.Player.ShareEnabled = "true"
					message = FormatMessage(self:Lang(player, "ChangedFeature"), { group = self:Lang(player, "GroupShare"), status = self:Lang(player, "Enabled") })
				end
			end
			if sfunc == "npc" then
				if not self:CheckPlugin(player, "npc", 1) then return end
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
			if sfunc == "economics" then
				if not self:CheckPlugin(player, "economics", 1) then return end
				if self.Config.Economics.Enabled == "true" then
					self.Config.Economics.Enabled = "false"
					message = FormatMessage(self:Lang(player, "ChangedFeature"), { group = self:Lang(player, "GroupEconomics"), status = self:Lang(player, "Disabled") })
					else
					self.Config.Economics.Enabled = "true"
					message = FormatMessage(self:Lang(player, "ChangedFeature"), { group = self:Lang(player, "GroupEconomics"), status = self:Lang(player, "Enabled") })
				end
			end
			self:SaveConfig()
			self:RustMessage(player, message)
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
				local found, targetplayer, targetname, targetid = self:CheckPlayer(player, args[2], "true")
				if not found then return end
				if not self:CheckGround(player) then return end
				if self:CheckRadius(player) then return end
				self:OpenPlayerBank(player, targetplayer)
			end
			if sfunc == "clan" then
				if not self:CheckPlugin(player, "clans", 1) then return end
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
		if func == "view" then
			if not permission.UserHasPermission(playerSteamID, "bankmanager.admin") then
				self:RustMessage(player, self:Lang(player, "NoPermission"))
				return
			end
			if not self:CheckPlugin(player, "economics", 1) then return end
			if args.Length < 3 then
				self:RustMessage(player, self:Lang(player, "WrongArgs"))
				return
			end
			local sfunc = args[1]
			if sfunc ~= "bank" and sfunc ~= "clan" then
				self:RustMessage(player, self:Lang(player, "WrongArgs"))
				return
			end
			if sfunc == "bank" then
				local found, targetplayer, targetname, targetid = self:CheckPlayer(player, args[2], "true")
				if not found then return end
				local playerData, PlayerSlots = self:GetPlayerData_PS(targetid, true)
				if playerData.Slots == nil or not playerData.Slots then
					playerData.Slots = self:GetInitialSlots(1)
					self:SaveDataFile(2)
					PlayerSlots = self:GetInitialSlots(1)
					else
					PlayerSlots = playerData.Slots
				end
				local message = FormatMessage(self:Lang(player, "ViewPlayerSlots"), { player = targetname, slots = PlayerSlots })
				self:RustMessage(player, message)
			end
			if sfunc == "clan" then
				if not self:CheckPlugin(player, "clans", 1) then return end
				local ClanList = clans:Call("GetAllClans")
				for line in string.gmatch(tostring(ClanList),"\"[^\r\n]+") do
					if line:gsub("\"", "") == args[2] then
						local playerData = self:GetPlayerData_CC(args[2], true)
						if playerData.Slots == nil or not playerData.Slots then
							playerData.Slots = self:GetInitialSlots(2)
							self:SaveDataFile(4)
							PlayerSlots = self:GetInitialSlots(2)
							else
							PlayerSlots = playerData.Slots
						end
						local message = FormatMessage(self:Lang(player, "ViewClanSlots"), { clan = args[2], slots = PlayerSlots })
						self:RustMessage(player, message)
						return
					end
				end
				local message = FormatMessage(self:Lang(player, "NoClanExists"), { clan = args[2] })
				self:RustMessage(player, message)
			end
			return
		end
		if func == "set" then
			if not permission.UserHasPermission(playerSteamID, "bankmanager.admin") then
				self:RustMessage(player, self:Lang(player, "NoPermission"))
				return
			end
			if not self:CheckPlugin(player, "economics", 1) then return end
			if args.Length < 4 then
				self:RustMessage(player, self:Lang(player, "WrongArgs"))
				return
			end
			local sfunc = args[1]
			if sfunc ~= "bank" and sfunc ~= "clan" then
				self:RustMessage(player, self:Lang(player, "WrongArgs"))
				return
			end
			if sfunc == "bank" then
				local PlayerSlots, AddSub, ModSlots, Modify = args[3], "1", string.sub(args[3], 1, 1), self:Lang(player, "Decreased")
				if ModSlots == "-" or ModSlots == "+" then
					if ModSlots == "+" then
						AddSub = "2"
						Modify = self:Lang(player, "Increased")
					end
					PlayerSlots = tonumber(string.sub(PlayerSlots, 2, 3))
					if not tonumber(PlayerSlots) or PlayerSlots < 1 or PlayerSlots > 29 then
						local message = FormatMessage(self:Lang(player, "NotNumber"), { maxnum = "29" })
						self:RustMessage(player, message)
						return
					end
					local TargetPlayer, NewPlayerSlots, CurData = args[2]
					if TargetPlayer == "*" then
						for current, data in pairs(Data_PS) do
							if data.Slots then
								CurData = data.Slots
								else
								CurData = self:GetInitialSlots(1)
							end
							if AddSub == "1" then
								NewPlayerSlots = CurData - PlayerSlots
								else
								NewPlayerSlots = CurData + PlayerSlots
							end
							if NewPlayerSlots > 30 then NewPlayerSlots = 30 end
							if NewPlayerSlots < 1 then NewPlayerSlots = 1 end
							data.Slots = NewPlayerSlots
						end
						self:SaveDataFile(2)
						if self.Config.Settings.GlobalAdminMessage == "true" then
							local message = FormatMessage(self:Lang(nil, "ModGlobalAlert"), { modify = Modify, bank = self:Lang(nil, "GroupPlayer"), slots = PlayerSlots })
							self:RustBroadcast(message)
							else
							local message = FormatMessage(self:Lang(player, "ModPlayerSlots"), { bank = "", player = self:Lang(player, "AllPlayers"), modify = Modify, slots = PlayerSlots })
							self:RustMessage(player, message)
						end
						else
						local found, targetplayer, targetname, targetid = self:CheckPlayer(player, TargetPlayer, "true")
						if not found then return end
						local playerData = self:GetPlayerData_PS(targetid, true)
						if playerData.Slots == nil or not playerData.Slots then
							playerData.Slots = self:GetInitialSlots(1)
							self:SaveDataFile(2)
							CurData = self:GetInitialSlots(1)
							else
							CurData = playerData.Slots
						end
						if AddSub == "1" then
							NewPlayerSlots = CurData - PlayerSlots
							else
							NewPlayerSlots = CurData + PlayerSlots
						end
						if NewPlayerSlots > 30 then NewPlayerSlots = 30 end
						if NewPlayerSlots < 1 then NewPlayerSlots = 1 end
						playerData.Slots = NewPlayerSlots
						self:SaveDataFile(2)
						local message = FormatMessage(self:Lang(player, "ModPlayerSlots"), { bank = self:Lang(player, "GroupPlayer"):gsub("^%l", string.upper), player = targetname, modify = Modify, slots = PlayerSlots })
						self:RustMessage(player, message)
						if targetplayer:IsConnected() then
							local message = FormatMessage(self:Lang(targetplayer, "ModPlayerAlert"), { modify = Modify, slots = PlayerSlots })
							self:RustMessage(targetplayer, message)
						end
					end
					else
					PlayerSlots = tonumber(args[3])
					if not tonumber(PlayerSlots) or PlayerSlots < 1 or PlayerSlots > 30 then
						local message = FormatMessage(self:Lang(player, "NotNumber"), { maxnum = "30" })
						self:RustMessage(player, message)
						return
					end
					local TargetPlayer = args[2]
					if TargetPlayer == "*" then
						for current, data in pairs(Data_PS) do
							data.Slots = PlayerSlots
						end
						self:SaveDataFile(2)
						if self.Config.Settings.GlobalAdminMessage == "true" then
							local message = FormatMessage(self:Lang(nil, "SetGlobalAlert"), { bank = self:Lang(nil, "GroupPlayer"), slots = PlayerSlots })
							self:RustBroadcast(message)
							else
							local message = FormatMessage(self:Lang(player, "SetPlayerSlots"), { bank = "", player = self:Lang(player, "AllPlayers"), slots = PlayerSlots })
							self:RustMessage(player, message)
						end
						else
						local found, targetplayer, targetname, targetid = self:CheckPlayer(player, TargetPlayer, "true")
						if not found then return end
						local playerData = self:GetPlayerData_PS(targetid, true)
						playerData.Slots = PlayerSlots
						self:SaveDataFile(2)
						local message = FormatMessage(self:Lang(player, "SetPlayerSlots"), { bank = self:Lang(player, "GroupPlayer"):gsub("^%l", string.upper), player = targetname, slots = PlayerSlots })
						self:RustMessage(player, message)
						if targetplayer:IsConnected() then
							local message = FormatMessage(self:Lang(targetplayer, "SetPlayerAlert"), { slots = PlayerSlots })
							self:RustMessage(targetplayer, message)
						end
					end
				end
			end
			if sfunc == "clan" then
				if not self:CheckPlugin(player, "clans", 1) then return end
				local PlayerSlots, AddSub, ModSlots, Modify = args[3], "1", string.sub(args[3], 1, 1), self:Lang(player, "Decreased")
				if ModSlots == "-" or ModSlots == "+" then
					if ModSlots == "+" then
						AddSub = "2"
						Modify = self:Lang(player, "Increased")
					end
					PlayerSlots = tonumber(string.sub(PlayerSlots, 2, 3))
					if not tonumber(PlayerSlots) or PlayerSlots < 1 or PlayerSlots > 29 then
						local message = FormatMessage(self:Lang(player, "NotNumber"), { maxnum = "29" })
						self:RustMessage(player, message)
						return
					end
					local TargetPlayer, NewPlayerSlots, CurData = args[2]
					if TargetPlayer == "*" then
						for current, data in pairs(Data_CC) do
							if data.Slots then
								CurData = data.Slots
								else
								CurData = self:GetInitialSlots(2)
							end
							if AddSub == "1" then
								NewPlayerSlots = CurData - PlayerSlots
								else
								NewPlayerSlots = CurData + PlayerSlots
							end
							if NewPlayerSlots > 30 then NewPlayerSlots = 30 end
							if NewPlayerSlots < 1 then NewPlayerSlots = 1 end
							data.Slots = NewPlayerSlots
						end
						self:SaveDataFile(4)
						if self.Config.Settings.GlobalAdminMessage == "true" then
							local message = FormatMessage(self:Lang(nil, "ModGlobalAlert"), { modify = Modify, bank = self:Lang(nil, "GroupClan"), slots = PlayerSlots })
							self:RustBroadcast(message)
							else
							local message = FormatMessage(self:Lang(player, "ModPlayerSlots"), { bank = "", player = self:Lang(player, "AllClans"), modify = Modify, slots = PlayerSlots })
							self:RustMessage(player, message)
						end
						else
						local ClanList = clans:Call("GetAllClans")
						for line in string.gmatch(tostring(ClanList),"\"[^\r\n]+") do
							if line:gsub("\"", "") == TargetPlayer then
								local playerData = self:GetPlayerData_CC(TargetPlayer, true)
								if playerData.Slots == nil or not playerData.Slots then
									playerData.Slots = self:GetInitialSlots(2)
									self:SaveDataFile(4)
									CurData = self:GetInitialSlots(2)
									else
									CurData = playerData.Slots
								end
								if AddSub == "1" then
									NewPlayerSlots = CurData - PlayerSlots
									else
									NewPlayerSlots = CurData + PlayerSlots
								end
								if NewPlayerSlots > 30 then NewPlayerSlots = 30 end
								if NewPlayerSlots < 1 then NewPlayerSlots = 1 end
								playerData.Slots = NewPlayerSlots
								self:SaveDataFile(4)
								local message = FormatMessage(self:Lang(player, "ModPlayerSlots"), { bank = self:Lang(player, "GroupClan"):gsub("^%l", string.upper), player = TargetPlayer, modify = Modify, slots = PlayerSlots })
								self:RustMessage(player, message)
								return
							end
						end
						local message = FormatMessage(self:Lang(player, "NoClanExists"), { clan = args[2] })
						self:RustMessage(player, message)
					end
					else
					PlayerSlots = tonumber(args[3])
					if not tonumber(PlayerSlots) or PlayerSlots < 1 or PlayerSlots > 30 then
						local message = FormatMessage(self:Lang(player, "NotNumber"), { maxnum = "30" })
						self:RustMessage(player, message)
						return
					end
					local TargetPlayer = args[2]
					if TargetPlayer == "*" then
						for current, data in pairs(Data_CC) do
							data.Slots = PlayerSlots
						end
						self:SaveDataFile(4)
						if self.Config.Settings.GlobalAdminMessage == "true" then
							local message = FormatMessage(self:Lang(nil, "SetGlobalAlert"), { bank = self:Lang(nil, "GroupClan"), slots = PlayerSlots })
							self:RustBroadcast(message)
							else
							local message = FormatMessage(self:Lang(player, "SetPlayerSlots"), { bank = "", player = self:Lang(player, "AllClans"), slots = PlayerSlots })
							self:RustMessage(player, message)
						end
						else
						local ClanList = clans:Call("GetAllClans")
						for line in string.gmatch(tostring(ClanList),"\"[^\r\n]+") do
							if line:gsub("\"", "") == TargetPlayer then
								local playerData = self:GetPlayerData_CC(TargetPlayer, true)
								playerData.Slots = PlayerSlots
								self:SaveDataFile(4)
								local message = FormatMessage(self:Lang(player, "SetPlayerSlots"), { bank = self:Lang(player, "GroupClan"):gsub("^%l", string.upper), player = TargetPlayer, slots = PlayerSlots })
								self:RustMessage(player, message)
								return
							end
						end
						local message = FormatMessage(self:Lang(player, "NoClanExists"), { clan = args[2] })
						self:RustMessage(player, message)
					end
				end
			end
			return
		end
		if func == "limits" then
			if args.Length < 2 then
				self:RustMessage(player, self:Lang(player, "WrongArgs"))
				return
			end
			local sfunc = args[1]
			if sfunc ~= "system" and sfunc ~= "bank" and sfunc ~= "clan" and sfunc ~= "economics" then
				self:RustMessage(player, self:Lang(player, "WrongArgs"))
				return
			end
			if sfunc == "system" then
				local message = FormatMessage(self:Lang(player, "LimitsSystem"), { l1 = self.Config.Settings.Radius, l2 = self.Config.Settings.Ground, l3 = self.Config.Settings.Tier, l4 = self.Config.Settings.BuildingBlocked })
				self:RustMessage(player, message)
			end
			if sfunc == "bank" then
				local MaxShare = self.Config.Player.MaxShare
				local found, CustomPlayerMaxSlots, CustomPlayerOpenCost, CustomPlayerPercentOpenCost, CustomPlayerSlotCost, CustomPlayerPercentSlotCost, CustomMaxShare, CustomItems = self:CheckCustomPermission(playerSteamID)
				if found then MaxShare = CustomMaxShare end
				local message = FormatMessage(self:Lang(player, "LimitsBank"), { l1 = self.Config.Player.Enabled, l2 = self.Config.Player.ShareEnabled, l3 = self.Config.Player.DefaultSlots, l4 = MaxShare, l5 = self.Config.Player.KeepDurability, l6 = self.Config.Player.Cooldown })
				self:RustMessage(player, message)
			end
			if sfunc == "clan" then
				local message = FormatMessage(self:Lang(player, "LimitsClan"), { l1 = self.Config.Clan.Enabled, l2 = self.Config.Clan.DefaultSlots, l3 = self.Config.Clan.MinMembers, l4 = self.Config.Clan.KeepDurability, l5 = self.Config.Clan.Cooldown })
				self:RustMessage(player, message)
			end
			if sfunc == "economics" then
				if not self:CheckPlugin(player, "economics", 1) then return end
				local PlayerMaxSlots = self.Config.Economics.PlayerMaxSlots
				local PlayerOpenCost = self.Config.Economics.PlayerOpenCost
				local PlayerPercentOpenCost = self.Config.Economics.PlayerPercentOpenCost
				local PlayerSlotCost = self.Config.Economics.PlayerSlotCost
				local PlayerPercentSlotCost = self.Config.Economics.PlayerPercentSlotCost
				local found, CustomPlayerMaxSlots, CustomPlayerOpenCost, CustomPlayerPercentOpenCost, CustomPlayerSlotCost, CustomPlayerPercentSlotCost, CustomMaxShare, CustomItems = self:CheckCustomPermission(playerSteamID)
				if found then
					PlayerMaxSlots = CustomPlayerMaxSlots
					PlayerOpenCost = CustomPlayerOpenCost
					PlayerPercentOpenCost = CustomPlayerPercentOpenCost
					PlayerSlotCost = CustomPlayerSlotCost
					PlayerPercentSlotCost = CustomPlayerPercentSlotCost
				end
				local message = FormatMessage(self:Lang(player, "LimitsEconomics"), { l1 = self.Config.Economics.Enabled, l2 = self.Config.Economics.Confirmation, l3 = self.Config.Economics.ConfirmExpire, l4 = self.Config.Economics.PlayerInitialSlots, l5 = PlayerMaxSlots, l6 = comma(tonumber(PlayerOpenCost)), l7 = comma(tonumber(PlayerPercentOpenCost)), l8 = PlayerSlotCost, l9 = PlayerPercentSlotCost, l10 = self.Config.Economics.ClanInitialSlots, l11 = self.Config.Economics.ClanMaxSlots, l12 = comma(tonumber(self.Config.Economics.ClanOpenCost)), l13 = comma(tonumber(self.Config.Economics.ClanPercentOpenCost)), l14 = self.Config.Economics.ClanSlotCost, l15 = self.Config.Economics.ClanPercentSlotCost })
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
				local found, CustomPlayerMaxSlots, CustomPlayerOpenCost, CustomPlayerPercentOpenCost, CustomPlayerSlotCost, CustomPlayerPercentSlotCost, CustomMaxShare, CustomItems = self:CheckCustomPermission(playerSteamID)
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
		if func == "buy" then
			if self.Config.Economics.Enabled ~= "true" then
				local message = FormatMessage(self:Lang(player, "NotEnabled"), { group = self:Lang(player, "GroupEconomics") })
				self:RustMessage(player, message)
				return
			end
			if not self:CheckPlugin(player, "economics", 1) then return end
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
				local playerData, PlayerSlots = self:GetPlayerData_PS(playerSteamID, true)
				if playerData.Slots == nil or not playerData.Slots then
					playerData.Slots = self:GetInitialSlots(1)
					self:SaveDataFile(2)
					PlayerSlots = self:GetInitialSlots(1)
					else
					PlayerSlots = tonumber(playerData.Slots)
				end
				if PlayerSlots >= tonumber(self.Config.Economics.PlayerMaxSlots) then
					local message = FormatMessage(self:Lang(player, "PlayerMaxSlots"), { maxslots = self.Config.Economics.PlayerMaxSlots })
					self:RustMessage(player, message)
					return
				end
				local NewSlots = tonumber(args[2])
				local MaxSlots = tonumber(self.Config.Economics.PlayerMaxSlots) - PlayerSlots
				if not tonumber(NewSlots) or NewSlots < 1 or NewSlots > MaxSlots then
					local message = FormatMessage(self:Lang(player, "NotNumber"), { maxnum = MaxSlots })
					self:RustMessage(player, message)
					return
				end
				local Cost = tonumber(self.Config.Economics.PlayerSlotCost)
				local UpCost = tonumber(self.Config.Economics.PlayerPercentSlotCost)
				local found, CustomPlayerMaxSlots, CustomPlayerOpenCost, CustomPlayerPercentOpenCost, CustomPlayerSlotCost, CustomPlayerPercentSlotCost, CustomMaxShare, CustomItems = self:CheckCustomPermission(playerSteamID)
				if found then
					Cost = tonumber(CustomPlayerSlotCost)
					UpCost = tonumber(CustomPlayerPercentSlotCost)
				end
				local Percentage = (Cost / 100 * UpCost) * ((PlayerSlots + NewSlots) - self:GetInitialSlots(1))
				Cost = Cost + Percentage
				local Money = self:EconomicsMoney(player)
				if tonumber(Money) < Cost then
					local message = FormatMessage(self:Lang(player, "InsufficientMoneySlot"), { cost = comma(Cost), current = comma(Money) })
					self:RustMessage(player, message)
					return
				end
				if self.Config.Economics.Confirmation == "true" then
					ConfirmPurchase[playerSteamID] = "1:"..NewSlots..":"..Cost
					local message = FormatMessage(self:Lang(player, "ConfirmPurchase"), { slots = NewSlots, cost = comma(Cost), expire = self.Config.Economics.ConfirmExpire })
					self:RustMessage(player, message)
					if Expire[playerSteamID] then Expire[playerSteamID]:Destroy() end
					Expire[playerSteamID] = timer.Once(tonumber(self.Config.Economics.ConfirmExpire), function()
						if ConfirmPurchase[playerSteamID] and ConfirmPurchase[playerSteamID] ~= nil then
							ConfirmPurchase[playerSteamID] = nil
						end
					end)
					else
					self:EconomicsWithdraw(player, Cost)
					playerData.Slots = playerData.Slots + NewSlots
					self:SaveDataFile(2)
					local message = FormatMessage(self:Lang(player, "CompletePurchase"), { slots = NewSlots, cost = comma(Cost) })
					self:RustMessage(player, message)
				end
			end
			if sfunc == "clan" then
				if not self:CheckPlugin(player, "clans", 1) then return end
				local found, playerClan, playerGroup, count = self:GetClanMember(player)
				if not found then return end
				if self.Config.Economics.ClanMemberBuy ~= "true" then
					if playerGroup ~= "owner" and playerGroup ~= "moderator" then
						self:RustMessage(player, self:Lang(player, "ClanMemberBuy"))
						return
					end
				end
				local playerData, PlayerSlots = self:GetPlayerData_CC(playerClan, true)
				if playerData.Slots == nil or not playerData.Slots then
					playerData.Slots = self:GetInitialSlots(2)
					self:SaveDataFile(4)
					PlayerSlots = self:GetInitialSlots(2)
					else
					PlayerSlots = tonumber(playerData.Slots)
				end
				if PlayerSlots >= tonumber(self.Config.Economics.ClanMaxSlots) then
					local message = FormatMessage(self:Lang(player, "ClanMaxSlots"), { clan = playerClan, maxslots = self.Config.Economics.ClanMaxSlots })
					self:RustMessage(player, message)
					return
				end
				local NewSlots = tonumber(args[2])
				local MaxSlots = tonumber(self.Config.Economics.ClanMaxSlots) - PlayerSlots
				if not tonumber(NewSlots) or NewSlots < 1 or NewSlots > MaxSlots then
					local message = FormatMessage(self:Lang(player, "NotNumber"), { maxnum = MaxSlots })
					self:RustMessage(player, message)
					return
				end
				local Cost = tonumber(self.Config.Economics.ClanSlotCost)
				local UpCost = tonumber(self.Config.Economics.ClanPercentSlotCost)
				local Percentage = (Cost / 100 * UpCost) * ((PlayerSlots + NewSlots) - self:GetInitialSlots(2))
				Cost = Cost + Percentage
				local Money = self:EconomicsMoney(player)
				if tonumber(Money) < Cost then
					local message = FormatMessage(self:Lang(player, "InsufficientMoneySlot"), { cost = comma(Cost), current = comma(Money) })
					self:RustMessage(player, message)
					return
				end
				if self.Config.Economics.Confirmation == "true" then
					ConfirmPurchase[playerSteamID] = "2:"..NewSlots..":"..Cost
					local message = FormatMessage(self:Lang(player, "ConfirmPurchase"), { slots = NewSlots, cost = comma(Cost), expire = self.Config.Economics.ConfirmExpire })
					self:RustMessage(player, message)
					if Expire[playerSteamID] then Expire[playerSteamID]:Destroy() end
					Expire[playerSteamID] = timer.Once(tonumber(self.Config.Economics.ConfirmExpire), function()
						if ConfirmPurchase[playerSteamID] and ConfirmPurchase[playerSteamID] ~= nil then
							ConfirmPurchase[playerSteamID] = nil
						end
					end)
					else
					self:EconomicsWithdraw(player, Cost)
					playerData.Slots = playerData.Slots + NewSlots
					self:SaveDataFile(4)
					local message = FormatMessage(self:Lang(player, "CompletePurchase"), { slots = NewSlots, cost = comma(Cost) })
					self:RustMessage(player, message)
				end
			end
			return
		end
		if func == "confirm" then
			if self.Config.Economics.Enabled ~= "true" then
				local message = FormatMessage(self:Lang(player, "NotEnabled"), { group = self:Lang(player, "GroupEconomics") })
				self:RustMessage(player, message)
				return
			end
			if not self:CheckPlugin(player, "economics", 1) then return end
			if ConfirmPurchase[playerSteamID] and ConfirmPurchase[playerSteamID] ~= nil then
				if Expire[playerSteamID] then Expire[playerSteamID]:Destroy() end
				local Call, NewSlots, Cost = tostring(ConfirmPurchase[playerSteamID]):match("([^:]+):([^:]+):([^:]+)")
				self:EconomicsWithdraw(player, tonumber(Cost))
				local playerData
				if Call == "1" then playerData = self:GetPlayerData_PS(playerSteamID, true) end
				if Call == "2" then
					local found, playerClan, playerGroup, count = self:GetClanMember(player)
					if not found then
						ConfirmPurchase[playerSteamID] = nil
						return
					end
					playerData = self:GetPlayerData_CC(playerClan, true)
				end
				playerData.Slots = playerData.Slots + tonumber(NewSlots)
				if Call == "1" then self:SaveDataFile(2) end
				if Call == "2" then self:SaveDataFile(4) end
				local message = FormatMessage(self:Lang(player, "CompletePurchase"), { slots = NewSlots, cost = comma(tonumber(Cost)) })
				self:RustMessage(player, message)
				ConfirmPurchase[playerSteamID] = nil
				else
				self:RustMessage(player, self:Lang(player, "NoConfirmPurchase"))
			end
			return
		end
		if func == "bank" then
			if not permission.UserHasPermission(playerSteamID, "bankmanager.admin") then
				if self.Config.Player.Enabled ~= "true" then
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
			if not self:CheckPlugin(player, "clans", 1) then return end
			local sfunc
			if args.Length >= 2 then sfunc = args[1] end
			if sfunc == nil then
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
			if self.Config.Player.ShareEnabled ~= "true" then
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
			local found, targetplayer, targetname, targetid = self:CheckPlayer(player, args[1], "false")
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
				local MaxShare = self.Config.Player.MaxShare
				local found, CustomPlayerMaxSlots, CustomPlayerOpenCost, CustomPlayerPercentOpenCost, CustomPlayerSlotCost, CustomPlayerPercentSlotCost, CustomMaxShare, CustomItems = self:CheckCustomPermission(playerSteamID)
				if found then MaxShare = CustomMaxShare end
				if tonumber(#playerData.Shared) >= tonumber(MaxShare) then
					local message = FormatMessage(self:Lang(player, "MaxShare"), { limit = MaxShare })
					self:RustMessage(player, message)
					return
				end
			end
			local found, targetplayer, targetname, targetid = self:CheckPlayer(player, args[1], "false")
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
			local found, targetplayer, targetname, targetid = self:CheckPlayer(player, args[1], "false")
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
	local playerData = self:GetPlayerData_PS(_playerSteamID, true)
	if playerData.Slots == nil or not playerData.Slots then
		playerData.Slots = self:GetInitialSlots(1)
		self:SaveDataFile(2)
	end
	local Cost = 0
	if self.Config.Economics.Enabled == "true" and self:CheckPlugin(player, "economics", 0) then
		if not permission.UserHasPermission(playerSteamID, "bankmanager.admin") then
			Cost = tonumber(self.Config.Economics.PlayerOpenCost)
			local UpCost = tonumber(self.Config.Economics.PlayerPercentOpenCost)
			local found, CustomPlayerMaxSlots, CustomPlayerOpenCost, CustomPlayerPercentOpenCost, CustomPlayerSlotCost, CustomPlayerPercentSlotCost, CustomMaxShare, CustomItems = self:CheckCustomPermission(playerSteamID)
			if found then
				Cost = tonumber(CustomPlayerOpenCost)
				UpCost = tonumber(CustomPlayerPercentOpenCost)
			end
			local Percentage = (Cost / 100 * UpCost) * (playerData.Slots - self:GetInitialSlots(1))
			Cost = Cost + Percentage
			if Cost > 0 then
				local Money = self:EconomicsMoney(player)
				if tonumber(Money) < Cost then
					local message = FormatMessage(self:Lang(player, "InsufficientMoneyOpen"), { cost = comma(Cost), current = comma(Money) })
					self:RustMessage(player, message)
					return
				end
				self:EconomicsWithdraw(player, Cost)
			end
		end
	end
	BankOpened[playerSteamID] = "true"
	timer.Once(.5, function()
		local PlayerPos = player.transform.position
		PlayerPos.y = PlayerPos.y - 1
		local box = global.GameManager.server:CreateEntity("assets/prefabs/deployable/large wood storage/box.wooden.large.prefab", PlayerPos, player.transform.rotation)
		box:SendMessage("SetDeployedBy", player, UnityEngine.SendMessageOptions.DontRequireReceiver)
		if not permission.UserHasPermission(playerSteamID, "bankmanager.admin") then
			if self.Config.Economics.Enabled == "true" and self:CheckPlugin(player, "economics", 0) then
				box.inventorySlots = playerData.Slots
				else
				box.inventorySlots = self:GetInitialSlots(1)
			end
		end
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
				local item = self:SpawnItem(data.item, data.quantity)
				if self.Config.Player.KeepDurability == "true" and data.durability then item.condition = data.durability end
				if data.skin then
					item.skin = data.skin
					local ent = item:GetHeldEntity()
					if ent then ent.skinID = data.skin end
				end
				if data.attachments then
					for _, _data in pairs(data.attachments) do
						if item.contents:GetSlot(0) then item.contents:GetSlot(0):Remove(0) end
						local newItem = global.ItemManager.CreateByName(_data.id, _data.quantity)
						if self.Config.Player.KeepDurability == "true" and _data.durability then newItem.condition = _data.durability end
					timer.Once(1, function() newItem:MoveToContainer(item.contents) end)
					end
					end
					if data.ammo and data.ammo.id then
					local mag = item:GetHeldEntity().primaryMagazine
					local itemDef = global.ItemManager.FindItemDefinition.methodarray[1]:Invoke(nil, util.TableToArray({data.ammo.id}))
					mag.ammoType = itemDef
					mag.contents = data.ammo.quantity
				end
				if not item:MoveToContainer(loot, data.pos) then
					item:Drop(player:GetDropPosition(), player:GetDropVelocity(), player.transform.rotation)
				end
			end
		end
		local loot = box:GetComponent("StorageContainer")
		loot:PlayerOpenLoot(player)
		local message
		if self.Config.Economics.Enabled == "true" and self:CheckPlugin(player, "economics", 0) then
			message = FormatMessage(self:Lang(player, "BankOpenedCost"), { player = target.displayName, cost = comma(tonumber(Cost)) })
			else
			message = FormatMessage(self:Lang(player, "BankOpened"), { player = target.displayName })
		end
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
	local playerData = self:GetPlayerData_CC(playerClan, true)
	if playerData.Slots == nil or not playerData.Slots then
		playerData.Slots = self:GetInitialSlots(2)
		self:SaveDataFile(4)
	end
	local Cost = 0
	if self.Config.Economics.Enabled == "true" and self:CheckPlugin(player, "economics", 0) then
		if not permission.UserHasPermission(playerSteamID, "bankmanager.admin") then
			Cost = tonumber(self.Config.Economics.ClanOpenCost)
			local UpCost = tonumber(self.Config.Economics.ClanPercentOpenCost)
			local Percentage = (Cost / 100 * UpCost) * (playerData.Slots - self:GetInitialSlots(2))
			Cost = Cost + Percentage
			if Cost > 0 then
				local Money = self:EconomicsMoney(player)
				if tonumber(Money) < Cost then
					local message = FormatMessage(self:Lang(player, "InsufficientMoneyOpen"), { cost = comma(Cost), current = comma(Money) })
					self:RustMessage(player, message)
					return
				end
				self:EconomicsWithdraw(player, Cost)
			end
		end
	end
	BankOpened[playerSteamID] = "true"
	timer.Once(.5, function()
		local PlayerPos = player.transform.position
		PlayerPos.y = PlayerPos.y - 1
		local box = global.GameManager.server:CreateEntity("assets/prefabs/deployable/large wood storage/box.wooden.large.prefab", PlayerPos, player.transform.rotation)
		box:SendMessage("SetDeployedBy", player, UnityEngine.SendMessageOptions.DontRequireReceiver)
		if not permission.UserHasPermission(playerSteamID, "bankmanager.admin") then
			if self.Config.Economics.Enabled == "true" and self:CheckPlugin(player, "economics", 0) then
				box.inventorySlots = playerData.Slots
				else
				box.inventorySlots = self:GetInitialSlots(2)
			end
		end
		box.name = "bank:"..playerSteamID
		ClanBank[playerSteamID] = box
		ClanName[playerSteamID] = playerClan
		box:Spawn()
		ClanUser[playerClan] = player.displayName..":"..playerSteamID
		local playerData = self:GetPlayerData_CB(playerClan, true)
		if #playerData.Bank > 0 then
			local loot = box:GetComponent("StorageContainer").inventory
			for current, data in pairs(playerData.Bank) do
				local item = self:SpawnItem(data.item, data.quantity)
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
		local message
		if self.Config.Economics.Enabled == "true" and self:CheckPlugin(player, "economics", 0) then
			message = FormatMessage(self:Lang(player, "ClanBankOpenedCost"), { clan = playerClan, cost = comma(tonumber(Cost)) })
			else
			message = FormatMessage(self:Lang(player, "ClanBankOpened"), { clan = playerClan })
		end
		self:RustMessage(player, message)
		CoolDown[playerSteamID] = time.GetUnixTimestamp()
	end)
end

function PLUGIN:SaveBank(player, call)
	local playerSteamID = rust.UserIDFromPlayer(player)
	local playerData, box, playerClan, found, CustomPlayerMaxSlots, CustomPlayerOpenCost, CustomPlayerPercentOpenCost, CustomPlayerSlotCost, CustomPlayerPercentSlotCost, CustomMaxShare, CustomItems
	if call == 1 then
		local TargetName, SteamID = "", playerSteamID
		if Shared[playerSteamID] then
			TargetName, SteamID = tostring(Shared[playerSteamID]):match("([^:]+):([^:]+)")
			found, CustomPlayerMaxSlots, CustomPlayerOpenCost, CustomPlayerPercentOpenCost, CustomPlayerSlotCost, CustomPlayerPercentSlotCost, CustomMaxShare, CustomItems = self:CheckCustomPermission(SteamID)
			else
			found, CustomPlayerMaxSlots, CustomPlayerOpenCost, CustomPlayerPercentOpenCost, CustomPlayerSlotCost, CustomPlayerPercentSlotCost, CustomMaxShare, CustomItems = self:CheckCustomPermission(SteamID)
		end
		playerData = self:GetPlayerData_PB(SteamID, true)
		box = Bank[playerSteamID]:GetComponent("StorageContainer")
		else
		playerClan = ClanName[playerSteamID]
		playerData = self:GetPlayerData_CB(playerClan, true)
		box = ClanBank[playerSteamID]:GetComponent("StorageContainer")
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
			end
			if SaveItem then
				local Stack = loot.Current.amount
				if not permission.UserHasPermission(playerSteamID, "bankmanager.admin") then
					if tonumber(loot.Current.amount) > tonumber(MaxStk) then
						if not string.match(Returned, "5") then Returned = Returned.."4" end
						Stack = tonumber(MaxStk)
						local item = self:SpawnItem(loot.Current.info.itemid, (loot.Current.amount - tonumber(MaxStk)))
						if not item:MoveToContainer(player.inventory.containerMain, -1) then
							item:Drop(player:GetDropPosition(), player:GetDropVelocity(), player.transform.rotation)
						end
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
		local message = FormatMessage(self:Lang(player, "Returned"), { reason = string.sub(Reason, 1, -3) })
		self:RustMessage(player, message)
	end
end

function PLUGIN:ReturnItem(player, loot, call)
	local KeepDurability = false
	if call == 1 and self.Config.Player.KeepDurability == "true" then KeepDurability = true end
	if call == 2 and self.Config.Clan.KeepDurability == "true" then KeepDurability = true end
	local item = self:SpawnItem(loot.info.itemid, loot.amount)
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
	if not item:MoveToContainer(player.inventory.containerMain, -1) then
		item:Drop(player:GetDropPosition(), player:GetDropVelocity(), player.transform.rotation)
	end
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

function PLUGIN:CheckPlayer(player, target, admin)
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
	if admin == "false" then
		if not permission.UserHasPermission(targetSteamID, "bankmanager.admin") and not permission.UserHasPermission(targetSteamID, "bankmanager.share") then
			local message = FormatMessage(self:Lang(player, "RequiredPermission"), { player = targetName })
			self:RustMessage(player, message)
			return false
		end
	end
	return true, target, targetName, targetSteamID
end

function PLUGIN:SpawnItem(ItemID, SpawnAmt)
	return global.ItemManager.CreateByItemID(tonumber(ItemID), tonumber(SpawnAmt), 0)
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
				return true, data.PlayerMaxSlots, data.PlayerOpenCost, data.PlayerPercentOpenCost, data.PlayerSlotCost, data.PlayerPercentSlotCost, data.MaxShare, data.Items
			end
		end
	end
	return false
end

function PLUGIN:CheckCooldown(player, call)
	local playerSteamID = rust.UserIDFromPlayer(player)
	if CoolDown[playerSteamID] then
		local Timestamp = time.GetUnixTimestamp()
		local Cooldown
		if call == 1 then Cooldown = tonumber(self.Config.Player.Cooldown) end
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

function PLUGIN:CheckPlugin(player, plugin, call)
	if plugin == "clans" then
		if not clans then
			if self.Config.Clan.Enabled == "true" then
				self.Config.Clan.Enabled = "false"
				self:SaveConfig()
			end
			if call == 1 then
				local message = FormatMessage(self:Lang(player, "NoPlugin"), { plugin = ClanPlugin })
				self:RustMessage(player, message)
			end
			return false
		end
		return true
	end
	if plugin == "npc" then
		if not npc then
			if self.Config.NPC.Enabled == "true" then
				self.Config.NPC.Enabled = "false"
				self:SaveConfig()
			end
			if call == 1 then
				local message = FormatMessage(self:Lang(player, "NoPlugin"), { plugin = NPCPlugin })
				self:RustMessage(player, message)
			end
			return false
		end
		return true
	end
	if plugin == "economics" then
		if not economics then
			if self.Config.Economics.Enabled == "true" then
				self.Config.Economics.Enabled = "false"
				self:SaveConfig()
			end
			if call == 1 then
				local message = FormatMessage(self:Lang(player, "NoPlugin"), { plugin = EconomicsPlugin })
				self:RustMessage(player, message)
			end
			return false
		end
		return true
	end
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

function PLUGIN:EconomicsMoney(player)
	if not self:CheckPlugin(player, "economics", 1) then return end
	local playerSteamID = rust.UserIDFromPlayer(player)
	return economics:Call("GetPlayerMoney", playerSteamID)
end

function PLUGIN:EconomicsWithdraw(player, cost)
	if not self:CheckPlugin(player, "economics", 1) then return end
	local playerSteamID = rust.UserIDFromPlayer(player)
	economics:Call("WithdrawS", playerSteamID, cost)
end

function PLUGIN:GetInitialSlots(call)
	if self.Config.Economics.Enabled == "true" then
		if call == 1 then return tonumber(self.Config.Economics.PlayerInitialSlots) end
		if call == 2 then return tonumber(self.Config.Economics.ClanInitialSlots) end
		else
		if call == 1 then return tonumber(self.Config.Player.DefaultSlots) end
		if call == 2 then return tonumber(self.Config.Clan.DefaultSlots) end
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
			if self.Config.NPC.Enabled ~= "true" then return end
			local playerSteamID = rust.UserIDFromPlayer(player)
			if self.Config.Player.Enabled ~= "true" and not permission.UserHasPermission(playerSteamID, "bankmanager.admin") then
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
			if self.Config.NPC.Enabled ~= "true" then return end
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

function PLUGIN:RustBroadcast(message)
	rust.BroadcastChat("<size="..tonumber(self.Config.Settings.MessageSize)..">"..self:Lang(nil, "Prefix")..message.."</size>")
end

function PLUGIN:SendHelpText(player)
	self:RustMessage(player, self:Lang(player, "Help"))
end