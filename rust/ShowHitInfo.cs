using Rust;
using System;
using System.Collections.Generic;
using Oxide.Core;

namespace Oxide.Plugins
{

    [Info("ShowHitInfo", "nomizzz", "1.1.0")]
    [Description("Displays the amount of damage a player deals upon hitting another entity to the player's chat log.")]
    class ShowHitInfo : RustPlugin
    {

        HashSet<ulong> users = new HashSet<ulong>();
        void Unload() => SaveData();
        void OnServerSave() => SaveData();

        private void OnEntityTakeDamage(BaseCombatEntity victim, HitInfo hitInfo)
        {
            if (victim == null || hitInfo == null) return;
            DamageType type = hitInfo.damageTypes.GetMajorityDamageType();
            if (type == null) return;

            if (hitInfo?.Initiator != null && hitInfo?.Initiator?.ToPlayer() != null && users.Contains(hitInfo.Initiator.ToPlayer().userID))
            {
                // Need to actually retrieve detailed information on next server tick, because HitInfo will not have been scaled according to hitboxes, protection, etc until then:
                NextTick(() =>
                {
                    SendReply(
                      hitInfo?.Initiator?.ToPlayer(),
                      string.Format(GetMessage("showhitinfo_displaydamage", hitInfo?.Initiator?.ToPlayer().UserIDString), hitInfo.damageTypes.Total(), victim.Health())
                    );
                });
            }
        }

        void Loaded()
        {
            LoadSavedData();
            LoadPermissions();

            // Register all informational messages for localization
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["showhitinfo_unauthorized"] = "You are not authorized to use this command.",
                ["showhitinfo_disabled"] = "Damage you deal will no longer be logged to your chat window.",
                ["showhitinfo_enabled"] = "Damage you deal will now be logged to your chat window.",
                ["showhitinfo_displaydamage"] = "You did {0} damage, target now has {1}HP"
            }, this);
        }

        string GetMessage(string name, string sid = null)
        {
            return lang.GetMessage(name, this, sid);
        }

        void SaveData() => Interface.Oxide.DataFileSystem.WriteObject("ShowHitInfo", users);

        void LoadSavedData()
        {
            HashSet<ulong> users = Interface.Oxide.DataFileSystem.ReadObject<HashSet<ulong>>("ShowHitInfo");
            this.users = users;
        }

        void LoadPermissions()
        {
            permission.RegisterPermission("showhitinfo.enable", this);
        }

        bool IsAllowed(BasePlayer player)
        {
            if (permission.UserHasPermission(player.userID.ToString(), "showhitinfo.enable") || player.net.connection.authLevel == 2) return true;
            SendReply(player, GetMessage("showhitinfo_unauthorized", player.UserIDString));
            return false;
        }

        [ChatCommand("hitinfo")]
        void ToggleHitInfo(BasePlayer player, string cmd, string[] args)
        {
            if (!IsAllowed(player)) return;
            if (users.Contains(player.userID))
            {
                users.Remove(player.userID);
                SendReply(player, GetMessage("showhitinfo_disabled", player.UserIDString));
            }
            else
            {
                users.Add(player.userID);
                SendReply(player, GetMessage("showhitinfo_enabled", player.UserIDString));
            }
        }

    }
}
