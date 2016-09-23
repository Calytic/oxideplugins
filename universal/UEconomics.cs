using System.Collections.Generic;
using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Libraries.Covalence;
using System.Linq;

namespace Oxide.Plugins
{
    [Info("Univeral Economics", "k1lly0u", "0.1.1", ResourceId = 2129)]
    class UEconomics : CovalencePlugin
    {
        #region Fields
        StoredData storedData;
        private DynamicConfigFile data;

        private Dictionary<string, int> moneyCache;
        #endregion

        #region Oxide Hooks
        void Loaded()
        {            
            data = Interface.Oxide.DataFileSystem.GetFile("ueconomics_data");
            lang.RegisterMessages(Messages, this);
            permission.RegisterPermission("ueconomics.admin", this);
            moneyCache = new Dictionary<string, int>();
        }
        void OnServerInitialized()
        {
            LoadVariables();
            LoadData();
            if (configData.ClearDefaults)
                ClearDefaultEntries();
        }
        void OnServerSave() => SaveData();
        void Unload() => SaveData();
        #endregion

        #region Functions
        private void ClearDefaultEntries()
        {
            PrintWarning("Clearing unused money data");
            for (int i = 0; i < moneyCache.Keys.ToArray().Length; i++)
            {
                var entry = moneyCache.Keys.ToArray()[i];
                if (moneyCache[entry] == configData.StartingMoney)
                    moneyCache.Remove(entry);
            } 
        }
        private void CheckUser(string ID)
        {
            if (!moneyCache.ContainsKey(ID))
                moneyCache.Add(ID, configData.StartingMoney);
        }
        private void Deposit(string ID, int amount)
        {
            CheckUser(ID);
            moneyCache[ID] += amount;
        }
        private bool Withdraw(string ID, int amount)
        {
            if (amount <= 0) return false;
            CheckUser(ID);
            int userMoney = GetUserMoney(ID);
            if (userMoney >= amount)
            {
                moneyCache[ID] -= amount;
                return true;
            }
            return false;
        }
        private int GetUserMoney(string ID)
        {            
            int amount;
            CheckUser(ID);
            moneyCache.TryGetValue(ID, out amount);
            return amount;
        }
        private bool IsBanker(IPlayer player) => permission.UserHasPermission(player.Id, "ueconomics.admin") || player.IsAdmin;
        private object GetUser(string name)
        {
            var targets = players.FindPlayers(name);
            if (targets != null)
            {
                if (targets.ToArray().Length > 1)
                    return msg("Multiple players found with that name");
                if (targets.ToArray().Length < 1)
                    return msg("No players found with that name");
                if (targets.ToArray()[0] != null)
                    return targets.ToArray()[0];
            }
            return msg("No players found with that name");
        }
        #endregion

