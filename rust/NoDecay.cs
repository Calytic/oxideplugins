using System;
using System.Collections.Generic;
using System.Text;
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
    [Info("NoDecay", "Deicide666ra/Piarb", "1.0.10", ResourceId = 1160)]
    [Description("Scales or disables decay of items")]

    class NoDecay : RustPlugin
    {
        private float c_twigMultiplier;
        private float c_woodMultiplier;
        private float c_stoneMultiplier;
        private float c_sheetMultiplier;
        private float c_armoredMultiplier;
        private float c_campfireMultiplier;
        private float c_reactivetargetMultiplier;
        private float c_highWoodWallMultiplier;
        private float c_highStoneWallMultiplier;
		private float c_fishtrapMultiplier;
		private float c_barricadeMultiplier;
		private float c_trapMultiplier;
		private float c_cupboard;
		private float c_sleepingbag;
		
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
			c_fishtrapMultiplier = Convert.ToSingle(GetConfigValue("Mutipliers", "fishtrapMultiplier", 0.0));
            c_reactivetargetMultiplier = Convert.ToSingle(GetConfigValue("Mutipliers", "reactivetargetMultiplier", 0.0));
			c_barricadeMultiplier = Convert.ToSingle(GetConfigValue("Mutipliers", "barricadesMultiplier", 0.0));
			c_trapMultiplier = Convert.ToSingle(GetConfigValue("Mutipliers", "trapMultiplier", 0.0));
			c_cupboard = Convert.ToSingle(GetConfigValue("Mutipliers", "cupboardMultiplier", 0.0));
			c_sleepingbag = Convert.ToSingle(GetConfigValue("Mutipliers", "sleepingbagMultiplier", 0.0));

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
                else if (entity.LookupShortPrefabName() == "cupboard.tool.deployed.prefab")
                {
                    var before = hitInfo.damageTypes.Get(Rust.DamageType.Decay);
                    hitInfo.damageTypes.Scale(Rust.DamageType.Decay, c_cupboard);

                    if (c_outputToRcon)
                        Puts($"Decay (cupboard) before: {before} after: {hitInfo.damageTypes.Get(Rust.DamageType.Decay)}");
                }								
				else if (entity.LookupShortPrefabName() == "sleepingbag_leather_deployed.prefab")
                {
                    var before = hitInfo.damageTypes.Get(Rust.DamageType.Decay);
                    hitInfo.damageTypes.Scale(Rust.DamageType.Decay, c_sleepingbag);

                    if (c_outputToRcon)
                        Puts($"Decay (sleeping bag) before: {before} after: {hitInfo.damageTypes.Get(Rust.DamageType.Decay)}");
                }								
				else if (entity.LookupShortPrefabName() == "beartrap.prefab")
                {
                    var before = hitInfo.damageTypes.Get(Rust.DamageType.Decay);
                    hitInfo.damageTypes.Scale(Rust.DamageType.Decay, c_trapMultiplier);

                    if (c_outputToRcon)
                        Puts($"Decay (beartrap) before: {before} after: {hitInfo.damageTypes.Get(Rust.DamageType.Decay)}");
                }												
				else if (entity.LookupShortPrefabName() == "landmine.prefab")
                {
                    var before = hitInfo.damageTypes.Get(Rust.DamageType.Decay);
                    hitInfo.damageTypes.Scale(Rust.DamageType.Decay, c_trapMultiplier);

                    if (c_outputToRcon)
                        Puts($"Decay (landmine) before: {before} after: {hitInfo.damageTypes.Get(Rust.DamageType.Decay)}");
                }																
				else if (entity.LookupShortPrefabName() == "spikes.floor.prefab")
                {
                    var before = hitInfo.damageTypes.Get(Rust.DamageType.Decay);
                    hitInfo.damageTypes.Scale(Rust.DamageType.Decay, c_trapMultiplier);

                    if (c_outputToRcon)
                        Puts($"Decay (spikes floor) before: {before} after: {hitInfo.damageTypes.Get(Rust.DamageType.Decay)}");
                }								
                else if (entity.LookupShortPrefabName() == "barricade.wood.prefab")
                {
                    var before = hitInfo.damageTypes.Get(Rust.DamageType.Decay);
                    hitInfo.damageTypes.Scale(Rust.DamageType.Decay, c_barricadeMultiplier);

                    if (c_outputToRcon)
                        Puts($"Decay (barricade wood) before: {before} after: {hitInfo.damageTypes.Get(Rust.DamageType.Decay)}");
                }				
                else if (entity.LookupShortPrefabName() == "barricade.metal.prefab")
                {
                    var before = hitInfo.damageTypes.Get(Rust.DamageType.Decay);
                    hitInfo.damageTypes.Scale(Rust.DamageType.Decay, c_barricadeMultiplier);

                    if (c_outputToRcon)
                        Puts($"Decay (barricade metal) before: {before} after: {hitInfo.damageTypes.Get(Rust.DamageType.Decay)}");
                }								
                else if (entity.LookupShortPrefabName() == "barricade.stone.prefab")
                {
                    var before = hitInfo.damageTypes.Get(Rust.DamageType.Decay);
                    hitInfo.damageTypes.Scale(Rust.DamageType.Decay, c_barricadeMultiplier);

                    if (c_outputToRcon)
                        Puts($"Decay (barricade stone) before: {before} after: {hitInfo.damageTypes.Get(Rust.DamageType.Decay)}");
                }								
                else if (entity.LookupShortPrefabName() == "barricade.woodwire.prefab")
                {
                    var before = hitInfo.damageTypes.Get(Rust.DamageType.Decay);
                    hitInfo.damageTypes.Scale(Rust.DamageType.Decay, c_barricadeMultiplier);

                    if (c_outputToRcon)
                        Puts($"Decay (barricade woodwire) before: {before} after: {hitInfo.damageTypes.Get(Rust.DamageType.Decay)}");
                }												
                else if (entity.LookupShortPrefabName() == "barricade.concrete.prefab")
                {
                    var before = hitInfo.damageTypes.Get(Rust.DamageType.Decay);
                    hitInfo.damageTypes.Scale(Rust.DamageType.Decay, c_barricadeMultiplier);

                    if (c_outputToRcon)
                        Puts($"Decay (barricade concrete) before: {before} after: {hitInfo.damageTypes.Get(Rust.DamageType.Decay)}");
                }												
                else if (entity.LookupShortPrefabName() == "barricade.sandbags.prefab")
                {
                    var before = hitInfo.damageTypes.Get(Rust.DamageType.Decay);
                    hitInfo.damageTypes.Scale(Rust.DamageType.Decay, c_barricadeMultiplier);

                    if (c_outputToRcon)
                        Puts($"Decay (barricade sandbags) before: {before} after: {hitInfo.damageTypes.Get(Rust.DamageType.Decay)}");
                }																
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
                else if (entity.LookupShortPrefabName() == "wall.external.high.stone.prefab")
                {
                    var before = hitInfo.damageTypes.Get(Rust.DamageType.Decay);
                    hitInfo.damageTypes.Scale(Rust.DamageType.Decay, c_highStoneWallMultiplier);

                    if (c_outputToRcon)
                        Puts($"Decay (high stone wall) before: {before} after: {hitInfo.damageTypes.Get(Rust.DamageType.Decay)}");
                }
                else if (entity.LookupShortPrefabName() == "wall.external.high.wood.prefab")
                {
                    var before = hitInfo.damageTypes.Get(Rust.DamageType.Decay);
                    hitInfo.damageTypes.Scale(Rust.DamageType.Decay, c_highWoodWallMultiplier);

                    if (c_outputToRcon)
                        Puts($"Decay (high wood wall) before: {before} after: {hitInfo.damageTypes.Get(Rust.DamageType.Decay)}");
                }
                else if (entity.LookupShortPrefabName() == "reactivetarget_deployed.prefab")
                {
                    var before = hitInfo.damageTypes.Get(Rust.DamageType.Decay);
                    hitInfo.damageTypes.Scale(Rust.DamageType.Decay, c_reactivetargetMultiplier);

                    if (c_outputToRcon)
                        Puts($"Decay (reactive target) before: {before} after: {hitInfo.damageTypes.Get(Rust.DamageType.Decay)}");
                }
                else if (entity.LookupShortPrefabName() == "mining.pumpjack.prefab")
                {
                    var before = hitInfo.damageTypes.Get(Rust.DamageType.Decay);
                    hitInfo.damageTypes.Scale(Rust.DamageType.Decay, 0.0f);

                    if (c_outputToRcon)
                        Puts($"Decay (pumpjack) before: {before} after: {hitInfo.damageTypes.Get(Rust.DamageType.Decay)}");
                }
				else if (entity.LookupShortPrefabName() == "survivalfishtrap.deployed.prefab")
                {
                    var before = hitInfo.damageTypes.Get(Rust.DamageType.Decay);
                    hitInfo.damageTypes.Scale(Rust.DamageType.Decay, c_fishtrapMultiplier);

                    if (c_outputToRcon)
                        Puts($"Decay (survival fish trap) before: {before} after: {hitInfo.damageTypes.Get(Rust.DamageType.Decay)}");
                }
                else if (block != null)
                    ProcessBuildingDamage(block, hitInfo);
                else
                    Puts($"Unsupported decaying entity detected: {entity.LookupShortPrefabName()} --- please notify author");
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
