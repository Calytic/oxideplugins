using System;
using System.Collections.Generic;
using System.Linq;
using Oxide.Core;
using Oxide.Core.Plugins;
using UnityEngine;
using Newtonsoft.Json.Linq;

namespace Oxide.Plugins
{
    [Info("AutoPurge", "Fujikura/Norn", "1.3.1", ResourceId = 1566)]
    [Description("Remove entities if the owner becomes inactive.")]
    public class AutoPurge : RustPlugin
    {
		[PluginReference]
        Plugin Clans;
		
		[PluginReference]
		Plugin Friends;
		
		private bool Changed = false;
		StoredData playerConnections = new StoredData();
		Timer mainTimer = null;
		static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        static readonly double MaxUnixSeconds = (DateTime.MaxValue - UnixEpoch).TotalSeconds;
		private List<ulong> groupModerator = new List<ulong>();
		private List<ulong> groupOwner = new List<ulong>();
		private string logFile = "oxide/logs/AutoPurgeLog.txt";
		private bool friendsEnabled = false;
		private bool clansEnabled = false;
		
		#region Config
		
		private int timerJob;
		private bool timerEnabled;
		private int inactiveAfter;
		private int removeRecordAfterDays;
		private bool removeRecordAfterPurge;
		private bool killSleepers;
		private bool showMessages;
		private bool testMode;
		private bool purgeOnStart;
		private bool logPurgeToFile;
		private bool showMessagesAdminOnly;
		private bool excludeGroupOwner;
		private bool excludeGroupModerator;
		private string excludePermission;
		private bool useFriendsApi;
		private bool useClansIO;
		
		private object GetConfig(string menu, string datavalue, object defaultValue)
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
		
		void LoadVariables()
        {
			timerJob = Convert.ToInt32(GetConfig("Timing", "timerJob", 21600));
			timerEnabled = Convert.ToBoolean(GetConfig("Timing", "timerEnabled", true));
			inactiveAfter = Convert.ToInt32(GetConfig("Timing", "inactiveAfter", 172800));
			removeRecordAfterDays = Convert.ToInt32(GetConfig("Generic", "removeRecordAfterDays", 30));
			showMessages = Convert.ToBoolean(GetConfig("Messaging", "showMessages", true));
            testMode = Convert.ToBoolean(GetConfig("Generic", "testMode", false));
			purgeOnStart = Convert.ToBoolean(GetConfig("Generic", "purgeOnStart", false));
			logPurgeToFile = Convert.ToBoolean(GetConfig("Generic", "logPurgeToFile", true));
			killSleepers = Convert.ToBoolean(GetConfig("Generic", "killSleepers", false));
			useFriendsApi = Convert.ToBoolean(GetConfig("Generic", "useFriendsApi", false));
			useClansIO = Convert.ToBoolean(GetConfig("Generic", "useClansIO", true));
			showMessagesAdminOnly = Convert.ToBoolean(GetConfig("Messaging", "showMessagesAdminOnly", false));
			removeRecordAfterPurge = Convert.ToBoolean(GetConfig("Generic", "removeRecordAfterPurge", true));
			excludeGroupOwner = Convert.ToBoolean(GetConfig("Exclution", "excludeGroupOwner", true));
			excludeGroupModerator = Convert.ToBoolean(GetConfig("Exclution", "excludeGroupModerator", true));
			excludePermission = Convert.ToString(GetConfig("Exclution", "excludePermission", "autopurge.exclude"));
			
			if (!Changed) return;
            SaveConfig();
            Changed = false;
        }
		
        protected override void LoadDefaultConfig()
        {
            Config.Clear();
            LoadVariables();
        }
		
		#endregion Config
		
	   #region Localization

		void LoadDefaultMessages()
		{
			lang.RegisterMessages(new Dictionary<string, string>
			                      {
									{"RunBefore", "<color=yellow>INFO:</color> Beginning <color=red>purge</color>. (<color=yellow>Slight lag may occur, please do not spam the chat.</color>)"},
									{"RunComplete", "Purge <color=green>complete</color> (<color=yellow>{0}</color> entities removed from <color=yellow>{1}</color> inactive players)."},
			                      },this);
		}

		#endregion
		
		#region StoredData
		
		class StoredData
        {
            public Dictionary<ulong, PlayerInfo> PlayerInfo = new Dictionary<ulong, PlayerInfo>();
            public StoredData(){}
        }
        
		class PlayerInfo
        {
            public ulong UserID;
			public string DisplayName;
            public int LastTime;
			public string ClanTag;
            public PlayerInfo(){}
        }

		void SaveData()
        {
            Interface.Oxide.DataFileSystem.WriteObject(this.Title, playerConnections);
        }

