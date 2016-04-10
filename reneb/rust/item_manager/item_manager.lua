PLUGIN.Name = "item_manager"
PLUGIN.Title = "Item Manager Plugin"
PLUGIN.Version = V(0, 1, 10)
PLUGIN.Description = "This will allow you to easily manage players inventories (use as plugin call or copy the code)"
PLUGIN.Author = "Reneb"
PLUGIN.HasConfig = false
	
function PLUGIN:Init()
end
function PLUGIN:InitializeTable()
	self.Table = {}
	local itemlist = global.ItemManager.GetItemDefinitions();
	local it = itemlist:GetEnumerator()
	while (it:MoveNext()) do
		local correctname = string.lower(it.Current.displayname)
		self.Table[correctname] = tostring(it.Current.shortname)
	end
end
function PLUGIN:FindItemWear( inv, name )
	if(not self.Table) then self:InitializeTable() end
	name = string.lower(name)
	local wearlist = inv.containerWear.itemList
	local it = wearlist:GetEnumerator()
	while (it:MoveNext()) do
		if(string.lower(tostring(it.Current.info.displayname)) == name) then
			return it.Current
		end
	end
	return false
end
function PLUGIN:FindItemMain( inv, name )
	if(not self.Table) then self:InitializeTable() end
	name = string.lower(name)
	local mainlist = inv.containerMain.itemList
	local it = mainlist:GetEnumerator()
	while (it:MoveNext()) do
		if(string.lower(tostring(it.Current.info.displayname)) == name) then
			return it.Current
		end
	end
	return false
end
function PLUGIN:FindItemBelt( inv, name )
	if(not self.Table) then self:InitializeTable() end
	name = string.lower(name)
	local mainlist = inv.containerBelt.itemList
	local it = beltlist:GetEnumerator()
	while (it:MoveNext()) do
		if(string.lower(tostring(it.Current.info.displayname)) == name) then
			return it.Current
		end
	end
	return false
end
function PLUGIN:GetItemBySlot( inv, slot, type )
	if(not self.Table) then self:InitializeTable() end
	type = string.lower(type)
	local container
	if(type == "belt") then
		container = inv.containerBelt
	elseif(type == "main") then
		container = inv.containerMain
	elseif(type == "wear") then
		container = inv.containerWear
	else
		return false, "wrong type: belt, main or wear"
	end
	if(slot+1 > container.capacity) then
		return false, "slot out of range"
	end
	return container:GetSlot(slot)
end
function PLUGIN:FindItemAllByName( inv, name )
	if(not self.Table) then self:InitializeTable() end
	name = string.lower(name)
	local beltlist = inv.containerBelt.itemList
	local itbelt = beltlist:GetEnumerator()
	while (itbelt:MoveNext()) do
		if(string.lower(tostring(itbelt.Current.info.displayname)) == name) then
			return itbelt.Current
		end
	end
	local mainlist = inv.containerMain.itemList
	local itmain = mainlist:GetEnumerator()
	while (itmain:MoveNext()) do
		if(string.lower(tostring(itmain.Current.info.displayname)) == name) then
			return itmain.Current
		end
	end
	local wearlist = inv.containerWear.itemList
	local itwear = wearlist:GetEnumerator()
	while (itwear:MoveNext()) do
		if(string.lower(tostring(itwear.Current.info.displayname)) == name) then
			return itwear.Current
		end
	end
	return false
