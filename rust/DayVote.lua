PLUGIN.Title        = "Day Vote"
PLUGIN.Description  = "Allows automatic or player voting to skip night cycles."
PLUGIN.Author       = "InSaNe8472"
PLUGIN.Version      = V(1,1,2)
PLUGIN.ResourceId   = 1275

local CanVote = true
local ActiveVote = false
local AllowRevote = false
local VoteList = {}
local VoteType = {}
local AutoCurrentNight = 0
local CurrentNight = 0
local AllowAirdrop = false
local AirdropControl = {}
local AirdropDelay = 1
local VoteRequired = ""
local VSky

function PLUGIN:Init()
	permission.RegisterPermission("dayvote.use", self.Plugin)
	permission.RegisterPermission("dayvote.admin", self.Plugin)
	command.AddChatCommand("dayvote", self.Plugin, "cmdDayVote")
	self:LoadDefaultConfig()
	self:LoadDefaultLang()
end

function PLUGIN:LoadDefaultConfig()
	self.Config.Airdrop = self.Config.Airdrop or {}
	self.Config.Settings = self.Config.Settings or {}
	self.Config.Vote = self.Config.Vote or {}
	self.Config.Revote = self.Config.Revote or {}
	self.Config.Airdrop.Enabled = self.Config.Airdrop.Enabled or "false"
	self.Config.Airdrop.Plugin = self.Config.Airdrop.Plugin or "EasyAirdrop"
	self.Config.Airdrop.Command = self.Config.Airdrop.Command or "massdrop 2"
	self.Config.Airdrop.AirdropWait = self.Config.Airdrop.AirdropWait or "false"
	self.Config.Airdrop.MinPlayers = self.Config.Airdrop.MinPlayers or "1"
	self.Config.Airdrop.Interval = self.Config.Airdrop.Interval or {
		"2400",
		"3000",
		"3600"
	}
	self.Config.Settings.MessageSize = self.Config.Settings.MessageSize or "12"
	self.Config.Settings.VoteMinPlayers = self.Config.Settings.VoteMinPlayers or "1"
	self.Config.Settings.AutoSkip = self.Config.Settings.AutoSkip or "false"
	self.Config.Settings.ShowTimeChanges = self.Config.Settings.ShowTimeChanges or "true"
	self.Config.Settings.UsePermissions = self.Config.Settings.UsePermissions or "true"
	self.Config.Settings.AllowAutoNights = self.Config.Settings.AllowAutoNights or "1"
	self.Config.Settings.AllowVoteNights = self.Config.Settings.AllowVoteNights or "1"
	self.Config.Settings.AllowVoteChange = self.Config.Settings.AllowVoteChange or "false"
	self.Config.Settings.DayHour = self.Config.Settings.DayHour or "6"
	self.Config.Settings.NightHour = self.Config.Settings.NightHour or "18"
	self.Config.Settings.AdminExempt = self.Config.Settings.AdminExempt or "false"
	self.Config.Settings.RoundPercent = self.Config.Settings.RoundPercent or "true"
	self.Config.Settings.ShowPlayerVotes = self.Config.Settings.ShowPlayerVotes or "false"
	self.Config.Settings.ShowDisconnectVotes = self.Config.Settings.ShowDisconnectVotes or "true"
	self.Config.Vote.Enabled = self.Config.Vote.Enabled or "true"
	self.Config.Vote.ShowProgress = self.Config.Vote.ShowProgress or "true"
	self.Config.Vote.ProgressInterval = self.Config.Vote.ProgressInterval or "25"
	self.Config.Vote.VoteDuration = self.Config.Vote.VoteDuration or "125"
	self.Config.Vote.VoteRequired = self.Config.Vote.VoteRequired or "50%"
	self.Config.Revote.Enabled = self.Config.Revote.Enabled or "false"
	self.Config.Revote.Announce = self.Config.Revote.Announce or "true"
	self.Config.Revote.WaitDuration = self.Config.Revote.WaitDuration or "30"
	self.Config.Revote.ShowProgress = self.Config.Revote.ShowProgress or "true"
	self.Config.Revote.ProgressInterval = self.Config.Revote.ProgressInterval or "15"
	self.Config.Revote.VoteDuration = self.Config.Revote.VoteDuration or "60"
	self.Config.Revote.VoteRequired = self.Config.Revote.VoteRequired or "40%"
	if not tonumber(self.Config.Airdrop.MinPlayers) or tonumber(self.Config.Airdrop.MinPlayers) < 1 then self.Config.Airdrop.MinPlayers = "1" end
	if self.Config.Airdrop.Interval == nil then self.Config.Airdrop.Interval = "3600" end
	if not tonumber(self.Config.Airdrop.Drops) or tonumber(self.Config.Airdrop.Drops) < 1 then self.Config.Airdrop.Drops = "1" end
	if not tonumber(self.Config.Settings.MessageSize) or tonumber(self.Config.Settings.MessageSize) < 1 then self.Config.Settings.MessageSize = "12" end
	if not tonumber(self.Config.Settings.VoteMinPlayers) or tonumber(self.Config.Settings.VoteMinPlayers) < 1 then self.Config.Settings.VoteMinPlayers = "1" end
	if not tonumber(self.Config.Vote.ProgressInterval) or tonumber(self.Config.Vote.ProgressInterval) < 1 then self.Config.Vote.ProgressInterval = "15" end
	if not tonumber(self.Config.Vote.VoteDuration) or tonumber(self.Config.Vote.VoteDuration) < 1 then self.Config.Vote.VoteDuration = "60" end
	if string.sub(self.Config.Settings.AllowAutoNights, 1, 1) == "+" then
		if not tonumber(string.sub(self.Config.Settings.AllowAutoNights, 2)) then
			self.Config.Settings.AllowAutoNights = "+1"
			else
			if not tonumber(self.Config.Settings.AllowAutoNights) or tonumber(self.Config.Settings.AllowAutoNights) < 1 then self.Config.Settings.AllowAutoNights = "1" end
		end
	end
	if string.sub(self.Config.Settings.AllowVoteNights, 1, 1) == "+" then
		if not tonumber(string.sub(self.Config.Settings.AllowVoteNights, 2)) then
			self.Config.Settings.AllowVoteNights = "+1"
			else
			if not tonumber(self.Config.Settings.AllowVoteNights) or tonumber(self.Config.Settings.AllowVoteNights) < 1 then self.Config.Settings.AllowVoteNights = "1" end
		end
	end
	if not tonumber(self.Config.Settings.DayHour) or tonumber(self.Config.Settings.DayHour) < 0 or tonumber(self.Config.Settings.DayHour) > 24 then self.Config.Settings.DayHour = "6" end
	if not tonumber(self.Config.Settings.NightHour) or tonumber(self.Config.Settings.NightHour) < 0 or tonumber(self.Config.Settings.NightHour) > 24 then self.Config.Settings.NightHour = "18" end
	if string.sub(self.Config.Vote.VoteRequired, -1) == "%" then
		local CheckVoteRequired = tonumber(string.sub(self.Config.Vote.VoteRequired, 1, -2))
		if not tonumber(CheckVoteRequired) or CheckVoteRequired < 10 or CheckVoteRequired > 100 then self.Config.Vote.VoteRequired = "50%" end
		isPercent = true
		else
		if not tonumber(self.Config.Vote.VoteRequired) or tonumber(self.Config.Vote.VoteRequired) < 1 then self.Config.Vote.VoteRequired = "3" end
		isPercent = false
	end
	if not tonumber(self.Config.Revote.WaitDuration) or tonumber(self.Config.Revote.WaitDuration) < 1 then self.Config.Revote.WaitDuration = "30" end
	if not tonumber(self.Config.Revote.ProgressInterval) or tonumber(self.Config.Revote.ProgressInterval) < 1 then self.Config.Revote.ProgressInterval = "15" end
	if not tonumber(self.Config.Revote.VoteDuration) or tonumber(self.Config.Revote.VoteDuration) < 1 then self.Config.Revote.VoteDuration = "60" end
	if string.sub(self.Config.Revote.VoteRequired, -1) == "%" then
		local CheckVoteRequired = tonumber(string.sub(self.Config.Revote.VoteRequired, 1, -2))
		if not tonumber(CheckVoteRequired) or CheckVoteRequired < 10 or CheckVoteRequired > 100 then self.Config.Revote.VoteRequired = "40%" end
		isPercentRV = true
		else
		if not tonumber(self.Config.Revote.VoteRequired) or tonumber(self.Config.Revote.VoteRequired) < 1 then self.Config.Revote.VoteRequired = "2" end
		isPercentRV = false
	end
	if self.Config.Airdrop.AirdropWait == "false" then AllowAirdrop = true end
	self:SaveConfig()
