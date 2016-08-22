PLUGIN.Title        = "Sign Manager"
PLUGIN.Description  = "Track sign changes and allows administrator access to all signs."
PLUGIN.Author       = "InSaNe8472"
PLUGIN.Version      = V(1,0,3)
PLUGIN.ResourceID   = 1363

local DataFile = "SignManager"
local Data = {}

function PLUGIN:Init()
	command.AddChatCommand("sign", self.Plugin, "cmdSign")
	permission.RegisterPermission("signmanager.create", self.Plugin)
	permission.RegisterPermission("signmanager.nobl", self.Plugin)
	permission.RegisterPermission("signmanager.admin", self.Plugin)
	self:LoadDataFile()
	self:LoadDefaultConfig()
	self:LoadDefaultLang()
end

function PLUGIN:LoadDefaultConfig()
	self.Config.Settings = self.Config.Settings or {}
	self.Config.SignArtist = self.Config.SignArtist or {}
	self.Config.Settings.LogToFile = self.Config.Settings.LogToFile or "true"
	self.Config.Settings.AllowSigns = self.Config.Settings.AllowSigns or "true"
	self.Config.Settings.UsePermissions = self.Config.Settings.UsePermissions or "true"
	self.Config.Settings.Timestamp = self.Config.Settings.Timestamp or "MM/dd/yyyy @ h:mm tt"
	self.Config.Settings.Radius = self.Config.Settings.Radius or "2"
	self.Config.Settings.MaxHistory = self.Config.Settings.MaxHistory or "10"
	self.Config.Settings.LogHistory = self.Config.Settings.LogHistory or "true"
	self.Config.Settings.LogAdmin = self.Config.Settings.LogAdmin or "true"
	self.Config.Settings.AdminWarn = self.Config.Settings.AdminWarn or "false"
	self.Config.Settings.PlayerWarn = self.Config.Settings.PlayerWarn or "true"
	self.Config.Settings.AdminOwnClear = self.Config.Settings.AdminOwnClear or "false"
	self.Config.Settings.MessageSize = self.Config.Settings.MessageSize or "13"
	self.Config.SignArtist.Enabled = self.Config.SignArtist.Enabled or "true"
	self.Config.SignArtist.EnableBlacklist = self.Config.SignArtist.EnableBlacklist or "false"
	self.Config.SignArtist.AdminWarn = self.Config.SignArtist.AdminWarn or "true"
	self.Config.SignArtist.Blacklist = self.Config.SignArtist.Blacklist or "porn,naked,naughty"
	self.Config.SignArtist.DeleteBlacklist = self.Config.SignArtist.DeleteBlacklist or "false"
	if not tonumber(self.Config.Settings.Radius) or tonumber(self.Config.Settings.Radius) < 1 then self.Config.Settings.Radius = "2" end
	if not tonumber(self.Config.Settings.MaxHistory) or tonumber(self.Config.Settings.MaxHistory) < 1 then self.Config.Settings.MaxHistory = "10" end
	if not tonumber(self.Config.Settings.MessageSize) or tonumber(self.Config.Settings.MessageSize) < 1 then self.Config.Settings.MessageSize = "13" end
	self:SaveConfig()
end

