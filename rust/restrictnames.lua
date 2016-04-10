PLUGIN.Name = "restrictnames"
PLUGIN.Title = "Restrict Names"
PLUGIN.Version = V(0, 1, 1)
PLUGIN.Description = "Restrict Names from entering your server"
PLUGIN.Author = "Reneb"
PLUGIN.HasConfig = true

function PLUGIN:Init()
    
end

function PLUGIN:LoadDefaultConfig()
	self.Config.IgnoreModerators = true
	self.Config.useRestrictName = self.Config.useRestrictName or true
	self.Config.useRestrictCharacters = self.Config.useRestrictCharacters or true
	self.Config.RestrictedNames = self.Config.RestrictedNames or {"SERVER CONSOLE","SERVER","Oxide"}
	self.Config.AllowedCharacters = self.Config.AllowedCharacters or "abcdefghijklmnopqrstuvwxyz1234567890 [](){}!@#$%^&*_-=+.|"
end

function PLUGIN:CanClientLogin(connection)
	if(not connection) then return end
	if(not connection.username) then return end
	if(self.Config.IgnoreModerators and connection.authLevel > 0) then return end
	local name = connection.username
	if(self.Config.useRestrictName) then
		for i=1, #self.Config.RestrictedNames do
			if(name == self.Config.RestrictedNames[i]) then
				print(connection.username .. " connection refused: Illegal Name")
				return "Connection Refused: You are not allowed to use this name"
			end
		end
	end
	if(self.Config.useRestrictCharacters) then
		for i = 1, name:len() do
			if(string.find( self.Config.AllowedCharacters,name:sub(i,i):lower(), nil, true ) == nil) then
				print(connection.username .. " connection refused: Illegal Character")
				return "Connection Refused: You have illegal characters in your name"
			end
		end
	end
	print(connection.username .. " has successfully joined the server")
end