using Oxide.Core;
using Oxide.Core.Plugins;
using Rust;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Oxide.Plugins
{
    [Info("LevelAPI", "Jordi", 0.8, ResourceId = 1450)]
    [Description("Level up with killing entities and gahtering resources!")]
    public class LevelAPI : RustPlugin
    {
        [PluginReference("Economy")]
        Plugin Reconomy;
        #region [HudElemets]
        FieldInfo displayName = typeof(BasePlayer).GetField("_displayName", (BindingFlags.Instance | BindingFlags.NonPublic));
        public static string json_type_detailed = @"[
            {""name"": ""LevelMsg"",
                ""parent"": ""HUD/Overlay"",
                ""components"":
                [
                    {
                         ""type"":""UnityEngine.UI.Image"",
                         ""color"":""0.1 0.1 0.1 0.7"",
                    },
                    {
                        ""type"":""RectTransform"",
                        ""anchormin"": ""0.020 {miny}"",
                        ""anchormax"": ""0.15 0.90""
                    }
                ]
            },
            {
                ""parent"": ""LevelMsg"",
                ""components"":
                [
                    {
                        ""type"":""UnityEngine.UI.Text"",
                        ""color"":""0.1 0.9 0.1 0.7"",
                        ""text"":""Level: {CurrentLevel}"",
                        ""fontSize"":10,
                        ""align"": ""MiddleCenter"",
                    },
                    {
                        ""type"":""RectTransform"",
                        ""anchormin"": ""0 0.6"",
                        ""anchormax"": ""1 0.93""
                    }
                ]
            },
            {
                ""parent"": ""LevelMsg"",
                ""components"":
                [
                    {
                        ""type"":""UnityEngine.UI.Text"",
                        ""color"":""0.1 0.9 0.1 0.7"",
                        ""text"":""XP: {CurrentXP}/{XPNeeded}"",
                        ""fontSize"":10,
                        ""align"": ""MiddleCenter"",
                    },
                    {
                        ""type"":""RectTransform"",
                        ""anchormin"": ""0 {maxx}"",
                        ""anchormax"": ""1 0.6""
                    }
                ]
            },
            {
                ""parent"": ""LevelMsg"",
                ""components"":
                [
                    {
                        ""type"":""UnityEngine.UI.Text"",
                        ""color"":""0.9 0.9 0.9 0.7"",
                        ""text"":""{XPAdded}"",
                        ""fontSize"":10,
                        ""align"": ""MiddleCenter"",
                    },
                    {
                        ""type"":""RectTransform"",
                        ""anchormin"": ""0 0.1"",
                        ""anchormax"": ""1 0.3""
                    }
                ]
            }
        ]
        ";
        String jsonpart = @",
            {
                ""parent"": ""LevelMsg"",
                ""components"":
                [
                    {
                        ""type"":""UnityEngine.UI.Text"",
                        ""color"":""0.9 0.9 0.9 0.7"",
                        ""text"":""{XPAdded}"",
                        ""fontSize"":10,
                        ""align"": ""MiddleCenter"",
                    },
                    {
                        ""type"":""RectTransform"",
                        ""anchormin"": ""0 0.1"",
                        ""anchormax"": ""1 0.3""
                    }
                ]
            }";
        public static string json_type_xpbar = @"[
            {""name"": ""LevelMsg"",
                ""parent"": ""HUD/Overlay"",
                ""components"":
                [
                    {
                         ""type"":""UnityEngine.UI.Image"",
                         ""color"":""0.1 0.1 0.1 0.8"",
                    },
                    {
                        ""type"":""RectTransform"",
                        ""anchormin"": ""0.155 0.95"",
                        ""anchormax"": ""0.489 0.99""
                    }
                ]
            },
            {
                ""parent"": ""LevelMsg"",
                ""components"":
                [
                    {
                         ""type"":""UnityEngine.UI.Image"",
                         ""color"":""0.1 0.1 1 0.4"",
                    },
                    {
                        ""type"":""RectTransform"",
                        ""anchormin"": ""0.005 0.2"",
                        ""anchormax"": ""{barxend} 0.8""
                    }
                ]
            },
            {""parent"": ""LevelMsg"",
                ""components"":
                [
                    {
                         ""type"":""UnityEngine.UI.Image"",
                         ""color"":""0.1 0.1 1 0.6"",
                    },
                    {
                        ""type"":""RectTransform"",
                        ""anchormin"": ""0.005 0.2"",
                        ""anchormax"": ""{XPBAR} 0.8""
                    }
                ]
            },
            {
                ""parent"": ""LevelMsg"",
                ""components"":
                [
                    {
                        ""type"":""UnityEngine.UI.Text"",
                        ""color"":""1 1 1 1"",
                        ""text"":""Level: {CurrentLevel}, XP: {CurrentXP}/{XPNeeded}{STREAK}"",
                        ""fontSize"":10,
                        ""align"": ""MiddleCenter"",
                    },
                    {
                        ""type"":""RectTransform"",
                        ""anchormin"": ""0 0.02"",
                        ""anchormax"": ""1 0.98""
                    }
                ]
            }
        ]
        ";

        public Hash<BasePlayer, Timer> Timers = new Hash<BasePlayer, Timer>();
        public Hash<BasePlayer, Double> XPToAdd = new Hash<BasePlayer, Double>();
        public Hash<BasePlayer, Timer> XPToAddTimers = new Hash<BasePlayer, Timer>();
        public Hash<BasePlayer, Double> PlayerStreakTime = new Hash<BasePlayer, Double>();
        public Hash<BasePlayer, Boolean> PlayerStreakEnded = new Hash<BasePlayer, Boolean>();
        public Hash<BasePlayer, Boolean> PlayerSeeksInv = new Hash<BasePlayer, Boolean>();
        public void LoadMsgGui(BasePlayer ply)
        {
            String json2 = "";
            if (sd.HudType_Detailed_Bar.Equals("Bar"))
            {
                json2 = json_type_xpbar;
                json2 = json2.Replace("{XPNeeded}", SimpleRound((sd.XpNeededPerLevel_Will_be_Mutiplied_By_Level * sd.PlayerLevel[ply.UserIDString]) * sd.PlayerLevel[ply.UserIDString]));
                json2 = json2.Replace("{XPBAR}", (0.995 * (sd.PlayerXP[ply.UserIDString] / ((sd.XpNeededPerLevel_Will_be_Mutiplied_By_Level * sd.PlayerLevel[ply.UserIDString]) * sd.PlayerLevel[ply.UserIDString]))).ToString());
                if ((XPToAdd[ply] == 0) == false)
                {
                    Double xpadd = XPToAdd[ply];
                    if (sd.MulitiplyByStreakAndLevel)
                    {
                        xpadd = xpadd * sd.PlayerLevel[ply.UserIDString];
                    }
                    json2 = json2.Replace("{STREAK}", ", Streak: +" + SimpleRound(xpadd) + "XP (Streak time left: " + Math.Round(PlayerStreakTime[ply]).ToString() + ")");
                    Double XPbarEnd = 0.0;
                    Double XPbar = (0.995 * (sd.PlayerXP[ply.UserIDString] / ((sd.XpNeededPerLevel_Will_be_Mutiplied_By_Level * sd.PlayerLevel[ply.UserIDString]) * sd.PlayerLevel[ply.UserIDString])));
                    XPbarEnd = ((0.995 * ((sd.PlayerXP[ply.UserIDString] + (XPToAdd[ply] * sd.PlayerLevel[ply.UserIDString]))) / ((sd.XpNeededPerLevel_Will_be_Mutiplied_By_Level * sd.PlayerLevel[ply.UserIDString]) * sd.PlayerLevel[ply.UserIDString])));
                    if (XPbarEnd > 0.995)
                    {
                        XPbarEnd = 0.995;
                    }
                    json2 = json2.Replace("{barxstart}", XPbar.ToString());
                    json2 = json2.Replace("{barxend}", XPbarEnd.ToString());
                }
                else
                {
                    json2 = json2.Replace("{STREAK}", "");
                    json2 = json2.Replace("{barxstart}", "0.0");
                    json2 = json2.Replace("{barxend}", "0.0");
                }
            }
            else if (sd.HudType_Detailed_Bar.Equals("Detailed"))
            {
                if ((XPToAdd[ply] == 0) == false)
                {
                    Double xpadd = XPToAdd[ply];
                    if (sd.MulitiplyByStreakAndLevel)
                    {
                        xpadd = xpadd * sd.PlayerLevel[ply.UserIDString];
                    }
                    json2 = json_type_detailed.Replace("{XPAdded}", "+" + xpadd + "XP");
                    json2 = json2.Replace("{miny}", "0.8");
                    json2 = json2.Replace("{maxx}", "0.32");
                }
                else
                {
                    json2 = json_type_detailed.Replace(jsonpart, "");
                    json2 = json2.Replace("{miny}", "0.826");
                    json2 = json2.Replace("{maxx}", "0.1");
                }
                json2 = json2.Replace("{XPNeeded}", ((sd.XpNeededPerLevel_Will_be_Mutiplied_By_Level * sd.PlayerLevel[ply.UserIDString]) * sd.PlayerLevel[ply.UserIDString]).ToString());
            }
            if ((ply.inventory.loot.IsLooting() || PlayerSeeksInv[ply]) == false)
            {
                Game.Rust.Cui.CuiHelper.DestroyUi(ply, "LevelMsg");
                if (sd.HudShowingWhen_PlayerHasStreak_Always_Never.Equals("Always")){
                    CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo { connection = ply.net.connection }, null, "AddUI", new Facepunch.ObjectList(json2.Replace("{CurrentLevel}", sd.PlayerLevel[ply.UserIDString].ToString()).Replace("{CurrentXP}", SimpleRound(sd.PlayerXP[ply.UserIDString]).ToString())));
                }
                if (sd.HudShowingWhen_PlayerHasStreak_Always_Never.Equals("PlayerHasStreak")){
                    if ((XPToAdd[ply] == 0) == false){
                        CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo { connection = ply.net.connection }, null, "AddUI", new Facepunch.ObjectList(json2.Replace("{CurrentLevel}", sd.PlayerLevel[ply.UserIDString].ToString()).Replace("{CurrentXP}", SimpleRound(sd.PlayerXP[ply.UserIDString]).ToString())));
                    }
                }
            }
            else
            {
                Game.Rust.Cui.CuiHelper.DestroyUi(ply, "LevelMsg");
            }
        }

        public void DestroyGui(BasePlayer player)
        {
            Game.Rust.Cui.CuiHelper.DestroyUi(player, "LevelMsg");
        }
        #endregion
        #region [Config]
        public Hash<BasePlayer, String> OldNames = new Hash<BasePlayer, String>();
        Timer ConfigReloader = null;
        StoredData sd = new StoredData();
        class StoredData
        {
            public Hash<String, Double> OnGather = new Hash<String, Double>();
            public Hash<String, Double> OnUpgrade = new Hash<String, Double>();
            public Hash<String, Double> PlayerXP = new Hash<String, Double>();
            public Hash<String, Double> PlayerLevel = new Hash<String, Double>();
            public Hash<String, String> PlayerName = new Hash<String, String>();
            public String HudShowingWhen_PlayerHasStreak_Always_Never = "Always";
            public Double XpNeededPerLevel_Will_be_Mutiplied_By_Level = 100;
            public Double MoneyPerLevel_Will_be_Mutiplied_By_Level = 12.75;
            public Boolean MulitiplyByStreakAndLevel = true;
            public float StreakTimeEnd = 10.0f;
            public String HudType_Detailed_Bar = "Bar";
            public int ScaleDamageGive = 1;
            public int MaxScaleDamageGive = 100;
            public int ScaleDamageReceive = 10;
            public int MaxScaleDamageReceive = 100;
            public StoredData()
            {
            }


        }

        void Init()
        {
            reloadconfig();
            saveconfig();
            ConfigReloader = timer.Repeat(30, 0, () =>
            {
                saveconfig();
                sd = Interface.GetMod().DataFileSystem.ReadObject<StoredData>("LevelAPI");
            });
            foreach (BasePlayer ply in BasePlayer.activePlayerList)
            {
                if (sd.PlayerName.ContainsKey(ply.UserIDString) == false)
                {
                    sd.PlayerName[ply.UserIDString] = ply.displayName;
                }
                else
                {
                    if (sd.PlayerName[ply.UserIDString].Contains("[Lvl "))
                    {
                        String name = ply.displayName;
                        for (int i = 0; i < 100; i++)
                        {
                            name = name.Replace("[Lvl " + i.ToString() + "]", "");
                        }
                        sd.PlayerName[ply.UserIDString] = name;
                    }
                }
                Timers.Add(ply, timer.Repeat(0.5F, 0, () =>
                {
                    LoadMsgGui(ply);
                    String name = sd.PlayerName[ply.UserIDString];
                    for (int i = 0; i < 100; i++)
                    {
                        name = name.Replace("[Lvl " + i.ToString() + "]", "");
                    }
                    displayName.SetValue(ply, name.Replace("[God]", "") + "[Lvl " + sd.PlayerLevel[ply.UserIDString] + "]");
                    ply.SendNetworkUpdate();
                }));
                PlayerStreakEnded[ply] = false;
                XPToAddTimers.Add(ply, timer.Repeat(0.1f, 0, () =>
                {
                    Streak(ply, 0.1);
                }));
            }
        }
        void Unload()
        {
            foreach (Timer tm in Timers.Values)
            {
                tm.Destroy();
            }
            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                String name = sd.PlayerName[player.UserIDString];
                for (int i = 0; i < 100; i++)
                {
                    name = name.Replace("[Lvl " + i.ToString() + "]", "");
                }
                displayName.SetValue(player, name.Replace("[God]", ""));
            }
            ConfigReloader.Destroy();
            saveconfig();
        }
        public void reloadconfig()
        {
            sd = Interface.GetMod().DataFileSystem.ReadObject<StoredData>("LevelAPI");
        }
        public void saveconfig()
        {
            Interface.GetMod().DataFileSystem.WriteObject<StoredData>("LevelAPI", sd);
        }
        #endregion
        #region [Hooks]
        void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            try
            {
                if (info.Initiator.ToPlayer().IsValid())
                {
                    BasePlayer ply = info.Initiator.ToPlayer();
                    Dictionary<DamageType, float> dmg = new Dictionary<DamageType, float>();
                    float EdittedTotal = 0;
                    float total = info.damageTypes.Total();
                    float scale = sd.ScaleDamageGive;
                    float hund = 100;
                    float scalecal = scale / hund;
                    int DamageTypeMax = (int)DamageType.LAST;
                    for (var i = 0; i < DamageTypeMax; i++)
                    {
                        EdittedTotal = info.damageTypes.Get((DamageType)i);
                        Double level = sd.PlayerLevel[ply.UserIDString];
                        if (level > sd.MaxScaleDamageGive)
                        {
                            level = sd.MaxScaleDamageGive;
                        }
                        for (int x = 0; x < level; x++)
                        {
                            EdittedTotal = EdittedTotal + (EdittedTotal * scalecal);
                        }
                        info.damageTypes.Set((DamageType)i, EdittedTotal);
                    }
                }
                if (entity.ToPlayer().IsValid())
                {
                    BasePlayer ply = entity.ToPlayer();
                    float EdittedTotal = 0;
                    float total = info.damageTypes.Total();
                    float scale = sd.ScaleDamageGive;
                    float hund = 100;
                    float scalecal = scale / hund;
                    int DamageTypeMax = (int)DamageType.LAST;
                    for (var i = 0; i < DamageTypeMax; i++)
                    {
                        EdittedTotal = info.damageTypes.Get((DamageType)i);
                        float NotEdittedTotal = info.damageTypes.Get((DamageType)i);
                        Double level = sd.PlayerLevel[ply.UserIDString];
                        for (int x = 0; x < level; x++)
                        {
                            if (EdittedTotal <= (NotEdittedTotal * (sd.MaxScaleDamageReceive / 100)))
                            {
                                EdittedTotal = EdittedTotal - (EdittedTotal * scalecal);
                            }
                        }
                        info.damageTypes.Set((DamageType)i, EdittedTotal);
                    }
                }
            }
            catch
            {

            }
            return;
        }
        void OnStructureUpgrade(BuildingBlock block, BasePlayer player, BuildingGrade.Enum grade)
        {
            if (sd.OnUpgrade.ContainsKey("From" + block.grade.ToString() + "To" + grade.ToString()) == false)
            {
                sd.OnUpgrade.Add("From" + block.grade.ToString() + "To" + grade.ToString(), 1);
            }
            else
            {
                    if (sd.PlayerXP.ContainsKey(player.UserIDString) == false)
                    {
                        sd.PlayerXP.Add(player.UserIDString, 0);
                    }
                    if (sd.PlayerLevel.ContainsKey(player.UserIDString) == false)
                    {
                        sd.PlayerLevel.Add(player.UserIDString, 1);
                    }
                    XPToAdd[player] = XPToAdd[player] + sd.OnUpgrade["From" + block.grade.ToString() + "To" + grade.ToString()];
                    PlayerStreakEnded[player] = false;
                    PlayerStreakTime[player] = sd.StreakTimeEnd;
                    if (sd.PlayerXP[player.UserIDString] >= (sd.XpNeededPerLevel_Will_be_Mutiplied_By_Level * sd.PlayerLevel[player.UserIDString]) * sd.PlayerLevel[player.UserIDString])
                    {
                        sd.PlayerXP[player.UserIDString] = 0;
                        sd.PlayerLevel[player.UserIDString] = sd.PlayerLevel[player.UserIDString] + 1;
                        Reconomy.Call("GiveMoney", player, sd.MoneyPerLevel_Will_be_Mutiplied_By_Level * sd.PlayerLevel[player.UserIDString], "You leveled up!", true);
                    }
            }
        }
        void OnDispenserGather(ResourceDispenser dispenser, BaseEntity entity, Item item)
        {
            if (sd.OnGather.ContainsKey(item.info.displayName.english) == false)
            {
                sd.OnGather.Add(item.info.displayName.english, 0.1);
            }
            else
            {
                if (entity.ToPlayer().IsValid())
                {
                    if (sd.PlayerXP.ContainsKey(entity.ToPlayer().UserIDString) == false)
                    {
                        sd.PlayerXP.Add(entity.ToPlayer().UserIDString, 0);
                    }
                    if (sd.PlayerLevel.ContainsKey(entity.ToPlayer().UserIDString) == false)
                    {
                        sd.PlayerLevel.Add(entity.ToPlayer().UserIDString, 1);
                    }
                    XPToAdd[entity.ToPlayer()] = XPToAdd[entity.ToPlayer()] + sd.OnGather[item.info.displayName.english];
                    PlayerStreakEnded[entity.ToPlayer()] = false;
                    PlayerStreakTime[entity.ToPlayer()] = sd.StreakTimeEnd;
                    if (sd.PlayerXP[entity.ToPlayer().UserIDString] >= (sd.XpNeededPerLevel_Will_be_Mutiplied_By_Level * sd.PlayerLevel[entity.ToPlayer().UserIDString]) * sd.PlayerLevel[entity.ToPlayer().UserIDString])
                    {
                        sd.PlayerXP[entity.ToPlayer().UserIDString] = 0;
                        sd.PlayerLevel[entity.ToPlayer().UserIDString] = sd.PlayerLevel[entity.ToPlayer().UserIDString] + 1;
                        Reconomy.Call("GiveMoney", entity.ToPlayer(), sd.MoneyPerLevel_Will_be_Mutiplied_By_Level * sd.PlayerLevel[entity.ToPlayer().UserIDString], "You leveled up!", true);
                    }
                }
            }
        }
        public void Streak(BasePlayer ply, Double Take)
        {
            if ((PlayerStreakTime[ply] <= 0.1) == false)
            {
                PlayerStreakTime[ply] = PlayerStreakTime[ply] - Take;
            }
            else
            {
                if (PlayerStreakEnded[ply] == false)
                {
                    StreakEnded(ply);
                    PlayerStreakEnded[ply] = true;
                }
            }
        }
        public void StreakEnded(BasePlayer ply)
        {
            if (sd.MulitiplyByStreakAndLevel)
            {
                SendReply(ply, "[LevelAPI]: You ended a streak and received: " + (XPToAdd[ply] * sd.PlayerLevel[ply.UserIDString]).ToString() + "XP");
                sd.PlayerXP[ply.UserIDString] = sd.PlayerXP[ply.UserIDString] + (XPToAdd[ply] * sd.PlayerLevel[ply.UserIDString]);
                if (sd.PlayerXP[ply.UserIDString] >= (sd.XpNeededPerLevel_Will_be_Mutiplied_By_Level * sd.PlayerLevel[ply.UserIDString]) * sd.PlayerLevel[ply.UserIDString])
                {
                    sd.PlayerXP[ply.UserIDString] = 0;
                    sd.PlayerLevel[ply.UserIDString] = sd.PlayerLevel[ply.UserIDString] + 1;
                    Reconomy.Call("GiveMoney", ply, sd.MoneyPerLevel_Will_be_Mutiplied_By_Level * sd.PlayerLevel[ply.UserIDString], "You leveled up!", true);
                }
            }
            else
            {
                SendReply(ply, "[LevelAPI]: You ended a streak and received: " + XPToAdd[ply].ToString() + "XP");
                sd.PlayerXP[ply.UserIDString] = sd.PlayerXP[ply.UserIDString] + XPToAdd[ply];
                if (sd.PlayerXP[ply.UserIDString] >= (sd.XpNeededPerLevel_Will_be_Mutiplied_By_Level * sd.PlayerLevel[ply.UserIDString]) * sd.PlayerLevel[ply.UserIDString])
                {
                    sd.PlayerXP[ply.UserIDString] = 0;
                    sd.PlayerLevel[ply.UserIDString] = sd.PlayerLevel[ply.UserIDString] + 1;
                    Reconomy.Call("GiveMoney", ply, sd.MoneyPerLevel_Will_be_Mutiplied_By_Level * sd.PlayerLevel[ply.UserIDString], "You leveled up!", true);
                }
            }
            XPToAdd[ply] = 0;
        }
        void OnPlayerInit(BasePlayer player)
        {
            PlayerStreakEnded[player] = false;
            XPToAddTimers[player] = timer.Repeat(0.1f,0, () =>
                        {
                            Streak(player, 0.1);
                        });
            if (player.displayName.Contains("[Lvl ") == false)
            {
                sd.PlayerName[player.UserIDString] = player.displayName;
                if (sd.PlayerName[player.UserIDString].Contains("[Lvl "))
                {
                    String name = player.displayName;
                    for (int i = 0; i < 100; i++)
                    {
                        name = name.Replace("[Lvl " + i.ToString() + "]", "");
                    }
                    sd.PlayerName[player.UserIDString] = name;
                }
            }
            Timers[player] =  timer.Repeat(0.5F , 0 ,() =>
            {
                LoadMsgGui(player);
                String name = sd.PlayerName[player.UserIDString];
                for (int i = 0; i < 100; i++)
                {
                    name = name.Replace("[Lvl " + i.ToString() + "]", "");
                }
                displayName.SetValue(player, name.Replace("[God]", "") + "[Lvl " + sd.PlayerLevel[player.UserIDString] + "]");
                player.SendNetworkUpdate();
            });
        }
        void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            if (XPToAddTimers.ContainsKey(player))
            {
                XPToAddTimers[player].Destroy();
            }
            XPToAddTimers[player].Destroy();
            Timers[player].Destroy();
            Timers.Remove(player);
        }
        #endregion

        String SimpleRound(double intr)
        {
            if ((intr / 1000000000) >= 1)
            {
                Double divided = (intr / 1000000000);
                return Math.Round(divided, 2).ToString() + "B";
            }
            if ((intr / 1000000) >= 1)
            {
                Double divided = (intr / 1000000);
                return Math.Round(divided, 2).ToString() + "M";
            }
            if ((intr / 1000) >= 1)
            {
                Double divided = (intr / 1000);
                return Math.Round(divided, 2).ToString() + "K";
            }
            intr = Math.Round(intr, 2);
            return intr.ToString();
        }
    }
}