end

function PLUGIN:LoadDefaultLang()
	lang.RegisterMessages(util.TableToLangDict({
		["AdminExempt"] = "Day vote administrators a currently exempt from voting.",
		["AdminMenu"] = "\n	<color=#ffd479>/dayvote toggle <vote | revote | airdrop></color> - Enable or disable day vote system\n"..
		"	<color=#ffd479>/dayvote start [{limit}]</color> - Manually start a day vote with optional requirement\n"..
		"	<color=#ffd479>/dayvote stop</color> - Manually stop a day vote\n"..
		"	<color=#ffd479>/dayvote set <0-23></color> - Manually set current time",
		["AdminTimeSet"] = "Administrator set current time to <color=#cd422b>{set}</color> from <color=#cd422b>{current}</color>.",
		["Airdrop"] = "airdrop",
		["AlreadyVoted"] = "You have already voted <color=#cd422b>{vote}</color> for this day vote.",
		["AutoSkip"] = "Night cycle started and has automatically been skipped.",
		["AutoSkipError"] = "Day vote is <color=#cd422b>disabled</color>. Night cycles are automatically skipped every <color=#cd422b>{allowed}</color> nights.",
		["ChangedStatus"] = "Day vote system {system} <color=#cd422b>{status}</color>.",
		["Connected"] = "A day vote is currently in progress. To skip, use <color=#cd422b>/dayvote yes</color>, otherwise, <color=#cd422b>/dayvote no</color>. Yes/no votes required is <color=#cd422b>{required}</color>.Current progress: <color=#cd422b>{yes}</color> voted yes, <color=#cd422b>{no}</color> voted no.",
		["Disabled"] = "disabled",
		["DisconnectVotes"] = "<color=#cd422b>{player}'s</color> vote of <color=#cd422b>{vote}</color> has been removed for disconnecting.",
		["Enabled"] = "enabled",
		["Help"] = "<color=#ffd479>/dayvote</color> - Allows automatic or player voting to skip night cycles.",
		["LangError"] = "Language Error: {lang}",
		["LimitsSystem"] = "\n	Automatically skip night cycles: <color=#ffd479>{l1}</color>\n"..
		"	Allowed automatic skipping nights: <color=#ffd479>{l2}</color>\n"..
		"	Administrator exempt: <color=#ffd479>{l3}</color>\n"..
		"	Minimum online players required: <color=#ffd479>{l4}</color>\n"..
		"	Round percentage counts: <color=#ffd479>{l5}</color>\n"..
		"	Vote change allowed: <color=#ffd479>{l6}</color>\n"..
		"	Day hour: <color=#ffd479>{l7}</color>\n"..
		"	Night hour: <color=#ffd479>{l8}</color>",
		["LimitsVote"] = "\n	Allowed voting nights: <color=#ffd479>{l1}</color>\n"..
		"	Vote required: <color=#ffd479>{l2}</color>\n"..
		"	Vote duration: <color=#ffd479>{l3} seconds</color>",
		["LimitsRevote"] = "\n	Revote enabled: <color=#ffd479>{l1}</color>\n"..
		"	Wait duration: <color=#ffd479>{l2} seconds</color>\n"..
		"	Vote duration: <color=#ffd479>{l3} seconds</color>\n"..
		"	Vote required: <color=#ffd479>{l4}</color>",
		["ManualAutoSkipError"] = "You cannot manually start or stop a day vote when night cycles are automatically skipped.",
		["ManualPercentProgress"] = "Current progress: <color=#cd422b>{yes}%</color> (<color=#cd422b>{required}%</color> required) of <color=#ffd479>{players}</color> online players voted yes, <color=#cd422b>{no}%</color> voted no. Use <color=#cd422b>/dayvote yes</color>, otherwise, <color=#cd422b>/dayvote no</color>.",
		["ManualPercentVoteOpen"] = "Administrator started a skip night cycle vote. To skip, type <color=#cd422b>/dayvote yes</color>, otherwise, <color=#cd422b>/dayvote no</color>. Yes votes required is <color=#cd422b>{required}%</color> of <color=#ffd479>{players}</color> online players. Voting ends in <color=#cd422b>{seconds} seconds</color>.",
		["ManualProgress"] = "Current progress: <color=#ffd479>{yes} yes</color>, <color=#cd422b>{no} no</color>. Yes/no votes required is <color=#cd422b>{required}</color>. Use <color=#cd422b>/dayvote yes</color>, otherwise, <color=#cd422b>/dayvote no</color>.",
		["ManualRevoteClose"] = "A day vote is not currently open. However, a pending <color=#f9169f>revote</color> has been aborted.",
		["ManualVoteClose"] = "Administrator aborted current day vote. <color=#cd422b>Night cycle will continue.</color>",
		["ManualVoteOpen"] = "Administrator started a skip night cycle vote. To skip, type <color=#cd422b>/dayvote yes</color>, otherwise, <color=#cd422b>/dayvote no</color>. Yes/no votes required is <color=#cd422b>{required}</color>. Voting ends in <color=#cd422b>{seconds} seconds</color>.",
		["Menu"] = "\n	<color=#ffd479>/dayvote limits <system | vote | revote></color> - View day vote limits and configuration\n"..
		"	<color=#ffd479>/dayvote progress</color> - View current day vote progress\n"..
		"	<color=#ffd479>/dayvote yes</color> - Vote yes to skip current night cycle\n"..
		"	<color=#ffd479>/dayvote no</color> - Vote no to skip current night cycle\n\n"..
		"	Current hour: <color=#ffd479>{hour}</color>\n"..
		"	Active day vote: <color=#ffd479>{status}</color>",
		["NoPermission"] = "You do not have permission to use this command.",
		["NotEnabled"] = "Day vote system is <color=#cd422b>disabled</color>.",
		["NotNumber"] = "Time must be a number between <color=#cd422b>0</color> and <color=#cd422b>23</color>.",
		["NoVote"] = "No day vote currently in progress. Wait until the night cycle has started.",
		["NoVoteChange"] = "You have already voted <color=#cd422b>{vote}</color> for this day vote. Permission to change your vote is disabled.",
		["PercentProgress"] = "Current progress: <color=#cd422b>{yes}%</color> (<color=#cd422b>{required}%</color> required) of <color=#ffd479>{players}</color> online players voted yes, <color=#cd422b>{no}%</color> voted no. Use <color=#cd422b>/dayvote yes</color>, otherwise, <color=#cd422b>/dayvote no</color>. Voting ends in <color=#cd422b>{seconds} seconds</color>.",
		["PercentVoteFailed"] = "<color=#cd422b>Voting is closed.</color> Day vote failed, <color=#cd422b>{yes}%</color> of <color=#ffd479>{players}</color> online players voted yes, <color=#cd422b>{no}%</color> voted no. Yes votes required is <color=#cd422b>{required}%</color>. Night cycle will continue.",
		["PercentVoteOpen"] = "Night cycle has started. To skip, use <color=#cd422b>/dayvote yes</color>, otherwise, <color=#cd422b>/dayvote no</color>. Yes votes required is <color=#cd422b>{required}%</color> of <color=#ffd479>{players}</color> online players. Voting ends in <color=#cd422b>{seconds} seconds</color>.",
		["PercentVotePassed"] = "<color=#cd422b>Voting is closed.</color> Day vote passed, <color=#cd422b>{required}%</color> yes vote requirement of <color=#ffd479>{players}</color> online players reached, <color=#cd422b>{no}%</color> voted no. Night cycle will end.",
		["PlayerVotes"] = "<color=#cd422b>{player}</color> has voted <color=#ffd479>{vote}</color>.",
		["PlusAutoSkipError"] = "Day vote is <color=#cd422b>disabled</color>. Night cycles are automatically skipped. Night cycles are forced every <color=#cd422b>{allowed}</color> nights.",
		["PlusVoteSkip"] = "Night cycle has started. Night cycle is forced every <color=#cd422b>{allowed}</color> nights. Night cycle will continue.",
		["PrecentConnected"] = "A day vote is currently in progress. To skip, use <color=#cd422b>/dayvote yes</color>, otherwise, <color=#cd422b>/dayvote no</color>. Yes votes required is <color=#cd422b>{required}%</color> of <color=#ffd479>{players}</color> online players. Current progress: <color=#cd422b>{yes}%</color> voted yes, <color=#cd422b>{no}%</color> voted no.",
		["Prefix"] = "[<color=#cd422b> Day Vote </color>] ",
		["RevotePrefix"] = "[<color=#f9169f> Revote </color>] ",
		["Progress"] = "Current progress: <color=#ffd479>{yes} yes</color>, <color=#cd422b>{no} no</color>. Yes/no votes required is <color=#cd422b>{required}</color>. Use <color=#cd422b>/dayvote yes</color>, otherwise, <color=#cd422b>/dayvote no</color>. Voting ends in <color=#cd422b>{seconds} seconds</color>.",
		["Revote"] = "revote",
		["RevoteAlert"] = "A one time only day revote will begin in <color=#cd422b>{seconds} seconds</color>.",
		["RevoteWait"] = "A pending <color=#f9169f>revote</color> is already scheduled to start in less than <color=#cd422b>{seconds} seconds</color>.",
		["SetRevoteClose"] = "A pending <color=#f9169f>revote</color> has been aborted.",
		["SetToNightHour"] = "Current time set to configured night hour. No day vote will be started or night cycle automatically skipped.",
		["SetVoteClose"] = "Administrator aborted current day vote by manually setting new time. <color=#cd422b>New time will now take effect.</color>",
		["SkipAutoSkip"] = "Night cycle has started. Automatic skipping allowed every <color=#cd422b>{allowed}</color> nights. Current night cycle is <color=#cd422b>{current}</color>. Night cycle will continue.",
		["SkipPlusAutoSkip"] = "Night cycle has started. Night cycle is forced every <color=#cd422b>{allowed}</color> nights. Night cycle will continue.",
		["TimeSet"] = "Current time set to <color=#cd422b>{set}</color> from <color=#cd422b>{current}</color>.",
		["Vote"] = "vote",
		["VoteAlreadyOpen"] = "A day vote is currently in progress.",
		["Voted"] = "You have successfully voted <color=#cd422b>{vote}</color> for this day vote.",
		["VoteFailed"] = "<color=#cd422b>Voting is closed.</color> Day vote failed, <color=#cd422b>{no} no</color> to <color=#ffd479>{yes} yes</color> votes. Yes/no votes required is <color=#cd422b>{required}</color>. Night cycle will continue.",
		["VoteFailedNoVote"] = "<color=#cd422b>Voting is closed.</color> Day vote failed, no votes were cast. Night cycle will continue.",
		["VoteMinPlayers"] = "You cannot manually start a day vote, minimum online players of <color=#cd422b>{minimum}</color> was not reached. Current online players is <color=#cd422b>{current}</color>.",
		["VoteNotNight"] = "You cannot manually start a day vote, current time of <color=#cd422b>{current}</color> cannot be between configured day and night hours of <color=#cd422b>{day}</color> and <color=#cd422b>{night}</color>.",
		["VoteNotOpen"] = "A day vote is not currently in progress.",
		["VoteOpen"] = "Night cycle has started. To skip, use <color=#cd422b>/dayvote yes</color>, otherwise, <color=#cd422b>/dayvote no</color>. Yes/no votes required is <color=#cd422b>{required}</color>. Voting ends in <color=#cd422b>{seconds} seconds</color>.",
		["VotePassed"] = "<color=#cd422b>Voting is closed.</color> Day vote passed, <color=#ffd479>{yes} yes</color> to <color=#cd422b>{no} no</color> votes. Night cycle will end.",
		["VoteSkip"] = "Night cycle has started. Day votes allowed every <color=#cd422b>{allowed}</color> nights. Current night cycle is <color=#cd422b>{current}</color>. Night cycle will continue.",
		["WrongArgs"] = "Syntax error. Use <color=#cd422b>/dayvote</color> for help."
	}), self.Plugin)
