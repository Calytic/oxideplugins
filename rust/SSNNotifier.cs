using System.Collections.Generic;
using System;
using UnityEngine;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Core.Libraries;
using System.Security.Cryptography;
using System.Text;

namespace Oxide.Plugins
{
    [Info("SSNNotifier", "Umlaut", "0.0.5")]
    class SSNNotifier : RustPlugin
    {
        // Types defenition

        enum TimeRange
        {
            Hour = 0,
            Day = 1,
            Week = 2,
            Month = 3,
            Year = 4
        }

        class BanItem
        {
            public string timestamp;
            public string reason;

            public BanItem()
            {
                timestamp = "";
                reason = "";
            }
        }

        class MuteItem
        {
            public string timestamp = "";
            public string reason = "";
            public TimeRange level = TimeRange.Hour;

            public DateTime untilDatetime()
            {
                return DateTime.ParseExact(timestamp, "yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.CurrentCulture) + timeSpan();
            }

            TimeSpan timeSpan()
            {
                switch (level)
                {
                    case TimeRange.Hour: return new TimeSpan(0, 1, 0, 0);
                    case TimeRange.Day: return new TimeSpan(1, 0, 0, 0);
                    case TimeRange.Week: return new TimeSpan(7, 1, 0, 0);
                    case TimeRange.Month: return new TimeSpan(28, 1, 0, 0);
                    case TimeRange.Year: return new TimeSpan(365, 1, 0, 0);
                    default: return new TimeSpan();
                }
            }
        }

        class ConfigData
        {
            public bool print_errors = true;
            public string server_name = "insert here name of your server";
            public string server_password = "insert here password of your server";

            public Dictionary<string, string> Messages = new Dictionary<string, string>();

            public Dictionary<ulong, BanItem> BannedPlayers = new Dictionary<ulong, BanItem>();
            public Dictionary<ulong, MuteItem> MutedPlayers = new Dictionary<ulong, MuteItem>();
        }

        // Object vars

        ConfigData m_configData;
        WebRequests m_webRequests = Interface.GetMod().GetLibrary<WebRequests>("WebRequests");

        public string m_host = "survival-servers-network.com";
        public string m_port = "1024";

        Dictionary<ulong, string> m_playersNames;
        Dictionary<ulong, List<ulong>> m_contextPlayers = new Dictionary<ulong, List<ulong>>();

        //

        void LoadConfig()
        {
            try
            {
                m_configData = Config.ReadObject<ConfigData>();
                InsertDefaultMessages();
            }
            catch
            {
                LoadDefaultConfig();
            }
        }

        void SaveConfig()
        {
            Config.WriteObject<ConfigData>(m_configData, true);
        }

        void LoadDynamic()
        {
            try
            {
                m_playersNames = Interface.GetMod().DataFileSystem.ReadObject<Dictionary<ulong, string>>("PlayersNames");
            }
            catch
            {
                m_playersNames = new Dictionary<ulong, string>();
            }
        }

        void SaveDynamic()
        {
            Interface.GetMod().DataFileSystem.WriteObject("PlayersNames", m_playersNames);
        }

        public void InsertDefaultMessage(string key, string message)
        {
            if (!m_configData.Messages.ContainsKey(key))
            {
                m_configData.Messages.Add(key, message);
            }
        }

