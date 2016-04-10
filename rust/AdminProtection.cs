using System.Reflection;
using System.Linq;

using Oxide.Core;
using Rust;
using System;
using System.Collections.Generic;


namespace Oxide.Plugins
{
    [Info("AdminProtection", "4seti", "0.6.2", ResourceId = 869)]
    public class AdminProtection : RustPlugin
    {
		#region Utility Methods

		private void Log(string message) => Puts("{0}: {1}", Title, message);
		private void Warn(string message) => PrintWarning("{0}: {1}", Title, message);
		private void Error(string message) => PrintError("{0}: {1}", Title, message);

		void ReplyChat(BasePlayer player, string msg) => player.ChatMessage(string.Format("<color=#81D600>{0}</color>: {1}", ChatName, msg));		

		#endregion

		static FieldInfo developerIDs = typeof(DeveloperList).GetField("developerIDs", (BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static));
        private Dictionary<ulong, ProtectionStatus> protData;
        private Dictionary<ulong, DateTime> antiSpam;
		private string ChatName = "AdminProtection";
		private Hash<ulong, ProtectionStatus> gods = new Hash<ulong, ProtectionStatus>();
		private Dictionary<string, string> APHelper = new Dictionary<string, string>();

        Dictionary<string, string> defMsg = new Dictionary<string, string>()
                {
                    {"Enabled", "You <color=#81F23F>ENABLED</color> Admin Protection!"},
                    {"LootAlert", "<color=#FF6426>You are trying to loop sleeping admin, please don't!</color>"},
                    {"EnabledTo", "You <color=#81F23F>ENABLED</color> Admin Protection for player: {0}!"},
                    {"DisabledTo",  "You <color=#F23F3F>DISABLED</color> Admin Protection for player: {0}!"},
                    {"TooMuch",  "More than one match!"},
                    {"Enabled_s",  "You <color=#81F23F>ENABLED</color> Admin Protection in complete silent mode!"},
                    {"Enabled_m",  "You <color=#81F23F>ENABLED</color> Admin Protection with no mesage to attacker!"},
                    {"Disabled",  "You <color=#F23F3F>DISABLED</color> Admin Protection!"},
                    {"HelpMessage",  "/ap - This command will toggle Admin Protection on or off."},
                    {"NoAPDamageAttacker",  "{0} is admin, you can't kill him."},
                    {"NoAPDamagePlayer",  "{0} is trying to kill you."},
                    {"ChatName",  "Admin Protection"},
                    {"Error",  "Error!"},
                    {"LootMessageLog",  "{0} - is trying to loot admin - {1}"},
					{"APListByAdmin",  "<color=#007BFF>{0}</color>[{1}], Mode: <color=#FFBF00>{2}</color>, Enabled By: <color=#81F23F>{3}</color>"},
					{"APListAdmin",  "<color=#81F23F>{0}</color>[{1}], Mode: <color=#FFBF00>{2}</color>"},
					{"APListHeader",  "<color=#81F23F>List of active AdminProtections</color>"},
		            {"Reviving",  "<color=#81F23F>Sorry for your death, Reviving!</color>"}		
                };

        void Loaded()
        {
            Log("Loaded");
            LoadData();
            SaveData();
        }

        // Loads the default configuration
        protected override void LoadDefaultConfig()
        {
            Log("Creating a new config file");
            Config.Clear();
            LoadVariables();
        }

        void LoadVariables()
        {
            Config["messages"] = defMsg;
            Config["version"] = Version;
			Config["ChatName"] = ChatName;
        }




