PLUGIN.Title        = "Mail Manager"
PLUGIN.Description  = "Allows players to send, receive and manage in-game mail."
PLUGIN.Author       = "InSaNe8472"
PLUGIN.Version      = V(1,1,8)
PLUGIN.ResourceID   = 1244

local DataFile = "MailManager"
local Data = {}
local ReplyHistory = {}
local CoolDown = {}
local MailWarn = {}
local popupApi

function PLUGIN:Init()
	permission.RegisterPermission("mailmanager.read", self.Plugin)
	permission.RegisterPermission("mailmanager.send", self.Plugin)
	permission.RegisterPermission("mailmanager.nocd", self.Plugin)
	permission.RegisterPermission("mailmanager.admin", self.Plugin)
	command.AddChatCommand("mail", self.Plugin, "cmdMail")
	command.AddConsoleCommand("mail.preview", self.Plugin, "cmdMailCP")
	command.AddConsoleCommand("mail.send", self.Plugin, "cmdMailCS")
	self:LoadDataFile()
	self:LoadDefaultConfig()
end

function PLUGIN:LoadDefaultConfig()
	self.Config.Settings = self.Config.Settings or {}
	self.Config.Messages = self.Config.Messages or {}
	self.Config.Settings.Prefix = self.Config.Settings.Prefix or "[<color=#cd422b>Mail Manager</color>]"
	self.Config.Settings.EnablePopup = self.Config.Settings.EnablePopup or "true"
	self.Config.Settings.MessageSize = self.Config.Settings.MessageSize or "13"
	self.Config.Settings.AllowConsoleSend = self.Config.Settings.AllowConsoleSend or "true"
	self.Config.Settings.CoolDown = self.Config.Settings.CoolDown or "30"
	self.Config.Settings.InboxLimit = self.Config.Settings.InboxLimit or "15"
	self.Config.Settings.Reminder = self.Config.Settings.Reminder or "true"
	self.Config.Settings.ReminderInterval = self.Config.Settings.ReminderInterval or "300"
	self.Config.Settings.Timestamp = self.Config.Settings.Timestamp or "MM/dd/yyyy @ h:mm tt"
	self.Config.Settings.UsePermissions = self.Config.Settings.UsePermissions or "true"
	self.Config.Settings.SendToChat = self.Config.Settings.SendToChat or "true"
	self.Config.Settings.SendToConsole = self.Config.Settings.SendToConsole or "true"
	self.Config.Messages.ChangedStatus = self.Config.Messages.ChangedStatus or "New mail reminder <color=#cd422b>{status}</color>."
	self.Config.Messages.Reminder = self.Config.Messages.Reminder or "You have new mail.  Use <color=#cd422b>/mail inbox</color> to view them."
	self.Config.Messages.ReadOnlyReminder = self.Config.Messages.ReadOnlyReminder or "You have new mail.  Use <color=#cd422b>/mail inbox</color> to view them.  You have read only permission and will not be able to respond."
	self.Config.Messages.WrongArgs = self.Config.Messages.WrongArgs or "Syntax error.  Use <color=#cd422b>/mail</color> for help."
	self.Config.Messages.WrongArgsConsole = self.Config.Messages.WrongArgsConsole or "Syntax error.  Use <color=#cd422b>/mail console</color> for help."
	self.Config.Messages.NoMail = self.Config.Messages.NoMail or "You do not have any mail."
	self.Config.Messages.NoPlayer = self.Config.Messages.NoPlayer or "Player not found.  Please try again."
	self.Config.Messages.MultiPlayer = self.Config.Messages.MultiPlayer or "Multiple players found.  Provide a more specific username."
	self.Config.Messages.Self = self.Config.Messages.Self or "You cannot send mail to yourself."
	self.Config.Messages.NotNumber = self.Config.Messages.NotNumber or "The ID must be a number."
	self.Config.Messages.NotRead = self.Config.Messages.NotRead or "No mail with ID <color=#cd422b>{id}</color> found."
	self.Config.Messages.NoReply = self.Config.Messages.NoReply or "You must read a message before you can reply."
	self.Config.Messages.Sent = self.Config.Messages.Sent or "Your message was successfully sent to <color=#cd422b>{player}</color> with subject <color=#f9169f>{subject}</color>."
	self.Config.Messages.ReadOnlySent = self.Config.Messages.ReadOnlySent or "Your message was successfully sent to <color=#cd422b>{player}</color> with subject <color=#f9169f>{subject}</color>.  <color=#cd422b>{player}</color> has read only permission and will not be able to respond."
	self.Config.Messages.Deleted = self.Config.Messages.Deleted or "Message with ID <color=#cd422b>{id}</color> successfully deleted."
	self.Config.Messages.NotDeleted = self.Config.Messages.NotDeleted or "No message with ID <color=#cd422b>{id}</color> found."
	self.Config.Messages.DeleteAll = self.Config.Messages.DeleteAll or "All messages successfully deleted."
	self.Config.Messages.NewMail = self.Config.Messages.NewMail or "You received new mail from <color=#cd422b>{player}</color> with subject <color=#f9169f>{subject}</color>.  Use <color=#cd422b>/mail read {id}</color> to read it."
	self.Config.Messages.ReadOnlyNewMail = self.Config.Messages.ReadOnlyNewMail or "You received new mail from <color=#cd422b>{player}</color> with subject <color=#f9169f>{subject}</color>.  Use <color=#cd422b>/mail read {id}</color> to read it.  You have read only permission and will not be able to respond."
	self.Config.Messages.CoolDown = self.Config.Messages.CoolDown or "You must wait <color=#cd422b>{cooldown} seconds</color> before sending another message."
	self.Config.Messages.SelfInboxFull = self.Config.Messages.SelfInboxFull or "<color=#cd422b>Your inbox is currently full.  You will not be able to receive new mail until you delete old messages.</color>"
	self.Config.Messages.SenderInboxFull = self.Config.Messages.SenderInboxFull or "Your message could not be sent to <color=#cd422b>{player}</color>.  Their inbox is currently full."
	self.Config.Messages.SelfNoPermission = self.Config.Messages.SelfNoPermission or "You do not have permission to use this command."
	self.Config.Messages.SenderNoPermission = self.Config.Messages.SenderNoPermission or "Your message could not be sent to <color=#cd422b>{player}</color>.  They do not have the required permissions."
	self.Config.Messages.SendAll = self.Config.Messages.SendAll or "Your message was successfully sent to <color=#cd422b>{total} players</color> with subject <color=#f9169f>{subject}</color>."
	self.Config.Messages.GroupNotFound = self.Config.Messages.GroupNotFound or "The <color=#cd422b>{group}</color> was not found."
	self.Config.Messages.SendAllGroup = self.Config.Messages.SendAllGroup or "Your message was successfully sent to <color=#cd422b>{total} players</color> in <color=#cd422b>{group}</color> with subject <color=#f9169f>{subject}</color>."
	self.Config.Messages.SendAllFail = self.Config.Messages.SendAllFail or "Your message was not sent to any players.  No players found or incorrect player permissions."
	self.Config.Messages.GlobalDelete = self.Config.Messages.GlobalDelete or "Mail for all players has been deleted."
	self.Config.Messages.CheckConsole = self.Config.Messages.CheckConsole or "Your mail information has been sent to the console.  <color=#cd422b>Press F1</color> to view it."
	if self.Config.Settings.SendToChat ~= "true" and self.Config.Settings.SendToConsole ~= "true" then self.Config.Settings.SendToChat = "true" end
	if not tonumber(self.Config.Settings.MessageSize) or tonumber(self.Config.Settings.MessageSize) < 1 then self.Config.Settings.MessageSize = "13" end
	if not tonumber(self.Config.Settings.CoolDown) or tonumber(self.Config.Settings.CoolDown) < 1 then self.Config.Settings.CoolDown = "30" end
	if not tonumber(self.Config.Settings.InboxLimit) or tonumber(self.Config.Settings.InboxLimit) < 1 then self.Config.Settings.InboxLimit = "15" end
	if not tonumber(self.Config.Settings.ReminderInterval) or tonumber(self.Config.Settings.ReminderInterval) < 1 then self.Config.Settings.ReminderInterval = "300" end
	self:SaveConfig()
