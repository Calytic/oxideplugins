namespace Oxide.Plugins
{
    [Info("HomeSigns", "Wulf/lukespragg", 0.1, ResourceId = 1455)]
    [Description("Allows players to only place signs where they can build.")]

    class HomeSigns : RustPlugin
    {
        #region Sign Blocking

        object CanBuild(Planner plan, Construction prefab)
        {
            var player = plan.ownerPlayer;
            if (!prefab.hierachyName.StartsWith("sign.") ||
                player.HasPlayerFlag(BasePlayer.PlayerFlags.HasBuildingPrivilege)) return null;
            PrintToChat(player, "Building is blocked!");
            return true;
        }

        #endregion
    }
}
