PLUGIN.Title        = "Chat Channels"
PLUGIN.Description  = "Allows players to create, join and manage chat channels.  Also allows players to chat locally."
PLUGIN.Author       = "InSaNe8472"
PLUGIN.Version      = V(1,0,9)
PLUGIN.ResourceID   = 1301

local DataFile = "ChatChannels"
local Data = {}
local PlayerChan = {}
local ChanOwner = {}
local JoinChanPass = {}
local ChanAuth = {}
local GlobalStatus = {}

function PLUGIN:Init()
	permission.RegisterPermission("chan.join", self.Plugin)
	permission.RegisterPermission("chan.create", self.Plugin)
	permission.RegisterPermission("chan.gread", self.Plugin)
	permission.RegisterPermission("chan.gsend", self.Plugin)
	permission.RegisterPermission("chan.admin", self.Plugin)
	command.AddChatCommand("chan", self.Plugin, "cmdChatChannels")
	self:LoadDefaultConfig()
	self:LoadDataFile()
end

function PLUGIN:LoadDefaultConfig()
	self.Config.Settings = self.Config.Settings or {}
	self.Config.Messages = self.Config.Messages or {}
	self.Config.Radius = self.Config.Radius or {}
	self.Config.Settings.Prefix = self.Config.Settings.Prefix or "[<color=#cd422b>Chat Channels</color>]"
	self.Config.Settings.AdminPrefix = self.Config.Settings.AdminPrefix or "[<color=#cd422b>ADMIN</color>]"
	self.Config.Settings.ChanPrefix = self.Config.Settings.ChanPrefix or "<color=white>[</color> <color=#f9169f>{channel}</color> <color=white>]</color>"
	self.Config.Settings.ConsolePrint = self.Config.Settings.ConsolePrint or "[{chat}] {tag} {player}: {message}"
	self.Config.Settings.Enabled = self.Config.Settings.Enabled or "true"
	self.Config.Settings.GlobalAdmin = self.Config.Settings.GlobalAdmin or "false"
	self.Config.Settings.TagsEnabled = self.Config.Settings.TagsEnabled or "true"
	self.Config.Settings.PrintToConsole = self.Config.Settings.PrintToConsole or "true"
	self.Config.Settings.UsePermissions = self.Config.Settings.UsePermissions or "true"
	self.Config.Settings.ShowNewChannels = self.Config.Settings.ShowNewChannels or "true"
	self.Config.Settings.PlayerColor = self.Config.Settings.PlayerColor or "teal"
	self.Config.Settings.MessageColor = self.Config.Settings.MessageColor or "white"
	self.Config.Settings.DefaultGlobalChat = self.Config.Settings.DefaultGlobalChat or "false"
	self.Config.Settings.GlobalChatCommand = self.Config.Settings.GlobalChatCommand or "g"
	self.Config.Messages.NoPermission = self.Config.Messages.NoPermission or "You do not have permission to use this command."
	self.Config.Messages.SystemDisabled = self.Config.Messages.SystemDisabled or "You have been kicked from <color=#cd422b>{channel}</color>.  Administrator has disable Chat Channels system.  You are now in global chat."
	self.Config.Messages.AlreadyStatus = self.Config.Messages.AlreadyStatus or "Chat channels is already <color=#cd422b>{status}</color>."
	self.Config.Messages.ChangedStatus = self.Config.Messages.ChangedStatus or "Chat channels <color=#cd422b>{status}</color>."
	self.Config.Messages.NotEnabled = self.Config.Messages.NotEnabled or "Chat channels is <color=#cd422b>disabled</color>."
	self.Config.Messages.WrongArgs = self.Config.Messages.WrongArgs or "Syntax error.  Use <color=#cd422b>/chan</color> for help."
	self.Config.Messages.NoChannels = self.Config.Messages.NoChannels or "No channels found."
	self.Config.Messages.InvalidPassword = self.Config.Messages.InvalidPassword or "Invalid channel password.  Must be at least five characters long and cannot contain restricted words."
	self.Config.Messages.InvalidChannel = self.Config.Messages.InvalidChannel or "Invalid channel name.  Cannot contain restricted words."
	self.Config.Messages.OwnerExists = self.Config.Messages.OwnerExists or "You may only own one channel at a time."
	self.Config.Messages.ChannelExists = self.Config.Messages.ChannelExists or "Channel <color=#cd422b>{channel}</color> already exists.  Choose another name."
	self.Config.Messages.OfficialChannelCreated = self.Config.Messages.OfficialChannelCreated or "<color=#cd422b>{player}</color> created new official channel <color=#cd422b>{channel}</color>."
	self.Config.Messages.PlayerChannelCreated = self.Config.Messages.PlayerChannelCreated or "<color=#cd422b>{player}</color> created new channel <color=#cd422b>{channel}</color>."
	self.Config.Messages.ChannelCreated = self.Config.Messages.ChannelCreated or "Channel <color=#cd422b>{channel}</color> successfully created.  To join your channel, use <color=#cd422b>/chan join {channel}</color>."
	self.Config.Messages.NewOfficialCreated = self.Config.Messages.NewOfficialCreated or "Official channel <color=#cd422b>{channel}</color> successfully created.  To join your channel, use <color=#cd422b>/chan join {channel}</color>."
	self.Config.Messages.WrongPassword = self.Config.Messages.WrongPassword or "Password provided for channel <color=#cd422b>{channel}</color> is incorrect."
	self.Config.Messages.AlreadyInChannel = self.Config.Messages.AlreadyInChannel or "You are already in channel <color=#cd422b>{channel}</color>.  You must leave before joining another."
	self.Config.Messages.AlreadyInJoinChannel = self.Config.Messages.AlreadyInJoinChannel or "You are already in channel <color=#cd422b>{channel}</color>."
	self.Config.Messages.JoinBanned = self.Config.Messages.JoinBanned or "You are banned from channel <color=#cd422b>{channel}</color>."
	self.Config.Messages.ChannelNotExists = self.Config.Messages.ChannelNotExists or "Channel <color=#cd422b>{channel}</color> does not exist."
	self.Config.Messages.NoChannel = self.Config.Messages.NoChannel or "You are not in a channel."
	self.Config.Messages.PartChannel = self.Config.Messages.PartChannel or "You have left channel <color=#cd422b>{channel}</color>.  You are now in global chat."
	self.Config.Messages.NotOwnerMod = self.Config.Messages.NoAccess or "You have no access to this channel."
	self.Config.Messages.NotOwner = self.Config.Messages.NotOwner or "Only the owner of this channel can use this command."
	self.Config.Messages.OwnerDeleted = self.Config.Messages.OwnerDeleted or "You have been kicked from <color=#cd422b>{channel}</color>.  <color=#cd422b>{owner}</color> has deleted the channel (reason: <color=#cd422b>{reason}</color>).  You are now in global chat."
	self.Config.Messages.ChannelDeleted = self.Config.Messages.ChannelDeleted or "Channel <color=#cd422b>{channel}</color> successfully deleted.  You are now in global chat."
	self.Config.Messages.OfficialRemoved = self.Config.Messages.OfficialRemoved or "Channel is no longer an official channel."
	self.Config.Messages.OfficialCreated = self.Config.Messages.OfficialCreated or "Channel is now an official channel."
	self.Config.Messages.NoPassword = self.Config.Messages.NoPassword or "Channel does not have a password."
	self.Config.Messages.PasswordRemoved = self.Config.Messages.PasswordRemoved or "Password for channel successfully removed."
	self.Config.Messages.MatchPassword = self.Config.Messages.MatchPassword or "Password for channel is already <color=#cd422b>{password}</color>."
	self.Config.Messages.PasswordChanged = self.Config.Messages.PasswordChanged or "Password for channel successfully set to <color=#cd422b>{password}</color>."
	self.Config.Messages.NoBans = self.Config.Messages.NoBans or "No bans found for channel."
	self.Config.Messages.NoMods = self.Config.Messages.NoMods or "No moderators found for channel."
	self.Config.Messages.UnbannedAll = self.Config.Messages.UnbannedAll or "All channel bans successfully removed."
	self.Config.Messages.UnmodAll = self.Config.Messages.UnmodAll or "All channel moderators successfully removed."
	self.Config.Messages.NoPlayer = self.Config.Messages.NoPlayer or "Player not found.  Please try again."
	self.Config.Messages.MultiPlayer = self.Config.Messages.MultiPlayer or "Multiple players found.  Provide a more specific username."
	self.Config.Messages.Self = self.Config.Messages.Self or "You cannot use channel commands on yourself."
	self.Config.Messages.Owner = self.Config.Messages.Owner or "You cannot use channel commands on channel owners."
	self.Config.Messages.Admin = self.Config.Messages.Admin or "You cannot use channel commands on administrators."
	self.Config.Messages.NotInChannel = self.Config.Messages.NotInChannel or "Player <color=#cd422b>{player}</color> is not in channel."
	self.Config.Messages.Kicked = self.Config.Messages.Kicked or "You have been kicked from channel <color=#cd422b>{channel}</color> by <color=#cd422b>{player}</color>.  You are now in global chat."
	self.Config.Messages.AlreadyBanned = self.Config.Messages.AlreadyBanned or "<color=#cd422b>{player}</color> is already banned from channel."
	self.Config.Messages.Banned = self.Config.Messages.Banned or "You have been banned from channel <color=#cd422b>{channel}</color> by <color=#cd422b>{player}</color>.  You are now in global chat."
	self.Config.Messages.NotBanned = self.Config.Messages.NotBanned or "<color=#cd422b>{player}</color> is not banned in channel."
	self.Config.Messages.AlreadyMod = self.Config.Messages.AlreadyMod or "<color=#cd422b>{player}</color> is already a moderator in channel."
	self.Config.Messages.NotMod = self.Config.Messages.NotMod or "<color=#cd422b>{player}</color> is not a moderator in channel."
	self.Config.Messages.ChanJoin = self.Config.Messages.ChanJoin or "<color=#cd422b>{player}</color> has joined the channel."
	self.Config.Messages.ChanPart = self.Config.Messages.ChanPart or "<color=#cd422b>{player}</color> has left the channel."
	self.Config.Messages.ChanKick = self.Config.Messages.ChanKick or "<color=#cd422b>{player}</color> has kicked <color=#cd422b>{target}</color> from the channel."
	self.Config.Messages.ChanBan = self.Config.Messages.ChanBan or "<color=#cd422b>{player}</color> has banned <color=#cd422b>{target}</color> from the channel."
	self.Config.Messages.ChanUnban = self.Config.Messages.ChanUnban or "<color=#cd422b>{player}</color> has unbanned <color=#cd422b>{target}</color> from the channel."
	self.Config.Messages.ChanMod = self.Config.Messages.ChanMod or "<color=#cd422b>{player}</color> has moderated <color=#cd422b>{target}</color> in the channel."
	self.Config.Messages.ChanUnmod = self.Config.Messages.ChanUnmod or "<color=#cd422b>{player}</color> has unmoderated <color=#cd422b>{target}</color> from the channel."
	self.Config.Messages.RadiusGlobalOnly = self.Config.Messages.RadiusGlobalOnly or "Radius chat may only be used while in global chat."
	self.Config.Messages.NoMessage = self.Config.Messages.NoMessage or "You must provide a message."
	self.Config.Messages.GlobalSend = self.Config.Messages.GlobalSend or "Your message, <color=#ffd479>{message}</color>, has been sent to channel <color=#cd422b>{channel}</color>."
	self.Config.Messages.GlobalSendAll = self.Config.Messages.GlobalSendAll or "Your message, <color=#ffd479>{message}</color>, has been sent to <color=#cd422b>{count}</color> channel(s)."
	self.Config.Messages.ForceJoined = self.Config.Messages.ForceJoined or "Administrator has forced you into channel <color=#cd422b>{channel}</color>.  (reason: <color=#ffd479>{reason}</color>)  Use <color=#cd422b>/chan</color> for help."
	self.Config.Messages.AlreadyInForceChannel = self.Config.Messages.AlreadyInForceChannel or "<color=#cd422b>{player}</color> is already in channel <color=#cd422b>{channel}</color>."
	self.Config.Messages.AdminForceJoined = self.Config.Messages.AdminForceJoined or "<color=#cd422b>{player}</color> has been forced into channel <color=#cd422b>{channel}</color>.  (reason: <color=#ffd479>{reason}</color>, channel has password: <color=#ffd479>{password}</color>, player is banned: <color=#ffd479>{banned}</color>)"
	self.Config.Messages.GlobalChatError = self.Config.Messages.GlobalChatError or "Global chat is already <color=#cd422b>{status}</color>."
	self.Config.Messages.GlobalChatOn = self.Config.Messages.GlobalChatOn or "Global chat is <color=#cd422b>enabled</color>.  Use <color=#cd422b>/{command} <message></color> to chat in global chat."
	self.Config.Messages.GlobalChatOff = self.Config.Messages.GlobalChatOff or "Global chat is <color=#cd422b>disabled</color>.  Use <color=#cd422b>/chan globalchat enable</color> to enable global chat."
	self.Config.Messages.AlreadyInGlobal = self.Config.Messages.AlreadyInGlobal or "You are already in global chat.  You may chat normally without commands."
	self.Config.Radius.Enable = self.Config.Radius.Enable or "true"
	self.Config.Radius.Prefix = self.Config.Radius.Prefix or "<color=white>[</color> <color={color}>{radius} ({meters}m)</color> <color=white>]</color>"
	self.Config.Radius.GlobalOnly = self.Config.Radius.GlobalOnly or "false"
	self.Config.Radius.WhisperCommand = self.Config.Radius.WhisperCommand or "w"
	self.Config.Radius.WhisperColor = self.Config.Radius.WhisperColor or "yellow"
	self.Config.Radius.WhisperRadius = self.Config.Radius.WhisperRadius or "50.0"
	self.Config.Radius.YellCommand = self.Config.Radius.YellCommand or "y"
	self.Config.Radius.YellColor = self.Config.Radius.YellColor or "orange"
	self.Config.Radius.YellRadius = self.Config.Radius.YellRadius or "150.0"
	self.Config.Radius.LocalCommand = self.Config.Radius.LocalCommand or "l"
	self.Config.Radius.LocalColor = self.Config.Radius.LocalColor or "red"
	self.Config.Radius.LocalRadius = self.Config.Radius.LocalRadius or "300.0"
	self.Config.Tags = self.Config.Tags or {
		"chan.tag_admin:<color=#cd422b>[ADMIN]</color>:teal:white",
		"chan.tag_mod:<color=orange>[MOD]</color>:teal:white",
		"chan.tag_player:<color=yellow>[PLAYER]</color>:teal:white"
	}
	if not tonumber(self.Config.Radius.WhisperRadius) or tonumber(self.Config.Radius.WhisperRadius) < 1 then self.Config.Radius.WhisperRadius = "50.0" end
	if not tonumber(self.Config.Radius.YellRadius) or tonumber(self.Config.Radius.YellRadius) < 1 then self.Config.Radius.YellRadius = "150.0" end
	if not tonumber(self.Config.Radius.LocalRadius) or tonumber(self.Config.Radius.LocalRadius) < 1 then self.Config.Radius.LocalRadius = "300.0" end
	self:SaveConfig()
	command.AddChatCommand(self.Config.Settings.GlobalChatCommand, self.Plugin, "cmdChatGlobal")
	if self.Config.Radius.Enable == "true" then
		command.AddChatCommand(self.Config.Radius.WhisperCommand, self.Plugin, "cmdWRadiusChat")
		command.AddChatCommand(self.Config.Radius.LocalCommand, self.Plugin, "cmdLRadiusChat")
		command.AddChatCommand(self.Config.Radius.YellCommand, self.Plugin, "cmdYRadiusChat")
	end
	if self.Config.Tags then
		local i = 1
		while self.Config.Tags[i] do
			local TagP = self.Config.Tags[i]:match("([^:]+)")
			permission.RegisterPermission(TagP, self.Plugin)
			i = i + 1
		end
	end
