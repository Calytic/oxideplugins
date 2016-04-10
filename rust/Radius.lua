PLUGIN.Title        = "Radius"
PLUGIN.Description  = "Allows players to view who is nearby within a radius.   Also allows players to chat locally."
PLUGIN.Author       = "InSaNe8472"
PLUGIN.Version      = V(1,0,4)
PLUGIN.ResourceID   = 1322

local RepeatTimer = {}
local friendsAPI, clans

function PLUGIN:Init()
	permission.RegisterPermission("radius.use", self.Plugin)
	permission.RegisterPermission("radius.hide", self.Plugin)
	permission.RegisterPermission("radius.repeat", self.Plugin)
	permission.RegisterPermission("radius.admin", self.Plugin)
	command.AddChatCommand("radius", self.Plugin, "cmdRadius")
	command.AddChatCommand("pradius", self.Plugin, "cmdPRadius")
	self:LoadDefaultConfig()
end

function PLUGIN:LoadDefaultConfig()
	self.Config.Settings = self.Config.Settings or {}
	self.Config.Messages = self.Config.Messages or {}
	self.Config.Radius = self.Config.Radius or {}
	self.Config.Settings.Enabled = self.Config.Settings.Enabled or "true"
	self.Config.Settings.UsePermissions = self.Config.Settings.UsePermissions or "true"
	self.Config.Settings.Prefix = self.Config.Settings.Prefix or "[<color=#cd422b> Radius </color>]"
	self.Config.Settings.MaxRadius = self.Config.Settings.MaxRadius or "125"
	self.Config.Settings.RepeatMin = self.Config.Settings.RepeatMin or "60"
	self.Config.Settings.RepeatMax = self.Config.Settings.RepeatMax or "300"
	self.Config.Settings.ColorRadiusNames = self.Config.Settings.ColorRadiusNames or "true"
	self.Config.Settings.HostileColor = self.Config.Settings.HostileColor or "#cd422b"
	self.Config.Settings.FriendlyColor = self.Config.Settings.FriendlyColor or "green"
	self.Config.Settings.UnknownColor = self.Config.Settings.UnknownColor or "yellow"
	self.Config.Messages.NotEnabled = self.Config.Messages.NotEnabled or "Radius system is <color=#cd422b>disabled</color>."
	self.Config.Messages.NoPermission = self.Config.Messages.NoPermission or "You do not have permission to use this command."
	self.Config.Messages.WrongArgs = self.Config.Messages.WrongArgs or "Syntax error.  Use <color=#cd422b>/radius</color> for help."
	self.Config.Messages.MaxRadius = self.Config.Messages.MaxRadius or "Radius must be between <color=#cd422b>1</color> and <color=#cd422b>{radius}</color>."
	self.Config.Messages.FoundRadius = self.Config.Messages.FoundRadius or "Players within <color=#cd422b>{radius}m</color> of your current location (<color=#cd422b>{count}</color>)..."
	self.Config.Messages.NoRadius = self.Config.Messages.NoRadius or "No players found within <color=#cd422b>{radius}m</color> of your current location."
	self.Config.Messages.NoPlayer = self.Config.Messages.NoPlayer or "Player not found.  Please try again."
	self.Config.Messages.MultiPlayer = self.Config.Messages.MultiPlayer or "Multiple players found.  Provide a more specific username."
	self.Config.Messages.Self = self.Config.Messages.Self or "To check your own radius, use <color=#cd422b>/radius <radius></color>."
	self.Config.Messages.PlayerFoundRadius = self.Config.Messages.PlayerFoundRadius or "Players within <color=#cd422b>{radius}m</color> of <color=#cd422b>{player}'s</color> current location (<color=#cd422b>{count}</color>)..."
	self.Config.Messages.PlayerNoRadius = self.Config.Messages.PlayerNoRadius or "No players found within <color=#cd422b>{radius}m</color> of <color=#cd422b>{player}'s</color> current location."
	self.Config.Messages.NoMessage = self.Config.Messages.NoMessage or "You must provide a message."
	self.Config.Messages.RepeatDisabled = self.Config.Messages.RepeatDisabled or "Automatic radius check <color=#cd422b>disabled</color>."
	self.Config.Messages.RepeatError = self.Config.Messages.RepeatError or "Repeat time must be between <color=#cd422b>{minrepeat}</color> and <color=#cd422b>{maxrepeat}</color>."
	self.Config.Messages.NoRepeat = self.Config.Messages.NoRepeat or "Automatic radius check is not enabled."
	self.Config.Messages.RepeatEnabled = self.Config.Messages.RepeatEnabled or "Automatic <color=#cd422b>{radius}m</color> radius check <color=#cd422b>enabled</color> with <color=#cd422b>{delay} second</color> delay."
	self.Config.Radius.Enable = self.Config.Radius.Enable or "true"
	self.Config.Radius.PrintToConsole = self.Config.Radius.PrintToConsole or "true"
	self.Config.Radius.ConsolePrint = self.Config.Radius.ConsolePrint or "[{chat}] {player}: {message}"
	self.Config.Radius.Prefix = self.Config.Radius.Prefix or "<color=white>[</color> <color={color}>{radius} ({meters}m)</color> <color=white>]</color>"
	self.Config.Radius.PlayerColor = self.Config.Radius.PlayerColor or "teal"
	self.Config.Radius.MessageColor = self.Config.Radius.MessageColor or "white"
	self.Config.Radius.WhisperCommand = self.Config.Radius.WhisperCommand or "w"
	self.Config.Radius.WhisperColor = self.Config.Radius.WhisperColor or "yellow"
	self.Config.Radius.WhisperRadius = self.Config.Radius.WhisperRadius or "50"
	self.Config.Radius.YellCommand = self.Config.Radius.YellCommand or "y"
	self.Config.Radius.YellColor = self.Config.Radius.YellColor or "orange"
	self.Config.Radius.YellRadius = self.Config.Radius.YellRadius or "150"
	self.Config.Radius.LocalCommand = self.Config.Radius.LocalCommand or "l"
	self.Config.Radius.LocalColor = self.Config.Radius.LocalColor or "#cd422b"
	self.Config.Radius.LocalRadius = self.Config.Radius.LocalRadius or "300"
	if not tonumber(self.Config.Settings.MaxRadius) or tonumber(self.Config.Settings.MaxRadius) < 1 then self.Config.Settings.MaxRadius = "125" end
	if not tonumber(self.Config.Settings.RepeatMin) or tonumber(self.Config.Settings.RepeatMin) < 1 then self.Config.Settings.RepeatMin = "60" end
	if not tonumber(self.Config.Settings.RepeatMax) or tonumber(self.Config.Settings.RepeatMax) < 1 then self.Config.Settings.RepeatMax = "300" end
	if tonumber(self.Config.Settings.RepeatMin) >= tonumber(self.Config.Settings.RepeatMax) then self.Config.Settings.RepeatMax = self.Config.Settings.RepeatMin + 1 end
	if not tonumber(self.Config.Radius.WhisperRadius) or tonumber(self.Config.Radius.WhisperRadius) < 1 then self.Config.Radius.WhisperRadius = "50.0" end
	if not tonumber(self.Config.Radius.YellRadius) or tonumber(self.Config.Radius.YellRadius) < 1 then self.Config.Radius.YellRadius = "150.0" end
	if not tonumber(self.Config.Radius.LocalRadius) or tonumber(self.Config.Radius.LocalRadius) < 1 then self.Config.Radius.LocalRadius = "300.0" end
	self:SaveConfig()
	if self.Config.Radius.Enable == "true" then
		command.AddChatCommand(self.Config.Radius.WhisperCommand, self.Plugin, "cmdWRadiusChat")
		command.AddChatCommand(self.Config.Radius.LocalCommand, self.Plugin, "cmdLRadiusChat")
		command.AddChatCommand(self.Config.Radius.YellCommand, self.Plugin, "cmdYRadiusChat")
	end
