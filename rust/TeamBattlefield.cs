using System.Collections.Generic;
using UnityEngine;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using System.Reflection;
using System;

namespace Oxide.Plugins
{
    [Info("TeamBattlefield", "BodyweightEnergy / k1lly0u", "2.1.2", ResourceId = 1330)]
    class TeamBattlefield : RustPlugin
    {
        #region Fields
        [PluginReference] Plugin Spawns;

        readonly MethodInfo entitySnapshot = typeof(BasePlayer).GetMethod("SendEntitySnapshot", BindingFlags.Instance | BindingFlags.NonPublic);

        private List<TBPlayer> TBPlayers = new List<TBPlayer>();
        private Dictionary<ulong, PlayerData> DCPlayers = new Dictionary<ulong, PlayerData>();
        private Dictionary<ulong, Timer> DCTimers = new Dictionary<ulong, Timer>();
        private bool UseTB;

        private int TeamA_Score;
        private int TeamB_Score;
        #endregion
        #region UI
        #region UI Main
        private const string UIMain = "TBUI_Main";
        private const string UIScoreboard = "TBUI_Scoreboard";
        public class UI
        {
            static public CuiElementContainer CreateElementContainer(string panelName, string color, string aMin, string aMax, bool useCursor = false)
            {
                var NewElement = new CuiElementContainer()
                {
                    {
                        new CuiPanel
                        {
                            Image = {Color = color},
                            RectTransform = {AnchorMin = aMin, AnchorMax = aMax},
                            CursorEnabled = useCursor
                        },
                        new CuiElement().Parent,
                        panelName
                    }
                };
                return NewElement;
            }
            static public void CreatePanel(ref CuiElementContainer container, string panel, string color, string aMin, string aMax, bool cursor = false)
            {
                container.Add(new CuiPanel
                {
                    Image = { Color = color },
                    RectTransform = { AnchorMin = aMin, AnchorMax = aMax },
                    CursorEnabled = cursor
                },
                panel);
            }
            static public void CreateLabel(ref CuiElementContainer container, string panel, string color, string text, int size, string aMin, string aMax, TextAnchor align = TextAnchor.MiddleCenter)
            {
                container.Add(new CuiLabel
                {
                    Text = { Color = color, FontSize = size, Align = align, Text = text },
                    RectTransform = { AnchorMin = aMin, AnchorMax = aMax }
                },
                panel);

            }
            static public void CreateButton(ref CuiElementContainer container, string panel, string color, string text, int size, string aMin, string aMax, string command, TextAnchor align = TextAnchor.MiddleCenter)
            {
                container.Add(new CuiButton
                {
                    Button = { Color = color, Command = command, FadeIn = 0.2f },
                    RectTransform = { AnchorMin = aMin, AnchorMax = aMax },
                    Text = { Text = text, FontSize = size, Align = align }
                },
                panel);
            }
        }
        #endregion

        #region Team Selection
        private void OpenTeamSelection(BasePlayer player)
        {
            var MainCont = UI.CreateElementContainer(UIMain, "0.1 0.1 0.1 0.95", "0 0", "1 1", true);
            UI.CreateLabel(ref MainCont, UIMain, "", $"{configData.TeamA.Chat_Color}Team A Players : {CountPlayers(Team.A)}</color>", 20, "0.2 0.55", "0.4 0.65");
            UI.CreateButton(ref MainCont, UIMain, "0.2 0.2 0.2 0.7", $"{configData.TeamA.Chat_Color}Team A</color>", 35, "0.2 0.45", "0.395 0.55", "TBUI_TeamSelect a");

            UI.CreateLabel(ref MainCont, UIMain, "", $"{configData.TeamB.Chat_Color}Team B Players : {CountPlayers(Team.B)}</color>", 20, "0.4 0.55", "0.6 0.65");
            UI.CreateButton(ref MainCont, UIMain, "0.2 0.2 0.2 0.7", $"{configData.TeamB.Chat_Color}Team B</color>", 35, "0.405 0.45", "0.595 0.55", "TBUI_TeamSelect b");

            if (configData.Spectators.EnableSpectators)
            {
                UI.CreateLabel(ref MainCont, UIMain, "", $"{configData.Spectators.Chat_Color}Spectators : {CountPlayers(Team.SPECTATOR)}</color>", 20, "0.6 0.55", "0.8 0.65");
                UI.CreateButton(ref MainCont, UIMain, "0.2 0.2 0.2 0.7", $"{configData.Spectators.Chat_Color}Spectate</color>", 35, "0.605 0.45", "0.795 0.55", "TBUI_TeamSelect spectator");
            }
            if (player.IsAdmin())
            {
                UI.CreateButton(ref MainCont, UIMain, "0.2 0.2 0.2 0.7", $"{configData.Admin.Chat_Color}Admin</color>", 35, "0.4 0.25", "0.6 0.35", "TBUI_TeamSelect admin");
            }
            CuiHelper.DestroyUi(player, UIMain);
            CuiHelper.AddUi(player, MainCont);
        }
        #endregion

