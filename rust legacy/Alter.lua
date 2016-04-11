PLUGIN.Title = "Alter"
PLUGIN.Description = "Alter the amount of resources needed for an item."
PLUGIN.Version = V(2, 0, 0)
PLUGIN.Author = "CareX ported by Reneb"

function PLUGIN:Init()
	print( "Alter is loading..." )
	
	
	self.Data = datafile.GetDataTable( "Alter - Data" ) or {}
	self.Data[ "crafting" ] = {}
	self.Data[ "ct" ] = self.Data[ "ct" ] or {}

	self.LastCraft = {}
	
	-- self.Chat = self.Config.Chat
	self.Chat = "Alter"
	
	command.AddChatCommand("alter", self.Plugin, "cmdAddItem" ) -- Alter the custom recipe for any item
	command.AddChatCommand("alterclear", self.Plugin, "cmdDelItem" ) -- clear the custom recipe
	command.AddChatCommand("alterct", self.Plugin, "cmdAddCT" ) -- Alter the crafting time on any item
	command.AddChatCommand("req", self.Plugin, "cmdRequest" ) -- Request resource needed to craft an item. ( if changed )
	command.AddChatCommand("alterhelp", self.Plugin, "cmdHelp" ) -- Request resource needed to craft an item. ( if changed )
	print ("[ ALTER ] Chat commands loaded " )
	
	
end

function PLUGIN:cmdAddCT( netuser, cmd, args )

	if( not (netuser:CanAdmin() )) then
		rust.Notice( netuser, "You're not an admin." )
		return
	end
	
	if( (args[1]) and (args[2]) ) then
		local item = tostring( args[ 1 ] )
		local t = tonumber( args[ 2 ] )
		local datablock = rust.GetDatablockByName( item )
		
		if (not datablock) then
			rust.Notice( netuser, "No such item!" )
			return
		end
		
		if( t == 0 ) and ( self.Data.ct[ item ] )then
			self.Data.ct[ item ] = nil
			rust.SendChatMessage( netuser, item .. " does not have a crafting time anymore" )
			return
		end
		
		if( not( self.Data[ item ] )) then
			self.Data.ct[ item ] = {}
			self.Data.ct[ item ] = t
		else
			self.Data.ct[ item ] = t
		end	
		rust.SendChatMessage( netuser, "Crafting time for " .. item .." set to: " .. t .. " seconds per item" )
		
		self:Save()
	else
		rust.SendChatMessage( netuser, "/alterct [\"ItemName\"] [ time ]" )
	end
end

function PLUGIN:cmdAddItem( netuser, cmd, args )
	if(args.Length > 2) then
		if( not (netuser:CanAdmin() )) then
			rust.Notice( netuser, "You're not an admin." )
			return
		end
		local item = tostring( args[0] )
		local resource = tostring( args[1] )
		local amount = tonumber( args[2] )
		if( ( not amount ) or (amount <= 0 ) ) then rust.Notice( netuser, self.Chat, "Invalid amount!" ) return end -- Check if valid amount is registered.
		local datablock1 = global.DatablockDictionary.GetByName( item )
		if (not datablock1) then
			rust.Notice( netuser, "No such item!" )
			return
		end
		local datablock2 = global.DatablockDictionary.GetByName( resource )
		if (not datablock2) then
			rust.Notice( netuser, "No such resource!" )
			return
		end
		if( not( self.Data[ item ] )) then
			self.Data[ item ] = {}
			self.Data[ item ][ resource ] = amount
		else
			if ( self.Data[ item ][ resource ] ) then
				rust.SendChatMessage( netuser, item .. " has " .. resource .. " already configured, changing it to current request." )
				self.Data[ item ][resource ] = amount
			end
			self.Data[ item ][ resource ] = amount
		end
		rust.SendChatMessage( netuser, "Crafting recipe for " .. item .. " changed to:")
		for key, value in pairs( self.Data[ item ] ) do
			rust.SendChatMessage( netuser, key .. " x" .. value )
		end
		self:Save()
	else
		rust.SendChatMessage( netuser, "/alter [\"item\"] [\"ResourceName\"] [amount]" )
		rust.SendChatMessage( netuser, "/alter [\"item\"] [\"ct\"] [time] to set crafting time" )
	end
