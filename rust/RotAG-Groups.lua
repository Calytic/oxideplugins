PLUGIN.Name = "RotAG-Groups"
PLUGIN.Title = "RotAG-Groups"
PLUGIN.Version = V(1, 7, 3)
PLUGIN.Description = "Groups System plugin for Oxide 2.0"
PLUGIN.Author = "TheRotAG"
PLUGIN.HasConfig = true

----------------------------------------- LOCALS -----------------------------------------
local adm, cmds, msgs, sets, API, AntiSpam, sys, ginfomsg, ccolor  = {}, {}, {}, {}, {}, {}, {}, {}, ""
local timestamp = time.GetUnixTimestamp()

local function QuoteSafe(string)
    return UnityEngine.StringExtensions.QuoteSafe(string)
end

local function HasAcces(player)
	return player:GetComponent("BaseNetworkable").net.connection.authLevel >= adm.Auth_LVL
end

------------------------------------------------------------------------------------------

----------------------------------- OTHER FUNCTIONS --------------------------------------
function PLUGIN:parseHelpMsgs(player, helpmsg)
	for i=1,#helpmsg do
		self:ChatMessage(player, helpmsg[i])
	end
end

function PLUGIN:GetAllPlayers()
    itPlayerList = global.BasePlayer.activePlayerList:GetEnumerator()
    playerList = {}
    while itPlayerList:MoveNext() do
        table.insert(playerList,itPlayerList.Current)
    end
    return playerList
end

function PLUGIN:ChatMessage(targetPlayer, msg)
	targetPlayer:SendConsoleCommand("chat.add", 0, "<color="..ccolor..">".. msgs.ChatName.."</color> "..msg)
end

------------------------------------------------------------------------------------------

-- for future use in case of need
-- ++++++++++++++++++++++++++++++
function GetGroupsAPI() 
	return API
end

function API:GetUserDataFromPlayer(player)
	return self:GetUserData(rust.UserIDFromPlayer(player))
end

function API:GetUserData(steamid)
	local data = USERS[steamid]
	return data
end 
-- ++++++++++++++++++++++++++++++

function PLUGIN:ArgsToTable(args, src)
    local argsTbl = {}
    if src == "chat" then
        local length = args.Length
        for i = 0, length - 1, 1 do
            argsTbl[i + 1] = args[i]
        end
        return argsTbl
    end
    if src == "console" then
        local i = 1
        while args:HasArgs(i) do
            argsTbl[i] = args:GetString(i - 1)
            i = i + 1
        end
        return argsTbl
    end
    return argsTbl
end

function PLUGIN:Init()
	
	self:LoadDefaultConfig()
	
	USERS = datafile.GetDataTable( "Groups" ) or {}
	adm.Auth_LVL = self.Config.Settings.Admin_LvL or 2
	self.Config.Settings.Admin_LvL = adm.Auth_LVL
	
	cmds = self.Config.Commands
	msgs = self.Config.Messages
	sets = self.Config.Settings
	self:LoadSavedData()
    self:SaveConfig()
	
	if cmds.groupHelp ~= "" then command.AddChatCommand(cmds.groupHelp, self.Plugin, "C_groupHelp") end
	if cmds.Group ~= "" then command.AddChatCommand(cmds.Group, self.Plugin, "C_Group") end
	if cmds.GroupChat ~= "" then command.AddChatCommand(cmds.GroupChat, self.Plugin, "C_GroupChat") end
	if cmds.GroupRemove ~= "" then
		command.AddChatCommand(cmds.GroupRemove, self.Plugin, "C_GroupRemove")
		command.AddConsoleCommand( "groups.remove", self.Plugin, "CC_GroupRemove" )
	end
	if cmds.GroupFF ~= "" then command.AddChatCommand(cmds.GroupFF, self.Plugin, "C_GroupFriendlyFire") end
	
	ccolor = sets.ChatColor
	
	print("Group Data loaded! v"..tostring( self.Version ) )
end

