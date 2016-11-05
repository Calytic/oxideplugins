using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Oxide.Core;
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
    [Info("Player Radar", "Austinv900 & Speedy2M", "2.0.3", ResourceId = 978)]
    [Description("Allows admins to have a Radar to help detect cheaters")]

    class AdminRadar : RustPlugin
    {
        #region External Refs
        [PluginReference]
        Plugin Godmode;
        [PluginReference]
        Plugin Vanish;
        #endregion
        public AdminRadar Return() => this;

        private static AdminRadar Instance;

        #region Libraries
        Dictionary<string, PlSettings> LoadedData = new Dictionary<string, PlSettings>();
        Dictionary<string, string> NameList = new Dictionary<string, string>();
        List<string> FilterList = new List<string>();
        List<string> ActiveRadars = new List<string>();
        #endregion

        #region Radar Class
        class Radar : MonoBehaviour
        {
            // Static Variables
            BasePlayer player;
            Vector3 bodyheight = new Vector3(0f, 0.9f, 0f);
            int arrowheight = 15;
            int arrowsize = 1;
            Vector3 textheight = new Vector3(0f, 0.0f, 0f);

            Dictionary<string, string> ExtMessages = new Dictionary<string, string>()
            {
                ["player"] = "{0} - |H: {1}|CW: {2}|AT: {3}|D: {4}m",
                ["sleeper"] = "{0}(<color=red>Sleeping</color>) - |H: {1}|D: {2}m",
                ["thing"] = "{0}{1} - |D: {2}m",
                ["npc"] = "{0} - |H: {1}|D: {2}m"
            };

            Dictionary<string, string> Messages = new Dictionary<string, string>()
            {
                ["player"] = "{0} - |D: {4}m",
                ["sleeper"] = "{0}(<color=red>Sleeping</color>) - |D: {2}m",
                ["thing"] = "{0}{1} - |D: {2}m",
                ["npc"] = "{0} - |D: {2}m"
            };

            // Changable Variables
            
            public string filter;
            public float RefreshTime;
            public bool ExtDetails;
            public float setdistance;
            public Dictionary<string, string> PlayerNameList;

            public bool players;
            public bool sleepers;
            public bool npcs;
            public bool storages;
            public bool toolcupboards;

            public bool playerbox;
            public bool arrows;

            AdminRadar ar = Instance;

            void Awake()
            {
                player = GetComponent<BasePlayer>();
                ar.Puts($"{ar.ConfigVersion} {ar.ChatIcon} {ar.ChatPrefix} {ar.defaultAllDistance}");
            }

            void radar()
            {
                if (players)
                {
                    if (filter == "all" || filter == "player")
                    {
                        string message = (ExtDetails) ? ExtMessages["player"] : Messages["player"];
                        foreach (var target in BasePlayer.activePlayerList)
                        {
                            bool posval = target.transform.position != new Vector3(0, 0, 0);
                            var distance = Math.Round(Vector3.Distance(target.transform.position, player.transform.position), 1);
                            if (distance < setdistance && target != player && posval)
                            {
                                var health = Math.Round(target.Health(), 0).ToString();
                                var cw = target?.GetActiveItem()?.info?.displayName?.english ?? "None";
								var weapon = target?.GetHeldEntity()?.GetComponent<BaseProjectile>() ?? null;
								var attachments = string.Empty;
								var contents = weapon?.GetItem()?.contents ?? null;
								if (weapon != null && contents != null && contents.itemList.Count >= 1)
								{
								attachments += "";
								for (int ii = 0; ii < contents.itemList.Count; ii++)
  								{
								var item = contents.itemList[ii];
								if (item == null) continue;
								attachments += item?.info?.displayName?.english ?? "None";
								}
								attachments += "";
								}
                                var msg = message.Replace("{0}", target.displayName).Replace("{1}", health).Replace("{2}", cw).Replace("{3}", attachments).Replace("{4}", distance.ToString());

                                if (playerbox) player.SendConsoleCommand("ddraw.box", RefreshTime, Color.green, target.transform.position + bodyheight, target.GetHeight());
                                player.SendConsoleCommand("ddraw.text", RefreshTime, Color.yellow, target.transform.position + textheight, msg);
                            }
                        }
                    }
                }
                if (sleepers)
                {
                    if (filter == "all" || filter == "sleeper")
                    {
                        string message = (ExtDetails) ? ExtMessages["sleeper"] : Messages["sleeper"];
                        foreach (var sleeper in BasePlayer.sleepingPlayerList)
                        {
                            bool posval = sleeper.transform.position != new Vector3(0, 0, 0);
                            var distance = Math.Round(Vector3.Distance(sleeper.transform.position, player.transform.position), 1);
                            var msg = message.Replace("{0}", sleeper.displayName).Replace("{1}", Math.Round(sleeper.Health(), 0).ToString()).Replace("{2}", distance.ToString());
                            if (distance < setdistance && posval)
                            {
                                player.SendConsoleCommand("ddraw.text", RefreshTime, UnityEngine.Color.grey, sleeper.transform.position + textheight, msg);
                            }
                        }
                    }
                }
                if (toolcupboards)
                {
                    if (filter == "all" || filter == "toolcupboard")
                    {
                        string message = (ExtDetails) ? ExtMessages["thing"] : Messages["thing"];
                        foreach (var Cupboard in Resources.FindObjectsOfTypeAll<BuildingPrivlidge>())
                        {
                            bool posval = Cupboard.transform.position != new Vector3(0, 0, 0);
                            var distance = Math.Round(Vector3.Distance(Cupboard.transform.position, player.transform.position), 1);
                            if (distance < setdistance && posval)
                            {
                                var arrowSky = Cupboard.transform.position;
                                var arrowGround = arrowSky + new Vector3(0, 0.9f, 0);
                                arrowGround.y = arrowGround.y + arrowheight;
                                var owner = FindOwner(Cupboard.OwnerID);
                                var msg = message.Replace("{0}", replacement(Cupboard.ShortPrefabName)).Replace("{1}", $"[{owner}]").Replace("{2}", distance.ToString());

                                if (arrows) player.SendConsoleCommand("ddraw.arrow", RefreshTime, Color.yellow, arrowGround, arrowSky, arrowsize);
                                player.SendConsoleCommand("ddraw.text", RefreshTime, UnityEngine.Color.magenta, Cupboard.transform.position + new Vector3(0f, 0.05f, 0f), msg);
                            }
                        }
                    }
                }
                if (storages)
                {
                    if (filter == "all" || filter == "storage")
                    {
                        string message = (ExtDetails) ? ExtMessages["thing"] : Messages["thing"];
                        foreach (var storage in Resources.FindObjectsOfTypeAll<StorageContainer>().Where(storage => storage.name.Contains("box.wooden.large.prefab") || storage.name.Contains("woodbox_deployed.prefab") || storage.name.Contains("heli_crate.prefab") || storage.name.Contains("small_stash_deployed.prefab")))
                        {
                            bool posval = storage.transform.position != new Vector3(0, 0, 0);
                            var distance = Math.Round(Vector3.Distance(storage.transform.position, player.transform.position), 1);
                            if (distance < setdistance && posval)
                            {
                                var owner = FindOwner(storage.OwnerID);
                                var arrowSky = storage.transform.position;
                                var arrowGround = arrowSky;
                                var msg = message.Replace("{0}", replacement(storage.ShortPrefabName)).Replace("{1}", $"[{owner}]").Replace("{2}", distance.ToString());
                                arrowGround.y = arrowGround.y + arrowheight;

                                if (arrows) player.SendConsoleCommand("ddraw.arrow", RefreshTime, Color.blue, arrowGround, arrowSky, arrowsize);
                                player.SendConsoleCommand("ddraw.text", RefreshTime, Color.green, storage.transform.position + new Vector3(0f, 0.05f, 0f), msg);
                            }
                        }
                    }
                }
                if (npcs)
                {
                    if (filter == "all" || filter == "npc")
                    {
                        string message = (ExtDetails) ? ExtMessages["npc"] : Messages["npc"];
                        foreach (var npc in Resources.FindObjectsOfTypeAll<BaseNPC>())
                        {
                            bool posval = npc.transform.position != new Vector3(0, 0, 0);
                            var distance = Math.Round(Vector3.Distance(npc.transform.position, player.transform.position), 1);
                            if (distance < setdistance && posval)
                            {
                                var health = Math.Round(npc.Health(), 0).ToString();
                                var msg = message.Replace("{0}", npc.ShortPrefabName.Replace(".prefab", string.Empty)).Replace("{1}", health).Replace("{2}", distance.ToString());

                                player.SendConsoleCommand("ddraw.text", RefreshTime, Color.yellow, npc.transform.position + textheight, msg);
                            }
                        }
                    }
                }
            }
            string FindOwner(ulong id)
            {
                string ID = id.ToString();
                return (PlayerNameList.ContainsKey(ID)) ? PlayerNameList[ID] : "MAP";
            }
            bool SpectateCheck(BasePlayer player, BasePlayer target) => player.IsSpectating() && target.HasChild(player);

            string replacement(string name)
            {
                return name.Replace(".prefab", string.Empty).Replace(".wooden.", string.Empty).Replace("_deployed", string.Empty).Replace("small_", string.Empty).Replace("_deployed", string.Empty).Replace(".tool.deployed", string.Empty).Replace("_", " ").ToUpper();
            }
        }
        #endregion

        #region Oxide
        void Init()
        {
            LoadDefaultConfig();
            LoadFilterList();
            LoadMessages();
            LoadSavedData();
            permission.RegisterPermission("adminradar." + permAllowed, this);
            Instance = this;
        }
        void Unload()
        {
            SaveLoadedData();

            foreach (var pl in BasePlayer.activePlayerList)
            {
                if (pl.GetComponent<Radar>()) GameObject.Destroy(pl.GetComponent<Radar>());
                if (ActiveRadars.Contains(pl.UserIDString)) ActiveRadars.Remove(pl.UserIDString);
            }
        }
        void OnServerSave()
        {
            SaveLoadedData();
        }
        void OnPlayerDisconnected(BasePlayer player)
        {
            if (player.GetComponent<Radar>()) { GameObject.Destroy(player.GetComponent<Radar>()); if (ActiveRadars.Contains(player.UserIDString)) ActiveRadars.Remove(player.UserIDString); }
        }

        #endregion

        #region Configuration
        // General Settings
        string permAllowed;
        bool playerRadar;
        bool ShowExtData;
        string ChatIcon;
        string ChatPrefix;
        string ConfigVersion { get { return GetConfig("2.0.0", "DoNotTouch", "ConfigVersion"); } }

        // Filters
        bool Tplayer;
        bool Tstorage;
        bool Tsleeper;
        bool Ttoolcupboard;
        bool Tnpc;
        bool Tall;

        // Default Values
        string defaultFilter;
        float defaultAllInvoke;
        float defaultAllDistance;
        float defaultPlayerInvoke;
        float defaultPlayerMaxDistance;
        float defaultSleeperInvoke;
        float defaultSleeperMaxDistance;
        float defaultstorageInvoke;
        float defaultStorageMaxDistance;
        float defaultToolCupboardInvoke;
        float defaultToolCupboardMaxDistance;
        float defaultNPCInvoke;
        float defaultNPCMaxDistance;

        // Invoke Limiting
        float limitAllInvokeHigh;
        float limitAllInvokeLow;
        float limitPlayerInvokeHigh;
        float limitPlayerInvokeLow;
        float limitSleeperInvokeHigh;
        float limitSleeperInvokeLow;
        float limitstorageInvokeHigh;
        float limitstorageInvokeLow;
        float limitToolCupboardInvokeHigh;
        float limitToolCupboardInvokeLow;
        float limitNPCInvokeHigh;
        float limitNPCInvokeLow;

        // Distance Limiting
        float limitAllDistanceHigh;
        float limitAllDistanceLow;
        float limitPlayerDistanceHigh;
        float limitPlayerDistanceLow;
        float limitSleeperDistanceHigh;
        float limitSleeperDistanceLow;
        float limitstorageDistanceHigh;
        float limitstorageDistanceLow;
        float limitToolCupboardDistanceHigh;
        float limitToolCupboardDistanceLow;
        float limitNPCDistanceHigh;
        float limitNPCDistanceLow;

        // Misc Settings
        bool radarboxs;
        bool radararrows;

        protected override void LoadDefaultConfig()
        {
            // General Settings
            SetConfig("General", "Permission (adminradar.?)", "allowed");
            SetConfig("General", "Radar", "ShowExtendedDetails", true);
            SetConfig("General", "Radar", "ShowPlayerBox", true);
            SetConfig("General", "Radar", "ShowArrow", true);
            SetConfig("General", "Chat", "IconProfile", string.Empty);
            SetConfig("General", "Chat", "ChatPrefix", "AdminRadar");
            SetConfig("General", "Commands", "GiveRadar", false);

            // Enabled Filters
            SetConfig("Settings", "Filters", "DefaultFilter | player | storage | sleeper | toolcupboard | npc", "player");
            SetConfig("Settings", "Filters", "Players", "Enabled", true);
            SetConfig("Settings", "Filters", "Storage", "Enabled", true);
            SetConfig("Settings", "Filters", "SleepingPlayers", "Enabled", true);
            SetConfig("Settings", "Filters", "ToolCupboards", "Enabled", true);
            SetConfig("Settings", "Filters", "NPCS", "Enabled", true);
            SetConfig("Settings", "Filters", "All", "Enabled (Can Cause Server Lag)", true);

            // Default Values
            /* All Settings */
            SetConfig("Settings", "Filters", "All", "DefaultInvoke", 1.5f);
            SetConfig("Settings", "Filters", "All", "MaxDistance", 300f);
            SetConfig("Settings", "Filters", "All", "Distance-Lowest", 30f);
            SetConfig("Settings", "Filters", "All", "Distance-Highest", 400f);
            SetConfig("Settings", "Filters", "All", "Invoke-Lowest", 1f);
            SetConfig("Settings", "Filters", "All", "Invoke-Highest", 3f);
            /* Player Settings */
            SetConfig("Settings", "Filters", "Players", "DefaultInvoke", 0.30f);
            SetConfig("Settings", "Filters", "Players", "MaxDistance", 2000f);
            SetConfig("Settings", "Filters", "Players", "Distance-Lowest", 100f);
            SetConfig("Settings", "Filters", "Players", "Distance-Highest", 1000f);
            SetConfig("Settings", "Filters", "Players", "Invoke-Lowest", 0.10f);
            SetConfig("Settings", "Filters", "Players", "Invoke-Highest", 1.00f);
            /* Storage Settings */
            SetConfig("Settings", "Filters", "Storage", "DefaultInvoke", 5.00f);
            SetConfig("Settings", "Filters", "Storage", "MaxDistance", 300f);
            SetConfig("Settings", "Filters", "Storage", "Distance-Lowest", 50f);
            SetConfig("Settings", "Filters", "Storage", "Distance-Highest", 300f);
            SetConfig("Settings", "Filters", "Storage", "Invoke-Lowest", 1.00f);
            SetConfig("Settings", "Filters", "Storage", "Invoke-Highest", 10.00f);
            /* Sleepers Settings */
            SetConfig("Settings", "Filters", "SleepingPlayers", "DefaultInvoke", 5.00f);
            SetConfig("Settings", "Filters", "SleepingPlayers", "MaxDistance", 300f);
            SetConfig("Settings", "Filters", "SleepingPlayers", "Distance-Lowest", 50f);
            SetConfig("Settings", "Filters", "SleepingPlayers", "Distance-Highest", 300f);
            SetConfig("Settings", "Filters", "SleepingPlayers", "Invoke-Lowest", 1.00f);
            SetConfig("Settings", "Filters", "SleepingPlayers", "Invoke-Highest", 10.00f);
            /* ToolCupboard Settings */
            SetConfig("Settings", "Filters", "ToolCupboards", "DefaultInvoke", 5.00f);
            SetConfig("Settings", "Filters", "ToolCupboards", "MaxDistance", 300f);
            SetConfig("Settings", "Filters", "ToolCupboards", "Distance-Lowest", 50f);
            SetConfig("Settings", "Filters", "ToolCupboards", "Distance-Highest", 300f);
            SetConfig("Settings", "Filters", "ToolCupboards", "Invoke-Lowest", 1.00f);
            SetConfig("Settings", "Filters", "ToolCupboards", "Invoke-Highest", 10.00f);
            /* NPC Settings */
            SetConfig("Settings", "Filters", "NPCS", "DefaultInvoke", 0.30f);
            SetConfig("Settings", "Filters", "NPCS", "MaxDistance", 300f);
            SetConfig("Settings", "Filters", "NPCS", "Distance-Lowest", 50f);
            SetConfig("Settings", "Filters", "NPCS", "Distance-Highest", 300f);
            SetConfig("Settings", "Filters", "NPCS", "Invoke-Lowest", 0.10f);
            SetConfig("Settings", "Filters", "NPCS", "Invoke-Highest", 1.00f);

            SetConfig("DoNotTouch", "ConfigVersion", "2.0.0");

            SaveConfig();

            ////////////////////////////////////////////////////////////////////
            ////                    Setting the Values                      ////
            ////////////////////////////////////////////////////////////////////

            // General Settings
            permAllowed = GetConfig("allowed", "General", "Permission (adminradar.?)");
            ShowExtData = GetConfig(true, "General", "Radar", "ShowExtendedDetails");
            radarboxs = GetConfig(true, "General", "Radar", "ShowPlayerBox");
            radararrows = GetConfig(true, "General", "Radar", "ShowArrow");
            ChatIcon = GetConfig(string.Empty, "General", "Chat", "IconProfile");
            ChatPrefix = GetConfig("AdminRadar", "General", "Chat", "ChatPrefix");
            playerRadar = GetConfig(false, "General", "Commands", "GiveRadar");

            // Enabled Filters
            defaultFilter = GetConfig("player", "Settings", "Filters", "DefaultFilter | player | storage | sleeper | toolcupboard | npc");
            Tplayer = GetConfig(true, "Settings", "Filters", "Players", "Enabled");
            Tstorage = GetConfig(true, "Settings", "Filters", "Storage", "Enabled");
            Tsleeper = GetConfig(true, "Settings", "Filters", "SleepingPlayers", "Enabled");
            Ttoolcupboard = GetConfig(true, "Settings", "Filters", "ToolCupboards", "Enabled");
            Tnpc = GetConfig(true, "Settings", "Filters", "NPCS", "Enabled");
            Tall = GetConfig(true, "Settings", "Filters", "All", "Enabled (Can Cause Server Lag)");

            // Default Values
            defaultPlayerInvoke = GetConfig(0.30f, "Settings", "Filters", "Players", "DefaultInvoke");
            defaultPlayerMaxDistance = GetConfig(2000f, "Settings", "Filters", "Players", "MaxDistance");
            limitPlayerDistanceHigh = GetConfig(1000f, "Settings", "Filters", "Players", "Distance-Highest");
            limitPlayerDistanceLow = GetConfig(100f, "Settings", "Filters", "Players", "Distance-Lowest");
            limitPlayerInvokeHigh = GetConfig(1.00f, "Settings", "Filters", "Players", "Invoke-Highest");
            limitPlayerInvokeLow = GetConfig(0.10f, "Settings", "Filters", "Players", "Invoke-Lowest");

            defaultSleeperInvoke = GetConfig(5.00f, "Settings", "Filters", "SleepingPlayers", "DefaultInvoke");
            defaultSleeperMaxDistance = GetConfig(300f, "Settings", "Filters", "SleepingPlayers", "MaxDistance");
            limitSleeperDistanceHigh = GetConfig(300f, "Settings", "Filters", "SleepingPlayers", "Distance-Highest");
            limitSleeperDistanceLow = GetConfig(50f, "Settings", "Filters", "SleepingPlayers", "Distance-Lowest");
            limitSleeperInvokeHigh = GetConfig(10.00f, "Settings", "Filters", "SleepingPlayers", "Invoke-Highest");
            limitSleeperInvokeLow = GetConfig(1.00f, "Settings", "Filters", "SleepingPlayers", "Invoke-Lowest");

            defaultstorageInvoke = GetConfig(5.00f, "Settings", "Filters", "Storage", "DefaultInvoke");
            defaultStorageMaxDistance = GetConfig(300f, "Settings", "Filters", "Storage", "MaxDistance");
            limitstorageDistanceHigh = GetConfig(300f, "Settings", "Filters", "Storage", "Distance-Highest");
            limitstorageDistanceLow = GetConfig(50f, "Settings", "Filters", "Storage", "Distance-Lowest");
            limitstorageInvokeHigh = GetConfig(10.00f, "Settings", "Filters", "Storage", "Invoke-Highest");
            limitstorageInvokeLow = GetConfig(1.00f, "Settings", "Filters", "Storage", "Invoke-Lowest");

            defaultToolCupboardInvoke = GetConfig(5.00f, "Settings", "Filters", "ToolCupboards", "DefaultInvoke");
            defaultToolCupboardMaxDistance = GetConfig(300f, "Settings", "Filters", "ToolCupboards", "MaxDistance");
            limitToolCupboardDistanceHigh = GetConfig(300f, "Settings", "Filters", "ToolCupboards", "Distance-Highest");
            limitToolCupboardDistanceLow = GetConfig(50f, "Settings", "Filters", "ToolCupboards", "Distance-Lowest");
            limitToolCupboardInvokeHigh = GetConfig(10.00f, "Settings", "Filters", "ToolCupboards", "Invoke-Highest");
            limitToolCupboardInvokeLow = GetConfig(1.00f, "Settings", "Filters", "ToolCupboards", "Invoke-Lowest");

            defaultNPCInvoke = GetConfig(0.30f, "Settings", "Filters", "NPCS", "DefaultInvoke");
            defaultNPCMaxDistance = GetConfig(300f, "Settings", "Filters", "NPCS", "MaxDistance");
            limitNPCDistanceHigh = GetConfig(300f, "Settings", "Filters", "NPCS", "Distance-Highest");
            limitNPCDistanceLow = GetConfig(50f, "Settings", "Filters", "NPCS", "Distance-Lowest");
            limitNPCInvokeHigh = GetConfig(1.00f, "Settings", "Filters", "NPCS", "Invoke-Highest");
            limitNPCInvokeLow = GetConfig(0.10f, "Settings", "Filters", "NPCS", "Invoke-Lowest");

            defaultAllInvoke = GetConfig(1.5f, "Settings", "Filters", "All", "DefaultInvoke");
            defaultAllDistance = GetConfig(300f, "Settings", "Filters", "All", "MaxDistance");
            limitAllDistanceHigh = GetConfig(400f, "Settings", "Filters", "All", "Distance-Highest");
            limitAllDistanceLow = GetConfig(30f, "Settings", "Filters", "All", "Distance-Lowest");
            limitAllInvokeHigh = GetConfig(3f, "Settings", "Filters", "All", "Invoke-Highest");
            limitAllInvokeLow = GetConfig(1f, "Settings", "Filters", "All", "Invoke-Lowest");
            //ConfigVersion = GetConfig("2.0.0", "DoNotTouch", "ConfigVersion");

            if (ConfigVersion != "2.0.0") Puts("Config File is Outdated - Please delete current config and reload the plugin");
        }
        #endregion

        #region Localization
        void LoadMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>()
            {
                ["RadarOff"] = "Radar has been <color=red>DEACTIVATED</color>",
                ["NoAccess"] = "Unknown command: {0}",
                ["RadarOn"] = "Radar has been <color=green>ACTIVATED</color> | <color=aqua>Filter <color=green>{0}</color>, RefreshTime <color=yellow>{1}</color>, Distance <color=purple>{2}</color></color>",
                ["InvalidSyntax"] = "Invalid command syntax: /{0} help",
                ["CommandDisabled"] = "The Command /{0} {1} has been disabled by the server administrator",
                ["RadarGive"] = "Radar has been {0} for {1}",
                ["Enabled"] = "<color=green>Enabled</color>",
                ["Disabled"] = "<color=red>Disabled</color>",
                ["SettingUpdate"] = "Setting {0} {1} has been changed from {2} to {3}",
                ["RadarList"] = "------[ ActiveRadars ]------\n{0}",
                ["NoRadars"] = "No players are currently using radar"
            }, this, "en");
        }
        #endregion

        #region Commands
        [ChatCommand("radar")]
        void ccmdRadar(BasePlayer player, string command, string[] args)
        {
            if (!Allowed(player)) { player.ChatMessage(Lang("NoAccess", player.UserIDString, command)); return; }
            if (args.Length == 0) { ToggleRadar(player); return; }
            if (args.Length == 1 && FilterList.Contains(filterValidation(args[0]))) { ToggleRadar(player, filterValidation(args[0])); return; }

            switch (args[0].ToLower())
            {
                case "give":
                    if (!playerRadar) { SendMessage(player, Lang("CommandDisabled", player.UserIDString, command, args[0])); return; }
                    if (args.Length < 2 || args.Length > 3) { SendMessage(player, Lang("InvalidSyntax", player.UserIDString, command)); return; }
                    var target = player;
                    string enabled = string.Empty;
                    if (args.Length == 2)
                    {
                        target = rust.FindPlayer(args[1]);
                        if (target.GetComponent<Radar>()) { enabled = Lang("Disabled", player.UserIDString); } else { enabled = Lang("Enabled", player.UserIDString); }
                        ToggleRadar(target);
                    }
                    if (args.Length == 3)
                    {
                        target = rust.FindPlayer(args[1]);
                        if (target.GetComponent<Radar>()) { enabled = Lang("Disabled", player.UserIDString); } else { enabled = Lang("Enabled", player.UserIDString); }
                        ToggleRadar(target, args[2]);
                    }
                    SendMessage(player, Lang("RadarGive", player.UserIDString, enabled, target.displayName));
                    break;

                case "list":
                    if (args.Length > 1) { SendMessage(player, Lang("InvalidSyntax", player.UserIDString, command)); return; }
                    string activeplayers = string.Empty;
                    if (RadarList(out activeplayers))
                    {
                        player.ChatMessage(Lang("RadarList", player.UserIDString, activeplayers));
                        return;
                    }
                    SendMessage(player, Lang("NoRadars", player.UserIDString));
                    break;

                case "help":
                    SendHelpText(player);
                    break;

                case "filterlist":
                    if (args.Length > 1) { SendMessage(player, Lang("InvalidSyntax", player.UserIDString, command)); return; }
                    string msg = "<color=red>Filter List</color>\n";
                    foreach (var filter in FilterList)
                    {
                        msg += $"<color=green>{filter}</color>\n";
                    }
                    SendMessage(player, msg);
                    break;

                case "setting":
                    if (args.Length == 1) { SendMessage(player, Lang("InvalidSyntax", player.UserIDString, command)); return; }
                    switch (args[1].ToLower())
                    {
                        case "player":
                            if (!HasPlayerData(player.UserIDString)) CreatePlayerData(player.UserIDString);
                            if (args.Length > 4 || args.Length <= 3) { SendMessage(player, Lang("InvalidSyntax", player.UserIDString, command)); return; }
                            if (args.Length == 4 && settingValidation(args[2]) == "invoke")
                            {
                                var oldsetting = LoadedData[player.UserIDString].playerinvoke;
                                float updatevalue;
                                float.TryParse(args[3], out updatevalue);
                                var newsetting = invokeClamp(args[1], updatevalue);
                                LoadedData[player.UserIDString].playerinvoke = newsetting;
                                SendMessage(player, Lang("SettingUpdate", player.UserIDString, args[1], settingValidation(args[2]), oldsetting.ToString(), newsetting.ToString()));
                            }
                            if (args.Length == 4 && settingValidation(args[2]) == "distance")
                            {
                                var oldsetting = LoadedData[player.UserIDString].playerdistance;
                                float updatevalue;
                                float.TryParse(args[3], out updatevalue);
                                var newsetting = distanceClamp(args[1], updatevalue);
                                LoadedData[player.UserIDString].playerdistance = newsetting;
                                SendMessage(player, Lang("SettingUpdate", player.UserIDString, args[1], settingValidation(args[2]), oldsetting.ToString(), newsetting.ToString()));
                            }
                            break;

                        case "sleeper":
                            if (!HasPlayerData(player.UserIDString)) CreatePlayerData(player.UserIDString);
                            if (args.Length > 4 || args.Length <= 3) { SendMessage(player, Lang("InvalidSyntax", player.UserIDString, command)); return; }
                            if (args.Length == 4 && settingValidation(args[2]) == "invoke")
                            {
                                var oldsetting = LoadedData[player.UserIDString].sleeperinvoke;
                                float updatevalue;
                                float.TryParse(args[3], out updatevalue);
                                var newsetting = invokeClamp(args[1], updatevalue);
                                LoadedData[player.UserIDString].sleeperinvoke = newsetting;
                                SendMessage(player, Lang("SettingUpdate", player.UserIDString, args[1], settingValidation(args[2]), oldsetting.ToString(), newsetting.ToString()));
                            }
                            if (args.Length == 4 && settingValidation(args[2]) == "distance")
                            {
                                var oldsetting = LoadedData[player.UserIDString].sleeperdistance;
                                float updatevalue;
                                float.TryParse(args[3], out updatevalue);
                                var newsetting = distanceClamp(args[1], updatevalue);
                                LoadedData[player.UserIDString].sleeperdistance = newsetting;
                                SendMessage(player, Lang("SettingUpdate", player.UserIDString, args[1], settingValidation(args[2]), oldsetting.ToString(), newsetting.ToString()));
                            }
                            break;

                        case "npc":
                            if (!HasPlayerData(player.UserIDString)) CreatePlayerData(player.UserIDString);
                            if (args.Length > 4 || args.Length <= 3) { SendMessage(player, Lang("InvalidSyntax", player.UserIDString, command)); return; }
                            if (args.Length == 4 && settingValidation(args[2]) == "invoke")
                            {
                                var oldsetting = LoadedData[player.UserIDString].npcinvoke;
                                float updatevalue;
                                float.TryParse(args[3], out updatevalue);
                                var newsetting = invokeClamp(args[1], updatevalue);
                                LoadedData[player.UserIDString].npcinvoke = newsetting;
                                SendMessage(player, Lang("SettingUpdate", player.UserIDString, args[1], settingValidation(args[2]), oldsetting.ToString(), newsetting.ToString()));
                            }
                            if (args.Length == 4 && settingValidation(args[2]) == "distance")
                            {
                                var oldsetting = LoadedData[player.UserIDString].npcdistance;
                                float updatevalue;
                                float.TryParse(args[3], out updatevalue);
                                var newsetting = distanceClamp(args[1], updatevalue);
                                LoadedData[player.UserIDString].npcdistance = newsetting;
                                SendMessage(player, Lang("SettingUpdate", player.UserIDString, args[1], settingValidation(args[2]), oldsetting.ToString(), newsetting.ToString()));
                            }
                            break;

                        case "storage":
                            if (!HasPlayerData(player.UserIDString)) CreatePlayerData(player.UserIDString);
                            if (args.Length > 4 || args.Length <= 3) { SendMessage(player, Lang("InvalidSyntax", player.UserIDString, command)); return; }
                            if (args.Length == 4 && settingValidation(args[2]) == "invoke")
                            {
                                var oldsetting = LoadedData[player.UserIDString].storageinvoke;
                                float updatevalue;
                                float.TryParse(args[3], out updatevalue);
                                var newsetting = invokeClamp(args[1], updatevalue);
                                LoadedData[player.UserIDString].storageinvoke = newsetting;
                                SendMessage(player, Lang("SettingUpdate", player.UserIDString, args[1], settingValidation(args[2]), oldsetting.ToString(), newsetting.ToString()));
                            }
                            if (args.Length == 4 && settingValidation(args[2]) == "distance")
                            {
                                var oldsetting = LoadedData[player.UserIDString].storagedistance;
                                float updatevalue;
                                float.TryParse(args[3], out updatevalue);
                                var newsetting = distanceClamp(args[1], updatevalue);
                                LoadedData[player.UserIDString].storagedistance = newsetting;
                                SendMessage(player, Lang("SettingUpdate", player.UserIDString, args[1], settingValidation(args[2]), oldsetting.ToString(), newsetting.ToString()));
                            }
                            break;

                        case "toolcupboard":
                            if (!HasPlayerData(player.UserIDString)) CreatePlayerData(player.UserIDString);
                            if (args.Length > 4 || args.Length <= 3) { SendMessage(player, Lang("InvalidSyntax", player.UserIDString, command)); return; }
                            if (args.Length == 4 && settingValidation(args[2]) == "invoke")
                            {
                                var oldsetting = LoadedData[player.UserIDString].toolcupboardinvoke;
                                float updatevalue;
                                float.TryParse(args[3], out updatevalue);
                                var newsetting = invokeClamp(args[1], updatevalue);
                                LoadedData[player.UserIDString].toolcupboardinvoke = newsetting;
                                SendMessage(player, Lang("SettingUpdate", player.UserIDString, args[1], settingValidation(args[2]), oldsetting.ToString(), newsetting.ToString()));
                            }
                            if (args.Length == 4 && settingValidation(args[2]) == "distance")
                            {
                                var oldsetting = LoadedData[player.UserIDString].toolcupboarddistance;
                                float updatevalue;
                                float.TryParse(args[3], out updatevalue);
                                var newsetting = distanceClamp(args[1], updatevalue);
                                LoadedData[player.UserIDString].toolcupboarddistance = newsetting;
                                SendMessage(player, Lang("SettingUpdate", player.UserIDString, args[1], settingValidation(args[2]), oldsetting.ToString(), newsetting.ToString()));
                            }
                            break;

                        case "all":
                            if (!HasPlayerData(player.UserIDString)) CreatePlayerData(player.UserIDString);
                            if (args.Length > 4 || args.Length <= 3) { SendMessage(player, Lang("InvalidSyntax", player.UserIDString, command)); return; }
                            if (args.Length == 4 && settingValidation(args[2]) == "invoke")
                            {
                                var oldsetting = LoadedData[player.UserIDString].allinvoke;
                                float updatevalue;
                                float.TryParse(args[3], out updatevalue);
                                var newsetting = invokeClamp(args[1], updatevalue);
                                LoadedData[player.UserIDString].allinvoke = newsetting;
                                SendMessage(player, Lang("SettingUpdate", player.UserIDString, args[1], settingValidation(args[2]), oldsetting.ToString(), newsetting.ToString()));
                            }
                            if (args.Length == 4 && settingValidation(args[2]) == "distance")
                            {
                                var oldsetting = LoadedData[player.UserIDString].alldistance;
                                float updatevalue;
                                float.TryParse(args[3], out updatevalue);
                                var newsetting = distanceClamp(args[1], updatevalue);
                                LoadedData[player.UserIDString].alldistance = newsetting;
                                SendMessage(player, Lang("SettingUpdate", player.UserIDString, args[1], settingValidation(args[2]), oldsetting.ToString(), newsetting.ToString()));
                            }
                            break;

                        case "filter":
                            if (!HasPlayerData(player.UserIDString)) CreatePlayerData(player.UserIDString);
                            if (args.Length > 3 || args.Length <= 2) { SendMessage(player, Lang("InvalidSyntax", player.UserIDString, command)); return; }
                            if (args.Length == 3)
                            {
                                var oldvalue = LoadedData[player.UserIDString].filter;
                                var newvalue = filterValidation(args[2]);
                                LoadedData[player.UserIDString].filter = newvalue;
                                SendMessage(player, Lang("SettingUpdate", player.UserIDString, "Default" , args[1], oldvalue, newvalue));
                            }
                            break;
                        default:
                            SendMessage(player, Lang("InvalidSyntax", player.UserIDString, command));
                            break;
                    }
                    break;

                default:
                    SendMessage(player, Lang("InvalidSyntax", player.UserIDString, command));
                    break;
            }
        }
        #endregion

        #region Plugin Methods
        void ToggleRadar(BasePlayer player, string filter = "")
        {
            if (IsRadar(player.UserIDString))
            {
                if (ActiveRadars.Contains(player.UserIDString)) ActiveRadars.Remove(player.UserIDString);
                GameObject.Destroy(player.GetComponent<Radar>());
                SendMessage(player, Lang("RadarOff", player.UserIDString));
                return;
            }

            if (filter == "") { filter = (LoadedData.ContainsKey(player.UserIDString)) ? LoadedData[player.UserIDString].filter : defaultFilter; }
            var repeat = SelectPlayerInvoke(player.UserIDString, filter);
            LoadNameList();

            if (!ActiveRadars.Contains(player.UserIDString)) ActiveRadars.Add(player.UserIDString);
            Radar whrd = player.gameObject.AddComponent<Radar>();

            whrd.CancelInvoke();
            whrd.InvokeRepeating("radar", 1f, repeat);
            whrd.RefreshTime = repeat;
            whrd.filter = filter;
            whrd.setdistance = SelectPlayerDistance(player.UserIDString, filter);
            whrd.PlayerNameList = NameList;
            whrd.ExtDetails = ShowExtData;
            whrd.players = Tplayer;
            whrd.sleepers = Tsleeper;
            whrd.storages = Tstorage;
            whrd.npcs = Tnpc;
            whrd.toolcupboards = Ttoolcupboard;
            whrd.arrows = radararrows;
            whrd.playerbox = radarboxs;
            SendMessage(player, Lang("RadarOn", player.UserIDString, filter.ToUpper(), repeat.ToString(), whrd.setdistance.ToString()));
        }
        #endregion

        #region Data Storage
        // DataSystem
        class PlSettings
        {
            public string filter;
            public float playerinvoke;
            public float sleeperinvoke;
            public float storageinvoke;
            public float toolcupboardinvoke;
            public float npcinvoke;
            public float allinvoke;

            public float playerdistance;
            public float sleeperdistance;
            public float storagedistance;
            public float toolcupboarddistance;
            public float npcdistance;
            public float alldistance;
        }
        StoredData storedData;
        class StoredData { public Dictionary<string, PlSettings> SavedData = new Dictionary<string, PlSettings>(); }
        void LoadSavedData()
        {
            try
            {
                storedData = Interface.Oxide.DataFileSystem.ReadObject<StoredData>(Name);
                LoadedData = storedData.SavedData;
            }
            catch
            {
                Puts("Failed to load data, creating new file");
                storedData = new StoredData();
            }
        }
        void SaveLoadedData()
        {
            storedData.SavedData = LoadedData;
            Interface.Oxide.DataFileSystem.WriteObject(Name, storedData);
        }

        void CreatePlayerData(string id)
        {
            if (!LoadedData.ContainsKey(id))
            {
                LoadedData.Add(id, new PlSettings
                {
                    filter = defaultFilter,
                    alldistance = defaultAllDistance,
                    allinvoke = defaultAllInvoke,
                    npcdistance = defaultNPCMaxDistance,
                    npcinvoke = defaultPlayerInvoke,
                    playerdistance = defaultPlayerMaxDistance,
                    playerinvoke = defaultPlayerInvoke,
                    sleeperdistance = defaultSleeperMaxDistance,
                    sleeperinvoke = defaultSleeperInvoke,
                    storagedistance = defaultStorageMaxDistance,
                    storageinvoke = defaultstorageInvoke,
                    toolcupboarddistance = defaultToolCupboardMaxDistance,
                    toolcupboardinvoke = defaultToolCupboardInvoke
                });
                SendMessage(rust.FindPlayer(id), "<color=yellow>Player Data File Created!</color>");
            }
        }
        #endregion

        #region Clamping & Value Validation

        float distanceClamp(string filter, float distance)
        {
            if (filter == "player") return (distance <= limitPlayerDistanceLow) ? limitPlayerDistanceLow : (distance >= limitPlayerDistanceHigh) ? limitPlayerDistanceHigh : distance;
            if (filter == "sleeper") return (distance <= limitSleeperDistanceLow) ? limitSleeperDistanceLow : (distance >= limitSleeperDistanceHigh) ? limitSleeperDistanceHigh : distance;
            if (filter == "storage") return (distance <= limitstorageDistanceLow) ? limitstorageDistanceLow : (distance >= limitstorageDistanceHigh) ? limitstorageDistanceHigh : distance;
            if (filter == "toolcupboard") return (distance <= limitToolCupboardDistanceLow) ? limitToolCupboardDistanceLow : (distance >= limitToolCupboardDistanceHigh) ? limitToolCupboardDistanceHigh : distance;
            if (filter == "npc") return (distance <= limitNPCDistanceLow) ? limitNPCDistanceLow : (distance >= limitNPCDistanceHigh) ? limitNPCDistanceHigh : distance;
            if (filter == "all") return (distance <= limitAllDistanceLow) ? limitAllDistanceLow : (distance >= limitAllDistanceHigh) ? limitAllDistanceHigh : distance;
            return defaultAllDistance;
        }
        float invokeClamp(string filter, float invokes)
        {
            if (filter == "player") return (invokes < limitPlayerInvokeLow) ? limitPlayerInvokeLow : (invokes > limitPlayerInvokeHigh) ? limitPlayerInvokeHigh : invokes;
            if (filter == "sleeper") return (invokes < limitSleeperInvokeLow) ? limitSleeperInvokeLow : (invokes > limitSleeperInvokeHigh) ? limitSleeperInvokeHigh : invokes;
            if (filter == "storage") return (invokes < limitstorageInvokeLow) ? limitstorageInvokeLow : (invokes > limitstorageInvokeHigh) ? limitstorageInvokeHigh : invokes;
            if (filter == "toolcupboard") return (invokes < limitToolCupboardInvokeLow) ? limitToolCupboardInvokeLow : (invokes > limitToolCupboardInvokeHigh) ? limitToolCupboardInvokeHigh : invokes;
            if (filter == "npc") return (invokes < limitNPCInvokeLow) ? limitNPCInvokeLow : (invokes > limitNPCInvokeHigh) ? limitNPCInvokeHigh : invokes;
            if (filter == "all") return (invokes < limitAllInvokeLow) ? limitAllInvokeLow : (invokes > limitAllInvokeHigh) ? limitAllInvokeHigh : invokes;
            return defaultAllInvoke;
        }

        string filterValidation(string filter)
        {
            if (Tplayer && filter.Contains("pla")) return "player";
            else if (Tsleeper && filter.Contains("sle")) return "sleeper";
            else if (Tstorage && filter.Contains("sto") || filter.Contains("bo") || filter.Contains("con")) return "storage";
            else if (Ttoolcupboard && filter.Contains("tool") || filter.Contains("cup") || filter.Contains("cab") || filter == "tc" || filter == "auth") return "toolcupboard";
            else if (Tnpc && filter == "npc" || filter.Contains("ani")) return "npc";
            else if (Tall && filter.Contains("al")) return "all";
            else return string.Empty;
        }

        string settingValidation(string arg) { return (arg.Contains("dis")) ? "distance" : (arg.Contains("inv")) ? "invoke" : arg; }

        float SelectPlayerInvoke(string id, string filter)
        {
            if (filter == "player") return (HasPlayerData(id)) ? invokeClamp(filter, LoadedData[id].playerinvoke) : invokeClamp(filter, defaultPlayerInvoke);
            if (filter == "sleeper") return (HasPlayerData(id)) ? invokeClamp(filter, LoadedData[id].sleeperinvoke) : invokeClamp(filter, defaultSleeperInvoke);
            if (filter == "storage") return (HasPlayerData(id)) ? invokeClamp(filter, LoadedData[id].storageinvoke) : invokeClamp(filter, defaultstorageInvoke);
            if (filter == "toolcupboard") return (HasPlayerData(id)) ? invokeClamp(filter, LoadedData[id].toolcupboardinvoke) : invokeClamp(filter, defaultToolCupboardInvoke);
            if (filter == "npc") return (HasPlayerData(id)) ? invokeClamp(filter, LoadedData[id].npcinvoke) : invokeClamp(filter, defaultNPCInvoke);
            if (filter == "all") return (HasPlayerData(id)) ? invokeClamp(filter, LoadedData[id].allinvoke) : invokeClamp(filter, defaultAllInvoke);
            return defaultAllInvoke;
        }

        float SelectPlayerDistance(string id, string filter)
        {
            if (filter == "player") return (HasPlayerData(id)) ? distanceClamp(filter, LoadedData[id].playerdistance) : distanceClamp(filter, defaultPlayerMaxDistance);
            if (filter == "sleeper") return (HasPlayerData(id)) ? distanceClamp(filter, LoadedData[id].sleeperdistance) : distanceClamp(filter, defaultSleeperMaxDistance);
            if (filter == "storage") return (HasPlayerData(id)) ? distanceClamp(filter, LoadedData[id].storagedistance) : distanceClamp(filter, defaultStorageMaxDistance);
            if (filter == "toolcupboard") return (HasPlayerData(id)) ? distanceClamp(filter, LoadedData[id].toolcupboarddistance) : distanceClamp(filter, defaultToolCupboardMaxDistance);
            if (filter == "npc") return (HasPlayerData(id)) ? distanceClamp(filter, LoadedData[id].npcdistance) : distanceClamp(filter, defaultNPCMaxDistance);
            if (filter == "all") return (HasPlayerData(id)) ? distanceClamp(filter, LoadedData[id].alldistance) : distanceClamp(filter, defaultAllDistance);
            return defaultAllDistance;
        }

        void LoadNameList()
        {
            foreach (var pl in covalence.Players.All)
            {
                if (!NameList.ContainsKey(pl.Id)) NameList.Add(pl.Id, pl.Name);
            }
        }

        void LoadFilterList()
        {
            FilterList.Clear();
            if (Tplayer) FilterList.Add("player");
            if (Tsleeper) FilterList.Add("sleeper");
            if (Tstorage) FilterList.Add("storage");
            if (Ttoolcupboard) FilterList.Add("toolcupboard");
            if (Tnpc) FilterList.Add("npc");
            if (Tall) FilterList.Add("all");
        }
        #endregion

        #region Helper
        void SendMessage(BasePlayer player, string message) => rust.SendChatMessage(player, $"<color=grey>[<color=teal>{ChatPrefix}</color>]</color>","<color=grey>" + message + "</color>", ChatIcon);
        string Lang(string key, string id = null, params object[] args) => string.Format(lang.GetMessage(key, this, id), args);
        bool IsRadar(string id) => ActiveRadars.Contains(id);
        bool Allowed(BasePlayer player) => permission.UserHasGroup(player.UserIDString, "admin") || permission.UserHasPermission(player.UserIDString, "adminradar." + permAllowed);
        private bool HasPlayerData(string id) => LoadedData.ContainsKey(id);

        string ListToString<T>(List<T> list, int first = 0, string seperator = ", ") => string.Join(seperator, (from val in list select val.ToString()).Skip(first).ToArray());
        void SetConfig(params object[] args) { List<string> stringArgs = (from arg in args select arg.ToString()).ToList(); stringArgs.RemoveAt(args.Length - 1); if (Config.Get(stringArgs.ToArray()) == null) Config.Set(args); }
        T GetConfig<T>(T defaultVal, params object[] args) { List<string> stringArgs = (from arg in args select arg.ToString()).ToList(); if (Config.Get(stringArgs.ToArray()) == null) { PrintError($"The plugin failed to read something from the config: {ListToString(stringArgs, 0, "/")}{Environment.NewLine}Please reload the plugin and see if this message is still showing. If so, please post this into the support thread of this plugin."); return defaultVal; } return (T)System.Convert.ChangeType(Config.Get(stringArgs.ToArray()), typeof(T)); }
        bool RadarList(out string list)
        {
            string namelist = string.Empty;
            foreach (var key in ActiveRadars)
            {
                namelist += $"<color=red>{rust.FindPlayer(key).displayName}</color>\n";
            }
            list = namelist;
            return ActiveRadars.Count != 0;
        }

        private void SendHelpText(BasePlayer player)
        {
            string message =
                "<size=13>---- Radar Commands ----\n" +
                "<color=red>/radar</color> <color=green>(filter)</color> - <color=yellow>activates radar with default settings or with optional filter</color>\n" +
                "<color=red>/radar list</color> - <color=yellow>Shows a list of players using Radar</color>\n" +
                "<color=red>/radar give</color> <color=green>[target] (filter)</color> - <color=yellow>Give a player radar with filter</color>\n" +
                "<color=red>/radar filterlist</color> - <color=yellow>shows available filters</color>\n" +
                "<color=red>/radar setting</color> <color=green>[filter] [invoke/distance] [value]</color> - <color=yellow>Set custom default filter setting for self</color>\n" +
                "<color=red>/radar setting filter</color> <color=green>[NewDefaultFilter]</color> - <color=yellow>Set a new default filter for self</color></size>";

            if (Allowed(player))
            {
                player.ChatMessage(message);
            }

        }
        #endregion

    }
}
