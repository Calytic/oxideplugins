using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Rust;
using Oxide.Core;
using Oxide.Core.Plugins;
using Newtonsoft.Json;
using Oxide.Core.Configuration;
using System.Linq;

namespace Oxide.Plugins
{
    [Info("Exodus", "Norn", "1.1.3")]
    [Description("Exodus Rust Gamemode.")]
    class Exodus : RustPlugin
    {
        [PluginReference]
        Plugin PopupNotifications;
        [PluginReference]
        Plugin ConnectionDB;
        [PluginReference]
        Plugin MagicTeleportation;
        private DynamicConfigFile DataFile;
        public static string file = "Exodus";
        public static Int32 UnixTimeStampUTC()
        {
            Int32 unixTimeStamp;
            DateTime currentTime = DateTime.Now;
            DateTime zuluTime = currentTime.ToUniversalTime();
            DateTime unixEpoch = new DateTime(1970, 1, 1);
            unixTimeStamp = (Int32)(zuluTime.Subtract(unixEpoch)).TotalSeconds;
            return unixTimeStamp;
        }
        #region static_variables
        StaticVariables StaticVars = null;
        public class StaticVariables{public Hash<ulong, Dictionary<string, object>> UserVariables = new Hash<ulong, Dictionary<string, object>>();}
        public List<string> StaticUserVar_LIST = new List<string>()
            {
                {"tCurrentName"},
                {"uUserID"},
                {"uHasWoken"},
                {"iMoneyQueue" },
                {"bResourceQMSG" },
                {"iTimeConnected" },
            };
        #endregion
        #region staticmessages
        public Dictionary<string, StaticText> StaticMessages = new Dictionary<string, StaticText>();
        public class StaticText
        {
            public string text;
            public string color = "green";
            public string cmd_association;
            public bool use_cmd_assocation;
            public StaticText()
            {
            }
        }
        #endregion
        public class StoredData
        {
            public Dictionary<ulong, User> Users = new Dictionary<ulong, User>();
            public Dictionary<int, BankInfo> Banks = new Dictionary<int, BankInfo>();
            public void Save()
            {
                Interface.GetMod().DataFileSystem.WriteObject(file, this);
            }
        }
        public class BankInfo
        {
            public int bankID;
            public float X;
            public float Y;
            public float Z;

            public BankInfo()
            {
            }
        }
        public class User
        {
            public string UserId;
            public string Name;
            public int Money;
            public Int32 Balance;
            public Int32 Created;
            public Int32 LastSeen;

            public User(BasePlayer player)
            {
            }
        }

        StoredData Database = null;
        void Unload()
        {
            Puts("Saving...");
            if(PayDayTimer != null) { PayDayTimer.Destroy(); }
            SaveData();
        }

        private void Loaded()
        {
            
        }
        private bool IsPlayerSetStatically(ulong steamid)
        {
            return StaticVars.UserVariables.ContainsKey(steamid);
        }
        private void OnPlayerSleepEnded(BasePlayer player)
        {
            if (!HasPlayerWoken(player)) { SetStaticPlayerVariable(player, "uHasWoken", "true"); }
            UpdatePlayer(player);
        }
        void OnPlayerInput(BasePlayer player, InputState input)
        {
            if (input.IsDown(BUTTON.FIRE_PRIMARY) &&  player.GetActiveItem() != null && player.GetActiveItem().info.displayName.english == "Targeting Computer" && player.net.connection.authLevel >= 2)
            {
                FireProjectFromPlayer(player, "ammo.rocket.hv");
            }
        }
        private static LayerMask GROUND_MASKS = LayerMask.GetMask("Terrain", "World", "Construction");
        static Vector3 GetGroundPosition(Vector3 sourcePos)
        {
            RaycastHit hitInfo;

            if (Physics.Raycast(sourcePos, Vector3.down, out hitInfo, GROUND_MASKS))
            {
                sourcePos.y = hitInfo.point.y;
            }
            sourcePos.y = Mathf.Max(sourcePos.y, TerrainMeta.HeightMap.GetHeight(sourcePos));
            return sourcePos;
        }
        void FireProjectFromPlayer(BasePlayer player, string entityname)
        {
            ItemDefinition projectileItem = null;
            try
            {
                projectileItem = ItemManager.FindItemDefinition(entityname);
                if (projectileItem != null)
                {
                    ItemModProjectile component = projectileItem.GetComponent<ItemModProjectile>();
                    Vector3 pos = player.transform.position;
                    pos.y = pos.y + 5;
                    BaseEntity entity = GameManager.server.CreateEntity(component.projectileObject.resourcePath, pos, new UnityEngine.Quaternion(), true);
                    ServerProjectile serverProjectile = entity.GetComponent<ServerProjectile>();

                    serverProjectile.gravityModifier = 0.1F;
                    serverProjectile.speed = 10F;

                    entity.SendMessage("InitializeVelocity", (object)(player.eyes.HeadForward() * 1f));
                    entity.SetVelocity(GetGroundPosition(player.eyes.HeadForward() * 1f));
                    entity.Spawn(true);
                }
            }
            catch { Puts(entityname + " is not a valid entity."); return; }
        }
        private bool HasPlayerWoken(BasePlayer player)
        {
            if (!IsPlayerSetStatically(player.userID)) {InitStaticPlayerVariables(player);}
            try { Dictionary<string, object> profile = StaticVars.UserVariables[player.userID];
                if (profile.ContainsKey("uHasWoken")) return Convert.ToBoolean(StaticVars.UserVariables[player.userID]["uHasWoken"]);
            }
            catch
            {
                Puts("Failed to call HasPlayerWoken() for " + player.displayName);
            }
            return false;
        }

