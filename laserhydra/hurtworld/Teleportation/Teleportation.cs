//Reference: UnityEngine.UI
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using System.Linq;
using Oxide.Core;
using System;

namespace Oxide.Plugins
{
    [Info("Teleportation", "LaserHydra", "1.4.1", ResourceId = 1519)]
    [Description("Teleportation plugin with many different teleportation features")]
    class Teleportation : HurtworldPlugin
    {
        #region Classes
        class Location
        {
            public float x;
            public float y;
            public float z;

            public Location(float x, float y, float z)
            {
                this.x = x;
                this.y = y;
                this.z = z;
            }

            internal Location(Vector3 vec)
            {
                this.x = vec.x;
                this.y = vec.y;
                this.z = vec.z;
            }

            internal static Location Get(string loc)
            {
                List<float> vars = (from var in loc.Split(' ') select Convert.ToSingle(var)).ToList();

                return new Location(vars[0], vars[1], vars[2]);
            }

            public override string ToString() => $"{this.x} {this.y} {this.z}";

            internal Vector3 vector
            {
                get
                {
                    return new Vector3(this.x, this.y, this.z);
                }
            }

            internal void Teleport(PlayerSession player) => player.WorldPlayerEntity.transform.position = this.vector;
        }
        #endregion

        #region Global Declaration
        Dictionary<ulong, Dictionary<string, Location>> homes = new Dictionary<ulong, Dictionary<string, Location>>();
        Dictionary<string, Location> warps = new Dictionary<string, Location>();

        Dictionary<PlayerSession, PlayerSession> pendingRequests = new Dictionary<PlayerSession, PlayerSession>();
        Dictionary<PlayerSession, Timer> pendingTimers = new Dictionary<PlayerSession, Timer>();

        Dictionary<string, DateTime> lastTpr = new Dictionary<string, DateTime>();
        Dictionary<string, DateTime> lastHome = new Dictionary<string, DateTime>();
        Dictionary<string, DateTime> lastWarp = new Dictionary<string, DateTime>();

        float tprPendingTimer = 30;
        float tprTeleportTimer = 15;

        float homeTeleportTimer = 15;
        int maxHomes = 3;

        float warpTeleportTimer = 15;
        #endregion

        #region Basic plugin Hooks
        void Loaded()
        {
            RegisterPerm("admin");
            RegisterPerm("tpr");
            RegisterPerm("home");
            RegisterPerm("warp");

            LoadConfig();
            LoadData();
            LoadMessages();

            foreach (KeyValuePair<string, object> kvp in GetConfig(new Dictionary<string, object> { { "teleportation.homelimit.vip", 5 } }, "Settings", "Home Limits"))
                RegisterPerm(kvp.Key.Replace("teleportation.", ""));

            tprPendingTimer = GetConfig(30f, "Settings", "TPR : Pending Timer");
            tprTeleportTimer = GetConfig(15f, "Settings", "TPR : Teleport Timer");

            homeTeleportTimer = GetConfig(15f, "Settings", "Home : Teleport Timer");
            maxHomes = GetConfig(3, "Settings", "Home : Maximal Homes");

            warpTeleportTimer = GetConfig(15f, "Settings", "Warp : Teleport Timer");
        }
        #endregion

        #region Data, Config & Messages saving/loading
        void LoadData()
        {
            try
            {
                //  Reading Homes
                foreach (KeyValuePair<ulong, Dictionary<string, string>> kvp in Interface.GetMod().DataFileSystem.ReadObject<Dictionary<ulong, Dictionary<string, string>>>("Teleportation/Homes"))
                {
                    Dictionary<string, Location> homeLocations = new Dictionary<string, Location>();
                    foreach (KeyValuePair<string, string> home in kvp.Value)
                        homeLocations.Add(home.Key, Location.Get(home.Value));

                    homes.Add(kvp.Key, homeLocations);
                }

                //  Reading Warps
                foreach (KeyValuePair<string, string> warp in Interface.GetMod().DataFileSystem.ReadObject<Dictionary<string, string>>("Teleportation/Warps"))
                    warps.Add(warp.Key, Location.Get(warp.Value));
            }
            catch(JsonReaderException)
            {
                //  Reading Homes
                homes = Interface.GetMod().DataFileSystem.ReadObject<Dictionary<ulong, Dictionary<string, Location>>>("Teleportation/Homes");

                //  Reading Warps
                foreach (KeyValuePair<string, string> warp in Interface.GetMod().DataFileSystem.ReadObject<Dictionary<string, string>>("Teleportation/Warps"))
                    warps.Add(warp.Key, Location.Get(warp.Value));
                
                SaveData();
            }
        }

