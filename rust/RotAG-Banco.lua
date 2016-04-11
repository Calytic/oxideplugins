PLUGIN.Name = "RotAG-Banco"
PLUGIN.Title = "RotAG-Banco"
PLUGIN.Version = V(1, 3, 0)
PLUGIN.Description = "Bank System to use with Economics from Bombardir."
PLUGIN.Author = "TheRotAG"
PLUGIN.HasConfig = true
PLUGIN.ResourceId = 735

----------------------------------------- LOCALS -----------------------------------------
local USERS, API, base_banco, cmds, msgs  = {}, {}, {}, {}, {}
local function SendMessage(player, msg)
	player:SendConsoleCommand("chat.add \"".. msgs.ChatName.."\" \"".. msg .."\"")
end
local function HasAcces(player)
	return player:GetComponent("BaseNetworkable").net.connection.authLevel >= API.Admin_LvL
end
------------------------------------------------------------------------------------------

----------------------------------------- API -----------------------------------------
function base_banco:Set(money)
	self[1] = money
	API.SaveData()
end

function base_banco:Transfer(base_bco, money)
	if self:Withdraw(money) then
		base_bco:Deposit(money)
		return true
	else
		return false 
	end
end

function base_banco:Deposit(money)
	self:Set(self[1] + money)
end

function base_banco:Withdraw(money)
	if self[1] >= money then
		self:Set(self[1] - money)
		return true
	else
		return false
	end
end

function GetBancoAPI()
	return API
end
 
function API.SaveData()
	datafile.SaveDataTable( "Banco" )
end

function API:GetUserDataFromPlayer(player)
	return self:GetUserData(rust.UserIDFromPlayer(player))
end

function API:GetUserData(steamid)
	local data = USERS[steamid]
	if not data then
		data = {}
		data[1] = self.SaldoInicial
		setmetatable(data, {__index = base_banco})
		USERS[steamid] = data
	end
	return data
end 
---------------------------------------------------------------------------------------

