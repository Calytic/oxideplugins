using System.Text.RegularExpressions;
using System.Collections.Generic;
using Oxide.Core.Plugins;
using System.Linq;
using System;

namespace Oxide.Plugins
{
    [Info("Updater", "LaserHydra", "2.0.1", ResourceId = 1700)]
    [Description("Notifies you if you have outdated plugins.")]
#if RUST
    class Updater : RustPlugin
#endif
#if HURTWORLD
    class Updater : HurtworldPlugin
#endif
#if REIGNOFKINGS
    class Updater : ReignOfKingsPlugin
#endif
    {
        #region Global Declaration

        Dictionary<Plugin, string> pluginList = new Dictionary<Plugin, string>();

        [PluginReference("EmailAPI")]
        Plugin EmailAPI;

        [PluginReference("PushAPI")]
        Plugin PushAPI;

        #endregion

        #region Plugin General

        ////////////////////////////////////////
        ///     Plugin Related Hooks
        ////////////////////////////////////////

        void Loaded()
        {
            RegisterPerm("use");

            LoadConfig();
            LoadMessages();

            timer.Repeat(GetConfig(60f, "Settings", "Auto Check Interval (in Minutes)") * 60, 0, () => CheckForUpdates());
            CheckForUpdates();
        }

        ////////////////////////////////////////
        ///     Config & Message Loading
        ////////////////////////////////////////

        void LoadConfig()
        {
            SetConfig("Settings", "Auto Check Interval (in Minutes)", 60f);
            SetConfig("Settings", "Use PushAPI", false);
            SetConfig("Settings", "Use EmailAPI", false);
            //SetConfig("Settings", "Log to File", true);
            
            SaveConfig();
        }

        void LoadMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                {"No Permission", "You don't have permission to use this command."},
                {"Outdated Plugin List", "Following plugins are outdated: {plugins}"},
                {"Outdated Plugin Info", "# {title} | Installed: {installed} - Latest: {latest} | {url}"}
            }, this);
        }

        protected override void LoadDefaultConfig() => PrintWarning("Generating new config file...");

    #endregion

        #region Commands

#if RUST
        [ConsoleCommand("updates")]
        void ccmdUpdates(ConsoleSystem.Arg arg)
        {
            if (arg == null)
                return;

            if (arg.connection?.userid != null)
                if (!HasPerm(arg.connection.userid, "use"))
                    return;

            CheckForUpdates();
        }

        [ChatCommand("updates")]
        void cmdUpdates(BasePlayer player)
        {
            if(!HasPerm(player.userID, "use"))
            {
                rust.SendChatMessage(player, GetMsg("No Permission", player.userID));
                return;
            }

            CheckForUpdates();
        }
#endif

#if HURTWORLD
        [ConsoleCommand("updates")]
        void ccmdUpdates(string cmd) => CheckForUpdates();

        [ChatCommand("updates")]
        void cmdUpdates(PlayerSession player)
        {
            if(!HasPerm(player.SteamId, "use"))
            {
                hurt.SendChatMessage(player, GetMsg("No Permission", player.steamId));
                return;
            }

            CheckForUpdates();
        }
#endif

#if ROK
        [ConsoleCommand("updates")]
        void ccmdUpdates(string cmd) => CheckForUpdates();

        [ChatCommand("updates")]
        void cmdUpdates(Player player)
        {
            if(!HasPerm(player.Id, "use"))
            {
                rok.SendChatMessage(player, GetMsg("No Permission", player.Id));
                return;
            }

            CheckForUpdates();
        }