        private string ReturnStaticPlayerVariable(BasePlayer player, string variable)
        {
            if (!IsPlayerSetStatically(player.userID)) { InitStaticPlayerVariables(player); }
            Dictionary<string, object> profile = StaticVars.UserVariables[player.userID]; 
            if (profile.ContainsKey(variable)) return StaticVars.UserVariables[player.userID][variable].ToString();
            return "null";
        }
        private object FindPlayerByID(ulong steamid)
        {
            BasePlayer targetplayer = BasePlayer.FindByID(steamid);
            if (targetplayer != null)
            {
                return targetplayer;
            }
            targetplayer = BasePlayer.FindSleeping(steamid);
            if (targetplayer != null)
            {
                return targetplayer;
            }
            return null;
        }
        private object FindPlayer(string tofind)
        {
            if (tofind.Length == 17)
            {
                ulong steamid;
                if (ulong.TryParse(tofind.ToString(), out steamid))
                {
                    return FindPlayerByID(steamid);
                }
            }
            List<BasePlayer> onlineplayers = BasePlayer.activePlayerList as List<BasePlayer>;
            object targetplayer = null;
            foreach (BasePlayer player in onlineplayers.ToArray())
            {

                if (player.displayName.ToString() == tofind)
                    return player;
                else if (player.displayName.ToString().Contains(tofind))
                {
                    if (targetplayer == null)
                        targetplayer = player;
                    else
                        return ReturnDynamicMessage("#multiple_players");
                }
            }
            if (targetplayer != null)
                return targetplayer;
            List<BasePlayer> offlineplayers = BasePlayer.sleepingPlayerList as List<BasePlayer>;
            foreach (BasePlayer player in offlineplayers.ToArray())
            {

                if (player.displayName.ToString() == tofind)
                    return player;
                else if (player.displayName.ToString().Contains(tofind))
                {
                    if (targetplayer == null)
                        targetplayer = player;
                    else
                        return ReturnDynamicMessage("#multiple_players");
                }
            }
            if (targetplayer == null)
                return ReturnDynamicMessage("#no_players");
            return targetplayer;
        }
        private void InitStaticPlayerVariables(BasePlayer player)
        {
            if(StaticVars.UserVariables.ContainsKey(player.userID)) { StaticVars.UserVariables.Remove(player.userID); }
            object profile = new Dictionary<string, object>();
            StaticVars.UserVariables[player.userID] = profile as Dictionary<string, object>;
            if (player != null && player.isConnected)
            {
                foreach(var key in StaticUserVar_LIST)
                {
                    switch (key)
                    {
                        case "uHasWoken": { SetStaticPlayerVariable(player, key, "false"); break; };
                        case "uUserID": { SetStaticPlayerVariable(player, key, player.userID.ToString()); break; };
                        case "tCurrentName": { SetStaticPlayerVariable(player, key, player.displayName.ToString()); break; };
                        case "iMoneyQueue": { SetStaticPlayerVariable(player, key, player.displayName.ToString()); break; };
                        case "bResourceQMSG": { SetStaticPlayerVariable(player, key, "false"); break; };
                        case "iTimeConnected":{ SetStaticPlayerVariable(player, key, "0"); break; };
                    }
                }
            }
        }
        private void SetStaticPlayerVariable(BasePlayer player, string variable, string value, bool debug = false)
        {
            ulong steamid = player.userID;
            if (!IsPlayerSetStatically(steamid)) { InitStaticPlayerVariables(player); return; }
            if (StaticVars.UserVariables[steamid].ContainsKey(variable))
                StaticVars.UserVariables[steamid][variable] = value;
            else
                StaticVars.UserVariables[steamid].Add(variable, value);
            if(debug) Puts(player.displayName + " : " + player.userID.ToString() + " : " + variable + " : " + StaticVars.UserVariables[steamid][variable].ToString());
        }

        void OnPlayerInit(BasePlayer player)
        {
            if (!PlayerExists(player)) { InitPlayer(player); }
            if (!IsPlayerSetStatically(player.userID))
            {
                InitStaticPlayerVariables(player);
                Puts("Temporary variables created for " + player.displayName + " (" + player.userID.ToString() + ")");
            }
            ShowMoneyBar(player);
        }
        private bool DestroyStaticPlayerVariables(BasePlayer player)
        {
            if (player != null)
            {
                if (IsPlayerSetStatically(player.userID))
                {
                    StaticVars.UserVariables.Remove(player.userID);
                    return true;
                }
            }
            return false;
        }
        void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            if (DestroyStaticPlayerVariables(player))
            {
                Puts("Destroying temporary variables created for " + player.displayName + " (" + player.userID.ToString() + ") ["+reason+"]");
            }
            new UIObject("uiPlayerInfo").Destroy(player);

        }
        #region gather_system
        private bool ResourceQueueCheck(BasePlayer player)
        {
            int x = 0;
            if (Int32.TryParse(ReturnStaticPlayerVariable(player, "iMoneyQueue"), out x))
            {
                int max_queue = Convert.ToInt32(Config["Gather", "MaxResourceQueue"]);
                if (x >= max_queue)
                {
                    GivePlayerMoney(player, x, true, true);
                    SetStaticPlayerVariable(player, "iMoneyQueue", "0");
                    return true;
                }
            }
            return false;
        }
        private void OnDispenserGather(ResourceDispenser dispenser, BaseEntity entity, Item item)
        {
            if (Convert.ToBoolean(Config["Gather", "MoneyGather"]))
            {
                if (!entity.ToPlayer()) return;
                var player = entity as BasePlayer;
                var gatherType = dispenser.gatherType.ToString("G");
                var amount = item.amount;
                int rate = Convert.ToInt32(Config["Gather", "MoneyRate"]);
                int divide = Convert.ToInt32(Config["Gather", "MoneyDivide"]);
                int max_queue = Convert.ToInt32(Config["Gather", "MaxResourceQueue"]);
                int money = (amount * rate) / divide;
                int x = 0;

                if (Int32.TryParse(ReturnStaticPlayerVariable(player, "iMoneyQueue"), out x))
                {
                    int value = x + money;
                    if (value != max_queue)
                    {
                        SetStaticPlayerVariable(player, "iMoneyQueue", value.ToString());
                    }
                }
                else
                {
                    SetStaticPlayerVariable(player, "iMoneyQueue", money.ToString());
                }
                ResourceQueueCheck(player);
                if (Convert.ToBoolean(ReturnStaticPlayerVariable(player, "bResourceQMSG"))) { PrintToChatEx(player, "Resource Queue: <color=yellow>" + ReturnStaticPlayerVariable(player, "iMoneyQueue") + "</color> / <color=red>" + max_queue.ToString() + "</color>.", false, "orange", false); }
            }
        }
        private void OnQuarryGather(MiningQuarry quarry, Item item)
        {

        }
        private void OnItemPickup(BasePlayer player, Item item)
        {
 
        }

