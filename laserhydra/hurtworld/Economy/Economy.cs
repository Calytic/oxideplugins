//Reference: UnityEngine.UI
using Oxide.Game.Hurtworld.Libraries;
using System.Collections.Generic;
using System.Linq;
using Oxide.Core;
using System;

namespace Oxide.Plugins
{
    [Info("Economy", "LaserHydra", "1.0.0", ResourceId = 0)]
    [Description("Adds an economy system to the game")]
    class Economy : HurtworldPlugin
    {
        Dictionary<string, MoneyAccount> accounts = new Dictionary<string, MoneyAccount>();
        
        string symbol = "$";
        string command = "money";
        Command commands = Interface.Oxide.GetLibrary<Command>();

        ////////////////////////////////////////
        ///     Plugin Hooks
        ////////////////////////////////////////

        void Loaded()
        {
#if !HURTWORLD
            throw new NotSupportedException("This plugin or the version of this plugin does not support this game!");
#endif

            LoadData();
            LoadConfig();
            LoadMessages();
            
            command = GetConfig("money", "Settings", "Command");

            foreach(PlayerSession player in GameManager.Instance.GetSessions().Values)
                CheckForAccount(player);

            commands.AddChatCommand(command, this, "cmdMoney");
            RegisterPerm("admin");
        }

        void OnPlayerConnected(PlayerSession player)
        {
            CheckForAccount(player);
        }

        ////////////////////////////////////////
        ///     Config, Data & Message Loading
        ////////////////////////////////////////

        void LoadConfig()
        {
            SetConfig("Settings", "Default Amount", 100.00D);
            SetConfig("Settings", "Command", "money");

            SaveConfig();
        }

        void LoadData()
        {
            accounts = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<string, MoneyAccount>>("Economy_Data");
        }

        void SaveData()
        {
            Interface.Oxide.DataFileSystem.WriteObject("Economy_Data", accounts);
        }

