PLUGIN.Title        = "Radar Manager"
PLUGIN.Description  = "Shows clan members, mutual friends, attackers and hit mark locations to other players."
PLUGIN.Author       = "InSaNe8472"
PLUGIN.Version      = V(1,0,9)
PLUGIN.ResourceID   = 1392

local ClanPlugin = "Clans"
local FriendPlugin = "Friends"
local AdminData = {}
local PlayerData = {}
local PlayerAData = {}
local PlayerHData = {}
local PlayerATimer = {}
local PlayerHTimer = {}
local Active, RSky, RefreshClanRadar, RefreshFriendRadar, ProcessData, clans, friendsAPI

function PLUGIN:Init()
	permission.RegisterPermission("radarmanager.all", self.Plugin)
	permission.RegisterPermission("radarmanager.clan", self.Plugin)
	permission.RegisterPermission("radarmanager.friend", self.Plugin)
	permission.RegisterPermission("radarmanager.attack", self.Plugin)
	permission.RegisterPermission("radarmanager.hit", self.Plugin)
	permission.RegisterPermission("radarmanager.hide", self.Plugin)
	permission.RegisterPermission("radarmanager.immune", self.Plugin)
	permission.RegisterPermission("radarmanager.admin", self.Plugin)
	command.AddChatCommand("rm", self.Plugin, "cmdRadarManager")
	self:LoadDefaultConfig()
end

function PLUGIN:LoadDefaultConfig()
	self.Config.Settings = self.Config.Settings or {}
	self.Config.Admin = self.Config.Admin or {}
	self.Config.Clan = self.Config.Clan or {}
	self.Config.Friend = self.Config.Friend or {}
	self.Config.Attack = self.Config.Attack or {}
	self.Config.Hit = self.Config.Hit or {}
	self.Config.Messages = self.Config.Messages or {}
	self.Config.Settings.EnableLimits = self.Config.Settings.EnableLimits or "true"
	self.Config.Settings.Prefix = self.Config.Settings.Prefix or "[ <color=#cd422b>Radar Manager</color> ]"
	self.Config.Settings.RememberPlayerConfig = self.Config.Settings.RememberPlayerConfig or "false"
	self.Config.Settings.NotifyForced = self.Config.Settings.NotifyForced or "true"
	self.Config.Settings.AllowForceChange = self.Config.Settings.AllowForceChange or "false"
	self.Config.Settings.ForceClan = self.Config.Settings.ForceClan or "false"
	self.Config.Settings.ForceFriend = self.Config.Settings.ForceFriend or "false"
	self.Config.Settings.ForceAttack = self.Config.Settings.ForceAttack or "false"
	self.Config.Settings.ForceHit = self.Config.Settings.ForceHit or "false"
	self.Config.Settings.UsePermissions = self.Config.Settings.UsePermissions or "true"
	self.Config.Settings.EnforceHours = self.Config.Settings.EnforceHours or "false"
	self.Config.Settings.StartHour = self.Config.Settings.StartHour or "0"
	self.Config.Settings.EndHour = self.Config.Settings.EndHour or "23"
	self.Config.Admin.ActiveTag = self.Config.Admin.ActiveTag or "<size=12><color=#cd422b>{player}</color> (<color=#ffd479>{location}</color>)</size>"
	self.Config.Admin.SleepTag = self.Config.Admin.SleepTag or "(SLEEP) <size=12><color=#cd422b>{player}</color> (<color=#ffd479>{location}</color>)</size>"
	self.Config.Admin.ActiveTagOffset = self.Config.Admin.ActiveTagOffset or "2"
	self.Config.Admin.SleepTagOffset = self.Config.Admin.SleepTagOffset or "1"
	self.Config.Clan.Enabled = self.Config.Clan.Enabled or "true"
	self.Config.Clan.Refresh = self.Config.Clan.Refresh or "3"
	self.Config.Clan.Radius = self.Config.Clan.Radius or "300"
	self.Config.Clan.Tag = self.Config.Clan.Tag or "<size=12><color=#cd422b>{player}</color> - <color=#ffd479>{location}</color> ({range}m)</size>"
	self.Config.Clan.TagOffset = self.Config.Clan.TagOffset or "2"
	self.Config.Friend.Enabled = self.Config.Friend.Enabled or "true"
	self.Config.Friend.Refresh = self.Config.Friend.Refresh or "3"
	self.Config.Friend.Radius = self.Config.Friend.Radius or "300"
	self.Config.Friend.Tag = self.Config.Friend.Tag or "<size=12><color=#2B60DE>{player}</color> - <color=#ffd479>{location}</color> ({range}m)</size>"
	self.Config.Friend.TagOffset = self.Config.Friend.TagOffset or "2"
	self.Config.Attack.Enabled = self.Config.Attack.Enabled or "false"
	self.Config.Attack.EnabledFirearm = self.Config.Attack.EnabledFirearm or "true"
	self.Config.Attack.EnabledRocket = self.Config.Attack.EnabledRocket or "true"
	self.Config.Attack.EnabledThrow = self.Config.Attack.EnabledThrow or "false"
	self.Config.Attack.ShowClan = self.Config.Attack.ShowClan or "false"
	self.Config.Attack.ShowFriend = self.Config.Attack.ShowFriend or "false"
	self.Config.Attack.Radius = self.Config.Attack.Radius or "300"
	self.Config.Attack.AttackTimeout = self.Config.Attack.AttackTimeout or "10"
	self.Config.Attack.TagTimeout = self.Config.Attack.TagTimeout or "2"
	self.Config.Attack.Tag = self.Config.Attack.Tag or "<size=12><color=#41A317>{player}</color> - <color=#ffd479>{location}</color> ({range}m)</size>"
	self.Config.Attack.TagOffset = self.Config.Attack.TagOffset or "2"
	self.Config.Hit.Enabled = self.Config.Hit.Enabled or "false"
	self.Config.Hit.ShowOnlineOnly = self.Config.Hit.ShowOnlineOnly or "false"
	self.Config.Hit.ShowClan = self.Config.Hit.ShowClan or "false"
	self.Config.Hit.ShowFriend = self.Config.Hit.ShowFriend or "false"
	self.Config.Hit.Radius = self.Config.Hit.Radius or "300"
	self.Config.Hit.HitTimeout = self.Config.Hit.AttackTimeout or "5"
	self.Config.Hit.TagTimeout = self.Config.Hit.TagTimeout or "0.5"
	self.Config.Hit.Tag = self.Config.Hit.Tag or "<size=12><color=#F87217>{player}</color> - <color=#ffd479>{location}</color> ({range}m)</size>"
	self.Config.Hit.TagOffset = self.Config.Hit.TagOffset or "2"
	self.Config.Messages.NoPermission = self.Config.Messages.NoPermission or "You do not have permission to use this command."
	self.Config.Messages.AdminRadarNotEnabled = self.Config.Messages.AdminRadarNotEnabled or "Administrator radar is not enabled."
	self.Config.Messages.AdminRadarEnabled = self.Config.Messages.AdminRadarEnabled or "Administrator radar enabled: <color=#cd422b>{radius} meter</color> radius, <color=#cd422b>{refresh} second</color> refresh, show sleepers <color=#cd422b>{sleepers}</color>"
	self.Config.Messages.AdminRadarDisabled = self.Config.Messages.AdminRadarDisabled or "Administrator radar disabled."
	self.Config.Messages.AdminDisabled = self.Config.Messages.AdminDisabled or "The <color=#cd422b>{radar}</color> radar has been disabled by an administrator.  Your <color=#cd422b>{radar}</color> radar has been disabled."
	self.Config.Messages.HourDisabled = self.Config.Messages.HourDisabled or "Radar systems may only be used between in-game hours of <color=#cd422b>{shour}</color> and <color=#cd422b>{ehour}</color>.  Current time is <color=#cd422b>{current}</color>.  All your active radar systems have been disabled."
	self.Config.Messages.ChangedStatus = self.Config.Messages.ChangedStatus or "Radar system {radar} <color=#cd422b>{status}</color>."
	self.Config.Messages.TypeDisabled = self.Config.Messages.TypeDisabled or "Radar system <color=#cd422b>{radar}</color> is currently disabled."
	self.Config.Messages.TypeDisabledPlugin = self.Config.Messages.TypeDisabledPlugin or "Radar system <color=#cd422b>{radar}</color> cannot be toggled.  The required plugin <color=#cd422b>{plugin}</color> is not installed."
	self.Config.Messages.PlayerRadarEnabled = self.Config.Messages.PlayerRadarEnabled or "You have <color=#cd422b>enabled</color> {radar} radar.  Players will be visible within a <color=#cd422b>{radius} meter</color> radius."
	self.Config.Messages.PlayerRadarDisabled = self.Config.Messages.PlayerRadarDisabled or "You have <color=#cd422b>disabled</color> {radar} radar."
	self.Config.Messages.NotEnabled = self.Config.Messages.NotEnabled or "All radar systems are <color=#cd422b>disabled</color>."
	self.Config.Messages.WrongArgs = self.Config.Messages.WrongArgs or "Syntax error.  Use <color=#cd422b>/rm</color> for help."
	self.Config.Messages.WrongHour = self.Config.Messages.WrongHour or "Radar systems may only be used between in-game hours of <color=#cd422b>{shour}</color> and <color=#cd422b>{ehour}</color>.  Current time is <color=#cd422b>{current}</color>."
	self.Config.Messages.Forced = self.Config.Messages.Forced or "Radar system <color=#cd422b>{radar}</color> is forced enabled and cannot be toggled."
	self.Config.Messages.NotifyForced = self.Config.Messages.NotifyForced or "One or more radar systems are force enabled.  Use <color=#cd422b>/rm</color> for help."
	self.Config.CustomPermissions = self.Config.CustomPermissions or {
		{["Permission"] = "radarmanager.vip1", ["ClanRadius"] = "400", ["FriendRadius"] = "400", ["AttackRadius"] = "400", ["HitRadius"] = "400", ["Tag"] = "<size=13><color=#FF00FF>{player}</color> - <color=#ffd479>{location}</color> ({range}m)</size>"},
		{["Permission"] = "radarmanager.vip2", ["ClanRadius"] = "500", ["FriendRadius"] = "500", ["AttackRadius"] = "500", ["HitRadius"] = "500", ["Tag"] = "<size=13><color=#FF00FF>{player}</color> - <color=#ffd479>{location}</color> ({range}m)</size>"}
	}
	if not tonumber(self.Config.Settings.StartHour) or tonumber(self.Config.Settings.StartHour) < 0 or tonumber(self.Config.Settings.StartHour) > 23 then self.Config.Settings.StartHour = "0" end
	if not tonumber(self.Config.Settings.EndHour) or tonumber(self.Config.Settings.EndHour) < 0 or tonumber(self.Config.Settings.EndHour) > 23 then self.Config.Settings.EndHour = "23" end
	if not tonumber(self.Config.Admin.ActiveTagOffset) or tonumber(self.Config.Admin.ActiveTagOffset) < 1 then self.Config.Admin.ActiveTagOffset = "2" end
	if not tonumber(self.Config.Admin.SleepTagOffset) or tonumber(self.Config.Admin.SleepTagOffset) < 1 then self.Config.Admin.SleepTagOffset = "1" end
	if not tonumber(self.Config.Clan.Refresh) or tonumber(self.Config.Clan.Refresh) < 1 then self.Config.Clan.Refresh = "3" end
	if not tonumber(self.Config.Clan.Radius) or tonumber(self.Config.Clan.Radius) < 1 then self.Config.Clan.Radius = "300" end
	if not tonumber(self.Config.Clan.TagOffset) or tonumber(self.Config.Clan.TagOffset) < 1 then self.Config.Clan.TagOffset = "2" end
	if not tonumber(self.Config.Friend.Refresh) or tonumber(self.Config.Friend.Refresh) < 1 then self.Config.Friend.Refresh = "3" end
	if not tonumber(self.Config.Friend.Radius) or tonumber(self.Config.Friend.Radius) < 1 then self.Config.Friend.Radius = "300" end
	if not tonumber(self.Config.Friend.TagOffset) or tonumber(self.Config.Friend.TagOffset) < 1 then self.Config.Friend.TagOffset = "2" end
	if not tonumber(self.Config.Attack.Radius) or tonumber(self.Config.Attack.Radius) < 1 then self.Config.Attack.Radius = "300" end
	if not tonumber(self.Config.Attack.AttackTimeout) or tonumber(self.Config.Attack.AttackTimeout) < 1 then self.Config.Attack.AttackTimeout = "10" end
	if not tonumber(self.Config.Attack.TagTimeout) or tonumber(self.Config.Attack.TagTimeout) < 1 then self.Config.Attack.TagTimeout = "2" end
	if tonumber(self.Config.Attack.TagTimeout) > tonumber(self.Config.Attack.AttackTimeout) then self.Config.Attack.TagTimeout = tonumber(self.Config.Attack.AttackTimeout) - 1 end
	if not tonumber(self.Config.Attack.TagOffset) or tonumber(self.Config.Attack.TagOffset) < 1 then self.Config.Attack.TagOffset = "2" end
	if not tonumber(self.Config.Hit.Radius) or tonumber(self.Config.Hit.Radius) < 1 then self.Config.Hit.Radius = "300" end
	if not tonumber(self.Config.Hit.HitTimeout) or tonumber(self.Config.Hit.HitTimeout) < 1 then self.Config.Hit.HitTimeout = "5" end
	if not tonumber(self.Config.Hit.TagTimeout) or tonumber(self.Config.Hit.TagTimeout) < 1 then self.Config.Hit.TagTimeout = "0.5" end
	if tonumber(self.Config.Hit.TagTimeout) > tonumber(self.Config.Hit.HitTimeout) then self.Config.Hit.TagTimeout = tonumber(self.Config.Hit.HitTimeout) - 1 end
	if not tonumber(self.Config.Hit.TagOffset) or tonumber(self.Config.Hit.TagOffset) < 1 then self.Config.Hit.TagOffset = "2" end
	if self.Config.CustomPermissions then
		for current, data in pairs(self.Config.CustomPermissions) do
			permission.RegisterPermission(data.Permission, self.Plugin)
		end
	end
	self:SaveConfig()