        void SaveData()
        {
            Dictionary<ulong, Dictionary<string, string>> homesSaveable = new Dictionary<ulong, Dictionary<string, string>>();

            foreach (KeyValuePair<ulong, Dictionary<string, Location>> kvp in homes)
            {
                Dictionary<string, string> homeStrings = new Dictionary<string, string>();
                foreach (KeyValuePair<string, Location> home in kvp.Value)
                homeStrings.Add(home.Key, home.Value.ToString());

                homesSaveable.Add(kvp.Key, homeStrings);
            }

            Interface.GetMod().DataFileSystem.WriteObject("Teleportation/Homes", homesSaveable);

            Dictionary<string, string> warpsSaveable = new Dictionary<string, string>();

            foreach (KeyValuePair<string, Location> warp in warps)
                warpsSaveable.Add(warp.Key, warp.Value.ToString());

            Interface.GetMod().DataFileSystem.WriteObject("Teleportation/Warps", warpsSaveable);
        }

        void LoadConfig()
        {
            SetConfig("Settings", "TPR : Enabled", true);
            SetConfig("Settings", "TPR : Pending Timer", 30f);
            SetConfig("Settings", "TPR : Teleport Timer", 15f);
            SetConfig("Settings", "TPR : Cooldown Enabled", true);
            SetConfig("Settings", "TPR : Cooldown in minutes", 5f);
            SetConfig("Settings", "TPR : Surrender on Teleport", false);

            SetConfig("Settings", "Home : Enabled", true);
            SetConfig("Settings", "Home : Maximal Homes", 3);
            SetConfig("Settings", "Home : Teleport Timer", 15f);
            SetConfig("Settings", "Home : Stake Radius", 10f);
            SetConfig("Settings", "Home : Check for Stake", true);
            SetConfig("Settings", "Home : Cooldown Enabled", true);
            SetConfig("Settings", "Home : Cooldown in minutes", 5f);
            SetConfig("Settings", "Home : Surrender on Teleport", false);

            SetConfig("Settings", "Warp : Enabled", true);
            SetConfig("Settings", "Warp : Cooldown Enabled", true);
            SetConfig("Settings", "Warp : Cooldown in minutes", 10f);
            SetConfig("Settings", "Warp : Teleport Timer", 15f);
            SetConfig("Settings", "Warp : Surrender on Teleport", false);

            SetConfig("Settings", "Home Limits", new Dictionary<string, object>
            {
                { "teleportation.homelimit.vip", 5 }
            });

            SaveConfig();
        }