        void InsertDefaultMessages()
        {
            InsertDefaultMessage("all_online_players_count", "All online players <color=cyan>%count</color>.");
            InsertDefaultMessage("invalid_arguments", "Invalid arguments.");
            InsertDefaultMessage("player_not_found", "Player not found.");
            InsertDefaultMessage("players_line", "<color=cyan>%number)</color> %player");
            InsertDefaultMessage("wellcome", "");

            InsertDefaultMessage("invalid_arguments", "Invalid arguments.");
            InsertDefaultMessage("player_not_found", "Player not found.");

            InsertDefaultMessage("player_was_not_banned", "Player <color=cyan>%player_name</color>(<color=cyan>%player_steamid</color>) was not banned.");
            InsertDefaultMessage("player_is_banned_already", "Player <color=cyan>%player_name</color>(<color=cyan>%player_steamid</color>) is banned already by reason \"<color=cyan>%reason</color>\".");
            InsertDefaultMessage("player_was_banned", "Player <color=cyan>%player_name</color>(<color=cyan>%player_steamid</color>) was banned by reason \"<color=cyan>%reason</color>\".");
            InsertDefaultMessage("player_was_unbanned", "Player <color=cyan>%player_name</color>(<color=cyan>%player_steamid</color>) was unbanned.");

            InsertDefaultMessage("player_was_not_muted", "Player <color=cyan>%player_name</color>(<color=cyan>%player_steamid</color>) was not muted.");
            InsertDefaultMessage("player_is_muted_already", "Player <color=cyan>%player_name</color>(<color=cyan>%player_steamid</color>) is muted already by reason \"<color=cyan>%reason</color> until <color=cyan>%until_datetime</color>(for <color=cyan>%level</color>)");
            InsertDefaultMessage("player_was_muted", "Player <color=cyan>%player_name</color>(<color=cyan>%player_steamid</color>) was muted by reason \"<color=cyan>%reason</color>\" until <color=cyan>%until_datetime</color>(for <color=cyan>%level</color>)");
            InsertDefaultMessage("player_was_unmuted", "Player <color=cyan>%player_name</color>(<color=cyan>%player_steamid</color>) was unmuted.");

            foreach (var timeRange in Enum.GetValues(typeof(TimeRange)))
            {
                InsertDefaultMessage(timeRange.ToString(), timeRange.ToString());
            }

            /*
            InsertDefaultMessage(TimeRange.Hour.ToString(), TimeRange.Hour.ToString());
            InsertDefaultMessage(TimeRange.Day.ToString(), TimeRange.Day.ToString());
            InsertDefaultMessage(TimeRange.Week.ToString(), TimeRange.Week.ToString());
            InsertDefaultMessage(TimeRange.Month.ToString(), TimeRange.Month.ToString());
            InsertDefaultMessage(TimeRange.Year.ToString(), TimeRange.Year.ToString());
            */

        }

        // Hooks

        void Loaded()
        {
            LoadConfig();
            LoadDynamic();

            NotifyServerOn();

            timer.Repeat(60, 0, () => SaveDynamic());
            timer.Repeat(60*5, 0, () => NotifyServerOn());

            checkPermission("SSNNotifier.mute");
            checkPermission("SSNNotifier.unmute");
            checkPermission("SSNNotifier.ban");
            checkPermission("SSNNotifier.unban");
        }

        void checkPermission(string _permission)
        {
            if (!permission.PermissionExists(_permission))
            {
                permission.RegisterPermission(_permission, this);
            }
        }

        private void Unload()
        {
            NotifyServerOff();
            SaveDynamic();
        }

        void LoadDefaultConfig()
        {
            m_configData = new ConfigData();
            InsertDefaultMessages();
            Config.WriteObject(m_configData, true);
        }

        // Players hooks

        object CanClientLogin(Network.Connection connection)
        {
            ulong userID = connection.userid;
            if (m_configData.BannedPlayers.ContainsKey(userID))
            {
                string playerName = PlayerName(userID);

                string message = m_configData.Messages["player_was_banned"];
                message = message.Replace("%player_name", playerName);
                message = message.Replace("%player_steamid", userID.ToString());
                message = message.Replace("%reason", m_configData.BannedPlayers[userID].reason);

                return message;
            }

            return true;
        }

        void OnPlayerInit(BasePlayer player)
        {
            NotifyPlayerConnected(player.userID, player.displayName, player.net.connection.ipaddress.Split(':')[0]);

            m_playersNames[player.userID] = player.displayName;
            if (m_configData.Messages["wellcome"].Length != 0)
                player.ChatMessage(m_configData.Messages["wellcome"]);
        }

