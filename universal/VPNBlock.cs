using System.Collections.Generic;
using System;
using System.Text.RegularExpressions;
using Oxide.Core;
using Oxide.Core.Libraries;
using Oxide.Core.Libraries.Covalence;
using Newtonsoft.Json;

namespace Oxide.Plugins
{
    [Info("VPNBlock", "Calytic", "0.0.3", ResourceId = 2115)]
    class VPNBlock : CovalencePlugin
    {
        List<string> allowedISPs = new List<string>();

        void Loaded()
        {
            LoadData();
            LoadMessages();
            permission.RegisterPermission("vpnblock.canvpn", this);
        }

        void LoadData()
        {
            allowedISPs = Interface.Oxide.DataFileSystem.ReadObject<List<string>>("vpnblock_allowedisp");
        }

        void SaveData()
        {
            Interface.Oxide.DataFileSystem.WriteObject("vpnblock_allowedisp", allowedISPs);
        }

        [Command("wisp")]
        void WhiteListISP(IPlayer player, string command, string[] args)
        {
            if (!IsAllowed(player)) return;

            if (args.Length == 0)
            {
                player.Reply(GetMsg("WISP Invalid", player.Id));
                return;
            }

            allowedISPs.Add(string.Join(" ", args));

            player.Reply(GetMsg("ISP Whitelisted", player.Id));
            SaveData();
        }

        void LoadMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                {"Unauthorized", "Unauthorized.  ISP/VPN not permitted"},
                {"Is Banned", "{0} is trying to connect from proxy VPN/ISP {1}"},
                {"ISP Whitelisted", "ISP Whitelisted"},
                {"WISP Invalid", "Syntax Invalid. /wisp [ISP NAME]"},
            }, this);
        }

        bool IsAllowed(IPlayer player)
        {
            if (player.IsAdmin) return true;
            return false;
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
                    foreach (string isp in allowedISPs)
                    {
                        if (playerispvpn.Contains(isp))
                        {
                            return;
                        }
                    }

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