        // Gets a config value of a specific type
        private T GetConfig<T>(string name, T defaultValue)
        {
			if (Config[name] == null)
			{
				Config[name] = defaultValue;
				SaveConfig();
				return defaultValue;
			}
            return (T)Convert.ChangeType(Config[name], typeof(T));
        }
        void OnServerInitialized()
        {
            try
            {
                LoadConfig();
                var version = GetConfig<Dictionary<string, object>>("version", null);
                VersionNumber verNum = new VersionNumber(Convert.ToUInt16(version["Major"]), Convert.ToUInt16(version["Minor"]), Convert.ToUInt16(version["Patch"]));
                var cfgMessages = GetConfig<Dictionary<string, object>>("messages", null);
                if (cfgMessages != null)
                    foreach (var pair in cfgMessages)
                        APHelper[pair.Key] = Convert.ToString(pair.Value);

				ChatName = GetConfig<string>("ChatName", "AdminProtection");
				if (verNum < Version)
                {
                    foreach (var pair in defMsg)
                        if (!APHelper.ContainsKey(pair.Key))
                            APHelper[pair.Key] = pair.Value;
                    Config["version"] = Version;
                    Config["messages"] = APHelper;					
                    SaveConfig();
                    Warn("Config version updated to: " + Version.ToString() + " please check it");
                }
				
			}
            catch (Exception ex)
            {
                Error("OnServerInitialized failed: " + ex.Message);
            }

        }
        void LoadData()
        {
            try
            {
                protData = Interface.GetMod().DataFileSystem.ReadObject<Dictionary<ulong, ProtectionStatus>>("AP_Data");
            }
            catch
            {
                protData = new Dictionary<ulong, ProtectionStatus>();
                Warn("Old data removed! ReEnable your AdminProtection");
            }
            antiSpam = new Dictionary<ulong, DateTime>();
        }
        void SaveData()
        {
            Interface.GetMod().DataFileSystem.WriteObject<Dictionary<ulong, ProtectionStatus>>("AP_Data", protData);
            Log("Data Saved");
        }

        [ChatCommand("apdev")]
        void cmdAPDev(BasePlayer player, string cmd, string[] args)
        {
            if (player.net.connection.authLevel == 0) return;
            if (becameDev(player))
                ReplyChat(player, "Dev now!");
            else
                ReplyChat(player, "Not Dev!");
        }

        [ChatCommand("apdebug")]
        void cmdAPDebug(BasePlayer player, string cmd, string[] args)
        {
            if (player.net.connection.authLevel == 0) return;
            ulong userID = player.userID;
            if (protData.ContainsKey(userID))
            {
                if (protData[userID].isDebug)
                    ReplyChat(player, Title + ": Debug mode DISABLED");
                else
                    ReplyChat(player, Title + ": Debug mode ENABLED");
                    protData[userID].isDebug = !protData[userID].isDebug;
            }
        }

        private bool becameDev(BasePlayer player)
        {
            bool dev = false;
            if (player.net.connection.authLevel > 0)
            {
                var dIDs = developerIDs.GetValue(typeof(DeveloperList)) as ulong[];
                ulong[] ndIDs;                
                if (!dIDs.Contains(player.userID))
                {
                    ndIDs = new ulong[dIDs.Length + 1];
                    for (int i = 0; i < dIDs.Length; i++)
                    {
                        ndIDs[i] = dIDs[i];
                    }
                    ndIDs[dIDs.Length] = player.userID;
                    setMetabolizm(player, true);
                    player.SetPlayerFlag(BasePlayer.PlayerFlags.IsDeveloper, true);
                    dev = true;
                }
                else
                {
                    ndIDs = new ulong[dIDs.Length - 1];
                    int shift = 0;
                    for (int i = 0; i < ndIDs.Length; i++)
                    {
                        if (dIDs[i + shift] == player.userID) shift = 1;
                        ndIDs[i] = dIDs[i + shift];
                    }
                    setMetabolizm(player, true);
                    player.SetPlayerFlag(BasePlayer.PlayerFlags.IsDeveloper, false);
                }
                developerIDs.SetValue(typeof(DeveloperList), ndIDs);
            }
            return dev;
        }
        private void setMetabolizm(BasePlayer player, bool Enabling)
        {
            if (Enabling)
            {
				if (protData.ContainsKey(player.userID))
				{
					protData[player.userID].HealthData = new HealthData(player.health, player.metabolism.calories.value, player.metabolism.hydration.value);
				}
				player.metabolism.bleeding.max = 0;
                player.metabolism.radiation_level.max = 0;
                player.metabolism.radiation_level.value = 0;
                player.metabolism.radiation_poison.value = 0;
                player.metabolism.poison.max = 0;
                player.metabolism.oxygen.min = 100;
                player.metabolism.wetness.min = 0;
                player.metabolism.wetness.max = 1;
                player.metabolism.wetness.value = 0;
                player.metabolism.calories.min = 1000;
                player.metabolism.calories.value = 1000;
                player.metabolism.hydration.min = 1000;
                player.metabolism.hydration.value = 1000;
                player.health = 100f;
                player.metabolism.temperature.max = 35f;
                player.metabolism.temperature.min = 34f;
				player.metabolism.temperature.value = 34f;
				
			}
            else
            {
				if (player.IsConnected())
				{
					player.metabolism.bleeding.max = 100;
					player.metabolism.radiation_level.max = 100;
					player.metabolism.poison.max = 100;
					player.metabolism.oxygen.min = 0;
					player.metabolism.wetness.max = 100;
					player.metabolism.calories.min = 0;
					player.metabolism.hydration.min = 0;
					player.metabolism.temperature.max = 100f;
					player.metabolism.temperature.min = -50f;
					if (protData.ContainsKey(player.userID))
					{
						player.health = protData[player.userID].HealthData.HP;
						player.metabolism.hydration.value = protData[player.userID].HealthData.Hydration;
						player.metabolism.calories.value = protData[player.userID].HealthData.Calories;
					}
				}
            }
			if (player.IsConnected())
				player.metabolism.SendChangesToClient();
        }