end
function PLUGIN:FindItemsAllByName( inv, name )
	if(not self.Table) then self:InitializeTable() end
	name = string.lower(name)
	local beltlist = inv.containerBelt.itemList
	local mainlist = inv.containerMain.itemList
	local wearlist = inv.containerWear.itemList
	local itbelt = beltlist:GetEnumerator()
	local itmain = mainlist:GetEnumerator()
	local itwear = wearlist:GetEnumerator()
	local tbl = {}
	local count = 0
	while (itbelt:MoveNext()) do
		if(string.lower(tostring(itbelt.Current.info.displayname)) == name) then
			tbl[ #tbl + 1] = itbelt.Current
			count = count + itbelt.Current.amount
		end
	end
	while (itmain:MoveNext()) do
		if(string.lower(tostring(itmain.Current.info.displayname)) == name) then
			tbl[ #tbl + 1] = itmain.Current
			count = count + itmain.Current.amount
		end
	end
	while (itwear:MoveNext()) do
		if(string.lower(tostring(itwear.Current.info.displayname)) == name) then
			tbl[ #tbl + 1] = itwear.Current
			count = count + itwear.Current.amount
		end
	end
	return tbl, count
end
function PLUGIN:FindItemsMainByName( inv, name )
	if(not self.Table) then self:InitializeTable() end
	name = string.lower(name)
	local mainlist = inv.containerMain.itemList
	local it = mainlist:GetEnumerator()
	local tbl = {}
	local count = 0
	while (it:MoveNext()) do
		if(string.lower(tostring(it.Current.info.displayname)) == name) then
			tbl[ #tbl + 1] = it.Current
			count = count + it.Current.amount
		end
	end
	return tbl, count
end
function PLUGIN:FindItemsWearByName( inv, name )
	if(not self.Table) then self:InitializeTable() end
	name = string.lower(name)
	local wearlist = inv.containerWear.itemList
	local it = wearlist:GetEnumerator()
	local tbl = {}
	local count = 0
	while (it:MoveNext()) do
		if(string.lower(tostring(it.Current.info.displayname)) == name) then
			tbl[ #tbl + 1] = it.Current
			count = count + it.Current.amount
		end
	end
	return tbl, count
end
function PLUGIN:FindItemsBeltByName( inv, name )
	if(not self.Table) then self:InitializeTable() end
	name = string.lower(name)
	local beltlist = inv.containerBelt.itemList
	local it = beltlist:GetEnumerator()
	local tbl = {}
	local count = 0
	while (it:MoveNext()) do
		if(string.lower(tostring(it.Current.info.displayname)) == name) then
			tbl[ #tbl + 1] = it.Current
			count = count + it.Current.amount
		end
	end
	return tbl, count
end
function PLUGIN:Clear( inv, type )
	if(not self.Table) then self:InitializeTable() end
	if(not type) then
		inv:Strip()
	else
		local container
		if(type == "belt") then
			container = inv.containerBelt
		elseif(type == "main") then
			container = inv.containerMain
		elseif(type == "wear") then
			container = inv.containerWear
		else
			return false, "wrong type: belt, main or wear"
		end
		container:Kill()
	end
end
function PLUGIN:GetItems( inv, type )
	if(not self.Table) then self:InitializeTable() end
	local tbl = {}
	local count = 0
	if(not type) then
		local beltlist = inv.containerBelt.itemList
		local mainlist = inv.containerMain.itemList
		local wearlist = inv.containerWear.itemList
		local itbelt = beltlist:GetEnumerator()
		local itmain = mainlist:GetEnumerator()
		local itwear = wearlist:GetEnumerator()
		while (itbelt:MoveNext()) do
			tbl[ #tbl + 1] = itbelt.Current
		end
		while (itmain:MoveNext()) do
			tbl[ #tbl + 1] = itmain.Current
		end
		while (itwear:MoveNext()) do
			tbl[ #tbl + 1] = itwear.Current
		end
	else
		local container
		if(type == "belt") then
			container = inv.containerBelt
		elseif(type == "main") then
			container = inv.containerMain
		elseif(type == "wear") then
			container = inv.containerWear
		else
			return false, "wrong type: belt, main or wear"
		end
		local list = container.itemList
		local it = list:GetEnumerator()
		while (it:MoveNext()) do
			tbl[ #tbl + 1] = it.Current
		end
	end	
	return tbl
end
function PLUGIN:GiveItem(inv,name,amount,type)
	if(not self.Table) then self:InitializeTable() end
	local itemname = false
	name = string.lower(name)
	if(self.Table[name]) then
		itemname = self.Table[name]
	else
		itemname = name
	end
	if(tonumber(amount) == nil) then
		return false, "amount is not valid"
	end
	local container
	if(type == "belt") then
		container = inv.containerBelt
	elseif(type == "main") then
		container = inv.containerMain
	elseif(type == "wear") then
		container = inv.containerWear
	else
		return false, "wrong type: belt, main or wear"
	end
	local giveitem = global.ItemManager.CreateByName(itemname,amount)
	if(not giveitem) then
		return false, itemname .. " is not a valid item name"
	end
	inv:GiveItem(giveitem,container);
end