end

function PLUGIN:LoadDataFile()
	local data = datafile.GetDataTable(DataFile)
	Data = data or {}
	if not Data.Channels then
		Data.Channels = {}
		self:SaveDataFile()
	end
	if self.Config.Settings.Enabled == "true" then
		local players = global.BasePlayer.activePlayerList:GetEnumerator()
		while players:MoveNext() do
			local playerSteamID = rust.UserIDFromPlayer(players.Current)
			PlayerChan[playerSteamID] = "global"
		end
	end
end

function PLUGIN:SaveDataFile()
	datafile.SaveDataTable(DataFile)
end

function PLUGIN:Unload()
	datafile.SaveDataTable(DataFile)
end

function PLUGIN:OnPlayerInit(player)
	if self.Config.Settings.Enabled == "true" then
		local playerSteamID = rust.UserIDFromPlayer(player)
		PlayerChan[playerSteamID] = "global"
	end
end

function PLUGIN:OnPlayerChat(arg)
	local player = arg.connection.player
	if self.Config.Settings.Enabled == "false" then
		local message = arg:GetString(0, "text")
		local PlayerTag, UnC, MsgC = self:PlayerTag(player)
		rust.BroadcastChat(PlayerTag.." <color="..UnC..">"..player.displayName.."</color>", "<color="..MsgC..">"..message.."</color>", rust.UserIDFromPlayer(player))
		if self.Config.Settings.PrintToConsole == "true" then self:PrintToConsole("Chat", PlayerTag, player, message) end
		return false
	end
	local playerSteamID = rust.UserIDFromPlayer(player)
	if GlobalStatus[playerSteamID] == nil then GlobalStatus[playerSteamID] = self.Config.Settings.DefaultGlobalChat end
	self:postChat(arg, 0, "chat", 0, 0, false)
	return false
