using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Libraries;
using Oxide.Core.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Oxide.Ext.MySql;
using System.Web;
using Oxide.Core.Configuration;
using System.Net;
using System.IO;
using Newtonsoft.Json.Linq;

namespace Oxide.Plugins
{
    [Info("EconomyCore", "Jordi", 0.6, ResourceId = 1417)]
    [Description("Adds a simple economy to RUST")]
    public class EconomyCore : RustPlugin
    {
        [PluginReference]
        Plugin DateCore;
        #region HUD
        public static string json = @"[
            {""name"": ""EconomyMsg"",
                ""parent"": ""HUD/Overlay"",
                ""components"":
                [
                    {
                         ""type"":""UnityEngine.UI.Image"",
                         ""color"":""0.1 0.1 0.1 0.7"",
                    },
                    {
                        ""type"":""RectTransform"",
                        ""anchormin"": ""0.020 0.95"",
                        ""anchormax"": ""0.15 0.99""
                    }
                ]
            },
            {
                ""parent"": ""EconomyMsg"",
                ""components"":
                [
                    {
                        ""type"":""UnityEngine.UI.Text"",
                        ""color"":""0.1 0.9 0.1 0.7"",
                        ""text"":""{msg}"",
                        ""fontSize"":15,
                        ""align"": ""MiddleCenter"",
                    },
                    {
                        ""type"":""RectTransform"",
                        ""anchormin"": ""0 0.1"",
                        ""anchormax"": ""1 0.8""
                    }
                ]
            }
        ]
        ";
        public Hash<BasePlayer, Timer> Timers = new Hash<BasePlayer, Timer>();
        public void LoadMsgGui(string Msg, BasePlayer ply)
        {
            
            Game.Rust.Cui.CuiHelper.DestroyUi(ply, "EconomyMsg");
            CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo { connection = ply.net.connection }, null, "AddUI", new Facepunch.ObjectList(json.Replace("{msg}", Msg)));
        }

        public void DestroyGui(BasePlayer player)
        {
            Game.Rust.Cui.CuiHelper.DestroyUi(player, "EconomyMsg");
        }
        #endregion
        static Dictionary<Door, Timer> ActiveTimers = new Dictionary<Door, Timer>();
        static Dictionary<String, Timer> HUDTIMERS = new Dictionary<String, Timer>();
        public ArraySegment<Action<BasePlayer, int>> onreceivemoney = new ArraySegment<Action<BasePlayer, int>>();
        public ArraySegment<Action<BasePlayer, int>> ontakemoney = new ArraySegment<Action<BasePlayer, int>>();
        HashSet<String> wta = new HashSet<String>();
        static readonly MethodInfo UpdateLayerMethod = typeof(BuildingBlock).GetMethod("UpdateLayer", (BindingFlags.Instance | BindingFlags.NonPublic));
        #region [CONFIGDATA]
        protected override void LoadDefaultConfig()
        {
            if (Config["Created"] == null || Config["Created"].Equals(false))
            {
                Config["UpdateTicker"] = 0.5F;
                Config["DebugMode"] = false;
                Config["UseVoteSystem"] = false;
                Config["ApiKeyFromRustServers"] = "API KEY FROM RUST SERVERS HERE";
                Config["onvote"] = 50;
                Config["RustServerListURL"] = "http://rust-servers.net/server/50682/";
                Config["priceperhp"] = 0.1;
                Config["ConnectionAPI"] = false;
                Config["ConnectionAPIKey"] = "Use a random username here";
                Config["ConnectionAPIPassword"] = "Use a random password here";
                Config["EarnAPI"] = true;
                Config["OnAnimalKill"] = 0.5;
                Config["OnBarrelKill"] = 0.2;
                Config["OnPlayerKill"] = 10.0;
                Config["notifyjoin"] = true;
                SaveConfig();
            }
        }
        class StoredData
        {
            public HashSet<String> td = new HashSet<String>();
            public Dictionary<String, Double> munnie = new Dictionary<String, Double>();
            public Dictionary<String, List<List<String>>> dailytransfers = new Dictionary<String, List<List<String>>>();
            public Dictionary<String, List<List<String>>> weeklytransfers = new Dictionary<String, List<List<String>>>();
            public Dictionary<String, List<List<String>>> monthlytransfers = new Dictionary<String, List<List<String>>>();
            public Dictionary<String, List<List<String>>> yearlytransfers = new Dictionary<String, List<List<String>>>();
            public Dictionary<String, Boolean> autorepair = new Dictionary<String, Boolean>();
            public StoredData()
            {
            }

        }
        #endregion
        void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
        {
            try
            {
                if (info.Initiator.ToPlayer().IsValid())
                {
                    if (entity.name.Contains("animals"))
                    {
                        GiveMoney(info.Initiator.ToPlayer(), (Double) Config["OnAnimalKill"], "You killed an animal!");
                    }
                    if (entity.name.Contains("loot-barrel") || entity.name.Contains("loot_barrel") || entity.name.Contains("loot_trash"))
                    {
                        GiveMoney(info.Initiator.ToPlayer(), (Double) Config["OnBarrelKill"], "You broke a loot barrel!");
                    }
                    if (entity.ToPlayer().IsValid())
                    {
                        GiveMoney(info.Initiator.ToPlayer(), (Double) Config["OnPlayerKill"], "You killed a player!");
                    }
                }
            }
            catch
            {
            }
        }
        //VOTE SYSTEM && CONFIG UPLOADER
        private readonly WebRequests webRequests = Interface.GetMod().GetLibrary<WebRequests>("WebRequests");
        public class Votes
        {
            public String name;
            public String address;
            public String port;
            public String month;
            public List<Vote> votes;
        }
        public class Vote
        {
            public String date;
            public int timestamp;
            public String nickname;
            public String steamid;
            public String claimed;
        }
        StoredData storedData = new StoredData();
        Connection connection = null;
        Timer votecheck;
        Timer updateconfig;
        static String localconfigold = "";
        static String siteconfigold = localconfigold;
        private void ExampleGetRequest()
        {
            if ((Boolean) Config["UseVoteSystem"])
            {
                foreach (BasePlayer ply in BasePlayer.activePlayerList)
                {
                    webRequests.EnqueueGet("http://rust-servers.net/api/?object=votes&element=claim&key=" + Config["ApiKeyFromRustServers"] + "&format=json", (code, response) => WebRequestCallback(code, response, ply), this);
                }
            }
        }

        private void WebRequestCallbackreadSite(int code, string response)
        {
            if (response.Equals(localconfigold) == false)
            {
                if (response.Equals("CREATED"))
                {
                    Puts("[ECO]: ShareConfig created on serverside!");
                    return;
                }
                if (response.Equals("ERROR"))
                {
                    Puts("[ECO]: ShareConfig could NOT be read from serverside (you may have an incorrect password)!");
                    return;
                }
                StoredData sd = JsonConvert.DeserializeObject<StoredData>(response);
                Interface.GetMod().DataFileSystem.WriteObject<StoredData>("EconomyCore", sd);
                localconfigold = Interface.GetMod().DataFileSystem.ReadObject<JObject>("EconomyCore").ToString();
                LoadConfigs();
                if ((Boolean) Config["DebugMode"])
                {
                    Puts("[ECO]: Configs downloaded.");
                }
            }
        }
        private void WebRequestCallbackWriteSite(int code, string response)
        {
            siteconfigold = response;
            if ((Boolean)Config["DebugMode"])
            {
                Puts("[ECO]: Configs uploaded.");
            }
        }
        private void WebRequestCallback(int code, string response, BasePlayer ply)
        {
            if (response == null || code != 200)
            {
                Puts("Couldn't get an answer from VoteSys");
                return;
            }
            if (response.Equals("1"))
            {
                    if (ply.isConnected && (ply.IsSleeping() == false))
                    {
                        SendReply(ply, ApplyColor("[ECO]: Thanks for voting! you received: $greenâ¬" + Config["onvote"] + "$r"));
                        rust.BroadcastChat("[ECO]", ApplyColor(ply.displayName + " did vote, he received: $greenâ¬" + Config["onvote"] + "$r Vote too on: " + Config["RustServerListURL"]));
                        GiveMoney(ply, (Double) Config["onvote"]);
                        webRequests.EnqueueGet("http://rust-servers.net/api/?action=post&object=votes&element=claim&key=" +  Config["ApiKeyFromRustServers"] + "&steamid=" + ply.UserIDString, (cod, resp) =>
                        {
                        }, this);
                    }
                }
        }
        // END VOTE SYSTEM

        private void LoadConfigs()
        {
            storedData = Interface.GetMod().DataFileSystem.ReadObject<StoredData>("EconomyCore");
            localconfigold = Interface.GetMod().DataFileSystem.ReadObject<JObject>("EconomyCore").ToString();
        }

        void Init()
        {
            LoadConfigs();
            if (!permission.PermissionExists("eco.give")) permission.RegisterPermission("eco.give", this);
           foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                if (!storedData.munnie.ContainsKey(player.UserIDString))
                {
                    storedData.munnie.Add(player.UserIDString, 1000.00);
                }
                if (storedData.autorepair.ContainsKey(player.UserIDString) == false)
                {
                    storedData.autorepair.Add(player.UserIDString, false);
                }
                Timers.Add(player, timer.Repeat(1, 0, () =>
                {
                    LoadMsgGui("Your money: â¬" + SimpleRound(Money(player)), player);
                }));
            }
           votecheck = timer.Repeat(30, 0, () => ExampleGetRequest());
           Interface.GetMod().DataFileSystem.WriteObject<StoredData>("EconomyCore", storedData);
           updateconfig = timer.Repeat((float) Config["UpdateTicker"], 0, () =>
               {
                   if ((Boolean) Config["ConnectionAPI"])
                   {
                       string CurConf = Interface.GetMod().DataFileSystem.ReadObject<JObject>("EconomyCore").ToString();
                       if (siteconfigold.Equals(CurConf) == false)
                       {
                           webRequests.EnqueueGet("http://jmnet.servegame.com/rust/plugin/api/writeconfig.php?id=" + Config["ConnectionAPIKey"] + "&pass=" + Config["ConnectionAPIPassword"] + "&config=" + CurConf, (code, response) => WebRequestCallbackWriteSite(code, response), this);
                       }
                       webRequests.EnqueueGet("http://jmnet.servegame.com/rust/plugin/api/readconfig.php?id=" + Config["ConnectionAPIKey"] + "&pass=" + Config["ConnectionAPIPassword"], (code, response) => WebRequestCallbackreadSite(code, response), this);
                   }
               });
        }
        void Unload()
        {
            votecheck.Destroy();
            updateconfig.Destroy();
        }
        void OnPluginLoaded(Plugin plug)
        {
            if (plug.Title.Equals("DateCore"))
            {
                DateCore.Call("removeonday", "EconomyOnDay");
                DateCore.Call("removeonweek", "EconomyOnWeek");
                DateCore.Call("removeonmonth", "EconomyOnMonth");
                DateCore.Call("removeonyear", "EconomyOnYear");
                Action<int> onday = (int day) =>
                {
                    Puts("ECONOMY DAY");
                    foreach (string key in storedData.dailytransfers.Keys)
                    {
                        List<List<String>> PaymentList = storedData.dailytransfers[key];
                        foreach (List<String> payment in PaymentList)
                        {
                            BasePlayer Payer = findPlayerbysteamid(key);
                            BasePlayer Receiver = findPlayerbysteamid(payment[0]);
                            double Paying = double.Parse(payment[1]);
                            if (CanPay(Payer, Paying, true))
                            {
                                GiveMoney(Payer, Paying * -1);
                                PrintToChat(Payer, ApplyColor("[ECO]: You payed $greenâ¬" + payment[1] + "$r to " + Receiver.displayName + "! " + payment[2]));
                                PrintToChat(Receiver, ApplyColor("[ECO]: You received $greenâ¬" + payment[1] + "$r from " + Payer.displayName + "! " + payment[2]));
                            }
                        }
                    }
                    Interface.GetMod().DataFileSystem.WriteObject<StoredData>("Economy", storedData);
                };
                DateCore.Call("addonday", onday, "EconomyOnDay");
                Action<int> onweek = (int day) =>
                {
                    Puts("ECONOMY WEEK");
                    foreach (string key in storedData.weeklytransfers.Keys)
                    {
                        List<List<String>> PaymentList = storedData.weeklytransfers[key];
                        foreach (List<String> payment in PaymentList)
                        {
                            BasePlayer Payer = findPlayerbysteamid(key);
                            BasePlayer Receiver = findPlayerbysteamid(payment[0]);
                            double Paying = double.Parse(payment[1]);
                            if (CanPay(Payer, Paying, true))
                            {
                                GiveMoney(Payer, Paying * -1);
                                PrintToChat(Payer, ApplyColor("[ECO]: You payed $greenâ¬" + payment[1] + "$r to " + Receiver.displayName + "! " + payment[2]));
                                PrintToChat(Receiver, ApplyColor("[ECO]: You received $greenâ¬" + payment[1] + "$r from " + Payer.displayName + "! " + payment[2]));
                            }
                        }
                    }
                    Interface.GetMod().DataFileSystem.WriteObject<StoredData>("Economy", storedData);
                };
                DateCore.Call("addonweek", onweek, "EconomyOnWeek");
                Action<int> onmonth = (int day) =>
                {
                    Puts("ECONOMY MONTH");
                    foreach (string key in storedData.monthlytransfers.Keys)
                    {
                        List<List<String>> PaymentList = storedData.monthlytransfers[key];
                        foreach (List<String> payment in PaymentList)
                        {
                            BasePlayer Payer = findPlayerbysteamid(key);
                            BasePlayer Receiver = findPlayerbysteamid(payment[0]);
                            double Paying = double.Parse(payment[1]);
                            if (CanPay(Payer, Paying, true))
                            {
                                GiveMoney(Payer, Paying * -1);
                                PrintToChat(Payer, ApplyColor("[ECO]: You payed $greenâ¬" + payment[1] + "$r to " + Receiver.displayName + "! " + payment[2]));
                                PrintToChat(Receiver, ApplyColor("[ECO]: You received $greenâ¬" + payment[1] + "$r from " + Payer.displayName + "! " + payment[2]));
                            }
                        }
                    }
                    Interface.GetMod().DataFileSystem.WriteObject<StoredData>("Economy", storedData);
                };
                DateCore.Call("addonmonth", onmonth, "EconomyOnMonth");
                Action<int> onyear = (int day) =>
                {
                    Puts("ECONOMY YEAR");
                    foreach (string key in storedData.yearlytransfers.Keys)
                    {
                        List<List<String>> PaymentList = storedData.yearlytransfers[key];
                        foreach (List<String> payment in PaymentList)
                        {
                            BasePlayer Payer = findPlayerbysteamid(key);
                            BasePlayer Receiver = findPlayerbysteamid(payment[0]);
                            double Paying = double.Parse(payment[1]);
                            if (CanPay(Payer, Paying, true))
                            {
                                GiveMoney(Payer, Paying * -1);
                                PrintToChat(Payer, ApplyColor("[ECO]: You payed $greenâ¬" + payment[1] + "$r to " + Receiver.displayName + "! " + payment[2]));
                                PrintToChat(Receiver, ApplyColor("[ECO]: You received $greenâ¬" + payment[1] + "$r from " + Payer.displayName + "! " + payment[2]));
                            }
                        }
                    }
                    Interface.GetMod().DataFileSystem.WriteObject<StoredData>("Economy", storedData);
                };
                DateCore.Call("addonyear", onyear, "EconomyOnYear");
            }
        }
        void OnPlayerInit(BasePlayer player)
        {
            if (!storedData.munnie.ContainsKey(player.UserIDString))
                {
                    storedData.munnie.Add(player.UserIDString, 1000.00);
                }
                if (storedData.autorepair.ContainsKey(player.UserIDString) == false)
                {
                    storedData.autorepair.Add(player.UserIDString, false);
                }
                Interface.GetMod().DataFileSystem.WriteObject<StoredData>("Economy", storedData);
            if ((Boolean) Config["notifyjoin"]){
                rust.BroadcastChat("[Join]", "Welcome: " + player.displayName);
            }
            Timers.Add(player, timer.Repeat(1,0,( )=> {
                LoadMsgGui("Your money: â¬" + SimpleRound(Money(player)), player);
            }));
        }
        void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            if ((Boolean) Config["notifyjoin"])
            {
                rust.BroadcastChat("[Quit]", "Goodbye: " + player.displayName);
            }
            Timers[player].Destroy();
            Timers.Remove(player);
        }
        void OnLoseCondition(Item item, ref float amount)
        {
            try
            {
                LoadConfigs();
                if (storedData.autorepair[item.GetOwnerPlayer().UserIDString].ToString().ToLower().Equals("true"))
                {
                    double repaircost = 0;
                    if (item.condition <= 10)
                    {
                        repaircost = (item.maxCondition - item.condition) * (Double) Config["priceperhp"];
                    }
                    try
                    {
                        if (item.contents.GetSlot(0).hasCondition && item.contents.GetSlot(0).condition <= 10)
                        {
                            repaircost = repaircost + ((item.contents.GetSlot(0).maxCondition - item.contents.GetSlot(0).condition) * (Double)Config["priceperhp"]);
                        }
                    }
                    catch
                    {
                    }
                    try
                    {
                        if (item.contents.GetSlot(1).hasCondition && item.contents.GetSlot(1).condition <= 10)
                        {
                            repaircost = repaircost + ((item.contents.GetSlot(1).maxCondition - item.contents.GetSlot(1).condition) * (Double)Config["priceperhp"]);
                        }
                    }
                    catch
                    {
                    }
                    if (repaircost != 0)
                    {
                        if (CanPay(item.GetOwnerPlayer(), repaircost, true))
                        {
                            GiveMoney(item.GetOwnerPlayer(), -repaircost);
                            if (item.condition <= 10)
                            {
                                item.condition = item.condition + (item.maxCondition - item.condition);
                                PrintToChat(item.GetOwnerPlayer(), "[ECO]: Your " + item.info.displayName.english + " is repaired!");
                            }
                            if (item.contents.GetSlot(0).hasCondition && item.contents.GetSlot(0).condition <= 10)
                            {
                                item.contents.GetSlot(0).condition = item.contents.GetSlot(0).maxCondition;
                                PrintToChat(item.GetOwnerPlayer(), "[ECO]: Your " + item.contents.GetSlot(0).info.displayName.english + " is repaired!");
                            }
                            if (item.contents.GetSlot(1).hasCondition && item.contents.GetSlot(1).condition <= 10)
                            {
                                item.contents.GetSlot(1).condition = item.contents.GetSlot(1).maxCondition;
                                PrintToChat(item.GetOwnerPlayer(), "[ECO]: Your " + item.contents.GetSlot(1).info.displayName.english + " is repaired!");
                            }
                            return;
                        }
                        return;
                    }
                }
                else
                {
                    return;
                }
            }
            catch
            {
                return;
            }
        }
        [ChatCommand("eco")]
        void econ(BasePlayer player, string command, string[] args)
        {
            if (args.Length == 0)
            {
                PrintToChat(player, "[ECO]: Incorrect usage! Use: /eco help");
                return;
            }
            if (args[0].Equals("auto"))
            {
                if (args.Length == 1)
                {
                    PrintToChat(player, "[ECO]: Error use: /eco auto (repair)");
                    return;
                }
                if (args[1].Equals("repair"))
                {
                    if (args.Length == 1)
                    {
                        PrintToChat(player, "[ECO]: Error use: /eco auto repair on/off");
                        return;
                    }
                    if (args[2].Equals("on"))
                    {
                        storedData.autorepair[player.UserIDString] = true;
                        Interface.GetMod().DataFileSystem.WriteObject<StoredData>("EconomyCore", storedData);
                        PrintToChat(player, "[ECO]: You turned auto repair on.");
                        return;
                    }
                    if (args[2].Equals("off"))
                    {
                        storedData.autorepair[player.UserIDString] = false;
                        Interface.GetMod().DataFileSystem.WriteObject<StoredData>("EconomyCore", storedData);
                        PrintToChat(player, "[ECO]: You turned auto repair off.");
                        return;
                    }
                    PrintToChat(player, "[ECO]: Error use: /eco auto repair on/off");
                    return;
                }
                PrintToChat(player, "[ECO]: Error use: /eco auto (repair)");
                return;
            }
            if (args[0].Equals("transfer"))
            {
                if (args.Length == 1)
                {
                    PrintToChat(player, "[ECO]: Error use: add/remove/list");
                    return;
                }
                if (args[1].Equals("add"))
                {
                    if (args.Length == 2)
                    {

                        PrintToChat(player, "[ECO]: Error use: add <day/week/month/year> <player> <amount> <transactionname> <reason>");
                        return;
                    }
                    BasePlayer ply = findPlayer(args[3]);
                    if (ply.Equals(new BasePlayer()))
                    {
                        PrintToChat(player, "[ECO]: Error, Player could not be found!");
                        return;
                    }
                    String reason = "";
                    if (args.Length > 5)
                    {
                        for (int i = 6; i < args.Length; i++)
                        {
                            reason = reason + " " + ApplyColor(args[i]);
                        }
                    }
                    if (args[2].Equals("day"))
                    {
                        List<List<String>> Actions = new List<List<String>>();
                        if (storedData.dailytransfers.ContainsKey(player.UserIDString))
                        {
                            Actions = storedData.dailytransfers[player.UserIDString];
                        }
                        List<String> Transaction = new List<String>();
                        Transaction.Insert(0, ply.UserIDString);
                        Transaction.Insert(1, double.Parse(args[4]).ToString());
                        Transaction.Insert(2, reason);
                        Transaction.Insert(3, args[5]);
                        int actionid = Actions.Count();
                        Actions.Insert(actionid, Transaction);
                        storedData.dailytransfers[player.UserIDString] = Actions;
                        PrintToChat(player, "[ECO]: Daily transaction added! (TransactionID: " + actionid.ToString() + ")");
                        Interface.GetMod().DataFileSystem.WriteObject<StoredData>("EconomyCore", storedData);
                        return;
                    }
                    if (args[2].Equals("week"))
                    {
                        List<List<String>> Actions = new List<List<String>>();
                        if (storedData.weeklytransfers.ContainsKey(player.UserIDString))
                        {
                            Actions = storedData.weeklytransfers[player.UserIDString];
                        }
                        List<String> Transaction = new List<String>();
                        Transaction.Insert(0, ply.UserIDString);
                        Transaction.Insert(1, double.Parse(args[4]).ToString());
                        Transaction.Insert(2, reason);
                        Transaction.Insert(3, args[5]);
                        int actionid = Actions.Count();
                        Actions.Insert(actionid, Transaction);
                        storedData.weeklytransfers[player.UserIDString] = Actions;
                        PrintToChat(player, "[ECO]: Weekly transaction added! (TransactionID: " + actionid.ToString() + ")");
                        Interface.GetMod().DataFileSystem.WriteObject<StoredData>("EconomyCore", storedData);
                        return;
                    }
                    if (args[2].Equals("month"))
                    {
                        List<List<String>> Actions = new List<List<String>>();
                        if (storedData.monthlytransfers.ContainsKey(player.UserIDString))
                        {
                            Actions = storedData.monthlytransfers[player.UserIDString];
                        }
                        List<String> Transaction = new List<String>();
                        Transaction.Insert(0, ply.UserIDString);
                        Transaction.Insert(1, double.Parse(args[4]).ToString());
                        Transaction.Insert(2, reason);
                        Transaction.Insert(3, args[5]);
                        int actionid = Actions.Count();
                        Actions.Insert(actionid, Transaction);
                        storedData.monthlytransfers[player.UserIDString] = Actions;
                        PrintToChat(player, "[ECO]: Monthly transaction added! (TransactionID: " + actionid.ToString() + ")");
                        Interface.GetMod().DataFileSystem.WriteObject<StoredData>("EconomyCore", storedData);
                        return;
                    }
                    if (args[2].Equals("year"))
                    {
                        List<List<String>> Actions = new List<List<String>>();
                        if (storedData.yearlytransfers.ContainsKey(player.UserIDString))
                        {
                            Actions = storedData.yearlytransfers[player.UserIDString];
                        }
                        List<String> Transaction = new List<String>();
                        Transaction.Insert(0, ply.UserIDString);
                        Transaction.Insert(1, double.Parse(args[4]).ToString());
                        Transaction.Insert(2, reason);
                        Transaction.Insert(3, args[5]);
                        int actionid = Actions.Count();
                        Actions.Insert(actionid, Transaction);
                        storedData.yearlytransfers[player.UserIDString] = Actions;
                        PrintToChat(player, "[ECO]: Yearly transaction added! (TransactionID: " + actionid.ToString() + ")");
                        Interface.GetMod().DataFileSystem.WriteObject<StoredData>("EconomyCore", storedData);
                        return;
                    }
                    PrintToChat(player, "[ECO]: Error use: add <day/week/month/year> <player> <amount> <transactionname> <reason>");
                    return;
                }
                if (args[1].Equals("remove"))
                {
                    if ((args.Length == 4) == false)
                    {
                        PrintToChat(player, "[ECO]: Error use: remove <TransactionID> <day/week/month/year>");
                        return;
                    }
                    int actionid = int.Parse(args[2]);
                    if (args[3].Equals("day"))
                    {
                        List<List<String>> Actions = new List<List<String>>();
                        if (storedData.dailytransfers.ContainsKey(player.UserIDString))
                        {
                            Actions = storedData.dailytransfers[player.UserIDString];
                        }
                        if (Actions.Count() - 1 >= actionid)
                        {
                            storedData.dailytransfers[player.UserIDString] = Actions;
                            PrintToChat(player, "[ECO]: Daily transaction removed! (TransactionID: " + actionid.ToString() + ")");
                            Interface.GetMod().DataFileSystem.WriteObject<StoredData>("EconomyCore", storedData);
                        }
                        else
                        {
                            PrintToChat(player, "[ECO]: Error, no transaction found!");
                            return;
                        }
                        return;
                    }
                    if (args[3].Equals("week"))
                    {
                        List<List<String>> Actions = new List<List<String>>();
                        if (storedData.weeklytransfers.ContainsKey(player.UserIDString))
                        {
                            Actions = storedData.weeklytransfers[player.UserIDString];
                        }
                        if (Actions.Count() - 1 >= actionid)
                        {
                            storedData.weeklytransfers[player.UserIDString] = Actions;
                            PrintToChat(player, "[ECO]: Weekly transaction removed! (TransactionID: " + actionid.ToString() + ")");
                            Interface.GetMod().DataFileSystem.WriteObject<StoredData>("EconomyCore", storedData);
                        }
                        else
                        {
                            PrintToChat(player, "[ECO]: Error, no transaction found!");
                            return;
                        }
                        return;
                    }
                    if (args[3].Equals("month"))
                    {
                        List<List<String>> Actions = new List<List<String>>();
                        if (storedData.monthlytransfers.ContainsKey(player.UserIDString))
                        {
                            Actions = storedData.monthlytransfers[player.UserIDString];
                        }
                        if (Actions.Count() - 1 >= actionid)
                        {
                            storedData.monthlytransfers[player.UserIDString] = Actions;
                            PrintToChat(player, "[ECO]: Monthly transaction removed! (TransactionID: " + actionid.ToString() + ")");
                            Interface.GetMod().DataFileSystem.WriteObject<StoredData>("EconomyCore", storedData);
                        }
                        else
                        {
                            PrintToChat(player, "[ECO]: Error, no transaction found!");
                            return;
                        }
                        return;
                    }
                    if (args[3].Equals("year"))
                    {
                        List<List<String>> Actions = new List<List<String>>();
                        if (storedData.yearlytransfers.ContainsKey(player.UserIDString))
                        {
                            Actions = storedData.yearlytransfers[player.UserIDString];
                        }
                        if (Actions.Count() - 1 >= actionid)
                        {
                            storedData.yearlytransfers[player.UserIDString] = Actions;
                            PrintToChat(player, "[ECO]: Yearly transaction removed! (TransactionID: " + actionid.ToString() + ")");
                            Interface.GetMod().DataFileSystem.WriteObject<StoredData>("EconomyCore", storedData);
                        }
                        else
                        {
                            PrintToChat(player, "[ECO]: Error, no transaction found!");
                            return;
                        }
                        return;
                    }
                    return;
                }
                if (args[1].Equals("list"))
                {
                    if ((storedData.dailytransfers.ContainsKey(player.UserIDString) || storedData.weeklytransfers.ContainsKey(player.UserIDString) || storedData.monthlytransfers.ContainsKey(player.UserIDString) || storedData.yearlytransfers.ContainsKey(player.UserIDString)) == false)
                    {
                        PrintToChat(player, "[ECO]: Error, You dont have any auto-transfers!");
                        return;
                    }
                    else
                    {
                        PrintToChat(player, "[ECO]: Your auto-transfers:");
                        if (storedData.dailytransfers.ContainsKey(player.UserIDString))
                        {
                            for (int i = 0; i < storedData.dailytransfers[player.UserIDString].Count; i++)
                            {
                                List<String> transfer = storedData.dailytransfers[player.UserIDString][i];
                                PrintToChat(player, "[Transfer] ID: " + i.ToString() + " Name: " + transfer[3] + " [Daily]");
                            }
                        }
                        if (storedData.weeklytransfers.ContainsKey(player.UserIDString))
                        {
                            for (int i = 0; i < storedData.weeklytransfers[player.UserIDString].Count; i++)
                            {
                                List<String> transfer = storedData.weeklytransfers[player.UserIDString][i];
                                PrintToChat(player, "[Transfer] ID: " + i.ToString() + " Name: " + transfer[3] + " [Weekly]");
                            }
                        }
                        if (storedData.monthlytransfers.ContainsKey(player.UserIDString))
                        {
                            for (int i = 0; i < storedData.monthlytransfers[player.UserIDString].Count; i++)
                            {
                                List<String> transfer = storedData.monthlytransfers[player.UserIDString][i];
                                PrintToChat(player, "[Transfer] ID: " + i.ToString() + " Name: " + transfer[3] + " [Monthly]");
                            }
                        }
                        if (storedData.yearlytransfers.ContainsKey(player.UserIDString))
                        {
                            for (int i = 0; i < storedData.yearlytransfers[player.UserIDString].Count; i++)
                            {
                                List<String> transfer = storedData.yearlytransfers[player.UserIDString][i];
                                PrintToChat(player, "[Transfer] ID: " + i.ToString() + " Name: " + transfer[3] + " [Yearly]");
                            }
                        }
                    }
                    return;
                }
                PrintToChat(player, "[ECO]: Error use: transer add, transfer, list remove");
                return;
            }
            if (args[0].Equals("bal"))
            {
                if (args.Length == 1)
                {
                    PrintToChat(player, "[ECO]: Your current money is: <color=\"green\">â¬" + SimpleRound(storedData.munnie[player.UserIDString]) + "</color>(â¬" + Math.Round(storedData.munnie[player.UserIDString], 2).ToString() + ")");
                }
                else
                {
                    BasePlayer ply = findPlayer(args[1]);
                    if (ply.Equals(new BasePlayer()))
                    {
                        PrintToChat(player, "[ECO]: Error, Player could not be found!");
                    }
                    else
                    {
                        PrintToChat(player, "[ECO]: " + ply.displayName + "'s money is: <color=\"green\">â¬" + SimpleRound(storedData.munnie[ply.UserIDString]) + "</color>(â¬" + Math.Round(storedData.munnie[ply.UserIDString],2).ToString() + ")");
                    }
                }
                return;
            }
            if (args[0].Equals("help"))
            {
                PrintToChat(player, "<color=\"green\">[ECO] Help</color>");
                if (player.IsAdmin())
                {
                    PrintToChat(player, "<color=\"gray\">(/eco ..) bal, give, pay, transfer </color>");
                }
                else
                {

                }
                return;
            }
            if (args[0].Equals("pay"))
            {
                try
                {
                    BasePlayer ply = findPlayer(args[1]);
                    if (ply.Equals(new BasePlayer()))
                    {
                        PrintToChat(player, "[ECO]: Error, Player could not be found!");
                    }
                    else
                    {
                        if (storedData.munnie[player.UserIDString] >= Double.Parse(args[2]))
                        {
                            if (args.Length > 3)
                            {
                                String msg = "";
                                for (int i = 3; i < args.Length; i++)
                                {
                                    msg = msg + " " + ApplyColor(args[i]);
                                }
                                storedData.munnie[player.UserIDString] = storedData.munnie[player.UserIDString] - Double.Parse(args[2]);
                                storedData.munnie[ply.UserIDString] = storedData.munnie[ply.UserIDString] + Double.Parse(args[2]);
                                PrintToChat(player, ApplyColor("[ECO]: You successfully gave " + ply.displayName + " $greenâ¬" + args[2] + "$r" + " Because: " + msg));
                                PrintToChat(player, "[ECO]: Your current money is: <color=\"green\">â¬" + SimpleRound(storedData.munnie[player.UserIDString]).ToString() + "</color>");
                                PrintToChat(ply, "[ECO]: You received: <color=\"green\">â¬" + Double.Parse(args[2]).ToString() + "</color> from: " + player.displayName + " Because: " + msg);
                                PrintToChat(ply, "[ECO]: Your current money is: <color=\"green\">â¬" + SimpleRound(storedData.munnie[ply.UserIDString]).ToString() + "</color>");
                                Interface.GetMod().DataFileSystem.WriteObject<StoredData>("EconomyCore", storedData);
                                return;
                            }
                            else
                            {
                                storedData.munnie[player.UserIDString] = storedData.munnie[player.UserIDString] - Double.Parse(args[2]);
                                storedData.munnie[ply.UserIDString] = storedData.munnie[ply.UserIDString] + Double.Parse(args[2]);
                                PrintToChat(player, ApplyColor("[ECO]: You successfully gave " + ply.displayName + " $greenâ¬" + args[2] + "$r"));
                                PrintToChat(player, "[ECO]: Your current money is: <color=\"green\">â¬" + SimpleRound(storedData.munnie[player.UserIDString]).ToString() + "</color>");
                                PrintToChat(ply, "[ECO]: You received: <color=\"green\">â¬" + Double.Parse(args[2]).ToString() + "</color> from: " + player.displayName);
                                PrintToChat(ply, "[ECO]: Your current money is: <color=\"green\">â¬" + SimpleRound(storedData.munnie[ply.UserIDString]).ToString() + "</color>");
                                Interface.GetMod().DataFileSystem.WriteObject<StoredData>("EconomyCore", storedData);
                                return;
                            }
                        }
                        else
                        {
                            Double missing = 0.00;
                            missing = (storedData.munnie[player.UserIDString] - Double.Parse(args[2])) * -1;
                            PrintToChat(player, ApplyColor("$red[ECO]: Error, You dont have enough money you need $r$greenâ¬" + SimpleRound(missing) + "$r!"));
                        }
                    }
                }
                catch
                {
                    PrintToChat(player, "[ECO]: Error, use /eco pay <player> <amount>");
                    return;
                }
            }
            if (args[0].Equals("give"))
            {
                Boolean cangive = false;
                if (player.net.connection.authLevel >= 1){
                    cangive = true;
                }
                if (CanUse("eco.give", player)){
                    cangive = true;
                }
                if (cangive)
                {
                    try
                    {
                        BasePlayer ply = findPlayer(args[1]);
                        if (ply.Equals(new BasePlayer()))
                        {
                            PrintToChat(player, "[ECO]: Error, Player could not be found!");
                        }
                        else
                        {
                            storedData.munnie[ply.UserIDString] = storedData.munnie[ply.UserIDString] + Double.Parse(args[2]);
                            PrintToChat(player, ApplyColor("[ECO]: You successfully gave " + ply.displayName + " $greenâ¬" + args[2] + "$r"));
                            PrintToChat(ply, "[ECO]: Your current money is: <color=\"green\">â¬" + SimpleRound(storedData.munnie[ply.UserIDString]).ToString() + "</color>");
                            Interface.GetMod().DataFileSystem.WriteObject<StoredData>("EconomyCore", storedData);
                            return;
                        }
                    }
                    catch
                    {
                        PrintToChat(player, "[ECO]: Error, use /eco give <player> <amount>");
                        return;
                    }
                }
                else
                {
                    PrintToChat(player, "[ECO]: You dont have permissions to this!");
                }
                return;
            }
            PrintToChat(player, "[ECO]: Incorrect usage! Use: /eco help");
            return;
        }
        Boolean CanUse(String perm, BasePlayer ply){
            foreach (String group in permission.GetUserGroups(ply.UserIDString)){
                foreach (String permis in permission.GetGroupPermissions(group)){
                    if (permis.Equals(perm)){
                        return true;
                    }
                }
            }
            return false;
        }
        BasePlayer findPlayerbysteamid(string steamid64)
        {
            foreach (BasePlayer ply in BasePlayer.activePlayerList)
            {
                if (ply.UserIDString.Equals(steamid64))
                {
                    return ply;
                }
            }
            return new BasePlayer();
        }

        BasePlayer findPlayer(string name)
        {
            foreach (BasePlayer ply in BasePlayer.activePlayerList)
            {
                if (ply.displayName.ToLower().Contains(name.ToLower()))
                {
                    return ply;
                }
            }
            return new BasePlayer();
        }
        Boolean CanPay(BasePlayer user, double amount, Boolean shouldInfo)
        {
            if (!(storedData.munnie[user.UserIDString] >= amount))
            {
                if (shouldInfo)
                {
                    Double missing = 0.00;
                    missing = (storedData.munnie[user.UserIDString] - amount) * -1;
                    PrintToChat(user, ApplyColor("$red[ECO]: Error, You dont have enough money you need $r$greenâ¬" + SimpleRound(missing) + "$r more, in total: $greenâ¬"+SimpleRound(amount)+"$r!"));
                }
            }
            return storedData.munnie[user.UserIDString] >= amount;
        }
        double Money(BasePlayer user)
        {
            return storedData.munnie[user.UserIDString];
        }
        void GiveMoney(BasePlayer user, double amount, String reason = "", Boolean ShouldNotfiy = true)
        {
            storedData.munnie[user.UserIDString] = storedData.munnie[user.UserIDString] + amount;
            if (ShouldNotfiy)
            {
                if (amount > 0)
                {
                    PrintToChat(user, ApplyColor("[ECO]: You received: $greenâ¬" + amount.ToString() + "$r" + " " + reason));
                    PrintToChat(user, "[ECO]: Your current money is: <color=\"green\">â¬" + SimpleRound(storedData.munnie[user.UserIDString]).ToString() + "</color>");
                }
                else
                {
                    PrintToChat(user, ApplyColor("[ECO]: You paid: $greenâ¬" + amount.ToString().Replace("-", "") + "$r" + " " + reason));
                    PrintToChat(user, "[ECO]: Your current money is: <color=\"green\">â¬" + SimpleRound(storedData.munnie[user.UserIDString]).ToString() + "</color>");
                }
            }
            Interface.GetMod().DataFileSystem.WriteObject<StoredData>("EconomyCore", storedData);
            return;
        }
        String ApplyColor(String str)
        {
            str = str.Replace("$red", "<color=\"red\">").Replace("$blue", "<color=\"blue\">").Replace("$black", "<color=\"black\">").Replace("$gray", "<color=\"gray\">").Replace("$green", "<color=\"green\">").Replace("$purple", "<color=\"purple\">").Replace("$r", "</color>");
            return str;
        }
        String SimpleRound(double intr)
        {
            if ((intr / 1000000000) >= 1)
            {
                Double divided = (intr / 1000000000);
                return Math.Round(divided, 2).ToString() + "B";
            }
            if ((intr / 1000000) >= 1)
            {
                Double divided = (intr / 1000000);
                return Math.Round(divided, 2).ToString() + "M";
            }
            if ((intr / 1000) >= 1)
            {
                Double divided = (intr / 1000);
                return Math.Round(divided, 2).ToString() + "K";
            }
            intr = Math.Round(intr, 2);
            return intr.ToString();
        }
    }
}

