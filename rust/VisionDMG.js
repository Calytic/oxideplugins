/**
 * VisionDMG
 * 
 * OxideMod Plugin for Rust
 * 
 * @author  VisionMise
 * @version  0.1.2
 * @alpha
 */


/**
 * controller
 *
 * VisionDMG controller
 * 
 * @param  {String} prefix       The title of the Oxide Plugin
 * @param  {Object} configObject The OxideMod Config Object
 * @return {Object} Self
 */
var controller             = function(prefix, configObject) {

    /**
     * Private storage
     */
    this.config     = {};
    this.rules      = {};
    this.plrRules   = {};
    
    this.prefix     = '';
    this.mode       = '';
    this.pvp        = '';

    this.debug      = false;
    this.loaded     = false;


    this.on 		= true;


    /**
     * Initialize Object
     * @param  {String} prefix       The title of the Oxide Plugin
     * @param  {Object} configObject The OxideMod Config Object
     * @return {Object} Self
     */
    this.init               = function(name, cfg) {

        //Set Config and Prefix
        this.config     = cfg;
        this.prefix     = name;

        //Send Header to Console
        this.send("Started");

        //Drink coffee (Set mode and rules)
        this.mode       = this.config['CurrentEntMode'];
        this.pvp        = this.config['CurrentPVPMode'];

        this.rules      = this.config['EntityModes'][this.mode];
        this.plrRules   = this.config['PVPModes'][this.pvp];

        this.send("Current Entity Mode: " + this.mode);
        this.send("Current Player Mode: " + this.pvp);

        //We're ready now.
        this.loaded     = true;
    };


    this.hook_BuildingBlock = function(attacker, block) {

        //If building damage not on
        if (!this.rules['players_can_damage_all_buildings']) {

            //but players can damage their own blocks
            if (this.rules['players_can_damage_own_buildings']) {

                //and this is the players block
                if (attacker.CanBuild()) {

                    //then allow the damage
                    return this.exit(2, "Allowing Damage for Rule 'players_can_damage_own_buildings' = yes");

                //and this is not the players block
                } else {

                    //then remove the damage, it is now theirs to damage
                    this.exit(-11, "Removing Damage for rule 'players_cab_damage_all_buildings' = no")
                    return true;
                }

            //and players cannot damage their own blocks
            } else {

                //then negate the damage
                this.exit(-3, "Removing Damage for Rule 'players_can_damage_all_buildings' = no");
                return true;
            }

        //but, If building damage is allowed
        } else {

            var lock        = block.GetSlot(0);
            var locked      = (!lock) ? -1 : lock.IsLocked();

            switch (locked) {

                //and there is a locked lock
                case true:

                    //and players can damage all locked items
                    if (this.rules['players_can_damage_locked_items']) {

                        //then allow damage
                        return this.exit(3, "Allowing damage for Rule 'players_can_damage_locked_items' = yes");
                    } else {

                        //then negate damage
                        this.exit(-4, "Removing damage for Rule 'players_can_damage_locked_items' = no");
                        return true;
                    }
                break;

                //And there is an unlocked lock
                case -1:

                    //then allow damage
                    return this.exit(4, "Allowing damage for Rule 'players_can_damage_all_buildings' = yes");
                break;

                //And there is no lock
                default:
                case false:
                
                    //then allow damage
                    return this.exit(5, "Allowing damage for Rule 'players_can_damage_all_buildings' = yes");
                break;
            }

            //Damaging buildings is okay (catch all)
            return this.exit(6, "Allowing Damage for Rule 'players_can_damage_all_buildings' = yes");
        }

        //Damaging buildings is okay (bad config)
        return this.exit(7, "Allowing Damage for Rule <Bad Config Value>");
    };


    this.hook_StorageContainer = function(attacker, lootContainer) {

        //If loot container damage not on
        if (!this.rules['players_can_damage_loot_containers']) {

            //then negate the damage
            this.exit(-3, "Removing Damage for Rule 'players_can_damage_all_buildings' = no");
            return true;

        //but, If loot container damage is allowed
        } else {

            var lock        = lootContainer.GetSlot(0);
            var locked      = (!lock) ? -1 : lock.IsLocked();

            switch (locked) {

                //and there is a locked lock
                case true:

                    //and players can damage locked items
                    if (this.rules['players_can_damage_locked_items']) {

                        //then allow damage
                        return this.exit(3, "Allowing damage for Rule 'players_can_damage_locked_items' = yes");

                    //and players cannot damage locked items
                    } else {

                        //then negate damage
                        this.exit(-4, "Removing damage for Rule 'players_can_damage_locked_items' = no");
                        return true;
                    }
                break;

                //and there is an unlocked lock
                case -1:

                    //then allow damage
                    return this.exit(4, "Allowing damage for Rule 'players_can_damage_loot_containers' = yes");
                break;

                //and there is no lock
                default:
                case false:
                
                    //then allow damage
                    return this.exit(5, "Allowing damage for Rule 'players_can_damage_loot_containers' = yes");
                break;
            }

            //Damaging loot containers is okay (catch all)
            return this.exit(6, "Allowing Damage for Rule 'players_can_damage_loot_containers' = yes");
        }

        //Damaging loot container is okay (bad config)
        return this.exit(7, "Allowing Damage for Rule <Bad Config Value>");
    };


    this.hook_BasePlayer    = function(attacker, victim) {
        
        var state           = '';
        var isHome          = this.isHome(victim);
        var isIntruder      = this.isIntruder(attacker);

        //Victim is not home and attacker is not home
        if (!isHome && !isIntruder) {
            state   = 'contested';

        //Victim is home but attacker is not home
        } else if (isHome && isIntruder) {
            state   = 'private';

        //victim is not home
        } else if (!isHome && isIntruder) {
            state   = 'trespassing';

        //vicitim is at home and intruder is at home
        } else if (isHome && !isIntruder) {
            state   = 'friendly';
        }


        var ruleSet 			= this.plrRules[this.pvp];
        if (!ruleSet) 			return;

        var currentPvpState 	= ruleSet[state];
        if (!currentPvpState) 	return;


        return (currentPvpState == false);
    };


    this.filterType         = function(victimType, attackerType) {

        if (!this.config[victimType]) {
            this.exit(-20, "No settings for " + victimType);
            return true;
        }

        if (!this.config[victimType]['damage_filter']) {
            this.exit(-21, "No filter settings for " + victimType);
            return true;
        }

        var allowedTypes = this.config[victimType]['damage_filter'];
        if (allowedTypes == false) return true;

        for (var index in allowedTypes) {
            var type    = allowedTypes[index];
            if (attackerType == type) return true;
        }

        return false;
    };


    this.isHome             = function(victim) {
        return victim.CanBuild();
    };


    this.isIntruder         = function(attacker) {
        return (attacker.CanBuild() == false);
    };
    
    
    this.pveSwitchEnabled   = function() {
		return this.config['pveSwitch']['enabled'];
    };

    this.shouldBeOn			= function() {
		var mode 			= this.config['pveSwitch']['pveModeTurnsController'];
        var inPVEmode       = this.getPVE();
        
        if (inPVEmode && mode == 'on') {
            return true;
        } else if(inPVEmode && mode != 'on') {
            return false;
        } else if (!inPVEmode && mode == 'on') {
            return false;
        } else {
            return true;
        }
    };

    this.getPVE 			= function() {
    	var visionSrc 		= this.config['pveSwitch']['useVisionPVP'];

    	if (!visionSrc) {
    		var global      = importNamespace("ConVar");
        	var server      = global.Server;

        	if (!server || typeof server == 'undefined') return;
        	var pveMode		= server.pve;
        	return (pveMode == 1) ? 'pve' : 'pvp';
    	} else {
    		//not implemented
    	}

    	return 'pvp';
    };


    this.calcDamage         = function(entity, info) {

        //needs morning coffee. not ready to start the day
        //if (!this.loaded) return;


        //entities
        var attacker        = info.Initiator;
        if (!attacker) return;

        var attackerType    = attacker.GetType();
        var victimType      = entity.GetType();

        //hook name
        var hook            = 'hook_' + victimType;

        //if there is not a handler for this hook, just exit
        if (!this[hook]) return;// this.exit(-1, 'No handler for hook ' + hook);
        
        //Is pve switch on
        var switching 		= this.pveSwitchEnabled();

        if (switching) {
			var desiredMode 	= this.shouldBeOn();

			if (desiredMode != this.on) {
				this.on 	= this.desiredMode;
			}
        }

        //if not working today, take the day off
        if (!this.on) return;

        //Filter attacker types based on victim type
        var allowProcessing = this.filterType(victimType, attackerType);
        if (!allowProcessing) {
            return this.exit(-2, 'Type '+ attackerType +' Filtered from type '+ victimType +'. Skipping');
        }

        //If damage is not on then remove damage
        if (!this.rules['damage_on']) {
            this.exit(1, "Removing Damage for Rule 'damage_on' = no");
            return true;
        }

        //set the handler
        var handler         = this[hook];

        //execute the handler function and return its result
        return handler(attacker, entity);
    };


    this.exit               = function(code, msg) {
        if (msg && this.debug)  this.send("Exiting: " + msg);
        if (this.debug)         this.send("Exit Code: (" + code + ")");
        return;
    };


    this.send               = function(output) {
        return bootstrap.console(output, 4);        
    };

    return this.init(prefix, configObject);
};


