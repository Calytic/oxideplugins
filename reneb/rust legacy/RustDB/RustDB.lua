PLUGIN.Name = "RustDB"
PLUGIN.Title = "RustDB"
PLUGIN.Version = V(1, 0, 0)
PLUGIN.Description = "Use RustDB to protect your server and share your bans"
PLUGIN.Author = "Reneb"
PLUGIN.HasConfig = true
 
function PLUGIN:Init()
	command.AddChatCommand( "rustdb", self.Plugin, "cmdRustDB" )
	ServerInitialized = false
end
 
function urlencode(str)
   if (str) then
      str = string.gsub (str, "\n", "\r\n")
      str = string.gsub (str, "([^%w ])",
         function (c) return string.format ("%%%02X", string.byte(c)) end)
      str = string.gsub (str, " ", "+")
   end
   return str    
end



function PLUGIN:OnServerInitialized()
	ServerInitialized = true
end

function PLUGIN:cmdRustDB(player,cmd,args)
	if(not player:CanAdmin()) then
		rust.SendChatMessage(player,"[RustDB] "..self.Config.Messages.NotAllowed)
		return
	end
	if(self.Config.RustDB.serverOwner == "XXXXXXXXXXXXXXXXX") then
		rust.SendChatMessage(player,"[RustDB] "..self.Config.Messages.serverOwnerIsXXX)
		return
	end
	if(string.len(self.Config.RustDB.serverOwner) ~= 17 or tonumber(self.Config.RustDB.serverOwner) == nil) then
		rust.SendChatMessage(player,"[RustDB] "..self.Config.Messages.serverOwnerIsWrong)
		return
	end
	if self.Config.RustDB.allowRustDBtoShowOwner then
	 show = "1"
	else
	 show = "0"
	end
	requestData = "action=owners&steamid="..tostring(self.Config.RustDB.serverOwner).."&reason="..tostring(show).."&ip="..tostring(self.Config.serverIP).."&port="..tostring(self.Config.serverPort)
	local r = webrequests.EnqueueGet("http://rustdb.net/api2.php?"..requestData, function(code, response)
		if(response == nil) then
			rust.SendChatMessage(player,"[RustDB] "..self.Config.Messages.tryAgain)
			return
		end
		print(response)
		rust.SendChatMessage(player,"[RustDB] "..self.Config.Messages.lookIntoConsole)
	end, self.Plugin)
end

function PLUGIN:sendToAdmins(msg)
	local allPlayers = global.PlayerClient.get_All()
	for i = 0 , allPlayers.Count , 1 do
		if(allPlayers[i].netUser:CanAdmin()) then
       		rust.SendChatMessage( allPlayers[i].netUser, "[RustDB] "..msg )
		end
    end
end
function PLUGIN:rustDBAnswers(response)
	if(response == nil) then return end
	if(self.Config.showRustDBAnswers) then
		print(response)
	end
end
function PLUGIN:RustDBBan( sourcePlayer, name, steamID, reason )
	if(name == nil or steamID == nil or reason == nil) then
		return 
	end
	if(name == "" or string.len(steamID) ~= 17 or reason == "") then
		return
	end
	if(sourcePlayer and sourcePlayer ~= nil) then
		reason = reason .. "(" .. sourcePlayer.displayName .. ")"
	else
		reason = reason .. "(Console)"
	end
	plugins.CallHook("cmdBan",steamID, name, reason)
end
function PLUGIN:cmdBan(steamID, name, reason)
	requestData = "action=ban&steamid="..urlencode(tostring(steamID)).."&name="..urlencode(tostring(name)).."&reason="..urlencode(tostring(reason)).."&ip="..tostring(self.Config.serverIP).."&port="..tostring(self.Config.serverPort)
	local r = webrequests.EnqueueGet("http://rustdb.net/api2.php?"..requestData, function(code, response)
		self:rustDBAnswers(response)
	end, self.Plugin)
end
function PLUGIN:RustDBUnban( steamID )
	if(steamID == nil) then
		return 
	end
	if(string.len(steamID) ~= 17) then
		return
	end
	plugins.CallHook("cmdUnban",steamID)
end
function PLUGIN:cmdUnban(steamID)
	requestData = "action=unban&steamid="..urlencode(tostring(steamID)).."&ip="..tostring(self.Config.serverIP).."&port="..tostring(self.Config.serverPort)
	local r = webrequests.EnqueueGet("http://rustdb.net/api2.php?"..requestData, function(code, response)
		self:rustDBAnswers(response)
	end, self.Plugin)
