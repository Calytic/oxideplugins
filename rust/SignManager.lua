PLUGIN.Title        = "Sign Manager"
PLUGIN.Description  = "Track sign changes and allows administrator access to all signs."
PLUGIN.Author       = "InSaNe8472"
PLUGIN.Version      = V(1,0,2)
PLUGIN.ResourceID   = 1363

local DataFile = "SignManager"
local Data = {}

function PLUGIN:Init()
	command.AddChatCommand("sign", self.Plugin, "cmdSign")
	permission.RegisterPermission("sign.create", self.Plugin)
	permission.RegisterPermission("sign.nobl", self.Plugin)
	permission.RegisterPermission("sign.admin", self.Plugin)
	self:LoadDataFile()
	self:LoadDefaultConfig()
end

function PLUGIN:LoadDefaultConfig()
	self.Config.Settings = self.Config.Settings or {}
	self.Config.SignArtist = self.Config.SignArtist or {}
	self.Config.Messages = self.Config.Messages or {}
	self.Config.Settings.LogToFile = self.Config.Settings.LogToFile or "true"
	self.Config.Settings.AllowSigns = self.Config.Settings.AllowSigns or "true"
	self.Config.Settings.UsePermissions = self.Config.Settings.UsePermissions or "true"
	self.Config.Settings.Prefix = self.Config.Settings.Prefix or "[ <color=#cd422b>Sign Manager</color> ]"
	self.Config.Settings.Timestamp = self.Config.Settings.Timestamp or "MM/dd/yyyy @ h:mm tt"
	self.Config.Settings.Radius = self.Config.Settings.Radius or "2"
	self.Config.Settings.MaxHistory = self.Config.Settings.MaxHistory or "10"
	self.Config.Settings.LogHistory = self.Config.Settings.LogHistory or "true"
	self.Config.Settings.LogAdmin = self.Config.Settings.LogAdmin or "true"
	self.Config.Settings.AdminWarn = self.Config.Settings.AdminWarn or "false"
	self.Config.Settings.PlayerWarn = self.Config.Settings.PlayerWarn or "true"
	self.Config.Settings.AdminOwnClear = self.Config.Settings.AdminOwnClear or "false"
	self.Config.SignArtist.Enabled = self.Config.SignArtist.Enabled or "true"
	self.Config.SignArtist.EnableBlacklist = self.Config.SignArtist.EnableBlacklist or "false"
	self.Config.SignArtist.AdminWarn = self.Config.SignArtist.AdminWarn or "true"
	self.Config.SignArtist.Blacklist = self.Config.SignArtist.Blacklist or "porn,naked,naughty"
	self.Config.SignArtist.DeleteBlacklist = self.Config.SignArtist.DeleteBlacklist or "false"
	self.Config.Messages.NoPermission = self.Config.Messages.NoPermission or "You do not have permission to use this command."
	self.Config.Messages.ChangedStatus = self.Config.Messages.ChangedStatus or "New sign creation <color=#cd422b>{status}</color>."
	self.Config.Messages.NoSignsAllowed = self.Config.Messages.NoSignsAllowed or "You are not allowed to create new signs."
	self.Config.Messages.GlobalLock = self.Config.Messages.GlobalLock or "<color=#ffd479>{count}</color> sign(s) have been <color=#ffd479>{status}</color>."
	self.Config.Messages.GlobalDelete = self.Config.Messages.GlobalDelete or "<color=#ffd479>{count}</color> sign(s) have been deleted."
	self.Config.Messages.WrongArgs = self.Config.Messages.WrongArgs or "Syntax error.  Use <color=#cd422b>/sign</color> for help."
	self.Config.Messages.NoPlayer = self.Config.Messages.NoPlayer or "Player not found.  Please try again."
	self.Config.Messages.MultiPlayer = self.Config.Messages.MultiPlayer or "Multiple players found.  Provide a more specific username."
	self.Config.Messages.Self = self.Config.Messages.Self or "You cannot clear your own sign data."
	self.Config.Messages.NoSigns = self.Config.Messages.NoSigns or "No signs found."
	self.Config.Messages.TooFar = self.Config.Messages.TooFar or "Too far from sign.  You must be within <color=#ffd479>{radius}m</color>."
	self.Config.Messages.NoInfo = self.Config.Messages.NoInfo or "No data found for sign."
	self.Config.Messages.NoSign = self.Config.Messages.NoSign or "No sign found.  You must be looking at a sign."
	self.Config.Messages.NoStats = self.Config.Messages.NoStats or "No database entries found."
	self.Config.Messages.NoHistory = self.Config.Messages.NoHistory or "No history entries found."
	self.Config.Messages.NoEdit = self.Config.Messages.NoEdit or "No edit entries found."
	self.Config.Messages.NoOwner = self.Config.Messages.NoOwner or "No owner entry found."
	self.Config.Messages.HistoryInfo = self.Config.Messages.HistoryInfo or "History: {history}"
	self.Config.Messages.Info = self.Config.Messages.Info or "Last edit: [<color=#ffd479>{timestamp}</color>] <color=#cd422b>{player}</color> (<color=#ffd479>{playerid}</color>)"
	self.Config.Messages.Link = self.Config.Messages.Link or "Image URL: <color=#ffd479>{url}</color>"
	self.Config.Messages.Owner = self.Config.Messages.Owner or "Owner: <color=#cd422b>{player}</color> (<color=#ffd479>{playerid}</color>)"
	self.Config.Messages.NotLocked = self.Config.Messages.NotLocked or "Sign is not locked."
	self.Config.Messages.Unlocked = self.Config.Messages.Unlocked or "Sign has been unlocked."
	self.Config.Messages.NoClearPlayer = self.Config.Messages.NoClearPlayer or "No stored sign data found for <color=#ffd479>{player}</color>."
	self.Config.Messages.ClearPlayer = self.Config.Messages.ClearPlayer or "Data for <color=#ffd479>{player}</color> cleared. (<color=#ffd479>{count}</color> entries)"
	self.Config.Messages.ClearSign = self.Config.Messages.ClearSign or "Data for sign cleared."
	self.Config.Messages.NoData = self.Config.Messages.NoData or "No stored sign data found."
	self.Config.Messages.DataCleared = self.Config.Messages.DataCleared or "Data for <color=#ffd479>{count}</color> sign(s) cleared."
	self.Config.Messages.AdminWarn = self.Config.Messages.AdminWarn or "<color=#cd422b>{player}</color> edited sign. (<color=#ffd479>{location}</color>)"
	self.Config.Messages.AdminWarnURL = self.Config.Messages.AdminWarnURL or "<color=#cd422b>{player}</color> edited sign with sign artist, url <color=#cd422b>{url}</color>. (<color=#ffd479>{location}</color>)"
	self.Config.Messages.AdminWarnBL = self.Config.Messages.AdminWarnBL or "<color=#cd422b>{player}</color> edited sign with possible blacklist url <color=#cd422b>{url}</color>. (<color=#ffd479>{location}</color>)"
	self.Config.Messages.PlayerWarn = self.Config.Messages.PlayerWarn or "Sign updated.  All signs are monitored for inappropriate content."
	self.Config.Messages.DeleteBlacklist = self.Config.Messages.DeleteBlacklist or "Your sign possibly contained inappropriate content and has been deleted.  If you believe this is an error, please contact an administrator."
	self.Config.Messages.EditCountError = self.Config.Messages.EditCountError or "Sign edit count is already zero."
	self.Config.Messages.EditCountReset = self.Config.Messages.EditCountReset or "Sign edit count has been reset."
	if not tonumber(self.Config.Settings.Radius) or tonumber(self.Config.Settings.Radius) < 1 then self.Config.Settings.Radius = "2" end
	if not tonumber(self.Config.Settings.MaxHistory) or tonumber(self.Config.Settings.MaxHistory) < 1 then self.Config.Settings.MaxHistory = "10" end
	self:SaveConfig()
