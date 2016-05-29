// Reference: MySql.Data

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MySql.Data;
using MySql.Data.MySqlClient;
using MySql.Data.Common;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("PoliticalSurvival", "Jonty", 0.2)]
    [Description("Political Survival - Become the President, tax your subjects and keep them in line!")]
    class PoliticalSurvival : RustPlugin
    {
        static void Main(string[] args) { }

        public class StrayPlayer
        {
            public ulong SteamId;
            public bool IsSettingTaxChest;

            public StrayPlayer(ulong pSteamId)
            {
                this.SteamId = pSteamId;
                this.IsSettingTaxChest = false;
            }
        }

        Dictionary<BasePlayer, StrayPlayer> OnlinePlayers;
        Dictionary<string, string> ServerMessages;

        MySqlConnection Database;

        string DatabaseHost = "";
        string DatabasePort = "";
        string DatabaseUsername = "";
        string DatabasePassword = "";
        string DatabaseName = "";

        ulong President = 0;
        double TaxLevel = 20.0;
        string RealmName = "";

        float TaxChestX = 0;
        float TaxChestY = 0;
        float TaxChestZ = 0;

        StorageContainer TaxContainer = null;

        private void LoadDefaultConfig()
        {
            CreateConfigEntry("Database", "Host", "127.0.0.1");
            CreateConfigEntry("Database", "Port", "3306");
            CreateConfigEntry("Database", "Username", "root");
            CreateConfigEntry("Database", "Password", "lol123");
            CreateConfigEntry("Database", "Name", "rust");

            SaveConfig();
        }

        void Init()
        {
            DatabaseHost = Config["Database", "Host"].ToString();
            DatabasePort = Config["Database", "Port"].ToString();
            DatabaseUsername = Config["Database", "Username"].ToString();
            DatabasePassword = Config["Database", "Password"].ToString();
            DatabaseName = Config["Database", "Name"].ToString();
            RealmName = lang.GetMessage("DefaultRealm", this);

            Puts("Political Survival is starting...");

            OnlinePlayers = new Dictionary<BasePlayer, StrayPlayer>();
            LoadServerMessages();

            try
            {
                Database = new MySqlConnection();
                Database.ConnectionString = "server=" + DatabaseHost + ";port=" + DatabasePort + ";uid=" + DatabaseUsername + ";pwd=" + DatabasePassword + ";database=" + DatabaseName + ";";
                Database.Open();

                MySqlCommand GetSettings = new MySqlCommand("SELECT president,tax_level,realm_name,tax_chest FROM settings LIMIT 1", Database);
                MySqlDataReader SettingsReader = GetSettings.ExecuteReader();

                while (SettingsReader.Read())
                {
                    President = SettingsReader.GetUInt64(0);
                    TaxLevel = SettingsReader.GetInt32(1);
                    RealmName = SettingsReader.GetString(2);

                    string[] TaxCoordinates = SettingsReader.GetString(3).Split(';');
                    TaxChestX = Convert.ToSingle(TaxCoordinates[0]);
                    TaxChestY = Convert.ToSingle(TaxCoordinates[1]);
                    TaxChestZ = Convert.ToSingle(TaxCoordinates[2]);				
                }

                SettingsReader.Dispose();
                GetSettings.Dispose();
            }
            catch (Exception e)
            {
                PrintToConsole(e.ToString());
                PrintToConsole("If this is the first time running the plugin, please edit the configuration!");
            }
         
            Puts("Realm name is " + RealmName);
            Puts("Tax level is " + TaxLevel);
            Puts("President is " + President);

            LoadTaxContainer();

            if (BasePlayer.activePlayerList.Count >= 1)
            {
                foreach (BasePlayer iPlayer in BasePlayer.activePlayerList)
                {
                    AddPlayer(iPlayer);
                }

                Puts(OnlinePlayers.Count + " players cached.");
            }

            Puts("Political Survival: Started");
        }

        void OnPlayerInit(BasePlayer Player)
        {
            PrintToChat(Player.displayName + " " + lang.GetMessage("PlayerConnected", this, Player.UserIDString) + " " + RealmName);
            AddPlayer(Player);
        }

        void OnPlayerDisconnected(BasePlayer Player, string Reason)
        {
            PrintToChat(Player.displayName + " " + lang.GetMessage("PlayerDisconnected", this, Player.UserIDString) + " " + RealmName);
            RemovePlayer(Player);
        }

        void OnDispenserGather(ResourceDispenser Dispenser, BaseEntity Entity, Item Item)
        {
            if (TaxLevel > 0 && President > 0)
            {
                int Tax = Convert.ToInt32(Math.Round((Item.amount * TaxLevel) / 100));
                Item.amount = Item.amount - Tax;

                if (TaxContainer == null)
                {
                    TaxChestX = 0;
                    TaxChestY = 0;
                    TaxChestZ = 0;
                    SaveTaxContainer();
                    LoadTaxContainer();
                    return;
                }

                if (!TaxContainer.inventory.IsFull())
                {
                    ItemDefinition ToAdd = ItemManager.FindItemDefinition(Item.info.itemid);

                    if (ToAdd != null)
                    {
                        TaxContainer.inventory.AddItem(ToAdd, Tax);
                    }
                }
            }
        }

        void OnPlantGather(PlantEntity Plant, Item Item, BasePlayer Player)
        {
            int Tax = Convert.ToInt32(Math.Round((Item.amount * TaxLevel) / 100));
            Item.amount = Item.amount - Tax;
        }

        void OnEntityDeath(BaseCombatEntity Entity, HitInfo Info)
        {
            BasePlayer Player = Entity.ToPlayer();

            if (Player != null)
            {
                if (IsPresident(Player.userID))
                {
                    BasePlayer Killer = null;

                    if (Info != null)
                        Killer = Info.Initiator.ToPlayer();

                    if (Killer != null && Killer.userID != Player.userID)
                    {
                        SetPresident(Killer.userID);
                        PrintToChat(string.Format(lang.GetMessage("PresidentMurdered", this), Killer.displayName));
                    }
                    else
                    {
                        SetPresident(0);
                        PrintToChat(string.Format(lang.GetMessage("PresidentDied", this)));
                    }
                }
            }
        }

        void OnPlayerAttack(BasePlayer Attacker, HitInfo Info)
        {
            BasePlayer Defender = Info.HitEntity.ToPlayer();

            if (Defender != null)
            {
                // Is a person
            }
            else
            {
                uint EntityId = Info.HitEntity.prefabID;

                if (EntityId == 2014947887 || EntityId == 3439001196)
                {
                    StrayPlayer Stray = OnlinePlayers[Attacker.ToPlayer()];

                    if (Stray == null)
                        return;

                    if (Stray.IsSettingTaxChest)
                    {
                        Vector3 BoxPosition = Info.HitEntity.transform.position;
                        float x = BoxPosition.x;
                        float y = BoxPosition.y;
                        float z = BoxPosition.z;

                        SendReply(Attacker.ToPlayer(), lang.GetMessage("SetNewTaxChest", this, Attacker.ToPlayer().UserIDString));

                        TaxChestX = x;
                        TaxChestY = y;
                        TaxChestZ = z;
						
                        SaveTaxContainer();
                        LoadTaxContainer();

                        Stray.IsSettingTaxChest = false;
                    }
                }
            }
        }

        [ChatCommand("settaxchest")]
        void SetTaxChestCommand(BasePlayer Player, string Command, string[] Arguments)
        {
            if (!IsPresident(Player.userID))
            {
                SendReply(Player, lang.GetMessage("PresidentError", this, Player.UserIDString));
                return;
            }

            StrayPlayer Stray = OnlinePlayers[Player];

            if (Stray.IsSettingTaxChest)
            {
                Stray.IsSettingTaxChest = false;
                SendReply(Player, lang.GetMessage("NotSettingNewTaxChest", this, Player.UserIDString));
            }
            else
            {
                Stray.IsSettingTaxChest = true;
                SendReply(Player, lang.GetMessage("SettingNewTaxChest", this, Player.UserIDString));
            }
        }

        [ChatCommand("info")]
        void InfoCommand(BasePlayer Player, string Command, string[] Arguments)
        {
            string PresidentName = "";

            if (President > 0)
            {
                BasePlayer BasePresident = BasePlayer.FindByID(President);

                if (BasePresident != null)
                {
                    PresidentName = BasePresident.displayName;
                }
                else
                {
                    BasePlayer SleepingPresident = BasePlayer.FindSleeping(President);

                    if (SleepingPresident != null)
                    {
                        PresidentName = SleepingPresident.displayName;
                    }
                    else
                    {
                        PresidentName = lang.GetMessage("ClaimPresident", this, Player.UserIDString);
                        President = 0;
                    }
                }
            }
            else
                PresidentName = lang.GetMessage("ClaimPresident", this, Player.UserIDString);

            SendReply(Player, lang.GetMessage("InfoPresident", this, Player.UserIDString) + ": " + PresidentName);
            SendReply(Player, lang.GetMessage("InfoRealmName", this, Player.UserIDString) + ": " + RealmName);
            SendReply(Player, lang.GetMessage("InfoTaxLevel", this, Player.UserIDString) + ": " + TaxLevel + "%");         
        }

        [ChatCommand("claimpresident")]
        void ClaimPresident(BasePlayer Player, string Command, string[] Arguments)
        {
            if (President < 1)
            {
                PrintToChat("<color=#008080ff><b>" + Player.displayName + "</b></color> " + lang.GetMessage("IsNowPresident", this));
                SetPresident(Player.userID);
            }
        }

        [ChatCommand("settax")]
        void SetTaxCommand(BasePlayer Player, string Command, string[] Arguments)
        {
            if (IsPresident(Player.userID))
            {
                double NewTaxLevel = Convert.ToDouble(MergeParams(Arguments, 0));

                if (NewTaxLevel > 25.0)
                    NewTaxLevel = 25.0;

                if (NewTaxLevel == TaxLevel)
                    return;

                if (NewTaxLevel < 1)
                    NewTaxLevel = 0;

                SetTaxLevel(NewTaxLevel);

                PrintToChat(string.Format(lang.GetMessage("UpdateTaxMessage", this), Player.displayName, NewTaxLevel));
            }
            else
                SendReply(Player, lang.GetMessage("PresidentError", this, Player.UserIDString));
        }

        [ChatCommand("realmname")]
        void RealmNameCommand(BasePlayer Player, string Command, string[] Arguments)
        {
            if (IsPresident(Player.userID))
            {
                string NewName = MergeParams(Arguments, 0);

                if (!String.IsNullOrEmpty(NewName))
                {
                    SetRealmName(NewName);
                }
            }
            else
                SendReply(Player, lang.GetMessage("PresidentError", this, Player.UserIDString));
        }

        [ChatCommand("pm")]
        void PrivateMessage(BasePlayer Player, string Command, string[] Arguments)
        {
            string Name = Arguments[0];
            string Message = MergeParams(Arguments, 1);

            if (IsPlayerOnline(Name))
            {
                BasePlayer Reciever = GetPlayer(Name);
                SendReply(Reciever, "<color=#ffff00ff>" + lang.GetMessage("PrivateFrom", this, Reciever.UserIDString) + " " + Player.displayName + "</color>: " + Message);
                SendReply(Player, "<color=#ffff00ff>" + lang.GetMessage("PrivateTo", this, Reciever.UserIDString) + " " + Reciever.displayName + "</color>: " + Message);
            }
            else
                SendReply(Player, Name + lang.GetMessage("PrivateError", this));
        }

        [ChatCommand("players")]
        void PlayersCommand(BasePlayer Player, string Command, string[] Arguments)
        {
            StringBuilder Builder = new StringBuilder();
            int PlayerCount = BasePlayer.activePlayerList.Count;
            int Cycle = 1;
            
            Builder.Append(string.Format(lang.GetMessage("OnlinePlayers", this), PlayerCount) + " ");

            foreach (BasePlayer iPlayer in BasePlayer.activePlayerList)
            {
                Builder.Append(iPlayer.displayName);

                if (Cycle < PlayerCount)
                    Builder.Append(", ");

                Cycle++;
            }

            SendReply(Player, Builder.ToString());
        }

        void AddPlayer(BasePlayer Player)
        {
            GetPlayerFromDatabase(Player);
        }

        void RemovePlayer(BasePlayer Player)
        {
            OnlinePlayers.Remove(Player);
        }

        StrayPlayer GetStrayPlayer(string Username)
        {
            return OnlinePlayers[BasePlayer.Find(Username)];
        }

        bool IsPlayerOnline(string Username)
        {
            if (BasePlayer.Find(Username) != null)
                return true;

            return false;
        }

        BasePlayer GetPlayer(string Username)
        {
            return BasePlayer.Find(Username);
        }

        string MergeParams(string[] Params, int Start)
        {
            var Merged = new StringBuilder();
            for (int i = Start; i < Params.Length; i++)
            {
                if (i > Start)
                    Merged.Append(" ");
                Merged.Append(Params[i]);
            }

            return Merged.ToString();
        }

        bool IsPresident(ulong SteamId)
        {
            if (President == SteamId)
                return true;

            return false;
        }

        void SetPresident(ulong SteamId)
        {
            President = SteamId;
            RealmName = lang.GetMessage("DefaultRealm", this);
            TaxLevel = 0.0;

            string PresidentText = "UPDATE settings SET tax_level = " + TaxLevel + ", realm_name = '" + RealmName + "', president = " + President;

            MySqlCommand UpdatePresident = new MySqlCommand(PresidentText, Database);
            UpdatePresident.ExecuteNonQuery();
            UpdatePresident.Dispose();
        }

        void SetTaxLevel(double NewTaxLevel)
        {
            TaxLevel = NewTaxLevel;

            string TaxText = "UPDATE settings SET tax_level = " + NewTaxLevel;

            MySqlCommand UpdateTax = new MySqlCommand(TaxText, Database);
            UpdateTax.ExecuteNonQuery();
            UpdateTax.Dispose();
        }

        void SetRealmName(string NewName)
        {
            if (NewName.Length > 36)
                NewName = NewName.Substring(0, 36);

            RealmName = NewName;
            PrintToChat(string.Format(lang.GetMessage("RealmRenamed", this), NewName));

            string RealmText = "UPDATE settings SET realm_name = '" + RealmName + "'";

            MySqlCommand RealmCommand = new MySqlCommand(RealmText, Database);
            RealmCommand.ExecuteNonQuery();
            RealmCommand.Dispose();
        }

        void GetPlayerFromDatabase(BasePlayer Player)
        {
            StrayPlayer IPlayer = null;

            string CommandText = "SELECT id FROM players WHERE steam_id = " + Player.userID;
            MySqlCommand Command = new MySqlCommand(CommandText, Database);
            bool Exists = Command.ExecuteScalar() != null ? true : false;
            Command.Dispose();

            if (!Exists)
            {
                string InsertText = "INSERT INTO players (steam_id) VALUES ('" + Player.userID + "')";
                MySqlCommand InsertCommand = new MySqlCommand(InsertText, Database);
                InsertCommand.ExecuteNonQuery();
                InsertCommand.Dispose();
            }

            string InfoText = "SELECT * FROM players WHERE steam_id = " + Player.userID;
            MySqlCommand InfoCommand = new MySqlCommand(InfoText, Database);
            MySqlDataReader InfoReader = InfoCommand.ExecuteReader();

            while (InfoReader.Read())
            {
                IPlayer = new StrayPlayer(Player.userID);
            }

            InfoReader.Dispose();
            InfoCommand.Dispose();

            OnlinePlayers.Add(Player, IPlayer);
        }

        void LoadTaxContainer()
        {
            foreach (StorageContainer Cont in StorageContainer.FindObjectsOfType<StorageContainer>())
            {
                Vector3 ContPosition = Cont.transform.position;
                if (ContPosition.x == TaxChestX && ContPosition.y == TaxChestY && ContPosition.z == TaxChestZ)
                {
                    Puts("Tax Container instance found: " + Cont.GetEntity().GetInstanceID());
                    TaxContainer = Cont;
                }
            }
        }

        void SaveTaxContainer()
        {
            string TaxCommandText = "UPDATE settings SET tax_chest = '" + TaxChestX + ";" + TaxChestY + ";" + TaxChestZ + "'";
            MySqlCommand TaxCommand = new MySqlCommand(TaxCommandText, Database);
            TaxCommand.ExecuteNonQuery();
            TaxCommand.Dispose();
        }

        private void CreateConfigEntry(string Key, string SubKey, string Value)
        {
            if (Config[Key, SubKey] != null)
                return;

            Config[Key, SubKey] = Value;
        }

        private void LoadServerMessages()
        {
            ServerMessages = new Dictionary<string, string>();

            ServerMessages.Add("StartingInformation", "<color=yellow>Welcome to {0}</color>. If you are new, we run a custom plugin where you can become the server President, tax players, and control the economy. Type /info for more information.");
            ServerMessages.Add("PlayerConnected", "has connected to");
            ServerMessages.Add("PlayerDisconnected", "has disconnected from");
            ServerMessages.Add("PresidentDied", "<color=#ff0000ff>The President has died!</color>");
            ServerMessages.Add("PresidentMurdered", "<color=#ff0000ff>The President has been murdered by {0}, who is now the President.</color>");
            ServerMessages.Add("RealmRenamed", "The realm has been renamed to <color=#008080ff>{0}</color>");
            ServerMessages.Add("DefaultRealm", "The land of the Free");
            ServerMessages.Add("OnlinePlayers", "Online players ({0}):");
            ServerMessages.Add("PrivateError", "is either offline or you typed the name wrong.");
            ServerMessages.Add("PrivateFrom", "PM from");
            ServerMessages.Add("PrivateTo", "PM sent to");
            ServerMessages.Add("PresidentError", "You need to be the President to do that!");
            ServerMessages.Add("SettingNewTaxChest", "You are now setting the new tax chest. Hit a storage box to make that the tax chest.");
            ServerMessages.Add("NotSettingNewTaxChest", "You are no longer setting the tax chest.");
            ServerMessages.Add("SetNewTaxChest", "You have set the new tax chest.");
            ServerMessages.Add("ClaimPresident", "Nobody! /claimpresident to become President!");
            ServerMessages.Add("IsNowPresident", "is now the President!");
            ServerMessages.Add("InfoPresident", "President");
            ServerMessages.Add("InfoRealmName", "Realm Name");
            ServerMessages.Add("InfoTaxLevel", "Tax level");
            ServerMessages.Add("UpdateTaxMessage", "President {0} has set Tax to {1}%");

            lang.RegisterMessages(ServerMessages, this);
        }
    }
}