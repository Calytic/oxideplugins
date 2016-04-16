/**
 * DamageController
 * By VisionMise
 * 
 * - Powered by VisionEngine -
 * 
 * Damage Controller uses rule-based game modes to control when damage is
 * dealt to players and objects.
 * 
 * This allows for endless dynamic configurations with many damage modes
 * built in as templates. Advanced users can configure the settings with
 * basic rules that determine Player Vs Player and Player Vs Object 
 * damage.
 * 
 * See README.md for more information
 * 
 * Last Updated April 2016
 * 
 * @version 0.1.2
 * @author VisionMise
 * @repo http://git.kuhlonline.com:8000/rust/damageController.git
 */


/** Global Settings */

    /**
     * engineVersion
     * @type [String] Version of VisionEngine
     */
    var engineVersion   = '0.5.2';
    
    
    /**
     * configVersion
     * @type [String] Version of DamageController Config
     */
    var configVersion   = '1.0.10';
    
    
    /**
     * Debug Level
     * Debugger Mode can be as high as 6
     * Admin should have this set to 0
     * Developers may want it set to about 3 or 4
     * @type [Integer] debugLevel
     */
    var debugLevel      = 3;
    
    
    /**
     * Generate List
     * This should be off. Used in development
     * to generate a list of all the possible
     * attacker and victim types.
     * @type [Boolean] genList
     */
    var genList         = true;


/**
 * VisionEngine
 * 
 * Still in alpha, the engine works as a global object
 * central to all functions and responsible for all
 * operations and cross-object method calls and can be
 * used independently or in any OxideMod Plugin
 * 
 * @version 0.5.2
 * @author VisionMise
 * 
 * @param [Object] plugin           The OxideMod Plugin Object
 * @param [Object] config           The OxideMod Config Object
 * @param [Object] rust             The OxideMod Rust Interop Object
 * @param [Object] data             The OxideMod Data Object
 * @param [Object] dmgController    Custom DamageController OxideMod Plugin Object
 */
