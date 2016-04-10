using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using Newtonsoft.Json;

using Oxide.Core;
using Oxide.Core.Configuration;

using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Economics", "Nogrod", "2.0.5")]
    class Economics : RustPlugin
    {
        private ConfigData configData;
        private DynamicConfigFile data;
        private Dictionary<ulong, double> economicsData;
        private bool changed;

        class ConfigData
        {
            public int Admin_Auth_LvL { get; set; }
            public bool CleanBase { get; set; }
            public double StartMoney { get; set; }
            public double Transfer_Fee { get; set; }
            public Dictionary<string, string> Commands { get; set; } = new Dictionary<string, string>();
        }

        protected override void LoadDefaultConfig()
        {
            var config = new ConfigData
            {
                StartMoney = 1000,
                Admin_Auth_LvL = 2,
                Transfer_Fee = 0.01f,
                CleanBase = true,
                Commands = new Dictionary<string, string>
                {
                    {"Balance", "balance"},
                    {"Deposit", "deposit"},
                    {"SetMoney", "setmoney"},
                    {"Transfer", "transfer"},
                    {"Withdraw", "withdraw"}
                }
            };
            Config.WriteObject(config, true);
        }

        private void Init()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                {"ChatName", "[Economy]"},
                {"NoPermission", "No Permission!"},
                {"NoPlayer", "No Player Found!"},
                {"New_Player_Balance", "New player balance: {0:C}"},
                {"Syntax_Error", "Syntax Error! /{0} <name/steamid> <money>"},
                {"Withdraw_Error", "'{0}' has not enough money!"},
                {"My_Balance", "Your Balance: {0:C}"},
                {"Received", "You received: {0:C}"},
                {"Lost", "You lost: {0:C}"},
                {"Balance", "Player Balance: {0:C}"},
                {"Transfer_Money_Error", "You do not have enough money!"},
                {"Transfer_Negative_Error", "Money can not be negative!"},
                {"Transfer_Error", "You can not transfer money yourself!"},
                {"Transfer_Success", "You have successfully transferred money to '{0}'!"},
                {"Transfer_Success_To", "'{0}' has transferred money to you! Check your balance '/balance'!"},
                {"Save_Success", "Economics data saved!"},
                {"Help1", "/balance - check your balance"},
                {"Help2", "/transfer <name> <money> - transfer [money] to [name] player for small fee"}
            }, this);

            data = Interface.Oxide.DataFileSystem.GetFile(nameof(Economics));
            data.Settings.Converters = new List<JsonConverter> { new RoundingJsonConverter(4) };

            try
            {
                configData = Config.ReadObject<ConfigData>();
                if (Config["Messages"] != null)
                    Config.WriteObject(configData, true);
            }
            catch
            {
                Config.Clear();
                LoadDefaultConfig();
                configData = Config.ReadObject<ConfigData>();
            }
            try
            {
                economicsData = data.ReadObject<Dictionary<ulong, double>>();
            }
            catch
            {
                economicsData = new Dictionary<ulong, double>();
                try
                {
                    var temp = data.ReadObject<Dictionary<ulong, int[]>>();
                    foreach (var intse in temp)
                    {
                        if (intse.Value.Length > 0 && !economicsData.ContainsKey(intse.Key))
                        {
                            economicsData.Add(intse.Key, intse.Value[0]);
                        }
                    }
                    changed = true;
                } catch { }
            }
            if (configData.CleanBase)
            {
                var players = economicsData.Keys.ToArray();
                foreach (var p in players)
                {
                    if (economicsData[p] == configData.StartMoney)
                        economicsData.Remove(p);
                }
                if (players.Length != economicsData.Count)
                    changed = true;
            }
            SaveEconomics();
            cmd.AddChatCommand(configData.Commands["Balance"], this, cmdBalance);
            cmd.AddChatCommand(configData.Commands["Deposit"], this, cmdDeposit);
            cmd.AddChatCommand(configData.Commands["SetMoney"], this, cmdSetMoney);
            cmd.AddChatCommand(configData.Commands["Transfer"], this, cmdTransfer);
            cmd.AddChatCommand(configData.Commands["Withdraw"], this, cmdWithdraw);
        }

        private void OnServerSave()
        {
            SaveEconomics();
        }

        private void Unload()
        {
            SaveEconomics();
        }

        private void SaveEconomics()
        {
            if (economicsData == null || !changed) return;
            data.WriteObject(economicsData);
        }

        private void SetS(string playerIdS, double money)
        {
            if (string.IsNullOrEmpty(playerIdS)) return;
            var playerId = Convert.ToUInt64(playerIdS);
            Set(playerId, money);
        }

        private void Set(ulong playerId, double money)
        {
            economicsData[playerId] = money >= 0 ? money : 0;
            changed = true;
        }

        private void DepositS(string playerIdS, double money)
        {
            if (string.IsNullOrEmpty(playerIdS)) return;
            var playerId = Convert.ToUInt64(playerIdS);
            Deposit(playerId, money);
        }

        private void Deposit(ulong playerId, double money)
        {
            if (money < 0) return;
            money += GetPlayerMoney(playerId);
            Set(playerId, money >= 0 ? money : double.MaxValue);
        }

        private bool WithdrawS(string playerIdS, double money)
        {
            if (string.IsNullOrEmpty(playerIdS)) return false;
            var playerId = Convert.ToUInt64(playerIdS);
            return Withdraw(playerId, money);
        }

        private bool Withdraw(ulong playerId, double money)
        {
            if (money < 0) return false;
            var playerMoney = GetPlayerMoney(playerId);
            if (playerMoney >= money)
            {
                Set(playerId, playerMoney - money);
                return true;
            }
            return false;
        }

        private bool TransferS(string playerIdS, string targetIdS, double money)
        {
            if (string.IsNullOrEmpty(playerIdS) || string.IsNullOrEmpty(targetIdS)) return false;
            var playerId = Convert.ToUInt64(playerIdS);
            var targetId = Convert.ToUInt64(targetIdS);
            return Transfer(playerId, targetId, money);
        }

        private bool Transfer(ulong playerId, ulong targetId, double money)
        {
            if (Withdraw(playerId, money))
            {
                Deposit(targetId, money);
                return true;
            }
            return false;
        }

        private double GetPlayerMoneyS(string playerIdS)
        {
            if (string.IsNullOrEmpty(playerIdS)) return 0;
            var playerId = Convert.ToUInt64(playerIdS);
            return GetPlayerMoney(playerId);
        }

        private double GetPlayerMoney(ulong playerId)
        {
            double playerData;
            return !economicsData.TryGetValue(playerId, out playerData) ? configData.StartMoney : playerData;
        }

        private void cmdTransfer(BasePlayer player, string command, string[] args)
        {
            if (args == null || args.Length <= 1)
            {
                PrintMessage(player, "Syntax_Error", "transfer");
                return;
            }
            var targetPlayer = FindPlayer(args[0]);
            if (targetPlayer == null)
            {
                PrintMessage(player, "NoPlayer");
                return;
            }
            double money;
            double.TryParse(args[1], out money);
            if (money <= 0)
            {
                PrintMessage(player, "Transfer_Negative_Error");
                return;
            }
            if (targetPlayer == player)
            {
                PrintMessage(player, "Transfer_Error");
                return;
            }
            if (!Withdraw(player.userID, money))
            {
                PrintMessage(player, "Transfer_Money_Error");
                return;
            }
            Deposit(targetPlayer.userID, money * (1 - configData.Transfer_Fee));
            PrintMessage(player, "Transfer_Success", targetPlayer.displayName);
            PrintMessage(targetPlayer, "Transfer_Success_To", player.displayName);
        }

        private void cmdBalance(BasePlayer player, string command, string[] args)
        {
            if (args != null && args.Length > 0)
            {
                if (!HasAccess(player))
                {
                    PrintMessage(player, "NoPermission");
                    return;
                }
                var targetPlayer = FindPlayer(args[0]);
                if (targetPlayer == null)
                {
                    PrintMessage(player, "NoPlayer");
                    return;
                }
                PrintMessage(player, "Balance", GetPlayerMoney(targetPlayer.userID));
                return;
            }
            PrintMessage(player, "My_Balance", GetPlayerMoney(player.userID));
        }

        private void cmdSetMoney(BasePlayer player, string command, string[] args)
        {
            if (!HasAccess(player))
            {
                PrintMessage(player, "NoPermission");
                return;
            }
            if (args == null || args.Length <= 1)
            {
                PrintMessage(player, "Syntax_Error", "setmoney");
                return;
            }
            var targetPlayer = FindPlayer(args[0]);
            if (targetPlayer == null)
            {
                PrintMessage(player, "NoPlayer");
                return;
            }
            double money;
            double.TryParse(args[1], out money);
            if (money < 0)
            {
                PrintMessage(player, "Transfer_Negative_Error");
                return;
            }
            Set(targetPlayer.userID, money);
            PrintMessage(player, "New_Player_Balance", GetPlayerMoney(targetPlayer.userID));
        }

        private void cmdDeposit(BasePlayer player, string command, string[] args)
        {
            if (!HasAccess(player))
            {
                PrintMessage(player, "NoPermission");
                return;
            }
            if (args == null || args.Length <= 1)
            {
                PrintMessage(player, "Syntax_Error", "deposit");
                return;
            }
            var targetPlayer = FindPlayer(args[0]);
            if (targetPlayer == null)
            {
                PrintMessage(player, "NoPlayer");
                return;
            }
            double money;
            double.TryParse(args[1], out money);
            if (money <= 0)
            {
                PrintMessage(player, "Transfer_Negative_Error");
                return;
            }
            Deposit(targetPlayer.userID, money);
            PrintMessage(player, "New_Player_Balance", GetPlayerMoney(targetPlayer.userID));
        }

        private void cmdWithdraw(BasePlayer player, string command, string[] args)
        {
            if (!HasAccess(player))
            {
                PrintMessage(player, "NoPermission");
                return;
            }
            if (args == null || args.Length <= 1)
            {
                PrintMessage(player, "Syntax_Error", "withdraw");
                return;
            }
            var targetPlayer = FindPlayer(args[0]);
            if (targetPlayer == null)
            {
                PrintMessage(player, "NoPlayer");
                return;
            }
            double money;
            double.TryParse(args[1], out money);
            if (money <= 0)
            {
                PrintMessage(player, "Transfer_Negative_Error");
                return;
            }
            if (Withdraw(targetPlayer.userID, money))
                PrintMessage(player, "New_Player_Balance", GetPlayerMoney(targetPlayer.userID));
            else
                PrintMessage(player, "Withdraw_Error", targetPlayer.displayName);
        }

        [ConsoleCommand("eco.c")]
        private void ccmdEco(ConsoleSystem.Arg arg)
        {
            if (!arg.HasArgs())
            {
                arg.ReplyWith("Economy Commands: 'eco.c deposit', 'eco.c save','eco.c balance', 'eco.c withdraw', 'eco.c setmoney', 'eco.c wipe'");
                return;
            }
            var player = arg.Player();
            if (player != null && !HasAccess(player))
            {
                arg.ReplyWith("No permission!");
                return;
            }
            var cmdArg = arg.GetString(0).ToLower();
            switch (cmdArg)
            {
                case "save":
                    changed = true;
                    SaveEconomics();
                    arg.ReplyWith("Economics data saved!");
                    break;
                case "wipe":
                    economicsData = new Dictionary<ulong, double>();
                    changed = true;
                    SaveEconomics();
                    arg.ReplyWith("Economics data wiped!");
                    break;
                case "deposit":
                case "balance":
                case "withdraw":
                case "setmoney":
                    var target = FindPlayer(arg.GetString(1));
                    if (target == null)
                    {
                        arg.ReplyWith($"No user with steam/name: '{arg.GetString(1)}'!");
                        return;
                    }
                    if (cmdArg.Equals("balance"))
                    {
                        arg.ReplyWith($"Balance({target.displayName}) = {GetPlayerMoney(target.userID)}");
                        return;
                    }
                    double money = arg.GetFloat(2, -1f);
                    if (money < 0) money = arg.GetUInt64(2, 0);
                    if (money >= 0)
                    {
                        if (cmdArg.Equals("setmoney"))
                        {
                            Set(target.userID, money);
                            arg.ReplyWith($"(SetMoney) New '{target.displayName}' balance: {GetPlayerMoney(target.userID)}");
                            PrintMessage(target, "My_Balance", GetPlayerMoney(target.userID));
                        }
                        else if (cmdArg.Equals("deposit"))
                        {
                            Deposit(target.userID, money);
                            arg.ReplyWith($"(Deposit) New '{target.displayName}' balance: {GetPlayerMoney(target.userID)}");
                            PrintMessage(target, "Received", money);
                        }
                        else if (Withdraw(target.userID, money))
                        {
                            arg.ReplyWith($"(Withdraw) New '{target.displayName}' balance: {GetPlayerMoney(target.userID)}");
                            PrintMessage(target, "Lost", money);
                        }
                        else
                            arg.ReplyWith("This user haven't enough money!");
                    }
                    else
                        arg.ReplyWith($"Syntax Error! (eco.c {cmdArg} <steam/name> <money>)");
                    break;
                default:
                    arg.ReplyWith("Economy Commands: 'eco.c deposit', 'eco.c save','eco.c balance', 'eco.c withdraw', 'eco.c setmoney'");
                    break;
            }
        }

        private bool HasAccess(BasePlayer player)
        {
            return player.net?.connection?.authLevel >= configData.Admin_Auth_LvL;
        }

        private void SendHelpText(BasePlayer player)
        {
            PrintMessage(player, "Help1");
            PrintMessage(player, "Help2");
        }

        private void PrintMessage(BasePlayer player, string msgId, params object[] args)
        {
            PrintToChat(player, lang.GetMessage(msgId, this, player.UserIDString), args);
        }

        private static BasePlayer FindPlayer(string nameOrIdOrIp)
        {
            foreach (var activePlayer in BasePlayer.activePlayerList)
            {
                if (activePlayer.userID.ToString() == nameOrIdOrIp)
                    return activePlayer;
                if (activePlayer.displayName.Contains(nameOrIdOrIp, CompareOptions.OrdinalIgnoreCase))
                    return activePlayer;
                if (activePlayer.net?.connection != null && activePlayer.net.connection.ipaddress == nameOrIdOrIp)
                    return activePlayer;
            }
            foreach (var sleepingPlayer in BasePlayer.sleepingPlayerList)
            {
                if (sleepingPlayer.userID.ToString() == nameOrIdOrIp)
                    return sleepingPlayer;
                if (sleepingPlayer.displayName.Contains(nameOrIdOrIp, CompareOptions.OrdinalIgnoreCase))
                    return sleepingPlayer;
            }
            return null;
        }

        public class RoundingJsonConverter : JsonConverter
        {
            private readonly int precision;
            private readonly MidpointRounding rounding;

            public RoundingJsonConverter()
                : this(2)
            {
            }

            public RoundingJsonConverter(int precision) : this(precision, MidpointRounding.AwayFromZero)
            {
                this.precision = precision;
            }

            public RoundingJsonConverter(int precision, MidpointRounding rounding)
            {
                this.precision = precision;
                this.rounding = rounding;
            }

            public override bool CanRead => false;

            public override bool CanConvert(Type objectType)
            {
                //return objectType == typeof(Dictionary<ulong, double>);
                return objectType == typeof(double);
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                throw new NotImplementedException();
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                /*var dict = (Dictionary<ulong, double>) value;
                writer.WriteStartObject();
                foreach (var d in dict)
                {
                    writer.WritePropertyName(d.Key.ToString());
                    writer.WriteValue(Math.Round(d.Value, precision, rounding));
                }
                writer.WriteEndObject();*/
                writer.WriteValue(Math.Round((double)value, precision, rounding));
            }
        }
    }
}
