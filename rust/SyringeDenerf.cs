
namespace Oxide.Plugins
{
	[Info("SyringeDenerf", "ignignokt84", "0.1.1", ResourceId = 1809)]
	class SyringeDenerf : RustPlugin
	{
		float healAmount = 25f; // Instant heal amount
		float hotAmount = 10f; // HealOverTime amount
		float hotTime = 10f; // HealOverTime time
		
		object OnHealingItemUse(HeldEntity item, BasePlayer target)
		{
			if(item is MedicalTool && item.LookupShortPrefabName().Contains("syringe"))
			{
				target.health = target.health + healAmount;
				target.metabolism.ApplyChange(MetabolismAttribute.Type.HealthOverTime, hotAmount, hotTime);
				return true;
			}
			return null;
		}
	}
}