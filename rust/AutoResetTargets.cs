namespace Oxide.Plugins
{
    [Info("Auto Reset Targets", "Dyceman/Dan", 1.00, ResourceId = 0)]
    [Description("Auto Reset Targets")]
    public class AutoResetTargets : RustPlugin
    {
        /*
        // Function OnEntityTakeDamage
        // PURPOSE: Resets the target that gets knocked down after [Configuration:ResetTime] seconds
        // RETURN: None
        */
        void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            if (entity is ReactiveTarget)
            {
                ReactiveTarget target = (ReactiveTarget)entity;

                if (target != null)
                {
                    if (target.IsKnockedDown() == true)
                    {
                        timer.Once(3f, () =>
                        {
                            target.SetFlag(BaseEntity.Flags.On, true);
                        });

                    }
                }
            }
        }
        
    }
}