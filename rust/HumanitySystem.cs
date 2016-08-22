using System;
using System.Collections.Generic;
using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Game.Rust.Cui;
using Oxide.Core.Plugins;
using UnityEngine;
using System.Linq;
using System.Reflection;

namespace Oxide.Plugins
{
    [Info("Humanity System", "DylanSMR", "1.1.5", ResourceId = 1999)]
    [Description("A humanity system based off of DayZ mod.")]
    public class HumanitySystem : RustPlugin
    {  
        [PluginReference] Plugin BetterChat;

        // / // / // / //
        //Configuration//
        // / // / // / //

        void LoadDefaultConfig()
        {
            PrintWarning("Creating default configuration");
            Config.Clear();
                Config["HumanityLossGainOnKill"] = 50;
                Config["HudPosition"] = 1;
                Config["HeadText"] = false;
            Config.Save();
        }

        // / // / // / //
        //Data System  //
        // / // / // / //

        static HumanityData humanityData;
        public List<ulong> inUI = new List<ulong>();

        class HumanityData
        {
            public Dictionary<ulong, players> playerH = new Dictionary<ulong, players>();
            public HumanityData() { }
        }

        class players
        {
            public ulong playerID;
            public int Humanity;
            public int Rank;
            public int Kills;
            public int Deaths;
            public players() { }

            internal static players Find(BasePlayer player)
            {
                return humanityData.playerH.Values.ToList().Find((d) => d.playerID == player.userID);
            }
        }

        // / // / // / //
        //Public Hooks //
        // / // / // / //

        public string GetMin()
        {
            var quad = Convert.ToInt32(Config["HudPosition"]);
            if(quad == 1) return "0.84 0.98";
            else if(quad == 2) return "0.01 0.98";
            else if(quad == 3) return "0.012 0.38";
            else if(quad == 4) return "0.841 0.42";
            else return "0.84 0.98";
        }

        public string GetMax()
        {
            var quad = Convert.ToInt32(Config["HudPosition"]);
            if(quad == 1) return "0.99 0.700";
            else if(quad == 2) return "0.15 0.700";
            else if(quad == 3) return "0.157 0.10";
            else if(quad == 4) return "0.987 0.14";
            else return "0.99 0.700";
        }

        public object GetRank(BasePlayer player)
        {
            if (players.Find(player) == null) OnPlayerInit(player);
            players playerData = players.Find(player);
            if(playerData.Rank == 0) return "Neutral";
            else if(playerData.Rank == 1) return "Hero";
            else if(playerData.Rank == 2) return "Bandit"; 
            return null;
        }

        public object GetStat(BasePlayer player, string Stat)
        {
            if (players.Find(player) == null) OnPlayerInit(player);
            players playerData = players.Find(player);
            if(Stat == "Kills") return playerData.Kills;
            if(Stat == "Deaths") return playerData.Deaths;
            if(Stat == "Rank") return playerData.Rank;
            if(Stat == "Humanity") return playerData.Humanity;
            return null;
        }

