using System.Collections.Generic;
using System.Linq;
using System;

namespace Oxide.Plugins
{
    [Info("Chat Guard", "LaserHydra", "2.1.0", ResourceId = 1486)]
    [Description("Allows you to censor unwanted words and symbols in the chat.")]
    class ChatGuard : RustPlugin
    {
        #region Plugin General

        ////////////////////////////////////////
        ///     Plugin Related Hooks
        ////////////////////////////////////////

        void Loaded()
        {
#if !RUST
            throw new NotSupportedException("This plugin or the version of this plugin does not support this game!");
#endif

            LoadConfig();
        }

        ////////////////////////////////////////
        ///     Config & Message Loading
        ////////////////////////////////////////

        void LoadConfig()
        {
            SetConfig("Banned Words", new List<string>
            {
                "bitch",
                "faggot",
                "fuck"
            });

            SetConfig("Settings", "Replacement", "*");
            SetConfig("Settings", "Use Custom Replacement", false);
            SetConfig("Settings", "Custom Replacement", "Unicorn");

            SaveConfig();
        }

        protected override void LoadDefaultConfig() => PrintWarning("Generating new config file...");
        #endregion

        #region Subject Related

        object OnPlayerChat(ConsoleSystem.Arg arg)
        {
            BasePlayer player = (BasePlayer) arg.connection.player;
            string message = arg.GetString(0, "");
            
            if (FilterText(message) != message)
            {
                Puts(@"Filtered ""{0}""", message);

                message = FilterText(message);
                player.SendConsoleCommand("chat.say", message);

                return false;
            }

            return null;
        }

        string FilterText(string original)
        {
            string filtered = original;

            foreach (string word in original.Split(' '))
                foreach (string bannedword in GetConfig(new List<object> { "bitch", "faggot", "fuck" }, "Banned Words"))
                    if (TranslateLeet(word).ToLower().Contains(bannedword.ToLower()))
                        filtered = filtered.Replace(word, Replace(word));

            /*
            foreach (string word in GetConfig(new List<object> { "bitch", "faggot", "fuck" }, "Banned Words"))
                filtered = new Regex(@"((?:[\S]?)+" + word + @"(?:[\S]?)+)", RegexOptions.IgnoreCase).Replace(filtered, (a) => Replace(a));*/

            return filtered;
        }

        string Replace(string original)
        {
            string filtered = string.Empty;

            if (!GetConfig(false, "Settings", "Use Custom Replacement"))
                for (; filtered.Count() < original.Count() ;)
                    filtered += GetConfig("*", "Settings", "Replacement");
            else
                filtered = GetConfig("Unicorn", "Settings", "Custom Replacement");

            return filtered;
        }

        string TranslateLeet(string original)
        {
            string translated = original;

            Dictionary<string, string> leetTable = new Dictionary<string, string>
            {
                { "}{", "h" },
                { "|-|", "h" },
                { "]-[", "h" },
                { "/-/", "h" },
                { "|{", "k" },
                { "/\\/\\", "m" },
                { "|\\|", "n" },
                { "/\\/", "n" },
                { "()", "o" },
                { "[]", "o" },
                { "vv", "w" },
                { "\\/\\/", "w" },
                { "><", "x" },
                { "2", "z" },
                { "4", "a" },
                { "@", "a" },
                { "8", "b" },
                { "Ã", "b" },
                { "(", "c" },
                { "<", "c" },
                { "{", "c" },
                { "3", "e" },
                { "â¬", "e" },
                { "6", "g" },
                { "9", "g" },
                { "&", "g" },
                { "#", "h" },
                { "$", "s" },
                { "7", "t" },
                { "|", "l" },
                { "1", "i" },
                { "!", "i" },
                { "0", "o" },
            };

            foreach (var leet in leetTable)
                translated = translated.Replace(leet.Key, leet.Value);

            return translated;
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
        ///     Chat Related
        ////////////////////////////////////////

        void BroadcastChat(string prefix, string msg = null) => rust.BroadcastChat(msg == null ? prefix : "<color=#C4FF00>" + prefix + "</color>: " + msg);

        void SendChatMessage(BasePlayer player, string prefix, string msg = null) => rust.SendChatMessage(player, msg == null ? prefix : "<color=#C4FF00>" + prefix + "</color>: " + msg);

        #endregion
    }
}
