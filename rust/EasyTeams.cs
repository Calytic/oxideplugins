using System;
using System.Collections.Generic;
using System.Linq;
using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Plugins;
using UnityEngine;

namespace Oxide.Plugins {
    [Info("EasyTeams", "Skrallex", "1.0.0")]
    [Description("Easily create minigame/pvp teams")]
    class EasyTeams : RustPlugin {

        [PluginReference]
        Plugin Kits;

        bool started = false;

        bool spectateWhenNoLives;
        bool permissionToLeave;
        bool usePermissionsOnly;
        int defaultLives;

        List<Team> teams = new List<Team>();

        #region Permissions
        const string adminPerm = "easyteams.admin";
        const string editPerm = "easyteams.edit";
        const string viewPerm = "easyteams.view";
        const string startPerm = "easyteams.start";
        const string stopPerm = "easyteams.stop";
        const string leavePerm = "easyteams.leave";
        #endregion

        #region Plugin Load/Unload
        void Loaded() {
            // Add permissions.
            permission.RegisterPermission(adminPerm, this);
            permission.RegisterPermission(editPerm, this);
            permission.RegisterPermission(viewPerm, this);
            permission.RegisterPermission(startPerm, this);
            permission.RegisterPermission(stopPerm, this);
            permission.RegisterPermission(leavePerm, this);

            // Load config and localisations.
            LoadDefaultMessages();
            LoadConfig();
        }
        #endregion

        #region Configuration
        protected override void LoadDefaultConfig() {
            Puts("Generating Default Config File");
            Config.Clear();
            Config["SpectateWhenNoLivesRemaining"] = false;
            Config["PermissionRequiredToLeave"] = false;
            Config["UsePermissionsOnly"] = false;
            Config["DefaultLives"] = -1;
            SaveConfig();
        }

        void LoadConfig() {
            spectateWhenNoLives = (bool)Config["SpectateWhenNoLivesRemaining"] == null ? false : (bool)Config["SpectateWhenNoLivesRemaining"];
            permissionToLeave = (bool)Config["PermissionRequiredToLeave"] == null ? false : (bool)Config["PermissionRequiredToLeave"];
            usePermissionsOnly = (bool)Config["UsePermissionsOnly"] == null ? false : (bool)Config["UsePermissionsOnly"];
            defaultLives = (int)Config["DefaultLives"] == null ? -1 : (int)Config["DefaultLives"];
        }
        #endregion

