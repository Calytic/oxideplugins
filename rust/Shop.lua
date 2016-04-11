PLUGIN.Title = "Shop"
PLUGIN.Version = V(1, 1, 0)
PLUGIN.Description = ""
PLUGIN.Author = "Bombardir"
PLUGIN.HasConfig = true
PLUGIN.ResourceId = 721  
   
 ----------------------------------------- LOCALS -----------------------------------------
local data, API, cmds, msgs, gen  = {}, {}, {}, {}, {}
local function SendChatMessage(player, msg)
	player:SendConsoleCommand("chat.add", (msgs.ChatPlayerIcon and rust.UserIDFromPlayer(player)) or 0, msgs.ChatFormat:format(msg))
end
function PLUGIN:GeneratePriceList()
	local enum = global.ItemManager.GetItemDefinitions():GetEnumerator()
	while enum:MoveNext() do
		local bps = enum.Current:GetComponent("ItemBlueprint")
		if not bps or not bps.userCraftable then
			local name    = enum.Current.displayName.translated
			local price = data.base[name] or 0
			data.base[name] = price
			data.generated[tostring(enum.Current.itemid)] = {price, price*gen.Sell_Modificator, false, name}
		end
	end
	local ASDF = true
	while ASDF do
		ASDF = false
		local enum = global.ItemManager.GetItemDefinitions():GetEnumerator()
		while enum:MoveNext() do
			local bps = enum.Current:GetComponent("ItemBlueprint")
			if bps and bps.userCraftable then 
				local name    = tostring(enum.Current.itemid)
				data.generated[name] = {false, false, false, enum.Current.displayName.translated}
				local bp_enum = bps.ingredients:GetEnumerator()
				while bp_enum:MoveNext() do
					local ingredient = bp_enum.Current.itemDef.displayName.translated
					local ingredient_id = tostring(bp_enum.Current.itemid)
					if gen.Generate_Ingredients then
						data.generated[name][5] = data.generated[name][5] or {}
						data.generated[name][5][ingredient] = bp_enum.Current.amount
					end
					local price = data.base[ingredient] or (data.generated[ingredient_id] and data.generated[ingredient_id][1])
					if price then
						local price = price*bp_enum.Current.amount
						data.generated[name][1] = (data.generated[name][1] or 0) + price
						data.generated[name][2] = data.generated[name][1]*gen.Sell_Modificator
						data.generated[name][3] = data.generated[name][1]*gen.Blueprint_Modificator
					else
						ASDF = true
					end
				end  
			end 
		end 
	end
	self.Config.Generate.New_Price_List = false
	self.Config.PriceList = data
	self:SaveConfig()
	print("Price List Generated!")  
end 

local function FindName( name )
	name = name:lower():gsub("[%(%)%.%%%+%-%*%?%[%]%^%$]", "%%%0")
	local finds, bool =  nil, false
	for item_id, tbl in pairs(data.generated) do
		local name2 = tbl[4]:lower()
		if name2 == name then
			finds = { item_id, tbl }
			bool  = true
			break
		end
		if name2:find(name) then
			if finds then
				if bool then
					finds = { finds[2][4], tbl[4] }
					bool = false
				else
					table.insert(finds, tbl[4])
				end
			else
				finds = { item_id, tbl }
				bool  = true
			end
		end
	end
	return bool, finds
end
------------------------------------------------------------------------------------------         
 