function PLUGIN:LoadDefaultLang()
	lang.RegisterMessages(util.TableToLangDict({
		["Prefix"] = "[ <color=#cd422b>Sign Manager</color> ] ",
		["NoPermission"] = "You do not have permission to use this command.",
		["ChangedStatus"] = "New sign creation <color=#cd422b>{status}</color>.",
		["NoSignsAllowed"] = "You are not allowed to create new signs.",
		["GlobalLock"] = "<color=#ffd479>{count}</color> sign(s) have been <color=#ffd479>{status}</color>.",
		["GlobalDelete"] = "<color=#ffd479>{count}</color> sign(s) have been deleted.",
		["WrongArgs"] = "Syntax error.  Use <color=#cd422b>/sign</color> for help.",
		["NoPlayer"] = "Player not found or multiple players found.  Provide a more specific username.",
		["LangError"] = "Language Error: ",
		["Self"] = "You cannot clear your own sign data.",
		["NoSigns"] = "No signs found.",
		["TooFar"] = "Too far from sign.  You must be within <color=#ffd479>{radius}m</color>.",
		["NoInfo"] = "No data found for sign.",
		["NoSign"] = "No sign found.  You must be looking at a sign.",
		["NoStats"] = "No database entries found.",
		["NoHistory"] = "No history entries found.",
		["NoEdit"] = "No edit entries found.",
		["NoOwner"] = "No owner entry found.",
		["HistoryInfo"] = "History: {history}",
		["Info"] = "Last edit: [<color=#ffd479>{timestamp}</color>] <color=#cd422b>{player}</color> (<color=#ffd479>{playerid}</color>)",
		["Link"] = "Image URL: <color=#ffd479>{url}</color>",
		["Owner"] = "Owner: <color=#cd422b>{player}</color> (<color=#ffd479>{playerid}</color>)",
		["NotLocked"] = "Sign is not locked.",
		["Unlocked"] = "Sign has been unlocked.",
		["NoClearPlayer"] = "No stored sign data found for <color=#ffd479>{player}</color>.",
		["ClearPlayer"] = "Data for <color=#ffd479>{player}</color> cleared. (<color=#ffd479>{count}</color> entries)",
		["ClearSign"] = "Data for sign cleared.",
		["NoData"] = "No stored sign data found.",
		["DataCleared"] = "Data for <color=#ffd479>{count}</color> sign(s) cleared.",
		["AdminWarn"] = "<color=#cd422b>{player}</color> edited sign. (<color=#ffd479>{location}</color>)",
		["AdminWarnURL"] = "<color=#cd422b>{player}</color> edited sign with sign artist, url <color=#cd422b>{url}</color>. (<color=#ffd479>{location}</color>)",
		["AdminWarnBL"] = "<color=#cd422b>{player}</color> edited sign with possible blacklist url <color=#cd422b>{url}</color>. (<color=#ffd479>{location}</color>)",
		["PlayerWarn"] = "Sign updated.  All signs are monitored for inappropriate content.",
		["DeleteBlacklist"] = "Your sign possibly contained inappropriate content and has been deleted.  If you believe this is an error, please contact an administrator.",
		["EditCountError"] = "Sign edit count is already zero.",
		["EditCountReset"] = "Sign edit count has been reset.",
		["Help"] = "<color=#ffd479>/sign</color> - Track sign changes and allows administrator access to all signs",
		["Menu"] = "\n<color=#ffd479>/sign toggle</color> - Enable or disable creation of new signs\n"..
		"<color=#ffd479>/sign stats</color> - View database statistics\n"..
		"<color=#ffd479>/sign lockall <true | false></color> - Lock or unlock all signs\n"..
		"<color=#ffd479>/sign deleteall</color> - Delete all signs (cannot be undone)\n"..
		"<color=#ffd479>/sign info</color> - View data for sign\n"..
		"<color=#ffd479>/sign unlock</color> - Unlock sign for editing\n"..
		"<color=#ffd479>/sign clear <sign | edits | player | all> [player]</color> - Clear data for sign, player or edit count (cannot be undone)",
		["Stats"] = "\n<color=#ffd479>Database entries:</color> {entries}\n"..
		"<color=#ffd479>Signs created:</color> {total}\n"..
		"<color=#ffd479>Sign edits:</color> {edits}\n"..
		"<color=#ffd479>SignArtist edits:</color> {artist}"
	}), self.Plugin)
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

local function FormatMessage(message, values)
	for key, value in pairs(values) do message = message:gsub("{" .. key .. "}", value) end
	return message
end

