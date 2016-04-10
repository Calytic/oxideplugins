using System;
using System.Collections.Generic;
using Oxide.Core;
using Oxide.Core.Plugins;
namespace Oxide.Plugins
{
    [Info("MagicDescription", "Norn", 0.1, ResourceId = 0)]
    [Description("Dynamic server description (plugins etc).")]
    public class MagicDescription : RustPlugin
    {
        protected override void LoadDefaultConfig()
        {
            Puts("No configuration file found, generating...");
            Config.Clear();

            // --- [ GENERAL SETTINGS ] ---
            Config["Refresh"] = true;
            Config["Description"] = ConVar.Server.description;
            SaveConfig();
        }
        class StoredData
        {
            public Dictionary<int, string> Plugins = new Dictionary<int, string>();
            public StoredData() { }
        }
        private void SaveData()
        {
            Interface.Oxide.DataFileSystem.WriteObject(this.Title, PluginData);
        }
		System.Random rnd = new System.Random();
        protected int GetRandomInt(int min, int max){ return rnd.Next(min, max); }
        StoredData PluginData;
        void Loaded()
        {
            PluginData = Interface.GetMod().DataFileSystem.ReadObject<StoredData>(this.Title);
        }
        [ConsoleCommand("plugins.refresh")]
        private void RefreshPluginsEx(ConsoleSystem.Arg arg)
        {
            if(arg.isAdmin) RefreshPlugins();
        }
        private void RefreshPlugins()
        {
            PluginData.Plugins.Clear();
            int current_count = 0;
            foreach (Plugin p in plugins.GetAll()) { PluginData.Plugins.Add(GetRandomInt(0, 99999), p.Title); current_count++; }
            if (Convert.ToBoolean(Config["Refresh"])) { Config["Refresh"] = false; SaveConfig(); }
            if (current_count != 0) { Puts("Added " + current_count + " plugins to the database."); SaveData(); }
        }
        private void InitPlugin()
        {
            if (Convert.ToBoolean(Config["Refresh"])) { RefreshPlugins(); }
            if (PluginData.Plugins.Count != 0) { Puts("Loaded " + PluginData.Plugins.Count.ToString() + " plugins from the database."); SaveData(); }
            string plugin_string = null; int count = 0;
            foreach (var loaded_plug in PluginData.Plugins) { plugin_string += loaded_plug.Value + ", "; count++; }
            if (plugin_string != null)
            { if (plugin_string.EndsWith(", ")) plugin_string = plugin_string.Remove(plugin_string.Length - 2); ConVar.Server.description = Config["Description"].ToString() + "\n\nPlugin List ["+count.ToString()+"]:\n\n" + plugin_string; }
        }
        private void UpdateDescription(string description)
        {
            if (description.Length >= 1)
            {
                string plugin_string = null; int count = 0;
                Config["Description"] = description; SaveConfig();
                foreach (var loaded_plug in PluginData.Plugins) { plugin_string += loaded_plug.Value + ", "; count++; }
                if (plugin_string != null) { if (plugin_string.EndsWith(", ")) plugin_string = plugin_string.Remove(plugin_string.Length - 2); ConVar.Server.description = Config["Description"].ToString() + "\n\nPlugin List [" + count.ToString() + "]:\n\n" + plugin_string; ; }
                Puts("\"server.description\" : \"" + Config["Description"].ToString() + "\"");
            }
        }
        object OnRunCommand(ConsoleSystem.Arg arg)
        {
            try {
                string command = arg.cmd.namefull.ToString().ToLower();
                if (command == "server.description" && arg.isAdmin)
                {
                    if (arg.HasArgs())
                    {
                        if (arg.Args.GetValue(0) != null)
                        {
                            UpdateDescription(arg.Args.GetValue(0).ToString());
                            return false;
                        }
                    }
                    else
                    {
                        Puts("\"server.description\" : \""+ Config["Description"].ToString() +"\"");
                        return false;
                    }
                }
            }
            catch { }
            return null;
        }
        void OnServerInitialized()
        {
            InitPlugin();
        }
    }
}