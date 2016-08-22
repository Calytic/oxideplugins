try:
    import BasePlayer
    import Oxide.Game.Rust.Cui as Cui
    from System.Collections.Generic import List, Dictionary
    from System import Action, Array, Int32, String
    import UnityEngine.TextAnchor as TextAnchor
    import time
except ImportError, e:
    print 'IMPORT ERROR',e

class AdminMenu:
    def __init__(self):
        self.Title = "Admin Menu"
        self.Author = "FlamingMojo"
        self.Version = V(2,0,6)
        self.HasConfig = True
        self.ResourceId = 1986

        self.fontSize = 12
        self.fadeInTime = 0.5
        self.fadeOutTime = 0.5
        
        self.weathertoggle = [False,False,False,False,False,False,False,False]
        ##Weathertoggle = [clouds, rain, wind, fog, mild, average, heavy, max]
        
        self.TINTCOLOURS = {
            'grey_tint'  : "0.1 0.1 0.1 0.7",
            'red_tint'   : "0.5 0.1 0.1 0.7",
            'green_tint' : "0.1 0.4 0.1 0.5",
            'yellow_tint': "0.7 0.7 0.1 0.5",
            'orange_tint': "0.8 0.5 0.1 0.5",
            'blue_tint'  : "0.1 0.1 0.4 0.5",
            'black_tint' : "0.01 0.01 0.01 0.9",
            'white_tint' : "1 1 1 0.9"}
        self.TEXTCOLOURS = {
            'red_text'      : "0.8 0.1 0.1",
            'yellow_text'   : "0.7 0.7 0.1",
            'green_text'    : "0.1 0.8 0.1",
            'blue_text'     : "0.1 0.1 0.8",
            'white_text'    : "1 1 1",
            'orange_text'   : "0.8 0.5 0.1",
            'black_text'    : "0 0 0"}

        self.Default = {
            'SETTINGS':{
                'Version' : '2.0.6',
                'ConnectedPlugins' : ('AdminMenu',
                                      'BetterChat',
                                      'HeliControl',
                                      'FancyDrop',
                                      'ServerRewards',
                                      'Godmode',
                                      'Airstrike',
                                      'RainOfFire',
                                      'Spectate',
                                      'Give',
                                      'Jail',
                                      'Economics',
                                      'Vanish',
                                      'XpAdder',
                                      'WeatherController'),
                'Broadcast' : True,
                'PlayersPerPage' : 10,
                'ManyItemButtons' : (1,100,1000,10000,50000),
                'FewItemButtons' : (1,100,1000),
                'Many Items' : ('Resources', 'Ammo'),
                'Single Items' : ('Weapons','Items','Construction','Traps'),
                'Economy':{
                    'admin' : 0,
                    'moderator' : 0}
                },
            'COMMANDS':{
                'AdminMenu':{
                    'Info':{
                        '__' : (1,'echo "This button does nothing"','grey_tint','white_text','adminmenu.allow.view',False)},
                    'Basic':{
                        'Kick'    :(4,'adminmenu.confirmaction kick {PLAYERID} ', 'yellow_tint', 'red_text', 'adminmenu.allow.kick',True),
                        'GoTo'    :(1,'adminmenu.goto {PLAYERID}','blue_tint', 'green_text', 'adminmenu.allow.goto',True),
                        'Bring'   :(2,'adminmenu.bring {PLAYERID}' ,'green_tint', 'blue_text','adminmenu.allow.bring',True),
                        'Kill'    :(3,'adminmenu.kill {PLAYERID}','black_tint','white_text', 'adminmenu.allow.kill',True),
                        'Ban'     :(5,'adminmenu.confirmaction ban {PLAYERID} ', 'red_tint', 'yellow_text', 'adminmenu.allow.ban',True),
                        'Perms'     :(6,'adminmenu.permissions {PLAYERID}', 'blue_tint', 'white_text', ' ',True)},
                    'Time' :{
                        'Dawn' : (1,'env.time 6','black_tint','white_text',' ',False),
                        'Noon' : (2,'env.time 12','black_tint','white_text',' ',False),
                        'Dusk' : (3,'env.time 18','black_tint','white_text',' ',False),
                        'Midnight' : (4,'env.time 0','black_tint','white_text',' ',False)}
                    },         
                'HeliControl' : {
                    'CallHelis' : {
                        'CallHeliTo' : (1,'callheli {PLAYERNAME}','red_tint','yellow_text','helicontrol.callheli',True)},
                    'KillHelis' : { 
                        'KillAll' : (1,'killheli','black_tint','yellow_text','helicontrol.killheli',False)}
                    },
                'BetterChat' :{
                    'Mute_Commands':{
                        'Unmute'  :(4,"unmute {PLAYERNAME}","red_tint","white_text","betterchat.mute", True),
                        'Mute5m'   :(1,"mute {PLAYERNAME} 5m","green_tint","white_text",'betterchat.mute',True),
                        'Mute15m'   :(2,"mute {PLAYERNAME} 15m","green_tint","white_text",'betterchat.mute',True),
                        'PermaMute':(3,"mute {PLAYERNAME}","green_tint","white_text",'betterchat.mute',True)}
                    },
                'Godmode' : {
                    'God_Commands':{
                        'God'     :(1,'adminmenu.god {PLAYERID}','blue_tint','white_text','godmode.allowed',True),
                        'UnGod'   :(2,'adminmenu.ungod {PLAYERID}','red_tint','white_text','godmode.allowed',True)}
                    },
                'RainOfFire' : {
                    'Rain_Fire'  : {
                        'RainFire'  :(1,'rof.onposition {PLAYERPOS}','red_tint','white_text',' ',True)}
                    },
                'Airstrike' : {
                    'Air_Strikes' : {
                        'Airstrike'     :(1,'airstrike {PLAYERPOS}','blue_tint','white_text','airstrike.admin',True),
                        'Squadstrike'   :(2,'squadstrike {PLAYERPOS}','red_tint','white_text','airstrike.mass',True)}
                    },
                'Spectate' : {
                        'Spectate': {
                            'Spectate'  :(1,'spectate {PLAYERNAME}','blue_tint','white_text','spectate.use',True)}
                    },                
                'Jail' : {
                    'Jail_Players':{
                        'JailPerm'  : (5,'jail.send {PLAYERID}','red_tint','yellow_text','jail.admin',True),
                        'Jail 5m'  : (1,'jail.send {PLAYERID} 300','red_tint','yellow_text','jail.admin',True),
                        'Jail 15m'  : (2,'jail.send {PLAYERID} 900','red_tint','yellow_text','jail.admin',True),
                        'Jail 30m'  : (3,'jail.send {PLAYERID} 1800','red_tint','yellow_text','jail.admin',True),
                        'Jail 1h'  : (4,'jail.send {PLAYERID} 3600','red_tint','yellow_text','jail.admin',True),
                        'UnJail'  :(6,'jail.free {PLAYERID}','blue_tint','yellow_text','jail.admin',True)}
                    },
                'FancyDrop' :{
                    'Drop' : {
                        'DropTo'   : (1,'ad.toplayer {PLAYERNAME}','blue_tint','yellow_text',' ',True),
                        'DropDirect' : (2,'ad.dropplayer {PLAYERNAME}','blue_tint','red_text',' ',True)}
                    },
                'ServerRewards' : {
                    'SR':{
                        'SRCheck' : (1,'adminmenu.srcheck {PLAYERID}','yellow_tint','blue_text',' ',True),
                        'SRClear' : (2,'adminmenu.srclear {PLAYERID}','yellow_tint','red_text',' ',True)}
                    },
                'Give':{
                    'SingleItems':{
                        'Weapon' : (1,'adminmenu.giveMenu {PLAYERID} Weapons','black_tint','white_text',' ',True),
                        'Misc' : (2,'adminmenu.giveMenu {PLAYERID} Misc','black_tint','white_text',' ',True),
                        'Item' : (3,'adminmenu.giveMenu {PLAYERID} Items','black_tint','white_text',' ',True),
                        'Trap' : (4,'adminmenu.giveMenu {PLAYERID} Traps','black_tint','white_text',' ',True),
                        'Attire' : (5,'adminmenu.giveMenu {PLAYERID} Attire','black_tint','white_text',' ',True),
                        'Construction' : (6,'adminmenu.giveMenu {PLAYERID} Construction ','black_tint','white_text',' ',True)
                        },
                    'SomeItems':{
                        'Medical' : (1,'adminmenu.giveMenu {PLAYERID} Medical','black_tint','white_text',' ',True),
                        'Food' : (2,'adminmenu.giveMenu {PLAYERID} Food','black_tint','white_text',' ',True)
                        },
                    'ManyItems':{
                        'Ammo' : (1,'adminmenu.giveMenu {PLAYERID} Ammo','black_tint','white_text',' ',True),
                        'Resources' : (2,'adminmenu.giveMenu {PLAYERID} Resources','black_tint','white_text',' ',True)}},
                'Economics' : {
                    'Balance':{
                        '-10000' : (1,'adminmenu.eco {PLAYERID} -10000','red_tint','white_text','adminmenu.allow.economy',True),
                        '-1000' : (2,'adminmenu.eco {PLAYERID} -1000','red_tint','white_text','adminmenu.allow.economy',True),
                        '-100' : (3,'adminmenu.eco {PLAYERID} -100','red_tint','white_text','adminmenu.allow.economy',True),
                        'Bal' : (4,'adminmenu.eco {PLAYERID} balance','blue_tint','white_text','adminmenu.allow.economy',True),
                        '+100' : (5,'adminmenu.eco {PLAYERID} 100','green_tint','white_text','adminmenu.allow.economy',True),
                        '+1000' : (6,'adminmenu.eco {PLAYERID} 1000','green_tint','white_text','adminmenu.allow.economy',True),
                        '+10000' : (7,'adminmenu.eco {PLAYERID} 10000','green_tint','white_text','adminmenu.allow.economy',True)},
                    },
                'Vanish' : {
                    'Vanish' : {
                        'Vanish' : (1,'vanish','green_tint','white_text','vanish.allowed',False)}
                    },
                'XpAdder':{
                    'XP': {
                        '-10' : (1,'xpadder.add {PLAYERID} -10','red_tint','white_text','xpadder.allow',True),
                        '-5' : (2,'xpadder.add {PLAYERID} -5','red_tint','white_text','xpadder.allow',True),
                        '-1' : (3,'xpadder.add {PLAYERID} -1','red_tint','white_text','xpadder.allow',True),
                        '+1' : (4,'xpadder.add {PLAYERID} 1','green_tint','white_text','xpadder.allow',True),
                        '+5' : (5,'xpadder.add {PLAYERID} 5','green_tint','white_text','xpadder.allow',True),
                        '+10' : (6,'xpadder.add {PLAYERID} 10','green_tint','white_text','xpadder.allow',True),
                        'RESET' : (7,'xpadder.reset {PLAYERID}','blue_tint','red_text','xpadder.allow',True)}
                    },
                'WeatherController':{
                    'Individual' : {
                        'Clouds' : (1,'adminmenu.weather clouds','white_tint','black_text','weathercontroller.canuseweather',False),
                        'Rain' : (2,'adminmenu.weather rain','blue_tint','black_text','weathercontroller.canuseweather',False),
                        'Fog' : (3,'adminmenu.weather fog','white_tint','blue_text','weathercontroller.canuseweather',False),
                        'Wind' : (4,'adminmenu.weather wind','grey_tint','white_text','weathercontroller.canuseweather',False),
                        'Auto' : (5,'adminmenu.weather auto','black_tint','white_text','weathercontroller.canuseweather',False)},
                    'Preset' : {
                        'Mild' : (1,'adminmenu.weather mild','white_tint','black_text','weathercontroller.canuseweather',False),
                        'Average' : (2,'adminmenu.weather average','yellow_tint','black_text','weathercontroller.canuseweather',False),
                        'Heavy' : (3,'adminmenu.weather heavy','orange_tint','white_text','weathercontroller.canuseweather',False),
                        'Max' : (4,'adminmenu.weather max','red_tint','white_text','weathercontroller.canuseweather',False),
                        'Auto' : (5,'adminmenu.weather auto','black_tint','white_text','weathercontroller.canuseweather',False)}
                   }
                },
            'REASONS' : {
                'KICK' : ("Spamming","Racism","Cheating","Disrespect"),
                'BAN'  : ("Spamming","Racism","Cheating","Disrespect")}
            }
            
        self.Messages = {
            'menuAlreadyOpen'   : '[AdminMenu]<color="#ff0000ff">Your AdminMenu is already open</color>',
            'noPermissionToView': '[AdminMenu]<color="#ff0000ff">Sorry you have no permission to view the AdminMenu</color>',
            'kickPlayer'        : '[AdminMenu]<color="#00ff00ff">{ADMINNAME}</color> kicked <color="#ff0000ff">{PLAYERNAME}</color>',
            'banPlayer'         : '[AdminMenu] <color="#00ff00ff">{ADMINNAME}</color> banned <color="#ff0000ff">{PLAYERNAME}</color>',
            'gotoPlayer'        : '[AdminMenu]<color="#00ff00ff">{ADMINNAME}</color> teleported to <color="#ff0000ff">{PLAYERNAME}</color>',
            'bringPlayer'       : '[AdminMenu]<color="#00ff00ff">{ADMINNAME}</color> teleported <color="#ff0000ff">{PLAYERNAME}</color> to them',
            'killPlayer'        : '[AdminMenu]<color="#00ff00ff">{ADMINNAME}</color> killed <color="#ff0000ff">{PLAYERNAME}</color>',
            'jailPlayer'        : '[AdminMenu]<color="#00ff00ff">{ADMINNAME}</color> jailed <color="#ff0000ff">{PLAYERNAME}</color>',
            'unjailPlayer'      : '[AdminMenu]<color="#00ff00ff">{ADMINNAME}</color> unjailed <color="#ff0000ff">{PLAYERNAME}</color>',
            'godPlayer'         : '[AdminMenu]<color="#00ff00ff">You</color> godded <color="#ff0000ff">{PLAYERNAME}</color>',
            'ungodPlayer'       : '[AdminMenu]<color="#00ff00ff">You</color> ungodded <color="#ff0000ff">{PLAYERNAME}</color>',
            'srClear'           : '[AdminMenu]<color="#00ff00ff">You</color> removed <color="#ff0000ff">{PLAYERNAME}</color> reward points',
            'srCheck'           : '[AdminMenu]<color="#ff0000ff">{PLAYERNAME}</color> has %d reward points',
            'Balance'           : '[AdminMenu]<color="#ff0000ff">{PLAYERNAME}</color> has %d coins',
            'ecoCooldown'       : '[AdminMenu]<color="#ff0000ff">Cooldown Remaining:</color> %s',
            'giveMoney'         : '[AdminMenu]<color="#00ff00ff">You</color> gave <color="#ff0000ff">{PLAYERNAME}</color> %s coins',
            'takeMoney'         : '[AdminMenu]<color="#00ff00ff">You</color> took  %s coins from <color="#ff0000ff">{PLAYERNAME}</color>'}

        self.PlayerUIs = dict()

        self.ItemList = {
	"Traps":{
		"Snap Trap" : "trap.bear",
		"Wooden Floor Spikes" : "spikes.floor",
		"Auto Turret" : "autoturret",
		"Land Mine" : "trap.landmine"
	},
	"Food":{
		"Blueberries" : "blueberries",
		"Raw Chicken Breast" : "chicken.raw",
		"Burned Wolf Meat" : "wolfmeat.burned",
		"Candy Cane" : "candycane",
		"Cooked Pork" : "meat.pork.cooked",
		"Burned Human Meat" : "humanmeat.burned",
		"Cooked Wolf Meat" : "wolfmeat.cooked",
		"Can of Tuna" : "can.tuna",
		"Pumpkin" : "pumpkin",
		"Spoiled Chicken" : "chicken.spoiled",
		"Corn Seed" : "seed.corn",
		"Mushroom" : "mushroom",
		"Corn" : "corn",
		"Cooked Fish" : "fish.cooked",
		"Minnows" : "fish.minnows",
		"Chocolate Bar" : "chocholate",
		"Black Raspberries" : "black.raspberries",
		"Hemp Seed" : "seed.hemp",
		"Water Jug" : "waterjug",
		"Burnt Bear Meat" : "bearmeat.burned",
		"Bear Meat" : "bearmeat",
		"Bear Meat Cooked" : "bearmeat.cooked",
		"Can of Beans" : "can.beans",
		"Cooked Human Meat" : "humanmeat.cooked",
		"Small Trout" : "fish.troutsmall",
		"Granola Bar" : "granolabar",
		"Small Water Bottle" : "smallwaterbottle",
		"Raw Fish" : "fish.raw",
		"Rotten Apple" : "apple.spoiled",
		"Burned Pork" : "meat.pork.burned",
		"Cooked Chicken." : "chicken.cooked",
		"Pork" : "meat.boar",
		"Apple" : "apple",
		"Raw Wolf Meat" : "wolfmeat.raw",
		"Spoiled Wolf Meat" : "wolfmeat.spoiled",
		"Pumpkin Seed" : "seed.pumpkin",
		"Raw Human Meat" : "humanmeat.raw",
		"Burned Chicken" : "chicken.burned",
		"Spoiled Human Meat" : "humanmeat.spoiled"
	},
	"Items":{
		"Water Purifier" : "water.purifier",
		"XXL Picture Frame" : "sign.pictureframe.xxl",
		"Small Oil Refinery" : "small.oil.refinery",
		"Two Sided Town Sign Post" : "sign.post.town.roof",
		"Wooden Sign" : "sign.wooden.medium",
		"Survival Fish Trap" : "fishtrap.small",
		"Large Banner on pole" : "sign.pole.banner.large",
		"Reactive Target" : "target.reactive",
		"Large Furnace" : "furnace.large",
		"Huge Wooden Sign" : "sign.wooden.huge",
		"Two Sided Ornate Hanging Sign" : "sign.hanging.ornate",
		"Large Wooden Sign" : "sign.wooden.large",
		"Furnace" : "furnace",
		"Repair Bench" : "box.repair.bench",
		"Small Wooden Sign" : "sign.wooden.small",
		"Jack O Lantern Happy" : "jackolantern.happy",
		"Large Banner Hanging" : "sign.hanging.banner.large",
		"Ceiling Light" : "ceilinglight",
		"XL Picture Frame" : "sign.pictureframe.xl",
		"Pookie Bear" : "pookie.bear",
		"Landscape Picture Frame" : "sign.pictureframe.landscape",
		"Two Sided Hanging Sign" : "sign.hanging",
		"Wind Turbine" : "generator.wind.scrap",
		"Portrait Picture Frame" : "sign.pictureframe.portrait",
		"SUPER Stocking" : "stocking.large",
		"Single Sign Post" : "sign.post.single",
		"Bota Bag" : "botabag",
		"Water Barrel" : "water.barrel",
		"Bed" : "bed",
		"Wood Storage Box" : "box.wooden",
		"One Sided Town Sign Post" : "sign.post.town",
		"Large Wood Box" : "box.wooden.large",
		"Lantern" : "lantern",
		"Jack O Lantern Angry" : "jackolantern.angry",
		"Tall Picture Frame" : "sign.pictureframe.tall",
		"Small Stocking" : "stocking.small",
		"Camp Fire" : "campfire",
		"Salvaged Shelves" : "shelves",
		"Small Stash" : "stash.small",
		"Double Sign Post" : "sign.post.double",
		"Research Table" : "research.table",
		"Sleeping Bag" : "sleepingbag",
		"Paper Map" : "map"
	},
	"Medical":{
		"Large Medkit" : "largemedkit",
		"Medical Syringe" : "syringe.medical",
		"Bandage" : "bandage",
		"Anti-Radiation Pills" : "antiradpills",
		"Blood" : "blood"
	},
	"Misc":{
		"Acoustic Guitar" : "fun.guitar",
		"Medium Present" : "xmas.present.medium",
		"Note" : "note",
		"Large Present" : "xmas.present.large",
		"Small Present" : "xmas.present.small",
		"Coal :(" : "coal",
		"Door Key" : "door.key"
	},
	"Weapons":{
		"Longsword" : "longsword",
		"Weapon Lasersight" : "weapon.mod.lasersight",
		"Revolver" : "pistol.revolver",
		"Thompson" : "smg.thompson",
		"Pump Shotgun" : "shotgun.pump",
		"Bone Club" : "bone.club",
		"M249" : "lmg.m249",
		"Muzzle Brake" : "weapon.mod.muzzlebrake",
		"Silencer" : "weapon.mod.silencer",
		"Mace" : "mace",
		"4x Zoom Scope" : "weapon.mod.small.scope",
		"Eoka Pistol" : "pistol.eoka",
		"Weapon Flashlight" : "weapon.mod.flashlight",
		"Machete" : "machete",
		"Crossbow" : "crossbow",
		"Salvaged Sword" : "salvaged.sword",
		"Semi-Automatic Rifle" : "rifle.semiauto",
		"Stone Spear" : "spear.stone",
		"Muzzle Boost" : "weapon.mod.muzzleboost",
		"Custom SMG" : "smg.2",
		"Flame Thrower" : "flamethrower",
		"Rocket Launcher" : "rocket.launcher",
		"Salvaged Cleaver" : "salvaged.cleaver",
		"F1 Grenade" : "grenade.f1",
		"Holosight" : "weapon.mod.holosight",
		"Bone Knife" : "knife.bone",
		"Bolt Action Rifle" : "rifle.bolt",
		"Wooden Spear" : "spear.wooden",
		"Waterpipe Shotgun" : "shotgun.waterpipe",
		"Hunting Bow" : "bow.hunting",
		"Semi-Automatic Pistol" : "pistol.semiauto",
		"Beancan Grenade" : "grenade.beancan",
		"Assault Rifle" : "rifle.ak"
	},
	"Construction":{
		"Stone Barricade" : "barricade.stone",
		"Pump Jack" : "mining.pumpjack",
		"High External Wooden Gate" : "gates.external.high.wood",
		"Sheet Metal Door" : "door.hinged.metal",
		"Metal horizontal embrasure" : "shutter.metal.embrasure.a",
		"Wood Shutters" : "shutter.wood.a",
		"Metal Barricade" : "barricade.metal",
		"High External Stone Gate" : "gates.external.high.stone",
		"Sheet Metal Double Door" : "door.double.hinged.metal",
		"Wooden Door" : "door.hinged.wood",
		"Wooden Window Bars" : "wall.window.bars.wood",
		"Chainlink Fence" : "wall.frame.fence",
		"Code Lock" : "lock.code",
		"Building Plan" : "building.planner",
		"Reinforced Window Bars" : "wall.window.bars.toptier",
		"A floor grill" : "floor.grill",
		"Ladder Hatch" : "floor.ladder.hatch",
		"Prison Cell Gate" : "wall.frame.cell.gate",
		"Metal Window Bars" : "wall.window.bars.metal",
		"Wooden Ladder" : "ladder.wooden.wall",
		"Armored Double Door" : "door.double.hinged.toptier",
		"Wooden Barricade" : "barricade.wood",
		"Armored Door" : "door.hinged.toptier",
		"Small Water Catcher" : "water.catcher.small",
		"Barbed Wooden Barricade" : "barricade.woodwire",
		"Mining Quarry" : "mining.quarry",
		"Tool Cupboard" : "cupboard.tool",
		"Prison Cell Wall" : "wall.frame.cell",
		"Large Water Catcher" : "water.catcher.large",
		"Chainlink Fence Gate" : "wall.frame.fence.gate",
		"Metal Vertical embrasure" : "shutter.metal.embrasure.b",
		"Wood Double Door" : "door.double.hinged.wood",
		"High External Stone Wall" : "wall.external.high.stone",
		"Lock" : "lock.key",
		"Sandbag Barricade" : "barricade.sandbags",
		"High External Wooden Wall" : "wall.external.high",
		"Shop Front" : "wall.frame.shopfront",
		"Concrete Barricade" : "barricade.concrete"
	},
	"Resources":{
		"Empty Tuna Can" : "can.tuna.empty",
		"Battery - Small" : "battery.small",
		"Wood" : "wood",
		"Sulfur" : "sulfur",
		"High Quality Metal Ore" : "hq.metal.ore",
		"Charcoal" : "charcoal",
		"Metal Fragments" : "metal.fragments",
		"High Quality Metal" : "metal.refined",
		"Human Skull" : "skull.human",
		"Cloth" : "cloth",
		"Metal Ore" : "metal.ore",
		"CCTV Camera" : "cctv.camera",
		"Animal Fat" : "fat.animal",
		"Wolf Skull" : "skull.wolf",
		"Targeting Computer" : "targeting.computer",
		"Explosives" : "explosives",
		"Stones" : "stones",
		"Leather" : "leather",
		"Bone Fragments" : "bone.fragments",
		"Low Grade Fuel" : "lowgradefuel",
		"Crude Oil" : "crude.oil",
		"Sulfur Ore" : "sulfur.ore",
		"Empty Can Of Beans" : "can.beans.empty",
		"Paper" : "paper",
		"Gun Powder" : "gunpowder"
	},
	"Attire":{
		"Burlap Shirt" : "burlap.shirt",
		"Boots" : "shoes.boots",
		"Boonie Hat" : "hat.boonie",
		"Bandana Mask" : "mask.bandana",
		"Burlap Shoes" : "burlap.shoes",
		"Bone Jacket" : "bone.armor.jacket",
		"Riot Helmet" : "riot.helmet",
		"Snow Jacket - Red" : "jacket.snow",
		"Leather Gloves" : "burlap.gloves",
		"Metal Facemask" : "metal.facemask",
		"Pants" : "pants",
		"Bucket Helmet" : "bucket.helmet",
		"Hazmat Helmet" : "hazmat.helmet",
		"Wood Chestplate" : "wood.armor.jacket",
		"Bone Armor Pants" : "bone.armor.pants",
		"Beenie Hat" : "hat.beenie",
		"Hoodie" : "hoodie",
		"Road Sign Jacket" : "roadsign.jacket",
		"Hide Boots" : "attire.hide.boots",
		"Metal Chest Plate" : "metal.plate.torso",
		"Coffee Can Helmet" : "coffeecan.helmet",
		"Road Sign Kilt" : "roadsign.kilt",
		"Wolf Headdress" : "hat.wolf",
		"Candle Hat" : "hat.candle",
		"Hazmat Jacket" : "hazmat.jacket",
		"Hide Vest" : "attire.hide.vest",
		"Jacket" : "jacket",
		"Hazmat Boots" : "hazmat.boots",
		"Hide Skirt" : "attire.hide.skirt",
		"Hide Poncho" : "attire.hide.poncho",
		"Hazmat Pants" : "hazmat.pants",
		"Miners Hat" : "hat.miner",
		"T-Shirt" : "tshirt",
		"Improvised Balaclava" : "mask.balaclava",
		"Santa Hat" : "santahat",
		"Burlap Headwrap" : "burlap.headwrap",
		"Baseball Cap" : "hat.cap",
		"Hide Halterneck" : "attire.hide.helterneck",
		"Hide Pants" : "attire.hide.pants",
		"Hazmat Gloves" : "hazmat.gloves",
		"Burlap Trousers" : "burlap.trousers",
		"Longsleeve T-Shirt" : "tshirt.long",
		"Wood Armor Pants" : "wood.armor.pants"
	},
	"Tools":{
		"Stone Hatchet" : "stonehatchet",
		"Stone Pick Axe" : "stone.pickaxe",
		"Flare" : "flare",
		"Salvaged Hammer" : "hammer.salvaged",
		"Torch" : "torch",
		"Salvaged Axe" : "axe.salvaged",
		"Camera" : "tool.camera",
		"Survey Charge" : "surveycharge",
		"Rock" : "rock",
		"Water Bucket" : "bucket.water",
		"Supply Signal" : "supply.signal",
		"Salvaged Icepick" : "icepick.salvaged",
		"Hammer" : "hammer",
		"Hatchet" : "hatchet",
		"Timed Explosive Charge" : "explosive.timed",
		"Pick Axe" : "pickaxe"
	},
	"Ammo":{
		"HV 5.56 Rifle Ammo" : "ammo.rifle.hv",
		"High Velocity Arrow" : "arrow.hv",
		"Rocket" : "ammo.rocket.basic",
		"Explosive 5.56 Rifle Ammo" : "ammo.rifle.explosive",
		"High Velocity Rocket" : "ammo.rocket.hv",
		"Wooden Arrow" : "arrow.wooden",
		"Pistol Bullet" : "ammo.pistol",
		"12 Gauge Slug" : "ammo.shotgun.slug",
		"HV Pistol Ammo" : "ammo.pistol.hv",
		"12 Gauge Buckshot" : "ammo.shotgun",
		"Incendiary 5.56 Rifle Ammo" : "ammo.rifle.incendiary",
		"5.56 Rifle Ammo" : "ammo.rifle",
		"Incendiary Pistol Bullet" : "ammo.pistol.fire",
		"Handmade Shell" : "ammo.handmade.shell",
		"Smoke Rocket" : "ammo.rocket.smoke",
		"Incendiary Rocket" : "ammo.rocket.fire"
	}}
        self.ItemNames = dict()
        for cat in self.ItemList.keys():
            for name in self.ItemList[cat]:
                self.ItemNames[self.ItemList[cat][name]] = name

    def OnServerInitialized(self):
        commandlist = {
            'adminmenu.create' : 'createMenu',
            'adminmenu.destroy' : 'destroyMenu',
            'adminmenu.confirmaction' : 'confirmAction',
            'adminmenu.givemenu' : 'giveMenu',
            'adminmenu.banplayer' : 'banPlayer',
            'adminmenu.kick' : 'kickPlayer',
            'adminmenu.kill' : 'killPlayer',
            'adminmenu.goto' : 'gotoPlayer',
            'adminmenu.bring' : 'bringPlayer',
            'adminmenu.srcheck' : 'srCheck',
            'adminmenu.srclear' : 'srClear',
            'adminmenu.eco' : 'economy',
            'adminmenu.god' : 'godPlayer',
            'adminmenu.ungod' : 'ungodPlayer',
            'adminmenu.permissions' : 'permissionsMenu',
            'adminmenu.groups' : 'groupPermissionsMenu',
            'adminmenu.weather' : 'weather'}
        chatcommandlist = {
            'adminmenu':'createMenuFromChat',
            'adm' : 'createMenuFromChat',
            'adm_nobc' : 'disableBroadcast',
            'adm_resetconfig' : 'DefaultConfigChat',
            'adm_addquick' : 'AddQuickAccess',
            'adm_removequick' : 'RemoveQuickAccess'}

        for comm in commandlist.keys():
            command.AddConsoleCommand(comm, self.Plugin, commandlist[comm])
        for comm in chatcommandlist.keys():
            command.AddChatCommand(comm, self.Plugin, chatcommandlist[comm])
        try:
            if self.Config['SETTINGS']['Version'] != self.Default['SETTINGS']['Version']:
                self.updateConfig()  
        except:
            self.LoadDefaultConfig()
            
        permissions = [
            'adminmenu.allow.view',
            'adminmenu.allow.ban',
            'adminmenu.allow.kick',
            'adminmenu.allow.goto',
            'adminmenu.allow.bring',
            'adminmenu.allow.kill',
            'adminmenu.allow.all',
            'adminmenu.allow.economy',
            'adminmenu.allow.config',
            'adminmenu.allow.permissions']
        for perm in permissions:
            if not permission.PermissionExists(perm):
                permission.RegisterPermission(perm, self.Plugin)
        Messages = Dictionary[str,str]()
        for msgkey in self.Messages.keys():
            Messages.Add(msgkey,self.Messages[msgkey])
        lang.RegisterMessages(Messages,self.Plugin)
        
        self.dataTable = data.GetData('AdminMenu')
        if not self.dataTable.keys():
            self.dataTable['Staff'] = dict()
            self.dataTable['StaffQuickAccess'] = dict()
            data.SaveData('AdminMenu')

    def DefaultConfigChat(self,player,cmd,args):
        if permission.UserHasPermission(player.UserIDString,'adminmenu.allow.config'):
            print '[AdminMenu] Loading default config'
            self.LoadDefaultConfig()
            
    def LoadDefaultConfig(self):
        self.Config.clear()
        self.Config = self.Default
        self.SaveConfig()
        
    def Unload(self):
        self.destroyAllPlayerUI()

    def getCallingPlayer(self, args):
        try:
            callingplyID = str(args.connection).split('/')[1]
            for player in BasePlayer.activePlayerList:
                if player.UserIDString == callingplyID:
                    callingply = player
        except:
            callingply = None
        return callingply
    
    def getCallingPlayerID(self, args):
        try:
            callingplyID = self.getCallingPlayer(args).UserIDString
        except:
            callingplyID = None
        return callingplyID
                              
    def getCallingPlayerName(self, args):
        try:
            callingplyName = self.getCallingPlayer(args).displayName
        except:
            callingplyName = None
        return callingplyName
        
    def getAvailablePlayerPlugins(self, player):
        pluginlist = []
        for plug in self.Config['SETTINGS']['ConnectedPlugins']:
            x = 0
            if plugins.Exists(plug):
                for commcat in self.Config['COMMANDS'][plug].keys():
                    for comm in self.Config['COMMANDS'][plug][commcat]:
                        permneeded = self.Config['COMMANDS'][plug][commcat][comm][4]
                        if permneeded == ' ':
                            if player.IsAdmin() or permission.UserHasGroup(player.UserIDString,'admin'):
                                x += 1
                        else:
                            if permission.UserHasPermission(player.UserIDString,permneeded) or (plug == 'AdminMenu' and permission.UserHasPermission(player.UserIDString,'adminmenu.allow.all')):
                                x += 1
            if x > 0:
                if plug not in pluginlist:
                    pluginlist.append(plug)
        return pluginlist
        
    def getPlayerslist(self, pagename, getPageNum = False):
        players = list()
        for player in BasePlayer.activePlayerList:
            players.append(player)
        for player in BasePlayer.sleepingPlayerList:
            players.append(player)
        x = 0
        playerPages = dict()
        done = False
        while not done:
            currpage = 'page%d'%x
            currpagelist = []
            for i in range(self.Config['SETTINGS']['PlayersPerPage']):
                try:
                    currpagelist.append(players[self.Config['SETTINGS']['PlayersPerPage']*x+i])
                except IndexError:
                    done = True
                    playerPages[currpage] = currpagelist
            playerPages[currpage] = currpagelist
            x += 1
        if getPageNum:
            return len(playerPages.keys())
        else:
            return playerPages[pagename]
    
    def createMenuFromChat(self,player,cmd,args):
        self.createMenu(args, player, player.UserIDString, player.displayName)

    def checkConfig(self):
        for plug in self.Config['SETTINGS']['ConnectedPlugins']:
            if plugins.Exists(plug):
                if plug not in self.Config['COMMANDS'].keys():
                    try:
                        default_plug = self.Default['COMMANDS'][plug]
                        self.Config['COMMANDS'][plug] = default_plug
                        print '[AdminMenu]Defaulting plugin %s' % default_plug
                    except:
                        print '[AdminMenu] %s not in default config.' % default_plug
                    self.SaveConfig()
                for default in self.Default['COMMANDS'][plug].keys():
                    if default not in self.Config['COMMANDS'][plug].keys():
                        self.Config['COMMANDS'][plug][default] = self.Default['COMMANDS'][plug][default]
                for commandcat in self.Config['COMMANDS'][plug].keys():
                    if ' ' in commandcat:
                        print '[AdminMenu] Invalid Config - CommandCategory has spaces: %s' % commandcat
                        newcommandcat = commandcat.replace(' ','_')
                        self.Config['COMMANDS'][plug][newcommandcat] = self.Config['COMMANDS'][plug][commandcat]
                        del self.Config['COMMANDS'][plug][commandcat]
                        print '[AdminMenu]Replacing spaces with _' 
                        self.SaveConfig()
                    cmdCat = self.Config['COMMANDS'][plug][commandcat].keys()
                    commandNum   = len(cmdCat)
                    valid = range(1,commandNum+1)
                    actual = []
                    for cmd in cmdCat:
                        actual.append(self.Config['COMMANDS'][plug][commandcat][cmd][0])
                        #check the command has a valid button colour
                        if self.Config['COMMANDS'][plug][commandcat][cmd][2] not in self.TINTCOLOURS.keys():
                            unknown_tint = self.Config['COMMANDS'][plug][commandcat][cmd][2]
                            print '[AdminMenu] Invalid Config - Unknown Tint name: %s' % unknown_tint
                            try:
                                default_tint = self.Default['COMMANDS'][plug][commandcat][cmd][2]
                                self.Config['COMMANDS'][plug][commandcat][cmd][2] = default_tint
                                print '[AdminMenu]Defaulting tint to %s' % default_tint
                            except:
                                default_tint = 'red_tint'
                                self.Config['COMMANDS'][plug][commandcat][cmd][2] = default_tint
                                print '[AdminMenu]Defaulting tint to %s' % default_tint
                            self.SaveConfig()
                        #Check the command has  avalid text colour
                        if self.Config['COMMANDS'][plug][commandcat][cmd][3] not in self.TEXTCOLOURS.keys():
                            unknown_text = self.Config['COMMANDS'][plug][commandcat][cmd][3]
                            print '[AdminMenu] Invalid Config - Unknown Text Colour: %s' % unknown_text
                            try:
                                default_tint = self.Default['COMMANDS'][plug][commandcat][cmd][3]
                                self.Config['COMMANDS'][plug][commandcat][cmd][3] = default_tint
                                print '[AdminMenu]Defaulting colour to %s' % default_tint
                            except:
                                default_tint = 'white_text'
                                self.Config['COMMANDS'][plug][commandcat][cmd][3] = default_tint
                                print '[AdminMenu]Defaulting colour to %s' % default_tint
                            self.SaveConfig()
                        ##Check the last command element is either True or False
                        if self.Config['COMMANDS'][plug][commandcat][cmd][5] not in [True,False]:
                            print '[AdminMenu] Invalid Config - last element should be True or False'
                            try:
                                default_val = self.Default['COMMANDS'][plug][commandcat][cmd][5]
                                self.Config['COMMANDS'][plug][commandcat][cmd][5] = default_val
                                print '[AdminMenu]Defaulting value to %s' % default_val
                            except:
                                default_val = False
                                self.Config['COMMANDS'][plug][commandcat][cmd][5] = default_val
                                print '[AdminMenu]Defaulting value to %s' % default_val
                            self.SaveConfig()
                    #If there isn't every number then rese to default 1,2,3,4,5.....
                    if sorted(actual) != sorted(valid):
                        print '[AdminMenu]Invalid Config - Command order for Plugin: %s CommandCategory: %s is incorrect'% (plug, commandcat)
                        print '[AdminMenu]Defaulting section AdminMenu Config [COMMANDS][%s][%s]' % (plug,commandcat)
                        i = 1
                        for cmd in cmdCat:
                            oldcmd = list(self.Config['COMMANDS'][plug][commandcat][cmd])
                            oldcmd[0] = i
                            self.Config['COMMANDS'][plug][commandcat][cmd] = oldcmd
                            i += 1
                        self.SaveConfig()
                        
    def updateConfig(self):
        self.Config['SETTINGS']['Version'] = self.Default['SETTINGS']['Version']
        self.SaveConfig()
        print '[AdminMenu] Outdated Config - Updating....'
        for category in self.Default.keys():
            for key in self.Default[category].keys():
                if key not in self.Config[category].keys():
                    self.Config[category][key] = self.Default[category][key]
                if key == 'ConnectedPlugins' and self.Config[category][key] != self.Default[category][key]:
                    self.Config[category][key] = self.Default[category][key]
        self.SaveConfig()
        self.checkConfig()
        print '[AdminMenu] Finished Updating Config'

    def createMenu(self, args, callingply = None, callingplyID = None, callingplyName = None):
        if not callingply or not callingplyID or not callingplyName:
            callingply = self.getCallingPlayer(args)
            callingplyID = callingply.UserIDString
            callingplyName = callingply.displayName
        if not callingply:
            return
        if not callingply.IsAdmin() and not permission.UserHasPermission(callingplyID,'adminmenu.allow.view'):
            rust.SendChatMessage(callingply,lang.GetMessage('noPermissionToView',self.Plugin,callingplyID) , None, '0')
            return
        self.checkConfig()
        SelectedPlugin = 'AdminMenu'
        PlayerPage = 'page0'
        SelectedCmdCat = None
        try:
            for arg in args.ArgsStr.split(' '):
                if arg in self.Config['SETTINGS']['ConnectedPlugins']:
                    SelectedPlugin = arg
                elif 'page' in arg:
                    PlayerPage = arg
                elif arg in self.Config['COMMANDS'][SelectedPlugin].keys():
                    SelectedCmdCat = arg
        except:
            pass
        if not SelectedCmdCat:
            SelectedCmdCat = self.Config['COMMANDS'][SelectedPlugin].keys()[0]
        playerPlugins = self.getAvailablePlayerPlugins(callingply)
        minX = 0.1
        minY = 0.2
        maxX = 0.9
        maxY = 0.9
        if callingplyID in self.PlayerUIs.keys():
            self.destroyMenu(args) 
            elements = Cui.CuiElementContainer()
        else:
            elements = Cui.CuiElementContainer()
        
        panel = Cui.CuiPanel()
        panel.Image.Color = self.TINTCOLOURS['grey_tint']
        panel.Image.FadeIn = self.fadeInTime
        panel.RectTransform.AnchorMin = '%f %f' % (minX, minY)
        panel.RectTransform.AnchorMax = '%f %f' % (maxX, maxY)
        panel.CursorEnabled = True
        elements.Add(panel)
        
        title_txtcomp = Cui.CuiTextComponent(Text = 'Admin menu V%s by FlamingMojo' % self.Config['SETTINGS']['Version'],
                        Color = self.TEXTCOLOURS['orange_text'],
                        FadeIn = self.fadeInTime,
                        Align = TextAnchor.MiddleCenter,
                        FontSize = self.fontSize)
        title_rectcomp = Cui.CuiRectTransformComponent(
            AnchorMin = '%f %f' % (minX,maxY - 0.05),
            AnchorMax = '%f %f' % (maxX,maxY))
        title_guielem = Cui.CuiElement(FadeOut = self.fadeOutTime)
        title_guielem.Components.Add(title_txtcomp)
        title_guielem.Components.Add(title_rectcomp)
        elements.Add(title_guielem)
        plugin_index = 0
        cmdcat_index = 0
        for plug in playerPlugins:
            plugin_btn = Cui.CuiButton()
            plugin_btn.Button.Command = 'adminmenu.create %s' % plug
            plugin_btn.Button.Color = self.TINTCOLOURS['black_tint']
            plugin_btn.Text.Text = plug
            plugin_btn.Text.Color = self.TEXTCOLOURS['white_text']
            plugin_btn.Text.FontSize = self.fontSize
            plugin_btn.Text.Align = TextAnchor.MiddleCenter
            plugin_btn.RectTransform.AnchorMin = '%f %f' % (minX+0.01,maxY-(0.125+(0.025*plugin_index)+(0.025*cmdcat_index)))
            plugin_btn.RectTransform.AnchorMax = '%f %f' % (minX+0.16,maxY-(0.1+(0.025*plugin_index)+(0.025*cmdcat_index)))
            plugin_index += 1
            if plug == SelectedPlugin:
                plugin_btn.Button.Color = self.TINTCOLOURS['green_tint']
                commandlist = self.Config['COMMANDS'][plug].keys()
                for cmdcat in commandlist:
                    newbtn = Cui.CuiButton()
                    newbtn.Button.Command = 'adminmenu.create %s %s %s' % (PlayerPage,plug,cmdcat)
                    newbtn.Button.Color = self.TINTCOLOURS['grey_tint']
                    if SelectedCmdCat:
                        if cmdcat == SelectedCmdCat:
                            newbtn.Button.Color = self.TINTCOLOURS['blue_tint']
                    newbtn.Text.Text = cmdcat
                    newbtn.Text.Color = self.TEXTCOLOURS['white_text']
                    newbtn.Text.FontSize = self.fontSize
                    newbtn.Text.Align = TextAnchor.MiddleCenter
                    newbtn.RectTransform.AnchorMin =  '%f %f' % (minX+0.04,maxY-(0.125+(0.025*plugin_index)+(0.025*cmdcat_index)))
                    newbtn.RectTransform.AnchorMax = '%f %f' % (minX+0.16,maxY-(0.1+(0.025*plugin_index)+(0.025*cmdcat_index)))
                    cmdcat_index += 1
                    elements.Add(newbtn)
            elements.Add(plugin_btn)
        player_labels = dict()
        if SelectedCmdCat != 'Info':      
            currentCmdCat = self.Config['COMMANDS'][SelectedPlugin][SelectedCmdCat].keys()
            for cmd in currentCmdCat:
                currcom = list(self.Config['COMMANDS'][SelectedPlugin][SelectedCmdCat][cmd])
                NoPerm = False
                cmd_btn_index = currcom[0] - 1
                currcom[2] = self.TINTCOLOURS[currcom[2]]
                currcom[3] = self.TEXTCOLOURS[currcom[3]]
                if currcom[4] == ' ' and callingply.IsAdmin():
                    NoPerm = True
                if permission.UserHasPermission(callingplyID,currcom[4]) or (SelectedPlugin == 'AdminMenu' and permission.UserHasPermission(callingplyID, 'adminmenu.allow.all')) or NoPerm:
                    if currcom[5]:
                        #then it's a player-specific command
                        page = int(PlayerPage.replace('page',''))
                        playerList = self.getPlayerslist(PlayerPage)
                        totalPages = self.getPlayerslist(PlayerPage,True)
                        if page < totalPages -1:
                            nextpage = page+1
                        else:
                            nextpage = 0
                        if page > 0:
                            prevpage = page-1
                        else:
                            prevpage = totalPages-1
                        if 'NextPrev' not in player_labels.keys():
                            next_btn = Cui.CuiButton()
                            next_btn.Button.Command = 'adminmenu.create page%d %s %s' % (nextpage,SelectedPlugin,SelectedCmdCat)
                            next_btn.Button.Color = self.TINTCOLOURS['black_tint'] 
                            next_btn.Text.Text = '>>>'
                            next_btn.Text.Color = self.TEXTCOLOURS['white_text']
                            next_btn.Text.FontSize = self.fontSize
                            next_btn.Text.Align = TextAnchor.MiddleCenter
                            next_btn.RectTransform.AnchorMin =  '%f %f'%(minX + 0.23, minY+0.01)
                            next_btn.RectTransform.AnchorMax = '%f %f'%(minX + 0.28, minY+0.06)
                            elements.Add(next_btn)

                            prev_btn = Cui.CuiButton()
                            prev_btn.Button.Command = 'adminmenu.create page%d %s %s' % (prevpage,SelectedPlugin,SelectedCmdCat)
                            prev_btn.Button.Color = self.TINTCOLOURS['black_tint']
                            prev_btn.Text.Text = '<<<'
                            prev_btn.Text.Color = self.TEXTCOLOURS['white_text']
                            prev_btn.Text.FontSize = self.fontSize
                            prev_btn.Text.Align = TextAnchor.MiddleCenter
                            prev_btn.RectTransform.AnchorMin =  '%f %f'%(minX + 0.17, minY+0.01)
                            prev_btn.RectTransform.AnchorMax = '%f %f'%(minX + 0.22, minY+0.06)
                            elements.Add(prev_btn)

                            player_labels['NextPrev'] = True
    
                        player_index = 0
                        for plr in playerList:
                            try:
                                playerIP = plr.net.connection.ipaddress
                                playerAwake = True
                            except:
                                playerIP = 'None (Sleeping)'
                                playerAwake = False
                            if playerAwake:
                                txtcolour = self.TEXTCOLOURS['white_text']
                            else:
                                txtcolour = self.TEXTCOLOURS['yellow_text']
                            player_string = plr.displayName
                            playerpos = plr.GetEstimatedWorldPosition()
                            temp = []
                            
                            for item in str(playerpos).split(' '):
                                item = item.replace('(' ,'')
                                item = item.replace(')' ,'')
                                item = item.replace(',' ,'')
                                temp.append(float(item))
                            playerpos = temp
                            currplrcom = list(('','','','','',''))
                            currplrcom[1]= currcom[1].replace('{PLAYERID}',plr.UserIDString)
                            currplrcom[1] = currplrcom[1].replace('{PLAYERNAME}',plr.displayName)
                            currplrcom[1] = currplrcom[1].replace('{ADMINID}',callingply.UserIDString)
                            currplrcom[1] = currplrcom[1].replace('{ADMINNAME}',callingply.displayName)
                            currplrcom[1] = currplrcom[1].replace('{PLAYERPOS}','%f %f %f' % tuple(playerpos))
                            currplrcom[2] = currcom[2]
                            currplrcom[3] = currcom[3]
                            if plr.UserIDString not in player_labels.keys():
                                player_lbl = Cui.CuiElement(FadeOut = self.fadeOutTime)
                                player_lbl_Txt = Cui.CuiTextComponent(Text = player_string,
                                        Color = txtcolour,
                                        FadeIn = self.fadeInTime,
                                        Align = TextAnchor.MiddleLeft,
                                        FontSize = self.fontSize)
                                player_lbl_Rect = Cui.CuiRectTransformComponent(AnchorMin = '%f %f' % (minX +0.18,maxY -(0.25+(0.025*player_index)+0.025)),
                                                                                AnchorMax = '%f %f' % (minX +0.25, maxY -(0.25+(0.025*player_index))))
                                player_lbl.Components.Add(player_lbl_Txt)
                                player_lbl.Components.Add(player_lbl_Rect)
                                elements.Add(player_lbl)
                                player_labels[plr.UserIDString] = True
                            plrbtn = Cui.CuiButton()
                            plrbtn.Button.Command = currplrcom[1]
                            plrbtn.Button.Color = currplrcom[2]
                            plrbtn.Text.Text = cmd
                            plrbtn.Text.Color = currplrcom[3]
                            plrbtn.Text.FontSize = self.fontSize
                            plrbtn.Text.Align = TextAnchor.MiddleCenter
                            plrbtn.RectTransform.AnchorMin = '%f %f' % (minX +0.28+(0.05*cmd_btn_index),maxY -(0.25+(0.025*player_index)+0.025))
                            plrbtn.RectTransform.AnchorMax = '%f %f' % (minX +0.32+(0.05*cmd_btn_index), maxY -(0.25+(0.025*player_index)))
                            player_index += 1
                            elements.Add(plrbtn)
                            
                    else:
                        #Then it's a general Admin Command
                        cmdbtn = Cui.CuiButton()
                        cmdbtn.Button.Command = currcom[1]
                        cmdbtn.Button.Color = currcom[2]
                        cmdbtn.Text.Text = cmd
                        cmdbtn.Text.Color = currcom[3]
                        cmdbtn.Text.FontSize = self.fontSize
                        cmdbtn.Text.Align = TextAnchor.MiddleCenter
                        cmdbtn.RectTransform.AnchorMin = '%f %f' % (minX +0.28+(0.05*cmd_btn_index), maxY -0.25)
                        cmdbtn.RectTransform.AnchorMax = '%f %f' % (minX +0.32+(0.05*cmd_btn_index), maxY -0.225)
                        elements.Add(cmdbtn)
                else:
                    #then user has no permissions; this shouldn't happen
                    pass
        elif permission.UserHasPermission(callingplyID, 'adminmenu.allow.view') or callingply.IsAdmin():
            #Display player info if no command selected
            title_txtcomp = Cui.CuiTextComponent(Text = 'NAME                                  ID                                       IP                             POS',
                            Color = self.TEXTCOLOURS['orange_text'],
                            FadeIn = self.fadeInTime,
                            Align = TextAnchor.MiddleLeft,
                            FontSize = self.fontSize)
            title_rectcomp = Cui.CuiRectTransformComponent(
                AnchorMin = '%f %f' % (minX+0.18,maxY - 0.25),
                AnchorMax = '%f %f' % (minX+0.6,maxY-0.15))
            title_guielem = Cui.CuiElement(FadeOut = self.fadeOutTime)
            title_guielem.Components.Add(title_txtcomp)
            title_guielem.Components.Add(title_rectcomp)
            elements.Add(title_guielem)
            playerList = self.getPlayerslist(PlayerPage)
            totalPages = self.getPlayerslist(PlayerPage,True)
            page = int(PlayerPage.replace('page','')) 
            if page < totalPages -1:
                nextpage = page+1
            else:
                nextpage = 0
            if page > 0:
                prevpage = page-1
            else:
                prevpage = totalPages-1
            if 'NextPrev' not in player_labels.keys():
                next_btn = Cui.CuiButton()
                next_btn.Button.Command = 'adminmenu.create page%d %s %s' % (nextpage,SelectedPlugin,SelectedCmdCat)
                next_btn.Button.Color = self.TINTCOLOURS['black_tint'] 
                next_btn.Text.Text = '>>>'
                next_btn.Text.Color = self.TEXTCOLOURS['white_text']
                next_btn.Text.FontSize = self.fontSize
                next_btn.Text.Align = TextAnchor.MiddleCenter
                next_btn.RectTransform.AnchorMin =  '%f %f'%(minX + 0.23, minY+0.01)
                next_btn.RectTransform.AnchorMax = '%f %f'%(minX + 0.28, minY+0.06)
                elements.Add(next_btn)

                prev_btn = Cui.CuiButton()
                prev_btn.Button.Command = 'adminmenu.create page%d %s %s' % (prevpage,SelectedPlugin,SelectedCmdCat)
                prev_btn.Button.Color = self.TINTCOLOURS['black_tint']
                prev_btn.Text.Text = '<<<'
                prev_btn.Text.Color = self.TEXTCOLOURS['white_text']
                prev_btn.Text.FontSize = self.fontSize
                prev_btn.Text.Align = TextAnchor.MiddleCenter
                prev_btn.RectTransform.AnchorMin =  '%f %f'%(minX + 0.17, minY+0.01)
                prev_btn.RectTransform.AnchorMax = '%f %f'%(minX + 0.22, minY+0.06)
                elements.Add(prev_btn)

                player_labels['NextPrev'] = True
            player_index = 0
            for plr in playerList:
                try:
                    playerIP = plr.net.connection.ipaddress
                    playerAwake = True
                except:
                    playerIP = 'None (Sleeping)'
                    playerAwake = False
                if playerAwake:
                    txtcolour = self.TEXTCOLOURS['white_text']
                else:
                    txtcolour = self.TEXTCOLOURS['yellow_text']
                
                playerpos = plr.GetEstimatedWorldPosition()
                
                player_string = '%s    %s    %s    %s' % (plr.displayName, plr.UserIDString,playerIP,playerpos)

                player_lbl = Cui.CuiElement(FadeOut = self.fadeOutTime)
                player_lbl_Txt = Cui.CuiTextComponent(Text = player_string,
                        Color = txtcolour,
                        FadeIn = self.fadeInTime,
                        Align = TextAnchor.MiddleLeft,
                        FontSize = self.fontSize)
                player_lbl_Rect = Cui.CuiRectTransformComponent(AnchorMin = '%f %f' % (minX +0.18,maxY -(0.25+(0.025*player_index)+0.025)),
                                                                AnchorMax = '%f %f' % (minX +0.6, maxY -(0.25+(0.025*player_index))))
                player_lbl.Components.Add(player_lbl_Txt)
                player_lbl.Components.Add(player_lbl_Rect)
                player_index += 1
                elements.Add(player_lbl)
        #And a close button no matter what
        close_btn = Cui.CuiButton()
        close_btn.Button.Command = 'adminmenu.destroy'
        close_btn.Button.Color = self.TINTCOLOURS['red_tint'] 
        close_btn.Text.Text = 'Close'
        close_btn.Text.Color = self.TEXTCOLOURS['white_text']
        close_btn.Text.FontSize = self.fontSize
        close_btn.Text.Align = TextAnchor.MiddleCenter
        close_btn.RectTransform.AnchorMin =  '%f %f' % (maxX-0.1,maxY-0.06)
        close_btn.RectTransform.AnchorMax = '%f %f' % (maxX-0.01,maxY-0.01)
        elements.Add(close_btn)
        if 'StaffQuickAccess' not in self.dataTable.keys():
            self.dataTable['StaffQuickAccess'] = dict()
        if callingply.UserIDString not in self.dataTable['StaffQuickAccess'].keys():
            self.dataTable['StaffQuickAccess'][callingply.UserIDString] = dict()
        else:
            for btn in self.dataTable['StaffQuickAccess'][callingply.UserIDString].keys():
                quickcmd = self.dataTable['StaffQuickAccess'][callingply.UserIDString][btn]
                quick_btn = Cui.CuiButton()
                quick_btn.Button.Command = quickcmd['command']
                quick_btn.Button.Color = self.TINTCOLOURS[quickcmd['btncolour']] 
                quick_btn.Text.Text = btn
                quick_btn.Text.Color = self.TEXTCOLOURS[quickcmd['textcolour']]
                quick_btn.Text.FontSize = self.fontSize
                quick_btn.Text.Align = TextAnchor.MiddleCenter
                quickbtn_index = quickcmd['index']
                quick_btn.RectTransform.AnchorMin =  '%f %f' % (minX + 0.01+(quickbtn_index * 0.07),minY+0.1)
                quick_btn.RectTransform.AnchorMax = '%f %f' % (minX+0.06 +(quickbtn_index * 0.07),minY+0.15)
                elements.Add(quick_btn)
        if permission.UserHasPermission(callingply.UserIDString,'adminmenu.allow.permissions'):
            groups_btn = Cui.CuiButton()
            groups_btn.Button.Command = 'adminmenu.groups'
            groups_btn.Button.Color = self.TINTCOLOURS['blue_tint'] 
            groups_btn.Text.Text = 'Groups'
            groups_btn.Text.Color = self.TEXTCOLOURS['white_text']
            groups_btn.Text.FontSize = self.fontSize
            groups_btn.Text.Align = TextAnchor.MiddleCenter
            groups_btn.RectTransform.AnchorMin =  '%f %f' % (minX + 0.01,maxY-0.06)
            groups_btn.RectTransform.AnchorMax = '%f %f' % (minX+0.06,maxY-0.01)
            elements.Add(groups_btn)
        self.PlayerUIs[callingplyID] = elements
        self.changeParent(callingply)
        Cui.CuiHelper.AddUi(callingply,self.PlayerUIs[callingplyID])
        
    def confirmAction(self, args):
        self.destroyMenu(args)
        targetPlyID = ''
        targetPlyName = ''
        for player in BasePlayer.activePlayerList:
            if player.UserIDString in args.ArgsStr:
                targetPly = player
                targetPlyName = player.displayName
                targetPlyID = player.UserIDString
        for player in BasePlayer.sleepingPlayerList:
            if player.UserIDString in args.ArgsStr:
                targetPly = player
                targetPlyName = player.displayName
                targetPlyID = player.UserIDString
        
        if 'kick' in args.ArgsStr:
            cmd = 'kick'
            length = len(self.Config['REASONS']['KICK'])*0.025
        elif 'ban' in args.ArgsStr:
            cmd = 'ban'
            length = len(self.Config['REASONS']['BAN'])*0.025
        else:
            return
        callingply = self.getCallingPlayer(args)
        callingplyID = callingply.UserIDString
        callingplyName = callingply.displayName
        elements = Cui.CuiElementContainer()
        box_panel = Cui.CuiPanel()
        box_panel.Image.Color = self.TINTCOLOURS['grey_tint']
        box_panel.Image.FadeIn = self.fadeInTime
        box_panel.RectTransform.AnchorMin = '0.3 %f' % (0.8 - length)
        box_panel.RectTransform.AnchorMax = '0.7 0.8'
        box_panel.CursorEnabled = True
        elements.Add(box_panel)
        reasons = None
        if cmd == 'kick':
            command = 'adminmenu.kick %s "%s"'
            txt = 'Why Kick %s ?' %targetPlyName
            reasons = self.Config['REASONS']['KICK']
        elif cmd == 'ban':
            reasons = self.Config['REASONS']['BAN']
            command = 'adminmenu.ban %s "%s"'
            txt = 'Why Perma Ban %s ?' %targetPlyName
        if not reasons:
            reasons = ['No Reason']            
        title_txtcomp = Cui.CuiTextComponent(Text = txt,
                        Color = self.TEXTCOLOURS['white_text'],
                        FadeIn = self.fadeInTime,
                        Align = TextAnchor.MiddleCenter,
                        FontSize = 24)
        title_rectcomp = Cui.CuiRectTransformComponent(AnchorMin = "0.3 0.75", AnchorMax = "0.7 0.8")
        title_guielem = Cui.CuiElement(FadeOut = self.fadeOutTime)
        title_guielem.Components.Add(title_txtcomp)
        title_guielem.Components.Add(title_rectcomp)
        elements.Add(title_guielem)
        x = 1
        for reason in reasons:
            yes_btn = Cui.CuiButton()
            yes_btn.Button.Command = command % (targetPlyID, reason)
            yes_btn.Button.Color = self.TINTCOLOURS['grey_tint']
            yes_btn.Text.Text = reason
            yes_btn.Text.Color = self.TEXTCOLOURS['white_text']
            yes_btn.Text.FontSize = self.fontSize
            yes_btn.Text.Align = TextAnchor.MiddleCenter
            yes_btn.RectTransform.AnchorMin = '0.3 %f' % (0.7-(x*0.025))
            yes_btn.RectTransform.AnchorMax = '0.7 %f' % ((0.7-(x*0.025))+0.025)
            elements.Add(yes_btn)
            x += 1
        no_btn = Cui.CuiButton()
        no_btn.Button.Command = 'adminmenu.destroy'
        no_btn.Button.Color = self.TINTCOLOURS['red_tint']
        no_btn.Text.Text = "Don't %s" %cmd
        no_btn.Text.Color = self.TEXTCOLOURS['white_text']
        no_btn.Text.FontSize = self.fontSize
        no_btn.Text.Align = TextAnchor.MiddleCenter
        no_btn.RectTransform.AnchorMin = '0.3 %f' % (0.675 - length)
        no_btn.RectTransform.AnchorMax = '0.7 %f' % (0.7 - length)
        elements.Add(no_btn)
        self.PlayerUIs[callingplyID] = elements
        self.changeParent(callingply)
        Cui.CuiHelper.AddUi(callingply,self.PlayerUIs[callingplyID])
        

    def economy(self,args):
        if not plugins.Exists('Economics'):
            print 'Economics is not installed'
        else:
            for player in BasePlayer.activePlayerList:
                if player.UserIDString in args.ArgsStr:
                    targetplyID = player.UserIDString
                    targetply = player
                    targetplyName = player.displayName
            for player in BasePlayer.sleepingPlayerList:
                if player.UserIDString in args.ArgsStr:
                    targetplyID = player.UserIDString
                    targetply = player
                    targetplyName = player.displayName
            callingply = self.getCallingPlayer(args)
            Bal = False
            amount = 0
            for arg in args.ArgsStr.split(' '):
                try:
                    amount = int(arg)
                except ValueError:
                    if arg == 'balance':
                        Bal = True
            if Bal:
                ECO = plugins.Find('Economics')
                balance = ECO.Call('GetPlayerMoney',targetplyID)
                if not balance:
                    balance = 0
                msg = lang.GetMessage('Balance',self.Plugin,callingply.UserIDString) % balance
                msg = msg.replace('{PLAYERNAME}',targetplyName)
                rust.SendChatMessage(callingply, msg, None, '0')
                return
            canUse = False
            ranks = self.Config['SETTINGS']['Economy'].keys()
            cooldown = 0
            for rank in ranks:
                if permission.UserHasGroup(callingply.UserIDString,rank):
                    cooldown = self.Config['SETTINGS']['Economy'][rank]
            if callingply.UserIDString not in self.dataTable['Staff'].keys():
                self.dataTable['Staff'][callingply.UserIDString] = dict()              
                data.SaveData('AdminMenu')
                canUse = True
            else:
                if targetplyID not in self.dataTable['Staff'][callingply.UserIDString].keys():
                    self.dataTable['Staff'][callingply.UserIDString][targetplyID] = time.time()
                    canUse = True
                    data.SaveData('AdminMenu')
                else:
                    if time.time() > self.dataTable['Staff'][callingply.UserIDString][targetplyID] + cooldown:
                        canUse = True
                        self.dataTable['Staff'][callingply.UserIDString][targetplyID] = time.time()
                        data.SaveData('AdminMenu')
                    else:
                        timeleft = round((self.dataTable['Staff'][callingply.UserIDString][targetplyID] + cooldown)- time.time())
                        timeleftstr = time.strftime('%H:%M:%S',time.localtime(timeleft))
                        msg = lang.GetMessage('ecoCooldown',self.Plugin,callingply.UserIDString) % timeleftstr
                        rust.SendChatMessage(callingply, msg, None, '0')
                
            if canUse:  
                if amount > 0:
                    rust.RunServerCommand('eco.c deposit %s %d' % (targetplyID,amount))
                    msg = lang.GetMessage('giveMoney',self.Plugin,callingply.UserIDString) % amount
                    msg = msg.replace('{PLAYERNAME}',targetplyName)
                    rust.SendChatMessage(callingply, msg, None, '0')
                    return
                if amount < 0:
                    rust.RunServerCommand('eco.c withdraw %s %d' % (targetplyID,abs(amount)))
                    msg = lang.GetMessage('takeMoney',self.Plugin,callingply.UserIDString) % abs(amount)
                    msg = msg.replace('{PLAYERNAME}',targetplyName)
                    rust.SendChatMessage(callingply, msg, None, '0')
                    return
    def getPlayerName(self, playerid):
        name = ''
        for player in BasePlayer.activePlayerList:
            if player.UserIDString == playerid:
                name = player.displayName
        for player in BasePlayer.sleepingPlayerList:
            if player.UserIDString == playerid:
                name = player.displayName
        return name

    
    def killPlayer(self, args):
        callingply = self.getCallingPlayer(args)
        callingplyID = self.getCallingPlayerID(args)
        callingplyName = self.getCallingPlayerName(args)
        if permission.UserHasPermission(callingplyID,'adminmenu.allow.kill')or permission.UserHasPermission(callingplyID, 'adminmenu.allow.all'):
            try:
                targetplyID = args.ArgsStr.split(' ')[0]
                for player in BasePlayer.activePlayerList:
                    if player.UserIDString == targetplyID:
                        player.Die()
                    if self.Config['SETTINGS']['Broadcast']:
                        msg = lang.GetMessage('killPlayer',self.Plugin,callingply.UserIDString)
                        msg = msg.replace('{ADMINNAME}',callingplyName)
                        msg = msg.replace('{PLAYERNAME}',self.getPlayerName(targetplyID))
                        rust.BroadcastChat(msg, None, '0')
            except:
                print 'Cannot find target'
                    
    def srCheck(self,args):
        if not plugins.Exists('ServerRewards'):
            print 'ServerRewards is not installed'
        else:
            callingply = self.getCallingPlayer(args)
            SR = plugins.Find('ServerRewards')
            targetplyID = None
            for player in BasePlayer.activePlayerList:
                if player.UserIDString in args.ArgsStr:
                    targetplyID = player.UserIDString
                    targetply = player
            for player in BasePlayer.sleepingPlayerList:
                if player.UserIDString in args.ArgsStr:
                    targetplyID = player.UserIDString
                    targetply = player
            if targetplyID:
                worked = SR.Call("CheckPoints",targetplyID)
                if not worked:
                    worked = 0
                msg = lang.GetMessage('srCheck',self.Plugin,callingply.UserIDString) % worked
                playerName = targetply.displayName
                msg = msg.replace('{PLAYERNAME}',playerName)
                rust.SendChatMessage(callingply, msg, None, '0')
            else:
                print 'Cannot find player'

    def srClear(self,args):
        if not plugins.Exists('ServerRewards'):
            print 'ServerRewards is not installed'
        else:
            callingply = self.getCallingPlayer(args)
            SR = plugins.Find('ServerRewards')
            targetplyID = None
            targetply = None
            for player in BasePlayer.activePlayerList:
                if player.UserIDString in args.ArgsStr:
                    targetplyID = player.UserIDString
                    targetply = player
            for player in BasePlayer.sleepingPlayerList:
                if player.UserIDString in args.ArgsStr:
                    targetplyID = player.UserIDString
                    targetply = player
            if targetplyID:
                SR.Call("RemovePlayer",targetplyID)
                playerName = targetply.displayName
                msg = lang.GetMessage('srClear',self.Plugin,callingply.UserIDString)
                msg = msg.replace('{PLAYERNAME}',playerName)
                rust.SendChatMessage(callingply, msg, None, '0')
            else:
                print 'Cannot find player'



    def godPlayer(self,args):
        if not plugins.Exists('Godmode'):
            print 'Godmode is not installed'
        else:
            callingply = self.getCallingPlayer(args)
            GodMode = plugins.Find('Godmode')
            targetply = None
            for player in BasePlayer.activePlayerList:
                if player.UserIDString in args.ArgsStr:
                    targetply = player
                    
            for player in BasePlayer.sleepingPlayerList:
                if player.UserIDString in args.ArgsStr:
                    targetply = player
            if targetply:
                playerName = targetply.displayName
                worked = GodMode.Call("EnableGodmode",targetply)
                msg = lang.GetMessage('godPlayer',self.Plugin,callingply.UserIDString)
                msg = msg.replace('{PLAYERNAME}',playerName)
                rust.SendChatMessage(callingply, msg, None, '0')
            else:
                print 'Cannot find player'

                
    def ungodPlayer(self,args):
        if not plugins.Exists('Godmode'):
            print 'Godmode is not installed'
        else:
            callingply = self.getCallingPlayer(args)
            GodMode = plugins.Find('Godmode')
            targetply = None
            for player in BasePlayer.activePlayerList:
                if player.UserIDString in args.ArgsStr:
                    targetply = player
            for player in BasePlayer.sleepingPlayerList:
                if player.UserIDString in args.ArgsStr:
                    targetply = player
            if targetply:
                playerName = targetply.displayName
                worked = GodMode.Call("DisableGodmode",targetply)
                msg = lang.GetMessage('ungodPlayer',self.Plugin,callingply.UserIDString)
                msg = msg.replace('{PLAYERNAME}',playerName)
                rust.SendChatMessage(callingply, msg, None, '0')
            else:
                print 'Cannot find player'
        
    def bringPlayer(self, args):
        callingply = self.getCallingPlayer(args)
        callingplyID = self.getCallingPlayerID(args)
        callingplyName = self.getCallingPlayerName(args)
        try:
            targetplyID = args.ArgsStr.split(' ')[0]
        except:
            targetplyID = '000000000'
            print '[AdminMenu] Failed to tp - Invalid player ID'
        if permission.UserHasPermission(callingplyID, 'adminmenu.allow.bring')or permission.UserHasPermission(callingplyID, 'adminmenu.allow.all'):
            try:
                rust.RunServerCommand('teleport %s %s' % (targetplyID,callingplyID))
                if self.Config['SETTINGS']['Broadcast']:
                    msg = lang.GetMessage('bringPlayer',self.Plugin,callingplyID)
                    msg = msg.replace('{ADMINNAME}',callingplyName)
                    msg = msg.replace('{PLAYERNAME}',self.getPlayerName(targetplyID))
                    rust.BroadcastChat(msg, None, '0')
            except:
                print '[AdminMenu] Failed to tp %s' % targetplyID
                
    def gotoPlayer(self, args):
        callingply = self.getCallingPlayer(args)
        callingplyID = self.getCallingPlayerID(args)
        callingplyName = self.getCallingPlayerName(args)
        try:
            targetplyID = args.ArgsStr.split(' ')[0]
        except:
            targetplyID = '000000000'
            print ' [AdminMenu]Failed to tp - Invalid player ID'
        if permission.UserHasPermission(callingplyID, 'adminmenu.allow.goto') or permission.UserHasPermission(callingplyID, 'adminmenu.allow.all'):
            try:
                rust.RunServerCommand('teleport %s %s' % (callingplyID,targetplyID))
                if self.Config['SETTINGS']['Broadcast']:
                    msg = lang.GetMessage('gotoPlayer',self.Plugin,callingplyID)
                    msg = msg.replace('{ADMINNAME}',callingplyName)
                    msg = msg.replace('{PLAYERNAME}',self.getPlayerName(targetplyID))
                    rust.BroadcastChat(msg, None, '0')
            except:
                print '[AdminMenu] Failed to tp %s' % targetplyID

    def kickPlayer(self, args):
        callingply = self.getCallingPlayer(args)
        callingplyID = self.getCallingPlayerID(args)
        callingplyName = self.getCallingPlayerName(args)
        for player in BasePlayer.activePlayerList:
            if player.UserIDString in args.ArgsStr:
                targetplyID = player.UserIDString
                targetplyName = player.displayName
        for player in BasePlayer.sleepingPlayerList:
            if player.UserIDString in args.ArgsStr:
                targetplyID = player.UserIDString
        try:
            reason = args.ArgsStr.split('"')[1]
        except KeyError:
            reason = None
        if permission.UserHasPermission(callingplyID, 'adminmenu.allow.kick')or permission.UserHasPermission(callingplyID, 'adminmenu.allow.all'):
            try:
                if not reason:
                    rust.RunServerCommand('kick %s' % targetplyID)
                else:
                    rust.RunServerCommand('kick %s %s' % (targetplyID, reason))
                if self.Config['SETTINGS']['Broadcast']:
                    msg = lang.GetMessage('kickPlayer',self.Plugin,callingplyID)
                    msg = msg.replace('{ADMINNAME}',callingplyName)
                    msg = msg.replace('{PLAYERNAME}',targetplyName)
                    rust.BroadcastChat(msg, None, '0')
                self.destroyMenu(args)
            except:
                print '[AdminMenu]Failed to kick %s' % targetplyName

    def banPlayer(self, args):
        callingply = self.getCallingPlayer(args)
        callingplyID = self.getCallingPlayerID(args)
        callingplyName = self.getCallingPlayerName(args)
        for player in BasePlayer.activePlayerList:
            if player.UserIDString in args.ArgsStr:
                targetplyID = player.UserIDString
                targetplyName = player.displayName
        for player in BasePlayer.sleepingPlayerList:
            if player.UserIDString in args.ArgsStr:
                targetplyID = player.UserIDString
        try:
            reason = args.ArgsStr.split('"')[1]
        except KeyError:
            reason = None
        if permission.UserHasPermission(callingplyID, 'adminmenu.allow.ban')or permission.UserHasPermission(callingplyID, 'adminmenu.allow.all'):
            try:
                if not reason:
                    rust.RunServerCommand('ban %s' % targetplyID)
                else:
                    rust.RunServerCommand('ban %s %s' % (targetplyID, reason))
                if self.Config['SETTINGS']['Broadcast']:
                    msg = lang.GetMessage('banPlayer',self.Plugin,callingplyID)
                    msg = msg.replace('{ADMINNAME}',callingplyName)
                    msg = msg.replace('{PLAYERNAME}',targetplyName)
                    rust.BroadcastChat(msg, None, '0')
                self.destroyMenu(args)
            except:
                print '[AdminMenu]Failed to ban %s' % targetplyName
    def findPlayerByName(self, username):
        foundplayers = 0
        for player in BasePlayer.activePlayerList:
            if username in player.displayName:
                targetply = player
                foundplayers += 1
        for player in BasePlayer.sleepingPlayerList:
            if username in player.displayName:
                targetply = player
                foundplayers += 1
        if foundplayers > 1:
            return '[AdminMenu] Found more than 1 player with that name'
        elif foundplayers == 0:
            return '[AdminMenu] Found no players with that name'
        else:
            return targetply
        
    def AddQuickAccess(self,player,cmd,args):
        if permission.UserHasPermission(player.UserIDString,'adminmenu.allow.view') or player.IsAdmin():
            if 'StaffQuickAccess' not in self.dataTable.keys():
                self.dataTable['StaffQuickAccess'] = dict()
            if not player.UserIDString in self.dataTable['StaffQuickAccess'].keys():
                self.dataTable['StaffQuickAccess'][player.UserIDString] = dict()
            commandlist = dict()
            for plug in self.Config['COMMANDS'].keys():
                for cmdcat in self.Config['COMMANDS'][plug].keys():
                    for cmd in self.Config['COMMANDS'][plug][cmdcat].keys():
                        commandlist[cmd] = self.Config['COMMANDS'][plug][cmdcat][cmd]
            targetply = player
            cmd = None
            cmdname = ''
            cmdNick = ''
            for arg in args:
                if '@' in arg:
                    targetply = self.findPlayerByName(arg.replace('@',''))
                    if type(targetply) == str:
                        rust.SendChatMessage(player, targetply, None, '0')
                        return
                elif arg in commandlist.keys():
                    cmdname = arg
                    cmd = list(commandlist[arg])
                else:
                    cmdNick = arg
            if not cmd or not cmdname:
                rust.SendChatMessage(player, '[Adminmenu] Invalid command', None, '0')
                return
            if not cmdNick:
                cmdNick = cmdname
            hasPerm = False
            if cmd[5] == ' ':
                if player.IsAdmin():
                    hasPerm = True
            else:
                if permission.UserHasPermission(player.UserIDString,cmd[4]):
                    hasPerm = True
            if hasPerm:
                playerpos = targetply.GetEstimatedWorldPosition()
                temp = []
                for item in str(playerpos).split(' '):
                    item = item.replace('(' ,'')
                    item = item.replace(')' ,'')
                    item = item.replace(',' ,'')
                    temp.append(float(item))
                playerpos = temp
                cmd[1]= cmd[1].replace('{PLAYERID}',targetply.UserIDString)
                cmd[1] = cmd[1].replace('{PLAYERNAME}',targetply.displayName)
                cmd[1] = cmd[1].replace('{ADMINID}',player.UserIDString)
                cmd[1] = cmd[1].replace('{ADMINNAME}',player.displayName)
                cmd[1] = cmd[1].replace('{PLAYERPOS}','%f %f %f' % tuple(playerpos))
                
                self.dataTable['StaffQuickAccess'][player.UserIDString][cmdNick] = dict()
                self.dataTable['StaffQuickAccess'][player.UserIDString][cmdNick]['index'] = cmd[0]
                self.dataTable['StaffQuickAccess'][player.UserIDString][cmdNick]['command'] = cmd[1]
                self.dataTable['StaffQuickAccess'][player.UserIDString][cmdNick]['btncolour'] = cmd[2]
                self.dataTable['StaffQuickAccess'][player.UserIDString][cmdNick]['textcolour'] = cmd[3]
                self.dataTable['StaffQuickAccess'][player.UserIDString][cmdNick]['perm'] = cmd[4]
                self.renumberQuickAccess(player.UserIDString)
                data.SaveData('AdminMenu')
            else:
                msg = '[AdminMenu] You do not have permission to use that command'
                rust.SendChatMessage(player, msg, None, '0')
        else:
            msg = '[AdminMenu] You do not have permission to use that command'
            rust.SendChatMessage(player, msg, None, '0')
    def renumberQuickAccess(self,playerID):
        if playerID not in self.dataTable['StaffQuickAccess'].keys():
            return
        else:
            i = 1
            for cmd in self.dataTable['StaffQuickAccess'][playerID].keys():
                newcmd = self.dataTable['StaffQuickAccess'][playerID][cmd]['index']
                newcmd = i
                i += 1
                self.dataTable['StaffQuickAccess'][playerID][cmd]['index'] = newcmd
            data.SaveData('AdminMenu')

    def RemoveQuickAccess(self,player,cmd,args):
        if 'StaffQuickAccess' not in self.dataTable.keys():
            self.dataTable['StaffQuickAccess'] = dict()
            return
        if player.UserIDString not in self.dataTable['StaffQuickAccess'].keys():
            return
        else:
            if args[0] not in self.dataTable['StaffQuickAccess'][player.UserIDString].keys():
                return
            else:
                cmd = args[0]
                del self.dataTable['StaffQuickAccess'][player.UserIDString][cmd]
                msg = '[AdminMenu]Deleted Quick Button: %s' % cmd
                rust.SendChatMessage(player, msg, None, '0')
                self.renumberQuickAccess(player.UserIDString)
            
    def getPermissionsPages(self,pagename,getNum = False):
        allpermissions = list(permission.GetPermissions())
        permPages = dict()
        done = False
        x = 0
        while not done:
            currpage = 'page%d'%x
            currpagelist = []
            for i in range(15):
                try:
                    currpagelist.append(allpermissions[15*x+i])
                except IndexError:
                    done = True
                    permPages[currpage] = currpagelist
            permPages[currpage] = currpagelist
            x += 1
        if getNum:
            return len(permPages.keys())
        else:
            return permPages[pagename]
    
    def groupPermissionsMenu(self,args):
        callingply = self.getCallingPlayer(args)
        if permission.UserHasPermission(callingply.UserIDString,'adminmenu.allow.permissions'):
            self.destroyMenu(args)
            elements = Cui.CuiElementContainer()
            permpage = 'page0'
            page = 0
            group = 'default'
            for arg in args.ArgsStr.split(' '):
                if 'page' in arg:
                    permpage = arg
                    page = int(permpage.replace('page',''))
                for grp in permission.GetGroups():
                    if arg == grp:
                        group = arg
            box_panel = Cui.CuiPanel()
            box_panel.Image.Color = self.TINTCOLOURS['grey_tint']
            box_panel.Image.FadeIn = self.fadeInTime
            box_panel.RectTransform.AnchorMin = '0.1 0.1'
            box_panel.RectTransform.AnchorMax = '0.7 0.92'
            box_panel.CursorEnabled = True
            elements.Add(box_panel)
            permissionsPage = self.getPermissionsPages(permpage)
            permissionsPageNum = self.getPermissionsPages(permpage,True)

            txt = 'Group: %s Permissions' % (group)
            title_txtcomp = Cui.CuiTextComponent(Text = txt,
                        Color = self.TEXTCOLOURS['yellow_text'],
                        FadeIn = self.fadeInTime,
                        Align = TextAnchor.MiddleCenter,
                        FontSize = self.fontSize+8)
            title_rectcomp = Cui.CuiRectTransformComponent(AnchorMin = "0.1 0.75", AnchorMax = "0.5 0.9")
            title_guielem = Cui.CuiElement(FadeOut = self.fadeOutTime)
            title_guielem.Components.Add(title_txtcomp)
            title_guielem.Components.Add(title_rectcomp)
            elements.Add(title_guielem)
            if page < permissionsPageNum -1:
                nextpage = page+1
            else:
                nextpage = 0
            if page > 0:
                prevpage = page-1
            else:
                prevpage = permissionsPageNum -1
            next_btn = Cui.CuiButton()
            next_btn.Button.Command = 'adminmenu.groups page%d %s' % (nextpage,group)
            next_btn.Button.Color = self.TINTCOLOURS['black_tint'] 
            next_btn.Text.Text = '>>>'
            next_btn.Text.Color = self.TEXTCOLOURS['white_text']
            next_btn.Text.FontSize = self.fontSize
            next_btn.Text.Align = TextAnchor.MiddleCenter
            next_btn.RectTransform.AnchorMin =  '%f %f'%(0.33, 0.11)
            next_btn.RectTransform.AnchorMax = '%f %f'%(0.38, 0.16)
            elements.Add(next_btn)

            prev_btn = Cui.CuiButton()
            prev_btn.Button.Command = 'adminmenu.groups page%d %s' % (prevpage,group)
            prev_btn.Button.Color = self.TINTCOLOURS['black_tint']
            prev_btn.Text.Text = '<<<'
            prev_btn.Text.Color = self.TEXTCOLOURS['white_text']
            prev_btn.Text.FontSize = self.fontSize
            prev_btn.Text.Align = TextAnchor.MiddleCenter
            prev_btn.RectTransform.AnchorMin =  '%f %f'%(0.22, 0.11)
            prev_btn.RectTransform.AnchorMax = '%f %f'%(0.27, 0.16)
            elements.Add(prev_btn)
            grp_index = 0
            for grp in permission.GetGroups():
                grp_btn = Cui.CuiButton()
                grp_btn.Button.Command = 'adminmenu.groups page%d %s' % (page,grp)
                grp_btn.Button.Color = self.TINTCOLOURS['blue_tint']
                grp_btn.Text.Text = grp
                grp_btn.Text.Color = self.TEXTCOLOURS['white_text']
                grp_btn.Text.FontSize = self.fontSize
                grp_btn.Text.Align = TextAnchor.MiddleCenter
                grp_btn.RectTransform.AnchorMin =  '%f %f'%(0.1, 0.875-(0.025*grp_index))
                grp_btn.RectTransform.AnchorMax = '%f %f'%(0.15, 0.9-(0.025*grp_index))
                elements.Add(grp_btn)
                grp_index += 1
            perm_index = 0
            for perm in permissionsPage:
                NoPerm = True
                if permission.GroupHasPermission(group,perm):
                    NoPerm = False
                    txtcolour = self.TEXTCOLOURS['green_text']
                else:
                    txtcolour = self.TEXTCOLOURS['red_text']
                perm_lbl = Cui.CuiElement(FadeOut = self.fadeOutTime)
                perm_lbl_Txt = Cui.CuiTextComponent(Text = perm,
                        Color = txtcolour,
                        FadeIn = self.fadeInTime,
                        Align = TextAnchor.MiddleLeft,
                        FontSize = self.fontSize)
                perm_lbl_Rect = Cui.CuiRectTransformComponent(AnchorMin = '%f %f' % (0.25,0.7-(0.025*perm_index)),
                                                                AnchorMax = '%f %f' % (0.45,0.725-((0.025*perm_index))))
                perm_lbl.Components.Add(perm_lbl_Txt)
                perm_lbl.Components.Add(perm_lbl_Rect)
                elements.Add(perm_lbl)
                perm_btn = Cui.CuiButton()
                if NoPerm:
                    perm_btn.Button.Command = 'grant group %s %s'%(group,perm)
                    perm_btn.Button.Color = self.TINTCOLOURS['green_tint'] 
                    perm_btn.Text.Text = 'Grant'
                else:
                    perm_btn.Button.Command = 'revoke group %s %s' % (group,perm)
                    perm_btn.Button.Color = self.TINTCOLOURS['red_tint'] 
                    perm_btn.Text.Text = 'Revoke'
                perm_btn.Text.Color = self.TEXTCOLOURS['white_text']
                perm_btn.Text.FontSize = self.fontSize
                perm_btn.Text.Align = TextAnchor.MiddleCenter
                perm_btn.RectTransform.AnchorMin =  '%f %f' % (0.5,0.7-(0.025*perm_index))
                perm_btn.RectTransform.AnchorMax = '%f %f' % (0.55,0.725-((0.025*perm_index)))
                elements.Add(perm_btn)
                perm_index += 1
            close_btn = Cui.CuiButton()
            close_btn.Button.Command = 'adminmenu.destroy'
            close_btn.Button.Color = self.TINTCOLOURS['red_tint'] 
            close_btn.Text.Text = 'Close'
            close_btn.Text.Color = self.TEXTCOLOURS['white_text']
            close_btn.Text.FontSize = self.fontSize
            close_btn.Text.Align = TextAnchor.MiddleCenter
            close_btn.RectTransform.AnchorMin =  '0.5 0.85'
            close_btn.RectTransform.AnchorMax = '0.55 0.9'
            elements.Add(close_btn)
            
            self.PlayerUIs[callingply.UserIDString] = elements
            self.changeParent(callingply)
            Cui.CuiHelper.AddUi(callingply,self.PlayerUIs[callingply.UserIDString])
  
            

    def permissionsMenu(self,args):
        callingply = self.getCallingPlayer(args)
        if permission.UserHasPermission(callingply.UserIDString,'adminmenu.allow.view') or callingply.IsAdmin():
            self.destroyMenu(args)
            elements = Cui.CuiElementContainer()
            targetPly = None
            for player in BasePlayer.activePlayerList:
                if player.UserIDString in args.ArgsStr:
                    targetPly = player
            for player in BasePlayer.sleepingPlayerList:
                if player.UserIDString in args.ArgsStr:
                    targetPly = player
            if not targetPly:
                return
            permpage = 'page0'
            page = 0
            for arg in args.ArgsStr.split(' '):
                if 'page' in arg:
                    permpage = arg
                    page = int(permpage.replace('page',''))
            box_panel = Cui.CuiPanel()
            box_panel.Image.Color = self.TINTCOLOURS['grey_tint']
            box_panel.Image.FadeIn = self.fadeInTime
            box_panel.RectTransform.AnchorMin = '0.1 0.25'
            box_panel.RectTransform.AnchorMax = '0.6 0.92'
            box_panel.CursorEnabled = True
            elements.Add(box_panel)
            
            permissionsPage = self.getPermissionsPages(permpage)
            permissionsPageNum = self.getPermissionsPages(permpage,True)

            if page < permissionsPageNum -1:
                nextpage = page+1
            else:
                nextpage = 0
            if page > 0:
                prevpage = page-1
            else:
                prevpage = permissionsPageNum -1
            next_btn = Cui.CuiButton()
            next_btn.Button.Command = 'adminmenu.permissions page%d %s' % (nextpage,targetPly.UserIDString)
            next_btn.Button.Color = self.TINTCOLOURS['black_tint'] 
            next_btn.Text.Text = '>>>'
            next_btn.Text.Color = self.TEXTCOLOURS['white_text']
            next_btn.Text.FontSize = self.fontSize
            next_btn.Text.Align = TextAnchor.MiddleCenter
            next_btn.RectTransform.AnchorMin =  '%f %f'%(0.33, 0.26)
            next_btn.RectTransform.AnchorMax = '%f %f'%(0.38, 0.31)
            elements.Add(next_btn)

            prev_btn = Cui.CuiButton()
            prev_btn.Button.Command = 'adminmenu.permissions page%d %s' % (prevpage,targetPly.UserIDString)
            prev_btn.Button.Color = self.TINTCOLOURS['black_tint']
            prev_btn.Text.Text = '<<<'
            prev_btn.Text.Color = self.TEXTCOLOURS['white_text']
            prev_btn.Text.FontSize = self.fontSize
            prev_btn.Text.Align = TextAnchor.MiddleCenter
            prev_btn.RectTransform.AnchorMin =  '%f %f'%(0.22, 0.26)
            prev_btn.RectTransform.AnchorMax = '%f %f'%(0.27, 0.31)
            elements.Add(prev_btn)
 
            userGroups = list(permission.GetUserGroups(targetPly.UserIDString))
            txt = '%s Permissions. Their Groups: %s' % (targetPly.displayName,userGroups)
            title_txtcomp = Cui.CuiTextComponent(Text = txt,
                        Color = self.TEXTCOLOURS['yellow_text'],
                        FadeIn = self.fadeInTime,
                        Align = TextAnchor.MiddleCenter,
                        FontSize = self.fontSize+8)
            title_rectcomp = Cui.CuiRectTransformComponent(AnchorMin = "0.1 0.75", AnchorMax = "0.5 0.9")
            title_guielem = Cui.CuiElement(FadeOut = self.fadeOutTime)
            title_guielem.Components.Add(title_txtcomp)
            title_guielem.Components.Add(title_rectcomp)
            elements.Add(title_guielem)
            permInherited = False
            perm_index = 0
            for perm in permissionsPage:
                NoPerm = True
                if permission.UserHasPermission(targetPly.UserIDString,perm):
                    txtcolour = self.TEXTCOLOURS['orange_text']
                    permInherited = True
                    NoPerm = False
                    nogroupHas = True
                    for group in userGroups:
                        if permission.GroupHasPermission(group, perm):
                            nogroupHas = False
                    if nogroupHas:
                        txtcolour = self.TEXTCOLOURS['white_text']
                        permInherited = False
                else:
                    txtcolour = self.TEXTCOLOURS['red_text']
                perm_lbl = Cui.CuiElement(FadeOut = self.fadeOutTime)
                if NoPerm:
                    permtxt = '(No Perm)     '+perm
                else:
                    if permInherited:
                        permtxt = '(Inherited)    '+perm
                    else:
                        permtxt = '(Granted)      '+perm
                perm_lbl_Txt = Cui.CuiTextComponent(Text = permtxt,
                        Color = txtcolour,
                        FadeIn = self.fadeInTime,
                        Align = TextAnchor.MiddleLeft,
                        FontSize = self.fontSize)
                perm_lbl_Rect = Cui.CuiRectTransformComponent(AnchorMin = '%f %f' % (0.2,0.7-(0.025*perm_index)),
                                                                AnchorMax = '%f %f' % (0.4,0.725-((0.025*perm_index))))
                perm_lbl.Components.Add(perm_lbl_Txt)
                perm_lbl.Components.Add(perm_lbl_Rect)
                elements.Add(perm_lbl)

                if permission.UserHasPermission(callingply.UserIDString,'adminmenu.allow.permissions'):
                    perm_btn = Cui.CuiButton()
                    if NoPerm or permInherited:
                        perm_btn.Button.Command = 'grant user %s %s'%(targetPly.UserIDString,perm)
                        perm_btn.Button.Color = self.TINTCOLOURS['green_tint'] 
                        perm_btn.Text.Text = 'Grant'
                    elif not permInherited and not NoPerm:
                        perm_btn.Button.Command = 'revoke user %s %s' % (targetPly.UserIDString,perm)
                        perm_btn.Button.Color = self.TINTCOLOURS['red_tint'] 
                        perm_btn.Text.Text = 'Revoke'
                    perm_btn.Text.Color = self.TEXTCOLOURS['white_text']
                    perm_btn.Text.FontSize = self.fontSize
                    perm_btn.Text.Align = TextAnchor.MiddleCenter
                    perm_btn.RectTransform.AnchorMin =  '%f %f' % (0.45,0.7-(0.025*perm_index))
                    perm_btn.RectTransform.AnchorMax = '%f %f' % (0.5,0.725-((0.025*perm_index)))
                    elements.Add(perm_btn)
                perm_index += 1
                
            close_btn = Cui.CuiButton()
            close_btn.Button.Command = 'adminmenu.destroy'
            close_btn.Button.Color = self.TINTCOLOURS['red_tint'] 
            close_btn.Text.Text = 'Close'
            close_btn.Text.Color = self.TEXTCOLOURS['white_text']
            close_btn.Text.FontSize = self.fontSize
            close_btn.Text.Align = TextAnchor.MiddleCenter
            close_btn.RectTransform.AnchorMin =  '0.5 0.85'
            close_btn.RectTransform.AnchorMax = '0.55 0.9'
            elements.Add(close_btn)
            
            self.PlayerUIs[callingply.UserIDString] = elements
            self.changeParent(callingply)
            Cui.CuiHelper.AddUi(callingply,self.PlayerUIs[callingply.UserIDString])
        else:
            #Then user has no permission to view.. how did they even get here?
            return

    def weather(self,args):
        callingply = self.getCallingPlayer(args)
        if not permission.UserHasPermission(callingply.UserIDString,'weathercontroller.canuseweather'):
            return
        if plugins.Exists('WeatherController'):
            WC = plugins.Find('WeatherController')
            for arg in args.ArgsStr.split(' '):
                if arg == 'clouds':
                    self.weathertoggle[0] = not self.weathertoggle[0]
                    WC.weather(callingply,"clouds",int(self.weathertoggle[0]))
                if arg == 'rain':
                    self.weathertoggle[1] = not self.weathertoggle[1]
                    WC.weather(callingply,'rain',int(self.weathertoggle[1]))
                if arg == 'wind':
                    self.weathertoggle[2] = not self.weathertoggle[2]
                    WC.weather(callingply,'wind',int(self.weathertoggle[2]))
                if arg == 'fog':
                    self.weathertoggle[3] = not self.weathertoggle[3]
                    WC.weather(callingply,'fog',int(self.weathertoggle[3]))
                if arg == 'mild':
                    self.weathertoggle[4] = not self.weathertoggle[4]
                    WC.mild(callingply,int(self.weathertoggle[4]))
                if arg == 'average':
                    self.weathertoggle[5] = not self.weathertoggle[5]
                    WC.average(callingply,int(self.weathertoggle[5]))
                if arg == 'heavy':
                    self.weathertoggle[6] = not self.weathertoggle[6]
                    WC.heavy(callingply,int(self.weathertoggle[6]))
                if arg == 'max':
                    self.weathertoggle[7] = not self.weathertoggle[7]
                    WC.max(callingply,int(self.weathertoggle[7]))
                if arg == 'auto':
                    self.weathertoggle = [False,False,False,False,False,False,False,False]
                    WC.weather(callingply,'auto',-1)
        else:
            print '[AdminMenu] WeatherController is not installed'
            ##Weathertoggle = [clouds, rain, wind, fog, mild, average, heavy, max]
              
    def giveMenu(self,args):
        callingply = self.getCallingPlayer(args)
        if not callingply.IsAdmin():
            return
        self.destroyMenu(args)
        if plugins.Exists('Give'):
            elements = Cui.CuiElementContainer()
            targetPly = None
            for player in BasePlayer.activePlayerList:
                if player.UserIDString in args.ArgsStr:
                    targetPly = player
            for player in BasePlayer.sleepingPlayerList:
                if player.UserIDString in args.ArgsStr:
                    targetPly = player
            if not targetPly:
                return
            box_panel = Cui.CuiPanel()
            box_panel.Image.Color = self.TINTCOLOURS['grey_tint']
            box_panel.Image.FadeIn = self.fadeInTime
            box_panel.RectTransform.AnchorMin = '0.1 0.1'
            box_panel.RectTransform.AnchorMax = '0.9 0.92'
            box_panel.CursorEnabled = True
            elements.Add(box_panel)
            txt = 'Give %s what?' % targetPly.displayName
            title_txtcomp = Cui.CuiTextComponent(Text = txt,
                        Color = self.TEXTCOLOURS['yellow_text'],
                        FadeIn = self.fadeInTime,
                        Align = TextAnchor.MiddleCenter,
                        FontSize = self.fontSize)
            title_rectcomp = Cui.CuiRectTransformComponent(AnchorMin = "0.15 0.92", AnchorMax = "0.85 0.97")
            title_guielem = Cui.CuiElement(FadeOut = self.fadeOutTime)
            title_guielem.Components.Add(title_txtcomp)
            title_guielem.Components.Add(title_rectcomp)
            elements.Add(title_guielem)
            
            close_btn = Cui.CuiButton()
            close_btn.Button.Command = 'adminmenu.destroy'
            close_btn.Button.Color = self.TINTCOLOURS['red_tint'] 
            close_btn.Text.Text = 'Close'
            close_btn.Text.Color = self.TEXTCOLOURS['white_text']
            close_btn.Text.FontSize = self.fontSize
            close_btn.Text.Align = TextAnchor.MiddleCenter
            close_btn.RectTransform.AnchorMin =  '0.75 0.1'
            close_btn.RectTransform.AnchorMax = '0.85 0.15'
            elements.Add(close_btn)
            
            singleitems = list(self.Config['SETTINGS']['Single Items'])
            manyitems = list(self.Config['SETTINGS']['Many Items'])
            for category in self.ItemList.keys():
                if category in args.ArgsStr:
                    i = 0
                    flip = False
                    for item in self.ItemList[category].keys():
                        flip = not flip
                        itemname = self.ItemNames[self.ItemList[category][item]]
                        item_txtcomp = Cui.CuiTextComponent(Text = itemname,
                            Color = self.TEXTCOLOURS['white_text'],
                            FadeIn = self.fadeInTime,
                            Align = TextAnchor.MiddleCenter,
                            FontSize = self.fontSize)
                        item_rectcomp = Cui.CuiRectTransformComponent(AnchorMin = "0.15 %f" % (0.9-(i*0.018)), AnchorMax = "0.3 %f" % (0.918-(i*0.018)))
                        item_guielem = Cui.CuiElement(FadeOut = self.fadeOutTime)
                        item_txtguielem = Cui.CuiElement(FadeOut = self.fadeOutTime)
                        item_bgelem = Cui.CuiImageComponent(FadeIn = self.fadeInTime)
                        if flip:
                            item_bgelem.Color = self.TINTCOLOURS['black_tint']
                        else:
                            item_bgelem.Color = self.TINTCOLOURS['grey_tint']
                        item_guielem.Components.Add(item_bgelem)
                        item_txtguielem.Components.Add(item_txtcomp)
                        item_txtguielem.Components.Add(item_rectcomp)
                        item_guielem.Components.Add(item_rectcomp)
                        elements.Add(item_guielem)
                        elements.Add(item_txtguielem)
                        if category in singleitems:
                            one_btn = Cui.CuiButton()
                            one_btn.Button.Command = 'inv.giveplayer %s %s %d' % (targetPly.UserIDString,self.ItemList[category][item],1)
                            if flip:
                                one_btn.Button.Color = self.TINTCOLOURS['black_tint']
                            else:
                                one_btn.Button.Color = self.TINTCOLOURS['grey_tint']
                            one_btn.Text.Text = '1'
                            one_btn.Text.Color = self.TEXTCOLOURS['white_text']
                            one_btn.Text.FontSize = self.fontSize
                            one_btn.Text.Align = TextAnchor.MiddleCenter
                            one_btn.RectTransform.AnchorMin =  '0.35 %f' % (0.9-(i*0.018))
                            one_btn.RectTransform.AnchorMax = '0.41 %f' % (0.918-(i*0.018))
                            elements.Add(one_btn)
                        elif category in manyitems:
                            k = 0
                            for j in self.Config['SETTINGS']['ManyItemButtons']:
                                one_btn = Cui.CuiButton()
                                one_btn.Button.Command = 'inv.giveplayer %s %s %d' % (targetPly.UserIDString,self.ItemList[category][item],j)
                                if flip:
                                    one_btn.Button.Color = self.TINTCOLOURS['black_tint']
                                else:
                                    one_btn.Button.Color = self.TINTCOLOURS['grey_tint']
                                one_btn.Text.Text = str(j)
                                one_btn.Text.Color = self.TEXTCOLOURS['white_text']
                                one_btn.Text.FontSize = self.fontSize
                                one_btn.Text.Align = TextAnchor.MiddleCenter
                                one_btn.RectTransform.AnchorMin =  '%f %f' %((0.35+(k*0.06)),(0.9-(i*0.018)))
                                one_btn.RectTransform.AnchorMax = '%f %f' % ((0.41+(k*0.06)),(0.918-(i*0.018)))
                                elements.Add(one_btn)
                                k += 1
                        else:
                            k = 0
                            for j in self.Config['SETTINGS']['FewItemButtons']:
                                one_btn = Cui.CuiButton()
                                one_btn.Button.Command = 'inv.giveplayer %s %s %d' % (targetPly.UserIDString,self.ItemList[category][item],j)
                                if flip:
                                    one_btn.Button.Color = self.TINTCOLOURS['black_tint']
                                else:
                                    one_btn.Button.Color = self.TINTCOLOURS['grey_tint']
                                one_btn.Text.Text = str(j)
                                one_btn.Text.Color = self.TEXTCOLOURS['white_text']
                                one_btn.Text.FontSize = self.fontSize
                                one_btn.Text.Align = TextAnchor.MiddleCenter
                                one_btn.RectTransform.AnchorMin =  '%f %f' %((0.35+(k*0.06)),(0.9-(i*0.018)))
                                one_btn.RectTransform.AnchorMax = '%f %f' % ((0.41+(k*0.06)),(0.918-(i*0.018)))
                                elements.Add(one_btn)
                                k += 1
                        i +=1
        
            self.PlayerUIs[callingply.UserIDString] = elements
            self.changeParent(callingply)
            Cui.CuiHelper.AddUi(callingply,self.PlayerUIs[callingply.UserIDString])
            
        else:
            print'Give not installed'

    def disableBroadcast(self,player,cmd,args):
        if permission.UserHasPermission(player.UserIDString,'adminmenu.allow.config'):
            self.Config['SETTINGS']['Broadcast'] = not self.Config['SETTINGS']['Broadcast']
            if self.Config['SETTINGS']['Broadcast']:
                msg = 'Enabled broadcasts from AdminMenu'
            else:
                msg = 'Disabled broadcasts from AdminMenu'
            self.SaveConfig()
        else:
            msg = 'You need adminmenu.allow.config permission to use this command'
        rust.SendChatMessage(player, msg, None, '0')

    def changeParent(self, callingply):
        for guielem in self.PlayerUIs[callingply.UserIDString]:
            if 'Button' in str(guielem.Components):
                guielem.Parent = "Overlay"

                    
    def destroyMenu(self,args):
        callingply = self.getCallingPlayer(args)
        callingplyID = self.getCallingPlayerID(args)
        elementNames = []
        if callingply:
            if callingplyID in self.PlayerUIs.keys() and self.PlayerUIs[callingplyID]:
                for guielem in self.PlayerUIs[callingplyID]:
                    elementNames.append(guielem.Name)
        if elementNames:
            for elem in elementNames:
                Cui.CuiHelper.DestroyUi(callingply, elem)
        self.PlayerUIs[callingplyID] = None

    def destroyAllPlayerUI(self):
        elementNames = dict()
        if self.PlayerUIs.keys():
            for player in self.PlayerUIs.keys():
                elementNames[player] = []
                if self.PlayerUIs[player]:
                    for guielem in self.PlayerUIs[player]:
                        elementNames[player].append(guielem.Name)
            for player in BasePlayer.activePlayerList:
                if player.UserIDString in self.PlayerUIs.keys():
                    for elem in elementNames[player.UserIDString]:
                        try:
                            Cui.CuiHelper.DestroyUi(player, elem)
                        except:
                            pass
        self.PlayerUIs = dict()

    
            
            

        

        
        
        
        
        

        
        
        
        
        
        
        
        
        


    
        
        
