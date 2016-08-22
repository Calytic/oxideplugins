using System.Collections.Generic;
using System.Linq;
using System;

namespace Oxide.Plugins
{
	[Info("XP Manager", "LaserHydra", "1.1.0", ResourceId = 2026)]
	[Description("XP Manager")]
	class XPManager : RustPlugin
	{
        #region Classes

        class Range
        {
            public int Min;
            public int Max;

            public Range(int Min, int Max)
            {
                this.Min = Min;
                this.Max = Max;
            }
        }

        #endregion

        #region Variables

        Dictionary<string, object> PermissionLevelRanges;
        Dictionary<string, object> PermissionMultipliers;
		Dictionary<string, object> EarningMultipliers;

		bool CanAdminsGainXP;
		bool CanSleepersGainXP;
		bool InstantMaxLevel;

		float LevelLossOnDeath;

		int MinimalLevel;
		int MaximalLevel;

		#endregion

		#region Oxide Hooks

		void Loaded()
		{
			LoadConfig();
            LoadMessages();

			RegisterPerm("admin");

			foreach (var pm in PermissionMultipliers)
				RegisterPerm(pm.Key);

            foreach (var pm in PermissionLevelRanges)
                RegisterPerm(pm.Key);

            foreach (var player in BasePlayer.activePlayerList)
				OnPlayerInit(player);
		}

		void OnPlayerInit(BasePlayer player)
		{
//PrintWarning($"Player Joined: '{player.displayName}', Level: {GetLevel(player)}, Max Level: {GetMaxLevel(player.userID)}, Min Level: {GetMinLevel(player.userID)}, Is Above Max Level: {GetLevel(player) > GetMaxLevel(player.userID)}");

			if (InstantMaxLevel && GetLevel(player) < GetMaxLevel(player.userID))
				SetLevel(player, GetMaxLevel(player.userID));

			if (GetLevel(player) > GetMaxLevel(player.userID))
				SetLevel(player, GetMaxLevel(player.userID));

			if (GetLevel(player) < GetMinLevel(player.userID))
				SetLevel(player, GetMinLevel(player.userID));
		}

		void OnEntityDeath(BaseCombatEntity victim, HitInfo info)
		{
			if (victim?.ToPlayer()?.xp?.CurrentLevel != null && LevelLossOnDeath != 0)
				SetLevel(victim.ToPlayer(), Mathx.Clamp(Convert.ToInt32(victim.ToPlayer().xp.CurrentLevel - (victim.ToPlayer().xp.CurrentLevel / 100) * LevelLossOnDeath), MinimalLevel, MaximalLevel));
		}

		object OnXpEarn(ulong id, float amount, string source)
		{
			//PrintWarning($"Earning XP: {id}, Reached Max Level: {GetLevel(id) >= MaximalLevel}, IsAdmin: {IsAdmin(id)}, IsOnline: {IsOnline(id)}, Returning: {amount} * {GetMultiplier(source)} * {GetPermissionMultiplier(id)} = {amount * GetMultiplier(source) * GetPermissionMultiplier(id)}");

			if (GetLevel(id) >= GetMaxLevel(id))
				return 0f;

			if (!CanAdminsGainXP && IsAdmin(id))
				return 0f;

			if (!CanSleepersGainXP && !IsOnline(id))
				return 0f;

			return amount * GetMultiplier(source) * (source == "Cheat" ? 1 : GetPermissionMultiplier(id));
		}

		#endregion

		#region Loading

