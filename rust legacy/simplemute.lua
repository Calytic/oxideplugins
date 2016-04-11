PLUGIN.Name = "Simple Mute"
PLUGIN.Title = "Simple Mute"
PLUGIN.Version = V(1, 0, 1)
PLUGIN.Description = "Use this plugin to mute players from spamming chat"
PLUGIN.Author = "Leez (Gramexer)"
PLUGIN.HasConfig = true

function PLUGIN:Init()

	command.AddChatCommand( "mute", self.Plugin, "cmdMute" )
	command.AddChatCommand( "unmute", self.Plugin, "cmdUnMute" )
	command.AddChatCommand( "test", self.Plugin, "cmdtest" )
	ServerInitialized = true
	
	mutedPlayers = {}
	
	permission.RegisterPermission("canmute", self.Plugin)
	
	end

function PLUGIN:OnPlayerChat(player, message)
	if (message:sub( 1, 1 ) ~= "/") then
		if(self:isMuted(player))then
		rust.Notice(player,self.Config.youAreMuted)
			return true
		end
	end
end

function PLUGIN:cmdMute(player,cmd,args)

	local playerID = rust.UserIDFromPlayer(player)

	if self:canMute(player) then

	
		if(args.Length == 0) then
		rust.Notice(player,"Syntax: /mute player minutes")
		
		end
	
		if(args.Length >= 1) then
		
			local target = rust.FindPlayer(args[0])
			
			if(target == nil)then
			rust.Notice(player,"No users found with name "..args[0])
				return
			end
			
			if(self:isMuted(target)) then
			rust.Notice(player,args[0].." is already muted.")
				return
			end
			
			local mutetime = self.Config.defaultMuteTime
			
			if(args.Length == 2) then
			
			
				mutetime = tonumber(args[1])
				
					
					if(mutetime == nil)then
						mutetime = self.Config.defaultMuteTime
					end
			
					
			end
			
			
		
			
			self:mutePlayer(target,mutetime*60)
			rust.Notice(player,"You muted "..target.displayName)
			if(self.Config.broadcastMutes == true)then
				rust.BroadcastChat(target.displayName..self.Config.hasBeenMuted.." ("..mutetime.." Minutes)")
			end
			
			
			return
		end
	
	
	
	else
	rust.Notice(player,"You don't have a permission to use this command.")
	
	--

	end

end


function PLUGIN:cmdUnMute(player,cmd,args)

	local playerID = rust.UserIDFromPlayer(player)

	if self:canMute(player) then
		
		
		if(args.Length == 0) then
		rust.Notice(player,"Syntax: /unmute player")
			return
		end
		
		if(args.Length == 1) then
		
			local target = rust.FindPlayer(args[0])
			
			if(target == nil)then
			rust.Notice(player,"No users found with name "..args[0])
				return
			end
			
			if (self:isMuted(target) == false) then
			rust.Notice(player,args[0].." is not muted.")
				return
			end
			
			self:unmutePlayer(target)
			rust.Notice(player,"You unmuted "..target.displayName)
				return
		end
		
		
		
		
		
	
	else
	rust.Notice(player,"You don't have a permission to use this command.")
	
	end



end



function PLUGIN:canMute(player)

	local playerID = rust.UserIDFromPlayer(player)

	if(player:CanAdmin() or permission.UserHasPermission(playerID, "canmute")) then
		return true
	else return false

end

end

function PLUGIN:isMuted(player)

	playerID = rust.UserIDFromPlayer(player)

	if(mutedPlayers[playerID] == "1")then
		return true
	end
	return false
end


function PLUGIN:mutePlayer(player, mutetime)

	rust.Notice(player,"You have been muted")
	playerID = rust.UserIDFromPlayer(player)
	mutedPlayers[playerID] = "1"


	timer.Once(mutetime, function()
		self:unmutePlayer(player)
	end, self.Plugin)
end

function PLUGIN:unmutePlayer(player)

	if(self:isMuted(player) == false)then
		return
	end

	rust.Notice(player,"Unmuted")
	playerID = rust.UserIDFromPlayer(player)
	mutedPlayers[playerID] = nil
	end

	
function PLUGIN:LoadDefaultConfig()
	self.Config.defaultMuteTime = 5
	self.Config.hasBeenMuted = " [color yellow]has been muted"
	self.Config.youAreMuted = "You are muted!."
	self.Config.broadcastMutes = true

end