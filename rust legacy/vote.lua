PLUGIN.Name = "Vote Rewards"
PLUGIN.Title = "Vote Rewards"
PLUGIN.Version = V(1, 0, 5)
PLUGIN.Description = "Reward your voters"
PLUGIN.Author = "Leez (Gramexer)"
PLUGIN.HasConfig = true



function PLUGIN:Init()

	command.AddChatCommand( "vote", self.Plugin, "cmdVote" )
	command.AddChatCommand( "reward", self.Plugin, "cmdReward")
	ServerInitialized = true
	
	playerInventory = {}
	
	
	local plug = plugins.Find("PlayerDatabase")
	if(not plug) then
	
		print("Player Database is not Installed! http://oxidemod.org/plugins/player-database-mysql-support.927/")

		return
	end
	
	
	
end


function PLUGIN:OnPlayerSpawn(player)


	local tmp = player.netUser
	timer.Once(2, function()
		self:LoadInventory(tmp)
	end, self.Plugin)
end

function PLUGIN:LoadInventory(netUser)

	if(netUser.playerClient)then
		local inv = netUser.playerClient.rootControllable.idMain:GetComponent( "Inventory" ) 
		local playerId = rust.UserIDFromPlayer(netUser)
		playerInventory[playerId] = inv
	end

end