end

function PLUGIN:LoadDataFile()
	local data = datafile.GetDataTable(DataFile)
	Data = data or {}
	if not Data.Edits then
		Data.Edits = 0
		self:SaveDataFile()
	end
	if not Data.Signs then
		Data.Signs = {}
		self:SaveDataFile()
	end
end

function PLUGIN:SaveDataFile()
	datafile.SaveDataTable(DataFile)
end

function PLUGIN:Unload()
	datafile.SaveDataTable(DataFile)
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

local function FormatMessage(message, values)
	for key, value in pairs(values) do message = message:gsub("{" .. key .. "}", value) end
	return message
end

function PLUGIN:OnEntityBuilt(entity, object)
	if string.match(tostring(object), "sign") then
		local playerSteamID = rust.UserIDFromPlayer(entity.ownerPlayer)
		if not permission.UserHasPermission(playerSteamID, "sign.admin") then
			if self.Config.Settings.AllowSigns == "false" then
				local ksign = object:GetComponentInParent(global.Signage._type)
				if self.Config.Settings.UsePermissions == "true" then
					if not permission.UserHasPermission(playerSteamID, "sign.create") then
						ksign:KillMessage()
						rust.SendChatMessage(entity.ownerPlayer, self.Config.Settings.Prefix.." "..self.Config.Messages.NoSignsAllowed)
						return
					end
					else
					ksign:KillMessage()
					rust.SendChatMessage(entity.ownerPlayer, self.Config.Settings.Prefix.." "..self.Config.Messages.NoSignsAllowed)
					return
				end
			end
			self:CreateNewEntry(entity, object, playerSteamID)
			else
			if self.Config.Settings.LogAdmin == "true" then
				self:CreateNewEntry(entity, object, playerSteamID)
			end
		end
	end