        #region Localisation
        void LoadDefaultMessages() {
            lang.RegisterMessages(new Dictionary<string, string> {
                {"Prefix", "<color=orange>EasyTeams</color>"},
                {"NoPermission", "You do not have permission to use this command."},
                {"NoKits", "The Kits plugin is not installed or an invalid version is in use!" },
                {"NoKitExists", "No Kit with that name exists. Try a different kit." },
                {"KitSet", "You set the Kit for team <color=red>{0}</color> to <color=red>{1}</color>." },

                {"CreateSyntax", "Invalid command syntax. Try '/teams_create TeamName'" },
                {"DeleteSyntax", "Invalid command syntax. Try '/teams_delete TeamName'" },
                {"SetSpawnSyntax", "Invalid command syntax. Try '/teams_setspawn TeamName'" },
                {"SetKitSyntax", "Invalid command syntax. Try '/teams_setkit TeamName KitName'" },
                {"AddPlayerSyntax", "Invalid command syntax. Try '/teams_add TeamName Player1Name Player2Name Player3Name ...'" },
                {"RemovePlayerSyntax", "Invalid command syntax. Try '/teams_remove TeamName Player1Name Player2Name Player3Name ...'" },
                {"ViewSyntax", "Invalid command syntax. Try '/teams_view {Optional: TeamName}'" },
                {"LivesSyntax", "Invalid command syntax. Try '/teams_setlives NumLives' or '/teams_setlives TeamName NumLives'" },
                {"SpectateSyntax", "Invalid command syntax. Try '/teams_spectate true' to use the spectator team, or '/teams_spectate false' to not use it." },

                {"TeamAlreadyExists", "That team already exists! Try a different name." },
                {"TeamNotExists", "There is no team with that name. Try a different name." },
                {"NoTeams", "There are no  teams currently setup. Use '/team_create TeamName'" },
                {"ManyPlayersFound", "More than one player name was found to match <color=red>{0}</color>. Please be more specific." },
                {"NoPlayersFound", "No players were found matching the name <color=red>{0}</color>. Please try again." },
                {"PlayerOnOtherTeam", "Player <color=red>{0}</color> is already on team <color=red>{1}</color>! Moving them to team <color=red>{2}</color>." },
                {"PlayerAlreadyOnTeam", "Player <color=red>{0}</color> is already on that team!" },
                {"PlayerNotOnAnyTeam", "Player <color=red>{0}</color> is not on any team." },
                {"PlayerNotOnTeam", "Player <color=red>{0}</color> is not on team <color=red>{1}</color>." },
                {"SelfNotOnTeam", "You can't leave because you are not on any team!" },
                {"EventLeft", "You have the left the current event!" },

                {"TeamCreated", "You added a new team <color=red>{0}</color>!" },
                {"TeamDeleted", "You deleted team <color=red>{0}</color>!" },
                {"TeamsCleared", "You have cleared all the teams!" },
                {"PlayerAdded", "You added player <color=red>{0}</color> to team <color=red>{1}</color>!" },
                {"PlayerRemoved", "You removed player <color=red>{0}</color> from team <color=red>{1}</color>!" },
                {"SpawnSet", "The spawn for team <color=red>{0}</color> has been set to your current position!" },
                {"TeamLivesSet", "You set the number of lives for team <color=red>{0}</color> to <color=red>{1}</color>." },
                {"AllLivesSet", "You set the number of lives for all teams to <color=red>{0}</color>." },
                {"SpawnsNotSet", "You have not yet setup spawns for team <color=red>{0}</color>!" },
                {"SpectateSet", "You have set the use of the spectate team to <color=red>{0}</color>." },
                {"SpectateTeamCreated", "The spectator team <color=red>spectate</color> has been automatically created for you." },

                {"AlreadyStarted", "An event is already started. Use '/teams_stop' first." },
                {"NotStarted", "There is no event currently started. Use '/teams_start' to start one." },
                {"NotifyPlayer", "An event is starting! You are on team <color=red>{0}</color>. Your team has <color=red>{1}</color> lives remaining." },
                {"EventEnded", "The event has now ended. Thanks for playing!" },

                {"TeamView", "Team <color=red>{0}</color> is set to use Kit <color=red>{1}</color>, has <color=red>{3}/{4}</color> lives remaining, and contains <color=red>{2}</color> players:" },
                {"PlayerView", "\n\t=> <color=red>{0}</color>" }
            }, this); 
        }
        #endregion

        #region Chat Commands
        [ChatCommand("teams_create")]
        void chatCmdCreate(BasePlayer player, string cmd, string[] args) {
            if (!IsAllowed(player, editPerm)) {
                ReplyPlayer(player, "NoPermission");
                return;
            }
            if (args.Length != 1) {
                ReplyPlayer(player, "CreateSyntax");
                return;
            }
            if (CheckForTeam(args[0]) != null) {
                ReplyPlayer(player, "TeamAlreadyExists");
                return;
            }
            create(player, args);
        }

        [ChatCommand("teams_delete")]
        void chatCmdDelete(BasePlayer player, string cmd, string[] args) {
            if (!IsAllowed(player, editPerm)) {
                ReplyPlayer(player, "NoPermission");
                return;
            }
            if (args.Length != 1) {
                ReplyPlayer(player, "DeleteSyntax");
                return;
            }
            if (CheckForTeam(args[0]) == null) {
                ReplyPlayer(player, "TeamNotExists");
                return;
            }
            delete(player, args);
        }

