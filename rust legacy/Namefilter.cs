using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("Namefilter", "UraniumRUST", 1.0)]
    [Description("Plugin automatically kick players with bad names.")]

    class Namefilter : RustLegacyPlugin
    {
        public static bool broadcast = true;
        public static string prefixo = "Oxide";
        public static List<object> block = new List<object>(){"admin, adm, shit, bitch"};
        
        void LoadDefaultConfig() { }
        private void CheckCfg<T>(string Key, ref T var)
        {
            if (Config[Key] is T)
                var = (T)Config[Key];
            else
                Config[Key] = var;
        }
           
        void LoadDefaultMessages()
        {
            var messages = new Dictionary<string, string>
            {
                {"InvalidNickName", "{0}[color red] was kicked (reserved or inappropriate nick)"}
            };
            lang.RegisterMessages(messages, this);
        }
        
        void Loaded()
        {			
            LoadDefaultMessages();
            permission.RegisterPermission("namefilter.allowed", this);
        }
        
        void Init()
        {
			CheckCfg<bool>("Enable or Disable Kick Message", ref broadcast);
			CheckCfg<string>("Chat Prefix", ref prefixo);
			CheckCfg<List<object>>("Blocked Names (needs to be lowercase)", ref block);
            SaveConfig();
        }
        
        bool hasAccess(NetUser netuser, string permissionname)
        {
            if (netuser.CanAdmin()) return true;
			if (permission.UserHasPermission(netuser.playerClient.userID.ToString(), "namefilter.allowed")) return true;
            return permission.UserHasPermission(netuser.playerClient.userID.ToString(), permissionname);
        }
        
            
        void OnPlayerConnected(NetUser netuser, ConsoleSystem.Arg arg)
        {
        if (hasAccess(netuser, "namefilter.allowed")) { return; }          
        var name = netuser.displayName.ToLower();
        var namenormal = netuser.displayName;
     
            foreach(string value in block){
                if(name.Contains(value)){
                    if(broadcast) { rust.SendChatMessage(netuser, prefixo, string.Format(GetMessage("InvalidNickName", netuser.userID.ToString()), name)); }
                    netuser.Kick(NetError.Facepunch_Kick_RCON, true);
                    return;
                }
            }
        }
        string GetMessage(string key, string steamId = null) => lang.GetMessage(key, this, steamId);
    }
}