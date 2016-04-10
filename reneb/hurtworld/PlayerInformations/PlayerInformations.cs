// Reference: UnityEngine.UI

using System.Collections.Generic;
using System;
using System.Reflection;
using System.Data;
using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Logging;
using Oxide.Core.Plugins;
using Steamworks;
using uLink;
namespace Oxide.Plugins
{
    [Info("PlayerInformations", "Reneb", "1.0.4", ResourceId = 1497)]
    [Description("Logs players informations.")]
    public class PlayerInformations : HurtworldPlugin
    {
        [PluginReference]
        Plugin PlayerDatabase;
         
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /// Configs
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private static bool IPuse = true;
        private static string IPpermission = "canips";
        private static bool IPallow = false;
        private static int IPauthlevel = 1;
        private static int IPmaxLogs = 5;

        private static bool NAMESuse = true;
        private static string NAMESpermission = "cannames"; 
        private static bool NAMESallow = true;
        private static int NAMESauthlevel = 1;
        private static int NAMESmaxLogs = 5;

        private static bool FCuse = true;
        private static bool FCallow = true;
        private static string FCpermission = "canlastseen";
        private static int FCauthlevel = 1;

        private static bool LSuse = true;
        private static string LSpermission = "canlastseen";
        private static bool LSallow = true;
        private static int LSauthlevel = 1;

        private static bool LPuse = true;
        private static string LPpermission = "canlastposition";
        private static bool LPallow = false;
        private static int LPauthlevel = 1;

        private static bool TPuse = true;
        private static string TPpermission = "cantimeplayed";
        private static bool TPallow = true;
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
            //CheckCfg<string>("IP Logs - Permission - oxide permission", ref IPpermission);
            CheckCfg<int>("IP Logs - Permission - authlevel", ref IPauthlevel);
            CheckCfg<int>("IP Logs - Max Logs per player", ref IPmaxLogs);
            CheckCfg<bool>("IP Logs - allow players", ref IPallow);

            CheckCfg<bool>("Names Logs - activated", ref NAMESuse);
            //CheckCfg<string>("Names Logs - Permission - oxide permission", ref NAMESpermission);
            CheckCfg<int>("Names Logs - Permission - authlevel", ref NAMESauthlevel);
            CheckCfg<int>("Names Logs - Max Logs per player", ref NAMESmaxLogs);
            CheckCfg<bool>("Names Logs - allow players", ref NAMESallow);

            CheckCfg<bool>("First Connection - activated", ref FCuse);
            //CheckCfg<string>("First Connection - Permission - oxide permission", ref FCpermission);
            CheckCfg<int>("First Connection - Permission - authlevel", ref FCauthlevel);
            CheckCfg<bool>("First Connection - allow players", ref FCallow);

            CheckCfg<bool>("Last Seen - activated", ref LSuse);
            //CheckCfg<string>("Last Seen - Permission - oxide permission", ref LSpermission);
            CheckCfg<int>("Last Seen - Permission - authlevel", ref LSauthlevel);
            CheckCfg<bool>("Last Seen - allow players", ref LSallow);

            CheckCfg<bool>("Last Position - activated", ref LPuse);
            //CheckCfg<string>("Last Position - Permission - oxide permission", ref LPpermission);
            CheckCfg<int>("Last Position - Permission - authlevel", ref LPauthlevel);
            CheckCfg<bool>("Last Position - allow players", ref LPallow);

            CheckCfg<bool>("Time Played - activated", ref TPuse);
            //CheckCfg<string>("Time Played - Permission - oxide permission", ref TPpermission);
            CheckCfg<int>("Time Played - Permission - authlevel", ref TPauthlevel);
            CheckCfg<bool>("Time Played - allow players", ref TPallow);

            SaveConfig();
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /// Oxide Hooks
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        void OnServerInitialized()
        {
            if (TPuse)
            {
                foreach (KeyValuePair<CSteamID, PlayerIdentity> pair in (Dictionary<CSteamID, PlayerIdentity>)Singleton<GameManager>.Instance.GetIdentifierMap())
                {
                    StartRecordTime(pair.Value);
                }
            }
        }

