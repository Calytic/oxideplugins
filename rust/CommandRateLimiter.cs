using System;
using System.Collections.Generic;
using System.Linq;
using Oxide.Core;
using UnityEngine;
using Oxide.Core.Libraries.Covalence;

namespace Oxide.Plugins
{
    [Info("CommandRateLimiter", "Calytic", "0.0.9", ResourceId = 1812)]
    public class CommandRateLimiter : CovalencePlugin
    {
        private int KickAfter;
        private int CooldownMS;
        private int ClearRateCountSeconds;
        private bool LogExcessiveUsage;
        private bool SendPlayerMessage;

        Dictionary<string, DateTime> lastRun = new Dictionary<string, DateTime>();
        Dictionary<string, Timer> rateTimer = new Dictionary<string, Timer>();
        Dictionary<string, int> rateCount = new Dictionary<string, int>();
        List<object> commandWhitelist = new List<object>();

        private Dictionary<string, Dictionary<string, int>> spamLog = new Dictionary<string, Dictionary<string, int>>();

        void OnServerInitialized()
        {
            Config["CooldownMS"] = CooldownMS = GetConfig("CooldownMS", 195);
            Config["KickAfter"] = KickAfter = GetConfig("KickAfter", 10);
            Config["ClearRateCountSeconds"] = ClearRateCountSeconds = GetConfig("ClearRateCountSeconds", 5);
            Config["LogExcessiveUsage"] = LogExcessiveUsage = GetConfig("LogExcessiveUsage", false);
            Config["SendPlayerMessage"] = SendPlayerMessage = GetConfig("SendPlayerMessage", true);
            Config["CommandWhitelist"] = commandWhitelist = GetConfig("CommandWhitelist", new List<object>());
            Config["Version"] = Version.ToString();
            SaveConfig();
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
            Config["SendPlayerMessage"] = true;
            Config["CommandWhitelist"] = new List<string>();
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

        object OnServerCommand(ConsoleSystem.Arg arg)
        {
            if (arg.cmd == null) return null;

            if (arg.Player() == null)
            {
                return null;
            }

            IPlayer player = null;
            player = covalence.Players.FindPlayerById(arg.Player().UserIDString);
            if (player == null) return null;

            if (player.IsConnected == false) return null;

            if (arg.isAdmin)
                return null;

            DateTime lastTime;

            if (lastRun.TryGetValue(player.Id, out lastTime))
            {
                TimeSpan ts = DateTime.Now - lastTime;
                if (ts.TotalMilliseconds < CooldownMS)
                {
                    if (arg.cmd.name != null)
                    {
                        if (commandWhitelist.Contains(arg.cmd.name))
                        {
                            return null;
                        }

                        if (LogExcessiveUsage)
                        {
                            if (!spamLog.ContainsKey(player.Id))
                            {
                                spamLog.Add(player.Id, new Dictionary<string, int>());

                                timer.In(ClearRateCountSeconds, delegate()
                                {
                                    List<string> msgs = new List<string>();

                                    foreach (KeyValuePair<string, int> kvp in spamLog[player.Id])
                                    {
                                        msgs.Add(kvp.Key + " (" + kvp.Value + ")");
                                    }
                                    string cmds = string.Join(", ", msgs.ToArray());
                                    PrintWarning(player.Name + " (" + player.Id + ") spamming commands: " + cmds);
                                    spamLog.Remove(player.Id);
                                });
                            }

                            if (!spamLog[player.Id].ContainsKey(arg.cmd.name))
                            {
                                spamLog[player.Id].Add(arg.cmd.name, 1);
                            }
                            else
                            {
                                spamLog[player.Id][arg.cmd.name]++;
                            }
                        }
                    }

                    int c = 0;
                    bool kicked = false;
                    if (rateCount.TryGetValue(player.Id, out c))
                    {
                        rateCount[player.Id]++;
                        if (KickAfter > 0 && (c + 1) >= KickAfter)
                        {
                            player.Kick(GetMsg("Kick Message"));
                            kicked = true;
                        }
                    }
                    else
                    {
                        rateCount.Add(player.Id, 1);
                    }

                    if (ClearRateCountSeconds > 0)
                    {
                        Timer rtimer;
                        if (rateTimer.TryGetValue(player.Id, out rtimer))
                        {
                            if (!rtimer.Destroyed)
                            {
                                rtimer.Destroy();
                            }

                            rateTimer.Remove(player.Id);
                        }

                        timer.In(ClearRateCountSeconds, delegate()
                        {
                            if (rateCount.TryGetValue(player.Id, out c))
                            {
                                rateCount[player.Id] = 0;
                            }
                        });
                    }
                    if (player != null && SendPlayerMessage && !kicked)
                    {
                        player.Reply(GetMsg("Player Message"));
                    }
                }
                else
                {
                    lastRun[player.Id] = DateTime.Now;
                }
            }
            else
            {
                lastRun.Add(player.Id, DateTime.Now);
            }

            return null;
        }

        void OnUserDisconnected(IPlayer player)
        {
            int c = 0;
            if (rateCount.TryGetValue(player.Id, out c))
            {
                rateCount.Remove(player.Id);
            }
        }

        string GetMsg(string key, object userID = null)
        {
            return lang.GetMessage(key, this, userID == null ? null : userID.ToString());
        }
    }
}