end

function PLUGIN:LoadDataFile()
	local data = datafile.GetDataTable(DataFile)
	Data = data or {}
end

function PLUGIN:SaveDataFile()
	datafile.SaveDataTable(DataFile)
end

function PLUGIN:Unload()
	datafile.SaveDataTable(DataFile)
end

function PLUGIN:OnServerInitialized()
	popupApi = plugins.Find("PopupNotifications") or false
	self:StartReminderTimer()
end

function PLUGIN:OnPlayerInit(player)
	local playerSteamID = rust.UserIDFromPlayer(player)
	local playerData = self:GetPlayerData(playerSteamID, false)
	if playerData and #playerData.Mail >= 1 and playerData.new then
		if self.Config.Settings.UsePermissions == "true" then
			if permission.UserHasPermission(playerSteamID, "mailmanager.admin") or permission.UserHasPermission(playerSteamID, "mailmanager.read") then
				if permission.UserHasPermission(playerSteamID, "mailmanager.admin") or permission.UserHasPermission(playerSteamID, "mailmanager.send") then
					self:RustMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.Reminder)
					else
					self:RustMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.ReadOnlyReminder)
				end
			end
			else
			self:RustMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.Reminder)
		end
	end
end

function PLUGIN:OnPlayerDisconnected(player)
	local playerSteamID = rust.UserIDFromPlayer(player)
	if ReplyHistory[playerSteamID] then
		ReplyHistory[playerSteamID] = nil
	end
end

function PLUGIN:GetPlayerData(playerSteamID, addNewEntry)
	local playerData = Data[playerSteamID]
	if not playerData and addNewEntry then
		playerData = {}
		playerData.id = 0
		playerData.new = false
		playerData.Mail = {}
		Data[playerSteamID] = playerData
		self:SaveDataFile()
	end
	return playerData
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

