ï»¿/*
TODO:
 - Automatically update configuration
 - Remove .prefab from block names in config
 - Add option for disabling decay entirely
 - Add control over individual blocks
*/

using System;
using System.Collections.Generic;
using System.Text;
using Rust;
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
    [Info("TwigsDecay", "Wulf/lukespragg", "1.5.9", ResourceId = 857)]
    [Description("")]

    class TwigsDecay : RustPlugin
    {
        Dictionary<string, int> damage = new Dictionary<string, int>();
        int timespan;
        DateTime lastUpdate = DateTime.Now;
        List<string> blocks = new List<string>();
        bool initialized = false;

        // A list of all translateable texts
        List<string> texts = new List<string>()
        {
            "Gate",
            "Twigs",
            "Wood",
            "Stone",
            "Metal",
            "TopTier",
            "Barricade",
            "Ladder",
            "%GRADE% buildings decay by %DAMAGE% HP per %TIMESPAN% minutes.",
            "%GRADE% buildings do not decay."
        };
        Dictionary<string, string> messages = new Dictionary<string, string>();

        protected override void LoadDefaultConfig()
        {
            var damage = new Dictionary<string, object>() {
                {"Gate"     , 0}, // health: 2000
                {"Twigs"    , 1}, // health: 5
                {"Wood"     , 0}, // health: 250
                {"Stone"    , 0}, // health: 500
                {"Metal"    , 0}, // health: 200
                {"TopTier"  , 0}, // health: 1000
                {"Barricade", 0}, // health: 350, 400, 500
                {"Ladder"   , 0}  // health: 50
            };
            Config["damage"] = damage;
            Config["timespan"] = 288;
            var blocks = new List<object>() {
                //"block.halfheight.prefab",
                //"block.halfheight.slanted.prefab",
                "block.stair.lshape.prefab",
                "block.stair.ushape.prefab",
                "floor.prefab",
                "floor.triangle.prefab",
                "foundation.prefab",
                "foundation.steps.prefab",
                "foundation.triangle.prefab",
                "pillar.prefab",
                "roof.prefab",
                "wall.prefab",
                "wall.doorway.prefab",
                //"door.hinged.prefab",
                "wall.external.high.wood.prefab",
                "wall.external.high.stone.prefab",
                "wall.low.prefab",
                "wall.window.prefab",
                "wall.window.bars.prefab"
            };
            Config["blocks"] = blocks;
            var messages = new Dictionary<string, object>();
            foreach (var text in texts)
            {
                if (messages.ContainsKey(text))
                    Puts("{0}: {1}", Title, "Duplicate translation string: " + text);
                else
                    messages.Add(text, text);
            }
            Config["messages"] = messages;
        }

        void Init()
        {
            if (ConVar.Decay.scale > 0f)
            {
                ConVar.Decay.scale = 0f;
                Puts("{0}: {1}", Title, "Default decay has been disabled");
            }
        }

        void OnServerInitialized()
        {
            LoadConfig();
            try
            {
                var damageConfig = (Dictionary<string, object>)Config["damage"];
                int val;
                foreach (var cfg in damageConfig)
                    damage.Add(cfg.Key, (val = Convert.ToInt32(cfg.Value)) >= 0 ? val : 0);
                timespan = Convert.ToInt32(Config["timespan"]);
                if (timespan < 0)
                    timespan = 15;
                var blocksConfig = (List<object>)Config["blocks"];
                foreach (var cfg in blocksConfig)
                    blocks.Add(Convert.ToString(cfg));
                initialized = true;
                var customMessages = (Dictionary<string, object>)Config["messages"];
                if (customMessages != null)
                    foreach (var pair in customMessages)
                        messages[pair.Key] = Convert.ToString(pair.Value);
            }
            catch (Exception ex)
            {
                PrintError("{0}: {1}", Title, "Failed to load configuration file: " + ex.Message);
            }
        }

        void OnTick()
        {
            if (!initialized)
                return;
            var now = DateTime.Now;
            if (lastUpdate > now.AddMinutes(-timespan))
                return;
            lastUpdate = now;
            int blocksDecayed = 0;
            int blocksDestroyed = 0;
            

            var allBlocks = UnityEngine.Object.FindObjectsOfType<BuildingBlock>();
            int amount;
            foreach (var block in allBlocks)
            {
                try
                {
                    string name = block.LookupShortPrefabName();
                    if (!blocks.Contains(name))
                        continue;

                    string grade = block.grade.ToString();
                    if (damage.TryGetValue(grade, out amount) && amount > 0)
                    {
                        ++blocksDecayed;
                        if (!decay(block, amount))
                            ++blocksDestroyed;
                    }
                        
                }
                catch
                {
                    continue;
                }
            }
            
            int barricadesDecayed = 0;
            int barricadesDestroyed = 0;
            if (damage.TryGetValue("Barricade", out amount) && amount > 0)
            {
                var allBarricades = UnityEngine.Object.FindObjectsOfType<Barricade>();
                foreach (var barricade in allBarricades)
                {
                    if (barricade.isDestroyed)
                        continue;
                    ++barricadesDecayed;
                    if (!decay(barricade, amount))
                        ++barricadesDestroyed;
                }
            }

            int laddersDecayed = 0;
            int laddersDestroyed = 0;
            if (damage.TryGetValue("Ladder", out amount) && amount > 0)
            {
                var allLadders = UnityEngine.Object.FindObjectsOfType<BaseCombatEntity>();
                foreach (var ladder in allLadders)
                {
                    if (ladder.isDestroyed || !ladder.LookupShortPrefabName().StartsWith("ladder"))
                        continue;
                    ++laddersDecayed;
                    if (!decay(ladder, amount))
                        ++laddersDestroyed;
                }
            }

            int gatesDecayed = 0;
            int gatesDestroyed = 0;
            if (damage.TryGetValue("Gate", out amount) && amount > 0)
            {
                var allGates = UnityEngine.Object.FindObjectsOfType<BaseCombatEntity>();
                foreach (var gate in allGates)
                {
                    if (gate.isDestroyed || !gate.LookupShortPrefabName().StartsWith("gates"))
                        continue;
                    ++gatesDecayed;
                    if (!decay(gate, amount))
                        ++gatesDestroyed;
                }
            }

            Puts("{0}: {1}", Title, "Decayed " +
                blocksDecayed + " blocks (" + blocksDestroyed + " destroyed), " +
                barricadesDecayed + " barricades (" + barricadesDestroyed + " destroyed) and " +
                gatesDecayed + " gates (" + gatesDestroyed + " destroyed) and " +
                laddersDecayed + " ladders (" + laddersDestroyed + " destroyed)"
            );
        }

        void SendHelpText(BasePlayer player)
        {
            var sb = new StringBuilder();
            foreach (var dmg in damage)
            {
                if (dmg.Value > 0)
                    sb.Append("  ").Append(_("%GRADE% buildings decay by %DAMAGE% HP per %TIMESPAN% minutes.", new Dictionary<string, string> {
                        { "GRADE", _(dmg.Key) },
                        { "DAMAGE", dmg.Value.ToString() },
                        { "TIMESPAN", timespan.ToString() }
                    })).Append("\n");
                else
                    sb.Append("  ").Append(_("%GRADE% buildings do not decay.", new Dictionary<string, string>() {
                        { "GRADE", _(dmg.Key) }
                    })).Append("\n");
            }
            player.ChatMessage(sb.ToString().TrimEnd());
        }

        // Translates a string
        string _(string text, Dictionary<string, string> replacements = null)
        {
            if (messages.ContainsKey(text) && messages[text] != null)
                text = messages[text];
            if (replacements != null)
                foreach (var replacement in replacements)
                    text = text.Replace("%" + replacement.Key + "%", replacement.Value);
            return text;
        }

        // Decays an entity, returns false if destroyed
        static bool decay(BaseCombatEntity entity, float amount)
        {
            entity.health -= amount;
            entity.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);
            if (entity.health <= 0f)
            {
                entity.Die();
                return false;
            }
            return true;
        }
    }
}
