using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Oxide.Core.Libraries.Covalence;

namespace Oxide.Plugins
{
    [Info("Whitelist", "Wulf/lukespragg", "3.3.0", ResourceId = 1932)]
    [Description("Restricts server access to whitelisted players only")]

    class Whitelist : CovalencePlugin
    {
        #region Initialization

        //const string permAdmin = "whitelist.admin";
        const string permAllow = "whitelist.allow";

        bool adminExcluded;
        bool resetOnRestart;

        protected override void LoadDefaultConfig()
        {
            // Options
            Config["Admin Excluded (true/false)"] = adminExcluded = GetConfig("Admin Excluded (true/false)", true);
            Config["Reset On Restart (true/false)"] = resetOnRestart = GetConfig("Reset On Restart (true/false)", false);

            // Cleanup
            Config.Remove("AdminExcluded");
            Config.Remove("ResetOnRestart");

            SaveConfig();
        }

        void OnServerInitialized()
        {
            LoadDefaultConfig();
            LoadDefaultMessages();

            //permission.RegisterPermission(permAdmin, this);
            permission.RegisterPermission(permAllow, this);

            foreach (var player in players.All)
            {
                if (!player.HasPermission("whitelist.allowed")) continue;
                permission.GrantUserPermission(player.Id, permAllow, null);
                permission.RevokeUserPermission(player.Id, "whitelist.allowed");
            }

            foreach (var group in permission.GetGroups())
            {
                if (!permission.GroupHasPermission(group, "whitelist.allowed")) continue;
                permission.GrantGroupPermission(group, permAllow, null);
                permission.RevokeGroupPermission(group, "whitelist.allowed");
            }

            if (!resetOnRestart) return;
            foreach (var group in permission.GetGroups())
                if (permission.GroupHasPermission(group, permAllow)) permission.RevokeGroupPermission(group, permAllow);
            foreach (var user in permission.GetPermissionUsers(permAllow))
                permission.RevokeUserPermission(Regex.Replace(user, "[^0-9]", ""), permAllow);
        }

        #endregion

        #region Localization

        void LoadDefaultMessages()
        {
            // English
            lang.RegisterMessages(new Dictionary<string, string>
            {
                //["CommandUsage"] = "Usage: {0} <name or id> <permission>",
                //["NoPlayersFound"] = "No players were found using '{0}'",
                //["NotAllowed"] = "You are not allowed to use the '{0}' command",
                ["NotWhitelisted"] = "You are not whitelisted",
                //["WhitelistAdd"] = "'{0}' has been added to the whitelist",
                //["WhitelistRemove"] = "'{0}' has been removed from the whitelist"
            }, this);

            // French
            lang.RegisterMessages(new Dictionary<string, string>
            {
                //["CommandUsage"] = "UtilisationÂ : {0} <nom ou id> <permission>",
                //["NoPlayersFound"] = "Pas de joueurs ont Ã©tÃ© trouvÃ©s Ã  lâaide de Â«Â {0}Â Â»",
                //["NotAllowed"] = "Vous nâÃªtes pas autorisÃ© Ã  utiliser la commande Â«Â {0}Â Â»",
                ["NotWhitelisted"] = "Vous nâÃªtes pas dans la liste blanche",
                //["Whitelisted"] = ""
            }, this, "fr");

            // German
            lang.RegisterMessages(new Dictionary<string, string>
            {
                //["CommandUsage"] = "Verbrauch: {0} < Name oder Id> <erlaubnis>",
                //["NoPlayersFound"] = "Keine Spieler wurden durch '{0}' gefunden",
                //["NotAllowed"] = "Sie sind nicht berechtigt, verwenden Sie den Befehl '{0}'",
                ["NotWhitelisted"] = "Du bist nicht zugelassenen",
                //["Whitelisted"] = ""
            }, this, "de");

            // Russian
            lang.RegisterMessages(new Dictionary<string, string>
            {
                //["CommandUsage"] = "ÐÑÐ¿Ð¾Ð»ÑÐ·Ð¾Ð²Ð°Ð½Ð¸Ðµ: {0} <Ð¸Ð¼Ñ Ð¸Ð»Ð¸ Ð¸Ð´ÐµÐ½ÑÐ¸ÑÐ¸ÐºÐ°ÑÐ¾Ñ> <ÑÐ°Ð·ÑÐµÑÐµÐ½Ð¸Ðµ>",
                //["NoPlayersFound"] = "ÐÐ³ÑÐ¾ÐºÐ¸ Ð½Ðµ Ð±ÑÐ»Ð¸ Ð½Ð°Ð¹Ð´ÐµÐ½Ñ Ñ Ð¿Ð¾Ð¼Ð¾ÑÑÑ {0}",
                //["NotAllowed"] = "ÐÐµÐ»ÑÐ·Ñ Ð¸ÑÐ¿Ð¾Ð»ÑÐ·Ð¾Ð²Ð°ÑÑ ÐºÐ¾Ð¼Ð°Ð½Ð´Ñ Â«{0}Â»",
                ["NotWhitelisted"] = "ÐÑ Ð½Ðµ Ð¼Ð¾Ð¶ÐµÑÐµ",
                //["Whitelisted"] = ""
            }, this, "ru");

            // Spanish
            lang.RegisterMessages(new Dictionary<string, string>
            {
                //["CommandUsage"] = "Uso: {0} <nombre o id> <permiso>",
                //["NoPlayersFound"] = "No hay jugadores se encontraron con '{0}'",
                //["NotAllowed"] = "No se permite utilizar el comando '{0}'",
                ["NotWhitelisted"] = "No estÃ¡s en lista blanca",
                //["Whitelisted"] = ""
            }, this, "es");
        }

        #endregion

        #region Whitelisting

        bool IsWhitelisted(string id)
        {
            var player = players.FindPlayerById(id);
            return player != null && adminExcluded && player.IsAdmin || permission.UserHasPermission(id, permAllow);
        }

        object CanUserLogin(string name, string id) => !IsWhitelisted(id) ? Lang("NotWhitelisted", id) : null;

        /*[Command("whitelist", "wl")]
        void Command(IPlayer player, string command, string[] args)
        {
            if (!player.HasPermission(permAdmin))
            {
                player.Reply(Lang("NotAllowed", player.Id, command));
                return;
            }

            if (args.Length == 0)
            {
                player.Reply(Lang("CommandUsage", player.Id, command));
                return;
            }

            var target = players.FindPlayer(string.Join(" ", args.ToArray()));
            if (target == null)
            {
                player.Reply(Lang("NoPlayersFound", player.Id, args[0]));
                return;
            }

            if (args[0] == "remove")
            {
                permission.RevokeUserPermission(target.Id, permAllow, this);
                player.Reply(Lang("Whitelisted", player.Id, args[0]));
            }
            else
            {
                permission.GrantUserPermission(target.Id, permAllow, this);
                player.Reply(Lang("Whitelisted", player.Id, args[0]));
            }
        }*/

        #endregion

        #region Helpers

        T GetConfig<T>(string name, T value) => Config[name] == null ? value : (T)Convert.ChangeType(Config[name], typeof(T));

        string Lang(string key, string id = null, params object[] args) => string.Format(lang.GetMessage(key, this, id), args);

        #endregion
    }
}
