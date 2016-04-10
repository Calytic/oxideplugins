PLUGIN.Title        = "Timed Events"
PLUGIN.Description  = "Allows automatic events to take place depending on server time."
PLUGIN.Author       = "InSaNe8472"
PLUGIN.Version      = V(1,1,1)
PLUGIN.ResourceID   = 1325

local ServerInitialized = false
local RepeatTimer = {}

function PLUGIN:Init()
	permission.RegisterPermission("event.admin", self.Plugin)
	command.AddChatCommand("te", self.Plugin, "cmdTEvent")
	self:LoadDefaultConfig()
end

function PLUGIN:LoadDefaultConfig()
	self.Config.Settings = self.Config.Settings or {}
	self.Config.Messages = self.Config.Messages or {}
	self.Config.Events = self.Config.Events or {}
	self.Config.Settings.Enabled = self.Config.Settings.Enabled or "true"
	self.Config.Settings.SayEnabled = self.Config.Settings.SayEnabled or "false"
	self.Config.Settings.ServerTime = self.Config.Settings.ServerTime or "true"
	self.Config.Settings.LocalTime = self.Config.Settings.LocalTime or "true"
	self.Config.Settings.Repeat = self.Config.Settings.Repeat or "true"
	self.Config.Settings.Prefix = self.Config.Settings.Prefix or "[<color=#cd422b>Timed Events</color>]"
	self.Config.Settings.SayPrefix = self.Config.Settings.SayPrefix or "[<color=#cd422b> SERVER </color>]"
	self.Config.Messages.NoPermission = self.Config.Messages.NoPermission or "You do not have permission to use this command."
	self.Config.Messages.WrongArgs = self.Config.Messages.WrongArgs or "Syntax error.  Use <color=#cd422b>/te</color> for help."
	self.Config.Messages.ChangedStatus = self.Config.Messages.ChangedStatus or "Timed events <color=#cd422b>{status}</color>."
	self.Config.Messages.TimeChangedStatus = self.Config.Messages.TimeChangedStatus or "Event group <color=#cd422b>{group}</color> now <color=#cd422b>{status}</color>."
	self.Config.Messages.ServerTime = self.Config.Messages.ServerTime or "Server time: <color=#cd422b>{stime}</color>"
	self.Config.Messages.InvalidFormat = self.Config.Messages.InvalidFormat or "Invalid event format <color=#cd422b>{eformat}</color> (<color=#cd422b>{group}</color>).  Use <color=#cd422b>/te</color> for help."
	self.Config.Messages.EventAdded = self.Config.Messages.EventAdded or "Event <color=#cd422b>{event}</color> with time <color=#cd422b>{etime}</color> (<color=#cd422b>{group}</color>) successfully added."
	self.Config.Messages.InvalidID = self.Config.Messages.InvalidID or "Invalid event ID <color=#cd422b>{id}</color> for <color=#cd422b>{group} group</color>.  Use <color=#cd422b>/te</color> for help."
	self.Config.Messages.EventDeleted = self.Config.Messages.EventDeleted or "Event with ID <color=#cd422b>{id}</color> (<color=#cd422b>{group}</color>) successfully deleted."
	self.Config.Messages.EventsCleared = self.Config.Messages.EventsCleared or "All <color=#cd422b>{group}</color> events successfully deleted."
	self.Config.Messages.NoEvents = self.Config.Messages.NoEvents or "No events found for <color=#cd422b>{group} group</color>."
	self.Config.Messages.EventRun = self.Config.Messages.EventRun or "Event with ID <color=#cd422b>{id}</color> (<color=#cd422b>{event}</color>) with original run time <color=#cd422b>{etime}</color> (<color=#cd422b>{group}</color>) successfully run."
	self.Config.Messages.PrintToConsole = self.Config.Messages.PrintToConsole or "Event list exceeds chat limit and has been sent to console (F1)."
	self.Config.Events.Local = self.Config.Events.Local or {
		"12.00.PM:say Time for an airdrop!",
		"12.00.PM:airdrop.massdrop 1",
		"01.00.PM:say Welcome to my server!  Enjoy your stay!",
		"02.00.PM:say Play fair!  No Cheating!",
		"03.00.PM:say Time for another airdrop!",
		"03.00.PM:airdrop.massdrop 1"
	}
	self.Config.Events.Server = self.Config.Events.Server or {
		"8:say Time for an airdrop!",
		"8:airdrop.massdrop 1",
		"9:say Welcome to my server!  Enjoy your stay!",
		"10:say Play fair!  No Cheating!",
		"11:say Time for another airdrop!",
		"11:airdrop.massdrop 1"
	}
	self.Config.Events.Repeat = self.Config.Events.Repeat or {
		"3600:say Saving server data.",
		"3600:server.save"
	}
	self:SaveConfig()