var bootstrap 		= {

	title: 			'VisionDMG',
	author: 		'Vision',
	version: 		V(0,1,2),
	description: 	'Vision Damage Controller',
	configVersion: 	'1.2',
	debugLevel: 	0,

	plugin_hooks: 	[
		'visionPVP'
	],

	permissions: 	[
		'restrict.LootContainer',
		'restrict.BuildingBlock',
		'restrict.BasePlayer'
	],

	config: {
		settings: {
			"ConfigVersion": 	"1.2",
			"CurrentEntMode":	"Protected PVE",
			"CurrentPVPMode": 	"Protected PVP",

            "pveSwitch":        {
                "enabled":                  false,
                "pveModeTurnsController":   "on",
                "useVisionPVP":             false
            },

			"Restrictions": 	{
				"StorageContainer":	0,
				"BuildingBlock":  	0,
				"BasePlayer": 		0
			},

			"BuildingBlock":	{
				"damage_filter":	[
					"BasePlayer"
				]
			},

			"StorageContainer":	{
				"damage_filter": 	[
					"BasePlayer"
				]
			},

			"BasePlayer": 		{
				"damage_filter":	[
					"BasePlayer"
				]
			},

			"Descriptions": {
				"contested": 	"Area of the map where no building privileges are defined",
				"private":  	"Area of the map where the attacker does not have building privileges but the victim does",
				"trespassing":  "Area of the map where the attacker and the victim explicitly to not have building privileges",
				"friendly": 	"Area of the map where the victim and attacker both have building privileges"
			},

			"PVPModes": {

		        "Vanilla PVP": {
		        	"contested": 	true,
					"private": 		true,
					"trespassing": 	true,
					"friendly": 	true,
					"description":	"Full PVP regardless of who or where. Same as Vanilla Rust"
		        },

		        "Protected PVP": {
		        	"contested": 	true,
					"private": 		false,
					"trespassing": 	true,
					"friendly": 	true,
					"description":	"Attackers cannot hurt victims when victims have building privileges. If the attacker gains access to the tool cupboard then the victim will not longer be protected."
		        },

		        "Friendly PVP": {
		        	"contested": 	true,
					"private": 		false,
					"trespassing": 	false,
					"friendly": 	true,
					"description":	"Attackers cannot hurt victims when on someone elses property. For this reason, victims will also protected when on their own property by having the private flag set to false."
		        },

		        "Builder": {
		        	"contested": 	true,
					"private": 		false,
					"trespassing": 	false,
					"friendly": 	false,
					"description":	"You cant do damage anywhere people are building. This makes it hard to stop intruders though."
		        }

		    },

		    "EntityModes": 		{

				"Vanilla PVP":	{
					"damage_on":							true,
					"players_can_damage_own_buildings":		true,
					"players_can_damage_all_buildings":		true,
					"players_can_damage_loot_containers":	true,
					"players_can_damage_locked_items":		true
				},

				"Builder": {
					"damage_on":							false,
					"players_can_damage_own_buildings":		true,
					"players_can_damage_all_buildings":		false,
					"players_can_damage_loot_containers":	true,
					"players_can_damage_locked_items":		false
				},

				"Protected PVE": {
					"damage_on":							true,
					"players_can_damage_own_buildings":		true,
					"players_can_damage_all_buildings":		false,
					"players_can_damage_loot_containers":	true,
					"players_can_damage_locked_items":		false
				},

				"Vanilla PVE": {
					"damage_on":							true,
					"players_can_damage_own_buildings":		false,
					"players_can_damage_all_buildings":		false,
					"players_can_damage_loot_containers":	false,
					"players_can_damage_locked_items":		false
				}

			}
		}
	},

	console: 		function(output, level) {
		var msg 	= " - [" + bootstrap['title'] + "] " + output + " - ";

		if (typeof level == 'undefined') {
			level 	= (bootstrap['debugLevel']);
		} else {
			if (parseInt(level) <= (bootstrap['debugLevel'])) {
				print(msg);
			}
		}

		return msg;
	}
};


