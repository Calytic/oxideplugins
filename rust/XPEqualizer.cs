using System.Collections.Generic;
using Oxide.Core;
using Oxide.Core.Configuration;
using Rust.Xp;

namespace Oxide.Plugins
{
    [Info("XPEqualizer", "k1lly0u", "0.1.4", ResourceId = 2003)]
    class XPEqualizer : RustPlugin
    {
        #region Fields
        EqualizerData equalData;
        private DynamicConfigFile data;

        #endregion

        #region Oxide Hooks
        void Loaded()
        {
            data = Interface.Oxide.DataFileSystem.GetFile("XPEqualizer");
            lang.RegisterMessages(Messages, this);
        }
        void Unload() => SaveData();
        void OnServerInitialized()
        {
            LoadVariables();
            LoadData();            
            foreach (var player in BasePlayer.activePlayerList)
                OnPlayerInit(player);
            SaveLoop();
        }
        void OnPlayerInit(BasePlayer player)
        {
            if (configData.SkipAdminXP && player.IsAdmin()) return;
            if (player == null) return;

            if (!equalData.PlayerXP.ContainsKey(player.userID))
            {
                if (player.xp.CurrentLevel < 2)
                {
                    var avgXP = GetAverageStats();
                    if (avgXP > 1)
                    {
                        ResetPlayerLevel(player.userID, avgXP);
                        SendMSG(player, MSG("boostMessage", player.UserIDString));
                    }
                }
                else
                {
                    var playerxp = GetPlayerXP(player);
                    if (playerxp == null) playerxp = 0f;
                    equalData.PlayerXP.Add(player.userID, (float)playerxp);
                }
            }
        }
        void OnXpEarned(ulong playerid, float amount, string source)
        {
            var player = BasePlayer.FindByID(playerid);
            if (player != null)
            {
                if (configData.SkipAdminXP && player.IsAdmin()) return;                

                if (equalData.PlayerXP.ContainsKey(player.userID))
                    equalData.PlayerXP[player.userID] += amount;
                else
                {
                    var playerxp = GetPlayerXP(player);
                    if (playerxp == null) playerxp = 0f;
                    equalData.PlayerXP.Add(player.userID, (float)playerxp);
                }
            }
            else if (equalData.PlayerXP.ContainsKey(playerid))
                equalData.PlayerXP[playerid] += amount;            
        }
        #endregion

        #region Functions
        private float GetAverageStats()
        {            
            float xpAmount = 0;
            float count = 0;

            foreach (var player in equalData.PlayerXP)
            {
                if (player.Value < 1) continue;
                xpAmount += player.Value;
                count++;
            }
            if (xpAmount == 0 || count == 0) return 1;
                     
            var averageXP = xpAmount / count;
            return averageXP;
        }
        private object GetPlayerXP(BasePlayer player) => player.xp.SpentXp + player.xp.UnspentXp;
        
        private void ResetPlayerLevel(ulong userid, float amount, string message = null)
        {
            if (amount <= 1) return;
            var agent = BasePlayer.FindXpAgent(userid);
            agent?.Reset();
            agent?.Add(Definitions.Cheat, amount);

            if (equalData.PlayerXP.ContainsKey(userid))
                equalData.PlayerXP[userid] = amount;
            else equalData.PlayerXP.Add(userid, amount);

            if (!string.IsNullOrEmpty(message))
            {
                var player = BasePlayer.FindByID(userid);
                if (player != null)
                    SendMSG(player, message);
            }   
        }
        private BasePlayer FindPlayer(BasePlayer player, string arg)
        {
            var foundPlayers = new List<BasePlayer>();
            ulong steamid;
            ulong.TryParse(arg, out steamid);
            string lowerarg = arg.ToLower();

            foreach (var p in BasePlayer.activePlayerList)
            {
                if (p != null)
                {
                    if (steamid != 0L)
                        if (p.userID == steamid) return p;
                    string lowername = p.displayName.ToLower();
                    if (lowername.Contains(lowerarg))
                    {
                        foundPlayers.Add(p);
                    }
                }
            }
            if (foundPlayers.Count == 0)
            {
                foreach (var sleeper in BasePlayer.sleepingPlayerList)
                {
                    if (sleeper != null)
                    {
                        if (steamid != 0L)
                            if (sleeper.userID == steamid)
                            {
                                foundPlayers.Clear();
                                foundPlayers.Add(sleeper);
                                return foundPlayers[0];
                            }
                        string lowername = player.displayName.ToLower();
                        if (lowername.Contains(lowerarg))
                        {
                            foundPlayers.Add(sleeper);
                        }
                    }
                }
            }
            if (foundPlayers.Count == 0)
            {
                if (player != null)
                    SendMSG(player, "XP Equalizer: ", MSG("noPlayers", player.UserIDString));
                return null;
            }
            if (foundPlayers.Count > 1)
            {
                if (player != null)
                    SendMSG(player, "XP Equalizer: ", MSG("multiPlayers", player.UserIDString));
                return null;
            }

            return foundPlayers[0];
        }
        #endregion

