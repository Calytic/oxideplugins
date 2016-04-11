// Reference: Oxide.Ext.Unity

/* 
 * Many thanks to feramor@computer.org for his HappyHour.cs plugin, which 
 * gave me examples of how to work with dates and timers in a plugin.
 */

/*
 * The MIT License (MIT)
 * Copyright (c) 2015 #db_arcane
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:

 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.

 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 */

using System;
using System.Collections.Generic;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Core.Libraries;

namespace Oxide.Plugins
{
    [Info("SchedShutdown", "db_arcane", "1.1.2")]
    public class SchedShutdown : RustPlugin
    {   
        static List<Oxide.Core.Libraries.Timer.TimerInstance> Timers = new List<Oxide.Core.Libraries.Timer.TimerInstance>();
        Oxide.Core.Libraries.Timer MainTimer;
        string TimeFormat = "HH:mm:ss";
        string EnabledStr = "enabled";
        string DisabledStr = "disabled";

        void Init()
        {
            LoadConfig();
            CleanupConfig();
            MainTimer = Interface.GetMod().GetLibrary<Oxide.Core.Libraries.Timer>("Timer");
        }

        [ConsoleCommand("schedule.shutdown")]
        private void ScheduleShutdown(ConsoleSystem.Arg arg)
        {
            string param = arg.ArgsStr.ToString();
            
            if (param == "")
            {
                 PrintShutdownStatus();
                 return;
            }    
            
            if (param == "enable")
            {
                if (Config["UTC_Time"].ToString() == "") {
                    this.Puts("The shutdown time has not been configured yet. The shutdown timer remains disabled.");
                    return;
                }
                
                Config["Status"] = EnabledStr;
                SaveConfig();
                ResetShutdownTimer();
                return;
            }
            
            if (param == DisabledStr)
            {
                Config["Status"] = DisabledStr;
                SaveConfig();
                ResetShutdownTimer();
                return;
            }
            
            if (!IsTimeValid(param))
            {
				this.Puts("The time entered was unreadable, must be in format like '01:30:00'. No changes have been made. ");
                PrintShutdownStatus();
                return;
            }
            Config["Status"] = EnabledStr;
            Config["UTC_Time"] = param;
            SaveConfig();
            ResetShutdownTimer();
        }
        
        [HookMethod("OnServerInitialized")]
        void myOnServerInitialized()
        {
            DateTime configTime; 
            DateTime mainTime = DateTime.UtcNow;
            
            if (Config["Status"].ToString() == DisabledStr) {
                PrintShutdownStatus();
				return;
			}
                
            // Set up timer for server save and shutdown
            try
            {
                configTime = DateTime.ParseExact(Config["UTC_Time"].ToString(), TimeFormat, null);                 
            }
            catch (Exception e)
            {
                PrintShutdownStatus();
                return;
            }
            
            DateTime shutdownTime = new DateTime(mainTime.Year, mainTime.Month, mainTime.Day, configTime.Hour, configTime.Minute, configTime.Second, DateTimeKind.Utc);
            if (mainTime > shutdownTime) 
            {
                shutdownTime = shutdownTime.AddDays(1);
            }
            long shutdownInterval = Convert.ToInt64((shutdownTime - mainTime).TotalSeconds);

            // schedule the server save command.
            Oxide.Core.Libraries.Timer.TimerInstance newTimer = MainTimer.Once(shutdownInterval, () => ConsoleSystem.Run.Server.Normal("server.save"));
            Timers.Add(newTimer);

            // schedule the restart command.  Restart simply shuts down the server after a 60-second countdown
            newTimer = MainTimer.Once(shutdownInterval, () => ConsoleSystem.Run.Server.Normal("restart"));
            Timers.Add(newTimer);
            
            PrintShutdownStatus();
        }
        
        [HookMethod("Unload")]
        void myUnload()
        {
            foreach (Oxide.Core.Libraries.Timer.TimerInstance CurrentTimer in Timers)
            {
                if (CurrentTimer != null)
                    if (CurrentTimer.Destroyed == false)
                        CurrentTimer.Destroy();
            }
            Timers.Clear();
        }

        [HookMethod("LoadDefaultConfig")]
        void myLoadDefaultConfig()
        {
            Config["Status"] = DisabledStr;
            Config["UTC_Time"] = "";
            SaveConfig();
        }

        private void ResetShutdownTimer()
        {
            myUnload();
            myOnServerInitialized();
        }
        
        private void PrintShutdownStatus()
        {
            string status = (Config["Status"].ToString() == DisabledStr) ? "DISABLED" : "ENABLED";
            string schedTime = Config["UTC_Time"].ToString();
            schedTime = (schedTime == "") ? "blank" : schedTime + " UTC" ;

            this.Puts("Shutdown timer is " + status + ", configured shutdown time is " + schedTime);
        }
        
        private void CleanupConfig()
        {
            string status = Config["Status"].ToString().ToLower();
            Config["Status"] = ((status != EnabledStr) && (status != DisabledStr)) ? DisabledStr : status;
            
            if (!IsTimeValid(Config["UTC_Time"].ToString())) 
            {
               Config["UTC_Time"] = "";
               Config["Status"] = DisabledStr;
            }
            SaveConfig();
        }
        
        private bool IsTimeValid(string timeString)
        {
            DateTime temp;

            try
            {
                temp = DateTime.ParseExact(timeString, TimeFormat, null);
            }
            catch (Exception e)
            {
                return false;
            }
            return true;
        }
        
    }
}
