using System;
using System.Collections.Generic;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;

namespace Oxide.Plugins
{
    [Info("Healer", "Wulf/lukespragg", "2.2.1", ResourceId = 658)]
    [Description("Allows players with permission to heal themselves or others")]

    class Healer : CovalencePlugin
    {
        #region Initialization

        readonly Hash<string, float> cooldowns = new Hash<string, float>();
        const string permUse = "healer.use";
        int maxAmount;
        int usageCooldown;

        protected override void LoadDefaultConfig()
        {
            Config["MaxAmount"] = maxAmount = GetConfig("MaxAmount", 100);
            Config["UsageCooldown"] = usageCooldown = GetConfig("UsageCooldown", 30);
            SaveConfig();
        }

        void Init()
        {
            LoadDefaultConfig();
            LoadDefaultMessages();
            permission.RegisterPermission(permUse, this);
        }

        #endregion

        #region Localization

        void LoadDefaultMessages()
        {
            // English
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["CommandUsage"] = "Usage: {0} <amount> <name or id> (target optional)",
                ["Cooldown"] = "Wait a bit before attempting to use '{0}' again",
                ["NotAllowed"] = "You are not allowed to use the '{0}' command",
                ["PlayerNotFound"] = "Player '{0}' was not found",
                ["PlayerWasHealed"] = "{0} was healed {1}",
                ["YouWereHealed"] = "You were healed {0}"
            }, this);

