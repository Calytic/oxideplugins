using System;
using System.Text;
using System.ComponentModel;
using System.Collections.Generic;
using System.Collections.Specialized;

using UnityEngine;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust;
using Oxide.Game.Rust.Cui;
using Oxide.Core.Libraries.Covalence;
//using Oxide.Game.Rust.Libraries.Covalence;

namespace Oxide.Plugins
{
    [Info("RustNotifications", "seanbyrne88", "0.7.1")]
    [Description("Configurable Notifications for Rust Events")]
    class RustNotifications : RustPlugin
    {

        [PluginReference]
        Plugin Slack;

        [PluginReference]
        Plugin Discord;

        private static NotificationConfigContainer Settings;

        private List<NotificationCooldown> UserLastNotified;

        private string SlackMethodName;
        private string DiscordMethodName;

        #region oxide methods
        void Init()
        {
            LoadConfigValues();
        }

        void OnPlayerInit(BasePlayer player)
        {
            SendPlayerConnectNotification(player);
        }

        void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            SendPlayerDisconnectNotification(player, reason);
        }

        void OnPlayerAttack(BasePlayer attacker, HitInfo info)
        {
            SendBaseAttackedNotification(attacker, info);
        }
        #endregion

        #region chat commands
        [ChatCommand("rustNotifyResetConfig")]
        void CommandResetConfig(BasePlayer player, string command, string[] args)
        {
            if (player.IsAdmin())
            {
                LoadDefaultConfig();
                LoadDefaultMessages();
                LoadConfigValues();
            }
        }

        #endregion

        #region private methods

        private string GetDisplayNameByID(ulong UserID)
        {
            IPlayer player = this.covalence.Players.FindPlayer(UserID.ToString());
            // BasePlayer player = BasePlayer.Find(UserID.ToString());
            if(player == null)
            {
                PrintWarning(String.Format("Tried to find player with ID {0} but they weren't in active or sleeping player list", UserID.ToString()));
                return "Unknown";
            }
            else
            {
                return player.Name;
            }
        }

        private bool IsPlayerActive(ulong UserID)
        {
            if (BasePlayer.activePlayerList.Exists(x => x.userID == UserID))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private bool IsPlayerNotificationCooledDown(ulong UserID, NotificationType NotificationType, int CooldownInSeconds)
        {
            if (UserLastNotified.Exists(x => x.NotificationType == NotificationType && x.PlayerID == UserID))
            {
                //check notification time per user, per notificationType, if it's cooled down send a message
                DateTime LastNotificationTime = UserLastNotified.Find(x => x.NotificationType == NotificationType && x.PlayerID == UserID).LastNotifiedAt;
                if ((DateTime.Now - LastNotificationTime).TotalSeconds > CooldownInSeconds)
                {
                    //SlackUserLastNotified[UserID] = DateTime.Now;
                    UserLastNotified.Find(x => x.NotificationType == NotificationType && x.PlayerID == UserID).LastNotifiedAt = DateTime.Now;
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                UserLastNotified.Add(new NotificationCooldown() { PlayerID = UserID, NotificationType = NotificationType, LastNotifiedAt = DateTime.Now });
                return true;
            }
        }
        #endregion

        #region notifications
        private void SendSlackNotification(BasePlayer player, string MessageText)
        {
            if (Settings.SlackConfig.Active)
            {
                Slack.Call(SlackMethodName, MessageText, BasePlayerToIPlayer(player));
            }
        }

        private void SendDiscordNotification(BasePlayer player, string MessageText)
        {
            if (Settings.DiscordConfig.Active)
            {
                Discord.Call(DiscordMethodName, MessageText);
            }
        }

        private IPlayer BasePlayerToIPlayer(BasePlayer player)
        {
            return covalence.Players.GetPlayer(player.UserIDString);
        }

        private void SendPlayerConnectNotification(BasePlayer player)
        {
            if (Settings.SlackConfig.DoNotifyWhenPlayerConnects)
            {
                //string MessageText = Lang("PlayerConnectedMessageTemplate", player.UserIDString).Replace("{DisplayName}", player.displayName);
                string MessageText = lang.GetMessage("PlayerConnectedMessageTemplate", this, player.UserIDString).Replace("{DisplayName}", player.displayName);
                SendSlackNotification(player, MessageText);
            }

            if (Settings.DiscordConfig.DoNotifyWhenPlayerConnects)
            {
                string MessageText = lang.GetMessage("PlayerConnectedMessageTemplate", this, player.UserIDString).Replace("{DisplayName}", player.displayName);
                SendDiscordNotification(player, MessageText);
            }
        }

        private void SendPlayerDisconnectNotification(BasePlayer player, string reason)
        {
            if (Settings.SlackConfig.DoNotifyWhenPlayerDisconnects)
            {
                string MessageText = lang.GetMessage("PlayerDisconnectedMessageTemplate", this, player.UserIDString).Replace("{DisplayName}", player.displayName).Replace("{Reason}", reason);
                SendSlackNotification(player, MessageText);
            }

            if (Settings.DiscordConfig.DoNotifyWhenPlayerDisconnects)
            {
                string MessageText = lang.GetMessage("PlayerDisconnectedMessageTemplate", this, player.UserIDString).Replace("{DisplayName}", player.displayName).Replace("{Reason}", reason);
                SendDiscordNotification(player, MessageText);
            }
        }

        private void SendBaseAttackedNotification(BasePlayer player, HitInfo info)
        {
            if (info.HitEntity != null)
            {
                //First check if the HitEntity is owned by a player.
                if (info.HitEntity.OwnerID != 0)
                {
                    string MessageText = lang.GetMessage("BaseAttackedMessageTemplate", this, player.UserIDString).Replace("{Attacker}", player.displayName).Replace("{Owner}", GetDisplayNameByID(info.HitEntity.OwnerID).Replace("{Damage}", info.damageTypes.Total().ToString()));
                    
                    if (IsPlayerActive(info.HitEntity.OwnerID) && IsPlayerNotificationCooledDown(info.HitEntity.OwnerID, NotificationType.ServerNotification, Settings.ServerConfig.NotificationCooldownInSeconds))
                    {
                        BasePlayer p = BasePlayer.activePlayerList.Find(x => x.userID == info.HitEntity.OwnerID);
                        PrintToChat(p, MessageText);
                    }
                    else
                    {
                        //Slack
                        if (Settings.SlackConfig.DoNotifyWhenBaseAttacked && IsPlayerNotificationCooledDown(info.HitEntity.OwnerID, NotificationType.SlackNotification, Settings.SlackConfig.NotificationCooldownInSeconds))
                        {
                            SendSlackNotification(player, MessageText);
                        }
                        //Discord
                        if (Settings.DiscordConfig.DoNotifyWhenBaseAttacked && IsPlayerNotificationCooledDown(info.HitEntity.OwnerID, NotificationType.DiscordNotification, Settings.DiscordConfig.NotificationCooldownInSeconds))
                        {
                            SendDiscordNotification(player, MessageText);
                        }
                    }
                }
            }
        }
        #endregion notifications

        #region localization
        private void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
                {
                    {"PlayerConnectedMessageTemplate", "{DisplayName} has joined the server"},
                    {"PlayerDisconnectedMessageTemplate", "{DisplayName} has left the server, reason: {Reason}"},
                    {"BaseAttackedMessageTemplate", "{Attacker} has attacked a structure built by {Owner}"},
                    {"TestMessage", "Hello World"}
                }, this);
        }
        #endregion

