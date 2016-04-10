using System;
using System.Collections.Generic;
using Oxide.Core;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("CommandRateLimiter", "Calytic", "0.0.5", ResourceId = 1231)]
    public class CommandRateLimiter : RustPlugin
    {
        private int KickAfter;
        private int CooldownMS;
        private int ClearRateCountSeconds;
        private bool LogExcessiveUsage;

        Dictionary<ulong, DateTime> lastRun = new Dictionary<ulong, DateTime>();
        Dictionary<ulong, Timer> rateTimer = new Dictionary<ulong, Timer>();
        Dictionary<ulong, int> rateCount = new Dictionary<ulong, int>();

        void OnServerInitialized()
        {
            CooldownMS = GetConfig("CooldownMS", 195);
            KickAfter = GetConfig("KickAfter", 10);
            ClearRateCountSeconds = GetConfig("ClearRateCountSeconds", 5);
            LogExcessiveUsage = GetConfig("LogExcessiveUsage", false);
            GetConfig("Version", Version.ToString());
            LoadMessages();
        }

        void LoadMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                {"Player Message", "You are doing that too often"},
                {"Kick Message", "Spamming"},
            }, this);
        }

        void LoadDefaultConfig()
        {
            Config["CooldownMS"] = 195;
            Config["KickAfter"] = 10;
            Config["ClearRateCountSeconds"] = 5;
            Config["LogExcessiveUsage"] = false;
            Config["Version"] = Version.ToString();
        }

        private T GetConfig<T>(string name, T defaultValue)
        {
            if (Config[name] == null)
            {
                return defaultValue;
            }

            return (T)Convert.ChangeType(Config[name], typeof(T));
        }

        object OnRunCommand(ConsoleSystem.Arg arg)
        {
            if (arg.cmd == null) return null;

            if (arg.connection != null)
            {
                if (arg.Player() == null) return true;
            }

            BasePlayer player = arg.Player();
            if (player == null)
                return null;

            if (player.IsAdmin())
                return null;

            DateTime lastTime;

            if(lastRun.TryGetValue(player.userID, out lastTime)) {
                TimeSpan ts = DateTime.Now - lastTime;
                if(ts.TotalMilliseconds < CooldownMS) {
                    if (LogExcessiveUsage && arg.cmd.name != null)
                    {
                        PrintWarning(player.displayName + " (" + player.UserIDString + ") spamming command: " + arg.cmd.name);
                    }

                    int c = 0;
                    if (rateCount.TryGetValue(player.userID, out c))
                    {
                        rateCount[player.userID]++;
                        if (KickAfter > 0 && (c + 1) >= KickAfter)
                        {
                            player.Kick(GetMsg("Kick Message"));
                        }
                    }
                    else
                    {
                        rateCount.Add(player.userID, 1); 
                    }

                    if (ClearRateCountSeconds > 0)
                    {
                        Timer rtimer;
                        if (rateTimer.TryGetValue(player.userID, out rtimer))
                        {
                            if (!rtimer.Destroyed)
                            {
                                rtimer.Destroy();
                            }

                            rateTimer.Remove(player.userID);
                        }

                        timer.In(ClearRateCountSeconds, delegate()
                        {
                            if (!player.IsConnected()) return;
                            if (rateCount.TryGetValue(player.userID, out c))
                            {
                                rateCount[player.userID] = 0;
                            }
                        });
                    }
                   
                    SendReply(player, GetMsg("Player Message"));
                } else {
                    lastRun[player.userID] = DateTime.Now;
                }
            } else {
                lastRun.Add(player.userID, DateTime.Now);
            }

            return null;
        }

        void OnPlayerDisconnected(BasePlayer player)
        {
            int c = 0;
            if (rateCount.TryGetValue(player.userID, out c))
            {
                rateCount.Remove(player.userID);
            }
        }

        string GetMsg(string key, object userID = null)
        {
            return lang.GetMessage(key, this, userID == null ? null : userID.ToString());
        }
    }
}