function PLUGIN:Init()
	if GetEconomyAPI then
		EcoAPI = GetEconomyAPI()
	else
		print("This Bank requires Economics! Please install: http://forum.rustoxide.com/plugins/economics.717/  ")
		return 
	end 
	
	USERS = datafile.GetDataTable( "Banco" ) or {}
	
	self.BankDataFile, self.BankData = USERS or {}
	
	API.SaldoInicial = self.Config.SaldoInicial or 10000
	API.Admin_LvL = self.Config.Admin_LvL or 2
	API.Limit = self.Config.Limit or 10000000
	API.DepositFee = self.Config.DepositFee or 5
	API.WithdrawFee = self.Config.WithdrawFee or 5
	
	self.Config.Limit = API.Limit
	self.Config.DepositFee = API.DepositFee
	self.Config.WithdrawFee = API.WithdrawFee
	self.Config.SaldoInicial = API.SaldoInicial
	self.Config.Admin_LvL = API.Admin_LvL
	self.Config.BaseLimpa = self.Config.BaseLimpa or true
	
	for k, v in pairs(USERS) do
		if self.Config.BaseLimpa and v[1] == API.SaldoInicial then -- Clean Base
			USERS[k] = nil
		else
			setmetatable(USERS[k], {__index = base_banco}) -- Bind Functions
		end 
	end
	datafile.SaveDataTable( "Banco" )
	
	self.Start = self.Config.Start
	self.Limit = self.Config.Limit
	self.DepositFee = self.Config.DepositFee
	self.WithdrawFee = self.Config.WithdrawFee
	self.TransferFee = self.Config.TransferFee
	self.AllowTrans = self.Config.AllowTrans
	
	self.Config.Commands = self.Config.Commands or {}
	self.Config.Commands.BB = self.Config.Commands.BB or "bb"
	self.Config.Commands.SetBB = self.Config.Commands.SetBB or "setbb"
	self.Config.Commands.TB = self.Config.Commands.TB or "tb"
	self.Config.Commands.DB = self.Config.Commands.DB or "db"
	self.Config.Commands.WB = self.Config.Commands.WB or "wb"
	
	self.Config.Messages = self.Config.Messages or {}
	self.Config.Messages.ChatName = self.Config.Messages.ChatName or "[Bank]"
	self.Config.Messages.NoPermission = self.Config.Messages.NoPermission or "No Permission!"
	self.Config.Messages.NoPlayer = self.Config.Messages.NoPlayer or "No Player Found!"
	self.Config.Messages.MultiplePlayersFound = self.Config.Messages.MultiplePlayersFound or "Multiple players found with that info!"
	self.Config.Messages.New_Player_Balance = self.Config.Messages.New_Player_Balance or "New player balance: %s"
	self.Config.Messages.Syntax_Error = self.Config.Messages.Syntax_Error or "Syntax Error! /%s <name/steamid> <money>"
	self.Config.Messages.Withdraw_Error = self.Config.Messages.Withdraw_Error or "You don't have enough money in the bank!"
	self.Config.Messages.My_Balance = self.Config.Messages.My_Balance or "Your Balance: %s"
	self.Config.Messages.Balance = self.Config.Messages.Balance or "Player Balance: %s"
	self.Config.Messages.Transfer_Money_Error = self.Config.Messages.Transfer_Money_Error or  "You do not have enough money!"
	self.Config.Messages.Transfer_Negative_Error = self.Config.Messages.Transfer_Negative_Error or "Money can not be negative!"
	self.Config.Messages.Transfer_Error = self.Config.Messages.Transfer_Error or "You can not transfer money to yourself!"
	self.Config.Messages.Transfer_Succes = self.Config.Messages.Transfer_Succes or "You have successfully transferred money to '%s'!"
	self.Config.Messages.Own_Transfer_Succes = self.Config.Messages.Own_Transfer_Succes or "You have successfully transferred money to your bank account!"
	self.Config.Messages.Transfer_Succes_To = self.Config.Messages.Transfer_Succes_To or "'%s' has transferred money to you! Check your bank account '/bb'!"
	self.Config.Messages.Save_Succes = self.Config.Messages.Save_Succes or "Bank data saved!"
	self.Config.Messages.DepositLimit = self.Config.Messages.DepositLimit or "This deposit will make your account exceed the Limit!"
	self.Config.Messages.TransferLimit = self.Config.Messages.TransferLimit or "This transfer will make your friend's account exceed the Limit!"
	self.Config.Messages.AccLimit = self.Config.Messages.AccLimit or "Your account already reached the limit!"
	self.Config.Messages.FriendAccLimit = self.Config.Messages.FriendAccLimit or "Your friend's account already reached the limit!"
	self.Config.Messages.Help = self.Config.Messages.Help or  {"use /bb to check your Bank Balance","use /tb \\\"friend's name\\\" amount -- to transfer money from your bank account to the bank account from a friend","use /db \\\"player name\\\" amount -- to deposit the requested amount from your wallet into the target player bank account","use /wb amount -- to withdraw the requested amount from your bank account to your wallet"}
	
	cmds = self.Config.Commands
	msgs = self.Config.Messages
	self:SaveConfig()

	command.AddConsoleCommand( "bnc.c", self.Plugin, "CC_Bnc" )

	if cmds.BB ~= "" then command.AddChatCommand(cmds.BB, self.Plugin, "C_BB") end
	if cmds.SetBB ~= "" then command.AddChatCommand(cmds.SetBB, self.Plugin, "C_SetBB") end
	if cmds.TB ~= "" then command.AddChatCommand(cmds.TB, self.Plugin, "C_TB") end
	if cmds.DB ~= "" then command.AddChatCommand(cmds.DB, self.Plugin, "C_DB") end
	if cmds.WB ~= "" then command.AddChatCommand(cmds.WB, self.Plugin, "C_WB") end	
end	

function PLUGIN:SendHelpText(player)
    for i=1,#msgs.Help do
		SendMessage(player, msgs.Help[i])
	end
end

function PLUGIN:C_TB(player, cmd, args)
	if args.Length > 1 then
		-- Search for the BasePlayer for the given (partial) name.
		local targetPlayer = self:FindPlayerByName( args[0] )

		-- Check if we found the targetted player.
		if #targetPlayer == 0 then
			-- The targetted player couldn't be found, send a message to the player.
			SendMessage( player, msgs.NoPlayer )

			return
		end

		-- Check if we found multiple players with that partial name.
		if #targetPlayer > 1 then
			-- Multiple players were found, send a message to the player.
			SendMessage( player, msgs.MultiplePlayersFound )

			return
		else
			-- Only one player was found, modify the targetPlayer variable value.
			target = targetPlayer[1]
			
		end
		local money = tonumber(args[1])	
		if money then
			if money > 0 then
				if target then
					if (target ~= player) then
						local pID = rust.UserIDFromPlayer(target)
						local moneyCheck = API:GetUserDataFromPlayer(player)[1]
						local data = API:GetUserDataFromPlayer(target)[1]
						local targetBB = API:GetUserDataFromPlayer(target)
						local isLimit = tonumber(data)
						if(isLimit < self.Limit) then
							if (money <= self.Limit) then
								if (money <= moneyCheck) then
									if API:GetUserDataFromPlayer(player):Transfer(API:GetUserDataFromPlayer(target), money) then
										SendMessage(player, msgs.Transfer_Succes:format(target.displayName))
										print("The user "..player.displayName.." transfered "..money.." to target.displayName")
										SendMessage(target, msgs.Transfer_Succes_To:format(player.displayName))
									end
								else
									SendMessage(player, msgs.Transfer_Money_Error)
								end
							else
								SendMessage(player, msgs.TransferLimit)
								return
							end
						else
							SendMessage( player, msgs.FriendAccLimit)
							return
						end
					else
						SendMessage(player, msgs.Transfer_Error)
					end
				else
					SendMessage(player, msgs.NoPlayer)
				end
			else
				SendMessage(player, msgs.Transfer_Negative_Error)
			end
		else
			SendMessage(player, msgs.Syntax_Error:format(cmds.Transfer))
		end
	else
		SendMessage(player, msgs.Syntax_Error:format(cmds.Transfer))
	end