        #region config
        NotificationConfigContainer DefaultConfigContainer()
        {
            return new NotificationConfigContainer
            {
                ServerConfig = DefaultServerNotificationConfig(),
                SlackConfig = DefaultClientNotificationConfig(),
                DiscordConfig = DefaultClientNotificationConfig()
            };
        }

        ServerNotificationConfig DefaultServerNotificationConfig()
        {
            return new ServerNotificationConfig
            {
                Active = true,
                DoNotifyWhenBaseAttacked = true,
                NotificationCooldownInSeconds = 60
            };
        }

        ClientNotificationConfig DefaultClientNotificationConfig()
        {
            return new ClientNotificationConfig
            {
                DoLinkSteamProfile = true,
                Active = false,
                DoNotifyWhenPlayerConnects = true,
                DoNotifyWhenPlayerDisconnects = true,
                DoNotifyWhenBaseAttacked = true,
                NotificationCooldownInSeconds = 60
            };
        }

        protected override void LoadDefaultConfig()
        {
            Config.Clear();
            Config.WriteObject(DefaultConfigContainer(), true);

            PrintWarning("Default Configuration File Created");
            LoadDefaultMessages();
            PrintWarning("Default Language File Created");

            UserLastNotified = new List<NotificationCooldown>();
        }

        protected void LoadConfigValues()
        {
            Settings = Config.ReadObject<NotificationConfigContainer>();

            UserLastNotified = new List<NotificationCooldown>();

            if (Settings.SlackConfig.DoLinkSteamProfile)
                SlackMethodName = "FancyMessage";
            else
                SlackMethodName = "SimpleMessage";

            DiscordMethodName = "SendMessage";
        }
        #endregion


        #region classes
        private class ServerNotificationConfig
        {
            public bool Active { get; set; }
            public bool DoNotifyWhenBaseAttacked { get; set; }
            public int NotificationCooldownInSeconds { get; set; }
        }

        private class ClientNotificationConfig : ServerNotificationConfig
        {
            public bool DoLinkSteamProfile { get; set; }
            public bool DoNotifyWhenPlayerConnects { get; set; }
            public bool DoNotifyWhenPlayerDisconnects { get; set; }
        }
        
        private class NotificationConfigContainer
        {
            public ServerNotificationConfig ServerConfig { get; set; }
            public ClientNotificationConfig SlackConfig { get; set; }
            public ClientNotificationConfig DiscordConfig { get; set; }
        }

        private enum NotificationType
        {
            SlackNotification,
            DiscordNotification,
            ServerNotification
        }

        private class NotificationCooldown
        {
            public NotificationType NotificationType { get; set; }
            public ulong PlayerID { get; set; }
            public DateTime LastNotifiedAt { get; set; }
        }

        #endregion
    }
}

