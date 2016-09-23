using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Oxide.Plugins
{
    [Info("MagicDescription", "Wulf/lukespragg", "1.2.3", ResourceId = 1447)]
    [Description("Adds dynamic information in the server description")]

    class MagicDescription : RustPlugin
    {
        #region Initialization

        readonly Regex varRegex = new Regex(@"\{([^\}]+)\}");
        List<object> exclusions;
        bool listPlugins;
        bool serverInitialized;
        int updateInterval;
        string description;

        protected override void LoadDefaultConfig()
        {
            Config["Description"] = description = GetConfig("Description", "Powered by {oxide.version}\n\n{version}\n\n{server.pve}");
            Config["ListPlugins"] = listPlugins = GetConfig("ListPlugins", true);
            Config["PluginExclusions"] = exclusions = GetConfig("PluginExclusions", new List<object> { "PrivateStuff", "OtherName" });
            Config["UpdateInterval"] = updateInterval = GetConfig("UpdateInterval", 300);
            SaveConfig();
        }

        void OnServerInitialized()
        {
            LoadDefaultConfig();
            serverInitialized = true;
            UpdateDescription();
            timer.Every(updateInterval, () => UpdateDescription());
        }

        #endregion

        #region Description Updating

        void OnPluginLoaded()
        {
            if (serverInitialized) UpdateDescription();
        }

        void OnPluginUnloaded() => UpdateDescription();

        void UpdateDescription(string text = "")
        {
            if (!string.IsNullOrEmpty(text))
            {
                Config["Description"] = text;
                description = text;
                SaveConfig();
            }

            var newDescription = new StringBuilder(description);
            var matches = varRegex.Matches(description);
            foreach (var match in matches)
            {
                if (match == null) continue;
                var matchString = match.ToString();
                var reply = ConsoleSystem.Run.Server.Quiet(matchString.Replace("{", "").Replace("}", ""));
                newDescription.Replace(matchString, reply ?? "");
            }

            if (listPlugins)
            {
                var loaded = plugins.GetAll();
                if (loaded.Length == 0) return;

                string pluginList = null;
                var count = 0;
                foreach (var plugin in loaded)
                {
                    if (plugin.IsCorePlugin || exclusions.Contains(plugin.Title)) continue;
                    pluginList += plugin.Title + ", ";
                    count++;
                }
                if (pluginList != null)
                {
                    if (pluginList.EndsWith(", ")) pluginList = pluginList.Remove(pluginList.Length - 2);
                    newDescription.Append($"\n\nPlugins ({count}): {pluginList}");
                }
            }

            if (newDescription.ToString() == ConVar.Server.description) return;
            ConVar.Server.description = newDescription.ToString();
            Puts("Server description updated!");
        }

        #endregion

        #region Command Handling

        object OnServerCommand(ConsoleSystem.Arg arg)
        {
            if (!serverInitialized) return null;

            var command = arg.cmd.namefull;
            if (command != "server.description" || !arg.isAdmin) return null;
            if (!arg.HasArgs() || arg.Args.GetValue(0) == null) return null;

            var newDescription = string.Join(" ", arg.Args.ToArray());
            UpdateDescription(newDescription);
            UnityEngine.Debug.Log($"server.description: {newDescription}");
            return true;
        }

        #endregion

        T GetConfig<T>(string name, T value) => Config[name] == null ? value : (T)System.Convert.ChangeType(Config[name], typeof(T));
    }
}