function PLUGIN:Init()
	if GetEconomyAPI then
		API = GetEconomyAPI()
	else
		print("This Shop requires Economics! Please install: http://forum.rustoxide.com/plugins/economics.717/  ")
		return 
	end 
	
	gen  = self.Config.Generate or {}
	gen.New_Price_List = gen.New_Price_List
	if gen.New_Price_List == nil then gen.New_Price_List = true end
	gen.Generate_Ingredients = gen.Generate_Ingredients or false
	gen.Sell_Modificator = gen.Sell_Modificator or 0.5
	gen.Blueprint_Modificator = gen.Blueprint_Modificator or 2
	gen.List_Items_Per_Page = gen.List_Items_Per_Page or 7
	self.Config.Generate = gen
	
	cmds = self.Config.Commands or {}
	cmds.Buy = cmds.Buy or "buy"
	cmds.Sell = cmds.Sell or "sell"
	cmds.List = cmds.List or "bsl"          
	self.Config.Commands = cmds        
	  
	msgs = self.Config.Message or {}
	msgs.ChatFormat = msgs.ChatFormat or "<color=#af5>[Shop]</color> %s" 
	if msgs.ChatPlayerIcon == nil then msgs.ChatPlayerIcon = true end
	msgs.Syntax_Error = msgs.Syntax_Error or "Syntax Error! /%s [\"item\" or \"item_bp\"] [<amount>]"
	msgs.Buy_Succes = msgs.Buy_Succes or  "Thank you for your purchase! We are waiting for you again! (You spent: %s)"
	msgs.Buy_Error = msgs.Buy_Error or  "You do not have enough money (still need: %s)!"
	msgs.Buy_Error2 = msgs.Buy_Error2 or  "You don't have enough space in your inventory!"
	msgs.Buy_Price = msgs.Buy_Price or  "The purchase price of this item: %s"
	msgs.Buy_Not = msgs.Buy_Not or  "This item can not be bought."
	msgs.Blueprint_Indicator = msgs.Blueprint_Indicator or "_bp"
	msgs.Sell_Succes = msgs.Sell_Succes or  "You have successfully sold item for %s!"
	msgs.Sell_Error = msgs.Sell_Error or  "You can't sell what you do not have!"
	msgs.Sell_Price = msgs.Sell_Price or  "Sale price of this item: %s"
	msgs.Sell_Not = msgs.Sell_Not or  "This item can not be sold."
	msgs.Not_Find = msgs.Not_Find or  "The item with this name can't be found in the store!"
	msgs.Matches = msgs.Matches or  "Found multiple items: %s"
	msgs.List = msgs.List or  "Name: {Name}, Price: {PPrice}, Sale: {SPrice}, Blueprint: {BPrice}"
	msgs.List_Beg = msgs.List_Beg or  "------------------- PAGE %s -------------------"
	msgs.List_No_Sale = msgs.List_No_Sale or  "no"
	msgs.List_No_Purchase = msgs.List_No_Purchase or  "no"
	msgs.List_No_Blueprint = msgs.List_No_Blueprint or  "no"
	msgs.List_End = msgs.List_End or  "-------------------------------------------------"
	msgs.Help = msgs.Help or  {"/sell \"item\" -- Shows the sales price","/sell \"item\" <amount> -- Sell item","/buy \"item\" -- Shows the purchase price","/buy \"item\" <amount> -- Buy item", "/buy \"item_bp\" <amount> -- Buy item blueprint","/bsl [<page>] -- Shows a list of the prices of a specific page"}
	self.Config.Message = msgs
 
	data = self.Config.PriceList or {}
	data.base = data.base or {}
	data.generated = data.generated or {}
	self.Config.PriceList = data
	print("Price List Loaded!") 
	self:SaveConfig()
	
	if cmds.Buy ~= "" then command.AddChatCommand(cmds.Buy, self.Plugin, "C_Buy") end
	if cmds.Sell ~= "" then command.AddChatCommand(cmds.Sell, self.Plugin, "C_Sell") end
	if cmds.List ~= "" then command.AddChatCommand(cmds.List, self.Plugin, "C_List") end
end

function PLUGIN:OnServerInitialized()
	if gen.New_Price_List then
		self:GeneratePriceList() 
	end
end

function PLUGIN:SendHelpText(player)
    for i=1,#msgs.Help do
		SendChatMessage(player, msgs.Help[i])
	end
end 

