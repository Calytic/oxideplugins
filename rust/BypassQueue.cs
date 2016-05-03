using Network;

namespace Oxide.Plugins
{
    [Info("BypassQueue", "Nogrod", "1.0.0")]
    class BypassQueue : RustPlugin
    {
        private const string Perm = "bypassqueue.allow";

        void OnServerInitialized()
        {
            permission.RegisterPermission(Perm, this);
        }

        object OnBypassQueue(Connection connection)
        {
            if (permission.UserHasPermission(connection.userid.ToString(), Perm))
                return true;
            return null;
        }
    }
}
