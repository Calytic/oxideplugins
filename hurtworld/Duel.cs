using Oxide.Core;
using Oxide.Core.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Oxide.Plugins
{
    /*
    [B]Changelog 0.1.1[/B]
    Below changes & Fixes suggested by @Kolvin
        [B]Additions[/B]
    [LIST]
    [*] A timer has been added to the winner of the duel, to give them time to pickup any loot (default 30 seconds)
    [*] Accept a duel from a specific player if playername has been specified /duel yes <playername> - Playername is optional
    [*] Added name of the player requesting the duel. NOTE: You will have to delete your lang file or update your translated file. String affected = msg_DuelRequestReceived
    [*] Added check to make sure that there are no duels taking place, if so tell the requestor to try again later. 
    [/LIST]
        [B]Fixes[/B]
    [LIST]
    [*] Fixed chat message displaying {money} instead of the actual value
    [*] Fixed declined message showing wrong player name
    [/LIST]
    */
    [Info("Wager of battle", "Pho3niX90", "0.1.1", ResourceId = 1726)]
    class Duel : HurtworldPlugin
    {
        [PluginReference]
        Plugin EconomyBanks;

        string MoneySym;

        #region [DEFAULTS]
        void LoadMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                {"msg_DuelWon", "You have received {moneySymbol}{money} from winning the duel"},
                {"msg_DuellRequestSent", "Duel request has been sent to {playerName}"},
                {"msg_DuelRequestReceived", "You have received a duel request for an amount of {moneySymbol}{money} from {player}, accept with /duel yes, or decline with /duel no"},
                {"msg_TeleportIn", "You will be transported in {seconds} seconds"},
                {"msg_PlayerDeclinedDuel", "The {player} has declined the duel."},
                {"toast_DuelRefund", "Duel refund: {moneySymbol}{money}"},
                {"broadcast_DuelWinner", "{playerName} has won the duel!" },
                {"broadcast_DuelStarting", "A duel will now take place between {player1} and {player2}"},
                {"err_NotEnoughtCash", "{Color:Red}You do not have enought cash to {method} this duel, please withdraw cash or make some.{/Color}" },
                {"err_NoDuelsFound", "{Color:Red}No duels were found for you!{/Color}" },
                {"err_DuelAlreadyHappening", "{Color:Red}Sorry, there is a duel taking place already. Please wait for it to finish.{/Color}" },
                {"syntax_Help", "/duel req <playername> <amount> - Requests a duel\n/duel <yes/no> - Accepts or Declines duel"},
                {"msg_NoTopPlayers", "There are no player wins"},
                {"msg_TopPlayersList", "{rank}: {playername} has {totalwins} total wins"},
                {"msg_TransportBackIn", "You will be transported back in {time}"},
                {"admin_msg_ArenaSaved", "Arena {playerN} has been saved at {position}"},
                {"admin_lootTimeSaved", "Loot Time has been set to {time} seconds"}
            }, this, "en");
        }
        protected override void LoadDefaultConfig()
        {
            PrintWarning("Creating a configuration file for " + this.Title);
            Config.Clear();
            Config["transportTime"] = 60;
            Config["duelDistanceFromPlayer"] = 20;
            Config["lootTime"] = 30;
            Config["arenaP1"] = "";
            Config["arenaP2"] = "";
            SaveConfig();
        }
        #endregion
        #region [HOOKS]
        void Init()
        {
            LoadData();
            LoadMessages();
        }
        void OnServerInitialized()
        {
            if (!isEcoLoaded())
                throw new PluginLoadFailure("EconomyBanks was not found, please download and install from http://oxidemod.org/plugins/economy-banks.1653/");
            MoneySym = (string)EconomyBanks.Call("GetMsg", "moneySymbol");
        }
        void OnPlayerDeath(PlayerSession player, EntityEffectSourceData source)
        {
            var tmpName = GetNameOfObject(source.EntitySource);
            if (tmpName.Length < 3) return;
            var murdererName = tmpName.Remove(tmpName.Length - 3);
            var isPlayer = (getSession(murdererName) != null) ? true : false;
            if (source.EntitySource.name == null || !isPlayer) return;

            var murderer = getSession(murdererName);
            var deceased = player;
            var deceasedId = player.SteamId;

            var myInClause = new ulong[] { (ulong)murderer.SteamId, (ulong)deceasedId };

            DuelData thisDuel = (from x in Duels where myInClause.Contains(x.Accepter) && myInClause.Contains(x.Requester) && x.Winner == 0 select x).FirstOrDefault();

            if (thisDuel == null || thisDuel.Accepted != 1 || thisDuel.Winner != 0) return;

            if (isPlayer)
            {
                thisDuel.Winner = (ulong)murderer.SteamId;
                thisDuel.WinnerName = murdererName;
                PrintWarning(murdererName + " got " + thisDuel.Wager);
                AddCash(murderer, thisDuel.Wager);

                //announce the winner.
                hurt.BroadcastChat(GetMsg("broadcast_DuelWinner", player).Replace("{playerName}", murdererName));
                PrintToChat(murderer, GetMsg("msg_DuelWon", player)
                                                .Replace("{moneySymbol}", MoneySym)
                                                .Replace("{money}", (thisDuel.Wager / 2).ToString()));
                //Make loser infamouse.
                //announce that the loser is now infamouse, due to duel laws.
                //Duels.Remove(thisDuel);
                Vector3 transportBack = default(Vector3);
                if (thisDuel.Accepter == (ulong)murderer.SteamId)
                {
                    transportBack = parseVector3(thisDuel.AccepterPos);
                }
                else if (thisDuel.Requester == (ulong)murderer.SteamId)
                {
                    transportBack = parseVector3(thisDuel.RequesterPos);
                }
                float lootTime = float.Parse(GetConf("lootTime", "30").ToString());
                PrintToChat(murderer, GetMsg("msg_TransportBackIn", player).Replace("{time}", lootTime.ToString()));
                timer.Once(lootTime, () =>
                {

                    murderer.WorldPlayerEntity.transform.position = transportBack;
                });
            }
            SaveData();
        }
        #endregion

        #region [LISTS]
        public List<DuelData> Duels = new List<DuelData>();

        public class DuelData
        {
            public DuelData(ulong Requester, string RequesterName, ulong Accepter, string AccepterName, double Wager, string RequesterPos, string AccepterPos = "", ulong Winner = 0, int Accepted = 0, string WinnerName = "")
            {
                this.Requester = Requester;
                this.RequesterPos = RequesterPos;
                this.RequesterName = RequesterName;
                this.Accepter = Accepter;
                this.AccepterPos = AccepterPos;
                this.AccepterName = AccepterName;
                this.Wager = Wager;
                this.Winner = Winner;
                this.WinnerName = WinnerName;
                this.Accepted = Accepted;
            }

            public ulong Requester { get; set; }
            public string RequesterName { get; set; }
            public ulong Accepter { get; set; }
            public string AccepterName { get; set; }
            public string RequesterPos { get; set; }
            public string AccepterPos { get; set; }
            public double Wager { get; set; }
            public ulong Winner { get; set; }
            public string WinnerName { get; set; }
            public int Accepted { get; set; }
        }
        #endregion

        #region [CHAT COMMANDS]
        int ParseOrZero(ulong val)
        {
            if (val != 0)
            {
                return 1;
            }
            else
            {
                return 0;
            }
        }
        [ChatCommand("duel")]
        void ChatCmd_RequestDuel(PlayerSession player, string command, string[] args)
        {

            if (args.Length == 0)
            {
                PrintToChat(player, GetMsg("syntax_Help", player));
                return;
            }

            double pot = 0;
            var Action = args[0];

            switch (Action)
            {
                case "top":
                    var TopDuelers = from d in Duels
                                     where d.Winner != 0
                                     group d by d.Winner
                                     into g
                                     select new
                                     {
                                         winner = g.Key,
                                         winnerName = g.Max(a => a.WinnerName),
                                         totalWins = g.Sum(a => ParseOrZero(a.Winner))
                                     };
                    TopDuelers.OrderByDescending(d => d.totalWins).Take(5);
                    int i = 1;
                    if (TopDuelers.Count() == 0)
                    {
                        PrintToChat(player, GetMsg("msg_NoTopPlayers", player));
                    }
                    foreach (var topDueler in TopDuelers)
                    {
                        PrintToChat(player, GetMsg("msg_TopPlayersList", player)
                                         .Replace("{rank}", i.ToString())
                                         .Replace("{playername}", topDueler.winnerName)
                                         .Replace("{totalwins}", topDueler.totalWins.ToString()));

                        i++;
                    }
                    break;
                case "req":
                    double requestorBalance = CashBalance(player);
                    var accepter = getSession(args[1]);

                    var checkDuels = (from x in Duels where x.Winner == 0 && x.Accepted == 1 select x).Count();

                    PrintWarning("Current duels taking place: " + checkDuels);
                    if (checkDuels > 0)
                    {
                        PrintToChat(player, GetMsg("err_DuelAlreadyHappening", player));
                        return;
                    }

                    if (args.Length == 2)
                    {
                        pot = 0;
                    }
                    else {
                        double.TryParse(args[2], out pot);
                    }
                    PrintToChat(player, GetMsg("msg_DuellRequestSent", player)
                                                .Replace("{playerName}", accepter.Name));

                    if (requestorBalance >= pot)
                    {
                        pot *= 2;
                        string thepos = player.WorldPlayerEntity.transform.position.ToString();
                        Duels.Add(new DuelData((ulong)player.SteamId, player.Name, (ulong)accepter.SteamId, accepter.Name, pot, thepos));
                        RemoveCash(player, pot / 2);
                        //Send message to acceptor that a duel is requested. 
                        PrintToChat(accepter, GetMsg("msg_DuelRequestReceived", player)
                            .Replace("{money}", (pot / 2).ToString())
                            .Replace("{player}", player.Name));
                    }
                    else
                    {
                        PrintToChat(player, GetMsg("err_NotEnoughtCash", player).Replace("{method}", "request"));
                    }
                    SaveData();
                    break;
                case "yes":
                    PlayerSession acceptedPlayer = null;
                    if (args.Length > 1)
                    {
                        acceptedPlayer = getSession(args[1]);
                    }
                    double accepterBalance = CashBalance(player);
                    DuelData currentDuel = null;
                    bool errs = false;
                    foreach (var PlayerDuel in Duels)
                    {

                        pot = 0;
                        if (PlayerDuel.Accepter == (ulong)player.SteamId && PlayerDuel.Winner == 0)
                        {
                            if (acceptedPlayer != null)
                            {
                                if ((ulong)acceptedPlayer.SteamId != PlayerDuel.Accepter)
                                {
                                    continue;
                                }
                            }

                            PlayerDuel.AccepterPos = player.WorldPlayerEntity.transform.position.ToString();
                            pot = PlayerDuel.Wager / 2;
                            if (accepterBalance >= pot)
                            {
                                PlayerDuel.Accepted = 1;
                                RemoveCash(player, pot);
                                doDuel(PlayerDuel);
                                currentDuel = PlayerDuel;
                            }
                            else
                            {
                                PrintToChat(player, GetMsg("err_NotEnoughtCash", player).Replace("{method}", "accept"));
                                errs = true;
                            }

                            break;
                        }
                    }
                    if (currentDuel == null && !errs)
                    {
                        PrintToChat(player, GetMsg("err_NoDuelsFound", player));
                    }
                    SaveData();
                    break;
                case "no":
                    DuelData declinedDuel = null;
                    foreach (var PlayerDuel in Duels)
                    {
                        pot = 0;
                        if (PlayerDuel.Accepter == (ulong)player.SteamId)
                        {
                            PlayerDuel.Accepted = 0;
                            declinedDuel = PlayerDuel;
                        }

                        break;

                    }
                    if (declinedDuel != null)
                    {
                        PrintToChat(getSession(declinedDuel.Requester.ToString()), GetMsg("msg_PlayerDeclinedDuel", player)
                                                .Replace("{player}", player.Name));
                        AddCash(getSession(declinedDuel.Requester.ToString()), (declinedDuel.Wager / 2));
                        EconomyBanks.Call("Toast", player, GetMsg("toast_DuelRefund", player).Replace("{money}", (declinedDuel.Wager / 2).ToString()));
                        Duels.Remove(declinedDuel);
                    }
                    else {
                        PrintToChat(player, GetMsg("err_NoDuelsFound", player));
                    }
                    SaveData();
                    break;
                case "setup":
                    if (!player.IsAdmin) return;
                    var ActionSetup = args[1];

                    switch (ActionSetup)
                    {
                        case "loottime":
                            PrintToChat(player, GetMsg("admin_lootTimeSaved", player).Replace("{time}", args[2]));

                            Config["lootTime"] = args[2];
                            break;
                        case "arena":
                            var Position = player.WorldPlayerEntity.transform.position;
                            PrintWarning(args[2]);
                            if (args[2].Equals("p1", StringComparison.CurrentCultureIgnoreCase))
                            {
                                Config["arenaP1"] = Position.ToString().Replace("(", "").Replace(")", "");
                            }
                            else if (args[2].Equals("p2", StringComparison.CurrentCultureIgnoreCase))
                            {
                                Config["arenaP2"] = Position.ToString().Replace("(", "").Replace(")", "");
                            }
                            PrintToChat(player, GetMsg("admin_msg_ArenaSaved", player)
                                                    .Replace("{playerN}", args[2])
                                                    .Replace("{position}", Position.ToString()));
                            SaveConfig();
                            break;
                    }
                    break;
            }
        }
        #endregion

        #region [HELPERS]
        void doDuel(DuelData Duel)
        {
            var SpawnPoint = GameManager.Instance.GetRandomSpawnPoint();
            string p1Position = GetConf("arenaP1", "").ToString();
            var p1PositionVec = p1Position.Split(',');
            string p2Position = GetConf("arenaP2", "").ToString();
            var p2PositionVec = p2Position.Split(',');

            Vector3 Vector1;
            Vector3 Vector2;

            if (p1Position != "")
            {
                Vector1 = new Vector3(float.Parse(p1PositionVec[0]), float.Parse(p1PositionVec[1]), float.Parse(p1PositionVec[2]));
            }
            else
            {
                Vector1 = new Vector3(SpawnPoint.x, SpawnPoint.y, SpawnPoint.z);
            }

            if (p2Position != "")
            {
                Vector2 = new Vector3(float.Parse(p2PositionVec[0]), float.Parse(p2PositionVec[1]), float.Parse(p2PositionVec[2]));
            }
            else
            {
                Vector2 = new Vector3(Vector1.x - int.Parse(GetConf("duelDistanceFromPlayer", "20").ToString()), Vector1.y, Vector1.z);
            }


            GameObject playerEntity1;
            GameObject playerEntity2;

            PlayerSession player1 = getSession(Duel.Accepter.ToString());

            PlayerSession player2 = getSession(Duel.Requester.ToString());


            PrintToChat(player1, GetMsg("msg_TeleportIn", player1).Replace("{seconds}", GetConf("transportTime", "60").ToString()));
            PrintToChat(player2, GetMsg("msg_TeleportIn", player2).Replace("{seconds}", GetConf("transportTime", "60").ToString()));

            timer.Once(float.Parse(GetConf("transportTime", "60").ToString()), () =>
            {
                playerEntity1 = player1.WorldPlayerEntity;
                playerEntity2 = player2.WorldPlayerEntity;

                hurt.BroadcastChat(GetMsg("broadcast_DuelStarting")
                                    .Replace("{player1}", getSession(Duel.Accepter.ToString()).Name)
                                    .Replace("{player2}", getSession(Duel.Requester.ToString()).Name));
                timer.Once(1f, () =>
                {

                    playerEntity1.transform.position = Vector1;
                    playerEntity2.transform.position = Vector2;


                    FPSInputControllerServer fps1 = playerEntity1.GetComponent<FPSInputControllerServer>();
                    FPSInputControllerServer fps2 = playerEntity2.GetComponent<FPSInputControllerServer>();
                    Vector3 vector1 = (playerEntity2.transform.position - playerEntity1.transform.position).normalized;
                    Vector3 vector2 = (playerEntity1.transform.position - playerEntity2.transform.position).normalized;
                    Quaternion lookRotation1 = Quaternion.LookRotation(vector1);
                    Quaternion lookRotation2 = Quaternion.LookRotation(vector2);
                    fps1.ResetViewAngleServer(lookRotation1);
                    fps2.ResetViewAngleServer(lookRotation2);

                    doEmote(player1); doEmote(player2);

                });
            });

            //give spear.
            //disable emote.
            SaveData();

        }
        void doEmote(PlayerSession player)
        {
            EmoteManagerServer EmoteServer = player.WorldPlayerEntity.GetComponent<EmoteManagerServer>();
            EmoteServer.BeginEmoteServer(EEmoteType.Salute);
        }
        double CashBalance(PlayerSession player)
        {
            return double.Parse(EconomyBanks.Call("Wallet", player).ToString());
        }
        double AccountBalance(PlayerSession player)
        {
            return double.Parse(EconomyBanks.Call("Balance", player).ToString());
        }
        void AddCash(PlayerSession player, double Amount)
        {
            EconomyBanks.Call("AddCash", player, Amount);
        }
        void RemoveCash(PlayerSession player, double Amount)
        {
            EconomyBanks.Call("RemoveCash", player, Amount);
        }
        void LoadData()
        {
            Duels = Interface.Oxide.DataFileSystem.ReadObject<List<DuelData>>("DuelData");
        }

        void SaveData()
        {
            Interface.Oxide.DataFileSystem.WriteObject("DuelData", Duels);
        }
        private PlayerSession getSession(string identifier)
        {
            var sessions = GameManager.Instance.GetSessions();
            PlayerSession session = null;
            foreach (var i in sessions)
            {
                if (i.Value.Name.ToLower().Contains(identifier.ToLower()) || identifier.Equals(i.Value.SteamId.ToString()))
                {
                    session = i.Value;
                    break;
                }
            }

            return session;
        }
        string GetNameOfObject(GameObject obj)
        {
            var ManagerInstance = GameManager.Instance;
            return ManagerInstance.GetDescriptionKey(obj);
        }
        private void PrintToChat(PlayerSession player, string Message)
        {
            hurt.SendChatMessage(player, Message);
        }
        string GetMsg(string key, object userID = null)
        {
            return (userID == null) ? lang.GetMessage(key, this) : lang.GetMessage(key, this, userID.ToString())
                .Replace("{moneySymbol}", MoneySym)
                .Replace("{Color:Red}", "<color=#ff0000ff>")
                .Replace("{/Color}", "</color>");
        }
        object GetConf(string Key, string Default) { return (Config[Key] == null || Config[Key].ToString() == "") ? Default : Config[Key]; }
        Vector3 parseVector3(string sourceString)
        {
            string outString;
            Vector3 outVector3;
            // Trim extranious parenthesis
            outString = sourceString.Substring(1, sourceString.Length - 2);
            // Split delimted values into an array
            var splitString = outString.Split(","[0]);
            // Build new Vector3 from array elements
            outVector3.x = float.Parse(splitString[0]);
            outVector3.y = float.Parse(splitString[1]);
            outVector3.z = float.Parse(splitString[2]);

            return outVector3;

        }
        bool isEcoLoaded()
        {
            if (EconomyBanks != null)
            { return true; }
            else
            { return false; }
        }
        #endregion
    }
}
