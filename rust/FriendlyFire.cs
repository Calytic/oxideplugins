using Oxide.Core;
using Oxide.Core.Libraries;
using Oxide.Core.Plugins;
using Rust;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Oxide.Plugins
{
    [Info("FriendlyFire", "playrust.io / dcode", "1.6.0", ResourceId = 840)]
    public class FriendlyFire : RustPlugin
    {

        #region Rust:IO Bindings

        private Library lib;
        private MethodInfo isInstalled;
        private MethodInfo hasFriend;
        private MethodInfo addFriend;
        private MethodInfo deleteFriend;

        private void InitializeRustIO() {
            lib = Interface.GetMod().GetLibrary<Library>("RustIO");
            if (lib == null || (isInstalled = lib.GetFunction("IsInstalled")) == null || (hasFriend = lib.GetFunction("HasFriend")) == null || (addFriend = lib.GetFunction("AddFriend")) == null || (deleteFriend = lib.GetFunction("DeleteFriend")) == null) {
                lib = null;
                Puts("{0}: {1}", Title, "Rust:IO is not present. You need to install Rust:IO first in order to use this plugin!");
            }
        }

        private bool IsInstalled() {
            if (lib == null) return false;
            return (bool)isInstalled.Invoke(lib, new object[] {});
        }

        private bool HasFriend(string playerId, string friendId) {
            if (lib == null) return false;
            return (bool)hasFriend.Invoke(lib, new object[] { playerId, friendId });
        }

        private bool AddFriend(string playerId, string friendId) {
            if (lib == null) return false;
            return (bool)addFriend.Invoke(lib, new object[] { playerId, friendId });
        }

        private bool DeleteFriend(string playerId, string friendId) {
            if (lib == null) return false;
            return (bool)deleteFriend.Invoke(lib, new object[] { playerId, friendId });
        }

        #endregion

        private List<ulong> manuallyEnabledBy = new List<ulong>();
        private List<ulong> apiBypassedFor = new List<ulong>();
        private List<string> texts = new List<string>() {
            "%NAME% is your friend and cannot be hurt. To disable this, unshare your location with %NAME% on the live map or type: <color=\"#ffd479\">/ff on</color>",

            "Usage: <color=\"#ffd479\">/ff [on|off]</color>",
            "Friendly fire is <color=#cd422b>enabled</color> for your friends:",
            "Friendly fire is <color=#8acd2b>disabled</color> for your friends:",
            "You do not have any friends currently.",
            "You may add or delete friends using the live map.",
            "To toggle friendly fire on or off, type: <color=\"#ffd479\">/ff on|off</color>",
            "Friendly fire for your friends is already <color=#cd422b>enabled</color>. Take care!",
            "You have <color=#cd422b>enabled</color> friendly fire for your friends. Take care!",
            "Friendly fire for your friends is already <color=#8acd2b>disabled</color>. They are safe!",
            "You have <color=#8acd2b>disabled</color> friendly fire for your friends. They are safe!",

            "<color=\"#ffd479\">/ff</color> - Displays your friendly fire status",
            "<color=\"#ffd479\">/ff on|off</color> - Toggles friendly fire <color=#cd422b>on</color> or <color=#8acd2b>off</color>"
        };
        private Dictionary<string, string> messages = new Dictionary<string, string>();
        private Dictionary<string, DateTime> notificationTimes = new Dictionary<string, DateTime>();

        // Translates a string
        private string _(string text, Dictionary<string, string> replacements = null) {
            if (messages.ContainsKey(text) && messages[text] != null)
                text = messages[text];
            if (replacements != null)
                foreach (var replacement in replacements)
                    text = text.Replace("%" + replacement.Key + "%", replacement.Value);
            return text;
        }


        // Loads the default configuration
        protected override void LoadDefaultConfig() {
            var messages = new Dictionary<string, object>();
            foreach (var text in texts) {
                if (messages.ContainsKey(text))
                    Puts("{0}: {1}", Title, "Duplicate translation string: " + text);
                else
                    messages.Add(text, text);
            }
            Config["messages"] = messages;
        }

        // Gets a config value of a specific type
        private T GetConfig<T>(string name, T defaultValue) {
            if (Config[name] == null)
                return defaultValue;
            return (T)Convert.ChangeType(Config[name], typeof(T));
        }

        [HookMethod("OnServerInitialized")]
        void OnServerInitialized() {
            try {
                InitializeRustIO();
                LoadConfig();
                var customMessages = GetConfig<Dictionary<string, object>>("messages", null);
                if (customMessages != null)
                    foreach (var pair in customMessages)
                        messages[pair.Key] = Convert.ToString(pair.Value);
            } catch (Exception ex) {
                Error("OnServerInitialized failed: " + ex.Message);
            }
        }

        private void RestoreDefaults(BasePlayer player) {
            manuallyEnabledBy.Remove(player.userID);
        }

        [HookMethod("OnPlayerInit")]
        void OnPlayerInit(BasePlayer player) {
            RestoreDefaults(player);
        }

        [HookMethod("OnPlayerDisconnected")]
        void OnPlayerDisconnected(BasePlayer player) {
            RestoreDefaults(player);
        }

        private object OnAttackShared(BasePlayer attacker, BasePlayer victim, HitInfo hit) {
            if (lib == null || attacker == victim)
                return null;
            if (manuallyEnabledBy.Contains(attacker.userID) || apiBypassedFor.Contains(attacker.userID))
                return null;
            var victimId = victim.userID.ToString();
            var attackerId = attacker.userID.ToString();
            if (!HasFriend(attackerId, victimId))
                return null;
            DateTime now = DateTime.UtcNow;
            DateTime time;
            var key = attackerId + "-" + victimId;
            if (!notificationTimes.TryGetValue(key, out time) || time < now.AddSeconds(-10)) {
                attacker.SendConsoleCommand("chat.add", "", _("%NAME% is your friend and cannot be hurt. To disable this, unshare your location with %NAME% on the live map or type: <color=\"#ffd479\">/ff on</color>", new Dictionary<string, string>() { { "NAME", victim.displayName } }));
                notificationTimes[key] = now;
            }
            // Clear the HitInfo (we don't want to rely on the return behavior because other plugins may cause conflicts)
            hit.damageTypes = new DamageTypeList();
            hit.DidHit = false;
            hit.HitEntity = null;
            hit.Initiator = null;
            hit.DoHitEffects = false;
            return false;
        }

        [HookMethod("OnPlayerAttack")]
        void OnPlayerAttack(BasePlayer attacker, HitInfo hit) {
            try {
                if (hit.HitEntity is BasePlayer)
                    OnAttackShared(attacker, hit.HitEntity as BasePlayer, hit);
            } catch (Exception ex) {
                Error("OnPlayerAttack failed: " + ex.Message);
            }
        }

        [HookMethod("OnEntityTakeDamage")]
        void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo hit) {
            try {
                if (entity is BasePlayer && hit.Initiator is BasePlayer)
                    OnAttackShared(hit.Initiator as BasePlayer, entity as BasePlayer, hit);
            } catch (Exception ex) {
                Error("OnEntityTakeDamage failed: " + ex.Message);
            }
        }

        [ChatCommand("ff")]
        private void cmdChatFF(BasePlayer player, string command, string[] args) {
            if (!IsInstalled())
                return;
            if (args.Length > 1) {
                SendReply(player, _("Usage: <color=\"#ffd479\">/ff [on|off]</color>"));
                return;
            }
            if (args.Length == 0) {
                var sb = new StringBuilder();
                int n = 0;
                sb.Append("<size=22>FriendlyFire</size> "+Version+" by <color=#ce422b>http://playrust.io</color>\n");
                if (manuallyEnabledBy.Contains(player.userID))
                    sb.Append(_("Friendly fire is <color=#cd422b>enabled</color> for your friends:")).Append("\n");
                else
                    sb.Append(_("Friendly fire is <color=#8acd2b>disabled</color> for your friends:")).Append("\n");
                var playerId = player.userID.ToString();
                foreach (var p in BasePlayer.activePlayerList) {
                    var pId = p.userID.ToString();
                    if (HasFriend(playerId, pId)) {
                        if (n > 0)
                            sb.Append(", ");
                        sb.Append(p.displayName);
                        ++n;
                    }
                }
                foreach (var p in BasePlayer.sleepingPlayerList) {
                    var pId = p.userID.ToString();
                    if (HasFriend(playerId, pId)) {
                        if (n > 0)
                            sb.Append(", ");
                        sb.Append(p.displayName);
                        ++n;
                    }
                }
                if (n == 0)
                    sb.Append(_("You do not have any friends currently."));
                sb.Append("\n").Append(_("You may add or delete friends using the live map."));
                sb.Append("\n").Append(_("To toggle friendly fire on or off, type: <color=\"#ffd479\">/ff on|off</color>"));
                SendReply(player, sb.ToString());
            } else if (args.Length == 1) {
                switch (args[0]) {
                    case "on":
                        if (manuallyEnabledBy.Contains(player.userID)) {
                            SendReply(player, _("Friendly fire for your friends is already <color=#cd422b>enabled</color>. Take care!"));
                        } else {
                            manuallyEnabledBy.Add(player.userID);
                            SendReply(player, _("You have <color=#cd422b>enabled</color> friendly fire for your friends. Take care!"));
                        }
                        break;
                    case "off":
                        if (!manuallyEnabledBy.Contains(player.userID)) {
                            SendReply(player, _("Friendly fire for your friends is already <color=#8acd2b>disabled</color>. They are safe!"));
                        } else {
                            manuallyEnabledBy.Remove(player.userID);
                            SendReply(player, _("You have <color=#8acd2b>disabled</color> friendly fire for your friends. They are safe!"));
                        }
                        break;
                    default:
                        SendReply(player, _("Usage: <color=\"#ffd479\">/ff [on|off]</color>"));
                        return;
                }
            }
        }

        [HookMethod("SendHelpText")]
        private void SendHelpText(BasePlayer player) {
            var sb = new StringBuilder()
               .Append("<size=18>FriendlyFire</size> by <color=#ce422b>http://playrust.io</color>\n")
               .Append("  ").Append(_("<color=\"#ffd479\">/ff</color> - Displays your friendly fire status")).Append("\n")
               .Append("  ").Append(_("<color=\"#ffd479\">/ff on|off</color> - Toggles friendly fire <color=#cd422b>on</color> or <color=#8acd2b>off</color>"));
            player.ChatMessage(sb.ToString());
        }

        [HookMethod("BuildServerTags")]
        private void BuildServerTags(IList<string> taglist) {
            taglist.Add("friendlyfire");
        }

        #region API Methods

        [HookMethod("EnableBypass")]
        private bool EnableBypass(object playerId) {
            if (playerId == null)
                throw new ArgumentException("playerId is null");
            if (playerId is string)
                playerId = Convert.ToUInt64((string)playerId);
            var uid = (ulong)playerId;
            if (!apiBypassedFor.Contains(uid)) {
                apiBypassedFor.Add(uid);
                return true;
            }
            return false;
        }

        [HookMethod("DisableBypass")]
        private bool DisableBypass(object playerId) {
            if (playerId == null)
                throw new ArgumentException("playerId is null");
            if (playerId is string)
                playerId = Convert.ToUInt64((string)playerId);
            var uid = (ulong)playerId;
            return apiBypassedFor.Remove(uid);
        }

        #endregion

        #region Utility Methods

        private void Log(string message) {
            Puts("{0}: {1}", Title, message);
        }

        private void Warn(string message) {
            PrintWarning("{0}: {1}", Title, message);
        }

        private void Error(string message) {
            PrintError("{0}: {1}", Title, message);
        }

        #endregion
    }
}
