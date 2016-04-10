PLUGIN.Title        = "Day Vote"
PLUGIN.Description  = "Allows automatic or player voting to skip night cycles."
PLUGIN.Author       = "InSaNe8472"
PLUGIN.Version      = V(1,1,1)
PLUGIN.ResourceID   = 1275

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

function PLUGIN:Init()
	permission.RegisterPermission("dayvote.use", self.Plugin)
	permission.RegisterPermission("dayvote.admin", self.Plugin)
	command.AddChatCommand("dayvote", self.Plugin, "cmdDayVote")
	self:LoadDefaultConfig()
end

function PLUGIN:LoadDefaultConfig()
	self.Config.Airdrop = self.Config.Airdrop or {}
	self.Config.Settings = self.Config.Settings or {}
	self.Config.Revote = self.Config.Revote or {}
	self.Config.Messages = self.Config.Messages or {}
	self.Config.Airdrop.Enabled = self.Config.Airdrop.Enabled or "false"
	self.Config.Airdrop.AirdropWait = self.Config.Airdrop.AirdropWait or "false"
	self.Config.Airdrop.MinPlayers = self.Config.Airdrop.MinPlayers or "1"
	self.Config.Airdrop.Interval = self.Config.Airdrop.Interval or {
		"2400",
		"3000",
		"3600"
	}
	self.Config.Airdrop.Drops = self.Config.Airdrop.Drops or "1"
	self.Config.Settings.Prefix = self.Config.Settings.Prefix or "[<color=#cd422b> Day Vote </color>]"
	self.Config.Settings.Enabled = self.Config.Settings.Enabled or "true"
	self.Config.Settings.VoteMinPlayers = self.Config.Settings.VoteMinPlayers or "1"
	self.Config.Settings.AutoSkip = self.Config.Settings.AutoSkip or "false"
	self.Config.Settings.ShowTimeChanges = self.Config.Settings.ShowTimeChanges or "true"
	self.Config.Settings.UsePermissions = self.Config.Settings.UsePermissions or "true"
	self.Config.Settings.AllowAutoNights = self.Config.Settings.AllowAutoNights or "1"
	self.Config.Settings.AllowVoteNights = self.Config.Settings.AllowVoteNights or "1"
	self.Config.Settings.AllowVoteChange = self.Config.Settings.AllowVoteChange or "false"
	self.Config.Settings.ShowProgress = self.Config.Settings.ShowProgress or "true"
	self.Config.Settings.ProgressInterval = self.Config.Settings.ProgressInterval or "25"
	self.Config.Settings.DayHour = self.Config.Settings.DayHour or "6"
	self.Config.Settings.NightHour = self.Config.Settings.NightHour or "18"
	self.Config.Settings.VoteDuration = self.Config.Settings.VoteDuration or "125"
	self.Config.Settings.VoteRequired = self.Config.Settings.VoteRequired or "50%"
	self.Config.Settings.AdminExempt = self.Config.Settings.AdminExempt or "false"
	self.Config.Settings.RoundPercent = self.Config.Settings.RoundPercent or "true"
	self.Config.Settings.ShowPlayerVotes = self.Config.Settings.ShowPlayerVotes or "false"
	self.Config.Settings.ShowDisconnectVotes = self.Config.Settings.ShowDisconnectVotes or "true"
	self.Config.Revote.Prefix = self.Config.Revote.Prefix or "[<color=#f9169f> Revote </color>]"
	self.Config.Revote.Enabled = self.Config.Revote.Enabled or "false"
	self.Config.Revote.Announce = self.Config.Revote.Announce or "true"
	self.Config.Revote.WaitDuration = self.Config.Revote.WaitDuration or "30"
	self.Config.Revote.ShowProgress = self.Config.Revote.ShowProgress or "true"
	self.Config.Revote.ProgressInterval = self.Config.Revote.ProgressInterval or "15"
	self.Config.Revote.VoteDuration = self.Config.Revote.VoteDuration or "60"
	self.Config.Revote.VoteRequired = self.Config.Revote.VoteRequired or "40%"
	self.Config.Messages.NoPermission = self.Config.Messages.NoPermission or "You do not have permission to use this command."
	self.Config.Messages.AutoSkip = self.Config.Messages.AutoSkip or "Night cycle started and has automatically been skipped."
	self.Config.Messages.SkipPlusAutoSkip = self.Config.Messages.SkipPlusAutoSkip or "Night cycle has started.  Night cycle is forced every <color=#cd422b>{allowed}</color> nights.  Night cycle will continue."
	self.Config.Messages.SkipAutoSkip = self.Config.Messages.SkipAutoSkip or "Night cycle has started.  Automatic skipping allowed every <color=#cd422b>{allowed}</color> nights.  Current night cycle is <color=#cd422b>{current}</color>.  Night cycle will continue."
	self.Config.Messages.PlusAutoSkipError = self.Config.Messages.PlusAutoSkipError or "Day vote is <color=#cd422b>disabled</color>.  Night cycles are automatically skipped.  Night cycles are forced every <color=#cd422b>{allowed}</color> nights."
	self.Config.Messages.AutoSkipError = self.Config.Messages.AutoSkipError or "Day vote is <color=#cd422b>disabled</color>.  Night cycles are automatically skipped every <color=#cd422b>{allowed}</color> nights."
	self.Config.Messages.ManualAutoSkipError = self.Config.Messages.ManualAutoSkipError or "You cannot manually start or stop a day vote when night cycles are automatically skipped."
	self.Config.Messages.ChangedStatus = self.Config.Messages.ChangedStatus or "Day vote <color=#cd422b>{status}</color>."
	self.Config.Messages.AdminTimeSet = self.Config.Messages.AdminTimeSet or "Administrator set current time to <color=#cd422b>{set}</color> from <color=#cd422b>{current}</color>."
	self.Config.Messages.NotEnabled = self.Config.Messages.NotEnabled or "Day vote is <color=#cd422b>disabled</color>."
	self.Config.Messages.VoteMinPlayers = self.Config.Messages.VoteMinPlayers or "You cannot manually start a day vote, minimum online players of <color=#cd422b>{minimum}</color> was not reached.  Current online players is <color=#cd422b>{current}</color>."
	self.Config.Messages.WrongArgs = self.Config.Messages.WrongArgs or "Syntax error.  Use <color=#cd422b>/dayvote</color> for help."
	self.Config.Messages.NoVote = self.Config.Messages.NoVote or "No day vote currently in progress.  Wait until the night cycle has started."
	self.Config.Messages.Voted = self.Config.Messages.Voted or "You have successfully voted <color=#cd422b>{vote}</color> for this day vote."
	self.Config.Messages.AlreadyVoted = self.Config.Messages.AlreadyVoted or "You have already voted <color=#cd422b>{vote}</color> for this day vote."
	self.Config.Messages.NoVoteChange = self.Config.Messages.NoVoteChange or "You have already voted <color=#cd422b>{vote}</color> for this day vote.  Permission to change your vote is disabled."
	self.Config.Messages.VoteAlreadyOpen = self.Config.Messages.VoteAlreadyOpen or "A day vote is currently in progress."
	self.Config.Messages.RevoteWait = self.Config.Messages.RevoteWait or "A pending <color=#f9169f>revote</color> is already scheduled to start in less than <color=#cd422b>{seconds} seconds</color>."
	self.Config.Messages.VoteNotNight = self.Config.Messages.VoteNotNight or "You cannot manually start a day vote, current time of <color=#cd422b>{current}</color> cannot be between configured day and night hours of <color=#cd422b>{day}</color> and <color=#cd422b>{night}</color>."
	self.Config.Messages.VoteNotOpen = self.Config.Messages.VoteNotOpen or "A day vote is not currently in progress."
	self.Config.Messages.PrecentConnected = self.Config.Messages.PrecentConnected or "A day vote is currently in progress.  To skip, use <color=#cd422b>/dayvote yes</color>, otherwise, <color=#cd422b>/dayvote no</color>.  Yes votes required is <color=#cd422b>{required}%</color> of <color=#ffd479>{players}</color> online players.  Current progress: <color=#cd422b>{yes}%</color> voted yes, <color=#cd422b>{no}%</color> voted no."
	self.Config.Messages.Connected = self.Config.Messages.Connected or "A day vote is currently in progress.  To skip, use <color=#cd422b>/dayvote yes</color>, otherwise, <color=#cd422b>/dayvote no</color>.  Yes/no votes required is <color=#cd422b>{required}</color>.    Current progress: <color=#cd422b>{yes}</color> voted yes, <color=#cd422b>{no}</color> voted no."
	self.Config.Messages.PercentVoteOpen = self.Config.Messages.PercentVoteOpen or "Night cycle has started.  To skip, use <color=#cd422b>/dayvote yes</color>, otherwise, <color=#cd422b>/dayvote no</color>.  Yes votes required is <color=#cd422b>{required}%</color> of <color=#ffd479>{players}</color> online players.  Voting ends in <color=#cd422b>{seconds} seconds</color>."
	self.Config.Messages.VoteOpen = self.Config.Messages.VoteOpen or "Night cycle has started.  To skip, use <color=#cd422b>/dayvote yes</color>, otherwise, <color=#cd422b>/dayvote no</color>.  Yes/no votes required is <color=#cd422b>{required}</color>.  Voting ends in <color=#cd422b>{seconds} seconds</color>."
	self.Config.Messages.PlusVoteSkip = self.Config.Messages.PlusVoteSkip or "Night cycle has started.  Night cycle is forced every <color=#cd422b>{allowed}</color> nights.  Night cycle will continue."
	self.Config.Messages.VoteSkip = self.Config.Messages.VoteSkip or "Night cycle has started.  Day votes allowed every <color=#cd422b>{allowed}</color> nights.  Current night cycle is <color=#cd422b>{current}</color>.  Night cycle will continue."
	self.Config.Messages.ManualVoteOpen = self.Config.Messages.ManualVoteOpen or "Administrator started a skip night cycle vote.  To skip, type <color=#cd422b>/dayvote yes</color>, otherwise, <color=#cd422b>/dayvote no</color>.  Yes/no votes required is <color=#cd422b>{required}</color>.  Voting ends in <color=#cd422b>{seconds} seconds</color>."
	self.Config.Messages.ManualPercentVoteOpen = self.Config.Messages.ManualPercentVoteOpen or "Administrator started a skip night cycle vote.  To skip, type <color=#cd422b>/dayvote yes</color>, otherwise, <color=#cd422b>/dayvote no</color>.  Yes votes required is <color=#cd422b>{required}%</color> of <color=#ffd479>{players}</color> online players.  Voting ends in <color=#cd422b>{seconds} seconds</color>."
	self.Config.Messages.ManualVoteClose = self.Config.Messages.ManualVoteClose or "Administrator aborted current day vote.  <color=#cd422b>Night cycle will continue.</color>"
	self.Config.Messages.ManualRevoteClose = self.Config.Messages.ManualRevoteClose or "A day vote is not currently open.  However, a pending <color=#f9169f>revote</color> has been aborted."
	self.Config.Messages.PercentProgress = self.Config.Messages.PercentProgress or "Current progress: <color=#cd422b>{yes}%</color> (<color=#cd422b>{required}%</color> required) of <color=#ffd479>{players}</color> online players voted yes, <color=#cd422b>{no}%</color> voted no.  Use <color=#cd422b>/dayvote yes</color>, otherwise, <color=#cd422b>/dayvote no</color>.  Voting ends in <color=#cd422b>{seconds} seconds</color>."
	self.Config.Messages.ManualPercentProgress = self.Config.Messages.ManualPercentProgress or "Current progress: <color=#cd422b>{yes}%</color> (<color=#cd422b>{required}%</color> required) of <color=#ffd479>{players}</color> online players voted yes, <color=#cd422b>{no}%</color> voted no.  Use <color=#cd422b>/dayvote yes</color>, otherwise, <color=#cd422b>/dayvote no</color>."
	self.Config.Messages.Progress = self.Config.Messages.Progress or "Current progress: <color=#ffd479>{yes} yes</color>, <color=#cd422b>{no} no</color>.  Yes/no votes required is <color=#cd422b>{required}</color>.  Use <color=#cd422b>/dayvote yes</color>, otherwise, <color=#cd422b>/dayvote no</color>.  Voting ends in <color=#cd422b>{seconds} seconds</color>."
	self.Config.Messages.ManualProgress = self.Config.Messages.ManualProgress or "Current progress: <color=#ffd479>{yes} yes</color>, <color=#cd422b>{no} no</color>.  Yes/no votes required is <color=#cd422b>{required}</color>.  Use <color=#cd422b>/dayvote yes</color>, otherwise, <color=#cd422b>/dayvote no</color>."
	self.Config.Messages.PercentVotePassed = self.Config.Messages.PercentVotePassed or "<color=#cd422b>Voting is closed.</color>  Day vote passed, <color=#cd422b>{required}%</color> yes vote requirement of <color=#ffd479>{players}</color> online players reached, <color=#cd422b>{no}%</color> voted no.  Night cycle will end."
	self.Config.Messages.PercentVoteFailed = self.Config.Messages.PercentVoteFailed or "<color=#cd422b>Voting is closed.</color>  Day vote failed, <color=#cd422b>{yes}%</color> of <color=#ffd479>{players}</color> online players voted yes, <color=#cd422b>{no}%</color> voted no.  Yes votes required is <color=#cd422b>{required}%</color>.  Night cycle will continue."
	self.Config.Messages.VotePassed = self.Config.Messages.VotePassed or "<color=#cd422b>Voting is closed.</color>  Day vote passed, <color=#ffd479>{yes} yes</color> to <color=#cd422b>{no} no</color> votes.  Night cycle will end."
	self.Config.Messages.VoteFailed = self.Config.Messages.VoteFailed or "<color=#cd422b>Voting is closed.</color>  Day vote failed, <color=#cd422b>{no} no</color> to <color=#ffd479>{yes} yes</color> votes.  Yes/no votes required is <color=#cd422b>{required}</color>.  Night cycle will continue."
	self.Config.Messages.VoteFailedNoVote = self.Config.Messages.VoteFailedNoVote or "<color=#cd422b>Voting is closed.</color>  Day vote failed, no votes were cast.  Night cycle will continue."
	self.Config.Messages.NotNumber = self.Config.Messages.NotNumber or "Time must be a number between <color=#cd422b>0</color> and <color=#cd422b>23</color>."
	self.Config.Messages.TimeSet = self.Config.Messages.TimeSet or "Current time set to <color=#cd422b>{set}</color> from <color=#cd422b>{current}</color>."
	self.Config.Messages.SetToNightHour = self.Config.Messages.SetToNightHour or "Current time set to configured night hour.  No day vote will be started or night cycle automatically skipped."
	self.Config.Messages.SetVoteClose = self.Config.Messages.SetVoteClose or "Administrator aborted current day vote by manually setting new time.  <color=#cd422b>New time will now take effect.</color>"
	self.Config.Messages.SetRevoteClose = self.Config.Messages.SetRevoteClose or "A pending <color=#f9169f>revote</color> has been aborted."
	self.Config.Messages.Revote = self.Config.Messages.Revote or "A one time only day revote will begin in <color=#cd422b>{seconds} seconds</color>."
	self.Config.Messages.AdminExempt = self.Config.Messages.AdminExempt or "Day vote administrators a currently exempt from voting."
	self.Config.Messages.PlayerVotes = self.Config.Messages.PlayerVotes or "<color=#cd422b>{player}</color> has voted <color=#ffd479>{vote}</color>."
	self.Config.Messages.DisconnectVotes = self.Config.Messages.DisconnectVotes or "<color=#cd422b>{player}'s</color> vote of <color=#cd422b>{vote}</color> has been removed for disconnecting."
	if not tonumber(self.Config.Airdrop.MinPlayers) or tonumber(self.Config.Airdrop.MinPlayers) < 1 then self.Config.Airdrop.MinPlayers = "1" end
	if self.Config.Airdrop.Interval == nil then self.Config.Airdrop.Interval = "3600" end
	if not tonumber(self.Config.Airdrop.Drops) or tonumber(self.Config.Airdrop.Drops) < 1 then self.Config.Airdrop.Drops = "1" end
	if not tonumber(self.Config.Settings.VoteMinPlayers) or tonumber(self.Config.Settings.VoteMinPlayers) < 1 then self.Config.Settings.VoteMinPlayers = "1" end
	if not tonumber(self.Config.Settings.ProgressInterval) or tonumber(self.Config.Settings.ProgressInterval) < 1 then self.Config.Settings.ProgressInterval = "15" end
	if not tonumber(self.Config.Settings.VoteDuration) or tonumber(self.Config.Settings.VoteDuration) < 1 then self.Config.Settings.VoteDuration = "60" end
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
	if string.sub(self.Config.Settings.VoteRequired, -1) == "%" then
		local CheckVoteRequired = tonumber(string.sub(self.Config.Settings.VoteRequired, 1, -2))
		if not tonumber(CheckVoteRequired) or CheckVoteRequired < 10 or CheckVoteRequired > 100 then self.Config.Settings.VoteRequired = "50%" end
		isPercent = true
		else
		if not tonumber(self.Config.Settings.VoteRequired) or tonumber(self.Config.Settings.VoteRequired) < 1 then self.Config.Settings.VoteRequired = "3" end
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
	AirdropControl = plugins.Find("AirdropControl")
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
			rust.RunServerCommand("airdrop.massdrop "..tonumber(self.Config.Airdrop.Drops))
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
	AirdropControl = plugins.Find("AirdropControl")
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
	if self.Config.Settings.Enabled == "true" then
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