        #region Commands
        [Command("eco")]
        void cmdEco(IPlayer player, string command, string[] args)
        {
            if (args == null || args.Length == 0)
            {
                player.Reply($"{Title}  v {Version}");
                player.Reply(msg("/eco balance - Check your account balance", player.Id));
                player.Reply(msg("/eco transfer <partialname> <amount> - Transfer funds to another player", player.Id));
                if (IsBanker(player))
                {
                    player.Reply(msg("/eco balance <partialname> - Check a players account balance", player.Id));
                    player.Reply(msg("/eco deposit <partialname> <amount> - Deposit funds to a players account", player.Id));
                    player.Reply(msg("/eco withdraw <partialname> <amount> - Withdraw funds from a players account", player.Id));
                    player.Reply(msg("/eco set <partialname> <amount> - Set the funds in a players account", player.Id));
                }
                return;
            }
            switch (args[0].ToLower())
            {
                case "balance":
                    if (args.Length >= 2 && IsBanker(player))
                    {
                        var target = GetUser(args[1]);
                        if (target is IPlayer)
                        {
                            int amount = GetUserMoney((target as IPlayer).Id);
                            player.Reply(string.Format(msg("{0}'s Account Balance: {1} {2}(s)", player.Id), (target as IPlayer).Name, amount, configData.CurrencyName));
                        }
                        else player.Reply((string)target);
                    }
                    else
                    {
                        int amount = GetUserMoney(player.Id);
                        player.Reply(string.Format(msg("Account Balance: {0} {1}(s)", player.Id), amount, configData.CurrencyName));
                    }
                    return;
                case "transfer":
                    if (args.Length >= 3)
                    {
                        var target = GetUser(args[1]);
                        if (target is IPlayer)
                        {
                            int sending;
                            if (!int.TryParse(args[2], out sending))
                            {
                                player.Reply(msg("You must enter an amount", player.Id));
                                return;
                            }
                            if (Withdraw(player.Id, sending))
                            {
                                Deposit((target as IPlayer).Id, sending);
                                if ((target as IPlayer).IsConnected)
                                    (target as IPlayer).Reply(string.Format(msg("{0} has deposited {1} {2}(s) into your account", (target as IPlayer).Id), player.Name, sending, configData.CurrencyName));
                                player.Reply(string.Format(msg("You have deposited {0} {1}(s) into {2}'s account", player.Id), sending, configData.CurrencyName, (target as IPlayer).Name));
                            }
                            else player.Reply(msg("You do not have enough funds to transfer", player.Id));
                        }
                        else player.Reply((string)target);
                    }
                    else player.Reply("/eco transfer <partialname> <amount>");
                    return;
                case "deposit":
                    if (IsBanker(player))
                    {
                        if (args.Length >= 3)
                        {
                            var target = GetUser(args[1]);
                            if (target is IPlayer)
                            {
                                int sending = 0;
                                if (!int.TryParse(args[2], out sending))
                                {
                                    player.Reply(msg("You must enter an amount", player.Id));
                                    return;
                                }
                                Deposit((target as IPlayer).Id, sending);
                                if ((target as IPlayer).IsConnected)
                                    (target as IPlayer).Reply(string.Format(msg("{0} has deposited {1} {2}(s) into your account", (target as IPlayer).Id), player.Name, sending, configData.CurrencyName));
                                player.Reply(string.Format(msg("You have deposited {0} {1}(s) into {2}'s account", player.Id), sending, configData.CurrencyName, (target as IPlayer).Name));
                            }
                            else player.Reply((string)target);
                        }
                        else player.Reply("/eco deposit <partialname> <amount>");
                    }
                    return;
                case "withdraw":
                    if (IsBanker(player))
                    {
                        if (args.Length >= 3)
                        {
                            var target = GetUser(args[1]);
                            if (target is IPlayer)
                            {
                                int amount = 0;
                                if (!int.TryParse(args[2], out amount))
                                {
                                    player.Reply(msg("You must enter an amount", player.Id));
                                    return;
                                }
                                if (Withdraw((target as IPlayer).Id, amount))
                                {
                                    if ((target as IPlayer).IsConnected)
                                        (target as IPlayer).Reply(string.Format(msg("{0} has withdrawn {1} {2}(s) from your account", (target as IPlayer).Id), player.Name, amount, configData.CurrencyName));
                                    player.Reply(string.Format(msg("You have withdrawn {0} {1}(s) from {2}'s account", player.Id), amount, configData.CurrencyName, (target as IPlayer).Name));
                                }
                                else player.Reply(string.Format(msg("{0} does not have enough funds to withdraw that amount", player.Id), (target as IPlayer).Name));
                            }
                            else player.Reply((string)target);
                        }
                        else player.Reply("/eco withdraw <partialname> <amount>");
                    }
                    return;
                case "set":
                    if (IsBanker(player))
                    {
                        if (args.Length >= 3)
                        {
                            var target = GetUser(args[1]);
                            if (target is IPlayer)
                            {
                                int amount = 0;
                                if (!int.TryParse(args[2], out amount))
                                {
                                    player.Reply(msg("You must enter an amount", player.Id));
                                    return;
                                }
                                CheckUser((target as IPlayer).Id);
                                moneyCache[(target as IPlayer).Id] = amount;
                                if ((target as IPlayer).IsConnected)
                                    (target as IPlayer).Reply(string.Format(msg("{0} has set your account funds to {1}", (target as IPlayer).Id), player.Name, amount));
                                player.Reply(string.Format(msg("You have set {0}'s account funds to {1}", player.Id), (target as IPlayer).Name, amount));
                                return;
                            }
                            else player.Reply((string)target);
                        }
                        else player.Reply("/eco set <partialname> <amount>");
                    }
                    return;
                default:
                    break;
            }
        }
        #endregion

