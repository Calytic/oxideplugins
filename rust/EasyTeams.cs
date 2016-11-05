using System;
using System.Collections.Generic;
using System.Linq;
using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Plugins;
using UnityEngine;

namespace Oxide.Plugins {
	[Info("EasyTeams", "Skrallex", "1.1.2")]
    [Description("Easily create minigame/pvp teams")]
    class EasyTeams : RustPlugin {
    	const string configVersion = "3";

    	[PluginReference]
    	static Plugin Kits;

        List<EventSettings> eventSettings = new List<EventSettings>();
    	StoredData data;

    	// Config Options
    	bool UsePermissionsOnly = false;

    	// Permissions
    	const string adminPerm = "easyteams.admin";
    	const string editPerm = "easyteams.edit";
    	const string viewPerm = "easyteams.view";
    	const string startPerm = "easyteams.start";
    	const string stopPerm = "easyteams.stop";
    	const string joinPerm = "easyteams.join";
    	const string joinSpecificPerm = "easyteams.joinSpecific";
    	const string claimPerm = "easyteams.claim";

    	public static Event teamEvent = new Event();
		System.Random rnd = new System.Random();

    	void Loaded() {
            // Load event settings file.
            data = Interface.Oxide.DataFileSystem.ReadObject<StoredData>("EasyTeamsData");
            if(data.eventSettings != null) {
                eventSettings = data.eventSettings;
            }

    		// Register permissions.
    		permission.RegisterPermission(adminPerm, this);
    		permission.RegisterPermission(editPerm, this);
    		permission.RegisterPermission(viewPerm, this);
    		permission.RegisterPermission(startPerm, this);
    		permission.RegisterPermission(stopPerm, this);
    		permission.RegisterPermission(joinPerm, this);
    		permission.RegisterPermission(joinSpecificPerm, this);
    		permission.RegisterPermission(claimPerm, this);

    		// Load config and localisations.
    		LoadDefaultMessages();
    		if(!GetConfigVersion().Equals(configVersion)) {
    			LoadDefaultConfig();
    		}
    		LoadConfig();
    	}

    	void Unload() {
    		if(teamEvent.started) {
    			StopEvent(null);
    		}
    	}

    	protected override void LoadDefaultConfig() {
    		Puts("Generating default config file");
    		Config.Clear();
    		Config["ConfigVersion"] = configVersion;
    		Config["UsePermissionsOnly"] = false;
    		SaveConfig();
    	}

    	string GetConfigVersion() {
    		return (string)Config["ConfigVersion"] == null ? "" : (string)Config["ConfigVersion"];
    	}

    	void LoadConfig() {
    		UsePermissionsOnly = (bool)Config["UsePermissionsOnly"] == null ? false :(bool)Config["UsePermissionsOnly"];
    	}

