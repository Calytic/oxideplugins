using System.Collections.Generic;
using System.Reflection;
using System;
using System.Data;
using UnityEngine;
using Oxide.Core.Plugins;
using Oxide.Core;
using System.Linq;
using Rust;
using Facepunch;

namespace Oxide.Plugins
{
    [Info("Portals", "LaserHydra", "1.3.03", ResourceId = 1234)]
    [Description("Create Portals and feel like in StarTrek")]
    class Portals : RustPlugin
    {
        class StoredData
        {
            public Dictionary<string, object> PortalsBackup = new Dictionary<string, object>();

            public StoredData()
            {
            }
        }

        StoredData storedData;

        void Loaded()
        {
            LoadDefaultConfig();
            storedData = Interface.GetMod().DataFileSystem.ReadObject<StoredData>("PortalsBackup");
            timer.Repeat(Convert.ToInt32(Config["TeleportTimer"]), 0, Portal);
            if(Config["Effects", "Enabled"].ToString() == "true") timer.Repeat(1.5F, 0, PortalFX);
            if(!permission.PermissionExists("portals.admin")) permission.RegisterPermission("portals.admin", this);

            foreach (var portal in Config)
            {
                string portalName = portal.Key.ToString();
                if(portalName == "TeleportTimer" || portalName == "Effects" ) continue;

                if(!permission.PermissionExists(Config[portalName, "Permission"].ToString())) permission.RegisterPermission(Config[portalName, "Permission"].ToString(), this);
            }
        }

        protected override void LoadDefaultConfig()
        {
            if(Config["PresetPortal", "EntranceX"] == null) Config["PresetPortal", "EntranceX"] = 0;
            if(Convert.ToInt32(Config["PresetPortal", "EntranceX"]) != 0) return;

            if(Config["PresetPortal", "EntranceY"] == null) Config["PresetPortal", "EntranceY"] = 0;
            if(Convert.ToInt32(Config["PresetPortal", "EntranceY"]) != 0) return;

            if(Config["PresetPortal", "EntranceZ"] == null) Config["PresetPortal", "EntranceZ"] = 0;
            if(Convert.ToInt32(Config["PresetPortal", "EntranceZ"]) != 0) return;

            if(Config["PresetPortal", "ExitX"] == null) Config["PresetPortal", "ExitX"] = 0;
            if(Convert.ToInt32(Config["PresetPortal", "ExitX"]) != 0) return;

            if(Config["PresetPortal", "ExitY"] == null) Config["PresetPortal", "ExitY"] = 10;
            if(Convert.ToInt32(Config["PresetPortal", "ExitY"]) != 10) return;

            if(Config["PresetPortal", "ExitZ"] == null) Config["PresetPortal", "ExitZ"] = 0;
            if(Convert.ToInt32(Config["PresetPortal", "ExitZ"]) != 0) return;

            if(Config["PresetPortal", "OneWay"] == null) Config["PresetPortal", "OneWay"] = "false";
            if(Config["PresetPortal", "OneWay"].ToString() != "false") return;

            if(Config["PresetPortal", "Radius"] == null) Config["PresetPortal", "Radius"] = 2;
            if(Convert.ToInt32(Config["PresetPortal", "Radius"]) != 2) return;

            if(Config["PresetPortal", "Permission"] == null) Config["PresetPortal", "Permission"] = "portals.use";
            if(Config["PresetPortal", "Permission"].ToString() != "portals.use") return;

            if(Config["TeleportTimer"] == null) Config["TeleportTimer"] = 10;
            if(Convert.ToInt32(Config["TeleportTimer"]) != 10) return;

            if(Config["Effects", "Enabled"] == null) Config["Effects", "Enabled"] = "true";
            if(Config["Effects", "Enabled"].ToString() != "true") return;

            if(Config["Effects", "Height"] == null) Config["Effects", "Height"] = "2.5";
            if(Config["Effects", "Height"].ToString() != "2.5") return;

            if(Config["Effects", "Spacing"] == null) Config["Effects", "Spacing"] = "0.2";
            if(Config["Effects", "Spacing"].ToString() != "0.2") return;
            SaveConfig();
        }

