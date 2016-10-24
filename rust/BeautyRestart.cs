using Oxide.Core;
using System;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using ConVar;
using Oxide.Game.Rust.Cui;

namespace Oxide.Plugins
{
    [Info("BeautyRestart", "Reynostrum", "1.0.0")]
    [Description("Restart the server with a cooldown GUI timer.")]
    class BeautyRestart : RustPlugin
    {
        #region Init/config
        int TimerSeconds;
        int Minutes;
        int Hours;
        int Seconds;
        string HoursF;
        string MinutesF;
        string SecondsF;
        string TimerMessage;
        bool IsTiming;
        static Timer TimerRefresh;
        string GUIAnchorMin => GetConfig("GUIAnchorMin", "0.401 0.85");
        string GUIAnchorMax => GetConfig("GUIAnchorMax", "0.587 0.90");
        string GUITextColor => GetConfig("GUITextColor", "255 0 0");
        string GUIBackgroundColor => GetConfig("GUIBackgroundColor", "0 0 0 0.90");
        protected override void LoadDefaultConfig()
        {
            Config["GUIAnchorMin"] = GUIAnchorMin;
            Config["GUIAnchorMax"] = GUIAnchorMax;
            Config["GUITextColor"] = GUITextColor;
            Config["GUIBackgroundColor"] = GUIBackgroundColor;
            SaveConfig();
        }
        #endregion

        #region Commands
        [ConsoleCommand("brestart")]
        void CMDConsoleRestart(ConsoleSystem.Arg arg)
        {
            if (arg.connection == null)
            {
                if (arg.Args == null || arg.Args.Length != 2)
                {
                    SendReply(arg, Lang("Error"));
                    return;
                }
                else Checkcommand(arg, null, arg.Args[0], arg.Args[1], true);
            }
            else SendReply(arg, Lang("NotAllowed"));
        }
        [ChatCommand("brestart")]
        void CMDCommandRestart(BasePlayer player, string cmd, string[] args)
        {
            if (HasPermission(player, "BeautyRestart.Restart") || player.IsAdmin())
            {
                if (args == null || args.Length != 2)
                {
                    SendReply(player, Lang("Error"));
                    return;
                }
                else Checkcommand(null, player, args[0], args[1], false);
            }
            else SendReply(player, Lang("NotAllowed", player.UserIDString));
        }
        [ConsoleCommand("bcancelrestart")]
        void CMDConsoleCancelRestart(ConsoleSystem.Arg arg)
        {
            if (arg.connection == null)
            {
                Abort();
                SendReply(arg, Lang("CancelRestart"));
            }
            else SendReply(arg, Lang("NotAllowed"));
        }
        [ChatCommand("bcancelrestart")]
        void CMDCommandCancelRestart(BasePlayer player, string cmd, string[] args)
        {
            if (HasPermission(player, "BeautyRestart.Restart") || player.IsAdmin())
            {
                Abort();
                SendReply(player, Lang("CancelRestart", player.UserIDString));
            }
            else SendReply(player, Lang("NotAllowed", player.UserIDString));
        }
        #endregion

        #region Basic hooks
        void Loaded()
        {
            lang.RegisterMessages(Messages, this);
            permission.RegisterPermission("BeautyRestart.Restart", this);
            LoadDefaultConfig();
        }
        void Unload()
        {
            Abort();
            if (TimerRefresh != null) { TimerRefresh.Destroy(); }
        }
        #endregion

