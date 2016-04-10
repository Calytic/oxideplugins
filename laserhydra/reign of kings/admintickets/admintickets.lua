PLUGIN.Title        = "Admin Tickets"
PLUGIN.Description  = "Gives players the opportunity to send Tickets to admins"
PLUGIN.Author       = "LaserHydra"
PLUGIN.Version      = V(1,5,5)
PLUGIN.ResourceId    = 1065

local ReportsData = {}

function PLUGIN:Init()
	command.AddChatCommand("ticket", self.Plugin, "cmdTicket")
    self:LoadDataFile()
	self:LoadDefaultConfig()
	
	activeTickets = timer.Repeat(300, 0, function()
		if #TicketsData.Tickets >= 1 then
			local players = CodeHatch.Engine.Networking.Server.get_ClientPlayers():GetEnumerator()
			while players:MoveNext() do
				if players.Current:HasPermission("isTicketsAdmin") then
					rok.SendChatMessage(players.Current, "[00FFFC]Tickets[FFFFFF]", "There are active tickets! Type /ticket list to view them!")
				end
			end
		end
	end, self.Plugin)
end

function PLUGIN:LoadDefaultConfig()
	self.Config.Extras = self.Config.Extras or {}
	self.Config.Extras.EnabledPushAPI = self.Config.Extras.EnabledPushAPI or false
	self.Config.Extras.EnabledEmailAPI = self.Config.Extras.EnabledEmailAPI or false
end

function PLUGIN:LoadDataFile()
    TicketsData = datafile.GetDataTable("admintickets")
    TicketsData = TicketsData or {}
    TicketsData.Tickets = TicketsData.Tickets or {}
	TicketsData.lastId = TicketsData.lastId or 0
end

function PLUGIN:Unload()
    datafile.SaveDataTable("admintickets")
end

function PLUGIN:ExtrasAPI(message)
	if self.Config.Extras.EnabledPushAPI == true then
		if not plugins.Exists("PushAPI") then 
			print("You enabled support for the PushAPI in the config, but PushAPI is not installed! Get it here: http://oxidemod.org/plugins/705/")
		return end
		local pushApi = plugins.Find("PushAPI")
		pushApi:CallHook("PushMessage", "Admin Tickets | A new Ticket has been made!", message, "high", "gamelan")
	end
	if self.Config.Extras.EnabledEmailAPI == true then
		if not plugins.Exists("EmailAPI") then 
			print("You enabled support for the EmailAPI in the config, but EmailAPI is not installed! Get it here: http://oxidemod.org/plugins/712/")
		return end
		local emailApi = plugins.Find("EmailAPI")
		emailApi:CallHook("EmailMessage", "Admin Tickets | A new Ticket has been made!", message)
	end
end