end

function PLUGIN:OnServerInitialized()
	if self.Config.Settings.ColorRadiusNames == "true" then
		friendsAPI = plugins.Find("0friendsAPI") or false
		clans = plugins.Find("Clans") or false
	end
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

function PLUGIN:cmdRadius(player, cmd, args)
	if self.Config.Settings.Enabled == "false" then
		rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.NotEnabled)
		return
	end
	local playerSteamID = rust.UserIDFromPlayer(player)
	if self.Config.Settings.UsePermissions == "true" then
		if not permission.UserHasPermission(playerSteamID, "radius.admin") then
			if not permission.UserHasPermission(playerSteamID, "radius.use") then
				rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.NoPermission)
				return
			end
		end
	end
	if args.Length == 0 then
		if permission.UserHasPermission(playerSteamID, "radius.admin") then
			rust.SendChatMessage(player, self.Config.Settings.Prefix.." <color=#ffd479>/pradius <player> <radius></color> - Find all online players within radius of target player")
		end
		local MaxRadius = "1 - "..self.Config.Settings.MaxRadius
		rust.SendChatMessage(player, self.Config.Settings.Prefix.." <color=#ffd479>/radius <"..MaxRadius.."></color> - Find all online players within radius of your current location")
		if permission.UserHasPermission(playerSteamID, "radius.admin") or permission.UserHasPermission(playerSteamID, "radius.repeat") then
			local RepeatRange = self.Config.Settings.RepeatMin.." - "..self.Config.Settings.RepeatMax
			rust.SendChatMessage(player, self.Config.Settings.Prefix.." <color=#ffd479>/radius repeat <off | "..RepeatRange.."> <radius></color> - Automtically repeat radius command")
		end
		return
	end
	if args.Length < 1 then
		rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.WrongArgs)
		return
	end
	local Radius = args[0]
	if Radius == "repeat" then
		if self.Config.Settings.UsePermissions == "true" then
			if not permission.UserHasPermission(playerSteamID, "radius.admin") then
				if not permission.UserHasPermission(playerSteamID, "radius.repeat") then
					rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.NoPermission)
					return
				end
			end
		end
		local Status = args[1]
		if Status == "off" then
			if RepeatTimer[playerSteamID] then
				RepeatTimer[playerSteamID]:Destroy()
				RepeatTimer[playerSteamID] = nil
				rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.RepeatDisabled)
				return
			end
			rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.NoRepeat)
			return
			else
			if args.Length < 3 then
				rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.WrongArgs)
				return
			end
			local Repeat = tonumber(Status)
			if not tonumber(Repeat) or Repeat < tonumber(self.Config.Settings.RepeatMin) or Repeat > tonumber(self.Config.Settings.RepeatMax) then
				local message = FormatMessage(self.Config.Messages.RepeatError, { minrepeat = self.Config.Settings.RepeatMin, maxrepeat = self.Config.Settings.RepeatMax })
				rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..message)
				return
			end
			self:PreCheck(player, tonumber(Repeat), tonumber(args[2]), "r")
			return
		end
		else
		if not self:PreCheck(player, 0, tonumber(Radius), 0) then return end
		self:CheckRadius(player, tonumber(Radius), 0)
		return
	end
