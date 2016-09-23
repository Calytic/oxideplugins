using System;
using System.Collections.Generic;
using System.Linq;
using Oxide.Core;
using Oxide.Core.Plugins;
using UnityEngine;
using Newtonsoft.Json.Linq;

namespace Oxide.Plugins
{
    [Info("AutoPurge", "Fujikura/Norn", "1.4.4", ResourceId = 1566)]
    [Description("Remove entities if the owner becomes inactive.")]
    public class AutoPurge : RustPlugin
    {
		[PluginReference]
        Plugin Clans;
		
		[PluginReference]
		Plugin Friends;
		
		private bool Changed = false;
		StoredData playerConnections = new StoredData();
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
			public string DisplayName;
            public int LastTime;
			public string LastDateTime;
            public PlayerInfo(){}
        }

		void SaveData()
        {
            Interface.Oxide.DataFileSystem.WriteObject(this.Title, playerConnections);
        }

		#endregion StoredData
		
		#region Connection
		
		private void UpdatePlayer(BasePlayer player)
        {
            if (player == null) return;
            PlayerInfo p = null;
            if (!playerConnections.PlayerInfo.TryGetValue(player.userID, out p))
            {
                var info = new PlayerInfo(); 
				info.DisplayName = player.displayName;
                info.LastTime = UnixTimeStampUTC();
				info.LastDateTime = DateTime.Now.ToString();
				playerConnections.PlayerInfo.Add(player.userID, info);
                return;
            }
            else
            {
                p.LastTime = UnixTimeStampUTC();
				p.LastDateTime = DateTime.Now.ToString();
				p.DisplayName = player.displayName;
            }
            return;
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
			JObject clan = (JObject)Clans.Call("GetClan", tag);
			if (clan == null) return false;
			foreach( var member in clan["members"])
				if (DataExistsFromID(Convert.ToUInt64(member)))
					if (UnixTimeStampUTC() - playerConnections.PlayerInfo[Convert.ToUInt64(member)].LastTime < inactiveAfter)
						return true;
			return false;
		}

		private bool CheckActiveFriends(ulong id)
		{
			foreach( var pair in playerConnections.PlayerInfo)
				if((bool)Friends?.CallHook("AreFriends", pair.Key, id))
					if (UnixTimeStampUTC() - playerConnections.PlayerInfo[pair.Key].LastTime < inactiveAfter)
						return true;
			return false;
		}
		
		#region serverhooks
		
		private void OnPlayerInit(BasePlayer player)
        {
            UpdatePlayer(player);
        }
		
		private void OnPlayerDisconnected(BasePlayer player)
        {
            UpdatePlayer(player);
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
				UpdatePlayer(player);
			StoredData cleanedConnections = new StoredData();			
			foreach( var pair in playerConnections.PlayerInfo)
			{
				if(UnixTimeStampUTC()-pair.Value.LastTime < removeRecordAfterDays * 86400)
					cleanedConnections.PlayerInfo.Add(pair.Key, pair.Value);
				//cleanedConnections.PlayerInfo[pair.Key].LastDateTime = UnixTimeStampToDateTime(pair.Value.LastTime).ToString();
			}
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
					Puts($"Purge will be executed every: {ts.TotalHours.ToString("0")} hours ({ts.Days.ToString("0")}D | {ts.Hours.ToString("0")}H | {ts.Minutes.ToString("0")}M | {ts.Seconds.ToString("0")}S)");
					Puts($"Players become inactive after: {its.TotalDays.ToString("0")} days ({its.Days.ToString("0")}D | {its.Hours.ToString("0")}H | {its.Minutes.ToString("0")}M | {its.Seconds.ToString("0")}S)");
					timer.Every(timerJob, () => MainTimer(false));
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
            foreach(var entity in BaseNetworkable.serverEntities.Where(p => (p as BaseEntity).OwnerID == owner).ToList())
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
			if (freshStart)
			{
				if (useClansIO && !clansEnabled) return;
				if (useFriendsApi && !friendsEnabled) return;
			}
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
			{
				ONLINE_PLAYERS.Add(onliner.userID);
				UpdatePlayer(onliner);
			}
			
			var entities = BaseNetworkable.serverEntities.Where(p => (p as BaseEntity).OwnerID != 0).ToList();
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
						if (UNIQUE_HITS.Contains(owner))
						{
							if (!testMode)
								entity.Kill();
							count++;
							continue;
						}
							
						// PermCheck begin
						if (permission.UserHasPermission(owner.ToString(), excludePermission))
						{
							EXCLUDE_BY_PERM.Add(owner);
							continue;
						}
						// PermCheck end
						// Clancheck begin
						if(clansEnabled)
							if (Clans.Call("GetClanOf", owner) != null && !CLANCHECK_NEGATIVE.Contains(owner))
								if (CheckActiveClanMember((string)Clans.Call("GetClanOf", owner)))
								{
									EXCLUDE_BY_CLAN.Add(owner);
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
									continue;
								}
								else
									FRIENDCHECK_NEGATIVE.Add(owner);
						// FriendCheck end
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