function PLUGIN:LoadDefaultConfig()
	
	self.Config.Version = self.Config.Version or "1.7.2."
	
	if self.Config.Version and self.Config.Version ~= "1.7.2" then
		self.Config.Settings = nil
		self.Config.Settings = {}
	end
	
	self.Config.Settings = self.Config.Settings or {}
	self.Config.Settings.GroupDamage = self.Config.Settings.GroupDamage or false
	self.Config.Settings.GroupNameLength = self.Config.Settings.GroupNameLength or 6
	self.Config.Settings.ChatColor = self.Config.Settings.ChatColor or "#3afb0fff"
	self.Config.Settings.GroupChatColor = self.Config.Settings.GroupChatColor or "#008000ff"
	
	--Player cmds:
	self.Config.Commands = self.Config.Commands or {}
	self.Config.Commands.groupHelp = self.Config.Commands.groupHelp or "grouphelp"
	--Group cmds:
    self.Config.Commands.Group = self.Config.Commands.Group or "group"
	self.Config.Commands.GroupChat = self.Config.Commands.GroupChat or "gc"
	--Admin cmds:
	self.Config.Commands.GroupRemove = self.Config.Commands.GroupRemove or "gr"
	self.Config.Commands.GroupFF = self.Config.Commands.GroupFF or "gff"
	--Help:
	self.Config.Messages = self.Config.Messages or {}
	self.Config.Messages.Help = self.Config.Messages.Help or {"Group System by TheRotAG","Use /grouphelp to see all Group commands."}
	self.Config.Messages.ChatName = self.Config.Messages.ChatName or "[Group System]"
	self.Config.Messages.GChatName = self.Config.Messages.GChatName or "[Group Chat]"
	self.Config.Messages.GroupHelp = self.Config.Messages.GroupHelp or {"----------------------------------------------------------------","RPG Group Help","----------------------------------------------------------------","Use /group create \"Group Name\"  -- to create a group","Use /group invite \"Player Name\" -- invite a player to your group *Only the leader can do it*","Use /group accept -- to accept the invitation to join a group","Use /group leave -- to leave you current group **","Use /group kick \"Player Name\" -- to kick the desired player from your group","Use /group leader \"Player Name\" -- to give the desired player Leader rights","Use /group list -- to see all groups","Use /group -- to check you group name and its members","Use /gc \"message\" -- to send a message to you group","----------------------------------------------------------------","** If you are the leader of the group, by using /group leave the group will be deleted forever"}
	self.Config.Messages.AdminHelp = self.Config.Messages.AdminHelp or {"----------------------------------------------------------------","----------------------------Admin Help--------------------------","Use /gr groupname -- to remove that group","----------------------------------------------------------------"}
	self.Config.Messages.Group = self.Config.Messages.Group or "Group Name: "
	self.Config.Messages.Leader = self.Config.Messages.Leader or "Leaders: "
	self.Config.Messages.Members = self.Config.Messages.Members or "Members: "
	self.Config.Messages.InTheGroup = self.Config.Messages.InTheGroup or "You are already in this group!"
	self.Config.Messages.GNameError = self.Config.Messages.GNameError or "The group name can't have more than 6 characters"
	self.Config.Messages.NoPData = self.Config.Messages.NoPData or "No player data found!"
	self.Config.Messages.UrentInGroup = self.Config.Messages.UrentInGroup or "You aren't in a group!"
	self.Config.Messages.WrongHelp = self.Config.Messages.WrongHelp or "Wrong Command! Use /grouphelp!"
	self.Config.Messages.WrongCmd = self.Config.Messages.WrongCmd or "Use /group create \"Group Name\" to create a group"
	self.Config.Messages.InAGroup = self.Config.Messages.InAGroup or "You already have a group called: "
	self.Config.Messages.GroupExists = self.Config.Messages.GroupExists or "This group already exists!"
	self.Config.Messages.GDataError = self.Config.Messages.GDataError or "There was an error while looking for the Group Data! Report it to an ADMIN"
	self.Config.Messages.GroupCreated = self.Config.Messages.GroupCreated or "You created the group "
	self.Config.Messages.LeftGroup = self.Config.Messages.LeftGroup or "You left the group "
	self.Config.Messages.NoPlayer = self.Config.Messages.NoPlayer or "No Player Found!"
	self.Config.Messages.MultiplePlayers = self.Config.Messages.MultiplePlayers or "Multiple players found with that info!"
	self.Config.Messages.NoPtoInvite = self.Config.Messages.NoPtoInvite or "You need to input the name from whose you want to invite"
	self.Config.Messages.InviteToJoin = self.Config.Messages.InviteToJoin or " has been invited to join your group"
	self.Config.Messages.Invited = self.Config.Messages.Invited or "You were invited to the group %s, to join the group use /group accept"
	self.Config.Messages.NotInvited = self.Config.Messages.NotInvited or "You doesn't have any invite pending"
	self.Config.Messages.ReportAdmin = self.Config.Messages.ReportAdmin or "Error encountered! Report it to an admin!"
	self.Config.Messages.YouJoined = self.Config.Messages.YouJoined or "You joined the group "
	self.Config.Messages.Joined = self.Config.Messages.Joined or " joined the group!"
	self.Config.Messages.UKicked = self.Config.Messages.UKicked or "You kicked %s from your group!"
	self.Config.Messages.GotKicked = self.Config.Messages.GotKicked or "You got kicked from your group!"
	self.Config.Messages.NoGroups = self.Config.Messages.NoGroups or "There are no groups in the server at the moment"
	self.Config.Messages.WhoKick = self.Config.Messages.WhoKick or "You need to inform whose you want to kick from your group."
	self.Config.Messages.GroupsInServer = self.Config.Messages.GroupsInServer or "\n Groups in the server at the moment: \n Group: %s \n\n Leaders: \n %s \n ------------------------------------------------------"
	self.Config.Messages.NoGroup = self.Config.Messages.NoGroup or "Group not found!"
	self.Config.Messages.RemovedGroup = self.Config.Messages.RemovedGroup or "You removed the group "
	self.Config.Messages.FF = self.Config.Messages.FF or "Friendly Fire! You hit "
	self.Config.Messages.GroupFFoff = self.Config.Messages.GroupFFoff or "Group Friendly Fire turned OFF"
	self.Config.Messages.GroupFFon = self.Config.Messages.GroupFFon or "Group Friendly Fire turned ON"
	self.Config.Messages.LeaderDelGroup = self.Config.Messages.LeaderDelGroup or "Your group has been disbanded by your Leader..."
	self.Config.Messages.HasBeenKicked = self.Config.Messages.HasBeenKicked or " has been kicked from your group!"
	self.Config.Messages.RemovedAllGroups = self.Config.Messages.RemovedAllGroups or "You've WIPED you Group Data!"
	self.Config.Messages.LeaderAdded = self.Config.Messages.LeaderAdded or "%s is now a Leader from your group"
	self.Config.Messages.NoLeaderToAdd = self.Config.Messages.NoLeaderToAdd or "Use /group leader \"Player Name\" -- to add the desired player as a leader of your group"
	self.Config.Messages.LeaderToAddNotInGroup = self.Config.Messages.LeaderToAddNotInGroup or "The player name is invalid or it isn't a member from your group"
	self.Config.Messages.InviteSyntax = self.Config.Messages.InviteSyntax or "Use /group invite \"Player Name\" -- invite a player to your group *Only the leader can do it*"
	self.Config.Messages.AlreadyLeader = self.Config.Messages.AlreadyLeader or "This player is already one of your leaders"
	self.Config.Messages.AlreadyInGroup = self.Config.Messages.AlreadyInGroup or "This player is already in your group"
	self.Config.Messages.MustBeLeader = self.Config.Messages.MustBeLeader or "You must be the leader of the group to invite someone"
	self.Config.Messages.KickSyntax = self.Config.Messages.KickSyntax or "Use /group kick \"Player Name\" -- to kick the desired player from your group"
	self.Config.Messages.LeaderSyntax = self.Config.Messages.LeaderSyntax or "Use /group leader \"Player Name\" -- to give the desired player Leader rights"