end

local function FindPlayer(NameOrIpOrSteamID, checkSleeper)
	local playerTbl = {}
	local enumPlayerList = global.BasePlayer.activePlayerList:GetEnumerator()
	while enumPlayerList:MoveNext() do
		local currPlayer = enumPlayerList.Current
		local currSteamID = rust.UserIDFromPlayer(currPlayer)
		local currIP = currPlayer.net.connection.ipaddress
		if currPlayer.displayName == NameOrIpOrSteamID or currSteamID == NameOrIpOrSteamID or currIP == NameOrIpOrSteamID then
			table.insert(playerTbl, currPlayer)
			return #playerTbl, playerTbl
		end
		local matched, _ = string.find(currPlayer.displayName:lower(), NameOrIpOrSteamID:lower(), 1, true)
		if matched then
			table.insert(playerTbl, currPlayer)
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

function PLUGIN:TableMessage(args)
	local argsTbl = {}
	local length = args.Length
	for i = 0, length - 1, 1 do
		argsTbl[i + 1] = args[i]
	end
	return argsTbl
end

local function FormatMessage(message, values)
	for key, value in pairs(values) do message = message:gsub("{" .. key .. "}", value) end
	return message
end

function PLUGIN:cmdChatChannels(player, cmd, args)
	local playerSteamID = rust.UserIDFromPlayer(player)
	if args.Length > 0 and args[0] == "enable" then
		if not permission.UserHasPermission(playerSteamID, "chan.admin") then
			rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.NoPermission)
			return
		end
		if self.Config.Settings.Enabled == "true" then
			local message = FormatMessage(self.Config.Messages.AlreadyStatus, { status = args[0].."d" })
			rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..message)
			return
		end
		self.Config.Settings.Enabled = "true"
		self:SaveConfig()
		local message = FormatMessage(self.Config.Messages.ChangedStatus, { status = args[0].."d" })
		rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..message)
		local players = global.BasePlayer.activePlayerList:GetEnumerator()
		while players:MoveNext() do
			local CSplayerSteamID = rust.UserIDFromPlayer(players.Current)
			PlayerChan[CSplayerSteamID] = "global"
		end
		return
	end
	if args.Length > 0 and args[0] == "disable" then
		if not permission.UserHasPermission(playerSteamID, "chan.admin") then
			rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.NoPermission)
			return
		end
		if self.Config.Settings.Enabled == "false" then
			local message = FormatMessage(self.Config.Messages.AlreadyStatus, { status = args[0].."d" })
			rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..message)
			return
		end
		self.Config.Settings.Enabled = "false"
		self:SaveConfig()
		local message = FormatMessage(self.Config.Messages.ChangedStatus, { status = args[0].."d" })
		rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..message)
		local players = global.BasePlayer.activePlayerList:GetEnumerator()
		while players:MoveNext() do
			local CSplayerSteamID = rust.UserIDFromPlayer(players.Current)
			if PlayerChan[CSplayerSteamID] ~= "global" then
				local message = FormatMessage(self.Config.Messages.SystemDisabled, { channel = PlayerChan[CSplayerSteamID] })
				rust.SendChatMessage(players.Current, self.Config.Settings.Prefix.." "..message)
				PlayerChan[CSplayerSteamID] = "global"
			end
		end
		return
	end
	if self.Config.Settings.Enabled == "false" then
		rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.NotEnabled)
		return
	end
	if self.Config.Settings.UsePermissions == "true" then
		if not permission.UserHasPermission(playerSteamID, "chan.admin") and not permission.UserHasPermission(playerSteamID, "chan.join") then
			rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.NoPermission)
			return
		end
	end
	if args.Length == 0 then
		if permission.UserHasPermission(playerSteamID, "chan.admin") then
			rust.SendChatMessage(player,
				self.Config.Settings.Prefix.." <color=#ffd479>/chan <enable | disable></color> - Enable or disable chat channel system\n"..
				self.Config.Settings.Prefix.." <color=#ffd479>/chan force <player> <channel> <reason></color> - Force a player into a channel\n"..
				self.Config.Settings.Prefix.." <color=#ffd479>/chan global <channel | *> <message></color> - Send a message to channel or all channels"
			)
		end
		rust.SendChatMessage(player,
			self.Config.Settings.Prefix.." <color=#ffd479>/chan list</color> - List currently active channels\n"..
			self.Config.Settings.Prefix.." <color=#ffd479>/chan globalchat [enable | disable]</color> - View and chat in global while in a channel (<color=#ffd479>/"..self.Config.Settings.GlobalChatCommand.." <message></color>)\n"..
			self.Config.Settings.Prefix.." <color=#ffd479>/chan create <channel> [password]</color> - Create a channel with optional password\n"..
			self.Config.Settings.Prefix.." <color=#ffd479>/chan delete <reason></color> - Delete your channel (cannot be undone)"
		)
		rust.SendChatMessage(player,
			self.Config.Settings.Prefix.." <color=#ffd479>/chan password <password></color> - Create, change or remove a channel password ('none' to remove)\n"..
			self.Config.Settings.Prefix.." <color=#ffd479>/chan join <channel> [password] | leave</color> - Join or leave a channel\n"..
			self.Config.Settings.Prefix.." <color=#ffd479>/chan kick | ban | unban | mod | unmod <player></color> - Kick, (un)ban or (un)mod a player in your channel\n"..
			self.Config.Settings.Prefix.." <color=#ffd479>/chan unbanall | banlist | unmodall | modlist | info</color> - Unban/unmod all players, view the ban/mod list or channel information\n\n"..
			self.Config.Settings.Prefix.." <color=#ffd479>Note</color>: With the exception of list, create and join, you must be in your channel to use commands."
		)
		return
		elseif args.Length > 0 then
		local func = args[0]
		if func ~= "list" and func ~= "create" and func ~= "delete" and func ~= "password" and func ~= "join" and func ~= "leave" and func ~= "kick" and func ~= "ban" and func ~= "unban" and
			func ~= "unbanall" and func ~= "banlist" and func ~= "unmodall" and func ~= "modlist" and func ~= "mod" and func ~= "unmod" and func ~= "info" and func ~= "global" and func ~= "force" and
			func ~= "globalchat" then
			rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.WrongArgs)
			return
		end
		if func == "globalchat" then
			if GlobalStatus[playerSteamID] == nil then GlobalStatus[playerSteamID] = self.Config.Settings.DefaultGlobalChat end
			if self.Config.Settings.UsePermissions == "true" then
				if not permission.UserHasPermission(playerSteamID, "chan.admin") and not permission.UserHasPermission(playerSteamID, "chan.gread") then
					rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.NoPermission)
					return
				end
			end
			if args.Length < 2 then
				rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.WrongArgs)
				return
			end
			local GlobalChat = args[1]
			if GlobalChat ~= "disable" and GlobalChat ~= "enable" then
				rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.WrongArgs)
				return
			end
			if GlobalChat == "disable" and GlobalStatus[playerSteamID] == "false" then
				local message = FormatMessage(self.Config.Messages.GlobalChatError, { status = GlobalChat.."d" })
				rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..message)
				return
			end
			if GlobalChat == "enable" and GlobalStatus[playerSteamID] == "true" then
				local message = FormatMessage(self.Config.Messages.GlobalChatError, { status = GlobalChat.."d" })
				rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..message)
				return
			end
			if GlobalChat == "disable" then
				GlobalStatus[playerSteamID] = "false"
				rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.GlobalChatOff)
				else
				GlobalStatus[playerSteamID] = "true"
				local message = FormatMessage(self.Config.Messages.GlobalChatOn, { command = self.Config.Settings.GlobalChatCommand })
				rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..message)
			end
			return
		end
		if func == "force" then
			if args.Length < 4 then
				rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.WrongArgs)
				return
			end
			local targetPlayer, targetSteamID = self:chanCmd(player, args[1], "force")
			if targetPlayer == nil then return end
			local JoinChan = args[2]
			if PlayerChan[targetSteamID] == JoinChan then
				local message = FormatMessage(self.Config.Messages.AlreadyInForceChannel, { player = targetPlayer.displayName, channel = JoinChan })
				rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..message)
				return
			end
			for current, data in pairs(Data.Channels) do
				if data.name == JoinChan then
					local ChanPass, ChanBan = "no", "no"
					if data.password ~= "none" and data.password ~= "official" then ChanPass = "yes" end
					if data.bans ~= "" and string.match(data.bans, targetSteamID) then ChanBan = "yes" end
					PlayerChan[targetSteamID] = JoinChan
					ChanOwner[targetSteamID] = data.owner
					self:postChat(0, targetPlayer, "join", JoinChan, 0, false)
					local reason = ""
					local args = self:TableMessage(args)
					local i = 4
					while args[i] do
						reason = reason..args[i].." "
						i = i + 1
					end
					reason = string.sub(reason, 1, -2)
					local message = FormatMessage(self.Config.Messages.ForceJoined, { channel = JoinChan, reason = reason })
					rust.SendChatMessage(targetPlayer, self.Config.Settings.Prefix.." "..message)
					message = FormatMessage(self.Config.Messages.AdminForceJoined, { player = targetPlayer.displayName, channel = JoinChan, reason = reason, password = ChanPass, banned = ChanBan })
					rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..message)
					return
				end
			end
			local message = FormatMessage(self.Config.Messages.ChannelNotExists, { channel = JoinChan })
			rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..message)
			return
		end
		if func == "global" then
			if not permission.UserHasPermission(playerSteamID, "chan.admin") then
				rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.NoPermission)
				return
			end
			if args.Length < 3 then
				rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.WrongArgs)
				return
			end
			if #Data.Channels == nil or #Data.Channels == 0 then
				rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.NoChannels)
				return
			end
			local MsgChan = args[1]
			if MsgChan ~= "*" then
				local ChanExists = false
				for current, data in pairs(Data.Channels) do
					if data.name == MsgChan then
						ChanExists = true
						break
					end
				end
				if not ChanExists then
					local message = FormatMessage(self.Config.Messages.ChannelNotExists, { channel = MsgChan })
					rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..message)
					return
				end
			end
			local gMessage = ""
			local args = self:TableMessage(args)
			local i = 3
			while args[i] do
				gMessage = gMessage..args[i].." "
				i = i + 1
			end
			gMessage = string.sub(gMessage, 1, -2)
			self:postChat(gMessage, player, "admin", MsgChan, 0, false)
			return
		end
		if func == "list" then
			local CreateList = ""
			local ChannelList = ""
			if #Data.Channels == nil or #Data.Channels == 0 then
				rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.NoChannels)
				return
			end
			for current, data in pairs(Data.Channels) do
				CreateList = "<color=orange>"..data.name.."</color>, "
				if data.password ~= "none" then CreateList = "<color=#cd422b>"..data.name.."</color>, " end
				if data.password == "official" then CreateList = "<color=green>"..data.name.."</color>, " end
				ChannelList = ChannelList..CreateList
			end
			rust.SendChatMessage(player,
				self.Config.Settings.Prefix.." Total: <color=#cd422b>"..#Data.Channels.."</color> | Legend: <color=green>Official</color> | <color=orange>Player</color> | <color=#cd422b>Password</color>\n"..
				self.Config.Settings.Prefix.." "..string.sub(ChannelList, 1, -3)
			)
			return
		end
		if func == "create" then
			if self.Config.Settings.UsePermissions == "true" then
				if not permission.UserHasPermission(playerSteamID, "chan.admin") and not permission.UserHasPermission(playerSteamID, "chan.create") then
					rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.NoPermission)
					return
				end
			end
			if args.Length < 2 then
				rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.WrongArgs)
				return
			end
			local ChanPass = "none"
			if args.Length >= 3 then
				ChanPass = args[2]
				if string.len(ChanPass) < 5 then
					rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.InvalidPassword)
					return
				end
				if ChanPass == "official" then
					if not permission.UserHasPermission(playerSteamID, "chan.admin") then
						rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.InvalidPassword)
						return
					end
				end
			end
			local CreateChan = args[1]
			if CreateChan == "global" or CreateChan == "*" then
				rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.InvalidChannel)
				return
			end
			for current, data in pairs(Data.Channels) do
				if not permission.UserHasPermission(playerSteamID, "chan.admin") then
					if data.owner == playerSteamID then
						rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.OwnerExists)
						return
					end
				end
				if data.name == CreateChan then
					local message = FormatMessage(self.Config.Messages.ChannelExists, { channel = CreateChan })
					rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..message)
					return
				end
			end
			local newChannel = {["name"] = CreateChan, ["password"] = ChanPass, ["owner"] = playerSteamID, ["mods"] = "", ["bans"] = ""}
			table.insert(Data.Channels, newChannel)
			self:SaveDataFile()
			if ChanPass == "official" then
				if self.Config.Settings.ShowNewChannels == "true" then
					local message = FormatMessage(self.Config.Messages.OfficialChannelCreated, { player = player.displayName, channel = CreateChan })
					rust.BroadcastChat(self.Config.Settings.Prefix.." "..message)
				end
				local message = FormatMessage(self.Config.Messages.NewOfficialCreated, { channel = CreateChan })
				rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..message)
				else
				if self.Config.Settings.ShowNewChannels == "true" and ChanPass == "none" then
					local message = FormatMessage(self.Config.Messages.PlayerChannelCreated, { player = player.displayName, channel = CreateChan })
					rust.BroadcastChat(self.Config.Settings.Prefix.." "..message)
				end
				local message = FormatMessage(self.Config.Messages.ChannelCreated, { channel = CreateChan })
				rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..message)
			end
			return
		end
		if func == "join" then
			if args.Length < 2 then
				rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.WrongArgs)
				return
			end
			local JoinChan = args[1]
			if PlayerChan[playerSteamID] == JoinChan then
				local message = FormatMessage(self.Config.Messages.AlreadyInJoinChannel, { channel = JoinChan })
				rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..message)
				return
			end
			if PlayerChan[playerSteamID] ~= "global" then
				local message = FormatMessage(self.Config.Messages.AlreadyInChannel, { channel = PlayerChan[playerSteamID] })
				rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..message)
				return
			end
			if args.Length >= 3 then JoinChanPass[playerSteamID] = args[2] end
			for current, data in pairs(Data.Channels) do
				if data.name == JoinChan then
					if permission.UserHasPermission(playerSteamID, "chan.admin") or data.password == "official" or data.owner == playerSteamID then
						PlayerChan[playerSteamID] = JoinChan
						ChanOwner[playerSteamID] = data.owner
						self:postChat(0, player, "join", JoinChan, 0, false)
						return
					end
					if data.password ~= "none" then
						if JoinChanPass[playerSteamID] ~= data.password then
							JoinChanPass[playerSteamID] = {}
							local message = FormatMessage(self.Config.Messages.WrongPassword, { channel = JoinChan })
							rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..message)
							return
						end
					end
					if data.bans ~= "" then
						if string.match(data.bans, playerSteamID) then
							local message = FormatMessage(self.Config.Messages.JoinBanned, { channel = JoinChan })
							rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..message)
							return
						end
					end
					JoinChanPass[playerSteamID] = {}
					PlayerChan[playerSteamID] = JoinChan
					ChanOwner[playerSteamID] = data.owner
					self:postChat(0, player, "join", JoinChan, 0, false)
					return
				end
			end
			local message = FormatMessage(self.Config.Messages.ChannelNotExists, { channel = JoinChan })
			rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..message)
			return
		end
		if PlayerChan[playerSteamID] == "global" then
			rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.NoChannel)
			return
		end
		if func == "leave" then
			local PartChannel = PlayerChan[playerSteamID]
			PlayerChan[playerSteamID] = "global"
			ChanOwner[playerSteamID] = ""
			self:postChat(0, player, "part", PartChannel, 0, false)
			local message = FormatMessage(self.Config.Messages.PartChannel, { channel = PartChannel })
			rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..message)
			return
		end
		if func == "info" then
			local numFound, targetPlayerTbl = FindPlayer(ChanOwner[playerSteamID], true)
			local targetName = ""
			if numFound == 0 then
				targetName = "<unknown>"
				else
				local targetPlayer = targetPlayerTbl[1]
				targetName = targetPlayer.displayName
			end
			local prefix = FormatMessage(self.Config.Settings.ChanPrefix, { channel = PlayerChan[playerSteamID] })
			rust.SendChatMessage(player, prefix.." Channel owner: <color=#cd422b>"..targetName.."</color>")
			local players = global.BasePlayer.activePlayerList:GetEnumerator()
			local ChanList = ""
			local ChanCount = 0
			while players:MoveNext() do
				local targetSteamID = rust.UserIDFromPlayer(players.Current)
				if PlayerChan[targetSteamID] == PlayerChan[playerSteamID] then
					local numFound, targetPlayerTbl = FindPlayer(targetSteamID, true)
					if numFound ~= 0 then
						local targetPlayer = targetPlayerTbl[1]
						local targetName = targetPlayer.displayName
						ChanList = ChanList..targetName..", "
						ChanCount = ChanCount + 1
					end
				end
			end
			rust.SendChatMessage(player, prefix.." [Players: <color=#cd422b>"..ChanCount.."</color>] "..string.sub(ChanList, 1, -3))
			return
		end
		if not permission.UserHasPermission(playerSteamID, "chan.admin") then
			ChanAuth[playerSteamID] = "none"
			for current, data in pairs(Data.Channels) do
				if data.name == PlayerChan[playerSteamID] then
					if string.match(data.mods, playerSteamID) then
						ChanAuth[playerSteamID] = "mod"
					end
				end
			end
			if ChanOwner[playerSteamID] == playerSteamID then
				ChanAuth[playerSteamID] = "owner"
			end
			if ChanAuth[playerSteamID] == "none" then
				local prefix = FormatMessage(self.Config.Settings.ChanPrefix, { channel = PlayerChan[playerSteamID] })
				rust.SendChatMessage(player, prefix.." "..self.Config.Messages.NoAccess)
				return
			end
			else
			ChanAuth[playerSteamID] = "admin"
		end
		local ChanMod = PlayerChan[playerSteamID]
		if func == "delete" then
			if ChanAuth[playerSteamID] ~= "admin" and ChanAuth[playerSteamID] ~= "owner" then
				local prefix = FormatMessage(self.Config.Settings.ChanPrefix, { channel = PlayerChan[playerSteamID] })
				rust.SendChatMessage(player, prefix.." "..self.Config.Messages.NotOwner)
				return
			end
			if args.Length < 2 then
				rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.WrongArgs)
				return
			end
			local DeleteReason = ""
			local args = self:TableMessage(args)
			local i = 2
			while args[i] do
				DeleteReason = DeleteReason..args[i].." "
				i = i + 1
			end
			DeleteReason = string.sub(DeleteReason, 1, -2)
			local players = global.BasePlayer.activePlayerList:GetEnumerator()
			while players:MoveNext() do
				local DelplayerSteamID = rust.UserIDFromPlayer(players.Current)
				if PlayerChan[DelplayerSteamID] == ChanMod then
					PlayerChan[DelplayerSteamID] = "global"
					if playerSteamID ~= DelplayerSteamID then
						local message = FormatMessage(self.Config.Messages.OwnerDeleted, { channel = ChanMod, owner = player.displayName, reason = DeleteReason })
						rust.SendChatMessage(players.Current, self.Config.Settings.Prefix.." "..message)
					end
				end
			end
			for current, data in pairs(Data.Channels) do
				if data.name == ChanMod then
					table.remove(Data.Channels, current)
					self:SaveDataFile()
					local message = FormatMessage(self.Config.Messages.ChannelDeleted, { channel = ChanMod })
					rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..message)
					return
				end
			end
		end
		if func == "password" then
			if args.Length < 2 then
				rust.SendChatMessage(player, self.Config.Settings.ChanPrefix..self.Config.Messages.WrongArgs)
				return
			end
			local ChanPass = args[1]
			local prefix = FormatMessage(self.Config.Settings.ChanPrefix, { channel = PlayerChan[playerSteamID] })
			if ChanPass ~= "none" then
				if string.len(ChanPass) < 5 then
					rust.SendChatMessage(player, prefix.." "..self.Config.Messages.InvalidPassword)
					return
				end
			end
			if ChanPass == "official" then
				if not permission.UserHasPermission(playerSteamID, "chan.admin") then
					rust.SendChatMessage(player, prefix.." "..self.Config.Messages.InvalidPassword)
					return
				end
			end
			for current, data in pairs(Data.Channels) do
				if data.name == ChanMod then
					if ChanPass == "none" then
						if data.password == ChanPass then
							rust.SendChatMessage(player, prefix.." "..self.Config.Messages.NoPassword)
							return
							else
							if data.password == "official" then
								rust.SendChatMessage(player, prefix.." "..self.Config.Messages.OfficialRemoved)
								else
								rust.SendChatMessage(player, prefix.." "..self.Config.Messages.PasswordRemoved)
							end
							data.password = ChanPass
							self:SaveDataFile()
							return
						end
						else
						if data.password == ChanPass then
							local message = FormatMessage(self.Config.Messages.MatchPassword, { password = ChanPass })
							rust.SendChatMessage(player, prefix.." "..message)
							return
						end
						data.password = ChanPass
						self:SaveDataFile()
						if ChanPass == "official" then
							rust.SendChatMessage(player, prefix.." "..self.Config.Messages.OfficialCreated)
							else
							local message = FormatMessage(self.Config.Messages.PasswordChanged, { password = ChanPass })
							rust.SendChatMessage(player, prefix.." "..message)
						end
						return
					end
				end
			end
		end
		if func == "unbanall" then
			for current, data in pairs(Data.Channels) do
				if data.name == ChanMod then
					local prefix = FormatMessage(self.Config.Settings.ChanPrefix, { channel = PlayerChan[playerSteamID] })
					if data.bans == "" then
						rust.SendChatMessage(player, prefix.." "..self.Config.Messages.NoBans)
						return
					end
					data.bans = ""
					self:SaveDataFile()
					rust.SendChatMessage(player, prefix.." "..self.Config.Messages.UnbannedAll)
					return
				end
			end
		end
		if func == "banlist" then
			local BanList = ""
			local prefix = FormatMessage(self.Config.Settings.ChanPrefix, { channel = PlayerChan[playerSteamID] })
			for current, data in pairs(Data.Channels) do
				if data.name == ChanMod then
					if data.bans == "" then
						rust.SendChatMessage(player, prefix.." "..self.Config.Messages.NoBans)
						return
						else
						local i = 1
						for str in data.bans:gmatch("([^:]+)") do
							local numFound, targetPlayerTbl = FindPlayer(str, true)
							if numFound ~= 0 then
								local targetPlayer = targetPlayerTbl[1]
								local targetName = targetPlayer.displayName
								BanList = BanList..targetName..", "
								i = i + 1
							end
						end
					end
				end
			end
			rust.SendChatMessage(player, prefix.." "..string.sub(BanList, 1, -3))
			return
		end
		if func == "unmodall" then
			local prefix = FormatMessage(self.Config.Settings.ChanPrefix, { channel = PlayerChan[playerSteamID] })
			if ChanAuth[playerSteamID] ~= "admin" and ChanAuth[playerSteamID] ~= "owner" then
				rust.SendChatMessage(player, prefix.." "..self.Config.Messages.NotOwner)
				return
			end
			for current, data in pairs(Data.Channels) do
				if data.name == ChanMod then
					if data.mods == "" then
						rust.SendChatMessage(player, prefix.." "..self.Config.Messages.NoMods)
						return
					end
					data.mods = ""
					self:SaveDataFile()
					rust.SendChatMessage(player, prefix.." "..self.Config.Messages.UnmodAll)
					return
				end
			end
		end
		if func == "modlist" then
			local ModList = ""
			local prefix = FormatMessage(self.Config.Settings.ChanPrefix, { channel = PlayerChan[playerSteamID] })
			for current, data in pairs(Data.Channels) do
				if data.name == ChanMod then
					if data.mods == "" then
						rust.SendChatMessage(player, prefix.." "..self.Config.Messages.NoMods)
						return
						else
						local i = 1
						for str in data.mods:gmatch("([^:]+)") do
							local numFound, targetPlayerTbl = FindPlayer(str, true)
							if numFound ~= 0 then
								local targetPlayer = targetPlayerTbl[1]
								local targetName = targetPlayer.displayName
								ModList = ModList..targetName..", "
								i = i + 1
							end
						end
					end
				end
			end
			rust.SendChatMessage(player, prefix.." "..string.sub(ModList, 1, -3))
			return
		end
		if args.Length < 2 then
			rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.WrongArgs)
			return
		end
		if func == "kick" then
			self:chanCmd(player, args[1], "kick")
			return
		end
		if func == "ban" then
			self:chanCmd(player, args[1], "ban")
			return
		end
		if func == "unban" then
			self:chanCmd(player, args[1], "unban")
			return
		end
		if ChanAuth[playerSteamID] ~= "admin" and ChanAuth[playerSteamID] ~= "owner" then
			local prefix = FormatMessage(self.Config.Settings.ChanPrefix, { channel = PlayerChan[playerSteamID] })
			rust.SendChatMessage(player, prefix.." "..self.Config.Messages.NotOwner)
			return
		end
		if func == "mod" then
			self:chanCmd(player, args[1], "mod")
			return
		end
		if func == "unmod" then
			self:chanCmd(player, args[1], "unmod")
			return
		end
	end