    	void LoadDefaultMessages() {
    		lang.RegisterMessages(new Dictionary<string, string> {
    			{"Prefix", "<color=orange>EasyTeams</color>"},
    			{"NoPermission", "You do not have permission to use this command."},
    			{"NoKits", "The <color=red>Kits</color> plugin is not installed or an invalid version is in use!"},
    			{"NoKitExists", "No kit with that name exists. Try a different name."},
    			{"KitSet", "You have set the kit for team <color=red>{0}</color> to '<color=red>{1}</color>'."},

    			{"SaveSyntax", "Invalid command syntax. Try <color=cyan>/teams_save</color> <color=red>EventName</color>"},
    			{"LoadSyntax", "Invalid command syntax. Try <color=cyan>/teams_load</color> <color=red>EventName</color>, or <color=cyan>/teams_load</color> to view available events."},
    			{"CreateSyntax", "Invalid command syntax. Try <color=cyan>/teams_create</color> <color=red>TeamName</color>"},
    			{"DeleteSyntax", "Invalid command syntax. Try <color=cyan>/teams_delete</color> <color=red>TeamName</color>"},
    			{"AddSyntax", "Invalid command syntax. Try <color=cyan>/teams_add</color> <color=red>TeamName Player1Name Player2Name Player3Name ...</color>"},
    			{"RemoveSyntax", "Invalid command syntax. Try <color=cyan>/teams_remove</color> <color=red>TeamName Player1Name Player2Name Player3Name ...</color>"},
    			{"ViewSyntax", "Invalid command syntax. Try <color=cyan>/teams_view</color> <color=red>{Optional: TeamName}</color>"},
    			{"JoinSyntax", "Invalid command syntax. Try <color=cyan>/teams_join</color> <color=red>TeamName</color> to join a specific team, or <color=cyan>/teams_join</color> to join a random team."},
				{"SpectateSyntax", "Invalid command syntax. Try <color=cyan>/teams_spectate> <color=red>True/False</color>."},
    			{"SetSpawnSyntax", "Invalid command syntax. Try <color=cyan>/teams_setspawn</color> <color=red>TeamName</color> to set the teams spawn point to your position."},
				{"SetKitSyntax", "Invalid command syntax. Try ."},

    			{"TeamAlreadyExists", "That team already exists! Try a different name."},
    			{"NoTeamExists", "There is no team with that name. Try a different name."},
    			{"NoTeams", "There are no teams currently setup. Use <color=cyan>/teams_create</color> <color=red>TeamName</color>"},
    			{"ManyPlayersFound", "More than one player name was found to match <color=red>{0}</color>. Please be more specific."},
    			{"NoPlayersFound", "No players were found matching the name <color=red>{0}</color>. Please try again."},
    			{"PlayerOnOtherTeam", "Player <color=red>{0}</color> is already on team <color=red>{1}</color>! Moving them to team <color=red>{2}</color>."},
    			{"PlayerAlreadyOnTeam", "Player <color=red>{0}</color> is already on that team!"},
    			{"PlayerNotOnAnyTeam", "Player <color=red>{0}</color> is not on any team."},
    			{"PlayerNotOnTeam", "Player <color=red>{0}</color> is not on team <color=red>{1}</color>."},
    			{"CantJoinAnother", "You can't join a different team when an event is started!"},
				{"TeamFull", "The team you're trying to join is already full. Try joining another."},
				{"NoJoinableTeams", "There doesn't appear to be any joinable teams at the moment. Wait for someone to leave or ask the event coordinator."},
    			{"NotInEvent", "You can't leave the event because you're not in it yet!"},
    			{"EventLeft", "You have left the event!"},

    			{"SaveExists", "An EventSettings with that name already exists. Overwriting."},
    			{"NoSaveExists", "Could not find an EventSettings with that name. Try a different name."},
    			{"EventSaved", "Successfully saved the event settings as '<color=red>{0}</color>'."},
    			{"EventLoaded", "Successfully loaded the event settings '<color=red>{0}</color>'."},
				{"EventsReloaded", "Successfully reloaded the event settings."},
    			{"NoEventsToLoad", "There are no saved events to load."},
    			{"ListEventsHeading", "The saved events are:"},
    			{"ListEventsEntry", "\n\t=> '<color=red>{0}</color>' (<color=red>{1}</color> teams)."},

    			{"TeamCreated", "You added team <color=red>{0}</color> as a new team!"},
    			{"TeamDeleted", "You deleted team <color=red>{0}</color>!"},
    			{"PlayerAdded", "You added player <color=red>{0}</color> to team <color=red>{1}</color>."},
    			{"PlayerRemoved", "You removed player <color=red>{0}</color> from team <color=red>{1}</color>!"},
    			{"TeamsCleared", "You have cleared all teams!"},
				{"SpectateSet", "You have set the use of the spectate team to <color=red>{0}</color>."},
    			{"SpawnSet", "You have set the spawn point for team <color=red>{0}</color> to your current position."},
    			{"SpawnsNotSet", "You have not yet setup spawns for team <color=red>{0}</color>!"},

				{"JoinNotAllowed", "You are not allowed to join this event! You must be added manually."},
				{"JIPNotAllowed", "The event has already started and you cannot join in progress."},
				{"JoinSpecificNotAllowed", "You are not allowed to specify a team to join for this event. Try /teams_join to join a random team."},
				{"JIPSpecificNotAllowed", "You are not allowed to specify a team to join after the event has started. Try /teams_join to join a random team."},
				{"NothingToClaim", "There are no event rewards/items for you to claim!"},
				{"EventItemsClaimed", "You have claimed your event rewards/items!"},

    			{"AlreadyStarted", "An event is already underway. Use '/teams_stop' first."},
    			{"NotStarted", "There is no event currently underway. Use '/teams_start' to start one."},
    			{"NotifyPlayer", "An event is starting! You are on team <color=red>{0}</color>. Your team has <color=red>{1}</color> lives remaining."},
    			{"EventEnded", "The event has now ended. Thanks for playing!"},
    			{"EventStarted", "You have started the event! Use <color=cyan>/teams_stop</color> to stop the event."},
    			{"EventStopped", "You have stopped the event!"},
    			{"TeamWon", "Team <color=red>{0}</color> has won the event! They had <color=red>{1}</color> lives remaining."},

    			{"ViewEvent", "Event Settings:\n\t Number of Teams: <color=red>{0}</color>\n\t Event Started: {1}\n\t Spectate Team Enabled: {2}" +
					"\n\t Player Join: {3}\n\t Player Join Specific Team: {4}\n\t Join In Progress: {5}" +
					"\n\t Join Specific Team In Progress: {6}\n\t Auto Balance Teams On Join: {7}\n\t Allow Team Switching: {8}\n\t End When Single Team Remaining: {9}" +
					"\n\t RestartWhenSingleTeamRemaining: {10}\n\t Keep Event Inventory As Reward: {11}\n\t Allow Reward Claiming: {12}\n\t Round Restart Timer: {13}s" +
					"\n\t Number of Top Teams: {14}\n\t Top Team Reward Kits: {15}"},
                {"ViewTeam", "Team Settings:\n\t Name: {0}\n\t Kit: {1}\n\t Lives Remaining: {2}/{3}\n\t Spawn Set: {4}{5}\n\t Joinable: {6}\n\t Max Players: {7}\n\t Players: {8}"},
                {"ViewPlayer", "\n\t\t=> <color=red>{0}</color>"},

                {"HelpText1", ""},
            	{"HelpText2", ""}
    			}, this);
    	}

    	void OnPlayerRespawned(BasePlayer player) {
    		if(!teamEvent.started)
    			return;
    		if(player == null)
    			return;
    		if(teamEvent.GetPlayersTeam(player) == null)
    			return;

    		Team team = teamEvent.GetPlayersTeam(player);
    		TeamPlayer tPlayer = teamEvent.GetTeamPlayer(player);
    		tPlayer.SaveItems(tPlayer.eventItems);

    		if(team.teamLives == -1) {
    			tPlayer.MoveToEvent(teamEvent, team, 1);
    			return;
    		}
    		if(team.usedLives < team.teamLives) {
    			team.usedLives++;
    			tPlayer.MoveToEvent(teamEvent, team, 1);
    			return;
    		}
    		if(teamEvent.spectateTeamEnabled) {
    			if(teamEvent.GetTeamByName("spectate") != null) {
    				Team spectate = teamEvent.GetTeamByName("spectate");
    				team.players.Remove(tPlayer);
    				spectate.players.Add(tPlayer);
    				tPlayer.MoveToEvent(teamEvent, spectate, 1);
    				return;
    			}
    		}
    	}

    	void OnEntityTakeDamage(BaseEntity entity, HitInfo info) {
    		if((entity as BasePlayer) == null) {
    			return;
    		}
    		BasePlayer target = entity as BasePlayer;
    		if(teamEvent.GetTeamPlayer(target) == null) {
    			return;
    		}
    		TeamPlayer tPlayer = teamEvent.GetTeamPlayer(target);
    		Team team = teamEvent.GetPlayersTeam(target);
    		if(team.usedLives == team.teamLives && !target.IsSleeping()) {
    			tPlayer.SaveItems(tPlayer.eventItems);
    		}
    	}

    	void OnPlayerDisconnected(BasePlayer player, string reason) {
    		if(teamEvent.GetPlayersTeam(player) == null)
    			return;
    		Team team = teamEvent.GetPlayersTeam(player);
    		RemovePlayer(player, team.name, player);
    	}

    	[ChatCommand("teams")]
    	void chatCmdTeams(BasePlayer player, string cmd, string[] args) {
			if(args.Length < 1) {
				ReplyPlayer(player, "HelpText1");
				ReplyPlayer(player, "HelpText2");
			}
    	}