function PLUGIN:cmdMail(player, cmd, args)
	local playerSteamID = rust.UserIDFromPlayer(player)
	if self.Config.Settings.UsePermissions == "true" then
		if not permission.UserHasPermission(playerSteamID, "mailmanager.admin") then
			if not permission.UserHasPermission(playerSteamID, "mailmanager.read") then
				self:RustMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.SelfNoPermission)
				return
			end
		end
	end
	if args.Length == 0 then
		if permission.UserHasPermission(playerSteamID, "mailmanager.admin") then
			self:RustMessage(player,
				self.Config.Settings.Prefix.." <color=#ffd479>/mail globalsend <subject> <message></color> - Send new message to all players\n"..
				self.Config.Settings.Prefix.." <color=#ffd479>/mail globaldelete</color> - Delete all mail for all players (cannot be undone)\n"..
				self.Config.Settings.Prefix.." <color=#ffd479>/mail <group | clan> <group_name | clan_name> <subject> <message></color> - Send new message to group or clan"
			)
		end
		self:RustMessage(player,
			self.Config.Settings.Prefix.." <color=#ffd479>/mail reminder</color> - Enable or disable new mail reminder\n"..
			self.Config.Settings.Prefix.." <color=#ffd479>/mail limits</color> - View mail limits\n"..
			self.Config.Settings.Prefix.." <color=#ffd479>/mail inbox</color> - View all messages in inbox\n"..
			self.Config.Settings.Prefix.." <color=#ffd479>/mail read <id></color> - Read message with ID\n"..
			self.Config.Settings.Prefix.." <color=#ffd479>/mail preview <subject> <message></color> - Preview message before sending"
		)
		self:RustMessage(player,
			self.Config.Settings.Prefix.." <color=#ffd479>/mail send <player> <subject> <message></color> - Send new message to player\n"..
			self.Config.Settings.Prefix.." <color=#ffd479>/mail reply <message></color> - Reply to last message read\n"..
			self.Config.Settings.Prefix.." <color=#ffd479>/mail forward <player> <id></color> - Forward message id to player\n"..
			self.Config.Settings.Prefix.." <color=#ffd479>/mail delete <id></color> - Delete message with ID\n"..
			self.Config.Settings.Prefix.." <color=#ffd479>/mail deleteall</color> - Delete all messages in inbox (cannot be undone)\n"..
			self.Config.Settings.Prefix.." <color=#ffd479>/mail console</color> - View available console commands"
		)
		return
		elseif args.Length > 0 then
		local func = args[0]
		if func ~= "reminder" and func ~= "limits" and func ~= "inbox" and func ~= "read" and func ~= "preview" and func ~= "send" and func ~= "reply" and func ~= "forward" and func ~= "delete" and
			func ~= "deleteall" and func ~= "globalsend" and func ~= "globaldelete" and func ~= "group" and func ~= "clan" and func ~= "console" then
			self:RustMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.WrongArgs)
			return
		end
		if func == "reminder" then
			if not MailWarn[playerSteamID] or MailWarn[playerSteamID] == nil then MailWarn[playerSteamID] = "true" end
			local message = ""
			if MailWarn[playerSteamID] == "true" then
				MailWarn[playerSteamID] = "false"
				message = FormatMessage(self.Config.Messages.ChangedStatus, { status = "disabled" })
				else
				MailWarn[playerSteamID] = "true"
				message = FormatMessage(self.Config.Messages.ChangedStatus, { status = "enabled" })
			end
			self:RustMessage(player, self.Config.Settings.Prefix.." "..message)
			return
		end
		if func == "limits" then
			local message = "Read, Send"
			if self.Config.Settings.UsePermissions == "true" then
				if not permission.UserHasPermission(playerSteamID, "mailmanager.admin") then
					if not permission.UserHasPermission(playerSteamID, "mailmanager.send") then
						message = "Read"
					end
				end
			end
			if permission.UserHasPermission(playerSteamID, "mailmanager.admin") or permission.UserHasPermission(playerSteamID, "mailmanager.nocd") then message = message..", No Cooldown" end
			self:RustMessage(player,
				self.Config.Settings.Prefix.." Send Cooldown: <color=#ffd479>"..self.Config.Settings.CoolDown.." seconds</color>\n"..
				self.Config.Settings.Prefix.." Inbox Limit: <color=#ffd479>"..self.Config.Settings.InboxLimit.." messages</color>\n"..
				self.Config.Settings.Prefix.." Your Permissions: <color=#ffd479>"..message.."</color>"
			)
			if not permission.UserHasPermission(playerSteamID, "mailmanager.admin") then
				local playerData = self:GetPlayerData(playerSteamID, false)
				if playerData and #playerData.Mail >= tonumber(self.Config.Settings.InboxLimit) then
					self:RustMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.SelfInboxFull)
				end
			end
			return
		end
		if func == "inbox" then
			local playerData = self:GetPlayerData(playerSteamID, false)
			if not playerData or #playerData.Mail == 0 then 
				self:RustMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.NoMail)
				return
			end
			if #playerData.Mail >= 1 then
				local message = {}
				local newcount, _newcount = 0, 0
				if self.Config.Settings.SendToConsole == "true" then
					player:SendConsoleCommand("echo <color=white>------------------------------ "..self.Config.Settings.Prefix.." : </color><color=#cd422b>Inbox </color><color=white>------------------------------</color>\n")
				end
				for Mail, data in pairs(playerData.Mail) do
					if self.Config.Settings.SendToChat == "true" then
						if data.new then
							message = "[<color=#cd422b> "..data.id.." </color>] : <color=#ffd479>"..data.timestamp.."</color> : <color=#cd422b>"..data.sender.."</color> : <color=#f9169f>"..data.subject.."</color>"
							newcount = newcount + 1
							else
							message = "[<color=green> "..data.id.." </color>] : <color=#ffd479>"..data.timestamp.."</color> : <color=#cd422b>"..data.sender.."</color> : <color=#f9169f>"..data.subject.."</color>"
						end
						self:RustMessage(player, self.Config.Settings.Prefix.." "..message)
					end
					if self.Config.Settings.SendToConsole == "true" then
						if data.new then
							message = "[<color=#cd422b> "..data.id.." </color>] : <color=#ffd479>"..data.timestamp.."</color> : <color=#cd422b>"..data.sender.."</color> : <color=#f9169f>"..data.subject.."</color>"
							_newcount = _newcount + 1
							else
							message = "[<color=green> "..data.id.." </color>] : <color=#ffd479>"..data.timestamp.."</color> : <color=#cd422b>"..data.sender.."</color> : <color=#f9169f>"..data.subject.."</color>"
						end
						player:SendConsoleCommand("echo "..message)
					end
				end
				if self.Config.Settings.SendToChat == "true" then
					self:RustMessage(player, self.Config.Settings.Prefix.." Your inbox contains <color=#cd422b>"..newcount.."</color> new message(s) and <color=green>"..#playerData.Mail.."</color> total message(s).  Use <color=#cd422b>/mail read <id></color> to read a message.")
				end
				if self.Config.Settings.SendToConsole == "true" then
					player:SendConsoleCommand("echo ".." Your inbox contains <color=#cd422b>".._newcount.."</color> new message(s) and <color=green>"..#playerData.Mail.."</color> total message(s).  Use <color=#cd422b>/mail read <id></color> to read a message.")
					self:RustMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.CheckConsole)
				end
			end
			if not permission.UserHasPermission(playerSteamID, "mailmanager.admin") then
				if #playerData.Mail >= tonumber(self.Config.Settings.InboxLimit) then
					self:RustMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.SelfInboxFull)
				end
			end
			return
		end
		if func == "read" then
			local playerData = self:GetPlayerData(playerSteamID, false)
			if not playerData or #playerData.Mail == 0 then
				self:RustMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.NoMail)
				return
			end
			if args.Length < 2 then
				self:RustMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.WrongArgs)
				return
			end
			if not tonumber(args[1]) then
				self:RustMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.NotNumber)
				return
			end
			for current, data in pairs(playerData.Mail) do
				if data.id == tonumber(args[1]) then
					if self.Config.Settings.SendToChat == "true" then
						self:RustMessage(player,
							"[<color=#cd422b> "..data.id.." </color>] : <color=#ffd479>"..data.timestamp.."</color> : <color=#cd422b>"..data.sender.."</color> : <color=#f9169f>"..data.subject.."</color>\n"..
							"<color=#ffd479>"..data.message:gsub("++", "\n").."</color>"
						)
					end
					if self.Config.Settings.SendToConsole == "true" then
						player:SendConsoleCommand("echo "..
							"<color=white>------------------------------ "..self.Config.Settings.Prefix.." : Message</color> <color=#cd422b>"..data.id.." </color><color=white>------------------------------</color>\n"..
							"<color=white>Received:</color><color=#ffd479> "..data.timestamp.."</color>\n"..
							"<color=white>Sender:</color><color=#cd422b> "..data.sender.."</color>\n"..
							"<color=white>Subject:</color><color=#f9169f> "..data.subject.."</color>\n"..
							"<color=white>Message:</color><color=#ffd479>\n"..data.message:gsub("++", "\n").."</color>"
						)
						self:RustMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.CheckConsole)
					end
					data.new = false
					playerData.new = false
					for Mail, newmail in pairs(playerData.Mail) do
						if newmail.new then
							playerData.new = true
							break
						end
					end
					self:SaveDataFile()
					ReplyHistory[playerSteamID] = data.senderid..":"..data.subject
					if not permission.UserHasPermission(playerSteamID, "mailmanager.admin") then
						if #playerData.Mail >= tonumber(self.Config.Settings.InboxLimit) then
							self:RustMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.SelfInboxFull)
						end
					end
					return
				end
			end
			local message = FormatMessage(self.Config.Messages.NotRead, { id = args[1] })
			self:RustMessage(player, self.Config.Settings.Prefix.." "..message)
			return
		end
		if func == "preview" then
			if self.Config.Settings.UsePermissions == "true" then
				if not permission.UserHasPermission(playerSteamID, "mailmanager.admin") then
					if not permission.UserHasPermission(playerSteamID, "mailmanager.send") then
						self:RustMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.SelfNoPermission)
						return
					end
				end
			end
			if args.Length < 3 then
				self:RustMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.WrongArgs)
				return
			end
			local subject = args[1]
			local args = self:TableMessage(args)
			local message = ""
			local i = 3
			while args[i] do
				message = message..args[i].." "
				i = i + 1
			end
			local timestamp = time.GetCurrentTime():ToLocalTime():ToString(self.Config.Settings.Timestamp)
			if self.Config.Settings.SendToChat == "true" then
				self:RustMessage(player,
					"[<color=#cd422b> PREVIEW </color>] : <color=#ffd479>"..timestamp.."</color> : <color=#cd422b>"..player.displayName.."</color> : <color=#f9169f>"..subject.."</color>\n"..
					"<color=#ffd479>"..message:gsub("++", "\n").."</color>"
				)
			end
			if self.Config.Settings.SendToConsole == "true" then
				player:SendConsoleCommand("echo "..
					"<color=white>------------------------------ Mail : Message</color> <color=#cd422b>PREVIEW </color><color=white>------------------------------</color>\n"..
					"<color=white>Received:</color><color=#ffd479> "..timestamp.."</color>\n"..
					"<color=white>Sender:</color><color=#cd422b> "..player.displayName.."</color>\n"..
					"<color=white>Subject:</color><color=#f9169f> "..subject.."</color>\n"..
					"<color=white>Message:</color><color=#ffd479>\n"..message:gsub("++", "\n").."</color>"
				)
				self:RustMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.CheckConsole)
			end
			return
		end
		if func == "send" then
			if self.Config.Settings.UsePermissions == "true" then
				if not permission.UserHasPermission(playerSteamID, "mailmanager.admin") then
					if not permission.UserHasPermission(playerSteamID, "mailmanager.send") then
						self:RustMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.SelfNoPermission)
						return
					end
				end
			end
			if not self:CheckCooldown(player, "chat") then return end
			if args.Length < 4 then
				self:RustMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.WrongArgs)
				return
			end
			local args = self:TableMessage(args)
			local message = ""
			local i = 4
			while args[i] do
				message = message..args[i].." "
				i = i + 1
			end
			self:sendMail(player, args[2], args[3], message, "send")
			return
		end
		if func == "reply" then
			if self.Config.Settings.UsePermissions == "true" then
				if not permission.UserHasPermission(playerSteamID, "mailmanager.admin") then
					if not permission.UserHasPermission(playerSteamID, "mailmanager.send") then
						self:RustMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.SelfNoPermission)
						return
					end
				end
			end
			if not self:CheckCooldown(player, "chat") then return end
			if not ReplyHistory[playerSteamID] then
				self:RustMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.NoReply)
				return
			end
			if args.Length < 2 then
				self:RustMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.WrongArgs)
				return
			end
			local args = self:TableMessage(args)
			local message = ""
			local i = 2
			while args[i] do
				message = message..args[i].." "
				i = i + 1
			end
			local ReplyTo, ReplySubject = tostring(ReplyHistory[playerSteamID]):match("([^:]+):([^:]+)")
			self:sendMail(player, ReplyTo, "Re: "..ReplySubject, message, "reply")
			return
		end
		if func == "forward" then
			local playerData = self:GetPlayerData(playerSteamID, false)
			if not playerData or #playerData.Mail == 0 then
				self:RustMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.NoMail)
				return
			end
			if args.Length < 3 then
				self:RustMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.WrongArgs)
				return
			end
			if not tonumber(args[2]) then
				self:RustMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.NotNumber)
				return
			end
			for current, data in pairs(playerData.Mail) do
				if data.id == tonumber(args[2]) then
					self:sendMail(player, args[1], "Fw: "..data.subject, data.message.."++ ++Original sender: "..data.sender, "send")
					return
				end
			end
			local message = FormatMessage(self.Config.Messages.NotRead, { id = args[2] })
			self:RustMessage(player, self.Config.Settings.Prefix.." "..message)
			return
		end
		if func == "delete" then
			local playerData = self:GetPlayerData(playerSteamID, false)
			if not playerData or #playerData.Mail == 0 then 
				self:RustMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.NoMail)
				return
			end
			if args.Length < 2 then
				self:RustMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.WrongArgs)
				return
			end
			if not tonumber(args[1]) then
				self:RustMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.NotNumber)
				return
			end
			for current, data in pairs(playerData.Mail) do
				if data.id == tonumber(args[1]) then
					table.remove(playerData.Mail, current)
					playerData.new = false
					for Mail, newmail in pairs(playerData.Mail) do
						if newmail.new then
							playerData.new = true
							break
						end
					end
					if #playerData.Mail == 0 then 
						playerData.id = 0
					end
					self:SaveDataFile()
					local message = FormatMessage(self.Config.Messages.Deleted, { id = args[1] })
					self:RustMessage(player, self.Config.Settings.Prefix.." "..message)
					if not permission.UserHasPermission(playerSteamID, "mailmanager.admin") then
						if #playerData.Mail >= tonumber(self.Config.Settings.InboxLimit) then
							self:RustMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.SelfInboxFull)
						end
					end
					return
				end
			end
			local message = FormatMessage(self.Config.Messages.NotDeleted, { id = args[1] })
			self:RustMessage(player, self.Config.Settings.Prefix.." "..message)
			return
		end
		if func == "deleteall" then
			local playerData = self:GetPlayerData(playerSteamID, false)
			if not playerData or #playerData.Mail == 0 then 
				self:RustMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.NoMail)
				return
			end
			playerData.id = 0
			playerData.new = false
			playerData.Mail = {}
			self:SaveDataFile()
			self:RustMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.DeleteAll)
			return
		end
		if func == "globalsend" then
			if not permission.UserHasPermission(playerSteamID, "mailmanager.admin") then
				self:RustMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.SelfNoPermission)
				return
			end
			if args.Length < 3 then
				self:RustMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.WrongArgs)
				return
			end
			local args = self:TableMessage(args)
			local message = ""
			local i = 3
			while args[i] do
				message = message..args[i].." "
				i = i + 1
			end
			local subject, count = args[2], 0
			local players = global.BasePlayer.activePlayerList:GetEnumerator()
			while players:MoveNext() do
				if players.Current ~= player then
					local targetSteamID = rust.UserIDFromPlayer(players.Current)
					if permission.UserHasPermission(targetSteamID, "mailmanager.read") then
						self:newMail(player, targetSteamID, players.Current, subject, message)
						count = count + 1
					end
				end
			end
			local players = global.BasePlayer.sleepingPlayerList:GetEnumerator()
			while players:MoveNext() do
				local targetSteamID = rust.UserIDFromPlayer(players.Current)
				if permission.UserHasPermission(targetSteamID, "mailmanager.read") then
					self:newMail(player, targetSteamID, players.Current, subject, message)
					count = count + 1
				end
			end
			if count == 0 then
				self:RustMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.SendAllFail)
				return
			end
			local message = FormatMessage(self.Config.Messages.SendAll, { total = count, subject = subject })
			self:RustMessage(player, self.Config.Settings.Prefix.." "..message)
			return
		end
		if func == "globaldelete" then
			if not permission.UserHasPermission(playerSteamID, "mailmanager.admin") then
				self:RustMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.SelfNoPermission)
				return
			end
			for current, data in pairs(Data) do
				if data.Mail then
					data.id = 0
					data.new = false
					data.Mail = {}
				end 
			end
			self:SaveDataFile()
			self:RustMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.GlobalDelete)
			return
		end
		if func == "group" then
			if not permission.UserHasPermission(playerSteamID, "mailmanager.admin") then
				self:RustMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.SelfNoPermission)
				return
			end
			if args.Length < 4 then
				self:RustMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.WrongArgs)
				return
			end
			local ToGroup = args[1]
			local Group, GroupExists = datafile.GetDataTable("oxide.groups"), false
			for current, data in pairs(Group) do
				if current == ToGroup then
					GroupExists = true
					break
				end
			end
			if not GroupExists then
				local message = FormatMessage(self.Config.Messages.GroupNotFound, { group = "group "..ToGroup })
				self:RustMessage(player, self.Config.Settings.Prefix.." "..message)
				return
			end
			local args = self:TableMessage(args)
			local message = ""
			local i = 4
			while args[i] do
				message = message..args[i].." "
				i = i + 1
			end
			local subject, count = args[3], 0
			local Group = datafile.GetDataTable("oxide.users")
			for current, data in pairs(Group) do
				local i, PlayerGroups = 1, data.Groups
				while PlayerGroups[i] do
					if PlayerGroups[i] == ToGroup then
						local numFound, targetPlayerTbl = FindPlayer(data.LastSeenNickname, true)
						local targetPlayer = nil
						if numFound == 1 then targetPlayer = targetPlayerTbl[1] end
						if targetPlayer ~= nil and targetPlayer ~= player then
							local targetSteamID = rust.UserIDFromPlayer(targetPlayer)
							if permission.UserHasPermission(targetSteamID, "mailmanager.read") then
								self:newMail(player, targetSteamID, targetPlayer, subject, message)
								count = count + 1
							end
						end
						break
					end
					i = i + 1
				end
			end
			if count == 0 then
				self:RustMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.SendAllFail)
				return
			end
			local message = FormatMessage(self.Config.Messages.SendAllGroup, { total = count, group = "group "..ToGroup, subject = subject })
			self:RustMessage(player, self.Config.Settings.Prefix.." "..message)
			return
		end
		if func == "clan" then
			if not permission.UserHasPermission(playerSteamID, "mailmanager.admin") then
				self:RustMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.SelfNoPermission)
				return
			end
			if args.Length < 4 then
				self:RustMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.WrongArgs)
				return
			end
			local ToGroup = args[1]
			local Group, GroupExists = datafile.GetDataTable("rustio_clans"), false
			for current, data in pairs(Group.clans) do
				if current == ToGroup then
					GroupExists = true
					break
				end
			end
			if not GroupExists then
				local message = FormatMessage(self.Config.Messages.GroupNotFound, { group = "clan "..ToGroup })
				self:RustMessage(player, self.Config.Settings.Prefix.." "..message)
				return
			end
			local args = self:TableMessage(args)
			local message = ""
			local i = 4
			while args[i] do
				message = message..args[i].." "
				i = i + 1
			end
			local subject, count = args[3], 0
			local Group = datafile.GetDataTable("rustio_clans")
			for current, data in pairs(Group.clans) do
				if current == ToGroup then 
					local i, PlayerGroups = 1, data.members
					while PlayerGroups[i] do
						local numFound, targetPlayerTbl = FindPlayer(PlayerGroups[i], true)
						local targetPlayer = nil
						if numFound == 1 then targetPlayer = targetPlayerTbl[1] end
						if targetPlayer ~= nil and targetPlayer ~= player then
							local targetSteamID = rust.UserIDFromPlayer(targetPlayer)
							if permission.UserHasPermission(targetSteamID, "mailmanager.read") then
								self:newMail(player, targetSteamID, targetPlayer, subject, message)
								count = count + 1
							end
						end
						i = i + 1
					end
					break
				end
			end
			if count == 0 then
				self:RustMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.SendAllFail)
				return
			end
			local message = FormatMessage(self.Config.Messages.SendAllGroup, { total = count, group = "clan "..ToGroup, subject = subject })
			self:RustMessage(player, self.Config.Settings.Prefix.." "..message)
			return
		end
		if func == "console" then
			self:RustMessage(player,
				self.Config.Settings.Prefix.." <color=#ffd479>mail.preview <subject> <message></color> - Preview message before sending\n"..
				self.Config.Settings.Prefix.." <color=#ffd479>mail.send <player> <subject> <message></color> - Send new message to player"
			)
			return
		end
	end