        void PortalFX()
        {
            foreach (var portal in Config)
            {
                string portalName = portal.Key.ToString();
                if(portalName == "TeleportTimer" || portalName == "Effects" ) continue;

                for(float i = 0F ; i <= Config["Effects", "Height"].ToString().ToFloat() ; i = i + Config["Effects", "Spacing"].ToString().ToFloat())
                {
                    var Entrance = new Vector3(Convert.ToInt32(Config[portalName, "EntranceX"]), Convert.ToInt32(Config[portalName, "EntranceY"]) + i, Convert.ToInt32(Config[portalName, "EntranceZ"]));
                    var Exit = new Vector3(Convert.ToInt32(Config[portalName, "ExitX"]), Convert.ToInt32(Config[portalName, "ExitY"]) + i, Convert.ToInt32(Config[portalName, "ExitZ"]));
                    Effect.server.Run("assets/prefabs/weapons/rocketlauncher/effects/pfx_fire_rocket_smokeout.prefab", Entrance, Vector3.up, null, true);

                    if(Config[portalName, "OneWay"].ToString() == "false")
                    {
                        Effect.server.Run("assets/prefabs/weapons/rocketlauncher/effects/pfx_fire_rocket_smokeout.prefab", Exit, Vector3.up, null, true);
                    }
                }
            }
        }

        void Portal()
        {
            foreach (var portal in Config)
            {
                string portalName = portal.Key.ToString();
                if(portalName == "TeleportTimer" || portalName == "Effects" ) continue;

                var Entrance = new Vector3(Convert.ToInt32(Config[portalName, "EntranceX"]), Convert.ToInt32(Config[portalName, "EntranceY"]), Convert.ToInt32(Config[portalName, "EntranceZ"]));
                var Exit = new Vector3(Convert.ToInt32(Config[portalName, "ExitX"]), Convert.ToInt32(Config[portalName, "ExitY"]), Convert.ToInt32(Config[portalName, "ExitZ"]));

                foreach(BasePlayer current in BasePlayer.activePlayerList)
                {
                    string uid = current.userID.ToString();
                    int DisEntrance = Convert.ToInt32(Vector3.Distance(current.transform.position, Entrance));
                    int DisExit = Convert.ToInt32(Vector3.Distance(current.transform.position, Exit));

                    if((bool)current.IsSleeping()) continue;
                    if((bool)current.IsSpectating()) continue;
                    if((bool)current.IsWounded()) continue;

                    if(DisEntrance < Convert.ToInt32(Config[portalName, "Radius"]))
                    {
                        if(!permission.UserHasPermission(uid, Config[portalName, "Permission"].ToString()))
                        {
                            SendChatMessage(current, "PORTALS", "You are not allowed to use this portal!");
                            return;
                        }

                        SendChatMessage(current,"PORTALS", "Teleported!");
                        ForcePlayerPosition(current, Exit);
                    }
                    else if (DisExit < Convert.ToInt32(Config[portalName, "Radius"]) && Config[portalName, "OneWay"].ToString() == "false")
                    {
                        if(!permission.UserHasPermission(uid, Config[portalName, "Permission"].ToString()))
                        {
                            SendChatMessage(current, "PORTALS", "You are not allowed to use this portal!");
                            return;
                        }

                        SendChatMessage(current,"PORTALS", "Teleported!");
                        ForcePlayerPosition(current, Entrance);
                    }
                }
            }
        }

        void BackupConfig()
        {
            storedData.PortalsBackup.Clear();
            foreach (var portal in Config)
            {
                string portalName = portal.Key.ToString();

                storedData.PortalsBackup[portalName] = portal.Value;
                continue;
            }

            Interface.GetMod().DataFileSystem.WriteObject("PortalsBackup", storedData);
        }

