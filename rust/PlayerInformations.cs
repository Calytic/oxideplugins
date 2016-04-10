using System.Collections.Generic;
using System;
using System.Linq;
using Oxide.Core;
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
    [Info("PlayerInformations", "Reneb", "1.0.5", ResourceId = 1345)]
    [Description("Logs players informations.")]
    public class PlayerInformations : RustPlugin
    {
        [PluginReference]
        Plugin PlayerDatabase;

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /// Configs
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private static bool IPuse = true;
        private static string IPpermission = "canips";
        private static int IPauthlevel = 1;
        private static int IPmaxLogs = 5;

        private static bool NAMESuse = true;
        private static string NAMESpermission = "cannames";
        private static int NAMESauthlevel = 1;
        private static int NAMESmaxLogs = 5;

        private static bool FCuse = true;
        private static string FCpermission = "canlastseen";
        private static int FCauthlevel = 1;

        private static bool LSuse = true;
        private static string LSpermission = "canlastseen";
        private static int LSauthlevel = 1;

        private static bool LPuse = true;
        private static string LPpermission = "canlastposition";
        private static int LPauthlevel = 1;

        private static bool TPuse = true;
        private static string TPpermission = "cantimeplayed";
        private static int TPauthlevel = 0;

        static DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0);

        void Loaded()
        {
            if (!permission.PermissionExists(IPpermission)) permission.RegisterPermission(IPpermission, this);
            if (!permission.PermissionExists(NAMESpermission)) permission.RegisterPermission(NAMESpermission, this);
            if (!permission.PermissionExists(FCpermission)) permission.RegisterPermission(FCpermission, this);
            if (!permission.PermissionExists(LSpermission)) permission.RegisterPermission(LSpermission, this);
            if (!permission.PermissionExists(LSpermission)) permission.RegisterPermission(LPpermission, this);
        }

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
            CheckCfg<bool>("IP Logs - activated", ref IPuse);
            CheckCfg<string>("IP Logs - Permission - oxide permission", ref IPpermission);
            CheckCfg<int>("IP Logs - Permission - authlevel", ref IPauthlevel);
            CheckCfg<int>("IP Logs - Max Logs per player", ref IPmaxLogs);

            CheckCfg<bool>("Names Logs - activated", ref NAMESuse);
            CheckCfg<string>("Names Logs - Permission - oxide permission", ref NAMESpermission);
            CheckCfg<int>("Names Logs - Permission - authlevel", ref NAMESauthlevel);
            CheckCfg<int>("Names Logs - Max Logs per player", ref NAMESmaxLogs);

            CheckCfg<bool>("First Connection - activated", ref FCuse);
            CheckCfg<string>("First Connection - Permission - oxide permission", ref FCpermission);
            CheckCfg<int>("First Connection - Permission - authlevel", ref FCauthlevel);

            CheckCfg<bool>("Last Seen - activated", ref LSuse);
            CheckCfg<string>("Last Seen - Permission - oxide permission", ref LSpermission);
            CheckCfg<int>("Last Seen - Permission - authlevel", ref LSauthlevel);

            CheckCfg<bool>("Last Position - activated", ref LPuse);
            CheckCfg<string>("Last Position - Permission - oxide permission", ref LPpermission);
            CheckCfg<int>("Last Position - Permission - authlevel", ref LPauthlevel);

            CheckCfg<bool>("Time Played - activated", ref TPuse);
            CheckCfg<string>("Time Played - Permission - oxide permission", ref TPpermission);
            CheckCfg<int>("Time Played - Permission - authlevel", ref TPauthlevel);

            SaveConfig();
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /// Oxide Hooks
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        void OnServerInitialized()
        {
            if (TPuse)
            {
                foreach (BasePlayer player in BasePlayer.activePlayerList)
                {
                    StartRecordTime(player);
                }
            }
        }

        void OnEntityDeath(BaseCombatEntity ent, HitInfo info)
        {
            if (!LPuse) return;
            BasePlayer player = ent.GetComponent<BasePlayer>();
            if (player != null)
            {
                if (!player.IsConnected())
                {
                    var LastPos = new Dictionary<string, object>
                    {
                        {"x", player.transform.position.x.ToString() },
                        {"y", player.transform.position.y.ToString() },
                        {"z", player.transform.position.z.ToString() }
                    };
                    PlayerDatabase.Call("SetPlayerData", player.userID.ToString(), "Last Position", LastPos);
                }
            }
        }

        void Unload()
        {
            if (TPuse)
            {
                foreach (BasePlayer player in BasePlayer.activePlayerList)
                {
                    EndRecordTime(player);
                }
            }
        }
        void OnPlayerInit(BasePlayer player)
        {
            if (IPuse)
                RecordIP(player);
            if (NAMESuse)
                RecordName(player);
            if (FCuse)
                RecordFirstConnection(player);
            if (TPuse)
                StartRecordTime(player);
        }
        void OnPlayerDisconnected(BasePlayer player)
        {
            if (LSuse)
                RecordLastSeen(player);
            if (TPuse)
                EndRecordTime(player);
        }

        [HookMethod("SendHelpText")]
        private void SendHelpText(BasePlayer player)
        {
            string msg = string.Empty;
            if (hasPermission(player, IPauthlevel, IPpermission)) { msg += "<color=\"#ffd479\">/lastips steamid/name</color> - get the last ips used by a user\n<color=\"#ffd479\">/ipowners XX.XX.XX.XX </color>- know what players used this ip\n"; }
            if (hasPermission(player, LSauthlevel, LSpermission)) { msg += "<color=\"#ffd479\">/lastseen steamid/name</color> - know when was this player last seen online\n"; }
            if (hasPermission(player, LPauthlevel, LPpermission)) { msg += "<color=\"#ffd479\">/lastposition steamid/name</color> - know where is the last position of a player\n"; }
            if (hasPermission(player, FCauthlevel, FCpermission)) { msg += "<color=\"#ffd479\">/firstconnection steamid/name</color> - know when was this player first seen online\n"; }
            if (hasPermission(player, TPauthlevel, TPpermission)) { msg += "<color=\"#ffd479\">/played steamid/name</color> - know how much time a player has played on this server\n"; }
            if (hasPermission(player, NAMESauthlevel, NAMESpermission)) { msg += "<color=\"#ffd479\">/lastnames steamid/name</color> - know the last names used by a user\n"; }
            if (msg != string.Empty)
            {
                msg = "<size=18>Players Information</size>\n" + msg;
                SendReply(player, msg);
            }
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /// General Functions
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        static double LogTime() { return DateTime.UtcNow.Subtract(epoch).TotalSeconds; }

        string TimeMinToString(string time) { return TimeMinToString(double.Parse(time)); }
        string TimeMinToString(double time)
        {
            TimeSpan timespan = TimeSpan.FromSeconds(time);
            DateTime date = new DateTime(1970, 1, 1, 0, 0, 0) + timespan;
            return string.Format("{0}:{1} {2}/{3}/{4}", date.Hour.ToString(), date.Minute.ToString(), date.Month.ToString(), date.Day.ToString(), date.Year.ToString());
        }

        string SecondsToString(string time) { return SecondsToString(decimal.Parse(time)); }
        string SecondsToString(decimal time)
        {
            decimal days = Math.Floor(time / 86400);
            time -= days * 86400;
            decimal hours = Math.Floor(time / 3600);
            time -= hours * 3600;
            decimal minutes = Math.Floor(time / 60);
            time -= minutes * 60;
            return string.Format("{0}d {1}h {2}m {3}s", days.ToString(), hours.ToString(), minutes.ToString(), Math.Floor(time).ToString());
        }

        private object FindPlayer(string arg)
        {
            string success = PlayerDatabase.Call("FindPlayer", arg) as string;
            if (success.Length == 17)
            {
                return ulong.Parse(success);
            }
            else
                return success;
        }
        private BasePlayer FindBasePlayerPlayer(ulong steamid)
        {
            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                if (player.userID == steamid)
                    return player;
            }
            foreach (BasePlayer player in BasePlayer.sleepingPlayerList)
            {
                if (player.userID == steamid)
                    return player;
            }
            return null;
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /// Permission
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        bool hasPermission(BasePlayer player, int authlevel, string permissionName)
        {
            if (player.net.connection.authLevel >= authlevel) return true;
            return permission.UserHasPermission(player.userID.ToString(), permissionName);
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /// Record IP Related
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        void RecordIP(BasePlayer player)
        {
            string playerip = player.net.connection.ipaddress;
            playerip = playerip.Substring(0, playerip.IndexOf(":"));
            var IPlist = new Dictionary<string, object>();

            var success = PlayerDatabase.Call("GetPlayerData", player.userID.ToString(), "IPs");
            if (success is Dictionary<string, object>)
                IPlist = (Dictionary<string, object>)success;

            if (IPlist.ContainsValue(playerip)) return;
            if (IPlist.Count >= IPmaxLogs)
            {
                var tempList = new Dictionary<string, object>();
                foreach (KeyValuePair<string, object> pair in IPlist)
                {
                    tempList.Add(pair.Key, pair.Value);
                }
                IPlist.Clear();
                for (int i = tempList.Count - IPmaxLogs + 1; i < tempList.Count; i++)
                {
                    IPlist.Add(IPlist.Count.ToString(), tempList[i.ToString()]);
                }
            }
            IPlist.Add(IPlist.Count.ToString(), playerip);
            PlayerDatabase.Call("SetPlayerData", player.userID.ToString(), "IPs", IPlist);
        }

        [ChatCommand("lastips")]
        void cmdChatLastIps(BasePlayer player, string command, string[] args)
        {
            if (!hasPermission(player, IPauthlevel, IPpermission)) { SendReply(player, "You don't have access to this command"); return; }
            if (!IPuse) { SendReply(player, "The database isn't set to record the IPs of players"); return; }
            if (args.Length == 0)
            {
                SendReply(player, "/lastips STEAMID/NAME");
                return;
            }
            var findplayer = FindPlayer(args[0]);
            if (!(findplayer is ulong))
            {
                SendReply(player, findplayer is string ? (string)findplayer : "Couldn't find a player that matches this name.");
                return;
            }
            var IPlist = new Dictionary<string, object>();
            var success = PlayerDatabase.Call("GetPlayerData", findplayer.ToString(), "IPs");
            if (success is Dictionary<string, object>)
                IPlist = (Dictionary<string, object>)success;
            if (IPlist.Count == 0)
            {
                SendReply(player, "No IPs logged for this player");
                return;
            }
            var name = (PlayerDatabase.Call("GetPlayerData", player.userID.ToString(), "default") as Dictionary<string, object>)["name"] as string;
            SendReply(player, string.Format("IP List for {0} - {1}", name, findplayer.ToString()));
            foreach (KeyValuePair<string, object> pair in IPlist)
            {
                SendReply(player, string.Format("{0} - {1}", pair.Key, pair.Value.ToString()));
            }
        }

        [ChatCommand("ipowners")]
        void cmdChatIps(BasePlayer player, string command, string[] args)
        {
            if (!hasPermission(player, IPauthlevel, IPpermission)) { SendReply(player, "You don't have access to this command"); return; }
            if (!IPuse) { SendReply(player, "The database isn't set to record the IPs of players"); return; }
            if (args.Length == 0)
            {
                SendReply(player, "/ipowners XX.XX.XX.XX");
                return;
            }
            HashSet<string> knownPlayers = new HashSet<string>();
            var success = PlayerDatabase.Call("GetAllKnownPlayers");
            if (success is HashSet<string>)
                knownPlayers = (HashSet<string>)success;
            if (knownPlayers.Count == 0)
            {
                SendReply(player, "Couldn't get the list of players");
                return;
            }

            var foundPlayers = new List<string>();
            foreach (string playerID in knownPlayers)
            {
                var playerIPs = new Dictionary<string, object>();
                var successs = PlayerDatabase.Call("GetPlayerData", playerID, "IPs");
                if (successs is Dictionary<string, object>)
                    playerIPs = (Dictionary<string, object>)successs;
                if (playerIPs.Count == 0) { continue; }
                if (playerIPs.ContainsValue(args[0]))
                {
                    foundPlayers.Add(playerID);
                }
            }
            SendReply(player, string.Format("Found {0} players with this matching ip", foundPlayers.Count.ToString()));
            foreach (string userid in foundPlayers)
            {
                var playerData = new Dictionary<string, object>();
                var successs = PlayerDatabase.Call("GetPlayerData", userid, "default");
                if (successs is Dictionary<string, object>)
                    playerData = (Dictionary<string, object>)successs;
                if (playerData.Count == 0) { continue; }

                SendReply(player, string.Format("{0} - {1}", userid, playerData.ContainsKey("name") ? playerData["name"] : "Unknown"));
            }
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /// Last Seen Related
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        void RecordLastSeen(BasePlayer player)
        {
            var LastSeenTable = new Dictionary<string, object>
            {
                { "0" , LogTime().ToString() }
            };
            PlayerDatabase.Call("SetPlayerData", player.userID.ToString(), "Last Seen", LastSeenTable);
        }

        [ChatCommand("lastseen")]
        private void cmdChatLastseen(BasePlayer player, string command, string[] args)
        {
            if (!hasPermission(player, LSauthlevel, LSpermission)) { SendReply(player, "You don't have access to this command"); return; }
            if (!LSuse) { SendReply(player, "This command has been deactivated"); return; }
            if (args.Length == 0)
            {
                SendReply(player, "/lastseen STEAMID/NAME");
                return;
            }
            var findplayer = FindPlayer(args[0]);
            if (!(findplayer is ulong))
            {
                SendReply(player, findplayer is string ? (string)findplayer : "Couldn't find a player that matches this name.");
                return;
            }
            BasePlayer targetPlayer = FindBasePlayerPlayer((ulong)findplayer);
            if (targetPlayer != null && targetPlayer.IsConnected())
            {
                SendReply(player, "This player is connected!");
                return;
            }
            var LastSeen = new Dictionary<string, object>();
            var success = PlayerDatabase.Call("GetPlayerData", findplayer.ToString(), "Last Seen");
            if (success is Dictionary<string, object>)
                LastSeen = (Dictionary<string, object>)success;
            if (LastSeen.Count == 0)
            {
                SendReply(player, "This player doesn't have a last seen logged");
                return;
            }
            var name = (PlayerDatabase.Call("GetPlayerData", findplayer.ToString(), "default") as Dictionary<string, object>)["name"] as string;
            SendReply(player, string.Format("{0} - {1} was last seen: {2}", name, findplayer.ToString(), TimeMinToString(LastSeen["0"] as string)));
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /// First Connection
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        void RecordFirstConnection(BasePlayer player)
        {
            var success = PlayerDatabase.Call("GetPlayerData", player.userID.ToString(), "First Connection");
            if (success is Dictionary<string, object>)
                return;

            var FirstConnectionTable = new Dictionary<string, object>
            {
                { "0" , LogTime().ToString() } 
            };
            PlayerDatabase.Call("SetPlayerData", player.userID.ToString(), "First Connection", FirstConnectionTable);
        }

        [ChatCommand("firstconnection")]
        private void cmdChatfirstconnection(BasePlayer player, string command, string[] args)
        {
            if (!hasPermission(player, FCauthlevel, FCpermission)) { SendReply(player, "You don't have access to this command"); return; }
            if (!FCuse) { SendReply(player, "This command has been deactivated"); return; }
            if (args.Length == 0)
            {
                SendReply(player, "/firstconnection STEAMID/NAME");
                return;
            }
            var findplayer = FindPlayer(args[0]);
            if (!(findplayer is ulong))
            {
                SendReply(player, findplayer is string ? (string)findplayer : "Couldn't find a player that matches this name.");
                return;
            }
            var FC = new Dictionary<string, object>();
            var success = PlayerDatabase.Call("GetPlayerData", findplayer.ToString(), "First Connection");
            if (success is Dictionary<string, object>)
                FC = (Dictionary<string, object>)success;
            if (FC.Count == 0)
            {
                SendReply(player, "This player doesn't have a first connection logged");
                return;
            }
            var name = (PlayerDatabase.Call("GetPlayerData", findplayer.ToString(), "default") as Dictionary<string, object>)["name"] as string;
            SendReply(player, string.Format("{0} - {1} first connected: {2}", name, findplayer.ToString(), TimeMinToString(FC["0"] as string)));
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /// Record Names
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        void RecordName(BasePlayer player)
        {
            string playername = player.displayName;
            var NameList = new Dictionary<string, object>();

            var success = PlayerDatabase.Call("GetPlayerData", player.userID.ToString(), "Names");
            if (success is Dictionary<string, object>)
                NameList = (Dictionary<string, object>)success;

            if (NameList.ContainsValue(playername)) return;
            if (NameList.Count >= NAMESmaxLogs)
            {
                var tempList = new Dictionary<string, object>();
                foreach (KeyValuePair<string, object> pair in NameList)
                {
                    tempList.Add(pair.Key, pair.Value);
                }
                NameList.Clear();
                for (int i = tempList.Count - NAMESmaxLogs + 1; i < tempList.Count; i++)
                {
                    NameList.Add(NameList.Count.ToString(), tempList[i.ToString()]);
                }
            }
            NameList.Add(NameList.Count.ToString(), playername);
            PlayerDatabase.Call("SetPlayerData", player.userID.ToString(), "Names", NameList);
        }

        [ChatCommand("lastnames")]
        private void cmdChatLastname(BasePlayer player, string command, string[] args)
        {
            if (!hasPermission(player, NAMESauthlevel, NAMESpermission)) { SendReply(player, "You don't have access to this command"); return; }
            if (!NAMESuse) { SendReply(player, "This command has been deactivated"); return; }
            if (args.Length == 0)
            {
                SendReply(player, "/lastnames STEAMID/NAME");
                return;
            }
            var findplayer = FindPlayer(args[0]);
            if (!(findplayer is ulong))
            {
                SendReply(player, findplayer is string ? (string)findplayer : "Couldn't find a player that matches this name.");
                return;
            }
            var NameList = new Dictionary<string, object>();
            var success = PlayerDatabase.Call("GetPlayerData", findplayer.ToString(), "Names");
            if (success is Dictionary<string, object>)
                NameList = (Dictionary<string, object>)success;
            if (NameList.Count == 0)
            {
                SendReply(player, "No Names logged for this player");
                return;
            }
            var name = (PlayerDatabase.Call("GetPlayerData", findplayer.ToString(), "default") as Dictionary<string, object>)["name"] as string;
            SendReply(player, string.Format("Name List for {0} - {1}", name, findplayer.ToString()));
            foreach (KeyValuePair<string, object> pair in NameList)
            {
                SendReply(player, string.Format("{0} - {1}", pair.Key, pair.Value.ToString()));
            }
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /// Record Time Played
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        Dictionary<BasePlayer, double> recordPlayTime = new Dictionary<BasePlayer, double>();
        void StartRecordTime(BasePlayer player)
        {
            if (recordPlayTime.ContainsKey(player))
                recordPlayTime.Remove(player);
            recordPlayTime.Add(player, LogTime());
        }
        void EndRecordTime(BasePlayer player)
        {
            if (!recordPlayTime.ContainsKey(player)) return;

            var TimePlayed = new Dictionary<string, object>();
            var success = PlayerDatabase.Call("GetPlayerData", player.userID.ToString(), "Time Played");
            if (success is Dictionary<string, object>)
                TimePlayed = (Dictionary<string, object>)success;

            double totaltime = LogTime() - recordPlayTime[player];
            if (TimePlayed.ContainsKey("0"))
                totaltime += double.Parse((string)TimePlayed["0"]);

            TimePlayed.Clear();
            TimePlayed.Add("0", totaltime.ToString());
            PlayerDatabase.Call("SetPlayerData", player.userID.ToString(), "Time Played", TimePlayed);
        }

        [ChatCommand("played")]
        private void cmdChatPlayed(BasePlayer player, string command, string[] args)
        {
            if (!hasPermission(player, TPauthlevel, TPpermission)) { SendReply(player, "You don't have access to this command"); return; }
            if (!TPuse) { SendReply(player, "This command has been deactivated"); return; }
            if (args.Length == 0)
            {
                SendReply(player, "/played STEAMID/NAME");
                return;
            }
            var findplayer = FindPlayer(args[0]);
            if (!(findplayer is ulong))
            {
                SendReply(player, findplayer is string ? (string)findplayer : "Couldn't find a player that matches this name.");
                return;
            }
            var TimePlayed = new Dictionary<string, object>();
            var success = PlayerDatabase.Call("GetPlayerData", findplayer.ToString(), "Time Played");
            if (success is Dictionary<string, object>)
                TimePlayed = (Dictionary<string, object>)success;
            if (TimePlayed.Count == 0)
            {
                SendReply(player, "This player doesn't have any time played recorded");
                return;
            }
            var name = (PlayerDatabase.Call("GetPlayerData", findplayer.ToString(), "default") as Dictionary<string, object>)["name"] as string;
            double tplayed = double.Parse(TimePlayed["0"] as string);
            if (recordPlayTime.ContainsKey(player))
                tplayed += LogTime() - recordPlayTime[player];
            SendReply(player, string.Format("{0} - {1} played: {2}", name, findplayer.ToString(), SecondsToString(tplayed.ToString())));
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /// Position Related
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        [ChatCommand("lastposition")]
        private void cmdChatLastPosition(BasePlayer player, string command, string[] args)
        {
            if (!hasPermission(player, LSauthlevel, LSpermission)) { SendReply(player, "You don't have access to this command"); return; }
            if (!LPuse) { SendReply(player, "This command has been deactivated"); return; }
            if (args.Length == 0)
            {
                SendReply(player, "/lastseen STEAMID/NAME");
                return;
            }
            var findplayer = FindPlayer(args[0]);
            if (!(findplayer is ulong))
            {
                SendReply(player, findplayer is string ? (string)findplayer : "Couldn't find a player that matches this name.");
                return;
            }
            BasePlayer targetPlayer = FindBasePlayerPlayer((ulong)findplayer);
            if (targetPlayer != null && targetPlayer.IsConnected())
            {
                SendReply(player, "This player is connected!");
                return;
            }
            else if (targetPlayer != null && !targetPlayer.IsConnected())
            {
                SendReply(player, string.Format("{0} - {1} current position is: {2} {3} {4}", targetPlayer.displayName, findplayer.ToString(), targetPlayer.transform.position.x.ToString(), targetPlayer.transform.position.y.ToString(), targetPlayer.transform.position.z.ToString()));
                return;
            }
            var LastPos = new Dictionary<string, object>();
            var success = PlayerDatabase.Call("GetPlayerData", findplayer.ToString(), "Last Position");
            if (success is Dictionary<string, object>)
                LastPos = (Dictionary<string, object>)success;
            if (LastPos.Count == 0)
            {
                SendReply(player, "This player doesn't have a position logged");
                return;
            }
            var name = (PlayerDatabase.Call("GetPlayerData", findplayer.ToString(), "default") as Dictionary<string, object>)["name"] as string;
            SendReply(player, string.Format("{0} - {1} last position was: {2} {3} {4}", name, findplayer.ToString(), LastPos["x"].ToString(), LastPos["y"].ToString(), LastPos["z"].ToString()));
        }     
    }
}