end

function PLUGIN:cmdMailCP(arg)
	local player = arg.connection.player
	local playerSteamID = rust.UserIDFromPlayer(player)
	if self.Config.Settings.UsePermissions == "true" then
		if not permission.UserHasPermission(playerSteamID, "mailmanager.admin") then
			if not permission.UserHasPermission(playerSteamID, "mailmanager.send") then
				self:RustMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.SelfNoPermission)
				return
			end
		end
	end
	local Subject, TempMsg = arg.ArgsStr:match("([^ ]+) ([^ ]+)")
	if not TempMsg then
		player:SendConsoleCommand("echo "..self.Config.Settings.Prefix.." "..self.Config.Messages.WrongArgsConsole)
		return
	end
	local args = self:TableMessage(arg.Args)
	local message = ""
	local i = 2
	while args[i] do
		message = message..args[i].." "
		i = i + 1
	end
	local timestamp = time.GetCurrentTime():ToLocalTime():ToString(self.Config.Settings.Timestamp)
	player:SendConsoleCommand("echo "..
		"<color=white>------------------------------ Mail : Message</color> <color=#cd422b>PREVIEW </color><color=white>------------------------------</color>\n"..
		"<color=white>Received:</color><color=#ffd479> "..timestamp.."</color>\n"..
		"<color=white>Sender:</color><color=#cd422b> "..player.displayName.."</color>\n"..
		"<color=white>Subject:</color><color=#f9169f> "..Subject.."</color>\n"..
		"<color=white>Message:</color><color=#ffd479>\n"..message:gsub("++", "\n").."</color>"
	)
	return
