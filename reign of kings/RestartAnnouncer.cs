
using System;
using System.Collections.Generic;
using CodeHatch.Build;
using Oxide.Core;
using Oxide.Core.Configuration;
using CodeHatch.Engine.Administration;
using CodeHatch.Engine.Networking;
using CodeHatch.Permissions;
using CodeHatch.Common;

namespace Oxide.Plugins
{
    [Info("Restart Timer", "D-Kay", "1.1")]
    public class RestartAnnouncer : ReignOfKingsPlugin {
        
        private int time;
        private int timeRemaining;
        private int timerType;
        private bool timerRunning = false;
        
        private List<object> defaultNotifyTimes = new List<object>()
        {
            1,
            2,
            3,
            4,
            5,
            10,
            15,
            20,
            30,
            45,
            60
        };

        private List<object> notifyTimes => GetConfig("NotifyTimes", defaultNotifyTimes);
        
        protected override void LoadDefaultConfig()
        {
            Config["NotifyTimes"] = notifyTimes;
            SaveConfig();
        }

        T GetConfig<T>(string name, T defaultValue)
        {
            if (Config[name] == null) return defaultValue;
            return (T)Convert.ChangeType(Config[name], typeof(T));
        }

        private void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                { "presetMessage", "[[64CEE1]Server[FFFFFF]]: " },
                { "restartMessage", "Server will restart in {0} minute(s) due to maintenance." },
                { "shutdownMessage", "Server will shutdown in {0} minute(s) due to maintenance." },
                { "unauthorizedUsage", "Unauthorized to use this command." },
                { "ongoingTimer", "There is already a timer running."}
            }, this);
        }

        private void Loaded()
        {
            LoadDefaultConfig();
            LoadDefaultMessages();

            permission.RegisterPermission("RestartAnnouncer.restart", this);
            permission.RegisterPermission("RestartAnnouncer.shutdown", this);

            time = 0;
            timerType = 0;
            timeRemaining = -1;
        }
		
		[ChatCommand("trestart")]
        private void RestartTimerCommand(Player player, string cmd, string[] input)
        {
            if (!player.HasPermission("RestartAnnouncer.restart"))
            {
                PrintToChat(player, GetMessage("unauthorizedUsage", player.Id.ToString()));
                return;
            }
            if (timerRunning) { PrintToChat(player, GetMessage("ongoingTimer", player.Id.ToString())); return; }
            int repeatTimes = Convert.ToInt32(input[0]);
            timeRemaining = Convert.ToInt32(input[0]);
            time = DateTime.Now.Minute;
            timerType = 1;
            sendMessage();
            timerRunning = true;
            timer.Repeat(60, repeatTimes, sendMessage);
        }
		
		[ChatCommand("tshutdown")]
        private void ShutdownTimerCommand(Player player, string cmd, string[] input)
        {
            if (!player.HasPermission("RestartAnnouncer.shutdown"))
            {
                PrintToChat(player, GetMessage("unauthorizedUsage", player.Id.ToString()));
                return;
            }
            if (timerRunning) { PrintToChat(player, GetMessage("ongoingTimer", player.Id.ToString())); return; }
            int repeatTimes = Convert.ToInt32(input[0]);
            timeRemaining = Convert.ToInt32(input[0]);
            time = DateTime.Now.Minute;
            timerType = 2;
            sendMessage();
            timerRunning = true;
            timer.Repeat(60, repeatTimes, sendMessage);
        }

        private void sendMessage()
        {
            string message = "";
            if (timerType == 1)
            {
                message = GetMessage("presetMessage") + string.Format(GetMessage("restartMessage"), timeRemaining.ToString());
            }
            else if (timerType == 2)
            {
                message = GetMessage("presetMessage") + string.Format(GetMessage("shutdownMessage"), timeRemaining.ToString());
            }

            if (timeRemaining == 0)
            {
                if (timerType == 1)
                {
                    SocketAdminConsole.RestartAfterShutdown = true;
                }
                else if (timerType == 2)
                {
                    SocketAdminConsole.RestartAfterShutdown = false;
                }
                Server.Shutdown();
            }
            else
            {
                foreach (var notify in notifyTimes)
                {
                    if (Convert.ToInt32(notify) == timeRemaining)
                    {
                        PrintToChat(message);
                    }
                }
            }
            timeRemaining--;
        }

        string GetMessage(string key, string userId = null) => lang.GetMessage(key, this, userId);
    }
}
