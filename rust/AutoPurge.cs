using System;
using System.Collections.Generic;
using UnityEngine;
using Oxide.Core.Plugins;
namespace Oxide.Plugins
{
    [Info("AutoPurge", "Norn", 0.1, ResourceId = 1566)]
    [Description("Remove entities if the owner becomes inactive.")]
    public class AutoPurge : RustPlugin
    {
        [PluginReference]
        Plugin EntityOwner;

        [PluginReference]
        Plugin ConnectionDB;

        #region Localization

        void LoadDefaultMessages()
        {
            var messages = new Dictionary<string, string>
            {
                {"PurgeRun", "<color=yellow>INFO:</color> Beginning <color=red>purge</color>... (<color=yellow>Slight lag may occur, please do not spam the chat.</color>)"},
                {"PurgeComplete", "Purge <color=green>complete</color> (<color=yellow>{count}</color> entities removed from <color=yellow>{unique_players}</color> inactive players)." },
            };
            lang.RegisterMessages(messages, this);
        }
        string GetMessage(string key, string steamId = null) => lang.GetMessage(key, this, steamId);
        #endregion

        protected override void LoadDefaultConfig()
        {
            Puts("No configuration file found, generating..."); Config.Clear();

            Config["General", "MainTimer"] = 21600; // 6 hours
            Config["General", "InactiveAfter"] = 172800; // 2 days
            Config["General", "Messages"] = true;
        }
        Timer mainTimer = null;
        bool ACTIVE = false;
        void Loaded()
        {
            if(ConnectionDB && EntityOwner) { ACTIVE = true; }
            if (ACTIVE)
            {
                if (!permission.PermissionExists("autopurge.run")) permission.RegisterPermission("autopurge.run", this);
                LoadDefaultMessages();
                int time = Convert.ToInt32(Config["General", "MainTimer"]);
                int inactive_time = Convert.ToInt32(Config["General", "InactiveAfter"]);
                TimeSpan its = TimeSpan.FromSeconds(inactive_time);
                TimeSpan ts = TimeSpan.FromSeconds(time); if (ts.Hours != 0) { Puts("Purge will be executed every: " + ts.Hours.ToString() + " hours, players become inactive after: "+its.Days.ToString()+" days."); }
                mainTimer = timer.Repeat(time, 0, () => MainTimer());
            } else { Puts("ERROR: One or more dependencies are missing, this plugin will not function."); }
        }
        ulong FindOwner(BaseEntity entity)
        {
            object returnhook = null;
            ulong ownerid = 0;
            returnhook = this.EntityOwner?.Call("FindEntityData", entity);
            if (returnhook != null) { if (!(returnhook is bool)) { ownerid = Convert.ToUInt64(returnhook); } }
            return ownerid;
        }
        private long UnixTimeStampUTC()
        {
            long unixTimeStamp;
            DateTime currentTime = DateTime.Now;
            DateTime zuluTime = currentTime.ToUniversalTime();
            DateTime unixEpoch = new DateTime(1970, 1, 1);
            unixTimeStamp = (Int32)(zuluTime.Subtract(unixEpoch)).TotalSeconds;
            return unixTimeStamp;
        }
        private long ConvertToUnixTime(DateTime datetime) { DateTime sTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc); return (long)(datetime - sTime).TotalSeconds; }
        void PurgeMessage(int type = 1, int count = 0, int unique_hits = 0)
        {
            if(type == 1) { foreach (var player in BasePlayer.activePlayerList) { if (player != null && player.IsConnected()) { PrintToChat(player, GetMessage("PurgeRun", player.UserIDString)); } } }
            else if(type == 2)
            {
                foreach (var player in BasePlayer.activePlayerList)
                {
                    string parsed_config = null;
                    if (player != null && player.IsConnected()) { parsed_config = GetMessage("PurgeComplete", player.UserIDString); parsed_config = parsed_config.Replace("{count}", count.ToString()); parsed_config = parsed_config.Replace("{unique_players}", unique_hits.ToString()); }
                    if(parsed_config.Length >= 1) { PrintToChat(player, parsed_config); }
                }
            }
            else { Puts("Invalid message type..."); }
        }
        [ConsoleCommand("autopurge.run")]
        void ccmdRunPurge(ConsoleSystem.Arg arg)
        {
            if (arg.connection != null)
            {
                if (permission.UserHasPermission(arg.connection.userid.ToString(), "autopurge.run"))
                {
                    Puts("Executing purge from internal command...");
                    MainTimer();
                }
            }
            else
            {
                Puts("Executing purge from external command...");
                MainTimer();
            }
        }
        void MainTimer()
        {

            if (Convert.ToBoolean(Config["General", "Messages"])) { PurgeMessage(1); }
            int count = 0; List<ulong> UNIQUE_HITS = new List<ulong>();
            foreach(var entity in BaseNetworkable.serverEntities.All())
            {
                ulong owner = FindOwner(entity.gameObject.ToBaseEntity());
                if(owner != 0 && Convert.ToBoolean(ConnectionDB.Call("ConnectionDataExistsFromID", owner)))
                {
                    DateTime LastSeen = Convert.ToDateTime(ConnectionDB.Call("LastSeenFromID", owner));
                    long last_seen_time = ConvertToUnixTime(LastSeen);
                    long current_time = UnixTimeStampUTC();
                    if(current_time - last_seen_time >= Convert.ToInt32(Config["General", "InactiveAfter"])) { entity.Kill(); count++; if (!UNIQUE_HITS.Contains(owner)) { UNIQUE_HITS.Add(owner); } }
                }
            }
            if (Convert.ToBoolean(Config["General", "Messages"])) { PurgeMessage(2, count, UNIQUE_HITS.Count); }
            if(count != 0) { Puts("Removed: " + count.ToString() + " entities from: " + UNIQUE_HITS.Count.ToString() + " inactive players"); } else { Puts("Nothing to remove... up to date."); }
        }
    }
}