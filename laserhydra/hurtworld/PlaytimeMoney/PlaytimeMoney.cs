//Reference: UnityEngine.UI
using System.Collections.Generic;
using Oxide.Core.Plugins;
using System.Linq;
using System;

namespace Oxide.Plugins
{
    [Info("Playtime Money", "LaserHydra", "1.0.0", ResourceId = 0)]
    [Description("Gives money to players for time on the server")]
    class PlaytimeMoney : HurtworldPlugin
    {
        Plugin Economy;

        class PluginNotFoundException : Exception
        {
            public PluginNotFoundException(string message) : base(message)
            {
            }
        }

        ////////////////////////////////////////
        ///     Plugin Related Hooks
        ////////////////////////////////////////

        void Loaded()
        {
#if !HURTWORLD
            throw new NotSupportedException("This plugin or the version of this plugin does not support this game!");
#endif

            Economy = (Plugin)plugins.Find("Economy");

            if (Economy == null)
                throw new PluginNotFoundException($"Economy plugin not found! '{this.Title}' needs Economy to work! Get Economy here: http://oxidemod.org/plugins/1602/");

            LoadConfig();
            LoadMessages();

            timer.Repeat(GetConfig(10F, "Settings", "Interval (in Minutes)") * 60F, 0, () => {
                foreach (PlayerSession current in GameManager.Instance.GetSessions().Values)
                    if (current != null && current.Name != null && current.IsLoaded)
                    {
                        Economy.Call("AddMoney", current, GetConfig(25D, "Settings", "Money Amount"));
                        SendChatMessage(current, GetMsg("Recieved Money", current.SteamId).Replace("{amount}", GetConfig(25D, "Settings", "Money Amount").ToString()));
                    }
            });
        }

        ////////////////////////////////////////
        ///     Config & Message Loading
        ////////////////////////////////////////
        
        void LoadConfig()
        {
            SetConfig("Settings", "Interval (in Minutes)", 10F);
            SetConfig("Settings", "Money Amount", 25D);

            SaveConfig();
        }

        void LoadMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                {"Recieved Money", "You have recieved {amount}$ for playing on the server."}
            }, this);
        }

        protected override void LoadDefaultConfig()
        {
            PrintWarning("Generating new config file...");
        }
        
        ////////////////////////////////////////
        ///     Converting
        ////////////////////////////////////////

        string ListToString(List<string> list, int first, string seperator)
        {
            return String.Join(seperator, list.Skip(first).ToArray());
        }

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

        string GetMsg(string key, object userID = null)
        {
            return lang.GetMessage(key, this, userID.ToString());
        }

        ////////////////////////////////////////
        ///     Chat Handling
        ////////////////////////////////////////

        void BroadcastChat(string prefix, string msg = null) => hurt.BroadcastChat(msg == null ? prefix : "<color=#C4FF00>" + prefix + "</color>: " + msg);

        void SendChatMessage(PlayerSession player, string prefix, string msg = null) => hurt.SendChatMessage(player, msg == null ? prefix : "<color=#C4FF00>" + prefix + "</color>: " + msg);
    }
}