end

function PLUGIN:OnServerInitialized()
	clans = plugins.Find(ClanPlugin) or false
	friendsAPI = plugins.Find(FriendPlugin) or false
	if clans or friendsAPI then	self:StartRadar() end
	if self.Config.Attack.Enabled == "true" or self.Config.Hit.Enabled == "true" then self:StartProcessData() end
	if self.Config.Settings.ForceClan == "true" or self.Config.Settings.ForceFriend == "true" or self.Config.Settings.ForceAttack == "true" or self.Config.Settings.ForceHit == "true" then
		local players = global.BasePlayer.activePlayerList:GetEnumerator()
		while players:MoveNext() do
			local playerSteamID = rust.UserIDFromPlayer(players.Current)
			local ForceClan, ForceFriend, ForceAttack, ForceHit = self:ForcePermissions(playerSteamID)
			PlayerData[playerSteamID] = ForceClan..":"..ForceFriend..":"..ForceAttack..":"..ForceHit
			if self.Config.Settings.NotifyForced == "true" then
				if ForceClan == "1" or ForceFriend == "1" or ForceAttack == "1" or ForceHit == "1" then
					rust.SendChatMessage(players.Current, self.Config.Settings.Prefix.." "..self.Config.Messages.NotifyForced)
				end
			end
		end
	end
end

function PLUGIN:StartRadar()
	if self.Config.Clan.Enabled == "true" and not RefreshClanRadar then
		RefreshClanRadar = timer.Repeat(tonumber(self.Config.Clan.Refresh), 0, function() self:RefreshClanRadar() end, self.Plugin)
	end
	if self.Config.Friend.Enabled == "true" and not RefreshFriendRadar then
		RefreshFriendRadar = timer.Repeat(tonumber(self.Config.Friend.Refresh), 0, function() self:RefreshFriendRadar() end, self.Plugin)
	end
end

