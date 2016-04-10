using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using Oxide.Core;

namespace Oxide.Plugins
{
    [Info("Random Warps", "LaserHydra", "1.1.1", ResourceId = 1397)]
    [Description("Teleports you to a random location of a multi-location warp")]
    class RandomWarps : RustPlugin
    {
        string pluginColor = "#00FF8D";

        ////////////////////////////////////////////
        ///     Data Handling
        ////////////////////////////////////////////
        class Data
        {
            public Dictionary<string, List<Dictionary<char, float>>> warps = new Dictionary<string, List<Dictionary<char, float>>>();

            public Data()
            {
            }
        }

        Data data;

        void LoadData()
        {
            data = Interface.GetMod().DataFileSystem.ReadObject<Data>("RandomWarps_Data");
        }

        void SaveData()
        {
            Interface.GetMod().DataFileSystem.WriteObject("RandomWarps_Data", data);
        }

        ////////////////////////////////////////////
        ///     Get Random Position
        ////////////////////////////////////////////

        Vector3 GetRandom(List<Dictionary<char, float>> warpPositions)
        {
            int random = UnityEngine.Random.Range(0, warpPositions.Count - 1);
            Dictionary<char, float> pos = warpPositions[random];

            return new Vector3(pos['x'], pos['y'], pos['z']);
        }

        ////////////////////////////////////////////
        ///     On Plugin Loaded
        ////////////////////////////////////////////

        void Loaded()
        {
            if (!permission.PermissionExists("rwarp.admin")) permission.RegisterPermission("rwarp.admin", this);

            LoadData();
            LoadConfig();
        }

        ////////////////////////////////////////////
        ///     Config Handling
        ////////////////////////////////////////////

        void LoadConfig()
        {
            SetConfig("Settings", "Time until teleport", 20);

            SaveConfig();
        }

        protected override void LoadDefaultConfig()
        {
            Puts("Generating new configfile...");
        }

        ////////////////////////////////////////////
        ///     Chat Command
        ////////////////////////////////////////////

        [ChatCommand("rwarp")]
        void rWarp(BasePlayer player, string cmd, string[] args)
        {
            if (args.Length < 1)
            {
                if (data.warps.Keys.Count == 0)
                {
                    SendChatMessage(player, "rWarp", "There are no random warps set up!");
                    return;
                }

                SendChatMessage(player, "rWarp", "Warps:");

                foreach (string warp in data.warps.Keys)
                {
                    SendChatMessage(player, "/rwarp " + warp);
                }

                return;
            }

            if (args.Length == 1)
            {
                if (!data.warps.ContainsKey(args[0]))
                {
                    SendChatMessage(player, "rWarp", "Warp does not exist!");
                }
                else
                {
                    SendChatMessage(player, "rWarp", $"You will be teleported in {(int)Config["Settings", "Time until teleport"]} seconds");

                    timer.Once((int)Config["Settings", "Time until teleport"], () => {
                        Teleport(player, GetRandom(data.warps[args[0]]));
                        SendChatMessage(player, "rWarp", $"Teleported to random warp <color={pluginColor}>{args[0]}</color>");
                    });
                }

                return;
            }

            if (!IsAdmin(player)) return;

            string warpName = args[1];

            switch (args[0])
            {
                case "add":
                    if (args.Length != 2) return;
                    if (data.warps.ContainsKey(warpName))
                    {
                        Dictionary<char, float> position = new Dictionary<char, float>();
                        position.Add('x', player.transform.position.x);
                        position.Add('y', player.transform.position.y);
                        position.Add('z', player.transform.position.z);
                        data.warps[warpName].Add(position);
                        SendChatMessage(player, "rWarp", $"You have added a spot to warp <color={pluginColor}>{warpName}</color>");
                    }
                    else
                    {
                        List<Dictionary<char, float>> warpPositions = new List<Dictionary<char, float>>();
                        Dictionary<char, float> position = new Dictionary<char, float>();
                        position.Add('x', player.transform.position.x);
                        position.Add('y', player.transform.position.y);
                        position.Add('z', player.transform.position.z);

                        warpPositions.Add(position);

                        data.warps.Add(warpName, warpPositions);

                        SendChatMessage(player, "rWarp", $"You have added a spot to warp <color={pluginColor}>{warpName}</color>");
                    }

                    SaveData();

                    break;

                case "remove":
                    if (args.Length != 2) return;

                    if (data.warps.ContainsKey(warpName))
                    {
                        data.warps.Remove(warpName);
                        SendChatMessage(player, "rWarp", $"You have removed the random warp <color={pluginColor}>{warpName}</color>");
                    }
                    else
                        SendChatMessage(player, "rWarp", $"Could not remove random warp <color={pluginColor}>{warpName}</color>. Warp does not exist!");

                    SaveData();

                    break;

                default:
                    break;
            }
        }