function PLUGIN:OnPlayerInit(player)
	local playerSteamID = rust.UserIDFromPlayer(player)
	if ActiveVote then
		local message = {}
		if VoteEnd then
			if isPercent then
				local pCount = self:getPlayerCount()
				local yes, no = self:GetPercentCount(pCount)
				message = FormatMessage(self.Config.Messages.PrecentConnected, { required = string.sub(self.Config.Settings.VoteRequired, 1, -2), players = pCount, yes = yes, no = no })
				else
				message = FormatMessage(self.Config.Messages.Connected, { required = self.Config.Settings.VoteRequired, yes = VoteType["yes"], no = VoteType["no"] })
			end
			rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..message)
		end
		if RevoteEnd then
			if isPercentRV then
				local pCount = self:getPlayerCount()
				local yes, no = self:GetPercentCount(pCount)
				message = FormatMessage(self.Config.Messages.PrecentConnected, { required = string.sub(self.Config.Revote.VoteRequired, 1, -2), players = pCount, yes = yes, no = no })
				else
				message = FormatMessage(self.Config.Messages.Connected, { required = self.Config.Revote.VoteRequired, yes = VoteType["yes"], no = VoteType["no"] })
			end
			rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..self.Config.Revote.Prefix.." "..message)
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
				local message = FormatMessage(self.Config.Messages.DisconnectVotes, { player = player.displayName, vote = VoteList[playerSteamID] })
				rust.BroadcastChat(self.Config.Settings.Prefix.." "..message)
			end
			VoteList[playerSteamID] = nil
		end
		self:CheckPercentVote()
	end