		#endregion StoredData
		
		#region Connection
		
		private void InitPlayer(BasePlayer player)
        {
            if (player == null || !player.isConnected) return;
            PlayerInfo p = null;
            if (!playerConnections.PlayerInfo.TryGetValue(player.userID, out p))
            {
                var info = new PlayerInfo(); 
                info.UserID = player.userID;
				info.DisplayName = player.displayName;
                info.LastTime = UnixTimeStampUTC();
                if(Clans && Clans?.Call("GetClanOf", player) != null)
					info.ClanTag = (string)Clans?.Call("GetClanOf", player);
				playerConnections.PlayerInfo.Add(info.UserID, info);
                return;
            }
            else
            {
                p.LastTime = UnixTimeStampUTC();
				p.DisplayName = player.displayName;
				if(Clans && Clans?.Call("GetClanOf", player) != null)
					p.ClanTag = (string)Clans?.Call("GetClanOf", player);
            }
            return;
        }
		
		private void SaveConnectionData(BasePlayer player)
        {
            if (DataExists(player))
            {
                playerConnections.PlayerInfo[player.userID].LastTime = UnixTimeStampUTC();
            }
            else
            {
                InitPlayer(player);
                SaveConnectionData(player);
            }
        }
		
		#endregion Connection

		private Int32 UnixTimeStampUTC()
        {
            Int32 unixTimeStamp;
            DateTime currentTime = DateTime.Now;
            DateTime zuluTime = currentTime.ToUniversalTime();
            DateTime unixEpoch = new DateTime(1970, 1, 1);
            unixTimeStamp = (Int32)(zuluTime.Subtract(unixEpoch)).TotalSeconds;
            return unixTimeStamp;
        }

		private static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
        {
            return unixTimeStamp > MaxUnixSeconds
               ? UnixEpoch.AddMilliseconds(unixTimeStamp)
               : UnixEpoch.AddSeconds(unixTimeStamp);
        }

		private DateTime LastSeen(BasePlayer player)
        {
            DateTime date = System.DateTime.Now;
            if (DataExistsFromID(player.userID)) date = UnixTimeStampToDateTime(playerConnections.PlayerInfo[player.userID].LastTime);
            return date;
        }
		
		private bool DataExistsFromID(ulong steamid)
        {
            if (playerConnections.PlayerInfo.ContainsKey(steamid)) { return true; }
            return false;
        }
        
		private bool DataExists(BasePlayer player)
        {
            if (player == null || !player.isConnected) return false;
            if (playerConnections.PlayerInfo.ContainsKey(player.userID)) { return true; }
            return false;
        }

		private bool CheckActiveClanMember(string tag)
		{
			foreach( var pair in playerConnections.PlayerInfo)
				if(pair.Value.ClanTag == tag)
					if (UnixTimeStampUTC() - playerConnections.PlayerInfo[pair.Value.UserID].LastTime < inactiveAfter)
						return true;
			
			return false;
		}

		private bool CheckActiveFriends(ulong id)
		{
			foreach( var pair in playerConnections.PlayerInfo)
				if((bool)Friends?.CallHook("AreFriends", pair.Value.UserID, id))
					if (UnixTimeStampUTC() - playerConnections.PlayerInfo[pair.Value.UserID].LastTime < inactiveAfter)
						return true;
			return false;
		}
		
		private void ClanChanges(string tag)
		{
			JObject clan = new JObject();
			JArray members = new JArray();
			
			if(Clans?.Call("GetClan", tag) != null)
				clan = (JObject)Clans?.Call("GetClan", tag);
			else
				return;

			members = (JArray) clan["members"];
			if (members.Count == 0) return;
			for  (var i = 0; i < members.Count; ++i)
			{
				if (DataExistsFromID(Convert.ToUInt64(members[i])))
				{
					playerConnections.PlayerInfo[Convert.ToUInt64(members[i])].ClanTag = tag;
				}
				else
				{
					var player = rust.FindPlayerById(Convert.ToUInt64(members[i]));
					if (player != null)
						InitPlayer(player);
				}
			}
		}
		
		private void OnClanUpdate(string tag)
		{
			ClanChanges(tag);
		}
		
		private void OnClanCreate(string tag)
		{
			ClanChanges(tag);
		}

		private void OnClanDestroy(string tag)
		{
			foreach ( var pair in playerConnections.PlayerInfo)
				if (playerConnections.PlayerInfo[pair.Key].ClanTag == tag)
					playerConnections.PlayerInfo[pair.Key].ClanTag = null;
		}
		
		

		#region serverhooks
		
		private void OnPlayerInit(BasePlayer player)
        {
            InitPlayer(player);
        }
		
		private void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            SaveConnectionData(player);
        }
		