function PLUGIN:C_Buy(player, cmd, args)
	if args.Length > 0 then
		local name, count = args[0]:gsub(msgs.Blueprint_Indicator, "")
		local isBP = count > 0
		local b, tbl = FindName(name)
		if b then
			local item_price
			if isBP then 
				item_price = tbl[2][3]
			else 
				item_price = tbl[2][1]
			end
			if item_price then
				local amount = args.Length > 1 and (tonumber(args[1]) or 1)
				if amount and amount > 0 then
					amount = math.floor(amount)
					local money = (isBP and item_price) or item_price*amount
					local user_ec = API:GetUserDataFromPlayer(player) 
					if user_ec:Withdraw(money) then
						if isBP then
							--local arr = util.TableToArray( { 0 } )
							--util.ConvertAndSetOnArray( arr, 0, tonumber(tbl[1]), System.Int32._type )
							--player.blueprints:Learn(global.ItemManager.FindItemDefinition.methodarray[0]:Invoke(nil,arr))
							if player.inventory:GiveItem(global.ItemManager.CreateByItemID(tonumber(tbl[1]),1,true)) then
								SendChatMessage(player, msgs.Buy_Succes:format(money))
							else
								SendChatMessage(player, msgs.Buy_Error2)
							end
						else
							if player.inventory:GiveItem(tonumber(tbl[1]), amount, true) then
								SendChatMessage(player, msgs.Buy_Succes:format(money))
							else
								SendChatMessage(player, msgs.Buy_Error2)
							end
						end
					else
						SendChatMessage(player, msgs.Buy_Error:format(money-user_ec[1]))
					end
				else
					SendChatMessage(player, msgs.Buy_Price:format(item_price))
				end
			else
				SendChatMessage(player, msgs.Buy_Not)
			end
		elseif tbl then
			SendChatMessage(player, msgs.Matches:format(table.concat(tbl, ", ")))
		else
			SendChatMessage(player, msgs.Not_Find)
		end
	else
		SendChatMessage(player, msgs.Syntax_Error:format(cmds.Buy))
	end
end

function PLUGIN:C_Sell(player, cmd, args)
	if args.Length > 0 then
		local b, tbl = FindName(args[0])
		if b then
			local item_price = tbl[2][2]
			if item_price then
				local amount = args.Length > 1 and (tonumber(args[1]) or 1)
				if amount and amount > 0 then
					amount = math.floor(amount)
					local inv = player.inventory
					local item_id = tonumber(tbl[1])
					if inv:GetAmount(item_id) >= amount  then
						inv:Take(inv:FindItemIDs(item_id), item_id, amount)
						local money = item_price*amount
						API:GetUserDataFromPlayer(player):Deposit(money)
						SendChatMessage(player, msgs.Sell_Succes:format(money))
					else
						SendChatMessage(player, msgs.Sell_Error)
					end
				else
					SendChatMessage(player, msgs.Sell_Price:format(item_price))
				end
			else
				SendChatMessage(player, msgs.Sell_Not)
			end
		elseif tbl then
			SendChatMessage(player, msgs.Matches:format(table.concat(tbl, ", ")))
		else
			SendChatMessage(player, msgs.Not_Find)
		end
	else
		SendChatMessage(player, msgs.Syntax_Error:format(cmds.Sell))
	end
end
 

function PLUGIN:C_List(player, cmd, args)
	local amount = (args.Length > 0 and tonumber(args[0])) or 1
	amount = math.floor(amount)
	local list_cur = 1
	local list_beh = (amount-1)*gen.List_Items_Per_Page
	local list_end = list_beh + gen.List_Items_Per_Page
	SendChatMessage(player, msgs.List_Beg:format(amount) )
	for _, table in pairs(data.generated) do
		if list_cur >= list_beh then
			if list_cur < list_end then
				SendChatMessage(player, msgs.List:gsub("{PPrice}", table[1] or msgs.List_No_Purchase):gsub("{SPrice}", table[2] or msgs.List_No_Sale):gsub("{BPrice}", table[3] or msgs.List_No_Blueprint):gsub("{Name}", table[4]) )
			else
				break
			end
		end
		list_cur = list_cur + 1    
	end
	SendChatMessage(player, msgs.List_End)
end