end

function PLUGIN:cmdDayVote(player, cmd, args)
	local playerSteamID = rust.UserIDFromPlayer(player)
	if args.Length > 0 and args[0] == "toggle" then
		if not permission.UserHasPermission(playerSteamID, "dayvote.admin") then
			rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.NoPermission)
			return
		end
		local message = ""
		if self.Config.Settings.Enabled == "true" then
			if ActiveVote then
				ActiveVote = false
				self:DestroyTimers()
				rust.BroadcastChat(self.Config.Settings.Prefix.." "..self.Config.Messages.ManualVoteClose)
			end
			if RevoteWait then
				self:DestroyTimers()
				rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.ManualRevoteClose)
			end
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
	if args.Length > 0 and args[0] == "start" then
		if not permission.UserHasPermission(playerSteamID, "dayvote.admin") then
			rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.NoPermission)
			return
		end
		if self.Config.Settings.Enabled == "false" then
			rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.NotEnabled)
			return
		end
		if self.Config.Settings.AutoSkip == "true" then
			rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.ManualAutoSkipError)
			return
		end
		local pCount = self:getPlayerCount()
		if pCount < tonumber(self.Config.Settings.VoteMinPlayers) then
			local message = FormatMessage(self.Config.Messages.VoteMinPlayers, { minimum = self.Config.Settings.VoteMinPlayers, current = pCount })
			rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..message)
			return
		end
		if ActiveVote then
			rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.VoteAlreadyOpen)
			return
		end
		if RevoteWait then
			local message = FormatMessage(self.Config.Messages.RevoteWait, { seconds = self.Config.Revote.WaitDuration })
			rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..message)
			return
		end
		local CurTime = tostring(VSky.Cycle.Hour):match"([^.]*).(.*)"
		if tonumber(CurTime) >= tonumber(self.Config.Settings.DayHour) and tonumber(CurTime) < tonumber(self.Config.Settings.NightHour) then
			local message = FormatMessage(self.Config.Messages.VoteNotNight, { current = CurTime, day = self.Config.Settings.DayHour, night = self.Config.Settings.NightHour })
			rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..message)
			return
		end
		if args.Length < 2 then
			self:newVote("admin", false)
			else
			local creq = tonumber(args[1])
			if isPercent then
				if not tonumber(creq) or creq < 10 or creq > 100 then
					rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.WrongArgs)
					return
				end
				else
				if not tonumber(creq) or creq < 1 then
					rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.WrongArgs)
					return
				end
			end
			self:newVote("admin", creq)
		end
		return
	end
	if args.Length > 0 and args[0] == "stop" then
		if not permission.UserHasPermission(playerSteamID, "dayvote.admin") then
			rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.NoPermission)
			return
		end
		if self.Config.Settings.Enabled == "false" then
			rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.NotEnabled)
			return
		end
		if self.Config.Settings.AutoSkip == "true" then
			rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.ManualAutoSkipError)
			return
		end
		if RevoteWait then
			self:DestroyTimers()
			rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.ManualRevoteClose)
			return
		end
		if not ActiveVote then
			rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.VoteNotOpen)
			return
		end
		ActiveVote = false
		self:DestroyTimers()
		rust.BroadcastChat(self.Config.Settings.Prefix.." "..self.Config.Messages.ManualVoteClose)
		return
	end
	if args.Length > 0 and args[0] == "set" then
		if not permission.UserHasPermission(playerSteamID, "dayvote.admin") then
			rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.NoPermission)
			return
		end
		if args.Length < 2 then
			rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.WrongArgs)
			return
		end
		if not tonumber(args[1]) or tonumber(args[1]) < 0 or tonumber(args[1]) > 23 then
			rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.NotNumber)
			return
		end
		if self.Config.Settings.ShowTimeChanges == "true" then
			local message = FormatMessage(self.Config.Messages.AdminTimeSet, { set = tonumber(args[1]), current = tostring(VSky.Cycle.Hour):match"([^.]*).(.*)" })
			rust.BroadcastChat(self.Config.Settings.Prefix.." "..message)
			else
			local message = FormatMessage(self.Config.Messages.TimeSet, { set = tonumber(args[1]), current = tostring(VSky.Cycle.Hour):match"([^.]*).(.*)" })
			rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..message)
		end
		if tonumber(args[1]) == tonumber(self.Config.Settings.NightHour) then
			CanVote = false
			rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.SetToNightHour)
			else
			CanVote = true
		end
		if ActiveVote then
			ActiveVote = false
			self:DestroyTimers()
			rust.BroadcastChat(self.Config.Settings.Prefix.." "..self.Config.Messages.SetVoteClose)
			else
			if RevoteWait then
				self:DestroyTimers()
				rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.SetRevoteClose)
			end
		end
		if not AllowAirdrop then AllowAirdrop = true end
		VSky.Cycle.Hour = tonumber(args[1])
		return
	end
	if self.Config.Settings.UsePermissions == "true" then
		if not permission.UserHasPermission(playerSteamID, "dayvote.admin") then
			if not permission.UserHasPermission(playerSteamID, "dayvote.use") then
				rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.NoPermission)
				return
			end
		end
	end
	if self.Config.Settings.Enabled == "false" then
		rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.NotEnabled)
		return
	end
	if args.Length == 0 then
		local Limit = "1+"
		if isPercent then Limit = "10-100" end
		if permission.UserHasPermission(playerSteamID, "dayvote.admin") then
			rust.SendChatMessage(player,
				self.Config.Settings.Prefix.." <color=#ffd479>/dayvote toggle</color> - Enable or disable day vote system\n"..
				self.Config.Settings.Prefix.." <color=#ffd479>/dayvote start ["..Limit.."]</color> - Manually start a day vote with optional requirement\n"..
				self.Config.Settings.Prefix.." <color=#ffd479>/dayvote stop</color> - Manually stop a day vote\n"..
				self.Config.Settings.Prefix.." <color=#ffd479>/dayvote set</color> - Manually set current time"
			)
		end
		rust.SendChatMessage(player,
			self.Config.Settings.Prefix.." <color=#ffd479>/dayvote limits [revote]</color> - View main or revote configuration limits\n"..
			self.Config.Settings.Prefix.." <color=#ffd479>/dayvote progress</color> - View current day vote progress\n"..
			self.Config.Settings.Prefix.." <color=#ffd479>/dayvote yes</color> - Vote yes to skip current night cycle\n"..
			self.Config.Settings.Prefix.." <color=#ffd479>/dayvote no</color> - Vote no to skip current night cycle\n\n"..
			self.Config.Settings.Prefix.." Active day vote: <color=#ffd479>"..tostring(ActiveVote).."</color>"
		)
		return
		elseif args.Length > 0 then
		local func = args[0]
		if func ~= "limits" and func ~= "progress" and func ~= "yes" and func ~= "no" then
			rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.WrongArgs)
			return
		end
		if func == "limits" then
			if args.Length >= 2 then
				if args[1] == "revote" then
					rust.SendChatMessage(player,
						self.Config.Settings.Prefix.." Revote enabled: <color=#ffd479>"..self.Config.Revote.Enabled.."</color>\n"..
						self.Config.Settings.Prefix.." Revote wait duration: <color=#ffd479>"..self.Config.Revote.WaitDuration.." seconds</color>\n"..
						self.Config.Settings.Prefix.." Revote vote duration: <color=#ffd479>"..self.Config.Revote.VoteDuration.." seconds</color>\n"..
						self.Config.Settings.Prefix.." Revote yes/no votes or percentage required: <color=#ffd479>"..self.Config.Revote.VoteRequired.."</color>\n\n"..
						self.Config.Settings.Prefix.." Active day vote: <color=#ffd479>"..tostring(ActiveVote).."</color>\n"..
						self.Config.Settings.Prefix.." Current hour: <color=#ffd479>"..tostring(VSky.Cycle.Hour):match"([^.]*).(.*)".."</color>"
					)
					return
				end
			end
			rust.SendChatMessage(player,
				self.Config.Settings.Prefix.." Administrator exempt: <color=#ffd479>"..self.Config.Settings.AdminExempt.."</color>\n"..
				self.Config.Settings.Prefix.." Automatically skip night cycles: <color=#ffd479>"..self.Config.Settings.AutoSkip.."</color>\n"..
				self.Config.Settings.Prefix.." Allowed automatic skipping nights: <color=#ffd479>"..self.Config.Settings.AllowAutoNights.."</color>\n"..
				self.Config.Settings.Prefix.." Minimum online players required: <color=#ffd479>"..self.Config.Settings.VoteMinPlayers.."</color>\n"..
				self.Config.Settings.Prefix.." Allowed voting nights: <color=#ffd479>"..self.Config.Settings.AllowVoteNights.."</color>"
			)
			rust.SendChatMessage(player,
				self.Config.Settings.Prefix.." Round percentage counts: <color=#ffd479>"..self.Config.Settings.RoundPercent.."</color>\n"..
				self.Config.Settings.Prefix.." Yes/no votes or percentage required: <color=#ffd479>"..self.Config.Settings.VoteRequired.."</color>\n"..
				self.Config.Settings.Prefix.." Day vote duration: <color=#ffd479>"..self.Config.Settings.VoteDuration.." seconds</color>\n"..
				self.Config.Settings.Prefix.." Vote change allowed: <color=#ffd479>"..self.Config.Settings.AllowVoteChange.."</color>\n"..
				self.Config.Settings.Prefix.." Day hour: <color=#ffd479>"..self.Config.Settings.DayHour.."</color>"
			)
			rust.SendChatMessage(player,
				self.Config.Settings.Prefix.." Night hour: <color=#ffd479>"..self.Config.Settings.NightHour.."</color>\n\n"..
				self.Config.Settings.Prefix.." <color=#ffd479>Note</color>: Allowed nights: 1 = every night, 2 = every other night, etc., +1 = skip after 1 night, +2 = skip after 2 nights, etc.\n\n"..
				self.Config.Settings.Prefix.." Active day vote: <color=#ffd479>"..tostring(ActiveVote).."</color>\n"..
				self.Config.Settings.Prefix.." Current hour: <color=#ffd479>"..tostring(VSky.Cycle.Hour):match"([^.]*).(.*)".."</color>"
			)
			return
		end
		if self.Config.Settings.AutoSkip == "true" then
			local message = {}
			if string.sub(self.Config.Settings.AllowAutoNights, 1, 1) == "+" then
				local AllowAutoNights = string.sub(self.Config.Settings.AllowAutoNights, 2)
				message = FormatMessage(self.Config.Messages.PlusAutoSkipError, { allowed = AllowAutoNights + 1 })
				else
				message = FormatMessage(self.Config.Messages.AutoSkipError, { allowed = self.Config.Settings.AllowAutoNights })
			end
			rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..message)
			return
		end
		if not ActiveVote then
			rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.NoVote)
			return
		end
		if func == "progress" then
			local message, pCount = "", self:getPlayerCount()
			if VoteEnd or RevoteEnd then
				if VoteEnd then
					if isPercent then
						local yes, no = self:GetPercentCount(pCount)
						message = FormatMessage(self.Config.Messages.ManualPercentProgress, { yes = yes, no = no, players = pCount, required = VoteRequired })
						else
						message = FormatMessage(self.Config.Messages.ManualProgress, { yes = VoteType["yes"], no = VoteType["no"], required = VoteRequired })
					end
					rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..message)
					else
					if isPercentRV then
						local yes, no = self:GetPercentCount(pCount)
						message = FormatMessage(self.Config.Messages.ManualPercentProgress, { yes = yes, no = no, players = pCount, required = string.sub(self.Config.Revote.VoteRequired, 1, -2) })
						else
						message = FormatMessage(self.Config.Messages.ManualProgress, { yes = VoteType["yes"], no = VoteType["no"], required = self.Config.Revote.VoteRequired })
					end
					rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..self.Config.Revote.Prefix.." "..message)
				end
			end
			return
		end
		if func == "yes" then
			if VoteList[playerSteamID] == "yes" then
				local message = FormatMessage(self.Config.Messages.AlreadyVoted, { vote = func })
				rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..message)
				return
			end
			if not permission.UserHasPermission(playerSteamID, "dayvote.admin") then
				if VoteList[playerSteamID] and self.Config.Settings.AllowVoteChange == "false" then
					local message = FormatMessage(self.Config.Messages.NoVoteChange, { vote = VoteList[playerSteamID] })
					rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..message)
					return
				end
				else
				if self.Config.Settings.AdminExempt == "true" then
					rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.AdminExempt)
					return
				end
			end
			if VoteList[playerSteamID] == "no" then VoteType["no"] = VoteType["no"] - 1 end
			VoteList[playerSteamID] = "yes"
			VoteType["yes"] = VoteType["yes"] + 1
			local message = FormatMessage(self.Config.Messages.Voted, { vote = "yes" })
			rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..message)
			if self.Config.Settings.ShowPlayerVotes == "true" then
				local message = FormatMessage(self.Config.Messages.PlayerVotes, { player = player.displayName, vote = "yes" })
				rust.BroadcastChat(self.Config.Settings.Prefix.." "..message)
			end
			self:CheckPercentVote()
			return
		end
		if func == "no" then
			if VoteList[playerSteamID] == "no" then
				local message = FormatMessage(self.Config.Messages.AlreadyVoted, { vote = func })
				rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..message)
				return
			end
			if not permission.UserHasPermission(playerSteamID, "dayvote.admin") then
				if VoteList[playerSteamID] and self.Config.Settings.AllowVoteChange == "false" then
					local message = FormatMessage(self.Config.Messages.NoVoteChange, { vote = VoteList[playerSteamID] })
					rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..message)
					return
				end
				else
				if self.Config.Settings.AdminExempt == "true" then
					rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.AdminExempt)
					return
				end
			end
			if VoteList[playerSteamID] == "yes" then VoteType["yes"] = VoteType["yes"] - 1 end
			VoteList[playerSteamID] = "no"
			VoteType["no"] = VoteType["no"] + 1
			local message = FormatMessage(self.Config.Messages.Voted, { vote = "no" })
			rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..message)
			if self.Config.Settings.ShowPlayerVotes == "true" then
				local message = FormatMessage(self.Config.Messages.PlayerVotes, { player = player.displayName, vote = "no" })
				rust.BroadcastChat(self.Config.Settings.Prefix.." "..message)
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
				local message = FormatMessage(self.Config.Messages.SkipPlusAutoSkip, { allowed = AllowAutoNights + 1 })
				rust.BroadcastChat(self.Config.Settings.Prefix.." "..message)
				return
			end
			else
			if self.Config.Settings.AllowAutoNights ~= "1" then
				AutoCurrentNight = AutoCurrentNight + 1
				if AutoCurrentNight > tonumber(self.Config.Settings.AllowAutoNights) then
					AutoCurrentNight = 1
				end
				if AutoCurrentNight > 1 then
					local message = FormatMessage(self.Config.Messages.SkipAutoSkip, { allowed = self.Config.Settings.AllowAutoNights, current = AutoCurrentNight })
					rust.BroadcastChat(self.Config.Settings.Prefix.." "..message)
					return
				end
			end
		end
		rust.BroadcastChat(self.Config.Settings.Prefix.." "..self.Config.Messages.AutoSkip)
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
				local message = FormatMessage(self.Config.Messages.PlusVoteSkip, { allowed = AllowVoteNights + 1 })
				rust.BroadcastChat(self.Config.Settings.Prefix.." "..message)
				return
			end
			else
			if self.Config.Settings.AllowVoteNights ~= "1" then
				CurrentNight = CurrentNight + 1
				if CurrentNight > tonumber(self.Config.Settings.AllowVoteNights) then
					CurrentNight = 1
				end
				if CurrentNight > 1 then
					local message = FormatMessage(self.Config.Messages.VoteSkip, { allowed = self.Config.Settings.AllowVoteNights, current = CurrentNight })
					rust.BroadcastChat(self.Config.Settings.Prefix.." "..message)
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
		VoteRequired = tonumber(string.sub(self.Config.Settings.VoteRequired, 1, -2))
		else
		VoteRequired = tonumber(self.Config.Settings.VoteRequired)
	end
	if call == "cycle" then
		AllowRevote = true
		local message = ""
		if isPercent then
			local pCount = self:getPlayerCount()
			message = FormatMessage(self.Config.Messages.PercentVoteOpen, { required = VoteRequired, players = pCount, seconds = self.Config.Settings.VoteDuration })
			else
			message = FormatMessage(self.Config.Messages.VoteOpen, { required = VoteRequired, seconds = self.Config.Settings.VoteDuration })
		end
		rust.BroadcastChat(self.Config.Settings.Prefix.." "..message)
	end
	if call == "admin" then
		AutoCurrentNight = 0
		CurrentNight = 0
		local message = ""
		if creq then VoteRequired = tonumber(creq) end
		if isPercent then
			local pCount = self:getPlayerCount()
			message = FormatMessage(self.Config.Messages.ManualPercentVoteOpen, { required = VoteRequired, players = pCount, seconds = self.Config.Settings.VoteDuration })
			else
			message = FormatMessage(self.Config.Messages.ManualVoteOpen, { required = VoteRequired, seconds = self.Config.Settings.VoteDuration })
		end
		rust.BroadcastChat(self.Config.Settings.Prefix.." "..message)
	end
	if self.Config.Settings.ShowProgress == "true" then
		local VoteDuration = self.Config.Settings.VoteDuration
		VoteProgress = timer.Repeat(tonumber(self.Config.Settings.ProgressInterval), 0, function()
			VoteDuration = VoteDuration - self.Config.Settings.ProgressInterval
			local message = {}
			if isPercent then
				local pCount = self:getPlayerCount()
				local yes, no = self:GetPercentCount(pCount)
				message = FormatMessage(self.Config.Messages.PercentProgress, { yes = yes, no = no, players = pCount, required = VoteRequired, seconds = VoteDuration })
				else
				message = FormatMessage(self.Config.Messages.Progress, { yes = VoteType["yes"], no = VoteType["no"], required = VoteRequired, seconds = VoteDuration })
			end
			if VoteDuration > 0 then rust.BroadcastChat(self.Config.Settings.Prefix.." "..message) end
		end, self.Plugin)
	end
	VoteEnd = timer.Once(tonumber(self.Config.Settings.VoteDuration), function()
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
				local message = FormatMessage(self.Config.Messages.PercentVotePassed, { required = VoteRequired, no = no, players = pCount })
				rust.BroadcastChat(self.Config.Settings.Prefix.." "..message)
				self:SetDayTime()
				return
			end
			if VoteType["yes"] == "0" and VoteType["no"] == "0" then
				rust.BroadcastChat(self.Config.Settings.Prefix.." "..self.Config.Messages.VoteFailedNoVote)
				else
				local yes, no = self:GetPercentCount(pCount)
				local message = FormatMessage(self.Config.Messages.PercentVoteFailed, { yes = yes, no = no, players = pCount, required = VoteRequired })
				rust.BroadcastChat(self.Config.Settings.Prefix.." "..message)
			end
			if self.Config.Revote.Enabled == "true" and AllowRevote then
				if self.Config.Revote.Announce == "true" then
					local message = FormatMessage(self.Config.Messages.Revote, { seconds = self.Config.Revote.WaitDuration })
					rust.BroadcastChat(self.Config.Settings.Prefix.." "..self.Config.Revote.Prefix.." "..message)
				end
				RevoteWait = timer.Once(tonumber(self.Config.Revote.WaitDuration), function() self:reVote() end, self.Plugin)
			end
			return
			else
			if tonumber(VoteType["yes"]) > tonumber(VoteType["no"]) then
				local CheckVote = VoteType["yes"] - VoteType["no"]
				if CheckVote >= VoteRequired then
					local message = FormatMessage(self.Config.Messages.VotePassed, { yes = VoteType["yes"], no = VoteType["no"] })
					rust.BroadcastChat(self.Config.Settings.Prefix.." "..message)
					self:SetDayTime()
					return
				end
			end
			if VoteType["yes"] == "0" and VoteType["no"] == "0" then
				rust.BroadcastChat(self.Config.Settings.Prefix.." "..self.Config.Messages.VoteFailedNoVote)
				else
				local message = FormatMessage(self.Config.Messages.VoteFailed, { yes = VoteType["yes"], no = VoteType["no"], required = VoteRequired })
				rust.BroadcastChat(self.Config.Settings.Prefix.." "..message)
			end
			if self.Config.Revote.Enabled == "true" and AllowRevote then
				if self.Config.Revote.Announce == "true" then
					local message = FormatMessage(self.Config.Messages.Revote, { seconds = self.Config.Revote.WaitDuration })
					rust.BroadcastChat(self.Config.Settings.Prefix.." "..self.Config.Revote.Prefix.." "..message)
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
		message = FormatMessage(self.Config.Messages.PercentVoteOpen, { required = string.sub(self.Config.Revote.VoteRequired, 1, -2), players = pCount, seconds = self.Config.Revote.VoteDuration })
		else
		message = FormatMessage(self.Config.Messages.VoteOpen, { required = self.Config.Revote.VoteRequired, seconds = self.Config.Revote.VoteDuration })
	end
	rust.BroadcastChat(self.Config.Settings.Prefix.." "..self.Config.Revote.Prefix.." "..message)
	if self.Config.Revote.ShowProgress == "true" then
		local VoteDuration = self.Config.Revote.VoteDuration
		RevoteProgress = timer.Repeat(tonumber(self.Config.Revote.ProgressInterval), 0, function()
			VoteDuration = VoteDuration - self.Config.Revote.ProgressInterval
			local message = {}
			if isPercentRV then
				local pCount = self:getPlayerCount()
				local yes, no = self:GetPercentCount(pCount)
				message = FormatMessage(self.Config.Messages.PercentProgress, { yes = yes, no = no, players = pCount, required = string.sub(self.Config.Revote.VoteRequired, 1, -2), seconds = VoteDuration })
				else
				message = FormatMessage(self.Config.Messages.Progress, { yes = VoteType["yes"], no = VoteType["no"], required = self.Config.Revote.VoteRequired, seconds = VoteDuration })
			end
			if VoteDuration > 0 then rust.BroadcastChat(self.Config.Settings.Prefix.." "..self.Config.Revote.Prefix.." "..message) end
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
				local message = FormatMessage(self.Config.Messages.PercentVotePassed, { required = string.sub(self.Config.Revote.VoteRequired, 1, -2), no = no, players = pCount })
				rust.BroadcastChat(self.Config.Settings.Prefix.." "..self.Config.Revote.Prefix.." "..message)
				self:SetDayTime()
				return
			end
			if VoteType["yes"] == "0" and VoteType["no"] == "0" then
				rust.BroadcastChat(self.Config.Settings.Prefix.." "..self.Config.Revote.Prefix.." "..self.Config.Messages.VoteFailedNoVote)
				else
				local yes, no = self:GetPercentCount(pCount)
				local message = FormatMessage(self.Config.Messages.PercentVoteFailed, { yes = yes, no = no, players = pCount, required = string.sub(self.Config.Revote.VoteRequired, 1, -2) })
				rust.BroadcastChat(self.Config.Settings.Prefix.." "..self.Config.Revote.Prefix.." "..message)
			end
			return
			else
			if tonumber(VoteType["yes"]) > tonumber(VoteType["no"]) then
				local CheckVote = VoteType["yes"] - VoteType["no"]
				if CheckVote >= tonumber(self.Config.Revote.VoteRequired) then
					local message = FormatMessage(self.Config.Messages.VotePassed, { yes = VoteType["yes"], no = VoteType["no"] })
					rust.BroadcastChat(self.Config.Settings.Prefix.." "..self.Config.Revote.Prefix.." "..message)
					self:SetDayTime()
					return
				end
			end
			if VoteType["yes"] == "0" and VoteType["no"] == "0" then
				rust.BroadcastChat(self.Config.Settings.Prefix.." "..self.Config.Revote.Prefix.." "..self.Config.Messages.VoteFailedNoVote)
				else
				local message = FormatMessage(self.Config.Messages.VoteFailed, { yes = VoteType["yes"], no = VoteType["no"], required = self.Config.Revote.VoteRequired })
				rust.BroadcastChat(self.Config.Settings.Prefix.." "..self.Config.Revote.Prefix.." "..message)
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
					local message = FormatMessage(self.Config.Messages.PercentVotePassed, { required = VoteRequired, no = no, players = pCount })
					rust.BroadcastChat(self.Config.Settings.Prefix.." "..message)
					else
					local message = FormatMessage(self.Config.Messages.PercentVotePassed, { required = string.sub(self.Config.Revote.VoteRequired, 1, -2), no = no, players = pCount })
					rust.BroadcastChat(self.Config.Settings.Prefix.." "..self.Config.Revote.Prefix.." "..message)
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

function PLUGIN:SendHelpText(player)
	rust.SendChatMessage(player, "<color=#ffd479>/dayvote</color> - Allows automatic or player voting to skip night cycles.")
end						