        [ConsoleCommand("portals.wipe")]
        void cmdWipePortals(ConsoleSystem.Arg arg)
        {
            Dictionary<string, object> settings = new Dictionary<string, object>();
            BasePlayer player = null;

            if(arg.connection != null && arg.connection.player != null)
            {
                if(permission.UserHasPermission(arg.connection.userid.ToString(), "portals.admin") == false)
                {
                    player = (BasePlayer)arg.connection.player;
                    player.SendConsoleCommand("echo 'You don't have permission to use this command!'");
                    return;
                }
            }


            foreach (var portal in Config)
            {
                string portalName = portal.Key.ToString();
                if(portalName != "TeleportTimer" && portalName != "Effects" ) continue;

                settings[portalName] = portal.Value;
                continue;
            }

            BackupConfig();
            Config.Clear();

            foreach (var current in settings)
            {
                string name = current.Key.ToString();
                Config[name] = current.Value;

                continue;
            }

            SaveConfig();
            LoadDefaultConfig();

            PrintWarning("Wiped all Portals!");
            if(player != null) player.SendConsoleCommand("echo '<color=yellow>Wiped all Portals!</color>'");
        }

        [ChatCommand("portal")]
        void cmdPortal(BasePlayer player, string cmd, string[] args)
        {
            string uid = player.userID.ToString();
            if(!permission.UserHasPermission(uid, "portals.admin"))
            {
                SendChatMessage(player, "PORTALS", "You have no permission to use this command!");
                return;
            }

            if(args.Length == 0)
            {
                SendChatMessage(player, "PORTALS", "\n/portal list\n/portal set <PortalName> <entrance|exit|oneway>");
                return;
            }

            if(args[0].ToLower() == "list")
            {
                foreach (var portal in Config)
                {
                    string portalName = portal.Key.ToString();
                    if(portalName == "TeleportTimer" || portalName == "Effects" ) continue;
                    var Entrance = new Vector3(Convert.ToInt32(Config[portalName, "EntranceX"]), Convert.ToInt32(Config[portalName, "EntranceY"]), Convert.ToInt32(Config[portalName, "EntranceZ"]));
                    var Exit = new Vector3(Convert.ToInt32(Config[portalName, "ExitX"]), Convert.ToInt32(Config[portalName, "ExitY"]), Convert.ToInt32(Config[portalName, "ExitZ"]));
                    string OneWay = (string)Config[portalName, "OneWay"];
                    SendChatMessage(player, "PORTALS", "<color=cyan>-----> " + portalName + " <-----</color>\n" +
                        "<color=cyan>Entrance:</color> " + Entrance.ToString() + "\n" +
                        "<color=cyan>Exit:</color> " + Exit.ToString() + "\n" +
                        "<color=cyan>OneWay:</color> " + OneWay);
                }
                return;
            }

            if(args[0].ToLower() == "wipe")
            {
                Dictionary<string, object> settings = new Dictionary<string, object>();

                foreach (var portal in Config)
                {
                    string portalName = portal.Key.ToString();
                    if(portalName != "TeleportTimer" && portalName != "Effects" ) continue;

                    settings[portalName] = portal.Value;
                    continue;
                }

                BackupConfig();
                Config.Clear();

                foreach (var current in settings)
                {
                    string name = current.Key.ToString();
                    Config[name] = current.Value;

                    continue;
                }

                SaveConfig();
                LoadDefaultConfig();

                SendChatMessage(player, "PORTALS", "Wiped all Portals!");
            }

            if(args[0].ToLower() == "set")
            {
                if(args.Length != 3 || args[2].ToLower() != "entrance" && args[2].ToLower() != "exit" && args[2].ToLower() != "oneway")
                {
                    SendChatMessage(player, "PORTALS", "Syntax: /portal set <PortalName> <entrance|exit|oneway>");
                    return;
                }

                var pos = player.transform.position;

                if(args[2].ToLower() == "oneway")
                {
                    string onewayState = "false";
                    if(Config[args[1], "OneWay"] == "true") onewayState = "false";
                    else onewayState = "true";

                    Config[args[1], "OneWay"] = onewayState;

                    SaveConfig();
                    SendChatMessage(player, "PORTALS", "Set <color=cyan>ONE WAY</color> for Portal <color=cyan>" + args[1] + "</color> to " + onewayState);
                }

                if(args[2].ToLower() == "entrance")
                {
                    Config[args[1], "EntranceX"] = Convert.ToInt32(pos.x);
                    Config[args[1], "EntranceY"] = Convert.ToInt32(pos.y + 0.5F);
                    Config[args[1], "EntranceZ"] = Convert.ToInt32(pos.z);
                    SaveConfig();
                    SendChatMessage(player, "PORTALS", "Set <color=cyan>ENTRANCE</color> for Portal <color=cyan>" + args[1] + "</color>");
                }

                if(args[2].ToLower() == "exit")
                {
                    Config[args[1], "ExitX"] = Convert.ToInt32(pos.x);
                    Config[args[1], "ExitY"] = Convert.ToInt32(pos.y + 0.5F);
                    Config[args[1], "ExitZ"] = Convert.ToInt32(pos.z);
                    SaveConfig();
                    SendChatMessage(player, "PORTALS", "Set <color=cyan>EXIT</color> for Portal <color=cyan>" + args[1] + "</color>");
                }

                if(Config[args[1], "OneWay"] == null) Config[args[1], "OneWay"] = "false";
                if(Config[args[1], "OneWay"].ToString() != "false") return;

                if(Config[args[1], "Permission"] == null) Config[args[1], "Permission"] = "portals.use";
                if(Config[args[1], "Permission"].ToString() != "portals.use") return;

                if(Config[args[1], "Radius"] == null) Config[args[1], "Radius"] = 2;
                if(Convert.ToInt32(Config[args[1], "Radius"]) != 2) return;
                return;
            }
        }