end

function PLUGIN:LoadSavedData()
    GroupData           	= datafile.GetDataTable( "Groups" )
    GroupData           	= GroupData or {}
	GroupData.PlayerData	= GroupData.PlayerData or {}
	GroupData.Groups		= GroupData.Groups or {}
	self:SaveData()
end

function PLUGIN:SaveData()  
    datafile.SaveDataTable( "Groups" )
end

function PLUGIN:SendHelpText(player)
    for i=1,#msgs.Help do
		self:ChatMessage(player, msgs.Help[i])
	end
	if HasAcces(player) then
		for i=1,#msgs.AdminHelp do
			self:ChatMessage( player, msgs.AdminHelp[i] )
		end
	end
end


-- HELPs
function PLUGIN:C_groupHelp( player, cmd, args )
	self:parseHelpMsgs( player, msgs.GroupHelp )
end


--Â§Â§Â§Â§Â§Â§Â§Â§Â§Â§Â§Â§Â§Â§Â§Â§Â§Â§Â§Â§Â§Â§Â§Â§Â§Â§Â§Â§Â§Â§Â§Â§Â§Â§Â§Â§Â§Â§Â§Â§Â§Â§Â§Â§Â§Â§Â§Â§Â§Â§Â§Â§Â§Â§Â§Â§Â§Â§Â§Â§Â§Â§Â§Â§Â§Â§Â§Â§Â§Â§Â§Â§Â§Â§Â§Â§Â§Â§Â§Â§Â§Â§Â§Â§Â§Â§Â§Â§Â§Â§Â§Â§Â§Â§Â§Â§Â§Â§Â§Â§Â§Â§Â§Â§Â§Â§Â§Â§Â§Â§
--Group Cmds

