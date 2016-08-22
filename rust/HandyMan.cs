using System.Collections.Generic;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Core.Configuration;
using Oxide.Game.Rust;
using Oxide.Plugins;
using UnityEngine;
using Facepunch;

/*****************************************************************************************************************************************************
Author  :   MrMan
Date    :   2016-02-22
Credits :   I have quite a few components / authors to credit as I've reverse engineered several plugins for various aspects of this plugin.

            bawNg / Nogrod - Building Grades
            I took the building structure configuration from Building Grades as the upgrade plugin bawNg made basically targets the structures that 
            you would normally repair. Basically this plugin provided me insight into how the configuration for a plugin can be used and adapted.

            Wulf / lukespragg - AutoDoors
            This plugin provided insight into how player preferences could be persisted.

            Zeiser/Visagalis - ZLevelsRemastered
            I noticed on my server that ZLevels was able to list help information. I saw how this was done and used it in HandyMan

            AlienX - Template for Rust Oxide
            This was my starting point. The template got the basics up and running and allowed me to do an initial test deployment with only 
            messaging functionality. It's basic, but very usefull.

*****************************************************************************************************************************************************
CHANGE HISTORY
*****************************************************************************************************************************************************
Version :   1.0.1.0
Date    :   2016-02-27
Changes :   Initial release of HandyMan - Published.
*****************************************************************************************************************************************************
Version :   1.0.1.1
Date    :   2016-03-07
Changes :   Changes made based on feedback from Wulf.
            - Removed excessive Put statements to eliminate chatting with the console.
            - Removed placeholder for registered messages as we're not using it anyway.
            - Created configuration entries for previous constant values that might allow for configuration.
            - Removed constant for version tracking as version is already tracked by the framework.
            - Updated plugin name, removing the "plugin" designation
*****************************************************************************************************************************************************
Version :   1.0.1.2
Date    :   2016-03-18
Changes :   Implemented the lang API
*****************************************************************************************************************************************************
Version :   1.0.2.1
Date    :   2016-07-28
Changes :   Implemented AOE repair for deployables
*****************************************************************************************************************************************************/

namespace Oxide.Plugins
{

    [Info("HandyMan", "MrMan", "1.0.2.1")]
    [Description("Provides AOE repair functionality to the player. Repair is only possible where you can build. HandyMan can be turned on or off.")]
    public class HandyMan : RustPlugin
    {
        #region Constants
       
        #endregion

        #region Members
        readonly DynamicConfigFile dataFile = Interface.Oxide.DataFileSystem.GetFile("HandyMan");
        Dictionary<ulong, bool> playerPrefs_IsActive = new Dictionary<ulong, bool>(); //Stores player preference values - on or off.

        private ConfigData configData; //Structure containing the configuration data once read

        private PluginTimers RepairMessageTimer; //Timer to control HandyMan chats
        private bool _allowHandyManFixMessage = true; //indicator allowing for handyman fix messages
        private bool _allowAOERepair = true; //indicator for allowing AOE repair
        private string _ChatmessagePrefix = "HandyMan"; //Chat prefix control

        #endregion
        
        /// <summary>
        /// Class defined to deal with configuration structure.
        /// </summary>
        class ConfigData
        {
            //Controls the range at which the AOE repair will work
            public float RepairRange { get; set; }
            public bool DefaultHandyManOn { get; set; }
            //Contains a list of possible affected structures
            public Dictionary<string, HashSet<string>> Categories { get; set; }
            public float HandyManChatInterval { get; set; } 
        }

        /// <summary>
        /// Responsible for loading default configuration.
        /// Also creates the initial configuration file
        /// </summary>
        protected override void LoadDefaultConfig()
        {
            ConfigData config = new ConfigData
            {
                DefaultHandyManOn = true,
                HandyManChatInterval = 30,
                RepairRange = 50,
                //Specifies the structure category dictionary
            };
            //Creates a config file - Note sync is turned on so changes in the file should be taken into account, overriding what is coded here
            Config.WriteObject(config, true);
        }

        

