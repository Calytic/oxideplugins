// Requires: Babel

using System;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
    [Info("BabelChat", "Wulf/lukespragg", "0.3.0", ResourceId = 1964)]
    [Description("Translates chat messages to each player's language preference or server default")]

    class BabelChat : CovalencePlugin
    {
        #region Initialization

        [PluginReference] Plugin AntiAds;
        [PluginReference] Plugin Babel;
        [PluginReference] Plugin BetterChat;

        bool forceDefault;
        bool prefixColors;
        bool showOriginal;

        protected override void LoadDefaultConfig()
        {
            // Options
            Config["Force Server Language (true/false)"] = forceDefault = GetConfig("Force Server Language (true/false)", false);
            Config["Random Prefix Colors (true/false)"] = prefixColors = GetConfig("Random Prefix Colors (true/false)", true);
            Config["Show Original Message (true/false)"] = showOriginal = GetConfig("Show Original Message (true/false)", false);

            // Cleanup
            Config.Remove("ForceDefault");
            Config.Remove("PrefixColors");
            Config.Remove("ShowOriginal");

            SaveConfig();
        }

        void Init() => LoadDefaultConfig();

        #endregion

        #region Chat Translation

        string Translate(IPlayer player, IPlayer target, string message)
        {
            var to = forceDefault ? lang.GetServerLanguage() : lang.GetLanguage(target.Id);
            var from = lang.GetLanguage(player.Id) ?? "auto";
            return (string)Babel.Call("Translate", message, to, from);
        }

        void SendMessage(IPlayer target, IPlayer player, string message)
        {
            var format = $"{player.Name}: {message}";

            if (BetterChat) format = (string)BetterChat.Call("API_GetFormatedMessage", player.Id, message);
            else if (prefixColors) switch (covalence.Game)
            {
                case "7DaysToDie":
                    format = $"[{Color()}]{player.Name}[ffffff]: {message}";
                    break;
                case "ReignOfKings":
                    format = $"[{Color()}]{player.Name}[ffffff]: {message}";
                    break;
                case "RustLegacy":
                    format = $"[color {Color()}]{player.Name}[/color]: {message}";
                    break;
                default:
                    format = $"<color=#{Color()}>{player.Name}</color>: {message}";
                    break;
            }
#if RUST
            var rust = Game.Rust.RustCore.FindPlayerByIdString(target.Id);
            rust?.SendConsoleCommand("chat.add", player.Id, format, 1.0);
#else
            target.Message(format);
#endif
        }

        #endregion

        #region Game Hooks

        object OnUserChat(IPlayer player, string message)
        {
            var isAd = AntiAds?.Call("IsAdvertisement", message);
            if (AntiAds && isAd != null && (bool)isAd) return null;

            foreach (var target in players.Connected)
            {
                if (player.Id == target.Id)
                {
                    SendMessage(player, player, message);
                    continue;
                }

                var to = forceDefault ? lang.GetServerLanguage() : lang.GetLanguage(target.Id);
                var from = lang.GetLanguage(player.Id) ?? "auto";
#if DEBUG
                PrintWarning($"To: {to}, From: {from}");
#endif
                Action<string> callback = response =>
                {
                    if (showOriginal) response = $"{message} \n{response}";
                    SendMessage(target, player, response);
                };
                Babel.Call("Translate", message, to, from, callback);
            }

            return !BetterChat ? (object)true : null;
        }

        bool OnBetterChat() => true;

        #endregion

        #region Helpers

        T GetConfig<T>(string name, T value) => Config[name] == null ? value : (T)Convert.ChangeType(Config[name], typeof(T));

        static string Color() => $"{new Random().Next(0x1000000):X6}";

        #endregion
    }
}