var env 					= function(pluginObject, configObject) {

	this.Plugin 		= {};
	this.config 		= {};
	this.plugins 		= {};

	this.init 			= function(pluginObject, configObject) {
		bootstrap.console("Loading Environment", 2);
		
		this.Plugin 	= pluginObject;
		this.config 	= configObject;

		//this.init_plugins();
		//this.init_permissions();

		bootstrap.console("Loading Complete", 2);
	};

	this.init_plugins	= function() {
		var pluginList 	= bootstrap['plugin_hooks'];

		if (!pluginList) return;

		for (var index in pluginList) {
			var pluginName 	= pluginList[index];
			var plugin 		= plugins.Find(pluginName);

			if (!plugin) {
				bootstrap.console("Plugin not available: " + pluginName, 3);
			} else {
				bootstrap.console("Plugin found: " + pluginName, 3);
				this.plugins[pluginName]	= plugin;
			}

		}
	};

	this.init_permissions	= function() {
		var permissions 	= bootstrap['permissions'];
		var prefix 			= bootstrap['title'];

		if (!permissions) 	return;

		for (var perm in permissions) {
			var permName 	= prefix +"."+ permissions[perm];

			if (!permission.PermissionExists(permName)) {
				bootstrap.console("Registering Permission: " + permName, 3);
				permission.RegisterPermission(permName, this.Plugin);
			}
		}
	};

	this.init_config 		= function() {

	};

	return this.init(pluginObject, configObject);
};


