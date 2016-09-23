using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;

using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Lottery", "Sami37", "1.0.0")]
    internal class Lottery : RustPlugin
    {
        #region Economy Support

        [PluginReference("Economics")]
        Plugin Economy;

        #endregion

        internal class playerinfo
        {
            public int multiplicator { get; set; } = 1;
            public double currentbet { get; set; }
            public double totalbet { get; set; }

        }

        int[] GetIntArray(int num)
        {
            List<int> listOfInts = new List<int>();
            while(num > 0)
            {
                listOfInts.Add(num % 10);
                num = num / 10;
            }
            listOfInts.Reverse();
            return listOfInts.ToArray();
        }

        #region general_variable
        private bool newConfig;
        public Dictionary<ulong, playerinfo> Currentbet = new Dictionary<ulong, playerinfo>();
        private string container;
        private string containerwin;
        private DynamicConfigFile data;
        private double jackpot = 50000;
        public Dictionary<string, int> IndividualRates { get; private set; }
        private Dictionary<string, object> DefaultWinRates = DefaultPay();
        #endregion

        #region config
        private void CheckCfg<T>(string Key, ref T var)
        {
            if (Config[Key] is T)
                var = (T) Config[Key];
            else
            {
                Config[Key] = var;
                newConfig = true;
            }
        }

        protected override void LoadDefaultConfig()
        {
            Config["WinRate"] = DefaultWinRates;
            Config["Jackpot"] = jackpot;
            SaveConfig();
        }
        static Dictionary<string, object> DefaultPay()
        {
            var d = new Dictionary<string, object>
            {
                { "111x", 1 },
                { "222x", 10 },
                { "333x", 50 },
                { "444x", 10 },
                { "555x", 75 },
                { "666x", 5 },
                { "777x", 75 },
                { "888x", 56 },
                { "999x", 42 },
                { "99x9", 52 },
                { "9x99", 57 },
                { "x999", 85 },
                { "99xx", 86 },
                { "9xxx", 86 }
            };
            return d;
        }
        #endregion

        #region data_init

        void LoadDefaultMessages()
        {
            var messages = new Dictionary<string, string>
            {
                {"NoPerm", "You don't have permission to do it."},
                {"NoWin", "You don't win anything."},
                {"NoEconomy", "Economics isn't installed."},
                {"NotEnoughMoney", "You don't have enough money."},
                {"Win", "You roll {0} and won {1}$"},
                {"NoBet", "You must bet before."},
                {"Balance", "Your current balance is {0}$"},
                {"CurrentBet", "Your current bet is {0}$"},
                {"Roll", "Roll 7777 to win \nthe current jackpot:\n {0}$"},
                {"Jackpot", "You roll {0} and won the jackpot : {1}$ !!!!!!"}
            };
            lang.RegisterMessages(messages, this);
        }

        void Init()
        {
            permission.RegisterPermission("Lottery.canuse", this);
            CheckCfg("Jackpot", ref jackpot);
            CheckCfg("WinRate", ref DefaultWinRates);
            SaveConfig();
            var dict = DefaultWinRates;
            IndividualRates = new Dictionary<string, int>();
            foreach (var entry in dict)
            {
                int rate;
                if (!int.TryParse(entry.Value.ToString(), out rate)) continue;
                IndividualRates.Add(entry.Key, rate);
            }

            data = Interface.Oxide.DataFileSystem.GetFile(Name);
            try
            {
                Currentbet = data.ReadObject<Dictionary<ulong, playerinfo>>();
            }
            catch (Exception e)
            {
                Currentbet = new Dictionary<ulong, playerinfo>();
                Puts(e.Message);
            }
            data.WriteObject(Currentbet);
        }

        void Unload()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                GUIDestroy(player);
                GUIDestroy(player);
            }
            Puts("Data saved.");
            SaveData(Currentbet);
        }
        #endregion
        
        #region save_data
        void SaveData(Dictionary<ulong, playerinfo> datas)
        {
            data.WriteObject(datas);
        }
        #endregion

        #region Lotery
        void GUIDestroy(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, "container");
            CuiHelper.DestroyUi(player, "containerwin");
            CuiHelper.DestroyUi(player, "ButtonBack");
            CuiHelper.DestroyUi(player, "ButtonForward");
        }
        
        void ShowLotery(BasePlayer player, string[] args)
        {
            if (!Economy.IsLoaded)
            {
                SendReply(player, lang.GetMessage("NoEconomy", this));
                return;
            }
            int multiplier = 1;
            int bet = 0;
            int from = 0;
            double currentBalance = (double)Economy?.Call("GetPlayerMoney", player.userID.ToString());
            playerinfo playerbet = new playerinfo();
            if (Currentbet.ContainsKey(player.userID))
            {
                Currentbet.TryGetValue(player.userID, out playerbet);
            }
            else
            {
                Currentbet.Add(player.userID, new playerinfo());
                Currentbet.TryGetValue(player.userID, out playerbet);
            }
            if (args != null && args.Length > 0)
            {
                if (args[0].Contains("less") || args[0].Contains("plus"))
                {
                    if (args[0].Contains("plus"))
                    {
                        if (currentBalance >= playerbet.currentbet*(playerbet.multiplicator + 1))
                        {
                            int.TryParse(args[1], out multiplier);
                            playerbet.multiplicator += multiplier;                            
                        }
                    }
                    if (args[0].Contains("less"))
                    {
                        if (playerbet.multiplicator > 1)
                            playerbet.multiplicator -= 1;
                    }
                }
                if (args[0].Contains("bet"))
                {
                    int.TryParse(args[1], out bet);
                    if(currentBalance < (playerbet.currentbet+bet)*playerbet.multiplicator)
                        SendReply(player, lang.GetMessage("NotEnoughMoney", this));
                    else
                        playerbet.currentbet += bet;
                }
                if (args[0].Contains("page"))
                {
                    int.TryParse(args[1], out from);
                }
            }
            int i = 0;
            double jackpots = Math.Round(Currentbet.Sum(v => v.Value.totalbet));
            jackpots += jackpot;
            var win = new CuiElementContainer();
            var containerwin = win.Add(new CuiPanel
            {
                Image =
                {
                    Color = "0.1 0.1 0.1 1"
                },
                RectTransform =
                {
                    AnchorMin = "0.8 0.2",
                    AnchorMax = "1 0.8"
                },
                CursorEnabled = true
            }, "Hud", "containerwin");
            win.Add(new CuiLabel
            {
                Text =
                {
                    Text = "Win Rate",
                    FontSize = 18,
                    Align = TextAnchor.MiddleCenter
                },
                RectTransform =
                {
                    AnchorMin = "0.1 0.85",
                    AnchorMax = "0.9 1"
                }
            }, containerwin);
            foreach (var elem in IndividualRates)
            {
                if (i >= from && i < from + 10)
                {
                    var pos = 0.91 - (i - from)/10.0;
                    var pos2 = 0.91 - (i - from)/20.0;
                    win.Add(new CuiLabel
                    {
                        Text =
                        {
                            Text = elem.Key + ": " + elem.Value + " %",
                            FontSize = 18,
                            Align = TextAnchor.MiddleCenter
                        },
                        RectTransform =
                        {
                            AnchorMin = $"{0.1} {pos}",
                            AnchorMax = $"{0.9} {pos2}"
                        }
                    }, containerwin);
                }
                i++;
            }
            var minfrom = from <= 10 ? 0 : from - 10;
            var maxfrom = from + 10 >= i ? from : from + 10;
            win.AddRange(ChangeBonusPage(minfrom, maxfrom));

            var elements = new CuiElementContainer();
#region background
            var container = elements.Add(new CuiPanel
            {
                Image =
                {
                    Color = "0.1 0.1 0.1 1"
                },
                RectTransform =
                {
                    AnchorMin = "0 0.2",
                    AnchorMax = "0.8 0.8"
                },
                CursorEnabled = true
            }, "Hud", "container");
#endregion
#region closebutton
            var closeButton = new CuiButton
            {
                Button =
                {
                    Command = "cmdDestroyUI",
                    Close = container,
                    Color = "0.8 0.8 0.8 0.2"
                },
                RectTransform =
                {
                    AnchorMin = "0.86 0.92",
                    AnchorMax = "0.97 0.98"
                },
                Text =
                {
                    Text = "X",
                    FontSize = 22,
                    Align = TextAnchor.MiddleCenter
                }
            };
            elements.Add(closeButton, container);
#endregion
#region currency
            elements.Add(new CuiLabel
            {
                Text =
                {
                    Text = string.Format(lang.GetMessage("Balance", this, player.UserIDString), currentBalance.ToString(CultureInfo.CurrentCulture)),
                    FontSize = 18,
                    Align = TextAnchor.MiddleCenter
                },
                RectTransform =
                {
                    AnchorMin = "0.1 0.91",
                    AnchorMax = "0.9 0.98"
                }
            }, container);
#endregion
#region multiplier
            elements.Add(new CuiLabel
            {
                Text =
                {
                    Text = "Multiplier : x" + playerbet.multiplicator,
                    FontSize = 18,
                    Align = TextAnchor.MiddleCenter
                },
                RectTransform =
                {
                    AnchorMin = "0.05 0.81",
                    AnchorMax = "0.15 0.88"
                }
            }, container);
            elements.Add(new CuiButton
            {
                Button =
                {
                    Command = "cmdLess",
                    Close = container,
                    Color = "0.8 0.8 0.8 0.2"
                },
                Text =
                {
                    Text = "-",
                    FontSize = 18,
                    Align = TextAnchor.MiddleCenter
                },
                RectTransform =
                {
                    AnchorMin = "0.2 0.81",
                    AnchorMax = "0.3 0.88"
                }
            }, container);
            elements.Add(new CuiButton
            {
                Button =
                {
                    Command = "cmdPlus",
                    Close = container,
                    Color = "0.8 0.8 0.8 0.2"
                },
                Text =
                {
                    Text = "+",
                    FontSize = 18,
                    Align = TextAnchor.MiddleCenter
                },
                RectTransform =
                {
                    AnchorMin = "0.31 0.81",
                    AnchorMax = "0.41 0.88"
                }
            }, container);
#endregion
#region bet
            elements.Add(new CuiLabel
            {
                Text =
                {
                    Text = "Bet modifiers :",
                    FontSize = 18,
                    Align = TextAnchor.MiddleCenter
                },
                RectTransform =
                {
                    AnchorMin = "0.05 0.61",
                    AnchorMax = "0.15 0.68"
                }
            }, container);
            elements.Add(new CuiButton
            {
                Button =
                {
                    Command = "cmdBet 1",
                    Close = container,
                    Color = "0.8 0.8 0.8 0.2"
                },
                Text =
                {
                    Text = "+1",
                    FontSize = 18,
                    Align = TextAnchor.MiddleCenter
                },
                RectTransform =
                {
                    AnchorMin = "0.05 0.51",
                    AnchorMax = "0.15 0.58"
                }
            }, container);
            elements.Add(new CuiButton
            {
                Button =
                {
                    Command = "cmdBet 5",
                    Close = container,
                    Color = "0.8 0.8 0.8 0.2"
                },
                Text =
                {
                    Text = "+5",
                    FontSize = 18,
                    Align = TextAnchor.MiddleCenter
                },
                RectTransform =
                {
                    AnchorMin = "0.155 0.51",
                    AnchorMax = "0.255 0.58"
                }
            }, container);
            elements.Add(new CuiButton
            {
                Button =
                {
                    Command = "cmdBet 10",
                    Close = container,
                    Color = "0.8 0.8 0.8 0.2"
                },
                Text =
                {
                    Text = "+10",
                    FontSize = 18,
                    Align = TextAnchor.MiddleCenter
                },
                RectTransform =
                {
                    AnchorMin = "0.26 0.51",
                    AnchorMax = "0.36 0.58"
                }
            }, container);
            elements.Add(new CuiButton
            {
                Button =
                {
                    Command = "cmdBet 100",
                    Close = container,
                    Color = "0.8 0.8 0.8 0.2"
                },
                Text =
                {
                    Text = "+100",
                    FontSize = 18,
                    Align = TextAnchor.MiddleCenter
                },
                RectTransform =
                {
                    AnchorMin = "0.365 0.51",
                    AnchorMax = "0.485 0.58"
                }
            }, container);
            elements.Add(new CuiButton
            {
                Button =
                {
                    Command = "cmdBet 1000",
                    Close = container,
                    Color = "0.8 0.8 0.8 0.2"
                },
                Text =
                {
                    Text = "+1000",
                    FontSize = 18,
                    Align = TextAnchor.MiddleCenter
                },
                RectTransform =
                {
                    AnchorMin = "0.05 0.41",
                    AnchorMax = "0.15 0.48"
                }
            }, container);
            elements.Add(new CuiButton
            {
                Button =
                {
                    Command = "cmdBet 10000",
                    Close = container,
                    Color = "0.8 0.8 0.8 0.2"
                },
                Text =
                {
                    Text = "+10000",
                    FontSize = 18,
                    Align = TextAnchor.MiddleCenter
                },
                RectTransform =
                {
                    AnchorMin = "0.155 0.41",
                    AnchorMax = "0.255 0.48"
                }
            }, container);
            elements.Add(new CuiButton
            {
                Button =
                {
                    Command = "cmdPlaceBet",
                    Close = container,
                    Color = "0.8 0.8 0.8 0.2"
                },
                Text =
                {
                    Text = "Place Bet",
                    FontSize = 18,
                    Align = TextAnchor.MiddleCenter
                },
                RectTransform =
                {
                    AnchorMin = "0.05 0.31",
                    AnchorMax = "0.255 0.38"
                }
            }, container);
#endregion
#region winpart
            elements.Add(new CuiLabel
            {
                Text =
                {
                    Text = string.Format(lang.GetMessage("CurrentBet", this, player.UserIDString), playerbet.currentbet*playerbet.multiplicator),
                    FontSize = 18,
                    Align = TextAnchor.MiddleCenter
                },
                RectTransform =
                {
                    AnchorMin = "0.71 0.71",
                    AnchorMax = "0.99 0.81"
                }
            }, container);

            elements.Add(new CuiLabel
            {
                Text =
                {
                    Text = string.Format(lang.GetMessage("Roll", this, player.UserIDString), jackpots),
                    FontSize = 18,
                    Align = TextAnchor.MiddleCenter
                },
                RectTransform =
                {
                    AnchorMin = "0.71 0.39",
                    AnchorMax = "0.99 0.59"
                }
            }, container);
#endregion
            CuiHelper.AddUi(player, elements);
            CuiHelper.AddUi(player, win);
            Currentbet.Remove(player.userID);
            Currentbet.Add(player.userID, playerbet);
            SaveData(Currentbet);
        }
        
        private static CuiElementContainer ChangeBonusPage(int pageless, int pagemore)
        {
            return new CuiElementContainer
            {
                {
                    new CuiButton
                    {
                        Button = {Command = $"cmdPage page {pageless}", Color = "0.5 0.5 0.5 0.2"},
                        RectTransform = {AnchorMin = "0.83 0.25", AnchorMax = "0.91 0.3"},
                        Text = {Text = "<<", FontSize = 20, Align = TextAnchor.MiddleCenter}
                    },
                    "Hud",
                    "ButtonBack"
                },
                {
                    new CuiButton
                    {
                        Button = {Command = $"cmdPage page {pagemore}", Color = "0.5 0.5 0.5 0.2"},
                        RectTransform = {AnchorMin = "0.92 0.25", AnchorMax = "1 0.30"},
                        Text = {Text = ">>", FontSize = 20, Align = TextAnchor.MiddleCenter}
                    },
                    "Hud",
                    "ButtonForward"
                }
            };
        }
        #endregion

        #region reward
        public int FindReward(int bet, int reference, int multiplicator)
        {
            int reward = 0;
            int[] number = GetIntArray(reference);
            string newreference;

            #region jackpot
            if (reference == 7777)
            {
                int jackpots = (int) Math.Round(Currentbet.Sum(v => v.Value.totalbet));
                reward = bet*(reward/100) * multiplicator + jackpots + (int)jackpot;
                return reward;
            }
            #endregion
            
            #region full_match
            if (IndividualRates.ContainsKey(reference.ToString()))
            {
                IndividualRates.TryGetValue(number.ToString(), out reward);
                return bet*(reward/100) * multiplicator;
            }
            #endregion

            #region three_match
            newreference = number[0].ToString() + number[1].ToString() + number[2].ToString() + "x";
            if(IndividualRates.ContainsKey(newreference))
            {
                IndividualRates.TryGetValue(newreference, out reward);
                return bet*(reward/100) * multiplicator;
            }
            newreference = number[0].ToString() + number[1].ToString() + "x" + number[3].ToString();
            if(IndividualRates.ContainsKey(newreference))
            {
                IndividualRates.TryGetValue(newreference, out reward);
                return bet*(reward/100) * multiplicator;
            }
            newreference = number[0].ToString() + "x" + number[2].ToString() + number[3].ToString();
            if(IndividualRates.ContainsKey(newreference))
            {
                IndividualRates.TryGetValue(newreference, out reward);
                return bet*(reward/100) * multiplicator;
            }
            newreference =  "x" + number[1].ToString() + number[2].ToString() + number[3].ToString();
            if(IndividualRates.ContainsKey(newreference))
            {
                IndividualRates.TryGetValue(newreference, out reward);
                return bet*(reward/100) * multiplicator;
            }
            #endregion

            #region two_match
            newreference = number[0].ToString() + number[1].ToString() + "x" + "x";
            if(IndividualRates.ContainsKey(newreference))
            {
                IndividualRates.TryGetValue(newreference, out reward);
                return bet*(reward/100) * multiplicator;
            }
            newreference = number[0].ToString() + "x" + "x" + number[3].ToString();
            if(IndividualRates.ContainsKey(newreference))
            {
                IndividualRates.TryGetValue(newreference, out reward);
                return bet*(reward/100) * multiplicator;
            }
            newreference =  "x" + "x" + number[2].ToString() + number[3].ToString();
            if(IndividualRates.ContainsKey(newreference))
            {
                IndividualRates.TryGetValue(newreference, out reward);
                return bet*(reward/100) * multiplicator;
            }
            newreference = number[0].ToString() + "x" + number[2].ToString() + "x";
            if(IndividualRates.ContainsKey(newreference))
            {
                IndividualRates.TryGetValue(newreference, out reward);
                return bet*(reward/100) * multiplicator;
            }
            newreference = "x" + number[1].ToString() + "x" + number[3].ToString();
            if(IndividualRates.ContainsKey(newreference))
            {
                IndividualRates.TryGetValue(newreference, out reward);
                return bet*(reward/100) * multiplicator;
            }
            newreference =  "x" + number[1].ToString() + number[2].ToString() + "x";
            if(IndividualRates.ContainsKey(newreference))
            {
                IndividualRates.TryGetValue(newreference, out reward);
                return bet*(reward/100) * multiplicator;
            }
           #endregion

            #region one_match
            newreference = number[0].ToString() + "x" + "x" + "x";
            if(IndividualRates.ContainsKey(newreference))
            {
                IndividualRates.TryGetValue(newreference, out reward);
                return bet*(reward/100) * multiplicator;
            }
            newreference =  "x" + number[1].ToString() + "x" + "x";
            if(IndividualRates.ContainsKey(newreference))
            {
                IndividualRates.TryGetValue(newreference, out reward);
                return bet*(reward/100) * multiplicator;
            }
            newreference =  "x" + "x" + number[2].ToString() + "x";
            if(IndividualRates.ContainsKey(newreference))
            {
                IndividualRates.TryGetValue(newreference, out reward);
                return bet*(reward/100) * multiplicator;
            }
            newreference = "x" + "x" + "x" + number[3].ToString();
            if(IndividualRates.ContainsKey(newreference))
            {
                IndividualRates.TryGetValue(newreference, out reward);
                return bet*(reward/100) * multiplicator;
            }
#endregion
            return reward;
        }
        #endregion

        #region Command
        [ChatCommand("lot")]
        private void cmdLotery(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, "Lottery.canuse"))
            {
                SendReply(player, string.Format(lang.GetMessage("NoPerm", this, player.UserIDString)));
                return;
            }
            ShowLotery(player, null);
        }

        [ConsoleCommand("cmdDestroyUI")]
        void cmdDestroyUI(ConsoleSystem.Arg arg)
        {
            if (arg.Player() == null) return;
            GUIDestroy(arg.Player());
        }

        [ConsoleCommand("cmdLess")]
        void cmdLess(ConsoleSystem.Arg arg)
        {
            if (arg.Player() == null) return;
            GUIDestroy(arg.Player());
            ShowLotery(arg.Player(), new[] {"less", "-1"});
        }

        [ConsoleCommand("cmdBet")]
        void cmdBet(ConsoleSystem.Arg arg)
        {
            if (arg.Player() == null) return;
            GUIDestroy(arg.Player());
            ShowLotery(arg.Player(), new[] {"bet", arg.Args[0]});
        }

        [ConsoleCommand("cmdPlus")]
        void cmdPlus(ConsoleSystem.Arg arg)
        {
            if (arg.Player() == null) return;
            GUIDestroy(arg.Player());
            ShowLotery(arg.Player(), new[] {"plus", "1"});
        }

        [ConsoleCommand("cmdPage")]
        void cmdPage(ConsoleSystem.Arg arg)
        {
            if (arg.Player() == null) return;
            GUIDestroy(arg.Player());
            ShowLotery(arg.Player(), new[] {"page", arg.Args[1]});
        }

        [ConsoleCommand("cmdPlaceBet")]
        void cmdPlaceBet(ConsoleSystem.Arg arg)
        {
            Dictionary<ulong, playerinfo> playerinfos = new Dictionary<ulong, playerinfo>();
            GUIDestroy(arg.Player());
            if (arg.Player() == null) return;

            playerinfo playerbet = new playerinfo();
            if (!Currentbet.ContainsKey(arg.Player().userID))
            {
                Currentbet.Add(arg.Player().userID, new playerinfo());
            }
            else
            {
                Currentbet.TryGetValue(arg.Player().userID, out playerbet);
            }
            if (playerbet.currentbet == 0)
            {
                SendReply(arg.Player(), string.Format(lang.GetMessage("NoBet", this)));
                return;
            }
            Economy?.CallHook("Withdraw", arg.Player().userID, playerbet.currentbet*playerbet.multiplicator);
            int random = UnityEngine.Random.Range(1000, 9999);
            playerbet.totalbet += playerbet.currentbet*(10/100.0);
            int reward = FindReward((int)playerbet.currentbet, random, playerbet.multiplicator);
            if (random == 7777)
            {
                foreach (var resetbet in Currentbet)
                {
                    resetbet.Value.totalbet = 0;
                    resetbet.Value.multiplicator = 1;
                    playerinfos.Add(resetbet.Key, resetbet.Value);
                }
                Currentbet.Clear();
                Currentbet = playerinfos;
                Economy?.CallHook("Deposit", arg.Player().userID, reward);
                SendReply(arg.Player(), string.Format(lang.GetMessage("Jackpot", this), random, reward));
            }
            else if (reward != 0 && random != 7777)
            {
                Currentbet.Remove(arg.Player().userID);
                Currentbet.Add(arg.Player().userID, playerbet);
                Economy?.CallHook("Deposit", arg.Player().userID, reward);
                SendReply(arg.Player(), string.Format(lang.GetMessage("Win", this), random, reward));
            }
            else if (reward == 0)
            {
                SendReply(arg.Player(), lang.GetMessage("NoWin", this));
            }
            else
            {
                Economy?.CallHook("Deposit", arg.Player().userID, reward);
                SendReply(arg.Player(), string.Format(lang.GetMessage("Win", this), random, reward));
            }

            playerbet.currentbet = 0;
            playerbet.multiplicator = 1;
            SaveData(Currentbet);
        }

        #endregion
    }
}