    	[ChatCommand("teams_save")]
    	void chatCmdTeamsSave(BasePlayer player, string cmd, string[] args) {
    		if(!IsAllowed(player, editPerm)) {
    			ReplyPlayer(player, "NoPermission");
    			return;
    		}
    		if(args.Length != 1) {
    			ReplyPlayer(player, "SaveSyntax");
    			return;
    		}
    		if(teamEvent.teams.Count < 1) {
    			ReplyPlayer(player, "NoTeams");
    			return;
    		}
    		if(GetEventSettingsByName(args[0]) != null) {
    			ReplyPlayer(player, "SaveExists");
    			EventSettings e = GetEventSettingsByName(args[0]);
    			eventSettings.Remove(e);
    		}
    		EventSettings e1 = new EventSettings(teamEvent, args[0]);
    		eventSettings.Add(e1);
    		SaveEventSettings();
    		ReplyFormatted(player, String.Format(Lang("EventSaved"), e1.eventName));
    	}

    	[ChatCommand("teams_load")]
    	void chatCmdTeamsLoad(BasePlayer player, string cmd, string[] args) {
    		if(!IsAllowed(player, editPerm)) {
    			ReplyPlayer(player, "NoPermission");
    			return;
    		}
    		if(args.Length == 0) {
    			ListEvents(player);
    			return;
    		}
    		if(args.Length != 1) {
    			ReplyPlayer(player, "LoadSyntax");
    			return;
    		}
    		if(GetEventSettingsByName(args[0]) == null) {
    			ReplyPlayer(player, "NoSaveExists");
    			return;
    		}
    		Event e = GetEventByName(args[0]);
    		teamEvent = e;
    		ReplyFormatted(player, String.Format(Lang("EventLoaded"), args[0]));
    	}

    	void ListEvents(BasePlayer player) {
    		if(eventSettings.Count < 1) {
    			ReplyPlayer(player, "NoEventsToLoad");
				return;
    		}
    		string reply = Lang("ListEventsHeading");
    		foreach(EventSettings settings in eventSettings) {
    			reply += String.Format(Lang("ListEventsEntry"), settings.eventName, settings.teams.Count);
    		}
    		ReplyFormatted(player, reply);
    	}

		[ChatCommand("teams_reload")]
		void chatCmdTeamsReload(BasePlayer player, string cmd, string[] args) {
			if(!IsAllowed(player, editPerm)) {
				ReplyPlayer(player, "NoPermission");
				return;
			}
			Reload(player);
		}

		void Reload(BasePlayer player) {
			data = Interface.Oxide.DataFileSystem.ReadObject<StoredData>("EasyTeamsData");
			if(data.eventSettings != null) {
				eventSettings = data.eventSettings;
			}
			ReplyPlayer(player, "EventsReloaded");
		}

    	[ChatCommand("teams_create")]
    	void chatCmdTeamsCreate(BasePlayer player, string cmd, string[] args) {
    		if(!IsAllowed(player, editPerm)) {
    			ReplyPlayer(player, "NoPermission");
    			return;
    		}
    		if(args.Length != 1) {
    			ReplyPlayer(player, "CreateSyntax");
    			return;
    		}
    		CreateTeam(player, args[0]);
    	}

    	void CreateTeam(BasePlayer player, string teamName) {
    		if(teamEvent.GetTeamByName(teamName) != null) {
    			ReplyPlayer(player, "TeamAlreadyExists");
    			return;
    		}
    		Team team = new Team();
    		team.name = teamName;
    		team.teamLives = -1;
    		teamEvent.teams.Add(team);
    		ReplyFormatted(player, String.Format(Lang("TeamCreated"), team.name));
    	}

    	[ChatCommand("teams_delete")]
    	void chatCmdTeamsDelete(BasePlayer player, string cmd, string[] args) {
    		if(!IsAllowed(player, editPerm)) {
    			ReplyPlayer(player, "NoPermission");
    			return;
    		}
    		if(args.Length != 1) {
    			ReplyPlayer(player, "DeleteSyntax");
    			return;
    		}
    		DeleteTeam(player, args[0]);
    	}

    	void DeleteTeam(BasePlayer player, string teamName) {
    		if(teamEvent.GetTeamByName(teamName) == null) {
    			ReplyPlayer(player, "NoTeamExists");
    			return;
    		}
    		Team team = teamEvent.GetTeamByName(teamName);
    		teamEvent.teams.Remove(team);
    		ReplyFormatted(player, String.Format(Lang("TeamDeleted"), team.name));
    	}

    	[ChatCommand("teams_add")]
    	void chatCmdTeamsAdd(BasePlayer player, string cmd, string[] args){
    		List<string> players = new List<string>();
    		if(!IsAllowed(player, editPerm)) {
    			ReplyPlayer(player, "NoPermission");
    			return;
    		}
    		if(args.Length < 2) {
    			ReplyPlayer(player, "AddSyntax");
    			return;
    		}
    		for(int i = 1; i < args.Length; i++) {
    			if(GetPlayersByName(args[i]).Count > 1) {
    				ReplyFormatted(player, String.Format(Lang("ManyPlayersFound"), args[i]));
    				return;
    			}
    			if(GetPlayersByName(args[i]).Count == 0) {
    				ReplyFormatted(player, String.Format(Lang("NoPlayersFound"), args[i]));
    				return;
    			}
    			players.Add(args[i]);
    		}
    		AddPlayers(player, args[0], players.ToArray());
    	}

    	void AddPlayers(BasePlayer player, string teamName, string[] players) {
    		foreach(string name in players) {
    			AddPlayer(player, teamName, name);
    		}
    	}

    	void AddPlayer(BasePlayer player, string teamName, string playerName) {
    		if(teamEvent.GetTeamByName(teamName) == null) {
    			ReplyPlayer(player, "NoTeamExists");
    			return;
    		}

    		Team team = teamEvent.GetTeamByName(teamName);
    		BasePlayer target = GetPlayersByName(playerName)[0];
    		if(teamEvent.GetPlayersTeam(target) != null) {
    			Team currentTeam = teamEvent.GetPlayersTeam(target);
    			if(currentTeam == team) {
    				ReplyFormatted(player, String.Format(Lang("PlayerAlreadyOnTeam"), target.displayName));
    				return;
    			}
    			ReplyFormatted(player, String.Format(Lang("PlayerOnOtherTeam"), target.displayName, currentTeam.name, team.name));
    			RemovePlayer(player, currentTeam.name, playerName);
    		}
    		TeamPlayer tPlayer = new TeamPlayer();
    		tPlayer.player = target;
    		team.players.Add(tPlayer);
    		ReplyFormatted(player, String.Format(Lang("PlayerAdded"), target.displayName, team.name));
    		if(teamEvent.started) {
				if(!tPlayer.itemsSaved) {
					tPlayer.MoveToEventAndSave(teamEvent, team);
					return;
				}
    			tPlayer.MoveToEventAndSave(teamEvent, team);
    		}
    	}

