using System;
using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("NoSuicide", "Wulf/lukespragg", "0.1.3")]
    [Description("Stops players from suiciding/killing themselves")]

    class NoSuicide : CovalencePlugin
    {
        // Do NOT edit this file, instead edit NoSuicide.en.json in oxide/lang,
        // or create a language file for another language using the 'en' file as a default.

        #region Localization

        readonly Dictionary<string, string> messages = new Dictionary<string, string>();

        void LoadDefaultMessages()
        {
            messages.Add("SuicideNotAllowed", "Sorry, suicide is not an option!");
            lang.RegisterMessages(messages, this);
        }

        #endregion

        #region Initialization

        void Loaded()
        {
#if !HURTWORLD && !RUST
            throw new NotSupportedException($"This plugin does not support {(covalence.Game ?? "this game")}");
#endif

            LoadDefaultMessages();
            permission.RegisterPermission("nosuicide.excluded", this);
        }

        #endregion

        #region Suicide Handling

        bool CanSuicide(string steamId)
        {
            if (HasPermission(steamId, "nosuicide.excluded")) return true;

            var player = players.GetOnlinePlayer(steamId);
            player.Message(GetMessage("SuicideNotAllowed", steamId));
            return false;
        }

#if HURTWORLD
        object OnPlayerSuicide(PlayerSession session) => CanSuicide(session.SteamId.ToString()) ? (object) null : true;
#endif

#if RUST
        object OnRunCommand(ConsoleSystem.Arg arg) => arg.cmd?.name != "kill" || CanSuicide(arg.connection?.userid.ToString()) ? (object) null : true;
#endif

        #endregion

        #region Helper Methods

        string GetMessage(string key, string steamId = null) => lang.GetMessage(key, this, steamId);

        bool HasPermission(string steamId, string perm) => permission.UserHasPermission(steamId, perm);

#endregion
    }
}