function PLUGIN:StartProcessData()
	ProcessData = timer.Repeat(1, 0, function() self:ProcessPlayerData() end, self.Plugin)
end

function PLUGIN:OnPlayerInit(player)
	if self.Config.Settings.ForceClan == "true" or self.Config.Settings.ForceFriend == "true" or self.Config.Settings.ForceAttack == "true" or self.Config.Settings.ForceHit == "true" then
		local playerSteamID = rust.UserIDFromPlayer(player)
		local ForceClan, ForceFriend, ForceAttack, ForceHit = self:ForcePermissions(playerSteamID)
		PlayerData[playerSteamID] = ForceClan..":"..ForceFriend..":"..ForceAttack..":"..ForceHit
		if self.Config.Settings.NotifyForced == "true" then
			if ForceClan == "1" or ForceFriend == "1" or ForceAttack == "1" or ForceHit == "1" then
				rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.NotifyForced)
			end
		end
	end
end

function PLUGIN:OnPlayerDisconnected(player)
	if self.Config.Settings.RememberPlayerConfig ~= "true" then
		local playerSteamID = rust.UserIDFromPlayer(player)
		if PlayerData[playerSteamID] or PlayerData[playerSteamID] ~= nil then PlayerData[playerSteamID] = nil end
	end
end

function PLUGIN:ForcePermissions(playerSteamID)
	local ForceClan, ForceFriend, ForceAttack, ForceHit = "0", "0", "0", "0"
	if self.Config.Clan.Enabled == "true" then
		if self.Config.Settings.UsePermissions == "true" then
			if permission.UserHasPermission(playerSteamID, "radarmanager.admin") or permission.UserHasPermission(playerSteamID, "radarmanager.clan") or permission.UserHasPermission(playerSteamID, "radarmanager.all") then
				if clans and self.Config.Settings.ForceClan == "true" then ForceClan = "1" end
			end
			else
			if clans and self.Config.Settings.ForceClan == "true" then ForceClan = "1" end
		end
	end
	if self.Config.Friend.Enabled == "true" then
		if self.Config.Settings.UsePermissions == "true" then
			if permission.UserHasPermission(playerSteamID, "radarmanager.admin") or permission.UserHasPermission(playerSteamID, "radarmanager.friend") or permission.UserHasPermission(playerSteamID, "radarmanager.all") then
				if friendsAPI and self.Config.Settings.ForceFriend == "true" then ForceFriend = "1" end 
			end
			else
			if friendsAPI and self.Config.Settings.ForceFriend == "true" then ForceFriend = "1" end 
		end
	end
	if self.Config.Attack.Enabled == "true" then
		if self.Config.Settings.UsePermissions == "true" then
			if permission.UserHasPermission(playerSteamID, "radarmanager.admin") or permission.UserHasPermission(playerSteamID, "radarmanager.attack") or permission.UserHasPermission(playerSteamID, "radarmanager.all") then
				if self.Config.Settings.ForceAttack == "true" then ForceAttack = "1" end 
			end
			else
			if self.Config.Settings.ForceAttack == "true" then ForceAttack = "1" end 
		end
	end
	if self.Config.Hit.Enabled == "true" then
		if self.Config.Settings.UsePermissions == "true" then
			if permission.UserHasPermission(playerSteamID, "radarmanager.admin") or permission.UserHasPermission(playerSteamID, "radarmanager.hit") or permission.UserHasPermission(playerSteamID, "radarmanager.all") then
				if self.Config.Settings.ForceHit == "true" then ForceHit = "1" end 
			end
			else
			if self.Config.Settings.ForceHit == "true" then ForceHit = "1" end 
		end
	end
	return ForceClan, ForceFriend, ForceAttack, ForceHit
end

local function FormatMessage(message, values)
	for key, value in pairs(values) do message = message:gsub("{" .. key .. "}", value) end
	return message
end

function PLUGIN:OnTick()
	if self.Config.Settings.EnforceHours ~= "false" then
		if self.Config.Clan.Enabled == "true" or self.Config.Friend.Enabled == "true" or self.Config.Attack.Enabled == "true" or self.Config.Hit.Enabled == "true" then
			if not RSky then RSky = global.TOD_Sky.get_Instance() end
			local CurHour = tostring(RSky.Cycle.Hour):match"([^.]*).(.*)"
			if not Active and tonumber(CurHour) >= tonumber(self.Config.Settings.StartHour) and tonumber(CurHour) < tonumber(self.Config.Settings.EndHour) then Active = true end
			if Active then
				if tonumber(CurHour) < tonumber(self.Config.Settings.StartHour) or tonumber(CurHour) >= tonumber(self.Config.Settings.EndHour) then
					Active = false
					local message = FormatMessage(self.Config.Messages.HourDisabled, { shour = self.Config.Settings.StartHour, ehour = self.Config.Settings.EndHour, current = CurHour })
					local players = global.BasePlayer.activePlayerList:GetEnumerator()
					while players:MoveNext() do
						local playerSteamID = rust.UserIDFromPlayer(players.Current)
						if not permission.UserHasPermission(playerSteamID, "radarmanager.admin") and not permission.UserHasPermission(playerSteamID, "radarmanager.immune") then
							if PlayerData[playerSteamID] or PlayerData[playerSteamID] ~= nil then
								if string.match(PlayerData[playerSteamID], "1") then rust.SendChatMessage(players.Current, self.Config.Settings.Prefix.." "..message) end
								PlayerData[playerSteamID] = nil
							end
						end
					end
				end
			end
		end
	end
end