end

function PLUGIN:getPlayerCount()
	local players, pCount = global.BasePlayer.activePlayerList:GetEnumerator(), 0
	while players:MoveNext() do
		if self.Config.Settings.AdminExempt == "true" then
			local playerSteamID = rust.UserIDFromPlayer(players.Current)
			if not permission.UserHasPermission(playerSteamID, "dayvote.admin") then
				pCount = pCount + 1
			end
			else
			pCount = pCount + 1
		end
	end
	return pCount
end

function PLUGIN:OnServerInitialized()
	AirdropControl = plugins.Find(self.Config.Airdrop.Plugin)
	if AirdropControl then
		if self.Config.Airdrop.Enabled == "true" then
			AirDropTimer = timer.Once(tonumber(self.Config.Airdrop.Interval[AirdropDelay]), function() self:RunAirDrop() end, self.Plugin)
		end
	end
end

function PLUGIN:RunAirDrop()
	if AllowAirdrop then
		local pCount = self:getPlayerCount()
		if pCount >= tonumber(self.Config.Airdrop.MinPlayers) then
			rust.RunServerCommand(self.Config.Airdrop.Command)
		end
		AirdropDelay = AirdropDelay + 1
	end
	if AirdropDelay > #self.Config.Airdrop.Interval then AirdropDelay = 1 end
	local Delay = self.Config.Airdrop.Interval[AirdropDelay]
	if not tonumber(Delay) or Delay == nil then Delay = "3600" end
	AirDropTimer = timer.Once(tonumber(Delay), function() self:RunAirDrop() end, self.Plugin)
	return
