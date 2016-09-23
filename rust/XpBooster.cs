using Rust.Xp;
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
    [Info("XpBooster", "Wulf/lukespragg", "0.6.0", ResourceId = 2001)]
    [Description("Multiplies the base XP players earn per source")]

    class XpBooster : RustPlugin
    {
        #region Initialization

        [PluginReference] Plugin AdminRadar;
        [PluginReference] Plugin Godmode;

        bool usePermissions;

        protected override void LoadDefaultConfig()
        {
            foreach (var def in Definitions.All) if (!def.Name.Contains("Cheat")) Config[def.Name] = GetConfig(def.Name, 1.0);
            Config["UsePermissions"] = usePermissions = GetConfig("UsePermissions", false);
            SaveConfig();
        }

        void Init()
        {
            LoadDefaultConfig();
            foreach (var def in Definitions.All)
                if (!def.Name.Contains("Cheat")) permission.RegisterPermission($"{Title}.{def.Name}".ToLower(), this);
        }

        #endregion

        #region XP Boosting

        object OnXpEarn(ulong steamId, double amount, string source)
        {
            if (string.IsNullOrEmpty(source) || source.Contains("Cheat")) return null;

            var id = steamId.ToString();
            if (Godmode && (bool)Godmode.Call("IsGod", id)) return null;
            if (AdminRadar && (bool)AdminRadar.Call("IsRadar", id)) return null;

            if (usePermissions && !permission.UserHasPermission(id, $"{Title}.{source}".ToLower())) return null;

            #if DEBUG
            PrintWarning($"Original amount: {amount}, Boosted amount: {amount * (double)Config[source]}");
            #endif

            return (float)(amount * (double)Config[source]);
        }

        #endregion

        #region Helpers

        T GetConfig<T>(string name, T value) => Config[name] == null ? value : (T)System.Convert.ChangeType(Config[name], typeof (T));

        string Lang(string key, string id = null, params object[] args) => string.Format(lang.GetMessage(key, this, id), args);

        #endregion
    }
}