function PLUGIN:cmdRadarManager(player, cmd, args)
	local playerSteamID = rust.UserIDFromPlayer(player)
	if self.Config.Settings.UsePermissions == "true" then
		if not permission.UserHasPermission(playerSteamID, "radarmanager.admin") and not permission.UserHasPermission(playerSteamID, "radarmanager.all") and
			not permission.UserHasPermission(playerSteamID, "radarmanager.clan") and not permission.UserHasPermission(playerSteamID, "radarmanager.friend") and
			not permission.UserHasPermission(playerSteamID, "radarmanager.attack") and not permission.UserHasPermission(playerSteamID, "radarmanager.hit") then
			rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.NoPermission)
			return
		end
	end
	if args.Length > 0 and args[0] == "toggle" then
		if not permission.UserHasPermission(playerSteamID, "radarmanager.admin") then
			rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.NoPermission)
			return
		end
		if args.Length < 2 then
			rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.WrongArgs)
			return
		end
		local sfunc = args[1]
		if sfunc ~= "clan" and sfunc ~= "friend" and sfunc ~= "attack" and sfunc ~= "hit" then
			rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.WrongArgs)
			return
		end
		if sfunc == "clan" then
			if not clans then
				local message = FormatMessage(self.Config.Messages.TypeDisabledPlugin, { radar = "clans", plugin = ClanPlugin })
				rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..message)
				return
			end
			if self.Config.Settings.ForceClan == "true" and self.Config.Settings.AllowForceChange ~= "true" then
				local message = FormatMessage(self.Config.Messages.Forced, { radar = "clan" })
				rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..message)
				return
			end
			local message
			if self.Config.Clan.Enabled == "true" then
				local players = global.BasePlayer.activePlayerList:GetEnumerator()
				while players:MoveNext() do
					local playerSteamID = rust.UserIDFromPlayer(players.Current)
					if PlayerData[playerSteamID] and PlayerData[playerSteamID] ~= nil and string.match(PlayerData[playerSteamID], "1") then
						local clan, friend, attack, hit = tostring(PlayerData[playerSteamID]):match("([^:]+):([^:]+):([^:]+):([^:]+)")
						if clan == "1" then
							PlayerData[playerSteamID] = "0:"..friend..":"..attack..":"..hit
							local message = FormatMessage(self.Config.Messages.AdminDisabled, { radar = "clan" })
							rust.SendChatMessage(players.Current, self.Config.Settings.Prefix.." "..message)
						end
					end
				end
				self.Config.Clan.Enabled = "false"
				if RefreshClanRadar then RefreshClanRadar:Destroy() end
				RefreshClanRadar = nil
				message = FormatMessage(self.Config.Messages.ChangedStatus, { radar = "clan", status = "disabled" })
				else
				self.Config.Clan.Enabled = "true"
				self:StartRadar()
				message = FormatMessage(self.Config.Messages.ChangedStatus, { radar = "clan", status = "enabled" })
			end
			self:SaveConfig()
			rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..message)
			return
		end
		if sfunc == "friend" then
			if not friendsAPI then
				local message = FormatMessage(self.Config.Messages.TypeDisabledPlugin, { radar = "friends", plugin = FriendPlugin })
				rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..message)
				return
			end
			if self.Config.Settings.ForceFriend == "true" and self.Config.Settings.AllowForceChange ~= "true" then
				local message = FormatMessage(self.Config.Messages.Forced, { radar = "friend" })
				rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..message)
				return
			end
			local message
			if self.Config.Friend.Enabled == "true" then
				local players = global.BasePlayer.activePlayerList:GetEnumerator()
				while players:MoveNext() do
					local playerSteamID = rust.UserIDFromPlayer(players.Current)
					if PlayerData[playerSteamID] and PlayerData[playerSteamID] ~= nil and string.match(PlayerData[playerSteamID], "1") then
						local clan, friend, attack, hit = tostring(PlayerData[playerSteamID]):match("([^:]+):([^:]+):([^:]+):([^:]+)")
						if friend == "1" then
							PlayerData[playerSteamID] = clan..":0:"..attack..":"..hit
							local message = FormatMessage(self.Config.Messages.AdminDisabled, { radar = "friend" })
							rust.SendChatMessage(players.Current, self.Config.Settings.Prefix.." "..message)
						end
					end
				end
				self.Config.Friend.Enabled = "false"
				if RefreshFriendRadar then RefreshFriendRadar:Destroy() end
				RefreshFriendRadar = nil
				message = FormatMessage(self.Config.Messages.ChangedStatus, { radar = "friend", status = "disabled" })
				else
				self.Config.Friend.Enabled = "true"
				self:StartRadar()
				message = FormatMessage(self.Config.Messages.ChangedStatus, { radar = "friend", status = "enabled" })
			end
			if self.Config.Clan.Enabled ~= "true" and self.Config.Friend.Enabled ~= "true" then
				if RefreshRadar then RefreshRadar:Destroy() end
				RefreshRadar = nil
			end
			self:SaveConfig()
			rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..message)
			return
		end
		if sfunc == "attack" then
			if self.Config.Settings.ForceAttack == "true" and self.Config.Settings.AllowForceChange ~= "true" then
				local message = FormatMessage(self.Config.Messages.Forced, { radar = "attack" })
				rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..message)
				return
			end
			local message
			if self.Config.Attack.Enabled == "true" then
				local players = global.BasePlayer.activePlayerList:GetEnumerator()
				while players:MoveNext() do
					local playerSteamID = rust.UserIDFromPlayer(players.Current)
					if PlayerData[playerSteamID] and PlayerData[playerSteamID] ~= nil and string.match(PlayerData[playerSteamID], "1") then
						local clan, friend, attack, hit = tostring(PlayerData[playerSteamID]):match("([^:]+):([^:]+):([^:]+):([^:]+)")
						if attack == "1" then
							PlayerData[playerSteamID] = clan..":"..friend..":0:"..hit
							local message = FormatMessage(self.Config.Messages.AdminDisabled, { radar = "attack" })
							rust.SendChatMessage(players.Current, self.Config.Settings.Prefix.." "..message)
						end
					end
				end
				self.Config.Attack.Enabled = "false"
				message = FormatMessage(self.Config.Messages.ChangedStatus, { radar = "attack", status = "disabled" })
				else
				self.Config.Attack.Enabled = "true"
				self:StartProcessData()
				message = FormatMessage(self.Config.Messages.ChangedStatus, { radar = "attack", status = "enabled" })
			end
			if self.Config.Attack.Enabled ~= "true" and self.Config.Hit.Enabled ~= "true" then
				if ProcessData then ProcessData:Destroy() end
				ProcessData = nil
			end
			self:SaveConfig()
			rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..message)
			return
		end
		if sfunc == "hit" then
			if self.Config.Settings.ForceHit == "true" and self.Config.Settings.AllowForceChange ~= "true" then
				local message = FormatMessage(self.Config.Messages.Forced, { radar = "hit" })
				rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..message)
				return
			end
			local message
			if self.Config.Hit.Enabled == "true" then
				local players = global.BasePlayer.activePlayerList:GetEnumerator()
				while players:MoveNext() do
					local playerSteamID = rust.UserIDFromPlayer(players.Current)
					if PlayerData[playerSteamID] and PlayerData[playerSteamID] ~= nil and string.match(PlayerData[playerSteamID], "1") then
						local clan, friend, attack, hit = tostring(PlayerData[playerSteamID]):match("([^:]+):([^:]+):([^:]+):([^:]+)")
						if hit == "1" then
							PlayerData[playerSteamID] = clan..":"..friend..":"..attack..":0"
							local message = FormatMessage(self.Config.Messages.AdminDisabled, { radar = "hit" })
							rust.SendChatMessage(players.Current, self.Config.Settings.Prefix.." "..message)
						end
					end
				end
				self.Config.Hit.Enabled = "false"
				message = FormatMessage(self.Config.Messages.ChangedStatus, { radar = "hit", status = "disabled" })
				else
				self.Config.Hit.Enabled = "true"
				self:StartProcessData()
				message = FormatMessage(self.Config.Messages.ChangedStatus, { radar = "hit", status = "enabled" })
			end
			if self.Config.Attack.Enabled ~= "true" and self.Config.Hit.Enabled ~= "true" then
				if ProcessData then ProcessData:Destroy() end
				ProcessData = nil
			end
			self:SaveConfig()
			rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..message)
			return
		end
	end
	if args.Length > 0 and args[0] == "admin" then
		if not permission.UserHasPermission(playerSteamID, "radarmanager.admin") then
			rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.NoPermission)
			return
		end
		if args.Length < 2 then
			rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.WrongArgs)
			return
		end
		sfunc = args[1]
		if sfunc ~= "off" and not tonumber(sfunc) then
			rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.WrongArgs)
			return
		end
		if sfunc == "off" then
			if not AdminData[playerSteamID] or AdminData[playerSteamID] == nil then
				rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.AdminRadarNotEnabled)
				return
			end
			if AdminData[playerSteamID] then AdminData[playerSteamID]:Destroy() end
			AdminData[playerSteamID] = nil
			rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.AdminRadarDisabled)
			return
		end
		if args.Length < 4 then
			rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.WrongArgs)
			return
		end
		local _sfunc = args[2]
		local sleepers = args[3]
		if tonumber(sfunc) < 1 or not tonumber(_sfunc) or tonumber(_sfunc) < 1 then
			rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.WrongArgs)
			return
		end
		if not tonumber(sleepers) or tonumber(sleepers) < 0 or tonumber(sleepers) > 1 then
			rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.WrongArgs)
			return
		end
		if AdminData[playerSteamID] then AdminData[playerSteamID]:Destroy() end
		AdminData[playerSteamID] = nil
		AdminData[playerSteamID] = timer.Repeat(tonumber(_sfunc), 0, function() self:RefreshAdminRadar(player, tonumber(sfunc), tonumber(_sfunc), tonumber(sleepers)) end, self.Plugin)
		local showsleepers = "false"
		if sleepers == "1" then showsleepers = "true" end
		local message = FormatMessage(self.Config.Messages.AdminRadarEnabled, { radius = sfunc, refresh = _sfunc, sleepers = showsleepers })
		rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..message)
		return
	end
	if self.Config.Clan.Enabled ~= "true" and self.Config.Friend.Enabled ~= "true" and self.Config.Attack.Enabled ~= "true" and self.Config.Hit.Enabled ~= "true" then
		rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.NotEnabled)
		return
	end
	if args.Length == 0 then
		if permission.UserHasPermission(playerSteamID, "radarmanager.admin") then
			rust.SendChatMessage(player,
				self.Config.Settings.Prefix.." <color=#ffd479>/rm toggle <clan | friend | attack | hit></color> - Enable or disable radar system\n"..
				self.Config.Settings.Prefix.." <color=#ffd479>/rm admin <off> | <radius> <refresh> <sleepers (0 | 1)></color> - Toggle global radar"
			)
		end
		rust.SendChatMessage(player,
			self.Config.Settings.Prefix.." <color=#ffd479>/rm limits <system | clan | friend | attack | hit></color> - View radar limits\n"..
			self.Config.Settings.Prefix.." <color=#ffd479>/rm active <clan | friend | attack | hit></color> - Toggle radar system"
		)
		return
		elseif args.Length > 0 then
		local func = args[0]
		if func ~= "limits" and func ~= "active" then
			rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.WrongArgs)
			return
		end
		if func == "limits" then
			if self.Config.Settings.EnableLimits ~= "true" then
				if not permission.UserHasPermission(playerSteamID, "radarmanager.admin") then
					rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.NoPermission)
					return
				end
			end
			if args.Length < 2 then
				rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.WrongArgs)
				return
			end
			sfunc = args[1]
			if sfunc ~= "system" and sfunc ~= "clan" and sfunc ~= "friend" and sfunc ~= "attack" and sfunc ~= "hit" then
				rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.WrongArgs)
				return
			end
			if sfunc == "system" then
				rust.SendChatMessage(player,
					self.Config.Settings.Prefix.." Enforce Hours: <color=#ffd479>"..self.Config.Settings.EnforceHours.."</color>\n"..
					self.Config.Settings.Prefix.." Hours: <color=#ffd479>"..self.Config.Settings.StartHour.." - "..self.Config.Settings.EndHour.."</color>\n"..
					self.Config.Settings.Prefix.." Clan Enabled: <color=#ffd479>"..self.Config.Clan.Enabled.."</color>\n"..
					self.Config.Settings.Prefix.." Friend Enabled: <color=#ffd479>"..self.Config.Friend.Enabled.."</color>\n"..
					self.Config.Settings.Prefix.." Attack Enabled: <color=#ffd479>"..self.Config.Attack.Enabled.."</color>\n"..
					self.Config.Settings.Prefix.." Hit Enabled: <color=#ffd479>"..self.Config.Hit.Enabled.."</color>"
				)
			end
			if sfunc == "clan" then
				local Radius = self.Config.Clan.Radius
				local UseTag = self.Config.Clan.Tag
				local found, CustomRadius, CustomTag = self:CheckCustomPermission("clan", playerSteamID)
				if found then
					Radius = CustomRadius
					UseTag = CustomTag
				end
				local loc = "x"..tostring(player.transform.position.x):match"([^.]*).(.*)".." y"..tostring(player.transform.position.y):match"([^.]*).(.*)".." z"..tostring(player.transform.position.z):match"([^.]*).(.*)"
				local tag = FormatMessage(UseTag, { player = player.displayName, location = loc, range = "0" })
				rust.SendChatMessage(player,
					self.Config.Settings.Prefix.." Clan Tag: <color=#ffd479>"..tag.."</color>\n"..
					self.Config.Settings.Prefix.." Radius: <color=#ffd479>"..Radius.." meters</color>\n"..
					self.Config.Settings.Prefix.." Refresh: <color=#ffd479>"..self.Config.Clan.Refresh.." second(s)</color>"
				)
			end
			if sfunc == "friend" then
				local Radius = self.Config.Friend.Radius
				local UseTag = self.Config.Friend.Tag
				local found, CustomRadius, CustomTag = self:CheckCustomPermission("friend", playerSteamID)
				if found then
					Radius = CustomRadius
					UseTag = CustomTag
				end
				local loc = "x"..tostring(player.transform.position.x):match"([^.]*).(.*)".." y"..tostring(player.transform.position.y):match"([^.]*).(.*)".." z"..tostring(player.transform.position.z):match"([^.]*).(.*)"
				local tag = FormatMessage(UseTag, { player = player.displayName, location = loc, range = "0" })
				rust.SendChatMessage(player,
					self.Config.Settings.Prefix.." Friend Tag: <color=#ffd479>"..tag.."</color>\n"..
					self.Config.Settings.Prefix.." Radius: <color=#ffd479>"..Radius.." meters</color>\n"..
					self.Config.Settings.Prefix.." Refresh: <color=#ffd479>"..self.Config.Friend.Refresh.." second(s)</color>"
				)
			end
			if sfunc == "attack" then
				local Radius = self.Config.Attack.Radius
				local UseTag = self.Config.Attack.Tag
				local found, CustomRadius, CustomTag = self:CheckCustomPermission("attack", playerSteamID)
				if found then
					Radius = CustomRadius
					UseTag = CustomTag
				end
				local loc = "x"..tostring(player.transform.position.x):match"([^.]*).(.*)".." y"..tostring(player.transform.position.y):match"([^.]*).(.*)".." z"..tostring(player.transform.position.z):match"([^.]*).(.*)"
				local tag = FormatMessage(UseTag, { player = player.displayName, location = loc, range = "0" })
				rust.SendChatMessage(player,
					self.Config.Settings.Prefix.." Attack Tag: <color=#ffd479>"..tag.."</color>\n"..
					self.Config.Settings.Prefix.." Show Clan: <color=#ffd479>"..self.Config.Attack.ShowClan.."</color>\n"..
					self.Config.Settings.Prefix.." Show Friend: <color=#ffd479>"..self.Config.Attack.ShowFriend.."</color>\n"..
					self.Config.Settings.Prefix.." Attack Timeout: <color=#ffd479>"..self.Config.Attack.AttackTimeout.." second(s)</color>\n"..
					self.Config.Settings.Prefix.." Radar Timeout: <color=#ffd479>"..self.Config.Attack.TagTimeout.." second(s)</color>\n"..
					self.Config.Settings.Prefix.." Detect Firearm: <color=#ffd479>"..self.Config.Attack.EnabledFirearm.."</color>\n"..
					self.Config.Settings.Prefix.." Detect Rocket: <color=#ffd479>"..self.Config.Attack.EnabledRocket.."</color>\n"..
					self.Config.Settings.Prefix.." Detect Throw: <color=#ffd479>"..self.Config.Attack.EnabledThrow.."</color>\n"..
					self.Config.Settings.Prefix.." Radius: <color=#ffd479>"..Radius.." meters</color>"
				)
			end
			if sfunc == "hit" then
				local Radius = self.Config.Hit.Radius
				local UseTag = self.Config.Hit.Tag
				local found, CustomRadius, CustomTag = self:CheckCustomPermission("hit", playerSteamID)
				if found then
					Radius = CustomRadius
					UseTag = CustomTag
				end
				local loc = "x"..tostring(player.transform.position.x):match"([^.]*).(.*)".." y"..tostring(player.transform.position.y):match"([^.]*).(.*)".." z"..tostring(player.transform.position.z):match"([^.]*).(.*)"
				local tag = FormatMessage(UseTag, { player = player.displayName, location = loc, range = "0" })
				rust.SendChatMessage(player,
					self.Config.Settings.Prefix.." Hit Tag: <color=#ffd479>"..tag.."</color>\n"..
					self.Config.Settings.Prefix.." Show Clan: <color=#ffd479>"..self.Config.Hit.ShowClan.."</color>\n"..
					self.Config.Settings.Prefix.." Show Friend: <color=#ffd479>"..self.Config.Hit.ShowFriend.."</color>\n"..
					self.Config.Settings.Prefix.." Hit Timeout: <color=#ffd479>"..self.Config.Hit.HitTimeout.." second(s)</color>\n"..
					self.Config.Settings.Prefix.." Radar Timeout: <color=#ffd479>"..self.Config.Hit.TagTimeout.." second(s)</color>\n"..
					self.Config.Settings.Prefix.." Radius: <color=#ffd479>"..Radius.." meters</color>\n"..
					self.Config.Settings.Prefix.." Online Only: <color=#ffd479>"..self.Config.Hit.ShowOnlineOnly.."</color>"
				)
			end
			return
		end
		if func == "active" then
			if self.Config.Settings.EnforceHours ~= "false" then
				if not permission.UserHasPermission(playerSteamID, "radarmanager.admin") and not permission.UserHasPermission(playerSteamID, "radarmanager.immune") then
					local CurHour = tostring(RSky.Cycle.Hour):match"([^.]*).(.*)"
					if tonumber(CurHour) < tonumber(self.Config.Settings.StartHour) or tonumber(CurHour) >= tonumber(self.Config.Settings.EndHour) then
						local message = FormatMessage(self.Config.Messages.WrongHour, { shour = self.Config.Settings.StartHour, ehour = self.Config.Settings.EndHour, current = CurHour })
						rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..message)
						return
					end
				end
			end
			if args.Length < 2 then
				rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.WrongArgs)
				return
			end
			sfunc = args[1]
			if sfunc ~= "clan" and sfunc ~= "friend" and sfunc ~= "attack" and sfunc ~= "hit" then
				rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.WrongArgs)
				return
			end
			if not PlayerData[playerSteamID] or PlayerData[playerSteamID] == nil then PlayerData[playerSteamID] = "0:0:0:0" end
			if sfunc == "clan" then
				if self.Config.Settings.UsePermissions == "true" then
					if not permission.UserHasPermission(playerSteamID, "radarmanager.admin") and not permission.UserHasPermission(playerSteamID, "radarmanager.all") and
						not permission.UserHasPermission(playerSteamID, "radarmanager.clan") then
						rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.NoPermission)
						return
					end
				end
				if self.Config.Clan.Enabled ~= "true" then
					local message = FormatMessage(self.Config.Messages.TypeDisabled, { radar = "clan" })
					rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..message)
					return
				end
				if not clans then
					local message = FormatMessage(self.Config.Messages.TypeDisabledPlugin, { radar = "clans", plugin = "Clans" })
					rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..message)
					return
				end
				if self.Config.Settings.ForceClan == "true" and self.Config.Settings.AllowForceChange ~= "true" then
					local message = FormatMessage(self.Config.Messages.Forced, { radar = "clan" })
					rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..message)
					return
				end
				local clan, friend, attack, hit = tostring(PlayerData[playerSteamID]):match("([^:]+):([^:]+):([^:]+):([^:]+)")
				local status = "0"
				if clan == "0" then status = "1" end
				PlayerData[playerSteamID] = status..":"..friend..":"..attack..":"..hit
				if status == "0" then
					local message = FormatMessage(self.Config.Messages.PlayerRadarDisabled, { radar = "clan" })
					rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..message)
					else
					local Radius = self.Config.Clan.Radius
					local found, CustomRadius, CustomTag = self:CheckCustomPermission("clan", playerSteamID)
					if found then Radius = CustomRadius end
					local message = FormatMessage(self.Config.Messages.PlayerRadarEnabled, { radar = "clan", radius = Radius })
					rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..message)
				end
			end
			if sfunc == "friend" then
				if self.Config.Settings.UsePermissions == "true" then
					if not permission.UserHasPermission(playerSteamID, "radarmanager.admin") and not permission.UserHasPermission(playerSteamID, "radarmanager.all") and
						not permission.UserHasPermission(playerSteamID, "radarmanager.friend") then
						rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.NoPermission)
						return
					end
				end
				if self.Config.Friend.Enabled ~= "true" then
					local message = FormatMessage(self.Config.Messages.TypeDisabled, { radar = "friend" })
					rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..message)
					return
				end
				if not friendsAPI then
					local message = FormatMessage(self.Config.Messages.TypeDisabledPlugin, { radar = "friends", plugin = "0friendsAPI" })
					rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..message)
					return
				end
				if self.Config.Settings.ForceFriend == "true" and self.Config.Settings.AllowForceChange ~= "true" then
					local message = FormatMessage(self.Config.Messages.Forced, { radar = "friend" })
					rust.SendChatMessage(players, self.Config.Settings.Prefix.." "..message)
					return
				end
				local clan, friend, attack, hit = tostring(PlayerData[playerSteamID]):match("([^:]+):([^:]+):([^:]+):([^:]+)")
				local status = "0"
				if friend == "0" then status = "1" end
				PlayerData[playerSteamID] = clan..":"..status..":"..attack..":"..hit
				if status == "0" then
					local message = FormatMessage(self.Config.Messages.PlayerRadarDisabled, { radar = "friend" })
					rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..message)
					else
					local Radius = self.Config.Friend.Radius
					local found, CustomRadius, CustomTag = self:CheckCustomPermission("friend", playerSteamID)
					if found then Radius = CustomRadius end
					local message = FormatMessage(self.Config.Messages.PlayerRadarEnabled, { radar = "friend", radius = Radius })
					rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..message)
				end
			end
			if sfunc == "attack" then
				if self.Config.Settings.UsePermissions == "true" then
					if not permission.UserHasPermission(playerSteamID, "radarmanager.admin") and not permission.UserHasPermission(playerSteamID, "radarmanager.all") and
						not permission.UserHasPermission(playerSteamID, "radarmanager.attack") then
						rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.NoPermission)
						return
					end
				end
				if self.Config.Attack.Enabled ~= "true" then
					local message = FormatMessage(self.Config.Messages.TypeDisabled, { radar = "attack" })
					rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..message)
					return
				end
				if self.Config.Settings.ForceAttack == "true" and self.Config.Settings.AllowForceChange ~= "true" then
					local message = FormatMessage(self.Config.Messages.Forced, { radar = "attack" })
					rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..message)
					return
				end
				local clan, friend, attack, hit = tostring(PlayerData[playerSteamID]):match("([^:]+):([^:]+):([^:]+):([^:]+)")
				local status = "0"
				if attack == "0" then status = "1" end
				PlayerData[playerSteamID] = clan..":"..friend..":"..status..":"..hit
				if status == "0" then
					local message = FormatMessage(self.Config.Messages.PlayerRadarDisabled, { radar = "attack" })
					rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..message)
					else
					local Radius = self.Config.Attack.Radius
					local found, CustomRadius, CustomTag = self:CheckCustomPermission("attack", playerSteamID)
					if found then Radius = CustomRadius end
					local message = FormatMessage(self.Config.Messages.PlayerRadarEnabled, { radar = "attack", radius = Radius })
					rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..message)
				end
			end
			if sfunc == "hit" then
				if self.Config.Settings.UsePermissions == "true" then
					if not permission.UserHasPermission(playerSteamID, "radarmanager.admin") and not permission.UserHasPermission(playerSteamID, "radarmanager.all") and
						not permission.UserHasPermission(playerSteamID, "radarmanager.hit") then
						rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.NoPermission)
						return
					end
				end
				if self.Config.Hit.Enabled ~= "true" then
					local message = FormatMessage(self.Config.Messages.TypeDisabled, { radar = "hit" })
					rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..message)
					return
				end
				if self.Config.Settings.ForceHit == "true" and self.Config.Settings.AllowForceChange ~= "true" then
					local message = FormatMessage(self.Config.Messages.Forced, { radar = "hit" })
					rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..message)
					return
				end
				local clan, friend, attack, hit = tostring(PlayerData[playerSteamID]):match("([^:]+):([^:]+):([^:]+):([^:]+)")
				local status = "0"
				if hit == "0" then status = "1" end
				PlayerData[playerSteamID] = clan..":"..friend..":"..attack..":"..status
				if status == "0" then
					local message = FormatMessage(self.Config.Messages.PlayerRadarDisabled, { radar = "hit" })
					rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..message)
					else
					local Radius = self.Config.Hit.Radius
					local found, CustomRadius, CustomTag = self:CheckCustomPermission("hit", playerSteamID)
					if found then Radius = CustomRadius end
					local message = FormatMessage(self.Config.Messages.PlayerRadarEnabled, { radar = "hit", radius = Radius })
					rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..message)
				end
			end
			return
		end
	end