        ////////////////////////////////////////////
        ///     Admin Check
        ////////////////////////////////////////////

        bool IsAdmin(BasePlayer player)
        {
            if (permission.UserHasPermission(player.UserIDString, "rwarp.admin")) return true;
            return false;
        }

        ////////////////////////////////////////////
        ///     Teleportation
        ////////////////////////////////////////////

        public void Teleport(BasePlayer player, Vector3 pos)
        {
            //  Thanks to mughisi's Teleportation plugin for this!
            player.ClientRPCPlayer(null, player, "StartLoading", null, null, null, null, null);
            player.SetPlayerFlag(BasePlayer.PlayerFlags.Sleeping, true);

            if (!BasePlayer.sleepingPlayerList.Contains(player))
                BasePlayer.sleepingPlayerList.Add(player);
                    
            player.MovePosition(pos);
            player.ClientRPCPlayer(null, player, "ForcePositionTo", pos, null, null, null, null);

            player.TransformChanged();
            player.SetPlayerFlag(BasePlayer.PlayerFlags.ReceivingSnapshot, true);
            player.UpdateNetworkGroup();

            player.SendNetworkUpdateImmediate(false);
            player.ClientRPCPlayer(null, player, "StartLoading", null, null, null, null, null);
            player.SendFullSnapshot();
        }

        ////////////////////////////////////////////
        ///     Player Finding
        ////////////////////////////////////////////

        BasePlayer GetPlayer(string searchedPlayer, BasePlayer executer, string prefix)
        {
            var foundPlayers =
                (from player in BasePlayer.activePlayerList
                 where player.displayName.ToLower().Contains(searchedPlayer.ToLower())
                 select player.displayName).ToList();

            switch (foundPlayers.Count)
            {
                case 0:
                    SendChatMessage(executer, prefix, "The Player can not be found.");
                    break;

                case 1:
                    return BasePlayer.Find(foundPlayers[0]);

                default:
                    string players = ListToString(foundPlayers, 0, ", ");
                    SendChatMessage(executer, prefix, "Multiple matching players found: \n" + players);
                    break;
            }

            return null;
        }

        ////////////////////////////////////////////
        ///     Converting
        ////////////////////////////////////////////

        string ListToString(List<string> list, int first, string seperator)
        {
            return String.Join(seperator, list.Skip(first).ToArray());
        }

        ////////////////////////////////////////////
        ///     Config Setup
        ////////////////////////////////////////////

        void SetConfig(string Arg1, object Arg2, object Arg3 = null, object Arg4 = null)
        {
            if (Arg4 == null)
            {
                Config[Arg1, Arg2.ToString()] = Config[Arg1, Arg2.ToString()] ?? Arg3;
            }
            else if (Arg3 == null)
            {
                Config[Arg1] = Config[Arg1] ?? Arg2;
            }
            else
            {
                Config[Arg1, Arg2.ToString(), Arg3.ToString()] = Config[Arg1, Arg2.ToString(), Arg3.ToString()] ?? Arg4;
            }
        }

        ////////////////////////////////////////////
        ///     Chat Handling
        ////////////////////////////////////////////

        void BroadcastChat(string prefix, string msg = null) => PrintToChat(msg == null ? prefix : $"<color={pluginColor}>" + prefix + "</color>: " + msg);

        void SendChatMessage(BasePlayer player, string prefix, string msg = null) => SendReply(player, msg == null ? prefix : $"<color={pluginColor}>" + prefix + "</color>: " + msg);
    }
}