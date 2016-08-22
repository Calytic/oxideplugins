using Oxide.Core.Plugins;
namespace Oxide.Plugins
{
    [Info("RespawnMessages", "Kappasaurus", 0.1)]
    [Description("Make customized notes players view on respawn!")]

    class RespawnMessages : RustPlugin
    {
        [PluginReference]
        Plugin PopupNotifications;
        void LoadDefaultConfig()
        {
            Config.Clear();
            Config["RespawnMessagePopup"] = "<size=20>Hey, <color=#cd422b>try not to die</color> this time!</size>";
            Config["RespawnMessageChat"] = "Hey, <color=#cd422b>try not to die</color> this time!";
            Config["Prefix"] = "[ <color=#cd422b>RespawnNotes</color> ]";
            Config["PopupEnabled"] = false;
            Config["ChatEnabled"] = true;
            Config["PrefixEnabled"] = true;
            Config.Save();
        }
        void OnPlayerRespawn(BasePlayer player)
        {
            if ((bool)Config["PrefixEnabled"] == true)
            {
                if ((bool)Config["PopupEnabled"] == true)
					{
						PopupNotifications?.Call("CreatePopupNotification", Config["RespawnMessagePopup"].ToString());
					}
                if ((bool)Config["ChatEnabled"] == true)
					{
						SendReply(player, Config["Prefix"].ToString() + " " + Config["RespawnMessageChat"].ToString());
					}
            }
            else

            {
                if ((bool)Config["PopupEnabled"] == true)
					{
						PopupNotifications?.Call("CreatePopupNotification", Config["RespawnMessagePopup"].ToString());
					}
                if ((bool)Config["ChatEnabled"] == true)
					{
							SendReply(player, Config["RespawnMessageChat"].ToString());
					}
            }
        }
    }
}
 