        /// <summary>
        /// Responsible for loading the configured list of structures that will be affected.
        /// Takes the text description given in the configuration and converts this to an internal system ID for the prefab
        /// </summary>
        internal void LoadAffectedStructures()
        {
            configData = Config.ReadObject<ConfigData>();
        }

        #region Oxide Hooks
        /// <summary>
        /// Called when plugin initially loads.
        /// This section is used to "prep" the plugin and any related / config data
        /// </summary>
        private void Init()
        {
            //Read the configuration data
            LoadAffectedStructures();

        }

        //Called when this plugin has been fully loaded
        private void Loaded()
        {
            LoadMessages();
            playerPrefs_IsActive = dataFile.ReadObject<Dictionary<ulong, bool>>();
        }

        void LoadMessages()
        {
            string helpText = "HandyMan - Help - v {ver} \n"
                            + "-----------------------------\n"
                            + "/HandyMan - Shows your current preference for HandyMan.\n"
                            + "/HandyMan on/off - Turns HandyMan on/off.";

            lang.RegisterMessages(new Dictionary<string, string>
            {
                {"Hired", "HandyMan has been Hired."},
                {"Fired", "HandyMan has been Fired."},
                {"Fix", "You fix this one, I'll get the rest."},
                {"NotAllowed", "You are not allowed to build here - I can't repair for you."},
                {"IFixed", "I fixed some damage over here..."},
                {"FixDone", "Guess I fixed them all..."},
                {"MissingFix", "I'm telling you... it disappeared... I can't find anything to fix."},
                {"Help", helpText}
            }, this);
        }


        /// <summary>
        /// TODO: Investigate entity driven repair.
        /// Currently only building structures are driving repair. I want to allow things like high external walls to also 
        /// drive repair, but they don't seem to fire under OnStructureRepair. I suspect this would be a better trigger as it would 
        /// allow me to check my entity configuration rather than fire on simple repair.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="info"></param>
        void OnHammerHit(BasePlayer player, HitInfo info)
        {
            //gets the correct entity type from the hammer target
            var e = info.HitEntity.GetComponent<BaseCombatEntity>();

            //checks to see that we have an entity - we should always have one
            if (e != null)
            {
                //yes - continue repair
                //checks if player preference for handyman exists on this player
                if (!playerPrefs_IsActive.ContainsKey(player.userID))
                {
                    //no - create a default entry for this player based on the default HandyMan configuration state
                    playerPrefs_IsActive[player.userID] = configData.DefaultHandyManOn;
                    dataFile.WriteObject(playerPrefs_IsActive);
                }

                //Check if repair should fire - This is to prevent a recursive / infinate loop when all structures in range fire this method.
                //This also checks if the player has turned HandyMan on
                if (_allowAOERepair && playerPrefs_IsActive[player.userID])
                {
                    //calls our custom method for this
                    Repair(e, player);
                }
            }
        }

        #endregion

        #region HelpText Hooks

        /// <summary>
        /// Responsible for publishing help for handyman on request
        /// </summary>
        /// <param name="player"></param>
        [HookMethod("SendHelpText")]
        private void SendHelpText(BasePlayer player)
        {
            player.ChatMessage(GetMsg("Help",player.userID).Replace("{ver}",Version.ToString()));
        }
        #endregion


        #region Repair Methods

        /// <summary>
        /// Executes the actual repair logic.
        /// </summary>
        /// <param name="block"></param>
        /// <param name="player"></param>
        void Repair(BaseCombatEntity block, BasePlayer player)
        {
            //Set message timer to prevent user spam
            ConfigureMessageTimer();

            //Checks to see if the player can build
            if (player.CanBuild())
            {
                //yes - Player can build - check if we can display our fix message
                if (_allowHandyManFixMessage)
                {
                    //yes - display our fix message
                    SendChatMessage(player, _ChatmessagePrefix, GetMsg("Fix", player.userID));
                    _allowHandyManFixMessage = false;
                }

                //Envoke the AOE repair set
                RepairAOE(block, player);

            }
            else
            {
                SendChatMessage(player, _ChatmessagePrefix, GetMsg("NotAllowed", player.userID));
            }
        }