function PLUGIN:C_Group( player, cmd, args )
	local args = self:ArgsToTable(args, "chat")
	local pID = rust.UserIDFromPlayer( player )
	local pData = GroupData.PlayerData[pID]
	local gData = GroupData.Groups
	local func, target = args[1], args[2]
	if not func then
		local gName = tostring(pData.Group)
		if not pData then
			self:ChatMessage( player, msgs.NoPData )
			return
		elseif not gName or (gName == "") then
			self:ChatMessage( player, msgs.UrentInGroup )
			return
		end
		local gLeaders = table.concat(gData[gName].LeadersNames, ", ")
		local gMembers = table.concat(gData[gName].Members, ", ")
		self:ChatMessage( player, msgs.Group .. gName )
		self:ChatMessage( player, msgs.Leader .. gLeaders )
		self:ChatMessage( player, msgs.Members .. gMembers )
		return
	end
	--Group Help misspell error
	if func == "help" then
		self:ChatMessage(player, msgs.WrongHelp)
		return
	end
	--Group Add Leader
	if func == "leader" then
		if not args[2] then
			self:ChatMessage(player, msgs.LeaderSyntax)
			return
		end
		-- Search for the BasePlayer for the given (partial) name.
		local targetPlayer = self:FindPlayerByName( args[2] )

		-- Check if we found the targetted player.
		if #targetPlayer == 0 then
			-- The targetted player couldn't be found, send a message to the player.
			self:ChatMessage( player, msgs.NoPlayer )

			return
		end

		-- Check if we found multiple players with that partial name.
		if #targetPlayer > 1 then
			-- Multiple players were found, send a message to the player.
			self:ChatMessage( player, msgs.MultiplePlayers )

			return
		else
			-- Only one player was found, modify the targetPlayer variable value.
			target = targetPlayer[1]
		end
		if target then
			local tID = rust.UserIDFromPlayer(target)
			local gName = GroupData.PlayerData[pID].Group
			if (gName == GroupData.PlayerData[tID].Group) then
				local isLeader = table.concat(GroupData.Groups[gName].Leaders, " ")
				if string.find(isLeader, tID) then
					self:ChatMessage(player, msgs.AlreadyLeader )
					return
				end
				table.insert(GroupData.Groups[gName].Leaders, tID)
				table.insert(GroupData.Groups[gName].LeadersNames, GroupData.PlayerData[tID].RealName)
				sys[ pID ] = ""
				ginfomsg[ pID ] = msgs.LeaderAdded:format(GroupData.PlayerData[tID].RealName)
				self:C_GroupChat( player, "", "" )
				self:SaveData()
			else
				self:ChatMessage(player, msgs.LeaderToAddNotInGroup)
			end
			return
		else
			self:ChatMessage(player, msgs.NoLeaderToAdd)
			return
		end
	end
	--Group Create
	if func == "create" then
		if not target then
			self:ChatMessage(player, msgs.WrongCmd)
			return
		end
		if string.len(target) > sets.GroupNameLength then
			self:ChatMessage( player, msgs.GNameError )
			return
		end
		local newGroup = tostring(target)
		GroupData.PlayerData[pID] = GroupData.PlayerData[pID] or {}
		GroupData.PlayerData[pID].Group = GroupData.PlayerData[pID].Group or ""
		local gName = tostring(pData.Group)
		local gData = GroupData.Groups[gName]
		if gName == newGroup and gData == newGroup then
			self:ChatMessage( player, msgs.InTheGroup )
			return
		elseif gName ~= "" then
			self:ChatMessage( player, msgs.InAGroup .. gName )
			return
		elseif GroupData.Groups[newGroup] then
			self:ChatMessage( player, msgs.GroupExists )
			return
		end
		GroupData.PlayerData[pID].Group = newGroup
		GroupData.Groups[newGroup] = GroupData.Groups[newGroup] or {}
		GroupData.Groups[newGroup].Leaders = GroupData.Groups[newGroup].Leaders or {}
		table.insert(GroupData.Groups[newGroup].Leaders, pID)
		GroupData.Groups[newGroup].LeadersNames = GroupData.Groups[newGroup].LeadersNames or {}
		table.insert(GroupData.Groups[newGroup].LeadersNames, GroupData.PlayerData[pID].RealName)
		GroupData.Groups[newGroup].Members = GroupData.Groups[newGroup].Members or {}
		GroupData.Groups[newGroup].Members = { tostring( GroupData.PlayerData[pID].RealName ) }
		self:ChatMessage( player, msgs.GroupCreated..tostring( newGroup ).."!" )
		self:SaveData()
		return
	end
	--Group Leave
	if func == "leave" then
		local gName = tostring(pData.Group)
		if (gName == nil or gName == "") then
			self:ChatMessage(player, msgs.UrentInGroup)
			return
		else
		local isLeader = table.concat(GroupData.Groups[gName].Leaders, " ")
		if string.find(isLeader, pID) then
			local playerList = self:GetAllPlayers()
			for k,pIDs in pairs(playerList) do
				local gPID = rust.UserIDFromPlayer(pIDs)
				local gCatch = GroupData.PlayerData[gPID].Group
				if gName == gCatch then
					self:ChatMessage(pIDs, msgs.LeaderDelGroup)
					GroupData.PlayerData[gPID].Group = ""
				end
			end
			GroupData.Groups[gName].Leaders = nil
			GroupData.Groups[gName].Members = nil
			GroupData.Groups[gName] = nil
		end
		GroupData.PlayerData[pID].Group = ""
		self:ChatMessage( player, msgs.LeftGroup .. gName )
		self:SaveData()
		end
		return
	end
	--Group Invite
	if func == "invite" then
		if not args[2] then
			self:ChatMessage( player, msgs.InviteSyntax )
			return
		end
		self:checkPData( player )
		-- Search for the BasePlayer for the given (partial) name.
		local targetPlayer = self:FindPlayerByName( args[2] )

		-- Check if we found the targetted player.
		if #targetPlayer == 0 then
			-- The targetted player couldn't be found, send a message to the player.
			self:ChatMessage( player, msgs.NoPlayer )

			return
		end

		-- Check if we found multiple players with that partial name.
		if #targetPlayer > 1 then
			-- Multiple players were found, send a message to the player.
			self:ChatMessage( player, msgs.MultiplePlayers )

			return
		else
			-- Only one player was found, modify the targetPlayer variable value.
			target = targetPlayer[1]
			
		end
		local pID = rust.UserIDFromPlayer(player)
		local gName = GroupData.PlayerData[pID].Group
		if (gName == "" or gName == nil ) then
			self:ChatMessage( player, msgs.UrentInGroup )
			return
		end
		local gLeader = table.concat(GroupData.Groups[gName].Leaders, " ")
		if target then
			local targetuserpID = rust.UserIDFromPlayer( target )
			if string.find(gLeader, pID) then	
				for _,members in pairs(GroupData.Groups[gName].Members) do
					if members == GroupData.PlayerData[targetuserpID].RealName then
						self:ChatMessage(player, msgs.AlreadyInGroup )
						return
					end
				end
			else
				self:ChatMessage(player, msgs.MustBeLeader )
				return
			end
			self:checkPData( target )
			sys[ targetuserpID ] = gName
			ginfomsg[ pID ] = GroupData.PlayerData[targetuserpID].RealName..msgs.InviteToJoin.."!"
			self:C_GroupChat(player, "", "")
			self:ChatMessage( target, msgs.Invited:format(gName))
		else
			self:ChatMessage(player, msgs.NoPtoInvite)
		end
		return
	end
	--Group Accept
	if func == "accept" then
		local Group = sys[ pID ]
		local pName = GroupData.PlayerData[pID].RealName
		if Group == "" or Group == nil then
			self:ChatMessage( player, msgs.NotInvited )
			return
		end
		local gMembers = GroupData.Groups[Group].Members
		GroupData.PlayerData[pID].Group = Group
		table.insert(gMembers, pName)
		sys[ pID ] = ""
		self:ChatMessage( player, msgs.YouJoined .. Group )
		ginfomsg[ pID ] = tostring(player.displayName)..msgs.Joined
		self:C_GroupChat( player, "", "" )
		self:SaveData()
		return
	end	
	--Group Kick
	if func == "kick" then
		if not args[2] then
			self:ChatMessage( player, msgs.KickSyntax )
			return
		end
		if not GroupData.PlayerData[pID].Group or GroupData.PlayerData[pID].Group == "" then
			self:ChatMessage( player, msgs.UrentInGroup )
			return
		end
		
		-- Search for the BasePlayer for the given (partial) name.
		local targetPlayer = self:FindPlayerByName( args[2] )

		-- Check if we found the targetted player.
		if #targetPlayer == 0 then
			-- The targetted player couldn't be found, send a message to the player.
			self:ChatMessage( player, msgs.NoPlayer )

			return
		end

		-- Check if we found multiple players with that partial name.
		if #targetPlayer > 1 then
			-- Multiple players were found, send a message to the player.
			self:ChatMessage( player, msgs.MultiplePlayers )

			return
		else
			-- Only one player was found, modify the targetPlayer variable value.
			target = targetPlayer[1]
			
		end
		local pGroup = GroupData.PlayerData[pID].Group
		if pGroup == "" or pGroup == nil then
			self:ChatMessage( player, msgs.UrentInGroup )
			return
		end
		local gMembers = GroupData.Groups[pGroup].Members
		local gLeaders, gLNames = GroupData.Groups[pGroup].Leaders, GroupData.Groups[pGroup].LeadersNames
		if target then
			local isLeader = table.concat(GroupData.Groups[pGroup].Leaders)
			local tSID = rust.UserIDFromPlayer(target)
			local rmember = GroupData.PlayerData[tSID].RealName
			if string.find(isLeader, pID) then
				if string.find(isLeader, tSID) then
					for i=0, #gLeaders do
						if gLeaders[i] == tSID then
							table.remove(gLeaders, i)
							break
						end
					end
					for i=0, #gLNames do
						if gLNames[i] == rmember then
							table.remove(gLNames, i)
							break
						end
					end
				end
				GroupData.PlayerData[tSID].Group = ""
				for i=1, #gMembers do
					if gMembers[i] == rmember then
						table.remove(gMembers, i)
						break
					end
				end
				self:ChatMessage( player, msgs.UKicked:format(rmember) )
				self:ChatMessage( target, msgs.GotKicked )
				ginfomsg[pID] = rmember..msgs.HasBeenKicked
				self:C_GroupChat( player, "", "" )
				return
			end
		else
			self:ChatMessage( player, msgs.WhoKick )
			return
		end
	end
	--Group List
	if func == "list" then
		for gName,gData in pairs(GroupData.Groups) do
			self:ChatMessage(player, "RETURN")
			if gName and (gName ~= "") then
				self:ChatMessage(player, msgs.GroupsInServer:format(tostring(gName),tostring(table.concat(GroupData.Groups[gName].LeadersNames, ", "))))
				return
			else
				self:ChatMessage( player, msgs.NoGroups )
				return
			end
		end
		self:ChatMessage( player, msgs.NoGroups )
	end
