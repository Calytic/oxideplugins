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
    [Info("Lottery", "Sami37", "1.0.3", ResourceId = 2145)]
    internal class Lottery : RustPlugin
    {
        #region Economy Support

        [PluginReference("Economics")]
        Plugin Economy;

        #endregion


        #region serverreward

        [PluginReference("ServerRewards")]
        Plugin ServerRewards;


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
        private bool newConfig, UseSR;
        public Dictionary<ulong, playerinfo> Currentbet = new Dictionary<ulong, playerinfo>();
        private string container, containerwin;
        private DynamicConfigFile data;
        private double jackpot, SRMinBet, SRjackpot, MinBetjackpot;
        private int JackpotNumber, SRJackpotNumber, DefaultMaxRange, DefaultMinRange;
        public Dictionary<string, int> IndividualRates { get; private set; }
        public Dictionary<string, int> SRRates { get; private set; }
        private Dictionary<string, object> DefaultWinRates = null;
        private Dictionary<string, int> SRWinRates = null;
        private List<string> DefaultBasePoint = null;
        #endregion

        #region config
		object GetConfig(string menu, string datavalue, object defaultValue)
		{
			var data = Config[menu] as Dictionary<string, object>;
			if (data == null)
			{
				data = new Dictionary<string, object>();
				Config[menu] = data;
				newConfig = true;
			}
			object value;
			if (!data.TryGetValue(datavalue, out value))
			{
				value = defaultValue;
				data[datavalue] = value;
				newConfig = true;
			}
			return value;
		}
		protected override void LoadDefaultConfig()
		{
			Config.Clear();
			LoadConfig();
		}

        void LoadConfig()
        {
            jackpot = Convert.ToDouble(GetConfig("Global", "Jackpot", 50000));
            DefaultWinRates = (Dictionary<string, object>)GetConfig("Global", "WinRate", DefaultPay());
            DefaultBasePoint = GetConfig("ServerRewards", "Match", DefaultSRPay()) as List<string>;
            SRWinRates = GetConfig("ServerRewards", "WinPoint", DefautSRWinPay()) as Dictionary<string,int>;
            SRjackpot = Convert.ToDouble(GetConfig("ServerRewards", "Jackpot", 10));
            SRMinBet = Convert.ToDouble(GetConfig("ServerRewards", "MinBet", 1000));
            MinBetjackpot = Convert.ToDouble(GetConfig("ServerRewards", "MinBetJackpot", 100000));
            SRJackpotNumber = Convert.ToInt32(GetConfig("ServerRewards", "JackpotMatch", 1869));
            JackpotNumber = Convert.ToInt32(GetConfig("Global", "JackpotMatch", 1058));
            DefaultMinRange = Convert.ToInt32(GetConfig("Global", "RollMinRange", 1000));
            DefaultMaxRange = Convert.ToInt32(GetConfig("Global", "RollMaxRange", 9999));
            UseSR = Convert.ToBoolean(GetConfig("ServerRewards", "Enabled", false));
		    if (!newConfig) return;
		    SaveConfig();
		    newConfig = false;
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

        static List<string> DefaultSRPay()
        {
            var d = new List<string>
            {
                "111x",
                "222x",
                "333x",
                "444x",
                "555x",
                "666x",
                "777x",
                "888x",
                "999x",
                "99x9",
                "9x99",
                "x999",
                "99xx",
                "9xxx"
            };
            return d;
        }

        static Dictionary<string, int> DefautSRWinPay()
        {
            var d = new Dictionary<string, int>
            {
                { "Match1Number", 1 },
                { "Match2Number", 2 },
                { "Match3Number", 3 },
                { "Match4Number", 4 }
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
                {"NoWin", "You roll {0} but don't win anything."},
                {"NoEconomy", "Economics isn't installed."},
                {"NotEnoughMoney", "You don't have enough money."},
                {"Win", "You roll {0} and won {1}$"},
                {"WinPoints", "You roll {0} and won {1} point(s)"},
                {"NoBet", "You must bet before."},
                {"Balance", "Your current balance is {0}$"},
                {"CurrentBet", "Your current bet is {0}$"},
                {"Roll", "Roll {0} to win \nthe current jackpot:\n {1}$"},
                {"Jackpot", "You roll {0} and won the jackpot : {1}$ !!!!!!"},
                {"MiniSRBet", "You need to bet more to place bet. (Min: {0})"},
                {"BetMore", "If you had bet more you could win the jackpot. (Min: {0})"},
                {"MinimumSRBet", "Minimum bet of {0} to win the current jackpot: {1} point(s)"}
            };
            lang.RegisterMessages(messages, this);
            Puts("Messages loaded...");
        }

		void OnServerInitialized() {
            LoadConfig();
			LoadDefaultMessages();
            permission.RegisterPermission("Lottery.canuse", this);
		    if (DefaultWinRates != null)
		    {
		        IndividualRates = new Dictionary<string, int>();
		        foreach (var entry in DefaultWinRates)
		        {
		            int rate;
		            if (!int.TryParse(entry.Value.ToString(), out rate)) continue;
		            IndividualRates.Add(entry.Key, rate);
		        }

                var ServerRewardsDict = SRWinRates;
                SRRates = new Dictionary<string, int>();
		        if (ServerRewardsDict != null)
		        {
                    foreach (var entry in ServerRewardsDict)
                    {
                        int rate;
                        if (!int.TryParse(entry.Value.ToString(), out rate)) continue;
                        SRRates.Add(entry.Key, rate);
                    }
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
		}

        void Unload()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                GUIDestroy(player);
                GUIDestroy(player);
            }
            Puts("Data saved.");
            if(Currentbet != null)
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
                SendReply(player, lang.GetMessage("NoEconomy", this, player.UserIDString));
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
                        SendReply(player, lang.GetMessage("NotEnoughMoney", this, player.UserIDString));
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
            if (UseSR && ServerRewards.IsLoaded)
            {
                foreach (var elem in SRRates)
                {
                    var pos = 0.86 - (i - from)/10.0;
                    var pos2 = 0.91 - (i - from)/20.0;
                    win.Add(new CuiLabel
                    {
                        Text =
                        {
                            Text = elem.Key + ": " + elem.Value + " point(s)",
                            FontSize = 18,
                            Align = TextAnchor.MiddleCenter
                        },
                        RectTransform =
                        {
                            AnchorMin = $"{0.1} {pos}",
                            AnchorMax = $"{0.9} {pos2}"
                        }
                    }, containerwin);
                    i++;
                }
            }
            else
            {
                foreach (var elem in IndividualRates)
                {
                    if (i == 0)
                    {
                        var pos = 0.81 - (i - from)/10.0;
                        var pos2 = 0.86 - (i - from)/20.0;
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
                    else if (i >= from && i < from + 9)
                    {
                        var pos = 0.81 - (i - from)/10.0;
                        var pos2 = 0.86 - (i - from)/20.0;
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
            }

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

            if (UseSR && ServerRewards.IsLoaded)
            {
                var mini = string.Format(lang.GetMessage("MinimumSRBet", this, player.UserIDString), MinBetjackpot, SRjackpot);
                elements.Add(new CuiLabel
                {
                    Text =
                    {
                        Text = mini,
                        FontSize = 18,
                        Align = TextAnchor.MiddleCenter
                    },
                    RectTransform =
                    {
                        AnchorMin = "0.71 0.39",
                        AnchorMax = "0.99 0.59"
                    }
                }, container);
            }
            else
            {
                elements.Add(new CuiLabel
                {
                    Text =
                    {
                        Text = string.Format(lang.GetMessage("Roll", this, player.UserIDString), JackpotNumber, jackpots),
                        FontSize = 18,
                        Align = TextAnchor.MiddleCenter
                    },
                    RectTransform =
                    {
                        AnchorMin = "0.71 0.39",
                        AnchorMax = "0.99 0.59"
                    }
                }, container);
            }
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
        public double FindReward(BasePlayer player, int bet, int reference, int multiplicator = 1)
        {
            int findReward = 0;
            float reward = 0;
            int[] number = GetIntArray(reference);
            string newreference;
            if (UseSR && ServerRewards.IsLoaded)
            {
                #region jackpot

                if (reference == SRJackpotNumber)
                {
                    if (bet*multiplicator >= MinBetjackpot)
                    {
                        findReward = findReward*multiplicator + (int) SRjackpot;
                        return findReward;
                    }
                    else
                    {
                        SendReply(player, string.Format(lang.GetMessage("BetMore", this, player.UserIDString), MinBetjackpot));
                    }
                }

                #endregion

                #region full_match
                    if (DefaultBasePoint.Contains(reference.ToString()))
                    {
                        SRWinRates.TryGetValue("Match4Number", out findReward);
                        return findReward*multiplicator;
                    }

                #endregion

                #region three_match

                newreference = number[0].ToString() + number[1].ToString() + number[2].ToString() + "x";
                if (DefaultBasePoint.Contains(newreference))
                {
                    SRWinRates.TryGetValue("Match3Number", out findReward);
                    return findReward*multiplicator;
                }
                newreference = number[0].ToString() + number[1].ToString() + "x" + number[3].ToString();
                if (DefaultBasePoint.Contains(newreference))
                {
                    SRWinRates.TryGetValue("Match4Number", out findReward);
                    return findReward*multiplicator;
                }
                newreference = number[0].ToString() + "x" + number[2].ToString() + number[3].ToString();
                if (DefaultBasePoint.Contains(newreference))
                {
                    SRWinRates.TryGetValue("Match4Number", out findReward);
                    return findReward*multiplicator;
                }
                newreference = "x" + number[1].ToString() + number[2].ToString() + number[3].ToString();
                if (DefaultBasePoint.Contains(newreference))
                {
                    SRWinRates.TryGetValue("Match4Number", out findReward);
                    return findReward*multiplicator;
                }

                #endregion

                #region two_match

                newreference = number[0].ToString() + number[1].ToString() + "x" + "x";
                if (DefaultBasePoint.Contains(newreference))
                {
                    SRWinRates.TryGetValue("Match2Number", out findReward);
                    return findReward*multiplicator;
                }
                newreference = number[0].ToString() + "x" + "x" + number[3].ToString();
                if (DefaultBasePoint.Contains(newreference))
                {
                    SRWinRates.TryGetValue("Match2Number", out findReward);
                    return findReward*multiplicator;
                }
                newreference = "x" + "x" + number[2].ToString() + number[3].ToString();
                if (DefaultBasePoint.Contains(newreference))
                {
                    SRWinRates.TryGetValue("Match2Number", out findReward);
                    return findReward*multiplicator;
                }
                newreference = number[0].ToString() + "x" + number[2].ToString() + "x";
                if (DefaultBasePoint.Contains(newreference))
                {
                    SRWinRates.TryGetValue("Match2Number", out findReward);
                    return findReward*multiplicator;
                }
                newreference = "x" + number[1].ToString() + "x" + number[3].ToString();
                if (DefaultBasePoint.Contains(newreference))
                {
                    SRWinRates.TryGetValue("Match2Number", out findReward);
                    return findReward*multiplicator;
                }
                newreference = "x" + number[1].ToString() + number[2].ToString() + "x";
                if (DefaultBasePoint.Contains(newreference))
                {
                    SRWinRates.TryGetValue("Match2Number", out findReward);
                    return findReward*multiplicator;
                }

                #endregion

                #region one_match

                newreference = number[0].ToString() + "x" + "x" + "x";
                if (DefaultBasePoint.Contains(newreference))
                {
                    SRWinRates.TryGetValue("Match1Number", out findReward);
                    return findReward*multiplicator;
                }
                newreference = "x" + number[1].ToString() + "x" + "x";
                if (DefaultBasePoint.Contains(newreference))
                {
                    SRWinRates.TryGetValue("Match1Number", out findReward);
                    return findReward*multiplicator;
                }
                newreference = "x" + "x" + number[2].ToString() + "x";
                if (DefaultBasePoint.Contains(newreference))
                {
                    SRWinRates.TryGetValue("Match1Number", out findReward);
                    return findReward*multiplicator;
                }
                newreference = "x" + "x" + "x" + number[3].ToString();
                if (DefaultBasePoint.Contains(newreference))
                {
                    SRWinRates.TryGetValue("Match1Number", out findReward);
                    return findReward*multiplicator;
                }

                #endregion

            }
            else
            {
                #region jackpot
                if (reference == JackpotNumber)
                {
                    int jackpots = (int) Math.Round(Currentbet.Sum(v => v.Value.totalbet));
                    return bet*(Math.Round((double)findReward/100, 2)) * multiplicator + jackpots + jackpot;
                }
                #endregion

                #region full_match
                if (IndividualRates.ContainsKey(reference.ToString()))
                {
                    IndividualRates.TryGetValue(number.ToString(), out findReward);
                    return bet*(Math.Round((double)findReward/100, 2)) * multiplicator;
                }
                #endregion

                #region three_match
                newreference = number[0].ToString() + number[1].ToString() + number[2].ToString() + "x";
                IndividualRates.TryGetValue(newreference, out findReward);

                if(IndividualRates.ContainsKey(newreference))
                {
                    IndividualRates.TryGetValue(newreference, out findReward);
                    return bet*(Math.Round((double)findReward/100, 2)) * multiplicator;
                }
                newreference = number[0].ToString() + number[1].ToString() + "x" + number[3].ToString();
                if(IndividualRates.ContainsKey(newreference))
                {
                    IndividualRates.TryGetValue(newreference, out findReward);
                    return bet*(Math.Round((double)findReward/100, 2)) * multiplicator;
                }
                newreference = number[0].ToString() + "x" + number[2].ToString() + number[3].ToString();
                if(IndividualRates.ContainsKey(newreference))
                {
                    IndividualRates.TryGetValue(newreference, out findReward);
                    return bet*(Math.Round((double)findReward/100, 2)) * multiplicator;
                }
                newreference =  "x" + number[1].ToString() + number[2].ToString() + number[3].ToString();
                if(IndividualRates.ContainsKey(newreference))
                {
                    IndividualRates.TryGetValue(newreference, out findReward);
                    return bet*(Math.Round((double)findReward/100, 2)) * multiplicator;
                }
                #endregion

                #region two_match
                newreference = number[0].ToString() + number[1].ToString() + "x" + "x";
                if(IndividualRates.ContainsKey(newreference))
                {
                    IndividualRates.TryGetValue(newreference, out findReward);
                    return bet*(Math.Round((double)findReward/100, 2)) * multiplicator;
                }
                newreference = number[0].ToString() + "x" + "x" + number[3].ToString();
                if(IndividualRates.ContainsKey(newreference))
                {
                    IndividualRates.TryGetValue(newreference, out findReward);
                    return bet*(Math.Round((double)findReward/100, 2)) * multiplicator;
                }
                newreference =  "x" + "x" + number[2].ToString() + number[3].ToString();
                if(IndividualRates.ContainsKey(newreference))
                {
                    IndividualRates.TryGetValue(newreference, out findReward);
                    return bet*(Math.Round((double)findReward/100, 2)) * multiplicator;
                }
                newreference = number[0].ToString() + "x" + number[2].ToString() + "x";
                if(IndividualRates.ContainsKey(newreference))
                {
                    IndividualRates.TryGetValue(newreference, out findReward);
                    return bet*(Math.Round((double)findReward/100, 2)) * multiplicator;
                }
                newreference = "x" + number[1].ToString() + "x" + number[3].ToString();
                if(IndividualRates.ContainsKey(newreference))
                {
                    IndividualRates.TryGetValue(newreference, out findReward);
                    return bet*(Math.Round((double)findReward/100, 2)) * multiplicator;
                }
                newreference =  "x" + number[1].ToString() + number[2].ToString() + "x";
                if(IndividualRates.ContainsKey(newreference))
                {
                    IndividualRates.TryGetValue(newreference, out findReward);
                    return bet*(Math.Round((double)findReward/100, 2)) * multiplicator;
                }
               #endregion

                #region one_match
                newreference = number[0].ToString() + "x" + "x" + "x";
                if(IndividualRates.ContainsKey(newreference))
                {
                    IndividualRates.TryGetValue(newreference, out findReward);
                    return bet*(Math.Round((double)findReward/100, 2)) * multiplicator;
                }
                newreference =  "x" + number[1].ToString() + "x" + "x";
                if(IndividualRates.ContainsKey(newreference))
                {
                    IndividualRates.TryGetValue(newreference, out findReward);
                    return bet*(Math.Round((double)findReward/100, 2)) * multiplicator;
                }
                newreference =  "x" + "x" + number[2].ToString() + "x";
                if(IndividualRates.ContainsKey(newreference))
                {
                    IndividualRates.TryGetValue(newreference, out findReward);
                    return bet*(Math.Round((double)findReward/100, 2)) * multiplicator;
                }
                newreference = "x" + "x" + "x" + number[3].ToString();
                if(IndividualRates.ContainsKey(newreference))
                {
                    IndividualRates.TryGetValue(newreference, out findReward);
                    return bet*(Math.Round((double)findReward/100, 2)) * multiplicator;
                }
    #endregion
            }

            return findReward;
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
                SendReply(arg.Player(), lang.GetMessage("NoBet", this, arg.Player().UserIDString));
                return;
            }
            int random = UnityEngine.Random.Range(DefaultMinRange, DefaultMaxRange);
            if (UseSR && ServerRewards.IsLoaded)
            {
                if (SRMinBet <= playerbet.currentbet*playerbet.multiplicator)
                {
                    double reward = FindReward(arg.Player(), (int)playerbet.currentbet, random, playerbet.multiplicator);
                    if(playerbet.currentbet*playerbet.multiplicator >= MinBetjackpot)
                        if (random == SRJackpotNumber)
                        {
                            foreach (var resetbet in Currentbet)
                            {
                                resetbet.Value.totalbet = 0;
                                resetbet.Value.multiplicator = 1;
                                playerinfos.Add(resetbet.Key, resetbet.Value);
                            }
                            Currentbet.Clear();
                            Currentbet = playerinfos;
                            ServerRewards?.Call("AddPoints", new object[] { arg.Player().userID, reward });
                            SendReply(arg.Player(), string.Format(lang.GetMessage("Jackpot", this, arg.Player().UserIDString), random, reward));
                            return;
                        }
                    if (reward != 0 && random != SRJackpotNumber)
                    {
                        Currentbet.Remove(arg.Player().userID);
                        Currentbet.Add(arg.Player().userID, playerbet);
                        ServerRewards?.Call("AddPoints", new object[] { arg.Player().userID, reward });
                        SendReply(arg.Player(), string.Format(lang.GetMessage("WinPoints", this, arg.Player().UserIDString), random, reward));
                    }
                    else if (reward == 0)
                    {
                        SendReply(arg.Player(), string.Format(lang.GetMessage("NoWin", this, arg.Player().UserIDString), random));
                    }
                    else
                    {
                        ServerRewards?.Call("AddPoints", new object[] { arg.Player().userID, reward });
                        SendReply(arg.Player(), string.Format(lang.GetMessage("WinPoints", this, arg.Player().UserIDString), random, reward));
                    }

                    playerbet.totalbet += playerbet.currentbet*(10/100.0);
                    Economy?.CallHook("Withdraw", arg.Player().userID, playerbet.currentbet*playerbet.multiplicator);
                    playerbet.currentbet = 0;
                    playerbet.multiplicator = 1;
                }
                else
                {
                    SendReply(arg.Player(), string.Format(lang.GetMessage("MiniSRBet", this, arg.Player().UserIDString), SRMinBet));
                }
            }
            else
            {
                double reward = FindReward(arg.Player(), (int)playerbet.currentbet, random, playerbet.multiplicator);
                if (random == JackpotNumber)
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
                    SendReply(arg.Player(), string.Format(lang.GetMessage("Jackpot", this, arg.Player().UserIDString), random, reward));
                }
                else if (reward != 0 && random != JackpotNumber)
                {
                    Currentbet.Remove(arg.Player().userID);
                    Currentbet.Add(arg.Player().userID, playerbet);
                    Economy?.CallHook("Deposit", arg.Player().userID, reward);
                    SendReply(arg.Player(), string.Format(lang.GetMessage("Win", this, arg.Player().UserIDString), random, reward));
                }
                else if (reward == 0)
                {
                    SendReply(arg.Player(), lang.GetMessage("NoWin", this, arg.Player().UserIDString));
                }
                else
                {
                    Economy?.CallHook("Deposit", arg.Player().userID, reward);
                    SendReply(arg.Player(), string.Format(lang.GetMessage("Win", this, arg.Player().UserIDString), random, reward));
                }

                playerbet.totalbet += playerbet.currentbet*(10/100.0);
                Economy?.CallHook("Withdraw", arg.Player().userID, playerbet.currentbet*playerbet.multiplicator);
                playerbet.currentbet = 0;
                playerbet.multiplicator = 1;
            }
            SaveData(Currentbet);
        }

        #endregion
    }
}