        public object RankAlgorithm(BasePlayer player)
        {
            if(players.Find(player) == null) OnPlayerInit(player);
            players playerData = players.Find(player);
            if(playerData.Humanity <= -2500){
                if(Convert.ToBoolean(BetterChat?.Call("API_IsUserInGroup", player.UserIDString, "Hero")) == true) BetterChat?.Call("API_RemoveUserFromGroup", player.UserIDString, "Hero");
                if(Convert.ToBoolean(BetterChat?.Call("API_IsUserInGroup", player.UserIDString, "Neutral")) == true) BetterChat?.Call("API_RemoveUserFromGroup", player.UserIDString, "Neutral");
                if(Convert.ToBoolean(BetterChat?.Call("API_IsUserInGroup", player.UserIDString, "Bandit")) == true || playerData.Rank == 2) return "Bandit";
                BetterChat?.Call("API_AddUserToGroup", player.UserIDString, "Bandit");             
                humanityData.playerH[player.userID].Rank = 2;
                SendReply(player, lang.GetMessage("NowA", this), "Bandit");     
            }
            else if(playerData.Humanity >= 2500){
                if(Convert.ToBoolean(BetterChat?.Call("API_IsUserInGroup", player.UserIDString, "Bandit"))) BetterChat?.Call("API_RemoveUserFromGroup", player.UserIDString, "Bandit");
                if(Convert.ToBoolean(BetterChat?.Call("API_IsUserInGroup", player.UserIDString, "Neutral"))) BetterChat?.Call("API_RemoveUserFromGroup", player.UserIDString, "Neutral");
                if(Convert.ToBoolean(BetterChat?.Call("API_IsUserInGroup", player.UserIDString, "Hero")) || playerData.Rank == 1) return "Hero";
                BetterChat?.Call("API_AddUserToGroup", player.UserIDString, "Hero");             
                humanityData.playerH[player.userID].Rank = 1;
                SendReply(player, lang.GetMessage("NowA", this), "Hero");     
            }else{
                if(Convert.ToBoolean(BetterChat?.Call("API_IsUserInGroup", player.UserIDString, "Bandit"))) BetterChat?.Call("API_RemoveUserFromGroup", player.UserIDString, "Bandit");
                if(Convert.ToBoolean(BetterChat?.Call("API_IsUserInGroup", player.UserIDString, "Hero"))) BetterChat?.Call("API_RemoveUserFromGroup", player.UserIDString, "Hero");
                if(Convert.ToBoolean(BetterChat?.Call("API_IsUserInGroup", player.UserIDString, "Neutral")) || playerData.Rank == 0) return "Neutral";
                BetterChat?.Call("API_AddUserToGroup", player.UserIDString, "Neutral");             
                humanityData.playerH[player.userID].Rank = 0;
                SendReply(player, lang.GetMessage("NowA", this), "Neutral");     
            }
            SaveData();
            return true;
        }


        public void CreateGroup(string groupName)
        {
            if(Convert.ToBoolean(BetterChat?.Call($"API_GroupExists", groupName))) return; 
            if(Convert.ToBoolean(BetterChat?.Call($"API_AddGroup", groupName))){
                Puts($"Created betterchat group - {groupName}");
                BetterChat?.Call($"API_SetGroupSetting", groupName, "priority", "500");
            }else Puts($"Failed to create group - {groupName}");
        }

        // / // / // / //
        //Save/Load Dat//
        // / // / // / //

        void Unload()
        {
            foreach(var entry in inUI)
            {
                BasePlayer player = BasePlayer.FindByID(entry);
                CuiHelper.DestroyUi(player, "HumanUI");
            }
        }
        
        void Loaded()
        {
            if(Convert.ToBoolean(Config["HeadText"])) CheckDis();
            if(BetterChat){
                Puts("Betterchat function loaded - Attempting to create groups...");
                if(Convert.ToBoolean(BetterChat?.Call($"API_GroupExists", "Hero"))) Puts("Betterchat groups already created...");
                else{
                    Puts("Creating betterchat groups...");
                    CreateGroup("Hero");
                    CreateGroup("Bandit");
                    CreateGroup("Neutral");}    
            }else Puts("Betterchat does not exist in plugins - Betterchat function disabled!");        
            humanityData = Interface.GetMod().DataFileSystem.ReadObject<HumanityData>(this.Title);
            lang.RegisterMessages(messages, this);
        }

        void SaveData() => Interface.Oxide.DataFileSystem.WriteObject(this.Title, humanityData);

        // / // / // / //
        //Language File//
        // / // / // / //

        Dictionary<string, string> messages = new Dictionary<string, string>()
        {
            {"Humanity", "{0}'s Stats: \n Humanity Rank: {1} \n Current Humanity: {2}"},
            {"NowA", "Congratulations! You now have the rank of: {0}"},
            {"Information", "HumanitySystem is the system based off of the DayZ mod humanity. DayZ's humanity system was a way to rank a player based on how they kill and how they interact with other players. \nRanks: \n *Hero(2500+ humanity) \n *Bandit(2500 and less humanity) \n *Neutral(Anything between hero and bandit)"},
        };

        // / // / // / //
        //OnPlayerInit //
        // / // / // / //
        
        void OnPlayerInit(BasePlayer player)
        {
            if(!humanityData.playerH.ContainsKey(player.userID))
            {
                var info = new players()
                {
                    playerID = player.userID,
                    Humanity = 0,
                    Rank = 0
                };
                humanityData.playerH.Add(player.userID, info);
                SaveData();
            }
            RankAlgorithm(player);
        }

        // / // / // / //
        //Chat Commands//
        // / // / // / //

