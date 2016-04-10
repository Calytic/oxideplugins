using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("PlayersList", "Reneb", "1.0.3")]
    [Description("Shows a players list in the chat.")]

    class PlayersList : HurtworldPlugin
    {
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /// Configs
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        protected override void LoadDefaultConfig() { }

        private void CheckCfg<T>(string key, ref T var)
        {
            if (Config[key] is T)
                var = (T)Config[key];
            else
                Config[key] = var;
        }

        static string permissionPlayersList = "canplayerslist";
        static string headermsg = "==== Players List ====";
        static string footermsg = "====================";
        static int maxchar = 50;
        static string playercolor = "green";
        static string admincolor = "orange";

        void Init()
        {
            CheckCfg("Permission - Oxide Permissions", ref permissionPlayersList);
            CheckCfg("Message - Header", ref headermsg);
            CheckCfg("Message - Footer", ref footermsg);
            CheckCfg("Message - Player Color", ref playercolor);
            CheckCfg("Message - Admin Color", ref admincolor);
            CheckCfg("Message - Max characters per line", ref maxchar);
            SaveConfig();
        }

        bool HasAccess(PlayerSession session)
        {
            return true;
            /*if (session.IsAdmin) return true;
            return permission.UserHasPermission(session.SteamId.ToString(), permissionPlayersList);*/
        }

        [ChatCommand("players")]
        void cmdPlayers(PlayerSession session, string command, string[] args)
        {
            if (!HasAccess(session)) { hurt.SendChatMessage(session, "You don't have access to this command"); return; }

            var playerslist = new List<string> {string.Empty};
            hurt.SendChatMessage(session, headermsg);

            foreach (var pair in GameManager.Instance.GetSessions())
            {
                var tplayer = pair.Value;
                if (!tplayer.IsLoaded) continue;

                if (playerslist[playerslist.Count - 1].Length + tplayer.Name.Length > maxchar)
                    playerslist.Add(string.Empty);
                if (playerslist[playerslist.Count - 1] != string.Empty)
                    playerslist[playerslist.Count - 1] += ", ";

                playerslist[playerslist.Count - 1] += $"<color={(tplayer.IsAdmin ? admincolor : playercolor)}>{tplayer.Name}</color>";
            }
            foreach(var msg in playerslist) hurt.SendChatMessage(session, msg);

            hurt.SendChatMessage(session, footermsg);
        }
    }
}