function PLUGIN:cmdTicket(player, cmd, args)
    local allArgs = ""
    local arg = args:GetEnumerator()
    local userId = rok.IdFromPlayer(player)
   
    while arg:MoveNext() do
        allArgs = allArgs .. " " .. arg.Current
    end
   
    if args.Length == 0 then
       
        if player:HasPermission("isTicketsAdmin") then
            rok.SendChatMessage(player, "[00FFFC]Admin Tickets[FFFFFF]:\n/ticket message\n/ticket list\n/ticket clear\n/ticket view [id]\n/ticket remove [id]")
        else
			rok.SendChatMessage(player, "[00FFFC]Admin Tickets[FFFFFF]:\n/ticket message")
		end
       
    elseif args.Length > 0 then
       
        if args.Length == 1 and player:HasPermission("isTicketsAdmin") and args[0] == "list" then
           
            if #TicketsData.Tickets == 0 then 
                rok.SendChatMessage(player, "[00FFFC]Tickets[FFFFFF]", "There currently are no tickets!")
                return false
            end
           
            if #TicketsData.Tickets >= 1 then
                rok.SendChatMessage(player, "[00FFFC]Tickets[FFFFFF]", "Following tickets has been made:")
                for Ticket, data in pairs(TicketsData.Tickets) do
                    rok.SendChatMessage(player, "[00FFFC]-----------------------------------\n" ..
                                                 "[00FFFC]TicketID[FFFFFF]: " .. data.uniqueId .. "\n" ..
                                                 "[00FFFC]Player[FFFFFF]: " .. data.player .. "\n" ..
                                                 "[00FFFC]SteamID[FFFFFF]: " .. data.steamId .. "\n" ..
                                                 --"[00FFFC]Position[FFFFFF]: " .. data.position .. "\n" ..
                                                 "[00FFFC]Message[FFFFFF]: " .. data.message)
                end
				rok.SendChatMessage(player, "[00FFFC]-----------------------------------")
            end
            return false
        end
       
        if args.Length == 1 and player:HasPermission("isTicketsAdmin") and args[0] == "clear" then
            if #TicketsData.Tickets == 0 then
                rok.SendChatMessage(player, "[00FFFC]Tickets[FFFFFF]", "List of tickets is already empty!")
                return false
            end
            TicketsData.Tickets = {}
            datafile.SaveDataTable("admintickets")
            rok.SendChatMessage(player, "[00FFFC]Tickets[FFFFFF]", "List of tickets successfully cleared.")
            return false
        end
       
		if args.Length == 2 and player:HasPermission("isTicketsAdmin") and args[0] == "remove" then
			for current, data in pairs(TicketsData.Tickets) do
				if data.uniqueId == tonumber(args[1]) then
					rok.SendChatMessage(player, "[00FFFC]Tickets[FFFFFF]", "Removed the ticket with the ID [00FFFC]" .. data.uniqueId .. "[FFFFFF]")
					table.remove(TicketsData.Tickets, current)
					break
				end
			end
			return false
		end
		
		--[[if args.Length == 2 and player:HasPermission("isTicketsAdmin") and args[0] == "tp" then
			for current, data in pairs(TicketsData.Tickets) do
				if data.uniqueId == tonumber(args[1]) then
					player:StartSleeping()
					rust.ForcePlayerPosition(player, data.x, data.y, data.z)
					
					--**--**--**-- Thanks to Reneb's Teleportation plugin for this part! **--**--**--**
					player:SetPlayerFlag(global.BasePlayer.PlayerFlags.ReceivingSnapshot, true);
					player:UpdateNetworkGroup();
					player:UpdatePlayerCollider(true, false);
					player:SendNetworkUpdateImmediate(false);
					player:ClientRPCPlayer(null, player, "StartLoading");
					player:SendFullSnapshot();
					player:SetPlayerFlag(global.BasePlayer.PlayerFlags.ReceivingSnapshot, false);
					player:ClientRPCPlayer(null, player, "FinishLoading" );
					--**--**--**--**--**--**--**--**--**--**--**--**--**--**--**--**--**--**--**--**--**
					
					
					rok.SendChatMessage(player, "[00FFFC]Tickets[FFFFFF]", "You got teleported to the position this ticket has been made:\n" ..
												"[00FFFC]-----------------------------------\n" ..
                                                "[00FFFC]TicketID: " .. data.uniqueId .. "\n" ..
                                                "[00FFFC]Player: " .. data.player .. "\n" ..
                                                "[00FFFC]SteamID: " .. data.steamId .. "\n" ..
                                                "[00FFFC]Position: " .. data.position .. "\n" ..
                                                "[00FFFC]Message: " .. data.message .. "\n" ..
												"[00FFFC]-----------------------------------")
					break
				end
			end
			return false
		end ]]--
		
		if args.Length == 2 and player:HasPermission("isTicketsAdmin") and args[0] == "view" then
			for current, data in pairs(TicketsData.Tickets) do
				if data.uniqueId == tonumber(args[1]) then
					rok.SendChatMessage(player, "[00FFFC]Tickets[FFFFFF]", "Following ticket was found:\n" ..
												"[00FFFC]-----------------------------------\n" ..
                                                "[00FFFC]TicketID[FFFFFF]: " .. data.uniqueId .. "\n" ..
                                                "[00FFFC]Player[FFFFFF]: " .. data.player .. "\n" ..
                                                "[00FFFC]SteamID[FFFFFF]: " .. data.steamId .. "\n" ..
                                               -- "[00FFFC]Position[FFFFFF]: " .. data.position .. "\n" ..
                                                "[00FFFC]Message[FFFFFF]: " .. data.message .. "\n" ..
												"[00FFFC]-----------------------------------")
					break
				end
			end
			return false
		end
	   
        local Ticket = {}
        Ticket.uniqueId = TicketsData.lastId + 1
        TicketsData.lastId = TicketsData.lastId + 1
        Ticket.player = player.DisplayName
        Ticket.steamId = rok.IdFromPlayer(player)
        Ticket.message = allArgs
		--[[
		Ticket.x = math.ceil(player.transform.position.x)
		Ticket.y = math.ceil(player.transform.position.y)
		Ticket.z = math.ceil(player.transform.position.z)
        Ticket.position = "X: " .. math.ceil(player.transform.position.x) .. ", Y: " .. math.ceil(player.transform.position.y) .. ", Z: " .. math.ceil(player.transform.position.z) 
		]]--
   
        table.insert(TicketsData.Tickets, Ticket)
        datafile.SaveDataTable("admintickets")
       
        local players = CodeHatch.Engine.Networking.Server.get_ClientPlayers():GetEnumerator()
        while players:MoveNext() do
			local currentUid = rok.IdFromPlayer(players.Current)
            if players.Current:HasPermission("isTicketsAdmin") then
                rok.SendChatMessage(players.Current, "[00FFFC]Tickets[FFFFFF]", "A new ticket has been made! Type /ticket view [00FFFC]" .. Ticket.uniqueId .. " to view it!")
            end
        end
		
		self:ExtrasAPI("A new Ticket has been made: \n" ..
									"TicketID: " .. Ticket.uniqueId .. "\n" ..
                                    "Player: " .. Ticket.player .. "\n" ..
                                    "SteamID: " .. Ticket.steamId .. "\n" ..
									"Steam Profile: https://steamcommunity.com/profiles/" .. data.steamId .. "\n" ..
                                    "Message: " .. Ticket.message)
       
        rok.SendChatMessage(player, "[00FFFC]Tickets[FFFFFF]", "You have sent following ticket: \n" ..
                                     "[00FFFC]-----------------------------------\n" ..
                                     "[00FFFC]TicketID[FFFFFF]: " .. Ticket.uniqueId .. "\n" ..
                                     --"[00FFFC]Position[FFFFFF]: " .. Ticket.position .. "\n" ..
                                     "[00FFFC]Message[FFFFFF]: " .. Ticket.message .. "\n" ..
                                     "[00FFFC]-----------------------------------")
       
    end      
end