end

function PLUGIN:PreCheck(player, Repeat, Radius, call)
	if not tonumber(Radius) or Radius < 1 then
		local message = FormatMessage(self.Config.Messages.MaxRadius, { radius = self.Config.Settings.MaxRadius })
		rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..message)
		return false
	end
	local playerSteamID = rust.UserIDFromPlayer(player)
	if not permission.UserHasPermission(playerSteamID, "radius.admin") then
		if Radius > tonumber(self.Config.Settings.MaxRadius) then
			local message = FormatMessage(self.Config.Messages.MaxRadius, { radius = self.Config.Settings.MaxRadius })
			rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..message)
			return false
		end
	end
	if call == "r" then
		if RepeatTimer[playerSteamID] then RepeatTimer[playerSteamID]:Destroy() end
		RepeatTimer[playerSteamID] = nil
		RepeatTimer[playerSteamID] = timer.Repeat(Repeat, 0, function() self:CheckRadius(player, tonumber(Radius), 1) end, self.Plugin)
		local message = FormatMessage(self.Config.Messages.RepeatEnabled, { radius = Radius, delay = Repeat })
		rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..message)
		return false
	end
	return true
end

function PLUGIN:CheckRadius(player, Radius, call)
	local playerSteamID = rust.UserIDFromPlayer(player)
	if call == 1 then
		if self.Config.Settings.UsePermissions == "true" then
			if not permission.UserHasPermission(playerSteamID, "radius.admin") then
				if not permission.UserHasPermission(playerSteamID, "radius.repeat") then
					if RepeatTimer[playerSteamID] then RepeatTimer[playerSteamID]:Destroy() end
					RepeatTimer[playerSteamID] = nil
					rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.RepeatDisabled)
					return
				end
			end
		end
	end
	local players = global.BasePlayer.activePlayerList:GetEnumerator()
	local FoundRadius = ""
	local RadiusCount = 0
	local playerClan
	if clans then playerClan = clans:Call("GetClanOf", playerSteamID) end
	while players:MoveNext() do
		if players.Current ~= player then
			if UnityEngine.Vector3.Distance(players.Current.transform.position, player.transform.position) <= Radius then
				local _playerSteamID = rust.UserIDFromPlayer(players.Current)
				if not permission.UserHasPermission(_playerSteamID, "radius.hide") then
					local Near = tostring(UnityEngine.Vector3.Distance(players.Current.transform.position, player.transform.position)):match"([^.]*).(.*)"
					local Color = self.Config.Settings.HostileColor
					if self.Config.Settings.ColorRadiusNames == "true" then
						if friendsAPI then
							if friendsAPI:Call("areFriends", playerSteamID, _playerSteamID) then
								Color = self.Config.Settings.FriendlyColor
								else
								if friendsAPI:Call("HasFriend", playerSteamID, _playerSteamID) or friendsAPI:Call("IsFriendFrom", playerSteamID, rust.UserIDFromPlayer(players.Current)) then
									Color = self.Config.Settings.UnknownColor
								end
							end
						end
						if clans then
							if clans:Call("GetClanOf", _playerSteamID) == playerClan then Color = self.Config.Settings.FriendlyColor end
						end
					end
					FoundRadius = FoundRadius.." <color="..Color..">"..players.Current.displayName.."</color> ("..Near.."m), "
					RadiusCount = RadiusCount + 1
				end
			end
		end
	end
	if FoundRadius == "" then
		local message = FormatMessage(self.Config.Messages.NoRadius, { radius = Radius })
		rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..message)
		else
		local message = FormatMessage(self.Config.Messages.FoundRadius, { radius = Radius, count = RadiusCount })
		rust.SendChatMessage(player,
			self.Config.Settings.Prefix.." "..message.."\n"..
			self.Config.Settings.Prefix.." "..string.sub(FoundRadius, 1, -3)
		)
	end
	return