var visionEngine = function(plugin, config, rust, data, dmgController) {
    /** Engine Variables */
        
        /**
         * @type [Object] engine Global Scoped variable of self
         */
        var engine              = this;
        
        /**
         * @type [Boolean] ready Boolean for Readiness
         */
        this.ready              = false;
        
        /**     
         * @type [Boolean] Boolean for Extension Readiness
         */
        this.extensionsReady    = false;
        
        /**
         * @type [Object] Extensions catalog
         */
        this.extensions         = {};
    
    
    /** Controllers */
    
        /**
         * OxideMod Plugin Controller
         * @param [Object] oxidePlugin OxideMod Plugin Object
         */    
        this.pluginController   = function(oxidePlugin) {
            return this;
        };
        
        /**
         * DamageController Mode Controller
         */  
        this.modeController     = function() {
            
            /**@type [String] mode The current DamageController Mode */
            this.mode           = '';
            
            /**@type [Object] modeConf Stores the rules for the Mode */
            this.modeConf       = {};
            
            /** 
             * Init
             * Gets the damageController mode and pre-loads the rules
             */
            this.init           = function() {
                this.mode       = (engine.config.settings['gameMode'])
                    ? engine.config.settings.gameMode
                    : 'pvp'
                ;
                
                engine.logger.debug("Set Damage Mode: " + this.mode, 1);
                
                return this.preLoadRules();
            };
            
            /**
             * Pre Load Rules
             * Loads the rules for the given DamageController mode in to memory
             */
            this.preLoadRules   = function() {
                this.modeConf   = (engine.config.settings['modes'][this.mode])
                    ? engine.config.settings['modes'][this.mode]
                    : {}
                ;
                
                engine.logger.debug("Preloaded Game Mode Rules", 1);
                
                return this;
            };
            
            /** return constructed object */
            return this.init();  
        };
        
        /**
         * DamageController Log Controller
         */  
        this.logController      = function() {
            
            /**
             * Init
             * Construct object
             */
            this.init           = function() {
                return this;
            };
            
            /**
             * Send
             * Sends text to the console
             */
            this.send           = function(text) {
                print("DMG> " + text);
            };
            
            /**
             * Debug
             * Sends debugging info to the console if below
             * the debugging threshold
             */
            this.debug          = function(text, level) {
                if (!level) level = 1;
                if (level <= debugLevel) print ("DMG ["+ level +"]> "+ text);  
            };
            
            /** return constructed object */
            return this.init();  
        };
        
        /**
         * OxideMod Config Controller
         * @param [Object] oxideConfig OxideMod Config Object
         */  
        this.configController   = function(oxideConfig) {
            
            /**@type [Boolean] ready State of readiness */
            this.ready          = false;
            
            /**@type [Object] Plugin Configuration */
            this.config         = {};
            
            /**
             * Init
             * Constructs Object and loads config
             * @param [Object] config   The OxideMod Config Object
             */
            this.init           = function(oxideConfig) {
                if (!oxideConfig) return;
                
                this.config     = oxideConfig;
                this.settings   = this.config.Settings || false;
                
                //Create config if it doesn't exist
                if (!this.settings) this.createConfig();    
                var confVersion = this.settings['configVersion'];
                
                //Update config if its out of date
                if (confVersion != configVersion) this.updateConfig();
                var confVersion = this.settings['configVersion'];
                
                engine.logger.debug("Config version " + confVersion + " Loaded", 1);
            };
            
            /**
             * Create Config
             * Creates config and saves it
             */
            this.createConfig   = function() {
                
                this.settings           = new engine.bootstrap().config;
                this.config.Settings    = this.settings;
                
                engine.controller.interop.SaveConfig();
                engine.logger.debug("Config Created", 2);
                
                return this;    
            };
            
            /**
             * Set
             * Sets a key in the config and saves it
             */
            this.set            = function(key, value) {
                this.settings[key]      = value;
                this.config.Settings    = this.settings;
                engine.controller.interop.SaveConfig();
                return this.settings[key];
            };
            
            /**
             * Get 
             * Gets a key from config if it exists
             */
            this.get            = function(key) {
                return (this.settings[key]) ? this.settings[key] : null;
            };
            
            /**
             * Update Config
             * Updates config and saves it
             */
            this.updateConfig   = function() {
                this.settings           = new engine.bootstrap().config;
                this.config.Settings    = this.settings;
                
                engine.controller.interop.SaveConfig();
                engine.logger.debug("Config Updated", 2);
                return this;
            };
            
            /**
             * Item
             * callback return of self
             */
            this.item           = function() {
                return this;
            };
            
            /** return constructed object */
            return this.init(oxideConfig);
        };
        
        /**
         * OxideMod Rust Interop Controller
         * @param [Object] oxideRust OxideMod Rust Object
         */  
        this.rustController     = function(oxideRust) {
            return this;
        };
        
        /**
         * OxideMod Data Controller
         * @param [Object] oxideData OxideMod Data Object
         */  
        this.dataController     = function(oxideData) {
            
            this.data           = {};
            this.table          = {};
            
            this.init           = function(data) {
                this.data       = data;
                this.table      = data.GetData('DamageController');
                return this;  
            };
            
            this.get            = function(key) {
                if (key && this.table[key]) return this.table[key];
                return this.table;
            };
            
            this.set            = function(key, value) {
                
                if (this.table[key]) {
                    var oldValue    = this.table[key];
                    
                    if (oldValue != value) {
                        this.table[key] = value;
                        this.save();
                        return this;
                    }
                }
                
                this.table[key] = value;
                this.save();
            };
            
            this.save           = function() {
                this.data.SaveData('DamageController');
            };
            
            return this.init(oxideData);
        };
        
        /**
         * Damage Controller
         * @param [Object] OxideMod DamageController Object
         */  
        this.dmgController      = function(damageController) {
            
            /**@type [Object] interop The OxideMod Plugin Object */
            this.interop        = {};
            
            /**
             * Init
             * Construct object and set Plugin Interop
             * @param [Object] dmgController    DamageController OxideMod Plugin Object
             */
            this.init           = function(dmg) {
                this.interop    = dmg;
                return this;
            };
            
            /** return constructed object */
            return this.init(damageController);  
        };
        
        /**
         * DamageController Event Controller
         * Handles Event Hooks and Event Raising
         */  
        this.eventController    = function() {
            
            /**@type [Object] Hooks stores the event handlers */
            this.hooks          = {};
            
            /**
             * Init
             * Constructs the object
             */
            this.init           = function() {
                return this;
            };
            
            /**
             * Raise Event
             * raises and event and calls all the hooked handlers for
             * the raised event
             * @param [String] eventName
             * @param [Object] param
             */
            this.raiseEvent     = function(eventName, param) {
                if (!this.hooks[eventName]) return;
                if (!engine.extensionsReady || !engine.ready) return;
                
                engine.logger.debug("Raised Event: " + eventName, 6);
                
                var exts        = this.hooks[eventName];
                var result      = undefined;
                
                for (var index in exts) {
                    var extName     = exts[index];
                    var $ext        = engine.extensions[extName];
                    
                    result          = $ext.raise(eventName, param);
                    
                    if (result['hasResult']) {
                        if (result['forceReturn']) {
                            if (result['return'] === false) {
                                return;            
                            } else if (result['return']) {
                                return result['return'];
                            } else {
                                return;
                            }
                        } 
                    }
                    
                }
                
                if (result['hasResult']) {
                    if (result['return'] == false) {
                        return;
                    } else if (result['return']) {
                        return result['return'];
                    } else {
                        return;
                    }
                } else {
                    if (result === false) return;
                    if (result) return result;
                    return;
                }
            };
            
            /**
             * Hook 
             * Assigns a handler to an event 
             */
            this.hook           = function(eventName, extName) {
                if (!this.hooks[eventName]) this.hooks[eventName] = [];
                this.hooks[eventName].push(extName);  
                engine.logger.debug("Hooked " + eventName  + " to " + extName, 4);
            };
        
            /** Return constructed object */
            return this.init();
        };
    
    
    /** Contructor */
    
        /**
         * Init
         * Initialize the Engine
         * @param [Object] plugin           The OxideMod Plugin Object
         * @param [Object] config           The OxideMod Config Object
         * @param [Object] rust             The OxideMod Rust Interop Object
         * @param [Object] data             The OxideMod Data Object
         * @param [Object] dmgController    Custom DamageController OxideMod Plugin Object
         */
        this.init           = function(oxidePlugin, oxideConfig, oxideRust, oxideData, dmgController) {
            
            this.logger     = new this.logController();
            this.events     = new this.eventController();
            
            this.plugin     = new this.pluginController(oxidePlugin);
            this.controller = new this.dmgController(dmgController);
            this.config     = new this.configController(oxideConfig);
            this.data       = new this.dataController(oxideData);
            this.modes      = new this.modeController();
            this.rust       = new this.rustController(oxideRust);
            
            this.ready      = true;
            this.loadExtensions();
            
            return this;
        };
    
    
    /** Base Options and Configuration */
    
        /**
         * Bootstrap
         * 
         * Basic Engine options and default configuration
         */
        this.bootstrap      = function() {
            
            this.config     = {};
            
            this.init       = function() {
                
                this.config = {
                    "engineVersion":    engineVersion,
                    "configVersion":    configVersion,
                    "gameMode":         "The Purge",
                    "modes": {
                        "Safehouse": {
                            "Rule 1 - Protect the Owner": {
                                "action": "protect",
                                "conditions": [
                                    "victim.GetType = BasePlayer",
                                    "attacker.GetType = BasePlayer",
                                    "victim.CanBuild = 1",
                                    "attacker.CanBuild = 0"
                                ]
                            },
                            "Rule 2 - Protect the Owners' Stuff only if they locked it": {
                                "action": "protect",
                                "conditions": [
                                    "victim.GetType = StorageContainer",
                                    "victim.Locked = 1",
                                    "attacker.CanBuild = 0"
                                ]
                            },
                            "Rule 3 - Protect the Owners' Abode": {
                                "action": "protect",
                                "conditions": [
                                    "victim.GetType = BuildingBlock",
                                    "attacker.CanBuild = 0"
                                ]
                            }
                        },
                        "Safehouse Purge": {
                            "Rule 1 - Protect the Owner during the day": {
                                "action": "protect",
                                "conditions": [
                                    "victim.GetType = BasePlayer",
                                    "attacker.GetType = BasePlayer",
                                    "victim.CanBuild = 1",
                                    "attacker.CanBuild = 0",
                                    "env.isDay = 1"
                                ]
                            },
                            "Rule 2 - Protect the Owners' Stuff only if they locked it during the day": {
                                "action": "protect",
                                "conditions": [
                                    "victim.GetType = StorageContainer",
                                    "victim.Locked = 1",
                                    "attacker.CanBuild = 0",
                                    "env.isDay = 1"
                                ]
                            },
                            "Rule 3 - Protect the Owners' Abode during the day": {
                                "action": "protect",
                                "conditions": [
                                    "victim.GetType = BuildingBlock",
                                    "attacker.CanBuild = 0",
                                    "env.isDay = 1"
                                ]
                            }
                        },
                        "Quiet House": {
                            "Rule 1 - Protect the Owner during the night": {
                                "action": "protect",
                                "conditions": [
                                    "victim.GetType = BasePlayer",
                                    "attacker.GetType = BasePlayer",
                                    "victim.CanBuild = 1",
                                    "attacker.CanBuild = 0",
                                    "env.isNight = 1"
                                ]
                            },
                            "Rule 2 - Protect the Owners' Stuff only if they locked it during the night": {
                                "action": "protect",
                                "conditions": [
                                    "victim.GetType = StorageContainer",
                                    "victim.Locked = 1",
                                    "attacker.CanBuild = 0",
                                    "env.isNight = 1"
                                ]
                            },
                            "Rule 3 - Protect the Owners' Abode during the night": {
                                "action": "protect",
                                "conditions": [
                                    "victim.GetType = BuildingBlock",
                                    "attacker.CanBuild = 0",
                                    "env.isNight = 1"
                                ]
                            }
                        },
                        "The Purge": {
                            "Rule 1 - PVP at night": {
                                "action": "damage",
                                "conditions": [
                                    "victim.GetType = BasePlayer",
                                    "attacker.GetType = BasePlayer",
                                    "env.isNight = 1"
                                ]
                            },
                            "Rule 2 - PVE at day": {
                                "action": "protect",
                                "conditions": [
                                    "victim.GetType = BasePlayer",
                                    "attacker.GetType = BasePlayer",
                                    "env.isDay = 1"
                                ]
                            }
                        },
                        "Quiet Night": {
                            "Rule 1 - PVE at night": {
                                "action": "protect",
                                "conditions": [
                                    "victim.GetType = BasePlayer",
                                    "attacker.GetType = BasePlayer",
                                    "env.isNight = 1"
                                ]
                            },
                            "Rule 2 - PVP at day": {
                                "action": "damage",
                                "conditions": [
                                    "victim.GetType = BasePlayer",
                                    "attacker.GetType = BasePlayer",
                                    "env.isDay = 1"
                                ]
                            }
                        },
                        "PVP": {
                            "Rule 1 - Protect Players": {
                                "action": "damage",
                                "conditions": [
                                    "victim.GetType = BasePlayer",
                                    "attacker.GetType = BasePlayer"
                                ]    
                            }
                        }, 
                        "PVE": {
                            "Rule 1 - Protect Players": {
                                "action": "protect",
                                "conditions": [
                                    "victim.GetType = BasePlayer",
                                    "attacker.GetType = BasePlayer"
                                ]    
                            },
                            "Rule 2 - Protect Buildings": {
                                "action": "protect",
                                "conditions": [
                                    "attacker.GetType = BasePlayer",
                                    "victim.GetType = BuildingBlock"
                                ]
                            },
                            "Rule 3 - Protect Doors": {
                                "action": "protect",
                                "conditions": [
                                    "attacker.GetType = BasePlayer",
                                    "victim.GetType = Door"
                                ]
                            },
                            "Rule 4 - Protect Containers": {
                                "action": "protect",
                                "conditions": [
                                    "attacker.GetType = BasePlayer",
                                    "victim.GetType = StorageContainer"
                                ]
                            }            
                        }
                    }  
                };
                
                return this;  
            };
            
            return this.init();  
        };
        
        this.notify         = function() {
              return "http://rust.kuhlonline.com/dmg?version=" + engineVersion;
        };
        
    
    /** Extension Handlers */
    
        /**
         * Load Extensions
         */
        this.loadExtensions = function() {
            if (!this.ready) return;
            
            this.extensions['extTime']  = new extTime(engine);
            engine.logger.debug("Loaded Engine Extension: Time", 2);
            
            this.extensions['extPVP']   = new extPVP(engine);
            engine.logger.debug("Loaded Engine Extension: PVP", 2);
            
            this.extensions['extPVO']   = new extPVO(engine);
            engine.logger.debug("Loaded Engine Extension: PVO ", 2);
            
            for (var ext in this.extensions) {
                if (this.extensions[ext]['handlers']) {
                    for (var eventName in this.extensions[ext].handlers) {
                        this.events.hook(this.extensions[ext].handlers[eventName], ext);
                    }
                }
            }
            
            this.extensionsReady        = true;
            return;
        };
    
    
    /** Construct and Return Self */
        return this.init(plugin, config, rust, data, dmgController);
};