end

function PLUGIN:OnPluginUnloaded()
	AirdropControl = plugins.Find(self.Config.Airdrop.Plugin)
	if not AirdropControl and AirDropTimer then
		AirDropTimer:Destroy()
		AirDropTimer = nil
	end
end

function PLUGIN:SetDayTime()
	if not AllowAirdrop then AllowAirdrop = true end
	VSky.Cycle.Hour = tonumber(self.Config.Settings.DayHour)
end

function PLUGIN:OnTick()
	if not VSky then VSky = global.TOD_Sky.get_Instance() end
	if self.Config.Vote.Enabled == "true" then
		if not CanVote then
			if tostring(VSky.Cycle.Hour):match"([^.]*).(.*)" == self.Config.Settings.DayHour then
				CanVote = true
			end
			else
			if self.Config.Settings.AutoSkip == "true" then
				if tostring(VSky.Cycle.Hour):match"([^.]*).(.*)" == self.Config.Settings.NightHour then
					self:newVote("auto", false)
				end
				else
				if not ActiveVote then
					if tostring(VSky.Cycle.Hour):match"([^.]*).(.*)" == self.Config.Settings.NightHour then
						self:newVote("cycle", false)
					end
				end
			end
		end
	end
end

local function FormatMessage(message, values)
	for key, value in pairs(values) do message = message:gsub("{" .. key .. "}", value) end
	return message
end

function PLUGIN:Lang(player, lng)
	local playerSteamID
	if player and player ~= nil then playerSteamID = rust.UserIDFromPlayer(player) end
	local message = lang.GetMessage(lng, self.Plugin, playerSteamID)
	if message == lng then message = FormatMessage(self:Lang(player, "LangError"), { lang = lng }) end
	return message
end

function PLUGIN:OnPlayerInit(player)
	local playerSteamID = rust.UserIDFromPlayer(player)
	if permission.UserHasPermission(playerSteamID, "dayvote.admin") and self.Config.Settings.AdminExempt == "true" then return end
	if ActiveVote then
		local message = {}
		if VoteEnd then
			if isPercent then
				local pCount = self:getPlayerCount()
				local yes, no = self:GetPercentCount(pCount)
				message = FormatMessage(self:Lang(player, "PercentConnected"), { required = string.sub(self.Config.Vote.VoteRequired, 1, -2), players = pCount, yes = yes, no = no })
				else
				message = FormatMessage(self:Lang(player, "Connected"), { required = self.Config.Vote.VoteRequired, yes = VoteType["yes"], no = VoteType["no"] })
			end
			self:RustMessage(player, message)
		end
		if RevoteEnd then
			if isPercentRV then
				local pCount = self:getPlayerCount()
				local yes, no = self:GetPercentCount(pCount)
				message = FormatMessage(self:Lang(player, "PrecentConnected"), { required = string.sub(self.Config.Revote.VoteRequired, 1, -2), players = pCount, yes = yes, no = no })
				else
				message = FormatMessage(self:Lang(player, "Connected"), { required = self.Config.Revote.VoteRequired, yes = VoteType["yes"], no = VoteType["no"] })
			end
			self:RustMessage(player, self:Lang(player, "RevotePrefix").." "..message)
		end
	end
end

function PLUGIN:OnPlayerDisconnected(player)
	if ActiveVote then
		local playerSteamID = rust.UserIDFromPlayer(player)
		if VoteList[playerSteamID] then
			if VoteList[playerSteamID] == "yes" then VoteType["yes"] = VoteType["yes"] - 1 end
			if VoteList[playerSteamID] == "no" then VoteType["no"] = VoteType["no"] - 1 end
			if self.Config.Settings.ShowDisconnectVotes == "true" then
				local message = FormatMessage(self:Lang(player, "DisconnectVotes"), { player = player.displayName, vote = VoteList[playerSteamID] })
				self:RustBroadcast(message)
			end
			VoteList[playerSteamID] = nil
		end
		self:CheckPercentVote()
	end
end

