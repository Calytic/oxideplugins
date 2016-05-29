using System.Collections.Generic;
using UnityEngine;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using System;

namespace Oxide.Plugins
{
    [Info("TeamBattlefield", "BodyweightEnergy / k1lly0u", "2.0.3", ResourceId = 1330)]
    class TeamBattlefield : RustPlugin
    {
        #region Fields
        [PluginReference] Plugin Spawns;

        private List<TBPlayer> TBPlayers = new List<TBPlayer>();
        private Dictionary<ulong, PlayerData> DCPlayers = new Dictionary<ulong, PlayerData>();
        private Dictionary<ulong, Timer> DCTimers = new Dictionary<ulong, Timer>();
        private bool UseTB;

        private int TeamA_Score;
        private int TeamB_Score;        
        #endregion

        #region Hooks       
        void OnServerInitialized()
        {
            LoadVariables();
            if (!CheckDependencies()) return;
            if (!CheckSpawnfiles()) return;
            UseTB = true;
            TeamA_Score = 0;
            TeamB_Score = 0;
            foreach (var player in BasePlayer.activePlayerList)
                OnPlayerInit(player);
        }
        private void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo hitInfo)
        {
            try
            {
                if (UseTB)
                    if (entity is BasePlayer && hitInfo.Initiator is BasePlayer)
                    {
                        if (entity as BasePlayer == null || hitInfo == null) return;
                        var victim = entity.ToPlayer();
                        var attacker = hitInfo.Initiator.ToPlayer();
                        if (victim != attacker)
                            if (victim.GetComponent<TBPlayer>() && attacker.GetComponent<TBPlayer>())
                                if (victim.GetComponent<TBPlayer>().team == attacker.GetComponent<TBPlayer>().team)
                                {
                                    hitInfo.damageTypes.ScaleAll(configData.FF_DamageScale);
                                    SendReply(hitInfo.Initiator as BasePlayer, "Friendly Fire!");
                                }
                    }
            }
            catch (Exception ex)
            {
            }
        }
        private void OnEntityDeath(BaseEntity entity, HitInfo hitInfo)
        {
            try
            {
                if (UseTB)
                    if (entity is BasePlayer && hitInfo.Initiator is BasePlayer)
                    {
                        if (entity as BasePlayer == null || hitInfo == null) return;
                        var victim = entity.ToPlayer();
                        var attacker = hitInfo.Initiator.ToPlayer();
                        if (victim != attacker)
                            if (victim.GetComponent<TBPlayer>() && attacker.GetComponent<TBPlayer>())
                                if (victim.GetComponent<TBPlayer>().team != attacker.GetComponent<TBPlayer>().team)
                                {
                                    attacker.GetComponent<TBPlayer>().kills++;
                                    AddPoints(attacker, victim, attacker.GetComponent<TBPlayer>().team);
                                }
                    }
            }
            catch (Exception ex)
            {
            }
        }       
        private void RefreshScoreboard()
        {
            foreach(var player in BasePlayer.activePlayerList)
            {
                TBPlayer.GetPlayerGUI(player).DestroyScoreboard();
                TBPlayer.GetPlayerGUI(player).Scoreboard(TeamA_Score.ToString(), TeamB_Score.ToString());
            }
        }
        private void OnPlayerInit(BasePlayer player)
        {
            if (UseTB)
            {
                if (player.IsSleeping())
                {
                    timer.Once(3, () =>
                    {
                        player.EndSleeping();
                        if (!player.GetComponent<TBPlayer>())
                        {
                            TBPlayers.Add(player.gameObject.AddComponent<TBPlayer>());
                            TBPlayer.GetPlayerGUI(player).Scoreboard(TeamA_Score.ToString(), TeamB_Score.ToString());
                            if (DCPlayers.ContainsKey(player.userID))
                            {
                                player.GetComponent<TBPlayer>().kills = DCPlayers[player.userID].kills;
                                player.GetComponent<TBPlayer>().team = DCPlayers[player.userID].team;
                                DCPlayers.Remove(player.userID);
                                DCTimers[player.userID].Destroy();
                                DCTimers.Remove(player.userID);
                            }
                            else cmdChangeTeam(player, "", new string[0]);
                        }
                    });
                }
                                
            }
        }     
        private void OnPlayerDisconnected(BasePlayer player)
        {
            if (player.GetComponent<TBPlayer>())
            {
                DCPlayers.Add(player.userID, new PlayerData { kills = player.GetComponent<TBPlayer>().kills, team = player.GetComponent<TBPlayer>().team});
                DCTimers.Add(player.userID, timer.Once(configData.RemoveSleeper_Timer * 60, () => { DCPlayers.Remove(player.userID); DCTimers[player.userID].Destroy(); DCTimers.Remove(player.userID); }));
                DestroyPlayer(player);
            }
        }
        private void DestroyPlayer(BasePlayer player)
        {
            if (TBPlayers.Contains(player.GetComponent<TBPlayer>()))
            {
                player.GetComponent<TBPlayer>().DestroyMenu();
                player.GetComponent<TBPlayer>().DestroyScoreboard();
                TBPlayers.Remove(player.GetComponent<TBPlayer>());
                UnityEngine.Object.Destroy(player.GetComponent<TBPlayer>());
            }
        }
        private void OnPlayerRespawned(BasePlayer player) 
        {
            if (UseTB)
                if (player.GetComponent<TBPlayer>())
                {
                    Team team = player.GetComponent<TBPlayer>().team;
                    player.inventory.Strip();
                    if (team != Team.SPECTATOR)
                    {                        
                        GivePlayerWeapons(player);
                        GivePlayerGear(player, team);

                        object newpos = null;

                        if (team == Team.A) newpos = Spawns.Call("GetRandomSpawn", new object[] { configData.TeamA_Spawnfile });
                        else if (team == Team.B) newpos = Spawns.Call("GetRandomSpawn", new object[] { configData.TeamB_Spawnfile });

                        if (newpos is Vector3)
                            MovePlayerPosition(player, (Vector3)newpos);
                    }
                }
                else OnPlayerInit(player);            
        }
        private object OnPlayerChat(ConsoleSystem.Arg arg)
        {
            if (UseTB)
                if (configData.UsePluginChatControl)
                {
                    BasePlayer player = (BasePlayer)arg.connection.player;
                    string message = arg.GetString(0, "text");
                    string color = configData.Spectator_Chat_Color + configData.Spectator_Chat_Prefix;
                    if (player.GetComponent<TBPlayer>())
                    {
                        switch (player.GetComponent<TBPlayer>().team)
                        {
                            case Team.A:
                                color = configData.TeamA_Chat_Color + configData.TeamA_Chat_Prefix;
                                break;
                            case Team.B:
                                color = configData.TeamB_Chat_Color + configData.TeamB_Chat_Prefix;
                                break;
                            case Team.ADMIN:
                                color = configData.Admin_Chat_Color + configData.Admin_Chat_Prefix;
                                break;
                        }
                    }
                    string formatMsg = color + player.displayName + "</color> : " + message;
                    Broadcast(formatMsg, player.userID.ToString());
                    return false;
                }
            return null;
        }
        void Unload()
        {
            foreach (var p in BasePlayer.activePlayerList)
                DestroyPlayer(p);

            var objects = UnityEngine.Object.FindObjectsOfType(typeof(TBPlayer));
            if (objects != null)
                foreach (var gameObj in objects)
                    UnityEngine.Object.Destroy(gameObj);

            TBPlayers.Clear();
            DCPlayers.Clear();
            DCTimers.Clear();
        }
        #endregion

