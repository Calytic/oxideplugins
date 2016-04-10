using System;
using System.Collections.Generic;
using System.Linq;
using Rust;
using Oxide.Core.Plugins;
namespace Oxide.Plugins
{
    [Info("MagicVariables", "Norn", 0.1, ResourceId = 1419)]
    [Description("Simple static variable system.")]
    public class MagicVariables : RustPlugin
    {
        public Int32 UnixTimeStampUTC()
        {
            Int32 unixTimeStamp;
            DateTime currentTime = DateTime.Now;
            DateTime zuluTime = currentTime.ToUniversalTime();
            DateTime unixEpoch = new DateTime(1970, 1, 1);
            unixTimeStamp = (Int32)(zuluTime.Subtract(unixEpoch)).TotalSeconds;
            return unixTimeStamp;
        }
        static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        static readonly double MaxUnixSeconds = (DateTime.MaxValue - UnixEpoch).TotalSeconds;

        public static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
        {
            return unixTimeStamp > MaxUnixSeconds
               ? UnixEpoch.AddMilliseconds(unixTimeStamp)
               : UnixEpoch.AddSeconds(unixTimeStamp);
        }
        public class StaticVariables
        {
            public int iResourceID;

            public Dictionary<string, object> Variables = new Dictionary<string, object>();
            public Hash<ulong, Dictionary<string, object>> UserVariables = new Hash<ulong, Dictionary<string, object>>();
            public Dictionary<ulong, PlayersInit> Users = new Dictionary<ulong, PlayersInit>();