        [ChatCommand("teams_setspawn")]
        void chatCmdSetSpawn(BasePlayer player, string cmd, string[] args) {
            if(!IsAllowed(player, editPerm)) {
                ReplyPlayer(player, "NoPermission");
                return;
            }
            if(args.Length != 1) {
                ReplyPlayer(player, "SetSpawnSyntax");
                return;
            }
            if(CheckForTeam(args[0]) == null) {
                ReplyPlayer(player, "TeamNotExists");
                return;
            }
            setSpawn(player, args);
        }

        [ChatCommand("teams_setkit")]
        void chatCmdSetKit(BasePlayer player, string cmd, string[] args) {
            if(!IsAllowed(player, editPerm)) {
                ReplyPlayer(player, "NoPermission");
                return;
            }
            if(args.Length != 2) {
                ReplyPlayer(player, "SetKitSyntax");
                return;
            }
            if(CheckForTeam(args[0]) == null) {
                ReplyPlayer(player, "TeamNotExists");
                return;
            }
            setKit(player, args);
        }

        [ChatCommand("teams_add")]
        void chatCmdAdd(BasePlayer player, string cmd, string[] args) {
            if(!IsAllowed(player, editPerm)) {
                ReplyPlayer(player, "NoPermission");
                return;
            }
            if(args.Length < 2) {
                ReplyPlayer(player, "AddPlayerSyntax");
                return;
            }
            if(CheckForTeam(args[0]) == null) {
                ReplyPlayer(player, "TeamNotExists");
                return;
            }
            for(int i = 1; i < args.Length; i++) {
                if (GetPlayers(args[i]).Count > 1) {
                    ReplyPlayer(player, String.Format(Lang("ManyPlayersFound"), args[i]), true);
                    return;
                }
                if (GetPlayers(args[i]).Count == 0) {
                    ReplyPlayer(player, String.Format(Lang("NoPlayersFound"), args[i]), true);
                    return;
                }
            }
            addPlayer(player, args);
        }

        [ChatCommand("teams_remove")]
        void chatCmdRemove(BasePlayer player, string cmd, string[] args) {
            if(!IsAllowed(player, editPerm)) {
                ReplyPlayer(player, "NoPermission");
                return;
            }
            if(args.Length < 2) {
                ReplyPlayer(player, "RemovePlayerSyntax");
                return;
            }
            if(CheckForTeam(args[0]) == null) {
                ReplyPlayer(player, "TeamNotExists");
                return;
            }
            for(int i = 1; i < args.Length; i++) {
                if(GetPlayers(args[i]).Count > 1) {
                    ReplyPlayer(player, String.Format(Lang("ManyPlayersFound"), args[i]), true);
                    return;
                }
                if(GetPlayers(args[i]).Count == 0) {
                    ReplyPlayer(player, String.Format(Lang("NoPlayersFound"), args[i]), true);
                    return;
                }
            }
            removePlayer(player, args);
        }

        [ChatCommand("teams_view")]
        void chatCmdView(BasePlayer player, string cmd, string[] args) {
            if(!IsAllowed(player, viewPerm)) {
                ReplyPlayer(player, "NoPermission");
                return;
            }
            if(args.Length == 0) {
                viewAll(player, args);
                return;
            }
            if(args.Length > 1) {
                ReplyPlayer(player, "ViewSyntax");
                return;
            }
            if(args.Length == 1 && CheckForTeam(args[0]) == null) {
                ReplyPlayer(player, "TeamNotExists");
                return;
            }
            view(player, args);
        }

        [ChatCommand("teams_clear")]
        void chatCmdClear(BasePlayer player, string cmd, string[] args) {
            if(!IsAllowed(player, editPerm)) {
                ReplyPlayer(player, "NoPermission");
                return;
            }
            clear(player, args);
        }

