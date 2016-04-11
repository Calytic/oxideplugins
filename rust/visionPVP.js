/**
 * VisionPVP
 *
 * @author          VisionMise
 * @version         0.4.2
 * @description     Please README.md for More Information
 * @url             http://visionmise.github.io/visionPVP/
 */


var engineVersion   = '0.4.2';
var configVersion   = '1.6.7';


/**
 * visionPVP Engine
 * 
 * @param  {Oxide.Plugin}   pluginObject
 * @param  {Oxide.Config}   configObject
 * @param  {Oxide.rust}     rust
 * @param  {Oxide.Data}     data
 * @param  {String}         prefix
 * @param  {String}         version
 * @param  {Oxide.Object}   interop
 * 
 * @return {visionPVP_engine}
 */
var visionPVP_engine                = function(pluginObject, configObject, rust, dataObject, prefix, version, interop) {

    /**
     * Prefix
     * @type {String}
     */
    this.prefix         = '';


    /**
     * Time API
     * @type {visionPVP_time_object}
     */
    this.time           = {};

    
    /**
     * Global Rust Object
     * @type {Oxide.Rust}
     */
    this.rust           = {};


    /**
     * Global Plugin Object
     * @type {Oxide.Plugin}
     */
    this.plugin         = {};


    /**
     * Global Oxide Object
     * @type {Oxide.Object}
     */
    this.interop        = {};


    /**
     * Global Config Object
     * @type {Oxide.Config}
     */
    this.config         = {};


    /**
     * Global Data Object
     * @type {Oxide.DataFile}
     */
    this.data           = {};


    /**
     * Permanent Storage
     * @type {visionPVP_data}
     */
    this.store          = {};


    /**
     * Current PVP Mode
     * @type {visionPVP_pvpmode_type}
     */
    this.pvpMode        = {};


    /**
     * Is Ready
     * @type {Boolean}
     */
    this.ready          = false;


    /**
     * Current Extended Mode Handler
     * @type {Object}
     */
    this.handler        = false;


    /**
     * VisionPVP Version
     * @type {String}
     */
    this.version        = '';


    /**
     * String Resources
     * @type {visionPVP_resource}
     */
    this.resources      = {};


    /**
     * Event Controller
     * @type {visionPVP_eventController}
     */
    this.eventController= {};


    /**
     * LoadConfig
     * @return {boolean} True if config was built
     */
    this.loadConfig     = function() {
        this.config.Settings    = this.config.Settings || false;

        //Build
        if (!this.config.Settings) {
            this.config.Settings = this.buildConfig();
            this.interop.SaveConfig();
            this.console("Created new Config version " + configVersion);
            return true;
        }

        //Upgrade
        if (this.config.Settings['config'] != configVersion) {
            var tmpConfig   = this.buildConfig();

            for (var key in tmpConfig) {
                var curVal     = this.config.Settings[key];
                var newVal      = tmpConfig[key];

                if (!curVal) {
                    this.config.Settings[key]   = newVal;
                } else if (key == 'config' || key == 'engine') {
                    this.config.Settings[key]   = newVal;
                }

            }
            
            this.interop.SaveConfig();
            this.console("Updated Config to version " + configVersion);
            return true;
        }

        return false;
    };


    /**
     * BuildCOnfig
     * @return {Object} Returns the default config object
     */
    this.buildConfig    = function() {

        var config      = {
            "version":      engineVersion,
            "config":       configVersion,
            "pvpMode":      "event",
            "pveMode":      "vision",
            "random":       {
                "minimum":          "1",
                "maximum":          "12",
                "player_warning":   "2"
            },
            "pvptime":      {
                'pvp_start_time':   "18",
                'pvp_stop_time':    "6"
            },
            "event": 		{
            	"pvp_event_mode":	"pvp",
            	"pvp_duration": 	"2"
            }
        };
        return config;
    };


    /**
     * init
     *
     * Kuhl PVP API Initialization 
     * @param  {Oxide.Plugin}    pluginObject
     * @param  {Oxide.Config}    configObject
     * @param  {Oxide.rust}      rust
     * @param  {String}          prefix
     * @return {this}
     */
    this.init           = function(pluginObject, configObject, rust, dataObject, prefix, version, interop) {

        /**
         * Name
         */
        this.prefix     = prefix;


        /**
         * Oxide Config Object
         * @type {Oxide.Config}
         */
        this.config     = configObject;


        /**
         * Oxide Hook Object
         * @type {Oxide.Object}
         */
        this.interop    = interop;
        

        /**
         * Oxide Plugin Object
         * @type {Oxide.Plugin}
         */
        this.plugin     = pluginObject;


        /**
         * Load Configuration
         */
        this.loadConfig();


        /**
         * Oxide Rust Object
         * @type {Oxide.Rust}
         */
        this.rust       = rust;


        /**
         * Oxide Data Object
         * @type {Oxide.DataFile}
         */
        this.data       = dataObject;        
        

        /**
         * String Resources
         * @type {visionPVP_resource}
         */
        this.resources  = new visionPVP_resource(this);


        /**
         * Print Startup
         */
        this.console(this.prefix + ' ' + this.resources.get('console', 'started'));


        /**
         * Event Controller
         * @type {visionPVP_eventController}
         */
        this.eventController    = new visionPVP_eventController(this);


        /**
         * Permanent Storage
         * @type {visionPVP_data}
         */
        this.store      = new visionPVP_data(this);


        /**
         * VisionPVP Engine Version
         * @type {String}
         */
        this.version    = version;
        

        this.startTime();


        /**
         * Set PVP Mode
         */
        var pvpModeStr  = this.config.Settings['pvpMode'];
        this.pvpMode    = new visionPVP_pvpmode_type(pvpModeStr);
        this.console(
            this.prefix                                 +
            ' '                                         +
            this.resources.get('console', 'mode_set')   +
            ' '                                         +
            this.pvpMode.label
        );

        /**
         * Check PVE Settings
         */
        var mode        = this.checkPVPMode();
        this.console(
            this.resources.get('console', 'PVE_label')  +
            ': '                                        +
            mode
        );



        
        /**
         * Return this object
         */
        return this;
    };


    this.startTime       = function() {
        /**
         * Time API
         * @type {visionPVP_time_object}
         */
        this.time       = new visionPVP_time_object();

         /**
         * Set Ready
         */
        this.ready      = true;
        interop.ready   = true;
    };


    /**
     * Tick Event
     * @return {Void}
     */
    this.timerTick      = function() {
        if (this.ready == false || this.interop.ready == false) return false;
        this.checkPVPMode();
    };


    /**
     * CHeck the PVP Mode Settings
     * @return {Integer} PVE Setting Value
     */
    this.checkPVPMode   = function() {
        var pve         = this.serverPveMode();
        var msg         = '';

        switch (this.pvpMode.name) {

            case 'pvp-night':
                if (this.time.isDay()) {
                    if (!pve) {
                        msg     = this.resources.get('chat', 'pvp-night_off');
                        this.serverPveSet(1, msg);
                        this.console(msg);
                    }
                } else {
                    if (pve) {
                        msg     = this.resources.get('chat', 'pvp-night_on');
                        this.serverPveSet(0, msg);
                        this.console(msg);
                    }
                }
            break;

            case 'pvp-day':
                if (this.time.isNight()) {
                    if (!pve) {
                        msg     = this.resources.get('chat', 'pvp-day_off');
                        this.serverPveSet(1, msg);
                        this.console(msg);
                    }
                } else {
                    if (pve) {
                        msg     = this.resources.get('chat', 'pvp-day_on');
                        this.serverPveSet(0, msg);
                        this.console(msg);
                    }
                }
            break;

            case 'pvp':
                if (pve == 1) {
                    msg         = this.resources.get('chat', 'pvp');
                    this.serverPveSet(0, msg);
                    this.console(msg);
                }
            break;

            case 'pve':
                if (pve == 0) {
                    msg         = this.resources.get('chat', 'pve');
                    this.serverPveSet(1, msg);
                    this.console(msg);
                }
            break;

            case 'random':
                if (!this.handler || this.handler.type != 'visionPVP_random_handler') {
                    this.handler    = new visionPVP_random_handler(this.store, this);
                }

                var mode        = this.handler.mode();

                this.serverPveSet(mode, this.handler.msg);
            break;

            case 'time':
                if (!this.handler || this.handler.type != 'visionPVP_pvptime_handler') {
                    this.handler    = new visionPVP_pvptime_handler(this.store, this);
                }

                var mode        = this.handler.mode();
                if (!mode) mode = 0;
                mode        = parseInt(mode);

                this.serverPveSet(mode, this.handler.msg);
            break;

            case 'event':
                if (!this.handler || this.handler.type != 'visionPVP_event_handler') {
                    this.handler    = new visionPVP_event_handler(this.store, this);
                }

                var mode        = this.handler.mode();

                if (!mode) mode = 0;
                mode        = parseInt(mode);
                this.serverPveSet(mode, this.handler.msg);
            break;

        }

        return this.serverPveMode();
    };


    /**
     * Server PVE Mode
     * @return {Boolean}
     */
    this.serverPveMode      = function() {
        var vpve                = this.config.Settings['pveMode'];
        var mode                = (vpve == 'vision') ? this._getVisionPVEMode() : this._getServerPVEMode();

        return mode;
    };


    /**
     * Get the PVE mode from the Rust Server Variable
     * @return {Integer}
     */
    this._getServerPVEMode  = function() {
        var global              = importNamespace("ConVar");
        var server              = global.Server;

        if (!server || typeof server == 'undefined') {
            this.exception(100);
            return -1;
        }

        var mode                = server.pve;

        if (typeof mode == 'undefined') {
            this.exception(101);
            return -1;
        }

        
        return server.pve;
    };

    this._setServerPVEMode  = function(newMode, reason, skipBroadcast) {
        var global              = importNamespace("ConVar");
        var server              = global.Server;
        var oldMode             = this.serverPveMode();

        if (oldMode == -1) {
            this.exception(102);
            return 0;
        }

        if (!server || typeof server == 'undefined') {
            this.exception(100);
            return 0;
        }

        if (newMode == oldMode) {
            return oldMode;
        }


        server.pve              = newMode;
        var currentMode         = server.pve;
        var changed             = (oldMode != currentMode);

        if (skipBroadcast == true) return currentMode;
        if (changed) {
            this.broadcast(reason);
            this.console(
                this.resources.get('console', 'mode_set')   + 
                ' '                                         + 
                currentMode
            );

            if (server.pve == 1) {
                var expl            = this.resources.get('chat', 'pve_expl');
                this.broadcast(expl);               
            } else {
                var expl            = this.resources.get('chat', 'pvp_expl');
                this.broadcast(expl);               
            }
        }

        return currentMode;
    };

    this._setVisionPVEMode  = function(newMode, reason) {
        var oldMode         = parseInt(this.store.get("pveMode"));
        var newMode         = parseInt(newMode);

        if (oldMode == newMode) return oldMode;

        var curMode         = this.store.set("pveMode", newMode);
        var changed         = (curMode != oldMode);
        
        if (changed) {
            this.broadcast(reason);
            this.console(
                this.resources.get('console', 'mode_set')   + 
                ' '                                         + 
                curMode
            );

            if (curMode == 1) {
                var expl            = this.resources.get('chat', 'pve_expl');
                this.broadcast(expl);               
            } else {
                var expl            = this.resources.get('chat', 'pvp_expl');
                this.broadcast(expl);               
            }

            this._setServerPVEMode(0, "", true);
        }

        return curMode;
    };


    /**
     * Get the PVE mode from the OxideMod Data object
     * @return {[type]} [description]
     */
    this._getVisionPVEMode  = function() {
        vpve_mode           = this.store.get('pveMode');

        if (vpve_mode != 0 && vpve_mode != 1) {
            vpve_mode       = 1;
            this.store.set('pveMode', 1);
        }

        return vpve_mode;
    };


    /**
     * Server PVP Mode
     * @return {visionPVP_pvpmode_type}
     */
    this.serverPvpMode      = function() {
        return this.pvpMode;
    };


    /**
     * Set Server PVE Mode
     * @param  {Integer} newMode PVE Mode
     * @return {Boolean} Global.Server.pve
     */
    this.serverPveSet       = function(newMode, reason) {

        var vpve                = this.config.Settings['pveMode'];

        if (vpve == 'vision') {
            var mode        = this._setVisionPVEMode(newMode, reason);
        } else {
            var mode        = this._setServerPVEMode(newMode, reason);
        }

        return mode;
    };


    /**
     * pvpSet
     * @param  {String}         newMode 
     * @return {Boolean}        True on Success
     */
    this.pvpSet         = function(newMode) {
        var mode        = new visionPVP_pvpmode_type(newMode);

        if (this.updateConfig('pvpMode', mode.name)) {
            var msg         = (
                this.resources.get('console', 'config_set') + 
                ' '                                         +
                mode.label
            );

            this.pvpMode    = mode;
            this.console(msg).broadcast(msg);

            return true;
        }

        return false;
    };


    /**
     * updateConfig
     * @param  {String}     key
     * @param  {Mixed}      value
     * @return {Boolean}    True on Update
     */
    this.updateConfig       = function(key, value) {
        if (!key || !value) return false;

        this.config.Settings[key]   = value;
        return true;
    };


    /**
     * Console
     * Prints to Server Console
     * @param  {String}
     * @return {visionPVP_engine} self
     */
    this.console        = function(text) {
        print(this.prefix + ": " + text);
        return this;
    };


    /**
     * Exception
     * Prints an error by number to the console
     * @param  {Integer} errorNum
     * @return {visionPVP_engine} self
     */
    this.exception      = function(errorNum) {
        var msg = this.resources.get('error', errorNum);
        print("! " + this.prefix + "-Error [" + errorNum + "]: " + msg);
        return this;
    };


    /**
     * Broadcast Global Chat
     * @param  {String} text 
     * @return {visionPVP_engine} self
     */
    this.broadcast      = function(text) {
        if (this.rust['BroadcastChat']) {
            this.rust.BroadcastChat(this.prefix, text);
            //print("[Broadcast: VisionPVP] " + text);
        }

        return this;    
    };


    /**
     * Return this as an initialized object
     */
    return this.init(pluginObject, configObject, rust, dataObject, prefix, version, interop);
};