end

function PLUGIN:cmdDelItem( netuser, cmd, args )
	if (args.Length > 0) then
		local item = tostring(args[1])
		if( self.Data[ item ] ) then
			self.Data[ item ] = nil
			rust.SendChatMessage( netuser,  item .. "'s custom made recipe has been deleted" )
			return
		else
			rust.SendChatMessage( netuser, item .. " does not have a custom recipe" )
		end
	else
		rust.SendChatMessage( netuser, "/alterclear [\"ItemName\"]" )
	end
end

function PLUGIN:cmdRequest( netuser, cmd, args )
	if(args.Length == 1) then
		local item = tostring( args[0] )
			if( item == "all" ) then
				rust.SendChatMessage( netuser, "Custom recipes are set for: " )
				for key, value in pairs( self.Data ) do
					if( not (key == "ct" )) then
						rust.SendChatMessage( netuser, key )
					end
				end
				return
			end
		if( self.Data[ item ] ) then
			rust.SendChatMessage( netuser, "Resources to craft " .. item .. " are: " )
			for key, value in pairs( self.Data[ item ] ) do
				rust.SendChatMessage( netuser, self.Chat, key .. " x" .. value )
			end
			if( self.Data.ct[ item ] ) then
				rust.SendChatMessage( netuser, "Crafting time: " .. self.Data.ct[ item ] )
			end
			return
		else
			rust.SendChatMessage( netuser, "This item is not custom changed." )
			return
		end
	elseif( args.Length == 2 ) then
		local item = tostring( args[ 0 ] )
		local amount = tonumber( args[ 1 ] )
		if( self.Data[ item ] ) then
			rust.SendChatMessage( netuser, "Resources to craft " .. amount .. "x " .. item .. " are: " )
			for key, value in pairs( self.Data[ item ] ) do
				rust.SendChatMessage( netuser, key .. " x" .. (value * amount) )
			end
			if( self.Data.ct[ item ] ) then
				rust.SendChatMessage( netuser, "Crafting time: " .. ( self.Data.ct[ item ] * amount ) )
			end
		else
			rust.SendChatMessage( netuser "This item is not custom changed." )
		end
	else
		rust.SendChatMessage( netuser, "/req [\"Item\"] " )
	end
end

function PLUGIN:OnItemCraft( inv, blueprint, amount, starttime )

    local netuser = inv.idMain:GetComponent(global.Character._type).netUser

    local userID = rust.UserIDFromPlayer( netuser )
    if( self.Data.crafting[ userID ] ) then rust.Notice( netuser, "You're already crafting!" ) return false end
	local item = blueprint.resultItem.name
	local taken = {}
	local check = true
	if ( self.Data[ tostring( item ) ] ) then
		if(not(self.LastCraft[userID] == tostring(item))) then
			rust.SendChatMessage( netuser, "Crafting " .. item .. " this will cost you: " )
			for key, value in pairs( self.Data[ item ] ) do
				rust.SendChatMessage( netuser, key .. " x" .. tostring((value * amount)) )
			end
		end
		self.LastCraft[userID] = tostring(item)
		for key, value in pairs(self.Data[ item ] ) do
			value = value * amount
			check = self:Convert( netuser, key, value )
			if( not ( check ) ) then
				rust.Notice( netuser, "You do not have enough materials!" )
				for key, value in pairs( taken ) do
					global.ConsoleSystem.Run("inv.giveplayer " .. rust.QuoteSafe( netuser.displayName ) .. " " .. rust.QuoteSafe( key ) .. " " .. tostring( value ), false )
				end
				return false
			end
			taken[ key ] = value
		end
		if ( self.Data.ct[ tostring( item ) ] ) then
            self.Data.crafting[ userID ] = true
			local Time = ( self.Data.ct[ tostring( item ) ] * amount )
			local i = Time
			timer.Repeat( 1, Time, function()
				global.ConsoleNetworker.SendClientCommand(netuser.playerClient.netPlayer, "notice.inventory "  .. tostring( i ))
				i = i - 1
				if( i <= 0 ) then
					
					global.ConsoleSystem.Run("inv.giveplayer " .. rust.QuoteSafe( netuser.displayName ) .. " " .. rust.QuoteSafe( item ) .. " " .. tostring( amount ), false )
					local msg = tostring( amount .. "x " .. item )
					global.ConsoleNetworker.SendClientCommand(netuser.playerClient.netPlayer, "notice.inventory \"" .. msg .. "\"")
                    self.Data.crafting[ userID ] = nil
					return
				end 
			end )
		end
		if( not( self.Data.ct[ tostring( item ) ] )) then
			if( check ) then
				global.ConsoleSystem.Run("inv.giveplayer " .. rust.QuoteSafe( netuser.displayName ) .. " " .. rust.QuoteSafe( item ) .. " " .. tostring( amount ) , false)
				local msg = tostring( amount .. "x " .. item )
				global.ConsoleNetworker.SendClientCommand(netuser.playerClient.netPlayer, "notice.inventory \"" .. msg .. "\"")
			end
		end
		return false
	end