        void OnPlayerDisconnected(BasePlayer player)
        {
            NotifyPlayerDisconnected(player.userID, player.displayName);
        }

        void OnEntityDeath(BaseCombatEntity entity, HitInfo hitInfo)
        {
            if (entity == null || hitInfo == null || hitInfo.Initiator == null)
            {
                return;
            }

            BasePlayer playerVictim = entity as BasePlayer;
            BasePlayer playerKiller = hitInfo.Initiator as BasePlayer;

            if (playerVictim == null || playerKiller == null || playerVictim == playerKiller)
            {
                return;
            }

            double distance = Math.Sqrt(
                Math.Pow(playerVictim.transform.position.x - playerKiller.transform.position.x, 2) +
                Math.Pow(playerVictim.transform.position.y - playerKiller.transform.position.y, 2) +
                Math.Pow(playerVictim.transform.position.z - playerKiller.transform.position.z, 2));

            NotifyMurder(playerVictim.userID, playerVictim.displayName, playerKiller.userID, playerKiller.displayName, hitInfo.Weapon.GetItem().info.itemid, distance, hitInfo.isHeadshot, playerVictim.IsSleeping());
        }

        object OnPlayerChat(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();

            string message = "";
            foreach (string line in arg.Args)
            {
                message += line + " ";
            }
            message = message.Trim();

            if (m_configData.MutedPlayers.ContainsKey(player.userID))
            {
                MuteItem muteItem = m_configData.MutedPlayers[player.userID];

                if (muteItem.untilDatetime() > DateTime.Now)
                {
                    message = m_configData.Messages["player_was_muted"];
                    message = message.Replace("%player_name", player.displayName);
                    message = message.Replace("%player_steamid", player.userID.ToString());
                    message = message.Replace("%reason", muteItem.reason);
                    message = message.Replace("%until_datetime", muteItem.untilDatetime().ToString("yyyy-MM-dd HH:mm:ss"));
                    message = message.Replace("%level", m_configData.Messages[muteItem.level.ToString()]);

                    player.ChatMessage(message);
                    return "handled";
                }
            }

            if (message != "" && message[0] != '/')
            {
                NotifyPlayerChatMessage(player.userID, player.displayName, message);
            }

            return null;
        }

        // Chat commands

        [ChatCommand("players")]
        void cmdChatPlayers(BasePlayer player, string command, string[] args)
        {
            string filter = "";
            int linesCount = 0;

            // 

            if (args.Length == 1)
            {
                if (!int.TryParse(args[0], out linesCount))
                {
                    filter = args[0];
                }
            }
            else if(args.Length == 2)
            {
                filter = args[0];
                if (!int.TryParse(args[1], out linesCount))
                {
                    player.ChatMessage(m_configData.Messages["invalid_arguments"]);
                }
            }
            else if (args.Length != 0)
            {
                player.ChatMessage(m_configData.Messages["invalid_arguments"]);
            }


            // Filtering by name

            List<BasePlayer> players = new List<BasePlayer>();
            foreach (BasePlayer currentPlayer in BasePlayer.activePlayerList)
            {
                if (filter != "" && !currentPlayer.displayName.Contains(filter, System.Globalization.CompareOptions.IgnoreCase))
                {
                    continue;
                }
                players.Add(currentPlayer);
            }

            // Sorting by name

            for (int f = 0; f < players.Count - 1; ++f)
            {
                for (int j = f + 1; j < players.Count; ++j)
                {
                    if (players[f].displayName.CompareTo(players[j].displayName) > 0)
                    {
                        BasePlayer tmpPlayer = players[f];
                        players[f] = players[j];
                        players[j] = tmpPlayer;
                    }
                }
            }

            // Context list

            int i = 0;
            List<ulong> contextPlayers = new List<ulong>();

            if (linesCount == 0)
            {
                foreach (BasePlayer currentPlayer in players)
                {
                    contextPlayers.Add(currentPlayer.userID);
                    player.ChatMessage(m_configData.Messages["players_line"].Replace("%number", (++i).ToString()).Replace("%player", currentPlayer.displayName) );
                }
            }
            else
            {
                List<BasePlayer> cPlayers = new List<BasePlayer>(players);
                int playerPerLine = (int)Math.Ceiling((double)players.Count/(double)linesCount);
                int index = 0;
                while (cPlayers.Count != 0)
                {
                    string line = "";
                    for (int z = 0; z < playerPerLine && cPlayers.Count != 0; ++z)
                    {
                        contextPlayers.Add(cPlayers[0].userID);
                        line += m_configData.Messages["players_line"].Replace("%number", (++index).ToString()).Replace("%player", cPlayers[0].displayName);
                        line += " ";
                        cPlayers.RemoveAt(0);
                    }
                    player.ChatMessage(line);
                }
            }
            player.ChatMessage(m_configData.Messages["all_online_players_count"].Replace("%count", BasePlayer.activePlayerList.Count.ToString()));
            SetContextPlayers(player.userID, contextPlayers);
        }

