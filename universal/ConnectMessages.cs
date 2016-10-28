using Oxide.Core.Libraries.Covalence;
using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("ConnectMessages", "Spicy", "1.0.1")]
    [Description("Provides connect and disconnect messages.")]

    class ConnectMessages : CovalencePlugin
    {
        #region Config

        private bool showConnectMessage;
        private bool showDisconnectMessage;
        private bool showDisconnectReason;

        private bool GetConfigValue(string key) => Config.Get<bool>("Settings", key);

        protected override void LoadDefaultConfig()
        {
            Config["Settings"] = new Dictionary<string, bool>
            {
                ["ShowConnectMessage"] = true,
                ["ShowDisconnectMessage"] = true,
                ["ShowDisconnectReason"] = false
            };
        }

        private void InitialiseConfig()
        {
            showConnectMessage = GetConfigValue("ShowConnectMessage");
            showDisconnectMessage = GetConfigValue("ShowDisconnectMessage");
            showDisconnectReason = GetConfigValue("ShowDisconnectReason");
        }

        #endregion

        #region Lang

        private string GetLangValue(string key, string userId) => lang.GetMessage(key, this, userId);

        private void InitialiseLang()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["ConnectMessage"] = "{0} has connected.",
                ["DisconnectMessage"] = "{0} has disconnected.",
                ["DisconnectMessageReason"] = "{0} has disconnected. ({1})"
            }, this);

            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["ConnectMessage"] = "{0} s'est connecté(e).",
                ["DisconnectMessage"] = "{0} s'est disconnecté(e).",
                ["DisconnectMessageReason"] = "{0} s'est disconnecté(e). ({1})"
            }, this, "fr");

            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["ConnectMessage"] = "{0} ha conectado.",
                ["DisconnectMessage"] = "{0} se ha desconectado.",
                ["DisconnectMessageReason"] = "{0} se ha desconectado. ({1})"
            }, this, "es");
        }

        #endregion

        #region Hooks

        private void OnServerInitialized()
        {
#if HURTWORLD
            GameManager.Instance.ServerConfig.ChatConnectionMessagesEnabled = false;
#endif

            InitialiseConfig();
            InitialiseLang();
        }

        private void OnUserConnected(IPlayer player)
        {
            if (!showConnectMessage)
                return;

            foreach (IPlayer _player in players.Connected)
                _player.Message(string.Format(GetLangValue("ConnectMessage", _player.Id), player.Name));
        }

        private void OnUserDisconnected(IPlayer player, string reason)
        {
            if (!showDisconnectMessage)
                return;

            foreach (IPlayer _player in players.Connected)
            {
                if (!showDisconnectReason)
                    _player.Message(string.Format(GetLangValue("DisconnectMessage", _player.Id), player.Name));
                else
                    _player.Message(string.Format(GetLangValue("DisconnectMessageReason", _player.Id), player.Name, reason));
            }
        }

        #endregion
    }
}