end

function PLUGIN:C_GroupChat( player, cmd, args )
	local pID = rust.UserIDFromPlayer( player )
	local pGroup = GroupData.PlayerData[pID].Group
	local rName = GroupData.PlayerData[pID].RealName
	if not args then
		return
	end
	if not pGroup or pGroup == "" then
		self:ChatMessage( player, msgs.UrentInGroup )
		return
	end
	local gmsg = ""
	if args.Length then
		for i=0, args.Length-1 do
			gmsg = gmsg..args[i].." "
		end
	else
		gmsg = ginfomsg[pID]
	end
	print( "GrpMsg - "..player.displayName..": "..gmsg )
	local playerList = self:GetAllPlayers()
    for k,pIDs in pairs(playerList) do
		local gPID = rust.UserIDFromPlayer(pIDs)
		local gCatch = GroupData.PlayerData[gPID].Group
		if pGroup == gCatch then
			ccolor = sets.GroupChatColor
			local originalChatName = msgs.ChatName
			msgs.ChatName = msgs.GChatName
			self:ChatMessage( pIDs, rName..": "..gmsg )
			msgs.ChatName = originalChatName
		end
	end
	ccolor = sets.ChatColor
end



--===============================================================================================================
--Admin Player Cmds

function PLUGIN:C_GroupRemove( player, cmd, args )
	if not HasAcces(player) then
		return
	end
	if not args[0] then
		self:parseHelpMsgs( player, msgs.AdminHelp )
		return
	end
	--Group Remove
	local groupname = tostring( args[0] )
	if groupname == "all" then
		GroupData.Groups = nil
		GroupData.Groups = {}
		GroupData.PlayerData = nil
		GroupData.PlayerData = {}
		self:ChatMessage( player, msgs.RemovedAllGroups )
		self:SaveData()
		return
	elseif not GroupData.Groups[ groupname ] then
		self:ChatMessage( player, msgs.NoGroup )
		return
	end
	for ID,PlyData in pairs(GroupData.PlayerData) do
		if PlyData.Group then
			if PlyData.Group == groupname then
				PlyData.Group = ""
			end
		end
	end
	GroupData.Groups[ groupname ] = nil
	self:ChatMessage( player, msgs.RemovedGroup..groupname.."!" )
	self:SaveData()
	return