    	[ChatCommand("teams_remove")]
    	void chatCmdTeamsRemove(BasePlayer player, string cmd, string[] args) {
    		List<string> players = new List<string>();
    		if(!IsAllowed(player, editPerm)) {
    			ReplyPlayer(player, "NoPermission");
    			return;
    		}
    		if(args.Length < 2) {
    			ReplyPlayer(player, "RemoveSyntax");
    			return;
    		}
    		for(int i = 1; i < args.Length; i++) {
    			if(GetPlayersByName(args[i]).Count > 1) {
    				ReplyFormatted(player, String.Format(Lang("ManyPlayersFound"), args[i]));
    				return;
    			}
    			if(GetPlayersByName(args[i]).Count == 0) {
    				ReplyFormatted(player, String.Format(Lang("NoPlayersFound"), args[i]));
    				return;
    			}
    			players.Add(args[i]);
    		}
    		RemovePlayers(player, args[0], players.ToArray());
    	}

    	void RemovePlayers(BasePlayer player, string teamName, string[] players) {
    		foreach(string name in players) {
    			RemovePlayer(player, teamName, name);
    		}
    	}

    	void RemovePlayer(BasePlayer player, string teamName, BasePlayer target) {
    		if(teamEvent.GetTeamByName(teamName) == null) {
    			ReplyPlayer(player, "NoTeamExists");
    			return;
    		}
    		Team team = teamEvent.GetTeamByName(teamName);
    		if(teamEvent.GetPlayersTeam(target) == null) {
    			ReplyPlayer(player, "PlayerNotOnAnyTeam");
    			return;
    		}
    		if(teamEvent.GetPlayersTeam(target) != team) {
    			ReplyFormatted(player, String.Format(Lang("PlayerNotOnTeam"), target.displayName, team.name));
    			return;
    		}
    		TeamPlayer tPlayer = teamEvent.GetTeamPlayer(target);
			if(teamEvent.started) {
				tPlayer.MoveFromEvent(teamEvent);
			}
    		team.players.Remove(tPlayer);
    		ReplyFormatted(player, String.Format(Lang("PlayerRemoved"), target.displayName, team.name));
    	}

    	void RemovePlayer(BasePlayer player, string teamName, string playerName) {
    		BasePlayer target = GetPlayersByName(playerName)[0];
    		RemovePlayer(player, teamName, target);
    	}

    	[ChatCommand("teams_view")]
    	void chatCmdTeamsView(BasePlayer player, string cmd, string[] args) {
    		if(!IsAllowed(player, viewPerm)) {
    			ReplyPlayer(player, "NoPermission");
    			return;
    		}
    		if(args.Length == 0) {
    			ViewAll(player);
    			return;
    		}
    		if(args.Length == 1) {
    			View(player, args[0]);
    			return;
    		}
    		ReplyPlayer(player, "ViewSyntax");
    	}

    	void ViewAll(BasePlayer player) {
    		if(teamEvent.teams.Count == 0) {
    			ReplyPlayer(player, "NoTeams");
    			return;
    		}
			string topTeamKits = "";
			foreach(string kit in teamEvent.topTeamRewardKits) {
				topTeamKits += kit;
			}
			string reply = String.Format(Lang("ViewEvent"), teamEvent.teams.Count, teamEvent.started, teamEvent.spectateTeamEnabled, teamEvent.allowPlayersJoin,
							teamEvent.allowPlayersJoinSpecificTeam, teamEvent.joinInProgress, teamEvent.joinSpecificTeamInProgress, teamEvent.autoBalanceTeamsOnJoin,
							teamEvent.allowTeamSwitching, teamEvent.endWhenSingleTeamRemaining, teamEvent.restartWhenSingleTeamRemaining, teamEvent.keepEventInventoryAsReward,
							teamEvent.allowRewardClaiming, teamEvent.roundRestartTimer, teamEvent.numberOfTopTeams, topTeamKits);
    		ReplyFormatted(player, reply);
    		foreach(Team team in teamEvent.teams) {
    			View(player, team.name);
    		}
    	}

    	void View(BasePlayer player, string teamName) {
    		if(teamEvent.GetTeamByName(teamName) == null) {
    			ReplyPlayer(player, "NoTeamExists");
    			return;
    		}
    		Team team = teamEvent.GetTeamByName(teamName);
			string spawnPos = "";
			if(team.spawnSet) {
				spawnPos = " (" + Math.Round(team.spawnPos.x, 1) + ", " + Math.Round(team.spawnPos.y, 1) + ", " + Math.Round(team.spawnPos.z, 1) + ")";
			}
    		string reply = String.Format(Lang("ViewTeam"), team.name, team.kitname, team.teamLives - team.usedLives, team.teamLives, team.spawnSet, spawnPos, team.joinable, team.maxPlayers, team.players.Count);

    		foreach(TeamPlayer tPlayer in team.players) {
    			reply += String.Format(Lang("ViewPlayer"), tPlayer.player.displayName);
    		}
    		ReplyFormatted(player, reply);
    	}

    	[ChatCommand("teams_clear")]
    	void chatCmdTeamsClear(BasePlayer player, string cmd, string[] args) {
    		if(!IsAllowed(player, editPerm)) {
    			ReplyPlayer(player, "NoPermission");
    			return;
    		}
    		Clear(player);
    	}

    	void Clear(BasePlayer player) {
    		teamEvent = new Event();
    		ReplyPlayer(player, "TeamsCleared");
    	}

    	[ChatCommand("teams_start")]
    	void chatCmdTeamsStart(BasePlayer player, string cmd, string[] args) {
    		if(!IsAllowed(player, startPerm)) {
    			ReplyPlayer(player, "NoPermission");
    			return;
    		}
    		StartEvent(player);
    	}

