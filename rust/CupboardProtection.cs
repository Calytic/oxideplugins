namespace Oxide.Plugins
{
    [Info("CupboardProtection", "Wulf/lukespragg", 0.1, ResourceId = 1390)]
    [Description("Makes cupboards invulnerable, unable to be destroyed.")]

    class CupboardProtection : RustPlugin
    {
        object OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            if (entity.name.Contains("cupboard")) return false;
            return null;
        }
    }
}