end

function PLUGIN:CC_GroupRemove( arg )
	local reply = ""
	local player
	if arg.connection then
		player = arg.connection.player
	end
	if not player or HasAcces(player) then
	end
	--Group Remove
	local groupname = arg:GetString( 0, "" )
	--print(groupname)
	if groupname == "all" then
		GroupData.Groups = nil
		GroupData.Groups = {}
		GroupData.PlayerData = nil
		GroupData.PlayerData = {}
		arg:ReplyWith( msgs.RemovedAllGroups )
		self:SaveData()
		return
	elseif not GroupData.Groups[ groupname ] then
		arg:ReplyWith( msgs.NoGroup )
		return
	end
	for ID,PlyData in pairs(GroupData.PlayerData) do
		if PlyData.Group then
			if PlyData.Group == groupname then
				PlyData.Group = ""
			end
		end
	end
	GroupData.Groups[ groupname ] = nil
	arg:ReplyWith( msgs.RemovedGroup..groupname.."!" )
	self:SaveData()
	return
end

function PLUGIN:C_GroupFriendlyFire( player, cmd, args )
	if not HasAcces(player) then
		return
	end
	if (sets.GroupDamage == false) then
		sets.GroupDamage = true
		self:ChatMessage( player, msgs.GroupFFoff )
	else
		sets.GroupDamage = false
		self:ChatMessage( player, msgs.GroupFFon )
	end
