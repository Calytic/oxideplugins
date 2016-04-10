using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using CodeHatch.Common;
using CodeHatch.Engine.Core.Commands;
using CodeHatch.Engine.Networking;

using Oxide.Game.ReignOfKings.Libraries;


namespace Oxide.Plugins
{
    [Info("Help Text", "Mughisi", "1.0.2", ResourceId = 1055)]
    public class HelpText : ReignOfKingsPlugin
    {

        #region Configuration Data
        // Do not modify these values, to configure this plugin edit
        // 'HelpText.json' in your server's config folder.
        // <drive>:\...\save\oxide\config\

        bool configChanged;

        // Plugin settings
        private const string DefaultChatPrefix = "Server";
        private const string DefaultChatPrefixColor = "950415";

        public string ChatPrefix { get; private set; }
        public string ChatPrefixColor { get; set; }

        #endregion

        private readonly FieldInfo ChatCommands = typeof (Command).GetField("chatCommands", BindingFlags.NonPublic | BindingFlags.Instance);
        private MethodInfo Enumerator;
        private MethodInfo MoveNext;
        private MethodInfo GetCurrent;

        private List<string> ReadOxideCommands()
        {
            var commands = new List<string>();
            var oxidecommands = ChatCommands.GetValue(cmd);
            if (Enumerator == null)
                Enumerator = oxidecommands.GetType().GetMethod("GetEnumerator");
            var enumerator = Enumerator.Invoke(oxidecommands, null);
            if (MoveNext == null)
                MoveNext = enumerator.GetType().GetMethod("MoveNext");
            if (GetCurrent == null)
                GetCurrent = enumerator.GetType().GetMethod("get_Current");
            while ((bool) MoveNext.Invoke(enumerator, null))
            {
                var command = GetCurrent.Invoke(enumerator, null);
                commands.Add(command.GetType().GetProperty("Key").GetValue(command).ToString().ToLower());
            }

            return commands;
        }

        [ChatCommand("Help")]
        private void Help(Player player, string command, string[] args)
        { 
            var RoKCommands = new List<CommandAttribute>();
            var registeredRoKCommands = CommandManager.RegisteredCommands.Keys.ToArray();
            var registeredOxideCommands = ReadOxideCommands();
            Array.Sort(registeredRoKCommands);
            
            foreach (var registeredCommand in registeredRoKCommands)
            {
                CommandAttribute RoKCommandAttr;
                var RoKCommand = registeredCommand;
                if (!CommandManager.RegisteredCommands.TryGetValue(RoKCommand, out RoKCommandAttr)) continue;
                if (RoKCommandAttr.IsAlias(RoKCommand)) continue;
                if (registeredOxideCommands.Contains(RoKCommand.ToLower()) && RoKCommand.ToLower() != "help") continue;
                if (player.HasPermission(RoKCommandAttr.Permission))
                    RoKCommands.Add(RoKCommandAttr);
            }

            CommandAttribute helpArgumentCommand;
            var helptext = string.Empty;
            if (args.Length <= 0)
            {
                if (RoKCommands.Count != 0)
                {
                    helptext = string.Concat(helptext, "For help with a command type '[666666]/help [command][-]'.\n");
                    foreach(var item in RoKCommands)
                        helptext = (!string.IsNullOrEmpty(item.Description) 
                            ? string.Concat(helptext, $"{item.Syntax} - [666666]{item.Description}[-]\n") 
                            : string.Concat(helptext, $"{item.Syntax}\n "));
                }
                else
                    helptext = string.Concat(helptext, "No help available, there is no hope.");
            }
            else if (!CommandManager.RegisteredCommands.TryGetValue(args[0].ToLower(), out helpArgumentCommand))
                helptext = string.Concat(helptext, $"Could not find command {args[0]}.");
            else if (!player.HasPermission(helpArgumentCommand.Permission))
                helptext = string.Concat(helptext, $"Could not find command {args[0]}.");
            else
            {
                helptext = (!string.IsNullOrEmpty(helpArgumentCommand.Description) 
                    ? string.Concat(helptext, $"{helpArgumentCommand.Syntax} - [666666]{helpArgumentCommand.Description}[-]\n") 
                    : string.Concat(helptext,$"{helpArgumentCommand.Syntax}\n "));
                if (helpArgumentCommand.SubCommands == null || helpArgumentCommand.SubCommands.Count <= 0) return;
                foreach (var subCommandAttribute in helpArgumentCommand.SubCommands)
                {
                    helptext = (!string.IsNullOrEmpty(subCommandAttribute.Description)
                        ? string.Concat(helptext, $"{subCommandAttribute.Syntax} - [666666]{subCommandAttribute.Description}[-]\n") 
                        : string.Concat(helptext, $"{subCommandAttribute.Syntax}\n "));
                }
            }
            SendMessage(player, helptext);
            if (args.Length > 0) return;
            plugins.CallHook("SendHelpText", player);
        }

        void SendMessage(Player player, string message, params object[] args) => SendReply(player, $"[{ChatPrefixColor}]{ChatPrefix}[FFFFFF]: {message}", args);
        
        void Warning(string msg) => PrintWarning($"{Title} : {msg}");

        void LoadConfigData()
        {
            // Plugin settings
            ChatPrefix = GetConfigValue("Settings", "ChatPrefix", DefaultChatPrefix);
            ChatPrefixColor = GetConfigValue("Settings", "ChatPrefixColor", DefaultChatPrefixColor);

            // Config stuff here

            if (!configChanged) return;
            Warning("The configuration file was updated!");
            SaveConfig();
        }

        T GetConfigValue<T>(string category, string setting, T defaultValue)
        {
            var data = Config[category] as Dictionary<string, object>;
            object value;
            if (data == null)
            {
                data = new Dictionary<string, object>();
                Config[category] = data;
                configChanged = true;
            }
            if (data.TryGetValue(setting, out value)) return (T)Convert.ChangeType(value, typeof(T));
            value = defaultValue;
            data[setting] = value;
            configChanged = true;
            return (T)Convert.ChangeType(value, typeof(T));
        }

    }
}
