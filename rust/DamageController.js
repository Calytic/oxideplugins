/**
 * DamageController
 * By VisionMise
 * 
 * - Powered by ProtoRust -
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
 * Last Updated April 20 2016
 * 
 * @version 0.2.5
 * @author VisionMise
 * @repo http://git.kuhlonline.com:8000/rust/damageController.git
 */


/** Global Settings */

    /**
     * engineVersion
     * @type [String] Version of ProtoRust
     */
    var engineVersion   = '0.5.4';
    
    
    /**
     * Plugin Version
     * @type [String] Version of DamageController
     */
    var pluginVersion   = '0.2.5';
    
    
    /**
     * configVersion
     * @type [String] Version of DamageController Config
     */
    var configVersion   = '1.2.7';
    
    
    /**
     * Debug Level
     * Debugger Mode can be as high as 6
     * Admin should have this set to 0
     * Developers may want it set to about 3 or 4
     * @type [Integer] debugLevel
     */
    var debugLevel      = 0;
    
    
    /**
     * Generate List
     * This should be off. Used in development
     * to generate a list of all the possible
     * attacker and victim types.
     * @type [Boolean] genList
     */
    var genList         = false;


/**
 * ProtoRust
 * 
 * Still in alpha, the engine works as a global object
 * central to all functions and responsible for all
 * operations and cross-object method calls and can be
 * used independently or in any OxideMod Plugin
 * 
 * @version 0.5.4
 * @author VisionMise
 * 
 * @param [Object] plugin           The OxideMod Plugin Object
 * @param [Object] config           The OxideMod Config Object
 * @param [Object] rust             The OxideMod Rust Interop Object
 * @param [Object] data             The OxideMod Data Object
 * @param [Object] dmgController    Custom DamageController OxideMod Plugin Object
 * @param [String] pluginName       The name of the plugin
 */