end
--===============================================================================================================

--~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
--Check Friendly Fire for the group

function PLUGIN:OnEntityAttacked( ent, hitinfo )
	if (not hitinfo or not ent:ToPlayer()) then return end
	if (not hitinfo.Initiator or hitinfo.damageTypes:Total() <= 0 or ent == hitinfo.Initiator) then return end
	if hitinfo.Initiator:ToPlayer() then
		aUser = hitinfo.Initiator
		aSID = rust.UserIDFromPlayer( aUser )
	else
		return
	end
	local vSID = rust.UserIDFromPlayer(ent)
	GroupData.PlayerData[aSID] = GroupData.PlayerData[aSID] or {}
	GroupData.PlayerData[aSID].Group = GroupData.PlayerData[aSID].Group or ""
	GroupData.PlayerData[vSID] = GroupData.PlayerData[vSID] or {}
	GroupData.PlayerData[vSID].Group = GroupData.PlayerData[vSID].Group or ""
    local apData = GroupData.PlayerData[aSID]
	local vpData = GroupData.PlayerData[vSID]
	if apData.Group and (apData.Group ~= "") and (apData.Group == vpData.Group) then
		if sets.GroupDamage == false then
			hitinfo.HitMaterial = 0
			hitinfo.damageTypes = new(Rust.DamageTypeList._type, nil)				
			AntiSpam[aSID] = AntiSpam[aSID] or 0
			if ( timestamp - AntiSpam[aSID] ) > 30 then
				self:ChatMessage( aUser, "Friendly Fire! You hit "..ent.displayName )
				AntiSpam[aSID] = timestamp
			end
		end
		return
	end