end

function PLUGIN:CreateNewEntry(entity, object, playerSteamID)
	if self.Config.Settings.LogToFile == "true" then
		local sign = object:GetComponentInParent(global.Signage._type)
		local location = tostring(sign.transform.position.x):match("([^.]+)").." "..tostring(sign.transform.position.y):match("([^.]+)").." "..tostring(sign.transform.position.z):match("([^.]+)")
		self:LogEvent(entity.ownerPlayer, "[Create] "..location)
	end
	local signas, signid = tostring(object:GetComponentInParent(global.Signage._type)):match"([^%[]*)%[([^%]]*)"
	for current, data in pairs(Data.Signs) do
		if data.signid == signid then
			data.owner, data.timestamp, data.edit, data.history, data.www = entity.ownerPlayer.displayName..":"..playerSteamID, "", "", "", ""
			self:SaveDataFile()
			return
		end
	end
	local newSign = {["signid"] = signid, ["owner"] = entity.ownerPlayer.displayName..":"..playerSteamID, ["timestamp"] = "", ["edit"] = "", ["history"] = "", ["www"] = ""}
	table.insert(Data.Signs, newSign)
	self:SaveDataFile()
	return
end

function PLUGIN:OnSignUpdated(sign, player, text)
	local playerSteamID = rust.UserIDFromPlayer(player)
	if not permission.UserHasPermission(playerSteamID, "sign.admin") then
		self:LogSign(sign, player, "")
		else
		if self.Config.Settings.LogAdmin == "true" then
			self:LogSign(sign, player, "")
		end
	end
end