        #region Chat Commands
        [ChatCommand("xpe")]
        private void cmdXPE(BasePlayer player, string command, string[] args)
        {
            if (!player.IsAdmin()) return;
            if (args == null || args.Length == 0)
            {
                SendMSG(player, "/xpe reset <playername>", MSG("resetName", player.UserIDString));
                SendMSG(player, "/xpe reset all", MSG("resetAll", player.UserIDString));
                SendMSG(player, "/xpe average", MSG("average", player.UserIDString));
            }
            var avgXP = GetAverageStats();
            if (args.Length == 1 && args[0].ToLower() == "average")
            {
                SendMSG(player, MSG("cAverage", player.UserIDString), $"{(int)avgXP} XP");
            }
            if (args.Length > 1)
            {
                
                if (args[1].ToLower() == "all")
                {
                    foreach (var p in BasePlayer.sleepingPlayerList)
                    {
                        if (p == null) continue;
                        if (p.IsAdmin() && configData.IgnoreAdmins_Reset) continue;
                        ResetPlayerLevel(p.userID, avgXP, MSG("resetMessage", p.UserIDString));
                    }
                    foreach (var p in BasePlayer.activePlayerList)
                    {
                        if (p == null) continue;
                        if (p.IsAdmin() && configData.IgnoreAdmins_Reset) continue;
                        ResetPlayerLevel(p.userID, avgXP, MSG("resetMessage", p.UserIDString));
                    }                        
                }
                else
                {
                    var target = FindPlayer(player, args[1]);
                    if (target != null)
                    {                       
                        ResetPlayerLevel(target.userID, avgXP, MSG("resetMessage", target.UserIDString));
                    }
                }
            }
        }
        #endregion

        #region Config        
        private ConfigData configData;
        class ConfigData
        {
            public string MSG_MainColor { get; set; }
            public string MSG_Color { get; set; }
            public bool IgnoreAdmins_Reset { get; set; }            
            public bool SkipAdminXP { get; set; }
        }
        private void LoadVariables()
        {
            LoadConfigVariables();
            SaveConfig();
        }
        protected override void LoadDefaultConfig()
        {
            var config = new ConfigData
            {
                IgnoreAdmins_Reset = true,
                MSG_MainColor = "<color=orange>",
                MSG_Color = "<color=#939393>",
                SkipAdminXP = true
            };
            SaveConfig(config);
        }
        private void LoadConfigVariables() => configData = Config.ReadObject<ConfigData>();
        void SaveConfig(ConfigData config) => Config.WriteObject(config, true);
        #endregion

        #region Data Management
        void SaveLoop() => timer.Once(600, () => { SaveData(); SaveLoop(); });
        void SaveData() => data.WriteObject(equalData);
        void LoadData()
        {
            try
            {
                equalData = data.ReadObject<EqualizerData>();
            }
            catch
            {
                equalData = new EqualizerData();
            }
        }
        class EqualizerData
        {
            public Dictionary<ulong, float> PlayerXP = new Dictionary<ulong, float>();
        }
        #endregion

        #region Messaging
        private void SendMSG(BasePlayer player, string message, string message2 = "") => SendReply(player, $"{configData.MSG_MainColor}{message}</color>{configData.MSG_Color}{message2}</color>");
        private string MSG(string key, string playerid = null) => lang.GetMessage(key, this, playerid);

        Dictionary<string, string> Messages = new Dictionary<string, string>
        {
            {"boostMessage", "Your XP has been boosted to the current server average" },
            {"resetMessage", "Your XP has been reset to the current server average" },
            {"noPlayers", "No players found" },
            {"multiPlayers", "Multiple players found with that name" },
            {"resetName", " - Resets the target players XP to the server average" },
            {"resetAll", " - Calculates the average server XP and sets all players to that amount" },
            {"average", " - Display the current average XP amount" },
            {"cAverage", "Current Average: " }
        };
        #endregion
    }
}
