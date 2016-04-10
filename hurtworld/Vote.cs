// Reference: UnityEngine.UI
using System.Collections.Generic;
using System;

namespace Oxide.Plugins
{
    [Info("Vote", "Noviets", "1.0.6", ResourceId = 1676)]
    [Description("Start a Yes or No vote")]
    class Vote : HurtworldPlugin
    {
		Dictionary<PlayerSession, string> curVote = new Dictionary<PlayerSession, string>();
		Dictionary<PlayerSession, string> voteYes = new Dictionary<PlayerSession, string>();
		Dictionary<PlayerSession, string> voteNo = new Dictionary<PlayerSession, string>();
		void LoadDefaultMessages()
        {
            var messages = new Dictionary<string, string>
            {
                {"nopermission","Vote: You dont have Permission to do this!"},
				{"voteerror","Vote: What do you want vote for?"},
				{"kickmsg","Vote kicked"},
				{"banmsg","Vote banned"},
				{"alreadyvoting","Vote: There's already a vote underway! You must wait for it to end before starting another."},
				{"playernotfound","Vote: Unable to find the player: {Player}. To do a kick or ban vote: /vote kick player (optional) -or- /vote ban player (optional)"},
				{"VoteStarted", "<color=yellow>[VOTE]</color> <color=orange>A new vote has started</color>: <color=green>{VoteFor}</color> ({For}/{Against})"},
				{"VoteEnded", "<color=yellow>[VOTE]</color> Voting has ended. {For}: <color=orange>{ForCount}</color>  {Against}: <color=orange>{AgainstCount}</color> Result: <color=orange>{Result}</color>"}
            };
			
			lang.RegisterMessages(messages, this);
        }
		string Msg(string msg, string SteamId = null) => lang.GetMessage(msg, this, SteamId);
		protected override void LoadDefaultConfig()
        {
			if(Config["VoteTimeSeconds"] == null) Config.Set("VoteTimeSeconds", 30);
			if(Config["SecondsChangeLootModeBack"] == null) Config.Set("SecondsChangeLootModeBack", 3600f);
			if(Config["VoteFor"] == null) Config.Set("VoteFor", "Yes");
			if(Config["Daytime"] == null) Config.Set("Daytime", 0.4);
			if(Config["Nighttime"] == null) Config.Set("Nighttime", 0.9);
			if(Config["AutoResult"] == null) Config.Set("AutoResult", true);
			if(Config["VoteAgainst"] == null) Config.Set("VoteAgainst", "No");
			SaveConfig();
        }
		void Loaded()
		{
			permission.RegisterPermission("Vote.admin", this);
			LoadDefaultConfig();
			LoadDefaultMessages();
			
		}
		[ChatCommand("vote")]
        void cmdvote(PlayerSession session, string command, string[] args)
        {
            if(permission.UserHasPermission(session.SteamId.ToString(),"Vote.admin"))
            {
				if (args.Length > 0)
				{
					if(curVote.Count < 1)
					{
						string votefor = string.Join(" ", args);
						string votetype = args[0];
						PlayerSession target = null;
						if(votetype.ToLower() == "kick" || votetype.ToLower() == "ban" && (bool)Config["AutoResult"])
						{ 
							target = GetSession(args[1]);
							if(target == null)
							{
								hurt.SendChatMessage(session, Msg("playernotfound",session.SteamId.ToString()).Replace("{Player}",args[1]));
								return;
							}
								
						}
						hurt.BroadcastChat(Msg("VoteStarted",session.SteamId.ToString()).Replace("{VoteFor}",votefor).Replace("{For}", Convert.ToString(Config["VoteFor"])).Replace("{Against}", Convert.ToString(Config["VoteAgainst"])));
						foreach (KeyValuePair<uLink.NetworkPlayer, PlayerSession> pair in (Dictionary<uLink.NetworkPlayer, PlayerSession>)GameManager.Instance.GetSessions())
						{
							if (pair.Value.IsLoaded)
							{
								StartVote(pair.Value, votefor);
							}
						}
						timer.Once(Convert.ToSingle(Config["VoteTimeSeconds"])+0.1f, () =>
						{
							string msg = Msg("VoteEnded",session.SteamId.ToString()).Replace("{ForCount}",voteYes.Count.ToString()).Replace("{AgainstCount}",voteNo.Count.ToString()).Replace("{For}", Convert.ToString(Config["VoteFor"])).Replace("{Against}", Convert.ToString(Config["VoteAgainst"]));
							if(voteYes.Count > voteNo.Count)
							{
								hurt.BroadcastChat(msg.Replace("{Result}", Convert.ToString(Config["VoteFor"])));
								if((bool)Config["AutoResult"])
								{
									if(votetype.ToLower() == "kick" || votetype.ToLower() == "ban") DoResult(votetype, target);
									if(votefor.ToLower().Contains("day")) DoResult("day");
									if(votefor.ToLower().Contains("night")) DoResult("night");
									if(votefor.ToLower().Contains("weather")) DoResult("weather");
									if(votefor.ToLower().Contains("lootmode"))
									{
										if(votefor.ToLower().Contains("everything") || votefor.ToLower().Contains("full")) DoResult("LootmodeEverything");
										if(votefor.ToLower().Contains("backpack")) DoResult("LootmodeBackpack");
										if(votefor.ToLower().Contains("infamy") || votefor.ToLower().Contains("default")) DoResult("LootmodeInfamy");
										if(votefor.ToLower().Contains("none") || votefor.ToLower().Contains("off")) DoResult("LootmodeNone");
									}
								}
							}
							else
								hurt.BroadcastChat(msg.Replace("{Result}", Convert.ToString(Config["VoteAgainst"])));
							curVote.Clear();
							voteYes.Clear();
							voteNo.Clear();
						});
					}
					else
						hurt.SendChatMessage(session, Msg("alreadyvoting",session.SteamId.ToString()));
				}
				else
					hurt.SendChatMessage(session, Msg("voteerror",session.SteamId.ToString()));
			}
			else
				hurt.SendChatMessage(session, Msg("nopermission",session.SteamId.ToString()));
		}