        #region Functions
        private bool CheckDependencies()
        {
            if (Spawns == null)
            {
                PrintWarning($"Spawns Database could not be found!");
                return false;
            }            
            return true;
        }
        private bool CheckSpawnfiles()
        {
            object successA = Spawns.Call("GetSpawnsCount", new object[] { configData.TeamA_Spawnfile });
            object successB = Spawns.Call("GetSpawnsCount", new object[] { configData.TeamB_Spawnfile });

            if (successA is string)
            {
                configData.TeamA_Spawnfile = null;
                Puts("Error finding the Team A spawn file");
                return false;
            }
            if (successB is string)
            {
                configData.TeamB_Spawnfile = null;
                Puts("Error finding the Team B spawn file");
                return false;
            }
            return true;
        }
        static void MovePlayerPosition(BasePlayer player, Vector3 destination)
        {
            player.SetPlayerFlag(BasePlayer.PlayerFlags.Sleeping, true);
            if (!BasePlayer.sleepingPlayerList.Contains(player)) BasePlayer.sleepingPlayerList.Add(player);

            player.CancelInvoke("InventoryUpdate");
            player.inventory.crafting.CancelAll(true);

            player.MovePosition(destination);
            player.ClientRPCPlayer(null, player, "ForcePositionTo", destination, null, null, null, null);
            player.TransformChanged();
            player.SetPlayerFlag(BasePlayer.PlayerFlags.ReceivingSnapshot, true);
            player.UpdateNetworkGroup();

            player.SendNetworkUpdateImmediate(false);
            player.ClientRPCPlayer(null, player, "StartLoading", null, null, null, null, null);
            player.SendFullSnapshot();
        }
        public void Broadcast(string message, string userid = "0") => ConsoleSystem.Broadcast("chat.add", userid, message, 1.0);
        private void StartSpectating(BasePlayer player)
        {
            if (!player.IsSpectating())
            {
                int num = UnityEngine.Random.Range(0, BasePlayer.activePlayerList.Count);
                BasePlayer target = BasePlayer.activePlayerList[num];               
                player.SetPlayerFlag(BasePlayer.PlayerFlags.Spectating, true);
                player.gameObject.SetLayerRecursive(10);
                player.CancelInvoke("MetabolismUpdate");
                player.CancelInvoke("InventoryUpdate");
                player.UpdateSpectateTarget(target.displayName);
                player.SetPlayerFlag(BasePlayer.PlayerFlags.ThirdPersonViewmode, true);
            }
        }
        private void EndSpectating(BasePlayer player)
        {
            if (player.IsSpectating())
            {
                player.SetParent(null, 0);
                player.metabolism.Reset();
                player.InvokeRepeating("InventoryUpdate", 1f, 0.1f * UnityEngine.Random.Range(0.99f, 1.01f));
                player.SetPlayerFlag(BasePlayer.PlayerFlags.Spectating, false);
                player.gameObject.SetLayerRecursive(17);
                player.SetPlayerFlag(BasePlayer.PlayerFlags.ThirdPersonViewmode, false);
            }
        }
        private void AddPoints(BasePlayer player, BasePlayer victim, Team team)
        {
            string colorAttacker = "";
            string colorVictim = "";
            string prefixAttacker = "";
            string prefixVictim = "";
            switch (team)
            {
                case Team.A:
                    TeamA_Score++;
                    colorAttacker = configData.TeamA_Chat_Color;                    
                    colorVictim = configData.TeamB_Chat_Color;
                    prefixAttacker = configData.TeamA_Chat_Prefix;
                    prefixVictim = configData.TeamB_Chat_Prefix;
                    break;
                case Team.B:
                    TeamB_Score++;
                    colorAttacker = configData.TeamB_Chat_Color;
                    colorVictim = configData.TeamA_Chat_Color;
                    prefixAttacker = configData.TeamB_Chat_Prefix;
                    prefixVictim = configData.TeamA_Chat_Prefix;
                    break;
                case Team.ADMIN:
                    return;
                case Team.SPECTATOR:
                    return;
            }
            RefreshScoreboard();
            if (configData.BroadcastDeath)
            {
                string formatMsg = colorAttacker + player.displayName + "</color> has killed " + colorVictim + victim.displayName + "</color>";
                Broadcast(formatMsg);
            }
        }
        #endregion