end

function PLUGIN:RefreshAdminRadar(player, radius, refresh, sleepers)
	local players = global.BasePlayer.activePlayerList:GetEnumerator()
	while players:MoveNext() do
		if players.Current ~= player then
			if UnityEngine.Vector3.Distance(players.Current.transform.position, player.transform.position) <= radius then
				local tag = players.Current.displayName
				if self.Config.Admin.ShowLocation == "true" then
					tag = tag.." (x"..tostring(players.Current.transform.position.x):match"([^.]*).(.*)".." y"..tostring(players.Current.transform.position.y):match"([^.]*).(.*)".." z"..tostring(players.Current.transform.position.z):match"([^.]*).(.*)"..")"
				end
				local loc = players.Current.transform.position.x..","..(players.Current.transform.position.y + tonumber(self.Config.Admin.ActiveTagOffset))..","..players.Current.transform.position.z
				local tagloc = "x"..tostring(players.Current.transform.position.x):match"([^.]*).(.*)".." y"..tostring(players.Current.transform.position.y):match"([^.]*).(.*)".." z"..tostring(players.Current.transform.position.z):match"([^.]*).(.*)"
				local tag = FormatMessage(self.Config.Admin.ActiveTag, { player = players.Current.displayName, location = tagloc })
				player:SendConsoleCommand("ddraw.text", refresh, 0, loc, tag)
			end
		end
	end
	if sleepers == 1 then
		local players = global.BasePlayer.sleepingPlayerList:GetEnumerator()
		while players:MoveNext() do
			if players.Current ~= player then
				if UnityEngine.Vector3.Distance(players.Current.transform.position, player.transform.position) <= radius then
					local loc = players.Current.transform.position.x..","..(players.Current.transform.position.y + tonumber(self.Config.Admin.SleepTagOffset))..","..players.Current.transform.position.z
					local tagloc = "x"..tostring(players.Current.transform.position.x):match"([^.]*).(.*)".." y"..tostring(players.Current.transform.position.y):match"([^.]*).(.*)".." z"..tostring(players.Current.transform.position.z):match"([^.]*).(.*)"
					local tag = FormatMessage(self.Config.Admin.SleepTag, { player = players.Current.displayName, location = tagloc })
					player:SendConsoleCommand("ddraw.text", refresh, 0, loc, tag)
				end
			end
		end
	end