function PLUGIN:cmdDayVote(player, cmd, args)
	local playerSteamID = rust.UserIDFromPlayer(player)
	if self.Config.Settings.UsePermissions == "true" then
		if not permission.UserHasPermission(playerSteamID, "dayvote.admin") then
			if not permission.UserHasPermission(playerSteamID, "dayvote.use") then
				self:RustMessage(player, self:Lang(player, "NoPermission"))
				return
			end
		end
	end
	if not permission.UserHasPermission(playerSteamID, "dayvote.admin") then
		if self.Config.Vote.Enabled == "false" then
			self:RustMessage(player, self:Lang(player, "NotEnabled"))
			return
		end
	end
	if args.Length == 0 then
		if permission.UserHasPermission(playerSteamID, "dayvote.admin") then
			local Limit = "1+"
			if isPercent then Limit = "10-100" end
			local message = FormatMessage(self:Lang(player, "AdminMenu"), { limit = Limit })
			self:RustMessage(player, message)
		end
		local message = FormatMessage(self:Lang(player, "Menu"), { hour = tostring(VSky.Cycle.Hour):match"([^.]*).(.*)", status = tostring(ActiveVote) })
		self:RustMessage(player, message)
		return
		elseif args.Length > 0 then
		local func = args[0]
		if func ~= "toggle" and func ~= "start" and func ~= "stop" and func ~= "set" and func ~= "limits" and func ~= "progress" and func ~= "yes" and func ~= "no" then
			self:RustMessage(player, self:Lang(player, "WrongArgs"))
			return
		end
		if func == "toggle" then
			if not permission.UserHasPermission(playerSteamID, "dayvote.admin") then
				self:RustMessage(player, self:Lang(player, "NoPermission"))
				return
			end
			if args.Length < 2 then
				self:RustMessage(player, self:Lang(player, "WrongArgs"))
				return
			end
			local sfunc = args[1]
			if sfunc ~= "vote" and sfunc ~= "revote" and sfunc ~= "airdrop" then
				self:RustMessage(player, self:Lang(player, "WrongArgs"))
				return
			end
			local message
			if sfunc == "vote" then
				if self.Config.Vote.Enabled == "true" then
					if ActiveVote then
						ActiveVote = false
						self:DestroyTimers()
						self:RustBroadcast(self:Lang(player, "ManualVoteClose"))
					end
					if RevoteWait then
						self:DestroyTimers()
						self:RustMessage(player, self:Lang(player, "ManualRevoteClose"))
					end
					self.Config.Vote.Enabled = "false"
					message = FormatMessage(self:Lang(player, "ChangedStatus"), { system = self:Lang(player, "Vote"), status = self:Lang(player, "Disabled") })
					else
					self.Config.Vote.Enabled = "true"
					message = FormatMessage(self:Lang(player, "ChangedStatus"), { system = self:Lang(player, "Vote"), status = self:Lang(player, "Enabled") })
				end
			end
			if sfunc == "revote" then
				if self.Config.Revote.Enabled == "true" then
					if ActiveVote then
						ActiveVote = false
						self:DestroyTimers()
						self:RustBroadcast(self:Lang(player, "ManualVoteClose"))
					end
					if RevoteWait then
						self:DestroyTimers()
						self:RustMessage(player, self:Lang(player, "ManualRevoteClose"))
					end
					self.Config.Revote.Enabled = "false"
					message = FormatMessage(self:Lang(player, "ChangedStatus"), { system = self:Lang(player, "Revote"), status = self:Lang(player, "Disabled") })
					else
					self.Config.Revote.Enabled = "true"
					message = FormatMessage(self:Lang(player, "ChangedStatus"), { system = self:Lang(player, "Revote"), status = self:Lang(player, "Enabled") })
				end
			end
			if sfunc == "airdrop" then
				if self.Config.Airdrop.Enabled == "true" then
					self.Config.Airdrop.Enabled = "false"
					message = FormatMessage(self:Lang(player, "ChangedStatus"), { system = self:Lang(player, "Airdrop"), status = self:Lang(player, "Disabled") })
					else
					self.Config.Airdrop.Enabled = "true"
					message = FormatMessage(self:Lang(player, "ChangedStatus"), { system = self:Lang(player, "Airdrop"), status = self:Lang(player, "Enabled") })
				end
			end
			self:SaveConfig()
			self:RustMessage(player, message)
			return
		end
		if func == "start" then
			if not permission.UserHasPermission(playerSteamID, "dayvote.admin") then
				self:RustMessage(player, self:Lang(player, "NoPermission"))
				return
			end
			if self.Config.Vote.Enabled == "false" then
				self:RustMessage(player, self:Lang(player, "NotEnabled"))
				return
			end
			if self.Config.Settings.AutoSkip == "true" then
				self:RustMessage(player, self:Lang(player, "ManualAutoSkipError"))
				return
			end
			local pCount = self:getPlayerCount()
			if pCount < tonumber(self.Config.Settings.VoteMinPlayers) then
				local message = FormatMessage(self:Lang(player, "VoteMinPlayers"), { minimum = self.Config.Settings.VoteMinPlayers, current = pCount })
				self:RustMessage(player, message)
				return
			end
			if ActiveVote then
				self:RustMessage(player, self:Lang(player, "VoteAlreadyOpen"))
				return
			end
			if RevoteWait then
				local message = FormatMessage(self:Lang(player, "RevoteWait"), { seconds = self.Config.Revote.WaitDuration })
				self:RustMessage(player, message)
				return
			end
			local CurTime = tostring(VSky.Cycle.Hour):match"([^.]*).(.*)"
			if tonumber(CurTime) >= tonumber(self.Config.Settings.DayHour) and tonumber(CurTime) < tonumber(self.Config.Settings.NightHour) then
				local message = FormatMessage(self:Lang(player, "VoteNotNight"), { current = CurTime, day = self.Config.Settings.DayHour, night = self.Config.Settings.NightHour })
				self:RustMessage(player, message)
				return
			end
			if args.Length < 2 then
				self:newVote("admin", false)
				else
				local creq = tonumber(args[1])
				if isPercent then
					if not tonumber(creq) or creq < 10 or creq > 100 then
						self:RustMessage(player, self:Lang(player, "WrongArgs"))
						return
					end
					else
					if not tonumber(creq) or creq < 1 then
						self:RustMessage(player, self:Lang(player, "WrongArgs"))
						return
					end
				end
				self:newVote("admin", creq)
			end
			return
		end
		if func == "stop" then
			if not permission.UserHasPermission(playerSteamID, "dayvote.admin") then
				self:RustMessage(player, self:Lang(player, "NoPermission"))
				return
			end
			if self.Config.Vote.Enabled == "false" then
				self:RustMessage(player, self:Lang(player, "NotEnabled"))
				return
			end
			if self.Config.Settings.AutoSkip == "true" then
				self:RustMessage(player, self:Lang(player, "ManualAutoSkipError"))
				return
			end
			if RevoteWait then
				self:DestroyTimers()
				self:RustMessage(player, self:Lang(player, "ManualRevoteClose"))
				return
			end
			if not ActiveVote then
				self:RustMessage(player, self:Lang(player, "VoteNotOpen"))
				return
			end
			ActiveVote = false
			self:DestroyTimers()
			self:RustBroadcast(self:Lang(player, "ManualVoteClose"))
			return
		end
		if func == "set" then
			if not permission.UserHasPermission(playerSteamID, "dayvote.admin") then
				self:RustMessage(player, self:Lang(player, "NoPermission"))
				return
			end
			if args.Length < 2 then
				self:RustMessage(player, self:Lang(player, "WrongArgs"))
				return
			end
			if not tonumber(args[1]) or tonumber(args[1]) < 0 or tonumber(args[1]) > 23.9 then
				self:RustMessage(player, self:Lang(player, "NotNumber"))
				return
			end
			if self.Config.Settings.ShowTimeChanges == "true" then
				local message = FormatMessage(self:Lang(player, "AdminTimeSet"), { set = tonumber(args[1]), current = tostring(VSky.Cycle.Hour):match"([^.]*).(.*)" })
				self:RustBroadcast(message)
				else
				local message = FormatMessage(self:Lang(player, "TimeSet"), { set = tonumber(args[1]), current = tostring(VSky.Cycle.Hour):match"([^.]*).(.*)" })
				self:RustMessage(player, message)
			end
			if tonumber(args[1]) == tonumber(self.Config.Settings.NightHour) then
				CanVote = false
				self:RustMessage(player, self:Lang(player, "SetToNightHour"))
				else
				CanVote = true
			end
			if ActiveVote then
				ActiveVote = false
				self:DestroyTimers()
				self:RustBroadcast(self:Lang(player, "SetVoteClose"))
				else
				if RevoteWait then
					self:DestroyTimers()
					self:RustMessage(player, self:Lang(player, "SetRevoteClose"))
				end
			end
			if not AllowAirdrop then AllowAirdrop = true end
			VSky.Cycle.Hour = tonumber(args[1])
			return
		end
		if func == "limits" then
			if args.Length < 2 then
				self:RustMessage(player, self:Lang(player, "WrongArgs"))
				return
			end
			local sfunc = args[1]
			if sfunc ~= "system" and sfunc ~= "vote" and sfunc ~= "revote" then
				self:RustMessage(player, self:Lang(player, "WrongArgs"))
				return
			end
			if sfunc == "system" then
				local message = FormatMessage(self:Lang(player, "LimitsSystem"), { l1 = self.Config.Settings.AutoSkip, l2 = self.Config.Settings.AllowAutoNights, l3 = self.Config.Settings.AdminExempt, l4 = self.Config.Settings.VoteMinPlayers, l5 = self.Config.Settings.RoundPercent, l6 = self.Config.Settings.AllowVoteChange, l7 = self.Config.Settings.DayHour, l8 = self.Config.Settings.NightHour })
				self:RustMessage(player, message)
			end
			if sfunc == "vote" then
				local message = FormatMessage(self:Lang(player, "LimitsVote"), { l1 = self.Config.Settings.AllowVoteNights, l2 = self.Config.Vote.VoteRequired:gsub("([^%w])", "%%%1"), l3 = self.Config.Vote.VoteDuration })
				self:RustMessage(player, message)
			end
			if sfunc == "revote" then
				local message = FormatMessage(self:Lang(player, "LimitsRevote"), { l1 = self.Config.Revote.Enabled, l2 = self.Config.Revote.WaitDuration, l3 = self.Config.Revote.VoteDuration, l4 = self.Config.Revote.VoteRequired:gsub("([^%w])", "%%%1") })
				self:RustMessage(player, message)
			end
			return
		end
		if self.Config.Settings.AutoSkip == "true" then
			local message = {}
			if string.sub(self.Config.Settings.AllowAutoNights, 1, 1) == "+" then
				local AllowAutoNights = string.sub(self.Config.Settings.AllowAutoNights, 2)
				message = FormatMessage(self:Lang(player, "PlusAutoSkipError"), { allowed = AllowAutoNights + 1 })
				else
				message = FormatMessage(self:Lang(player, "AutoSkipError"), { allowed = self.Config.Settings.AllowAutoNights })
			end
			self:RustMessage(player, message)
			return
		end
		if not ActiveVote then
			self:RustMessage(player, self:Lang(player, "NoVote"))
			return
		end
		if func == "progress" then
			local message, pCount = "", self:getPlayerCount()
			if VoteEnd or RevoteEnd then
				if VoteEnd then
					if isPercent then
						local yes, no = self:GetPercentCount(pCount)
						message = FormatMessage(self:Lang(player, "ManualPercentProgress"), { yes = yes, no = no, players = pCount, required = VoteRequired })
						else
						message = FormatMessage(self:Lang(player, "ManualProgress"), { yes = VoteType["yes"], no = VoteType["no"], required = VoteRequired })
					end
					self:RustMessage(player, message)
					else
					if isPercentRV then
						local yes, no = self:GetPercentCount(pCount)
						message = FormatMessage(self:Lang(player, "ManualPercentProgress"), { yes = yes, no = no, players = pCount, required = string.sub(self.Config.Revote.VoteRequired, 1, -2) })
						else
						message = FormatMessage(self:Lang(player, "ManualProgress"), { yes = VoteType["yes"], no = VoteType["no"], required = self.Config.Revote.VoteRequired })
					end
					self:RustMessage(player, self:Lang(player, "RevotePrefix").." "..message)
				end
			end
			return
		end
		if func == "yes" then
			if VoteList[playerSteamID] == "yes" then
				local message = FormatMessage(self:Lang(player, "AlreadyVoted"), { vote = func })
				self:RustMessage(player, message)
				return
			end
			if not permission.UserHasPermission(playerSteamID, "dayvote.admin") then
				if VoteList[playerSteamID] and self.Config.Settings.AllowVoteChange == "false" then
					local message = FormatMessage(self:Lang(player, "NoVoteChange"), { vote = VoteList[playerSteamID] })
					self:RustMessage(player, message)
					return
				end
				else
				if self.Config.Settings.AdminExempt == "true" then
					self:RustMessage(player, self:Lang(player, "AdminExempt"))
					return
				end
			end
			if VoteList[playerSteamID] == "no" then VoteType["no"] = VoteType["no"] - 1 end
			VoteList[playerSteamID] = "yes"
			VoteType["yes"] = VoteType["yes"] + 1
			local message = FormatMessage(self:Lang(player, "Voted"), { vote = "yes" })
			self:RustMessage(player, message)
			if self.Config.Settings.ShowPlayerVotes == "true" then
				local message = FormatMessage(self:Lang(player, "PlayerVotes"), { player = player.displayName, vote = "yes" })
				self:RustBroadcast(message)
			end
			self:CheckPercentVote()
			return
		end
		if func == "no" then
			if VoteList[playerSteamID] == "no" then
				local message = FormatMessage(self:Lang(player, "AlreadyVoted"), { vote = func })
				self:RustMessage(player, message)
				return
			end
			if not permission.UserHasPermission(playerSteamID, "dayvote.admin") then
				if VoteList[playerSteamID] and self.Config.Settings.AllowVoteChange == "false" then
					local message = FormatMessage(self:Lang(player, "NoVoteChange"), { vote = VoteList[playerSteamID] })
					self:RustMessage(player, message)
					return
				end
				else
				if self.Config.Settings.AdminExempt == "true" then
					self:RustMessage(player, self:Lang(player, "AdminExempt"))
					return
				end
			end
			if VoteList[playerSteamID] == "yes" then VoteType["yes"] = VoteType["yes"] - 1 end
			VoteList[playerSteamID] = "no"
			VoteType["no"] = VoteType["no"] + 1
			local message = FormatMessage(self:Lang(player, "Voted"), { vote = "no" })
			self:RustMessage(player, message)
			if self.Config.Settings.ShowPlayerVotes == "true" then
				local message = FormatMessage(self:Lang(player, "PlayerVotes"), { player = player.displayName, vote = "no" })
				self:RustBroadcast(message)
			end
			return
		end
	end