/**
 * Event Return Object
 * 
 * Used to evaluate how to and when to return values raised by events
 * 
 * @param [any] result
 * @param [boolean] returnNow
 */
var eventReturn = function(result, returnNow) {
    
    /**@type [Object] return Result Catalog */
    this.return     = {};
    
    /**@type [any] eventResult the Result Value */
    this.eventResult;
    
    /**
     * Init
     * Constructs the object
     * @param [any] returnResult
     * @param [Boolean] returnNow
     */
    this.init   = function(returnResult, returnNow) {
        this.eventResult    = returnResult;
        this.return         = {
            'hasResult':    true,
            'return':       this.eventResult,
            'forceReturn':  returnNow
        };
        
        return this.return;
    };
  
    /** return result */
    return this.init(result, returnNow);  
};


/**
 * VisionEngine Action Extension
 */
var extAction   = function(options, action, entities) {
    
    this.options    = {};
    this.action     = '';
    this.entities   = {};
    this.returnCode = false;
  
    this.init   = function(actionOptions, targetAction, entities) {
        this.options    = actionOptions;
        this.action     = targetAction;
        return this.execute();
    };
    
    this.execute                = function() {
        
        if (this.action = 'protect') {
            this.returnCode     = true;
        } else if (this.action = 'damage') {
            this.returnCode     = false;
        } else {
            this.executeTargetAction();    
        }
        
        return this.returnCode;
    };
    
    this.action_api             = function() {
        return false;
    };
    
    this.action_script          = function() {
        return false;
    };
    
    this.executeTargetAction    = function() {
        var funcName    = "action_" + this.action;
        var exists      = (!this[funcName]) ? false : true;
        
        if (!exists) {
            this.returnCode     = false;
            return this;
        }
        
        return this[funcName](this.entities);
    };
    
    return this.init(options, action, entities);  
};