    	void StartEvent(BasePlayer player) {
    		if(teamEvent.started) {
    			ReplyPlayer(player, "AlreadyStarted");
    			return;
    		}
    		if(teamEvent.teams.Count == 0) {
    			ReplyPlayer(player, "NoTeams");
    			return;
    		}
    		foreach(Team team in teamEvent.teams) {
    			if(!team.spawnSet) {
    				ReplyFormatted(player, String.Format(Lang("SpawnsNotSet"), team.name));
    				return;
    			}
    		}
    		foreach(Team team in teamEvent.teams) {
    			int i = 0;
    			foreach(TeamPlayer tPlayer in team.players) {
    				tPlayer.MoveToEventAndSave(teamEvent, team, i);
    				i++;
    			}
    		}
    		teamEvent.started = true;
    		ReplyPlayer(player, "EventStarted");
    	}

		[ChatCommand("teams_end")]
		void chatCmdTeamsEnd(BasePlayer player, string cmd, string[] args) {

		}

    	[ChatCommand("teams_stop")]
    	void chatCmdTeamsStop(BasePlayer player, string cmd, string[] args) {
    		if(!IsAllowed(player, stopPerm)) {
    			ReplyPlayer(player, "NoPermission");
    			return;
    		}
    		StopEvent(player);
    	}

    	void StopEvent(BasePlayer player) {
    		if(!teamEvent.started) {
    			ReplyPlayer(player, "NotStarted");
    			return;
    		}
    		foreach(Team team in teamEvent.teams) {
    			foreach(TeamPlayer tPlayer in team.players) {
    				tPlayer.MoveFromEvent(teamEvent);
    			}
    		}

    		teamEvent.started = false;
    		ReplyPlayer(player, "EventStopped");
    	}

		[ChatCommand("teams_restart")]
		void chatCmdTeamsRestart(BasePlayer player, string cmd, string[] args) {
			if(!IsAllowed(player, startPerm)) {
				ReplyPlayer(player, "NoPermission");
				return;
			}
			RestartEvent(player);
		}

		void RestartEvent(BasePlayer player) {
			if(!teamEvent.started) {
				ReplyPlayer(player, "NotStarted");
				return;
			}
			foreach(Team team in teamEvent.teams) {
				foreach(TeamPlayer tPlayer in team.players) {
					tPlayer.MoveToEvent(teamEvent, team);
				}
			}
			ReplyPlayer(player, "EventRestarted");
		}

    	[ChatCommand("teams_join")]
    	void chatCmdTeamsJoin(BasePlayer player, string cmd, string[] args) {
    		if(!IsAllowed(player, joinPerm)) {
    			ReplyPlayer(player, "NoPermission");
    			return;
    		}
			if(!teamEvent.allowPlayersJoin) {
				ReplyPlayer(player, "JoinNotAllowed");
				return;
			}
			if(teamEvent.started && !teamEvent.joinInProgress) {
				ReplyPlayer(player, "JIPNotAllowed");
				return;
			}
			if(teamEvent.GetPlayersTeam(player) != null && !teamEvent.allowTeamSwitching) {
				ReplyPlayer(player, "CantJoinAnother");
				return;
			}
			if(args.Length > 0) {
				if(!teamEvent.allowPlayersJoinSpecificTeam) {
					ReplyPlayer(player, "JoinSpecificNotAllowed");
					return;
				}
				if(teamEvent.started && !teamEvent.joinSpecificTeamInProgress) {
					ReplyPlayer(player, "JIPSpecificNotAllowed");
					return;
				}
				JoinTeam(player, args[0]);
				return;
			}
			JoinRandomTeam(player);
			return;
    	}

    	void JoinRandomTeam(BasePlayer player) {
    		string teamName = "";
    		int leastPlayers = 1000;

			if(!teamEvent.autoBalanceTeamsOnJoin) {
				int i = rnd.Next(0, teamEvent.teams.Count);
				Puts("i = " + i);
				foreach(Team team in teamEvent.teams) {
					Puts("testing for team " + team.name);
					if(team == teamEvent.teams.ElementAt(i)) {
						if(team.name.Equals("spectate")) {
							i = rnd.Next(0, teamEvent.teams.Count);
							continue;
						}
						if(!team.joinable) {
							i = rnd.Next(0, teamEvent.teams.Count);
							continue;
						}
						if(team.players.Count >= team.maxPlayers && team.maxPlayers != -1) {
							i = rnd.Next(0, teamEvent.teams.Count);
							continue;
						}
						teamName = team.name;
					}
				}
				if(teamName == "") {
					ReplyPlayer(player, "NoJoinableTeams");
					return;
				}
			}
			else {
				foreach(Team team in teamEvent.teams) {
					if(team.name.Equals("spectate")){
						continue;
					}
					if(!team.joinable)
						continue;
					if(team.players.Count >= team.maxPlayers)
						continue;
					if(team.players.Count < leastPlayers) {
						teamName = team.name;
						leastPlayers = team.players.Count;
					}
				}
			}
            JoinTeam(player, teamName);
    	}

    	void JoinTeam(BasePlayer player, string teamName) {
    		if(teamEvent.GetTeamByName(teamName) == null) {
    			ReplyPlayer(player, "NoTeamExists");
    			return;
    		}
			Team team = teamEvent.GetTeamByName(teamName);
			if(team.players.Count >= team.maxPlayers && team.maxPlayers != -1) {
				ReplyPlayer(player, "TeamFull");
				return;
			}
    		AddPlayer(player, teamName, player.displayName);
    	}

    	[ChatCommand("teams_leave")]
    	void chatCmdTeamsLeave(BasePlayer player, string cmd, string[] args) {
    		Leave(player);
    	}

    	void Leave(BasePlayer player) {
    		if(teamEvent.GetTeamPlayer(player) == null) {
    			ReplyPlayer(player, "NotInEvent");
    			return;
    		}
    		TeamPlayer tPlayer = teamEvent.GetTeamPlayer(player);
    		Team team = teamEvent.GetPlayersTeam(player);
			if(teamEvent.started)
    			tPlayer.MoveFromEvent(teamEvent);
    		RemovePlayer(player, team.name, player);
    		ReplyPlayer(player, "EventLeft");
    	}

