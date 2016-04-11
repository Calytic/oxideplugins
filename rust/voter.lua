PLUGIN.Title = "Voter"
PLUGIN.Version = V(1, 4, 0)
PLUGIN.Description = "Rewards players for voting."
PLUGIN.Author = "Bombardir"
PLUGIN.HasConfig = true
PLUGIN.ResourceId = 752
 
----------------------------------------- LOCALS -----------------------------------------
local USERS, API, msgs, settings, requests, ITEMS  = {}, {}, {}, {}, {}, {}
local function SendMessage(player, msg, chatname)
	player:SendConsoleCommand("chat.add", (msgs.ChatPlayerIcon and rust.UserIDFromPlayer(player)) or 0, msgs.ChatFormat:gsub("{NAME}", chatname or msgs.ChatName):gsub("{MESSAGE}", msg))
end
------------------------------------------------------------------------------------------
function PLUGIN:Init()
	if GetEconomyAPI then
		API = GetEconomyAPI()
	end 
	settings = self.Config.Settings or {}

	settings.VoteCommand = settings.VoteCommand or "vote"
	settings.RewardsCommand = settings.RewardsCommand or "rewards"
	settings.DataFile = settings.DataFile or "VoterData"
	 
	local trackers = {
	["[TopRustServers]"] ={ "http://api.toprustservers.com/api/put?plugin=voter&key={KEY}&uid=", "toprustservers.com/server/" }, 
	["[Rust-Servers]"] = { "http://rust-servers.net/api/?action=custom&object=plugin&element=reward&key={KEY}&steamid=", "rust-servers.net/server/" }, 
	["[Rust-ServerList]"] = { "http://rust-serverlist.net/api.php?apikey={KEY}&mode=vote&uid=", "rust-serverlist.net/server.php?id=" } 
	} 
	settings.Trackers = settings.Trackers or {}
	for tracker, table in pairs(trackers) do
		settings.Trackers[tracker] = settings.Trackers[tracker] or {}
		settings.Trackers[tracker].Key = settings.Trackers[tracker].Key or ""
		settings.Trackers[tracker].ID = settings.Trackers[tracker].ID or ""
		settings.Trackers[tracker].PointsForVote = settings.Trackers[tracker].PointsForVote or 1
		if settings.Trackers[tracker].Key ~= "" and settings.Trackers[tracker].ID ~= "" then
			requests[tracker] = {}
			requests[tracker][1] = table[1]:gsub("{KEY}", settings.Trackers[tracker].Key)
			requests[tracker][2] = table[2] .. settings.Trackers[tracker].ID
		end
	end
	 
	settings.Rewards = settings.Rewards or {{ price = 3, reward = { burlap_shoes = 1, burlap_shoes_bp = 1, bow_hunting = 2 } }, { price = 4, reward = { shotgun_waterpipe = 2, Money = 50 } },{ price = 5, reward = { smg_thompson = 1, Money = 400 } }}
	self.Config.Settings = settings
	
	
	msgs = self.Config.Messages or {}
	msgs.ChatFormat = msgs.ChatFormat or "<color=#af5>{NAME}:</color> {MESSAGE}" 
	if msgs.ChatPlayerIcon == nil then msgs.ChatPlayerIcon = true end
	msgs.ChatName = msgs.ChatName or "[Voter]"
	
	msgs.RewardsBegin = (msgs.RewardsBegin or "-------- /%s [id] (Get Reward) --------"):format(settings.RewardsCommand)
	msgs.RewardsBalance = msgs.RewardsBalance or "Your points: %s"
	msgs.RewardsList = msgs.RewardsList or "Points: {PRICE}, Reward: {REWARD}"
	msgs.RewardsEnd = msgs.RewardsEnd or "-------------------------------------------------"
	msgs.RewardNotFound = msgs.RewardNotFound or "Reward with this ID cann't be found!"
	msgs.RewardNotPoints = msgs.RewardNotPoints or  "You do not have enough points for this reward!"
	msgs.RewardGived = msgs.RewardGived or  "You got your reward!"
	
	msgs.StatusCanVote = msgs.StatusCanVote or "You can vote at '%s' (Points for voting: %i)"
	msgs.StatusGetPoint = msgs.StatusGetPoint or "Thanks for vote! (Points received: %s)"
	msgs.StatusBadApiKey = msgs.StatusBadApiKey or "Invalid API key."
	msgs.StatusNotAvailable = msgs.StatusNotAvailable or "The tracker is not available now. Please try again later."
	msgs.Help = msgs.Help or  {"/vote -- show a list of available urls for voting and get points for voting","/rewards -- show the rewards information and your points","/rewards [ID] -- get the reward"}
	
	self.Config.Messages = msgs
	
	self:SaveConfig()
	
	USERS = datafile.GetDataTable( settings.DataFile ) or {}
	
	command.AddChatCommand(settings.VoteCommand, self.Plugin, "C_Vote")
	command.AddChatCommand(settings.RewardsCommand, self.Plugin, "C_Rewards")
