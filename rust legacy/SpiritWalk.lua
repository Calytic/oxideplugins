PLUGIN.Title = "SpiritWalk"
PLUGIN.Description = "Leave your body behind and go walking as a spirit"
PLUGIN.Author = "Bond... James Bond."
PLUGIN.Version = V(1, 0, 0)

-- Fixed up by Spirited Wolf! (Thanks as I didn't have the time to fully debug!)
function PLUGIN:Init()
 
   	command.AddChatCommand( "s", self.Plugin, "cmdSpirit")
   	
   permission.RegisterPermission("canspirit", self.Plugin);
 
   end
function PLUGIN:cmdSpirit( netuser, args )
   if ( not self:isAdmin(netuser) and not netuser:CanAdmin() ) then
     return
   end
   
   if (args[0]) then
     rust.Notice( netuser, "Syntax: /s to spirit around, and again to return back to your corpse." )
   else
     local char = netuser.playerClient.rootControllable.rootCharacter
     local hp = -100
     
     if (char.takeDamage.maxHealth < 0 or char.takeDamage.health < 0 ) then
       char.takeDamage.maxHealth = 100
       char.takeDamage.health = 100
       rust.Notice( netuser, "You have returned to your body." )
     else
       char.takeDamage.maxHealth = 100
       char.takeDamage.health = hp
       rust.Notice( netuser,  "Your spirit is set free." )
     end
   
   end
 end
 
 function PLUGIN:isAdmin(netuser)
	if(netuser:CanAdmin()) then return true end
	return permission.UserHasPermission(rust.UserIDFromPlayer(netuser), "canspirit")
end