end

function PLUGIN:OnServerInitialized()
	ServerInitialized = true
    self:SetRepeat()
end

function PLUGIN:OnTick()
	if self.Config.Settings.Enabled == "true" then
		if self.Config.Settings.LocalTime == "true" and self.Config.Events.Local[1] then
			if LocalTime ~= time.GetCurrentTime():ToLocalTime():ToString("hh.mm.tt") then
				LocalTime = time.GetCurrentTime():ToLocalTime():ToString("hh.mm.tt")
				local i = 1
				while self.Config.Events.Local[i] do
					local EventTime, EventAction = self.Config.Events.Local[i]:match("([^:]+):([^:]+)")
					if string.lower(EventTime) == string.lower(LocalTime) then
						self:RunCommand(EventAction)
					end
					i = i + 1
				end
			end
		end
		if self.Config.Settings.ServerTime == "true" and self.Config.Events.Server[1] then
			if not ESky then ESky = global.TOD_Sky.get_Instance() end
			if ServerTime ~= tostring(ESky.Cycle.Hour):match"([^.]*).(.*)" then
				ServerTime = tostring(ESky.Cycle.Hour):match"([^.]*).(.*)"
				local i = 1
				while self.Config.Events.Server[i] do
					local EventTime, EventAction = self.Config.Events.Server[i]:match("([^:]+):([^:]+)")
					if tonumber(EventTime) == tonumber(ServerTime) then
						self:RunCommand(EventAction)
					end
					i = i + 1
				end
			end
		end
	end
end

function PLUGIN:SetRepeat()
	if self.Config.Settings.Repeat == "true" and self.Config.Events.Repeat[1] then
		local i = 1
		while self.Config.Events.Repeat[i] do
			if RepeatTimer[i] or RepeatTimer[i] ~= nil then
				RepeatTimer[i]:Destroy()
				RepeatTimer[i] = nil
			end
			local RepeatTime, RepeatCommand = self.Config.Events.Repeat[i]:match("([^:]+):([^:]+)")
			RepeatTimer[i] = timer.Repeat(tonumber(RepeatTime), 0, function() self:RunCommand(RepeatCommand) end, self.Plugin)
			i = i + 1
		end
		local i = 1
		while RepeatTimer[i] do
			if not self.Config.Events.Repeat[i] then
				RepeatTimer[i]:Destroy()
				RepeatTimer[i] = nil
			end
			i = i + 1
		end
		else
		local i = 1
		while RepeatTimer[i] do
			RepeatTimer[i]:Destroy()
			RepeatTimer[i] = nil
			i = i + 1
		end
	end
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