end

function PLUGIN:cmdChatGlobal(player, cmd, args)
	local playerSteamID = rust.UserIDFromPlayer(player)
	if self.Config.Settings.UsePermissions == "true" then
		if not permission.UserHasPermission(playerSteamID, "chan.admin") and not permission.UserHasPermission(playerSteamID, "chan.gsend") then
			rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.NoPermission)
			return
		end
	end
	if PlayerChan[playerSteamID] == "global" then
		rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.AlreadyInGlobal)
		return
	end
	if GlobalStatus[playerSteamID] == "false" then
		rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.GlobalChatOff)
		return
	end
	if args.Length < 1 then
		rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.WrongArgs)
		return
	end
	local message = ""
	local args = self:TableMessage(args)
	local i = 1
	while args[i] do
		message = message..args[i].." "
		i = i + 1
	end
	message = string.sub(message, 1, -2)
	self:postChat(message, player, "chat", 0, 0, true)
	return
end

function PLUGIN:chanCmd(player, target, call)
	local playerSteamID, prefix = rust.UserIDFromPlayer(player), ""
	if call == "force" then
		prefix = self.Config.Settings.Prefix
		else
		prefix = FormatMessage(self.Config.Settings.ChanPrefix, { channel = PlayerChan[playerSteamID] })
	end
	local numFound, targetPlayerTbl = FindPlayer(target, true)
	if numFound == 0 then
		rust.SendChatMessage(player, prefix.." "..self.Config.Messages.NoPlayer)
		return
	end
	if numFound > 1 then
		local targetNameString = ""
		for i = 1, numFound do
			targetNameString = targetNameString..targetPlayerTbl[i].displayName..", "
		end
		rust.SendChatMessage(player, prefix.." "..self.Config.Messages.MultiPlayer)
		rust.SendChatMessage(player, targetNameString)
		return
	end
	local targetPlayer = targetPlayerTbl[1]
	local targetName = targetPlayer.displayName
	if player == targetPlayer then
		rust.SendChatMessage(player, prefix.." "..self.Config.Messages.Self)
		return
	end
	local targetSteamID = rust.UserIDFromPlayer(targetPlayer)
	if call == "force" then return targetPlayer, targetSteamID end
	if not permission.UserHasPermission(playerSteamID, "chan.admin") then
		if targetSteamID == ChanOwner[playerSteamID] then
			rust.SendChatMessage(player, prefix.." "..self.Config.Messages.Owner)
			return
		end
		if permission.UserHasPermission(targetSteamID, "chan.admin") then
			rust.SendChatMessage(player, prefix.." "..self.Config.Messages.Admin)
			return
		end
	end
	local ChanMod = PlayerChan[playerSteamID]
	local ChanUser = PlayerChan[targetSteamID]
	if call == "kick" then
		if ChanMod ~= ChanUser then
			local message = FormatMessage(self.Config.Messages.NotInChannel, { player = targetName })
			rust.SendChatMessage(player, prefix.." "..message)
			return
		end
		PlayerChan[targetSteamID] = "global"
		local message = FormatMessage(self.Config.Messages.Kicked, { channel = ChanMod, player = player.displayName })
		rust.SendChatMessage(targetPlayer, self.Config.Settings.Prefix.." "..message)
		self:postChat(0, targetName, "kick", ChanMod, player, false)
		return
	end
	if call == "ban" then
		for current, data in pairs(Data.Channels) do
			if data.name == ChanMod then
				if string.match(data.bans, targetSteamID) then
					local message = FormatMessage(self.Config.Messages.AlreadyBanned, { player = targetName })
					rust.SendChatMessage(player, prefix.." "..message)
					return
				end
				data.bans = data.bans..targetSteamID..":"
				self:SaveDataFile()
				PlayerChan[targetSteamID] = "global"
				local message = FormatMessage(self.Config.Messages.Banned, { channel = ChanMod, player = player.displayName })
				rust.SendChatMessage(targetPlayer, self.Config.Settings.Prefix.." "..message)
				self:postChat(0, targetName, "ban", ChanMod, player, false)
				return
			end
		end
	end
	if call == "unban" then
		for current, data in pairs(Data.Channels) do
			if data.name == ChanMod then
				if not string.match(data.bans, targetSteamID) then
					local message = FormatMessage(self.Config.Messages.NotBanned, { player = targetName })
					rust.SendChatMessage(player, prefix.." "..message)
					return
				end
				data.bans = data.bans:gsub(targetSteamID..":", "")
				self:SaveDataFile()
				self:postChat(0, targetName, "unban", ChanMod, player, false)
				return
			end
		end
	end
	if call == "mod" then
		for current, data in pairs(Data.Channels) do
			if data.name == ChanMod then
				if string.match(data.mods, targetSteamID) then
					local message = FormatMessage(self.Config.Messages.AlreadyMod, { player = targetName })
					rust.SendChatMessage(player, prefix.." "..message)
					return
				end
				data.mods = data.mods..targetSteamID..":"
				self:SaveDataFile()
				self:postChat(0, targetName, "mod", ChanMod, player, false)
				return
			end
		end
	end
	if call == "unmod" then
		for current, data in pairs(Data.Channels) do
			if data.name == ChanMod then
				if not string.match(data.mods, targetSteamID) then
					local message = FormatMessage(self.Config.Messages.NotMod, { player = targetName })
					rust.SendChatMessage(player, prefix.." "..message)
					return
				end
				data.mods = data.mods:gsub(targetSteamID..":", "")
				self:SaveDataFile()
				self:postChat(0, targetName, "unmod", ChanMod, player, false)
				return
			end
		end
	end
