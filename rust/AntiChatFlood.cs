using System.Collections.Generic;
using System;
using UnityEngine;
using Oxide.Core.Plugins;
using System.Reflection;
using Oxide.Core;
using System.Data;
using Rust;

namespace Oxide.Plugins
{
    [Info("AntiChatFlood", "DylanSMR", "1.0.1")]
    [Description("Data test stuff.")]

    class AntiChatFlood : RustPlugin
    {
        [PluginReference]
        Plugin BetterChat;              
        
        private bool Changed;
        
        //////////
        //Config//
        //////////
 
        static bool WarningEnabled = true;
        static int WaitTillMsg = 5;
        static int MaxWarnings = 3;
        static bool AdminBypass = true;
        public int AuthToBypass = 1;
        
        static bool DisableBetterChat = false; 
 
        void OnServerInitialized()
        {
            LoadVariables();
        } 
 
        void LoadDefaultConfig()
        {
            Puts("Creating a new config file");
            Config.Clear();
            LoadVariables();
        }
 
        private void LoadVariables()
        {
            LoadConfigVariables();
            SaveConfig();
        }
        
        private void LoadConfigVariables()
        {
            CheckCfg("Time untill a player can chat again.", ref WaitTillMsg);
            CheckCfg("If the warning system is enabled", ref WarningEnabled);
            CheckCfg("How many warnings untill a player is kicked.", ref MaxWarnings);
            CheckCfg("If the plugin disables betterchat when this loades", ref DisableBetterChat);
        }
        
        void Loaded() 
        {    
			lang.RegisterMessages(new Dictionary<string,string>{
				["ACF_WAIT"] = "<color='#DD0000'>You are typing to fast - Please wait {0} seconds.</color>",
                ["ACF_WARNING"] = "<color='#DD0000'>You now have a current total of {0} warnings.</color>",
                ["ACF_ADDED"] = "{0} was added to the data file.",
                ["ACF_REMOVED"] = "{0} was removed from the (temp)data file.",
                ["ACF_KICKING"] = "Kicking {0} for reaching the warning limit of {1}.",
                ["ACF_REASON"] = "You were kicked as you reached the max limit of warnings.",
                ["ACF_HELP"] = "<color='#DD0000'>Help Commands: </cwarning> to check your warnings></color>",
                ["ACF_ADMINHELP"] = "<color='DD0000'>Admin Help Commands: </awipe (target)> to wipe a players warnings | </awipeall> to wipe all player warnings.</color>",
                ["ACF_WIPEDALL"] = "<color='#DD0000'>All players warning wiped to 0, each database file was erased.</color>",
                ["ACF_WIPEDAT"] = "{0} wiped {1}'s warnings!!!",
                ["ACF_WIPEDP"] = "<color='#DD0000'>You have wiped {0}'s warnings</color>",
                ["ACF_WIPEDY"] = "<color='DD0000'>{0} has wiped your warnings</color>",
                ["ACF_WARNINGS"] = "<color='DD0000'>You have a current total of {0} warnings and need {1} more warning to be kicked</color>",
                
			}, this);
            
            if(BetterChat != null)
            {
                if(!DisableBetterChat)
                {
                    rust.RunServerCommand("oxide.unload AntiChatFlood");
                    PrintWarning("PLUGIN UNLOADED DUE TO BETTERCHAT EXISTING!!!");  
                }
                else
                {
                    rust.RunServerCommand("oxide.unload BetterChat");
                    PrintWarning("BETTERCHAT UNLOADED!!!");
                }
            }             		
        }         

        private void CheckCfg<T>(string Key, ref T var)
        {
            if (Config[Key] is T)
                var = (T)Config[Key];
            else
                Config[Key] = var;
        }
        private void CheckCfgFloat(string Key, ref float var)
        {

            if (Config[Key] != null)
                var = Convert.ToSingle(Config[Key]);
            else
                Config[Key] = var;
        }        
        
        object GetConfig(string menu, string datavalue, object defaultValue)
        {
            var data = Config[menu] as Dictionary<string, object>;
            if (data == null)
            {
                data = new Dictionary<string, object>();
                Config[menu] = data;
                Changed = true;
            }
            object value;
            if (!data.TryGetValue(datavalue, out value))
            {
                value = defaultValue;
                data[datavalue] = value;
                Changed = true;
            }
            return value;
        }
        
        ///////////////////////////////
        //TEMP DATA-DO NOT EDIT BELOW//
        ///////////////////////////////
        

        List<ulong> playerWait = new List<ulong>();        
        private Dictionary<ulong, PlayerWarnings> pWarn = new Dictionary<ulong, PlayerWarnings>(); 

        class PlayerWarnings 
        {
            public string Name;
            public float CurrentWarnings;
        }
                 
        ////////////////////
        //Plugin Functions//
        ////////////////////
        
        void Unload()
        {
            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                pWarn.Remove(player.userID);
            }
        }
        
