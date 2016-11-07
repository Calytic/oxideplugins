using System;

namespace Oxide.Plugins
{
	[Info("SyringeDenerf", "ignignokt84", "0.1.3", ResourceId = 1809)]
	class SyringeDenerf : RustPlugin
	{
		private bool hasConfigChanged;
		float healAmount = 25f; // Instant heal amount
		float hotAmount = 10f; // Heal-over-time amount
		float hotTime = 10f; // Heal-over-time time
		
		object OnHealingItemUse(HeldEntity item, BasePlayer target)
		{
			if(item is MedicalTool && item.ShortPrefabName.Contains("syringe"))
			{
				target.health = target.health + healAmount;
				target.metabolism.ApplyChange(MetabolismAttribute.Type.HealthOverTime, hotAmount, hotTime);
				return true;
			}
			return null;
		}
		
		// Loaded
		void Loaded()
		{
			LoadConfig();
		}
		
		// loads default configuration
		protected override void LoadDefaultConfig()
		{
			Config.Clear();
			LoadConfig();
		}
		
		// loads config from file
		private void LoadConfig()
		{
			healAmount = Convert.ToSingle(GetConfig("Instant Heal Amount", healAmount));
			hotAmount = Convert.ToSingle(GetConfig("Heal-over-time Amount", hotAmount));
			hotTime = Convert.ToSingle(GetConfig("Heal-over-time Time", hotTime));
			
			if (!hasConfigChanged) return;
			SaveConfig();
			hasConfigChanged = false;
		}
		
		// get config options, or set to default value if not found
		private object GetConfig(string str, object defaultValue)
		{
			object value = Config[str];
			if (value == null)
			{
				value = defaultValue;
				Config[str] = value;
				hasConfigChanged = true;
			}
			return value;
		}
	}
}