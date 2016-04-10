

		
				
	
		
	
			
		**Features** 


* Allows admins to prod an entire building (including deployables)

* Allows admins to change or remove ownership from an entire building (including deployables)

* Allows admins to see cupboard/turret authorization

* Allows admins to change authorization for all nearby cupboards/turrets

* Supports message localization

* Supports both authLevel and permission authentication.

* Provides an API for other plugins to integrate easily


**Chat Commands** 

	
	
``


/prod

Check ownership of entity you are looking at


/own

/own all

Gives you ownership of the entire structure (including deployables)


/own all PlayerName

Gives the specified player ownership of the entire structure (including deployables)


/unown all

Removes ownership from an entire structure (including deployables)

 ``



	
	
``


/prod2

Checks the ownership of the entire structure (including deployables)


/prod2 block

Checks the ownership of the structure (and only the structure)


/prod2 cupboard

Checks the authorization of nearby cupboards


/prod2 storage

Checks the ownership of nearby storage containers

 ``



	
	
``


/auth

/auth cupboard

Check the authorization of the cupboard you are looking at


/auth turret

Check the authorization of the turret you are looking at


/auth PlayerName

/auth cupboard PlayerName

Gives authorization on all nearby cupboards to target player


/auth turret PlayerName

Gives authorization on all nearby turrets to target player


/deauth PlayerName

/deauth cupboard PlayerName

Removes authorization of all nearby cupboards from target player


/deauth turret PlayerName

Removes authorization of all nearby turrets from target player


/authclean PlayerName

Removes stuck building privilege/block

 ``


**Console Commands** 

	
	
``


authclean "Player Name"

authclean #STEAMID

Removes stuck building privilege/block

 ``


**Own/Prod2/Unown options** 


* all

* block

* storage

* sign

* sleepingbag

* plant

* oven

* turret

* door

* cupboard


**Permissions** 

This plugin uses Oxide's permission system. To assign a permission, use **grant user <username|steamid> <permission>** . To remove a permission, use **revoke user <username|steamid> <permission>** .


* **entityowner.canchangeowners** (allow auth and own commands)
**Ex.**  grant user Calytic entityowner.canchangeowners 
**Ex.**  revoke user Calytic entityowner.canchangeowners 
**Ex.**  grant group moderator entityowner.canchangeowners

* **entityowner.cancheckowners** (allow prod command)
**Ex.**  grant user Calytic entityowner.cancheckowners 
**Ex.**  revoke user Calytic entityowner.cancheckowners 
**Ex.**  grant group moderator entityowner.cancheckowners 


**Configuration Options** 


* 
**EntityLimit** (default: 8000)

There is a hard cap on how many entities may be included in any given ownership command.  By default, this cap is 8000.




* 
**messages** (Localization)

It is possible to change most of the messages sent by EntityOwner into a different language.




* 
**DistanceThreshold** (default: 3)

It is suggested to ratchet the threshold down by 1 tenth each time (2.9, 2.8, 2.7 ..) until you are satisfied with the level of precision when using commands like /own and /prod2.


The DistanceThreshold option allows you to configure how far the ownership commands will seek for other nearby entities (starting from the first entity). Changing the threshold will make ownership commands more or less precise.


**API Methods** 

	
	
``


// Gets the name and status of the owner player

// Returns null if no owner found

string GetOwnerName(BaseEntity entity)


// Get the BasePlayer instance (if known) of the owner player

// Returns null if no owner found

BasePlayer GetOwnerPlayer(BaseEntity entity)


// Removes the ownership data from a BasePlayer - method faster if

// BasePlayer provided.

RemoveOwner(BaseEntity entity)


// Changes ownership of a BaseEntity to the specified player

ChangeOwner(BaseEntity entity, BasePlayer player)


// Retrieves the owner player.userID in string format

// Returns false when no owner found

object FindEntityData(BaseEntity entity)


// Clears all entity associations with a particular player

ClearProfile(BasePlayer player)


// Grab all constructions associated with a particular player

List<BuildingBlock> GetProfileConstructions(BasePlayer player)


// Grab all deployables associated with a particular player

List<BaseEntity> GetProfileDeployables(BasePlayer player)

 ``


**Upgrading From Building Owners** 


EntityOwner is **not** compatible with BuildingOwners.
		
	
	
	