    	[ChatCommand("teams_claim")]
    	void chatCmdTeamsClaim(BasePlayer player, string cmd, string[] args) {
    		if(!IsAllowed(player, claimPerm)) {
    			ReplyPlayer(player, "NoPermission");
    			return;
    		}
    		Claim(player);
    	}

    	void Claim(BasePlayer player) {
    		if(teamEvent.GetTeamPlayer(player) == null) {
    			ReplyPlayer(player, "NotInEvent");
    			return;
    		}
    		TeamPlayer tPlayer = teamEvent.GetTeamPlayer(player);
    		if(tPlayer.eventItems.Count == 0) {
    			ReplyPlayer(player, "NothingToClaim");
    			return;
    		}
    		tPlayer.RestoreEventItems();
    		ReplyPlayer(player, "EventItemsClaimed");
    	}

		[ChatCommand("teams_spectate")]
		void chatCmdTeamsSpectate(BasePlayer player, string cmd, string[] args) {
			bool option;
			if(!IsAllowed(player, editPerm)) {
				ReplyPlayer(player, "NoPermission");
				return;
			}
			if(args.Length != 1) {
				ReplyPlayer(player, "SpectateSyntax");
				return;
			}
			if(!Boolean.TryParse(args[0], out option)) {
				ReplyPlayer(player, "SpectateSyntax");
				return;
			}
			Boolean.TryParse(args[0], out option);
			Spectate(player, option);
		}

		void Spectate(BasePlayer player, bool enabled) {
			teamEvent.spectateTeamEnabled = enabled;
			ReplyFormatted(player, String.Format(Lang("SpectateSet"), enabled));

			if(enabled) {
				if(teamEvent.GetTeamByName("spectate") == null) {
					CreateTeam(player, "spectate");
				}
				return;
			}
			DeleteTeam(player, "spectate");
		}

    	[ChatCommand("teams_setspawn")]
    	void chatCmdTeamsSetSpawn(BasePlayer player, string cmd, string[] args) {
    		if(!IsAllowed(player, editPerm)) {
    			ReplyPlayer(player, "NoPermission");
    			return;
    		}
    		if(args.Length != 1) {
    			ReplyPlayer(player, "SetSpawnSyntax");
    			return;
    		}
    		SetSpawn(player, args[0]);
    	}

    	void SetSpawn(BasePlayer player, string teamName) {
    		if(teamEvent.GetTeamByName(teamName) == null) {
    			ReplyPlayer(player, "NoTeamExists");
    			return;
    		}
    		Team team = teamEvent.GetTeamByName(teamName);
    		Pos playerPos = new Pos(player.transform.position);
    		team.SetSpawnPos(playerPos);
    		ReplyFormatted(player, String.Format(Lang("SpawnSet"), team.name));
    	}

    	[ChatCommand("teams_setkit")]
    	void chatCmdTeamsSetKit(BasePlayer player, string cmd, string[] args) {
    		if(!IsAllowed(player, editPerm)) {
    			ReplyPlayer(player, "NoPermission");
    			return;
    		}
    		if(args.Length != 2) {
    			ReplyPlayer(player, "SetKitSyntax");
    			return;
    		}
    		SetKit(player, args[0], args[1]);
    	}

    	void SetKit(BasePlayer player, string teamName, string kitName) {
    		if(teamEvent.GetTeamByName(teamName) == null) {
    			ReplyPlayer(player, "NoTeamExists");
    			return;
    		}
    		Team team = teamEvent.GetTeamByName(teamName);
    		object success = Interface.Oxide.CallHook("isKit", kitName);
    		if(!(success is bool)) {
    			ReplyPlayer(player, "NoKits");
    			return;
    		}
    		if(!(bool)success) {
    			ReplyPlayer(player, "NoKitExists");
    			return;
    		}
    		team.kitname = kitName;
    		ReplyFormatted(player, String.Format(Lang("KitSet"), team.name, team.kitname));
    	}

		Event GetTeamEvent() {
			return teamEvent;
		}

    	void ReplyPlayer(BasePlayer player, string langKey) {
    		SendReply(player, Lang("Prefix") + ": " + Lang(langKey));
    	}

        void ReplyFormatted(BasePlayer player, string msg) {
    		SendReply(player, Lang("Prefix") + ": " + msg);
    	}

        string Lang(string key) {
    		return lang.GetMessage(key, this, null);
    	}

    	bool IsAllowed(BasePlayer player, string perm) {
    		if(player.IsAdmin() && !UsePermissionsOnly) return true;
    		if(permission.UserHasPermission(player.UserIDString, adminPerm)) return true;
    		if(permission.UserHasPermission(player.UserIDString, perm)) return true;
    		return false;
    	}

    	List<BasePlayer> GetPlayersByName(string playerName) {
    		List<BasePlayer> foundPlayers = new List<BasePlayer>();
    		foreach(BasePlayer activePlayer in BasePlayer.activePlayerList) {
    			if(activePlayer.displayName.ToLower().Contains(playerName.ToLower()))
    				foundPlayers.Add(activePlayer);
    		}
    		return foundPlayers;
    	}

        EventSettings GetEventSettingsByName(string eventName) {
            foreach(EventSettings e in eventSettings) {
                if(e.eventName.ToLower().Equals(eventName.ToLower()))
                    return e;
            }
            return null;
        }

        Event GetEventByName(string eventName) {
        	if(GetEventSettingsByName(eventName) != null) {
        		EventSettings settings = GetEventSettingsByName(eventName);
        		Event e = new Event(settings);
        		return e;
        	}

            return null;
        }

    	void SaveEventSettings() {
            if(eventSettings != null) {
                data.eventSettings = eventSettings;
            }
            Interface.Oxide.DataFileSystem.WriteObject("EasyTeamsData", data);
    	}

	   	public class Team {
	   		public string name = "";
	   		public string kitname = "";
	   		public int teamLives = -1, usedLives = 0;
			public int maxPlayers = -1;
	   		public Pos spawnPos = new Pos(0.0f, 0.0f, 0.0f);
	   		public bool spawnSet = false;
	   		public bool joinable = true;
	   		public List<TeamPlayer> players = new List<TeamPlayer>();

	   		public void SetSpawnPos(Pos pos) {
	   			this.spawnPos = pos;
	   			this.spawnSet = true;
	   		}
	    }

