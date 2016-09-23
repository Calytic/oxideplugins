using System;
using System.Collections.Generic;
using System.Linq;
using Oxide.Core.Libraries.Covalence;

namespace Oxide.Plugins
{
    [Info("PlayerList", "Wulf/lukespragg", "0.2.0", ResourceId = 2126)]
    [Description("Shows a count and list of all online players unless hidden")]

    class PlayerList : CovalencePlugin
    {
        #region Initialization

        const string permAllow = "playerlist.allow";
        const string permHide = "playerlist.hide";
        bool separateAdmin;
        string adminColor;

        protected override void LoadDefaultConfig()
        {
            Config["AdminColor"] = adminColor = GetConfig("AdminColor", "e68c17");
            Config["SeparateAdmin"] = separateAdmin = GetConfig("SeparateAdmin", false);
            SaveConfig();
        }

        void Init()
        {
            LoadDefaultConfig();
            LoadDefaultMessages();
            permission.RegisterPermission(permAllow, this);
            permission.RegisterPermission(permHide, this);
        }

        #endregion

        #region Localization

        void LoadDefaultMessages()
        {
            // English
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["AdminCount"] = "{0} admin online",
                ["AdminList"] = "Admin online ({0}): {1}",
                ["NobodyOnline"] = "No players are currently online",
                ["NotAllowed"] = "You are not allowed to use the '{0}' command",
                ["OnlyYou"] = "You are the only one online!",
                ["PlayerCount"] = "{0} player(s) online",
                ["PlayerList"] = "Players online ({0}): {1}"
            }, this);

            // French
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["AdminCount"] = "{0} administrateurs en ligne",
                ["AdminList"] = "Administrateurs en ligne ({0})Â : {1}",
                ["NobodyOnline"] = "Aucuns joueurs ne sont actuellement en ligne",
                ["NotAllowed"] = "Vous nâÃªtes pas autorisÃ© Ã  utiliser la commande Â«Â {0}Â Â»",
                ["OnlyYou"] = "Vous Ãªtes la seule personne en ligneÂ !",
                ["PlayerCount"] = "{0} joueur(s) en ligne",
                ["PlayerList"] = "Joueurs en ligne ({0}) : {1}"
            }, this, "fr");

            // German
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["AdminCount"] = "{0} Administratoren online",
                ["AdminList"] = "Administratoren online ({0}): {1}",
                ["NobodyOnline"] = "Keine Spieler sind gerade online",
                ["NotAllowed"] = "Sie sind nicht berechtigt, verwenden Sie den Befehl '{0}'",
                ["OnlyYou"] = "Du bist der einzige Online!",
                ["PlayerCount"] = "{0} Spieler online",
                ["PlayerList"] = "Spieler online ({0}): {1}"
            }, this, "de");

            // Russian
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["AdminCount"] = "{0} Ð°Ð´Ð¼Ð¸Ð½Ð¸ÑÑÑÐ°ÑÐ¾ÑÑ Ð¾Ð½Ð»Ð°Ð¹Ð½",
                ["AdminList"] = "ÐÐ´Ð¼Ð¸Ð½Ð¸ÑÑÑÐ°ÑÐ¾ÑÑ Ð¾Ð½Ð»Ð°Ð¹Ð½ ({0}): {1}",
                ["NobodyOnline"] = "ÐÐ¸ Ð¾Ð´Ð¸Ð½ Ð¸Ð· Ð¸Ð³ÑÐ¾ÐºÐ¾Ð² Ð¾Ð½Ð»Ð°Ð¹Ð½",
                ["NotAllowed"] = "ÐÐµÐ»ÑÐ·Ñ Ð¸ÑÐ¿Ð¾Ð»ÑÐ·Ð¾Ð²Ð°ÑÑ ÐºÐ¾Ð¼Ð°Ð½Ð´Ñ Â«{0}Â»",
                ["OnlyYou"] = "ÐÑ ÑÐ²Ð»ÑÐµÑÐµÑÑ ÐµÐ´Ð¸Ð½ÑÑÐ²ÐµÐ½Ð½ÑÐ¼ Ð¾Ð½Ð»Ð°Ð¹Ð½!",
                ["PlayerCount"] = "{0} Ð¸Ð³ÑÐ¾ÐºÐ° (Ð¾Ð²) Ð¾Ð½Ð»Ð°Ð¹Ð½",
                ["PlayerList"] = "ÐÐ³ÑÐ¾ÐºÐ¾Ð² Ð¾Ð½Ð»Ð°Ð¹Ð½ ({0}): {1}"
            }, this, "ru");

            // Spanish
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["AdminCount"] = "{0} administradores en lÃ­nea",
                ["AdminList"] = "Los administradores en lÃ­nea ({0}): {1}",
                ["NobodyOnline"] = "No hay jugadores estÃ¡n actualmente en lÃ­nea",
                ["NotAllowed"] = "No se permite utilizar el comando '{0}'",
                ["OnlyYou"] = "Usted es el Ãºnico en lÃ­nea!",
                ["PlayerCount"] = "{0} jugadores en lÃ­nea",
                ["PlayerList"] = "Jugadores en lÃ­nea ({0}): {1}"
            }, this, "es");
        }

        #endregion

        #region Commands

        [Command("online")]
        void OnlineCommand(IPlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.Id, permAllow) && player.Id != "server_console")
            {
                player.Reply(Lang("NotAllowed", player.Id, command));
                return;
            }

            var adminCount = players.Connected.Count(p => p.IsAdmin && !permission.UserHasPermission(p.Id, permHide));
            var playerCount = players.Connected.Count(p => !p.IsAdmin && !permission.UserHasPermission(p.Id, permHide));

            player.Reply($"{Lang("AdminCount", player.Id, adminCount)}, {Lang("PlayerCount", player.Id, playerCount)}");
        }

        [Command("players", "who")]
        void PlayersCommand(IPlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.Id, permAllow) && player.Id != "server_console")
            {
                player.Reply(Lang("NotAllowed", player.Id, command));
                return;
            }

            var adminCount = players.Connected.Count(p => p.IsAdmin && !permission.UserHasPermission(p.Id, permHide));
            var playerCount = players.Connected.Count(p => !p.IsAdmin && !permission.UserHasPermission(p.Id, permHide));
            var totalCount = adminCount + playerCount;

            switch (totalCount)
            {
                case 0:
                    player.Reply(Lang("NobodyOnline", player.Id));
                    break;
                case 1:
                    player.Reply(Lang("OnlyYou", player.Id));
                    break;
                default:
                    var adminList = string.Join(", ", players.Connected.Where(p => p.IsAdmin).Select(p => covalence.FormatText($"[#{adminColor}]{p.Name}[/#]")).ToArray());
                    var playerList = string.Join(", ", players.Connected.Where(p => !p.IsAdmin).Select(p => p.Name).ToArray());
                    if (separateAdmin && !string.IsNullOrEmpty(adminList)) player.Reply(Lang("AdminList", player.Id, adminCount, adminList));
                    if (!string.IsNullOrEmpty(playerList)) player.Reply(Lang("PlayerList", player.Id, playerCount, playerList));
                    break;
            }
        }

        #endregion

        T GetConfig<T>(string name, T value) => Config[name] == null ? value : (T)Convert.ChangeType(Config[name], typeof(T));

        string Lang(string key, string id = null, params object[] args) => string.Format(lang.GetMessage(key, this, id), args);
    }
}
