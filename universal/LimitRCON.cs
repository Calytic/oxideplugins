using System;
using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("LimitRCON", "Wulf/lukespragg", "0.1.0", ResourceId = 0)]
    [Description("Limits RCON access to specific IP addresses")]

    class LimitRCON : RustPlugin
    {
        #region Configuration

        List<object> ips;

        protected override void LoadDefaultConfig()
        {
            Config["AllowedIPs"] = ips = GetConfig("AllowedIPs", new List<object> { "127.0.0.1", "8.8.8.8" });
            SaveConfig();
        }

        void Init() => LoadDefaultConfig();

        #endregion

        object OnRconConnection(System.Net.IPEndPoint ipEndPoint)
        {
            return IsLocalIp(ipEndPoint.Address.ToString()) ? null : (!ips.Contains(ipEndPoint.Address.ToString()) ? (object)true : null);
        }

        #region Helpers

        T GetConfig<T>(string name, T defaultValue) => Config[name] == null ? defaultValue : (T)Convert.ChangeType(Config[name], typeof(T));

        static bool IsLocalIp(string ipAddress)
        {
            var split = ipAddress.Split(new[] { "." }, StringSplitOptions.RemoveEmptyEntries);
            var ip = new[] { int.Parse(split[0]), int.Parse(split[1]), int.Parse(split[2]), int.Parse(split[3]) };
            return ip[0] == 10 || ip[0] == 127 || (ip[0] == 192 && ip[1] == 168) || (ip[0] == 172 && (ip[1] >= 16 && ip[1] <= 31));
        }

        #endregion
    }
}