end

function PLUGIN:C_BB(player, cmd, args)
	if args.Length > 0 then
		if HasAcces(player) then
			-- Search for the BasePlayer for the given (partial) name.
			local targetPlayer = self:FindPlayerByName( args[0] )

			-- Check if we found the targetted player.
			if #targetPlayer == 0 then
				-- The targetted player couldn't be found, send a message to the player.
				SendMessage( player, msgs.NoPlayer )

				return
			end

			-- Check if we found multiple players with that partial name.
			if #targetPlayer > 1 then
				-- Multiple players were found, send a message to the player.
				SendMessage( player, msgs.MultiplePlayersFound )

				return
			else
				-- Only one player was found, modify the targetPlayer variable value.
				target = targetPlayer[1]
				
			end
			if target then
				SendMessage(player, msgs.Balance:format( API:GetUserDataFromPlayer(target)[1] )) 
			else
				SendMessage(player, msgs.NoPlayer)
			end
		else
			SendMessage(player, msgs.NoPermission)
		end
	else
		SendMessage(player, msgs.My_Balance:format(API:GetUserDataFromPlayer(player)[1]))
	end
end

function PLUGIN:C_SetBB(player, cmd, args) 
	if HasAcces(player) then
		-- Search for the BasePlayer for the given (partial) name.
		local targetPlayer = self:FindPlayerByName( args[0] )

		-- Check if we found the targetted player.
		if #targetPlayer == 0 then
			-- The targetted player couldn't be found, send a message to the player.
			SendMessage( player, msgs.NoPlayer )

			return
		end

		-- Check if we found multiple players with that partial name.
		if #targetPlayer > 1 then
			-- Multiple players were found, send a message to the player.
			SendMessage( player, msgs.MultiplePlayersFound )

			return
		else
			-- Only one player was found, modify the targetPlayer variable value.
			target = targetPlayer[1]
			
		end
		if args.Length > 1 then
			local money = tonumber(args[1])
			if money then
				if target then
					local data = API:GetUserDataFromPlayer(target)
					data:Set(money)
					SendMessage(player, msgs.New_Player_Balance:format( data[1] )) 
				else
					SendMessage(player, msgs.NoPlayer)
				end
			else
				SendMessage(player, msgs.Syntax_Error:format(cmds.SetMoney))
			end
		else
			SendMessage(player, msgs.Syntax_Error:format(cmds.SetMoney))
		end
	else
		SendMessage(player, msgs.NoPermission)
	end
end

