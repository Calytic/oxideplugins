using System.Collections.Generic;
using UnityEngine;
using System;

namespace Oxide.Plugins
{
    [Info("WaterDisconnect", "Spicy", "1.1.1")]
    [Description("Kills players that log out underwater.")]

    class WaterDisconnect : RustPlugin
    {
        #region Initialisation

        void Init()
        {
            SetupConfiguration();

            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["LoggedOutUnderwater"] = "{0} [{1}] logged out underwater ({2}, {3}, {4})!",
                ["PlayerWasKilled"] = "{0} [{1}] was killed for logging out underwater.",
                ["PlayerWasDamaged"] = "{0} [{1}] was damaged for {2} for logging out underwater.",
                ["ConfigurationError"] = "Please enable only one option. Kill OR damage.",
                ["ChatBroadcastKilled"] = "{0} was killed because they logged out underwater.",
                ["ChatBroadcastDamaged"] = "{0} was damaged because they logged out underwater."
            }, this);

            if (kill && damage)
            {
                Puts(lang.GetMessage("ConfigurationError", this));
                return;
            }
        }

        #endregion

        #region Configuration

        bool kill;
        bool damage;
        int damageAmount;
        bool broadcastChat;

        protected override void LoadDefaultConfig()
        {
            Config["Settings"] = new Dictionary<string, object>
            {
                ["Kill"] = false,
                ["Damage"] = false,
                ["DamageAmount"] = 10,
                ["BroadcastChat"] = false
            };
        }

        void SetupConfiguration()
        {
            kill = Config.Get<bool>("Settings", "Kill");
            damage = Config.Get<bool>("Settings", "Damage");
            damageAmount = Config.Get<int>("Settings", "DamageAmount");
            broadcastChat = Config.Get<bool>("Settings", "BroadcastChat");
        }

        #endregion

        #region Server Hook

        void OnPlayerDisconnected(BasePlayer player)
        {
            if (player.IsHeadUnderwater())
            {
                Vector3 position = player.transform.position;

                Puts(string.Format(lang.GetMessage("LoggedOutUnderwater", this),
                    player.displayName, player.UserIDString,
                    Math.Floor(position.x), Math.Floor(position.y), Math.Floor(position.z)));

                if (kill && damage)
                {
                    Puts(lang.GetMessage("ConfigurationError", this));
                    return;
                }

                if (kill)
                {
                    player.Kill();
                    Puts(string.Format(lang.GetMessage("PlayerWasKilled", this),
                    player.displayName, player.UserIDString));
                    if (broadcastChat)
                        rust.BroadcastChat(string.Format(lang.GetMessage("ChatBroadcastKilled", this), player.displayName));
                    return;
                }

                if (damage)
                {
                    player.Hurt(damageAmount);
                    Puts(string.Format(lang.GetMessage("PlayerWasDamaged", this),
                    player.displayName, player.UserIDString, damageAmount));
                    if (broadcastChat)
                        rust.BroadcastChat(string.Format(lang.GetMessage("ChatBroadcastDamaged", this), player.displayName));
                    return;
                }
            }
        }

        #endregion
    }
}