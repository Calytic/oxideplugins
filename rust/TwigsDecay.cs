using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Oxide.Core;

namespace Oxide.Plugins
{
    [Info("TwigsDecay", "Wulf/lukespragg/Nogrod", "2.0.0", ResourceId = 857)]
    class TwigsDecay : RustPlugin
    {
        private readonly FieldInfo decayTimer = typeof(DecayEntity).GetField("decayTimer", BindingFlags.Instance | BindingFlags.NonPublic);
        private readonly FieldInfo decayDelayTime = typeof(DecayEntity).GetField("decayDelayTime", BindingFlags.Instance | BindingFlags.NonPublic);
        private readonly Dictionary<string, float> damage = new Dictionary<string, float>();
        private readonly Dictionary<BuildingGrade.Enum, float> damageGrade = new Dictionary<BuildingGrade.Enum, float>();
        int timespan;
        bool ignoreAlivePlayers;
        bool ignoreDecayTimer;
        private readonly HashSet<string> blocks = new HashSet<string>();
        private readonly HashSet<ulong> activePlayers = new HashSet<ulong>();

        // A list of all translateable texts
        private readonly List<string> texts = new List<string>()
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

        private readonly Dictionary<string, string> messages = new Dictionary<string, string>();

        protected override void LoadDefaultConfig()
        {
            var damage = new Dictionary<string, object>() {
                {"Gate"     , 0}, // health: 2000
                {"Wall"     , 0}, // health: 2000
                {"Twigs"    , 1}, // health: 5
                {"Wood"     , 0}, // health: 250
                {"Stone"    , 0}, // health: 500
                {"Metal"    , 0}, // health: 200
                {"TopTier"  , 0}, // health: 1000
                {"Barricade", 0}, // health: 350, 400, 500
                {"Ladder"   , 0}  // health: 50
            };
            Config["ignoreAlivePlayers"] = true;
            Config["ignoreDecayTimer"] = false;
            Config["damage"] = damage;
            Config["timespan"] = 288;
            var blocks = new List<object>
            {
                //"block.halfheight",
                //"block.halfheight.slanted",
                "block.stair.lshape",
                "block.stair.ushape",
                "floor",
                "floor.triangle",
                "foundation",
                "foundation.steps",
                "foundation.triangle",
                "pillar",
                "roof",
                "wall",
                "wall.doorway",
                //"door.hinged",
                "wall.external.high.wood",
                "wall.external.high.stone",
                "wall.low",
                "wall.window",
                "wall.window.bars"
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
                foreach (var cfg in damageConfig)
                {
                    float val = (val = Convert.ToSingle(cfg.Value)) >= 0 ? val : 0;
                    try
                    {
                        var grade = (BuildingGrade.Enum)Enum.Parse(typeof (BuildingGrade.Enum), cfg.Key, false);
                        damageGrade.Add(grade, val);
                    }
                    catch (Exception)
                    {
                        damage.Add(cfg.Key, val);
                    }
                }
                timespan = Convert.ToInt32(Config["timespan"]);
                if (timespan < 0)
                    timespan = 15;
                ignoreAlivePlayers = Convert.ToBoolean(Config["ignoreAlivePlayers"]);
                ignoreDecayTimer = Convert.ToBoolean(Config["ignoreDecayTimer"]);
                var blocksConfig = (List<object>)Config["blocks"];
                foreach (var cfg in blocksConfig)
                    blocks.Add(Convert.ToString(cfg));
                var customMessages = (Dictionary<string, object>)Config["messages"];
                if (customMessages != null)
                    foreach (var pair in customMessages)
                        messages[pair.Key] = Convert.ToString(pair.Value);
                timer.Every(timespan*60, OnTimer);
            }
            catch (Exception ex)
            {
                PrintError("{0}: {1}", Title, "Failed to load configuration file: " + ex.Message);
            }
        }

