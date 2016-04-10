using System;
using System.Collections.Generic;
using System.Text;
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
    [Info("NoDecay", "Deicide666ra", "1.0.8", ResourceId = 1160)]
    class NoDecay : RustPlugin
    {
        private float c_twigMultiplier;
        private float c_woodMultiplier;
        private float c_stoneMultiplier;
        private float c_sheetMultiplier;
        private float c_armoredMultiplier;
        private float c_campfireMultiplier;
        private float c_highWoodWallMultiplier;
        private float c_highStoneWallMultiplier;

        private bool c_outputToRcon;

        private bool g_configChanged;

        void Loaded() => LoadConfigValues();
        protected override void LoadDefaultConfig() => Puts("New configuration file created.");

        void LoadConfigValues()
        {
            c_twigMultiplier = Convert.ToSingle(GetConfigValue("Mutipliers", "twigMultiplier", 1.0));
            c_woodMultiplier = Convert.ToSingle(GetConfigValue("Mutipliers", "woodMultiplier", 0.0));
            c_stoneMultiplier = Convert.ToSingle(GetConfigValue("Mutipliers", "stoneMultiplier", 0.0));
            c_sheetMultiplier = Convert.ToSingle(GetConfigValue("Mutipliers", "sheetMultiplier", 0.0));
            c_armoredMultiplier = Convert.ToSingle(GetConfigValue("Mutipliers", "armoredMultiplier", 0.0));
            c_campfireMultiplier = Convert.ToSingle(GetConfigValue("Mutipliers", "campfireMultiplier", 0.0));
            c_highWoodWallMultiplier = Convert.ToSingle(GetConfigValue("Mutipliers", "highWoodWallMultiplier", c_woodMultiplier));
            c_highStoneWallMultiplier = Convert.ToSingle(GetConfigValue("Mutipliers", "highStoneWallMultiplier", c_stoneMultiplier));
            c_outputToRcon = Convert.ToBoolean(GetConfigValue("Debug", "outputToRcon", false));

            if (g_configChanged)
            {
                Puts("Configuration file updated.");
                SaveConfig();
            }
        }

        object GetConfigValue(string category, string setting, object defaultValue)
        {
            var data = Config[category] as Dictionary<string, object>;
            object value;
            if (data == null)
            {
                data = new Dictionary<string, object>();
                Config[category] = data;
                g_configChanged = true;
            }

            if (data.TryGetValue(setting, out value)) return value;
            value = defaultValue;
            data[setting] = value;
            g_configChanged = true;
            return value;
        }

        [HookMethod("SendHelpText")]
        private void SendHelpText(BasePlayer player)
        {
            var sb = new StringBuilder();
            sb.Append("<color=yellow>NoDecay 1.0.3</color> Â· Controls decay\n");
            sb.Append("  Â· ").AppendLine($"twig={c_twigMultiplier} - campfire={c_campfireMultiplier}");
            sb.Append("  Â· ").Append($"wood ={ c_woodMultiplier} - stone ={ c_stoneMultiplier} - sheet ={ c_sheetMultiplier} - armored ={ c_armoredMultiplier}");
            player.ChatMessage(sb.ToString());
        }

        void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo hitInfo)
        {
            var tick = DateTime.Now;
            try
            {
                if (!hitInfo.damageTypes.Has(Rust.DamageType.Decay)) return;

                var block = entity as BuildingBlock;
                if (entity.LookupShortPrefabName() == "campfire.prefab")
                    ProcessCampfireDamage(hitInfo);
                else if (entity.LookupShortPrefabName() == "gates.external.high.stone.prefab")
                {
                    var before = hitInfo.damageTypes.Get(Rust.DamageType.Decay);
                    hitInfo.damageTypes.Scale(Rust.DamageType.Decay, c_highStoneWallMultiplier);

                    if (c_outputToRcon)
                        Puts($"Decay (high stone gate) before: {before} after: {hitInfo.damageTypes.Get(Rust.DamageType.Decay)}");
                }
                else if (entity.LookupShortPrefabName() == "gates.external.high.wood.prefab")
                {
                    var before = hitInfo.damageTypes.Get(Rust.DamageType.Decay);
                    hitInfo.damageTypes.Scale(Rust.DamageType.Decay, c_highWoodWallMultiplier);

                    if (c_outputToRcon)
                        Puts($"Decay (high wood gate) before: {before} after: {hitInfo.damageTypes.Get(Rust.DamageType.Decay)}");
                }
                else if (block != null)
                    ProcessBuildingDamage(block, hitInfo);
                //else
                    //Puts($"Unsupported decaying entity detected: {entity.LookupShortPrefabName()} --- please notify author");
            }
            finally
            {
                var ms = (DateTime.Now - tick).TotalMilliseconds;
                if (ms > 10) Puts($"NoDecay.OnEntityTakeDamage took {ms} ms to execute.");
            }
        }

        void ProcessCampfireDamage(HitInfo hitInfo)
        {
            var before = hitInfo.damageTypes.Get(Rust.DamageType.Decay);
            hitInfo.damageTypes.Scale(Rust.DamageType.Decay, c_campfireMultiplier);
            if (c_outputToRcon)
                Puts($"Decay campfire before: {before} after: {hitInfo.damageTypes.Get(Rust.DamageType.Decay)}");
        }

        void ProcessBuildingDamage(BuildingBlock block, HitInfo hitInfo)
        {
            var multiplier = 1.0f;
            var isHighWall = block.LookupShortPrefabName().Contains("wall.external");

            string type = "other";
            switch (block.grade)
            {
                case BuildingGrade.Enum.Twigs:
                    multiplier = c_twigMultiplier;
                    type = "twig";
                    break;
                case BuildingGrade.Enum.Wood:
                    if (isHighWall)
                    {
                        multiplier = c_highWoodWallMultiplier;
                        type = "high wood wall";
                    }
                    else
                    {
                        multiplier = c_woodMultiplier;
                        type = "wood";
                    }
                    break;
                case BuildingGrade.Enum.Stone:
                    if (isHighWall)
                    {
                        multiplier = c_highStoneWallMultiplier;
                        type = "high stone wall";
                    }
                    else
                    {
                        multiplier = c_stoneMultiplier;
                        type = "stone";
                    }
                    break;
                case BuildingGrade.Enum.Metal:
                    multiplier = c_sheetMultiplier;
                    type = "sheet";
                    break;
                case BuildingGrade.Enum.TopTier:
                    multiplier = c_armoredMultiplier;
                    type = "armored";
                    break;
            };

            var before = hitInfo.damageTypes.Get(Rust.DamageType.Decay);
            hitInfo.damageTypes.Scale(Rust.DamageType.Decay, multiplier);

            if (c_outputToRcon)
                Puts($"Decay ({type}) before: {before} after: {hitInfo.damageTypes.Get(Rust.DamageType.Decay)}");
        }
    }
}
