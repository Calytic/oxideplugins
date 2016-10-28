using Oxide.Core.Libraries.Covalence;
using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("Position", "Spicy", "1.0.0")]
    [Description("Shows players their positions.")]

    class Position : CovalencePlugin
    {
        #region Helpers

        private string GetLangValue(string key, IPlayer player) => lang.GetMessage(key, this, player.Id);

        private bool Authorised(IPlayer player, string permissionName) => player.IsAdmin || permission.UserHasPermission(player.Id, permissionName);

        #endregion

        #region Lang

        private void InitialiseLang()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["Position"] = "Position: ({0}, {1}, {2})."
            }, this);

            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["Position"] = "Position: ({0}, {1}, {2})."
            }, this, "fr");

            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["Position"] = "Posición: ({0}, {1}, {2})."
            }, this, "es");
        }

        #endregion

        #region Hooks

        private void OnServerInitialized()
        {
            InitialiseLang();
            permission.RegisterPermission("position.use", this);
        }

        #endregion

        #region Commands

        [Command("position")]
        private void cmdPosition(IPlayer player, string command, string[] args)
        {
            if (!Authorised(player, "position.use"))
                return;

            float x, y, z;
            player.Position(out x, out y, out z);
            player.Reply(string.Format(GetLangValue("Position", player), x, y, z));
        }

        #endregion
    }
}
