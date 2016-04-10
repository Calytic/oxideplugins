namespace Oxide.Plugins
{
    [Info("Anti-Wounded", "SkinN", "2.0.0", ResourceId = 1045)]
    class AntiWounded : RustPlugin
    {
        private bool CanBeWounded(BasePlayer player, HitInfo info)
        {
            return false;
        }
    }
}