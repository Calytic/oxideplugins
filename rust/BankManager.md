**Introduction**

Allow your players to get back on their feet after a raid or server wipe with stored banked items.

**Description**

Allows players to deposit and withdraw items from a bank.

**Notes**


* Item restrictions and configurations are checked when **CLOSING** the bank, **NOT** when adding items to the bank.  Any such items will be returned to the players main inventory.  Bank admins bypass item checking.
* Only **ONE** player can be in a bank (if shared with other players) at a time. This also applies to the clan bank. Bank admins do not bypass this restriction.
* If bank access is toggled or the plugin is reloaded or unloaded, all open banks will be saved and closed and the player notified.
* Max bank (including custom permissions) cannot be larger than 30 items.

* Setting "UsePermissions" applies to bank.use only.


**Item List**

Instead of changing each item individually, change the defaults to what you want. Change "ForceUpdate" under the default sections to true and reload the plugin to force all items to update to your new default config. The "ForceUpdate" setting will return to false automatically.  "Forceupdate" does not require "PerformItemCheck" to be true to work.


Items will only use configurations that apply to them.  For example, the stacking configuration is only used for items that can stack.

What it looks like

Custom Permissions: "wood:1:0:2:1000",

Default: "wood:1:0:2:1000:1:0:2:1000",


What it means

(0 = disabled/no, 1 = enabled/yes)

(Player Bank/Clan Bank)


Section 1: Item name

Section 2: Item can be deposited into banks

Section 3: Blueprint of item can be deposited into banks

Section 4: Max Deposit - how many times item can be deposited into a bank at once

Section 5: Max Stack - how large of a stack can be deposited at one time, if stack exceeds item configuration, excess is returned to players main inventory

**Commands**

The command "/bank info item" will show players information about the item in their first inventory slot (most upper left corner of main inventory).  This information includes but not limited to if an item can be banked and how large a stack can be stored.


The command "/bank clan toggle <moderator | member>" allows clan members to toggle who has access to the clan bank.  The owner of the clan can control moderators and members.  Moderators can control members.  Members cannot use this command.

**Player Bank**

When a player opens a shared bank, that player will temporarily inherit the bank owners permissions.  This includes, but not limited to, max bank and custom items.


Opening a bank creates a large box under the player that cannot be opened by other players or destroyed.  This box is removed when the bank is closed.

**Clan Bank**

If a clan falls below the minimum required members and currently have items stored in the bank.  The owner may open the bank once to withdraw the items.  Any items remaining in the bank when closing will be returned to the owners main inventory.

**Permissions**

This plugin uses oxide permissions.


bankmanager.use - Allows players to use the bank system.

bankmanager.share - Allows players to share their banks with other players.

bankmanager.admin - Grants players access to admin functions and bypass certain restrictions.

````
grant <group|user> <name|id> bankmanager.use

revoke <group|user> <name|id> bankmanager.use
````


**Usage for players
**

/bank limits <bank | clan> - View bank limits

/bank info <item | clan> - View item information (first inventory slot) or clan information

/bank <bank | clan> - Open personal or clan bank

/bank share <player> - Open bank of shared player

/bank add <player> - Share your bank with player

/bank remove <player> - Unshare your bank with player

/bank removeall - Unshare your bank with all players

/bank list <player> - List players sharing your bank

/bank clan toggle <moderator | member> - Toggle group bank access

**Usage for administrators
**

/bank toggle <bank | clan> - Enable or disable bank system

/bank admin <bank | clan> <player | clan> - Open player or clan bank


**Configuration file**