        [ChatCommand("HStatus")] void HumanStat(BasePlayer player) => SendReply(player, lang.GetMessage("Humanity", this), player.displayName, GetRank(player), GetStat(player, "Humanity"));
        [ChatCommand("HInfo")] void HumanInfo(BasePlayer player) => SendReply(player, lang.GetMessage("Information", this));

        // / // / // / //
        //Death Handler//
        // / // / // / //   

        void OnEntityDeath(BaseCombatEntity victimEntity, HitInfo info)
        {
            if (info?.Initiator?.ToPlayer() != null && victimEntity?.ToPlayer() != null)
            {
                BasePlayer victim = victimEntity.ToPlayer();
                BasePlayer attacker = info.Initiator.ToPlayer();
                CuiHelper.DestroyUi(victim, "HumanUI");
                if(victim.userID == attacker.userID) return;

                if (players.Find(victim) == null)
                    OnPlayerInit(victim);

                if (players.Find(attacker) == null)
                    OnPlayerInit(attacker);

                players victimData = players.Find(victim);
                players attackerData = players.Find(attacker);

                victimData.Deaths++;
                attackerData.Kills++;

                if (victimData.Rank == 0 || victimData.Rank == 1)
                {
                    attackerData.Humanity -= Convert.ToInt32(Config["HumanityLossGainOnKill"]);
                    RankAlgorithm(attacker);
                }
                else if (victimData.Rank == 2)
                {
                    attackerData.Humanity += Convert.ToInt32(Config["HumanityLossGainOnKill"]);
                    RankAlgorithm(attacker);
                }
                SaveData();
            }
        }
        // / // / // / //
        //Rank Popup He//
        // / // / // / //

        void CheckDis()
        {
            try 
            {
                foreach(var player in BasePlayer.activePlayerList)
                {
                    BasePlayer nearbyP = null;
                        List<BaseEntity> nearby = new List<BaseEntity>();
                        Vis.Entities(player.transform.position, 20, nearby);
                        foreach (var ent in nearby)               
                            if (ent is BasePlayer)
                                nearbyP = ent.ToPlayer();
                                DrawChatMessage(player, nearbyP);
                }
                timer.Once(1, () => CheckDis());
            }
            catch(System.Exception) { return; }
        }
		void DrawChatMessage(BasePlayer player, BasePlayer nearby)
		{
            try 
            {
                    
                var rank = (string)("["+GetRank(nearby)+"]");
                Color messageColor = new Color(32,32,32,1);
                    
                if(!nearby.IsVisible(player.transform.position)) return;
                nearby.SendConsoleCommand("ddraw.text", 0.1f, messageColor, player.transform.position + new Vector3(0, 1.9f, 0),"<size=25>" + rank + "</size>");
                timer.Repeat(0.3f, 50, () =>
                {
                    nearby.SendConsoleCommand("ddraw.text", 0.1f, messageColor, player.transform.position + new Vector3(0, 1.9f, 0),"<size=25>" + rank + "</size>");
                });
            }
            catch(System.Exception) { return; }
		}

        // / // / // / //
        //CUI Elements //
        // / // / // / // 

        [ChatCommand("hmt")]
        private void RenderUI(BasePlayer player)
        {
            if(inUI.Contains(player.userID))
            {
                CuiHelper.DestroyUi(player, "HumanUI");
                inUI.Remove(player.userID);
                return;
            }
            inUI.Add(player.userID);
            var elements = new CuiElementContainer();
            var mainName = elements.Add(new CuiPanel
            {
                Image =
                {
                    Color = "0 0 0 0.0"
                },
                RectTransform =
                {
                    AnchorMin = GetMin(),
                    AnchorMax = GetMax()
                }
            }, "Hud", "HumanUI");

            FillElements(ref elements, mainName, Convert.ToInt32(GetStat(player, "Humanity")), GetRank(player).ToString(), Convert.ToInt32(GetStat(player, "Kills")), Convert.ToInt32(GetStat(player, "Deaths")));

            CuiHelper.AddUi(player, elements);
        }  