end

function PLUGIN:RefreshClanRadar()
	if clans then
		local players = global.BasePlayer.activePlayerList:GetEnumerator()
		while players:MoveNext() do
			local playerSteamID = rust.UserIDFromPlayer(players.Current)
			if PlayerData[playerSteamID] and PlayerData[playerSteamID] ~= nil and string.match(PlayerData[playerSteamID], "1") then
				local clan, friend, attack, hit = tostring(PlayerData[playerSteamID]):match("([^:]+):([^:]+):([^:]+):([^:]+)")
				if clan == "1" then
					local Access = true
					if self.Config.Settings.UsePermissions == "true" then
						if not permission.UserHasPermission(playerSteamID, "radarmanager.admin") and not permission.UserHasPermission(playerSteamID, "radarmanager.all") then
							if not permission.UserHasPermission(playerSteamID, "radarmanager.clan") then Access = false end
						end
					end
					if Access then
						local localPlayer = players.Current
						local localClan = clans:Call("GetClanOf", playerSteamID)
						local Radius = self.Config.Clan.Radius
						local UseTag = self.Config.Clan.Tag
						local found, CustomRadius, CustomTag = self:CheckCustomPermission("clan", playerSteamID)
						if found then
							Radius = CustomRadius
							UseTag = CustomTag
						end
						local _players = global.BasePlayer.activePlayerList:GetEnumerator()
						while _players:MoveNext() do
							if _players.Current ~= localPlayer then
								local _playerSteamID = rust.UserIDFromPlayer(_players.Current)
								if not permission.UserHasPermission(_playerSteamID, "radarmanager.hide") then
									local Range = UnityEngine.Vector3.Distance(_players.Current.transform.position, localPlayer.transform.position)
									if Range <= tonumber(Radius) then
										if localClan and clans:Call("GetClanOf", _playerSteamID) == localClan then
											local loc = _players.Current.transform.position.x..","..(_players.Current.transform.position.y + tonumber(self.Config.Clan.TagOffset))..",".._players.Current.transform.position.z
											local tagloc = "x"..tostring(_players.Current.transform.position.x):match"([^.]*).(.*)".." y"..tostring(_players.Current.transform.position.y):match"([^.]*).(.*)".." z"..tostring(_players.Current.transform.position.z):match"([^.]*).(.*)"
											local tag = FormatMessage(UseTag, { player = _players.Current.displayName, location = tagloc, range = tostring(Range):match"([^.]*).(.*)" })
											localPlayer:SendConsoleCommand("ddraw.text", tonumber(self.Config.Clan.Refresh), 0, loc, tag)
										end
									end
								end
							end
						end
					end
				end
			end
		end
	end