end

function PLUGIN:postChat(arg, player, call, chanuser, owner, globalchat)
	if call == "Whisper" or call == "Local" or call == "Yell" then
		local radius = ""
		local color = ""
		if call == "Whisper" then
			radius = tonumber(self.Config.Radius.WhisperRadius)
			color = self.Config.Radius.WhisperColor
		end
		if call == "Local" then
			radius = tonumber(self.Config.Radius.LocalRadius)
			color = self.Config.Radius.LocalColor
		end
		if call == "Yell" then
			radius = tonumber(self.Config.Radius.YellRadius)
			color = self.Config.Radius.YellColor
		end
		local prefix = FormatMessage(self.Config.Radius.Prefix, { color = color, radius = call, meters = radius })
		local PlayerTag, UnC, MsgC = self:PlayerTag(player)
		local players = global.BasePlayer.activePlayerList:GetEnumerator()
		while players:MoveNext() do
			if self.Config.Radius.GlobalOnly == "true" then
				if UnityEngine.Vector3.Distance(players.Current.transform.position, player.transform.position) <= radius then
					local playerSteamID = rust.UserIDFromPlayer(players.Current)
					if PlayerChan[playerSteamID] == "global" then
						rust.SendChatMessage(players.Current, prefix.." "..PlayerTag.." <color="..UnC..">"..player.displayName.."</color>", "<color="..MsgC..">"..chanuser.."</color>", rust.UserIDFromPlayer(player))
					end
				end
				else
				if UnityEngine.Vector3.Distance(players.Current.transform.position, player.transform.position) <= radius then
					rust.SendChatMessage(players.Current, prefix.." "..PlayerTag.." <color="..UnC..">"..player.displayName.."</color>", "<color="..MsgC..">"..chanuser.."</color>", rust.UserIDFromPlayer(player))
				end
			end
		end
		if self.Config.Settings.PrintToConsole == "true" then self:PrintToConsole(call, PlayerTag, player, chanuser) end
		return
	end
	if call == "admin" then
		local PlayerTag, UnC, MsgC = self:PlayerTag(player)
		local players = global.BasePlayer.activePlayerList:GetEnumerator()
		if chanuser == "*" then
			while players:MoveNext() do
				local LPplayerSteamID = rust.UserIDFromPlayer(players.Current)
				playerChanID = PlayerChan[LPplayerSteamID]
				if playerChanID ~= "global" then
					local prefix = FormatMessage(self.Config.Settings.ChanPrefix, { channel = playerChanID })
					rust.SendChatMessage(players.Current, self.Config.Settings.AdminPrefix.." "..prefix.." "..PlayerTag.." <color="..UnC..">"..player.displayName.."</color>", "<color="..MsgC..">"..arg.."</color>", playerSteamID)
				end
			end
			local message = FormatMessage(self.Config.Messages.GlobalSendAll, { message = arg, count = #Data.Channels })
			rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..message)
			else
			local prefix = FormatMessage(self.Config.Settings.ChanPrefix, { channel = chanuser })
			while players:MoveNext() do
				local LPplayerSteamID = rust.UserIDFromPlayer(players.Current)
				playerChanID = PlayerChan[LPplayerSteamID]
				if playerChanID == chanuser then
					rust.SendChatMessage(players.Current, self.Config.Settings.AdminPrefix.." "..prefix.." "..PlayerTag.." <color="..UnC..">"..player.displayName.."</color>", "<color="..MsgC..">"..arg.."</color>", playerSteamID)
				end
			end
			local message = FormatMessage(self.Config.Messages.GlobalSend, { message = arg, channel = chanuser })
			rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..message)
		end
	end
	if call ~= "chat" then
		local players = global.BasePlayer.activePlayerList:GetEnumerator()
		while players:MoveNext() do
			local playerSteamID = rust.UserIDFromPlayer(players.Current)
			local PlayerData = PlayerChan[playerSteamID]
			if PlayerData == chanuser then
				local prefix = FormatMessage(self.Config.Settings.ChanPrefix, { channel = chanuser })
				if call == "join" then
					local message = FormatMessage(self.Config.Messages.ChanJoin, { player = player.displayName })
					rust.SendChatMessage(players.Current, prefix.." "..message)
				end
				if call == "part" then
					local message = FormatMessage(self.Config.Messages.ChanPart, { player = player.displayName })
					rust.SendChatMessage(players.Current, prefix.." "..message)
				end
				if call == "kick" then
					local message = FormatMessage(self.Config.Messages.ChanKick, { player = owner.displayName, target = player })
					rust.SendChatMessage(players.Current, prefix.." "..message)
				end
				if call == "ban" then
					local message = FormatMessage(self.Config.Messages.ChanBan, { player = owner.displayName, target = player })
					rust.SendChatMessage(players.Current, prefix.." "..message)
				end
				if call == "unban" then
					local message = FormatMessage(self.Config.Messages.ChanUnban, { player = owner.displayName, target = player })
					rust.SendChatMessage(players.Current, prefix.." "..message)
				end
				if call == "mod" then
					local message = FormatMessage(self.Config.Messages.ChanMod, { player = owner.displayName, target = player })
					rust.SendChatMessage(players.Current, prefix.." "..message)
				end
				if call == "unmod" then
					local message = FormatMessage(self.Config.Messages.ChanUnmod, { player = owner.displayName, target = player })
					rust.SendChatMessage(players.Current, prefix.." "..message)
				end
			end
		end
		return
		else
		local newplayer, message, playerSteamID, PlayerData = "", "", "", ""
		if not globalchat then
			newplayer = arg.connection.player
			message = arg:GetString(0, "text")
			playerSteamID = rust.UserIDFromPlayer(newplayer)
			PlayerData = PlayerChan[playerSteamID]
			else
			newplayer = player
			message = arg
			playerSteamID = rust.UserIDFromPlayer(newplayer)
			PlayerData = "global"
		end
		local PlayerTag, UnC, MsgC = self:PlayerTag(newplayer)
		local playerChanID = ""
		local prefix = FormatMessage(self.Config.Settings.ChanPrefix, { channel = PlayerData })
		local players = global.BasePlayer.activePlayerList:GetEnumerator()
		while players:MoveNext() do
			local LPplayerSteamID = rust.UserIDFromPlayer(players.Current)
			playerChanID = PlayerChan[LPplayerSteamID]
			local SendGlobal = true
			if permission.UserHasPermission(LPplayerSteamID, "chan.admin") and self.Config.Settings.GlobalAdmin == "true" then SendGlobal = false end
			if SendGlobal then
				if permission.UserHasPermission(LPplayerSteamID, "chan.admin") or permission.UserHasPermission(LPplayerSteamID, "chan.gread") then
					if PlayerData == "global" and GlobalStatus[LPplayerSteamID] == "true" then
						if playerChanID ~= "global" then
							rust.SendChatMessage(players.Current, PlayerTag.." <color="..UnC..">"..newplayer.displayName.."</color>", "<color="..MsgC..">"..message.."</color>", playerSteamID)
						end
					end
				end
			end
			if playerChanID == PlayerData then
				if playerChanID == "global" then
					rust.SendChatMessage(players.Current, PlayerTag.." <color="..UnC..">"..newplayer.displayName.."</color>", "<color="..MsgC..">"..message.."</color>", playerSteamID)
					else
					rust.SendChatMessage(players.Current, prefix.." "..PlayerTag.." <color="..UnC..">"..newplayer.displayName.."</color>", "<color="..MsgC..">"..message.."</color>", playerSteamID)
				end
			end
		end
		if globalchat then
			if permission.UserHasPermission(playerSteamID, "chan.admin") or permission.UserHasPermission(playerSteamID, "chan.gread") then
				rust.SendChatMessage(newplayer, PlayerTag.." <color="..UnC..">"..newplayer.displayName.."</color>", "<color="..MsgC..">"..message.."</color>", playerSteamID)
			end
		end
		if self.Config.Settings.PrintToConsole == "true" then
			if playerChanID == "global" then
				self:PrintToConsole("Global", PlayerTag, newplayer, message)
				else
				if globalchat then
					self:PrintToConsole("To Global from #"..playerChanID, PlayerTag, newplayer, message)
					else
					self:PrintToConsole("#"..playerChanID, PlayerTag, newplayer, message)
				end
			end
		end
		if self.Config.Settings.GlobalAdmin == "true" then
			local players = global.BasePlayer.activePlayerList:GetEnumerator()
			while players:MoveNext() do
				if players.Current ~= newplayer then
					local playerSteamID = rust.UserIDFromPlayer(players.Current)
					if PlayerChan[playerSteamID] ~= PlayerData then
						if permission.UserHasPermission(playerSteamID, "chan.admin") then
							if playerChanID == "global" then
								rust.SendChatMessage(players.Current, PlayerTag.." <color="..UnC..">"..newplayer.displayName.."</color>", "<color="..MsgC..">"..message.."</color>", playerSteamID)
								else
								rust.SendChatMessage(players.Current, self.Config.Settings.AdminPrefix.." "..prefix.." "..PlayerTag.." <color="..UnC..">"..newplayer.displayName.."</color>", "<color="..MsgC..">"..message.."</color>", playerSteamID)
							end
						end
					end
				end
			end
		end
	end
	return
