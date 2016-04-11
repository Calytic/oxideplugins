This has no use for servers without any other plugins that doesn't require it.


Available functions:

Functions to find an Item depending on where it could be:

Item item = self:FindItemWear( playerinventory inv, string name)

Item item = self:FindItemBelt( playerinventory inv, string name)

Item item = self:FindItemMain( playerinventory inv, string name)

Functions to find Items depending on where they could be:

table items, int count = self:FindItemsMainByName( playerinventory inv, string name)

table items, int count = self:FindItemsWearByName( playerinventory inv, string name)

table items, int count = self:FindItemsBeltByName( playerinventory inv, string name)

Function to find an Item anywhere in a players inventory:

Item item = self:FindItemAllByName( playerinventory inv, string name)

Function to find all items anywhere in a players inventory:

table items, int count = self:FindItemsAllByName( playerinventory inv, string name)

Usage: To Remove all specific items from a players inventory:

````
items, count = self:FindItemsAllByName( player.inventory, "Rock" )

for i, #items do

   item = items[i]

   item:RemoveFromContainer()

end
````

Clear a part of an inventory or all inventory of a player:

self:Clear( player inventory inv )

self:Clear( playerinventory inv, string container)

Usage: To clear everything that the player is wearing:

````
self:Clear(player.inventory,"wear")
````

Get List of items in a players inventory:

table items = self:GetItems( playerinventory inv, string type )

table items = self:GetItems( playerinventory inv )

````
items = self:GetItems( player.inventory, "wear" )

for i, #items do

   print(player.displayName .. " is wearing: ".. items[i].info.displayname)

end
````

Get item by slot number:

Item item = self:GetItemBySlot(playerinventory inv, int slot, string type)

Give players some stuff, you may choose where to add it:

Item item = self:GiveItem(playerinventory inv, string name,int amount,string type)

````
item = self:GiveItem( player.inventory, "Bolt Action Rifle", 1, "belt" )
````