var api 					= function(oxidePlugin, env, controller) {

    this.setEntityMode     	= function(newMode) {

    };

    this.setPlayerMode     	= function(newMode) {

    };

    this.getEntityMode     	= function() {

    };

    this.getPlayerMode     	= function() {

    };

    this.createEntityMode   = function(ownBuildings, allBuildings, lootContainers, lockedItems) {

    };

    this.createPlayerMode   = function(contestedPVP, privatePVP, trespassingPVP, friendlyPVP) {

    };

    this.pveSwitch     		= function() {

    };

    this.usingVisionPVP     = function() {

    };

    this.pveOnMode          = function() {

    };

    this.isOn               = function() {

    };

    this.isOff              = function() {

    };

    this.onEntityModeChange = function() {};
    this.onPlayerModeChange = function() {};
    this.onTurnOn 			= function() {};
    this.onTurnOff 			= function() {};

};


var VisionDMG			= {

	Title: 				bootstrap.title,
	Author: 			bootstrap.author,
	Version: 			bootstrap.version,
	Description: 		bootstrap.description,
	ResourceId: 		0,
	HasConfig: 			true,

	env: 				{},
	controller: 		{},
	apiObject:			{},

	Init: 				function() {
		var config      = this.LoadDefaultConfig();
		this.env 		= new env(this.Plugin, config);
		this.controller = new controller(bootstrap['title'], config);
		this.apiObject	= new api(this.Plugin, this.env, this.controller);
	},

	LoadDefaultConfig: 	function() {
		this.Config.Settings    = this.Config.Settings || bootstrap['config']['settings'];
		return this.Config.Settings;
	},

	api: 				function() {
		return this.apiObject;
	},

	OnEntityTakeDamage: function(entity, info) {
		return this.controller.calcDamage(entity, info);
	}

};