end

function PLUGIN:cmdMailCS(arg)
	local player = arg.connection.player
	local playerSteamID = rust.UserIDFromPlayer(player)
	if not permission.UserHasPermission(playerSteamID, "mailmanager.admin") then
		if self.Config.Settings.AllowConsoleSend ~= "true" then
			player:SendConsoleCommand("echo "..self.Config.Settings.Prefix.." "..self.Config.Messages.SelfNoPermission)
			return
		end
	end
	if self.Config.Settings.UsePermissions == "true" then
		if not permission.UserHasPermission(playerSteamID, "mailmanager.admin") then
			if not permission.UserHasPermission(playerSteamID, "mailmanager.send") then
				player:SendConsoleCommand("echo "..self.Config.Settings.Prefix.." "..self.Config.Messages.SelfNoPermission)
				return
			end
		end
	end
	if not self:CheckCooldown(player, "console") then return end
	local SendTo, Subject, TempMsg = arg.ArgsStr:match("([^ ]+) ([^ ]+) ([^ ]+)")
	if not TempMsg then
		player:SendConsoleCommand("echo "..self.Config.Settings.Prefix.." "..self.Config.Messages.WrongArgsConsole)
		return
	end
	local args = self:TableMessage(arg.Args)
	local message = ""
	local i = 3
	while args[i] do
		message = message..args[i].." "
		i = i + 1
	end
	self:sendMail(player, SendTo, Subject, message, "console")
	return
