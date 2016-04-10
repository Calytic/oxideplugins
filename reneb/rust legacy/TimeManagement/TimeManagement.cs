using System.Collections.Generic;
using System;
using System.Reflection;
using System.Data;
using UnityEngine;
using Oxide.Core;
using Oxide.Core.Plugins;
using RustProto;

namespace Oxide.Plugins
{
    [Info("TimeManagement", "Reneb", "1.0.0")]
    class TimeManagement : RustLegacyPlugin
    {
        /////////////////////////////
        // FIELDS
        /////////////////////////////

        bool pluginActivated = true;
        Plugins.Timer plugintimer;
        float cachedTime;
        float cachedDelta;
        float cachedCurrentTime;
        float cachedDaytime;
        float cachedNighttime;
        /////////////////////////////
        // Config Management
        /////////////////////////////

        public static string notAllowed = "You are not allowed to use this command.";
        public static int dayTimeSeconds = 2700;
        public static int nightTimeSeconds = 900;
        public static int dayTime = 7;
        public static int nightTime = 19;
        public static int startTime = 12;
        public static bool freezeTime = false;

        void LoadDefaultConfig() { }

        private void CheckCfg<T>(string Key, ref T var)
        {
            if (Config[Key] is T)
                var = (T)Config[Key]; 
            else
                Config[Key] = var;
        } 

        void Init()
        {
            CheckCfg<string>("Messages: Not Allowed", ref notAllowed);
            CheckCfg<int>("Settings: Day Starts At", ref dayTime);
            CheckCfg<int>("Settings: Night Stats At", ref nightTime);
            CheckCfg<int>("Settings: Day length in seconds", ref dayTimeSeconds);
            CheckCfg<int>("Settings: Night length in seconds", ref nightTimeSeconds);
            CheckCfg<int>("Settings: Start Time", ref startTime);
            CheckCfg<bool>("Settings: Freeze Time", ref freezeTime);
            SaveConfig();
        }


        /////////////////////////////
        // Oxide Hooks
        ///////////////////////////// 

        void Loaded()
        {
            if (!permission.PermissionExists("cantime")) permission.RegisterPermission("cantime", this);
            if (!permission.PermissionExists("all")) permission.RegisterPermission("all", this);
        }
        void OnServerSave()
        {
        }
        void OnServerInitialized()
        {
            InitiateTime(true);
        }
        void Unload()
        {
            plugintimer.Destroy();  
        } 
        void InitiateTime(bool settime)
        {
            if(plugintimer != null) plugintimer.Destroy();
            env.daylength = 999999999f;
            env.nightlength = 99999999f;
            if(settime) EnvironmentControlCenter.Singleton.SetTime( Convert.ToSingle(startTime) );
            cachedDaytime = Convert.ToSingle(dayTime);
            cachedNighttime = Convert.ToSingle(nightTime);
            if (cachedDaytime< 1f) cachedDaytime = 1f;
            if (cachedNighttime < 1f) cachedNighttime = 1f;
            plugintimer = timer.Repeat(1f, 0, () => CheckTime());
        }
        void CheckTime()
        {
            if (freezeTime) return;
            cachedTime = EnvironmentControlCenter.Singleton.GetTime();
            if (cachedTime >= cachedDaytime && cachedTime < cachedNighttime)
            {
                cachedDelta = cachedTime + (1f / dayTimeSeconds) * (cachedNighttime -cachedDaytime);
                EnvironmentControlCenter.Singleton.SetTime( (float)cachedDelta );
            }
            else
            {
                cachedDelta = cachedTime + (1f / nightTimeSeconds)*(cachedDaytime + 24f - cachedNighttime);
                EnvironmentControlCenter.Singleton.SetTime( (float)cachedDelta);
            }
        }
        bool hasAccess(NetUser netuser)
        {
            if (netuser.CanAdmin()) return true;
            if (permission.UserHasPermission(netuser.playerClient.userID.ToString(), "cantime")) return true;
            return permission.UserHasPermission(netuser.playerClient.userID.ToString(), "all");
        }
        [ChatCommand("time")]
        void cmdChatTime(NetUser netuser, string command, string[] args)
        {
            if(!hasAccess(netuser)) { SendReply(netuser, notAllowed); return; }
            if(args.Length == 0)
            {
                SendReply(netuser, string.Format("CurrentTime {0} | Default StartTime {1} | Time Freeze {2}", EnvironmentControlCenter.Singleton.GetTime().ToString(), startTime.ToString(), freezeTime.ToString()));
                SendReply(netuser, string.Format("Day Starts At {0} | Night Starts At {1} | Day Length {2}s | Night Length {3}s", dayTime.ToString(), nightTime.ToString(), dayTimeSeconds.ToString(), nightTimeSeconds.ToString()));
                return;
            }
            switch (args[0].ToLower())
            {
                case "freeze":
                    if (freezeTime)
                    {
                        Config["Settings: Freeze Time"] = false;
                        freezeTime = false;
                        SendReply(netuser, "Time was unfroozen");
                    }
                    else
                    {
                        freezeTime = true;
                        Config["Settings: Freeze Time"] = true;
                        SendReply(netuser, "Time was froozen");
                    }
                    break;
                case "daylength":
                    if (args.Length == 1) return;
                    dayTimeSeconds = Convert.ToInt32(args[1]);
                    Config["Settings: Day length in seconds"] = dayTimeSeconds;
                    SendReply(netuser, "Day Length is now: " + args[1] + "s");
                break;
                case "nightlength":
                    if (args.Length == 1) return;
                    nightTimeSeconds = Convert.ToInt32(args[1]);
                    Config["Settings: Night length in seconds"] = nightTimeSeconds;
                    SendReply(netuser, "Night Length is now: " + args[1] + "s");
               break;
                case "day":
                    if (args.Length == 1) return;
                    dayTime = Convert.ToInt32(args[1]);
                    Config["Settings: Day Starts At"] = dayTime;
                    SendReply(netuser, "Day now Starts at: " + args[1] + " o'clock");
                    break;
                case "night":
                    nightTime = Convert.ToInt32(args[1]);
                    Config["Settings: Night Stats At"] = nightTime;
                    SendReply(netuser, "Night now Starts at: " + args[1] + " o'clock");
                    break;
                case "start":
                    startTime = Convert.ToInt32(args[1]);
                    Config["Settings: Start Time"] = startTime;
                    SendReply(netuser, "Initial Time now is: " + args[1] + " o'clock");
                    break;
                default: 
                    float newtime = 12f;
                    if(float.TryParse(args[0], out newtime))
                    {
                        EnvironmentControlCenter.Singleton.SetTime(newtime);
                        return;
                    }
                    SendReply(netuser, "/time XX|freeze|daylength|nightlenght|day|night|start optionvalue");
                    return;
                    break;
            }
            SaveConfig();
            InitiateTime(false);
        }

    }
}