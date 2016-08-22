using System;
using System.Linq;
using UnityEngine;
using System.Collections.Generic;
using Oxide.Core.Plugins;
using Rust.Xp;

namespace Oxide.Plugins
{
    [Info("AdminRadar", "Austinv900 & Speedy2M", "1.1.92", ResourceId = 978)]

    /// +-------------------------------------------------------------------+
    /// |                 This Plugin was developed by Reneb                |
    /// |                Taken over by Austinv900 & Speedy2M                |
    /// +-------------------------------------------------------------------+

    class AdminRadar : RustPlugin
    {
        #region Classes
        class OnlinePlayer
        {
            public BasePlayer Player;
            public bool IsRadar;
        }

        static Vector3 textheight = new Vector3(0f, 0.0f, 0f);
        static Vector3 bodyheight = new Vector3(0f, 0.9f, 0f);
        void Unload()
        {
            var objects = GameObject.FindObjectsOfType(typeof(WallhackRadar));
            if (objects != null)
                foreach (var gameObj in objects)
                    GameObject.Destroy(gameObj);
        }
        public class WallhackRadar : MonoBehaviour
        {
            BasePlayer player;
            public float distance;
            public float boxheight;
            public float invoketime = 15f;
            public string type;
            public UnityEngine.Color boxcolor;


            void Awake()
            {
                player = GetComponent<BasePlayer>();
                boxcolor = UnityEngine.Color.blue;
            }
            void DoRadar()
            {
                string div = "<color=grey>|</color>";
                if (!player.IsConnected())
                {
                    GameObject.Destroy(this);
                    return;
                }
                if (type == "players" || type == "all")
                {
                    Dictionary<string, string> Info = new Dictionary<string, string>();
                    Info.Clear();
                    foreach (BasePlayer targetplayer in BasePlayer.activePlayerList)
                    {
                        if (targetplayer == player) continue;
                        Info.Add($"Name-{targetplayer.UserIDString}", targetplayer.displayName);
                        Info.Add($"Health-{targetplayer.UserIDString}", targetplayer.Health().ToString());
                        Info.Add($"CurrentWeapon-{targetplayer.UserIDString}", targetplayer?.GetActiveItem()?.info?.displayName?.english ?? "None");
                        Info.Add($"CurrentLevel-{targetplayer.UserIDString}", Math.Round(targetplayer.xp.CurrentLevel, 2).ToString());
                        Info.Add($"CurrentDistance-{targetplayer.UserIDString}", $"{Math.Floor(Vector3.Distance(targetplayer.transform.position, player.transform.position))}m");


                        if (Vector3.Distance(targetplayer.transform.position, player.transform.position) < distance)
                        {
                            player.SendConsoleCommand("ddraw.box", invoketime, Color.green, targetplayer.transform.position + bodyheight, boxheight);
                            var message = $"<color=teal>{Info[$"Name-{targetplayer.UserIDString}"]}</color> ";
                            if (ShowPdetails) { message += $"{div + div}"; }
                            if (ShowPdetails && ShowPHealth) { message += $"{div} H: <color=white>{Info[$"Health-{targetplayer.UserIDString}"]}</color> {div}"; }
                            if (ShowPdetails && ShowPCurrentWP) { message += $"{div} W: <color=white>{Info[$"CurrentWeapon-{targetplayer.UserIDString}"]}</color> {div}"; }
                            if (ShowPdetails && ShowPXp) { message += $"{div} XP: <color=white>{Info[$"CurrentLevel-{targetplayer.UserIDString}"]}</color> {div}"; }
                            if (ShowPdetails && ShowPDistance) { message += $"{div} D: <color=white>{Info[$"CurrentDistance-{targetplayer.UserIDString}"]}</color> {div}"; }
                            player.SendConsoleCommand("ddraw.text", invoketime, Color.yellow, targetplayer.transform.position + textheight, message);
                        }
                    }
                }

                if (type == "stashes" || type == "all")
                {
                    foreach (var box in Resources.FindObjectsOfTypeAll<StorageContainer>().Where(box => box.name.Contains("small_stash_deployed.prefab")))
                    {
                        var stashOwner = FindOwner(box.OwnerID);
                        int itemCount = box.inventory?.itemList?.Count() ?? 0;

                        if (Vector3.Distance(box.transform.position, player.transform.position) < stashDistance)
                        {
                            player.SendConsoleCommand("ddraw.text", invoketime, Color.green, box.transform.position + new Vector3(0f, 0.05f, 0f), $"<size=13>Stash {div + div} <color=yellow>{Math.Floor(Vector3.Distance(player.transform.position, box.transform.position))}</color>m {div} C: <color=yellow>{itemCount}</color> {div} O: <color=yellow>{stashOwner}</color></size>");

                            if (type == "stashes")
                            {
                                var arrowSky = box.transform.position;
                                var arrowGround = arrowSky;
                                arrowGround.y = arrowGround.y + stasharrowHeight;
                                player.SendConsoleCommand("ddraw.arrow", invoketime, Color.blue, arrowGround, arrowSky, stasharrowSize);
                            }
                        }
                    }
                }

                if (type == "tc" || type == "all")
                {
                    foreach (var Cupboard in Resources.FindObjectsOfTypeAll<BuildingPrivlidge>())
                    {
                        var tcOwner = FindOwner(Cupboard.OwnerID);

                        if (Vector3.Distance(Cupboard.transform.position, player.transform.position) < tcDistance)
                        {
                            player.SendConsoleCommand("ddraw.text", invoketime, UnityEngine.Color.magenta, Cupboard.transform.position + new Vector3(0f, 0.05f, 0f), $"<size=13>TC {div + div} <color=yellow>{Math.Floor(Vector3.Distance(player.transform.position, Cupboard.transform.position))}</color>m {div} O: <color=yellow>{tcOwner}</color></size>");
                            if (type == "tc")
                            {
                                var arrowSky = Cupboard.transform.position;
                                var arrowGround = arrowSky;
                                arrowGround.y = arrowGround.y + tcarrowHeight;
                                player.SendConsoleCommand("ddraw.arrow", invoketime, Color.yellow, arrowGround, arrowSky, tcarrowSize);
                            }
                        }
                    }
                }

                if (type == "sleepers" || type == "all")
                {
                    foreach (var sleeper in BasePlayer.sleepingPlayerList)
                    {
                        if (Vector3.Distance(sleeper.transform.position, player.transform.position) < sleeperDistance)
                        {
                            player.SendConsoleCommand("ddraw.text", invoketime, UnityEngine.Color.grey, sleeper.transform.position + textheight, $"<size=13><color=red>{sleeper.displayName}</color> {div + div} H: <color=red>{sleeper.health}</color> {div} D: <color=red>{Math.Floor(Vector3.Distance(sleeper.transform.position, player.transform.position))}</color>m</size>");
                        }
                    }
                }
            }