        #region Giving Items
        private void GivePlayerWeapons(BasePlayer player)
        {
            foreach (var entry in configData.z_StartingWeapons)
            {
                for (var i = 0; i < entry.amount; i++)
                    GiveItem(player, BuildWeapon(entry), entry.container);
                if (!string.IsNullOrEmpty(entry.ammoType))
                    GiveItem(player, BuildItem(entry.ammoType, entry.ammo), "main");
            }
        }
        private void GivePlayerGear(BasePlayer player, Team team)
        {
            foreach (var entry in configData.z_CommonGear)            
                GiveItem(player, BuildItem(entry.shortname, entry.amount, entry.skin), entry.container);

            var teamGear = new List<Gear>();
            if (team == Team.A) teamGear = configData.z_TeamA_Gear;
            else if (team == Team.B) teamGear = configData.z_TeamB_Gear;
            else if (team == Team.ADMIN) teamGear = configData.z_Admin_Gear;

            if (teamGear != null)
                foreach(var entry in teamGear)
                    GiveItem(player, BuildItem(entry.shortname, entry.amount, entry.skin), entry.container);
        }
        private Item BuildItem(string shortname, int amount = 1, int skin = 0)
        {
            var definition = ItemManager.FindItemDefinition(shortname);
            if (definition != null)
            {
                var item = ItemManager.Create(definition, amount, false, skin);
                if (item != null)
                    return item;
            }
            Puts("Error making item: " + shortname);
            return null;
        }
        private Item BuildWeapon(Weapon newWeapon)
        {
            var item = BuildItem(newWeapon.shortname, 1, newWeapon.skin);
            if (item == null) return null;
            var weapon = item.GetHeldEntity() as BaseProjectile;
            if (weapon != null)
            {
                weapon.primaryMagazine.contents = weapon.primaryMagazine.capacity;
                if (!string.IsNullOrEmpty(newWeapon.ammoType))
                {
                    var ammoType = ItemManager.FindItemDefinition(newWeapon.ammoType);
                    if (ammoType != null)
                        weapon.primaryMagazine.ammoType = ammoType;
                }
            }
            if (newWeapon.contents == null) return item;

            foreach (var content in newWeapon.contents)
                BuildItem(content)?.MoveToContainer(item.contents);

            return item;
        }
        public void GiveItem(BasePlayer player, Item item, string container)
        {
            if (item == null) return;
            ItemContainer cont;
            switch (container)
            {
                case "wear":
                    cont = player.inventory.containerWear;
                    break;
                case "belt":
                    cont = player.inventory.containerBelt;
                    break;
                default:
                    cont = player.inventory.containerMain;
                    break;
            }
            player.inventory.GiveItem(item, cont);
        }
        #endregion

