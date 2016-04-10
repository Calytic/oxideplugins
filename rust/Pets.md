It's Beta, so there can bugs now! 

About lags: 1 pet will not create more lags than 1 rust animal. (mb even less )
How to:

* /pet -- activate\deactivate npc mode
* /pet free -- release the pet
* /pet sleep -- pet will sleep (low hunger and thirst lose, stamina recovers 3 times faster)
* /pet draw  -- on\off draw system (you also can off it globally

in config)
* /pet info -- show info about your pet (health, hunger, thrist, stamina...)Draw system is very simple now, when u give the command to ur pet, plugin will show pet's target. There 3 indicators cyan:move, red:attack, yellow:eat.


* To select look at NPC and press USE button 
* To attack other npc or player look at him and press USE
* To use follow/unfollow command press Secondary Button (def: Reload) + Main Button (def: Use)
* To feed npc look at dead npc and press USE (++hunger, ++thirst, +health)
* To open pet's inventory get closer to him and press USE
* All pets will be saved on Restart in data/Pets.json

You also can change Button(USE) in config!

List of buttons:

FORWARD

  BACKWARD

  LEFT

  RIGHT

  JUMP

  DUCK

  SPRINT

  USE

  FIRE_PRIMARY

  FIRE_SECONDARY

  RELOAD
  FIRE_THIRD

Permissions:


* 'canhorse'         -- Grant permission to horse taming
* 'canbear'        -- Grant permission to  bear taming
* 'canwolf'        -- Grant permission to  wolf taming
* 'canchicken'  -- Grant permission to  chicken taming
* 'canboar'        -- Grant permission to  boar taming
* 'canstag'         -- Grant permission to  stag taming

P.S. You also can off permission system in config
You can grant a user permission by using:
oxide.grant user <username> <permission>

To create a group:
oxide.group add <groupname>

To assign permission to a group:
oxide.grant group <groupname> <permission>

To add users to a group:
oxide.usergroup add <username> <groupname>