            public StaticVariables()
            {
            }
        }
        public class PlayersInit
        {
            public ulong uUserID;
            public string tDisplayName;
            public PlayersInit()
            {
            }
        }
        public class PluginInfo
        {
            public Dictionary<Plugin, StaticVariables> Plugins = new Dictionary<Plugin, StaticVariables>();
            public PluginInfo()
            {
            }
        }
        PluginInfo Data = new PluginInfo();
        private bool PluginExists(Plugin plugin)
        {
            if (Data.Plugins.ContainsKey(plugin))
            {
                return true;
            }
            return false;
        }
        private bool SetStaticVariable(Plugin plugin, string variable, string value, bool debug = false)
        {
            if (PluginExists(plugin))
            {
                if (!Data.Plugins[plugin].Variables.ContainsKey(variable))
                {
                    Data.Plugins[plugin].Variables.Add(variable, value);
                    if (debug) Puts(plugin.Title + " : " + variable + " = " + value + " : Initiation.");
                    return true;
                }
                else
                {
                    Data.Plugins[plugin].Variables[variable] = value;
                    if (debug) Puts(plugin.Title + " : " + variable + " = " + value + " : Updating.");
                    return true;
                }
            }
            return false;
        }
        private string GetStaticVariable(Plugin plugin, string variable, bool debug = false)
        {
            if (PluginExists(plugin))
            {
                if (Data.Plugins[plugin].Variables.ContainsKey(variable))
                {
                    if (debug) Puts(plugin.Title + " : " + Data.Plugins[plugin].Variables[variable].ToString() + " : Data Returned.");
                    return Data.Plugins[plugin].Variables[variable].ToString();
                }
            }
            return "0";
        }
        private bool RemoveStaticVariable(Plugin plugin, string variable, bool debug = false)
        {
            if (PluginExists(plugin))
            {
                if (Data.Plugins[plugin].Variables.ContainsKey(variable))
                {
                    Data.Plugins[plugin].Variables.Remove(variable);
                    return true;
                }
            }
            return false;
        }
        private bool DestroyPlugin(Plugin plugin, bool debug = false)
        {
            try
            {
                if (PluginExists(plugin))
                {
                    Data.Plugins.Remove(plugin);
                    if (debug) Puts("Removing " + plugin.Title + " from the plugin list.");
                    return true;
                }
            }
            catch
            {
                if (debug) Puts("DEBUG: Failed To Call Hook DestroyPlugin(" + plugin.Title.ToString() + ");");
            }
            return false;
        }
        private bool InitPlugin(Plugin plugin, bool debug = false)
        {
            try
            {
                if (!PluginExists(plugin))
                {
                    StaticVariables d = new StaticVariables();
                    d.iResourceID = plugin.ResourceId;
                    Data.Plugins.Add(plugin, d);
                    SetStaticVariable(plugin, "INIT_TIMESTAMP", UnixTimeStampUTC().ToString());
                    if (debug) Puts("[" + plugin.Title + "] added " + plugin.Title + " [Resource ID: " + Data.Plugins[plugin].iResourceID.ToString() + "].");
                    return true;
                }
            }
            catch
            {
                if (debug) Puts("DEBUG: Failed To Call Hook InitPlugin(" + plugin.Title.ToString() + ");");
            }
            return false;
        }
        void OnServerInitialized()
        {
            int config_protocol = Convert.ToInt32(Config["Protocol"]);
            if (config_protocol != Protocol.network)
            {
                Config["Protocol"] = Protocol.network;
                SaveConfig();
            }
        }
        private bool RemoveStaticPlayerVariable(Plugin plugin, BasePlayer player, string variable, bool debug = false)
        {
            if (!PlayerExists(plugin, player)) { return false; }
            if (PlayerHasVariables(plugin, player) == 0) { return false; }
            if (Data.Plugins[plugin].UserVariables[player.userID].ContainsKey(variable))
            {
                if (debug) Puts("[" + plugin.Title + "] Removing static variable " + variable + ":" + Data.Plugins[plugin].UserVariables[player.userID][variable] + ".");
                Data.Plugins[plugin].UserVariables[player.userID].Remove(variable);
                return true;
            }
            return false;
        }
        private string GetStaticPlayerVariable(Plugin plugin, BasePlayer player, string variable, bool debug = false)
        {
            string default_return = "0";
            if (!PlayerExists(plugin, player)) { return default_return; }
            if (PlayerHasVariables(plugin, player) == 0) { return default_return; }
            if (Data.Plugins[plugin].UserVariables[player.userID].ContainsKey(variable))
            {
                default_return = Data.Plugins[plugin].UserVariables[player.userID][variable].ToString();
            }
            return default_return;
        }
        private void SetStaticPlayerVariable(Plugin plugin, BasePlayer player, string variable, string value, bool debug = false)
        {
            ulong steamid = player.userID;
            if (!PlayerExists(plugin, player)) { InitPlayer(plugin, player); return; }
            if (Data.Plugins[plugin].UserVariables.ContainsKey(player.userID))
            {
                if (Data.Plugins[plugin].UserVariables[player.userID].ContainsKey(variable))
                {
                    Data.Plugins[plugin].UserVariables[player.userID][variable] = value;
                }
                else
                {
                    Data.Plugins[plugin].UserVariables[player.userID].Add(variable, value);
                }
            }
            else
            {
                Dictionary<string, object> zdata = new Dictionary<string, object>();
                zdata.Add(variable, value);
                Data.Plugins[plugin].UserVariables.Add(player.userID, zdata);
            }
            if (debug) Puts(player.displayName + " : " + player.userID.ToString() + " : " + variable + " : " + Data.Plugins[plugin].UserVariables[player.userID][variable].ToString());
        }
        private int PlayerHasVariables(Plugin plugin, BasePlayer player)
        {
            if (PluginExists(plugin) && PlayerExists(plugin, player))
            {
                if (!Data.Plugins[plugin].UserVariables.ContainsKey(player.userID)) { return 0; }
                return Data.Plugins[plugin].UserVariables[player.userID].Count();
            }
            return 0;
        }
        private bool InitPlayer(Plugin plugin, BasePlayer player, bool debug = false)
        {
            if (!PlayerExists(plugin, player) && PluginExists(plugin))
            {
                PlayersInit data = new PlayersInit();
                data.tDisplayName = player.displayName;
                data.uUserID = player.userID;
                Data.Plugins[plugin].Users.Add(player.userID, data);
                if (debug) Puts("[" + plugin.Title + "] added " + Data.Plugins[plugin].Users[player.userID].tDisplayName + " (" + Data.Plugins[plugin].Users[player.userID].uUserID.ToString() + ") to the user list.");
                return true;
            }
            return false;
        }
        private bool RemovePlayer(Plugin plugin, BasePlayer player, bool debug = false)
        {
            if (PluginExists(plugin))
            {
                if (Data.Plugins[plugin].Users.ContainsKey(player.userID))
                {
                    if (debug) Puts("[" + plugin.Title + "] Removing " + Data.Plugins[plugin].Users[player.userID].tDisplayName + " [ " + player.userID + " ] ");
                    Data.Plugins[plugin].Users.Remove(player.userID);
                    return true;
                }
            }
            return false;
        }
        private bool PlayerExists(Plugin plugin, BasePlayer player)
        {
            if (PluginExists(plugin))
            {
                if (Data.Plugins[plugin].Users.ContainsKey(player.userID))
                {
                    return true;
                }
            }
            return false;
        }
        protected override void LoadDefaultConfig()
        {
            Puts("No configuration file found, generating...");
            Config.Clear();
            Config["Protocol"] = Protocol.network;
            SaveConfig();
        }
    }
}