		new void LoadConfig()
		{
            PermissionLevelRanges = GetConfig<Dictionary<string, object>>("Permission Level Range", new Dictionary<string, object> { { "levelrange.vip", "5-100" }, { "levelrange.vip+", "10-100" } });
            PermissionMultipliers = GetConfig<Dictionary<string, object>>("Permission Multipliers", new Dictionary<string, object> { { "multiplier.vip", 1.5f }, { "multiplier.vip+", 2f } });
            EarningMultipliers = GetConfig<Dictionary<string, object>>("Earning Multipliers", StandardEarningMultipliers);

			CanAdminsGainXP = GetConfig<bool>("Can Admins Gain XP", true);
			CanSleepersGainXP = GetConfig<bool>("Can Sleepers Gain XP", true);
			InstantMaxLevel = GetConfig<bool>("Instant Max Level", false);

			LevelLossOnDeath = GetConfig<float>("Level Loss On Death (Percentage of current level)", 0f);
			MinimalLevel = GetConfig<int>("Minimal Level", 1);
			MaximalLevel = GetConfig<int>("Maximal Level", GetMaxLevel());

			foreach (var em in StandardEarningMultipliers)
				if (!EarningMultipliers.ContainsKey(em.Key))
					EarningMultipliers.Add(em.Key, em.Value);

			if (MaximalLevel > GetMaxLevel())
			{
				PrintWarning($"Maximal Level is set above the original max level of {GetMaxLevel()}! Automaticly lowering to {GetMaxLevel()} to prevent issues.");
				MaximalLevel = GetMaxLevel();
			}

			if (EarningMultipliers.Any((m) => Convert.ToSingle(m.Value) > 500))
			{
				PrintWarning("Earning Multipliers over 500 were detected! Automaticly lowering to 500 to prevent issues.");

				foreach (var em in EarningMultipliers.Where((m) => Convert.ToSingle(m.Value) > 500).ToList())
					EarningMultipliers[em.Key] = 500;
			}

			SaveConfig();
		}

		void LoadMessages()
		{
			lang.RegisterMessages(new Dictionary<string, string>
			{
				{"No Permission", "You don't have permission to use this command."},
				{"Info", $"XP Infomations about '{{player}}'{Environment.NewLine}Level: {{level}}"},
				{"Level Set", "{player}'s level was set to {level}."},
				{"Level Added", "{level} levels were given to {player}."},
				{"Level Reseted", "{player}'s level was reseted."}
			}, this);
		}

		protected override void LoadDefaultConfig() => PrintWarning("Generating new config file...");

		#endregion

		#region XP Related

		int GetMaxLevel() => Rust.Xp.Config.Levels.Length;

		float GetLevel(BasePlayer player) => player.xp.CurrentLevel;

		float GetLevel(ulong id) => BasePlayer.FindXpAgent(id)?.CurrentLevel ?? 1f;

		void AddLevel(BasePlayer player, int level) => player.xp.Add(Rust.Xp.Definitions.Cheat, Rust.Xp.Config.LevelToXp((int)player.xp.CurrentLevel + level) - player.xp.EarnedXp);

		void AddLevel(ulong id, int level)
		{
			var agent = BasePlayer.FindXpAgent(id);

			agent.Add(Rust.Xp.Definitions.Cheat, Rust.Xp.Config.LevelToXp((int) agent.CurrentLevel + level) - agent.EarnedXp);
		}

		void SetLevel(BasePlayer player, int level)
		{
			float spentXp = player.xp.SpentXp;

			player.xp.Reset();
			player.xp.Add(Rust.Xp.Definitions.Cheat, Rust.Xp.Config.LevelToXp(level));

			player.xp.SpendXp((int) spentXp, string.Empty);
		}

		void SetLevel(ulong id, int level)
		{
			var agent = BasePlayer.FindXpAgent(id);

			float spentXp = agent?.SpentXp ?? 0f;

			agent?.Reset();
			agent?.Add(Rust.Xp.Definitions.Cheat, Rust.Xp.Config.LevelToXp(level));

			agent?.SpendXp((int) spentXp, string.Empty);
		}

		float GetMultiplier(string source)
		{
			if (EarningMultipliers.ContainsKey(source))
				return Convert.ToSingle(EarningMultipliers[source]);

			return 1f;
		}

		float GetPermissionMultiplier(ulong id)
		{
			float multiplier = 1f;

			foreach (var m in PermissionMultipliers)
				if (HasPerm(id, m.Key) && Convert.ToSingle(m.Value) > multiplier)
					multiplier = Convert.ToSingle(m.Value);

			return multiplier;
		}