		void DoResult(string votetype, PlayerSession target = null)
		{
			if(votetype == "kick")
				Singleton<GameManager>.Instance.KickPlayer(target.SteamId.ToString(), Msg("kickmsg",target.SteamId.ToString()));
			if(votetype == "ban")
			{
				cmd("ban " + target.SteamId.ToString());
				Singleton<GameManager>.Instance.KickPlayer(target.SteamId.ToString(), Msg("banmsg",target.SteamId.ToString()));
			}
			if(votetype == "day")
				cmd("settime "+Convert.ToString(Config["Daytime"]));
			if(votetype == "night")
				cmd("settime "+Convert.ToString(Config["Nighttime"]));
			if(votetype == "weather")
			{
				uLink.NetworkView[] objectsOfType = UnityEngine.Object.FindObjectsOfType<uLink.NetworkView>();
				int num = 0;
				foreach (uLink.NetworkView view in objectsOfType)
				{
					if (view.isActiveAndEnabled && view.name.ToLower().Contains("sandstorm") || view.name.ToLower().Contains("rain") || view.name.ToLower().Contains("blizzard") || view.name.ToLower().Contains("fog") || view.name.ToLower().Contains("overcast") || view.name.ToLower().Contains("snow"))
					{
						Singleton<NetworkManager>.Instance.NetDestroy(view);
						++num;
					}
				}
			}
			if(votetype.Contains("Lootmode"))
			{
				string original = Singleton<GameManager>.Instance.ServerConfig.PlayerLootMode.ToString();
				string lootmode = "";
				if(votetype == "LootmodeEverything"){
					Singleton<GameManager>.Instance.ServerConfig.PlayerLootMode = EServerPlayerLootMode.Everything;
					lootmode = "<color=yellow>FULL LOOT</color> (You drop Everything)";
				}
				if(votetype == "LootmodeBackpack"){
					Singleton<GameManager>.Instance.ServerConfig.PlayerLootMode = EServerPlayerLootMode.BackpackOnly;
					lootmode = "<color=yellow>Backpack only</color> (Ignores Infamy)";
				}
				if(votetype == "LootmodeInfamy"){
					Singleton<GameManager>.Instance.ServerConfig.PlayerLootMode = EServerPlayerLootMode.BackpackOnlyWithInfamy;
					lootmode = "<color=yellow>Infamy based</color> (This is the default loot mode)";
				}
				if(votetype == "LootmodeNone"){
					Singleton<GameManager>.Instance.ServerConfig.PlayerLootMode = EServerPlayerLootMode.None;
					lootmode = "<color=yellow>None</color> (You drop Nothing)";
				}
				hurt.BroadcastChat("<color=orange>[Loot Mode]</color> Loot mode has been changed to: "+lootmode);
				if(Singleton<GameManager>.Instance.ServerConfig.PlayerLootMode.ToString() != "BackpackOnlyWithInfamy")
				{
					hurt.BroadcastChat("<color=orange>[Loot Mode]</color> Loot mode will be restored to default in "+Config["SecondsChangeLootModeBack"].ToString()+" minutes");
					timer.Once(Convert.ToSingle(Config["SecondsChangeLootModeBack"]), () => 
					{
						hurt.BroadcastChat("<color=orange>[Loot Mode]</color> Loot Mode has ended! Loot mode has been returned to Default!");
						Singleton<GameManager>.Instance.ServerConfig.PlayerLootMode = EServerPlayerLootMode.BackpackOnlyWithInfamy;
					});
				}
			}
			
		}
		void cmd(string cmd) => ConsoleManager.Instance?.ExecuteCommand(cmd);
		void StartVote(PlayerSession session, string voteFor)
        {
            curVote.Add(session, voteFor);
            timer.Once(Convert.ToSingle(Config["VoteTimeSeconds"]), () => {
                curVote.Remove(session);
            });
        }
		void OnPlayerChat(PlayerSession session, string message)
		{
			if (curVote.ContainsKey(session))
			{
				if(message.ToLower() == Convert.ToString(Config["VoteFor"]).ToLower())
				{
					curVote.Remove(session);
					voteYes.Add(session, "Yes");
				}
				if(message.ToLower() == Convert.ToString(Config["VoteAgainst"]).ToLower())
				{
					curVote.Remove(session);
					voteNo.Add(session, "No");
				}
			}
		}
		private PlayerSession GetSession(String source) 
		{
			try
			{
				foreach (KeyValuePair<uLink.NetworkPlayer, PlayerSession> p in (Dictionary<uLink.NetworkPlayer, PlayerSession>)GameManager.Instance.GetSessions())
				{
					if(p.Value != null)
					{
						if(p.Value.IsLoaded) {
							if (source.ToLower() == p.Value.Name.ToLower())
								return p.Value;
						}
					}
				}
				foreach (KeyValuePair<uLink.NetworkPlayer, PlayerSession> pair in (Dictionary<uLink.NetworkPlayer, PlayerSession>)GameManager.Instance.GetSessions())
				{
					if(pair.Value != null)
					{
						if(pair.Value.IsLoaded)
						{
							if(source == pair.Value.SteamId.ToString())
								return pair.Value;
							if (pair.Value.Name.ToLower().Contains(source.ToLower()))
								return pair.Value;
						}
					}
				}
			}
			catch {
				Puts("An error occurred fetching session");
				return null; 
			}
			return null;
		}
	}
}