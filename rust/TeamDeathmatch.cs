// Requires: EventManager
using System.Collections.Generic;
using UnityEngine;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using Rust;

namespace Oxide.Plugins
{
    [Info("Team Deathmatch", "k1lly0u", "0.2.21", ResourceId = 1484)]
    class TeamDeathmatch : RustPlugin
    {
        #region Fields        
        [PluginReference]
        EventManager EventManager;

        [PluginReference]
        Plugin Spawns;

        private bool UseTDM;
        private bool Started;
        private bool Changed;

        public string Kit;

        public int TeamAKills;
        public int TeamBKills;

        private List<TDMPlayer> TDMPlayers = new List<TDMPlayer>();
        private ConfigData configData;
        #endregion

        #region Oxide Hooks       
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
            var success = EventManager.RegisterEventGame(configData.EventName);
            if (success == null)
            {
                Puts("Event plugin doesn't exist");
                return;
            }
        }        
        void Unload()
        {
            foreach (var player in BasePlayer.activePlayerList) DestroyUI(player);
            if (UseTDM && Started)            
                EventManager.EndEvent();

            var objects = UnityEngine.Object.FindObjectsOfType<TDMPlayer>();
            if (objects != null)
                foreach (var gameObj in objects)
                    UnityEngine.Object.Destroy(gameObj);
        }
        private void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo hitinfo)
        {
            if (UseTDM && Started)
            {
                if (entity is BasePlayer && hitinfo?.Initiator is BasePlayer)
                {
                    var victim = entity.GetComponent<TDMPlayer>();
                    var attacker = hitinfo.Initiator.GetComponent<TDMPlayer>();
                    if (victim != null && attacker != null && victim.player.userID != attacker.player.userID)
                    {
                        if (victim.team == attacker.team)
                        {
                            if (configData.FF_Damage_Modifier <= 0)
                            {
                                hitinfo.damageTypes = new DamageTypeList();
                                hitinfo.DoHitEffects = false;
                            }
                            else
                                hitinfo.damageTypes.ScaleAll(configData.FF_Damage_Modifier);
                            SendReply(attacker.player, TitleM() + lang.GetMessage("ff", this, attacker.player.UserIDString));
                        }
                    }
                }
            }
        }
        #endregion

        #region EventManager Hooks       
        void OnSelectEventGamePost(string name)
        {
            if (configData.EventName.Equals(name))
            {
                if (!string.IsNullOrEmpty(configData.TeamA_Spawnfile) && !string.IsNullOrEmpty(configData.TeamB_Spawnfile))
                {
                    UseTDM = true;
                    EventManager.SelectSpawnfile(configData.TeamA_Spawnfile);
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
                if (player.IsSleeping()) player.EndSleeping();
                timer.Once(3, () =>
                {                    
                    if (!player.GetComponent<TDMPlayer>()) return;
                    GiveTeamGear(player);
                    CreateScoreboard(player);
                });
            }
        }
        private void GiveTeamGear(BasePlayer player)
        {
            player.health = configData.StartingHealth;
            EventManager.GivePlayerKit(player, Kit);
            if (!EventManager.UseClassSelection)
                GiveTeamShirts(player);
        }
        private void GiveTeamShirts(BasePlayer player)
        {
            if (player.GetComponent<TDMPlayer>().team == Team.A)
            {
                Item shirt = ItemManager.CreateByPartialName(configData.TeamA_Shirt);
                shirt.skin = configData.TeamA_SkinID;
                shirt.MoveToContainer(player.inventory.containerWear);
            }
            else if (player.GetComponent<TDMPlayer>().team == Team.B)
            {
                Item shirt = ItemManager.CreateByPartialName(configData.TeamB_Shirt);
                shirt.skin = configData.TeamB_SkinID;
                shirt.MoveToContainer(player.inventory.containerWear);
            }
        }
        object OnSelectSpawnFile(string name)
        {
            if (UseTDM)
            {
                if (name.EndsWith("_a"))
                {
                    configData.TeamA_Spawnfile = name;
                    configData.TeamB_Spawnfile = name.Replace("_a", "_b");
                    return true;
                }
            }
            return null;
        }        
        private object CanEventOpen()
        {
            if (UseTDM)
            {
                if (!CheckSpawnfiles()) return "error";
                return true;
            }
            return null;
        }
        private bool CheckSpawnfiles()
        {
            object success = Spawns.Call("GetSpawnsCount", configData.TeamA_Spawnfile);
            if (success is string)
            {                
                Puts("Error finding the Team A spawn file");
                return false;
            }
            success = Spawns.Call("GetSpawnsCount", configData.TeamB_Spawnfile);
            if (success is string)
            {
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
            if (!UseTDM) return null;
            PrintToChat(TitleM() + lang.GetMessage("OpenMsg", this));
            return null;
        }
        object OnEventClosePost()
        {
            return null;
        }
        object OnEventEndPre()
        {
            if (!UseTDM) return null;
            CheckScores(true);
            foreach (TDMPlayer p in TDMPlayers)
            {
                p.team = Team.NONE;
                DestroyUI(p.player);
                UnityEngine.Object.Destroy(p);
            }

            Started = false;
            TDMPlayers.Clear();
            TeamAKills = 0;
            TeamBKills = 0;
            return null;
        }
        void OnPlayerSelectClass(BasePlayer player)
        {
            if (UseTDM && Started)
                GiveTeamShirts(player);
        }
        object OnEventCancel()
        {
            CheckScores(true);
            return null;
        }

        object OnEventEndPost()
        {
            var objects = UnityEngine.Object.FindObjectsOfType<TDMPlayer>();
            if (objects != null)
                foreach (var gameObj in objects)
                    UnityEngine.Object.Destroy(gameObj);
            return null;
        }
        object OnEventStartPre()
        {
            if (UseTDM)
            {
                Started = true;
            }
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
            if (UseTDM)
            {
                if (player.GetComponent<TDMPlayer>())
                    UnityEngine.Object.Destroy(player.GetComponent<TDMPlayer>());
                TDMPlayers.Add(player.gameObject.AddComponent<TDMPlayer>());
                if (Started)
                {
                    TeamAssign(player);
                    //OnEventPlayerSpawn(player);
                }
            }
            return null;
        }
        object OnEventLeavePost(BasePlayer player)
        {
            if (UseTDM)
            {
                DestroyUI(player);
                var tDMPlayer = player.GetComponent<TDMPlayer>();
                if (tDMPlayer)
                {                    
                    TDMPlayers.Remove(tDMPlayer);
                    UnityEngine.Object.Destroy(tDMPlayer);
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
                    if (vic.GetComponent<TDMPlayer>())
                    {
                        var victim = vic.GetComponent<TDMPlayer>();
                        DestroyUI(vic);
                        if (hitinfo.Initiator is BasePlayer)
                        {

                            var attacker = hitinfo.Initiator.GetComponent<TDMPlayer>();
                            if ((victim.player.userID != attacker.player.userID) && (attacker.team != victim.team))
                            {
                                AddKill(attacker.player, victim.player);
                            }
                        }
                    }
                }
            }
            return;
        }
        private void RefreshSB()
        {
            foreach (TDMPlayer p in TDMPlayers)            
                CreateScoreboard(p.player);            
        }
        object EventChooseSpawn(BasePlayer player, Vector3 destination)
        {
            if (UseTDM)
            {
                if (!CheckForTeam(player))
                {
                    TeamAssign(player);
                    return false;
                }
                Team team = player.GetComponent<TDMPlayer>().team;
                object newpos = null;
                if (team == Team.A) newpos = Spawns.Call("GetRandomSpawn", configData.TeamA_Spawnfile);
                else if (team == Team.B) newpos = Spawns.Call("GetRandomSpawn", configData.TeamB_Spawnfile);
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
                if (!string.IsNullOrEmpty(configData.ZoneName))
                    return configData.ZoneName;
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
            if (!player.GetComponent<TDMPlayer>())
                TDMPlayers.Add(player.gameObject.AddComponent<TDMPlayer>());
            if (player.GetComponent<TDMPlayer>().team == Team.NONE)
                return false;
            return true;
        }
        private void TeamAssign(BasePlayer player)
        {
            if (UseTDM && Started)
            {
                Team team = CountForBalance();
                if (player.GetComponent<TDMPlayer>().team == Team.NONE)
                {
                    player.GetComponent<TDMPlayer>().team = team;
                    string color = string.Empty;
                    if (team == Team.A) color = configData.TeamA_Color;
                    else if (team == Team.B) color = configData.TeamB_Color;
                    SendReply(player, string.Format(lang.GetMessage("AssignTeam", this, player.UserIDString), GetTeamName(team, player), color));
                    Puts("Player " + player.displayName + " assigned to Team " + team);
                    player.Respawn();                    
                }
            }
        }

        private string GetTeamName(Team team, BasePlayer player = null)
        {
            switch (team)
            {
                case Team.A:
                    return lang.GetMessage("TeamA", this, player?.UserIDString);
                case Team.B:
                    return lang.GetMessage("TeamB", this, player?.UserIDString);
                default:
                    return lang.GetMessage("TeamNone", this, player?.UserIDString);
            }
        }
        private Team CountForBalance()
        {
            Team PlayerNewTeam;
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

        #region UI Scoreboard

        private void CreateScoreboard(BasePlayer player)
        {
            string GUIMin = $"{configData.GUIPosX} {configData.GUIPosY}";
            string GUIMax = $"{configData.GUIPosX + configData.GUIDimX} {configData.GUIPosY + configData.GUIDimY}";
            DestroyUI(player);
            var panelName = "TDMScoreboard";
            var element = EventManager.UI.CreateElementContainer(panelName, "0.3 0.3 0.3 0.7", GUIMin, GUIMax, false);
            EventManager.UI.CreateLabel(ref element, panelName, "", $"<color={configData.TeamA_Color}>{TeamAKills} : Team A</color>   ||   Limit : {configData.KillLimit}   ||   <color={configData.TeamB_Color}>Team B : {TeamBKills}</color>", configData.GUI_TextSize, "0 0", "1 1");           
            CuiHelper.AddUi(player, element);
        }
        #endregion

        #region Functions
        private void DestroyUI(BasePlayer player) => CuiHelper.DestroyUi(player, "TDMScoreboard");
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

        #region Commands
        [ConsoleCommand("tdm.spawns.a")]
        void ccmdSpawnsA(ConsoleSystem.Arg arg)
        {
            if (!isAuth(arg)) return;
            if (arg.Args == null || arg.Args.Length == 0)
            {
                SendReply(arg, "tdm.spawns.a \"filename\"");
                return;
            }
            object success = Spawns.Call("GetSpawnsCount", arg.Args[0]);
            if (success is string)
            {
                SendReply(arg, (string)success);
                return;
            }
            configData.TeamA_Spawnfile = arg.Args[0];
            SaveConfig();
            SendReply(arg, string.Format("Team A spawnfile is now {0} .", configData.TeamA_Spawnfile));
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
            object success = Spawns.Call("GetSpawnsCount", arg.Args[0]);
            if (success is string)
            {
                SendReply(arg, (string)success);
                return;
            }
            configData.TeamB_Spawnfile = arg.Args[0];
            SaveConfig();
            SendReply(arg, string.Format("Team B spawnfile is now {0} .", configData.TeamB_Spawnfile));
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
            int KillLimit;
            if (!int.TryParse(arg.Args[0], out KillLimit))
            {
                SendReply(arg, "The kill count needs to be a number");
                return;
            }
            configData.KillLimit = KillLimit;
            SaveConfig();
            SendReply(arg, string.Format("Kill count to win event is now {0} .", configData.KillLimit));
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
                default:
                    return;
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

            string color = string.Empty;
            if (p.team == Team.A) color = configData.TeamA_Color;
            else if (p.team == Team.B) color = configData.TeamB_Color;

            SendReply(p.player, string.Format(TitleM() + "You have been moved to <color=" + color + ">Team {0}</color>", newTeam.ToString().ToUpper()));
            SendReply(arg, string.Format("{0} has been moved to Team {1}", p.player.displayName, newTeam.ToString().ToUpper()));
        }
        bool isAuth(ConsoleSystem.Arg arg)
        {
            if (arg.connection?.authLevel < 1)
            {
                SendReply(arg, "You dont not have permission to use this command.");
                return false;
            }
            return true;
        }
        #endregion

        #region Scoring
        
        void AddKill(BasePlayer player, BasePlayer victim)
        {
            var p = player.GetComponent<TDMPlayer>();
            if (!p) return;

            if (p.team == Team.A) TeamAKills++;
            else if (p.team == Team.B) TeamBKills++;
            RefreshSB();

            EventManager.AddTokens(player.UserIDString, configData.Tokens_Kill);
            string color = string.Empty;
            if (p.team == Team.A) color = configData.TeamA_Color;
            else if (p.team == Team.B) color = configData.TeamB_Color;
            MessagePlayers(string.Format(TitleM() + "<color=" + color + ">" + lang.GetMessage("KillMsg", this) + "</color>", player.displayName, victim.displayName));
            CheckScores();
        }
        void CheckScores(bool timelimitreached = false)
        {
            if (TDMPlayers.Count <= 1)
            {
                MessagePlayers(TitleM() + lang.GetMessage("NoPlayers", this));
                EventManager.CloseEvent();
                EventManager.EndEvent();
                return;
            }
            Team winner = Team.NONE;
            if (EventManager.EventMode == EventManager.GameMode.Normal)
            {
                if (TeamAKills >= configData.KillLimit) winner = Team.A;
                else if (TeamBKills >= configData.KillLimit) winner = Team.B;
            }
            if (timelimitreached)
            {
                if (TeamAKills > TeamBKills) winner = Team.A;
                else if (TeamBKills > TeamAKills) winner = Team.B;                
            }
            if (winner != Team.NONE)
                Winner(winner);
        }
        void Winner(Team winner)
        {
            foreach (TDMPlayer p in TDMPlayers)
            {
                if (p.team == winner)
                    EventManager.AddTokens(p.player.UserIDString, configData.Tokens_Win);                    
            }
            MessagePlayers(string.Format(TitleM() + lang.GetMessage("WinMsg", this), GetTeamName(winner)));
            EventManager.CloseEvent();
            timer.Once(2, () => EventManager.EndEvent());
        }
        #endregion

        #region Class      
        class TDMPlayer : MonoBehaviour
        {
            public BasePlayer player;
            public Team team;            

            void Awake()
            {
                player = GetComponent<BasePlayer>();
                enabled = false;
                team = Team.NONE;
            }
        }

        #endregion

        #region Config     
        class ConfigData
        {
            public string DefaultKit { get; set; }
            public string EventName { get; set; }
            public string TeamA_Spawnfile { get; set; }
            public string TeamB_Spawnfile { get; set; }
            public string TeamA_Color { get; set; }
            public string TeamB_Color { get; set; }
            public string TeamA_Shirt { get; set; }
            public string TeamB_Shirt { get; set; }
            public int TeamA_SkinID { get; set; }
            public int TeamB_SkinID { get; set; }
            public float GUIPosX { get; set; }
            public float GUIPosY { get; set; }
            public float GUIDimX { get; set; }
            public float GUIDimY { get; set; }
            public int GUI_TextSize { get; set; }
            public float FF_Damage_Modifier { get; set; }
            public float StartingHealth { get; set; }
            public int KillLimit { get; set; }
            public int Tokens_Kill { get; set; }
            public int Tokens_Win { get; set; }
            public string ZoneName { get; set; }
        }  
        private void LoadVariables()
        {
            LoadConfigVariables();
            SaveConfig();
        }
        
        private void LoadConfigVariables()
        {
            configData = Config.ReadObject<ConfigData>();
        }
        protected override void LoadDefaultConfig()
        {
            Puts("Creating a new config file");
            var config = new ConfigData
            {
                DefaultKit = "tdm_kit",
                EventName = "TeamDeathmatch",
                TeamA_Spawnfile = "tdmspawns_a",
                TeamB_Spawnfile = "tdmspawns_b",
                TeamA_Color = "#33CC33",
                TeamB_Color = "#003366",
                TeamA_Shirt = "tshirt",
                TeamA_SkinID = 0,
                TeamB_Shirt = "tshirt",
                TeamB_SkinID = 14177,
                GUIPosX = 0.3f,
                GUIPosY = 0.92f,
                GUIDimX = 0.4f,
                GUIDimY = 0.06f,
                GUI_TextSize = 20,
                KillLimit = 10,
                FF_Damage_Modifier = 0,
                StartingHealth = 100,
                Tokens_Kill = 1,
                Tokens_Win = 5,
                ZoneName = ""
            };
            SaveConfig(config);
        }
        void SaveConfig(ConfigData config)
        {
            Config.WriteObject(config, true);
        }
        object GetEventConfig(string configname)
        {
            if (!UseTDM) return null;
            return Config[configname];
        }
        #endregion

        #region messages
        private string TitleM() => $"<color=orange>{Title}: </color>";
        Dictionary<string, string> messages = new Dictionary<string, string>
        {
            {"WinMsg", "Team {0} has won the game!" },
            {"NoPlayers", "Not enough players to continue. Ending event" },
            {"KillMsg", "{0} killed {1}" },
            {"OpenMsg", "Use tactics and work together to defeat the enemy team" },
            {"skillTime", "{0} {1} left to kill the slasher!" },
            {"ff", "Don't shoot your team mates!"},
            {"AssignTeam", "You have been assigned to <color={1}>Team {0}</color>"},
            {"TeamA", "A" },
            {"TeamB", "B" },
            {"TeamNone", "None" }
        };
        #endregion
    }
}