/**
 * VisionEngine Time Extension
 * 
 * Gets information releated to time from Rust to 
 * provide time of day or if it's day or night
 * @param [VisionEngine] engine
 */
var extTime      = function(engine) {
    
    /**
     * Rust TOD_Sky Object
     * @type {Global.TOD_Sky}
     * @type sky
     */
    this.sky            = {};
    
    
    /**
     * Event Controller Interface Function 
     */
    this.raise          = function(eventName) {};


    /**
     * Initialize Time API
     * @return {Self}
     */
    this.init           = function() {
        var global              = importNamespace(""); 

        if (global['TOD_Sky']['Instance']) {
            this.sky                = global.TOD_Sky.Instance;
            if (this.sky) engine.logger.debug("Sky Found", 3);
        }
        
        return this;
    };


    /**
     * Hour of the day
     * @return {Integer}
     */
    this.hour           = function() {
        if (!this.sky) this.init();
        var hour = parseInt(this.sky.Cycle.Hour);
        return hour;
    };


    /**
     * Time of Day
     * @return {Float}
     */
    this.time           = function() {
        if (!this.sky) this.init();
        var time = this.sky.Cycle.Hour;
        return time;
    };


    /**
     * Is it daytime
     * @return {Boolean}
     */
    this.isDay          = function() {
        if (!this.sky) this.init();
        return this.sky.IsDay;
    };


    /**
     * Is is night time
     * @return {Boolean}
     */
    this.isNight        = function() {
        if (!this.sky) this.init();
        return this.sky.IsNight;
    };
    
    this.handlers   = {};
    
    return this.init();
};