end

function PLUGIN:cmdWRadiusChat(player, cmd, args)
	self:RadiusChat(player, "Whisper", args)
	return
end

function PLUGIN:cmdLRadiusChat(player, cmd, args)
	self:RadiusChat(player, "Local", args)
	return
end

function PLUGIN:cmdYRadiusChat(player, cmd, args)
	self:RadiusChat(player, "Yell", args)
	return
end

function PLUGIN:RadiusChat(player, call, args)
	if self.Config.Radius.GlobalOnly == "true" then
		local playerSteamID = rust.UserIDFromPlayer(player)
		if PlayerChan[playerSteamID] ~= "global" then
			rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.RadiusGlobalOnly)
			return
		end
	end
	if args.Length < 1 then
		rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.NoMessage)
		return
	end
	local args = self:TableMessage(args)
	local message = ""
	local i = 1
	while args[i] do
		message = message..args[i].." "
		i = i + 1
	end
	self:postChat(0, player, call, message, 0, false)
	return
end

function PLUGIN:PlayerTag(player)
	local Tag, PlayerTag, UnC, MsgC = "", "", self.Config.Settings.PlayerColor, self.Config.Settings.MessageColor
	if self.Config.Settings.TagsEnabled == "true" then
		if self.Config.Tags then
			local playerSteamID = rust.UserIDFromPlayer(player)
			local i = 1
			while self.Config.Tags[i] do
				local TagP = self.Config.Tags[i]:match("([^:]+)")
				if permission.UserHasPermission(playerSteamID, TagP) then
					TagP, Tag, UnC, MsgC = self.Config.Tags[i]:match("([^*]+):([^*]+):([^*]+):([^*]+)")
					PlayerTag = Tag
					break
				end
				i = i + 1
			end
		end
	end
	return PlayerTag, UnC, MsgC
