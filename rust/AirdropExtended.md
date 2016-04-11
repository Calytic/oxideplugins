===============================================


Introduction


Welcome. This plugin is intended to be used as a controller for airdrop. It allows you to control nearly everything in your airdrop. See feature list for details.


See trello board for plugin bugs & feature suggestions: [Trello](https://trello.com/b/7nChbU68/airdrop-settings-features-requests)

[>>DEFAULT /oxide/data/airdropExtended_defaultSettings.json CONTENTS<<](http://pastebin.com/3St6Lc2e)
[ >> CONFIG EXPLAINED <<](http://pastebin.com/7YXVDsVn)


If this plugin helps you and you want it to be updated on a regular basis - consider making a donation[ ](https://www.paypal.com/cgi-bin/webscr?business=baton256@gmail.com&lc=US&item_name=Donation+(from+OxideMod.org)&cmd=_donations&rm=1&no_shipping=1&currency_code=USD)

[](https://www.paypal.com/cgi-bin/webscr?business=zakharov.g@gmail.com&lc=US&item_name=Donation+(from+OxideMod.org)&cmd=_donations&rm=1&no_shipping=1&currency_code=USD)

=====================================================
Installation


When plugin is added to your server plugins folder it will generate default settings for you in oxide/data/airdropExtended_defaultSettings.json file.


Configuration file in oxide/config/airdropExtended.json will contain a name of settings file that plugin is using right now.


You can always generate new default settings via "aire.generate <settings_name>" command.

Then type "aire.load <settings_name>" to load them.


I strongly recommend you to check all plugin configuration options.  Read the description of each command & setting below.


If you change settings manually by editing the file - you will need to reload them to the plugin by typing either "aire.load <my_settings_name>" or "aire.reload" to reload current settings file.


=====================================================

Custom loot, game updates and new items


When new game update is out, there might be new items added to the game (m249 for example) that you want in your loot. You need to manually add them to your config file after update.


To update your existing custom loot settings I suggest these 2 methods:


1) Easy way. 

type "aire.generate update"

open with text editor airdropExtended_update.json file.

Manually check for new items and copy them to your current settings file.

Then reload your current settings.


2) Hard way. 

Check [Oxide Docs for complete list of Rust](http://docs.oxidemod.org/rust/#item-list) items and add them manually to your config file.


=====================================================


Features:


* Control airdrop timing and supply signals: Drop frequency, disabling drops/supply signals, number of crates & more
* Rich notifications about everything regarding airdrop: planes, supply signals, drops.
* Many configuration options such as: player count, plane limit, number of crates, plane speed, drop location, drop frequency, supply drop despawn
* Plugin automatically creates default airdrop loot settings which you can customize as you want.
* Notifications to players around on SupplyDrop landed or SupplySignal thrown
* Despawn airdrop containers in few minutes after they landed
* Easy switching between different plugin settings via TimedExecute plugin
* Most settings have apropriate commands to change them from RCON/Console/Chat.

=====================================================


Commands :

<parameter> - required parameter.

[parameter] - optional parameter
Settings generation:


* aire.load <setttings_name> - loads settings file with specified name. Important note: plugin saves settings file to /data folder. It uses naming convention: airdropExtended_<settings_name>.json If you need to load settings file airdropExtended_goodloot.json type aire.load goodloot.
* aire.reload <settings_name> - reload current settings from file.
* aire.save [settings_name] - save current settings to settings_name or to current settings.
* aire.generate <settings_name> - generates new settings file with all items from game, using standard presets. Should work with new game updates.

Call airdrop:


* aire.drop - drops to random pos, using DropLocation settings from data file.
* aire.topos x z or x,z or x;z- drops to specified location
* aire.toplayer <player name/steam_id> - drops to specified player
* aire.tome - works only from chat! drops to player, who's calling
* aire.massdrop 5 - calls N planes with drops to random locations

Test drop at runtime:


* aire.test - prints sample supply drop contents to console

Workflow of custom loot configuration:


1) change your chance/groups/items etc. using settings file/console/chat

2) reload your settings

3) call aire.test few times to test it out


Sample output:

````
> aire.test

[Oxide] 6:05 PM [Info] aire:Test airdrop crate contents:

[Oxide] 6:05 PM [Info] aire:===================================================

[Oxide] 6:05 PM [Info] aire:Item: |  Snow Jacket - Wood|, bp: False, count: 1

[Oxide] 6:05 PM [Info] aire:Item: |  High Quality Metal|, bp: False, count: 850

[Oxide] 6:05 PM [Info] aire:Item: |   Small Wooden Sign|, bp: False, count: 1

[Oxide] 6:05 PM [Info] aire:Item: |        Burlap Shirt|, bp: False, count: 1

[Oxide] 6:05 PM [Info] aire:Item: |        Wooden Arrow|, bp: False, count: 34

[Oxide] 6:05 PM [Info] aire:Item: |                Wood|, bp: False, count: 956

[Oxide] 6:05 PM [Info] aire:===================================================

> aire.test

[Oxide] 6:05 PM [Info] aire:Test airdrop crate contents:

[Oxide] 6:05 PM [Info] aire:===================================================

[Oxide] 6:05 PM [Info] aire:Item: |           Land Mine|, bp: False, count: 1

[Oxide] 6:05 PM [Info] aire:Item: |            Revolver|, bp: False, count: 1

[Oxide] 6:05 PM [Info] aire:Item: |              Sulfur|, bp: False, count: 832

[Oxide] 6:05 PM [Info] aire:Item: |       Stone Hatchet|, bp: False, count: 1

[Oxide] 6:05 PM [Info] aire:Item: |         Granola Bar|, bp: False, count: 6

[Oxide] 6:05 PM [Info] aire:Item: |           Paper Map|, bp: False, count: 1

[Oxide] 6:05 PM [Info] aire:===================================================
````

Change settings in runtime:


* aire.minfreq <frequency_in_seconds> - set min drop frequency in seconds
* aire.maxfreq <frequency_in_seconds> - set max drop frequency in seconds

* aire.players <min_players> - set min player count
* aire.event <event_enabled:true/false> - disable/enable game built-in airdrop (I hardly recommend you to keep this option enabled)
* aire.timer <timer_enabled:true/false> - disable/enable plugin airdrop timer.

* aire.supply <supplydrop_enabled:true/false> - disable/enable calling plane with supply signal.

* aire.despawntime <despawn_time_in_seconds> - set supply drop container despawn time (starts when drop is landed)
* aire.setitem <item_name> <chance> <min> <max> <is_blueprint> - configure custom loot settings for an item.
* aire.setitemgroup <group_name> <max_amount> - set max amount of items in group.
* aire.capacity <capacity> - set capacity of supply drop container. Default is 6. Max is 18.
* aire.planespeed <speedInSeconds> - set plane speed
* aire.planelimit <planeLimit> - set limit of maximum allowed planes in the air at one time. The plugin will queue planes that exceed <planeLimit>.
* aire.enableplanelimit <true/false> - enable/disable plane limit queue.
* aire.crates <mincrates> [<maxcrates>] - set min & max crates to drop. default is 1-1.
* aire.onelocation <true/false> - drops all crates near or spread across a map.
* aire.customloot <true/false> - enable/disable custom loot.
* aire.pick <pick_strategy:int> - Pick strategy affects how custom loot in supply drop container is generated. I strongly recommend you to read it below.
* aire.localize <message> <text> - Sets property with name message to value text in Localization section of config. With this command you can change messages, prefix and color setting from console/chat.

Example: 

aire.localize NotifyOnPlaneSpawnedMessage "Wooot!"
* aire.notify <switch> <true/false> - enables/disables chat notification 

Example: 

aire.notify NotifyOnPlaneSpawned true

=====================================================


Permissions (Oxide):

[How to use oxide permission system](http://oxidemod.org/threads/using-oxides-permission-system.8296/)

settings load/save:


* aire.canLoad
* aire.canReload
* aire.canSave
* aire.canGenerate

Change settings in runtime:


* aire.canMinFreq
* aire.canMaxFreq
* aire.canTimer
* aire.canEvent
* aire.canSupply
* aire.canDespawnTime
* aire.canSetItem
* aire.canSetItemGroup
* aire.canCapacity
* aire.canPlaneSpeed
* aire.canEnablePlaneLimit
* aire.canPlaneLimit
* aire.canCrates
* aire.canOneLocation

Call airdrop:


* aire.canDrop
* aire.canDropToPos
* aire.canDropToPlayer
* aire.canDropToMe
* aire.canMassDrop

Test drop at runtime:


* aire.canTest

===============================================


Plugin config:

/oxide/config/airdropExtended.json 


has only one setting settingsName - settings from data folder to use.

The template for settings in data folder is "airdropExtended_{yoursettingsname}.json"


Config placement in data folder is in for changing settings without plugin reload with command aire.load/aire.save & TimedExecute plugin.

To validate your config use: [JSON Formatter & Validator](https://jsonformatter.curiousconcept.com/)


=====================================================


Data folder settings explanation:


Config explanation:
[>>DEFAULT /oxide/data/airdropExtended_defaultSettings.json CONTENTS<<](http://pastebin.com/3St6Lc2e)
[ >> CONFIG EXPLAINED <<](http://pastebin.com/7YXVDsVn)

===============================================


Even more detailed explanation: [Airdrop Extended | Page 17 | Oxide](http://oxidemod.org/threads/airdrop-extended.10212/page-17#post-140072)


Plugin can generate custom loot using 2 methods, called PickStrategy. You need to choose one of them.

- Capacity: in settings "PickStrategy": 0

This method based on a capacity of airdrop.

group_weight = group max loot amount / airdrop capacity .
item_weight = item_chance / sum of all item_chance.


For each picked item selects group from all groups that have non zero   weight, based on group_weight. In that group picks item based on its item_weight.


Short explanation - chance of item and max amount in loot of group represents their chances to appear in airdrop.

- GroupSize: in settings "PickStrategy": 1

Picks items group.MaxAmountInLoot items randomly from the group.

If all groups have sum Amount > Capacity, then their amounts will be decreased to match Capacity.


In short - chance of item represents chance to be picked from group. MaxAmountInLoot of group represents number of items that are guaranteed to appear in loot.

==============================================


Post bugs & suggestions in a plugin thread.