        #region UsefulMethods
        //--------------------------->   Position forcing   <---------------------------//

        public void ForcePlayerPosition(BasePlayer player, Vector3 pos)
        {
            PutToSleep(player);
            player.transform.position = pos;

            //    Thx to @Wulf for this line:
            var LastPositionValue = typeof(BasePlayer).GetField("lastPositionValue", (BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic));

            LastPositionValue.SetValue(player, player.transform.position);
            player.ClientRPCPlayer(null, player, "ForcePositionTo", pos);
            player.TransformChanged();

            player.SetPlayerFlag(BasePlayer.PlayerFlags.ReceivingSnapshot, true);
            player.UpdateNetworkGroup();

            player.SendNetworkUpdateImmediate(false);
            player.ClientRPCPlayer(null, player, "StartLoading");
            player.SendFullSnapshot();
            player.SetPlayerFlag(BasePlayer.PlayerFlags.ReceivingSnapshot, false);
            player.ClientRPCPlayer(null, player, "FinishLoading" );
        }

        void PutToSleep(BasePlayer player)
        {
            player.SetPlayerFlag(BasePlayer.PlayerFlags.Sleeping, true);
            if(BasePlayer.sleepingPlayerList.Contains(player) == false) BasePlayer.sleepingPlayerList.Add(player);

            player.CancelInvoke("InventoryUpdate");
            player.inventory.crafting.CancelAll(true);
        }

        //--------------------------->   Player finding   <---------------------------//

        BasePlayer GetPlayer(string searchedPlayer, BasePlayer executer, string prefix)
        {
            BasePlayer targetPlayer = null;
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

            if (matchingPlayers.Length == 0)
            {
                SendChatMessage(executer, prefix, "No matching players found!");
            }

            if (matchingPlayers.Length > 1)
            {
                SendChatMessage(executer, prefix, "Multiple players found:");
                string multipleUsers = "";
                foreach (string matchingplayer in matchingPlayers)
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
                SendChatMessage(executer, prefix, multipleUsers);
            }

            if (matchingPlayers.Length == 1)
            {
                targetPlayer = BasePlayer.Find(matchingPlayers[0]);
            }
            return targetPlayer;
        }

        //---------------------------->   Converting   <----------------------------//

        string ArrayToString(string[] array, int first)
        {
            int count = 0;
            string output = array[first];
            foreach (string current in array)
            {
                if (count <= first)
                {
                    count++;
                    continue;
                }

                output = output + " " + current;
                count++;
            }
            return output;
        }

        //---------------------------->   Chat Sending   <----------------------------//

        void BroadcastChat(string prefix, string msg)
        {
            PrintToChat("<color=cyan>" + prefix + "</color>: " + msg);
        }

        void SendChatMessage(BasePlayer player, string prefix, string msg)
        {
            SendReply(player, "<color=cyan>" + prefix + "</color>: " + msg);
        }

        //---------------------------------------------------------------------------//
        #endregion
    }
}