        private void FillElements(ref CuiElementContainer elements, string mainPanel, int humanity, string rank, int kills, int deaths)
        {
            //Color correction//
            var colorCorrection = new CuiElement
            {
                Name = CuiHelper.GetGuid(),
                Parent = mainPanel,
                Components =
                        {
                            new CuiImageComponent { Color = "102 102 102 0.1" },
                            new CuiRectTransformComponent{ AnchorMin = "0 1" , AnchorMax = $"1 0"}
                        }
            };
            elements.Add(colorCorrection);
            //Side bars//
            var sideBar1 = new CuiElement
            {
                Name = CuiHelper.GetGuid(),
                Parent = mainPanel,
                Components =
                        {
                            new CuiImageComponent { Color = "0.1 0.1 0.1 0.98" },
                            new CuiRectTransformComponent{ AnchorMin = "0 1" , AnchorMax = $"0.03 0"}
                        }
            };
            elements.Add(sideBar1);
            var sideBar2 = new CuiElement
            {
                Name = CuiHelper.GetGuid(),
                Parent = mainPanel,
                Components =
                        {
                            new CuiImageComponent { Color = "0.1 0.1 0.1 0.98" },
                            new CuiRectTransformComponent{ AnchorMin = "0.97 1" , AnchorMax = $"1 0"}
                        }
            };
            elements.Add(sideBar2);
            //Top-Bottom bar//
            var topBar1 = new CuiElement
            {
                Name = CuiHelper.GetGuid(),
                Parent = mainPanel,
                Components =
                        {
                            new CuiImageComponent { Color = "0.1 0.1 0.1 0.98" },
                            new CuiRectTransformComponent{ AnchorMin = "0 0.03" , AnchorMax = $"1 0"}
                        }
            };
            elements.Add(topBar1);
            var topBar2 = new CuiElement
            {
                Name = CuiHelper.GetGuid(),
                Parent = mainPanel,
                Components =
                        {
                            new CuiImageComponent { Color = "0.1 0.1 0.1 0.98" },
                            new CuiRectTransformComponent{ AnchorMin = "0 1" , AnchorMax = $"1 0.97"}
                        }
            };
            elements.Add(topBar2);    
            //Text//
            var humanityText = new CuiElement
            {
                Name = CuiHelper.GetGuid(),
                Parent = colorCorrection.Name,
                Components =
                        {
                            new CuiTextComponent { Text = "Humanity Status:", FontSize = 18, Align = TextAnchor.MiddleLeft, Color = "0 0 0" },
                            new CuiRectTransformComponent{ AnchorMin = "0.17 0.73", AnchorMax = $"1 0.95" }
                        }
            };
            elements.Add(humanityText);
            var humanityTText = new CuiElement
            {
                Name = CuiHelper.GetGuid(),
                Parent = colorCorrection.Name,
                Components =
                        {
                            new CuiTextComponent { Text = $"Humanity: {humanity}", FontSize = 15, Align = TextAnchor.MiddleLeft, Color = "0 0 0" },
                            new CuiRectTransformComponent{ AnchorMin = "0.05 0.57", AnchorMax = $"1 0.74" }
                        }
            };
            elements.Add(humanityTText);
            var humanityRank = new CuiElement
            {
                Name = CuiHelper.GetGuid(),
                Parent = colorCorrection.Name,
                Components =
                        {
                            new CuiTextComponent { Text = $"Rank: {rank}", FontSize = 15, Align = TextAnchor.MiddleLeft, Color = "0 0 0" },
                            new CuiRectTransformComponent{ AnchorMin = "0.05 0.49", AnchorMax = $"1 0.68" }
                        }
            };
            elements.Add(humanityRank);
            var humanityKills = new CuiElement
            {
                Name = CuiHelper.GetGuid(),
                Parent = colorCorrection.Name,
                Components =
                        {
                            new CuiTextComponent { Text = $"Kills: {kills}", FontSize = 15, Align = TextAnchor.MiddleLeft, Color = "0 0 0" },
                            new CuiRectTransformComponent{ AnchorMin = "0.05 0.41", AnchorMax = $"1 0.61" }
                        }
            };
            elements.Add(humanityKills);
            var humanityDeaths = new CuiElement
            {
                Name = CuiHelper.GetGuid(),
                Parent = colorCorrection.Name,
                Components =
                        {
                            new CuiTextComponent { Text = $"Deaths: {deaths}", FontSize = 15, Align = TextAnchor.MiddleLeft, Color = "0 0 0" },
                            new CuiRectTransformComponent{ AnchorMin = "0.05 0.31", AnchorMax = $"1 0.56" }
                        }
            };
            elements.Add(humanityDeaths);
        }        
    }
}