end
function PLUGIN:BuildServerTags(tags) tags:Add("rustdb") end
function PLUGIN:OnPlayerConnected( player )
	if(self.Config.onJoin.broadcastBans or self.Config.onJoin.autoKick.activated) then
		requestData = "action=banned&steamid="..rust.UserIDFromPlayer(player).."&ip="..tostring(self.Config.serverIP).."&port="..tostring(self.Config.serverPort)
		local r = webrequests.EnqueueGet("http://rustdb.net/api2.php?"..requestData, function(code, response)
			if(response == nil) then return end
			if(string.sub(response,1,1) ~= nil and string.sub(response,1,1) ~= "0" and string.sub(response,1,1) ~= "") then
				nbans = string.sub(response,1,string.find(response, "%s"))
				if(nbans ~= nil and nbans ~= "" and tonumber(nbans) ~= nil) then
					if(tonumber(nbans) > 0) then
						local reason = string.sub(response,(string.find(response, "%s")+1))
						if(string.find(reason, "<")) then reason = string.sub(reason,1,(string.find(reason, "<") - 1)) end
						if(self.Config.onJoin.autoKick.activated and tonumber(nbans) >= self.Config.onJoin.autoKick.minBansRequired) then
							rust.BroadcastChat("[RustDB] " .. player.displayName .. " has too many bans (" .. nbans ..") on RustDB (" .. reason .. ")! Connection has been rejected")
							rust.SendChatMessage( player, "[RustDB] You have been a badboy and have too much bans on RustDB!!")
							player:Kick(global.NetError.Facepunch_Kick_RCON,true)
						elseif(self.Config.onJoin.broadcastBanned) then
							if(tonumber(nbans) > 1) then
								rust.BroadcastChat("[RustDB] " .. player.displayName .. " has " .. nbans .. " ban entries in RustDB (" .. reason .. ")")
							else
								rust.BroadcastChat("[RustDB] " .. player.displayName .. " has " .. nbans .. " ban entry in RustDB (" .. reason .. ")")
							end
						elseif(self.Config.onJoin.sendToAdminsBanned) then
							self:sendToAdmins("" .. player.displayName .. " has " .. nbans .. " ban entry in RustDB (" .. reason .. ")")
						end
						if(self.Config.onJoin.logBanned) then
							print("[RustDB] " .. player.displayName .. " has " .. nbans .. " ban entry in RustDB. (" .. reason .. ")")
						end
					end
				end
			end
			self:rustDBAnswers(response)
		end, self.Plugin)  
	end
end

function PLUGIN:OnRunCommand(arg, wantsfeedback)
    -- Sanity checks
    if (arg == nil) then return end
	if(arg.Args == nil) then return end
	if (arg.Function == nil) then return end
	if(not ServerInitialized) then return end
	if(arg.Function == "ban" or arg.Function == "banid") then
		if(arg.Args.Length < 2) then arg:ReplyWith(self.Config.Messages.youMustSpecifyAReason) return end
		player = nil
    	if(arg.argUser) then
    		player = arg.argUser
   		end
   		targetSteam = false
   		targetReason = false
   		targetName = false
   		targetPlayer, err = self:FindPlayer(tostring(arg.Args[0]))
		if(not targetPlayer) then
			if(tonumber(arg.Args[0]) == nil or string.len(arg.Args[0]) ~= 17) then
				arg:ReplyWith(err)
				return false
			else
				targetSteam = arg.Args[0]
			end 
		else
			targetName = targetPlayer.displayName
			targetSteam = rust.UserIDFromPlayer(targetPlayer)
		end
		if(arg.Args.Length > 2) then
			targetReason = arg.Args[2]
			targetName = arg.Args[1]
		else
			targetReason = arg.Args[1]
		end
		self:RustDBBan( player, targetName, targetSteam, targetReason )
	elseif(arg.Function == "removeid") then
		if(tonumber(arg.Args[0]) == nil or string.len(arg.Args[0]) ~= 17) then
			arg:ReplyWith(self.Config.Messages.serverOwnerIsWrong)
			return false
		end
		self:RustDBUnban( arg.Args[0] )
	end
end

function PLUGIN:LoadDefaultConfig()
	self.Config.onJoin = {}
	self.Config.onJoin.broadcastBanned = true
	self.Config.onJoin.logBanned = false
	self.Config.onJoin.sendToAdminsBanned = false
	self.Config.onJoin.autoKick = {}
	self.Config.onJoin.autoKick.activated = true
	self.Config.onJoin.autoKick.minBansRequired = 2
	
	self.Config.RustDB = {}
	self.Config.RustDB.serverOwner = "XXXXXXXXXXXXXXXXX"
	self.Config.RustDB.allowRustDBtoShowOwner = true
	
	self.Config.Messages = {}
	self.Config.Messages.NotAllowed = "You are not allowed to use this command"
	self.Config.Messages.serverOwnerIsXXX = "You need to set the serverOwner SteamID64 in the configs"
	self.Config.Messages.serverOwnerIsWrong = "You didn't set a proper SteamID64."
	self.Config.Messages.tryAgain = "Couldn't contact RustDB, please try again"
	self.Config.Messages.lookIntoConsole = "Please look into your server console to see RustDB's answer"
	self.Config.Messages.youMustSpecifyAReason = "You must specify a reason to add a ban on RustDB"
	self.Config.Messages.PlayerDoesntExist  = "No players found"
	self.Config.Messages.MultiplePlayersFound = "Multiple players found"
	
	self.Config.showRustDBAnswers = true
	self.Config.serverIP = ""
	self.Config.serverPort = ""
end

function PLUGIN:FindPlayer( target )
	local targetplayer = rust.FindPlayer(target)
	if(targetplayer == nil) then return false, self.Config.Messages.PlayerDoesntExist end
	return targetplayer
end

