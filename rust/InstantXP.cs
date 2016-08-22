using System.Collections.Generic;
using Oxide.Core;
using Oxide.Core.Configuration;

namespace Oxide.Plugins
{
    [Info("InstantXP", "k1lly0u", "0.1.2", ResourceId = 2017)]
    class InstantXP : RustPlugin
    {
        #region Fields
        IXPData ixpData;
        private DynamicConfigFile data;

        int[] Levels;
        #endregion

        #region Oxide Hooks 
        void Loaded()
        {
            data = Interface.Oxide.DataFileSystem.GetFile("instantxp_permissions");
            lang.RegisterMessages(Messages, this);
        }
        void OnServerInitialized()
        {
            Levels = Rust.Xp.Config.Levels;
            LoadVariables();
            LoadData();
            foreach (var perm in ixpData.Permissions)
                permission.RegisterPermission(perm.Key, this);
        }
        void OnPlayerInit(BasePlayer player)
        {
            var level = GetLevel(player.userID);
            if (level == 0) return;
            if (player.xp.CurrentLevel < level)
            {
                player.xp.Reset();
                player.xp.Add(Rust.Xp.Definitions.Cheat, Levels[level - 1]);
                SendReply(player, string.Format(LA("levelSet", player.UserIDString), level));
            }
        }
        void OnUserPermissionGranted(string name, string perm)
        {
            if (ixpData.Permissions.ContainsKey(perm))
            {
                var player = BasePlayer.Find(name);
                if (player != null)
                    OnPlayerInit(player);
            }
        }
        #endregion

        #region Functions
        private int GetLevel(ulong playerid)
        {
            int level = configData.DefaultLevel;
            foreach (var entry in ixpData.Permissions)
            {
                if (permission.UserHasPermission(playerid.ToString(), entry.Key))
                {
                    level = entry.Value;
                    break;
                }
            }
            return level;
        }
        #endregion

        #region Chat Commands
        [ChatCommand("ixp")]
        private void cmdRod(BasePlayer player, string command, string[] args)
        {
            if (player.IsAdmin())
            {
                if (args == null || args.Length == 0)
                {
                    SendMSG(player, LA("addSyn", player.UserIDString));
                    SendMSG(player, LA("editSyn", player.UserIDString));
                    SendMSG(player, LA("remSyn", player.UserIDString));
                    SendMSG(player, LA("listSyn", player.UserIDString));
                    return;
                }
                if (args.Length >= 1)
                {
                    switch (args[0].ToLower())
                    {
                        case "add":
                            if (args.Length == 3)
                            {
                                string perm = args[1].ToLower();
                                if (!perm.StartsWith(Title.ToLower() + "."))
                                    perm = Title.ToLower() + "." + perm;
                                if (!permission.PermissionExists(perm) && !ixpData.Permissions.ContainsKey(perm))
                                {
                                    int level = 0;
                                    if (int.TryParse(args[2], out level))
                                    {
                                        ixpData.Permissions.Add(perm, level);
                                        permission.RegisterPermission(perm, this);
                                        SaveData();
                                        SendMSG(player, string.Format(LA("addPerm", player.UserIDString), perm, level));
                                        return;
                                    }
                                    SendMSG(player, LA("validNum", player.UserIDString));
                                    return;
                                }
                                SendMSG(player, LA("existPerm", player.UserIDString));
                                return;
                            }
                            SendMSG(player, LA("addSyn", player.UserIDString));
                            return;
                        case "edit":
                            if (args.Length == 3)
                            {
                                if (ixpData.Permissions.ContainsKey(args[1].ToLower()))
                                {
                                    int level = 0;
                                    if (int.TryParse(args[2], out level))
                                    {
                                        ixpData.Permissions[args[1].ToLower()] = level;
                                        SaveData();
                                        SendMSG(player, string.Format(LA("editPerm", player.UserIDString), args[1].ToLower(), level));
                                        return;
                                    }
                                    SendMSG(player, LA("validNum", player.UserIDString));
                                    return;
                                }
                                SendMSG(player, string.Format(LA("noExistPerm", player.UserIDString), args[1].ToLower()));
                                return;
                            }
                            SendMSG(player, LA("editSyn", player.UserIDString));
                            return;
                        case "remove":
                            if (args.Length >= 2)
                                if (ixpData.Permissions.ContainsKey(args[1].ToLower()))
                                {
                                    ixpData.Permissions.Remove(args[1].ToLower());
                                    SaveData();
                                    SendMSG(player, string.Format(LA("remPerm", player.UserIDString), args[1].ToLower()));
                                    return;
                                }
                            SendMSG(player, string.Format(LA("noExistPerm", player.UserIDString), args[1].ToLower()));
                            return;
                        case "list":
                            if (ixpData.Permissions.Count > 0)
                            {
                                SendMSG(player, LA("currentPerms", player.UserIDString));
                                foreach (var entry in ixpData.Permissions)
                                    SendMSG(player, $"{entry.Key} -- {entry.Value}");
                                return;
                            }
                            SendMSG(player, LA("noPermsSet", player.UserIDString));
                            return;
                    }
                }
            }
        }
        private void SendMSG(BasePlayer player, string message) => SendReply(player, "<color=orange>" + message + "</color>");
        #endregion

        #region Config        
        private ConfigData configData;
        class ConfigData
        {
            public int DefaultLevel { get; set; }
        }
        private void LoadVariables()
        {
            LoadConfigVariables();
            SaveConfig();
        }
        protected override void LoadDefaultConfig()
        {
            var config = new ConfigData
            {
                DefaultLevel = 0
            };
            SaveConfig(config);
        }
        private void LoadConfigVariables() => configData = Config.ReadObject<ConfigData>();
        void SaveConfig(ConfigData config) => Config.WriteObject(config, true);
        #endregion 

        #region Data Management
        void SaveData() => data.WriteObject(ixpData);
        void LoadData()
        {
            try
            {
                ixpData = data.ReadObject<IXPData>();
            }
            catch
            {
                ixpData = new IXPData();
            }
        }
        class IXPData
        {
            public Dictionary<string, int> Permissions = new Dictionary<string, int>();
        }
        #endregion

        #region Messaging
        Dictionary<string, string> Messages = new Dictionary<string, string>
        {
            { "noPermsSet", "There are currently no permissions set up" },
            { "currentPerms", "Current permissions;" },
            { "noExistPerm", "The permission {0} does not exist" },
            { "remPerm", "You have successfully remove the permission {0}" },
            { "editSyn", "/ixp edit <permission> <level> - Edits a existing permission and level" },
            { "validNum", "You must enter a valid level number" },
            { "editPerm", "You have successfully edited the permission {0} with a level of {1}" },
            { "addSyn", "/ixp add <permission> <level> - Adds a new permission and level" },
            { "existPerm", "That permission already exists" },
            { "addPerm", "You have successfully added the permission {0} with a starting level of {1}" },
            { "remSyn", "/ixp remove <permission> - Remove a permission" },
            { "listSyn", "/ixp list - Lists all permissions and assigned level" },
            { "levelSet", "Your level has been automatically raised to {0}" }
        };
        private string LA(string key, string userid = null) => lang.GetMessage(key, this, userid);
        #endregion
    }
}