        #region Console Commands
        
        [ConsoleCommand("tbf.list")]
        private void cmdList(ConsoleSystem.Arg arg)
        {
            for (int i = 0; i < TBPlayers.Count; i++)
                SendReply(arg, "Name: " + TBPlayers[i].player.displayName + ", Team: " + TBPlayers[i].team.ToString()); 
        }
        [ConsoleCommand("tbf.clearscore")]
        private void cmdClearscore(ConsoleSystem.Arg arg)
        {
            if (isAuth(arg))
            {
                TeamA_Score = 0;
                TeamB_Score = 0;
                RefreshScoreboard();
                SendReply(arg, "Score's have been reset");
            }
        }

        [ConsoleCommand("tbf.assign")]
        private void cmdAssign(ConsoleSystem.Arg arg)
        {
            if (!isAuth(arg)) return;
            if (arg.Args == null || arg.Args.Length == 0)            
            {
                SendReply(arg, "Format: tbf.assign <PARTIAL_PLAYERNAME> <[\"a\",\"b\",\"spectator\"]>");
                return;
            }           
            if (arg.Args.Length == 2)
            {
                var partialPlayerName = arg.Args[0];
                var foundPlayers = FindPlayer(partialPlayerName);
                if (foundPlayers.Count == 0)
                {
                    SendReply(arg, "No players found");
                    return;
                }
                if (foundPlayers.Count > 1)
                {
                    SendReply(arg, "Multiple players found");
                    return;
                }
                var newTeam = Team.SPECTATOR;

                switch (arg.Args[1].ToUpper())
                {
                    case "A":
                        newTeam = Team.A;
                        break;

                    case "B":
                        newTeam = Team.B;
                        break;
                    case "SPECTATOR":
                        newTeam = Team.SPECTATOR;
                        break;

                    default:
                        SendReply(arg, "Invalid team assignment.");
                        return;
                }
                if (foundPlayers[0] != null)
                {
                    AssignPlayerToTeam(foundPlayers[0], newTeam);
                    SendReply(arg, foundPlayers[0].displayName + " has been successfully assigned to team " + newTeam.ToString());
                }
                else SendReply(arg, "There was a error assigning a new team");
            }
        }

        [ConsoleCommand("tbf.version")]
        private void cmdVersion(ConsoleSystem.Arg arg) => SendReply(arg, Title + "  --  V " + Version.ToString() + "  --  by " + Author);
       
