using Network;

namespace Oxide.Plugins
{
    [Info("BypassQueue", "Nogrod", "1.0.1", ResourceId = 1855)]
    class BypassQueue : RustPlugin
    {
        private const string Perm = "bypassqueue.allow";

        void OnServerInitialized()
        {
            permission.RegisterPermission(Perm, this);
        }

        object CanBypassQueue(Connection connection)
        {
            if (permission.UserHasPermission(connection.userid.ToString(), Perm))
                return true;
            return null;
        }
    }
}
