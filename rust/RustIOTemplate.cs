// Reference: Oxide.Ext.Rust
// Reference: Newtonsoft.Json
// Reference: UnityEngine

using Oxide.Core;
using Oxide.Core.Libraries;
using Oxide.Core.Plugins;
using System.Reflection;

namespace Oxide.Plugins
{
    [Info("Rust:IO Template", "Author name", "1.0.0", ResourceId = 123)]
    public class RustIOTemplate : RustPlugin
    {
        #region Rust:IO Bindings

        private Library lib;
        private MethodInfo isInstalled;
        private MethodInfo hasFriend;
        private MethodInfo addFriend;
        private MethodInfo deleteFriend;

        private void InitializeRustIO() {
            lib = Interface.GetMod().GetLibrary<Library>("RustIO");
            if (lib == null || (isInstalled = lib.GetFunction("IsInstalled")) == null || (hasFriend = lib.GetFunction("HasFriend")) == null || (addFriend = lib.GetFunction("AddFriend")) == null || (deleteFriend = lib.GetFunction("DeleteFriend")) == null) {
                lib = null;
                Puts("{0}: {1}", Title, "Rust:IO is not present. You need to install Rust:IO first in order to use this plugin!");
            }
        }

        private bool IsInstalled() {
            if (lib == null) return false;
            return (bool)isInstalled.Invoke(lib, new object[] {});
        }

        private bool HasFriend(string playerId, string friendId) {
            if (lib == null) return false;
            return (bool)hasFriend.Invoke(lib, new object[] { playerId, friendId });
        }

        private bool AddFriend(string playerId, string friendId) {
            if (lib == null) return false;
            return (bool)addFriend.Invoke(lib, new object[] { playerId, friendId });
        }

        private bool DeleteFriend(string playerId, string friendId) {
            if (lib == null) return false;
            return (bool)deleteFriend.Invoke(lib, new object[] { playerId, friendId });
        }

        #endregion

        [HookMethod("OnServerInitialized")]
        void OnServerInitialized() {
            InitializeRustIO();
        }
    }
}