        void OnTimer()
        {
            var started = Interface.Oxide.Now;
            int blocksDecayed = 0;
            int blocksDestroyed = 0;
            int barricadesDecayed = 0;
            int barricadesDestroyed = 0;
            int laddersDecayed = 0;
            int laddersDestroyed = 0;
            int gatesDecayed = 0;
            int gatesDestroyed = 0;
            int wallDecayed = 0;
            int wallDestroyed = 0;

            float barricadeAmount;
            damage.TryGetValue("Barricade", out barricadeAmount);
            float ladderAmount;
            damage.TryGetValue("Ladder", out ladderAmount);
            float gateAmount;
            damage.TryGetValue("Gate", out gateAmount);
            float wallAmount;
            damage.TryGetValue("Wall", out wallAmount);

            if (ignoreAlivePlayers)
            {
                activePlayers.Clear();
                foreach (var player in BasePlayer.activePlayerList)
                    activePlayers.Add(player.userID);
                foreach (var player in BasePlayer.sleepingPlayerList)
                    activePlayers.Add(player.userID);
            }

            var entities = BaseNetworkable.serverEntities.entityList.Values;
            var kill = new List<BaseNetworkable>();
            foreach (var entity in entities)
            {
                if (entity.isDestroyed) continue;
                if (entity is BuildingBlock)
                {
                    var block = (BuildingBlock) entity;
                    if (!blocks.Contains(Utility.GetFileNameWithoutExtension(block.PrefabName)))
                        continue;
                    float amount;
                    if (!damageGrade.TryGetValue(block.grade, out amount) || amount <= 0) continue;
                    ++blocksDecayed;
                    if (!decay(block, amount))
                    {
                        kill.Add(entity);
                        ++blocksDestroyed;
                    }
                } else if (entity is Barricade)
                {
                    if (barricadeAmount <= 0) continue;
                    ++barricadesDecayed;
                    if (!decay((Barricade) entity, barricadeAmount))
                    {
                        kill.Add(entity);
                        ++barricadesDestroyed;
                    }
                }
                else if (entity is BaseCombatEntity)
                {
                    var combat = (BaseCombatEntity)entity;
                    var prefab = Utility.GetFileNameWithoutExtension(combat.PrefabName);
                    if (ladderAmount > 0 && prefab.StartsWith("ladder"))
                    {
                        ++laddersDecayed;
                        if (!decay(combat, ladderAmount))
                        {
                            kill.Add(entity);
                            ++laddersDestroyed;
                        }
                    }
                    else if (gateAmount > 0 && prefab.StartsWith("gates.external"))
                    {
                        ++gatesDecayed;
                        if (!decay(combat, gateAmount))
                        {
                            kill.Add(entity);
                            ++gatesDestroyed;
                        }
                    }
                    else if (wallAmount > 0 && prefab.StartsWith("wall.external"))
                    {
                        ++wallDecayed;
                        if (!decay(combat, wallAmount))
                        {
                            kill.Add(entity);
                            ++wallDestroyed;
                        }
                    }
                }
            }
            foreach (var networkable in kill)
                networkable.KillMessage();

            Puts($"Decayed {blocksDecayed} blocks ({blocksDestroyed} destroyed), {barricadesDecayed} barricades ({barricadesDestroyed} destroyed) and {gatesDecayed} gates ({gatesDestroyed} destroyed) and {wallDecayed} walls ({wallDestroyed} destroyed) and {laddersDecayed} ladders ({laddersDestroyed} destroyed)");
            Puts("Took: {0}", Interface.Oxide.Now - started);
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
        private bool decay(BaseCombatEntity entity, float amount)
        {
            var decay = entity as DecayEntity;
            if (!ignoreDecayTimer && decay != null && (float)decayTimer.GetValue(decay) < (float)decayDelayTime.GetValue(decay)) return true;
            if (entity.OwnerID == 0 || ignoreAlivePlayers && activePlayers.Contains(entity.OwnerID)) return true;
            //if (decay != null && !decay.enabled) return true;
            entity.health -= amount;
            if (entity.health <= 0f)
                return false;
            entity.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);
            return true;
        }
    }
}