        int GetMinLevel(ulong id)
        {
            string perm = PermissionLevelRanges.Keys.ToList().Find((k) => HasPerm(id, k));

            if (perm != null)
                return LevelRangeFromPermission(perm).Min;

            return MinimalLevel;
        }

        int GetMaxLevel(ulong id)
        {
            string perm = PermissionLevelRanges.Keys.ToList().Find((k) => HasPerm(id, k));

            if (perm != null)
                return LevelRangeFromPermission(perm).Max;

            return MaximalLevel;
        }

        Range LevelRangeFromPermission(string perm)
        {
            if (!PermissionLevelRanges.ContainsKey(perm))
            {
                PrintError($"Tried to get Min/Max Level for non existant permission '{perm}'!");
                return new Range(MinimalLevel, MaximalLevel);
            }
            
            string str = PermissionLevelRanges[perm].ToString();
            string[] strVars = str.Split('-');
            int[] vars = (from s in strVars where CanBeParsedTo<int>(s) select Convert.ToInt32(s)).ToArray();

            if (vars.Length != 2)
            {
                PrintError($"Min/Max Level for permission '{perm}' is formatted incorrectly! It should be 'min-max' where min and max are numbers!");
                return new Range(MinimalLevel, MaximalLevel);
            }

            return new Range(vars[0], vars[1]);
        }

        Dictionary<string, object> StandardEarningMultipliers
		{
			get
			{
				Dictionary<string, object> dic = new Dictionary<string, object>();

				foreach (var def in Rust.Xp.Definitions.All.Where((d) => d.Name != "Cheat"))
					dic.Add(def.Name, 1f);

				return dic;
			}
		}

        #endregion

        #region Commands

        [ConsoleCommand("xp")]
        void XPCCmd(ConsoleSystem.Arg arg) => XPCmd((BasePlayer)arg.connection?.player ?? null, arg.cmd.name, arg.HasArgs() ? arg.Args : new string[0], true);

		[ChatCommand("xp")]
		void XPCmd(BasePlayer player, string cmd, string[] args, bool console = false)
        {
            string commandPrefix = console ? string.Empty : "/";

            if (player != null && !HasPerm(player.userID, "admin"))
			{
				Reply(player, GetMsg("No Permission"), console);
				return;
			}

			if (args.Length == 0)
			{
                Reply(player, $"{commandPrefix}xp <reset|setlvl|addlvl|info>", console);
				return;
			}

			int level;
			BasePlayer target;

			switch (args[0])
			{
				case "reset":
					if (args.Length != 2)
					{
                        Reply(player, $"Syntax: {commandPrefix}xp reset <player>", console);
						return;
					}

					target = GetPlayer(args[1], player);

					if (target == null)
						return;

					target.xp.Reset();
                    Reply(player, GetMsg("Level Reseted").Replace("{player}", target.displayName), console);
					break;

				case "setlvl":
					if (args.Length != 3)
					{
                        Reply(player, $"Syntax: {commandPrefix}xp setlvl <player> <level>", console);
						return;
					}

					target = GetPlayer(args[1], player);

					if (target == null)
						return;

					if (!int.TryParse(args[2], out level))
					{
                        Reply(player, $"'{args[0]}' is not a valid number!", console);
						return;
					}

					SetLevel(target, level);
                    Reply(player, GetMsg("Level Set").Replace("{player}", target.displayName).Replace("{level}", level.ToString()), console);
					break;

				case "addlvl":
					if (args.Length != 3)
					{
                        Reply(player, $"Syntax: {commandPrefix}xp addlvl <player> <level>", console);
						return;
					}

					target = GetPlayer(args[1], player);

					if (target == null)
						return;

					if (!int.TryParse(args[2], out level))
					{
                        Reply(player, $"'{args[0]}' is not a valid number!", console);
						return;
					}

					AddLevel(target, level);
                    Reply(player, GetMsg("Level Added").Replace("{player}", target.displayName).Replace("{level}", level.ToString()), console);
					break;

				case "info":
                    if (args.Length != 2)
                    {
                        Reply(player, $"Syntax: {commandPrefix}xp info <player>", console);
                        return;
                    }
                    else
					{
						target = GetPlayer(args[1], player);

						if (target == null)
							return;
					}

                    Reply(player, GetMsg("Info").Replace("{player}", target.displayName).Replace("{level}", ((int)target.xp.CurrentLevel).ToString()), console);
					break;

				default:

                    Reply(player, $"{commandPrefix}xp <reset|setlvl|addlvl|info>", console);

					break;
			}
		}