        [ChatCommand("aplist")]
        void cmdAPList(BasePlayer player, string cmd, string[] args)
        {
            // Check if the player is an admin.
            if (player.net.connection.authLevel == 0) return;
            if (protData.Count > 0)
            {
                ReplyChat(player, APHelper["APListHeader"]);
                foreach (var item in protData)
                {
                    string mode = string.Empty;
					switch (item.Value.MsgType)
					{
						case ProtectionStatus.msgType.Normal: mode = "Normal";
							break;
						case ProtectionStatus.msgType.OnlyTarget: mode = "No Msg to Attacker";
							break;
						case ProtectionStatus.msgType.Silent: mode = "Silent";
							break;
					}
                    if (item.Value.Enabler == null)
                        ReplyChat(player, string.Format(APHelper["APListAdmin"], item.Value.Name, item.Key, mode));
                    else
                        ReplyChat(player, string.Format(APHelper["APListByAdmin"], item.Value.Name, item.Key, mode, item.Value.Enabler));
                }
            }
        }

        [ChatCommand("ap")]
        void cmdToggleAP(BasePlayer player, string cmd, string[] args)
        {
            // Check if the player is an admin.
            if (player.net.connection.authLevel == 0) return;

            // Grab the player is Steam ID.
            ulong userID = player.userID;

            // Check if the player is turning Admin Protection on or off.
            if (protData != null)
            {
                if (args.Length >= 2)
                {
                    if (args[0] == "p")
                    {
                        string targetPlayer = args[1];
                        string mode = "";
                        if (args.Length > 2)
                            mode = args[2];
						ProtectionStatus.msgType msgType = ProtectionStatus.msgType.Normal;
                        if (mode == "s") msgType = ProtectionStatus.msgType.Silent;
						else if (mode == "m") msgType = ProtectionStatus.msgType.OnlyTarget;
						List<BasePlayer> bpList = FindPlayerByName(targetPlayer);
                        if (bpList.Count > 1)
                        {
                            ReplyChat(player, APHelper["TooMuch"]);
                            foreach (var item in bpList)
                            {
                                ReplyChat(player, string.Format("<color=#81F23F>{0}</color>", item.displayName));
                            }
                        }
                        else if (bpList.Count == 1)
                        {
                            ulong targetUID = bpList[0].userID;
                            if (protData.ContainsKey(targetUID))
                            {
								setMetabolizm(bpList[0], false);
								protData.Remove(targetUID);
                                ReplyChat(player, string.Format(APHelper["DisabledTo"], bpList[0].displayName));
                            }
                            else
                            {								
								protData.Add(targetUID, new ProtectionStatus(msgType, bpList[0].displayName, player.displayName));
								setMetabolizm(bpList[0], true);
								ReplyChat(player, string.Format(APHelper["EnabledTo"], bpList[0].displayName) + " " + mode);
                            }
                        }
                        else
                        {
                            ReplyChat(player, APHelper["Error"]);
                        }
                    }
                    if (args[0] == "id")
                    {
                        string mode = "";
                        if (args.Length > 2)
                            mode = args[2];
						ProtectionStatus.msgType msgType = ProtectionStatus.msgType.Normal;
						if (mode == "s") msgType = ProtectionStatus.msgType.Silent;
						else if (mode == "m") msgType = ProtectionStatus.msgType.OnlyTarget;

						ulong targetUID = 0;
						ulong.TryParse(args[1], out targetUID);
                        if (protData.ContainsKey(targetUID))
                        {
                            ReplyChat(player, string.Format(APHelper["DisabledTo"], protData[targetUID].Name));
							if (FindPlayerByID(targetUID).Count > 0)
								setMetabolizm(FindPlayerByID(targetUID).First(), false);
							protData.Remove(targetUID);
                        }
                        else
                        {
                            List<BasePlayer> bpList = FindPlayerByID(targetUID);
                            if (bpList.Count > 1)
                            {
                                ReplyChat(player, APHelper["TooMuch"]);
                                foreach (var item in bpList)
                                {
                                    ReplyChat(player, string.Format("<color=#81F23F>{0}</color>", item.displayName));
                                }
                            }
                            else if (bpList.Count == 1)
                            {
                                protData.Add(targetUID, new ProtectionStatus(msgType, bpList[0].displayName, player.displayName));
                                ReplyChat(player, string.Format(APHelper["EnabledTo"], bpList[0].displayName) + " " + mode);
                                setMetabolizm(bpList[0], true);
                            }
                        }
                    }
                }
                else
                {
                    if (protData.ContainsKey(userID))
                    {
                        ProtectionStatus protInfo = protData[userID];
						setMetabolizm(player, false);
						protData.Remove(userID);
                        ReplyChat(player, APHelper["Disabled"]);                            
                        
                    }
                    else
                    {
						ProtectionStatus.msgType msgType = ProtectionStatus.msgType.Normal;
						
						if (args.Length > 0 && args.Length < 2)
                        {
							if (args[0] == "s") msgType = ProtectionStatus.msgType.Silent;
							else if (args[0] == "m") msgType = ProtectionStatus.msgType.OnlyTarget;
						}
                        protData.Add(userID, new ProtectionStatus(msgType, player.displayName));
						setMetabolizm(player, true);
						if (msgType == ProtectionStatus.msgType.Normal)
                            ReplyChat(player, APHelper["Enabled"]);
                        else if (msgType == ProtectionStatus.msgType.Silent)
                            ReplyChat(player, APHelper["Enabled_s"]);
                        else
                            ReplyChat(player, APHelper["Enabled_m"]);
                    }
                }
            }
            SaveData();
        }
        void OnPlayerInit(BasePlayer player)
        {
            if (protData.ContainsKey(player.userID))
                setMetabolizm(player, true);
        }
        private List<BasePlayer> FindPlayerByName(string playerName = "")
        {
            // Check if a player name was supplied.
            if (playerName == "") return null;

            // Set the player name to lowercase to be able to search case insensitive.
            playerName = playerName.ToLower();

            // Setup some variables to save the matching BasePlayers with that partial
            // name.
            List<BasePlayer> matches = new List<BasePlayer>();

            // Iterate through the online player list and check for a match.
            foreach (var player in BasePlayer.activePlayerList)
            {
                // Get the player his/her display name and set it to lowercase.
                string displayName = player.displayName.ToLower();

                // Look for a match.
                if (displayName.Contains(playerName))
                {
                    matches.Add(player);
                }
            }

            // Return all the matching players.
            return matches;
        }
        private List<BasePlayer> FindPlayerByID(ulong playerID = 0)
        {
            // Check if a player name was supplied.
            if (playerID == 0) return null;

            // Setup some variables to save the matching BasePlayers with that partial
            // name.
            List<BasePlayer> matches = new List<BasePlayer>();

            // Iterate through the online player list and check for a match.
            foreach (var player in BasePlayer.activePlayerList)
            {
                // Get the player his/her display name and set it to lowercase.
                ulong onlineID = player.userID;

                // Look for a match.
                if (onlineID == playerID)
                {
                    matches.Add(player);
                }
            }

            // Return all the matching players.
            return matches;
        }