        [ChatCommand("teams_setlives")]
        void chatCmdLives(BasePlayer player, string cmd, string[] args) {
            int lives;
            if(!IsAllowed(player, editPerm)) {
                ReplyPlayer(player, "NoPermission");
                return;
            }
            if(args.Length == 1) {
                if(!Int32.TryParse(args[0], out lives)) {
                    ReplyPlayer(player, "LivesSyntax");
                    return;
                }
                setAllLives(player, args);
                return;
            }
            if(args.Length == 2) {
                if(CheckForTeam(args[0]) == null) {
                    ReplyPlayer(player, "TeamNotExists");
                    return;
                }
                if(!Int32.TryParse(args[1], out lives)) {
                    ReplyPlayer(player, "LivesSyntax");
                    return;
                }
            }
            setLives(player, args);
        }

        [ChatCommand("teams_spectate")]
        void chatCmdSpectate(BasePlayer player, string cmd, string[] args) {
            bool useSpectator = true;
            if (!IsAllowed(player, editPerm)) {
                ReplyPlayer(player, "NoPermission");
                return;
            }
            if(args.Length != 1) {
                ReplyPlayer(player, "SpectateSyntax");
                return;
            }
            if(!Boolean.TryParse(args[0], out useSpectator)) {
                ReplyPlayer(player, "SpectateSyntax");
                return;
            }
            setSpectate(player, args);
        }

        [ChatCommand("teams_start")]
        void chatCmdStart(BasePlayer player, string cmd, string[] args) {
            if(!IsAllowed(player, startPerm)) {
                ReplyPlayer(player, "NoPermission");
                return;
            }
            if(started) {
                ReplyPlayer(player, "AlreadyStarted");
                return;
            }
            foreach(Team team in teams) {
                if(!team.spawnSet) {
                    ReplyPlayer(player, String.Format(Lang("SpawnsNotSet"), team.name), true);
                    return;
                }
            }
            start(player, args);
        }

        [ChatCommand("teams_stop")]
        void chatCmdStop(BasePlayer player, string cmd, string[] args) {
            if(!IsAllowed(player, stopPerm)) {
                ReplyPlayer(player, "NoPermission");
                return;
            }
            if(!started) {
                ReplyPlayer(player, "NotStarted");
                return;
            }
            stop(player, args);
        }

        [ChatCommand("teams_restart")]
        void chatCmdRestart(BasePlayer player, string cmd, string[] args) {
            if(!IsAllowed(player, startPerm)) {
                ReplyPlayer(player, "NoPermission");
                return;
            }
            if(!started) {
                ReplyPlayer(player, "NotStarted");
                return;
            }
            restart(player, args);
        }

        [ChatCommand("teams_leave")]
        void chatCmdLeave(BasePlayer player, string cmd, string[] args) {
            if(!IsAllowed(player, leavePerm) && permissionToLeave) {
                ReplyPlayer(player, "NoPermission");
                return;
            }
            if(PlayerOnTeam(player) == null) {
                ReplyPlayer(player, "SelfNotOnTeam");
                return;
            }
            leave(player);
        }
        #endregion

        #region Game Hooks

        void OnPlayerRespawned(BasePlayer player) {
            if (!started)
                return;
            if (player == null)
                return;
            if (PlayerOnTeam(player) == null)
                return;

            Team playerTeam = PlayerOnTeam(player);
            if (GetPlayerOnTeam(player, playerTeam) == null)
                return;

            TeamPlayer tPlayer = GetPlayerOnTeam(player, playerTeam);
            if(playerTeam.teamLives == -1) {
                respawn(tPlayer, playerTeam);
                return;
            }
            if(playerTeam.usedLives < playerTeam.teamLives) {
                playerTeam.usedLives++;
                respawn(tPlayer, playerTeam);
                return;
            }
            if(spectateWhenNoLives) {
                if(CheckForTeam("spectate") != null) {
                    Team spectate = CheckForTeam("spectate");
                    tPlayer.team = playerTeam;
                    playerTeam.players.Remove(tPlayer);
                    spectate.players.Add(tPlayer);
                    respawn(tPlayer, spectate);
                    return;
                }
            }
            leave(player);
        }

        #endregion