		private void OnServerSave()
        {
            SaveData();
        }

		private void OnServerShutdown()
        {
            SaveData();
        }
		
		
		private void Unload()
        {
            SaveData();
        }
        
		private void Loaded()
        {
            LoadVariables();
			LoadDefaultMessages();
		}
	
		private void OnServerInitialized()
        {
			if (!permission.PermissionExists(excludePermission)) permission.RegisterPermission(excludePermission, this);
			playerConnections = Interface.GetMod().DataFileSystem.ReadObject<StoredData>(this.Title);
			foreach (BasePlayer player in BasePlayer.activePlayerList)
				InitPlayer(player);
			StoredData cleanedConnections = new StoredData();			
			foreach( var pair in playerConnections.PlayerInfo)
				if(UnixTimeStampUTC()-pair.Value.LastTime < removeRecordAfterDays * 86400)
					cleanedConnections.PlayerInfo.Add(pair.Key, pair.Value);
			playerConnections = cleanedConnections;
			SaveData();
			cleanedConnections = null;
			if (excludeGroupOwner)
				foreach ( var user in ServerUsers.GetAll(ServerUsers.UserGroup.Owner).ToList())
					groupOwner.Add(user.steamid);
			if (excludeGroupModerator)
				foreach ( var user in ServerUsers.GetAll(ServerUsers.UserGroup.Moderator).ToList())
					groupModerator.Add(user.steamid);
			NextTick( () => {
				TimeSpan its = TimeSpan.FromSeconds(inactiveAfter);
				TimeSpan ts = TimeSpan.FromSeconds(timerJob);
				if (timerEnabled)
				{
					if (ts.Hours != 0)
					{
						Puts("Purge will be executed every: " + ts.Hours.ToString() + " hours, players become inactive after: "+its.Days.ToString()+" days.");
					}
					else
					{
						Puts("Purge will be executed every: " + ts.Minutes.ToString() + " minutes, players become inactive after: "+its.Minutes.ToString()+" minutes.");
					}
					mainTimer = timer.Repeat(timerJob, 0, () => {MainTimer();});
				}
				else
					Puts("Timer function disabled by config. Purge needs to be started by command 'autopurge.run'");

				if (Clans && useClansIO)
				{
					clansEnabled = true;
					Puts("Plugin 'Clans' found - Clan support activated");
				}
				if (!Clans && useClansIO)
					PrintWarning("Plugin 'Clans' not found - Clan support not active");
				if (Friends && useFriendsApi)
				{
					friendsEnabled = true;
					Puts("Plugin 'Friends' found - Friends support activated");
				}
				if (!Friends && useFriendsApi)
					PrintWarning("Plugin 'Friends' not found - Friends support not active");
				if (testMode) PrintWarning("Running in TestMode. Nothing will be purged");
				if (purgeOnStart)
					MainTimer(true);

			});
		}

		#endregion serverhooks

		[ConsoleCommand("autopurge.remove")]
        void ccmdRunRemove(ConsoleSystem.Arg arg)
        {
            if(arg.connection != null && arg.connection.authLevel < 2)
				return;
			if (arg.Args == null)
			{
				SendReply(arg, string.Format("Please specify a target steamid"));
				return;
			}
			ulong owner = 0;
			if(arg.Args.Length >= 1) ulong.TryParse(arg.Args[0],out owner);
			if (owner == 0) return;
			
			int count = 0;
            foreach(var entity in BaseNetworkable.serverEntities.All().Where(p => (p as BaseEntity).OwnerID == owner).ToList())
			{
				entity.Kill();
				count++;
			}
			SendReply(arg, $"Removed: {count} entities from ID: {owner}");
			if (logPurgeToFile && count > 0)
				ConVar.Server.Log(logFile,$"Manually removed: {count} entities from ID: {owner}");
		}

        [ConsoleCommand("autopurge.run")]
        void ccmdRunPurge(ConsoleSystem.Arg arg)
        {
            if(arg.connection != null && arg.connection.authLevel < 2)
				return;
			MainTimer();
        }

