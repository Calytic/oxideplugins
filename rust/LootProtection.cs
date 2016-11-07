using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("LootProtection", "Wulf/lukespragg", "0.5.0", ResourceId = 1150)]
    [Description("Protects corpses and/or sleepers with permission from being looted by other players")]

    class LootProtection : CovalencePlugin
    {
        #region Initialization

        const string permBypass = "lootprotection.bypass";
        const string permCorpse = "lootprotection.corpse";
        const string permSleeper = "lootprotection.sleeper";

        void Init()
        {
            LoadDefaultMessages();

            permission.RegisterPermission(permBypass, this);
            permission.RegisterPermission(permCorpse, this);
            permission.RegisterPermission(permSleeper, this);
        }

        void OnServerInitialized()
        {
            foreach (var player in players.All)
            {
                if (!player.HasPermission("lootprotection.enable")) continue;
                permission.RevokeUserPermission(player.Id, "lootprotection.enable");
            }

            foreach (var group in permission.GetGroups())
            {
                if (!permission.GroupHasPermission(group, "lootprotection.enable")) continue;
                permission.RevokeGroupPermission(group, "lootprotection.enable");
            }
        }

        #endregion

        #region Localization

        void LoadDefaultMessages()
        {
            // English
            lang.RegisterMessages(new Dictionary<string, string> { ["LootProtection"] = "{0} has loot protection enabled" }, this);

            // French
            lang.RegisterMessages(new Dictionary<string, string> { ["LootProtection"] = "{0} a protection de butin activÃ©e" }, this, "fr");

            // German
            lang.RegisterMessages(new Dictionary<string, string> { ["LootProtection"] = "{0} hat Beute Schutz aktiviert" }, this, "de");

            // Russian
            lang.RegisterMessages(new Dictionary<string, string> { ["LootProtection"] = "{0} Ð¸Ð¼ÐµÐµÑ Ð²ÐºÐ»ÑÑÐµÐ½Ð° Ð·Ð°ÑÐ¸ÑÐ° ÐÑÑ" }, this, "ru");

            // Spanish
            lang.RegisterMessages(new Dictionary<string, string> { ["LootProtection"] = "{0} tiene botÃ­n protecciÃ³n activada" }, this, "es");
        }

        #endregion

        #region Loot Protection

        object OnLootEntity(BasePlayer looter, BaseEntity entity)
        {
            var corpse = entity as LootableCorpse;
            var sleeper = entity as BasePlayer;

            if (permission.UserHasPermission(looter.UserIDString, permBypass)) return null;

            if (corpse != null && permission.UserHasPermission(corpse.playerSteamID.ToString(), permCorpse))
            {
                NextFrame(looter.EndLooting);
                looter.ChatMessage(Lang("LootProtection", looter.UserIDString, corpse.playerName));
                return true;
            }

            if (sleeper != null && permission.UserHasPermission(sleeper.UserIDString, permSleeper))
            {
                NextFrame(looter.EndLooting);
                looter.ChatMessage(Lang("LootProtection", looter.UserIDString, sleeper.displayName));
                return true;
            }

            return null;
        }

        #endregion

        #region Helpers

        string Lang(string key, string id = null, params object[] args) => string.Format(lang.GetMessage(key, this, id), args);

        #endregion
    }
}