end

function PLUGIN:newVote(call, creq)
	CanVote = false
	if call == "auto" then
		if string.sub(self.Config.Settings.AllowAutoNights, 1, 1) == "+" then
			local AllowAutoNights = string.sub(self.Config.Settings.AllowAutoNights, 2)
			AutoCurrentNight = AutoCurrentNight + 1
			if AutoCurrentNight >= AllowAutoNights + 1 then
				AutoCurrentNight = 0
				local message = FormatMessage(self:Lang(player, "SkipPlusAutoSkip"), { allowed = AllowAutoNights + 1 })
				self:RustBroadcast(message)
				return
			end
			else
			if self.Config.Settings.AllowAutoNights ~= "1" then
				AutoCurrentNight = AutoCurrentNight + 1
				if AutoCurrentNight > tonumber(self.Config.Settings.AllowAutoNights) then
					AutoCurrentNight = 1
				end
				if AutoCurrentNight > 1 then
					local message = FormatMessage(self:Lang(player, "SkipAutoSkip"), { allowed = self.Config.Settings.AllowAutoNights, current = AutoCurrentNight })
					self:RustBroadcast(message)
					return
				end
			end
		end
		self:RustBroadcast(self:Lang(player, "AutoSkip"))
		self:SetDayTime()
		return
	end
	local pCount = self:getPlayerCount()
	if pCount < tonumber(self.Config.Settings.VoteMinPlayers) then return end
	if call == "cycle" then
		if string.sub(self.Config.Settings.AllowVoteNights, 1, 1) == "+" then
			local AllowVoteNights = string.sub(self.Config.Settings.AllowVoteNights, 2)
			CurrentNight = CurrentNight + 1
			if CurrentNight >= AllowVoteNights + 1 then
				CurrentNight = 0
				local message = FormatMessage(self:Lang(player, "PlusVoteSkip"), { allowed = AllowVoteNights + 1 })
				self:RustBroadcast(message)
				return
			end
			else
			if self.Config.Settings.AllowVoteNights ~= "1" then
				CurrentNight = CurrentNight + 1
				if CurrentNight > tonumber(self.Config.Settings.AllowVoteNights) then
					CurrentNight = 1
				end
				if CurrentNight > 1 then
					local message = FormatMessage(self:Lang(player, "VoteSkip"), { allowed = self.Config.Settings.AllowVoteNights, current = CurrentNight })
					self:RustBroadcast(message)
					return
				end
			end
		end
	end
	VoteList = {}
	VoteType["yes"] = "0"
	VoteType["no"] = "0"
	ActiveVote, AllowRevote = true, false
	if isPercent then
		VoteRequired = tonumber(string.sub(self.Config.Vote.VoteRequired, 1, -2))
		else
		VoteRequired = tonumber(self.Config.Vote.VoteRequired)
	end
	if call == "cycle" then
		AllowRevote = true
		local message
		if isPercent then
			local pCount = self:getPlayerCount()
			message = FormatMessage(self:Lang(player, "PercentVoteOpen"), { required = VoteRequired, players = pCount, seconds = self.Config.Vote.VoteDuration })
			else
			message = FormatMessage(self:Lang(player, "VoteOpen"), { required = VoteRequired, seconds = self.Config.Vote.VoteDuration })
		end
		self:RustBroadcast(message)
	end
	if call == "admin" then
		AutoCurrentNight = 0
		CurrentNight = 0
		local message
		if creq then VoteRequired = tonumber(creq) end
		if isPercent then
			local pCount = self:getPlayerCount()
			message = FormatMessage(self:Lang(player, "ManualPercentVoteOpen"), { required = VoteRequired, players = pCount, seconds = self.Config.Vote.VoteDuration })
			else
			message = FormatMessage(self:Lang(player, "ManualVoteOpen"), { required = VoteRequired, seconds = self.Config.Vote.VoteDuration })
		end
		self:RustBroadcast(message)
	end
	if self.Config.Vote.ShowProgress == "true" then
		local VoteDuration = self.Config.Vote.VoteDuration
		VoteProgress = timer.Repeat(tonumber(self.Config.Vote.ProgressInterval), 0, function()
			VoteDuration = VoteDuration - self.Config.Vote.ProgressInterval
			local message = {}
			if isPercent then
				local pCount = self:getPlayerCount()
				local yes, no = self:GetPercentCount(pCount)
				message = FormatMessage(self:Lang(player, "PercentProgress"), { yes = yes, no = no, players = pCount, required = VoteRequired, seconds = VoteDuration })
				else
				message = FormatMessage(self:Lang(player, "Progress"), { yes = VoteType["yes"], no = VoteType["no"], required = VoteRequired, seconds = VoteDuration })
			end
			if VoteDuration > 0 then self:RustBroadcast(message) end
		end, self.Plugin)
	end
	VoteEnd = timer.Once(tonumber(self.Config.Vote.VoteDuration), function()
		ActiveVote = false
		VoteEnd = nil
		if VoteProgress then
			VoteProgress:Destroy()
			VoteProgress = nil
		end
		if isPercent then
			local pCount = self:getPlayerCount()
			local Percent = VoteRequired / 100 * pCount
			if self.Config.Settings.RoundPercent == "true" then
				if string.find(Percent, "%.") then Percent = tostring(Percent):match"([^.]*).(.*)" end
			end
			if tonumber(VoteType["yes"]) ~= 0 and tonumber(VoteType["yes"]) >= tonumber(Percent) then
				local yes, no = self:GetPercentCount(pCount)
				local message = FormatMessage(self:Lang(player, "PercentVotePassed"), { required = VoteRequired, no = no, players = pCount })
				self:RustBroadcast(message)
				self:SetDayTime()
				return
			end
			if VoteType["yes"] == "0" and VoteType["no"] == "0" then
				self:RustBroadcast(self:Lang(player, "VoteFailedNoVote"))
				else
				local yes, no = self:GetPercentCount(pCount)
				local message = FormatMessage(self:Lang(player, "PercentVoteFailed"), { yes = yes, no = no, players = pCount, required = VoteRequired })
				self:RustBroadcast(message)
			end
			if self.Config.Revote.Enabled == "true" and AllowRevote then
				if self.Config.Revote.Announce == "true" then
					local message = FormatMessage(self:Lang(player, "RevoteAlert"), { seconds = self.Config.Revote.WaitDuration })
					self:RustBroadcast(self:Lang(player, "RevotePrefix").." "..message)
				end
				RevoteWait = timer.Once(tonumber(self.Config.Revote.WaitDuration), function() self:reVote() end, self.Plugin)
			end
			return
			else
			if tonumber(VoteType["yes"]) > tonumber(VoteType["no"]) then
				local CheckVote = VoteType["yes"] - VoteType["no"]
				if CheckVote >= VoteRequired then
					local message = FormatMessage(self:Lang(player, "VotePassed"), { yes = VoteType["yes"], no = VoteType["no"] })
					self:RustBroadcast(message)
					self:SetDayTime()
					return
				end
			end
			if VoteType["yes"] == "0" and VoteType["no"] == "0" then
				self:RustBroadcast(self:Lang(player, "VoteFailedNoVote"))
				else
				local message = FormatMessage(self:Lang(player, "VoteFailed"), { yes = VoteType["yes"], no = VoteType["no"], required = VoteRequired })
				self:RustBroadcast(message)
			end
			if self.Config.Revote.Enabled == "true" and AllowRevote then
				if self.Config.Revote.Announce == "true" then
					local message = FormatMessage(self:Lang(player, "RevoteAlert"), { seconds = self.Config.Revote.WaitDuration })
					self:RustBroadcast(self:Lang(player, "RevotePrefix").." "..message)
				end
				RevoteWait = timer.Once(tonumber(self.Config.Revote.WaitDuration), function() self:reVote() end, self.Plugin)
			end
			return
		end
	end, self.Plugin)
