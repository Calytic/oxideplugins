
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Rust;
using UnityEngine;


namespace Oxide.Plugins
{
    [Info("Suicide Cooldown", "Mughisi", 1.0)]
    class SuicideCooldown : RustPlugin
    {
        public static SuicideCooldown Instance;

        private SuicideCooldownConfig config;

        public class SuicideCooldownConfig
        {
            public List<PermissionInfo> Permissions { get; set; }

            [JsonIgnore]
            public Dictionary<string, PermissionInfo> Cooldowns = new Dictionary<string, PermissionInfo>();

            public class PermissionInfo
            {
                public string Permission { get; set; }
                public int Cooldown { get; set; }
                public int Priority { get; set; }
            }

            public void Initialize()
            {
                foreach (var entry in Permissions)
                {
                    if (!entry.Permission.StartsWith("suicidecooldown."))
                        entry.Permission = $"suicidecooldown.{entry.Permission}";
                    Instance.permission.RegisterPermission(entry.Permission, Instance);
                    Cooldowns.Add(entry.Permission, entry);
                }
            }

            public int GetCooldown(BasePlayer player)
            {
                var permissionInfo = new PermissionInfo { Permission = "none", Priority = -100, Cooldown = 60 };
                var playerPermissions = Instance.permission.GetUserPermissions(player.UserIDString).Where(x => x.StartsWith("suicidecooldown."));

                foreach (var permission in playerPermissions)
                {
                    PermissionInfo perm;
                    if (!Cooldowns.TryGetValue(permission, out perm)) continue;
                    if (perm.Priority > permissionInfo.Priority) permissionInfo = perm;
                }

                return permissionInfo.Cooldown;
            }
        }

        private void Init()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                { "Cooldown", "You can't suicide again so quickly, remaining cooldown: {0} seconds." }
            }, this);
        }

        protected override void LoadConfig()
        {
            Instance = this;

            base.LoadConfig();
            config = Config.ReadObject<SuicideCooldownConfig>();
            config.Initialize();
        }

        protected override void LoadDefaultConfig()
        {
            config = new SuicideCooldownConfig
            {
                Permissions = new List<SuicideCooldownConfig.PermissionInfo>
                {
                    new SuicideCooldownConfig.PermissionInfo
                    {
                        Permission = "permission_one",
                        Priority = 1,
                        Cooldown = 30
                    },
                    new SuicideCooldownConfig.PermissionInfo
                    {
                        Permission = "permission_two",
                        Priority = 2,
                        Cooldown = 0
                    }
                }
            };
        }

        protected override void SaveConfig() => Config.WriteObject(config);

        [ChatCommand("kill")]
        private void KillCommandChat(BasePlayer player)
        {
            if (player.IsSpectating()) return;

            if (player.IsDead()) return;

            if (!player.CanSuicide())
            {
                SendReply(player, lang.GetMessage("Cooldown", this, player.UserIDString), GetCooldown(player));
            }
            else
            {
                MarkSuicide(player);
                player.Hurt(1000f, DamageType.Suicide, player, false);
            }
        }

        [ChatCommand("suicide")]
        private void SuicideCommandChat(BasePlayer player) => KillCommandChat(player);

        [ConsoleCommand("kill")]
        private void KillCommandConsole(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();

            if (!player) return;

            if (player.IsSpectating()) return;

            if (player.IsDead()) return;

            if (!player.CanSuicide())
            {
                SendReply(arg, lang.GetMessage("Cooldown", this, player.UserIDString), GetCooldown(player));
            }
            else
            {
                MarkSuicide(player);
                player.Hurt(1000f, DamageType.Suicide, player, false);
            }
        }

        [ConsoleCommand("suicide")]
        private void SuicideCommandConsole(ConsoleSystem.Arg arg) => KillCommandConsole(arg);

        private void MarkSuicide(BasePlayer player)
        {
            var cooldown = config.GetCooldown(player);
            player.nextSuicideTime = Time.realtimeSinceStartup + cooldown;
        }

        private int GetCooldown(BasePlayer player)
        {
            return (int)(player.nextSuicideTime - Time.realtimeSinceStartup);
        }
    }
}