        [ChatCommand("ban")]
        void cmdBan(BasePlayer player, string command, string[] args)
        {
            if (player.net.connection.authLevel == 0 && !permission.UserHasPermission(player.userID.ToString(), "SSNBans.ban"))
            {
                return;
            }

            if (args.Length < 2)
            {
                player.ChatMessage(m_configData.Messages["invalid_arguments"]);
                return;
            }

            ulong userId = UserIdByAlias(player.userID, args[0]);
            if (userId == 0)
            {
                player.ChatMessage(m_configData.Messages["player_not_found"]);
                return;
            }

            string playerName = PlayerName(userId);

            string reason = "";
            for (int i = 1; i < args.Length; ++i)
            {
                reason += args[i];
                if (i < args.Length - 1)
                {
                    reason += " ";
                }
            }

            if (m_configData.BannedPlayers.ContainsKey(userId))
            {

                BanItem banItem = m_configData.BannedPlayers[userId];
                string message = m_configData.Messages["player_is_banned_already"];
                message = message.Replace("%player_name", playerName);
                message = message.Replace("%player_steamid", userId.ToString());
                message = message.Replace("%reason", banItem.reason);
                player.ChatMessage(message);
            }
            else
            {
                BanItem banItem = new BanItem();
                banItem.reason = reason;
                banItem.timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                m_configData.BannedPlayers[userId] = banItem;

                ConsoleSystem.Run.Server.Quiet(string.Format("banid {0} \"{1}\" \"{2}\"", userId.ToString(), playerName, reason).ToString(), true);
                ConsoleSystem.Run.Server.Quiet("server.writecfg", true);

                SaveConfig();

                string message = m_configData.Messages["player_was_banned"];
                message = message.Replace("%player_name", playerName);
                message = message.Replace("%player_steamid", userId.ToString());
                message = message.Replace("%reason", banItem.reason);
                ConsoleSystem.Broadcast("chat.add", 0, message, 1.0);

                BasePlayer targetPlayer = BasePlayer.FindByID(userId);
                if (targetPlayer != null)
                {
                    targetPlayer.Kick(message);
                }

                NotifyPlayerBan(userId, playerName, reason);
            }
        }

