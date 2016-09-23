using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("IPBlacklist", "Ankawi", "1.0.0")]
    [Description("Blacklist IP addresses from joining your server")]
    class IPBlacklist : CovalencePlugin
    {
        protected override void LoadDefaultConfig() => Config["Banned IPs"] = new List<object> { " ", " ", " " };
        void Init()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["BlacklistedIP"] = "You are not allowed to play on this server"

            }, this);
        }
        object CanUserLogin(string name, string id, string ip)
        {
            var bannedIPs = (List<object>)Config["Banned IPs"];
            return bannedIPs.Contains(ip) ? lang.GetMessage("BlacklistedIP", this, id) : null;
        }
    }
}