end


--~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~


--"""""""""""""""""""""""""""""""""""""""""""""""""""""""""""""""""""""""""""""""""""""""""""""""""""""""""""""""
--Rust Hooks


function PLUGIN:OnPlayerInit( player )
	local pID = rust.UserIDFromPlayer( player )
	self:checkPData( player )
	GroupData.PlayerData[pID].RealName = player.displayName
	print("The player "..pID.." uses the name of "..GroupData.PlayerData[pID].RealName)
	self:TagName( player )
	self:SaveData()
end

function PLUGIN:OnPlayerSpawn( player )
	local pID = rust.UserIDFromPlayer( player )
	self:checkPData( player )
	GroupData.PlayerData[pID].RealName = GroupData.PlayerData[pID].RealName or player.displayName
	self:TagName( player )
end

function PLUGIN:OnPlayerChat(arg)
	local player = arg.connection.player
	self:TagName(player)
end

--"""""""""""""""""""""""""""""""""""""""""""""""""""""""""""""""""""""""""""""""""""""""""""""""""""""""""""""""

--############################################################################################################
--Plugin Functions
function PLUGIN:checkPData( player )
	local pID = rust.UserIDFromPlayer( player )
	GroupData.PlayerData[pID]			= GroupData.PlayerData[pID] or {}
	GroupData.PlayerData[pID].Group		= GroupData.PlayerData[pID].Group or ""
end

function PLUGIN:TagName( player, args )
	local pID = rust.UserIDFromPlayer( player )
	local realName = GroupData.PlayerData[pID].RealName
	local gTag = ""
	if GroupData.PlayerData[pID].Group and (GroupData.PlayerData[pID].Group ~= "") then
		gTag = "["..tostring(GroupData.PlayerData[pID].Group).."] "
	else
		gTag = ""
	end
	player.displayName = gTag..realName
end

function PLUGIN:FindPlayerByName( playerName )
    -- Check if a player name was supplied.
    if not playerName then return end

    -- Set the player name to lowercase to be able to search case insensitive.
    playerName = string.lower( playerName )

    -- Setup some variables to save the matching BasePlayers with that partial
    -- name.
    local matches = {}
    local itPlayerList = global.BasePlayer.activePlayerList:GetEnumerator()
    
    -- Iterate through the online player list and check for a match.
    while itPlayerList:MoveNext() do
        -- Get the player his/her display name and set it to lowercase.
        local displayName = string.lower( itPlayerList.Current.displayName )
        
        -- Look for a match.
        if string.find( displayName, playerName, 1, true ) then
            -- Match found, add the player to the list.
            table.insert( matches, itPlayerList.Current )
        end
    end

    -- Return all the matching players.
    return matches
end
--############################################################################################################