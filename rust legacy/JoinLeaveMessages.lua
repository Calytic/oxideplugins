PLUGIN.Title = "Join-Leave Messages"
PLUGIN.Description = "Broadcast messages when a player joins/disconnects with an optional MOTD"
PLUGIN.Version = V(1, 0, 4)
PLUGIN.Author = "mvrb"
PLUGIN.HasConfig = true

function PLUGIN:Init()
	self:LoadDefaultConfig()
end

function PLUGIN:LoadDefaultConfig()
	self.Config.chatName 	= 	self.Config.chatName   	or 		"Oxide"
	self.Config.showMOTD 	= 	self.Config.showMOTD   	or 		"true"
	self.Config.MOTD  		=	self.Config.MOTD		or
	{
			"Welcome! Type /help to get help.",
			"Please read the server rules by typing /rules."
	}	
	self.Config.JoinMSG  	= 	self.Config.JoinMSG  	or 		" has connected to the server" 
	self.Config.LeaveMSG 	= 	self.Config.LeaveMSG 	or 		" has disconnected from the server"
	
	self:SaveConfig()
end

function PLUGIN:OnPlayerConnected(netuser)
	if(self.Config.showMOTD == "true") then	
			for i = 1, #self.Config.MOTD do	rust.SendChatMessage(netuser, self.Config.chatName, self.Config.MOTD[i])	end		
	end
	
	rust.BroadcastChat(self.Config.chatName, netuser.displayName .. self.Config.JoinMSG)
end

function PLUGIN:OnPlayerDisconnected(netuser)
    rust.BroadcastChat(self.Config.chatName, netuser:GetLocalData().displayName.. self.Config.LeaveMSG)
end