        private void OnSurveyGather(SurveyCharge surveyCharge, Item item)
        {

        }

        private void OnQuarryEnabled(MiningQuarry quarry)
        {

        }
        #endregion

        private string GivePlayerMoney(BasePlayer player, int amount, bool onUser = true, bool message = true)
        {
            User UserData = null;
            string return_string = "Account doesn't exist";

            if (Database.Users.TryGetValue(player.userID, out UserData) == false)
            {
                return return_string;
            }
            int compare = UserData.Money + amount;
            if (onUser)
            {
                if(compare >= UserData.Money)
                {
                    UserData.Money += amount;
                    return_string = "<color=#89FF12>$" + amount.ToString() + "</color> has been added to your wallet. \n[Wallet: <color=#89FF12>$" + UserData.Money.ToString() + "</color>]";
                }
                else
                {
                    UserData.Money += amount;
                    return_string = "<color=#FF3333>$" + amount.ToString() + "</color> has been removed from your wallet. \n[Wallet: <color=#89FF12>$" + UserData.Money.ToString() + "</color>]";
                }
                
            }
            else
            {
                if (compare >= UserData.Money)
                {
                    UserData.Balance += amount;
                    return_string = "<color=#89FF12>$" + amount.ToString() + "</color> has been added to your bank account. \n[Balance: <color=#89FF12>$" + UserData.Balance.ToString() + "</color>]";
                }
                else
                {
                    UserData.Balance += amount;
                    return_string = "<color=#FF3333>$" + amount.ToString() + "</color> has been removed from your bank account. \n[Balance: <color=#89FF12>$" + UserData.Balance.ToString() + "</color>]";
                }
            }
            UserData.LastSeen = UnixTimeStampUTC();
            Interface.GetMod().DataFileSystem.WriteObject(file, Database);
            UpdatePlayer(player);
            if (message)
            {
                return PrintToChatEx(player, return_string);
            }
            return "-1";
        }
        void UpdatePlayer(BasePlayer player)
        {
            User UserData = null;
            if (Database.Users.TryGetValue(player.userID, out UserData) == false)
            {
                PrintToChatEx(player, "<color=red>WARNING:</color> You currently have no wallet or bank account (<color=yellow>/bank</color>).", false, "orange", false);
                return;
            }
            else { ShowMoneyBar(player); }
        }
        System.Random rnd = new System.Random();
        protected int GetRandomInt(int min, int max)
        {
            return rnd.Next(min, max);
        }
        void PayDay()
        {
            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                if(player != null)
                {
                    if(PlayerExists(player))
                    {
                        int interest_rate = Convert.ToInt32(Config["Bank", "InterestRate"]);
                        Int32 cheque = (999) + GetRandomInt(1, 4999);
                        Int32 interest = ((Int32)GetPlayerMoney(player, false) / 1000) * (interest_rate);
                        Int32 tax = GetRandomInt(1, 150);
                        Int32 final_cheque = (cheque) + (interest) - (tax);
                        GivePlayerMoney(player, final_cheque, false, false);
                        PrintToChatEx(player, "<color=#7ADEFF>PAYDAY:</color> Cheque: $" + cheque.ToString() + ", Interest: $" + interest.ToString() + ", Tax: <color=red>-$" + tax.ToString() + "</color>, Final Cheque: <color=#99FF99>$" + final_cheque.ToString() +"</color>.", false);
                    }
                    else
                    {
                        PrintToChatEx(player, "<color=#7ADEFF>BANK</color>\n<color=red>PAYDAY</color>\nYou currently have no wallet or bank account.");
                    }
                }
            }
        }
        void UpdatePlayers()
        {
            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                User UserData = null;
                UserData.Money = 0;
                if (Database.Users.TryGetValue(player.userID, out UserData) == false)
                {
                    return;
                }
            }
        }
        public static int GetRandomNumber(int min, int max)
        {
            System.Random r = new System.Random();
            int n = r.Next();
            return n;
        }
        private int GetPlayerMoney(BasePlayer player, bool type = true)
        {
            User playerBalance = null;
            if (Database.Users.TryGetValue(player.userID, out playerBalance) == true)
            {
                if (type)
                {
                    return playerBalance.Money;
                }
                else
                {
                    return playerBalance.Balance;
                }
            }
            return 0;
        }
        private bool PlayerDeposit(BasePlayer player, int amount, bool type = true)
        {
            User playerBalance = null;
            if (Database.Users.TryGetValue(player.userID, out playerBalance) == true)
            {
                if (type)
                {
                    if (playerBalance.Money >= amount)
                    {
                        GivePlayerMoney(player, -amount);
                        GivePlayerMoney(player, amount, false);
                        return true;
                    }
                }
                else
                {
                    if (playerBalance.Balance >= amount)
                    {
                        GivePlayerMoney(player, amount, type);
                        return true;
                    }
                }

            }
            return false;
        }
        const string moneybar = @"[
          {
            ""name"": ""CurrentMoney"",
            ""parent"": ""HUD/Overlay"",
            ""components"": [
              {
                ""type"": ""UnityEngine.UI.Text"",
                ""text"": ""${money}"",
                ""fontSize"": ""15"",
                ""color"": ""0 1 0 1"",
                ""align"": ""LowerRight""
              },
              {
                ""type"": ""RectTransform"",
                ""anchormin"": ""0 0.144411777853966"",
                ""anchormax"": ""0.771111777853966 0.2""
              }
            ]
          }
        ]";
        private string ParseMoneyBar(BasePlayer player)
        {
            string json = moneybar.Replace("{money}", GetPlayerMoney(player, true).ToString()) + "]";
            return json;
        }

        private void ShowMoneyBar(BasePlayer player)
        {
            CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "DestroyUI", new Facepunch.ObjectList("CurrentMoney"));
            CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "AddUI", new Facepunch.ObjectList(ParseMoneyBar(player)));
        }
        private bool PlayerWithdraw(BasePlayer player, int amount, bool type = true)
        {
            User playerBalance = null;
            if (Database.Users.TryGetValue(player.userID, out playerBalance) == true)
            {
                if (type)
                {
                    if (playerBalance.Money >= amount)
                    {
                        GivePlayerMoney(player, -amount, true);
                        return true;
                    }
                }
                else
                {
                    if (playerBalance.Balance >= amount)
                    {
                        GivePlayerMoney(player, -amount, false, false);
                        GivePlayerMoney(player, amount, true);
                        return true;
                    }
                }

            }
            return false;
        }
        private string ReturnDynamicMessage(string cmd)
        {
            string return_text = "null";
            foreach (var message in StaticMessages.Values)
            {
                if (message.cmd_association.Equals(cmd, StringComparison.Ordinal))
                {
                    return_text = message.text;
                }
            }
            return return_text;
        }
        private bool CreateStaticMessage(string text, string color, string cmd_association, bool cmd_use_association = false)
        {
            StaticText data = null;
            if (StaticMessages.TryGetValue(cmd_association, out data) == false)
            {
                StaticText mData = new StaticText();
                mData.color = color;
                mData.text = text;
                mData.cmd_association = cmd_association;
                mData.use_cmd_assocation = cmd_use_association;
                StaticMessages.Add(mData.cmd_association, mData);
                return true;
            }
            return false;
        }
        private int CreateBank(float X, float Y, float Z)
        {
            BankInfo pData = new BankInfo();
            pData.bankID = GetRandomNumber(0, 25);
            pData.X = X;
            pData.Y = Y;
            pData.Z = Z;
            Database.Banks.Add(pData.bankID, pData);
            Interface.GetMod().DataFileSystem.WriteObject(file, Database);
            return pData.bankID;
        }
        private bool PlayerExists(BasePlayer player)
        {
            User playerBalance = null;
            if (Database.Users.TryGetValue(player.userID, out playerBalance) == false)
            {
                return false;
            }
            return true;
        }
        private bool InitPlayer(BasePlayer player)
        {
            User pData = null;
            if (!PlayerExists(player))
            {
                pData = new User(player);
                pData.UserId = player.userID.ToString();
                pData.Name = player.displayName;
                pData.Money = Convert.ToInt32(Config["Defaults", "Wallet"]);
                pData.Balance = Convert.ToInt32(Config["Defaults", "Balance"]);
                pData.Created = UnixTimeStampUTC();
                pData.LastSeen = UnixTimeStampUTC();
                Database.Users.Add(player.userID, pData);
                Interface.GetMod().DataFileSystem.WriteObject(file, Database);
                Puts("Registering account: " + player.displayName + " [ " + player.userID.ToString() + " / " + player.net.connection.ipaddress.ToString() + " ]");
                return true;
            }
            return false;
        }
        private string PrintToChatEx(BasePlayer player, string result, bool type = true, string tcolour = "orange", bool use_title = true)
        {
            if (PopupNotifications && type)
            {
                string rstr = "null";
                if(use_title)
                {
                    rstr = "<color=" + tcolour + ">" + this.Title.ToString() + "</color>\n" + result;
                } else
                {
                    rstr = result;
                }
                PopupNotifications?.Call("CreatePopupNotification", rstr, player);
            }
            else
            {
                string rstr = "null";
                if (use_title)
                {
                    rstr = "<color=\"" + tcolour + "\">[" + this.Title.ToString() + "]</color> " + result;
                }
                else
                {
                    rstr = result;
                }
                PrintToChat(player, rstr);
            }
            return result;
        }

        Timer PayDayTimer = null;
        private void OnServerInitialized()
        {
            if (Config["Gather", "DistanceMultiplier"] == null) { Puts("Resetting configuration file (out of date)..."); LoadDefaultConfig(); }
            LoadData();
            CreateStaticMessage("USAGE: /bank deposit <amount>", "white", "deposit", false);
            CreateStaticMessage("USAGE: /bank withdraw <amount>", "white", "withdraw", false);
            CreateStaticMessage("USAGE: /bank <deposit | withdraw | balance | list>", "white", "help", false);
            CreateStaticMessage("USAGE: /p <qmsg>", "white", "p", false);
            CreateStaticMessage("<color=red>ADMIN:</color> /bank <create | warp>", "red", "ahelp", false);
            CreateStaticMessage("Attempting to withdraw from bank... Withdrawal failed.", "white", "#withdraw_failed", false);
            CreateStaticMessage("Attempting to deposit to bank... Deposit failed.", "white", "#deposit_failed", false);
            CreateStaticMessage("FAILED: Found multiple players, be more specific.", "white", "#multiple_players", false);
            CreateStaticMessage("FAILED: Could not find player.", "white", "#multiple_players", false);
            PayDayTimer = timer.Repeat(1800, 0, () => PayDay());
        }
        class AnimalInfo { public string Name { get; set; } public int Cost { get; set; } public string PrefabID { get; set; } }
        private Dictionary<string, AnimalInfo> Animals = new Dictionary<string, AnimalInfo>()
        {
            { "Bear", new AnimalInfo { Name="Bear", Cost=350, PrefabID="bear.prefab" } },
            { "Boar", new AnimalInfo { Name="Boar", Cost=325, PrefabID="boar.prefab"} },
            { "Chicken", new AnimalInfo { Name="Chicken", Cost=75, PrefabID="chicken.prefab"} },
            { "Horse", new AnimalInfo { Name="Horse", Cost=150, PrefabID="horse.prefab"} },
            { "Stag", new AnimalInfo { Name="Stag", Cost=125, PrefabID="stag.prefab"} },
            { "Wolf", new AnimalInfo { Name="Wolf", Cost=250, PrefabID="wolf.prefab"} },
        };
        void LoadData()
        {
            Puts("Firing up " + this.Title + " " + this.Version.ToString() + "...");
            try { Database = Interface.GetMod().DataFileSystem.ReadObject<StoredData>(file); } catch { Database = new StoredData(); }
            try { StaticVars = new StaticVariables();  } catch { } // Non file variables.
            AnimalCosts();
        }
        protected override void LoadDefaultConfig()
        {
            Puts("No configuration file found, generating...");
            Config.Clear();

            // --- [ GENERAL ] ----

            Config["General", "Protocol"] = Protocol.network;
            Config["General", "MessagesEnabled"] = true;

            // --- [ DEPENDENCIES ] ----

            Config["Dependencies", "PopupNotifications"] = true;

            // --- [ GATHER ] ----

            Config["Gather", "MoneyGather"] = true;
            Config["Gather", "MoneyDivide"] = 20;
            Config["Gather", "MoneyRate"] = 2;
            Config["Gather", "DistanceMultiplier"] = 5;
            Config["Gather", "MaxResourceQueue"] = 25000;

            // --- [ BANK ] ----

            Config["Bank", "InterestRate"] = 2;

            // --- [ TELEPORT ] ----

            Config["Teleport", "Wait"] = 3;

            // --- [ DEFAULTS ] ----

            Config["Defaults", "Wallet"] = 150;
            Config["Defaults", "Balance"] = 2500;

            // --- [ REWARDS ] ----

            Config["Rewards", "Wolf"] = 250;
            Config["Rewards", "Stag"] = 125;
            Config["Rewards", "Horse"] = 150;
            Config["Rewards", "Chicken"] = 75;
            Config["Rewards", "Boar"] = 325; 
            Config["Rewards", "Bear"] = 350;

            SaveConfig();
        }
        private void AnimalCosts()
        {
            Puts("Updating "+Animals.Count.ToString()+" animals...");
            Animals["Wolf"].Cost = Convert.ToInt32(Config["Rewards", "Wolf"]);
            Animals["Stag"].Cost = Convert.ToInt32(Config["Rewards", "Stag"]);
            Animals["Horse"].Cost = Convert.ToInt32(Config["Rewards", "Horse"]);
            Animals["Chicken"].Cost = Convert.ToInt32(Config["Rewards", "Chicken"]);
            Animals["Boar"].Cost = Convert.ToInt32(Config["Rewards", "Boar"]);
            Animals["Bear"].Cost = Convert.ToInt32(Config["Rewards", "Bear"]);
        }
        void SaveData()
        {
            Interface.Oxide.DataFileSystem.WriteObject(file, Database);
        }
        private bool PlayerToPoint(BasePlayer player, float radi, float x, float y, float z)
        {
            float oldposx = 0.0f, oldposy = 0.0f, oldposz = 0.0f, tempposx = 0.0f, tempposy = 0.0f, tempposz = 0.0f;
            oldposx = player.transform.position.x;
            oldposy = player.transform.position.y;
            oldposz = player.transform.position.z;
            tempposx = (oldposx - x);
            tempposy = (oldposy - y);
            tempposz = (oldposz - z);
            if (((tempposx < radi) && (tempposx > -radi)) && ((tempposy < radi) && (tempposy > -radi)) && ((tempposz < radi) && (tempposz > -radi)))
            {
                return true;
            }
            return false;
        }
        int GetDistance(BaseCombatEntity vic, HitInfo hitInfo) // DeathNotes
        {
            float distance = 0F;
            if (vic.transform.position != null) distance = Vector3.Distance(vic.transform.position, hitInfo?.Initiator?.transform.position ?? vic.transform.position);
            else distance = 0F;
            return Convert.ToInt32(distance);
        }
        void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
        {
            if (Convert.ToBoolean(Config["Gather", "MoneyGather"]))
            {
                if (entity is BaseNPC)
                {
                    var player = info.Initiator as BasePlayer;
                    string entity_animal = entity.LookupShortPrefabName();
                    bool popups = false;
                    int rate = Convert.ToInt32(Config["Gather", "MoneyRate"]);
                    if (info.Initiator is BasePlayer)
                    {
                        int wallet = GetPlayerMoney(player, true); int gain = 0; string animal_name = "null";
                        int distance = GetDistance(entity, info);
                        int distance_gain = distance * Convert.ToInt32(Config["Gather", "DistanceMultiplier"]);
                        foreach(var animal in Animals.Values)
                        {
                            if(entity_animal == animal.PrefabID)
                            {
                                animal_name = animal.Name;
                                gain = (animal.Cost * rate) + distance_gain;
                            }
                        }
                        if (animal_name != "null")
                        {
                            GivePlayerMoney(player, gain, true, popups);
                            PrintToChatEx(player, "Wallet: +<color=#9CFF7A>$" + gain.ToString() + "</color> for killing a <color=#FF1212>" + animal_name.ToString() + "</color>.\n<color=yellow>Distance:</color> "+ distance.ToString() + "m [ +<color=#9CFF7A>$" + distance_gain.ToString()+"</color> ]");
                        }
                    }
                }   
            }
        }
        public static DateTime UnixTSToDateTime(double unixTimeStamp)
        {
            System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            return dtDateTime;
        }
        private void ShowPlayerInfoPanel(BasePlayer player)
        {
            UIObject ui = new UIObject("uiPlayerInfo");
            string queue = ReturnStaticPlayerVariable(player, "iMoneyQueue");
            if(queue == player.displayName) { queue = "0"; }
            int seconds = Convert.ToInt32(ConnectionDB.CallHook("SecondsPlayed", player));
            TimeSpan ts = TimeSpan.FromSeconds(seconds);
            DateTime init_date = UnixTSToDateTime(Database.Users[player.userID].Created);
            string panel = ui.AddPanel("uiPlayerInfo", 0.3, 0.2, 0.3, 0.4, new UIColor(0.1, 0.1, 0.1, 0.8), true);
            ui.AddText("Title", 0, 0, 1, 0.2, new UIColor(1, 0, 0, 1), this.Title + " v" + this.Version.ToString(), 25, panel);
            ui.AddText("MoneyText", 0.1, 0.2, 1.0, 0.6, new UIColor(1, 0, 1, 1),
                "<color=#4DFFED>Name:</color> <color=#00FF11>"
                + player.displayName +
                "</color>\n<color=#4DFFED>Steam ID (64):</color> <color=#00FF11>"
                + player.userID.ToString() +
                "</color>\n<color=#4DFFED>IP Address:</color> <color=#00FF11>"
                + player.net.connection.ipaddress.ToString() +
                "</color>\n<color=#4DFFED>Wallet:</color> <color=#00FF11>$"
                + GetPlayerMoney(player, true).ToString() +
                "</color>\n<color=#4DFFED>Balance:</color> <color=#00FF11>$"
                + GetPlayerMoney(player, false).ToString() +
                "</color>\n<color=#4DFFED>Minutes Played:</color> <color=#00FF11>"
                + Math.Round(ts.TotalMinutes).ToString() +
                "</color>\n<color=#4DFFED>Date Joined:</color> <color=#00FF11>"
                + init_date.ToShortDateString() +
                "</color>\nResource Queue: <color=yellow>" + queue + "</color> / <color=red>" + Config["Gather", "MaxResourceQueue"] + "</color>", 18, panel, 4);
            string button = ui.AddButton("ProceedButton", 0.9, 0, 0.1, 0.1, new UIColor(0, 0.5, 0.5, 1), "", panel, panel);
            ui.AddText("ProceedButtonText", 0.3, 0, 0.4, 0.8, new UIColor(1, 1, 1, 1), "X", 15, button);
            ui.Draw(player);

        }
        [ChatCommand("p")]
        void cmdPlayer(BasePlayer player, string cmd, string[] args)
        {
            if (args.Length == 0 || args.Length > 2)
            {
                ShowPlayerInfoPanel(player);
                PrintToChatEx(player, ReturnDynamicMessage("p"), false);
                if (player.net.connection.authLevel >= 1)
                {
                    //PrintToChatEx(player, ReturnDynamicMessage("ahelp"), false);
                }
            }
            else if (args[0] == "qmsg")
            {
                if (args.Length == 1)
                {
                    if(!Convert.ToBoolean(ReturnStaticPlayerVariable(player, "bResourceQMSG"))) {
                        SetStaticPlayerVariable(player, "bResourceQMSG", "true");
                        PrintToChatEx(player, "You have <color=green>enabled</color> Resource Queue messages.", false);
                    }
                    else
                    {
                        SetStaticPlayerVariable(player, "bResourceQMSG", "false");
                        PrintToChatEx(player, "You have <color=red>disabled</color> Resource Queue messages.", false);
                    }
                }
            }
            else if (args[0] == "help" && args.Length == 1)
            {
                PrintToChatEx(player, ReturnDynamicMessage("p"), false);
            }
            else
            {
                PrintToChatEx(player, "Syntax Error: /p help - Shows you more information about the commands.", false);
            }
        }
        [ChatCommand("bank")]
        void cmdBank(BasePlayer player, string cmd, string[] args)
        {
            if (args.Length == 0 || args.Length > 2)
            {
                Puts(player.displayName + " has played for " + Convert.ToString(ConnectionDB.CallHook("SecondsPlayed", player)) + " seconds.");
                PrintToChatEx(player, ReturnDynamicMessage("help"), false);
                if (player.net.connection.authLevel >= 1)
                {
                    PrintToChatEx(player, ReturnDynamicMessage("ahelp"), false);
                }
            }
            else if (args[0] == "warp")
            {
                if (player.net.connection.authLevel >= 2)
                {
                    if (args.Length == 1)
                    {
                        int count = 0;
                        foreach (var bank in Database.Banks.Values)
                        {
                            if (bank.X != 0 && bank.Z != 0)
                            {
                                count++;
                                PrintToChatEx(player, "[ID: " + count.ToString() + "] X: " + bank.X.ToString() + " Y: " + bank.Y.ToString() + " Z:" + bank.Y.ToString() + ".", false);
                            }
                        }
                        if (count == 0)
                        {
                            PrintToChatEx(player, "No banks currently exist.", false);
                        }
                    }
                    else if (args.Length == 2)
                    {
                        int count = 0;
                        int foundcount = 0;
                        foreach (var bank in Database.Banks.Values)
                        {
                            if (bank.X != 0 && bank.Z != 0)
                            {
                                count++;
                                if (args[1].ToString() == count.ToString())
                                {
                                    float x = Convert.ToSingle(bank.X);
                                    float y = Convert.ToSingle(bank.Y);
                                    float z = Convert.ToSingle(bank.Z);
                                    bool success = Convert.ToBoolean(MagicTeleportation.CallHook("InitTeleport", player, x, y, z, false, true, "Bank" + count.ToString(), null, count, Convert.ToInt32(Config["Teleport", "Wait"])));
                                    if (!success) { PrintToChat(player, "You <color=red>can't</color> teleport currently.", false); }
                                    foundcount++;
                                    return;
                                }
                            }
                        }
                        if (count == 0 || foundcount == 0)
                        {
                            PrintToChatEx(player, "Invalid bank/id.", false);
                        }
                    }

                }
                else
                {
                    PrintToChatEx(player, "You do not have access to this command.", false);
                }
            }
            else if (args[0] == "list")
            {
                if(args.Length == 1)
                {
                    int count = 0;
                    foreach (var bank in Database.Banks.Values)
                    {
                        if(bank.X != 0 && bank.Z != 0)
                        {
                            count++;
                            PrintToChatEx(player, "[ID: " + count.ToString() + "] X: " + bank.X.ToString() +" Y: " + bank.Y.ToString() + " Z:" + bank.Y.ToString() + ".", false);
                        }  
                    }
                    if (count == 0)
                    {
                        PrintToChatEx(player, "No banks currently exist.", false);
                    }
                }
            }
            else if (args[0] == "create" && args.Length == 1)
            {
                if (player.net.connection.authLevel >= 2)
                {
                    int new_id = CreateBank(player.transform.position.x, player.transform.position.y, player.transform.position.z);
                    PrintToChatEx(player, "Created bank " + new_id.ToString() + " at your current location.");
                }
                else
                {
                    PrintToChatEx(player, "You do not have access to this command.", false);
                }
            }
            else if (args[0] == "balance" && args.Length == 1)
            {
                int count = 0;
                foreach (var bank in Database.Banks.Values)
                {
                    if (PlayerToPoint(player, 3.00f, bank.X, bank.Y, bank.Z))
                    {
                        string color;
                        if (GetPlayerMoney(player, false) <= 0) { color = "#FF1212"; }
                        else
                        {
                            color = "#9CFF7A";
                        }
                        PrintToChatEx(player, "<color=#7ADEFF>BANK</color>\n Current Balance: <color=" + color + ">$" + GetPlayerMoney(player, false).ToString() + "</color>");
                        return;
                    }
                }
                if(count == 0) { PrintToChatEx(player, "You are <color=red>not</color> at a bank!", true);  }
            }
            else if (args[0] == "deposit")
            {
                if (args.Length == 1)
                {
                    PrintToChatEx(player, ReturnDynamicMessage(args[0].ToString()));
                }
                else
                {
                    int count = 0;
                    foreach (var bank in Database.Banks.Values)
                    {
                        if (PlayerToPoint(player, 3.00f, bank.X, bank.Y, bank.Z))
                        {
                            int amount = 0;

                            if (Int32.TryParse(args[1], out amount))
                            {
                                if (!PlayerDeposit(player, amount, true))
                                {
                                    PrintToChatEx(player, ReturnDynamicMessage("#deposit_failed"));
                                    count++;
                                    return;
                                }
                                else
                                {
                                    PrintToChatEx(player, "You have successfully deposited $" + amount.ToString() + " to your bank account. [New Balance: $" + GetPlayerMoney(player, false).ToString() + "]", false);
                                    count++;
                                    return;
                                }
                            }
                        }
                    }
                    if (count == 0)
                    {
                        PrintToChatEx(player, "You're not at a bank.");
                    }
                }

            }
            else if (args[0] == "withdraw")
            {
                if(args.Length == 1)
                {
                    PrintToChatEx(player, ReturnDynamicMessage(args[0].ToString()));
                }
                else
                {
                    int count = 0;
                    foreach (var bank in Database.Banks.Values)
                    {
                        if (PlayerToPoint(player, 3.00f, bank.X, bank.Y, bank.Z))
                        {
                            int amount = 0;

                            if (Int32.TryParse(args[1], out amount))
                            {
                                if (!PlayerWithdraw(player, amount, false))
                                {
                                    PrintToChatEx(player, ReturnDynamicMessage("#withdraw_failed"));
                                    count++;
                                    return;
                                }
                                else
                                {
                                    PrintToChatEx(player, "You have successfully withdrawn $" + amount.ToString() + " from your bank account. [New Balance: $"+GetPlayerMoney(player, false).ToString()+"]", false);
                                    count++;
                                    return;
                                }
                            }
                        }
                    }
                    if (count == 0)
                    {
                        PrintToChatEx(player, "You're not at a bank.");
                    }
                }
                
            }
            else if (args[0] == "help" && args.Length == 1)
            {
                PrintToChatEx(player, ReturnDynamicMessage(args[0].ToString()));
            }
            else
            {
                PrintToChatEx(player, "Syntax Error: /bank help - Shows you more information about the commands.", false);
            }
        }

        [ChatCommand("givemoney")]
        private void GiveMoney(BasePlayer player, string command, string[] args)
        {
            if (player.net.connection.authLevel >= 2)
            {
                if (args.Length != 1)
                {
                    PrintToChat(player, "[Usage:] /givemoney amount  (ex: /givemoney 1000)");
                    return;
                }
                int amount = 0;
                if (int.TryParse(args[0], out amount) == false || amount == 0)
                {
                    PrintToChat(player, "Could not determine amount, amount could not be parsed or is zero!");
                    return;
                }
                GivePlayerMoney(player, amount);
            }
            else
            {
                PrintToChat(player, this.Title.ToString(), "You do not have access to this command.");
            }
        }
        ////////////////////////////////////////
        ///     UI Builder
        ////////////////////////////////////////

        class UIColor
        {
            double red;
            double green;
            double blue;
            double alpha;

            public UIColor(double red, double green, double blue, double alpha)
            {
                this.red = red;
                this.green = green;
                this.blue = blue;
                this.alpha = alpha;
            }

            public string GetString()
            {
                return $"{red.ToString()} {green.ToString()} {blue.ToString()} {alpha.ToString()}";
            }
        }

        class UIObject
        {
            List<object> ui = new List<object>();
            string name;

            public UIObject(string name)
            {
                this.name = name;
            }

            string RandomString()
            {
                string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
                List<char> charList = chars.ToList();

                string random = "";

                for (int i = 0; i <= UnityEngine.Random.Range(5, 10); i++)
                    random = random + charList[UnityEngine.Random.Range(0, charList.Count - 1)];

                return random;
            }

            public void Draw(BasePlayer player)
            {
                CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "AddUI", new Facepunch.ObjectList(JsonConvert.SerializeObject(ui).Replace("{NEWLINE}", Environment.NewLine)));
            }

            public void Destroy(BasePlayer player)
            {
                CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "DestroyUI", new Facepunch.ObjectList(name));
            }

            public string AddPanel(string name, double left, double top, double width, double height, UIColor color, bool mouse = false, string parent = "HUD/Overlay")
            {
                name = name + RandomString();

                string type = "";
                if (mouse) type = "NeedsCursor";

                ui.Add(new Dictionary<string, object> {
                    {"name", name},
                    {"parent", parent},
                    {"components",
                        new List<object> {
                            new Dictionary<string, string> {
                                {"type", "UnityEngine.UI.Image"},
                                {"color", color.GetString()}
                            },

                            new Dictionary<string, string> {
                                {"type", "RectTransform"},
                                {"anchormin", $"{left.ToString()} {((1 - top) - height).ToString()}"},
                                {"anchormax", $"{(left + width).ToString()} {(1 - top).ToString()}"}
                            },
                            new Dictionary<string, string> {
                                {"type", type}
                            }
                        }
                    }
                });

                return name;
            }

            public string AddText(string name, double left, double top, double width, double height, UIColor color, string text, int textsize = 15, string parent = "HUD/Overlay", int alignmode = 0)
            {
                name = name + RandomString(); text = text.Replace("\n", "{NEWLINE}"); string align = "";
                switch(alignmode)
                {
                    case 0: { align = "LowerCenter"; break; };
                    case 1: { align = "LowerLeft"; break; };
                    case 2: { align = "LowerRight"; break; };
                    case 3: { align = "MiddleCenter"; break; };
                    case 4: { align = "MiddleLeft"; break; };
                    case 5: { align = "MiddleRight"; break; };
                    case 6: { align = "UpperCenter"; break; };
                    case 7: { align = "UpperLeft"; break; };
                    case 8: { align = "UpperRight"; break; }
                    default: { align = "MiddleRight"; break; };
                }
                ui.Add(new Dictionary<string, object> {
                    {"name", name},
                    {"parent", parent},
                    {"components",
                        new List<object> {
                            new Dictionary<string, string> {
                                {"type", "UnityEngine.UI.Text"},
                                {"text", text},
                                {"fontSize", textsize.ToString()},
                                {"color", color.GetString()},
                                {"align", align}
                            },

                            new Dictionary<string, string> {
                                {"type", "RectTransform"},
                                {"anchormin", $"{left.ToString()} {((1 - top) - height).ToString()}"},
                                {"anchormax", $"{(left + width).ToString()} {(1 - top).ToString()}"}
                            }
                        }
                    }
                });

                return name;
            }

            public string AddButton(string name, double left, double top, double width, double height, UIColor color, string command = "", string parent = "HUD/Overlay", string closeUi = "")
            {
                name = name + RandomString();

                ui.Add(new Dictionary<string, object> {
                    {"name", name},
                    {"parent", parent},
                    {"components",
                        new List<object> {
                            new Dictionary<string, string> {
                                {"type", "UnityEngine.UI.Button"},
                                {"close", closeUi},
                                {"command", command},
                                {"color", color.GetString()},
                                {"imagetype", "Tiled"}
                            },

                            new Dictionary<string, string> {
                                {"type", "RectTransform"},
                                {"anchormin", $"{left.ToString()} {((1 - top) - height).ToString()}"},
                                {"anchormax", $"{(left + width).ToString()} {(1 - top).ToString()}"}
                            }
                        }
                    }
                });
                return name;
            }
        }
    }
}