        void MainTimer(bool freshStart = false)
        {
			if (showMessages && !freshStart)
			{
				if(showMessagesAdminOnly)
				{
					foreach(var admin in BasePlayer.activePlayerList.Where(p => p.IsAdmin()).ToList())
					SendReply(admin, string.Format(lang.GetMessage("RunBefore", this, admin.UserIDString)));
				}
				else
					PrintToChat(string.Format(lang.GetMessage("RunBefore", this)));
			}
			
			int count = 0;
			List<ulong> UNIQUE_HITS = new List<ulong>();
			List<ulong> EXCLUDE_BY_CLAN = new List<ulong>();
			List<ulong> CLANCHECK_NEGATIVE = new List<ulong>();
			List<ulong> EXCLUDE_BY_FRIEND = new List<ulong>();
			List<ulong> FRIENDCHECK_NEGATIVE = new List<ulong>();			
			List<ulong> ONLINE_PLAYERS = new List<ulong>();
			List<ulong> EXCLUDE_BY_PERM = new List<ulong>();	
			foreach (BasePlayer onliner in BasePlayer.activePlayerList)
				ONLINE_PLAYERS.Add(onliner.userID);
			
			var entities = BaseNetworkable.serverEntities.All().Where(p => (p as BaseEntity).OwnerID != 0).ToList();
			//var entities = BaseNetworkable.serverEntities.All().ToList();
			Puts("Included entity count on this run: "+entities.Count);
			foreach (var entity in entities)
            {
				if (entity == null) continue;
				ulong owner = 0;
				try { owner = (entity as BaseEntity).OwnerID; }
				catch { continue; }
				if (DataExistsFromID(owner) && !ONLINE_PLAYERS.Contains(owner) && !EXCLUDE_BY_CLAN.Contains(owner) && !EXCLUDE_BY_FRIEND.Contains(owner) && !EXCLUDE_BY_PERM.Contains(owner) && !groupOwner.Contains(owner) && !groupModerator.Contains(owner))
                {
                    if (UnixTimeStampUTC() - playerConnections.PlayerInfo[owner].LastTime >= inactiveAfter)
					{ 
						// Clancheck begin
						if(clansEnabled)
							if (playerConnections.PlayerInfo[owner].ClanTag != null && !CLANCHECK_NEGATIVE.Contains(owner))
								if (CheckActiveClanMember(playerConnections.PlayerInfo[owner].ClanTag))
								{
									EXCLUDE_BY_CLAN.Add(owner);
									//Debug.Log("Exclude by Clan");
									continue;
								}
								else
									CLANCHECK_NEGATIVE.Add(owner);
						// Clancheck end
						// FriendCheck begin
						if(friendsEnabled)
							if (!FRIENDCHECK_NEGATIVE.Contains(owner))
								if(CheckActiveFriends(owner))
								{
									EXCLUDE_BY_FRIEND.Add(owner);
									//Debug.Log("Exclude by Friend");
									continue;
								}
								else
									FRIENDCHECK_NEGATIVE.Add(owner);
						// FriendCheck end
						// PermCheck begin
							if (permission.UserHasPermission(owner.ToString(), excludePermission))
							{
								EXCLUDE_BY_PERM.Add(owner);
								continue;
							}
						// PermCheck end
						if (!testMode)
						{
							entity.Kill();
						}
						count++;
						if (!UNIQUE_HITS.Contains(owner))
						{
							UNIQUE_HITS.Add(owner);
						}
					}
                }
            }
			if (showMessages && !freshStart)
			{
				if (showMessagesAdminOnly)
				{
					foreach(var admin in BasePlayer.activePlayerList.Where(p => p.IsAdmin()).ToList())
					SendReply(admin, string.Format(lang.GetMessage("RunComplete", this, admin.UserIDString), count, UNIQUE_HITS.Count));
				}
				else
					PrintToChat(string.Format(lang.GetMessage("RunComplete", this), count, UNIQUE_HITS.Count));
			}
            if (count != 0)
			{
				string playerIds = "";
				string ifTest = "";
				if(testMode)
				{
					ifTest = "TestMode >> ";
				}
				foreach (var id in UNIQUE_HITS)
				{
					playerIds += playerConnections.PlayerInfo[id].DisplayName+"("+id.ToString()+") ";
					if (removeRecordAfterPurge && !testMode) playerConnections.PlayerInfo.Remove(id);
					if (killSleepers && !testMode)
					{
						foreach (BasePlayer sleeper in BasePlayer.sleepingPlayerList.ToList())
						{
							if (UNIQUE_HITS.Contains(sleeper.userID))
								sleeper.KillMessage();
						}
					}
				}
				Puts(ifTest+ "Removed: " + count.ToString() + " entities from: " + UNIQUE_HITS.Count.ToString() + " inactive players");
				Puts(ifTest+ "Affected IDs: " + playerIds);
				if (logPurgeToFile)
				{
					ConVar.Server.Log(logFile, ifTest+ "Removed: " + count.ToString() + " entities from: " + UNIQUE_HITS.Count.ToString() + " inactive players");
					ConVar.Server.Log(logFile, ifTest+ "Affected IDs: " + playerIds);
				}
			}
			else
			{
				Puts("Nothing to remove... up to date.");
				if (logPurgeToFile)
					ConVar.Server.Log(logFile,"Nothing to remove... up to date.");
			}
        }

	}
}