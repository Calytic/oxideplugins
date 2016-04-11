/* Copyright (c) 2015 Wojciech BartÅomiej Chojnacki (aka skyman)
This software is provided 'as-is', without any express or implied warranty.
In no event will the authors be held liable for any damages arising from the use of this software.

Permission is granted to anyone to use this software for any purpose,
including commercial applications, and to alter it and redistribute it freely,
subject to the following restrictions:
    1. The origin of this software must not be misrepresented; you must not claim that you wrote the original software.
        If you use this software in a product, an acknowledgment in the product documentation would be appreciated but is not required.
    2. Altered source versions must be plainly marked as such, and must not be misrepresented as being the original software.
    3. This notice may not be removed or altered from any source distribution.
    
    Oxide API:
    http://docs.oxidemod.org/rust/
*/

using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Oxide.Core;

namespace Oxide.Plugins
{
    [Info("EpicLanterns", "skyman", "0.0.1", ResourceId = 1357)]
    [Description("Automatically toggles lanterns after sunrise and sunset or in other given timeframe!")]
    class EpicLanterns : RustPlugin
    {
        private string _pluginPrefix;
        protected enum MessageType { Info, Warning, Error };

        private bool _areCachedCorrectly = false;
        private bool _areTurnedOn;
        private Dictionary<string, BaseEntity> _lanterns = new Dictionary<string, BaseEntity>();
        private bool _isLoaded = false, _isInitialized = false;

        /**
            Called when the plugin is being loaded
            Other plugins may or may not be present, dependant on load order
            Other plugins WILL have been executed though, so globals exposed by them will be present
        */
        private void Init()
        {
            this._pluginPrefix = (this.Title + " (" + this.Version + ")").ToString();

            // Validate config and if it is invalid show an error and unload plugin
            if (this.validateConfig() == false)
            {
                echo("Internal plugin error. Cannot initialize plugin. Try previous versions!", MessageType.Error);
                this.Unload();
            }
        }

        /**
            Called when the config for the plugin should be initialized
            Only called if the config file does not already exist
        */
        protected override void LoadDefaultConfig()
        {
            echo("Generating a completely new configuration file.", MessageType.Warning);
            Config.Clear();
            Config["toggleOnAt"] = 18;
            Config["toggleOffAt"] = 8;
            Config["freeLight"] = true;
            SaveConfig();
        }

        /**
            Called when specified plugin has been loaded
        */
        private void OnPluginLoaded(RustPlugin pluginName)
        {
            string epicLanterns = "Oxide.Plugins.EpicLanterns";

            if (epicLanterns.Equals(pluginName.GetType().Name)) {
                this._isLoaded = true;
                if (this._isInitialized)
                {
                    this.createLanternsCache();

                    // Calculate correct lanters state
                    int currentTime = Convert.ToInt16(Math.Floor(TOD_Sky.Instance.Cycle.Hour));

                    // Toggle on, because it is after time to turn on all lanterns
                    if (currentTime >= (int)Config["toggleOnAt"])
                    {
                        echo("Lanterns should light up.");
                        this._areTurnedOn = true;
                    }
                    // Toggle off, because it is after time to turn off all lanterns, but before time to turn them on
                    else if (currentTime >= (int)Config["toggleOffAt"] && currentTime < (int)Config["toggleOnAt"])
                    {
                        echo("Lanterns should not light up.");
                        this._areTurnedOn = false;
                    }
                    // If none of the above did not work, it means that currently it is time between toggling on and toggling off, so turn them on
                    else if (currentTime < (int)Config["toggleOffAt"])
                    {
                        echo("Lanterns should light up.");
                        this._areTurnedOn = true;
                    }

                    // Apply current state to all lanterns
                    smartToggleLanterns();
                }
            }
        }

        /**
            Called from ServerMgr.Initialize
            Called after the server startup has been completed and is awaiting connections
            No return behavior
        */
        private void OnServerInitialized()
        {
            this._isInitialized = true;
            if (this._isLoaded)
            {
                this.createLanternsCache();
            }
        }

        /**
            Called when the plugin is being unloaded
        */
        private void Unload()
        {
            SaveConfig();
        }

        private void OnTick()
        {
            // Get current time in order to check if lanterns should be turned on or turned off
            int currentTime = Convert.ToInt16(Math.Floor(TOD_Sky.Instance.Cycle.Hour));

            // Toggle on, because it is after time to turn on all lanterns
            if (this._areTurnedOn == false && currentTime >= (int)Config["toggleOnAt"])
            {
                echo("Lanterns turning on!");
                this._areTurnedOn = true;
                this.smartToggleLanterns();
            }
            // Toggle off, because it is after time to turn off all lanterns, but before time to turn them on
            else if (this._areTurnedOn == true && currentTime >= (int)Config["toggleOffAt"] && currentTime < (int)Config["toggleOnAt"])
            {
                echo("Lanterns turning off!");
                this._areTurnedOn = false;
                this.smartToggleLanterns();
            }
        }

        /**
            Called from Deployer.DoDeploy_Regular and Deployer.DoDeploy_Slot
            Called right after an item has been deployed
            No return behavior
        */
        private void OnItemDeployed(Deployer deployer, BaseEntity entity)
        {
            // Don't proceed if deployed item wasn't lantern
            if (!this.isLantern(entity)) return;

            // Add entity to cache
            this._lanterns.Add(getEntityId(entity), entity);

            // Check if light is free or whether entity has fuel
            if ((bool)Config["freeLight"] == true || this.hasFuel(entity))
            {
                // If any condition is true, then it is safe to modify BaseEntity.Flags.On
                entity.SetFlag(BaseEntity.Flags.On, this._areTurnedOn);
            }

            echo("Lanterns (+A): " + this._lanterns.Count);
        }