        void Unload()
        {
            if (TPuse)
            {
                foreach (KeyValuePair<CSteamID, PlayerIdentity> pair in (Dictionary<CSteamID, PlayerIdentity>)Singleton<GameManager>.Instance.GetIdentifierMap())
                {
                    if (pair.Value == null)
                        continue;
                    EndRecordTime(pair.Value);
                }
            }
        }
        void OnPlayerConnected(PlayerSession player)
        {
            if (IPuse)
                RecordIP(player.Player, player.Identity);
            if (NAMESuse)
                RecordName(player.Player, player.Identity);
            if (FCuse)
                RecordFirstConnection(player.Player, player.Identity);
            if (TPuse)
                StartRecordTime(player.Identity);
        }
        void OnPlayerDisconnected(PlayerSession player)
        {
            if (LSuse)
                RecordLastSeen(player.Player, player.Identity);
            if (TPuse)
                EndRecordTime(player.Identity);
            if (LPuse)
                RecordPosition(player.Player, player.Identity);
            
        }

        [HookMethod("SendHelpText")]
        private void SendHelpText(PlayerSession player)
        {
            string msg = string.Empty;
            if (hasPermission(player, IPauthlevel, IPpermission, IPallow)) { msg += "<color=\"#ffd479\">/lastips steamid/name</color> - get the last ips used by a user\n<color=\"#ffd479\">/ipowners XX.XX.XX.XX </color>- know what players used this ip\n"; }
            if (hasPermission(player, LSauthlevel, LSpermission, LSallow)) { msg += "<color=\"#ffd479\">/lastseen steamid/name</color> - know when was this player last seen online\n"; }
            if (hasPermission(player, LPauthlevel, LPpermission, LPallow)) { msg += "<color=\"#ffd479\">/lastposition steamid/name</color> - know where is the last position of a player\n"; }
            if (hasPermission(player, FCauthlevel, FCpermission, FCallow)) { msg += "<color=\"#ffd479\">/firstconnection steamid/name</color> - know when was this player first seen online\n"; }
            if (hasPermission(player, TPauthlevel, TPpermission, TPallow)) { msg += "<color=\"#ffd479\">/played steamid/name</color> - know how much time a player has played on this server\n"; }
            if (hasPermission(player, NAMESauthlevel, NAMESpermission, NAMESallow)) { msg += "<color=\"#ffd479\">/lastnames steamid/name</color> - know the last names used by a user\n"; }
            if (msg != string.Empty)
            {
                msg = "<size=18>Players Information</size>\n" + msg;
                hurt.SendChatMessage(player, msg);
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
        private PlayerIdentity FindBasePlayerPlayer(ulong steamid)
        {
            foreach (KeyValuePair<CSteamID, PlayerIdentity> pair in (Dictionary<CSteamID, PlayerIdentity>)Singleton<GameManager>.Instance.GetIdentifierMap())
            {
                if (pair.Value.SteamId.m_SteamID == steamid)
                    return pair.Value;
            }
            return null;
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /// Permission
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        bool hasPermission(PlayerSession player, int authlevel, string permissionName, bool tempVar)
        {
            if (authlevel > 0 && player.IsAdmin) return true;
            return permission.UserHasPermission(player.SteamId.ToString(), permissionName);
            return tempVar;
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /// Record IP Related
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        void RecordIP(NetworkPlayer netPlayer, PlayerIdentity player)
        {
            if (netPlayer == null || player == null) return;
            string playerip = netPlayer.ipAddress;
            var IPlist = new Dictionary<string, object>();

            var success = PlayerDatabase.Call("GetPlayerData", player.SteamId.ToString(), "IPs");
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
            PlayerDatabase.Call("SetPlayerData", player.SteamId.m_SteamID.ToString(), "IPs", IPlist);
        }

        [ChatCommand("lastips")]
        void cmdChatLastIps(PlayerSession player, string command, string[] args)
        {
            if (!hasPermission(player, IPauthlevel, IPpermission, IPallow)) { hurt.SendChatMessage(player, "You don't have access to this command"); return; }
            if (!IPuse) { hurt.SendChatMessage(player, "The database isn't set to record the IPs of players");  return; }
            if (args.Length == 0)
            {
                hurt.SendChatMessage(player, "/lastips STEAMID/NAME");
                return;
            }
            var findplayer = FindPlayer(args[0]);
            if (!(findplayer is ulong))
            {
                hurt.SendChatMessage(player, findplayer is string ? (string)findplayer : "Couldn't find a player that matches this name.");
                return;
            }
            var IPlist = new Dictionary<string, object>();
            var success = PlayerDatabase.Call("GetPlayerData", findplayer.ToString(), "IPs");
            if (success is Dictionary<string, object>)
                IPlist = (Dictionary<string, object>)success;
            if (IPlist.Count == 0)
            {
                hurt.SendChatMessage(player, "No IPs logged for this player");
                return;
            }
            var name = (PlayerDatabase.Call("GetPlayerData", findplayer.ToString(), "default") as Dictionary<string, object>)["name"] as string;
            hurt.SendChatMessage(player, string.Format("IP List for {0} - {1}", name, findplayer.ToString()));
            foreach (KeyValuePair<string, object> pair in IPlist)
            {
                hurt.SendChatMessage(player, string.Format("{0} - {1}", pair.Key, pair.Value.ToString()));
            }
        }

        [ChatCommand("ipowners")]
        void cmdChatIPOwnerss(PlayerSession player, string command, string[] args)
        {
            if (!hasPermission(player, IPauthlevel, IPpermission, IPallow)) { hurt.SendChatMessage(player, "You don't have access to this command"); return; }
            if (!IPuse) { hurt.SendChatMessage(player, "The database isn't set to record the IPs of players"); return; }
            if (args.Length == 0)
            {
                hurt.SendChatMessage(player, "/ipowners XX.XX.XX.XX");
                return;
            }
            HashSet<string> knownPlayers = new HashSet<string>();
            var success = PlayerDatabase.Call("GetAllKnownPlayers");
            if (success is HashSet<string>)
                knownPlayers = (HashSet<string>)success;
            if (knownPlayers.Count == 0)
            {
                hurt.SendChatMessage(player, "Couldn't get the list of players");
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
            hurt.SendChatMessage(player, string.Format("Found {0} players with this matching ip", foundPlayers.Count.ToString()));
            foreach (string userid in foundPlayers)
            {
                var playerData = new Dictionary<string, object>();
                var successs = PlayerDatabase.Call("GetPlayerData", userid, "default");
                if (successs is Dictionary<string, object>)
                    playerData = (Dictionary<string, object>)successs;
                if (playerData.Count == 0) { continue; }
                hurt.SendChatMessage(player, string.Format("{0} - {1}", userid, playerData.ContainsKey("name") ? playerData["name"] : "Unknown"));
            }
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /// Last Seen Related
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        void RecordLastSeen(NetworkPlayer netPlayer, PlayerIdentity player)
        {
            var LastSeenTable = new Dictionary<string, object>
            {
                { "0" , LogTime().ToString() }
            };
            PlayerDatabase.Call("SetPlayerData", player.SteamId.ToString(), "Last Seen", LastSeenTable);
        }

        [ChatCommand("lastseen")]
        private void cmdChatLastseen(PlayerSession player, string command, string[] args)
        {
            if (!hasPermission(player, LSauthlevel, LSpermission, LSallow)) { hurt.SendChatMessage(player, "You don't have access to this command");  return; }
            if (!LSuse) { hurt.SendChatMessage(player, "This command has been deactivated"); return; }
            if (args.Length == 0)
            {
                hurt.SendChatMessage(player, "/lastseen STEAMID/NAME");
                return;
            }
            var findplayer = FindPlayer(args[0]);
            if (!(findplayer is ulong))
            {
                hurt.SendChatMessage(player, findplayer is string ? (string)findplayer : "Couldn't find a player that matches this name.");
                return;
            }
            PlayerIdentity targetPlayer = FindBasePlayerPlayer((ulong)findplayer);
            if (targetPlayer != null && targetPlayer.ConnectedSession != null)
            {
                hurt.SendChatMessage(player, "This player is connected!");
                return;
            }
            var LastSeen = new Dictionary<string, object>();
            var success = PlayerDatabase.Call("GetPlayerData", findplayer.ToString(), "Last Seen");
            if (success is Dictionary<string, object>)
                LastSeen = (Dictionary<string, object>)success;
            if (LastSeen.Count == 0)
            {
                hurt.SendChatMessage(player, "This player doesn't have a last seen logged");
                return;
            }
            var name = (PlayerDatabase.Call("GetPlayerData", findplayer.ToString(), "default") as Dictionary<string, object>)["name"] as string;
            hurt.SendChatMessage(player, string.Format("{0} - {1} was last seen: {2}", name, findplayer.ToString(), TimeMinToString(LastSeen["0"] as string)));
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /// First Connection
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        void RecordFirstConnection(NetworkPlayer netPlayer, PlayerIdentity player)
        {
            var success = PlayerDatabase.Call("GetPlayerData", player.SteamId.m_SteamID.ToString(), "First Connection");
            if (success is Dictionary<string, object>)
                return;

            var FirstConnectionTable = new Dictionary<string, object>
            {
                { "0" , LogTime().ToString() } 
            };
            PlayerDatabase.Call("SetPlayerData", player.SteamId.m_SteamID.ToString(), "First Connection", FirstConnectionTable);
        }

        [ChatCommand("firstconnection")]
        private void cmdChatfirstconnection(PlayerSession player, string command, string[] args)
        {
            if (!hasPermission(player, FCauthlevel, FCpermission, FCallow)) { hurt.SendChatMessage(player, "You don't have access to this command"); return; }
            if (!FCuse) { hurt.SendChatMessage(player, "This command has been deactivated"); return; }
            if (args.Length == 0)
            {
                hurt.SendChatMessage(player, "/firstconnection STEAMID/NAME");
                return;
            }
            var findplayer = FindPlayer(args[0]);
            if (!(findplayer is ulong))
            {
                hurt.SendChatMessage(player, findplayer is string ? (string)findplayer : "Couldn't find a player that matches this name.");
                return;
            }
            var FC = new Dictionary<string, object>();
            var success = PlayerDatabase.Call("GetPlayerData", findplayer.ToString(), "First Connection");
            if (success is Dictionary<string, object>)
                FC = (Dictionary<string, object>)success;
            if (FC.Count == 0)
            {
                hurt.SendChatMessage(player, "This player doesn't have a first connection logged");
                return;
            }
            var name = (PlayerDatabase.Call("GetPlayerData", findplayer.ToString(), "default") as Dictionary<string, object>)["name"] as string;
            hurt.SendChatMessage(player, string.Format("{0} - {1} first connected: {2}", name, findplayer.ToString(), TimeMinToString(FC["0"] as string)));
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /// Record Names
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        void RecordName(NetworkPlayer netPlayer, PlayerIdentity player)
        {
            string playername = player.Name;
            var NameList = new Dictionary<string, object>();

            var success = PlayerDatabase.Call("GetPlayerData", player.SteamId.m_SteamID.ToString(), "Names");
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
            PlayerDatabase.Call("SetPlayerData", player.SteamId.m_SteamID.ToString(), "Names", NameList);
        }

        [ChatCommand("lastnames")]
        private void cmdChatLastname(PlayerSession player, string command, string[] args)
        {
            if (!hasPermission(player, NAMESauthlevel, NAMESpermission, NAMESallow)) { hurt.SendChatMessage(player, "You don't have access to this command"); return; }
            if (!NAMESuse) { hurt.SendChatMessage(player, "This command has been deactivated"); return; }
            if (args.Length == 0)
            {
                hurt.SendChatMessage(player, "/lastnames STEAMID/NAME");
                return;
            }
            var findplayer = FindPlayer(args[0]);
            if (!(findplayer is ulong))
            {
                hurt.SendChatMessage(player, findplayer is string ? (string)findplayer : "Couldn't find a player that matches this name.");
                return;
            }
            var NameList = new Dictionary<string, object>();
            var success = PlayerDatabase.Call("GetPlayerData", findplayer.ToString(), "Names");
            if (success is Dictionary<string, object>)
                NameList = (Dictionary<string, object>)success;
            if (NameList.Count == 0)
            {
                hurt.SendChatMessage(player, "No Names logged for this player");
                return;
            }
            var name = (PlayerDatabase.Call("GetPlayerData", findplayer.ToString(), "default") as Dictionary<string, object>)["name"] as string;
            hurt.SendChatMessage(player, string.Format("Name List for {0} - {1}", name, findplayer.ToString()));
            foreach (KeyValuePair<string, object> pair in NameList)
            {
                hurt.SendChatMessage(player, string.Format("{0} - {1}", pair.Key, pair.Value.ToString()));
            }
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /// Record Time Played
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        Dictionary<PlayerIdentity, double> recordPlayTime = new Dictionary<PlayerIdentity, double>();
        void StartRecordTime( PlayerIdentity player)
        {
            if (recordPlayTime.ContainsKey(player))
                recordPlayTime.Remove(player);
            recordPlayTime.Add(player, LogTime());
        }
        void EndRecordTime(PlayerIdentity player)
        {
            if (!recordPlayTime.ContainsKey(player)) return;

            var TimePlayed = new Dictionary<string, object>();
            var success = PlayerDatabase.Call("GetPlayerData", player.SteamId.m_SteamID.ToString(), "Time Played");
            if (success is Dictionary<string, object>)
                TimePlayed = (Dictionary<string, object>)success;

            double totaltime = LogTime() - recordPlayTime[player];
            if (TimePlayed.ContainsKey("0"))
                totaltime += double.Parse((string)TimePlayed["0"]);

            TimePlayed.Clear();
            TimePlayed.Add("0", totaltime.ToString());
            PlayerDatabase.Call("SetPlayerData", player.SteamId.m_SteamID.ToString(), "Time Played", TimePlayed);
        }

        [ChatCommand("played")]
        private void cmdChatPlayed(PlayerSession player, string command, string[] args)
        {
            if (!hasPermission(player, TPauthlevel, TPpermission, TPallow)) { hurt.SendChatMessage(player, "You don't have access to this command"); return; }
            if (!TPuse) { hurt.SendChatMessage(player, "This command has been deactivated"); return; }
            if (args.Length == 0)
            {
                hurt.SendChatMessage(player, "/played STEAMID/NAME");
                return;
            }
            var findplayer = FindPlayer(args[0]);
            if (!(findplayer is ulong))
            {
                hurt.SendChatMessage(player, findplayer is string ? (string)findplayer : "Couldn't find a player that matches this name.");
                return;
            }
            var TimePlayed = new Dictionary<string, object>();
            var success = PlayerDatabase.Call("GetPlayerData", findplayer.ToString(), "Time Played");
            if (success is Dictionary<string, object>)
                TimePlayed = (Dictionary<string, object>)success;
            if (TimePlayed.Count == 0)
            {
                hurt.SendChatMessage(player, "This player doesn't have any time played recorded");
                return;
            }
            var name = (PlayerDatabase.Call("GetPlayerData", findplayer.ToString(), "default") as Dictionary<string, object>)["name"] as string;
            double tplayed = double.Parse(TimePlayed["0"] as string);
            if (recordPlayTime.ContainsKey(player.Identity))
                tplayed += LogTime() - recordPlayTime[player.Identity];
            hurt.SendChatMessage(player, string.Format("{0} - {1} played: {2}", name, findplayer.ToString(), SecondsToString(tplayed.ToString())));
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /// Position Related
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        [ChatCommand("lastposition")]
        private void cmdChatLastPosition(PlayerSession player, string command, string[] args)
        {
            if (!hasPermission(player, LSauthlevel, LSpermission, LSallow)) { hurt.SendChatMessage(player, "You don't have access to this command"); return; }
            if (!LPuse) { hurt.SendChatMessage(player, "This command has been deactivated"); return; }
            if (args.Length == 0)
            {
                hurt.SendChatMessage(player, "/lastseen STEAMID/NAME");
                return;
            }
            var findplayer = FindPlayer(args[0]);
            if (!(findplayer is ulong))
            {
                hurt.SendChatMessage(player, findplayer is string ? (string)findplayer : "Couldn't find a player that matches this name.");
                return;
            }
            PlayerIdentity targetPlayer = FindBasePlayerPlayer((ulong)findplayer);
            if (targetPlayer != null && targetPlayer.ConnectedSession != null)
            {
                hurt.SendChatMessage(player, "This player is connected!");
                return;
            }
            var LastPos = new Dictionary<string, object>();
            var success = PlayerDatabase.Call("GetPlayerData", findplayer.ToString(), "Last Position");
            if (success is Dictionary<string, object>)
                LastPos = (Dictionary<string, object>)success;
            if (LastPos.Count == 0)
            {
                hurt.SendChatMessage(player, "This player doesn't have a position logged");
                return;
            }
            var name = (PlayerDatabase.Call("GetPlayerData", findplayer.ToString(), "default") as Dictionary<string, object>)["name"] as string;
            hurt.SendChatMessage(player, string.Format("{0} - {1} last position was: {2} {3} {4}", name, findplayer.ToString(), LastPos["x"].ToString(), LastPos["y"].ToString(), LastPos["z"].ToString()));
        } 
        void RecordPosition(NetworkPlayer netPlayer, PlayerIdentity player)
        {
            var net = GameManager.GetPlayerEntity(netPlayer) as UnityEngine.GameObject;
            if (net == null) return;
            var LastPos = new Dictionary<string, object>
                    {
                        {"x", net.transform.position.x.ToString() },
                        {"y", net.transform.position.y.ToString() },
                        {"z", net.transform.position.z.ToString() }
                    };
            if (player == null) return;
            PlayerDatabase.Call("SetPlayerData", player.SteamId.m_SteamID.ToString(), "Last Position", LastPos);
        }
    }
}