        void LoadMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                {"No Permission", "You don't have permission to use this command."},
                {"Invalid Amount Argument", "Invalid amount argument! Amount must be a number!"},
                {"Too Low Amount", "The amount must not be below 0!"},
                {"Not Enough Money", "You do not have enough money to do that!"},
                {"Balance", "Your Balance: {balance}$"},
                {"Admin Balance", "{player}'s Balance: {balance}$"},
                {"Command Description - Balance", "show your balance"},
                {"Command Description - Admin Balance", "show balance of a player"},
                {"Command Description - Transfer", "transfer money to another player"},
                {"Command Description - Admin Transfer", "transfer money from one player to another"},
                {"Command Description - Add", "give money to a player"},
                {"Command Description - Substract", "substract money from a player"},
                {"Admin Transfer - To Admin", "{amount}$ from {player} was given to {target}."},
                {"Admin Transfer - To Player", "{amount}$ from {player} was given to you by {admin}."},
                {"Admin Transfer - To Target", "{amount}$ from you was given to {target} by {admin}."},
                {"Transfer - To Player", "{player} has transfered {amount}$ to you."},
                {"Transfer - To Target", "You have transfered {amount}$ to {target}."},
                {"Add - To Admin", "You have added {amount}$ to {target}'s account."},
                {"Add - To Target", "{admin} has added {amount}$ to your account."},
                {"Substract - To Admin", "You have substracted {amount}$ from {target}'s account."},
                {"Substract - To Target", "{admin} has substracted {amount}$ from your account."}
            }, this);
        }

        protected override void LoadDefaultConfig() => PrintWarning("Generating new config file...");

        ////////////////////////////////////////
        ///     Commands
        ////////////////////////////////////////

        void cmdMoney(PlayerSession player, string cmd, string[] args)
        {
            CheckForAccount(player);
            if(args.Length == 0)
            {
                if(HasPerm(player.SteamId, "admin"))
                    SendChatMessage(player, $"<color=#C4FF00>/{command} balance</color> {GetMsg("Command Description - Balance", player.SteamId)}" +
                        $"{Environment.NewLine}<color=#C4FF00>/{command} balance <player> </color> {GetMsg("Command Description - Admin Balance", player.SteamId)}" +
                        $"{Environment.NewLine}<color=#C4FF00>/{command} transfer <player> <amount></color> {GetMsg("Command Description - Transfer", player.SteamId)}" +
                        $"{Environment.NewLine}<color=#C4FF00>/{command} transfer <player> <target> <amount></color> {GetMsg("Command Description - Admin Transfer", player.SteamId)}" +
                        $"{Environment.NewLine}<color=#C4FF00>/{command} add <player> <amount></color> {GetMsg("Command Description - Add", player.SteamId)}" +
                        $"{Environment.NewLine}<color=#C4FF00>/{command} substract <player> <amount></color> {GetMsg("Command Description - Substract", player.SteamId)}");
                else
                    SendChatMessage(player, $"<color=#C4FF00>/{command} balance</color> {GetMsg("Command Description - Balance", player.SteamId)}" +
                        $"{Environment.NewLine}<color=#C4FF00>/{command} transfer <player> <amount></color> {GetMsg("Command Description - Transfer", player.SteamId)}");

                return;
            }

            switch(args[0].ToString())
            {
                case "balance":
                    if(args.Length == 2)
                    {
                        if(!HasPerm(player.SteamId, "admin"))
                        {
                            SendChatMessage(player, $"Syntax: <color=#C4FF00>/{command} balance</color> {GetMsg("Command Description - Balance", player.SteamId)}");
                            return;
                        }

                        PlayerSession balanceTarget = GetPlayer(args[1], player);
                        
                        if(balanceTarget != null)
                            SendChatMessage(player, GetMsg("Admin Balance", player.SteamId).Replace("{player}", balanceTarget.Name).Replace("{balance}", GetBalance(player).ToString()));
                    }
                    else
                        SendChatMessage(player, GetMsg("Balance", player.SteamId).Replace("{balance}", GetBalance(player).ToString()));

                    break;

                case "transfer":
                    if(args.Length == 4)
                    {
                        if(!HasPerm(player.SteamId, "admin"))
                        {
                            SendChatMessage(player, $"Syntax: <color=#C4FF00>/{command} transfer <player> <amount></color> {GetMsg("Command Description - Transfer", player.SteamId)}");
                            return;
                        }

                        PlayerSession transferPlayer1 = GetPlayer(args[1], player);
                        PlayerSession transferPlayer2 = GetPlayer(args[2], player);
                        
                        if(transferPlayer1 == null || transferPlayer2 == null)
                            return;

                        int admintransferAmount = 0;

                        try
                        {
                            admintransferAmount = Convert.ToInt32(args[3]);
                        }
                        catch(Exception)
                        {
                            SendChatMessage(player, GetMsg("Invalid Amount Argument", player.SteamId));
                            return;
                        }

                        TransferMoney(transferPlayer1, transferPlayer2, admintransferAmount);

                        SendChatMessage(player, GetMsg("Admin Transfer - To Admin", player.SteamId).Replace("{player}", transferPlayer1.Name).Replace("{target}", transferPlayer2.Name).Replace("{admin}", player.Name).Replace("{amount}", admintransferAmount.ToString()));
                        SendChatMessage(transferPlayer1, GetMsg("Admin Transfer - To Player", player.SteamId).Replace("{player}", transferPlayer1.Name).Replace("{target}", transferPlayer2.Name).Replace("{admin}", player.Name).Replace("{amount}", admintransferAmount.ToString()));
                        SendChatMessage(transferPlayer2, GetMsg("Admin Transfer - To Target", player.SteamId).Replace("{player}", transferPlayer1.Name).Replace("{target}", transferPlayer2.Name).Replace("{admin}", player.Name).Replace("{amount}", admintransferAmount.ToString()));
                    }
                    else if(args.Length == 3)
                    {
                        PlayerSession transferTarget = GetPlayer(args[1], player);

                        if(transferTarget == null)
                            return;

                        int transferAmount = 0;

                        try
                        {
                            transferAmount = Convert.ToInt32(args[2]);
                        }
                        catch(Exception)
                        {
                            SendChatMessage(player, GetMsg("Invalid Amount Argument", player.SteamId));
                            return;
                        }

                        if(!GetMoneyAccount(player).CanPay(transferAmount))
                        {
                            SendChatMessage(player, GetMsg("Not Enough Money", player.SteamId));
                            return;
                        }

                        TransferMoney(player, transferTarget, transferAmount);

                        SendChatMessage(player, GetMsg("Transfer - To Player", player.SteamId).Replace("{player}", player.Name).Replace("{target}", transferTarget.Name).Replace("{amount}", transferAmount.ToString()));
                        SendChatMessage(transferTarget, GetMsg("Transfer - To Target", player.SteamId).Replace("{player}", player.Name).Replace("{target}", transferTarget.Name).Replace("{amount}", transferAmount.ToString()));
                    }
                    else
                        SendChatMessage(player, $"Syntax: <color=#C4FF00>/{command} transfer <player> <amount></color> {GetMsg("Command Description - Transfer", player.SteamId)}");
                    break;

                case "add":
                    if(!HasPerm(player.SteamId, "admin"))
                        goto default;

                    if(args.Length != 3)
                    {
                        SendChatMessage(player, $"Syntax: {Environment.NewLine}<color=#C4FF00>/{command} add <player> <amount></color> {GetMsg("Command Description - Add", player.SteamId)}");
                        return;
                    }

                    PlayerSession addTarget = GetPlayer(args[1], player);

                    if(addTarget == null)
                        return;

                    int addAmount = 0;

                    try
                    {
                        addAmount = Convert.ToInt32(args[2]);
                    }
                    catch(Exception)
                    {
                        SendChatMessage(player, GetMsg("Invalid Amount Argument", player.SteamId));
                        return;
                    }

                    if(addAmount < 0)
                    {
                        SendChatMessage(player, GetMsg("Too Low Amount", player.SteamId));
                        return;
                    }

                    AddMoney(addTarget, addAmount);

                    SendChatMessage(player, GetMsg("Add - To Admin", player.SteamId).Replace("{admin}", player.Name).Replace("{target}", addTarget.Name).Replace("{amount}", addAmount.ToString()));
                    SendChatMessage(addTarget, GetMsg("Add - To Target", player.SteamId).Replace("{admin}", player.Name).Replace("{target}", addTarget.Name).Replace("{amount}", addAmount.ToString()));
                        
                    break;

                case "substract":
                    if(!HasPerm(player.SteamId, "admin"))
                        goto default;

                    if(args.Length != 3)
                    {
                        SendChatMessage(player, $"Syntax: {Environment.NewLine}<color=#C4FF00>/{command} substract <player> <amount></color> {GetMsg("Command Description - Substract", player.SteamId)}");
                        return;
                    }

                    PlayerSession substractTarget = GetPlayer(args[1], player);

                    if(substractTarget == null)
                        return;

                    int substractAmount = 0;

                    try
                    {
                        substractAmount = Convert.ToInt32(args[2]);
                    }
                    catch(Exception)
                    {
                        SendChatMessage(player, GetMsg("Invalid Amount Argument", player.SteamId));
                        return;
                    }

                    if(substractAmount < 0)
                    {
                        SendChatMessage(player, GetMsg("Too Low Amount", player.SteamId));
                        return;
                    }

                    SubstractMoney(substractTarget, substractAmount);

                    SendChatMessage(player, GetMsg("Substract - To Admin", player.SteamId).Replace("{admin}", player.Name).Replace("{target}", substractTarget.Name).Replace("{amount}", substractAmount.ToString()));
                    SendChatMessage(substractTarget, GetMsg("Substract - To Target", player.SteamId).Replace("{admin}", player.Name).Replace("{target}", substractTarget.Name).Replace("{amount}", substractAmount.ToString()));

                    break;

                default:
                    SendChatMessage(player, $"<color=#C4FF00>/{command} balance</color> {GetMsg("Command Description - Balance", player.SteamId)}" +
                        $"{Environment.NewLine}<color=#C4FF00>/{command} transfer <player> <amount></color> {GetMsg("Command Description - Transfer", player.SteamId)}");
                    break;
            }
        }

        ////////////////////////////////////////
        ///     Subject Related
        ////////////////////////////////////////

        class MoneyAccount
        {
            public double money = 0f;

            internal MoneyAccount()
            {
            }

            internal MoneyAccount(double amount)
            {
                money = Round(amount);
            }

            internal float Round(double value)
            {
                return (float)Math.Round(value, 2);
            }

            internal void AddMoney(double amount)
            {
                money += Round(amount);
            }

            internal void SubstractMoney(double amount)
            {
                money -= Round(amount);
            }

            internal bool CanPay(double amount)
            {
                if(money - Round(amount) < 0)
                    return false;

                return true;
            }
        }

        MoneyAccount GetMoneyAccount(PlayerSession player)
        {
            CheckForAccount(player);

            return accounts[player.SteamId.ToString()];
        }

        //////////////////////////// API Methods /////////////////////////////

        //  Check if player has account: if not, create one
        void CheckForAccount(PlayerSession player)
        {
            if(!accounts.ContainsKey(player.SteamId.ToString()))
            {
                PrintWarning($"Adding Money Account for {player.Name}");
                accounts.Add(player.SteamId.ToString(), new MoneyAccount(GetConfig(100.00D, "Settings", "Default Amount")));
                SaveData();
            }
        }

        //  Transfer money from one player to another 
        void TransferMoney(PlayerSession player, PlayerSession target, double amount)
        {
            GetMoneyAccount(player).SubstractMoney(amount);
            GetMoneyAccount(target).AddMoney(amount);

            SaveData();
        }

        //  Add money to a players account
        void AddMoney(PlayerSession player, double amount)
        {
            GetMoneyAccount(player).AddMoney(amount);

            SaveData();
        }

        //  Substract money from a players account
        void SubstractMoney(PlayerSession player, double amount)
        {
            GetMoneyAccount(player).SubstractMoney(amount);

            SaveData();
        }

        //  Get the amount of money a player has
        double GetBalance(PlayerSession player)
        {
            return GetMoneyAccount(player).money;
        }

        //////////////////////////////////////////////////////////////////////


        ////////////////////////////////////////
        ///     Player Finding
        ////////////////////////////////////////

        PlayerSession GetPlayer(string searchedPlayer, PlayerSession player)
        {
            foreach (PlayerSession current in GameManager.Instance.GetSessions().Values)
                if (current != null && current.Name != null && current.IsLoaded && current.Name.ToLower() == searchedPlayer)
                    return current;

            List<PlayerSession> foundPlayers =
                (from current in GameManager.Instance.GetSessions().Values
                 where current != null && current.Name != null && current.IsLoaded && current.Name.ToLower().Contains(searchedPlayer.ToLower())
                 select current).ToList();

            switch (foundPlayers.Count)
            {
                case 0:
                    SendChatMessage(player, "The player can not be found.");
                    break;

                case 1:
                    return foundPlayers[0];

                default:
                    List<string> playerNames = (from current in foundPlayers select current.Name).ToList();
                    string players = ListToString(playerNames, 0, ", ");
                    SendChatMessage(player, "Multiple matching players found: \n" + players);
                    break;
            }

            return null;
        }

        ////////////////////////////////////////
        ///     Converting
        ////////////////////////////////////////

        string ListToString(List<string> list, int first, string seperator)
        {
            return String.Join(seperator, list.Skip(first).ToArray());
        }

        ////////////////////////////////////////
        ///     Config & Message Related
        ////////////////////////////////////////

        void SetConfig(params object[] args)
        {
            List<string> stringArgs = (from arg in args select arg.ToString()).ToList<string>();
            stringArgs.RemoveAt(args.Length - 1);

            if (Config.Get(stringArgs.ToArray()) == null) Config.Set(args);
        }

        T GetConfig<T>(T defaultVal, params object[] args)
        {
            List<string> stringArgs = (from arg in args select arg.ToString()).ToList<string>();
            if(Config.Get(stringArgs.ToArray()) == null)
            {
                PrintError($"The plugin failed to read something from the config: {ListToString(stringArgs, 0, "/")}{Environment.NewLine}Please reload the plugin and see if this message is still showing. If so, please post this into the support thread of this plugin.");
                return defaultVal;
            }

            return (T)Convert.ChangeType(Config.Get(stringArgs.ToArray()), typeof(T));
        }

        string GetMsg(string key, object userID = null)
        {
            return lang.GetMessage(key, this, userID.ToString());
        }

        ////////////////////////////////////////
        ///     Permission Related
        ////////////////////////////////////////

        void RegisterPerm(params string[] permArray)
        {
            string perm = ListToString(permArray.ToList(), 0, ".");

            permission.RegisterPermission($"{PermissionPrefix}.{perm}", this);
        }

        bool HasPerm(object uid, params string[] permArray)
        {
            uid = uid.ToString();
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

        ////////////////////////////////////////
        ///     Chat Handling
        ////////////////////////////////////////

        void BroadcastChat(string prefix, string msg = null) => hurt.BroadcastChat(msg == null ? prefix : "<color=#C4FF00>" + prefix + "</color>: " + msg);
        
        void SendChatMessage(PlayerSession player, string prefix, string msg = null) => hurt.SendChatMessage(player, msg == null ? prefix : "<color=#C4FF00>" + prefix + "</color>: " + msg);
    }
}