function PLUGIN:C_DB(player, cmd, args)
	local money = 0
	local target
	if args.Length > 1 then
		-- Search for the BasePlayer for the given (partial) name.
		local targetPlayer = self:FindPlayerByName( args[0] )

		-- Check if we found the targetted player.
		if #targetPlayer == 0 then
			-- The targetted player couldn't be found, send a message to the player.
			SendMessage( player, msgs.NoPlayer )

			return
		end

		-- Check if we found multiple players with that partial name.
		if #targetPlayer > 1 then
			-- Multiple players were found, send a message to the player.
			SendMessage( player, msgs.MultiplePlayersFound )

			return
		else
			-- Only one player was found, modify the targetPlayer variable value.
			target = targetPlayer[1]
			
		end
		money = tonumber(args[1])
		if money then
			if money > 0 then
				if target then
					local pID = rust.UserIDFromPlayer(target)
					local depositFee = math.floor(money * (self.DepositFee / 100))
					local moneyCheck = EcoAPI:GetUserDataFromPlayer(player)[1]
					local data = API:GetUserDataFromPlayer(target)[1]
					local targetBB = API:GetUserDataFromPlayer(target)
					local isLimit = tonumber(data)
					if(isLimit < self.Limit) then
						if (money <= self.Limit) then
							if (money <= moneyCheck) then
								EcoAPI:GetUserDataFromPlayer(player):Withdraw(money)
								targetBB:Deposit(money)
								targetBB:Withdraw(depositFee)
								SendMessage(player, msgs.Transfer_Succes:format(target.displayName))
								SendMessage(target, msgs.Transfer_Succes_To:format(player.displayName))
							else
								SendMessage(player, msgs.Transfer_Money_Error)
							end
						else
							SendMessage(player, msgs.DepositLimit)
							return
						end
					else
						SendMessage( player, msgs.AccLimit)
						return
					end
				else
					return
				end
			else
				return
			end
		end
	elseif args.Length == 1 then
		money = tonumber(args[0])
		local depositFee = math.floor(money * (self.DepositFee / 100))
		local moneyCheck = EcoAPI:GetUserDataFromPlayer(player)[1]
		local data = API:GetUserDataFromPlayer(player)[1]
		local targetBB = API:GetUserDataFromPlayer(player)
		local isLimit = tonumber(data)
		local pID = rust.UserIDFromPlayer(player)
		if(isLimit < self.Limit) then
			if (money <= self.Limit) then
				if (money <= moneyCheck) then
					if (tonumber(moneyCheck) > money) then		
						EcoAPI:GetUserDataFromPlayer(player):Withdraw(money)
						targetBB:Deposit(money)
						targetBB:Withdraw(depositFee)
						SendMessage(player, msgs.Own_Transfer_Succes)
					else
						SendMessage(player, msgs.Transfer_Money_Error)
					end
				else
					SendMessage(player, msgs.Transfer_Money_Error)
				end
			else
				SendMessage(player, msgs.DepositLimit)
				return
			end
		else
			SendMessage( player, msgs.AccLimit)
			return
		end
	else
		SendMessage(player, msgs.Syntax_Error:format(cmds.DB))
	end
end

function PLUGIN:C_WB(player, cmd, args) 
	if args.Length > 0 then
		local money = tonumber(args[0])
		if money then
			local WithdrawWithFee = money + math.floor(money * (self.WithdrawFee / 100))
			local WithdrawFee = WithdrawWithFee - money	
			local balanceCheck = API:GetUserDataFromPlayer(player)[1]
			local playerBB = API:GetUserDataFromPlayer(player)
			local playerEco = EcoAPI:GetUserDataFromPlayer(player)
			if (tonumber(balanceCheck) >= WithdrawWithFee) then
				playerBB:Withdraw(WithdrawWithFee)
				playerEco:Deposit(money)
				SendMessage(player, msgs.New_Player_Balance:format( playerEco[1] ))
			else
				SendMessage(player, msgs.Withdraw_Error)
			end
		else
			SendMessage(player, msgs.Syntax_Error:format(cmds.WB))
		end
	else
		SendMessage(player, msgs.Syntax_Error:format(cmds.WB))
	end
end

function PLUGIN:CC_Bnc(arg)
	local reply = ""
	local player
	if arg.connection then
		player = arg.connection.player
	end
	if not player or HasAcces(player) then
		local cmd = arg:GetString( 0, "" )
		if cmd == "save" then
			API.SaveData()
			reply = "Banco data saved!"
		elseif cmd == "deposit" or cmd == "balance" or cmd == "withdraw" or cmd == "setmoney" then
			local steam  = arg:GetString( 1, "" )
			local target = global.BasePlayer.Find(steam)
			local userdata
			if target then
				userdata = API:GetUserDataFromPlayer( target )
				steam = target.displayName
			elseif steam:match("%d%d%d%d%d%d%d%d%d%d%d%d%d%d%d%d%d") then
				userdata = API:GetUserData(steam)
			end
			if userdata then
				if cmd == "balance" then
					reply =  "Balance(" .. steam .. ") = " .. tostring(userdata[1]) 
				else
					local money = tonumber(arg:GetString( 2, "" ))
					if money then
						if cmd == "setmoney" then
							userdata:Set(money)
							reply = "(SetMoney) New '" .. steam .. "' balance: " .. tostring(userdata[1])
						elseif cmd == "deposit" then
							userdata:Deposit(money)
							reply = "(Deposit) New '" .. steam .. "' balance: " .. tostring(userdata[1])
						elseif userdata:Withdraw(money) then
							reply = "(Withdraw) New '" .. steam .. "' balance: " .. tostring(userdata[1])
						else
							reply = "This user doesn't have enough money!"
						end
					else
						reply =  "Syntax Error! (bnc.c " .. cmd .. " <steam/name> <money>)"
					end
				end
			else
				reply = "No user with steam/name: '" .. steam .. "' !"
			end
		else
			reply = "Economy Commands: 'bnc.c deposit', 'bnc.c save','bnc.c balance', 'bnc.c withdraw', 'bnc.c setmoney'"  
		end
	else
		reply = "No permission!"
	end
	arg:ReplyWith(reply)
	return true
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