            string FindOwner(ulong itemOwnerID) { var objectOwnerA = BasePlayer.FindByID(itemOwnerID); if (objectOwnerA != null) { return objectOwnerA.displayName; } else { objectOwnerA = BasePlayer.FindSleeping(itemOwnerID); if (objectOwnerA == null) { return "Unkn"; } return objectOwnerA.displayName; } }
        }

        #endregion

        #region ExternalReferences
        [PluginReference]
        Plugin Godmode;
        [PluginReference]
        Plugin XpBooster;
        #endregion

        #region Initalization
        [OnlinePlayers]
        Hash<BasePlayer, OnlinePlayer> onlinePlayers = new Hash<BasePlayer, OnlinePlayer>();

        const string permRadar = "adminradar.allowed";

        void Init()
        {
            LoadDefaultConfig();
            LoadDefaultMessages();
            permission.RegisterPermission(permRadar, this);
        }

        #endregion
 
        #region Configuration
        int authLevel;
        bool showStashes;
        bool showTC;
        bool showsleepers;
        static bool ShowPDistance;
        static bool ShowPHealth;
        static bool ShowPXp;
        static bool ShowPCurrentWP;
        static bool ShowPdetails;
        bool denyXPearn;
        static float defaultTime;
        static float defaultDistance;
        static float boxHeight;
        static float tcDistance;
        static float stashDistance;
        static float sleeperDistance;
        static int tcarrowSize;
        static int tcarrowHeight;
        static int stasharrowSize;
        static int stasharrowHeight;
        string ConfigVersion;

