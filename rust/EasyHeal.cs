using System.Collections.Generic;
using System.Reflection;
using System;
using System.Data;
using UnityEngine;
using Oxide.Core;

namespace Oxide.Plugins
{
    [Info("Easy Heal", "LaserHydra", "2.2.0", ResourceId = 984)]
    [Description("Heal yourself or others")]
    class EasyHeal : RustPlugin
    {
        [ChatCommand("healall")]
        void cmdHealAll(BasePlayer player)
        {
            if (!player.IsAdmin())
            {
                SendChatMessage(player, "HEAL", "You have no permission to use this command!");
                Puts(player.displayName + " got rejected from using /healall");

                return;
            }
            Puts(player.displayName + " used /healall");
            BroadcastChat("HEAL", "All players have been healed by <color=orange>" + player.displayName + "</color>!");
            foreach (BasePlayer current in BasePlayer.activePlayerList)
            {
                current.metabolism.hydration.value = 1000;
                current.metabolism.calories.value = 1000;
                current.InitializeHealth(100, 100);
            }
        }

        [ChatCommand("heal")]
        void cmdHeal(BasePlayer player, string cmd, string[] args)
        {
            if (!player.IsAdmin())
            {
                SendChatMessage(player, "HEAL", "You have no permission to use this command!");

                if (args.Length != 1)
                {
                    Puts(player.displayName + " got rejected from using /heal");
                    return;
                }

                if (args.Length == 1)
                {
                    Puts(player.displayName + " got rejected from using /heal " + args[0]);
                }

                return;
            }

            if (args.Length != 1)
            {
                player.metabolism.hydration.value = 1000;
                player.metabolism.calories.value = 1000;
                player.InitializeHealth(100, 100);
                SendChatMessage(player, "HEAL", "You have healed yourself!");
                Puts(player.displayName + " used /heal");
                return;
            }

            if (args.Length == 1)
            {
                Puts(player.displayName + " used /heal " + args[0]);
                string[] target;
                target = GetPlayer(args[0]);
                if (target.Length == 0)
                {
                    SendChatMessage(player, "HEAL", "No matching players found!");
                }

                if (target.Length > 1)
                {
                    SendChatMessage(player, "HEAL", "Multiple players found:");
                    string multipleUsers = "";
                    foreach (string matchingplayer in target)
                    {
                        if (multipleUsers == "")
                        {
                            multipleUsers = "<color=yellow>" + matchingplayer + "</color>";
                            continue;
                        }

                        if (multipleUsers != "")
                        {
                            multipleUsers = multipleUsers + ", " + "<color=yellow>" + matchingplayer + "</color>";
                        }

                    }
                    SendChatMessage(player, "HEAL", multipleUsers);
                }

                if (target.Length == 1)
                {
                    BasePlayer targetPlayer = BasePlayer.Find(target[0]);
                    targetPlayer.metabolism.hydration.value = 1000;
                    targetPlayer.metabolism.calories.value = 1000;
                    targetPlayer.InitializeHealth(100, 100);
                    if (targetPlayer == player)
                    {
                        SendChatMessage(player, "HEAL", "You have healed yourself!");
                    }
                    else if(targetPlayer != player)
                    {
                        SendChatMessage(targetPlayer, "HEAL", "You got healed by <color=orange>" + player.displayName + "</color>");
                        SendChatMessage(player, "HEAL", "You have healed <color=orange>" + targetPlayer.displayName + "</color>");
                    }
                }

            }
        }

        //--------------------------->   Player finding   <---------------------------//

        string[] GetPlayer(string searchedPlayer)
        {
            List<string> foundPlayers = new List<string>();
            string searchedLower = searchedPlayer.ToLower();
            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                string display = player.displayName;
                string displayLower = display.ToLower();

                if (!displayLower.Contains(searchedLower))
                {
                    continue;
                }
                if (displayLower.Contains(searchedLower))
                {
                    foundPlayers.Add(display);
                }
            }
            var matchingPlayers = foundPlayers.ToArray();
            return matchingPlayers;
        }

        //---------------------------->   Chat Sending   <----------------------------//

        void BroadcastChat(string prefix, string msg)
        {
            PrintToChat("<color=orange>" + prefix + "</color>: " + msg);
        }

        void SendChatMessage(BasePlayer player, string prefix, string msg)
        {
            SendReply(player, "<color=orange>" + prefix + "</color>: " + msg);
        }

        //---------------------------------------------------------------------------//
    }
}