end

function PLUGIN:RefreshFriendRadar()
	if friendsAPI then
		local players = global.BasePlayer.activePlayerList:GetEnumerator()
		while players:MoveNext() do
			local playerSteamID = rust.UserIDFromPlayer(players.Current)
			if PlayerData[playerSteamID] and PlayerData[playerSteamID] ~= nil and string.match(PlayerData[playerSteamID], "1") then
				local clan, friend, attack, hit = tostring(PlayerData[playerSteamID]):match("([^:]+):([^:]+):([^:]+):([^:]+)")
				if friend == "1" then
					local Access = true
					if self.Config.Settings.UsePermissions == "true" then
						if not permission.UserHasPermission(playerSteamID, "radarmanager.admin") and not permission.UserHasPermission(playerSteamID, "radarmanager.all") then
							if not permission.UserHasPermission(playerSteamID, "radarmanager.friend") then Access = false end
						end
					end
					if Access then
						local localPlayer = players.Current
						local Radius = self.Config.Friend.Radius
						local UseTag = self.Config.Friend.Tag
						local found, CustomRadius, CustomTag = self:CheckCustomPermission("friend", playerSteamID)
						if found then
							Radius = CustomRadius
							UseTag = CustomTag
						end
						local _players = global.BasePlayer.activePlayerList:GetEnumerator()
						while _players:MoveNext() do
							if _players.Current ~= localPlayer then
								local _playerSteamID = rust.UserIDFromPlayer(_players.Current)
								if not permission.UserHasPermission(_playerSteamID, "radarmanager.hide") then
									local Range = UnityEngine.Vector3.Distance(_players.Current.transform.position, localPlayer.transform.position)
									if Range <= tonumber(Radius) then
										if friendsAPI:Call("AreFriendsS", playerSteamID, _playerSteamID) then
											local loc = _players.Current.transform.position.x..","..(_players.Current.transform.position.y + tonumber(self.Config.Friend.TagOffset))..",".._players.Current.transform.position.z
											local tagloc = "x"..tostring(_players.Current.transform.position.x):match"([^.]*).(.*)".." y"..tostring(_players.Current.transform.position.y):match"([^.]*).(.*)".." z"..tostring(_players.Current.transform.position.z):match"([^.]*).(.*)"
											local tag = FormatMessage(UseTag, { player = _players.Current.displayName, location = tagloc, range = tostring(Range):match"([^.]*).(.*)" })
											localPlayer:SendConsoleCommand("ddraw.text", tonumber(self.Config.Friend.Refresh), 0, loc, tag)
										end
									end
								end
							end
						end
					end
				end
			end
		end
	end
end

function PLUGIN:OnWeaponFired(baseProjectile, player, modProjectile, projectiles)
	if self.Config.Attack.EnabledFirearm == "true" then self:DrawAttackRadar(player) end
end

function PLUGIN:OnRocketLaunched(player, entity)
	if self.Config.Attack.EnabledRocket == "true" then self:DrawAttackRadar(player) end
end

function PLUGIN:OnExplosiveThrown(player, entity)
	if self.Config.Attack.EnabledThrow == "true" then self:DrawAttackRadar(player) end
end

function PLUGIN:OnMeleeThrown(player, entity)
	if self.Config.Attack.EnabledThrow == "true" then self:DrawAttackRadar(player) end
end

