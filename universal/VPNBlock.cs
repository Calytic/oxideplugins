using System.Collections.Generic;
using System;
using System.Text.RegularExpressions;
using Oxide.Core;
using Oxide.Core.Libraries;
using Oxide.Core.Libraries.Covalence;
using Newtonsoft.Json;

namespace Oxide.Plugins
{
    [Info("VPNBlock", "Calytic", "0.0.21", ResourceId = 2115)]
    class VPNBlock : CovalencePlugin
    {
        void Loaded()
        {
            LoadMessages();
            permission.RegisterPermission("vpnblock.canvpn", this);
        }

        void LoadMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                {"Unauthorized", "Unauthorized.  ISP/VPN not permitted"},
                {"Is Banned", "{0} is trying to connect from proxy VPN/ISP {1}"},
            }, this);
        }

        bool hasAccess(IPlayer player, string permissionname)
        {
            if (player.IsAdmin) return true;
            return permission.UserHasPermission(player.Id, permissionname);
        }

        void OnUserConnected(IPlayer player)
        {
            if (hasAccess(player, "vpnblock.canvpn"))
            {
                return;
            }

            string ip = IpAddress(player.Address);

            var url = string.Format("http://legacy.iphub.info/api.php?ip=" + ip + "&showtype=4");
            webrequest.EnqueueGet(url, (code, response) =>
            {
                if (code != 200 || string.IsNullOrEmpty(response))
                {
                    PrintError("Service temporarily offline");
                }
                else
                {
                    var jsonresponse = JsonConvert.DeserializeObject<Dictionary<string, object>>(response);
                    var playervpn = (jsonresponse["proxy"].ToString());
                    string msg = GetMsg("Unauthorized");
                    var playerispvpn = (jsonresponse["asn"].ToString());

                    if (playervpn == "1")
                    {
                        player.Kick(msg);
                        PrintWarning(GetMsg("Is Banned"), player.Name + "(" + player.Id + "/" + ip + ")", playerispvpn);
                    }
                }
            }, this);
        }

        string IpAddress(string ip)
        {
            return Regex.Replace(ip, @":{1}[0-9]{1}\d*", "");
        }

        string GetMsg(string key, object userID = null)
        {
            return lang.GetMessage(key, this, userID == null ? null : userID.ToString());
        }
    }
}