end

function PLUGIN:OnServerInitialized()
	-- Credits to Reneb (http://forum.rustoxide.com/plugins/give.666/)
	local it = global.ItemManager.GetItemDefinitions():GetEnumerator()
	while (it:MoveNext()) do
		ITEMS[tostring(it.Current.shortname)] = it.Current.displayname.translated
	end
	------------------------------------------------------------------
end 

function PLUGIN:SendHelpText(player)
    for i=1,#msgs.Help do
		SendMessage(player, msgs.Help[i])
	end
end


function PLUGIN:C_Vote(player)
	local steamid = rust.UserIDFromPlayer(player)
	for shortname, table in pairs(requests) do
		local url = table[1]..steamid
		webrequests.EnqueueGet(url, 
		function(code, content)
			if (code == 200) then
				local cont = tostring(content)
				if cont == "1" then
					local points = settings.Trackers[shortname].PointsForVote
					USERS[steamid] = (USERS[steamid] or 0) + points
					SendMessage(player, msgs.StatusGetPoint:format(points) , shortname)
					datafile.SaveDataTable( settings.DataFile )
					if shortname == "[Rust-ServerList]" then
						webrequests.EnqueueGet(url:gsub("vote", "claimed"), function() end, self.Plugin)
					end
				elseif cont == "API NOT SET UP" or cont == "Error: incorrect server key" or cont == "Bad APIKEY" then
					SendMessage(player, msgs.StatusBadApiKey, shortname)
				else
					SendMessage(player, msgs.StatusCanVote:format(table[2], settings.Trackers[shortname].PointsForVote), shortname)
				end
			else
				SendMessage(player, msgs.StatusNotAvailable, shortname)
			end
		end, self.Plugin )
	end
end

function PLUGIN:C_Rewards(player, cmd, args)
	local arg2 = args.Length > 0 and tonumber(args[0])
	if arg2 then
		local reward = settings.Rewards[arg2]  
		if reward then
			local steamid = rust.UserIDFromPlayer(player)
			local data = USERS[steamid] or 0
			if data >= reward.price then
				local inv = player.inventory
				for shortname, amount in pairs(reward.reward) do
					if shortname == "Money" then
						if API then API:GetUserDataFromPlayer(player):Deposit(amount) end
					else
						local name, count = shortname:gsub("_bp", "")
						if count == 0 then
							item = global.ItemManager.CreateByName(name, amount)
						else
							local def = global.ItemManager.FindItemDefinition.methodarray[1]:Invoke(nil, util.TableToArray( { name } ) )
							if def then item = global.ItemManager.CreateByItemID(def.itemid,amount,true) end
						end
						if item then inv:GiveItem(item) end
					end
				end
				USERS[steamid] = data - reward.price
				datafile.SaveDataTable( settings.DataFile )
				SendMessage(player, msgs.RewardGived)
			else
				SendMessage(player, msgs.RewardNotPoints)
			end
		else
			SendMessage(player,  msgs.RewardNotFound)
		end
	else  
		SendMessage(player,  msgs.RewardsBegin)
		SendMessage(player, msgs.RewardsBalance:format(USERS[rust.UserIDFromPlayer(player)] or 0))
		for i=1, #settings.Rewards do		
			local reward = settings.Rewards[i]
			local msg = msgs.RewardsList:gsub("{PRICE}", tostring(reward.price))
			local names = {}
			for shortname, amount in pairs (reward.reward) do
				local name, count = shortname:gsub("_bp", "")
				table.insert(names, (ITEMS[name] or name) .. ((count > 0 and " BP") or "") .. " x" .. tostring(amount) )
			end
			SendMessage(player, msg:gsub("{REWARD}", table.concat(names, ", ")), "[ID " .. tostring(i) .. "]" )
		end
		SendMessage(player,  msgs.RewardsEnd)
	end
end