        [ConsoleCommand("tbf.help")]
        private void cmdHelp(ConsoleSystem.Arg arg)
        {
            SendReply(arg, "TeamBattlefield Console Commands:");
            SendReply(arg, "tbf.list - Lists Teams and Disconnect Times of players.");
            SendReply(arg, "tbf.assign <PartialPlayerName> [one/two/spectator] - Assigns player to team.");
            SendReply(arg, "tbf.purge - Removes players from all teams if they're been disconnected for more than 5 minutes.");
            SendReply(arg, "tbf.version - Prints current version number of plugin.");
        }

        [ConsoleCommand("tbf.purge")]
        private void cmdPurge(ConsoleSystem.Arg arg)
        {
            int count = DCPlayers.Count;
            foreach (var entry in DCTimers)
                entry.Value.Destroy();
            DCPlayers.Clear();
            DCTimers.Clear();
            SendReply(arg, string.Format("You have removed {0} inactive player data", count));
        }

        [ChatCommand("switchteam")]
        private void cmdChangeTeam(BasePlayer player, string command, string[] args) => TBPlayer.GetPlayerGUI(player).TeamSelection(CountPlayers(Team.A), CountPlayers(Team.B), CountPlayers(Team.SPECTATOR));

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
        [ChatCommand("t")]
        private void cmdTeamChat(BasePlayer player, string command, string[] args)
        {
            if (player.GetComponent<TBPlayer>())
            {
                var message = string.Join(" ", args);
                if (string.IsNullOrEmpty(message))
                    return;

                var sendingPlayer = player.GetComponent<TBPlayer>();
                var team = sendingPlayer.team;
                string color = "";                
                switch (team)
                {
                    case Team.A:
                        color = configData.TeamA_Chat_Color;
                        break;
                    case Team.B:
                        color = configData.TeamB_Chat_Color;
                        break;
                    case Team.ADMIN:
                        color = configData.Admin_Chat_Color;
                        return;
                    case Team.SPECTATOR:
                        color = configData.Spectator_Chat_Color;
                        return;
                }               

                foreach (var p in TBPlayers)
                {
                    if (p.team == player.GetComponent<TBPlayer>().team)
                    {
                        SendReply(p.player, $"{color}Team Chat : </color>{message}");
                    }
                }
            }
        }
        #endregion

        #region UI Commands
        [ConsoleCommand("TeamSelectA")]
        private void cmdTeamSelectA(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            AssignPlayerToTeam(player, Team.A);
        }

        [ConsoleCommand("TeamSelectB")]
        private void cmdTeamSelectB(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            AssignPlayerToTeam(player, Team.B);
        }

        [ConsoleCommand("TeamSelectSpec")]
        private void cmdTeamSelectSpec(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            AssignPlayerToTeam(player, Team.SPECTATOR);
        }

        [ConsoleCommand("TeamSelectAdmin")]
        private void cmdTeamSelectAdmin(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            AssignPlayerToTeam(player, Team.ADMIN);
        }
        #endregion

        #region Team Management

        enum Team
        {
            A,
            B,
            SPECTATOR,
            ADMIN
        }
        private List<BasePlayer> FindPlayer(string arg)
        {
            var foundPlayers = new List<BasePlayer>();
            ulong steamid;
            ulong.TryParse(arg, out steamid);
            string lowerarg = arg.ToLower();

            foreach (var p in BasePlayer.activePlayerList)
                if (p != null)
                {
                    if (steamid != 0L)
                        if (p.userID == steamid)
                        {
                            foundPlayers.Add(p);
                            return foundPlayers;
                        }
                    string lowername = p.displayName.ToLower();
                    if (lowername.Contains(lowerarg))
                        foundPlayers.Add(p);
                }
            return foundPlayers;
        }
        private string CountPlayers(Team team)
        {
            int i = 0;
            foreach (var entry in TBPlayers)
            {
                if (entry.team == team)
                    i++;
            }
            return i.ToString();
        }
        private void AssignPlayerToTeam(BasePlayer player , Team team)
        {           

            TBPlayer.GetPlayerGUI(player).DestroyMenu();

            if (player.GetComponent<TBPlayer>().team == team)
                return;
            
            int aCount = int.Parse(CountPlayers(Team.A));
            int bCount = int.Parse(CountPlayers(Team.B));
            if (team == Team.A)
                if (aCount > bCount + configData.MaximumTeamCountDifference)
                {
                    team = Team.B;
                    SendReply(player, "There are too many players on Team A, auto assigning to Team B");
                }
            if (team == Team.B)
                if (bCount > aCount + configData.MaximumTeamCountDifference)
                {
                    team = Team.A;
                    SendReply(player, "There are too many players on Team B, auto assigning to Team A");
                }

            player.GetComponent<TBPlayer>().team = team;

            if (team == Team.ADMIN) return;
            if (team == Team.SPECTATOR)
            {
                StartSpectating(player);
                return;
            }
            EndSpectating(player);
            player.DieInstantly();
        }       
        #endregion