#endif

    #endregion

        #region Subject Related

        void Notify(string message)
        {
            if (GetConfig(false, "Settings", "Use PushAPI") && PushAPI != null)
                PushAPI.Call("PushMessage", "Plugin Update Notification", message);

            if (GetConfig(false, "Settings", "Use EmailAPI") && EmailAPI != null)
                EmailAPI.Call("EmailMessage", "Plugin Update Notification", message);

            PrintWarning(message);
        }

        void CheckForUpdates()
        {
            pluginList.Clear();

            foreach(Plugin plugin in plugins.GetAll())
            {
                if (plugin.ResourceId == 0)
                {
                    pluginList.Add(plugin, plugin.Version.ToString());

                    if (pluginList.Count == plugins.GetAll().Count())
                        NotifyOutdated();

                    continue;
                }

                webrequest.EnqueueGet($"http://oxidemod.org/plugins/{plugin.ResourceId}/", (code, response) => 
                {
                    if (code == 200 && response != null)
                    {
                        string latest = "0.0.0.0";

                        Match version = new Regex(@"<h3>Version (\d{1,7}(\.\d{1,7})+?)<\/h3>").Match(response);
                        if (version.Success)
                            latest = version.Groups[1].ToString();

                        pluginList.Add(plugin, latest);

                        if (pluginList.Count == plugins.GetAll().Count())
                            NotifyOutdated();
                    }
                    else
                    {
                        pluginList.Add(plugin, plugin.Version.ToString());

                        if (pluginList.Count == plugins.GetAll().Count())
                            NotifyOutdated();
                    }

                }, this);
            }
        }

        void NotifyOutdated()
        {
            Dictionary<Plugin, string> outdated = new Dictionary<Plugin, string>();

            foreach (var kvp in pluginList)
                if (IsOutdated(kvp.Key.Version.ToString(), kvp.Value))
                    outdated.Add(kvp.Key, kvp.Value);

            if (outdated.Count != 0)
            {
                string message = Environment.NewLine +
                                    GetMsg("Outdated Plugin List").Replace("{plugins}", Environment.NewLine + ListToString((from kvp in outdated select Environment.NewLine + GetMsg("Outdated Plugin Info").Replace("{title}", kvp.Key.Title).Replace("{installed}", kvp.Key.Version.ToString()).Replace("{latest}", kvp.Value).Replace("{url}", $"http://oxidemod.org/plugins/{kvp.Key.ResourceId}/")).ToList(), 0, Environment.NewLine) +
                                    Environment.NewLine);

                Notify(message);
            }

            pluginList.Clear();
        }

        bool IsOutdated(string installed, string latest)
        {
            char[] chars = "1234567890.".ToCharArray();

            foreach (char Char in installed.ToCharArray())
                if (!chars.Contains(Char))
                    installed = installed.Replace(Char.ToString(), "");

            foreach (char Char in latest.ToCharArray())
                if (!chars.Contains(Char))
                    latest = latest.Replace(Char.ToString(), "");

            int[] installedArray = (from v in installed.Split('.') select Convert.ToInt32(v)).ToArray();
            int[] latestArray = (from v in latest.Split('.') select Convert.ToInt32(v)).ToArray();

            int i = 0;
            foreach(int lst in latestArray)
            {
                int inst = installedArray.Count() - 1 >= i ? installedArray[i] : 0;

                if (lst > inst)
                    return true;
                else if (lst < inst)
                    return false;

                i++;
            }

            return false;
        }

        #endregion
        
        #region General Methods

        ////////////////////////////////////////
        ///     Converting
        ////////////////////////////////////////

        string ListToString<T>(List<T> list, int first, string seperator) => string.Join(seperator, (from item in list select item.ToString()).Skip(first).ToArray());

        ////////////////////////////////////////
        ///     Config Related
        ////////////////////////////////////////

        void SetConfig(params object[] args)
        {
            List<string> stringArgs = (from arg in args select arg.ToString()).ToList();
            stringArgs.RemoveAt(args.Length - 1);

            if (Config.Get(stringArgs.ToArray()) == null) Config.Set(args);
        }

        T GetConfig<T>(T defaultVal, params object[] args)
        {
            List<string> stringArgs = (from arg in args select arg.ToString()).ToList();
            if (Config.Get(stringArgs.ToArray()) == null)
            {
                PrintError($"The plugin failed to read something from the config: {ListToString(stringArgs, 0, "/")}{Environment.NewLine}Please reload the plugin and see if this message is still showing. If so, please post this into the support thread of this plugin.");
                return defaultVal;
            }

            return (T)Convert.ChangeType(Config.Get(stringArgs.ToArray()), typeof(T));
        }

        ////////////////////////////////////////
        ///     Message Related
        ////////////////////////////////////////

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

        #endregion
    }
}