	    public class TeamPlayer {
	    	public BasePlayer player;
	    	public float health, hydration, calories, bleeding;
	    	public Pos homePos;
	    	public Team team;
			public bool itemsSaved = false;
	    	public List<InvItem> items = new List<InvItem>();
	    	public List<InvItem> kitItems = new List<InvItem>();
	    	public List<InvItem> eventItems = new List<InvItem>();

	    	public void SaveHealth() {
	    		health = player.health;
	    		hydration = player.metabolism.hydration.value;
	    		calories = player.metabolism.calories.value;
	    		bleeding = player.metabolism.bleeding.value;
	    	}

	    	public void SavePos() {
	    		homePos = new Pos(player.transform.position);
	    	}

	    	public void SaveItems(List<InvItem> itemList) {
	    		itemList.Clear();
	    		itemList.AddRange(GetItems(player.inventory.containerWear, "wear"));
	    		itemList.AddRange(GetItems(player.inventory.containerMain, "main"));
	    		itemList.AddRange(GetItems(player.inventory.containerBelt, "belt"));
				if(itemList == items)
					itemsSaved = true;
	    	}

	    	public void RestoreHealth() {
	    		player.InitializeHealth(health, 100);
	    		player.metabolism.hydration.value = hydration;
	    		player.metabolism.calories.value = calories;
	    		player.metabolism.bleeding.value = bleeding;
	    	}

	    	public void RestorePos() {
	    		TeleportTo(homePos);
	    	}

			public void RestoreItems(List<InvItem> itemList, bool strip = true) {
	    		if(strip)
	    			player.inventory.Strip();
            	foreach (var invItem in itemList) {
          			var item = ItemManager.CreateByItemID(invItem.itemId, invItem.amount, invItem.skin);
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
				        var item1 = ItemManager.CreateByItemID(invItemCont.itemId, invItemCont.amount);
				        if (item1 == null)
				    		continue;
				        item1.condition = invItemCont.condition;
				        item1.MoveToContainer(item.contents);
          			}
            	}
				if(itemList == items)
					itemsSaved = false;
	    	}

	    	public void RestoreEventItems() {
	    		List<InvItem> gainedItems = new List<InvItem>();
	    		foreach(InvItem eventItem in eventItems) {
	    			gainedItems.Add(eventItem);
	    			foreach(InvItem kitItem in kitItems) {
	    				if(eventItem.itemId == kitItem.itemId && eventItem.skin == kitItem.skin) {
	    					if(kitItem.amount >= eventItem.amount) {
	    						kitItem.amount -= eventItem.amount;
	    						eventItem.amount = 0;
	    					}
	    					if(eventItem.amount > 0) {
	    				    	eventItem.amount -= kitItem.amount;
	    				    	kitItem.amount = 0;
	    					}
	    				}
	    			}
	    			if(eventItem.amount <= 0) {
	    				gainedItems.Remove(eventItem);
	    			}
	    		}
	    		RestoreItems(gainedItems, false);
	    		eventItems = new List<InvItem>();
	    	}

	    	public void Strip() {
	    		player.inventory.Strip();
	    	}

	    	public void Heal() {
	    		player.metabolism.hydration.value = 250;
            	player.metabolism.calories.value = 500;
            	player.metabolism.bleeding.value = 0;
            	player.InitializeHealth(100, 100);
	    	}

	    	public void TeleportTo(Pos pos) {
	    		Vector3 vec3Pos = new Vector3(pos.x, pos.y, pos.z);

	    		if (player.net?.connection != null)
                    player.ClientRPCPlayer(null, player, "StartLoading", null, null, null, null, null);
                StartSleeping();
                player.MovePosition(vec3Pos);

                if (player.net?.connection != null)
                    player.ClientRPCPlayer(null, player, "ForcePositionTo", vec3Pos);
                player.TransformChanged();

                if (player.net?.connection != null)
                    player.SetPlayerFlag(BasePlayer.PlayerFlags.ReceivingSnapshot, true);
                player.UpdateNetworkGroup();
                player.SendNetworkUpdateImmediate(false);

                if (player.net?.connection == null) return;
                try { player.ClearEntityQueue(null); } catch { }
                player.SendFullSnapshot();
	    	}

	    	public void StartSleeping() {
	    		if (player.IsSleeping())
                    return;

                player.SetPlayerFlag(BasePlayer.PlayerFlags.Sleeping, true);
                if (!BasePlayer.sleepingPlayerList.Contains(player))
                    BasePlayer.sleepingPlayerList.Add(player);
                player.CancelInvoke("InventoryUpdate");
	    	}

	    	public void MoveToEventAndSave(Event teamEvent, Team playerTeam, int spawnOffset = 1) {
				team = playerTeam;
				SaveHealth();
				SavePos();
				SaveItems(items);
				MoveToEvent(teamEvent, playerTeam, spawnOffset);
	    	}

	    	public void MoveToEvent(Event teamEvent, Team playerTeam, int spawnOffset = 1) {
				Pos offsetSpawn = new Pos(playerTeam.spawnPos.x + spawnOffset * 0.1f, playerTeam.spawnPos.y + spawnOffset * 0.1f, playerTeam.spawnPos.z + spawnOffset * 0.1f);
				TeleportTo(offsetSpawn);
				Strip();
				Heal();
				Interface.Oxide.CallHook("GiveKit", player, playerTeam.kitname);
				SaveItems(kitItems);
	    	}

	    	public void MoveFromEvent(Event teamEvent) {
	    		if(!teamEvent.GetPlayersTeam(player).name.ToLower().Equals("spectate"))
					SaveItems(eventItems);
				Strip();
				RestorePos();
				RestoreHealth();
				RestoreItems(items);
	    	}

	    	private IEnumerable<InvItem> GetItems(ItemContainer container, string containerName) {
                return container.itemList.Select(item => new InvItem {
                    itemId = item.info.itemid,
                    container = containerName,
                    amount = item.amount,
                    ammo = (item.GetHeldEntity() as BaseProjectile)?.primaryMagazine.contents ?? 0,
                    skin = item.skin,
                    condition = item.condition,
                    contents = item.contents?.itemList.Select(item1 => new InvItem {
                        itemId = item1.info.itemid,
                        amount = item1.amount,
                        condition = item1.condition
                    }).ToArray()
                });
            }
	    }