        #region Team Methods
        void create(BasePlayer player, string[] args) {
            Team team = new Team();
            team.name = args[0];
            team.teamLives = defaultLives;
            teams.Add(team);
            ReplyPlayer(player, String.Format(Lang("TeamCreated"), team.name), true);
        }

        void delete(BasePlayer player, string[] args) {
            Team team = CheckForTeam(args[0]);
            teams.Remove(team);
            ReplyPlayer(player, String.Format(Lang("TeamDeleted"), team.name), true);
        }

        void setSpawn(BasePlayer player, string[] args) {
            Team team = CheckForTeam(args[0]);
            team.SetSpawnPos(player.transform.position);
            ReplyPlayer(player, String.Format(Lang("SpawnSet"), team.name), true);
        }

        void setKit(BasePlayer player, string[] args) {
            Team team = CheckForTeam(args[0]);
            object success = Interface.Oxide.CallHook("isKit", args[1]);
            if (!(success is bool)) {
                ReplyPlayer(player, "NoKits");
                return;
            }
            if(!(bool)success) {
                ReplyPlayer(player, "NoKitExists");
                return;
            }
            team.kitname = args[1];
            ReplyPlayer(player, String.Format(Lang("KitSet"), team.name, team.kitname), true);
        }

        void addPlayer(BasePlayer player, string[] args) {
            Team team = CheckForTeam(args[0]);
            List<TeamPlayer> players = team.players;
            for(int i = 1; i < args.Length; i++) {
                BasePlayer target = GetPlayers(args[i])[0];
                if(PlayerOnTeam(target) != null) {
                    Team currentTeam = PlayerOnTeam(target);
                    if(currentTeam == team) {
                        ReplyPlayer(player, String.Format(Lang("PlayerAlreadyOnTeam"), target.displayName), true);
                        continue;
                    }
                    ReplyPlayer(player, String.Format(Lang("PlayerOnOtherTeam"), target.displayName, currentTeam.name, team.name), true);
                    string[] remArgs = { currentTeam.name, args[i] };
                    removePlayer(player, remArgs);
                }
                TeamPlayer tPlayer = new TeamPlayer();
                tPlayer.player = target;
                players.Add(tPlayer);
                ReplyPlayer(player, String.Format(Lang("PlayerAdded"), target.displayName, team.name), true);
            }
        }

        void removePlayer(BasePlayer player, string[] args) {
            Team team = CheckForTeam(args[0]);
            List<TeamPlayer> players = team.players;
            for(int i = 1; i < args.Length; i++) {
                BasePlayer target = GetPlayers(args[i])[0];
                if(PlayerOnTeam(target) == null) {
                    ReplyPlayer(player, String.Format(Lang("PlayerNotOnAnyTeam"), target.displayName), true);
                    continue;
                }
                if (PlayerOnTeam(target) != team) {
                    ReplyPlayer(player, String.Format(Lang("PlayerNotOnTeam"), target.displayName, team.name), true);
                    continue;
                }
                if (GetPlayerOnTeam(target, team) == null) {
                    ReplyPlayer(player, String.Format(Lang("PlayerNotOnTeam"), target.displayName, team.name), true);
                    continue;
                }
                TeamPlayer tPlayer = GetPlayerOnTeam(target, team);
                ReplyPlayer(player, String.Format(Lang("PlayerRemoved"), target.displayName, team.name), true);
                players.Remove(tPlayer);
            }
        }

        void view(BasePlayer player, string[] args) {
            Team team = CheckForTeam(args[0]);
            string reply = String.Format(Lang("TeamView"), team.name, team.kitname, team.players.Count, team.teamLives - team.usedLives, team.teamLives);
            foreach(TeamPlayer tPlayer in team.players) {
                reply += String.Format(Lang("PlayerView"), tPlayer.player.displayName);
            }
            ReplyPlayer(player, reply, true);
        }