        [ChatCommand("unban")]
        void cmdUnban(BasePlayer player, string command, string[] args)
        {
            if (player.net.connection.authLevel == 0 && !permission.UserHasPermission(player.userID.ToString(), "SSNBans.unban"))
            {
                return;
            }

            if (args.Length != 1)
            {
                player.ChatMessage(m_configData.Messages["invalid_arguments"]);
                return;
            }

            ulong userID = UserIdByAlias(player.userID, args[0]);
            if (userID == 0)
            {
                player.ChatMessage(m_configData.Messages["player_not_found"]);
                return;
            }

            string playerName = PlayerName(userID);

            if (m_configData.BannedPlayers.ContainsKey(userID))
            {
                ConsoleSystem.Run.Server.Quiet(string.Format("unban {0}", userID.ToString()).ToString(), true);
                ConsoleSystem.Run.Server.Quiet("server.writecfg", true);

                m_configData.BannedPlayers.Remove(userID);
                SaveConfig();

                string message = m_configData.Messages["player_was_unbanned"];
                message = message.Replace("%player_name", playerName);
                message = message.Replace("%player_steamid", userID.ToString());
                ConsoleSystem.Broadcast("chat.add", 0, message, 1.0);
            }
            else
            {
                string message = m_configData.Messages["player_was_not_banned"];
                message = message.Replace("%player_name", playerName);
                message = message.Replace("%player_steamid", userID.ToString());
                player.ChatMessage(message);
            }
        }

        [ChatCommand("bans")]
        void cmdChatBans(BasePlayer player, string command, string[] args)
        {
            if (args.Length > 1)
            {
                player.ChatMessage(m_configData.Messages["invalid_arguments"]);
                return;
            }

            List<ulong> contextPlayers = new List<ulong>();
            foreach (ulong userID in m_configData.BannedPlayers.Keys)
            {
                string playerName = PlayerName(userID);
                if (args.Length == 1 && !playerName.Contains(args[0], System.Globalization.CompareOptions.IgnoreCase))
                {
                    continue;
                }

                BanItem banItem = m_configData.BannedPlayers[userID];
                contextPlayers.Add(userID);

                string message = m_configData.Messages["player_was_banned"];
                message = message.Replace("%player_name", playerName);
                message = message.Replace("%player_steamid", userID.ToString());
                message = message.Replace("%reason", banItem.reason);

                player.ChatMessage(contextPlayers.Count.ToString() + ") " + banItem.timestamp + " " + message);
            }
            SetContextPlayers(player.userID, contextPlayers);
        }

        [ChatCommand("mute")]
        void cmdChatMute(BasePlayer player, string command, string[] args)
        {
            string message;
            if (player.net.connection.authLevel == 0 && !permission.UserHasPermission(player.userID.ToString(), "SSNMutes.mute"))
            {
                return;
            }

            if (args.Length < 2)
            {
                player.ChatMessage(m_configData.Messages["invalid_arguments"]);
                return;
            }

            ulong userID = UserIdByAlias(player.userID, args[0]);
            if (userID == 0)
            {
                player.ChatMessage(m_configData.Messages["player_not_found"]);
                return;
            }

            string playerName = PlayerName(userID);

            string reason = "";
            for (int i = 1; i < args.Length; ++i)
            {
                reason += args[i];
                if (i < args.Length - 1)
                {
                    reason += " ";
                }
            }

            MuteItem muteItem;
            if (m_configData.MutedPlayers.ContainsKey(userID))
            {
                muteItem = m_configData.MutedPlayers[userID];
                if (muteItem.untilDatetime() > DateTime.Now)
                {
                    message = m_configData.Messages["player_is_muted_already"];
                    message = message.Replace("%player_name", playerName);
                    message = message.Replace("%player_steamid", userID.ToString());
                    message = message.Replace("%reason", muteItem.reason);
                    message = message.Replace("%until_datetime", muteItem.untilDatetime().ToString("yyyy-MM-dd HH:mm:ss"));
                    message = message.Replace("%level", m_configData.Messages[muteItem.level.ToString()]);

                    player.ChatMessage(message);
                    return;
                }

                int intLevel = (int)muteItem.level + 1;
                if (intLevel > (int)TimeRange.Year)
                {
                    intLevel = (int)TimeRange.Year;
                }
                muteItem.level = (TimeRange)intLevel;
            }
            else
            {
                muteItem = new MuteItem();
            }

            muteItem.reason = reason;
            muteItem.timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            m_configData.MutedPlayers[userID] = muteItem;
            SaveConfig();

            message = m_configData.Messages["player_was_muted"];
            message = message.Replace("%player_name", playerName);
            message = message.Replace("%player_steamid", userID.ToString());
            message = message.Replace("%reason", muteItem.reason);
            message = message.Replace("%until_datetime", muteItem.untilDatetime().ToString("yyyy-MM-dd HH:mm:ss"));
            message = message.Replace("%level", m_configData.Messages[muteItem.level.ToString()]);

            ConsoleSystem.Broadcast("chat.add", 0, message, 1.0);
            
            NotifyPlayerMute(userID, playerName, reason);
        }