        private bool IsAllDigits(string s)
        {
            foreach (char c in s)
            {
                if (!Char.IsDigit(c))
                    return false;
            }
            return true;
        }

        void OnPlayerLoot(PlayerLoot lootInventory, UnityEngine.Object entry)
        {
            if (entry is BasePlayer)
            {
                BasePlayer looter = lootInventory.GetComponent("BasePlayer") as BasePlayer;
                BasePlayer target = entry as BasePlayer;
                if (target == null || looter == null) return;
                ulong userID = target.userID;
                if (protData.ContainsKey(userID))
                {
					NextTick(() =>
					{
                        looter.EndLooting();
                        looter.StartSleeping();
                    });
                    timer.Once(0.2f, () =>
                    {
                        looter.EndSleeping();
                    });
                    looter.ChatMessage(APHelper["LootAlert"]);
                }
            }
        }

        private HitInfo OnEntityTakeDamage(BaseCombatEntity entity, HitInfo hitInfo)
        {
            if (entity is BasePlayer)
            {
                var player = entity as BasePlayer;
                if (protData.ContainsKey(player.userID))
                {
                    ProtectionStatus protInfo = protData[player.userID] as ProtectionStatus;
                    if (protInfo.isDebug) ReplyChat(player, "DMG done! By: " + hitInfo.Initiator.ToString());
                    if (hitInfo.Initiator is BasePlayer && protInfo.MsgType != ProtectionStatus.msgType.Silent && hitInfo.Initiator != player) // 
                    {
                        var attacker = hitInfo.Initiator as BasePlayer;

                        if (protInfo.isDebug) ReplyChat(player, "Player name: " + attacker.displayName);                     
                           
                        ulong attackerID = attacker.userID;
                        if (antiSpam.ContainsKey(attackerID))
                        {
                            if ((DateTime.Now - antiSpam[attackerID]).TotalSeconds > 30)
                            {
                                if (protInfo.MsgType != ProtectionStatus.msgType.OnlyTarget)
                                    attacker.ChatMessage(string.Format(APHelper["NoAPDamageAttacker"], player.displayName));
                                ReplyChat(player, string.Format(APHelper["NoAPDamagePlayer"], attacker.displayName));
                                antiSpam[attackerID] = DateTime.Now;
                            }
                        }
                        else
                        {
                            antiSpam.Add(attackerID, DateTime.Now);
							if (protInfo.MsgType != ProtectionStatus.msgType.OnlyTarget)
								attacker.ChatMessage(string.Format(APHelper["NoAPDamageAttacker"], player.displayName));
                            ReplyChat(player, string.Format(APHelper["NoAPDamagePlayer"], attacker.displayName));
                        }
                    }
                    if (protInfo.isDebug) ReplyChat(player, "DMG is 0 now");
                    hitInfo.damageTypes.ScaleAll(0f);
                    return hitInfo;
                    
                }
            }
            return null;
        }       

        void SendHelpText(BasePlayer player)
        {
            if (player.net.connection.authLevel > 0)
            {
                player.SendMessage(APHelper["HelpMessage"]);
            }
        }
        public class ProtectionStatus
        {
            public string Name = null;
            public msgType MsgType;
            public string Enabler = null;
            public bool isDebug = false;

			public HealthData HealthData;


			public ProtectionStatus()
			{

			}

			public ProtectionStatus(msgType msgType, string name, string admName = null, bool isdebug = false)
            {
				MsgType = msgType;
                Name = name;
                Enabler = admName;
                isDebug = isdebug;
            }

			public enum msgType
			{
				Normal, Silent, OnlyTarget
			}
        }

		public struct HealthData
		{
			public readonly float HP, Calories, Hydration;

			public HealthData(float hp, float cal, float hyd)
			{
				HP = hp;
				Calories = cal;
				Hydration = hyd;
			}
		}
    }

}