		#endregion

		#region Helpers

        bool CanBeParsedTo<T>(string str)
        {
            try
            {
                T parsed = (T) Convert.ChangeType(str, typeof(T));
                return true;
            }
            catch
            {
                return false;
            }
        }

		bool IsOnline(ulong id) => BasePlayer.FindByID(id)?.IsConnected() ?? false;

		bool IsAdmin(ulong id) => BasePlayer.FindByID(id)?.IsAdmin() ?? false;

		BasePlayer GetPlayer(string searchedPlayer, BasePlayer player)
		{
			foreach (BasePlayer current in BasePlayer.activePlayerList)
				if (current.displayName.ToLower() == searchedPlayer.ToLower())
					return current;

			List<BasePlayer> foundPlayers =
				(from current in BasePlayer.activePlayerList
				 where current.displayName.ToLower().Contains(searchedPlayer.ToLower())
				 select current).ToList();

			switch (foundPlayers.Count)
			{
				case 0:
					SendChatMessage(player, "The player can not be found.");
					break;

				case 1:
					return foundPlayers[0];

				default:
					List<string> playerNames = (from current in foundPlayers select current.displayName).ToList();
					string players = ListToString(playerNames, 0, ", ");
					SendChatMessage(player, "Multiple matching players found: \n" + players);
					break;
			}

			return null;
		}

		string ListToString<T>(List<T> list, int first = 0, string seperator = ", ") => string.Join(seperator, (from val in list select val.ToString()).Skip(first).ToArray());

		T GetConfig<T>(params object[] args)
		{
			List<string> stringArgs = (from arg in args select arg.ToString()).ToList();
			stringArgs.RemoveAt(args.Length - 1);

			if (Config.Get(stringArgs.ToArray()) == null)
			{
				PrintWarning($"Adding '{string.Join("/", stringArgs.ToArray())}' to configfile.");

				Config.Set(args);
			}

			return (T)Convert.ChangeType(Config.Get(stringArgs.ToArray()), typeof(T));
		}

		void LoadData<T>(ref T data, string filename = "?") => data = Core.Interface.Oxide.DataFileSystem.ReadObject<T>(filename == "?" ? this.Title : filename);

		void SaveData<T>(ref T data, string filename = "?") => Core.Interface.Oxide.DataFileSystem.WriteObject(filename == "?" ? this.Title : filename, data);

		string GetMsg(string key, object userID = null) => lang.GetMessage(key, this, userID == null ? null : userID.ToString());

		void RegisterPerm(params string[] permArray)
		{
			string perm = ListToString(permArray.ToList(), 0, ".");

			permission.RegisterPermission($"{PermissionPrefix}.{perm}", this);
		}

		bool HasPerm(object uid, params string[] permArray)
		{
			string perm = ListToString(permArray.ToList(), 0, ".");

			return permission.UserHasPermission(uid.ToString(), $"{PermissionPrefix}.{perm}");
		}

		string PermissionPrefix
		{
			get
			{
				return this.Title.Replace(" ", "").ToLower();
			}
		}

        void Reply(BasePlayer player, string message, bool console)
        {
            if (console && player == null)
                Puts(message);
            else if (console)
                player.ConsoleMessage(message);
            else
                SendChatMessage(player, message);
        }

		void BroadcastChat(string prefix, string msg = null) => rust.BroadcastChat(msg == null ? prefix : "<color=#C4FF00>" + prefix + "</color>: " + msg);

		void SendChatMessage(BasePlayer player, string prefix, string msg = null) => rust.SendChatMessage(player, msg == null ? prefix : "<color=#C4FF00>" + prefix + "</color>: " + msg);

		#endregion
	}
}