function PLUGIN:LogSign(sign, player, www)
	if self.Config.Settings.LogToFile == "true" then
		local location = tostring(sign.transform.position.x):match("([^.]+)").." "..tostring(sign.transform.position.y):match("([^.]+)").." "..tostring(sign.transform.position.z):match("([^.]+)")
		if www == "" then
			self:LogEvent(player, "[Edit] "..location)
			else
			self:LogEvent(player, "[SignArtist] "..location.." "..www)
		end
	end
	local playerSteamID = rust.UserIDFromPlayer(player)
	local StopWarn = false
	if www ~= "" then
		if self.Config.SignArtist.Enabled ~= "true" then return end
		if self.Config.SignArtist.EnableBlacklist == "true" then
			local playerSteamID = rust.UserIDFromPlayer(player)
			if not permission.UserHasPermission(playerSteamID, "sign.admin") then
				if self.Config.Settings.UsePermissions == "true" then
					if not permission.UserHasPermission(playerSteamID, "sign.nobl") then
						StopWarn = self:ProcessBlacklist(sign, player, www)
					end
					else
					StopWarn = self:ProcessBlacklist(sign, player, www)
				end
			end
		end
	end
	if StopWarn == "exit" then return end
	if not permission.UserHasPermission(playerSteamID, "sign.admin") then
		if self.Config.Settings.AdminWarn == "true" and not StopWarn then
			local players = global.BasePlayer.activePlayerList:GetEnumerator()
			local location = tostring(sign.transform.position.x):match("([^.]+)").." "..tostring(sign.transform.position.y):match("([^.]+)").." "..tostring(sign.transform.position.z):match("([^.]+)")
			local message = ""
			if www == "" then
				message = FormatMessage(self.Config.Messages.AdminWarn, { player = player.displayName, location = location })
				else
				message = FormatMessage(self.Config.Messages.AdminWarnURL, { player = player.displayName, url = www, location = location })
			end
			while players:MoveNext() do
				local playerSteamID = rust.UserIDFromPlayer(players.Current)
				if permission.UserHasPermission(playerSteamID, "sign.admin") then
					rust.SendChatMessage(players.Current, self.Config.Settings.Prefix.." "..message)
				end
			end
		end
		if self.Config.Settings.PlayerWarn == "true" then
			rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.PlayerWarn)
		end
	end
	Data.Edits = Data.Edits + 1
	local playerSteamID = rust.UserIDFromPlayer(player)
	local timestamp = time.GetCurrentTime():ToLocalTime():ToString(self.Config.Settings.Timestamp)
	local signas, signid = tostring(sign):match"([^%[]*)%[([^%]]*)"
	for current, data in pairs(Data.Signs) do
		if data.signid == signid then
			data.timestamp, data.edit, data.www = timestamp, player.displayName..":"..playerSteamID, www
			if self.Config.Settings.LogHistory == "true" then
				data.history = data.history..player.displayName..","
				local _, count = string.gsub(data.history, ",", "")
				if count > tonumber(self.Config.Settings.MaxHistory) then
					local newhistory, wordcnt = "", 1
					for word in string.gmatch(data.history, "([^,]+)") do
						if wordcnt > 1 then newhistory = newhistory..word.."," end
						wordcnt = wordcnt + 1
					end
					data.history = newhistory
				end
			end
			self:SaveDataFile()
			return
		end
	end
	local newSign = {["signid"] = signid, ["owner"] = player.displayName..":"..playerSteamID, ["timestamp"] = timestamp, ["edit"] = player.displayName..":"..playerSteamID, ["history"] = player.displayName..",", ["www"] = www}
	table.insert(Data.Signs, newSign)
	self:SaveDataFile()
end

