# Note:
    # I add an underscore at the biginning of the variable name for example: "_variable" to prevent
    # conflicts with build-in variables from Oxide.

# Use to manage the player's inventory.
import ItemManager

# Use to get player's information.
import BasePlayer

# The plug-in name should be the same as the class name and file name.
class StartupItems:

    # Always start with a constructor.
    def __init__(self):
        
        # All the variables listed below are recommended for the plug-in and developer informaton.
        self.Title = 'StartupItems'
        self.Description = 'Set default items when player respawn after dead.'
        self.Author = 'RedNinja1337'
        self.Version = V(1, 0, 5)
        self.Url = 'http://oxidemod.org/plugins/startupitems.1323/'
        self.ResourceId = 1323

    # Create the configuration file if it does not exists.
    def LoadDefaultConfig(self):
        
        # Add some demo data as an example on the configuration file.
        self.Config['GroupItems'] = ({
            
                             'admin':({'item_shortname':'attire.hide.boots', 'Amount':1, 'Container':'Wear'},
                                       {'item_shortname':'attire.hide.pants', 'Amount':1, 'Container':'Wear'},
                                       {'item_shortname':'rock', 'Amount':1, 'Container':'Belt'},
                                       {'item_shortname':'bow.hunting', 'Amount':1, 'Container':'Belt'},
                                       {'item_shortname':'arrow.hv', 'Amount':25, 'Container':'Main'},),
                             
                             'moderator':({},),
                             
                             'player':({},)
                          })

    # Called from BasePlayer.Respawn.
    # Called when the player spawns (specifically when they click the "Respawn" button).
    # ONLY called after the player has transitioned from dead to not-dead, so not when they're waking up.
    def OnPlayerRespawned(self, BasePlayer):

        # Check if there is any group set on the configuration file.
        if self.Config['GroupItems']:

            # If at least one group is found on the configuration file then set the variable "_GroupItems" equals the group's dictionary.
            _GroupItems = self.Config['GroupItems']

            # Set the variable "_Group" equals the list of groups the player belogs to. By default all players belog to the group "player".
            _Group = permission.GetUserGroups(BasePlayer.userID.ToString())

            # Set the variable "_SetGroup" equals the last group the user was added from Oxide.Group. By default all players belog to the group "player".
            _SetGroup = _GroupItems.get(_Group[-1])

            # Check if the group exists in the config file.
            if _SetGroup:
            
                try: # Catch the "KeyNotFoundException" error if "Container", "item_shortname" or "Amount" is not found on the config file.
                    if _SetGroup[0]['Container'] and _SetGroup[0]['item_shortname'] and _SetGroup[0]['Amount']:

                        # Set the variable "inv" equals the player's inventory.
                        inv = BasePlayer.inventory

                        # Empty the player's inventory.
                        inv.Strip()

                        # Iterate through the list of items for the specify group from the configuration file.
                        for item in _SetGroup:

                            # Add the items set on the configuration file to each container on the player's inventory.
                            if item['Container'].lower() == 'main':
                                inv.GiveItem(ItemManager.CreateByName(item['item_shortname'],item['Amount']), inv.containerMain)
                            elif item['Container'].lower() == 'belt':
                                inv.GiveItem(ItemManager.CreateByName(item['item_shortname'],item['Amount']), inv.containerBelt)
                            elif item['Container'].lower() == 'wear':
                                inv.GiveItem(ItemManager.CreateByName(item['item_shortname'],item['Amount']), inv.containerWear)
                            else: return
                    else: print False

                # Catch the "KeyNotFoundException" error if "Container", "item_shortname" or "Amount" is not found on the config file.
                except KeyError: return
            else: return
        else: return