        void Init()
        {
            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                pWarn.Add(player.userID, new PlayerWarnings()); 
            }
        }
        
        private string GetMessage(string name, string sid = null) {
			return lang.GetMessage(name, this, sid);
		}
        
        void OnPlayerInit(BasePlayer player)
        {
            SetVars(player);
        }
        
        void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            pWarn.Remove(player.userID);
            PrintWarning(String.Format(lang.GetMessage("ACF_REMOVED", this), player.displayName));
        }
        
        void SetVars(BasePlayer player) // Now lets add some data to the dictionary
        {
            if (!pWarn.ContainsKey(player.userID)) 
                pWarn.Add(player.userID, new PlayerWarnings()); 
            pWarn[player.userID].Name = player.displayName;
            pWarn[player.userID].CurrentWarnings = 0;
            
            PrintWarning(String.Format(lang.GetMessage("ACF_ADDED", this), player.displayName));
        }
        
        void KickPlayer(BasePlayer player)
        {  
            BasePlayer target = player;
            Network.Net.sv.Kick(target.net.connection, String.Format(lang.GetMessage("ACF_REASON", this)));      
        }
        
        private object FindPlayer(string arg)
        {
            var foundPlayers = new List<BasePlayer>();
            ulong steamid;
            ulong.TryParse(arg, out steamid);
            string lowerarg = arg.ToLower();

            foreach (var p in BasePlayer.activePlayerList)
            {
                if (steamid != 0L)
                    if (p.userID == steamid)
                    {
                        foundPlayers.Clear();
                        foundPlayers.Add(p);
                        return foundPlayers;
                    }
                string lowername = p.displayName.ToLower();
                if (lowername.Contains(lowerarg))
                {
                    foundPlayers.Add(p);
                }
            }
            if (foundPlayers.Count == 0) return lang.GetMessage("noPlayers", this);
            if (foundPlayers.Count > 1) return lang.GetMessage("multiPlayers", this);

            return foundPlayers[0];
        }
        
        void WipeAllStats()
        {
            foreach(BasePlayer player in BasePlayer.activePlayerList)
            {
                pWarn[player.userID].CurrentWarnings = 0;
                SetVars(player);
                PrintWarning(String.Format(lang.GetMessage("ACF_WIPEDALL", this)));
                SendReply(player, String.Format(lang.GetMessage("ACF_WIPEDALL", this)));
            }  
        }
        /////////////////
        //Chat Handlers//
        /////////////////
       
        object OnPlayerChat(ConsoleSystem.Arg arg)
        {
            
			BasePlayer player = (BasePlayer)arg.connection.player;	
    		BasePlayer target = (BasePlayer)arg.connection.player;          
            if(player.net.connection.authLevel >= AuthToBypass && AdminBypass == true)
            {
                return null;
            }
            if(BetterChat != null)
            {
                return BetterChat;    
            }            
            if(playerWait.Contains(player.userID))
            {
                SendReply(player, String.Format(lang.GetMessage("ACF_WAIT", this), WaitTillMsg));
                if (WarningEnabled = true)
                {
                    if (pWarn.ContainsKey(player.userID))  
                    {
                        if (pWarn[player.userID].CurrentWarnings <= MaxWarnings)
                        {
                            pWarn[player.userID].CurrentWarnings++;
                            SendReply(player, String.Format(lang.GetMessage("ACF_WARNING", this), pWarn[player.userID].CurrentWarnings));
                        }
                        else
                        {
                            pWarn[player.userID].CurrentWarnings = 0;
                            PrintWarning(String.Format(lang.GetMessage("ACF_KICKING", this), player.displayName, MaxWarnings));  
                            timer.Once(2, () =>
                            {
                                KickPlayer(player);
                            });
                        }
                    }
                    else
                    {
                        SetVars(player);
                        timer.Once(2, () =>
                        {
                            if (pWarn[player.userID].CurrentWarnings <= MaxWarnings)
                            {
                                pWarn[player.userID].CurrentWarnings++;
                                SendReply(player, String.Format(lang.GetMessage("ACF_WARNING", this), pWarn[player.userID].CurrentWarnings));
                            }      
                            else
                            {
                                KickPlayer(player);
                            }
                        });
                    }
                }
            }
            else
            {
                playerWait.Add(player.userID);  
                timer.Once(WaitTillMsg, () => playerWait.Remove(player.userID));
                return null;
            }
            return true;
        }
        
        
        /////////////////
        //Chat Commands//
        /////////////////
        
        [ChatCommand("chelp")]
        void help(BasePlayer player)
        {
            if (player.net.connection.authLevel > 1)
            {
                SendReply(player, String.Format(lang.GetMessage("ACF_ADMINHELP", this)));
            }
            else
            {
                SendReply(player, String.Format(lang.GetMessage("ACF_HELP", this)));
            }
        }
        
        [ChatCommand("cwipeall")]
        void AWipeAll(BasePlayer player)
        {
            if (player.net.connection.authLevel > 1)
            {
                WipeAllStats();
            }                      
        }
        
        [ChatCommand("cwipe")]
        void playerwipe(BasePlayer player, string command, string[] args)     
        {
            if (args.Length == 1)
            {
                if(player.net.connection.authLevel > 1)
                {
                    object addPlayer = FindPlayer(args[0]);             
                    BasePlayer target = (BasePlayer)addPlayer;         
                    pWarn[target.userID].CurrentWarnings = 0;
                
                    PrintWarning(String.Format(lang.GetMessage("ACF_WIPEDAT", this), player.displayName, target));
                    SendReply(player, String.Format(lang.GetMessage("ACF_WIPEDP", this), target.displayName));
                    SendReply(target, String.Format(lang.GetMessage("ACF_WIPEDY", this), player.displayName));
                
                    SetVars(target);
                }
            }
            else
            {
                if (player.net.connection.authLevel > 1)
                {
                    SendReply(player, String.Format(lang.GetMessage("ACF_ADMINHELP", this)));
                }
                else
                {
                    SendReply(player, String.Format(lang.GetMessage("ACF_HELP", this)));
                }
            }
        }    
        
        [ChatCommand("cwarning")]  
        void CheckWarnings(BasePlayer player)
        {
            if (pWarn.ContainsKey(player.userID)) 
            {
                var TillKick = MaxWarnings + 1 - pWarn[player.userID].CurrentWarnings;
                SendReply(player, String.Format(lang.GetMessage("ACF_WARNINGS", this), pWarn[player.userID].CurrentWarnings, TillKick ));
            }
            else
            {
                SetVars(player);
            }
        }
    }
}