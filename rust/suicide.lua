PLUGIN.Name = "suicide"
PLUGIN.Title = "Suicide & Kill"
PLUGIN.Version = V(0, 1, 2)
PLUGIN.Description = "Suicide & Kill chat command"
PLUGIN.Author = "Reneb"
PLUGIN.HasConfig = true

function PLUGIN:Init()
	command.AddChatCommand( "suicide", self.Plugin, "cmdSuicide" )
	command.AddChatCommand( "kill", self.Plugin, "cmdSuicide" )
	command.AddChatCommand( "die", self.Plugin, "cmdSuicide" )
end

function PLUGIN:LoadDefaultConfig()
	self.Config.KillForModerators = self.Config.KillForModerators or true
	self.Config.Messages = {}
	self.Config.Messages.NotAllowed = self.Config.Messages.NotAllowed or "You are not allowed to use this command on someone else"
	self.Config.Messages.PlayerDoesntExist = self.Config.Messages.PlayerDoesntExist or "{username} doesn't exist"
	self.Config.Messages.PlayerWasKilled = self.Config.Messages.PlayerWasKilled or "{username} was killed"
	self.Config.Messages.MultiplePlayersFound =  self.Config.Messages.MultiplePlayersFound or "Multiple Players Found"
end

function PLUGIN:cmdSuicide( player, com, args )
	local authlevel = player:GetComponent("BaseNetworkable").net.connection.authLevel
	local neededlevel = 2
	if(self.Config.KillForModerators) then
		neededlevel = 1
	end
	if(args.Length >= 1) then
		if(authlevel and authlevel >= neededlevel) then
			local target = ""
			for i=0, args.Length-1 do
				if(i == 0) then
					target = args[i]
				else
					target = target .. " " .. args[i]
				end
			end
			local targetplayer = global.BasePlayer.Find(target)
			if(not targetplayer) then
				local plistenum = player.activePlayerList
				local it = plistenum:GetEnumerator()
				while (it:MoveNext()) do
					if(targetplayer) then
						player:ChatMessage(self:BuildMSG(self.Config.Messages.MultiplePlayersFound,target))
						return
					end
					if(string.find(it.Current.displayName,target)) then
						targetplayer = it.Current
					end
				end
				if(not targetplayer) then
					player:ChatMessage(self:BuildMSG(self.Config.Messages.PlayerDoesntExist,target))
					return
				end
			end
			targetplayer:Hurt(1000,Rust.DamageType.Suicide,nil)
			player:ChatMessage(self:BuildMSG(self.Config.Messages.PlayerWasKilled,targetplayer.displayName))
		else
			player:ChatMessage(self:BuildMSG(self.Config.Messages.NotAllowed,targetplayer.displayName))
		end
	else
		player:Hurt(1000,Rust.DamageType.Suicide,nil)
	end
end
function PLUGIN:BuildMSG(msg,name)
	return tostring(string.gsub(msg, "{username}", name))
end