function PLUGIN:Lang(player, lng)
	local playerSteamID
	if player and player ~= nil then playerSteamID = rust.UserIDFromPlayer(player) end
	local message = lang.GetMessage(lng, self.Plugin, playerSteamID)
	if message == lng then message = lang.GetMessage("Prefix", self.Plugin, playerSteamID)..lang.GetMessage("LangError", self.Plugin, playerSteamID)..lng end
	return message
end

function PLUGIN:OnEntityBuilt(entity, object)
	if string.match(tostring(object), "sign") then
		local player = entity:GetOwnerPlayer()
		local playerSteamID = rust.UserIDFromPlayer(player)
		if not permission.UserHasPermission(playerSteamID, "signmanager.admin") then
			if self.Config.Settings.AllowSigns == "false" then
				local ksign = object:GetComponentInParent(global.Signage._type)
				if self.Config.Settings.UsePermissions == "true" then
					if not permission.UserHasPermission(playerSteamID, "signmanager.create") then
						ksign:KillMessage()
						self:RustMessage(player, self:Lang(player, "NoSignsAllowed"))
						return
					end
					else
					ksign:KillMessage()
					self:RustMessage(player, self:Lang(player, "NoSignsAllowed"))
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
	local player = entity:GetOwnerPlayer()
	if self.Config.Settings.LogToFile == "true" then
		local sign = object:GetComponentInParent(global.Signage._type)
		local location = tostring(sign.transform.position.x):match("([^.]+)").." "..tostring(sign.transform.position.y):match("([^.]+)").." "..tostring(sign.transform.position.z):match("([^.]+)")
		self:LogEvent(player, "[Create] "..location)
	end
	local signas, signid = tostring(object:GetComponentInParent(global.Signage._type)):match"([^%[]*)%[([^%]]*)"
	for current, data in pairs(Data.Signs) do
		if data.signid == signid then
			data.owner, data.timestamp, data.edit, data.history, data.www = player.displayName..":"..playerSteamID, "", "", "", ""
			self:SaveDataFile()
			return
		end
	end
	local newSign = {["signid"] = signid, ["owner"] = player.displayName..":"..playerSteamID, ["timestamp"] = "", ["edit"] = "", ["history"] = "", ["www"] = ""}
	table.insert(Data.Signs, newSign)
	self:SaveDataFile()
	return
end

