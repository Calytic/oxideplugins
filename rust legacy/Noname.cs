using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("Noname", "UraniumRUST", 1.1, ResourceId = 1607)])]
    [Description("Plugin automatically kick players with less than 4 characters in the name")]
    public class Noname : RustLegacyPlugin
    {
        public static bool broadcast = true;
        public static string prefixo = "Oxide";
    
        void LoadDefaultMessages()
        {
            var messages = new Dictionary<string, string>
            {
                {"MessageNoName", "Player without nick[COLOR RED] was kicked from the server."},
                {"MessageSmallName", "{0}[COLOR RED] was kicked from the server (very small nick)."}
            };
            lang.RegisterMessages(messages, this);
        }
        
        void LoadDefaultConfig() { }
        private void CheckCfg<T>(string Key, ref T var)
        {
            if (Config[Key] is T)
                var = (T)Config[Key];
            else
                Config[Key] = var;
        }
        
        void Loaded() 
        {
            LoadDefaultMessages();
            permission.RegisterPermission("noname.allowed", this);
        }
        
        void Init()
        {
			CheckCfg<bool>("Broadcast", ref broadcast);
			CheckCfg<string>("Chat Prefix", ref prefixo);
            SaveConfig();
        }
        
        bool hasAccess(NetUser netuser, string permissionname)
        {
            if (netuser.CanAdmin()) return true;
			if (permission.UserHasPermission(netuser.playerClient.userID.ToString(), "noname.allowed")) return true;
            return permission.UserHasPermission(netuser.playerClient.userID.ToString(), permissionname);
        }
        
        void OnPlayerConnected(NetUser netuser, ConsoleSystem.Arg arg)
        {
            if (hasAccess(netuser, "noname.allowed")) { return; }    
            var name = netuser.displayName;
            if (name.Length < 4)
            {
                if (string.IsNullOrEmpty(name))
                {
                   if(broadcast)
                    { 
                        rust.SendChatMessage(netuser, prefixo, GetMessage("MessageNoName", netuser.userID.ToString()));
                        netuser.Kick(NetError.Facepunch_Kick_RCON, true);
                        return;
                    }
                    else
                    {
                        netuser.Kick(NetError.Facepunch_Kick_RCON, true);
                        return;
                    }
                }
                  if(broadcast)
                    {
                        rust.SendChatMessage(netuser, prefixo, string.Format(GetMessage("MessageSmallName", netuser.userID.ToString()), name));
                        netuser.Kick(NetError.Facepunch_Kick_RCON, true);
                        return;
                    }
                        else
                    {
                     netuser.Kick(NetError.Facepunch_Kick_RCON, true);
                     return;
                    }
            }  
        }
        string GetMessage(string key, string steamId = null) => lang.GetMessage(key, this, steamId);
    }
}