        #region Functions
        void Checkcommand(ConsoleSystem.Arg arg, BasePlayer player, string minutes, string name, bool console)
        {
            int Num;
            bool isNum = int.TryParse(minutes, out Num);
            if (isNum && name != "")
            {
                CreatingTimer(Num, name);
                if (console) SendReply(arg, Lang("TimerSet", null, StyleSeconds(Num * 60)));
            }
            else
            {
                if (console) SendReply(arg, Lang("Error"));
                else SendReply(player, Lang("Error", player.UserIDString));
            }
        }
        void Abort()
        {
            if (TimerRefresh != null) TimerRefresh.Destroy();
            foreach (BasePlayer player in BasePlayer.activePlayerList) FULLGUIHandler(player, "Unload");
            timer.Once(1f, () =>
            {
                foreach (BasePlayer player in BasePlayer.activePlayerList) FULLGUIHandler(player, "Unload");
            });
            Hours = 0;
            Minutes = 0;
            Seconds = 0;
            IsTiming = false;
            PrintToChat(Lang("GlobalCanceled"));
        }
        string StyleSeconds(int SecondsT)
        {
            string Format;
            Hours = SecondsT / 3600;
            Minutes = (SecondsT - Hours * 3600) / 60;
            Seconds = SecondsT - (Hours * 3600 + Minutes * 60);
            if (Seconds <= 9) SecondsF = "0" + Seconds;
            else SecondsF = Seconds.ToString();
            if (Minutes <= 9) MinutesF = "0" + Minutes;
            else MinutesF = Minutes.ToString();
            if (Hours <= 9) HoursF = "0" + Hours;
            else HoursF = Hours.ToString();
            if (Hours == 0) Format = MinutesF + "m " + SecondsF + "s";
            else Format = HoursF + "h " + MinutesF + "m " + SecondsF + "s";
            return Format;
        }
        void DoRestart()
        {
            BasePlayer[] array = BasePlayer.activePlayerList.ToArray();
            for (int i = 0; i < (int)array.Length; i++)
            {
                BasePlayer basePlayer = array[i];
                basePlayer.Kick(TimerMessage);
            }
            timer.Once(2, () => ConsoleSystem.Run.Server.Normal("quit"));
        }
        void TimeIn()
        {
            PrintToChat(Lang("TimerSet", null, StyleSeconds(TimerSeconds)));
            TimerRefresh = timer.Repeat(1f, TimerSeconds, () =>
            {
                TimerSeconds--;
                
                if (Seconds == 1 && Minutes == 0 && Hours == 0)
                {
                    Puts("Restarting...");
                    DoRestart();
                    return;
                }
                foreach (BasePlayer player in BasePlayer.activePlayerList)
                {
                    CuiHelper.DestroyUi(player, "GUITexto");
                    GUITexto(player);
                }
                NextFrame(() =>
                {
                    if (Seconds == 0)
                    {
                        Puts(Lang("TimerSet", null, StyleSeconds(TimerSeconds)));
                        PrintToChat(Lang("TimerSet", null, StyleSeconds(TimerSeconds)));
                    }
                });
            });
        }     
        void CreatingTimer(int CreatingTimerMinutes, string TimerName)
        {
            if (IsTiming) return;
            IsTiming = true;
            TimerMessage = TimerName;
            TimerSeconds = CreatingTimerMinutes * 60;
            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                FULLGUIHandler(player, "Unload");
                FULLGUIHandler(player, "Load");
            };
            TimeIn();
        }      
        void FULLGUIHandler(BasePlayer player, string Command)
        {
            if (Command == "Unload")
            {
                CuiHelper.DestroyUi(player, "GUITexto");
                CuiHelper.DestroyUi(player, "GUIBackground");
            }
            else if (Command == "Load")
            {
                if (!IsTiming) return;
                GUIBackground(player);
                GUITexto(player);
            }
        }
        #endregion
        
        #region GUI
        void GUITexto(BasePlayer player)
        {
            var GUIElementn = new CuiElementContainer();
            var GUIBackgroundn = GUIElementn.Add(new CuiLabel
            {
                Text =
                    {
                        Text = StyleSeconds(TimerSeconds),
                        FontSize = 12,
                        Align = TextAnchor.LowerCenter,
                        Color = GUITextColor
                    },
                RectTransform =
                    {
                        AnchorMin = GUIAnchorMin + "5",
                        AnchorMax = GUIAnchorMax
                    }
            }, "Hud", "GUITexto");
            CuiHelper.AddUi(player, GUIElementn);
        }
        void GUIBackground(BasePlayer player)
        {
            var GUIElementn = new CuiElementContainer();
            var GUIBackgroundn = GUIElementn.Add(new CuiPanel
            {
                Image =
                    {
                        Color = GUIBackgroundColor
                    },
                RectTransform =
                    {
                        AnchorMin = GUIAnchorMin,
                        AnchorMax = GUIAnchorMax
                    },
                CursorEnabled = false
            }, "Hud", "GUIBackground");
            GUIElementn.Add(new CuiLabel
            {
                Text =
                    {
                        Text = TimerMessage.ToUpper(),
                        FontSize = 14,
                        Align = TextAnchor.UpperCenter,
                        Color = GUITextColor
                    },
                RectTransform =
                    {
                        AnchorMin = "0.00 0.1",
                        AnchorMax = "1 0.9"
                    }
            }, GUIBackgroundn);
            CuiHelper.AddUi(player, GUIElementn);
        }
        #endregion

        #region Oxide hooks
        void OnPlayerInit(BasePlayer player)
        {
            if (player.HasPlayerFlag(BasePlayer.PlayerFlags.ReceivingSnapshot))
            {
                timer.Once(1, () => OnPlayerInit(player));
                return;
            }
            FULLGUIHandler(player, "Unload");
            FULLGUIHandler(player, "Load");
        }
        #endregion

        #region Helpers
        T GetConfig<T>(string name, T defaultValue) => Config[name] == null ? defaultValue : (T)Convert.ChangeType(Config[name], typeof(T));
        string Lang(string key, string id = null, params object[] args) => string.Format(lang.GetMessage(key, this, id), args);
        bool HasPermission(BasePlayer player, string perm) => permission.UserHasPermission(player.UserIDString, perm);
        Dictionary<string, string> Messages = new Dictionary<string, string>
        {
            {"NotAllowed", "You are not allowed to use this command." },
            {"CancelRestart", "You have canceled the restart." },
            {"TimerSet", "The server will restart in {0}." },
            {"Error", "Use: brestart (minutes) (message)" },
            {"GlobalCanceled", "Server restart has been canceled." }
        };
        #endregion
    }
}