        void LoadMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                {"No Permission", "You don't have permission to use this command."},
                {"Request Ran Out", "Your pending teleport request ran out of time."},
                {"Request Sent", "Teleport request sent."},
                {"Request Got", "{player} would like to teleport to you. Accept by typing /tpa."},
                {"Teleported", "You have been teleported to {target}."},
                {"Accepted Request", "{player} has accepted your teleport request."},
                {"No Pending", "You don't have a pending teleport request."},
                {"Already Pending", "{player} already has a teleport request pending."},
                {"Teleporting Soon", "You will be teleported in {time} seconds."},
                {"Teleport To Self", "You may not teleport to yourself."},
                {"No Homes", "You do not have any homes."},
                {"Home Set", "You have set your home '{home}'"},
                {"Home Removed", "You have removed your home '{home}'"},
                {"Home Exists", "You already have a home called '{home}'"},
                {"Home Teleported", "You have been teleported to your home '{home}'"},
                {"Home List", "Your Homes: {homes}"},
                {"Max Homes", "You may not have more than {count} homes!"},
                {"Unknown Home", "You don't have a home called '{home}'"},
                {"No Stake", "You need to be close to a stake you own to set a home."},
                {"Home Cooldown", "You need to wait {time} minutes before teleporting to a home again."},
                {"TPR Cooldown", "You need to wait {time} minutes before sending the next teleport request."},
                {"Warp Set", "You have set warp '{warp}' at your current location."},
                {"Warp Removed", "You have removed warp '{warp}'"},
                {"Warp Teleported", "You have been teleported to warp '{warp}'"},
                {"Unknown Warp", "There is no warp called '{warp}'"},
                {"Warp List", "Available Warps: {warps}"},
                {"Warp Exists", "There already is a warp called '{warp}'"},
                {"No Warps", "There are no warps set."},
                {"Warp Cooldown", "You need to wait {time} minutes before teleporting to a warp again."}
            }, this);
        }

        protected override void LoadDefaultConfig() => PrintWarning("Generating new config file...");
        #endregion

        #region Admin Teleportation
        [ChatCommand("tp")]
        void cmdTeleport(PlayerSession player, string command, string[] args)
        {
            if (!HasPerm(player.SteamId, "admin"))
            {
                SendChatMessage(player, GetMsg("No Permission", player.SteamId));
                return;
            }

            switch (args.Length)
            {
                case 1:

                    PlayerSession target = GetPlayer(args[0], player);
                    if (target == null) return;

                    TeleportPlayer(player, target);
                    SendChatMessage(player, GetMsg("Teleported", player.SteamId).Replace("{target}", target.Name));

                    break;

                case 2:

                    PlayerSession teleportPlayer = GetPlayer(args[0], player);
                    PlayerSession targetPlayer = GetPlayer(args[1], player);
                    if (targetPlayer == null || teleportPlayer == null) return;

                    TeleportPlayer(teleportPlayer, targetPlayer);
                    SendChatMessage(teleportPlayer, GetMsg("Teleported", teleportPlayer.SteamId).Replace("{target}", targetPlayer.Name));

                    break;

                case 3:

                    float x = Convert.ToSingle(args[0].Replace("~", player.WorldPlayerEntity.transform.position.x.ToString()));
                    float y = Convert.ToSingle(args[1].Replace("~", player.WorldPlayerEntity.transform.position.y.ToString()));
                    float z = Convert.ToSingle(args[2].Replace("~", player.WorldPlayerEntity.transform.position.z.ToString()));

                    Teleport(player, new Vector3(x, y, z));
                    SendChatMessage(player, GetMsg("Teleported", player.SteamId).Replace("{target}", $"(X: {x}, Y: {y}, Z: {z})."));

                    break;

                default:

                    SendChatMessage(player, "/tp <target>\n/tp <player> <target>\n/tp <x> <y> <z>");

                    break;
            }
        }

        [ChatCommand("tphere")]
        void cmdTeleportHere(PlayerSession player, string command, string[] args)
        {
            if (!HasPerm(player.SteamId, "admin"))
            {
                SendChatMessage(player, GetMsg("No Permission", player.SteamId));
                return;
            }

            if (args.Length != 1)
            {
                SendChatMessage(player, "Syntax: /tphere <player>");
                return;
            }

            PlayerSession target = GetPlayer(args[0], player);
            if (target == null) return;

            if (target == player)
            {
                SendChatMessage(player, GetMsg("Teleport To Self", player.SteamId));
                return;
            }

            TeleportPlayer(target, player);
            SendChatMessage(target, GetMsg("Teleported", target.SteamId).Replace("{target}", player.Name));
        }
        #endregion

        #region Homes
        int GetHomeLimit(PlayerSession player)
        {
            int limit = GetConfig(3, "Settings", "Home : Maximal Homes");

            foreach (KeyValuePair<string, object> kvp in GetConfig(new Dictionary<string, object> { { "teleportation.homelimit.vip", 5 } }, "Settings", "Home Limits"))
                if (HasPerm(player.SteamId, kvp.Key.Replace("teleportation.", "")) && Convert.ToInt32(kvp.Value) > limit)
                    limit = Convert.ToInt32(kvp.Value);

            return limit;
        }

        [ChatCommand("removehome")]
        void cmdRemoveHome(PlayerSession player, string command, string[] args)
        {
            if (!HasPerm(player.SteamId, "home"))
            {
                SendChatMessage(player, GetMsg("No Permission", player.SteamId));
                return;
            }

            if (!GetConfig(true, "Settings", "Home : Enabled"))
                return;

            if (args.Length != 1)
            {
                SendChatMessage(player, "Syntax: /removehome <home>");
                return;
            }

            string home = args[0].ToLower();

            if (!GetHomes(player).Contains(home))
            {
                SendChatMessage(player, GetMsg("Unknown Home", player.SteamId).Replace("{home}", home));
                return;
            }

            RemoveHome(player, home);
            SendChatMessage(player, GetMsg("Home Removed", player.SteamId).Replace("{home}", home));
        }

        [ChatCommand("sethome")]
        void cmdSetHome(PlayerSession player, string command, string[] args)
        {
            if (!HasPerm(player.SteamId, "home"))
            {
                SendChatMessage(player, GetMsg("No Permission", player.SteamId));
                return;
            }

            if (!GetConfig(true, "Settings", "Home : Enabled"))
                return;

            if (GetConfig(true, "Settings", "Home : Check for Stake") && !HasStakeAuthority(player))
            {
                SendChatMessage(player, GetMsg("No Stake", player.SteamId));
                return;
            }

            if (args.Length != 1)
            {
                SendChatMessage(player, "Syntax: /sethome <home>");
                return;
            }

            string home = args[0].ToLower();

            if (GetHomes(player).Contains(home))
            {
                SendChatMessage(player, GetMsg("Home Exists", player.SteamId).Replace("{home}", home));
                return;
            }
            
            if (HomeCount(player) >= GetHomeLimit(player))
            {
                SendChatMessage(player, GetMsg("Max Homes", player.SteamId).Replace("{count}", GetHomeLimit(player).ToString()));
                return;
            }

            AddHome(player, home);
            SendChatMessage(player, GetMsg("Home Set", player.SteamId).Replace("{home}", home));
        }

        [ChatCommand("home")]
        void cmdHome(PlayerSession player, string command, string[] args)
        {
            if (!HasPerm(player.SteamId, "home"))
            {
                SendChatMessage(player, GetMsg("No Permission", player.SteamId));
                return;
            }

            if (!GetConfig(true, "Settings", "Home : Enabled"))
                return;

            if (args.Length != 1)
            {
                SendChatMessage(player, "Syntax: /home <home>");
                return;
            }

            string home = args[0].ToLower();

            if (!GetHomes(player).Contains(home))
            {
                SendChatMessage(player, GetMsg("Unknown Home", player.SteamId).Replace("{home}", home));
                return;
            }

            if (GetConfig(true, "Settings", "Home : Cooldown Enabled"))
            {
                if (lastHome.ContainsKey(player.SteamId.ToString()))
                {
                    DateTime dateTime = lastHome[player.SteamId.ToString()];
                    TimeSpan ts = DateTime.Now.Subtract(dateTime);
                    float cooldown = Convert.ToSingle(Config["Settings", "Home : Cooldown in minutes"]);
                    float nextHome = (cooldown - Convert.ToSingle(ts.Minutes));

                    if (ts.Minutes <= cooldown)
                    {
                        SendChatMessage(player, GetMsg("Home Cooldown", player.SteamId).Replace("{time}", nextHome.ToString()));
                        return;
                    }
                    else
                    {
                        lastHome[player.SteamId.ToString()] = DateTime.Now;
                    }
                }
                else
                {
                    lastHome[player.SteamId.ToString()] = DateTime.Now;
                }
            }

            if (GetConfig(false, "Settings", "Home : Surrender on Teleport"))
                StartSurrender(player);

            SendChatMessage(player, GetMsg("Teleporting Soon", player.SteamId).Replace("{time}", homeTeleportTimer.ToString()));

            timer.Once(homeTeleportTimer, () => {
                homes[id(player)][home].Teleport(player);
                SendChatMessage(player, GetMsg("Home Teleported", player.SteamId).Replace("{home}", home));

                if (GetConfig(false, "Settings", "Home : Surrender on Teleport"))
                    StopSurrender(player);
            });
        }

        [ChatCommand("homes")]
        void cmdHomes(PlayerSession player, string command, string[] args)
        {
            if (!HasPerm(player.SteamId, "home"))
            {
                SendChatMessage(player, GetMsg("No Permission", player.SteamId));
                return;
            }

            if (!GetConfig(true, "Settings", "Home : Enabled"))
                return;

            if (HomeCount(player) == 0)
                SendChatMessage(player, GetMsg("No Homes", player.SteamId));
            else
                SendChatMessage(player, GetMsg("Home List", player.SteamId).Replace("{homes}", ListToString(GetHomes(player), 0, ", ")));
        }

        void AddHome(PlayerSession player, string name)
        {
            if (!homes.ContainsKey(id(player)))
                homes.Add(id(player), new Dictionary<string, Location>());
            
            homes[id(player)].Add(name, new Location(player.WorldPlayerEntity.transform.position));

            SaveData();
        }

        void RemoveHome(PlayerSession player, string name)
        {
            if (!homes.ContainsKey(id(player)))
                homes.Add(id(player), new Dictionary<string, Location>());

            homes[id(player)].Remove(name);

            SaveData();
        }

        List<string> GetHomes(PlayerSession player)
        {
            if (!homes.ContainsKey(id(player)))
                homes.Add(id(player), new Dictionary<string, Location>());

            return homes[id(player)].Keys.ToList();
        }

        int HomeCount(PlayerSession player) => GetHomes(player).Count;
        #endregion

        #region Warps
        [ChatCommand("removewarp")]
        void cmdRemoveWarp(PlayerSession player, string command, string[] args)
        {
            if (!HasPerm(player.SteamId, "admin"))
            {
                SendChatMessage(player, GetMsg("No Permission", player.SteamId));
                return;
            }

            if (!GetConfig(true, "Settings", "Warp : Enabled"))
                return;

            if (args.Length != 1)
            {
                SendChatMessage(player, "Syntax: /removewarp <warp>");
                return;
            }

            string warp = args[0].ToLower();

            if (!warps.ContainsKey(warp))
            {
                SendChatMessage(player, GetMsg("Unknown Warp", player.SteamId).Replace("{warp}", warp));
                return;
            }

            RemoveWarp(warp);
            SendChatMessage(player, GetMsg("Warp Removed", player.SteamId).Replace("{warp}", warp));
        }

        [ChatCommand("setwarp")]
        void cmdSetWarp(PlayerSession player, string command, string[] args)
        {
            if (!HasPerm(player.SteamId, "admin"))
            {
                SendChatMessage(player, GetMsg("No Permission", player.SteamId));
                return;
            }

            if (!GetConfig(true, "Settings", "Warp : Enabled"))
                return;

            if (args.Length != 1)
            {
                SendChatMessage(player, "Syntax: /setwarp <warp>");
                return;
            }

            string warp = args[0].ToLower();

            if (warps.ContainsKey(warp))
            {
                SendChatMessage(player, GetMsg("Warp Exists", player.SteamId).Replace("{warp}", warp));
                return;
            }

            AddWarp(player, warp);
            SendChatMessage(player, GetMsg("Warp Set", player.SteamId).Replace("{warp}", warp));
        }

        [ChatCommand("warp")]
        void cmdWarp(PlayerSession player, string command, string[] args)
        {
            if (!HasPerm(player.SteamId, "warp"))
            {
                SendChatMessage(player, GetMsg("No Permission", player.SteamId));
                return;
            }

            if (!GetConfig(true, "Settings", "Warp : Enabled"))
                return;

            if (args.Length != 1)
            {
                SendChatMessage(player, "Syntax: /warp <warp>");
                return;
            }

            string warp = args[0].ToLower();

            if (!warps.ContainsKey(warp))
            {
                SendChatMessage(player, GetMsg("Unknown Warp", player.SteamId).Replace("{warp}", warp));
                return;
            }

            if (GetConfig(true, "Settings", "Warp : Cooldown Enabled"))
            {
                if (lastWarp.ContainsKey(player.SteamId.ToString()))
                {
                    DateTime dateTime = lastWarp[player.SteamId.ToString()];
                    TimeSpan ts = DateTime.Now.Subtract(dateTime);
                    float cooldown = Convert.ToSingle(Config["Settings", "Warp : Cooldown in minutes"]);
                    float nextWarp = (cooldown - Convert.ToSingle(ts.Minutes));

                    if (ts.Minutes <= cooldown)
                    {
                        SendChatMessage(player, GetMsg("Warp Cooldown", player.SteamId).Replace("{time}", nextWarp.ToString()));
                        return;
                    }
                    else
                    {
                        lastWarp[player.SteamId.ToString()] = DateTime.Now;
                    }
                }
                else
                {
                    lastWarp[player.SteamId.ToString()] = DateTime.Now;
                }
            }

            if (GetConfig(false, "Settings", "Warp : Surrender on Teleport"))
                StartSurrender(player);

            SendChatMessage(player, GetMsg("Teleporting Soon", player.SteamId).Replace("{time}", warpTeleportTimer.ToString()));

            timer.Once(warpTeleportTimer, () => {
                warps[warp].Teleport(player);
                SendChatMessage(player, GetMsg("Warp Teleported", player.SteamId).Replace("{warp}", warp));

                if (GetConfig(false, "Settings", "Warp : Surrender on Teleport"))
                    StopSurrender(player);
            });
        }

        [ChatCommand("warps")]
        void cmdWarps(PlayerSession player, string command, string[] args)
        {
            if (!HasPerm(player.SteamId, "warp"))
            {
                SendChatMessage(player, GetMsg("No Permission", player.SteamId));
                return;
            }

            if (!GetConfig(true, "Settings", "Warp : Enabled"))
                return;

            if (warps.Count == 0)
                SendChatMessage(player, GetMsg("No Warps", player.SteamId));
            else
                SendChatMessage(player, GetMsg("Warp List", player.SteamId).Replace("{warps}", ListToString(warps.Keys.ToList(), 0, ", ")));
        }

        void AddWarp(PlayerSession player, string name)
        {
            if (warps.ContainsKey(name))
                return;

            warps.Add(name, new Location(player.WorldPlayerEntity.transform.position));

            SaveData();
        }

        void RemoveWarp(string name)
        {
            if (!warps.ContainsKey(name))
                return;

            warps.Remove(name);

            SaveData();
        }
        #endregion

        #region Teleport Requests
        [ChatCommand("tpr")]
        void cmdTpr(PlayerSession player, string command, string[] args)
        {
            if (!HasPerm(player.SteamId, "tpr"))
            {
                SendChatMessage(player, GetMsg("No Permission", player.SteamId));
                return;
            }

            if (!GetConfig(true, "Settings", "TPR : Enabled"))
                return;

            if (args.Length != 1)
            {
                SendChatMessage(player, "Syntax: /tpr <player>");
                return;
            }

            PlayerSession target = GetPlayer(args[0], player);
            if (target == null) return;

            if (target == player)
            {
                SendChatMessage(player, GetMsg("Teleport To Self", player.SteamId));
                return;
            }

            if (pendingRequests.ContainsValue(target) || pendingRequests.ContainsKey(target))
            {
                SendChatMessage(player, GetMsg("Already Pending", player.SteamId).Replace("{player}", target.Name));
                return;
            }

            if (GetConfig(true, "Settings", "TPR : Cooldown Enabled"))
            {
                if (lastTpr.ContainsKey(player.SteamId.ToString()))
                {
                    DateTime dateTime = lastTpr[player.SteamId.ToString()];
                    TimeSpan ts = DateTime.Now.Subtract(dateTime);
                    float cooldown = GetConfig(5f, "Settings", "TPR : Cooldown in minutes");
                    float nextTp = (cooldown - Convert.ToSingle(ts.Minutes));

                    if (ts.Minutes <= cooldown)
                    {
                        SendChatMessage(player, GetMsg("TPR Cooldown", player.SteamId).Replace("{time}", nextTp.ToString()));
                        return;
                    }
                }
            }

            SendRequest(player, target);
        }

        [ChatCommand("tpa")]
        void cmdTpa(PlayerSession player, string command, string[] args)
        {
            if (!HasPerm(player.SteamId, "tpr"))
            {
                SendChatMessage(player, GetMsg("No Permission", player.SteamId));
                return;
            }

            if (!GetConfig(true, "Settings", "TPR : Enabled"))
                return;

            if (!pendingRequests.ContainsValue(player))
            {
                SendChatMessage(player, GetMsg("No Pending", player.SteamId));
                return;
            }

            PlayerSession source = FindKey(pendingRequests, player);

            if (source == null)
                return;

            if (GetConfig(true, "Settings", "TPR : Cooldown Enabled"))
                lastTpr[source.SteamId.ToString()] = DateTime.Now;

            SendChatMessage(source, GetMsg("Accepted Request", player.SteamId).Replace("{player}", player.Name));
            SendChatMessage(source, GetMsg("Teleporting Soon", source.SteamId).Replace("{time}", tprTeleportTimer.ToString()));

            if (GetConfig(false, "Settings", "TPR : Surrender on Teleport"))
                StartSurrender(source);

            if (pendingTimers.ContainsKey(source))
                pendingTimers[source].Destroy();

            if (pendingRequests.ContainsKey(source))
                pendingRequests.Remove(source);

            timer.Once(tprTeleportTimer, () => {
                SendChatMessage(source, GetMsg("Teleported", source.SteamId).Replace("{target}", player.Name));
                TeleportPlayer(source, player);

                if (GetConfig(false, "Settings", "TPR : Surrender on Teleport"))
                    StopSurrender(player);

                if (pendingTimers.ContainsKey(player))
                    pendingTimers.Remove(player);
            });
        }

        void SendRequest(PlayerSession player, PlayerSession target)
        {
            pendingRequests[player] = target;

            SendChatMessage(player, GetMsg("Request Sent", player.SteamId));
            SendChatMessage(target, GetMsg("Request Got", target.SteamId).Replace("{player}", player.Name));

            pendingTimers[player] = timer.Once(tprPendingTimer, () => {
                pendingRequests.Remove(player);

                SendChatMessage(player, GetMsg("Request Ran Out", player.SteamId));
                SendChatMessage(target, GetMsg("Request Ran Out", target.SteamId));
            });
        }
        #endregion

        #region General
        void StartSurrender(PlayerSession player)
        {
            EmoteManagerServer emote = player.WorldPlayerEntity.GetComponent<EmoteManagerServer>();
            emote.BeginEmoteServer(EEmoteType.Surrender);
        }

        void StopSurrender(PlayerSession player)
        {
            EmoteManagerServer emote = player.WorldPlayerEntity.GetComponent<EmoteManagerServer>();
            emote.EndEmoteServer();
        }

        ulong id(PlayerSession player) => Convert.ToUInt64(player.SteamId.ToString());

        void TeleportPlayer(PlayerSession player, PlayerSession target)
        {
            GameObject playerEntity = target.WorldPlayerEntity;
            Teleport(player, playerEntity.transform.position);
        }

        void Teleport(PlayerSession player, Vector3 location)
        {
            GameObject playerEntity = player.WorldPlayerEntity;
            playerEntity.transform.position = location;
        }
        
        K FindKey<K, V>(Dictionary<K, V> dic, V value)
        {
            foreach (KeyValuePair<K, V> kvp in dic)
                if ((object)kvp.Value == (object)value)
                    return kvp.Key;

            return default(K);
        }

        bool HasStakeAuthority(PlayerSession player)
        {
            bool hasAuthority = false;
            GameObject playerEntity = player.WorldPlayerEntity;
            float radius = GetConfig(10f, "Settings", "Home : Stake Radius");
            List<OwnershipStakeServer> entities = StakesInArea(playerEntity.transform.position, radius);

            foreach (OwnershipStakeServer entity in entities)
            {
                hasAuthority = entity.AuthorizedPlayers.Contains(player.Identity);
                break;
            }

            return hasAuthority;
        }

        List<OwnershipStakeServer> StakesInArea(Vector3 pos, float radius)
        {
            List<OwnershipStakeServer> entities = new List<OwnershipStakeServer>();

            foreach (OwnershipStakeServer entity in Resources.FindObjectsOfTypeAll<OwnershipStakeServer>())
            {
                if (Vector3.Distance(entity.transform.position, pos) <= radius)
                    entities.Add(entity);
            }

            return entities;
        }

        ////////////////////////////////////////
        ///     Player Finding
        ////////////////////////////////////////

        PlayerSession GetPlayer(string searchedPlayer, PlayerSession player)
        {
            foreach (PlayerSession current in GameManager.Instance.GetSessions().Values)
                if (current != null && current.Name != null && current.IsLoaded && current.Name.ToLower() == searchedPlayer)
                    return current;

            List<PlayerSession> foundPlayers =
                (from current in GameManager.Instance.GetSessions().Values
                 where current != null && current.Name != null && current.IsLoaded && current.Name.ToLower().Contains(searchedPlayer.ToLower())
                 select current).ToList();

            switch (foundPlayers.Count)
            {
                case 0:
                    SendChatMessage(player, "The player can not be found.");
                    break;

                case 1:
                    return foundPlayers[0];

                default:
                    List<string> playerNames = (from current in foundPlayers select current.Name).ToList();
                    string players = ListToString(playerNames, 0, ", ");
                    SendChatMessage(player, "Multiple matching players found: \n" + players);
                    break;
            }

            return null;
        }

        ////////////////////////////////////////
        ///     Converting
        ////////////////////////////////////////

        string ListToString(List<string> list, int first, string seperator) => string.Join(seperator, list.Skip(first).ToArray());

        ////////////////////////////////////////
        ///     Config & Message Related
        ////////////////////////////////////////

        void SetConfig(params object[] args)
        {
            List<string> stringArgs = (from arg in args select arg.ToString()).ToList<string>();
            stringArgs.RemoveAt(args.Length - 1);

            if (Config.Get(stringArgs.ToArray()) == null) Config.Set(args);
        }

        T GetConfig<T>(T defaultVal, params object[] args)
        {
            List<string> stringArgs = (from arg in args select arg.ToString()).ToList<string>();
            if (Config.Get(stringArgs.ToArray()) == null)
            {
                PrintError($"The plugin failed to read something from the config: {ListToString(stringArgs, 0, "/")}{Environment.NewLine}Please reload the plugin and see if this message is still showing. If so, please post this into the support thread of this plugin.");
                return defaultVal;
            }

            return (T)Convert.ChangeType(Config.Get(stringArgs.ToArray()), typeof(T));
        }

        string GetMsg(string key, object userID = null) => lang.GetMessage(key, this, userID == null ? null : userID.ToString());

        ////////////////////////////////////////
        ///     Permission Related
        ////////////////////////////////////////

        void RegisterPerm(params string[] permArray)
        {
            string perm = ListToString(permArray.ToList(), 0, ".");

            permission.RegisterPermission($"{PermissionPrefix}.{perm}", this);
        }

        bool HasPerm(object uid, params string[] permArray)
        {
            uid = uid.ToString();
            string perm = ListToString(permArray.ToList(), 0, ".");

            return permission.UserHasPermission(uid.ToString(), $"{PermissionPrefix}.{perm}");
        }

        string PermissionPrefix
        {
            get
            {
                return this.Title.Replace(" ", "").ToLower();
            }
        }

        ////////////////////////////////////////
        ///     Chat Handling
        ////////////////////////////////////////

        void BroadcastChat(string prefix, string msg = null) => hurt.BroadcastChat(msg == null ? prefix : "<color=#C4FF00>" + prefix + "</color>: " + msg);

        void SendChatMessage(PlayerSession player, string prefix, string msg = null) => hurt.SendChatMessage(player, msg == null ? prefix : "<color=#C4FF00>" + prefix + "</color>: " + msg);
        #endregion
    }
}