end

function PLUGIN:sendMail(player, target, subject, message, sendtype)
	local numFound, targetPlayerTbl = FindPlayer(target, true)
	if numFound == 0 then
		if sendtype == "console" then
			player:SendConsoleCommand("echo "..self.Config.Settings.Prefix.." "..self.Config.Messages.NoPlayer)
			else
			self:RustMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.NoPlayer)
		end
		return
	end
	if numFound > 1 then
		local targetNameString = ""
		for i = 1, numFound do
			targetNameString = targetNameString..targetPlayerTbl[i].displayName..", "
		end
		if sendtype == "console" then
			player:SendConsoleCommand("echo "..self.Config.Settings.Prefix.." "..self.Config.Messages.MultiPlayer)
			player:SendConsoleCommand("echo "..targetNameString)
			else
			self:RustMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.MultiPlayer)
			self:RustMessage(player, targetNameString)
		end
		return
	end
	local targetPlayer = targetPlayerTbl[1]
	local targetName = targetPlayer.displayName
	local targetSteamID = rust.UserIDFromPlayer(targetPlayer)
	local playerSteamID = rust.UserIDFromPlayer(player)
	if player == targetPlayer then
		if sendtype == "console" then
			player:SendConsoleCommand("echo "..self.Config.Settings.Prefix.." "..self.Config.Messages.Self)
			else
			self:RustMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.Self)
		end
		--return
	end
	if self.Config.Settings.UsePermissions == "true" then
		if not permission.UserHasPermission(targetSteamID, "mailmanager.admin") and not permission.UserHasPermission(targetSteamID, "mailmanager.read") then
			local message = FormatMessage(self.Config.Messages.SenderNoPermission, { player = targetName })
			if sendtype == "console" then
				player:SendConsoleCommand("echo "..self.Config.Settings.Prefix.." "..message)
				else
				self:RustMessage(player, self.Config.Settings.Prefix.." "..message)
			end
			return
		end
	end
	local tplayerData = self:GetPlayerData(targetSteamID, false)
	if tplayerData and #tplayerData.Mail >= tonumber(self.Config.Settings.InboxLimit) then
		local message = FormatMessage(self.Config.Messages.SenderInboxFull, { player = targetName })
		if sendtype == "console" then
			player:SendConsoleCommand("echo "..self.Config.Settings.Prefix.." "..message)
			else
			self:RustMessage(player, self.Config.Settings.Prefix.." "..message)
		end
		return
	end
	self:newMail(player, targetSteamID, targetPlayer, subject, message)
	local message = FormatMessage(self.Config.Messages.Sent, { player = targetName, subject = subject })
	if self.Config.Settings.UsePermissions == "true" then
		if not permission.UserHasPermission(targetSteamID, "mailmanager.admin") then
			if not permission.UserHasPermission(targetSteamID, "mailmanager.send") then
				message = FormatMessage(self.Config.Messages.ReadOnlySent, { player = targetName, subject = subject })
			end
		end
	end
	if sendtype == "console" then
		player:SendConsoleCommand("echo "..self.Config.Settings.Prefix.." "..message)
		else
		self:RustMessage(player, self.Config.Settings.Prefix.." "..message)
	end
	if not permission.UserHasPermission(playerSteamID, "mailmanager.admin") then
		if not permission.UserHasPermission(playerSteamID, "mailmanager.nocd") then
			CoolDown[playerSteamID] = time.GetUnixTimestamp()
			local playerData = self:GetPlayerData(playerSteamID, false)
		end
		if playerData and #playerData.Mail >= tonumber(self.Config.Settings.InboxLimit) then
			if sendtype == "console" then
				player:SendConsoleCommand("echo "..self.Config.Settings.Prefix.." "..self.Config.Messages.SelfInboxFull)
				else
				self:RustMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.SelfInboxFull)
			end
		end
	end
	if sendtype == "reply" and ReplyHistory[playerSteamID] then
		ReplyHistory[playerSteamID] = nil
	end
	return