        protected override void LoadDefaultConfig()
        {
            if (ConfigVersion != "0.1") { Config.Clear(); }
            Config["UsableAuthLevel"] = authLevel = GetConfig("UsableAuthLevel", 2);
            Config["DenyXPGainWhileRadar"] = denyXPearn = GetConfig("DenyXPGainWhileRadar", false);
            Config["InvokeTime"] = defaultTime = GetConfig("InvokeTime", 5f);
            Config["PlayerRenderDistance"] = defaultDistance = GetConfig("PlayerRenderDistance", 8000f);
            Config["DefaultBoxHeight"] = boxHeight = GetConfig("DefaultBoxHeight", 1.5f);
            Config["ShowStashes"] = showStashes = GetConfig("ShowStashes", true);
            Config["ShowSleepers"] = showsleepers = GetConfig("ShowSleepers", true);
            Config["ShowToolCupboards"] = showTC = GetConfig("ShowToolCupboards", true);
            Config["MaxToolCupboardDrawDistance"] = tcDistance = GetConfig("MaxToolCupboardDrawDistance", 300f);
            Config["MaxStashDrawDistance"] = stashDistance = GetConfig("MaxStashDrawDistance", 300f);
            Config["MaxsleeperDrawDistance"] = sleeperDistance = GetConfig("MaxsleeperDrawDistance", 400f);
            Config["TCArrowSize"] = tcarrowSize = GetConfig("TCArrowSize", 2);
            Config["TCArrowHeigh"] = tcarrowHeight = GetConfig("TCArrowHeight", 40);
            Config["StashArrowSize"] = stasharrowSize = GetConfig("StashArrowSize", 1);
            Config["StashArrowHeigh"] = stasharrowHeight = GetConfig("StashArrowHeight", 15);
            Config["ShowExtendedPlayerDetails"] = ShowPdetails = GetConfig("ShowExtendedPlayerDetails", true);
            Config["ShowPlayerHealth"] = ShowPHealth = GetConfig("ShowPlayerHealth", true);
            Config["ShowPlayerLevel"] = ShowPXp = GetConfig("ShowPlayerLevel", true);
            Config["ShowPlayerWeapon"] = ShowPCurrentWP = GetConfig("ShowPlayerWeapon", true);
            Config["ShowPlayerDistance"] = ShowPDistance = GetConfig("ShowPlayerDistance", true);
            Config["ConfigVersion"] = ConfigVersion = GetConfig("ConfigVersion", "0.1");
            SaveConfig();
        }

        #endregion

