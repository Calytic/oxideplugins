using ConVar;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("XMasEvent", "Reynostrum", "1.0.2")]
    [Description("Allow admins to use Rust XMas Event.")]

    class XMasTime : RustPlugin
    {
        #region Init/Config
        bool status = false;
        int daycount = 1;
        string RustTime;
        float spawnRange => GetConfig("spawnRange", 50f);
        int giftsPerPlayer => GetConfig("giftsPerPlayer", 2);
        int NightsBetweenChristmas => GetConfig("NightsBetweenChristmas", 1);
        bool MessageOnChristmas => GetConfig("MessageOnChristmas", true);
        protected override void LoadDefaultConfig()
        {
            PrintWarning("Plugin is loading default configuration.");
            Config["spawnRange"] = spawnRange;
            Config["giftsPerPlayer"] = giftsPerPlayer;
            Config["NightsBetweenChristmas"] = NightsBetweenChristmas;
            Config["MessageOnChristmas"] = MessageOnChristmas;
            SaveConfig();
        }
        void Loaded()
        {
            lang.RegisterMessages(Messages, this);
            permission.RegisterPermission("xmastime.use", this);
            LoadDefaultConfig();
        }
        #endregion

        #region Functions
        private void OnTick()
        {
            if (status || NightsBetweenChristmas == 0) return;
            RustTime = TOD_Sky.Instance.Cycle.DateTime.ToString("HH:mm");
            if (RustTime == "23:59")
            {
                status = true;
                if (NightsBetweenChristmas == daycount)
                {
                    daycount = 1;
                    XMasFunction();
                }
                else daycount++;
                RustTimeCheckFunction();
            }
        }
        void XMasFunction()
        {
            XMas.spawnRange = spawnRange;
            XMas.giftsPerPlayer = giftsPerPlayer;
            GameManager gameManager = GameManager.server;
            Vector3 vector3 = new Vector3();
            Quaternion quaternion = new Quaternion();
            BaseEntity baseEntity = gameManager.CreateEntity("Assets/Prefabs/Misc/XMas/XMasRefill.prefab", vector3, quaternion, true);
            if (baseEntity) baseEntity.Spawn();
            if (MessageOnChristmas) PrintToChat(Lang("XMasMessage"));
        }
        void RustTimeCheckFunction()
        {
            timer.Once(5f, () =>
                {
                    RustTime = TOD_Sky.Instance.Cycle.DateTime.ToString("HH:mm");
                    if (RustTime != "23:59") status = false;
                    else RustTimeCheckFunction();
                });
        }
        #endregion

        #region Chat Commands
        [ChatCommand("xmas")]
        void XMasCall(BasePlayer player)
        {
            if (!player.IsAdmin() && !HasPermission(player, "xmastime.use"))
            {
                PrintToChat(player, Lang("NotAllowed", player.UserIDString));
                return;
            }
            XMasFunction();
        }
        #endregion

        #region Helpers
        T GetConfig<T>(string name, T defaultValue) => Config[name] == null ? defaultValue : (T)Convert.ChangeType(Config[name], typeof(T));
        string Lang(string key, string id = null, params object[] args) => string.Format(lang.GetMessage(key, this, id), args);
        bool HasPermission(BasePlayer player, string perm) => permission.UserHasPermission(player.UserIDString, perm);
        Dictionary<string, string> Messages = new Dictionary<string, string>
        {
            {"NotAllowed", "You are not allowed to use this command." },
            {"XMasMessage", "Â¡Merry Christmas!" }
        };
        #endregion
    }
}