        #region Config        
        private ConfigData configData;
        class ConfigData
        {
            public bool ClearDefaults { get; set; }
            public int StartingMoney { get; set; }
            public string CurrencyName { get; set; }
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
                ClearDefaults = true,
                CurrencyName = "dollar",
                StartingMoney = 1000
            };
            SaveConfig(config);
        }
        private void LoadConfigVariables() => configData = Config.ReadObject<ConfigData>();
        void SaveConfig(ConfigData config) => Config.WriteObject(config, true);
        #endregion

        #region Data Management
        void SaveData()
        {
            storedData.money = moneyCache;
            data.WriteObject(storedData);
        }
        void LoadData()
        {
            try
            {
                storedData = data.ReadObject<StoredData>();
                moneyCache = storedData.money;
            }
            catch
            {
                storedData = new StoredData();
            }
        }
        class StoredData
        {
            public Dictionary<string, int> money = new Dictionary<string, int>();
        }
        #endregion

        #region Localization
        string msg(string key, string id = null) => lang.GetMessage(key, this, id);

        Dictionary<string, string> Messages = new Dictionary<string, string>
        {
            {"{0} has set your account funds to {1}","{0} has set your account funds to {1}" },
            {"You have set {0}'s account funds to {1}","You have set {0}'s account funds to {1}" },
            {"You must enter an amount", "You must enter an amount"},
            {"{0} does not have enough funds to withdraw that amount", "{0} does not have enough funds to withdraw that amount"},
            {"You have withdrawn {0} {1}(s) from {2}'s account", "You have withdrawn {0} {1}(s) from {2}'s account"},
            {"{0} has withdrawn {1} {2}(s) from your account", "{0} has withdrawn {1} {2}(s) from your account"},
            {"You have deposited {0} {1}(s) into {2}'s account", "You have deposited {0} {1}(s) into {2}'s account"},
            {"{0} has deposited {1} {2}(s) into your account", "{0} has deposited {1} {2}(s) into your account"},
            {"You do not have enough funds to transfer", "You do not have enough funds to transfer"},
            {"Account Balance: {0} {1}(s)", "Account Balance: {0} {1}(s)"},
            {"{0}'s Account Balance: {1} {2}(s)", "{0}'s Account Balance: {1} {2}(s)"},
            {"No players found with that name", "No players found with that name"},
            {"Multiple players found with that name", "Multiple players found with that name"},
            {"/eco balance - Check your account balance", "/eco balance - Check your account balance"},
            {"/eco transfer <partialname> <amount> - Transfer funds to another player", "/eco transfer <partialname> <amount> - Transfer funds to another player"},
            {"/eco balance <partialname> - Check a players account balance", "/eco balance <partialname> - Check a players account balance"},
            {"/eco deposit <partialname> <amount> - Deposit funds to a players account", "/eco deposit <partialname> <amount> - Deposit funds to a players account"},
            {"/eco withdraw <partialname> <amount> - Withdraw funds from a players account", "/eco withdraw <partialname> <amount> - Withdraw funds from a players account"},
            {"/eco set <partialname> <amount> - Set the funds in a players account", "/eco set <partialname> <amount> - Set the funds in a players account"}
        };
        #endregion
    }
}