        [ChatCommand("unmute")]
        void cmdChatUnnute(BasePlayer player, string command, string[] args)
        {
            if (player.net.connection.authLevel == 0 && !permission.UserHasPermission(player.userID.ToString(), "SSNMutes.unmute"))
            {
                return;
            }

            if (args.Length != 1)
            {
                player.ChatMessage(m_configData.Messages["invalid_arguments"]);
                return;
            }

            ulong userID = UserIdByAlias(player.userID, args[0]);
            if (userID == 0)
            {
                player.ChatMessage(m_configData.Messages["player_not_found"]);
                return;
            }

            string playerName = PlayerName(userID);

            if (m_configData.MutedPlayers.ContainsKey(userID))
            {
                MuteItem muteItem = m_configData.MutedPlayers[userID];
                if (muteItem.level == TimeRange.Hour)
                {
                    m_configData.MutedPlayers.Remove(userID);
                }
                else
                {
                    muteItem.level = (TimeRange)((int)muteItem.level - 1);
                }
                SaveConfig();

                string message = m_configData.Messages["player_was_unmuted"];
                message = message.Replace("%player_name", playerName);
                message = message.Replace("%player_steamid", userID.ToString());

                ConsoleSystem.Broadcast("chat.add", 0, message, 1.0);
            }
            else
            {
                string message = m_configData.Messages["player_was_not_muted"];
                message = message.Replace("%player_name", playerName);
                message = message.Replace("%player_steamid", userID.ToString());

                player.ChatMessage(message);
            }
        }

        [ChatCommand("mutes")]
        void cmdChatMutes(BasePlayer player, string command, string[] args)
        {
            if (args.Length > 1)
            {
                player.ChatMessage(m_configData.Messages["invalid_arguments"]);
                return;
            }

            List<ulong> contextPlayers = new List<ulong>();
            foreach (ulong userID in m_configData.MutedPlayers.Keys)
            {
                string playerName = PlayerName(userID);
                if (args.Length == 1 && !playerName.Contains(args[0], System.Globalization.CompareOptions.IgnoreCase))
                {
                    continue;
                }

                MuteItem muteItem = m_configData.MutedPlayers[userID];
                contextPlayers.Add(userID);

                string message = m_configData.Messages["player_was_muted"];
                message = message.Replace("%player_name", playerName);
                message = message.Replace("%player_steamid", userID.ToString());
                message = message.Replace("%reason", muteItem.reason);
                message = message.Replace("%until_datetime", muteItem.untilDatetime().ToString("yyyy-MM-dd HH:mm:ss"));
                message = message.Replace("%level", m_configData.Messages[muteItem.level.ToString()]);

                player.ChatMessage(contextPlayers.Count.ToString() + ") " + muteItem.timestamp + " " + message);
            }
            SetContextPlayers(player.userID, contextPlayers);
        }

        //

        private ulong UserIdByAlias(ulong contextId, string alias)
        {
            if (alias.Length == 17)
            {
                ulong userId;
                if (ulong.TryParse(alias, out userId))
                {
                    return userId;
                }
            }
            int index;
            if (int.TryParse(alias, out index))
            {
                if (m_contextPlayers.ContainsKey(contextId) && (index - 1) < m_contextPlayers[contextId].Count)
                {
                    return m_contextPlayers[contextId][index - 1];
                }
            }
            return 0;
        }