        #region Externally Called Functions
        string GetPlayerTeam (ulong playerID)
        {
            foreach (var entry in TBPlayers)
                if (entry.player.userID == playerID)
                    return entry.team.ToString();
            return null;            
        }
        Dictionary<ulong, string> GetTeams()
        {
            Dictionary<ulong, string> returnedList = new Dictionary<ulong, string>();
            foreach (var player in TBPlayers)
                returnedList.Add(player.player.userID, player.team.ToString());
            
            return returnedList;
        }
        #endregion

        #region Config        
        private ConfigData configData;
        class ConfigData
        {            
            public int MaximumTeamCountDifference { get; set; }
            public int RemoveSleeper_Timer { get; set; }
            public string TeamA_Spawnfile { get; set; }
            public string TeamA_Chat_Prefix { get; set; }
            public string TeamA_Chat_Color { get; set; }
            public string TeamB_Spawnfile  { get; set; }
            public string TeamB_Chat_Prefix { get; set; }
            public string TeamB_Chat_Color { get; set; }
            public string Admin_Chat_Color { get; set; }
            public string Admin_Chat_Prefix { get; set; }
            public string Spectator_Chat_Color { get; set; }
            public string Spectator_Chat_Prefix { get; set; }
            public List<Gear> z_CommonGear { get; set; }
            public List<Weapon> z_StartingWeapons { get; set; }
            public List<Gear> z_TeamA_Gear { get; set; }
            public List<Gear> z_TeamB_Gear { get; set; }
            public List<Gear> z_Admin_Gear { get; set; }
            public float FF_DamageScale { get; set; }
            public bool UsePluginChatControl { get; set; }
            public bool BroadcastDeath { get; set; }
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