end

function PLUGIN:newMail(player, targetSteamID, targetPlayer, subject, message)
	local playerSteamID = rust.UserIDFromPlayer(player)
	local playerData = self:GetPlayerData(targetSteamID, true)
	playerData.id = playerData.id + 1
	playerData.new = true
	local timestamp = time.GetCurrentTime():ToLocalTime():ToString(self.Config.Settings.Timestamp)
	local newMail = {["timestamp"] = timestamp, ["sender"] = player.displayName, ["senderid"] = playerSteamID, ["subject"] = subject, ["message"] = message, ["new"] = true, ["id"] = playerData.id}
	table.insert(playerData.Mail, newMail)
	self:SaveDataFile()
	if targetPlayer:IsConnected() then
		local playerName = player.displayName
		local message = FormatMessage(self.Config.Messages.ReadOnlyNewMail, { player = playerName, subject = subject, id = playerData.id })
		if permission.UserHasPermission(targetSteamID, "mailmanager.admin") or permission.UserHasPermission(targetSteamID, "mailmanager.send") then
			message = FormatMessage(self.Config.Messages.NewMail, { player = playerName, subject = subject, id = playerData.id })
		end		
		self:RustMessage(targetPlayer, self.Config.Settings.Prefix.." "..message)
		self:ShowPopup(player, message)
	end