        void viewAll(BasePlayer player, string[] args) {
            if(teams.Count == 0) {
                ReplyPlayer(player, "NoTeams");
                return;
            }
            foreach(Team team in teams) {
                string reply = String.Format(Lang("TeamView"), team.name, team.kitname, team.players.Count, team.teamLives - team.usedLives, team.teamLives);
                foreach(TeamPlayer tPlayer in team.players) {
                    reply += String.Format(Lang("PlayerView"), tPlayer.player.displayName);
                }
                ReplyPlayer(player, reply, true);
            }
        }

        void clear(BasePlayer player, string[] args) {
            if(teams.Count == 0) {
                ReplyPlayer(player, "NoTeams");
                return;
            }
            teams = new List<Team>();
            ReplyPlayer(player, "TeamsCleared");
        }

        void setLives(BasePlayer player, string[] args) {
            Team team = CheckForTeam(args[0]);
            int teamLives = 0;
            Int32.TryParse(args[1], out teamLives);

            team.teamLives = teamLives;
            ReplyPlayer(player, String.Format(Lang("TeamLivesSet"), team.name, team.teamLives), true);
        }

        void setAllLives(BasePlayer player, string[] args) {
            if(teams.Count == 0) {
                ReplyPlayer(player, "NoTeams");
                return;
            }
            int allLives = 0;
            Int32.TryParse(args[0], out allLives);
            foreach(Team team in teams) {
                team.teamLives = allLives;
            }
            ReplyPlayer(player, String.Format(Lang("AllLivesSet"), allLives), true);
        }

        void start(BasePlayer player, string[] args) {
            foreach(Team team in teams) {
                int i = 0;
                foreach(TeamPlayer tPlayer in team.players) {
                    ReplyPlayer(tPlayer.player, String.Format(Lang("NotifyPlayer"), team.name, team.teamLives), true);
                    tPlayer.SaveHealth();
                    tPlayer.SavePos();
                    tPlayer.SaveInv();
                    if(team.spawnSet) {
                        Vector3 offsetSpawn = new Vector3(team.spawnPos.x + i * 0.1f, team.spawnPos.y, team.spawnPos.z + i * 0.1f);
                        tPlayer.TeleportTo(offsetSpawn);
                        i++;
                    }
                    HealPlayer(tPlayer.player);
                    StripPlayer(tPlayer.player);
                    Kits.Call("GiveKit", tPlayer.player, team.kitname);
                }
            }

            started = true;
        }

        void stop(BasePlayer player, string[] args) {
            foreach(Team team in teams) {
                foreach(TeamPlayer tPlayer in team.players) {
                    ReplyPlayer(tPlayer.player, "EventEnded");
                    StripPlayer(tPlayer.player);
                    tPlayer.RestorePos();
                    tPlayer.RestoreHealth();
                    tPlayer.RestoreInv();
                }
            }
            started = false;
        }

        void restart(BasePlayer player, string[] args) {
            if(CheckForTeam("spectate") != null) {
                Team spectate = CheckForTeam("spectate");
                List<TeamPlayer> list = new List<TeamPlayer>();
                foreach(TeamPlayer tPlayer in spectate.players) {
                    if(tPlayer.team != null) {
                        list.Add(tPlayer);
                    }
                }
                foreach (TeamPlayer tPlayer in list) {
                    spectate.players.Remove(tPlayer);
                    tPlayer.team.players.Add(tPlayer);
                }
            }

            foreach (Team team in teams) {
                int i = 0;
                foreach(TeamPlayer tPlayer in team.players) {
                    Vector3 offsetSpawn = new Vector3(team.spawnPos.x + i * 0.1f, team.spawnPos.y, team.spawnPos.z + i * 0.1f);
                    tPlayer.TeleportTo(offsetSpawn);
                    i++;
                    HealPlayer(tPlayer.player);
                    StripPlayer(tPlayer.player);
                    Kits.Call("GiveKit", tPlayer.player, team.kitname);
                }
            }
        }

