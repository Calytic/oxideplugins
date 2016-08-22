using Oxide.Core.Plugins;
using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("Plugin name", "Author name", "1.0.0", ResourceId = 123)]
    public class Template : RustPlugin // rename this to how your plugin file is named
    {
        #region RustIO Bindings
        [PluginReference] Plugin RustIO;
        bool IO() => RustIO != null ? RustIO.Call<bool>("IsInstalled") : false;
        bool HasFriend(string playerId, string friendId) => RustIO != null ? RustIO.Call<bool>("HasFriend", playerId, friendId) : false;
        bool AddFriend(string playerId, string friendId) => RustIO != null ? RustIO.Call<bool>("AddFriend", playerId, friendId) : false;
        bool DeleteFriend(string playerId, string friendId) => RustIO != null ? RustIO.Call<bool>("DeleteFriend", playerId, friendId) : false;
        List<string> GetFriends(string playerId) => RustIO != null ? RustIO.Call<List<string>>("GetFriends", playerId) : new List<string>();
        #endregion

        [HookMethod("OnServerInitialized")]
        void OnServerInitialized() {
            if (!IO())
                PrintWarning("This plugin uses the Rust:IO API, but Rust:IO is not installed.");
        }
    }
}
