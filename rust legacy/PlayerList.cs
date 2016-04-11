using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Oxide.Plugins
{
    [Info("Player List", "Hatemail", 0.1, ResourceId = 70)]
    public class PlayerList : RustLegacyPlugin
    {

        private string chatName;
        private int maxPlayersPerLine;
        private string singlePlayerMessage;
        private bool adminOnly;
        private string onlineMessage;
        private int messageLength;

        private void Init()
        {

        }

        void Loaded()
        {
            chatName = Config.Get<string>("Settings", "ChatName");
            onlineMessage = Config.Get<string>("Settings", "OnlineMessage");
            singlePlayerMessage = Config.Get<string>("Settings", "SinglePlayerMessage");
            maxPlayersPerLine = Config.Get<int>("Settings", "PlayersPerLine");
            adminOnly = Config.Get<bool>("Settings", "AdminMode");
            messageLength = Config.Get<int>("Settings", "MaxLength");  
        }

        protected override void LoadDefaultConfig()
        {

            Config["Settings"] = new Dictionary<string, object> {
                { "ChatName", "Oxide" }, { "PlayersPerLine", 5}, { "SinglePlayerMessage", "You're the only one on you silly sap." },
                { "OnlineMessage", "{0}/50 Players Online" }, { "MaxLength", 80} , { "AdminMode", true}
            };
        }

        private void Unloaded()
        {

        }

        [ChatCommand("players")]
        void cmdPlayers(NetUser netUser, string command, string[] args)
        {
            if (PlayerClient.All.Count == 1)
            {
                rust.SendChatMessage(netUser, chatName, singlePlayerMessage);
            }
            else
            {
                if (adminOnly && netUser.CanAdmin())
                {
                    logAllOnlinePlayers(netUser);
                }
                else if (!adminOnly)
                {
                    logAllOnlinePlayers(netUser);
                }
                else
                {
                    rust.SendChatMessage(netUser, chatName, String.Format(onlineMessage, PlayerClient.All.Count, server.maxplayers));
                }
            }
           
        }

        private void logAllOnlinePlayers(NetUser netUser)
        {
            rust.SendChatMessage(netUser, chatName, string.Format(onlineMessage, PlayerClient.All.Count)); 
            var displayNames = PlayerClient.All.Select(pc => pc.netUser.displayName).ToList();
            displayNames.Sort();
            StringBuilder sb = new StringBuilder(messageLength + 25);
            int totalPlayersAdded = 0;
            for (int i = 0; i < displayNames.Count; i++)
            {
                if (totalPlayersAdded < maxPlayersPerLine && (sb.Length + displayNames[i].Length) < messageLength)
                {
                    sb.Append(displayNames[i]);
                    totalPlayersAdded += 1;
                }
                if (totalPlayersAdded == maxPlayersPerLine)
                {
                    rust.SendChatMessage(netUser, chatName, sb.ToString().TrimEnd(' ', ','));
                    sb.Length = 0;
                    totalPlayersAdded = 0;
                }
                else if ((sb.Length + displayNames[i].Length) >= messageLength)
                {
                    rust.SendChatMessage(netUser, chatName, sb.ToString().TrimEnd(' ', ','));
                    sb.Length = 0;
                    totalPlayersAdded = 1;
                    sb.Append(displayNames[i]);
                    sb.Append(", ");
                }
                else
                {
                    sb.Append(", ");
                }
            }
            if (sb.Length > 0)
            {
                rust.SendChatMessage(netUser, chatName, sb.ToString().TrimEnd(' ', ','));
            }
        }
    }
}
