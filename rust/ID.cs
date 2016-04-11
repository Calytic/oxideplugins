using System;
using System.Collections.Generic;
using System.Linq;
using Rust;
using Oxide;

namespace Oxide.Plugins
{
    [Info("ID Lookup", "Cheeze", "0.1")]
    [Description("Lookup a connected player's steamid")]
    class ID : RustPlugin
    {



        #region vclean command
        [ChatCommand("id")]
        void cmdID(BasePlayer player, string cmd, string[] args)
        {

                if (args.Length == 1)
                {
                    int n;
                string name = GetPlayer(args[0], player).displayName.ToString();
                string targetID = null;                
                        targetID = GetPlayer(args[0], player).userID.ToString();


                    SendReply(player, name + "'s ID = " + targetID);
                }
                else
                {
                    SendReply(player, "Incorrect syntax! /id {target}");
                }
        }

        #endregion

        #region Functions
        BasePlayer GetPlayer(string searchedPlayer, BasePlayer executer, string prefix = null)
        {
            BasePlayer targetPlayer = null;
            List<string> foundPlayers = new List<string>();
            string searchedLower = searchedPlayer.ToLower();

            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                if (player.displayName.ToLower().Contains(searchedLower)) foundPlayers.Add(player.displayName);
            }

            switch (foundPlayers.Count)
            {
                case 0:
                    SendChatMessage(executer, prefix, "The Player can not be found.");
                    break;

                case 1:
                    targetPlayer = BasePlayer.Find(foundPlayers[0]);
                    break;

                default:
                    string players = ListToString(foundPlayers, 0, ", ");
                    SendChatMessage(executer, prefix, "Multiple matching players found: \n" + players);
                    break;
            }

            return targetPlayer;
        }

        string ListToString(List<string> list, int first, string seperator)
        {
            return String.Join(seperator, list.Skip(first).ToArray());
        }

        void SendChatMessage(BasePlayer player, string prefix, string msg = null)
        {
            SendReply(player, msg == null ? prefix : "<color=#00FF8D>" + prefix + "</color>: " + msg);
        }
        #endregion

    }
}