function PLUGIN:cmdTEvent(player, cmd, args)
	local playerSteamID = rust.UserIDFromPlayer(player)
	if not permission.UserHasPermission(playerSteamID, "event.admin") then
		rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.NoPermission)
		return
	end
	if args.Length == 0 then
		local AddFormat, ServerTime = "", ""
		if self.Config.Settings.LocalTime == "true" then ServerTime = "local" end
		if self.Config.Settings.ServerTime == "true" then
			if ServerTime ~= "" then
				ServerTime = ServerTime..", server"
				else
				ServerTime = "server"
			end
		end
		if self.Config.Settings.Repeat == "true" then
			if ServerTime ~= "" then
				ServerTime = ServerTime..", repeat"
				else
				ServerTime = "repeat"
			end
		end
		rust.SendChatMessage(player,
			self.Config.Settings.Prefix.." <color=#ffd479>/te toggle <system | local | server | repeat></color> - Enable or disable system or event group\n"..
			self.Config.Settings.Prefix.." <color=#ffd479>/te time</color> - View current server time\n"..
			self.Config.Settings.Prefix.." <color=#ffd479>/te list <l |s | r></color> - View current events\n"..
			self.Config.Settings.Prefix.." <color=#ffd479>/te add <l | s | r> <00.00.PM:Event | 0-24:Event | 1-86400:Event></color> - Add new event"
		)
		rust.SendChatMessage(player,
			self.Config.Settings.Prefix.." <color=#ffd479>/te delete <l | s | r> <id></color> - Delete existing event (list for id's)\n"..
			self.Config.Settings.Prefix.." <color=#ffd479>/te clear <l | s | r></color> - Delete all existing events (cannot be undone)\n"..
			self.Config.Settings.Prefix.." <color=#ffd479>/te run <l | s | r> <id></color> - Manually run an event ID (list for id's)\n"..
			self.Config.Settings.Prefix.." <color=#ffd479>Enabled</color>: "..self.Config.Settings.Enabled.."\n"..
			self.Config.Settings.Prefix.." <color=#ffd479>Groups</color>: "..ServerTime
		)
		return
		elseif args.Length > 0 then
		local func = args[0]
		if func ~= "toggle" and func ~= "time" and func ~= "list" and func ~= "add" and func ~= "delete" and func ~= "clear" and func ~= "run" then
			rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.WrongArgs)
			return
		end
		if func == "toggle" then
			if args.Length < 2 then
				rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.WrongArgs)
				return
			end
			local sfunc = args[1]
			if sfunc ~= "system" and sfunc ~= "local" and sfunc ~= "server" and sfunc ~= "repeat" then
				rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.WrongArgs)
				return
			end
			local message = ""
			if sfunc == "system" then
				if self.Config.Settings.Enabled == "true" then
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
			if sfunc == "local" then
				if self.Config.Settings.LocalTime == "true" then
					self.Config.Settings.LocalTime = "false"
					message = FormatMessage(self.Config.Messages.TimeChangedStatus, { group = "local", status = "disabled" })
					else
					self.Config.Settings.LocalTime = "true"
					message = FormatMessage(self.Config.Messages.TimeChangedStatus, { group = "local", status = "enabled" })
				end
				self:SaveConfig()
				rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..message)
				return
			end
			if sfunc == "server" then
				if self.Config.Settings.ServerTime == "true" then
					self.Config.Settings.ServerTime = "false"
					message = FormatMessage(self.Config.Messages.TimeChangedStatus, { group = "server", status = "disabled" })
					else
					self.Config.Settings.ServerTime = "true"
					message = FormatMessage(self.Config.Messages.TimeChangedStatus, { group = "server", status = "enabled" })
				end
				self:SaveConfig()
				rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..message)
				return
			end
			if sfunc == "repeat" then
				if self.Config.Settings.Repeat == "true" then
					self.Config.Settings.Repeat = "false"
					message = FormatMessage(self.Config.Messages.TimeChangedStatus, { group = "repeat", status = "disabled" })
					else
					self.Config.Settings.Repeat = "true"
					message = FormatMessage(self.Config.Messages.TimeChangedStatus, { group = "repeat", status = "enabled" })
				end
				self:SaveConfig()
				rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..message)
				return
			end
		end
		if func == "time" then
			local LocalTime = time.GetCurrentTime():ToLocalTime():ToString("hh:mm tt")
			local message = FormatMessage(self.Config.Messages.ServerTime, { stime = LocalTime.." (Local)" })
			rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..message)
			local LocalTime = global.TOD_Sky.get_Instance()
			local message = FormatMessage(self.Config.Messages.ServerTime, { stime = tostring(LocalTime.Cycle.Hour):match"([^.]*).(.*)".." (Server)" })
			rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..message)
			return
		end
		local sfunc = ""
		if args.Length >= 2 then sfunc = args[1] end
		if sfunc ~= "l" and sfunc ~= "s" and sfunc ~= "r" then
			rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.WrongArgs)
			return
		end
		if func == "list" then
			local Group, GroupList = "", ""
			if sfunc == "l" then
				Group = "Local"
				GroupList = self.Config.Events.Local
			end
			if sfunc == "s" then
				Group = "Server"
				GroupList = self.Config.Events.Server
			end
			if sfunc == "r" then
				Group = "Repeat"
				GroupList = self.Config.Events.Repeat
			end
			if not GroupList[1] then
				local message = FormatMessage(self.Config.Messages.NoEvents, { group = string.lower(Group) })
				rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..message)
				return
			end
			local console = false
			local i, List = 1, ""
			while GroupList[i] do
				if not console and i > 5 then console = true end
				local EventTime, EventAction = GroupList[i]:match("([^:]+):([^:]+)")
				List = List.."<color=#cd422b>"..i..": </color><color=#ffd479>"..EventTime.."</color> - "..EventAction.."\n"
				i = i + 1
			end
			if not console then
				rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..Group.." events:\n"..string.sub(List, 1, -3))
				else
				rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.PrintToConsole)
				player:SendConsoleCommand("echo "..Group.." events:\n"..string.sub(List, 1, -3))
			end
			return
		end
		if func == "add" then
			if args.Length < 3 then
				rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.WrongArgs)
				return
			end
			local NewEvent = args[2]
			local EventArgs = ""
			if args.Length >= 4 then
				local args = self:TableMessage(args)
				local i = 4
				while args[i] do
					EventArgs = EventArgs..args[i].." "
					i = i + 1
				end
				EventArgs = string.sub(EventArgs, 1, -2)
				end
				local message = ""
				if sfunc == "l" then
				if string.sub(NewEvent, 3, 3) ~= "." or string.sub(NewEvent, 6, 6) ~= "." or string.sub(NewEvent, 9, 9) ~= ":" or not tonumber(string.sub(NewEvent, 1, 2)) or
				not tonumber(string.sub(NewEvent, 4, 5)) or string.sub(NewEvent, 10, 10) == "" then
				local message = FormatMessage(self.Config.Messages.InvalidFormat, { eformat = NewEvent, group = "local" })
				rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..message)
				return
				end
				if string.upper(string.sub(NewEvent, 7, 8)) ~= "PM" then
				if string.upper(string.sub(NewEvent, 7, 8)) ~= "AM" then
				local message = FormatMessage(self.Config.Messages.InvalidFormat, { eformat = NewEvent, group = "local" })
				rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..message)
				return
				end
				end
				local EventTime, EventAction = NewEvent:match("([^:]+):([^:]+)")
				table.insert(self.Config.Events.Local, string.upper(EventTime)..":"..EventAction.." "..EventArgs)
				message = FormatMessage(self.Config.Messages.EventAdded, { event = EventAction.." "..EventArgs, etime = string.upper(EventTime), group = "local" })
				end
				if sfunc == "s" then
				local EventTime, EventAction = NewEvent:match("([^:]+):([^:]+)")
				if not tonumber(EventTime) or tonumber(EventTime) < 0 or tonumber(EventTime) > 24 then
				local message = FormatMessage(self.Config.Messages.InvalidFormat, { eformat = NewEvent, group = "server" })
				rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..message)
				return
				end
				if string.len(EventTime) == 1 then
				if string.sub(NewEvent, 2, 2) ~= ":" or string.sub(NewEvent, 3, 3) == "" then
				local message = FormatMessage(self.Config.Messages.InvalidFormat, { eformat = NewEvent, group = "server" })
				rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..message)
				return
				end
				end
				if string.len(EventTime) == 2 then
				if string.sub(NewEvent, 3, 3) ~= ":" or string.sub(NewEvent, 4, 4) == "" then
				local message = FormatMessage(self.Config.Messages.InvalidFormat, { eformat = NewEvent, group = "server" })
				rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..message)
				return
				end
				end
				local EventTime, EventAction = NewEvent:match("([^:]+):([^:]+)")
				table.insert(self.Config.Events.Server, EventTime..":"..EventAction.." "..EventArgs)
				message = FormatMessage(self.Config.Messages.EventAdded, { event = EventAction.." "..EventArgs, etime = string.upper(EventTime), group = "server" })
				end
				if sfunc == "r" then
				local EventTime, EventAction = NewEvent:match("([^:]+):([^:]+)")
				if not tonumber(EventTime) or tonumber(EventTime) < 0 or tonumber(EventTime) > 86400 then
				local message = FormatMessage(self.Config.Messages.InvalidFormat, { eformat = NewEvent, group = "repeat" })
				rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..message)
				return
				end
				local EventTime, EventAction = NewEvent:match("([^:]+):([^:]+)")
				table.insert(self.Config.Events.Repeat, EventTime..":"..EventAction.." "..EventArgs)
				message = FormatMessage(self.Config.Messages.EventAdded, { event = EventAction.." "..EventArgs, etime = string.upper(EventTime), group = "repeat" })
				end
				self:SaveConfig()
				if sfunc == "r" then self:SetRepeat() end
				rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..message)
				return
				end
				if func == "delete" then
				if args.Length < 3 or not tonumber(args[2]) then
				rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.WrongArgs)
				return
				end
				local DelEvent = tonumber(args[2])
				local message = ""
				if sfunc == "l" then
				if not self.Config.Events.Local[DelEvent] then
				message = FormatMessage(self.Config.Messages.InvalidID, { id = DelEvent, group = "local" })
				rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..message)
				return
				end
				table.remove(self.Config.Events.Local, DelEvent)
				message = FormatMessage(self.Config.Messages.EventDeleted, { id = DelEvent, group = "local" })
				end
				if sfunc == "s" then
				if not self.Config.Events.Server[DelEvent] then
				message = FormatMessage(self.Config.Messages.InvalidID, { id = DelEvent, group = "server" })
				rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..message)
				return
				end
				table.remove(self.Config.Events.Server, DelEvent)
				message = FormatMessage(self.Config.Messages.EventDeleted, { id = DelEvent, group = "server" })
				end
				if sfunc == "r" then
				if not self.Config.Events.Repeat[DelEvent] then
				message = FormatMessage(self.Config.Messages.InvalidID, { id = DelEvent, group = "repeat" })
				rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..message)
				return
				end
				table.remove(self.Config.Events.Repeat, DelEvent)
				message = FormatMessage(self.Config.Messages.EventDeleted, { id = DelEvent, group = "repeat" })
				end
				self:SaveConfig()
				if sfunc == "r" then self:SetRepeat() end
				rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..message)
				return
				end
				if func == "clear" then
				local message = ""
				if sfunc == "l" then
				self.Config.Events.Local = {}
				message = FormatMessage(self.Config.Messages.EventsCleared, { group = "local" })
				end
				if sfunc == "s" then
				self.Config.Events.Server = {}
				message = FormatMessage(self.Config.Messages.EventsCleared, { group = "server" })
				end
				if sfunc == "r" then
				self.Config.Events.Repeat = {}
				message = FormatMessage(self.Config.Messages.EventsCleared, { group = "repeat" })
				end
				self:SaveConfig()
				if sfunc == "r" then self:SetRepeat() end
				rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..message)
				return
				end
				if func == "run" then
				if args.Length < 3 or not tonumber(args[2]) then
				rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..self.Config.Messages.WrongArgs)
				return
				end
				local RunEvent = tonumber(args[2])
				local message = ""
				if sfunc == "l" then
				if not self.Config.Events.Local[RunEvent] then
				local message = FormatMessage(self.Config.Messages.InvalidID, { id = RunEvent, group = "local" })
				rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..message)
				return
				end
				local EventTime, EventAction = self.Config.Events.Local[RunEvent]:match("([^:]+):([^:]+)")
				self:RunCommand(EventAction)
				message = FormatMessage(self.Config.Messages.EventRun, { id = RunEvent, event = EventAction, etime = EventTime, group = "local" })
				end
				if sfunc == "s" then
				if not self.Config.Events.Server[RunEvent] then
				local message = FormatMessage(self.Config.Messages.InvalidID, { id = RunEvent, group = "server" })
				rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..message)
				return
				end
				local EventTime, EventAction = self.Config.Events.Server[RunEvent]:match("([^:]+):([^:]+)")
				self:RunCommand(EventAction)
				message = FormatMessage(self.Config.Messages.EventRun, { id = RunEvent, event = EventAction, etime = EventTime, group = "server" })
				end
				if sfunc == "r" then
				if not self.Config.Events.Repeat[RunEvent] then
				local message = FormatMessage(self.Config.Messages.InvalidID, { id = RunEvent, group = "repeat" })
				rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..message)
				return
				end
				local EventTime, EventAction = self.Config.Events.Repeat[RunEvent]:match("([^:]+):([^:]+)")
				self:RunCommand(EventAction)
				message = FormatMessage(self.Config.Messages.EventRun, { id = RunEvent, event = EventAction, etime = EventTime, group = "repeat" })
				end
				rust.SendChatMessage(player, self.Config.Settings.Prefix.." "..message)
				return
				end
				return
				end
				end
				
				function PLUGIN:RunCommand(EventAction)
				if ServerInitialized then
				local prefix = self.Config.Settings.Prefix:gsub(" ", "")
				local message = prefix.." "..EventAction
				if self.Config.Settings.SayEnabled == "true" then
				if string.sub(EventAction, 1, 4) == "say " then
				rust.BroadcastChat(self.Config.Settings.SayPrefix.." "..string.sub(EventAction, 5))
				message = prefix.." "..string.sub(EventAction, 5)
				else
				rust.RunServerCommand(EventAction)
				end
				else
				rust.RunServerCommand(EventAction)
				end
				message = message:gsub("<color=%p*%w*>", "")
				message = message:gsub("</color>", "")
				print(message)
				end
				end
				
				function PLUGIN:SendHelpText(player)
				local playerSteamID = rust.UserIDFromPlayer(player)
				if permission.UserHasPermission(playerSteamID, "event.admin") then
				rust.SendChatMessage(player, "<color=#ffd479>/te</color> - View, add, delete and run server events")
				end
				end								