        /**
            Called from BaseCombatEntity.Die
            hitInfo might be null, check it before use
            Editing hitInfo has no effect because the death has already happened
            No return behavior
        */
        private void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
        {
            // Don't proceed if deployed item wasn't lantern
            if (!this.isLantern(entity)) return;

            removeLanternFromCache(getEntityId(entity));
        }

        /**
            Called from DestroyOnGroundMissing.OnGroundMissing
            Called when an entity (sleepingbag, sign, furnace,â¦) is going to be destroyed because the buildingblock it is on was removed
            Returning a non-null value overides default server behavior
        */
        private void OnEntityGroundMissing(BaseEntity entity)
        {
            // Don't proceed if deployed item wasn't lantern
            if (!this.isLantern(entity)) return;

            removeLanternFromCache(getEntityId(entity));
        }

        /////////////////////
        // General methods //
        /////////////////////
        /**
            Prints given message.
            May be expanded in the future versions.
        */
        protected void echo(string message, MessageType messageType = MessageType.Info)
        {
            switch (messageType)
            {
                case MessageType.Warning:
                    Interface.Oxide.LogWarning(String.Format("{0}", message));
                    break;
                case MessageType.Error:
                    Interface.Oxide.LogError(String.Format("{0}", message));
                    break;
                case MessageType.Info:
                default:
                    Interface.Oxide.LogInfo(String.Format("{0}", message));
                    break;
            }
        }

        /**
            Create and return entity ID based on its coordinates (x, y, z).
        */
        public string getEntityId(BaseEntity entity)
        {
            return entity.transform.position.ToString();
        }

        /////////////////////////////
        // Plugin specific methods //
        /////////////////////////////
        /**
            Clears config and create new one
        */
        private bool badConfig(string missingKey)
        {
            echo("Config key is missing: " + missingKey, MessageType.Error);
            this.LoadDefaultConfig();
            return false;
        }

        /**
            Checks config
        */
        protected bool validateConfig()
        {
            // Check if required config keys exist
            if (Config["toggleOnAt"] == null)
            {
                return badConfig("toggleOnAt");
            }

            if (Config["toggleOffAt"] == null)
            {
                return badConfig("toggleOffAt");
            }

            if (Config["freeLight"] == null)
            {
                return badConfig("freeLight");
            }

            int toggleOnAt = (int)Config["toggleOnAt"];
            int toggleOffAt = (int)Config["toggleOffAt"];
            bool freeLight = (bool)Config["freeLight"];

            // Check if toggle hours are correct
            if ((toggleOnAt < 0 && toggleOnAt >= 24) || (toggleOffAt < 0 && toggleOffAt >= 24))
            {
                echo("Wrong settings! Toggle hours should be within range: 0-23.", MessageType.Error);
                return false;
            }

            // Check if toggleOn time 
            if (toggleOffAt > toggleOnAt)
            {
                echo("Wrong settings! toggleOffAt should be lower than toggleOnAt. toggleOffAt: " + toggleOffAt + ", toggleOnAt: " + toggleOnAt, MessageType.Error);
                return false;
            }

            // If everything went good, just continue
            return true;
        }

        /**
            Checks whether entity is a deployed lantern
        */
        protected bool isLantern(BaseEntity entity)
        {
            if (entity.LookupShortPrefabName() == "lantern_deployed.prefab")
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        protected bool isLantern(BaseOven entity)
        {
            if (entity.LookupShortPrefabName() == "lantern_deployed.prefab")
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /**
            TODO: Check whether lantern has fuel (ItemModBurnable)
        */
        protected bool hasFuel(BaseEntity entity)
        {
            return true;
        }

        /**
            Create a list of all deployed lanters and return it for further use.
        */
        protected void createLanternsCache()
        {
            // Clear current lantern list
            this._lanterns.Clear();

            // Then get all object of type BaseEntity
            List<BaseEntity> entities = Component.FindObjectsOfType<BaseEntity>().ToList();

            if (entities.Count > 0)
            {
                // After that, add only IDs of deployed lanterns
                foreach (BaseEntity entity in entities)
                {
                    if (entity.LookupShortPrefabName() == "lantern_deployed.prefab")
                    {
                        this._lanterns.Add(getEntityId(entity), entity);
                    }
                }
            }

            this._areCachedCorrectly = true;
            echo("Total numbers of lanterns: " + this._lanterns.Count);
        }

        /**
            Remove lantern if it cease to exist. If it wasn't in the list then inform that cache is not correct.
        */
        protected void removeLanternFromCache(string entityId)
        {
            if (this._lanterns.Remove(entityId) == false)
            {
                this._areCachedCorrectly = false;
                echo("Removed lantern wasn't in the list. It should not happen, so at the next toggle of lanterns, the list will be refreshed (which may cause small lag) in order to be sure that all lanters are cached correctly.");
            }
            echo("Lanterns (-R): " + this._lanterns.Count);
        }

        /**
            Toggle lanterns with extra-check whether cache is accurate.
        */
        protected void smartToggleLanterns()
        {
            // Only if needed refresh lantern list (it will also mark that cache is now correct)
            if (this._areCachedCorrectly == false)
            {
                this.createLanternsCache();
            }

            // Apply new state to all lanterns
            foreach (string entityId in this._lanterns.Keys)
            {
                // If light is free or given lantern has fuel, turn lights
                if ((bool)Config["freeLight"] == true || this.hasFuel(this._lanterns[entityId]))
                {
                    this._lanterns[entityId].SetFlag(BaseEntity.Flags.On, _areTurnedOn);
                }
            }
        }

    }
}