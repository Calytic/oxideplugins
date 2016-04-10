using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("SuicideBomber", "Calytic @ cyclone.network", "0.0.2", ResourceId = 1425)]
    class SuicideBomber : RustPlugin
    {
        private float damage;
        private float radius;
        private int explosives;
        private int c4;
        private bool flare;
        private bool scream;

        private Dictionary<string, string> messages = new Dictionary<string, string>();

        private List<string> texts = new List<string>() {
            "You lack the required {0} timed explosives",
            "You lack the required {0} explosives",
            "You lack the required flare",
            "You will explode shortly..",
        };

        void OnServerInitialized()
        {
            damage = this.GetConfig<float>("damage", 1200f);
            radius = this.GetConfig<float>("radius", 12f);
            c4 = this.GetConfig<int>("c4", 1);
            explosives = this.GetConfig<int>("explosives", 10);
            scream = this.GetConfig<bool>("scream", true);
            flare = this.GetConfig<bool>("flare", true);

            Dictionary<string, object> customMessages = GetConfig<Dictionary<string, object>>("messages", null);
            if (customMessages != null)
            {
                foreach (KeyValuePair<string, object> kvp in customMessages)
                {
                    messages[kvp.Key] = kvp.Value.ToString();
                }
            }

            LoadData();
        }

        void LoadData()
        {
            if (this.Config["VERSION"] == null)
            {
                // FOR COMPATIBILITY WITH INITIAL VERSIONS WITHOUT VERSIONED CONFIG
                this.ReloadConfig();
            }
            else if (this.GetConfig<string>("VERSION", this.Version.ToString()) != this.Version.ToString())
            {
                // ADDS NEW, IF ANY, CONFIGURATION OPTIONS
                this.ReloadConfig();
            }
        }

        protected void ReloadConfig()
        {
            Dictionary<string, object> messages = new Dictionary<string, object>();

            foreach (string text in texts)
            {
                if (!messages.ContainsKey(text))
                {
                    messages.Add(text, text);
                }
            }

            Config["messages"] = messages;
            Config["VERSION"] = this.Version.ToString();

            // NEW CONFIGURATION OPTIONS HERE
            Config["c4"] = 1;
            Config["flare"] = true;
            // END NEW CONFIGURATION OPTIONS

            PrintWarning("Upgrading Configuration File");
            this.SaveConfig();
        }

        protected override void LoadDefaultConfig()
        {
            PrintWarning("Creating new configuration");
            Config.Clear();

            Dictionary<string, object> messages = new Dictionary<string, object>();

            foreach (string text in texts)
            {
                if (messages.ContainsKey(text))
                {
                    PrintWarning("Duplicate translation string: " + text);
                }
                else
                {
                    messages.Add(text, text);
                }
            }

            Config["messages"] = messages;
            Config["damage"] = 1200f;
            Config["radius"] = 12f;
            Config["explosives"] = 10;
            Config["c4"] = 1;
            Config["scream"] = true;
            Config["flare"] = true;
            Config["VERSION"] = this.Version.ToString();
        }

        private T GetConfig<T>(string name, T defaultValue)
        {
            if (Config[name] == null)
            {
                return defaultValue;
            }

            return (T)Convert.ChangeType(Config[name], typeof(T));
        }

        void OnPlayerInput(BasePlayer player, InputState input)
        {
            Item activeItem = player.GetActiveItem();

            if (activeItem != null && activeItem.info.shortname == "targeting.computer" && input.WasJustPressed(BUTTON.USE))
            {
                bool fail = false;
                if (c4 > 0)
                {
                    int c4_amount = player.inventory.GetAmount(498591726);
                    if (c4_amount < c4)
                    {
                        Effect.server.Run("assets/prefabs/locks/keypad/effects/lock.code.denied.prefab", player.transform.position);
                        SendReply(player, messages["You lack the required {0} timed explosives"], c4);
                        fail = true;
                    }
                }

                if (explosives > 0)
                {
                    int explosives_amount = player.inventory.GetAmount(1755466030);
                    if (explosives_amount < explosives)
                    {
                        Effect.server.Run("assets/prefabs/locks/keypad/effects/lock.code.denied.prefab", player.transform.position);
                        SendReply(player, messages["You lack the required {0} explosives"], explosives);
                        fail = true;
                    }
                }

                if (flare)
                {
                    int flare_amount = player.inventory.GetAmount(97513422);
                    if (flare_amount < 1)
                    {
                        Effect.server.Run("assets/prefabs/locks/keypad/effects/lock.code.denied.prefab", player.transform.position);
                        SendReply(player, messages["You lack the required flare"]);
                        fail = true;
                    }
                }

                if (fail)
                {
                    return;
                }
                else
                {
                    player.inventory.Take(null, 498591726, c4);
                    player.inventory.Take(null, 1755466030, explosives);
                    player.inventory.Take(null, 97513422, 1);
                }

                SendReply(player, messages["You will explode shortly.."]);

                activeItem.Remove(0f);
                activeItem.RemoveFromContainer();

                Effect.server.Run("assets/prefabs/locks/keypad/effects/lock.code.updated.prefab", player.transform.position);

                timer.Once(2f, delegate()
                {
                    Effect.server.Run("assets/prefabs/locks/keypad/effects/lock.code.lock.prefab", player.transform.position);
                });

                if (scream)
                {
                    timer.Once(3f, delegate()
                    {
                        Effect.server.Run("assets/bundled/prefabs/fx/player/beartrap_scream.prefab", player.transform.position);
                    });
                }

                timer.Once(4f, delegate()
                {
                    Effect.server.Run("assets/prefabs/locks/keypad/effects/lock.code.lock.prefab", player.transform.position);
                });

                timer.Once(6f, delegate()
                {
                    Effect.server.Run("assets/bundled/prefabs/fx/firebomb.prefab", player.transform.position);
                    Effect.server.Run("assets/bundled/prefabs/fx/gas_explosion_small.prefab", player.transform.position);

                    Effect.server.Run("assets/prefabs/npc/patrol helicopter/effects/rocket_explosion.prefab", player.transform.position);
                    Effect.server.Run("assets/prefabs/npc/patrol helicopter/effects/rocket_explosion.prefab", player.transform.position);
                    Effect.server.Run("assets/prefabs/npc/patrol helicopter/effects/rocket_explosion.prefab", player.transform.position);
                });

                timer.Once(6.2f, delegate() {
                    Effect.server.Run("assets/prefabs/tools/c4/effects/c4_explosion.prefab", player.transform.position);

                    List<BaseCombatEntity> entities = new List<BaseCombatEntity>();
                    Vis.Entities<BaseCombatEntity>(player.transform.position, radius/3, entities);

                    foreach (BaseCombatEntity e in entities)
                    {
                        e.Hurt(damage, global::Rust.DamageType.Explosion, player, true);
                    }

                    List<BaseCombatEntity> entities2 = new List<BaseCombatEntity>();
                    Vis.Entities<BaseCombatEntity>(player.transform.position, radius/2, entities2);

                    foreach (BaseCombatEntity e in entities2)
                    {
                        if (entities.Contains(e))
                        {
                            continue;
                        }
                        e.Hurt(damage/2, global::Rust.DamageType.Explosion, player, true);
                    }

                    List<BaseCombatEntity> entities3 = new List<BaseCombatEntity>();
                    Vis.Entities<BaseCombatEntity>(player.transform.position, radius, entities3);

                    foreach (BaseCombatEntity e in entities3)
                    {
                        if (entities.Contains(e) || entities2.Contains(e))
                        {
                            continue;
                        }

                        e.Hurt(damage/4, global::Rust.DamageType.Explosion, player, true);
                    }

                    if (player.net.connection.authLevel == 0)
                    {
                        player.Die();
                    }
                });

                timer.Once(6.4f, delegate()
                {
                    Effect.server.Run("assets/prefabs/tools/c4/effects/c4_explosion.prefab", player.transform.position);
                });
            }
        }
    }
}