/**
 * Event Controller
 * @param  {visionPVP_engine}           engine
 * @return {visionPVP_eventController}  self
 */
var visionPVP_eventController       = function(engine) {

    /**
     * Engine
     * @type {visionPVP_engine}
     */
    this.engine             = {};


    /**
     * Hooks 
     * @type {Object}
     */
    this.hooks              = {};


    /**
     * Initialize
     * @param  {visionPVP_engine}   engine
     * @return {visionPVP_eventController} self
     */
    this.init               = function(engine) {
        this.engine         = engine;
        return this;
    };


    /**
     * Register Hooks
     * @param  {Object} visionPVP object
     * @return {visionPVP_eventController} self
     */
    this.registerHooks      = function(object) {
        if (!object || !object.hooks) return false;

        var hooks   = object.hooks;
        for (var eventName in hooks) {
            var callback    = hooks[eventName];
            if (!this.hooks[eventName]) this.hooks[eventName] = {};
            this.hooks[eventName][this.hooks.length]    = callback;
        }

        return this;
    };


    /**
     * Raise Event
     * @param  {string}     eventName
     * @param  {object}     data
     * @return {array}      result list
     */
    this.raiseEvent         = function(eventName, data) {
        if (!this.hooks || !this.hooks[eventName]) return false;

        var callbackList    = this.hooks[eventName];
        var results         = {};

        for (var index in callbackList) {
            var callback            = callbackList[index];
            results[results.length] = callback(data, this.engine);
        }

        return results;
    };


    /**
     * return self
     */
    return this.init(engine);
};