function PLUGIN:ProcessBlacklist(sign, player, www)
	local StopWarn = false
	if self.Config.SignArtist.AdminWarn == "true" or self.Config.SignArtist.DeleteBlacklist == "true" then
		for word in string.gmatch(self.Config.SignArtist.Blacklist, "([^,]+)") do
			if string.match(www, word) then
				if self.Config.SignArtist.AdminWarn == "true" then
					StopWarn = true
					local players = global.BasePlayer.activePlayerList:GetEnumerator()
					local location = tostring(sign.transform.position.x):match("([^.]+)").." "..tostring(sign.transform.position.y):match("([^.]+)").." "..tostring(sign.transform.position.z):match("([^.]+)")
					local message = FormatMessage(self.Config.Messages.AdminWarnBL, { player = player.displayName, url = www, location = location })
					while players:MoveNext() do
						local playerSteamID = rust.UserIDFromPlayer(players.Current)
						if permission.UserHasPermission(playerSteamID, "sign.admin") then
							rust.SendChatMessage(players.Current, self.Config.Settings.Prefix.." "..message)
						end
					end
				end
				if self.Config.SignArtist.DeleteBlacklist == "true" then
					if sign:GetComponentInParent(global.Signage._type) then
						sign:KillMessage()
						rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.DeleteBlacklist)
						return "exit"
					end
				end
				return StopWarn
			end
		end
	end
end

