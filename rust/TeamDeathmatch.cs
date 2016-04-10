using System.Collections.Generic;
using System;
using System.Data;
using UnityEngine;
using Oxide.Core;
using Oxide.Core.Plugins;
using System.Timers;
using Rust;
using Oxide.Game.Rust.Cui;

namespace Oxide.Plugins
{
    [Info("Team Deathmatch", "k1lly0u", "0.2.04", ResourceId = 1484)]
    class TeamDeathmatch : RustPlugin
    {
        ////////////////////////////////////////////////////////////
        // Setting all fields //////////////////////////////////////
        ////////////////////////////////////////////////////////////
        [PluginReference]
        Plugin EventManager;

        [PluginReference]
        Plugin ZoneManager;

        [PluginReference]
        Plugin Spawns;

        private bool UseTDM;
        private bool Started;
        private bool Changed;

        public string Kit;

        public int TeamAKills;
        public int TeamBKills;

        private List<TDMPlayer> TDMPlayers = new List<TDMPlayer>();
        
        #region oxide hooks
        //////////////////////////////////////////////////////////////////////////////////////
        // Oxide Hooks ///////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////
        void Loaded()
        {
            lang.RegisterMessages(messages, this);
            UseTDM = false;
            Started = false;
        }
        void OnServerInitialized()
        {
            if (EventManager == null)
            {
                Puts("Event plugin doesn't exist");
                return;
            }
            LoadVariables();
            RegisterGame();
        }
        void RegisterGame()
        {
            var success = EventManager.Call("RegisterEventGame", new object[] { GameName });
            if (success == null)
            {
                Puts("Event plugin doesn't exist");
                return;
            }
        }
        void LoadDefaultConfig()
        {
            Puts("Event Deathmatch: Creating a new config file");
            Config.Clear();
            LoadVariables();
        }
        void Unload()
        {
            if (UseTDM)
            {
                if (Started) EventManager.Call("EndEvent", new object[] { });
                foreach (TDMPlayer p in TDMPlayers) DestroyAllUI(p);                
                var objects = GameObject.FindObjectsOfType(typeof(TDMPlayer));
                if (objects != null)
                    foreach (var gameObj in objects)
                        GameObject.Destroy(gameObj);
            }
        }
        private void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo hitinfo)
        {
            if (UseTDM && Started)
            {
                try
                {
                    if (entity is BasePlayer && hitinfo.Initiator is BasePlayer)
                    {
                        var victim = entity.GetComponent<TDMPlayer>();
                        var attacker = hitinfo.Initiator.GetComponent<TDMPlayer>();
                        if (victim.player.userID != attacker.player.userID)
                        {
                            if (victim.team == attacker.team)
                            {
                                hitinfo.damageTypes.ScaleAll(ffDamageMod);
                                SendReply(attacker.player, lang.GetMessage("title", this, attacker.player.UserIDString) + lang.GetMessage("ff", this, attacker.player.UserIDString));
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                }
            }
        }
        #endregion       

        #region eventmanager hooks
        //////////////////////////////////////////////////////////////////////////////////////
        // Beginning Of Event Manager Hooks //////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////
        void OnSelectEventGamePost(string name)
        {
            if (GameName == name)
            {
                if (SpawnsA != null && SpawnsA != "" && SpawnsB != null && SpawnsB != "")
                {
                    UseTDM = true;
                    EventManager.Call("SelectSpawnfile", new object[] { SpawnsA });
                }
                else Puts("Check your config for valid spawn entries");
            }
            else
                UseTDM = false;
        }
        void OnEventPlayerSpawn(BasePlayer player)
        {
            if (UseTDM && Started)
            {
                timer.Once(3, () =>
                {
                    if (player.IsSleeping()) player.EndSleeping();
                });
                if (!player.GetComponent<TDMPlayer>()) return;

                GiveTeamGear(player);
                
                TDMPlayer.GetGUI(player).DestroyNotification();
                TDMPlayer.GetGUI(player).UseUI(KillLimit.ToString(), TeamAKills.ToString(), TeamBKills.ToString());

            }
        }
        private void GiveTeamGear(BasePlayer player)
        {
            player.inventory.Strip();
            EventManager.Call("GivePlayerKit", new object[] { player, Kit });
            player.health = StartHealth;
            if (player.GetComponent<TDMPlayer>().team == Team.A) Give(player, BuildItem(TeamAShirt, 1, TeamASkin));
            else if (player.GetComponent<TDMPlayer>().team == Team.B) Give(player, BuildItem(TeamBShirt, 1, TeamBSkin));
        }
       
        object OnSelectSpawnFile(string name)
        {
            if (UseTDM) return true;
            return null;
        }
        void OnSelectEventZone(MonoBehaviour monoplayer, string radius)
        {
            if (UseTDM)
            {
                return;
            }
        }
        void OnPostZoneCreate(string name)
        {
            if (name == GameName)
            {
                return;
            }
        }
        private object CanEventOpen()
        {
            if (UseTDM)
            {
                bool spawns = CheckSpawnfiles();
                if (!spawns) return "error";
                return true;
            }
            return null;
        }
        private bool CheckSpawnfiles()
        {
            object successA = Spawns.Call("GetSpawnsCount", new object[] { SpawnsA });
            object successB = Spawns.Call("GetSpawnsCount", new object[] { SpawnsB });

            if (successA is string)
            {
                SpawnsA = null;
                Puts("Error finding the Team A spawn file");
                return false;
            }
            if (successB is string)
            {
                SpawnsB = null;
                Puts("Error finding the Team B spawn file");
                return false;
            }
            return true;
        }
        object CanEventStart()
        {
            return null;
        }
        object OnEventOpenPost()
        {
            if (UseTDM)
                MessageAll(lang.GetMessage("title", this) + lang.GetMessage("OpenMsg", this));
            return null;
        }
        object OnEventClosePost()
        {
            return null;
        }
        object OnEventEndPre()
        {
            if (UseTDM)
            {
                foreach (TDMPlayer p in TDMPlayers)
                {
                    p.team = Team.NONE;
                    p.kills = 0;                    
                    DestroyAllUI(p);
                    GameObject.Destroy(p);
                }

                Started = false;
                TDMPlayers.Clear();
                TeamAKills = 0;
                TeamBKills = 0;                
            }
            return null;
        }
        object OnEventEndPost()
        {
            var objects = GameObject.FindObjectsOfType(typeof(TDMPlayer));
            if (objects != null)
                foreach (var gameObj in objects)
                    GameObject.Destroy(gameObj);
            return null;
        }
        object OnEventStartPre()
        {
            if (UseTDM) Started = true;
            return null;
        }
        object OnEventStartPost()
        {
            RefreshSB();
            return null;
        }
        object CanEventJoin(BasePlayer player)
        {
            if (player.GetComponent<TDMPlayer>())
                player.GetComponent<TDMPlayer>().team = Team.NONE;
            return null;
        }
        object OnSelectKit(string kitname)
        {
            if (UseTDM)
            {
                Kit = kitname;
                return true;
            }
            return null;
        }
        object OnEventJoinPost(BasePlayer player)
        {
            if (UseTDM && Started)
            {
                OnEventPlayerSpawn(player);
                RefreshSB();
            }
            return null;
        }
        object OnEventLeavePost(BasePlayer player)
        {
            if (UseTDM)
            {
                if (player.GetComponent<TDMPlayer>())
                {
                    DestroyAllUI(player.GetComponent<TDMPlayer>());
                    TDMPlayers.Remove(player.GetComponent<TDMPlayer>());
                    GameObject.Destroy(player.GetComponent<TDMPlayer>());
                    if (Started)
                        CheckScores();
                }
            }
            return null;
        }
        void OnEventPlayerAttack(BasePlayer attacker, HitInfo hitinfo)
        {
            if (UseTDM)
            {
                if (!(hitinfo.HitEntity is BasePlayer))
                {
                    hitinfo.damageTypes = new DamageTypeList();
                    hitinfo.DoHitEffects = false;
                }
            }
        }
        void OnEventPlayerDeath(BasePlayer vic, HitInfo hitinfo)
        {
            if (UseTDM && Started)
            {
                if (hitinfo.Initiator != null && vic != null)
                {
                    if (vic.GetComponent<TDMPlayer>()) DestroyAllUI(vic.GetComponent<TDMPlayer>());
                    if (vic is BasePlayer && hitinfo.Initiator is BasePlayer)
                    {
                        var victim = vic.GetComponent<TDMPlayer>();
                        var attacker = hitinfo.Initiator.GetComponent<TDMPlayer>();
                        if ((victim.player.userID != attacker.player.userID) && (attacker.team != victim.team))
                        {
                            AddKill(attacker.player, victim.player);
                        }
                    }
                }
            }
            return;
        }
        private void RefreshSB()
        {
            foreach (TDMPlayer p in TDMPlayers)
            {
                DestroyAllUI(p);
                TDMPlayer.GetGUI(p.player).UseUI(KillLimit.ToString(), TeamAKills.ToString(), TeamBKills.ToString());
            }
        }
        object EventChooseSpawn(BasePlayer player, Vector3 destination)
        {
            if (UseTDM)
            {
                if (!CheckForTeam(player))
                {
                    TeamAssign(player);
                    OnEventPlayerSpawn(player);                    
                }
                Team team = player.GetComponent<TDMPlayer>().team;
                object newpos = null;
                if (team == Team.A) newpos = Spawns.Call("GetRandomSpawn", new object[] { SpawnsA });
                else if (team == Team.B) newpos = Spawns.Call("GetRandomSpawn", new object[] { SpawnsB });
                if (!(newpos is Vector3))
                {
                    Puts("Error finding a spawn point, spawnfile corrupt or invalid");
                    return null;
                }
                return (Vector3)newpos;
            }
            return null;
        }
        object OnRequestZoneName()
        {
            if (UseTDM)
            {
                return ZoneName;
            }
            return null;
        }
        #endregion

        #region team funtions
        enum Team
        {
            A,
            B,
            NONE
        }
        private bool CheckForTeam(BasePlayer player)
        {
            if (!player.GetComponent<TDMPlayer>()) TDMPlayers.Add(player.gameObject.AddComponent<TDMPlayer>());
            if (player.GetComponent<TDMPlayer>().team == Team.NONE) return false;
            else return true;
        }
        private void TeamAssign(BasePlayer player)
        {
            if (UseTDM)
            {
                Team team = CountForBalance();
                player.GetComponent<TDMPlayer>().team = team;
                string color = "";
                if (team == Team.A) color = ColorA;
                else if (team == Team.B) color = ColorB;
                SendReply(player, string.Format("You have been assigned to <color=" + color + ">Team {0}</color>", team));
                Puts("Player " + player.displayName.ToString() + " assigned to Team " + team);
            }
        }
        private Team CountForBalance()
        {
            Team PlayerNewTeam = Team.NONE;
            int aCount = Count(Team.A);
            int bCount = Count(Team.B);

            if (aCount > bCount) PlayerNewTeam = Team.B;
            else PlayerNewTeam = Team.A;

            return PlayerNewTeam;
        }
        private int Count(Team team)
        {
            int count = 0;
            foreach (var player in TDMPlayers)
            {
                if (player.team == team) count++;
            }
            return count;
        }
        #endregion

        #region functions
        private void DestroyAllUI(TDMPlayer p)
        {
            TDMPlayer.GetGUI(p.player).DestroyNotification();
        }
        private void Give(BasePlayer player, Item item)
        {
            player.inventory.GiveItem(item, player.inventory.containerWear);
        }
        private Item BuildItem(string shortname, int amount, object skin)
        {
            var definition = ItemManager.FindItemDefinition(shortname);
            if (definition != null)
            {
                if (skin != null)
                {
                    Item item = ItemManager.CreateByItemID((int)definition.itemid, amount, false, (int)skin);
                    return item;
                }
                else
                {
                    Item item = ItemManager.CreateByItemID((int)definition.itemid, amount, false);
                    return item;
                }
            }
            return null;
        }
        private void MessagePlayers(string msg)
        {
            if (UseTDM && Started)
            {
                foreach (var p in TDMPlayers)
                {
                    SendReply(p.player, msg);
                }
            }
        }
        private void MessageAll(string msg)
        {
            foreach (var p in BasePlayer.activePlayerList)
            {
                SendReply(p, msg);
            }
        }

        List<TDMPlayer> FindPlayer(string arg)
        {
            var foundPlayers = new List<TDMPlayer>();

            ulong steamid;
            ulong.TryParse(arg, out steamid);
            string lowerarg = arg.ToLower();

            foreach (var p in TDMPlayers)
            {
                if (steamid != 0L)
                    if (p.player.userID == steamid)
                    {
                        foundPlayers.Clear();
                        foundPlayers.Add(p);
                        return foundPlayers;
                    }
                string lowername = p.player.displayName.ToLower();
                if (lowername.Contains(lowerarg))
                {
                    foundPlayers.Add(p);
                }
            }
            return foundPlayers;
        }       
        #endregion

        #region commands
        ////////////////////////////////////////////////////////////
        // Console Commands ////////////////////////////////////////
        ////////////////////////////////////////////////////////////

        [ConsoleCommand("tdm.spawns.a")]
        void ccmdSpawnsA(ConsoleSystem.Arg arg)
        {
            if (!isAuth(arg)) return;
            if (arg.Args == null || arg.Args.Length == 0)
            {
                SendReply(arg, "tdm.spawns.a \"filename\"");
                return;
            }
            object success = EventManager.Call("SelectSpawnfile", ((string)arg.Args[0]));
            if (success is string)
            {
                SendReply(arg, (string)success);
                return;
            }
            SpawnsA = arg.Args[0];
            SaveConfig();
            SendReply(arg, string.Format("Team A spawnfile is now {0} .", SpawnsA));
        }
             
        [ConsoleCommand("tdm.spawns.b")]
        void ccmdSpawnsB(ConsoleSystem.Arg arg)
        {
            if (!isAuth(arg)) return;
            if (arg.Args == null || arg.Args.Length == 0)
            {
                SendReply(arg, "tdm.spawns.b \"filename\"");
                return;
            }
            object success = EventManager.Call("SelectSpawnfile", ((string)arg.Args[0]));
            if (success is string)
            {
                SendReply(arg, (string)success);
                return;
            }
            SpawnsB = arg.Args[0];
            SaveConfig();
            SendReply(arg, string.Format("Team B spawnfile is now {0} .", SpawnsB));
        }

        [ConsoleCommand("tdm.kills")]
        void ccmdKills(ConsoleSystem.Arg arg)
        {
            if (!isAuth(arg)) return;
            if (arg.Args == null || arg.Args.Length == 0)
            {
                SendReply(arg, "tdm.kills XX ");
                return;
            }
            if (!int.TryParse(arg.Args[0], out KillLimit))
            {
                SendReply(arg, "The kill count needs to be a number");
                return;
            }
            if (arg.Args.Length == 1)
            {
                int newKills = int.Parse(arg.Args[0]);
                newKills = Convert.ToInt32(KillLimit);
            }
            SaveConfig();
            SendReply(arg, string.Format("Kill count to win event is now {0} .", KillLimit));
        }

        [ConsoleCommand("tdm.team")]
        private void cmdTeam(ConsoleSystem.Arg arg)
        {
            if (!UseTDM) return;
            if (!isAuth(arg)) return;
            if (arg.Args == null || arg.Args.Length != 2)
            {
                SendReply(arg, "Format: tdm.team \"playername\" \"A\" or \"B\"");
                return;
            }
            var fplayer = FindPlayer(arg.Args[0]);
            if (fplayer.Count == 0)
            {
                SendReply(arg, "No players found.");
                return;
            }
            if (fplayer.Count > 1)
            {
                SendReply(arg, "Multiple players found.");
                return;
            }
            var newTeamArg = arg.Args[1].ToUpper();
            var newTeam = Team.NONE;
            switch (newTeamArg)
            {
                case "A":
                    newTeam = Team.A;
                    break;

                case "B":
                    newTeam = Team.B;
                    break;
            }
            var p = fplayer[0].GetComponent<TDMPlayer>();
            var currentTeam = p.team;

            if (newTeam == currentTeam)
            {
                SendReply(arg, p.player.displayName + " is already on " + currentTeam);
                return;
            }
            p.team = newTeam;
            p.player.Hurt(300, DamageType.Bullet, null, true);

            string color = "";
            if (p.team == Team.A) color = ColorA;
            else if (p.team == Team.B) color = ColorB;

            SendReply(p.player, string.Format(lang.GetMessage("title", this) + "You have been moved to <color=" + color + ">Team {0}</color>", newTeam.ToString().ToUpper()));
            SendReply(arg, string.Format("{0} has been moved to Team {1}", p.player.displayName, newTeam.ToString().ToUpper()));
        }        
        
        bool isAuth(ConsoleSystem.Arg arg)
        {
            if (arg.connection != null)
            {
                if (arg.connection.authLevel < 1)
                {
                    SendReply(arg, "You dont not have permission to use this command.");
                    return false;
                }
            }
            return true;
        }
        #endregion

        #region scoring
        //////////////////////////////////////////////////////////////////////////////////////
        // End Of Event Manager Hooks ////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////

        void AddKill(BasePlayer player, BasePlayer victim)
        {
            var p = player.GetComponent<TDMPlayer>();
            if (!p) return;

            p.kills++;
            if (p.team == Team.A) TeamAKills++;
            else if (p.team == Team.B) TeamBKills++;
            RefreshSB();

            EventManager.Call("AddTokens", player.UserIDString, KillTokens);
            string color = "";
            if (player.GetComponent<TDMPlayer>().team == Team.A) color = ColorA;
            else if (player.GetComponent<TDMPlayer>().team == Team.B) color = ColorB;
            MessagePlayers(string.Format(lang.GetMessage("title", this) + "<color=" + color + ">" + lang.GetMessage("KillMsg", this) + "</color>", player.displayName, victim.displayName));
            CheckScores();
        }
        void CheckScores()
        {
            if (TDMPlayers.Count <= 1)
            {
                MessagePlayers(lang.GetMessage("title", this) + lang.GetMessage("NoPlayers", this));
                var emptyobject = new object[] { };
                EventManager.Call("CloseEvent", emptyobject);
                EventManager.Call("EndEvent", emptyobject);
                return;
            }
            Team winner = Team.NONE;
            if (TeamAKills >= KillLimit) winner = Team.A;
            else if (TeamBKills >= KillLimit) winner = Team.B;
            if (winner != Team.NONE)
                Winner(winner);
        }
        void Winner(Team winner)
        {
            foreach (TDMPlayer p in TDMPlayers)
            {
                if (p.team == winner) EventManager.Call("AddTokens", p.player.userID.ToString(), WinTokens);
            }
            MessagePlayers(string.Format(lang.GetMessage("title", this) + lang.GetMessage("WinMsg", this), winner));
            var emptyobject = new object[] { };
            EventManager.Call("CloseEvent", emptyobject);
            timer.Once(2, () => EventManager.Call("EndEvent", emptyobject));
        }
        #endregion

        #region class
        ////////////////////////////////////////////////////////////
        // TDMPlayer class to store informations ////////////
        ////////////////////////////////////////////////////////////
        class TDMPlayer : MonoBehaviour
        {
            public BasePlayer player;
            public Team team;
            public int kills;
            int i = 0;

            void Awake()
            {
                player = GetComponent<BasePlayer>();
                enabled = false;
                kills = 0;
                team = Team.NONE;

            }
            public static TDMPlayer GetGUI(BasePlayer player)
            {
                TDMPlayer component = player.GetComponent<TDMPlayer>();
                if (component == null)
                    component = player.gameObject.AddComponent<TDMPlayer>();
                return component;
            }
            public void UseUI(string killlimit, string akills, string bkills)
            {
                Vector2 posMin = GUIPos;
                Vector2 posMax = GUIPos + GUIDim;

                var elements = new CuiElementContainer();
                var mainbg = elements.Add(new CuiPanel
                {
                    Image =
                    {
                        Color = "0.1 0.1 0.1 0.75"
                    },
                    RectTransform =
                    {
                        AnchorMin = posMin.x + " " + posMin.y,
                        AnchorMax = posMax.x + " " + posMax.y
                    },
                    CursorEnabled = false
                }, "HUD/Overlay", "ScoreBoard");

                elements.Add(new CuiLabel
                {
                    Text =
                {
                    Text = KillLimitText,
                    FontSize = TextSize,
                    Align = TextAnchor.MiddleLeft,
                    Color = KillLimitColor,
                    FadeIn = 0.1f
                },
                    RectTransform =
                {
                    AnchorMin = "0.05 0.66",
                    AnchorMax = "0.5 1"
                }
                }, mainbg);
                elements.Add(new CuiLabel
                {
                    Text =
                {
                    Text = killlimit,
                    FontSize = TextSize,
                    Align = TextAnchor.MiddleRight,
                    Color = KillLimitColor,
                    FadeIn = 0.1f
                },
                    RectTransform =
                {
                    AnchorMin = "0.5 0.66",
                    AnchorMax = "0.85 1"
                }
                }, mainbg);
                elements.Add(new CuiLabel
                {
                    Text =
                {
                    Text = TeamAText,
                    FontSize = TextSize,
                    Align = TextAnchor.MiddleLeft,
                    Color = TeamAColor,
                    FadeIn = 0.1f
                },
                    RectTransform =
                {
                    AnchorMin = "0.05 0.33",
                    AnchorMax = "0.5 0.66"
                }
                }, mainbg);
                elements.Add(new CuiLabel
                {
                    Text =
                {
                    Text = akills,
                    FontSize = TextSize,
                    Align = TextAnchor.MiddleRight,
                    Color = TeamAColor,
                    FadeIn = 0.1f
                },
                    RectTransform =
                {
                    AnchorMin = "0.5 0.33",
                    AnchorMax = "0.85 0.66"
                }
                }, mainbg);
                elements.Add(new CuiLabel
                {
                    Text =
                {
                    Text = TeamBText,
                    FontSize = TextSize,
                    Align = TextAnchor.MiddleLeft,
                    Color = TeamBColor,
                    FadeIn = 0.1f
                },
                    RectTransform =
                {
                    AnchorMin = "0.05 0",
                    AnchorMax = "0.5 0.33"
                }
                }, mainbg);
                elements.Add(new CuiLabel
                {
                    Text =
                {
                    Text = bkills,
                    FontSize = TextSize,
                    Align = TextAnchor.MiddleRight,
                    Color = TeamBColor,
                    FadeIn = 0.1f
                },
                    RectTransform =
                {
                    AnchorMin = "0.5 0",
                    AnchorMax = "0.85 0.33"
                }
                }, mainbg);
                CuiHelper.AddUi(player, elements);
                i++;
            }
            public void DestroyNotification()
            {
                CuiHelper.DestroyUi(player, "ScoreBoard");
            }
        }
        #endregion

        #region config
        //////////////////////////////////////////////////////////////////////////////////////
        // Configurations ////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////

        static string DefaultKit = "tdm_kit";
        static string GameName = "TeamDeathmatch";
        static string ZoneName = "tdm_zone";
        static string SpawnsA = "tdmspawns_a";
        static string SpawnsB = "tdmspawns_b";
        static string ColorA = "#33CC33";
        static string ColorB = "#003366";
        public string ScoreMsg = "<color=" + ColorA + ">Team A</color> : <color=" + ColorA + ">{0}</color>    -||   <color=#cc0000>Limit : {2}</color>   ||-    <color=" + ColorB + ">{1}</color> : <color=" + ColorB + ">Team B</color>";

        static string TeamAShirt = "tshirt";
        static int TeamASkin = 0;
        static string TeamBShirt = "tshirt";
        static int TeamBSkin = 14177;

        static float GUIPosX = 0.82f;
        static float GUIPosY = 0.78f;
        static float GUIDimX = 0.13f;
        static float GUIDimY = 0.13f;

        static Vector2 GUIPos = new Vector2(GUIPosX, GUIPosY);
        static Vector2 GUIDim = new Vector2(GUIDimX, GUIDimY);
        static int TextSize = 20;
        static string KillLimitColor = "0.698 0.13 0.13 1.0";
        static string KillLimitText = "Kill Limit : ";

        static string TeamAColor = "0.0 0.788235294 0.0 1.0";
        static string TeamAText = "Team A : ";

        static string TeamBColor = "0.0 0.5 1.0 1.0";
        static string TeamBText = "Team B : ";

        static int KillLimit = 10;
        static float ffDamageMod = 0;
        static float StartHealth = 100;


        static int KillTokens = 1;
        static int WinTokens = 5;

        private void LoadVariables()
        {
            LoadConfigVariables();
            SaveConfig();
        }
        private void LoadConfigVariables()
        {


            CheckCfg("TeamDeathmatch - Team A - SpawnFile", ref SpawnsA);
            CheckCfg("TeamDeathmatch - Team A - Shirt", ref TeamAShirt);
            CheckCfg("TeamDeathmatch - Team A - Skin", ref TeamASkin);
            CheckCfg("TeamDeathmatch - Team A - Color", ref ColorA);
            CheckCfg("TeamDeathmatch - Team B - SpawnFile", ref SpawnsB);
            CheckCfg("TeamDeathmatch - Team B - Shirt", ref TeamBShirt);
            CheckCfg("TeamDeathmatch - Team B - Skin", ref TeamBSkin);
            CheckCfg("TeamDeathmatch - Team B - Color", ref ColorB);

            CheckCfg("TeamDeathmatch - Options - Kit", ref DefaultKit);
            CheckCfg("TeamDeathmatch - Options - Zone name", ref ZoneName);
            CheckCfg("TeamDeathmatch - Options - Kills to win", ref KillLimit);
            CheckCfgFloat("TeamDeathmatch - Options - Start health", ref StartHealth);
            CheckCfgFloat("TeamDeathmatch - Options - Friendlyfire damage ratio", ref ffDamageMod);

            CheckCfgFloat("Scoreboard - GUI - Position X", ref GUIPosX);
            CheckCfgFloat("Scoreboard - GUI - Position Y", ref GUIPosY);
            CheckCfgFloat("Scoreboard - GUI - Dimensions X", ref GUIDimX);
            CheckCfgFloat("Scoreboard - GUI - Dimensions Y", ref GUIDimY);
            CheckCfg("Scoreboard - Text - Max kills", ref KillLimitText);
            CheckCfg("Scoreboard - Text - Team A kills", ref TeamAText);
            CheckCfg("Scoreboard - Text - Team B kills", ref TeamBText);
            CheckCfg("Scoreboard - Colors - Total kills", ref KillLimitColor);
            CheckCfg("Scoreboard - Colors - Team A kills", ref TeamAColor);
            CheckCfg("Scoreboard - Colors - Team B kills", ref TeamBColor);
            CheckCfg("Scoreboard - GUI - Text size", ref TextSize);

            CheckCfg("Tokens - Per Kill", ref KillTokens);
            CheckCfg("Tokens - On Win", ref WinTokens);

            Kit = DefaultKit;

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

        object GetEventConfig(string configname)
        {
            if (!UseTDM) return null;
            if (Config[configname] == null) return null;
            return Config[configname];
        }
        #endregion

        #region messages
        Dictionary<string, string> messages = new Dictionary<string, string>()
        {
            {"title", "<color=#cc0000>TeamDeathmatch</color> : " },
            {"WinMsg", "Team {0} has won the game!" },
            {"NoPlayers", "Not enough players to continue. Ending event" },
            {"KillMsg", "{0} killed {1}" },
            {"OpenMsg", "Use tactics and work together to defeat the enemy team" },
            {"skillTime", "{0} {1} left to kill the slasher!" },
            {"ff", "Don't shoot your team mates!"},
        };
        #endregion
    }
}

