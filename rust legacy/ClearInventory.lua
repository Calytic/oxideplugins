PLUGIN.Title = " Clear Inventory"
PLUGIN.Description = "Clear you're own inventory and other players"
PLUGIN.Author = "Ezki & mvrb"
PLUGIN.Version = V( 1, 0, 0 )

function PLUGIN:Init()	
	command.AddChatCommand( "clearinv", self.Plugin, "ClearInventory" )
	permission.RegisterPermission( "clearinventory.allowed", self.Plugin )
end

function PLUGIN:ClearInventory( netuser, cmd, args )
	local inv
	if args.Length == 0 then
		inv = netuser.playerClient.rootControllable.idMain:GetComponent( "Inventory" )
	elseif args.Length == 1 and permission.UserHasPermission( rust.UserIDFromPlayer( netuser ), "clearinventory.allowed" ) then
		local target = rust.FindPlayer( args[0] )
		if not target then return end
		inv = target.playerClient.rootControllable.idMain:GetComponent( "Inventory" )
	end
	if not inv or tostring( type ( inv ) ) == "string" then return end
	inv:Clear()
end