end

function PLUGIN:cmdPRadius(player, cmd, args)
	local playerSteamID = rust.UserIDFromPlayer(player)
	if not permission.UserHasPermission(playerSteamID, "radius.admin") then
		rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.NoPermission)
		return
	end
	if self.Config.Settings.Enabled == "false" then
		rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.NotEnabled)
		return
	end
	if args.Length == 0 then
		rust.SendChatMessage(player, self.Config.Settings.Prefix.." <color=#ffd479>/pradius <player> <radius></color> - Find all players within radius of target player")
		return
	end
	if args.Length < 2 then
		rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.WrongArgs)
		return
	end
	local numFound, targetPlayerTbl = FindPlayer(args[0], true)
	if numFound == 0 then
		rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.NoPlayer)
		return
	end
	if numFound > 1 then
		local targetNameString = ""
		for i = 1, numFound do
			targetNameString = targetNameString..targetPlayerTbl[i].displayName..", "
		end
		rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.MultiPlayer)
		rust.SendChatMessage(player, targetNameString)
		return
	end
	local targetPlayer = targetPlayerTbl[1]
	local targetName = targetPlayer.displayName
	
	if player == targetPlayer then
		rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.Self)
		return
	end
	local Radius = tonumber(args[1])
	if not tonumber(Radius) or Radius < 1 then
		local message = FormatMessage(self.Config.Messages.MaxRadius, { radius = self.Config.Settings.MaxRadius })
		rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..message)
		return
	end
	local players = global.BasePlayer.activePlayerList:GetEnumerator()
	local FoundRadius = ""
	local RadiusCount = 0
	local playerClan
	if clans then playerClan = clans:Call("GetClanOf", playerSteamID) end
	while players:MoveNext() do
		if players.Current ~= targetPlayer then
			if UnityEngine.Vector3.Distance(players.Current.transform.position, targetPlayer.transform.position) <= Radius then
				local Near = tostring(UnityEngine.Vector3.Distance(players.Current.transform.position, targetPlayer.transform.position)):match"([^.]*).(.*)"
				local Color = self.Config.Settings.HostileColor
				if self.Config.Settings.ColorRadiusNames == "true" then
					if friendsAPI then
						if friendsAPI:Call("areFriends", playerSteamID, rust.UserIDFromPlayer(players.Current)) then
							Color = self.Config.Settings.FriendlyColor
							else
							if friendsAPI:Call("HasFriend", playerSteamID, rust.UserIDFromPlayer(players.Current)) or friendsAPI:Call("IsFriendFrom", playerSteamID, rust.UserIDFromPlayer(players.Current)) then
								Color = self.Config.Settings.UnknownColor
							end
						end
					end
					if clans then
						if clans:Call("GetClanOf", rust.UserIDFromPlayer(players.Current)) == playerClan then Color = self.Config.Settings.FriendlyColor end
					end
				end
				FoundRadius = FoundRadius.." <color="..Color..">"..players.Current.displayName.."</color> ("..Near.."m), "
				RadiusCount = RadiusCount + 1
			end
		end
	end
	if FoundRadius == "" then
		local message = FormatMessage(self.Config.Messages.PlayerNoRadius, { radius = Radius, player = targetName })
		rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..message)
		else
		local message = FormatMessage(self.Config.Messages.PlayerFoundRadius, { radius = Radius, count = RadiusCount, player = targetName })
		rust.SendChatMessage(player,
			self.Config.Settings.Prefix.." "..message.."\n"..
			self.Config.Settings.Prefix.." "..string.sub(FoundRadius, 1, -3)
		)
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
	local players = global.BasePlayer.activePlayerList:GetEnumerator()
	while players:MoveNext() do
		if UnityEngine.Vector3.Distance(players.Current.transform.position, player.transform.position) <= radius then
			rust.SendChatMessage(players.Current, prefix.." <color="..self.Config.Radius.PlayerColor..">"..player.displayName.."</color>", "<color="..self.Config.Radius.MessageColor..">"..message.."</color>", rust.UserIDFromPlayer(player))
		end
	end
	if self.Config.Radius.PrintToConsole == "true" then
		local prefix = self.Config.Settings.Prefix:gsub(" ", "")
		local message = prefix.." "..FormatMessage(self.Config.Radius.ConsolePrint, { chat = call, player = player.displayName, message = message })
		message = message:gsub("<color=%p*%w*>", "")
		message = message:gsub("</color>", "")
		print(message)
	end
	return
end

function PLUGIN:SendHelpText(player)
	rust.SendChatMessage(player, "<color=#ffd479>/radius</color> - Allows players to view who is nearby within a radius")
	if self.Config.Radius.Enable == "true" then rust.SendChatMessage(player, "<color=#ffd479>/"..self.Config.Radius.WhisperCommand.."</color> (<color=#cd422b>"..self.Config.Radius.WhisperRadius.."m</color>), <color=#ffd479>/"..self.Config.Radius.YellCommand.."</color> (<color=#cd422b>"..self.Config.Radius.YellRadius.."m</color>), <color=#ffd479>/"..self.Config.Radius.LocalCommand.."</color> (<color=#cd422b>"..self.Config.Radius.LocalRadius.."m</color>) <color=#ffd479><message></color> - Allows players to chat locally") end
end