            // French
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["CommandUsage"] = "UtilisationÂ : {0} <montant> <nom ou id> (objectif en option)",
                ["Cooldown"] = "Attendre un peu avant de tenter de rÃ©utiliser Â«Â {0}Â Â»",
                ["NotAllowed"] = "Vous nâÃªtes pas autorisÃ© Ã  utiliser la commande Â«Â {0}Â Â»",
                ["PlayerNotFound"] = "Player Â«Â {0}Â Â» nâa pas Ã©tÃ© trouvÃ©e",
                ["PlayerWasHealed"] = "{0} a Ã©tÃ© guÃ©ri {1}",
                ["YouWereHealed"] = "Vous avez Ã©tÃ© guÃ©ri {0}"
            }, this, "fr");

            // German
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["CommandUsage"] = "Verwendung: {0} <Betrag> <Name oder Id> (Ziel optional)",
                ["Cooldown"] = "Noch ein bisschen warten Sie, bevor Sie '{0}' wieder verwenden",
                ["NotAllowed"] = "Sie sind nicht berechtigt, verwenden Sie den Befehl '{0}'",
                ["PlayerNotFound"] = "Player '{0}' wurde nicht gefunden",
                ["PlayerWasHealed"] = "{0} wurde geheilt {1}",
                ["YouWereHealed"] = "Sie wurden geheilt {0}"
            }, this, "de");

            // Russian
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["CommandUsage"] = "ÐÑÐ¿Ð¾Ð»ÑÐ·Ð¾Ð²Ð°Ð½Ð¸Ðµ: {0} <ÑÑÐ¼Ð¼Ð°> <Ð¸Ð¼Ñ Ð¸Ð»Ð¸ id> (ÑÐµÐ»Ñ Ð½ÐµÐ¾Ð±ÑÐ·Ð°ÑÐµÐ»ÑÐ½Ð¾)",
                ["Cooldown"] = "ÐÐ¾Ð´Ð¾Ð¶Ð´Ð¸ÑÐµ Ð½ÐµÐ¼Ð½Ð¾Ð³Ð¾, Ð¿ÑÐµÐ¶Ð´Ðµ ÑÐµÐ¼ Ð¸ÑÐ¿Ð¾Ð»ÑÐ·Ð¾Ð²Ð°ÑÑ Â«{0}Â» ÑÐ½Ð¾Ð²Ð°",
                ["NotAllowed"] = "ÐÐµÐ»ÑÐ·Ñ Ð¸ÑÐ¿Ð¾Ð»ÑÐ·Ð¾Ð²Ð°ÑÑ ÐºÐ¾Ð¼Ð°Ð½Ð´Ñ Â«{0}Â»",
                ["PlayerNotFound"] = "ÐÐ³ÑÐ¾Ðº Â«{0}Â» Ð½Ðµ Ð½Ð°Ð¹Ð´ÐµÐ½",
                ["PlayerWasHealed"] = "{0} Ð±ÑÐ» Ð¸ÑÑÐµÐ»ÐµÐ½ {1}",
                ["YouWereHealed"] = "ÐÑ Ð±ÑÐ»Ð¸ Ð·Ð°ÑÑÐ±ÑÐµÐ²Ð°Ð²ÑÐ¸ÐµÑÑ {0}"
            }, this, "ru");

            // Spanish
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["CommandUsage"] = "Uso: {0} <cantidad> <nombre o id> (destino opcional)",
                ["Cooldown"] = "Esperar un poco antes de intentar volver a utilizar '{0}'",
                ["NotAllowed"] = "No se permite utilizar el comando '{0}'",
                ["PlayerNotFound"] = "Jugador '{0}' no se encontrÃ³",
                ["PlayerWasHealed"] = "{0} es {1} curado",
                ["YouWereHealed"] = "Fuiste sanado {0}"
            }, this, "es");
        }

        #endregion

        #region Commands

        [Command("heal")]
        void HealCommand(IPlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.Id, permUse) && player.Id != "server_console")
            {
                player.Reply(Lang("NotAllowed", player.Id, command));
                return;
            }

            if (args.Length == 0)
            {
                player.Reply(Lang("CommandUsage", player.Id, command));
                return;
            }

            if (!cooldowns.ContainsKey(player.Id)) cooldowns.Add(player.Id, 0f);
            if (usageCooldown != 0 && cooldowns[player.Id] + usageCooldown > Interface.Oxide.Now)
            {
                player.Reply(Lang("Cooldown", player.Id, command));
                return;
            }

            float amount;
            var amountGiven = float.TryParse(args[0], out amount);

            IPlayer target;
            if (args.Length >= 2 && amountGiven) target = players.FindPlayer(args[1]) ?? players.GetPlayer(args[1]) ?? player;
            else target = players.FindPlayer(args[0]) ?? players.GetPlayer(args[0]) ?? player;
            if (target.Id == "server_console" || !target.IsConnected)
            {
                var name = args.Length >= 2 && amountGiven ? args[1] : !amountGiven ? args[0] : target.Name;
                player.Reply(Lang("PlayerNotFound", player.Id, name));
                return;
            }

            if (amount > maxAmount || amount.Equals(0)) amount = maxAmount;
#if RUST
            var basePlayer = target.Object as BasePlayer;
            basePlayer.metabolism.bleeding.value = 0;
            basePlayer.metabolism.calories.value += amount;
            basePlayer.metabolism.dirtyness.value = 0;
            basePlayer.metabolism.hydration.value += amount;
            basePlayer.metabolism.oxygen.value = 1;
            basePlayer.metabolism.poison.value = 0;
            basePlayer.metabolism.radiation_level.value = 0;
            basePlayer.metabolism.radiation_poison.value = 0;
            basePlayer.metabolism.wetness.value = 0;
#endif
            target.Heal(amount);
            cooldowns[player.Id] = Interface.Oxide.Now;
            target.Message(Lang("YouWereHealed", player.Id, amount));
            if (!Equals(target, player)) player.Reply(Lang("PlayerWasHealed", player.Id, target.Name, amount));
        }

        #endregion

        T GetConfig<T>(string name, T value) => Config[name] == null ? value : (T)Convert.ChangeType(Config[name], typeof(T));

        string Lang(string key, string id = null, params object[] args) => string.Format(lang.GetMessage(key, this, id), args);
    }
}