        #region Scoreboard       
        public void Scoreboard(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, UIScoreboard);
            var MainCont = UI.CreateElementContainer(UIScoreboard, "0.1 0.1 0.1 0.5", "0.39 0.95", "0.61 1", false);
            UI.CreateLabel(ref MainCont, UIScoreboard, "", $"{configData.TeamA.Chat_Color}Team A: {TeamA_Score}</color>   ||   {configData.TeamB.Chat_Color}{TeamB_Score} : Team B</color>", 20, "0 0", "1 1");

            CuiHelper.AddUi(player, MainCont);
        }
        #endregion               
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
                            {
                                if (victim.GetComponent<TBPlayer>().team == attacker.GetComponent<TBPlayer>().team)
                                {
                                    hitInfo.damageTypes.ScaleAll(configData.Options.FF_DamageScale);
                                    SendReply(hitInfo.Initiator as BasePlayer, "Friendly Fire!");
                                }
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
                {
                    if (entity is BasePlayer && hitInfo.Initiator is BasePlayer)
                    {
                        if (entity as BasePlayer == null || hitInfo == null) return;
                        var victim = entity.ToPlayer();
                        var attacker = hitInfo.Initiator.ToPlayer();
                        if (victim != attacker)
                        {
                            if (victim.GetComponent<TBPlayer>() && attacker.GetComponent<TBPlayer>())
                            {
                                if (victim.GetComponent<TBPlayer>().team != attacker.GetComponent<TBPlayer>().team)
                                {
                                    attacker.GetComponent<TBPlayer>().kills++;
                                    AddPoints(attacker, victim);
                                }                                
                            }
                        }
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
                Scoreboard(player);
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
                        OnPlayerInit(player);
                    });
                }
                else InitPlayer(player);               
            }
        }  
        private void InitPlayer(BasePlayer player)
        {
            if (!player.GetComponent<TBPlayer>())
            {
                TBPlayers.Add(player.gameObject.AddComponent<TBPlayer>());
                Scoreboard(player);
                if (DCPlayers.ContainsKey(player.userID))
                {
                    player.GetComponent<TBPlayer>().kills = DCPlayers[player.userID].kills;
                    player.GetComponent<TBPlayer>().team = DCPlayers[player.userID].team;
                    DCPlayers.Remove(player.userID);
                    DCTimers[player.userID].Destroy();
                    DCTimers.Remove(player.userID);
                    player.DieInstantly();
                    player.Respawn();
                }
                else OpenTeamSelection(player);
            }            
        }   
        private void OnPlayerDisconnected(BasePlayer player)
        {
            if (UseTB)
            {
                if (player.GetComponent<TBPlayer>())
                {
                    DCPlayers.Add(player.userID, new PlayerData { kills = player.GetComponent<TBPlayer>().kills, team = player.GetComponent<TBPlayer>().team });
                    DCTimers.Add(player.userID, timer.Once(configData.Options.RemoveSleeper_Timer * 60, () => { DCPlayers.Remove(player.userID); DCTimers[player.userID].Destroy(); DCTimers.Remove(player.userID); }));
                    DestroyPlayer(player);
                }
            }
        }
        private void DestroyPlayer(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, UIMain);
            CuiHelper.DestroyUi(player, UIScoreboard);
            if (TBPlayers.Contains(player.GetComponent<TBPlayer>()))
            {                
                TBPlayers.Remove(player.GetComponent<TBPlayer>());
                UnityEngine.Object.Destroy(player.GetComponent<TBPlayer>());
            }
        }
        private void OnPlayerRespawned(BasePlayer player) 
        {
            if (UseTB)
            {
                if (player.GetComponent<TBPlayer>())
                {
                    Team team = player.GetComponent<TBPlayer>().team;
                    player.inventory.Strip();
                    if (team != Team.SPECTATOR)
                    {
                        GivePlayerWeapons(player);
                        GivePlayerGear(player, team);

                        object newpos = null;

                        if (team == Team.A) newpos = Spawns.Call("GetRandomSpawn", new object[] { configData.TeamA.Spawnfile });
                        else if (team == Team.B) newpos = Spawns.Call("GetRandomSpawn", new object[] { configData.TeamB.Spawnfile });
                        else if (team == Team.ADMIN && !string.IsNullOrEmpty(configData.Admin.Spawnfile)) newpos = Spawns.Call("GetRandomSpawn", new object[] { configData.Admin.Spawnfile });

                        if (newpos is Vector3)
                            MovePlayerPosition(player, (Vector3)newpos);
                    }
                }
                else OnPlayerInit(player);
            }           
        }
        private object OnPlayerChat(ConsoleSystem.Arg arg)
        {
            if (UseTB)
            {
                if (configData.Options.UsePluginChatControl)
                {
                    BasePlayer player = (BasePlayer)arg.connection.player;
                    string message = arg.GetString(0, "text");
                    string color = configData.Spectators.Chat_Color + configData.Spectators.Chat_Prefix;
                    if (player.GetComponent<TBPlayer>())
                    {
                        switch (player.GetComponent<TBPlayer>().team)
                        {
                            case Team.A:
                                color = configData.TeamA.Chat_Color + configData.TeamA.Chat_Prefix;
                                break;
                            case Team.B:
                                color = configData.TeamB.Chat_Color + configData.TeamB.Chat_Prefix;
                                break;
                            case Team.ADMIN:
                                color = configData.Admin.Chat_Color + configData.Admin.Chat_Prefix;
                                break;
                        }
                    }
                    string formatMsg = $"{color} {player.displayName}</color> : {message}";
                    PrintToChat(formatMsg);
                    return false;
                }
            }
            return null;
        }
        void Unload()
        {
            foreach (var p in BasePlayer.activePlayerList)
                DestroyPlayer(p);

            var objects = UnityEngine.Object.FindObjectsOfType<TBPlayer>();
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
            object successA = Spawns.Call("GetSpawnsCount", configData.TeamA.Spawnfile);
            object successB = Spawns.Call("GetSpawnsCount", configData.TeamB.Spawnfile);
            object successAdmin = Spawns.Call("GetSpawnsCount", configData.Admin.Spawnfile);
            if (successA is string)
            {
                configData.TeamA.Spawnfile = null;
                Puts("Error finding the Team A spawn file");
                return false;
            }
            if (successB is string)
            {
                configData.TeamB.Spawnfile = null;
                Puts("Error finding the Team B spawn file");
                return false;
            }
            if (successAdmin is string)
            {
                configData.Admin.Spawnfile = null;
                SaveConfig(configData);
                Puts("Error finding the Admin spawn file, removing admin spawn points");                
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
        
        private void StartSpectating(BasePlayer player, BasePlayer target)
        {
            if (!player.IsSpectating())
            {
                player.SetPlayerFlag(BasePlayer.PlayerFlags.Spectating, true);
                player.gameObject.SetLayerRecursive(10);
                player.CancelInvoke("MetabolismUpdate");
                player.CancelInvoke("InventoryUpdate");
                player.ClearEntityQueue();
                entitySnapshot.Invoke(player, new object[] { target });
                player.gameObject.Identity();
                player.SetParent(target, 0);
            }
        }
        private void EndSpectating(BasePlayer player)
        {
            if (player.IsSpectating())
            {
                player.SetParent(null, 0);
                player.SetPlayerFlag(BasePlayer.PlayerFlags.Spectating, false);
                player.gameObject.SetLayerRecursive(17);
                player.metabolism.Reset();
                player.InvokeRepeating("InventoryUpdate", 1f, 0.1f * UnityEngine.Random.Range(0.99f, 1.01f));
            }
        }       
        private void AddPoints(BasePlayer player, BasePlayer victim)
        {
            string colorAttacker = "";
            string colorVictim = "";
            string prefixAttacker = "";
            string prefixVictim = "";
            switch (player.GetComponent<TBPlayer>().team)
            {
                case Team.NONE:
                    return;
                case Team.A:
                    TeamA_Score++;
                    colorAttacker = configData.TeamA.Chat_Color; 
                    prefixAttacker = configData.TeamA.Chat_Prefix;                    
                    break;
                case Team.B:
                    TeamB_Score++;
                    colorAttacker = configData.TeamB.Chat_Color;                    
                    prefixAttacker = configData.TeamB.Chat_Prefix;                    
                    break;
                case Team.ADMIN:
                    colorAttacker = configData.Admin.Chat_Color;
                    prefixAttacker = configData.Admin.Chat_Prefix;
                    return;
                case Team.SPECTATOR:
                    return;
            }
            switch (victim.GetComponent<TBPlayer>().team)
            {
                case Team.NONE:
                    return;
                case Team.A:
                    colorVictim = configData.TeamA.Chat_Color;
                    prefixVictim = configData.TeamA.Chat_Prefix;
                    break;
                case Team.B:
                    colorVictim = configData.TeamB.Chat_Color;
                    prefixVictim = configData.TeamB.Chat_Prefix;
                    break;
                case Team.SPECTATOR:
                    return;
                case Team.ADMIN:
                    colorVictim = configData.Admin.Chat_Color;
                    prefixVictim = configData.Admin.Chat_Prefix;
                    break;               
            }
            RefreshScoreboard();
            if (configData.Options.BroadcastDeath)
            {
                string formatMsg = colorAttacker + player.displayName + "</color> has killed " + colorVictim + victim.displayName + "</color>";
                PrintToChat(formatMsg);
            }
        }
        #endregion

        #region Giving Items
        private void GivePlayerWeapons(BasePlayer player)
        {
            foreach (var entry in configData.Gear.StartingWeapons)
            {
                for (var i = 0; i < entry.amount; i++)
                    GiveItem(player, BuildWeapon(entry), entry.container);
                if (!string.IsNullOrEmpty(entry.ammoType))
                    GiveItem(player, BuildItem(entry.ammoType, entry.ammo), "main");
            }
        }
        private void GivePlayerGear(BasePlayer player, Team team)
        {
            foreach (var entry in configData.Gear.CommonGear)            
                GiveItem(player, BuildItem(entry.shortname, entry.amount, entry.skin), entry.container);

            var teamGear = new List<Gear>();
            if (team == Team.A) teamGear = configData.TeamA.Gear;
            else if (team == Team.B) teamGear = configData.TeamB.Gear;
            else if (team == Team.ADMIN) teamGear = configData.Admin.Gear;

            if (teamGear != null)
                foreach(var entry in teamGear)
                    GiveItem(player, BuildItem(entry.shortname, entry.amount, entry.skin), entry.container);
        }
        private Item BuildItem(string shortname, int amount = 1, ulong skin = 0)
        {
            var definition = ItemManager.FindItemDefinition(shortname);
            if (definition != null)
            {
                var item = ItemManager.Create(definition, amount, skin);
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
                        if (!configData.Spectators.EnableSpectators)
                        {
                            SendReply(arg, "You have spectators disabled in the config");
                            return;
                        }
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
            SendReply(arg, "tbf.list - Lists teams and disconnect times of players.");
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
        private void cmdChangeTeam(BasePlayer player, string command, string[] args) => OpenTeamSelection(player);

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
                        color = configData.TeamA.Chat_Color;
                        break;
                    case Team.B:
                        color = configData.TeamB.Chat_Color;
                        break;
                    case Team.ADMIN:
                        color = configData.Admin.Chat_Color;
                        return;
                    case Team.SPECTATOR:
                        color = configData.Spectators.Chat_Color;
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
        [ConsoleCommand("TBUI_TeamSelect")]
        private void cmdTeamSelectA(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            var team = ConvertStringToTeam(arg.GetString(0));
            AssignPlayerToTeam(player, team);
        }
        
        private Team ConvertStringToTeam(string team)
        {
            switch (team)
            {
                case "a": return Team.A;
                case "b": return Team.B;
                case "admin": return Team.ADMIN;
                case "spectator": return Team.SPECTATOR;
                default:
                    return Team.A;
            }
        }
        #endregion

        #region Team Management
        enum Team
        {
            NONE,
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
        private int CountPlayers(Team team)
        {
            int i = 0;
            foreach (var entry in TBPlayers)
            {
                if (entry.team == team)
                    i++;
            }
            return i;
        }
        private void AssignPlayerToTeam(BasePlayer player , Team team)
        {
            CuiHelper.DestroyUi(player, UIMain);
            if (!player.GetComponent<TBPlayer>())
                TBPlayers.Add(player.gameObject.AddComponent<TBPlayer>());
            else if (player.GetComponent<TBPlayer>().team == team)
                return;

            bool isSpec = false;
            if (player.GetComponent<TBPlayer>().team == Team.SPECTATOR)
                isSpec = true;

            int aCount = CountPlayers(Team.A);
            int bCount = CountPlayers(Team.B);
            if (team == Team.A)
            {
                if (aCount > bCount + configData.Options.MaximumTeamCountDifference)
                {
                    team = Team.B;
                    SendReply(player, "There are too many players on Team A, auto assigning to Team B");
                }
            }
            if (team == Team.B)
            {
                if (bCount > aCount + configData.Options.MaximumTeamCountDifference)
                {
                    team = Team.A;
                    SendReply(player, "There are too many players on Team B, auto assigning to Team A");
                }
            }
            if (team == Team.SPECTATOR)
            {
                var target = GetRandomTeammate(player);
                player.GetComponent<TBPlayer>().team = team;
                if (target != null)
                    StartSpectating(player, target);
                else StartSpectating(player, BasePlayer.activePlayerList[UnityEngine.Random.Range(0, BasePlayer.activePlayerList.Count - 1)]);
                return;               
            }

            player.GetComponent<TBPlayer>().team = team;
           
            if (isSpec)
                EndSpectating(player);
            player.DieInstantly();            
            player.Respawn();
        } 
        private BasePlayer GetRandomTeammate(BasePlayer player)
        {
            var teammates = new List<BasePlayer>();
            var team = player.GetComponent<TBPlayer>().team;
            foreach (var tm in TBPlayers)
            {
                if (tm.player == player) continue;
                if (tm.team == team)
                    teammates.Add(tm.player);
            }
            if (teammates.Count > 0)
                return teammates[UnityEngine.Random.Range(0, teammates.Count - 1)];
            else return null;
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
        class TeamOptions
        {
            public string Spawnfile { get; set; }
            public string Chat_Prefix { get; set; }
            public string Chat_Color { get; set; }
            public List<Gear> Gear { get; set; }
        }
        class Options
        {
            public int MaximumTeamCountDifference { get; set; }
            public int RemoveSleeper_Timer { get; set; }
            public float FF_DamageScale { get; set; }
            public bool UsePluginChatControl { get; set; }
            public bool BroadcastDeath { get; set; }
            
        }
        class GUI
        {
            public float XPosition { get; set; }
            public float YPosition { get; set; }
            public float XDimension { get; set; }
            public float YDimension { get; set; }
        }
        class ConfigGear
        {
            public List<Gear> CommonGear { get; set; }
            public List<Weapon> StartingWeapons { get; set; }
        }
        class Spectators
        {
            public bool EnableSpectators { get; set; }
            public string Chat_Color { get; set; }
            public string Chat_Prefix { get; set; }
        }
        class ConfigData
        {            
            public TeamOptions TeamA { get; set; }
            public TeamOptions TeamB { get; set; }
            public TeamOptions Admin { get; set; }
            public ConfigGear Gear { get; set; }
            public Options Options { get; set; }
            public Spectators Spectators { get; set; }
            public GUI ScoreboardUI { get; set; }               
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
                Admin = new TeamOptions
                {
                    Chat_Color = "<color=#00ff04>",
                    Chat_Prefix = "[Admin] ",
                    Gear = new List<Gear>
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
                    },
                    Spawnfile = "admin_spawns"
                },
                Gear = new ConfigGear
                {
                    CommonGear = new List<Gear>
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

                StartingWeapons = new List<Weapon>
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
                }
                },
                Options = new Options
                {
                    BroadcastDeath = true,
                    FF_DamageScale = 0.5f,
                    MaximumTeamCountDifference = 5,
                    RemoveSleeper_Timer = 5,
                    UsePluginChatControl = true
                },
                ScoreboardUI = new GUI
                {
                    XDimension = 0.22f,
                    XPosition = 0.39f,
                    YDimension = 0.05f,
                    YPosition = 0.95f
                },
                Spectators = new Spectators
                {
                    Chat_Color = "<color=white>",
                    Chat_Prefix = "[Spectator] ",
                    EnableSpectators = true
                },
                TeamA = new TeamOptions
                {
                    Spawnfile = "team_a_spawns",
                    Chat_Color = "<color=#0066ff>",
                    Chat_Prefix = "[Team A] ",
                    Gear = new List<Gear>
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
                    }
                },
                TeamB = new TeamOptions
                {
                    Chat_Color = "<color=#ff0000>",
                    Chat_Prefix = "[Team B] ",
                    Spawnfile = "team_b_spawns",
                    Gear = new List<Gear>
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
                team = Team.NONE;
            }
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
            public ulong skin;
            public int amount;
            public string container;
        }
        class Weapon
        {
            public string name;
            public string shortname;
            public ulong skin;
            public string container;
            public int amount;
            public int ammo;
            public string ammoType;
            public string[] contents = new string[0];
        }
        #endregion
    }
}