end

function PLUGIN:reVote()
	RevoteWait = nil
	VoteList = {}
	VoteType["yes"] = "0"
	VoteType["no"] = "0"
	ActiveVote = true
	local message = {}
	if isPercentRV then
		local pCount = self:getPlayerCount()
		message = FormatMessage(self:Lang(player, "PercentVoteOpen"), { required = string.sub(self.Config.Revote.VoteRequired, 1, -2), players = pCount, seconds = self.Config.Revote.VoteDuration })
		else
		message = FormatMessage(self:Lang(player, "VoteOpen"), { required = self.Config.Revote.VoteRequired, seconds = self.Config.Revote.VoteDuration })
	end
	self:RustBroadcast(self:Lang(player, "RevotePrefix").." "..message)
	if self.Config.Revote.ShowProgress == "true" then
		local VoteDuration = self.Config.Revote.VoteDuration
		RevoteProgress = timer.Repeat(tonumber(self.Config.Revote.ProgressInterval), 0, function()
			VoteDuration = VoteDuration - self.Config.Revote.ProgressInterval
			local message = {}
			if isPercentRV then
				local pCount = self:getPlayerCount()
				local yes, no = self:GetPercentCount(pCount)
				message = FormatMessage(self:Lang(player, "PercentProgress"), { yes = yes, no = no, players = pCount, required = string.sub(self.Config.Revote.VoteRequired, 1, -2), seconds = VoteDuration })
				else
				message = FormatMessage(self:Lang(player, "Progress"), { yes = VoteType["yes"], no = VoteType["no"], required = self.Config.Revote.VoteRequired, seconds = VoteDuration })
			end
			if VoteDuration > 0 then self:RustBroadcast(self:Lang(player, "RevotePrefix").." "..message) end
		end, self.Plugin)
	end
	RevoteEnd = timer.Once(tonumber(self.Config.Revote.VoteDuration), function()
		ActiveVote = false
		RevoteEnd = nil
		if RevoteProgress then
			RevoteProgress:Destroy()
			RevoteProgress = nil
		end
		if isPercentRV then
			local pCount = self:getPlayerCount()
			local Percent = tonumber(string.sub(self.Config.Revote.VoteRequired, 1, -2)) / 100 * pCount
			if self.Config.Settings.RoundPercent == "true" then
				if string.find(Percent, "%.") then Percent = tostring(Percent):match"([^.]*).(.*)" end
			end
			if tonumber(VoteType["yes"]) ~= 0 and tonumber(VoteType["yes"]) >= tonumber(Percent) then
				local yes, no = self:GetPercentCount(pCount)
				local message = FormatMessage(self:Lang(player, "PercentVotePassed"), { required = string.sub(self.Config.Revote.VoteRequired, 1, -2), no = no, players = pCount })
				self:RustBroadcast(self:Lang(player, "RevotePrefix").." "..message)
				self:SetDayTime()
				return
			end
			if VoteType["yes"] == "0" and VoteType["no"] == "0" then
				self:RustBroadcast(self:Lang(player, "RevotePrefix").." "..self:Lang(player, "VoteFailedNoVote"))
				else
				local yes, no = self:GetPercentCount(pCount)
				local message = FormatMessage(self:Lang(player, "PercentVoteFailed"), { yes = yes, no = no, players = pCount, required = string.sub(self.Config.Revote.VoteRequired, 1, -2) })
				self:RustBroadcast(self:Lang(player, "RevotePrefix").." "..message)
			end
			return
			else
			if tonumber(VoteType["yes"]) > tonumber(VoteType["no"]) then
				local CheckVote = VoteType["yes"] - VoteType["no"]
				if CheckVote >= tonumber(self.Config.Revote.VoteRequired) then
					local message = FormatMessage(self:Lang(player, "VotePassed"), { yes = VoteType["yes"], no = VoteType["no"] })
					self:RustBroadcast(self:Lang(player, "RevotePrefix").." "..message)
					self:SetDayTime()
					return
				end
			end
			if VoteType["yes"] == "0" and VoteType["no"] == "0" then
				self:RustBroadcast(self:Lang(player, "RevotePrefix")" "..self:Lang(player, "VoteFailedNoVote"))
				else
				local message = FormatMessage(self:Lang(player, "VoteFailed"), { yes = VoteType["yes"], no = VoteType["no"], required = self.Config.Revote.VoteRequired })
				self:RustBroadcast(self:Lang(player, "RevotePrefix").." "..message)
			end
			return
		end
	end, self.Plugin)