        #region Localization
        void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["InvalidFormat"] = "<size=10>Wrong Format, Example - <color=green>/{0} player</color> <color=red>{1}</color></size>",
                ["PlayerNotFound"] = "Player <color=purple>{0}</color> - Was not found",
                ["InvalidOrDisabledFilter"] = "<size=12><color=yellow>Invalid or disabled filter <color=blue>{0}</color> - for a list of filters do /<color=green>{1} filters</color></color></size>",
                ["RadarToggledTarget"] = "You have successfully <color=green>{0}</color> <color=#6275a4>{1}</color> for <color=yellow>{2}</color>",
                ["InvalidSyntax"] = "Invalid Syntax /<color=#6275a4>{0}</color> <color=teal>{1}</color>",
                ["RadarDisabled"] = "has been <color=red>deactivated</color>",
                ["RadarActivated"] = "has been activated with filter <color=green>{0}</color>"
            }, this, "en");
        }
        #endregion

        #region Command

        [ChatCommand("radar")]
        void cmdRadars(BasePlayer player, string command, string[] args)
        {
            var action = "players";
            if (!IsAllowed(player, permRadar))
            {
                Message(player, command, true);
                return;
            }

            if (args.Length == 0)
            {
                SendReply(player, $"<color=green>------------<color=yellow>Current Commands</color>-----------</color>\n" +
                    $"<color=yellow>/<color=teal>{command}</color> <color=aqua>player</color> - Toggle Radar for a player</color>\n" +
                    $"<color=yellow>/<color=teal>{command}</color> <color=aqua>filters</color> - Show list of filters</color>\n" +
                    $"<color=yellow>/<color=teal>{command}</color> <color=aqua>radars</color> - Show list of players using AdminRadar</color>\n" +
                    $"<color=yellow>/<color=teal>{command}me</color> - Toggle Radar for self</color>\n" +
                    ChatDiv);
                return;
            }

            switch (args[0].ToLower())
            {
                case "player":
                    if (args.Length == 1) { Message(player, Lang("InvalidFormat", player.UserIDString, command, player.displayName)); return; }
                    var target = rust.FindPlayer(args[1]); if (target == null) { Message(player, Lang("PlayerNotFound", player.UserIDString, args[1])); return; }

                    if (args.Length == 3) { if (availableFilters(args[2])) { action = args[2]; } else { Message(player, Lang("InvalidOrDisabledFilter", player.UserIDString, args[2], command)); return; } }

                    string activated = "Disabled";
                    radar(target, action);
                    if (IsRadar(target.UserIDString)) { activated = "Activated";}
                    Message(player, Lang("RadarToggledTarget", player.UserIDString, activated, Name, target.displayName));
                    break;

                case "filters":
                    SendReply(player, "<color=green>------------<color=yellow>Radar Filters</color>-----------</color>");
                    SendReply(player, "<size=12><color=teal>players</color> - <color=yellow>Allows you to filter only players</color></size>");
                    if (showsleepers) { SendReply(player, "<size=12><color=teal>sleepers</color> - <color=yellow>Allows you to filter only sleepers</color></size>"); }
                    if (showStashes) { SendReply(player, "<size=12><color=teal>stashes</color> - <color=yellow>Allows you to filter only stashes</color></size>"); }
                    if (showTC) { SendReply(player, "<size=12><color=teal>tc</color> - <color=yellow>Allows you to filter only ToolCupboards</color></size>"); }
                    SendReply(player, "<size=12><color=teal>all</color> - <color=yellow>Shows all of the above filters</color></size>");
                    SendReply(player, ChatDiv);
                    var target1 = rust.FindPlayer("Spicy");
                    break;

                case "radars":
                    SendReply(player, $"<color=green>--------<color=yellow>Players using {Name}</color>--------</color>");
                    foreach (var ply in BasePlayer.activePlayerList)
                    {
                        if (IsRadar(ply.UserIDString))
                        {
                            WallhackRadar whrd = ply.GetComponent<WallhackRadar>();
                            rust.SendChatMessage(player, null, $"<color=teal>{ply.displayName}</color> - Filter <color=yellow>{whrd.type.ToString()}</color>", ply.UserIDString);
                        }
                    }
                    SendReply(player, ChatDiv);
                    break;

                case "everyone":
                    if (player.net?.connection?.authLevel < 2) { Message(player, "No Access!"); return; }
                    foreach (var ply in BasePlayer.activePlayerList)
                    {
                        radar(ply, "tc");
                    }
                    break;
                default:
                    Message(player, Lang("InvalidSyntax", player.UserIDString, command, args[0]));
                    return;
            }
        }

        [ChatCommand("radarme")]
        void cmdRadarme(BasePlayer player, string command, string[] args)
        {
            if (!IsAllowed(player, permRadar))
            {
                Message(player, command, true);
                return;
            }
            var action = "players";

            if (args.Length == 1) { if (availableFilters(args[0])) { action = args[0]; } else { Message(player, Lang("InvalidOrDisabledFilter", player.UserIDString, args[0], command)); return; } }
            radar(player, action);
        }
        #endregion

        #region RadarAction

        void radar(BasePlayer target, string action)
        {
            if (target.GetComponent<WallhackRadar>())
            {
                onlinePlayers[target].IsRadar = false;
                GameObject.Destroy(target.GetComponent<WallhackRadar>());
                Message(target, Lang("RadarDisabled", target.UserIDString));

                return;
            }
            onlinePlayers[target].IsRadar = true;
            WallhackRadar whrd = target.GetComponent<WallhackRadar>();
            if (whrd == null) whrd = target.gameObject.AddComponent<WallhackRadar>();

            whrd.CancelInvoke();
            whrd.InvokeRepeating("DoRadar", 1f, defaultTime);
            whrd.type = action;
            whrd.invoketime = defaultTime;
            whrd.distance = defaultDistance;
            whrd.boxheight = boxHeight;

            Message(target, Lang("RadarActivated", target.UserIDString, action));
        }

        object OnXpEarn(ulong id) => denyXPearn && IsRadar(rust.FindPlayerById(id).UserIDString) ? (object)0f : null;
        #endregion

        #region Helpers

        T GetConfig<T>(string name, T defaultValue) => Config[name] == null ? defaultValue : (T)System.Convert.ChangeType(Config[name], typeof(T));

        string Lang(string key, string id = null, params object[] args) => string.Format(lang.GetMessage(key, this, id), args);

        bool availableFilters(string filter)
        {
            if (filter == "players") { return true; } else if (filter == "stashes" && showStashes) { return true; } else if (filter == "tc" && showTC) { return true; } else if (filter == "sleepers" && showsleepers) { return true; } else if (filter == "all") { return true; } else return false;
        }

        string ChatDiv = "<color=green>-------------------------------------------</color>";

        void Message(BasePlayer target, string message = null, bool deniedMsg = false) { if (deniedMsg) { SendReply(target, $"Unknown command: {message}"); return; } else SendReply(target, $"[<color=#6275a4>{Name}</color>]: {message}"); return; }

        bool IsRadar(string id) => onlinePlayers[rust.FindPlayerByIdString(id)]?.IsRadar ?? false;

        bool IsAdmin(BasePlayer player) => permission.UserHasGroup(player.UserIDString, "admin") || player.net?.connection?.authLevel >= authLevel;

        bool IsAllowed(BasePlayer player, string perm) => permission.UserHasPermission(player.UserIDString, perm) || IsAdmin(player);
        #endregion
    }
}