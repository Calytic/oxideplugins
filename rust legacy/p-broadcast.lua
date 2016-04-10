PLUGIN.Title = "PaiN Broadcast"
PLUGIN.Description = "Broadcast messages to your players."
PLUGIN.Author = "PaiN"
PLUGIN.Version = V(0, 7, 5)
PLUGIN.ResourceId = 1101

function PLUGIN:Init()
command.AddChatCommand("bcast", self.Plugin, "cmdBroadcast")
command.AddChatCommand("note", self.Plugin, "cmdNotify")
self:LoadDefaultConfig()
permission.RegisterPermission("canbroadcast", self.Plugin)
end

function PLUGIN:LoadDefaultConfig()
self.Config.Messages = self.Config.Messages or {}
self.Config.Messages.ChatPrefix = self.Config.Messages.ChatPrefix or "PaiN Broadcast"
self.Config.Messages.NoPermission = self.Config.Messages.NoPermission or "You do not have permission to use this command"
self.Config.Messages.BcastSyntax = self.Config.Messages.BcastSyntax or "Syntax: /bcast \"message\" "
self.Config.Messages.NoteSyntax = self.Config.Messages.NoteSyntax or "Syntax: /note \"player\" \"message\" "
self.Config.Messages.PlayerNotFound = self.Config.Messages.PlayerNotFound or "Player Not Found!"
self:SaveConfig()
end

function PLUGIN:cmdBroadcast(netuser, cmd, args)
local steamId = rust.UserIDFromPlayer(netuser)
    if not permission.UserHasPermission(steamId, "canbroadcast") then
        rust.SendChatMessage(netuser, self.Config.Messages.ChatPrefix, self.Config.Messages.NoPermission)
        return
    end

if args.Length == 0 then
rust.SendChatMessage(netuser, self.Config.Messages.ChatPrefix, self.Config.Messages.BcastSyntax)
end

if args.Length >= 1 then
	local BcastMessage = tostring(args[0])
	 rust.BroadcastChat(self.Config.Messages.ChatPrefix, BcastMessage)
	end
end

function PLUGIN:cmdNotify(netuser, cmd, args)
local steamId = rust.UserIDFromPlayer(netuser)
    if not permission.UserHasPermission(steamId, "canbroadcast") then
        rust.SendChatMessage(netuser, self.Config.Messages.ChatPrefix, self.Config.Messages.NoPermission)
        return
    end

if args.Length == 0 then
rust.SendChatMessage(netuser, self.Config.Messages.ChatPrefix, self.Config.Messages.NoteSyntax)
end

if args.Length >= 1 then
	local targetPlayer = rust.FindPlayer(args[0])
	if targetPlayer then
		local NoteMsg = tostring(args[1])
		rust.Notice(targetPlayer, NoteMsg, "!", 5)
	else
	    rust.SendChatMessage(netuser, self.Config.Messages.ChatPrefix,  self.Config.Messages.PlayerNotFound)
		end
	end
end