end

function PLUGIN:CheckPercentVote()
	if VoteEnd or RevoteEnd then
		if isPercent or isPercentRV then
			if VoteEnd and not isPercent then return end
			if RevoteEnd and not isPercentRV then return end
			local pCount = self:getPlayerCount()
			local Percent = {}
			if VoteEnd then
				Percent = VoteRequired / 100 * pCount
				else
				Percent = tonumber(string.sub(self.Config.Revote.VoteRequired, 1, -2)) / 100 * pCount
			end
			if self.Config.Settings.RoundPercent == "true" then
				if string.find(Percent, "%.") then Percent = tostring(Percent):match"([^.]*).(.*)" end
			end
			if tonumber(VoteType["yes"]) ~= 0 and tonumber(VoteType["yes"]) >= tonumber(Percent) then
				ActiveVote = false
				local yes, no = self:GetPercentCount(pCount)
				if VoteEnd then
					local message = FormatMessage(self:Lang(player, "PercentVotePassed"), { required = VoteRequired, no = no, players = pCount })
					self:RustBroadcast(message)
					else
					local message = FormatMessage(self:Lang(player, "PercentVotePassed"), { required = string.sub(self.Config.Revote.VoteRequired, 1, -2), no = no, players = pCount })
					self:RustBroadcast(self:Lang(player, "RevotePrefix").." "..message)
				end
				self:DestroyTimers()
				self:SetDayTime()
				return
			end
		end
	end
end

function PLUGIN:GetPercentCount(pCount)
	local yes, no = "0", "0"
	if VoteType["yes"] ~= "0" then
		yes = tonumber(VoteType["yes"]) / pCount * 100
		if string.find(yes, "%.") then yes = tostring(yes):match"([^.]*).(.*)" end
	end
	if VoteType["no"] ~= "0" then
		no = tonumber(VoteType["no"]) / pCount * 100
		if string.find(no, "%.") then no = tostring(no):match"([^.]*).(.*)" end
	end
	return yes, no
end

function PLUGIN:DestroyTimers()
	if VoteProgress then VoteProgress:Destroy() end
	if VoteEnd then VoteEnd:Destroy() end
	if RevoteWait then RevoteWait:Destroy() end
	if RevoteProgress then RevoteProgress:Destroy() end
	if RevoteEnd then RevoteEnd:Destroy() end
	VoteProgress = nil
	VoteEnd = nil
	RevoteWait = nil
	RevoteProgress = nil
	RevoteEnd = nil
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