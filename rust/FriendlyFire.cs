using Oxide.Core;
using Oxide.Core.Plugins;

using Rust;

using System;
using System.Collections.Generic;
using System.Text;

using UnityEngine;

namespace Oxide.Plugins
{
    [Info(FF, "Nogrod", "2.0.1", ResourceId = 687)]
    public class FriendlyFire : RustPlugin
    {
        private const string FF = "FriendlyFire";
        private readonly HashSet<ulong> bypass = new HashSet<ulong>();
        private readonly Dictionary<string, int> times = new Dictionary<string, int>();
        private Dictionary<ulong, bool> ffData;
        private ConfigData configData;

        [PluginReference]
        private Plugin Friends;
        [PluginReference]
        private Plugin Clans;

        class ConfigData
        {
            public bool FriendlyFire { get; set; }
        }

        protected override void LoadDefaultConfig()
        {
            var config = new ConfigData
            {
                FriendlyFire = false
            };
            Config.WriteObject(config, true);
        }

        private string _(string msgId, BasePlayer player, params object[] args)
        {
            return string.Format(lang.GetMessage(msgId, this, player.UserIDString), args);
        }

        private bool FriendlyFireEnabled(ulong userId)
        {
            if (bypass.Contains(userId)) return true;
            bool enabled;
            return ffData.TryGetValue(userId, out enabled) && enabled;
        }

        private void EnableFriendlyFire(ulong userId)
        {
            ffData[userId] = true;
            Interface.Oxide.DataFileSystem.WriteObject(FF, ffData);
        }

        private void DisableFriendlyFire(ulong userId)
        {
            ffData[userId] = false;
            Interface.Oxide.DataFileSystem.WriteObject(FF, ffData);
        }

        private void Init()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                {"CannotHurt", "{0} is your friend and cannot be hurt. To disable, type: <color=\"#33cccc\">/ff on</color>"},
                {"Usage", "Usage: <color=\"#33cccc\">/ff [on|off]</color>"},
                {"FFFriends", "Friendly fire is {0} for your friends:"},
                {"FFNotAvailable", "Friendly fire is not available."},
                {"FFEnabled", "<color=red>enabled</color>"},
                {"FFDisabled", "<color=green>disabled</color>"},
                {"NoFriends", "You do not have any friends."},
                {"FFToggle", "To toggle friendly fire on or off, type: <color=\"#33cccc\">/ff on|off</color>"},
                {"FFAlready", "Friendly fire for your friends is already {0}."},
                {"FFChanged", "You have {0} friendly fire for your friends."},
                {"FFHelp1", "<color=\"#33cccc\">/ff</color> - Displays your friendly fire status"},
                {"FFHelp2", "<color=\"#33cccc\">/ff on|off</color> - Toggles friendly fire <color=red>on</color> or <color=green>off</color>"}
            }, this);
            configData = Config.ReadObject<ConfigData>();
            try
            {
                ffData = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, bool>>(FF);
            }
            catch
            {
                ffData = new Dictionary<ulong, bool>();
            }
        }

        private object OnAttackInternal(BasePlayer attacker, BasePlayer victim, HitInfo hit)
        {
            if (configData.FriendlyFire || attacker == victim || FriendlyFireEnabled(attacker.userID))
                return null;
            var victimId = victim.userID;
            var attackerId = attacker.userID;
            var hasFriend = (bool)(Friends?.CallHook("HasFriend", attackerId, victimId) ?? false);
            if (!hasFriend)
            {
                hasFriend = (bool)(Clans?.CallHook("HasFriend", attacker.UserIDString, victim.UserIDString) ?? false);
                if (!hasFriend)
                    return null;
            }
            var now = Facepunch.Math.Epoch.Current;
            int time;
            var key = $"{attackerId}-{victimId}";
            if (!times.TryGetValue(key, out time) || time < now)
            {
                PrintToChat(attacker, _("CannotHurt", attacker, victim.displayName));
                times[key] = now + 10;
            }
            hit.damageTypes = new DamageTypeList();
            hit.DidHit = false;
            hit.HitEntity = null;
            hit.Initiator = null;
            hit.DoHitEffects = false;
            hit.HitMaterial = 0;
            return false;
        }

        void OnPlayerAttack(BasePlayer attacker, HitInfo hitInfo)
        {
            if (hitInfo?.HitEntity is BasePlayer)
                OnAttackInternal(attacker, (BasePlayer) hitInfo.HitEntity, hitInfo);
        }

        void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo hitInfo)
        {
            if (entity is BasePlayer && hitInfo?.Initiator is BasePlayer)
                OnAttackInternal((BasePlayer) hitInfo.Initiator, (BasePlayer) entity, hitInfo);
        }

        [ConsoleCommand("ff.toggle")]
        private void ccmdFFToggle(ConsoleSystem.Arg arg)
        {
            if (arg.connection != null && (arg.Player() == null || !arg.Player().IsAdmin())) return;
            configData.FriendlyFire = !configData.FriendlyFire;
            Config.WriteObject(configData, true);
        }

        [ChatCommand("ff")]
        private void cmdChatFF(BasePlayer player, string command, string[] args)
        {
            if (configData.FriendlyFire)
            {
                SendReply(player, _("FFNotAvailable", player));
                return;
            }
            if (args.Length > 1)
            {
                SendReply(player, _("Usage", player));
                return;
            }
            if (args.Length == 0)
            {
                var sb = new StringBuilder();
                sb.AppendLine(_("FFFriends", player, _(FriendlyFireEnabled(player.userID) ? "FFEnabled" : "FFDisabled", player)));
                var friendList = (string[]) Friends?.CallHook("GetFriendList", player.userID);
                if (friendList != null && friendList.Length > 0)
                    sb.AppendLine(string.Join(", ", friendList));
                else
                    sb.AppendLine(_("NoFriends", player));
                sb.Append(_("FFToggle", player));
                SendReply(player, sb.ToString());
                return;
            }
            switch (args[0])
            {
                case "1":
                case "on":
                case "true":
                case "yes":
                    if (FriendlyFireEnabled(player.userID))
                        SendReply(player, _("FFAlready", player, _("FFEnabled", player)));
                    else
                    {
                        EnableFriendlyFire(player.userID);
                        SendReply(player, _("FFChanged", player, _("FFEnabled", player)));
                    }
                    break;
                case "0":
                case "off":
                case "false":
                case "no":
                    if (!FriendlyFireEnabled(player.userID))
                        SendReply(player, _("FFAlready", player, _("FFDisabled", player)));
                    else
                    {
                        DisableFriendlyFire(player.userID);
                        SendReply(player, _("FFChanged", player, _("FFDisabled", player)));
                    }
                    break;
                default:
                    SendReply(player, _("Usage", player));
                    break;
            }
        }

        private void SendHelpText(BasePlayer player)
        {
            var sb = new StringBuilder();
            sb.Append("  ").AppendLine(_("FFHelp1", player));
            sb.Append("  ").Append(_("FFHelp2", player));
            player.ChatMessage(sb.ToString());
        }

        #region API Methods

        private bool EnableBypass(object userId)
        {
            if (userId == null)
                throw new ArgumentException(nameof(userId));
            if (userId is string)
                userId = Convert.ToUInt64((string) userId);
            return bypass.Add((ulong) userId);
        }

        private bool DisableBypass(object userId)
        {
            if (userId == null)
                throw new ArgumentException(nameof(userId));
            if (userId is string)
                userId = Convert.ToUInt64((string) userId);
            return bypass.Remove((ulong) userId);
        }

        #endregion
    }
}