end

function PLUGIN:Convert( netuser, itemname, amount )
	local datablock = global.DatablockDictionary.GetByName(  itemname )
    local inv = netuser.playerClient.rootControllable.idMain:GetComponent(global.Inventory._type);
    if (not inv) then
        rust.Notice( netuser, "Inventory not found!" )
		return false
    end
    local weapon = ((itemname == "M4") or (itemname == "9mm Pistol") or (itemname == "Shotgun") or (itemname == "P250") or (itemname == "MP5A4") or (itemname == "Pipe Shotgun")
            or (itemname == "Bolt Action Rifle") or (itemname == "Revolver") or (itemname == "HandCannon") or (itemname == "Torch") or (itemname == "Research Kit 1")) 
    local i = 0  
    local item = inv:FindItem(datablock)
    if (item) then
        if (not weapon) then
            while (i < amount) do
                if (item.uses > 0) then
                    item:SetUses( item.uses - 1 )
                    i = i + 1
                else
                    inv:RemoveItem( item )
                    item = inv:FindItem(datablock)
                    if (not item) then
                        break
                    end
                end
            end
        else
            while (i < amount) do
                inv:RemoveItem( item )
                i = i + 1
                item = inv:FindItem(datablock)
                if (not item) then
                    break
                end
            end
        end
    else
		return false
    end 
    if ((not weapon) and (item) and (item.uses <= 0)) then
        inv:RemoveItem( item )
    end
	if( not ( i == amount )) then
		rust.Notice( netuser, "Not enough " .. itemname )
        global.ConsoleSystem.Run("inv.giveplayer " .. rust.QuoteSafe( netuser.displayName ) .. " " .. rust.QuoteSafe( itemname ) .. " " .. tostring( i ) )
		return false
	end
	return true
end

function PLUGIN:SendHelpText( netuser )
    rust.SendChatMessage( netuser, "Use /alterhelp to list all the Alter commands!" )
end

function PLUGIN:cmdHelp( netuser, cmd, args )
	rust.SendChatMessage( netuser, "Thank you for using Alter! Here are the commands:" )
	rust.SendChatMessage( netuser, "/req [\"ItemName\"] to check the custom crafting recipe (if there is one)" )
	if( netuser:CanAdmin() ) then --admin commands
		rust.SendChatMessage( netuser,  "/alter [\"ItemName\"] [\"Resource\"] [\"amount\"] -- to add/change the custom recipe" )
		rust.SendChatMessage( netuser, "/alterclear [\"ItemName\"] -- to clear the custom recipe" )
		rust.SendChatMessage( netuser, "/alterct [\"ItemName\"] [Time] -- to set a custom crafting time. (This only works if there is a custom recipe" )
		rust.SendChatMessage( netuser, "TIP: for more info simply type the chat-command. " )
	end
end

function PLUGIN:Save()
	datafile.SaveDataTable( "Alter - Data" )
	print( "[ HS ] Data file Saved" )
end