var ProtoRust = function(oxide_plugin, oxide_config, oxide_rust, oxide_data, dmgController, pluginName) {
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
        
        
        this.logger     = {};
        this.events     = {};
        this.plugin     = {};
        this.controller = {};
        this.config     = {};
        this.data       = {};
        this.modes      = {};
        this.rust       = {};
        this.commands   = {};
        this.pluginName = '';
    
    
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
         * @todo remove from engine and move to extension
         */  
        this.modeController     = function() {
            
            /**@type [String] mode The current DamageController Mode */
            this.mode           = '';
            
            /**@type [Object] modeConf Stores the rules for the Mode */
            this.modeConf       = {};
            
            this.eventConf      = {};
            
            /** 
             * Init
             * Gets the damageController mode and pre-loads the rules
             */
            this.init           = function() {
                this.mode       = (engine.config.settings['gameMode'])
                    ? engine.config.settings.gameMode
                    : 'pvp'
                ;
                
                this.eventConf  = engine.config.settings['events'];
                
                engine.logger.debug("Set Damage Mode: " + this.mode, 1);
                
                return this.preLoadRules();
            };
            
            this.setMode        = function(mode) {
                engine.config.settings['gameMode'] =  mode;
                engine.config.save();
                
                engine.logger.debug("Changed mode to " + mode, 3);
                var popups      = engine.extensions['extPopupNotifications'];
                popups.notify("Game mode changed to " + mode);
                
                return this.init();
            };
            
            /**
             * Pre Load Rules
             * Loads the rules for the given DamageController mode in to memory
             */
            this.preLoadRules   = function() {
                this.modeConf   = (engine.config.settings['gameModes'][this.mode])
                    ? engine.config.settings['gameModes'][this.mode]
                    : {}
                ;
                
                engine.logger.debug("Preloaded Game Mode Rules", 1);
                
                return this;
            };
            
            /** return constructed object */
            return this.init();  
        };
        
        /**
         * ProtoRust Log Controller
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
                print("[DamageController] " + text);
            };
            
            /**
             * Debug
             * Sends debugging info to the console if below
             * the debugging threshold
             */
            this.debug          = function(text, level) {
                if (!level) level = 1;
                if (level <= debugLevel) print ("[DamageController] ["+ level +"]> "+ text);  
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
            this.init           = function(cfg) {
                if (!cfg) return;
                
                this.config     = cfg;
                this.settings   = this.config.Settings || false;
                
                //Create config if it doesn't exist
                if (!this.settings) this.createConfig();    
                var confVersion = this.settings['configVersion'];
                
                //Update config if its out of date
                if (confVersion != configVersion) this.updateConfig();
                var confVersion = this.settings['configVersion'];
                
                engine.logger.debug("Config version " + confVersion + " Loaded", 1);
                return this;
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
            
            this.save           = function() {
                this.config.Settings    = this.settings;
                engine.controller.interop.SaveConfig();  
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
         * Plugin Interop
         * @todo Change from Plugin Dependent to generic interop object for portability
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
         * ProtoRust Event Controller
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
                
                engine.logger.debug("Raised Event: " + eventName, 5);
                
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
        
        /**
         * DamageController Command Controller
         * Handles commands from chat and console
         * @todo Move to extension and remove from engine, commands are plugin dependent not engine dependent
         */
        this.commandController  = function() {
            
            this.init           = function() {
                return this;
            };
            
            this.damageCMD      = function(arg, player) {
                
                var subCmd  = (arg[0]) ? arg[0].toLowerCase().trim() : '';                
                engine.logger.debug("CMD: " + subCmd + " => " + arg[1], 3);
                
                if (!subCmd) {
                    if (player) {
                        engine.sendChat(player, "Game mode is " + engine.modes.mode);
                        return;
                    } else {
                        engine.logger.send("Game mode is " + engine.modes.mode);
                    }
                }
                
                var funcName    = "cmd_" + subCmd;
                
                if (!this[funcName]) {
                    if (player) {
                        engine.sendChat(player, "Command not understood: " + subCmd);
                    } else {
                        engine.logger.send("Command not understood: " + subCmd);
                    }
                    return;
                }
                
                return this[funcName](arg, player);
            };
            
            this.cmd_mode       = function(arg, player) {
                
                var newMode     = (arg[1]) ? arg[1] : '';
                var msg         = '';
                
                if (player) {
                    if (newMode != '' && player.IsAdmin()) {
                        engine.modes.setMode(newMode);
                        msg     = "Game mode changed to " + newMode;
                        engine.logger.send(msg);
                    } else {
                        msg     = "Game mode is " + engine.modes.mode;
                        engine.sendChat(player, msg);    
                    }                    
                } else {
                    if (newMode) {
                        engine.modes.setMode(newMode);
                        msg     = "Game mode changed to " + newMode;
                        engine.logger.send(msg);
                    } else {
                        msg     = "Game mode is " + engine.modes.mode;
                        engine.logger.send(msg);
                    }
                }               
                
                return msg;
            };
            
            return this.init();
        };
        
        
        /**
         * Send a Chat Message
         * @global
         */
        this.sendChat           = function(player, msg) {
            rust.SendChatMessage(player.ToPlayer(), msg);
            return this;  
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
        this.init           = function(oxidePlugin, oxideConfig, oxideRust, oxideData, dmgController, pluginName) {
            
            this.pluginName = pluginName;
            this.logger     = new this.logController();
            this.events     = new this.eventController();
            
            this.plugin     = new this.pluginController(oxidePlugin);
            this.controller = new this.dmgController(dmgController);
            this.config     = new this.configController(oxideConfig);
            this.data       = new this.dataController(oxideData);
            this.modes      = new this.modeController();
            this.rust       = new this.rustController(oxideRust);
            this.commands   = new this.commandController();
            
            this.ready      = true;
            this.loadExtensions();
            
            return this;
        };
    
    
    /** Base Options and Configuration */
    
        /**
         * Bootstrap
         * @todo Move to outside engine
         * Basic Engine options and default configuration
         */
        this.bootstrap      = function() {
            
            this.config             = {};
            this.chatCommands       = {};
            this.consoleCommands    = {};
            
            this.init       = function() {
                
                this.config = {
                    "engineVersion":    engineVersion,
                    "configVersion":    configVersion,
                    "gameMode":         "Light PVP",
                    "events": {
                        "The Purge": {
                            "day": "[PVE MODE]\nIt is now Daytime and PVP is restricted",
                            "night": "[PVP MODE]\nIt is now Night and PVP is allowed"
                        },
                        "Quiet Night": {
                            "night": "[PVE MODE]\nIt is now Night and PVP is restricted",
                            "day": "[PVP MODE]\nIt is now Day and PVP is allowed"
                        },
                        "Safehouse Purge": {
                            "night": "[SPECIAL PVP MODE]\nIt is now Night and PVP is allowed except for inside player homes",
                            "day": "[SPECIAL PVE MODE]\nIt is now Daytime and PVP is restricted everywhere"
                        }
                    },
                    "gameModes": {
                        
                        "Safehouse Purge": {
                            "BasePlayer": {
                                "rules": [
                                    {
                                        "name": "Protect Player at Home during the day",
                                        "conditions": [
                                            "attacker.GetType = BasePlayer",
                                            "attacker.CanBuild = 0",
                                            "victim.CanBuild = 1",
                                            "env.isDay = 1"
                                        ],
                                        "action": {
                                            "type": "protect"
                                        }
                                    }
                                ]
                            },
                            "Door": {
                                "rules": [
                                    {
                                        "name": "Protect Door at Home during the day",
                                        "conditions": [
                                            "attacker.GetType = BasePlayer",
                                            "attacker.CanBuild = 0",
                                            "env.isDay = 1"
                                        ],
                                        "action": {
                                            "type": "protect"
                                        }
                                    }
                                ]
                            },
                            "StorageContainer": {
                                "rules": [
                                    {
                                        "name": "Protect Locked Storage Containers at Home during the day",
                                        "conditions": [
                                            "attacker.GetType = BasePlayer",
                                            "attacker.CanBuild = 0",
                                            "victim.locked = 1",
                                            "env.isDay = 1"
                                        ],
                                        "action": {
                                            "type": "protect"
                                        }
                                    }
                                ]
                            },
                            "BuildingBlock": {
                                "rules": [
                                    {
                                        "name": "Protect Buildings at Home during the day",
                                        "conditions": [
                                            "attacker.GetType = BasePlayer",
                                            "attacker.CanBuild = 0",
                                            "env.isDay = 1"
                                        ],
                                        "action": {
                                            "type": "protect"
                                        }
                                    }
                                ]
                            }
                        },
                        
                        "Safehouse": {
                            "BasePlayer": {
                                "rules": [
                                    {
                                        "name": "Protect Player at Home",
                                        "conditions": [
                                            "attacker.GetType = BasePlayer",
                                            "attacker.CanBuild = 0",
                                            "victim.CanBuild = 1"
                                        ],
                                        "action": {
                                            "type": "protect"
                                        }
                                    }
                                ]
                            },
                            "Door": {
                                "rules": [
                                    {
                                        "name": "Protect Door at Home",
                                        "conditions": [
                                            "attacker.GetType = BasePlayer",
                                            "attacker.CanBuild = 0"
                                        ],
                                        "action": {
                                            "type": "protect"
                                        }
                                    }
                                ]
                            },
                            "StorageContainer": {
                                "rules": [
                                    {
                                        "name": "Protect Locked Storage Containers at Home",
                                        "conditions": [
                                            "attacker.GetType = BasePlayer",
                                            "attacker.CanBuild = 0",
                                            "victim.locked = 1"
                                        ],
                                        "action": {
                                            "type": "protect"
                                        }
                                    }
                                ]
                            },
                            "BuildingBlock": {
                                "rules": [
                                    {
                                        "name": "Protect Buildings at Home",
                                        "conditions": [
                                            "attacker.GetType = BasePlayer",
                                            "attacker.CanBuild = 0"
                                        ],
                                        "action": {
                                            "type": "protect"
                                        }
                                    }
                                ]
                            }
                        },
                        
                        "The Purge": {
                            "BasePlayer": {
                                "rules": [
                                    {
                                        "name": "Protect Player during the day",
                                        "conditions": [
                                            "attacker.GetType = BasePlayer",
                                            "env.isDay = 1"
                                        ],
                                        "action": {
                                            "type": "protect"
                                        }
                                    }
                                ]
                            },
                            "Door": {
                                "rules": [
                                    {
                                        "name": "Protect Door during the day",
                                        "conditions": [
                                            "attacker.GetType = BasePlayer",
                                            "env.isDay = 1"
                                        ],
                                        "action": {
                                            "type": "protect"
                                        }
                                    }
                                ]
                            },
                            "StorageContainer": {
                                "rules": [
                                    {
                                        "name": "Protect Locked Storage Containers during the day",
                                        "conditions": [
                                            "attacker.GetType = BasePlayer",
                                            "victim.locked = 1",
                                            "env.isDay = 1"
                                        ],
                                        "action": {
                                            "type": "protect"
                                        }
                                    }
                                ]
                            },
                            "BuildingBlock": {
                                "rules": [
                                    {
                                        "name": "Protect Buildings during the day",
                                        "conditions": [
                                            "attacker.GetType = BasePlayer",
                                            "env.isDay = 1"
                                        ],
                                        "action": {
                                            "type": "protect"
                                        }
                                    }
                                ]
                            }
                        },
                        
                        "Quiet Night": {
                            "BasePlayer": {
                                "rules": [
                                    {
                                        "name": "Protect Player during the night",
                                        "conditions": [
                                            "attacker.GetType = BasePlayer",
                                            "env.isNight = 1"
                                        ],
                                        "action": {
                                            "type": "protect"
                                        }
                                    }
                                ]
                            },
                            "Door": {
                                "rules": [
                                    {
                                        "name": "Protect Door during the night",
                                        "conditions": [
                                            "attacker.GetType = BasePlayer",
                                            "env.isNight = 1"
                                        ],
                                        "action": {
                                            "type": "protect"
                                        }
                                    }
                                ]
                            },
                            "StorageContainer": {
                                "rules": [
                                    {
                                        "name": "Protect Locked Storage Containers during the night",
                                        "conditions": [
                                            "attacker.GetType = BasePlayer",
                                            "victim.locked = 1",
                                            "env.isNight = 1"
                                        ],
                                        "action": {
                                            "type": "protect"
                                        }
                                    }
                                ]
                            },
                            "BuildingBlock": {
                                "rules": [
                                    {
                                        "name": "Protect Buildings during the night",
                                        "conditions": [
                                            "attacker.GetType = BasePlayer",
                                            "env.isNight = 1"
                                        ],
                                        "action": {
                                            "type": "protect"
                                        }
                                    }
                                ]
                            }
                        },
                        
                        "Sleepy": {
                            "BasePlayer": {
                                "rules": [
                                    {
                                        "name": "Protect Sleepers",
                                        "conditions": [
                                            "victim.isSleeping = 1"
                                        ],
                                        "action": {
                                            "type": "protect"
                                        }
                                    }
                                ]
                            }
                        },
                        
                        "Save the wounded": {
                            "BasePlayer": {
                                "rules": [
                                    {
                                        "name": "Protect the Wounded",
                                        "conditions": [
                                            "victim.isWounded = 1"
                                        ],
                                        "action": {
                                            "type": "protect"
                                        }
                                    }
                                ]
                            }
                        },
                        
                        "Admin Gods": {
                            "BasePlayer": {
                                "rules": [
                                    {
                                        "name": "Protect Admins",
                                        "conditions": [
                                            "victim.isAdmin = 1"
                                        ],
                                        "action": {
                                            "type": "protect"
                                        }
                                    }
                                ]
                            }
                        },
                        
                        "Locked means no": {
                            "Door": {
                                "rules": [
                                    {
                                        "name": "Protect doors",
                                        "conditions": [
                                            "victim.locked = 1"
                                        ],
                                        "action": {
                                            "type": "protect"
                                        }
                                    }
                                ]
                            },
                            "StorageContainer": {
                                "rules": [
                                    {
                                        "name": "Protect storage containers",
                                        "conditions": [
                                            "victim.locked = 1"
                                        ],
                                        "action": {
                                            "type": "protect"
                                        }
                                    }
                                ]
                            }
                        },
                        
                        "Light PVP": {
                            "BasePlayer": {
                                "rules": [
                                    {
                                        "name": "Protect Player at Home",
                                        "conditions": [
                                            "attacker.GetType = BasePlayer",
                                            "attacker.CanBuild = 0",
                                            "victim.CanBuild = 1"
                                        ],
                                        "action": {
                                            "type": "protect"
                                        }
                                    },
                                    {
                                        "name": "Protect the Wounded",
                                        "conditions": [
                                            "victim.isWounded = 1",
                                            "attacker.CanBuild = 0"
                                        ],
                                        "action": {
                                            "type": "protect"
                                        }
                                    },
                                    {
                                        "name": "Protect Sleepers",
                                        "conditions": [
                                            "victim.isSleeping = 1",
                                            "attacker.CanBuild = 0"
                                        ],
                                        "action": {
                                            "type": "protect"
                                        }
                                    }
                                ]
                            },
                            "Door": {
                                "rules": [
                                    {
                                        "name": "Protect Doors at Home",
                                        "conditions": [
                                            "attacker.GetType = BasePlayer",
                                            "attacker.CanBuild = 0"
                                        ],
                                        "action": {
                                            "type": "protect"
                                        }
                                    }
                                ]
                            },
                            "StorageContainer": {
                                "rules": [
                                    {
                                        "name": "Protect Locked Storage Containers at Home",
                                        "conditions": [
                                            "attacker.GetType = BasePlayer",
                                            "attacker.CanBuild = 0",
                                            "victim.locked = 1"
                                        ],
                                        "action": {
                                            "type": "protect"
                                        }
                                    }
                                ]
                            },
                            "BuildingBlock": {
                                "rules": [
                                    {
                                        "name": "Protect Buildings",
                                        "conditions": [
                                            "attacker.GetType = BasePlayer"
                                        ],
                                        "action": {
                                            "type": "protect"
                                        }
                                    }
                                ]
                            },
                            "BuildingPrivlidge": {
                                "rules": [
                                    {
                                        "name": "Protect Tool Cupboards",
                                        "conditions": [
                                            "victim.GetType = BuildingPrivlidge",
                                            "attacker.CanBuild = 0"
                                        ],
                                        "action": {
                                            "type": "protect"
                                        }
                                    }
                                ]
                            }
                        },
                        
                        "PVE": {
                            "BasePlayer": {
                                "rules": [
                                    {
                                        "name": "Protect Players",
                                        "conditions": [
                                            "attacker.GetType = BasePlayer"
                                        ],
                                        "action": {
                                            "type": "protect"
                                        }
                                    }
                                ]
                            },
                            "Door": {
                                "rules": [
                                    {
                                        "name": "Protect Door",
                                        "conditions": [
                                            "attacker.GetType = BasePlayer",
                                            "attacker.CanBuild = 0"
                                        ],
                                        "action": {
                                            "type": "protect"
                                        }
                                    }
                                ]
                            },
                            "StorageContainer": {
                                "rules": [
                                    {
                                        "name": "Protect Storage Containers",
                                        "conditions": [
                                            "attacker.GetType = BasePlayer",
                                            "attacker.CanBuild = 0"
                                        ],
                                        "action": {
                                            "type": "protect"
                                        }
                                    }
                                ]
                            },
                            "BuildingBlock": {
                                "rules": [
                                    {
                                        "name": "Protect Buildings",
                                        "conditions": [
                                            "attacker.GetType = BasePlayer",
                                            "attacker.CanBuild = 0"
                                        ],
                                        "action": {
                                            "type": "protect"
                                        }
                                    }
                                ]
                            },
                            "BuildingPrivlidge": {
                                "rules": [
                                    {
                                        "name": "Protect Tool Cupboards",
                                        "conditions": [
                                            "victim.GetType = BuildingPrivlidge",
                                            "attacker.CanBuild = 0"
                                        ],
                                        "action": {
                                            "type": "protect"
                                        }
                                    }
                                ]
                            }
                        },
                        
                        "PVP": {}
                        
                    }
                };
                
                this.chatCommands   = {
                    "dmg": "damageCMD"
                };
                
                this.consoleCommands   = {
                    "mode": "damageCMD"
                };
                
                return this;  
            };
            
            return this.init();  
        };
        
        
        /**
         * Home (Server Host) Location
         * Checks in with server so
         */
        this.location       = function() {
              return "http://rust.kuhlonline.com/dmg?mod=" + this.pluginName + "&version=" + pluginVersion + "&protorust=" + engineVersion;
        };
        
    
    /** Extension Handlers */
    
        /**
         * Load Extensions
         */
        this.loadExtensions = function() {
            if (!this.ready) return;
            
            this.extensions['extTime']  = new extTime(engine);
            engine.logger.debug("Loaded Engine Extension: Time", 2);
            
            this.extensions['pvx']      = new pvx(engine);
            engine.logger.debug("Loaded Engine Extension: PVX ", 2);
            
            this.extensions['extPopupNotifications']    = new extPopupNotifications(engine);
            //engine.logger.debug("Loaded Engine Extension: PopupNotifications ", 2);
            
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
        return this.init(oxide_plugin, oxide_config, oxide_rust, oxide_data, dmgController, pluginName);
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
 * ProtoRust Action Extension
 */
var extAction   = function(action) {
    
    this.type       = '';
    this.action     = {};
    this.returnCode = false;
  
    this.init       = function(action) {
        this.action = action;
        this.type   = (this.action['type']) 
            ? this.action['type'].trim().toLowerCase() 
            : ''
        ;
        
        return this.execute();
    };
    
    this.execute    = function() {        
        switch (this.type) {
            
            case 'popup':
            break;
            
            case 'chat':
            break;
            
            case 'command':
            break;
            
            case 'api':
            break;
            
            case 'protect':
                this.returnCode     = true;
            break;
            
            default:
            case 'damage':
            case 'return':
                this.returnCode     = false;
            break;
        };
        
        return this.returnCode;
    };
    
    return this.init(action);  
};


/**
 * ProtoRust Time Extension
 * 
 * Gets information releated to time from Rust to 
 * provide time of day or if it's day or night
 * @param [ProtoRust] engine
 */
var extTime      = function(engine) {
    
    /**
     * Rust TOD_Sky Object
     * @type {Global.TOD_Sky}
     * @type sky
     */
    this.sky            = {};
    
    this.tod            = '';
    
    
    /**
     * Event Controller Interface Function 
     * @param [String] eventName Name of the event to raise
     * @param [Object] param Arguments for the callback function
     */
    this.raise          = function(eventName, param) {
        var funcName    = (this.hooks[eventName]) ? this.hooks[eventName] : '';
        if (!funcName) return;
        
        if (!this[funcName]) return;
        engine.logger.debug("Calling Event Handler: Time " + eventName +"."+ funcName, 6);
        return this[funcName](param);
    };


    /**
     * Initialize Time API
     * @return {Self}
     */
    this.init           = function() {
        var global              = importNamespace(""); 

        if (global['TOD_Sky']['Instance']) {
            this.sky                = global.TOD_Sky.Instance;
            if (this.sky) {
                engine.logger.debug("Sky Found", 3);
                this.tod            = (this.isDay()) ? 'day' : 'night';
            }
        }
        
        return this;
    };


    /**
     * Hour of the day
     * @return {Integer}
     */
    this.hour           = function() {
        if (!this.sky) this.init();
        if (!this.sky['Cycle']) return;
        var hour = parseInt(this.sky.Cycle.Hour);
        return hour;
    };


    /**
     * Time of Day
     * @return {Float}
     */
    this.time           = function() {
        if (!this.sky) this.init();
        if (!this.sky['Cycle']) return;
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
    
    
    this.notifyTime     = function() {
        var popups      = engine.extensions['extPopupNotifications'];
        var mode        = engine.modes.mode;
        var events      = engine.modes.eventConf;
        var msg         = (events[mode]) ? events[mode] : '';
        
        if (!msg) return false;
        var newTod      = (this.isNight()) ? 'night' : 'day';
        if (newTod == this.tod) return false;
        
        var text        = (msg[newTod]) ? msg[newTod] : '';
        if (!text) return false;
        text            = " - " + mode + " - \n" + text;
        
        if (this.tod == 'day' && this.isNight()) {
            this.tod    = newTod;
            popups.notify(text);
        } else if (this.tod == 'night' && this.isDay()) {
            this.tod    = newTod;
            popups.notify(text);
        }
        
        return true;
    };
    
    this.handlers   = [
        'OnTick'
    ];
    
    this.hooks      = {
        'OnTick': 'notifyTime'     
    };
    
    return this.init();
};


/**
 * ProtoRust PVX Extension
 * 
 * Monitors damage given.
 * Uses modeController to store rules in memoery
 * to be evaluated when damage is dealt so it can
 * be allowed or negated based on rule actions
 * @param [ProtoRust] engine
 */
var pvx           = function(protoRust) {
    
    /**
     * @type [ProtoRust]
     */
    this.engine     = {};
  
  
    /**
     * Init
     * Constructed
     * @param [ProtoRust] engine
     */
    this.init       = function(engine) {
        this.engine = engine;
        return this;
    };
    
    
    /**
     * Config
     * Static config for exposed object list
     */
    this.config     = {
        'victim':   {
            'BasePlayer': true,
            'BaseNPC': true,
            'Door': true,
            'StorageContainer': true,
            'BuildingBlock': true,
            'BuildingPrivlidge': true
        },
        'attacker': {
            'BasePlayer': true,
            'BaseNPC': true,
            'BaseHelicopter': true
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
        'OnEntityTakeDamage':   'evalDamage'
    };
    
    
    /**
     * Event Controller Interface Function 
     * @param [String] eventName Name of the event to raise
     * @param [Object] param Arguments for the callback function
     */
    this.raise      = function(eventName, param) {
        var funcName    = (this.hooks[eventName]) ? this.hooks[eventName] : '';
        
        if (!funcName)          return;
        if (!this[funcName])    return;
        
        this.engine.logger.debug("Calling Event Handler: PVP " + eventName +"."+ funcName, 6);
        return this[funcName](param);
    };
    
    
    /**
     * Validate
     * Filters out any entity that cannot be evaluated
     * @param [String] attackerType entity object type
     * @param [String] VictimType entity object type 
     */
    this.validate   = function(attackerType, victimType) {
        //Get Attacker Entities List
        var entities    = (this.config['attacker'])
            ? this.config['attacker']
            : []
        ;
        
        //Make sure the attacker type is on the list
        if (!entities) {
            this.engine.logger.debug("No attacker Entities in config", 4);
            return false;
        }
        
        if (!entities[attackerType]) {
            this.engine.logger.debug(attackerType  + " attacker not on the list, skipping", 4);
            return false;
        }
        
        //Get Victim Entities List
        var entities    = (this.config['victim'])
            ? this.config['victim']
            : []
        ;
        
        //Make sure the victim type is on the list
        if (!entities) {
            this.engine.logger.debug("No victims Entities in config", 4);
            return false;
        }
        
        if (!entities[victimType]) {
            this.engine.logger.debug(victimType  + " victim not on the list, skipping", 4);
            return false;
        }
        
        return true;
    };
    
    
    /**
     * Victim
     * Creates an exposed Entity Object for evaluation
     * @param [Object] entity  
     * @param [String] type
     */
    this.victim     = function(victim, type) {
        var canBuild    = (victim['CanBuild']) ? victim.CanBuild() : false;
        var isAdmin     = (type == 'BasePlayer' && victim.IsAdmin());
        var isSleeping  = (type == 'BasePlayer' && victim.IsSleeping());
        var isWounded   = (type == 'BasePlayer' && victim.IsWounded());
        
        if (victim['GetSlot']) {
            var lock        = victim.GetSlot(0);
            if (!lock) lock = false;
            
            var locked      = (!lock['IsLocked']) ? false : lock.IsLocked();    
        } else {
            var locked      = false;
        }
        
        return {
            'CanBuild': canBuild,
            'isAdmin': isAdmin,
            'isSleeping': isSleeping,
            'isWounded': isWounded,
            'locked': locked,
            'GetType': type
        };
    };
    
    
    /**
     * Attacker
     * Creates an exposed Entity Object for evaluation
     * @param [Object] entity 
     * @param [String] type
     */
    this.attacker   = function(attacker, type) {
        var canBuild    = (attacker['CanBuild']) ? attacker.CanBuild() : false;
        var isAdmin     = (type == 'BasePlayer' && attacker.isAdmin());
        
        return {
            'CanBuild': canBuild,
            'isAdmin': isAdmin,
            'GetType': type
        };
    };
    
    
    /**
     * Area
     * Creates an exposed Entity Object for evaluation
     * @param [Object] entity Attacker entity object 
     * @param [Object] entity Victim entity Object
     */
    this.area       = function(attacker, victim) {
        var aCanBuild               = (attacker['CanBuild']) ? attacker.CanBuild() : false;
        var vCanBuild               = (victim['CanBuild']) ? victim.CanBuild() : false;
        
        if (!attacker) {
            var friendly            = vCanBuild;
            var privateProperty     = (vCanBuild == false);
        } else {
            var friendly            = (aCanBuild && vCanBuild);
            var privateProperty     = ((aCanBuild) == false && vCanBuild == false);
        }
        
        return {
            "-exp-contested": friendly,
            "-exp-friendly": friendly,
            "-exp-private": privateProperty
        };
    };
    
    
    /**
     * Environment
     * Creates an exposed Entity Object for evaluation
     */
    this.env        = function() {
        var tm  = this.engine.extensions['extTime'];
        
        return {
            'hour': tm.hour(),
            'isNight': tm.isNight(),
            'isDay': tm.isDay()   
        };
    };
    
    
    /**
     * Eval Damage
     * EventHandler
     * Evaluates rules in DamageController Mode to either take action or not
     * based on conditions
     * 
     * @param [Object] param
     */
    this.evalDamage = function(param) {
        var entity  = param['entity'];
        var hitInfo = param['hitInfo'];
        
        this.engine.logger.debug("Evaluating Damage", 6);
        
        //Make sure there is HitInfo before continuing
        if (!hitInfo || !hitInfo['Initiator']) {
            return new eventReturn(false, false);
        }
        
        //Make sure there is a victim
        if (!entity) {
            return new eventReturn(false, false);
        }
        
        //Get Attacker if any. If not return
        var attacker        = hitInfo.Initiator;
        if (!attacker)      return new eventReturn(false, false);
        
        //Get Victim
        var victim          = entity;
        if (!victim)        return new eventReturn(false, false);
        
        
        
        //Get Attacker and Victim Types
        var attackerType    = attacker.GetType();
        var victimType      = entity.GetType();
        
        this.engine.logger.debug("Checking " + attackerType + " attacking " + victimType, 5);
        
        //Validate Attacker and Victim Types
        if (!this.validate(attackerType, victimType)) {
            return new eventReturn(false, false);
        }
        
        this.engine.logger.debug("Evaluating " + attackerType + " attacking " + victimType, 4);
        
        //Create Public objects for evaluation
        //prevents rules from exposing all properties and methods
        var publicObjects   = {
            'victim':   this.victim(victim, victimType),
            'attacker': this.attacker(attacker, attackerType),
            'env':      this.env(),
            'area':     this.area(attacker, victim)    
        };
        
        
        //Get Rules for current mode
        var modeRules       = this.engine.modes.modeConf;
        if (!modeRules) {
            this.engine.logger.debug("No Mode Rules found", 4);
            return new eventReturn(false, false);
        }
        
        //get rules specific to victim type
        var victimRules     = [];
        if (modeRules[victimType] && modeRules[victimType]['rules']) {
            victimRules     = modeRules[victimType]['rules'];
        }
          
        if (!victimRules) {
            this.engine.logger.debug("No Victim Rules found for " + victimType, 4);
            return new eventReturn(false, false);
        }
        
        //Set loop break triggers to default
        var stopRule        = -1;
        var stopCondition   = -1 
        var evalResult      = -1;       
        
        for (var ruleIndex in victimRules) {
            var rule        = victimRules[ruleIndex];
            var ruleName    = (rule['name']) ? rule['name'] : '';
            var conditions  = (rule['conditions']) ? rule['conditions'] : [];
            var action      = (rule['action']) ? (rule['action']) : {};
            
            this.engine.logger.debug("\tRule: " + ruleName, 3);
            
            if (!rule || !ruleName || !conditions || !action) continue;
            if (stopRule === true) this.engine.logger.debug("\tStopping Rule Eval", 4);
            if (stopRule === true) break;
            if (stopRule == -1) stopRule = false;
            stopCondition       = -1;
            
            for (var conditionIndex in conditions) {
                
                if (stopCondition === true) this.engine.logger.debug("\tStopping Condition Evaluation", 4);
                if (stopCondition === true) break;
                if (stopCondition === -1) stopCondition = false;
                
                var condition   = (conditions[conditionIndex]);
                if (!condition) {
                    this.engine.logger.debug("No conditions found", 4);
                    continue;
                }
                
                var parts       = condition.trim().split(" ", 3);
                var target      = parts[0];
                var op          = (parts[1]) ? parts[1].trim() : '=';
                var evalValue   = (parts[2]) ? parts[2].trim() : '';               
                if (!target || !evalValue) continue;
                
                parts           = target.split(".", 2);
                var key         = parts[0].toLowerCase();
                var property    = (parts[1]) ? parts[1] : '';
                if (!key || !property) continue;
                
                var compValue   = (publicObjects[key][property]) ? publicObjects[key][property] : '';
                
                if (!compValue) continue;
                
                evalResult      = this.evalCondition(evalValue, compValue, op);
                this.engine.logger.debug("\t((" + key +"."+ property +"="+ evalValue +") "+ op +" "+ compValue +") = "+ evalResult, 3);
                
                if (evalResult === false) {
                    stopCondition = true;
                }
            }
            
            if (evalResult === false || evalResult == -1) {
                this.engine.logger.debug("\tConditions not met, taking no action", 3);
                evalResult      = -1
                stopRule        = false;
                continue;
            }
            
            if (evalResult === true) {
                stopRule        = true;
                this.engine.logger.debug("\tConditions met, taking action", 3);
                continue;
            }
            
        }
        
        if (evalResult == true) {
            var aType       = (action['type']) ? action['type'] : 'Unknown';
            this.engine.logger.debug("\tExecuting action: " + aType, 3);
            return new eventReturn(new extAction(action), true);
        } else {
            return new eventReturn(false, true);
        }
    };
        
        
    /**
     * Eval Condition
     * @param [any] evalValue
     * @param [any] compValue
     * @param [String] op
     */
    this.evalCondition      = function(evalValue, compValue, op) {
        var evalResult      = false;
        
        switch (op) {
            
            case '<':
            case '<<':
                evalResult  = (evalValue < compValue);
            break;
            
            case '<=':
                evalResult  = (evalValue <= compValue);
            break;
            
            case '>':
            case '>>':
                evalResult  = (evalValue > compValue);
            break;
            
            case '>=':
                evalResult  = (evalValue >= compValue);
            break;
            
            case '=':
            case '==':
                evalResult  = (evalValue == compValue);
            default:
            break;
        }
        
        return evalResult;
    };
    
    
    /**
     * Construct and return instance
     */
    return this.init(protoRust);
};


var extPopupNotifications   = function(engine) {
  
    this.instance   = {};
    this.ready      = false;
    
    this.init       = function() {
        this.instance   = plugins.Find("PopupNotifications");
        this.ready      = (!this.instance) ? false : true;
        
        if (this.ready) {
            engine.logger.debug("PopupNotifications Plugin Found", 2);
        }
        
        return this;
    };
    
    this.notify     = function(msg) {
        if (!this.ready) return this;
        
        this.instance.CallHook("CreatePopupNotification", msg);
        return this;  
    };
    
    return this.init();
};


/**
 * DamageController
 * 
 * Minimal plugin object for OxideMod to intialize the ProtoRust
 * and hook specific events throw by OxideMod
 * 
 * OxideMod Plugin
 * @version 0.2.5
 * @author VisionMise
 */
var DamageController = {
    
    Title:              "DamageController",
    Author:             "VisionMise",
    Version:            V(0, 2, 5),
    ResourceId:         1841,
    HasConfig:          true,
    
    engine:             false,
    ready:              false,
    
    Init:               function() {
        
        //Create new Engine
        this.engine     = new ProtoRust(
            this.Plugin,
            this.Config,
            rust,
            data,
            this,
            'DamageController'
        );
        
        //Plugin is Ready
        this.ready      = true;
        
        
        //Add Chat Commands
        var cmds        = this.engine.bootstrap().chatCommands;
        for (var cmd in cmds) {
            command.AddChatCommand(cmd, this.Plugin, 'OnChatCommand');
            this.engine.logger.debug("Added chat command: " + cmd, 3);
        }
        
        //Add Console Commands
        var cnls        = this.engine.bootstrap().consoleCommands;
        for (var cnl in cnls) {
            command.AddConsoleCommand("DamageController." + cnl, this.Plugin, 'OnConsoleCommand');
            this.engine.logger.debug("Added console command: " + cnl, 3);
        }
        
        //Reload Config
        this.engine.config  = new this.engine.configController(this.Config); 
        this.engine.logger.send("DamageController done loading");
        
        //Track Plugin Usage
        webrequests.EnqueueGet(this.engine.location(), function(code, response) {
            if (response == null || code != 200) return;
            
            this.engine.logger.debug(response, 3);
        }.bind(this), this.Plugin);
        
    },
    
    OnEntityTakeDamage: function(entity, hitInfo) {
        if (!this.ready || !this.engine) return;
        return this.engine.events.raiseEvent('OnEntityTakeDamage', {'entity': entity, 'hitInfo': hitInfo});
    },
    
     OnTick: function() {
        if (!this.ready || !this.engine) return;
        return this.engine.events.raiseEvent('OnTick', {});
    },
    
    OnChatCommand: function(player, cmd, arg) {
        var cmds        = this.engine.bootstrap().chatCommands;
        var cmdName     = (cmds[cmd])
            ? cmds[cmd]
            : null
        ;
        
        this.engine.config  = new this.engine.configController(this.Config); 
        this.engine.logger.debug("Chat Command: " + cmd + " => " + cmdName, 3);
        
        if (!cmdName) return;
        if (this.engine.commands[cmdName]) {
            this.engine.commands[cmdName](arg, player);
            this.engine.logger.debug("Executed " + cmdName, 4);
        }
        
        return;
    },
    
    OnConsoleCommand: function(arg) {
        var cmds        = this.engine.bootstrap().consoleCommands;
        var result      = null;
        var cmdName     = (cmds[arg.Cmd.name])
            ? cmds[arg.Cmd.name]
            : null
        ;
        
        var param       = (arg.Args) ? arg.Args : false;
        if (!param) {
            param       = [arg.Cmd.name];
        } else {
            param.unshift(arg.Cmd.name);
        }
        
        this.engine.config  = new this.engine.configController(this.Config); 
        this.engine.logger.debug("Console Command: " + cmdName, 3);
        
        if (!cmdName) return;
        if (this.engine.commands[cmdName]) {
            result  = this.engine.commands[cmdName](param, false);
            this.engine.logger.debug("Executed " + cmdName, 4);
        }
        
        return result;
    }
};