end

function PLUGIN:CheckCooldown(player, call)
	local playerSteamID = rust.UserIDFromPlayer(player)
	if not permission.UserHasPermission(playerSteamID, "mailmanager.admin") and not permission.UserHasPermission(playerSteamID, "mailmanager.nocd") then
		if CoolDown[playerSteamID] == nil then CoolDown[playerSteamID] = 1 end
		local Timestamp = time.GetUnixTimestamp()
		if Timestamp - CoolDown[playerSteamID] < tonumber(self.Config.Settings.CoolDown) then
			local remaining = tonumber(self.Config.Settings.CoolDown) - (Timestamp - CoolDown[playerSteamID])
			local message = FormatMessage(self.Config.Messages.CoolDown, { cooldown = remaining })
			if call == "console" then
				player:SendConsoleCommand("echo "..self.Config.Settings.Prefix.." "..message)
				else
				self:RustMessage(player, self.Config.Settings.Prefix.." "..message)
			end
			return false
		end
	end
	return true
end

function PLUGIN:StartReminderTimer()
	if self.Config.Settings.Reminder == "true" then
		timer.Repeat(tonumber(self.Config.Settings.ReminderInterval), 0, function()
			local players = global.BasePlayer.activePlayerList:GetEnumerator()
			while players:MoveNext() do
				local playerSteamID = rust.UserIDFromPlayer(players.Current)
				if not MailWarn[playerSteamID] or MailWarn[playerSteamID] == nil then MailWarn[playerSteamID] = "true" end
				if MailWarn[playerSteamID] == "true" then
					local playerData = self:GetPlayerData(playerSteamID, false)
					if players.Current:IsConnected() and playerData and #playerData.Mail >= 1 and playerData.new then
						if self.Config.Settings.UsePermissions == "true" then
							if permission.UserHasPermission(playerSteamID, "mailmanager.admin") or permission.UserHasPermission(playerSteamID, "mailmanager.read") then
								if permission.UserHasPermission(playerSteamID, "mailmanager.admin") or permission.UserHasPermission(playerSteamID, "mailmanager.send") then
									self:RustMessage(players.Current, self.Config.Settings.Prefix.." "..self.Config.Messages.Reminder)
									self:ShowPopup(player, self.Config.Messages.Reminder)
									else
									self:RustMessage(players.Current, self.Config.Settings.Prefix.." "..self.Config.Messages.ReadOnlyReminder)
									self:ShowPopup(player, self.Config.Messages.ReadOnlyReminder)
								end
							end
							else
							self:RustMessage(players.Current, self.Config.Settings.Prefix.." "..self.Config.Messages.Reminder)
							self:ShowPopup(player, self.Config.Messages.Reminder)
						end
					end
				end
			end
		end, self.Plugin)
	end
end

function PLUGIN:ShowPopup(player, message)
	if self.Config.Settings.EnablePopup == "true" and popupApi then
		popupApi:CallHook("CreatePopupNotification", message, player)
	end
end

function PLUGIN:RustMessage(player, message)
	rust.SendChatMessage(player, "<size="..tonumber(self.Config.Settings.MessageSize)..">"..message.."</size>")
end

function PLUGIN:SendHelpText(player)
	self:RustMessage(player, "<color=#ffd479>/mail</color> - Send, receive and manage in-game mail.")
end