// Reference: Newtonsoft.Json

using System.Collections.Generic;
using System;
using System.Reflection;
using System.Data;
using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Plugins;
using Oxide.Core.Libraries;
using Newtonsoft.Json;


namespace Oxide.Plugins
{
    [Info("VACKicker", "Reneb", "1.0.0")]
    class VACKicker : RustLegacyPlugin
    { 
        JsonSerializerSettings jsonsettings;
        static string apikey = "";
        static string reason = "You have too many VAC Bans to join this server";
        static int maxAllowed = 1;
        static bool shouldBan = true;
        void LoadDefaultConfig() { }

        private void CheckCfg<T>(string Key, ref T var)
        {
            if (Config[Key] is T)
                var = (T)Config[Key];
            else
                Config[Key] = var;
        }

        void Init()
        { 
            CheckCfg<string>("STEAM API Key: http://steamcommunity.com/dev/apikey", ref apikey);
            CheckCfg<string>("Messages: Too many VAC Bans", ref reason);
            CheckCfg<int>("Settings: Max VAC allowed", ref maxAllowed);
            CheckCfg<bool>("Settings: Ban", ref shouldBan);
            SaveConfig(); 
        }
        void Loaded()
        {
            jsonsettings = new JsonSerializerSettings();
            jsonsettings.Converters.Add(new KeyValuesConverter()); 
        }
        void OnPlayerConnected(NetUser netuser)
        {
            if(apikey != "")
            { 
                var url = string.Format("http://api.steampowered.com/ISteamUser/GetPlayerBans/v1/?key={0}&steamids={1}", apikey, netuser.playerClient.userID.ToString());
                Interface.GetMod().GetLibrary<WebRequests>("WebRequests").EnqueueGet(url, (code, response) =>
                {
                    if (code != 200) return;
                    var jsonresponse = JsonConvert.DeserializeObject<Dictionary<string, object>>(response, jsonsettings);
                    if (!(jsonresponse["players"] is List<object>)) return;
                    if (!(((List<object>)jsonresponse["players"])[0] is Dictionary<string, object>)) return;
                    var playerdata = ((List<object>)jsonresponse["players"])[0] as Dictionary<string, object>;
                    if (Convert.ToInt32(playerdata["NumberOfVACBans"]) < maxAllowed) return;
                    if(shouldBan)
                        if (!BanList.Contains(netuser.playerClient.userID))
                            BanList.Add(netuser.playerClient.userID, netuser.displayName, string.Format("{0} VAC Bans", playerdata["NumberOfVACBans"].ToString()));
                    netuser.Kick(NetError.Facepunch_Kick_RCON, true);
                }
            , this);
            }
        }
    }
}