function PLUGIN:cmdSign(player, cmd, args)
	local playerSteamID = rust.UserIDFromPlayer(player)
	if not permission.UserHasPermission(playerSteamID, "sign.admin") then
		rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.NoPermission)
		return
	end
	if args.Length == 0 then
		rust.SendChatMessage(player,
			self.Config.Settings.Prefix.." <color=#ffd479>/sign toggle</color> - Enable or disable creation of new signs\n"..
			self.Config.Settings.Prefix.." <color=#ffd479>/sign stats</color> - View database statistics\n"..
			self.Config.Settings.Prefix.." <color=#ffd479>/sign lockall <true | false></color> - Lock or unlock all signs\n"..
			self.Config.Settings.Prefix.." <color=#ffd479>/sign deleteall</color> - Delete all signs (cannot be undone)"
		)
		rust.SendChatMessage(player,
			self.Config.Settings.Prefix.." <color=#ffd479>/sign info</color> - View data for sign\n"..
			self.Config.Settings.Prefix.." <color=#ffd479>/sign unlock</color> - Unlock sign for editing\n"..
			self.Config.Settings.Prefix.." <color=#ffd479>/sign clear <sign | edits | player | all> [player]</color> - Clear data for sign, player or edit count (cannot be undone)"
		)
		return
		elseif args.Length > 0 then
		local func = args[0]
		if func ~= "toggle" and func ~= "stats" and func ~= "lockall" and func ~= "deleteall" and func ~= "info" and func ~= "unlock" and func ~= "clear" then
			rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.WrongArgs)
			return
		end
		if func == "toggle" then
			local message = ""
			if self.Config.Settings.AllowSigns == "true" then
				self.Config.Settings.AllowSigns = "false"
				message = FormatMessage(self.Config.Messages.ChangedStatus, { status = "disabled" })
				else
				self.Config.Settings.AllowSigns = "true"
				message = FormatMessage(self.Config.Messages.ChangedStatus, { status = "enabled" })
			end
			self:SaveConfig()
			rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..message)
			return
		end
		if func == "stats" then
			local SignList = UnityEngine.Object.FindObjectsOfTypeAll(global.Signage._type)
			SignList = SignList:GetEnumerator()
			local TotalSigns = 0
			while SignList:MoveNext() do
				if SignList.Current:GetComponentInParent(global.Signage._type) then TotalSigns = TotalSigns + 1 end
			end
			local SignArtist = "Not enabled"
			if self.Config.SignArtist.Enabled == "true" then
				SignArtist = 0
				for current, data in pairs(Data.Signs) do
					if data.www ~= "" then SignArtist = SignArtist + 1 end
				end
			end
			rust.SendChatMessage(player,
				self.Config.Settings.Prefix.." <color=#ffd479>Database entries:</color> "..#Data.Signs.."\n"..
				self.Config.Settings.Prefix.." <color=#ffd479>Signs created:</color> "..TotalSigns.."\n"..
				self.Config.Settings.Prefix.." <color=#ffd479>Sign edits:</color> "..Data.Edits.."\n"..
				self.Config.Settings.Prefix.." <color=#ffd479>SignArtist signs:</color> "..SignArtist
			)
			return
		end
		if func == "lockall" then
			if args.Length < 2 then
				rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.WrongArgs)
				return
			end
			local sfunc = args[1]
			if sfunc ~= "true" and sfunc ~= "false" then
				rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.WrongArgs)
				return
			end
			local SignList = UnityEngine.Object.FindObjectsOfTypeAll(global.Signage._type)
			SignList = SignList:GetEnumerator()
			local lfunc, count = false, 0
			if sfunc == "true" then lfunc = true end
			while SignList:MoveNext() do
				if SignList.Current:GetComponentInParent(global.Signage._type) then
					SignList.Current:SetFlag(global.BaseEntity.Flags.Locked, lfunc)
					count = count + 1
				end
			end
			if count > 0 then
				local message = ""
				if sfunc == "true" then
					message = FormatMessage(self.Config.Messages.GlobalLock, { count = count, status = "locked" })
					else
					message = FormatMessage(self.Config.Messages.GlobalLock, { count = count, status = "unlocked" })
				end
				rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..message)
				else
				rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.NoSigns)
			end
			return
		end
		if func == "deleteall" then
			local SignList = UnityEngine.Object.FindObjectsOfTypeAll(global.Signage._type)
			SignList = SignList:GetEnumerator()
			local count = 0
			while SignList:MoveNext() do
				if SignList.Current:GetComponentInParent(global.Signage._type) then
					SignList.Current:KillMessage()
					count = count + 1
				end
			end
			if count > 0 then
				local message = FormatMessage(self.Config.Messages.GlobalDelete, { count = count })
				rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..message)
				else
				rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.NoSigns)
			end
			Data.Edits = 0
			Data.Signs = {}
			self:SaveDataFile()
			return
		end
		if func == "info" then
			local found, sign, signid = self:FindSign(player)
			if not found then return end
			for current, data in pairs(Data.Signs) do
				if data.signid == signid then
					if self.Config.Settings.LogHistory == "true" then
						if data.timestamp ~= "" then
							local showhistory, wordcnt = "", 1
							for word in string.gmatch(data.history, '([^,]+)') do
								if wordcnt % 2 == 0 then
									showhistory = showhistory..wordcnt.." - <color=#ffd479>"..word.."</color>, "
									else
									showhistory = showhistory..wordcnt.." - <color=#cd422b>"..word.."</color>, "
								end
								wordcnt = wordcnt + 1
							end
							local message = FormatMessage(self.Config.Messages.HistoryInfo, { history = string.sub(showhistory, 1, -3) })
							rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..message)
							else
							rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.NoHistory)
						end
					end
					if data.edit ~= "" then
						local edit, editid = tostring(data.edit):match"([^:]*):([^:]*)"
						local message = FormatMessage(self.Config.Messages.Info, { player = edit, playerid = editid, timestamp = data.timestamp })
						rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..message)
						else
						rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.NoEdit)
					end
					if data.www ~= "" then
						local message = FormatMessage(self.Config.Messages.Link, { url = data.www })
						rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..message)
					end
					if data.owner then
						local owner, ownerid = tostring(data.owner):match"([^:]*):([^:]*)"
						message = FormatMessage(self.Config.Messages.Owner, { player = owner, playerid = ownerid })
						rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..message)
						else
						rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.NoOwner)
					end
					return
				end
			end
			rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.NoInfo)
			return
		end
		if func == "unlock" then
			local found, sign, signid = self:FindSign(player)
			if not found then return end
			if not sign:IsLocked() then
				rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.NotLocked)
				return
			end
			sign:SetFlag(global.BaseEntity.Flags.Locked, false)
			rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.Unlocked)
			return
		end
		if func == "clear" then
			if args.Length < 2 then
				rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.WrongArgs)
				return
			end
			local sfunc = args[1]
			if sfunc ~= "sign" and sfunc ~= "edits" and sfunc ~= "player" and sfunc ~= "all" then
				rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.WrongArgs)
				return
			end
			if sfunc == "sign" then
				local found, sign, signid = self:FindSign(player)
				if not found then return end
				for current, data in pairs(Data.Signs) do
					if data.signid == signid then
						table.remove(Data.Signs, current)
						self:SaveDataFile()
						rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.ClearSign)
						return
					end
				end
				rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.NoInfo)
				return
			end
			if sfunc == "edits" then
				if Data.Edits < 1 then
					rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.EditCountError)
					return
				end
				Data.Edits = 0
				self:SaveDataFile()
				rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.EditCountReset)
				return
			end
			if sfunc == "player" then
				if args.Length < 3 then
					rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.WrongArgs)
					return
				end
				local found, playerName, playerID = self:FindPlayerCheck(player, args[2])
				if not found then return end
				local count = 0
				for current = #Data.Signs, 1, -1 do
					local edit, editid = tostring(Data.Signs[current].edit):match"([^:]*):([^:]*)"
					if edit == tostring(playerName) or editid == tostring(playerID) then
						table.remove(Data.Signs, current)
						count = count + 1
						else
						if string.match(Data.Signs[current].history, tostring(playerName)..",") then
							local newhistory = ""
							for word in string.gmatch(Data.Signs[current].history, "([^,]+)") do
								if word ~= tostring(playerName) then newhistory = newhistory..word.."," end
							end
							Data.Signs[current].history = newhistory
							count = count + 1
						end
					end
				end
				if count > 0 then
					self:SaveDataFile()
					local message = FormatMessage(self.Config.Messages.ClearPlayer, { player = playerName, count = count })
					rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..message)
					else
					local message = FormatMessage(self.Config.Messages.NoClearPlayer, { player = playerName })
					rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..message)
				end
				return
			end
			if sfunc == "all" then
				if #Data.Signs < 1 then
					rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.NoData)
					return
				end
				local message = FormatMessage(self.Config.Messages.DataCleared, { count = #Data.Signs })
				Data.Signs = {}
				self:SaveDataFile()
				rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..message)
				return
			end
		end
	end
end

function PLUGIN:FindSign(player)
	local sign = false
	local arg = util.TableToArray({player.eyes:HeadRay()})
	local hits = UnityEngine.Physics.RaycastAll["methodarray"][2]:Invoke(nil,arg)
	local it = hits:GetEnumerator()
	while it:MoveNext() do
		if it.Current.collider:GetComponentInParent(global.Signage._type) then
			if tonumber(tostring(it.Current.distance):match("([^.]+)")) > tonumber(self.Config.Settings.Radius) then
				local message = FormatMessage(self.Config.Messages.TooFar, { radius = self.Config.Settings.Radius })
				rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..message)
				return false
			end
			sign = it.Current.collider:GetComponentInParent(global.Signage._type)
			break
		end
	end
	if not sign then
		rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.NoSign)
		return false
	end
	local signas, signid = tostring(sign):match"([^%[]*)%[([^%]]*)"
	return true, sign, signid
end

function PLUGIN:FindPlayerCheck(player, target)
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
	if self.Config.Settings.AdminOwnClear ~= "true" then
		if player == targetPlayer then
			rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.Self)
			return false
		end
	end
	return true, targetName, targetSteamID
end

function PLUGIN:LogEvent(player, logdata)
	ConVar.Server.Log("oxide/logs/SignManager.txt", player.displayName.." ("..rust.UserIDFromPlayer(player).."): "..logdata)
end

function PLUGIN:SendHelpText(player)
	local playerSteamID = rust.UserIDFromPlayer(player)
	if permission.UserHasPermission(playerSteamID, "sign.admin") then
		rust.SendChatMessage(player, "<color=#ffd479>/sign</color> - Track sign changes and allows administrator access to all signs.")
	end
end																		