                Admin_Chat_Color = "<color=#00ff04>",
                Admin_Chat_Prefix = "[Admin] ",
                BroadcastDeath = true,
                MaximumTeamCountDifference = 4,
                RemoveSleeper_Timer = 5,
                TeamA_Spawnfile = "team_a_spawns",
                TeamB_Spawnfile = "team_b_spawns",
                TeamA_Chat_Color = "<color=#0066ff>",
                TeamA_Chat_Prefix = "[Team A] ",
                TeamB_Chat_Color = "<color=#ff0000>",
                FF_DamageScale = 0.0f,
                Spectator_Chat_Color = "<color=white>",
                Spectator_Chat_Prefix = "[Spectator] ",
                TeamB_Chat_Prefix = "[Team B] ",
                UsePluginChatControl = true,
                z_CommonGear = new List<Gear>
                {
                    {
                        new Gear
                        {
                            name = "Machete",
                            shortname = "machete",
                            amount = 1,
                            container = "belt"
                        }
                    },
                    {
                        new Gear
                        {
                            name = "Medical Syringe",
                            shortname = "syringe.medical",
                            amount = 2,
                            container = "belt"
                        }
                    },
                    {
                        new Gear
                        {
                            name = "Bandage",
                            shortname = "bandage",
                            amount = 1,
                            container = "belt"
                        }
                    },                    
                    {
                        new Gear
                        {
                            name = "Paper Map",
                            shortname = "map",
                            amount = 1,
                            container = "belt"
                        }
                    },
                    {
                        new Gear
                        {
                            name = "Metal ChestPlate",
                            shortname = "metal.plate.torso",
                            amount = 1,
                            container = "wear"
                        }
                    }
                },
                z_StartingWeapons = new List<Weapon>
                {
                    new Weapon
                    {
                            name = "AssaultRifle",
                            shortname = "rifle.ak",
                            container = "belt",
                            ammoType = "ammo.rifle.hv",
                            ammo = 120,
                            amount = 1,
                            contents = new [] {"weapon.mod.holosight"}
                    },
                    new Weapon
                    {
                            name = "SemiAutoPistol",
                            shortname = "pistol.semiauto",
                            container = "belt",
                            ammoType = "ammo.pistol.hv",
                            amount = 1,
                            ammo = 120,
                            contents = new [] {"weapon.mod.silencer"}
                    }
                },
                z_TeamA_Gear = new List<Gear>
                {
                    new Gear
                    {
                        name = "Hoodie",
                        shortname = "hoodie",
                        amount = 1,
                        container = "wear",
                        skin = 14178
                    },
                    new Gear
                    {
                        name = "Pants",
                        shortname = "pants",
                        amount = 1,
                        container = "wear",
                        skin = 10020
                    },
                    new Gear
                    {
                        name = "Gloves",
                        shortname = "burlap.gloves",
                        amount = 1,
                        container = "wear",
                        skin = 10128
                    },
                    new Gear
                    {
                        name = "Boots",
                        shortname = "shoes.boots",
                        amount = 1,
                        container = "wear",
                        skin = 10023
                    }
                },
                z_TeamB_Gear = new List<Gear>
                {
                    new Gear
                    {
                        name = "Hoodie",
                        shortname = "hoodie",
                        amount = 1,
                        container = "wear",
                        skin = 0
                    },
                    new Gear
                    {
                        name = "Pants",
                        shortname = "pants",
                        amount = 1,
                        container = "wear",
                        skin = 10019
                    },
                    new Gear
                    {
                        name = "Gloves",
                        shortname = "burlap.gloves",
                        amount = 1,
                        container = "wear",
                        skin = 10128
                    },
                    new Gear
                    {
                        name = "Boots",
                        shortname = "shoes.boots",
                        amount = 1,
                        container = "wear",
                        skin = 10023
                    }
                },
                z_Admin_Gear = new List<Gear>
                {
                    new Gear
                    {
                        name = "Hoodie",
                        shortname = "hoodie",
                        amount = 1,
                        container = "wear",
                        skin = 10129
                    },
                    new Gear
                    {
                        name = "Pants",
                        shortname = "pants",
                        amount = 1,
                        container = "wear",
                        skin = 10078
                    },
                    new Gear
                    {
                        name = "Gloves",
                        shortname = "burlap.gloves",
                        amount = 1,
                        container = "wear",
                        skin = 10128
                    },
                    new Gear
                    {
                        name = "Boots",
                        shortname = "shoes.boots",
                        amount = 1,
                        container = "wear",
                        skin = 10023
                    }
                }
            };
            SaveConfig(config);
        }
        private void LoadConfigVariables() => configData = Config.ReadObject<ConfigData>();
        void SaveConfig(ConfigData config) => Config.WriteObject(config, true);        
        #endregion

        #region Classes
        class TBPlayer : MonoBehaviour
        {
            public BasePlayer player;
            public int kills;
            public Team team;
            
            void Awake()
            {
                player = GetComponent<BasePlayer>();
                enabled = false;
                kills = 0;
            }
            public static TBPlayer GetPlayerGUI(BasePlayer player)
            {
                TBPlayer component = player.GetComponent<TBPlayer>();
                if (component == null)
                    component = player.gameObject.AddComponent<TBPlayer>();
                return component;
            }
            public void TeamSelection(string aCount, string bCount, string specCount)
            {
                var TeamSelect = new CuiElementContainer()
                {
                {
                    new CuiPanel
                    {
                        Image = {Color = "0.1 0.1 0.1 0.9"},
                        RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
                        CursorEnabled = true
                    },
                    new CuiElement().Parent,
                    "TeamSelectionMenu"
                },
                /// Player Count
                {
                    new CuiLabel
                    {
                        Text = {Color = "0.0 0.5 1.0 1.0", FontSize = 20, Align = TextAnchor.MiddleCenter, FadeIn = 1.0f, Text = "Team A Players: " + aCount},
                        RectTransform = { AnchorMin = "0.2 0.55", AnchorMax = "0.4 0.65" }
                    },
                    "TeamSelectionMenu"
                },
                {
                    new CuiLabel
                    {
                        Text = {Color = "0.9 0.1 0.2 1.0", FontSize = 20, Align = TextAnchor.MiddleCenter, FadeIn = 1.0f, Text = "Team B Players: " + bCount},
                        RectTransform = { AnchorMin = "0.4 0.55", AnchorMax = "0.6 0.65" }
                    },
                    "TeamSelectionMenu"
                },
                    {
                    new CuiLabel
                    {
                        Text = { Color = "0.9 0.9 0.9 1.0", FontSize = 20, Align = TextAnchor.MiddleCenter, FadeIn = 1.0f, Text = "Spectators: " + specCount },
                        RectTransform = { AnchorMin = "0.6 0.55", AnchorMax = "0.8 0.65" }
                    },
                    "TeamSelectionMenu"
                },

                /// Buttons
                {
                    new CuiButton
                    {
                        Button = {Color = "0.1 0.1 0.6 1.0", Command = "TeamSelectA", FadeIn = 1.0f },
                        RectTransform = {AnchorMin = "0.2 0.45", AnchorMax = "0.395 0.55"},
                        Text = {Text = "Team A", FontSize = 35, Align = TextAnchor.MiddleCenter}
                    },
                    "TeamSelectionMenu"
                },
                {
                    new CuiButton
                    {
                        Button = {Color = "0.698 0.13 0.13 1.0", Command = "TeamSelectB", FadeIn = 1.0f },
                        RectTransform = {AnchorMin = "0.405 0.45", AnchorMax = "0.595 0.55"},
                        Text = {Text = "Team B", FontSize = 35, Align = TextAnchor.MiddleCenter}
                    },
                    "TeamSelectionMenu"
                },
                {
                    new CuiButton
                    {
                        Button = {Color = "0.5 0.5 0.5 1.0", Command = "TeamSelectSpec", FadeIn = 1.0f },
                        RectTransform = {AnchorMin = "0.605 0.45", AnchorMax = "0.795 0.55"},
                        Text = {Text = "Spectate", FontSize = 35, Align = TextAnchor.MiddleCenter}
                    },
                    "TeamSelectionMenu"
                } };

                // Admin button
                if (player.net.connection.authLevel > 0)
                {
                    TeamSelect.Add(new CuiButton
                    {
                        Button = { Color = "0.2 0.6 0.2 1.0", Command = "TeamSelectAdmin", FadeIn = 1.0f },
                        RectTransform = { AnchorMin = "0.4 0.25", AnchorMax = "0.6 0.35" },
                        Text = { Text = "Admin", FontSize = 35, Align = TextAnchor.MiddleCenter }
                    }, 
                    "TeamSelectionMenu");
                }

                CuiHelper.AddUi(player, TeamSelect);
            }
            public void Scoreboard(string aCount, string bCount)
            {
                var Scoreboard = new CuiElementContainer()
                {
                {
                    new CuiPanel
                    {
                        Image = {Color = "0.1 0.1 0.1 0.75"},
                        RectTransform = {AnchorMin = "0.41 0.95", AnchorMax = "0.59 1"}
                    },
                    new CuiElement().Parent,
                    "Scoreboard"
                },
                /// Player Count
                {
                    new CuiLabel
                    {
                        Text = {Color = "0.0 0.5 1.0 1.0", FontSize = 16, Align = TextAnchor.MiddleCenter, FadeIn = 1.0f, Text = "Team A: " + aCount},
                        RectTransform = { AnchorMin = "0.01 0.02", AnchorMax = "0.499 0.998" }
                    },
                    "Scoreboard"
                },
                {
                    new CuiLabel
                    {
                        Text = {Color = "0.9 0.1 0.2 1.0", FontSize = 16, Align = TextAnchor.MiddleCenter, FadeIn = 1.0f, Text = "Team B: " + bCount},
                        RectTransform = { AnchorMin = "0.501 0.02", AnchorMax = "0.999 0.998" }
                    },
                    "Scoreboard"
                }};
                CuiHelper.AddUi(player, Scoreboard);
            }
            public void DestroyMenu() => CuiHelper.DestroyUi(player, "TeamSelectionMenu");
            public void DestroyScoreboard() => CuiHelper.DestroyUi(player, "Scoreboard");

        }        
        class PlayerData
        {
            public int kills;
            public Team team;
        }
        class Gear
        {
            public string name;
            public string shortname;
            public int skin;
            public int amount;
            public string container;
        }
        class Weapon
        {
            public string name;
            public string shortname;
            public int skin;
            public string container;
            public int amount;
            public int ammo;
            public string ammoType;
            public string[] contents = new string[0];
        }
        #endregion
    }
}