/**
 * VisionPVP Data Handler
 * @param  {visionPVP_engine} engine 
 * @return {visionPVP_engine} self
 */
var visionPVP_data                  = function(engine) {

    this.prefix             = '';
    this.engine             = {};
    this.table              = {};

    this.init               = function(engine) {
        this.engine         = engine;
        this.prefix         = this.engine.prefix;
        this.table          = this.engine.data.GetData(this.prefix) || {};
        
        return this;
    };

    this.get                = function(key) {
        if (key) return this.table[key];
        return this.table;
    };

    this.set                = function(key, value) {
        var oldValue        = this.table[key];

        if ((oldValue != value)) {
            this.table[key]      = value;
            this.save();
        }
        
        return this.table[key];
    };

    this.save               = function() {
        this.engine.data.SaveData(this.prefix);
    };

    return this.init(engine);
};


/**
 * VisionPVP Time Object
 * @return {visionPVP_time_object}
 */
var visionPVP_time_object           = function() {

    /**
     * Rust TOD_Sky Object
     * @type {Global.TOD_Sky}
     */
    this.sky            = {};


    /**
     * Initialize Time API
     * @return {Void}
     */
    this.init           = function() {
        var global              = importNamespace(""); 

        if (global['TOD_Sky']['Instance']) {
            this.sky                = global.TOD_Sky.Instance;
        }
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


    /**
     * Return this as an initialized object
     */
    return this.init();
};


/**
 * PVP Mode Type
 * @param  {String}
 * @return {visionPVP_pvpmode_type}
 */
var visionPVP_pvpmode_type          = function(typeName) {

    /**
     * Numeric Value of Mode
     * @type {Integer}
     */
    this.value          = 0;


    /**
     * Friendly Name of Mode
     * @type {String}
     */
    this.label          = '';


    /**
     * System Name of Mode
     * @type {String}
     */
    this.name           = '';


    /**
     * Reason
     * @type {String}
     */
    this.reason         = '';


    /**
     * PVE 
     * @type {Boolean}
     */
    this.pve            = false;


    /**
     * Initialize
     * @param  {String} typeName Can be the following:
     * - pvp
     * - pve
     * - pvp-night
     * - pvp-day
     * - random     
     * - time
     * - event      // Not Implemented
     * - interval   // Not Implemented
     * @return {Void}
     */
    this.init           = function(typeName) {
        var compStr     = typeName.toLowerCase().replace(" ", "");

        switch (compStr) {

            case 'pve':
                this.value  = 0;
                this.label  = 'PVE';
                this.name   = 'pve';
                this.pve    = true;
            break;

            case 'pvp-day':
                this.value  = 2;
                this.label  = 'Daytime PVP';
                this.name   = 'pvp-day';
                this.pve    = false;
            break;

            case 'pvp-night':
                this.value  = 3;
                this.label  = 'Nighttime PVP';
                this.name   = 'pvp-night';
                this.pve    = false;
            break;

            case 'pvp':
                this.value  = 1;
                this.label  = 'PVP';
                this.name   = 'pvp';
                this.pve    = false;
            break;

            case 'random':
                this.value  = -1;
                this.label  = 'Random Times';
                this.name   = 'random';
            break;

            case 'interval':
                this.value  = 4;
                this.label  = 'On an Interval';
                this.name   = 'interval';
            break;


            /**
             * @deprecated Missing OxideMod API Event
             */
            case 'event':
                this.value  = 5;
                this.label  = 'On an Event';
                this.name   = 'event';
            break;

            case 'time':
                this.value  = 6;
                this.label  = 'Time Based';
                this.name   = 'time';
                this.pve    = true;
            break;

        }
    };


    /**
     * Return initialized self
     */
    return this.init(typeName);
};


/**
 * Random PVP Mode Handler
 * @param  {visionPVP_data} dataObject API Data
 * @param  {visionPVP_engine} engine
 * @return {visionPVP_random_handler} self
 */
var visionPVP_random_handler        = function(dataObject, engine) {

    this.data           = {};
    this.engine         = {};
    this.msg            = "";
    this.nextHour       = 0;
    this.nextMode       = '';
    this.warned         = false;
    this.type           = 'visionPVP_random_handler';

    this.init           = function(dataObject, engine) {
        this.data       = dataObject;
        this.engine     = engine;

        this.engine.config.Settings['random'] = this.engine.config.Settings['random'] || {
            'minimum':      1,
            'maximum':      12
        };

        var lastHour    = this.data.get("last_change");
        var nextHour    = this.data.get("next_change");

        if (!lastHour)  this.data.set("last_change", -1);
        if (!nextHour)  this.data.set("next_change", -1);
    };

    this.calcHours      = function(nextHour, curHour) {

        if (nextHour == curHour) {
            return 0;
        } else if (nextHour < curHour) {
            return ((nextHour + 24) - curHour);
        } else {
            return (nextHour - curHour);
        }
    };

    this.warnPlayers    = function() {
        if (this.warned) return false;
        var curHour     = engine.time.hour();
        var nextHour    = this.nextHour;

        var pve         = this.engine.serverPveMode();
        var mode        = (pve == 0) ? 'PVE' : 'PVP';

        var hours       = this.calcHours(nextHour, curHour);
        var warning     = this.engine.resources.get('chat', 'rndWarning').replace('%mode%', mode).replace('%hours%', hours);

        this.engine.rust.BroadcastChat(this.engine.prefix, warning);
        this.engine.console(warning);
        this.warned     = true;
    };

    this.mode           = function() {
        var curHour     = engine.time.hour();
        var nextHour    = this.data.get("next_change");

        if (nextHour == -1 || nextHour == false) {
            var pve         = this.engine.serverPveMode();
            var min         = this.engine.config.Settings['random']['minimum'];
            var max         = this.engine.config.Settings['random']['maximum'];
            var rnd         = (Math.floor((Math.random() * max) + min) - 1);

            lastHour        = nextHour;
            nextHour        = (curHour + rnd);
            if (nextHour > 24) nextHour -= 24;
            if (nextHour == lastHour) return pve;

            this.data.set("next_change", nextHour);
            var hours       = this.calcHours(nextHour, curHour);
            var nextMode    = (pve == 0) ? 'PVE' : 'PVP';
            var msg         = this.engine.resources.get('console', 'rnd_hour_set');


            this.nextHour   = nextHour;
            this.nextMode   = nextMode;
            this.warned     = false;
            this.engine.console(msg.replace('%hour%',nextHour));

            var msg = this.engine.resources.get('console', 'rnd_cur_hour');
            this.engine.console(msg.replace('%hour%', curHour));

            return pve;
        } else {
            if (nextHour == curHour) {
                return this.randomize();
            } else {                
                var curHour     = engine.time.hour();
                var nextHour    = this.data.get('next_change');
                var hours       = this.calcHours(nextHour, curHour);

                if (this.engine.config.Settings['random']['player_warning'] && (this.engine.config.Settings['random']['player_warning'] > 0)) {
                    if (parseInt(this.engine.config.Settings['random']['player_warning']) > (parseInt(hours) - 1)) {
                        this.warnPlayers();
                    }
                }

                return this.engine.serverPveMode();
            }
        }
    };

    this.randomize     = function() {
        var pve         = this.engine.serverPveMode();
        var curHour     = engine.time.hour();
        var newMode     = (pve == 0) ? 'PVE' : 'PVP';

        this.data.set("last_change", curHour);
        this.data.set("next_change", -1);

        var msg         = this.engine.resources.get('chat', 'random');
        this.msg        = msg.replace('%mode%', newMode);

        return (pve == 1) ? 0 : 1;
    };

    return this.init(dataObject, engine);
};


/**
 * Time-Based PVP Mode Handler
 * @param  {visionPVP_data}     dataObject 
 * @param  {visionPVP_engine}   engine 
 * @return {visionPVP_pvptime_handler} self
 */
var visionPVP_pvptime_handler       = function(dataObject, engine) {

    this.data           = {};
    this.engine         = {};
    this.msg            = "";
    this.type           = 'visionPVP_pvptime_handler';

    this.start          = 0;
    this.stop           = 0;

    this.init           = function(dataObject, engine) {
        this.data       = dataObject;
        this.engine     = engine;

        this.engine.config.Settings['pvptime'] = this.engine.config.Settings['pvptime'] || {
            'pvp_start_time':   16,
            'pvp_stop_time':    8
        };

        this.start      = parseInt(this.engine.config.Settings['pvptime']['pvp_start_time']);
        this.stop       = parseInt(this.engine.config.Settings['pvptime']['pvp_stop_time']);

        return this;
    };

    this.mode           = function(dataObject, engine) {
        var curHour     = this.engine.time.hour();
        var pve         = this.engine.serverPveMode();

        if (this.start > this.stop && curHour > this.stop) {
            var stop    = this.stop + 24;
            var start   = this.start;
            var pvpon   = (curHour >= start && curHour < stop);
        } else {
            var stop    = this.stop;
            var start   = this.start;
            var pvpon   = (curHour >= start || curHour < stop);
        }

        var pveMode     = (pvpon == false);

        if (pve != pveMode) {
            if (!pveMode) {
                this.msg    = this.engine.resources.get('chat', 'pvp_start');
            } else {
                this.msg    = this.engine.resources.get('chat', 'pvp_stop');
            }

            this.engine.resources.get('console', 'pvp_time');
        }

        return pveMode;
    };

    return this.init(dataObject, engine);
};


/**
 * AirDrop Handler
 * @deprecated  Missing Event from Oxide API
 * @param  {visionPVP_data}     dataObject
 * @param  {visionPVP_engine}   engine
 * @return {visionPVP_event_handler}
 */
var visionPVP_event_handler         = function(dataObject, engine) {

    this.engine         = {};
    this.data           = {};
    this.type           = 'visionPVP_event_handler';
    this.hooks          = {};
        
    this.pveMode        = 1;
    this.length         = 0;
    this.msg            = '';
    this.lastChange     = -1;
    this.switched       = false;

    this.init           = function(dataObject, engine) {
        this.engine     = engine;
        this.data       = dataObject;
        this.pveMode    = parseInt((this.engine.config.Settings['event']['pvp_event_mode'] != 'pvp'));
        this.length     = parseInt(this.engine.config.Settings['event']['pvp_duration']);

        this.hooks          = {
            'onairdrop':        this.invoke
        };

        this.engine.eventController.registerHooks(this);
    };

    this.mode           = function() {
        var curHour     = this.engine.time.hour();

        this.lastChange = parseInt(this.data.get("last_event"));
        var stop        = (this.lastChange + parseInt(this.length));

        this.switched   = (curHour <= stop);

        if (this.switched) {
            var msg         = this.engine.resources.get('chat', 'event_start');
            var mode        = (this.pveMode == 1) ? 'PVE' : 'PVP';
            this.msg        = msg.replace('%mode%', mode).replace("%hours%", this.length);
        } else {
            var msg         = this.engine.resources.get('chat', 'event_stop');
            var mode        = (this.pveMode != 1) ? 'PVE' : 'PVP';
            this.msg        = msg.replace('%mode%', mode).replace("%hours%", this.length);

        }

        if (this.switched) {
            if (this.pveMode == 1) {
                return 1;
            } else {
                return 0;
            }
        } else{
            if (this.pveMode != 1) {
                return 1;
            } else {
                return 0;
            }
        }
    };

    this.invoke         = function(data, engine) {
        var msg                 = engine.resources.get('console', 'event');
        var lastChange          = engine.time.hour();
        var mode                = (this.pveMode == 1) ? 'PVE' : 'PVP';
         
        engine.store.set('last_event', lastChange);
        engine.console(msg.replace('%mode%', mode));

        return this;
    };

    return this.init(dataObject, engine);
};


/**
 * visionPVP_resource Hanlder
 * @param  {visionPVP_engine} engine parent
 * @return {visionPVP_resource} self
 */
var visionPVP_resource              = function(engine) {


    /**
     * VisionPVP Parent Engine Object
     * @type {Object}
     */
    this.engine         = {};


    /**
     * Engine Version
     * @type {String}
     */
    this.version        = '';


    /**
     * Initialize
     * @param  {visionPVP_engine} engine parent
     * @return {visionPVP_resource} self
     */
    this.init           = function(engine) {
        this.engine     = engine;
        this.version    = engine.version;
        this.configCheck();
    };


    /**
     * Check Config
     * @return {Boolean} False if error
     */
    this.configCheck    = function() {
        this.engine.config.Settings['resources']    = this.engine.config.Settings['resources'] || false;

        if (!this.engine.config.Settings['resources']) {
            this.engine.console("Resources Empty. Updating Resources...");
            return this.buildResources();
        } else {
            var version         = this.engine.config.Settings['resources']['config'];

            if (version != configVersion) {
                this.engine.console("Version Mismatch. Updating Resources...");
                return this.buildResources();
            } 

            return true;
        }
    };


    /**
     * Build Resources
     * @return {Boolean} Always True
     */
    this.buildResources = function() {
        var resources       = {
            'config':               configVersion,
            'console':              {
                'started':          'started PVP/PVE Tracking',
                'mode_set':         'PVE mode set to',
                'PVE_label':        'PVE mode',
                'config_set':       'Server PVP Mode Changed to',
                'build_config':     'Updated Configuration',
                'rnd_hour_set':     'Random Hour Chosen: %hour%',
                'rnd_next_mode':    'The server will change to %mode% in %hours% hours',
                'rnd_cur_hour':     'The current server hour is %hour%',
                'pvp_time':         'Server Time Triggered',
                'event':            'Event triggered mode change to %mode%'
            },

            'error':                {
                100:                'Could not get server namespace from Rust Server',
                101:                'Could not get server pve variable Rust Server',
                102:                'Unable to get current Server Mode from Rust Server'
            },

            'chat':                 {
                'pvp-night_off':    "Player Vs Player is now restricted due to the Daytime Compliance Laws",
                'pvp-night_on':     "Player Vs Player is now allowed per the Night-time Freedom Act",
                'pvp-day_on':       "Player Vs Player is now allowed per the Day-time Freedom Act",
                'pvp-day_off':      "Player Vs Player is now restricted due to the Night-time Compliance Laws",
                'pvp':              "Player Vs Player is now allowed",
                'pve':              "Player Vs Player is now restricted",
                'random':           "Player Vs Player will be random. A random hour of the day will be chosen to change to %mode% mode",
                'rndWarning':       "Warning: %mode% mode will be in %hours% hours.",
                'pvp_start':        "In compliance with the population laws, Player Vs Player killing is now allowed",
                'pvp_stop':         "Player Vs Player freedoms have just been revoked in compliance with the population laws",
                'event_start':      "Warning: %mode% mode will be inacted for %hours% hours in compliance with the SOTF Aggreement",
                'event_stop':       "%mode% is now over in compliance with the SOTF Agreement",
                'pve_expl': 		"Players attempting to damage other players or buildings will be hurt. You are not protected from fire, explosions, drounding, cold, etc..",
                'pvp_expl': 		"Players may raid, kill, damage buildings without consequence."
            },

            'label':                {
                'pvp-day':          'Daytime PVP',
                'pvp-night':        'Nighttime PVP',
                'pvp':              'PVP Only',
                'pve':              'PVE Only',
                'random':           'Random PVP/PVE',
                'time':             'Time-Based PVP',
                'event': 			'Event-Based PVP'
            }
        }

        this.engine.config.Settings['resources']    = resources;
        this.engine.interop.SaveConfig();
        return true;
    };


    /**
     * Get Resource by Section and Key
     * @param  {String} section
     * @param  {String} key
     * @return {String} Value of key or false if not found or set
     */
    this.get            = function(section, key) {
        if (
            typeof this.engine.config.Settings['resources'] == 'undefined'
            ||
            !this.engine.config.Settings['resources']
        ) {
            return false;
        } else if (
            typeof this.engine.config.Settings['resources'][section] == 'undefined'
            ||
            !this.engine.config.Settings['resources'][section]
        ) {
            return false;
        } else if (
            typeof this.engine.config.Settings['resources'][section][key] == 'undefined'
            ||
            !this.engine.config.Settings['resources'][section][key]
        ) {
            return false;
        }

        return this.engine.config.Settings['resources'][section][key];
    };


    /**
     * Return self
     */
    return this.init(engine);
}


/**
 * Oxide Interop Object
 * @type {Object}
 */
var visionPVP = {


    /**
     * Oxide Plugin Variables
     */
    Title:          "visionPVP",
    Author:         "VisionMise",
    Version:        V(0, 4, 2),
    ResourceId:     1135,
    HasConfig:      true,


    /**
     * API Variables
     */
    engine:         false,
    ready:          false,
    prefix:         "visionPVP",
    modes:          ['pvp', 'pve', 'pvp-night', 'pvp-day', 'random', 'time', 'event'],
    serverReady:    false,
    pluginReady:    false,

    
    /**
     * OnServerInitialized Oxide Hook
     */
    OnServerInitialized:    function () {      
        
        var consoleCommands = {
            'pvp':          'setPvpMode',
            'rnd':          'randomize',
            'rndmin':       'rndMin',
            'rndmax':       'rndMax'
        };

        var chatCommands    = {
            //'pvp':          'getPvpMode'
        };

        for (var cmd in consoleCommands) {
            var name    = this.prefix + "." + cmd;
            var func    = consoleCommands[cmd];
             command.AddConsoleCommand(name, this.Plugin, func);
        }

        for (var cmd in chatCommands) {
            var name    = cmd;
            var func    = chatCommands[cmd];

            command.AddChatCommand(name, this.Plugin, func);
        }

        this.serverReady    = true;
    },

    Init:                   function() {
        print("Starting "+ this.prefix +" Engine");        
        this.engine         = new visionPVP_engine(this.Plugin, this.Config, rust, data, this.prefix, engineVersion, this);
    },

    OnPluginLoaded:         function() {
        this.engine.startTime();
        this.pluginReady    = true;
    },

    /**
     * OnTick Oxide Hook
     */
    OnTick:                 function() {

        if (!this.engine) {
            this.engine = -1;
        } else {
            if (this.ready == false) {
                this.ready = this.engine.ready;
            }

            if (this.ready && this.engine.ready) {
                this.engine.timerTick();
            }
        }
    },


    /** Chat Commands */

        /**
         * getPvpMode
         * @param  {Oxide.Player}   player 
         * @param  {String}         cmd    
         * @param  {Array}          args   
         * @return {Void} 
         */
        getPvpMode:             function(player, cmd, args) {
            var pve     = this.engine.serverPveMode();
            var pvp     = false;

            if (pve == 1) {
                var msg = "The server is in PVE Mode. Players cannot hurt or kill other players.";
                pvp = false;
            } else {
                var msg = "The server is in PVP Mode. Players can hurt and kill other players.";
                pvp = true;
            }

            //rust.SendChatMessage(player, this.prefix, msg);
            return pvp;
        },


    /** Console Commands */

        /**
         * setPvpMode
         * @param {Oxide.Object} param
         */
        setPvpMode:             function(param) {
            if (!param.Args) {
                var pve     = this.engine.serverPveMode();

                print("Server PVE: " + ((pve == 1) ? 'Enabled' : 'Disabled'));
                print("Current Config: " + this.engine.pvpMode.name);
                print(" ");
                print("Usage: ");
                print(this.prefix + ".pvp pvp-night");
                print("The available modes are " + this.modes.join(', '));

                return false;
            }

            var modeStr         = param.Args[0];
            if (this.engine.pvpSet(modeStr)) {
                this.SaveConfig();
            }
        },

        randomize:              function() {
            var mode        = this.engine.pvpMode;

            if (mode.value !== -1) {
                print("Not in Random Mode");
                return false;
            }

            print(this.engine.handler.type);

            this.engine.handler.randomize();
        },

        rndMin:                 function(param) {
            var mode        = this.engine.pvpMode;

            if (mode.value !== -1) {
                print("Not in Random Mode");
                return false;
            }

            var min             = this.Config.Settings['random']['minimum'];

            if (!param.Args) {
                print("Current Random Minimum Hours: " + min);
                return true;
            } else {
                var newMin                                  = param.Args[0];
                this.Config.Settings['random']['minimum']   = newMin;
                var min                                     = this.Config.Settings['random']['minimum'];

                print("New Random Minimum Hours: " + min);
            }
        },

        rndMax:                 function(param) {
            var mode        = this.engine.pvpMode;

            if (mode.value !== -1) {
                print("Not in Random Mode");
                return false;
            }

            var max             = this.Config.Settings['random']['maximum'];

            if (!param.Args) {
                print("Current Random Maximum Hours: " + max);
                return true;
            } else {
                var newMax                                  = param.Args[0];
                this.Config.Settings['random']['maximum']   = newMax;
                var max                                     = this.Config.Settings['random']['maximum'];

                print("New Random Maximum Hours: " + max);
            }
        },


    /** Oxide Hooks */


    	OnEntityTakeDamage:        function(entity, hitInfo) {
            if (hitInfo['HitEntity']) {
                if (hitInfo.HitEntity.GetComponent("BasePlayer")) {
                    if (this.engine.serverPveMode() == 1) {
                        return true;
                    }
                }
            }
    	},


    	/**
    	 * @depreicated Missing OxideMod API Event
    	 */
        OnAirdrop:              function() {
            this.engine.eventController.raiseEvent('onairdrop', {});
        }
};