function PLUGIN:DrawAttackRadar(player)
	if self.Config.Attack.Enabled == "true" then
		local _playerSteamID = rust.UserIDFromPlayer(player)
		if not permission.UserHasPermission(_playerSteamID, "radarmanager.hide") then
			local localClan
			if clans and self.Config.Attack.ShowClan == "false" then localClan = clans:Call("GetClanOf", _playerSteamID) end
			local loc = player.transform.position.x..","..(player.transform.position.y + tonumber(self.Config.Attack.TagOffset))..","..player.transform.position.z
			local tagloc = "x"..tostring(player.transform.position.x):match"([^.]*).(.*)".." y"..tostring(player.transform.position.y):match"([^.]*).(.*)".." z"..tostring(player.transform.position.z):match"([^.]*).(.*)"
			local players = global.BasePlayer.activePlayerList:GetEnumerator()
			while players:MoveNext() do
				if players.Current ~= player then
					local playerSteamID = rust.UserIDFromPlayer(players.Current)
					if not PlayerAData[playerSteamID] or PlayerAData[playerSteamID] == nil then PlayerAData[playerSteamID] = "" end
					if not string.match(PlayerAData[playerSteamID], _playerSteamID..":") then
						if PlayerData[playerSteamID] and PlayerData[playerSteamID] ~= nil and string.match(PlayerData[playerSteamID], "1") then
							local clan, friend, attack, hit = tostring(PlayerData[playerSteamID]):match("([^:]+):([^:]+):([^:]+):([^:]+)")
							if attack == "1" then
								local Radius = self.Config.Attack.Radius
								local UseTag = self.Config.Attack.Tag
								local found, CustomRadius, CustomTag = self:CheckCustomPermission("attack", playerSteamID)
								if found then
									Radius = CustomRadius
									UseTag = CustomTag
								end
								local Range = UnityEngine.Vector3.Distance(players.Current.transform.position, player.transform.position)
								if Range <= tonumber(Radius) then
									local Access = true
									if clans and self.Config.Attack.ShowClan == "false" then
										if localClan and clans:Call("GetClanOf", playerSteamID) == localClan then Access = false end
									end
									if Access and friendsAPI and self.Config.Attack.ShowFriend == "false" then
										if friendsAPI:Call("AreFriendsS", playerSteamID, _playerSteamID) then Access = false end
									end
									if Access and self.Config.Settings.UsePermissions == "true" then
										if not permission.UserHasPermission(playerSteamID, "radarmanager.admin") and not permission.UserHasPermission(playerSteamID, "radarmanager.all") and
										not permission.UserHasPermission(playerSteamID, "radarmanager.attack") then Access = false end
									end
									if Access then
										PlayerAData[playerSteamID] = PlayerAData[playerSteamID].._playerSteamID..":"..time.GetUnixTimestamp()..","
										local tag = FormatMessage(UseTag, { player = player.displayName, location = tagloc, range = tostring(Range):match"([^.]*).(.*)" })
										players.Current:SendConsoleCommand("ddraw.text", tonumber(self.Config.Attack.TagTimeout), 0, loc, tag)
									end
								end
							end
						end
					end
				end
			end
		end
	end
end

function PLUGIN:OnPlayerAttack(attacker, info)
	if self.Config.Hit.Enabled == "true" then
		if info.HitEntity then
			if info.HitEntity:ToPlayer() and info.HitEntity ~= attacker then
				local Access = true
				if self.Config.Hit.ShowOnlineOnly == "true" then
					if not info.HitEntity:IsConnected() then Access = false end
				end
				if Access then
					local _playerSteamID = rust.UserIDFromPlayer(info.HitEntity)
					if not permission.UserHasPermission(_playerSteamID, "radarmanager.hide") then
						local playerSteamID = rust.UserIDFromPlayer(attacker)
						if not PlayerHData[playerSteamID] or PlayerHData[playerSteamID] == nil then PlayerHData[playerSteamID] = "" end
						if not string.match(PlayerHData[playerSteamID], _playerSteamID..":") then
							if PlayerData[playerSteamID] and PlayerData[playerSteamID] ~= nil and string.match(PlayerData[playerSteamID], "1") then
								local clan, friend, attack, hit = tostring(PlayerData[playerSteamID]):match("([^:]+):([^:]+):([^:]+):([^:]+)")
								if hit == "1" then
									local Radius = self.Config.Hit.Radius
									local UseTag = self.Config.Hit.Tag
									local found, CustomRadius, CustomTag = self:CheckCustomPermission("hit", playerSteamID)
									if found then
										Radius = CustomRadius
										UseTag = CustomTag
									end
									local Range = UnityEngine.Vector3.Distance(info.HitEntity.transform.position, attacker.transform.position)
									if Range <= tonumber(Radius) then
										local Access = true
										if clans and self.Config.Hit.ShowClan == "false" then
											local localClan = clans:Call("GetClanOf", _playerSteamID)
											if localClan and clans:Call("GetClanOf", playerSteamID) == localClan then Access = false end
										end
										if Access and friendsAPI and self.Config.Hit.ShowFriend == "false" then
											if friendsAPI:Call("AreFriendsS", playerSteamID, _playerSteamID) then Access = false end
										end
										if Access and self.Config.Settings.UsePermissions == "true" then
											if not permission.UserHasPermission(playerSteamID, "radarmanager.admin") and not permission.UserHasPermission(playerSteamID, "radarmanager.all") and
											not permission.UserHasPermission(playerSteamID, "radarmanager.hit") then Access = false end
										end
										if Access then
											PlayerHData[playerSteamID] = PlayerHData[playerSteamID].._playerSteamID..":"..time.GetUnixTimestamp()..","
											local loc = info.HitEntity.transform.position.x..","..(info.HitEntity.transform.position.y + tonumber(self.Config.Hit.TagOffset))..","..info.HitEntity.transform.position.z
											local tagloc = "x"..tostring(info.HitEntity.transform.position.x):match"([^.]*).(.*)".." y"..tostring(info.HitEntity.transform.position.y):match"([^.]*).(.*)".." z"..tostring(info.HitEntity.transform.position.z):match"([^.]*).(.*)"
											local tag = FormatMessage(UseTag, { player = info.HitEntity.displayName, location = tagloc, range = tostring(Range):match"([^.]*).(.*)" })
											attacker:SendConsoleCommand("ddraw.text", tonumber(self.Config.Hit.TagTimeout), 0, loc, tag)
										end
									end
								end
							end
						end
					end
				end
			end
		end
	end
end

function PLUGIN:ProcessPlayerData(playerSteamID)
	local Timestamp = time.GetUnixTimestamp()
	local players = global.BasePlayer.activePlayerList:GetEnumerator()
	while players:MoveNext() do
		local playerSteamID = rust.UserIDFromPlayer(players.Current)
		if PlayerAData[playerSteamID] then
			for word in string.gmatch(PlayerAData[playerSteamID], "([^,]+)") do
			local playerID, Timeout = tostring(word):match("([^:]+):([^:]+)")
			if Timestamp - Timeout >= tonumber(self.Config.Attack.AttackTimeout) then
				PlayerAData[playerSteamID] = tostring(PlayerAData[playerSteamID]):gsub(playerID..":"..Timeout..",", "")
			end
		end
	end
	if PlayerHData[playerSteamID] then
		for word in string.gmatch(PlayerHData[playerSteamID], "([^,]+)") do
			local playerID, Timeout = tostring(word):match("([^:]+):([^:]+)")
			if Timestamp - Timeout >= tonumber(self.Config.Hit.HitTimeout) then
				PlayerHData[playerSteamID] = tostring(PlayerHData[playerSteamID]):gsub(playerID..":"..Timeout..",", "")
			end
		end
	end
	end
end

function PLUGIN:CheckCustomPermission(call, playerSteamID)
	if self.Config.CustomPermissions then
		local Radius
		for current, data in pairs(self.Config.CustomPermissions) do
			if permission.UserHasPermission(playerSteamID, data.Permission) then
				if call == "clan" then Radius = data.ClanRadius end
				if call == "friend" then Radius = data.FriendRadius end
				if call == "attack" then Radius = data.AttackRadius end
				if call == "hit" then Radius = data.HitRadius end
				return true, Radius, data.Tag
			end
		end
	end
	return false
end

function PLUGIN:SendHelpText(player)
	rust.SendChatMessage(player, "<color=#ffd479>/rm</color> - Shows clan members, mutual friends, attackers and hit mark locations to other players")
end