/**
 * VisionEngine PVP Extension
 * 
 * Monitors damage given from players to players.
 * Uses modeController to store rules in memoery
 * to be evaluated when damage is dealt so it can
 * be allowed or negated based on rule actions
 * @param [VisionEngine] engine
 */
var extPVP       = function(engine) {
    
    /**
     * Event Controller Interface Function 
     * @param [String] eventName Name of the event to raise
     * @param [Object] param Arguments for the callback function
     */
    this.raise          = function(eventName, param) {
        var funcName    = (this.hooks[eventName]) ? this.hooks[eventName] : '';
        if (!funcName) return;
        
        if (!this[funcName]) return;
        engine.logger.debug("Calling Event Handler: PVP " + eventName +"."+ funcName, 6);
        return this[funcName](param);
    };
    
    /**
     * Victim
     * Creates an exposed Entity Object for evaluation
     * @param [Object] entity  
     */
    this.victim     = function(entity) {
       var canBuild    = (entity['CanBuild']) ? entity.CanBuild() : false;
        
        return {
              'CanBuild': canBuild,
              'GetType': 'BasePlayer' 
        };
    };
    
    /**
     * Attacker
     * Creates an exposed Entity Object for evaluation
     * @param [Object] entity 
     */
    this.attacker   = function(entity) {
        var canBuild    = (entity['CanBuild']) ? entity.CanBuild() : false;
        
        return {
            'CanBuild': canBuild,
            'GetType': 'BasePlayer'
        };
    };
    
    /**
     * Area
     * Creates an exposed Entity Object for evaluation
     * @param [Object] entity Attacker entity object 
     * @param [Object] entity Victim entity Object
     */
    this.area       = function(attacker, victim) {
        
        if (!attacker) {
            var friendly            = victim.CanBuild();
            var privateProperty     = (victim.CanBuild() == false);
        } else {
            var friendly            = (attacker.CanBuild() && victim.CanBuild());
            var privateProperty     = (attacker.CanBuild() == false && victim.CanBuild() == false);
        }
        
        return {
            "contested": friendly,
            "friendly": friendly,
            "private": privateProperty
        };
    };
    
    /**
     * Environment
     * Creates an exposed Entity Object for evaluation
     */
    this.env        = function() {
        
        var tm  = engine.extensions['extTime'];
        
        return {
            "hour": tm.hour(),
            "isNight": tm.isNight(),
            "isDay": tm.isDay()
        }
    };
    
    /**
     * Validate
     * Filters out any entity that cannot be evaluated
     * @param [String] attackerType entity object type
     * @param [String] VictimType entity object type 
     */
    this.validate     = function(attackerType, victimType) {
        var ents    = (this.config['attacker'])
            ? this.config['attacker']
            : {}
        ;
        
        if (!ents) return false;
        if (!ents[attackerType]) return false;
        
        var ents    = (this.config['victim'])
            ? this.config['victim']
            : {}
        ;
        
        if (!ents) return false;
        if (!ents[victimType]) return false;
        
        return true;
    };
    
    /**
     * Damage
     * EventHandler
     * Evaluates rules in DamageController Mode to either take action or not
     * based on conditions
     * 
     * @param [Object] param
     */
    this.damage     = function(param) {
        var entity  = param['entity'];
        var hitInfo = param['hitInfo'];
        
        if (!hitInfo || !hitInfo['Initiator']) return new eventReturn(false, false);
        if (!entity) return new eventReturn(false, false);
        
        var attacker        = hitInfo.Initiator;
        if (!attacker)      return new eventReturn(false, false);
        
        engine.logger.debug("PVP Damage Callback " + entity.GetType() + " attacked by  " + hitInfo.Initiator, 5);
        
        var attackerType    = attacker.GetType();
        var victimType      = entity.GetType();
        if (!this.validate(attackerType, victimType)) return new eventReturn(false, false);
        
        engine.logger.debug("PVP Damage Callback " + attackerType + " attacks " + victimType, 4);
        
        var keys            = {
            'victim':   this.victim(entity),
            'attacker': this.attacker(attacker),
            'area': this.area(attacker, entity),
            'env': this.env()
        };
        
        for (var ruleName in engine.modes.modeConf) {
            var rule        = engine.modes.modeConf[ruleName];
            var conditions  = (rule['conditions']) ? rule['conditions'] : [];
            var action      = (rule['action']) ? rule['action'] : '';
            var actionOpt   = (rule['options']) ? rule['options'] : [];
            var valid       = false;
            
            engine.logger.debug("Evaluating Rule " + ruleName, 3);
            
            for (var index in conditions) {
                var cond    = conditions[index];
                var obj     = cond.split(" = ", 2);
                var key     = obj[0];
                var val     = (obj[1]) ? obj[1] : '';
                var obj     = key.split(".", 2);
                var target  = obj[0];
                var prop    = (obj[1]) ? obj[1] : '';
                
                engine.logger.debug("Evaluating Condition: " + condition, 3);
                engine.logger.debug(target + "|" + key +"."+ prop + " ==? " + val, 3);
                
                if (stopEval) engine.logger.debug("\t Breaking out of evaluation", 3);                
                if (stopEval) break;
                
               if (!val || !prop) {
                    engine.logger.debug("\t No Value or Property Found", 3);
                    continue;
                }
                
                var comp    = keys[target][prop];
                
                if (comp != val) {
                    valid       = false;
                    stopEval    = true;
                    engine.logger.debug("\t" + comp + " does not equal " + val, 3);
                    continue;
                }
                
                engine.logger.debug("\t" + comp + " equals " + val, 3);
                valid       = true;
            }
        }
        
        if (!valid) {
            engine.logger.debug("Conditions not met, taking no action", 3);
            return new eventReturn(false, true);
        }
        
        var code    = new extAction(actionOpt, action);
        engine.logger.debug("Conditions met, taking action: " + action, 3);
        return new eventReturn(code, true);
    };
    
    /**
     * Config
     * Static config for exposed object list
     */
    this.config     = {
        "victim": {
            'BasePlayer': true
        },
        "attacker": {
            'BasePlayer': true
        }
    };
    
    /**
     * Handlers
     * List of events that can be handled for EventController
     */
    this.handlers   = [
        'OnEntityTakeDamage'
    ];
    
    /**
     * Hooks
     * Catalog of EventHandlers and callback functions
     */
    this.hooks      = {
        'OnEntityTakeDamage': 'damage'  
    };
    
};