function PLUGIN:OnSignUpdated(sign, player, text)
	local playerSteamID = rust.UserIDFromPlayer(player)
	if not permission.UserHasPermission(playerSteamID, "signmanager.admin") then
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
			if not permission.UserHasPermission(playerSteamID, "signmanager.admin") then
				if self.Config.Settings.UsePermissions == "true" then
					if not permission.UserHasPermission(playerSteamID, "signmanager.nobl") then
						StopWarn = self:ProcessBlacklist(sign, player, www)
					end
					else
					StopWarn = self:ProcessBlacklist(sign, player, www)
				end
			end
		end
	end
	if StopWarn == "exit" then return end
	if not permission.UserHasPermission(playerSteamID, "signmanager.admin") then
		if self.Config.Settings.AdminWarn == "true" and not StopWarn then
			local players = global.BasePlayer.activePlayerList:GetEnumerator()
			local location = tostring(sign.transform.position.x):match("([^.]+)").." "..tostring(sign.transform.position.y):match("([^.]+)").." "..tostring(sign.transform.position.z):match("([^.]+)")
			local message
			if www == "" then
				message = FormatMessage(self:Lang(player, "AdminWarn"), { player = player.displayName, location = location })
				else
				message = FormatMessage(self:Lang(player, "AdminWarnURL"), { player = player.displayName, url = www, location = location })
			end
			while players:MoveNext() do
				local playerSteamID = rust.UserIDFromPlayer(players.Current)
				if permission.UserHasPermission(playerSteamID, "signmanager.admin") then
					self:RustMessage(players.Current, message)
				end
			end
		end
		if self.Config.Settings.PlayerWarn == "true" then
			self:RustMessage(player, self:Lang(player, "PlayerWarn"))
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
					local message = FormatMessage(self:Lang(player, "AdminWarnBL"), { player = player.displayName, url = www, location = location })
					while players:MoveNext() do
						local playerSteamID = rust.UserIDFromPlayer(players.Current)
						if permission.UserHasPermission(playerSteamID, "signmanager.admin") then
							self:RustMessage(players.Current, message)
						end
					end
				end
				if self.Config.SignArtist.DeleteBlacklist == "true" then
					if sign:GetComponentInParent(global.Signage._type) then
						sign:KillMessage()
						self:RustMessage(player, self:Lang(player, "DeleteBlacklist"))
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
	if not permission.UserHasPermission(playerSteamID, "signmanager.admin") then
		self:RustMessage(player, self:Lang(player, "NoPermission"))
		return
	end
	if args.Length == 0 then
		self:RustMessage(player, self:Lang(player, "Menu"))
		return
		elseif args.Length > 0 then
		local func = args[0]
		if func ~= "toggle" and func ~= "stats" and func ~= "lockall" and func ~= "deleteall" and func ~= "info" and func ~= "unlock" and func ~= "clear" then
			self:RustMessage(player, self:Lang(player, "WrongArgs"))
			return
		end
		if func == "toggle" then
			local message
			if self.Config.Settings.AllowSigns == "true" then
				self.Config.Settings.AllowSigns = "false"
				message = FormatMessage(self:Lang(player, "ChangedStatus"), { status = "disabled" })
				else
				self.Config.Settings.AllowSigns = "true"
				message = FormatMessage(self:Lang(player, "ChangedStatus"), { status = "enabled" })
			end
			self:SaveConfig()
			self:RustMessage(player, message)
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
			self:RustMessage(player, FormatMessage(self:Lang(player, "Stats"), { entries = #Data.Signs, total = TotalSigns, edits = Data.Edits, artist = SignArtist }))
			return
		end
		if func == "lockall" then
			if args.Length < 2 then
				self:RustMessage(player, self:Lang(player, "WrongArgs"))
				return
			end
			local sfunc = args[1]
			if sfunc ~= "true" and sfunc ~= "false" then
				self:RustMessage(player, self:Lang(player, "WrongArgs"))
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
				local message
				if sfunc == "true" then
					message = FormatMessage(self:Lang(player, "GlobalLock"), { count = count, status = "locked" })
					else
					message = FormatMessage(self:Lang(player, "GlobalLock"), { count = count, status = "unlocked" })
				end
				self:RustMessage(player, message)
				else
				self:RustMessage(player, self:Lang(player, "NoSigns"))
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
				local message = FormatMessage(self:Lang(player, "GlobalDelete"), { count = count })
				self:RustMessage(player, message)
				else
				self:RustMessage(player, self:Lang(player, "NoSigns"))
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
							local message = FormatMessage(self:Lang(player, "HistoryInfo"), { history = string.sub(showhistory, 1, -3) })
							self:RustMessage(player, message)
							else
							self:RustMessage(player, self:Lang(player, "NoHistory"))
						end
					end
					if data.edit ~= "" then
						local edit, editid = tostring(data.edit):match"([^:]*):([^:]*)"
						local message = FormatMessage(self:Lang(player, "Info"), { player = edit, playerid = editid, timestamp = data.timestamp })
						self:RustMessage(player, message)
						else
						self:RustMessage(player, self:Lang(player, "NoEdit"))
					end
					if data.www ~= "" then
						local message = FormatMessage(self:Lang(player, "Link"), { url = data.www })
						self:RustMessage(player, message)
					end
					if data.owner then
						local owner, ownerid = tostring(data.owner):match"([^:]*):([^:]*)"
						message = FormatMessage(self:Lang(player, "Owner"), { player = owner, playerid = ownerid })
						self:RustMessage(player, message)
						else
						self:RustMessage(player, self:Lang(player, "NoOwner"))
					end
					return
				end
			end
			self:RustMessage(player, self:Lang(player, "NoInfo"))
			return
		end
		if func == "unlock" then
			local found, sign, signid = self:FindSign(player)
			if not found then return end
			if not sign:IsLocked() then
				self:RustMessage(player, self:Lang(player, "NotLocked"))
				return
			end
			sign:SetFlag(global.BaseEntity.Flags.Locked, false)
			self:RustMessage(player, self:Lang(player, "Unlocked"))
			return
		end
		if func == "clear" then
			if args.Length < 2 then
				self:RustMessage(player, self:Lang(player, "WrongArgs"))
				return
			end
			local sfunc = args[1]
			if sfunc ~= "sign" and sfunc ~= "edits" and sfunc ~= "player" and sfunc ~= "all" then
				self:RustMessage(player, self:Lang(player, "WrongArgs"))
				return
			end
			if sfunc == "sign" then
				local found, sign, signid = self:FindSign(player)
				if not found then return end
				for current, data in pairs(Data.Signs) do
					if data.signid == signid then
						table.remove(Data.Signs, current)
						self:SaveDataFile()
						self:RustMessage(player, self:Lang(player, "ClearSign"))
						return
					end
				end
				self:RustMessage(player, self:Lang(player, "NoInfo"))
				return
			end
			if sfunc == "edits" then
				if Data.Edits < 1 then
					self:RustMessage(player, self:Lang(player, "EditCountError"))
					return
				end
				Data.Edits = 0
				self:SaveDataFile()
				self:RustMessage(player, self:Lang(player, "EditCountReset"))
				return
			end
			if sfunc == "player" then
				if args.Length < 3 then
					self:RustMessage(player, self:Lang(player, "WrongArgs"))
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
					local message = FormatMessage(self:Lang(player, "ClearPlayer"), { player = playerName, count = count })
					self:RustMessage(player, message)
					else
					local message = FormatMessage(self:Lang(player, "NoClearPlayer"), { player = playerName })
					self:RustMessage(player, message)
				end
				return
			end
			if sfunc == "all" then
				if #Data.Signs < 1 then
					self:RustMessage(player, self:Lang(player, "NoData"))
					return
				end
				local message = FormatMessage(self:Lang(player, "DataCleared"), { count = #Data.Signs })
				Data.Signs = {}
				self:SaveDataFile()
				self:RustMessage(player, message)
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
				local message = FormatMessage(self:Lang(player, "TooFar"), { radius = self.Config.Settings.Radius })
				self:RustMessage(player, message)
				return false
			end
			sign = it.Current.collider:GetComponentInParent(global.Signage._type)
			break
		end
	end
	if not sign then
		self:RustMessage(player, self:Lang(player, "NoSign"))
		return false
	end
	local signas, signid = tostring(sign):match"([^%[]*)%[([^%]]*)"
	return true, sign, signid
end

function PLUGIN:FindPlayerCheck(player, target)
	local target = rust.FindPlayer(target)
	if not target then
		self:RustMessage(player, self:Lang(player, "NoPlayer"))
		return false
	end
	local targetName = target.displayName
	local targetSteamID = rust.UserIDFromPlayer(target)
	if self.Config.Settings.AdminOwnClear ~= "true" then
		if player == target then
			self:RustMessage(player, self:Lang(player, "Self"))
			return false
		end
	end
	return true, targetName, targetSteamID
end

function PLUGIN:LogEvent(player, logdata)
	ConVar.Server.Log("oxide/logs/SignManager.txt", player.displayName.." ("..rust.UserIDFromPlayer(player).."): "..logdata)
end

function PLUGIN:RustMessage(player, message)
	rust.SendChatMessage(player, "<size="..tonumber(self.Config.Settings.MessageSize)..">"..self:Lang(player, "Prefix")..message.."</size>")
end

function PLUGIN:SendHelpText(player)
	local playerSteamID = rust.UserIDFromPlayer(player)
	if permission.UserHasPermission(playerSteamID, "signmanager.admin") then
		self:RustMessage(player, self:Lang(player, "Help"))
	end
end