end

function PLUGIN:PrintToConsole(chat, tag, player, message)
	local prefix = self.Config.Settings.Prefix:gsub(" ", "")
	local message = prefix.." "..FormatMessage(self.Config.Settings.ConsolePrint, { chat = chat, tag = tag, player = player.displayName, message = message:gsub("%%", "%%%%") })
	message = message:gsub("<color=%p*%w*>", "")
	message = message:gsub("</color>", "")
	print(message)
end

function InChatChannel(id)
	if PlayerChan[id] == "global" or PlayerChan[id] == "" or PlayerChan[id] == nil then return false end
	return true
end

function PLUGIN:SendHelpText(player)
	rust.SendChatMessage(player, "<color=#ffd479>/chan</color> - Allows players to create, join and manage chat channels")
	if self.Config.Radius.Enable == "true" then rust.SendChatMessage(player, "<color=#ffd479>/"..self.Config.Radius.WhisperCommand.."</color> (<color=#cd422b>"..self.Config.Radius.WhisperRadius.."m</color>), <color=#ffd479>/"..self.Config.Radius.YellCommand.."</color> (<color=#cd422b>"..self.Config.Radius.YellRadius.."m</color>), <color=#ffd479>/"..self.Config.Radius.LocalCommand.."</color> (<color=#cd422b>"..self.Config.Radius.LocalRadius.."m</color>) <color=#ffd479><message></color> - Allows players to chat locally") end
end																										