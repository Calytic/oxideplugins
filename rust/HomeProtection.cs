namespace Oxide.Plugins
{
    [Info("HomeProtection", "Wulf/lukespragg", 0.1, ResourceId = 1391)]
    [Description("Protects you and your home from intruders.")]

    class HomeProtection : RustPlugin
    {
        object OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            if ((entity is BuildingBlock) && (info.Initiator is BasePlayer))
                if (!info.Initiator.ToPlayer().CanBuild())
                    return false;

            if ((entity is BasePlayer))
                if (entity.ToPlayer().CanBuild())
                    return false;

            return null;
        }
    }
}