````
{

  "Clan": {

  "Cooldown": "3",

  "Enabled": "true",

  "KeepDurability": "true",

  "MaxBank": "15",

  "MinMembers": "3"

  },

  "CustomPermissions": [

  {

  "Items": [

  "wood:1:0:3:2000"

  ],

  "MaxBank": "20",

  "MaxShare": "20",

  "Permission": "bankmanager.vip1"

  },

  {

  "Items": [

  "wood:1:0:3:3000"

  ],

  "MaxBank": "30",

  "MaxShare": "30",

  "Permission": "bankmanager.vip2"

  }

  ],

  "Defaults": {

  "ForceUpdate": "false",

  "Items": [

  "Ammunition:0:0:2:1000:0:0:2:1000",

  "Attire:0:0:2:1000:0:0:2:1000",

  "Construction:0:0:2:1000:0:0:2:1000",

  "Food:0:0:2:1000:0:0:2:1000",

  "Items:0:0:2:1000:0:0:2:1000",

  "Medical:0:0:2:1000:0:0:2:1000",

  "Misc:0:0:2:1000:0:0:2:1000",

  "Resources:1:0:2:1000:1:0:2:1000",

  "Tool:0:0:2:1000:0:0:2:1000",

  "Traps:0:0:2:1000:0:0:2:1000",

  "Unknown:0:0:2:1000:0:0:2:1000",

  "Weapon:0:0:2:1000:0:0:2:1000"

  ]

  },

  "Items": [

  "rifle.ak:0:0:2:1000:0:0:2:1000",

  "ammo.handmade.shell:0:0:2:1000:0:0:2:1000",

  "ammo.pistol:0:0:2:1000:0:0:2:1000",

  "ammo.pistol.fire:0:0:2:1000:0:0:2:1000",

  "ammo.pistol.hv:0:0:2:1000:0:0:2:1000",

  "ammo.rifle:0:0:2:1000:0:0:2:1000",

  "ammo.rifle.explosive:0:0:2:1000:0:0:2:1000",

  "ammo.rifle.incendiary:0:0:2:1000:0:0:2:1000",

  "ammo.rifle.hv:0:0:2:1000:0:0:2:1000",

  "ammo.rocket.basic:0:0:2:1000:0:0:2:1000",

  "ammo.rocket.fire:0:0:2:1000:0:0:2:1000",

  "ammo.rocket.hv:0:0:2:1000:0:0:2:1000",

  "ammo.rocket.smoke:0:0:2:1000:0:0:2:1000",

  "ammo.shotgun:0:0:2:1000:0:0:2:1000",

  "ammo.shotgun.slug:0:0:2:1000:0:0:2:1000",

  "antiradpills:0:0:2:1000:0:0:2:1000",

  "apple:0:0:2:1000:0:0:2:1000",

  "apple.spoiled:0:0:2:1000:0:0:2:1000",

  "arrow.hv:0:0:2:1000:0:0:2:1000",

  "arrow.wooden:0:0:2:1000:0:0:2:1000",

  "autoturret:0:0:2:1000:0:0:2:1000",

  "axe.salvaged:0:0:2:1000:0:0:2:1000",

  "bandage:0:0:2:1000:0:0:2:1000",

  "barricade.concrete:0:0:2:1000:0:0:2:1000",

  "barricade.metal:0:0:2:1000:0:0:2:1000",

  "barricade.sandbags:0:0:2:1000:0:0:2:1000",

  "barricade.stone:0:0:2:1000:0:0:2:1000",

  "barricade.wood:0:0:2:1000:0:0:2:1000",

  "barricade.woodwire:0:0:2:1000:0:0:2:1000",

  "battery.small:1:0:2:1000:1:0:2:1000",

  "trap.bear:0:0:2:1000:0:0:2:1000",

  "bed:0:0:2:1000:0:0:2:1000",

  "black.raspberries:0:0:2:1000:0:0:2:1000",

  "blood:0:0:2:1000:0:0:2:1000",

  "blueberries:0:0:2:1000:0:0:2:1000",

  "blueprint_book:0:0:2:1000:0:0:2:1000",

  "blueprint_fragment:0:0:2:1000:0:0:2:1000",

  "blueprint_library:0:0:2:1000:0:0:2:1000",

  "blueprint_page:0:0:2:1000:0:0:2:1000",

  "rifle.bolt:0:0:2:1000:0:0:2:1000",

  "bone.armor.jacket:0:0:2:1000:0:0:2:1000",

  "bone.armor.pants:0:0:2:1000:0:0:2:1000",

  "bone.club:0:0:2:1000:0:0:2:1000",

  "bone.fragments:1:0:2:1000:1:0:2:1000",

  "botabag:0:0:2:1000:0:0:2:1000",

  "bow.hunting:0:0:2:1000:0:0:2:1000",

  "box.wooden.large:0:0:2:1000:0:0:2:1000",

  "box.wooden:0:0:2:1000:0:0:2:1000",

  "bucket.helmet:0:0:2:1000:0:0:2:1000",

  "building.planner:0:0:2:1000:0:0:2:1000",

  "burlap.gloves:0:0:2:1000:0:0:2:1000",

  "burlap.headwrap:0:0:2:1000:0:0:2:1000",

  "burlap.shirt:0:0:2:1000:0:0:2:1000",

  "burlap.shoes:0:0:2:1000:0:0:2:1000",

  "burlap.trousers:0:0:2:1000:0:0:2:1000",

  "tool.camera:0:0:2:1000:0:0:2:1000",

  "campfire:0:0:2:1000:0:0:2:1000",

  "can.beans:0:0:2:1000:0:0:2:1000",

  "can.beans.empty:1:0:2:1000:1:0:2:1000",

  "can.tuna:0:0:2:1000:0:0:2:1000",

  "can.tuna.empty:1:0:2:1000:1:0:2:1000",

  "cctv.camera:1:0:2:1000:1:0:2:1000",

  "charcoal:1:0:2:1000:1:0:2:1000",

  "chicken.burned:0:0:2:1000:0:0:2:1000",

  "chicken.cooked:0:0:2:1000:0:0:2:1000",

  "chicken.raw:0:0:2:1000:0:0:2:1000",

  "chicken.spoiled:0:0:2:1000:0:0:2:1000",

  "chocholate:0:0:2:1000:0:0:2:1000",

  "cloth:1:0:2:1000:1:0:2:1000",

  "coffeecan.helmet:0:0:2:1000:0:0:2:1000",

  "corn:0:0:2:1000:0:0:2:1000",

  "seed.corn:0:0:2:1000:0:0:2:1000",

  "crossbow:0:0:2:1000:0:0:2:1000",

  "crude.oil:1:0:2:1000:1:0:2:1000",

  "cupboard.tool:0:0:2:1000:0:0:2:1000",

  "door.hinged.metal:0:0:2:1000:0:0:2:1000",

  "door.hinged.toptier:0:0:2:1000:0:0:2:1000",

  "door.hinged.wood:0:0:2:1000:0:0:2:1000",

  "door.key:0:0:2:1000:0:0:2:1000",

  "explosive.timed:0:0:2:1000:0:0:2:1000",

  "explosives:1:0:2:1000:1:0:2:1000",

  "fat.animal:1:0:2:1000:1:0:2:1000",

  "flare:0:0:2:1000:0:0:2:1000",

  "lowgradefuel:1:0:2:1000:1:0:2:1000",

  "furnace:0:0:2:1000:0:0:2:1000",

  "furnace.large:0:0:2:1000:0:0:2:1000",

  "gates.external.high.stone:0:0:2:1000:0:0:2:1000",

  "gates.external.high.wood:0:0:2:1000:0:0:2:1000",

  "generator.wind.scrap:0:0:2:1000:0:0:2:1000",

  "granolabar:0:0:2:1000:0:0:2:1000",

  "grenade.beancan:0:0:2:1000:0:0:2:1000",

  "grenade.f1:0:0:2:1000:0:0:2:1000",

  "fun.guitar:0:0:2:1000:0:0:2:1000",

  "gunpowder:1:0:2:1000:1:0:2:1000",

  "hammer:0:0:2:1000:0:0:2:1000",

  "hammer.salvaged:0:0:2:1000:0:0:2:1000",

  "hat.beenie:0:0:2:1000:0:0:2:1000",

  "hat.boonie:0:0:2:1000:0:0:2:1000",

  "hat.candle:0:0:2:1000:0:0:2:1000",

  "hat.cap:0:0:2:1000:0:0:2:1000",

  "hat.miner:0:0:2:1000:0:0:2:1000",

  "hatchet:0:0:2:1000:0:0:2:1000",

  "hazmat.boots:0:0:2:1000:0:0:2:1000",

  "hazmat.gloves:0:0:2:1000:0:0:2:1000",

  "hazmat.helmet:0:0:2:1000:0:0:2:1000",

  "hazmat.jacket:0:0:2:1000:0:0:2:1000",

  "hazmat.pants:0:0:2:1000:0:0:2:1000",

  "attire.hide.boots:0:0:2:1000:0:0:2:1000",

  "attire.hide.pants:0:0:2:1000:0:0:2:1000",

  "attire.hide.poncho:0:0:2:1000:0:0:2:1000",

  "attire.hide.vest:0:0:2:1000:0:0:2:1000",

  "weapon.mod.holosight:0:0:2:1000:0:0:2:1000",

  "hoodie:0:0:2:1000:0:0:2:1000",

  "hq.metal.ore:1:0:2:1000:1:0:2:1000",

  "humanmeat.burned:0:0:2:1000:0:0:2:1000",

  "humanmeat.cooked:0:0:2:1000:0:0:2:1000",

  "humanmeat.raw:0:0:2:1000:0:0:2:1000",

  "humanmeat.spoiled:0:0:2:1000:0:0:2:1000",

  "icepick.salvaged:0:0:2:1000:0:0:2:1000",

  "jacket.snow:0:0:2:1000:0:0:2:1000",

  "jacket:0:0:2:1000:0:0:2:1000",

  "jackolantern.angry:0:0:2:1000:0:0:2:1000",

  "jackolantern.happy:0:0:2:1000:0:0:2:1000",

  "knife.bone:0:0:2:1000:0:0:2:1000",

  "ladder.wooden.wall:0:0:2:1000:0:0:2:1000",

  "trap.landmine:0:0:2:1000:0:0:2:1000",

  "lantern:0:0:2:1000:0:0:2:1000",

  "largemedkit:0:0:2:1000:0:0:2:1000",

  "leather:1:0:2:1000:1:0:2:1000",

  "lock.code:0:0:2:1000:0:0:2:1000",

  "lock.key:0:0:2:1000:0:0:2:1000",

  "longsword:0:0:2:1000:0:0:2:1000",

  "lmg.m249:0:0:2:1000:0:0:2:1000",

  "mace:0:0:2:1000:0:0:2:1000",

  "machete:0:0:2:1000:0:0:2:1000",

  "map:0:0:2:1000:0:0:2:1000",

  "mask.balaclava:0:0:2:1000:0:0:2:1000",

  "mask.bandana:0:0:2:1000:0:0:2:1000",

  "bearmeat:0:0:2:1000:0:0:2:1000",

  "meat.pork.burned:0:0:2:1000:0:0:2:1000",

  "meat.pork.cooked:0:0:2:1000:0:0:2:1000",

  "meat.boar:0:0:2:1000:0:0:2:1000",

  "wolfmeat.burned:0:0:2:1000:0:0:2:1000",

  "wolfmeat.cooked:0:0:2:1000:0:0:2:1000",

  "wolfmeat.raw:0:0:2:1000:0:0:2:1000",

  "wolfmeat.spoiled:0:0:2:1000:0:0:2:1000",

  "metal.facemask:0:0:2:1000:0:0:2:1000",

  "metal.fragments:1:0:2:1000:1:0:2:1000",

  "metal.ore:1:0:2:1000:1:0:2:1000",

  "metal.plate.torso:0:0:2:1000:0:0:2:1000",

  "metal.refined:1:0:2:1000:1:0:2:1000",

  "mining.pumpjack:0:0:2:1000:0:0:2:1000",

  "mining.quarry:0:0:2:1000:0:0:2:1000",

  "mushroom:0:0:2:1000:0:0:2:1000",

  "note:0:0:2:1000:0:0:2:1000",

  "pants:0:0:2:1000:0:0:2:1000",

  "paper:1:0:2:1000:1:0:2:1000",

  "pickaxe:0:0:2:1000:0:0:2:1000",

  "pistol.eoka:0:0:2:1000:0:0:2:1000",

  "pistol.revolver:0:0:2:1000:0:0:2:1000",

  "pistol.semiauto:0:0:2:1000:0:0:2:1000",

  "pumpkin:0:0:2:1000:0:0:2:1000",

  "seed.pumpkin:0:0:2:1000:0:0:2:1000",

  "box.repair.bench:0:0:2:1000:0:0:2:1000",

  "research.table:0:0:2:1000:0:0:2:1000",

  "riot.helmet:0:0:2:1000:0:0:2:1000",

  "roadsign.jacket:0:0:2:1000:0:0:2:1000",

  "roadsign.kilt:0:0:2:1000:0:0:2:1000",

  "rock:0:0:2:1000:0:0:2:1000",

  "rocket.launcher:0:0:2:1000:0:0:2:1000",

  "salt.water:1:0:2:1000:1:0:2:1000",

  "salvaged.cleaver:0:0:2:1000:0:0:2:1000",

  "salvaged.sword:0:0:2:1000:0:0:2:1000",

  "shelves:0:0:2:1000:0:0:2:1000",

  "shoes.boots:0:0:2:1000:0:0:2:1000",

  "shotgun.pump:0:0:2:1000:0:0:2:1000",

  "shotgun.waterpipe:0:0:2:1000:0:0:2:1000",

  "shutter.metal.embrasure.a:0:0:2:1000:0:0:2:1000",

  "shutter.metal.embrasure.b:0:0:2:1000:0:0:2:1000",

  "shutter.wood.a:0:0:2:1000:0:0:2:1000",

  "sign.hanging.banner.large:0:0:2:1000:0:0:2:1000",

  "sign.hanging:0:0:2:1000:0:0:2:1000",

  "sign.hanging.ornate:0:0:2:1000:0:0:2:1000",

  "sign.pictureframe.landscape:0:0:2:1000:0:0:2:1000",

  "sign.pictureframe.portrait:0:0:2:1000:0:0:2:1000",

  "sign.pictureframe.tall:0:0:2:1000:0:0:2:1000",

  "sign.pictureframe.xl:0:0:2:1000:0:0:2:1000",

  "sign.pictureframe.xxl:0:0:2:1000:0:0:2:1000",

  "sign.pole.banner.large:0:0:2:1000:0:0:2:1000",

  "sign.post.double:0:0:2:1000:0:0:2:1000",

  "sign.post.single:0:0:2:1000:0:0:2:1000",

  "sign.post.town:0:0:2:1000:0:0:2:1000",

  "sign.post.town.roof:0:0:2:1000:0:0:2:1000",

  "sign.wooden.huge:0:0:2:1000:0:0:2:1000",

  "sign.wooden.large:0:0:2:1000:0:0:2:1000",

  "sign.wooden.medium:0:0:2:1000:0:0:2:1000",

  "sign.wooden.small:0:0:2:1000:0:0:2:1000",

  "weapon.mod.silencer:0:0:2:1000:0:0:2:1000",

  "skull.human:1:0:2:1000:1:0:2:1000",

  "skull.wolf:1:0:2:1000:1:0:2:1000",

  "sleepingbag:0:0:2:1000:0:0:2:1000",

  "small.oil.refinery:0:0:2:1000:0:0:2:1000",

  "stash.small:0:0:2:1000:0:0:2:1000",

  "smallwaterbottle:0:0:2:1000:0:0:2:1000",

  "smg.2:0:0:2:1000:0:0:2:1000",

  "spear.stone:0:0:2:1000:0:0:2:1000",

  "spear.wooden:0:0:2:1000:0:0:2:1000",

  "spikes.floor:0:0:2:1000:0:0:2:1000",

  "stone.pickaxe:0:0:2:1000:0:0:2:1000",

  "stonehatchet:0:0:2:1000:0:0:2:1000",

  "stones:1:0:2:1000:1:0:2:1000",

  "sulfur:1:0:2:1000:1:0:2:1000",

  "sulfur.ore:1:0:2:1000:1:0:2:1000",

  "supply.signal:0:0:2:1000:0:0:2:1000",

  "surveycharge:0:0:2:1000:0:0:2:1000",

  "syringe.medical:0:0:2:1000:0:0:2:1000",

  "targeting.computer:1:0:2:1000:1:0:2:1000",

  "smg.thompson:0:0:2:1000:0:0:2:1000",

  "torch:0:0:2:1000:0:0:2:1000",

  "tshirt:0:0:2:1000:0:0:2:1000",

  "tshirt.long:0:0:2:1000:0:0:2:1000",

  "wall.external.high.stone:0:0:2:1000:0:0:2:1000",

  "wall.external.high:0:0:2:1000:0:0:2:1000",

  "wall.window.bars.metal:0:0:2:1000:0:0:2:1000",

  "wall.window.bars.toptier:0:0:2:1000:0:0:2:1000",

  "wall.window.bars.wood:0:0:2:1000:0:0:2:1000",

  "water:1:0:2:1000:1:0:2:1000",

  "water.catcher.large:0:0:2:1000:0:0:2:1000",

  "water.catcher.small:0:0:2:1000:0:0:2:1000",

  "hat.wolf:0:0:2:1000:0:0:2:1000",

  "wood:1:0:2:1000:1:0:2:1000",

  "wood.armor.jacket:0:0:2:1000:0:0:2:1000",

  "wood.armor.pants:0:0:2:1000:0:0:2:1000"

  ],

  "Messages": {

  "BankBox": "This box is a bank owned by another player and cannot be opened or destroyed.",

  "BankClosed": "Bank closed for <color=#cd422b>{player}</color>.",

  "BankDisabled": "Your open bank has been saved and closed.  The bank system has been reloaded, unloaded or disabled by an administrator.",

  "BankOpened": "Bank opened for <color=#cd422b>{player}</color>.",

  "BuildingBlocked": "You cannot access a bank in building blocked areas.",

  "ChangedClanStatus": "Clan <color=#cd422b>{clan}'s</color> group <color=#ffd479>{group}</color> bank access <color=#cd422b>{status}</color>.",

  "ChangedStatus": "Bank group <color=#cd422b>{group}</color> now <color=#cd422b>{status}</color>.",

  "CheckRadius": "You cannot access a bank within <color=#cd422b>{range} meters</color> of another online player.  Current nearest range is <color=#cd422b>{current} meters</color>.",

  "ClanBankClosed": "Bank closed for clan <color=#cd422b>{clan}</color>.",

  "ClanBankOpened": "Bank opened for clan <color=#cd422b>{clan}</color>.",

  "ClanError": "An error occured while retrieving your clan information.",

  "ClanNoPermission": "You do not have permission to access <color=#cd422b>{clan}'s</color> bank.",

  "ClanOccupied": "Clan <color=#cd422b>{clan}'s</color> bank is currently occupied by <color=#cd422b>{player}</color> ({id}).",

  "ClanOwner": "Your clan, <color=#cd422b>{clan}</color>, currently has <color=#ffd479>{members} member(s)</color>.  You must have minimum <color=#cd422b>{required} members</color> to use clan bank.  As owner, you may access existing banked items.  They will be returned to you upon closing your inventory.",

  "CoolDown": "You must wait <color=#cd422b>{cooldown} seconds</color> before using this command again.",

  "DeleteAll": "You no longer share your bank with anyone. (<color=#cd422b>{entries}</color> player(s) removed)",

  "MaxShare": "You may only share your bank with <color=#cd422b>{limit} player(s)</color> at one time.",

  "MinClanMembers": "Your clan, <color=#cd422b>{clan}</color>, currently has <color=#ffd479>{members} member(s)</color>.  You must have minimum <color=#cd422b>{required} members</color> to use clan bank.",

  "MultiPlayer": "Multiple players found.  Provide a more specific username.",

  "NoClan": "You do not belong to a clan.",

  "NoClanExists": "Clan <color=#cd422b>{clan}</color> does not exist.",

  "NoItem": "No item found in first slot of inventory to check for information.",

  "NoPermission": "You do not have permission to use this command.",

  "NoPlayer": "Player not found.  Please try again.",

  "NoPlugin": "The <color=#cd422b>{plugin} plugin</color> is not installed.",

  "NoShares": "You do not share your bank with anyone.",

  "NotEnabled": "Bank group <color=#cd422b>{group}</color> is <color=#cd422b>disabled</color>.",

  "NotShared": "<color=#cd422b>{player}</color> does not share their bank with you.",

  "Occupied": "<color=#cd422b>{target}'s</color> bank is currently occupied by <color=#cd422b>{player}</color> ({id}).",

  "PlayerAdded": "You now share your bank with <color=#cd422b>{player}</color>.",

  "PlayerDeleted": "You no longer share your bank with <color=#cd422b>{player}</color>.",

  "PlayerExists": "You already share your bank with <color=#cd422b>{player}</color>.",

  "PlayerNotExists": "You do not share your bank with <color=#cd422b>{player}</color>.",

  "RequiredPermission": "You cannot share your bank with <color=#cd422b>{player}</color>.  They do not have the required permissions.",

  "Returned": "One or more items have been returned to you for the following reason(s): <color=#cd422b>{reason}</color>",

  "Self": "You cannot use commands on yourself.",

  "WrongArgs": "Syntax error.  Use <color=#cd422b>/bank</color> for help.",

  "WrongRank": "You may only toggle access for ranks lower than your own."

  },

  "Settings": {

  "BuildingBlocked": "true",

  "Cooldown": "3",

  "Enabled": "true",

  "KeepDurability": "true",

  "MaxBank": "15",

  "MaxShare": "10",

  "MessageSize": "13",

  "PerformItemCheck": "true",

  "Prefix": "[<color=#cd422b> Bank Manager </color>]",

  "Radius": "5",

  "UsePermissions": "true"

  }

}
````

Configuration file will be created and updated automatically.

**More to come**


* Your suggestions


**Known Issues**


* None