        void leave(BasePlayer player) {
            Team currentTeam = PlayerOnTeam(player);
            TeamPlayer tPlayer = GetPlayerOnTeam(player, currentTeam);
            StripPlayer(tPlayer.player);
            tPlayer.RestorePos();
            tPlayer.RestoreHealth();
            tPlayer.RestoreInv();
            currentTeam.players.Remove(tPlayer);
            ReplyPlayer(player, "EventLeft");
        }

        void respawn(TeamPlayer tPlayer, Team team) {
            tPlayer.TeleportTo(team.spawnPos);
            HealPlayer(tPlayer.player);
            StripPlayer(tPlayer.player);
            Kits.Call("GiveKit", tPlayer.player, team.kitname);
        }

        void setSpectate(BasePlayer player, string[] args) {
            Boolean.TryParse(args[0], out this.spectateWhenNoLives);
            ReplyPlayer(player, String.Format(Lang("SpectateSet"), this.spectateWhenNoLives), true);
            if (CheckForTeam("spectate") == null) {
                Team spectate = new Team();
                spectate.name = "spectate";
                teams.Add(spectate);
                ReplyPlayer(player, "SpectateTeamCreated");
            }
        }

        #endregion

        #region Supporting Methods
        Team CheckForTeam(string teamName) {
            foreach(Team team in teams) {
                if (team.name.ToLower() == teamName.ToLower())
                    return team;
            }
            return null;
        }

        Team PlayerOnTeam(BasePlayer player) {
            foreach(Team team in teams) {
                foreach(TeamPlayer tPlayer in team.players) {
                    if(tPlayer.player == player) {
                        return team;
                    }
                }
            }
            return null;
        }

        TeamPlayer GetPlayerOnTeam(BasePlayer player, Team team) {
            foreach(TeamPlayer tPlayer in team.players) {
                if(tPlayer.player == player) {
                    return tPlayer;
                }
            }
            return null;
        }

        void ReplyPlayer(BasePlayer player, string langkey) {
            SendReply(player, Lang("Prefix") + ": " + Lang(langkey));
        }

        void ReplyPlayer(BasePlayer player, string msg, bool formatted) {
            SendReply(player, Lang("Prefix") + ": " + msg);
        }

        List<BasePlayer> GetPlayers(string searchedPlayer) {
            List<BasePlayer> foundPlayers = new List<BasePlayer>();
            foreach(BasePlayer activePlayer in BasePlayer.activePlayerList) {
                if(activePlayer.displayName.ToLower().Contains(searchedPlayer.ToLower())) {
                    foundPlayers.Add(activePlayer);
                }
            }
            return foundPlayers;
        }

        void HealPlayer(BasePlayer player) {
            player.metabolism.hydration.value = 250;
            player.metabolism.calories.value = 500;
            player.metabolism.bleeding.value = 0;
            player.InitializeHealth(100, 100);
        }

        void StripPlayer(BasePlayer player) {
            player.inventory.Strip();
        }

        string Lang(string key) {
            return lang.GetMessage(key, this, null);
        }

        bool IsAllowed(BasePlayer player, string perm) {
            if (player.IsAdmin() && !usePermissionsOnly) return true;
            if (permission.UserHasPermission(player.UserIDString, adminPerm)) return true;
            if (permission.UserHasPermission(player.UserIDString, perm)) return true;
            return false;
        }
        #endregion

        #region Data Classes
        public class Team {
            public string name = "";
            public string kitname = "";
            public int teamLives = -1, usedLives = 0;
            public Vector3 spawnPos;
            public bool spawnSet = false;
            public List<TeamPlayer> players = new List<TeamPlayer>();

            public void SetSpawnPos(Vector3 pos) {
                spawnPos = pos;
                spawnSet = true;
            }

            public void AddPlayer(TeamPlayer player) {
                players.Add(player);
            }
        }

        public class TeamPlayer {
            public BasePlayer player;
            public float health, hydration, calories, bleeding;
            public Vector3 home;
            public Team team = null;
            public List<InvItem> invItems = new List<InvItem>();