/**
 * VisionEngine PVO Extension
 * 
 * Monitors damage given from players to object or animals.
 * Uses modeController to store rules in memoery
 * to be evaluated when damage is dealt so it can
 * be allowed or negated based on rule actions
 */
var extPVO       = function(engine) {
    
    /**
     * Event Controller Interface Function 
     * @param [String] eventName Name of the event to raise
     * @param [Object] param Arguments for the callback function
     */
    this.raise          = function(eventName, param) {
        var funcName    = (this.hooks[eventName]) ? this.hooks[eventName] : '';
        if (!funcName) return new eventReturn(undefined, true);
        
        if (!this[funcName]) return new eventReturn(undefined, true);;
        engine.logger.debug("Calling Event Handler: PVO " + eventName +"."+ funcName, 6);
        return this[funcName](param);
    };
    
    /**
     * Victim
     * Creates an exposed Entity Object for evaluation
     * @param [Object] entity 
     */
    this.victim     = function(entity) {
        var canBuild    = false;
        
        var lock        = entity.GetSlot(0);
        var locked      = (!lock) ? -1 : lock.IsLocked();
        
        return {
              'CanBuild': canBuild,
              'GetType': entity.GetType(),
              'Locked': locked 
        };
    };
    
    /**
     * Attacker
     * Creates an exposed Entity Object for evaluation
     * @param [Object] entity 
     */
    this.attacker   = function(entity) {
        var canBuild    = (entity['CanBuild']) ? entity.CanBuild() : false;
        var type        = (entity['GetType']) ? entity.GetType() : 'Unknown';
        
        return {
            'CanBuild': canBuild,
            'GetType': type
        };
    };
    
    /**
     * Environment
     * Creates an exposed Entity Object for evaluation
     */
    this.env        = function() {
        
        var tm  = engine.extensions['extTime'];
        
        return {
            "hour": tm.hour(),
            "isNight": tm.isNight(),
            "isDay": tm.isDay()
        }
    };
    
    /**
     * Validate
     * Filters out any entity that cannot be evaluated
     * @param [String] attackerType entity object type
     * @param [String] VictimType entity object type 
     */
    this.validate   = function(attackerType, victimType) {        
        if (genList) {
            var list = engine.config.get('generatedVictimList');
            if (!list) list = {};
            
            list[victimType] = victimType;
            engine.config.set('generatedVictimList', list);  
            engine.logger.debug("Added " + victimType + " to the list", 2);
            
            var list = engine.config.get('generatedAttackerList');
            if (!list) list = {};
            
            list[attackerType] = attackerType;
            engine.config.set('generatedAttackerList', list);  
            engine.logger.debug("Added " + attackerType + " to the list", 2);
        }
        
        var ents    = (this.config['attacker'])
            ? this.config['attacker']
            : {}
        ;
        
        if (!ents) return false;
        if (!ents[attackerType]) return false;
        
        var ents    = (this.config['victim'])
            ? this.config['victim']
            : {}
        ;
        
        if (!ents) return false;
        if (!ents[victimType]) return false;
        
        return true;
    };
    
    /**
     * Damage
     * EventHandler
     * Evaluates rules in DamageController Mode to either take action or not
     * based on conditions
     * 
     * @param [Object] param
     */
    this.damage     = function(param) {
        var entity  = param['entity'];
        var hitInfo = param['hitInfo'];
        
        if (!hitInfo || !hitInfo['Initiator']) return new eventReturn(false, false);
        if (!entity) return new eventReturn(false, false);
        
        var attacker        = hitInfo.Initiator;
        if (!attacker)      return new eventReturn(false, false);
        
        var attackerType    = attacker.GetType();
        var victimType      = entity.GetType();
        if (!this.validate(attackerType, victimType)) return new eventReturn(false, false);
        
        engine.logger.debug("PVO Damage Callback " + attackerType + " attacks " + victimType, 4);
        
         var keys            = {
            'victim':   this.victim(entity),
            'attacker': this.attacker(attacker),
            'area': {},
            'env': this.env()
        };
        
        for (var ruleName in engine.modes.modeConf) {
            var rule        = engine.modes.modeConf[ruleName];
            var conditions  = (rule['conditions']) ? rule['conditions'] : [];
            var action      = (rule['action']) ? rule['action'] : '';
            var actionOpt   = (rule['options']) ? rule['options'] : '';
            var valid       = false;
            var stopEval    = false;
            
            engine.logger.debug("Checking Rule " + ruleName, 3);
            
            for (var index in conditions) {
                var cond    = conditions[index];
                var obj     = cond.split(" = ", 2);
                var key     = obj[0];
                var val     = (obj[1]) ? obj[1] : '';
                var obj     = key.split(".", 2);
                var target  = obj[0];
                var prop    = (obj[1]) ? obj[1] : '';
                
                
                engine.logger.debug("\tEvaluating Condition: " + cond, 3);
                engine.logger.debug("\t" + target +"."+ prop + " ==? " + val, 3);
                
                if (stopEval) engine.logger.debug("\t Breaking out of evaluation", 3);                
                if (stopEval) break;
                
                if (!val || !prop) {
                    engine.logger.debug("\t No Value or Property Found", 3);
                    continue;
                }
                
                var comp    = keys[target][prop];
                
                if (comp != val) {
                    valid       = false;
                    stopEval    = true;
                    engine.logger.debug("\t" + comp + " does not equal " + val, 3);
                    continue;
                }
                
                engine.logger.debug("\t" + comp + " equals " + val, 3);
                valid       = true;
            }
            
            if (valid) break;
        }
        
        if (!valid) {
            engine.logger.debug("Conditions not met, taking no action", 3);
            return new eventReturn(false, true);
        }
        
        var code    = new extAction(actionOpt, action).execute();
        engine.logger.debug("Conditions met, taking action: " + action, 3);
        return new eventReturn(code, true);
    };
    
    /**
     * Config
     * Static config for exposed object list
     */
    this.config     = {
        "victim": {
            'StorageContainer': true,
            'BuildingBlock': true,
            'Door': true,
            'BaseNPC': true
        },
        "attacker": {
            'BasePlayer': true
        }
    };
    
    /**
     * Handlers
     * List of events that can be handled for EventController
     */
    this.handlers   = [
        'OnEntityTakeDamage'
    ];
    
    /**
     * Hooks
     * Catalog of EventHandlers and callback functions
     */
    this.hooks      = {
        'OnEntityTakeDamage': 'damage'  
    };
    
};


/**
 * DamageController
 * 
 * Minimal plugin object for OxideMod to intialize the VisionEngine
 * and hook specific events throw by OxideMod
 * 
 * OxideMod Plugin
 * @version 0.1.2
 * @author VisionMise
 */
var DamageController = {
    
    Title:              "DamageController",
    Author:             "VisionMise",
    Version:            V(0, 1, 2),
    ResourceId:         0,
    HasConfig:          true,
    
    engine:             false,
    ready:              false,
    
    OnPluginLoaded:     function() {
        
        this.engine     = new visionEngine(
            this.Plugin,
            this.Config,
            rust,
            data,
            this
        );
        
        this.ready      = true;
        this.engine.logger.send("DamageController done loading");
        
        webrequests.EnqueueGet(this.engine.notify(), function(code, response) {
            if (response == null || code != 200) return;
            
            this.engine.logger.debug(response, 2);
        }.bind(this), this.Plugin);
    },
    
    OnEntityTakeDamage: function(entity, hitInfo) {
        if (!this.ready || !this.engine) return;
        return this.engine.events.raiseEvent('OnEntityTakeDamage', {'entity': entity, 'hitInfo': hitInfo});
    }
};