        /// <summary>
        /// Contains the actual AOE repair logic
        /// </summary>
        /// <param name="block"></param>
        /// <param name="player"></param>
        private void RepairAOE(BaseCombatEntity block, BasePlayer player)
        {
            //This needs to be set to false in order to prevent the subsequent repairs from triggering the AOE repair.
            //If you don't do this - you create an infinate repair loop.
            _allowAOERepair = false;

            //Sets up our RepairBlock collection
            var blocks_torepair = new HashSet<BaseCombatEntity>();
            //gets the position of the block we just hit
            var position = new OBB(block.transform, block.bounds).ToBounds().center;
            //sets up the collectionf or the blocks that will be affected
            var blocks = Pool.GetList<BaseCombatEntity>();

            //gets a list of entities within a specified range of the current target
            Vis.Entities(position, configData.RepairRange, blocks, 270532864);
            

            //check if we have blocks - we should always have at least 1
            if (blocks.Count > 0)
            {
                bool hasRepaired = false;

                //cycle through our block list - figure out which ones need repairing
                foreach (var item in blocks)
                {
                    //check to see if the block has been damaged before repairing.
                    if (item.Health() < item.MaxHealth())
                    {
                        //yes - repair
                        item.DoRepair(player);
                        item.SendNetworkUpdate();
                        hasRepaired = true;
                    }
                }
                Pool.FreeList(ref blocks);

                //checks to see if any blocks were repaired
                if (hasRepaired)
                {
                    //yes - indicate
                    SendChatMessage(player, _ChatmessagePrefix, GetMsg("IFixed", player.userID));
                }
                else
                {
                    //No - indicate
                    SendChatMessage(player, _ChatmessagePrefix, GetMsg("FixDone", player.userID));
                }
            }
            else
            {
                SendChatMessage(player, _ChatmessagePrefix, GetMsg("MissingFix", player.userID));
            }
            _allowAOERepair = true;
        }

        /// <summary>
        /// Responsible for preventing spam to the user by setting a timer to prevent messages from Handyman for a set duration.
        /// </summary>
        private void ConfigureMessageTimer()
        {
            //checks if our timer exists
            if (RepairMessageTimer == null)
            {
                //no - create it
                RepairMessageTimer = new PluginTimers(this);
                //set it to fire every xx seconds based on configuration
                RepairMessageTimer.Every(configData.HandyManChatInterval, RepairMessageTimer_Elapsed);
            }
        }

        /// <summary>
        /// Timer for our repair message elapsed - set allow to true
        /// </summary>
        private void RepairMessageTimer_Elapsed()
        {
            //set the allow message to true so the next message will show
            _allowHandyManFixMessage = true;
        }

        #endregion


        #region Chat and Console Command Examples
        [ChatCommand("HandyMan")]
        private void ChatCommand_HandyMan(BasePlayer player, string command, string[] args)
        {
            if (args != null && args.Length >= 1)
            {
                if (args[0].ToLower() == "on")
                {

                    playerPrefs_IsActive[player.userID] = true;
                }
                else
                {
                    playerPrefs_IsActive[player.userID] = false;
                }
                dataFile.WriteObject(playerPrefs_IsActive);
            }

            if (playerPrefs_IsActive[player.userID] == true)
            {
                SendChatMessage(player, _ChatmessagePrefix, GetMsg("Hired", player.userID));
            }
            else
            {
                SendChatMessage(player, _ChatmessagePrefix, GetMsg("Fired", player.userID));
            }
        }

        [ConsoleCommand("HealthCheck")]
        private void ConsoleCommand_HealthCheck()
        {
            Puts("HandyMan is running.");
        }
        #endregion

        #region Helpers


        /// <summary>
        /// Retreives the configured message from the lang API storage.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="userID"></param>
        /// <returns></returns>
        string GetMsg(string key, object userID = null)
        {
            return lang.GetMessage(key, this, userID.ToString());
        }

        /// <summary>
        /// Writes message to player chat
        /// </summary>
        /// <param name="player"></param>
        /// <param name="prefix"></param>
        /// <param name="msg"></param>
        private void SendChatMessage(BasePlayer player, string prefix, string msg = null) => SendReply(player, msg == null ? prefix : "<color=#00FF8D>" + prefix + "</color>: " + msg);

        #endregion
    }
}