            public void SaveHealth() {
                health = player.health;
                hydration = player.metabolism.hydration.value;
                calories = player.metabolism.calories.value;
                bleeding = player.metabolism.bleeding.value;
            }

            public void SavePos() {
                home = player.transform.position;
            }

            public void SaveInv() {
                invItems.Clear();
                invItems.AddRange(GetItems(player.inventory.containerWear, "wear"));
                invItems.AddRange(GetItems(player.inventory.containerMain, "main"));
                invItems.AddRange(GetItems(player.inventory.containerBelt, "belt"));
            }

            public void RestoreHealth() {
                player.InitializeHealth(health, 100);
                player.metabolism.hydration.value = hydration;
                player.metabolism.calories.value = calories;
                player.metabolism.bleeding.value = bleeding;
            }

            public void RestorePos() {
                TeleportTo(home);
                //Return them to the previous position
            }

            public void RestoreInv() {
                player.inventory.Strip();
                foreach (var invItem in invItems) {
                    var item = ItemManager.CreateByItemID(invItem.itemID, invItem.amount, invItem.bp, invItem.skin);
                    item.condition = invItem.condition;
                    var weapon = item.GetHeldEntity() as BaseProjectile;
                    if (weapon != null)
                        weapon.primaryMagazine.contents = invItem.ammo;
                    if (invItem.container == "belt")
                        player.inventory.GiveItem(item, player.inventory.containerBelt);
                    if (invItem.container == "main")
                        player.inventory.GiveItem(item, player.inventory.containerMain);
                    if (invItem.container == "wear")
                        player.inventory.GiveItem(item, player.inventory.containerWear);
                    if (invItem.contents == null)
                        continue;
                    foreach (var invItemCont in invItem.contents) {
                        var item1 = ItemManager.CreateByItemID(invItemCont.itemID, invItemCont.amount);
                        if (item1 == null)
                            continue;
                        item1.condition = invItemCont.condition;
                        item1.MoveToContainer(item.contents);
                    }
                }
            }

            private IEnumerable<InvItem> GetItems(ItemContainer container, string containerName) {
                return container.itemList.Select(item => new InvItem {
                    itemID = item.info.itemid,
                    bp = item.IsBlueprint(),
                    container = containerName,
                    amount = item.amount,
                    ammo = (item.GetHeldEntity() as BaseProjectile)?.primaryMagazine.contents ?? 0,
                    skin = item.skin,
                    condition = item.condition,
                    contents = item.contents?.itemList.Select(item1 => new InvItem {
                        itemID = item1.info.itemid,
                        amount = item1.amount,
                        condition = item1.condition
                    }).ToArray()
                });
            }

            public void TeleportTo(Vector3 pos) {
                if (player.net?.connection != null)
                    player.ClientRPCPlayer(null, player, "StartLoading", null, null, null, null, null);
                StartSleeping(player);
                player.MovePosition(pos);
                if (player.net?.connection != null)
                    player.ClientRPCPlayer(null, player, "ForcePositionTo", pos);
                player.TransformChanged();
                if (player.net?.connection != null)
                    player.SetPlayerFlag(BasePlayer.PlayerFlags.ReceivingSnapshot, true);
                player.UpdateNetworkGroup();
                player.SendNetworkUpdateImmediate(false);
                if (player.net?.connection == null) return;
                try { player.ClearEntityQueue(null); } catch { }
                player.SendFullSnapshot();
            }

            private void StartSleeping(BasePlayer player) {
                if (player.IsSleeping())
                    return;
                player.SetPlayerFlag(BasePlayer.PlayerFlags.Sleeping, true);
                if (!BasePlayer.sleepingPlayerList.Contains(player))
                    BasePlayer.sleepingPlayerList.Add(player);
                player.CancelInvoke("InventoryUpdate");
            }
        }

        public class InvItem {
            public int itemID, skin, amount, ammo;
            public bool bp;
            public string container;
            public float condition;
            public InvItem[] contents;
        }
        #endregion
    }
}