using System;
using System.Collections.Generic;
using Rust;
using Oxide.Core;
using Oxide.Core.Plugins;
using System.Reflection;

namespace Oxide.Plugins
{
    [Info("MagicHammer", "Norn", 0.4, ResourceId = 1375)]
    [Description("Hit stuff with the hammer and do things.")]
    public class MagicHammer : RustPlugin
    {
        int MODE_REPAIR = 1;
        int MODE_DESTROY = 2;
        int MAX_MODES = 2;
        [PluginReference]
        Plugin PopupNotifications;
        class StoredData
        {
            public Dictionary<ulong, MagicHammerInfo> Users = new Dictionary<ulong, MagicHammerInfo>();
            public StoredData()
            {
            }
        }

        class MagicHammerInfo
        {
            public ulong UserId;
            public int Mode;
            public bool Enabled;
            public bool Messages_Enabled;
            public MagicHammerInfo()
            {
            }
        }

        StoredData hammerUserData;
        static FieldInfo buildingPriv;
        void Loaded()
        {
            if (!permission.PermissionExists("can.mh")) permission.RegisterPermission("can.mh", this);
            hammerUserData = Interface.GetMod().DataFileSystem.ReadObject<StoredData>(this.Title + "_users");
            buildingPriv = typeof(BasePlayer).GetField("buildingPrivlidges", (BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic));
        }
        void OnPlayerInit(BasePlayer player)
        {
            InitPlayerData(player);
        }
        bool InitPlayerData(BasePlayer player)
        {
            if(CanMagicHammer(player))
            {
                MagicHammerInfo p = null;
                if (hammerUserData.Users.TryGetValue(player.userID, out p) == false)
                {
                    var info = new MagicHammerInfo();
                    info.Enabled = false;
                    info.Mode = MODE_REPAIR; //Repair
                    info.UserId = player.userID;
                    info.Messages_Enabled = true;
                    hammerUserData.Users.Add(player.userID, info);
                    Interface.GetMod().DataFileSystem.WriteObject(this.Title + "_users", hammerUserData);
                    Puts("Adding entry " + player.userID.ToString());
                }
            }
            else
            {
                MagicHammerInfo p = null;
                if (hammerUserData.Users.TryGetValue(player.userID, out p))
                {
                    Puts("Removing " + player.userID + " from magic hammer data, cleaning up...");
                    hammerUserData.Users.Remove(player.userID);
                }
            }
            return false;
        }
        protected override void LoadDefaultConfig()
        {
            Puts("Updating configuration file...");
            Config.Clear();
            Config["iProtocol"] = Protocol.network;
            Config["bUsePopupNotifications"] = false;
            Config["bMessagesEnabled"] = true;
            Config["tMessageRepaired"] = "Entity: <color=#F2F5A9>{entity_name}</color> health <color=#2EFE64>updated</color> from <color=#FF4000>{current_hp}</color>/<color=#2EFE64>{new_hp}</color>.";
            Config["tMessageDestroyed"] = "Entity: <color=#F2F5A9>{entity_name}</color> <color=#FF4000>destroyed</color>.";
            Config["tMessageUsage"] = "/mh <enabled/mode>.";
            Config["tHammerEnabled"] = "Status: {hammer_status}.";
            Config["tHammerMode"] = "You have switched to: {hammer_mode} mode.";
            Config["tHammerModeText"] = "Choose your mode: 1 = <color=#2EFE64>repair</color>, 2 = <color=#FF4000>destroy</color>.";
            Config["tNoAccessCupboard"] = "You <color=#FF4000>don't</color> have access to all the tool cupboards around you.";
            Config["bDestroyCupboardCheck"] = true;
            SaveConfig();
        }
        bool CanMagicHammer(BasePlayer player)
        {
            if (permission.UserHasPermission(player.userID.ToString(), "can.mh")) return true;
            return false;
        }
        private void PrintToChatEx(BasePlayer player, string result, string tcolour = "#F5A9F2")
        {
            if (Convert.ToBoolean(Config["bMessagesEnabled"]))
            {
                if (Convert.ToBoolean(Config["bUsePopupNotifications"]))
                {
                    PopupNotifications?.Call("CreatePopupNotification", "<color=" + tcolour + ">" + this.Title.ToString() + "</color>\n" + result, player);
                }
                else
                {
                    PrintToChat(player, "<color=\"" + tcolour + "\">[" + this.Title.ToString() + "]</color> " + result);
                }
            }
        }
        void Unload()
        {
            Puts("Saving hammer database...");
            SaveData();
        }
        void SaveData()
        {
            Interface.Oxide.DataFileSystem.WriteObject(this.Title+"_users", hammerUserData);
        }
        int GetPlayerHammerMode(BasePlayer player)
        {
            MagicHammerInfo p = null;
            if (hammerUserData.Users.TryGetValue(player.userID, out p))
            {
                return p.Mode;
            }
            return -1;
        }
        bool SetPlayerHammerMode(BasePlayer player, int mode)
        {
            MagicHammerInfo p = null;
            if (hammerUserData.Users.TryGetValue(player.userID, out p))
            {
                p.Mode = mode;
                return true;
            }
            return false;
        }
        bool SetPlayerHammerStatus(BasePlayer player, bool enabled)
        {
            MagicHammerInfo p = null;
            if (hammerUserData.Users.TryGetValue(player.userID, out p))
            {
                p.Enabled = enabled;
                return true;
            }
            return false;
        }
        bool MagicHammerEnabled(BasePlayer player)
        {
            MagicHammerInfo p = null;
            if (hammerUserData.Users.TryGetValue(player.userID, out p))
            {
                return p.Enabled;
            }
            return false;
        }
        [ChatCommand("mh")]
        void cmdMH(BasePlayer player, string cmd, string[] args)
        {
            if (CanMagicHammer(player))
            {
                MagicHammerInfo p = null;
                if (hammerUserData.Users.TryGetValue(player.userID, out p) == false)
                {
                    InitPlayerData(player);
                }
                if (args.Length == 0 || args.Length > 2)
                {
                    PrintToChatEx(player, Config["tMessageUsage"].ToString());
                    if (player.net.connection.authLevel >= 1)
                    {
                        // Future Admin Cmds
                    }
                }
                else if (args[0] == "mode")
                {
                    if (args.Length == 1)
                    {
                        PrintToChatEx(player, Config["tHammerModeText"].ToString());
                    }
                    else if (args.Length == 2)
                    {
                        int mode = Convert.ToInt16(args[1]);
                        if (mode >= 1 && mode <= MAX_MODES)
                        {
                            string mode_text = "null";
                            if (mode == MODE_REPAIR)
                            {
                                mode_text = "<color=#2EFE64>repair</color>";
                            }
                            else if (mode == MODE_DESTROY)
                            {
                                mode_text = "<color=#FF4000>destroy</color>";
                            }
                            SetPlayerHammerMode(player, mode);
                            string parsed_config = Config["tHammerMode"].ToString();
                            parsed_config = parsed_config.Replace("{hammer_mode}", mode_text);
                            PrintToChatEx(player, parsed_config);
                        }
                        else
                        {
                            PrintToChatEx(player, "Valid modes: 1 - " + MAX_MODES.ToString() + "."); // Invalid Mode
                        }
                    }
                }
                else if (args[0] == "enabled")
                {
                    if (MagicHammerEnabled(player))
                    {
                        string parsed_config = Config["tHammerEnabled"].ToString();
                        parsed_config = parsed_config.Replace("{hammer_status}", "<color=#FF4000>disabled</color>");
                        PrintToChatEx(player, parsed_config);
                        SetPlayerHammerStatus(player, false);
                    }
                    else
                    {
                        string parsed_config = Config["tHammerEnabled"].ToString();
                        parsed_config = parsed_config.Replace("{hammer_status}", "<color=#2EFE64>enabled</color>");
                        PrintToChatEx(player, parsed_config);
                        SetPlayerHammerStatus(player, true);
                    }
                }
            }
        }
        void OnStructureRepairEx(BuildingBlock block, BasePlayer player)
        {
            if (CanMagicHammer(player) && MagicHammerEnabled(player))
            {
                int mode = GetPlayerHammerMode(player);
                if(mode != -1)
                {
                    string block_shortname = block.blockDefinition.hierachyName.ToString();
                    string block_displayname = block.blockDefinition.info.name.english.ToString();
                    float max_health = block.MaxHealth(); float current_health = block.Health();
                    if (mode == MODE_REPAIR)
                    {
                        if (current_health != max_health)
                        {
                            block.health = block.MaxHealth(); float new_hp = block.Health();
                            if (current_health != new_hp)
                            {
                                string parsed_config = Config["tMessageRepaired"].ToString();
                                parsed_config = parsed_config.Replace("{current_hp}", current_health.ToString());
                                parsed_config = parsed_config.Replace("{new_hp}", new_hp.ToString());
                                if (block_displayname.Length == 0)
                                {
                                    parsed_config = parsed_config.Replace("{entity_name}", block_shortname);
                                }
                                else
                                {
                                    parsed_config = parsed_config.Replace("{entity_name}", block_displayname);
                                }
                                PrintToChatEx(player, parsed_config);
                            }
                        }
                    }
                    else if (mode == MODE_DESTROY)
                    {
                        if (Convert.ToBoolean(Config["bDestroyCupboardCheck"]))
                        {
                            if (hasTotalAccess(player))
                            {
                                string parsed_config = Config["tMessageDestroyed"].ToString();
                                if (block_displayname.Length == 0)
                                {
                                    parsed_config = parsed_config.Replace("{entity_name}", block_shortname);
                                }
                                else
                                {
                                    parsed_config = parsed_config.Replace("{entity_name}", block_displayname);
                                }
                                PrintToChatEx(player, parsed_config);
                                RemoveEntity(block);
                            }
                            else
                            {
                                PrintToChatEx(player, Config["tNoAccessCupboard"].ToString());
                            }
                        }
                        else
                        {
                            string parsed_config = Config["tMessageDestroyed"].ToString();
                            if (block_displayname.Length == 0)
                            {
                                parsed_config = parsed_config.Replace("{entity_name}", block_shortname);
                            }
                            else
                            {
                                parsed_config = parsed_config.Replace("{entity_name}", block_displayname);
                            }
                            PrintToChatEx(player, parsed_config);
                            RemoveEntity(block);
                        }
                    }
                }
            }
        }
        static void RemoveEntity(BaseEntity entity)
        {
            if (entity == null) return;
            entity.KillMessage();
        }
        static bool hasTotalAccess(BasePlayer player) // Thanks Reneb
        {
            List<BuildingPrivlidge> playerpriv = buildingPriv.GetValue(player) as List<BuildingPrivlidge>;
            if (playerpriv.Count == 0)
            {
                return false;
            }
            foreach (BuildingPrivlidge priv in playerpriv.ToArray())
            {
                List<ProtoBuf.PlayerNameID> authorized = priv.authorizedPlayers;
                bool foundplayer = false;
                foreach (ProtoBuf.PlayerNameID pni in authorized.ToArray())
                {
                    if (pni.userid == player.userID)
                        foundplayer = true;
                }
                if (!foundplayer)
                {
                    return false;
                }
            }
            return true;
        }
        private void OnServerInitialized()
        {
            if (Config["tNoAccessCupboard"] == null) { Puts("Resetting configuration file (out of date)..."); LoadDefaultConfig(); }
        }
        void OnStructureRepair(BuildingBlock block, BasePlayer player)
        {
            OnStructureRepairEx(block, player);
        }
    }
}