        private void SetContextPlayers(ulong context, List<ulong> players)
        {
            m_contextPlayers[context] = players;
        }

        private string PlayerName(ulong userID)
        {
            if (m_playersNames.ContainsKey(userID))
            {
                return m_playersNames[userID];
            }
            else
            {
                return "unknown";
            }
        }

        // Web request/response

        private void SendWebRequest(string subUrl, List<string> values)
        {
            string requestUrl = "http://%host:%port/%suburl".Replace("%host", m_host).Replace("%port", m_port).Replace("%suburl", subUrl);

            Dictionary<string, string> headers = new Dictionary<string, string>();
            headers.Add("server_name", m_configData.server_name);

            string body = "";
            foreach (string line in values)
            {
                body += line;
                body += "\n";
            }

            byte[] data = MD5.Create().ComputeHash(Encoding.UTF8.GetBytes(body + m_configData.server_password));
            StringBuilder sBuilder = new StringBuilder();
            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }

            headers.Add("salt", sBuilder.ToString());
            m_webRequests.EnqueuePost(requestUrl, body, (code, response) => ReceiveWebResponse(code, response), this, headers);
        }

        private void ReceiveWebResponse(int code, string response)
        {
            if (response == null)
            {
                if (m_configData.print_errors)
                {
                    Puts("Couldn't get an answer from SSN service.");
                }
            }
            else if (code != 200)
            {
                if (m_configData.print_errors)
                {
                    Puts("SSN error (%code): %text".Replace("%code", code.ToString()).Replace("%text", response));
                }
            }
        }

        // Notifiers

        private void NotifyMurder(ulong victimSteamId, string victimDisplayName, ulong killerSteamId, string killerDisplayName, int weaponRustItemId, double distance, bool isHeadshot, bool isSleeping)
        {
            List<string> values = new List<string>();
            values.Add(victimSteamId.ToString());
            values.Add(victimDisplayName);
            values.Add(killerSteamId.ToString());
            values.Add(killerDisplayName);
            values.Add(weaponRustItemId.ToString());
            values.Add(ItemManager.CreateByItemID(weaponRustItemId).info.displayName.english);
            values.Add(distance.ToString());
            values.Add(isHeadshot ? "true" : "false");
            values.Add(isSleeping ? "true" : "false");

            SendWebRequest("murder/create", values);
        }

        private void NotifyPlayerConnected(ulong steamid, string displayName, string ipAddress)
        {
            List<string> values = new List<string>();
            values.Add(steamid.ToString());
            values.Add(displayName);
            values.Add(ipAddress);

            SendWebRequest("player/connect", values);
        }

        private void NotifyPlayerDisconnected(ulong steamid, string displayName)
        {
            List<string> values = new List<string>();
            values.Add(steamid.ToString());
            values.Add(displayName);
            SendWebRequest("player/disconnect", values);
        }

        private void NotifyPlayerChatMessage(ulong steamid, string displayName, string messageText)
        {
            List<string> values = new List<string>();
            values.Add(steamid.ToString());
            values.Add(displayName);
            values.Add(messageText);

            SendWebRequest("player/chat_message", values);
        }

        private void NotifyPlayerBan(ulong steamid, string displayName, string reason)
        {
            List<string> values = new List<string>();
            values.Add(steamid.ToString());
            values.Add(displayName);
            values.Add(reason);
            SendWebRequest("player/ban", values);
        }

        private void NotifyPlayerMute(ulong steamid, string displayName, string reason)
        {
            List<string> values = new List<string>();
            values.Add(steamid.ToString());
            values.Add(displayName);
            values.Add(reason);
            SendWebRequest("player/mute", values);
        }

        private void NotifyServerOn()
        {
            List<string> values = new List<string>();
            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                values.Add(player.userID.ToString());
            }
            SendWebRequest("server/on", values);
        }

        private void NotifyServerOff()
        {
            SendWebRequest("server/off", new List<string>());
        }
    }
}