	    public class InvItem {
	    	public int itemId, amount, ammo;
			public ulong skin;
	    	public string container;
	    	public float condition;
	    	public InvItem[] contents;
	    }

	    public class Event {
			public bool started = false;
			public bool spectateTeamEnabled = false;
			public bool allowPlayersJoin = false;
			public bool allowPlayersJoinSpecificTeam = false;
			public bool joinInProgress = false;
			public bool joinSpecificTeamInProgress = false;
			public bool autoBalanceTeamsOnJoin = false;
			public bool allowTeamSwitching = false;
			public bool endWhenSingleTeamRemaining = false;
			public bool restartWhenSingleTeamRemaining = false;
			public bool keepEventInventoryAsReward = false;
			public bool allowRewardClaiming = false;

			public int roundRestartTimer = 15;
			public int numberOfTopTeams = 3;
			public List<string> topTeamRewardKits = new List<string>();

	    	public List<Team> teams = new List<Team>();
	    	public Event() {
			}

	    	public Event(EventSettings settings) {
				this.spectateTeamEnabled = settings.spectateTeamEnabled;
				this.allowPlayersJoin = settings.allowPlayersJoin;
				this.allowPlayersJoinSpecificTeam = settings.allowPlayersJoinSpecificTeam;
				this.joinInProgress = settings.joinInProgress;
				this.joinSpecificTeamInProgress = settings.joinSpecificTeamInProgress;
				this.autoBalanceTeamsOnJoin = settings.autoBalanceTeamsOnJoin;
				this.allowTeamSwitching = settings.allowTeamSwitching;
				this.endWhenSingleTeamRemaining = settings.endWhenSingleTeamRemaining;
				this.restartWhenSingleTeamRemaining = settings.restartWhenSingleTeamRemaining;
				this.keepEventInventoryAsReward = settings.keepEventInventoryAsReward;
				this.allowRewardClaiming = settings.allowRewardClaiming;

				this.roundRestartTimer = settings.roundRestartTimer;
				this.numberOfTopTeams = settings.numberOfTopTeams;
				this.topTeamRewardKits = settings.topTeamRewardKits;
	    		this.teams = settings.teams;
	    	}

	    	public Team GetTeamByName(string teamName) {
	    		foreach(Team team in teams) {
	    			if(team.name.ToLower().Equals(teamName.ToLower()))
	    				return team;
	    		}
	    		return null;
	    	}

	    	public Team GetPlayersTeam(BasePlayer player) {
	    		foreach(Team team in teams) {
	    			foreach(TeamPlayer tPlayer in team.players) {
	    				if(tPlayer.player == player) {
	    					return team;
	    				}
	    			}
	    		}
	    		return null;
	    	}

	    	public TeamPlayer GetTeamPlayer(BasePlayer player) {
	    		foreach(Team team in teams) {
	    			foreach(TeamPlayer tPlayer in team.players) {
	    				if(tPlayer.player == player){
	    					return tPlayer;
	    				}
	    			}
	    		}
	    		return null;
	    	}
	    }

	    public class EventSettings {
            public string eventName = "";
			public bool spectateTeamEnabled = false;
			public bool allowPlayersJoin = false;
			public bool allowPlayersJoinSpecificTeam = false;
			public bool joinInProgress = false;
			public bool joinSpecificTeamInProgress = false;
			public bool autoBalanceTeamsOnJoin = false;
			public bool allowTeamSwitching = false;
			public bool endWhenSingleTeamRemaining = false;
			public bool restartWhenSingleTeamRemaining = false;
			public bool keepEventInventoryAsReward = false;
			public bool allowRewardClaiming = false;

			public int roundRestartTimer = 15;
			public int numberOfTopTeams = 3;
			public List<string> topTeamRewardKits = new List<string>();

            public List<Team> teams = new List<Team>();

            public EventSettings(Event teamEvent, string eventName) {
            	this.eventName = eventName;
            	this.spectateTeamEnabled = teamEvent.spectateTeamEnabled;
            	this.allowPlayersJoin = teamEvent.allowPlayersJoin;
            	this.allowPlayersJoinSpecificTeam = teamEvent.allowPlayersJoinSpecificTeam;
            	this.joinInProgress = teamEvent.joinInProgress;
				this.joinSpecificTeamInProgress = teamEvent.joinSpecificTeamInProgress;
				this.autoBalanceTeamsOnJoin = teamEvent.autoBalanceTeamsOnJoin;
				this.allowTeamSwitching = teamEvent.allowTeamSwitching;
				this.endWhenSingleTeamRemaining = teamEvent.endWhenSingleTeamRemaining;
				this.restartWhenSingleTeamRemaining = teamEvent.restartWhenSingleTeamRemaining;
				this.keepEventInventoryAsReward = teamEvent.keepEventInventoryAsReward;
				this.allowRewardClaiming = teamEvent.allowRewardClaiming;

				this.roundRestartTimer = teamEvent.roundRestartTimer;
				this.numberOfTopTeams = teamEvent.numberOfTopTeams;
				this.topTeamRewardKits = teamEvent.topTeamRewardKits;

            	foreach(Team team in teamEvent.teams) {
            		Team emptyTeam = team;
            		emptyTeam.players = new List<TeamPlayer>();
            		teams.Add(emptyTeam);
            	}

				if(numberOfTopTeams > topTeamRewardKits.Count) {
					int diff = numberOfTopTeams - topTeamRewardKits.Count;
					for(int i = 0; i < diff; i++) {
						topTeamRewardKits.Add("");
					}
				}
				if(topTeamRewardKits.Count > numberOfTopTeams) {
					int diff = topTeamRewardKits.Count - numberOfTopTeams;
					for(int i = 0; i < diff; i++) {
						topTeamRewardKits.Remove(topTeamRewardKits.Last());
					}
				}
            }

            public EventSettings() {
			}
	    }

	    public class StoredData {
	    	public List<EventSettings> eventSettings = new List<EventSettings>();
	    }

        [System.Serializable]
	    public class Pos {
	    	public float x, y, z;

	    	public Pos(float x, float y, float z) {
	    		this.x = x;
	    		this.y = y;
	    		this.z = z;
	    	}

	    	public Pos(Vector3 vec3) {
	    		this.x = vec3.x;
	    		this.y = vec3.y;
	    		this.z = vec3.z;
	    	}

	    	public Pos() {}
	    }
    }
}