function PLUGIN:cmdVote(player,cmd,args)

	local plug = plugins.Find("PlayerDatabase")
	if(not plug) then
	
		print("Player Database is not Installed! http://oxidemod.org/plugins/player-database-mysql-support.927/")
		rust.SendChatMessage(player,"[color red]Player Database is not Installed![/color] http://oxidemod.org/plugins/player-database-mysql-support.927/")
		return
	end



	if(args.Length == 0)then

		rust.SendChatMessage(player, self.Config.Messages.HelpMessage)
		rust.SendChatMessage(player, self.Config.Messages.HelpMessage2)

		return
	end


	if(args[0] == "check") then
	
		----------------------
		-- TopRustServer Check
		----------------------
		if(#self.Config.trsAPIKey >= 5)then
		
		local playerid = tostring(rust.UserIDFromPlayer(player))
		
			webrequests.EnqueueGet("http://api.toprustservers.com/api/get?plugin=voter&key="..tostring(self.Config.trsAPIKey).."&uid="..rust.UserIDFromPlayer(player), function(code, response)

				if response == nil or code ~= 200 then 
					print("Could not connect to TopRustServers!") 
					rust.SendChatMessage(player, "[color #ff0000]ERROR! Couldn't connect to TopRustServers. Please try again later") --TODO: Config message
					return 
				end
	
				self:CheckResponse(response,player,"trs")
			end, self.Plugin)

		end
		
		---------------------------
		--rust-serverlist.net
		---------------------------		
		
		if(#self.Config.rslAPIKey >= 5)then
		
		local playerid = tostring(rust.UserIDFromPlayer(player))
			webrequests.EnqueueGet("http://www.rust-serverlist.net/api.php?sid="..tostring(self.Config.rslServerID).."}&apikey="..self.Config.rslAPIKey.."&uid="..rust.UserIDFromPlayer(player).."&mode=vote", function(code, response)

			if response == nil or code ~= 200 then 
				print("Could not connect to rust-serverlist.net") 
				rust.SendChatMessage(player, "[color #ff0000]ERROR! Couldn't connect to rust-serverlist.net. Please try again later") --TODO: Config message
				return 
			end
			self:CheckResponse(response,player,"rsl")
			end, self.Plugin)

		end--
		
		
	----------------------
	-- Rust-server.net check
	----------------------
		if(#self.Config.rssAPIKey >= 5)then
			
		local playerid = tostring(rust.UserIDFromPlayer(player))
			webrequests.EnqueueGet("http://rust-servers.net/api/?object=votes&element=claim&key="..tostring(self.Config.rssAPIKey).."&steamid="..rust.UserIDFromPlayer(player), function(code, response)

			if response == nil or code ~= 200 then 
				print("Could not connect to Rust-server.net") 
				rust.SendChatMessage(player, "[color #ff0000]ERROR! Couldn't connect to Rust-server.net. Please try again later") --TODO: Config message
				return 
		
			end
			
			self:CheckResponse(response,player,"rss")
			end, self.Plugin)
	
		end
		
	

	

	end
end


function PLUGIN:cmdReward(player,cmd,args)

	local plug = plugins.Find("PlayerDatabase")
	if(not plug) then
	
	print("Player Database is not Installed! http://oxidemod.org/plugins/player-database-mysql-support.927/")
	rust.SendChatMessage(player,"[color red]Player Database is not Installed![/color] http://oxidemod.org/plugins/player-database-mysql-support.927/")
		return
	end

	ConfigData = self.Config
	KitData = ConfigData["Rewards"]
	
	-------------------------------------------------
	--  No arguments : Print possible rewards and help
	-------------------------------------------------
	if(args.Length == 0) then

		rust.SendChatMessage(player, "[color #0066CC]---------------------------------------------------")
		rust.SendChatMessage(player, "Select your reward:")
		local playerTokens = self:CheckPlayerPoints(player)

		if (playerTokens == 0) then 
			rust.SendChatMessage(player, "[color red]You dont have any points.[/color]")
			else rust.SendChatMessage(player, "[color green]You have " .. playerTokens .. " points left.[/color]")
		end

		rust.SendChatMessage(player, "[color #0066CC]---------------------------------------------------")



		for key, value in pairs(KitData) do
			rust.SendChatMessage(player, "/reward[color yellow] "..KitData[key]["name"].. "[/color] - [color #D0D0D0]" ..KitData[key]["desc"].. " [/color]-[color yellow] "..KitData[key]["price"].." points")

		end

	else

		tmpkey = tostring(args[0])

	-------------------------------------------------
	-- Unknown argument : Print error message and userinput
	-------------------------------------------------
	
	if(KitData[tmpkey] == nil)then
		rust.SendChatMessage(player,"[color red] Can't find reward [/color] \""..args[0].."\"")

		return
	end
	
	-------------------------------------------------
	-- Kit Found || Check Kit prices and compare with player points
	-------------------------------------------------
	
	tmpData = KitData[tmpkey]
	kitItems = tmpData["items"]

	
	kitPrice = (KitData[tmpkey]["price"])
	local playerPoints = self:CheckPlayerPoints(player)
	
	--------------------------------------------------
	-- If enough points add items and send messages
	--------------------------------------------------
	
	if (tonumber(playerPoints) >= tonumber(kitPrice)) then	

		for key,value in pairs( kitItems ) do
			self:GiveItem(player,kitItems[key]["ItemName"],kitItems[key]["Amount"])
		end
		
		self:RemovePlayerPoints(player, kitPrice)
		
		rust.BroadcastChat(player.displayName.."  has voted and got [color cyan]"..KitData[tmpkey]["name"].."[/color] for it.")
		rust.BroadcastChat("Type /vote to get your rewards today!")	
		

	---------------------------------------------------
	-- No points | Error 
	---------------------------------------------------
	
	else 

	rust.SendChatMessage(player, self.Config.Messages.NotEnoughpoints..kitPrice)
	
	end
	end
end

function PLUGIN:CheckPlayerPoints(player)
	playerid = tostring(rust.UserIDFromPlayer(player))
	
	local plug = plugins.Find("PlayerDatabase")
	points = plug:Call("GetPlayerData", playerid, "rewardtokens")
	
	-- Check if rewardspoints has been added to database
	if(points == nil) then
		points = 0
		plug:Call("SetPlayerData", playerid, "rewardtokens", points)

	end

	return points
end 

function PLUGIN:SetPlayerPoints(player, points)
	playerid = tostring(rust.UserIDFromPlayer(player))
	
	local plug = plugins.Find("PlayerDatabase")

	
	-- Add reward points
	points = points + self.Config.PointsPerVote
	plug:Call("SetPlayerData", playerid, "rewardtokens", points)
	
end 

function PLUGIN:RemovePlayerPoints(player, price)
	playerid = tostring(rust.UserIDFromPlayer(player))
	
	local plug = plugins.Find("PlayerDatabase")

	
	-- remove reward points
	points = points - price
	plug:Call("SetPlayerData", playerid, "rewardtokens", points)
	
end 

function PLUGIN:CheckResponse(response,player,sitename)

	if(sitename == "trs")then
	
		webrequest = "http://api.toprustservers.com/api/put?plugin=voter&key="..tostring(self.Config.trsAPIKey).."&uid="..rust.UserIDFromPlayer(player)
		NotVoted = self.Config.Messages.trsNotVoted
		HasVoted = self.Config.Messages.trsHasVoted
		
	
	end
	
	if(sitename == "rsl")then
	
		webrequest = "http://www.rust-serverlist.net/api.php?sid="..tostring(self.Config.rslServerID).."}&apikey="..self.Config.rslAPIKey.."&uid="..rust.UserIDFromPlayer(player).."&mode=claimed"
		
		NotVoted = self.Config.Messages.rslNotVoted
		HasVoted = self.Config.Messages.rslHasVoted
		AlreadyVoted = self.Config.Messages.rslAlreadyVoted
	end

	if(sitename == "rss")then
	
		webrequest = "http://rust-servers.net/api/?action=post&object=votes&element=claim&key="..tostring(self.Config.rssAPIKey).."&steamid="..rust.UserIDFromPlayer(player)
		NotVoted = self.Config.Messages.rssNotVoted
		HasVoted = self.Config.Messages.rssHasVoted
		AlreadyVoted = self.Config.Messages.rssAlreadyVoted
		
	end


	if (response == "0") then
		rust.SendChatMessage(player, NotVoted)
		--print(playerid.." has not voted")--
		return		
	end

    -- If voted
    if (response == "1") then
    	rust.SendChatMessage(player, HasVoted)
    	playerid = tostring(rust.UserIDFromPlayer(player))
    	tokens = self:CheckPlayerPoints(player)
		
		self:SetPlayerPoints(player, tokens);
		rust.InventoryNotice(player, "+ "..self.Config.PointsPerVote.." Reward points!")

	-- Send message to API that points has been awarded 
	webrequests.EnqueueGet(webrequest, function(code, response)

			if response == nil or code ~= 200 then 
				print("Could not connect to rust-serverlist.net") 
				rust.SendChatMessage(player, "[color #ff0000]ERROR! Couldn't connect to rust-serverlist.net. Please try again later") --TODO: Config message
				return 
			end
		end, self.Plugin)

	
end

if (response == "2") then
		rust.SendChatMessage(player, AlreadyVoted)
			return		
	end
end

function PLUGIN:GiveItem(netuser,itemname, amountas)

	netuserID = rust.UserIDFromPlayer(netuser)
	
	local amount = tonumber( amountas ) or 1
	local pref = rust.InventorySlotPreference(global["Inventory+Slot+Kind"].Default, false, global["Inventory+Slot+KindFlags"].Default)
	local inv = netuser.playerClient.rootControllable.idMain:GetComponent( "Inventory" )
	if(playerInventory[netuserID] ~= nil)then
	inv = playerInventory[netuserID]
	end
	
	inv:AddItemAmount( global.DatablockDictionary.GetByName(itemname), amount, pref )

end

function PLUGIN:LoadDefaultConfig()
	
	self.Config.trsAPIKey = "xxx"
	
	self.Config.rssAPIKey = "xxx"
	self.Config.rslAPIKey = "xxx"
	self.Config.rslServerID = ""	
	
	
	self.Config.PointsPerVote = 1

	self.Config.Messages = {}
	self.Config.Messages.HelpMessage = "Vote us at http://toprustservers and earn some sweet voter rewards today!"
	self.Config.Messages.HelpMessage2 = "type [color cyan]/vote check[/color] to claim your points and [color cyan]/reward[/color] to see all possible rewards"
----
	
	
	self.Config.Messages.trsNotVoted = "You have not voted us yet at TopRustServer"	
	self.Config.Messages.trsHasVoted = "Thanks for voting us at TopRustServers"

	
	
	self.Config.Messages.rslNotVoted = "You have not voted us yet at Rust-serverlist.net"	
	self.Config.Messages.rslHasVoted = "Thanks for voting us at Rust-serverlist.net"
		
	
	self.Config.Messages.rslAlreadyVoted = "You have already voted us at Rust-serverlist.net! (You can vote every 24h)"
		
	self.Config.Messages.rssNotVoted = "You have not voted us yet at Rust-servers.net"	
	self.Config.Messages.rssHasVoted = "Thanks for voting us at Rust-servers.net"
	
	self.Config.Messages.rssAlreadyVoted = "You have already voted us at Rust-servers.net! (You can vote every 24h)"
	
	self.Config.Messages.NotEnoughpoints = "[color #ff0000]You don't have enough points! Need: "
	
	self.Config.Messages.Broadcast = "[color #ff0000]You don't have enough points! Need: "
	
	--
	self.Config.Rewards = {}
	self.Config.Rewards.materials = {}
	self.Config.Rewards.materials.price = 1
	self.Config.Rewards.materials.name = "materials"
	self.Config.Rewards.materials.desc = "Material pack (100 x WoodPlanks and 50 x Low Quality Metal)"
	self.Config.Rewards.materials.items = {}
	self.Config.Rewards.materials.items.a = {}
	self.Config.Rewards.materials.items.a.Amount = 100
	self.Config.Rewards.materials.items.a.ItemName = "Wood Planks"
	self.Config.Rewards.materials.items.b = {}
	self.Config.Rewards.materials.items.b.Amount = 50
	self.Config.Rewards.materials.items.b.ItemName = "Low Quality Metal"
	
	self.Config.Rewards.supply = {}
	self.Config.Rewards.supply.price = 1
	self.Config.Rewards.supply.name = "supply"
	self.Config.Rewards.supply.desc = "Your very own Supply Signal"
	self.Config.Rewards.supply.items = {}
	self.Config.Rewards.supply.items.a = {}
	self.Config.Rewards.supply.items.a.Amount = 1
	self.Config.Rewards.supply.items.a.ItemName = "Supply Signal"
	
	self.Config.Rewards.guns = {}
	self.Config.Rewards.guns.price = 1
	self.Config.Rewards.guns.name = "guns"
	self.Config.Rewards.guns.desc = "Go shoot your enemies"
	self.Config.Rewards.guns.items = {}
	self.Config.Rewards.guns.items.a = {}
	self.Config.Rewards.guns.items.a.Amount = 1
	self.Config.Rewards.guns.items.a.ItemName = "9mm Pistol"
	self.Config.Rewards.guns.items.b = {}
	self.Config.Rewards.guns.items.b.Amount = 100
	self.Config.Rewards.guns.items.b.ItemName = "9mm Ammo"
end