using System.Collections.Generic;
using Oxide.Core.Libraries;
using Oxide.Core;
using System.Linq;

namespace Oxide.Plugins
{
    [Info("XP Permissions", "PaiN", "0.3.1", ResourceId = 2024)]
    [Description("Grants player permission on their level up.")]
    class XPPermissions : RustPlugin
    {

        ConfigFile Cfg = new ConfigFile();

        class ConfigFile
        {
            public Dictionary<int, List<_Permission>> Permissions = new Dictionary<int, List<_Permission>>
            {
                {5, new List<_Permission> { new _Permission("default", "kit.default", "tag.default", "bgrade.permission") } }
            };
        }

        class _Permission
        {
            public List<string> Permission = new List<string>();
            public string Group;

            public _Permission(string groupname, params string[] permission)
            {
                foreach(string perm in permission)
                Permission.Add(perm);
                Group = groupname; 
            } 
        } 

        void Loaded() 
        {
            Cfg = Config.ReadObject<ConfigFile>();
            LoadMessages();
            permission.RegisterPermission("xppermissions.admin", this);
        }

        void LoadMessages()
        {
            Dictionary<string, string> messages = new Dictionary<string, string>
            {
                //{"CMD_NO_PERMISSION", "You do not have permission to use this command" },
                {"LVL_PERMISSION_GRANTED", "You have been granted the permission '{perm}' for reaching level {lvl}." },
                {"CONSOLE_LVL_PERM_GRANTED", "'{perm}' permission has been granted to ({id}/{name}) for reaching level ({lvl})"},
               // {"CMD_ADD_SYNTAX", "Syntax: \"/xpp add <permission> <level> <group>\" "},
               // {"CMD_XPPERMISSION_ADDED", "New XP Permission added! || Permission: {0} ## Level: {1} ## Group: {2} ||" },
              //  {"CMD_REMOVE_SYNTAX", "Syntax: \"/xpp remove <Level> <OPTIONAL:Group> <OPTIONAL:Permission> \" " },
              //  {"CMD_SYNTAX", "/xpp add <permission> <level> <group> \n /xpp remove <Level> <OPTIONAL:Group> <OPTIONAL:Permission> "}
            };
             
            lang.RegisterMessages(messages, this);   
        }

        protected override void LoadDefaultConfig() { PrintWarning("Creating a new configuration file . . ."); Config.WriteObject(Cfg, true); }

        void OnXpEarned(ulong id, float amount, string source)
        {
            BasePlayer player = BasePlayer.FindByID(id);
            var agent = BasePlayer.FindXpAgent(id);
            int level = (int)agent.CurrentLevel;
            if (Cfg.Permissions.Any(x => x.Key == level))
            {
                List<_Permission> info = Cfg.Permissions[level];
                foreach (_Permission lvl in info.ToList())
                {
                    foreach (var perm in lvl.Permission.ToList())
                    {
                        if (!permission.PermissionExists(perm))
                        {
                            PrintWarning(string.Format("Permission '{0}' does not exists. Attempt to grant permission has failed.", perm));
                            return;
                        }

                        if (permission.UserHasGroup(id.ToString(), lvl.Group) && !permission.UserHasPermission(id.ToString(), perm))
                        {
                            ConsoleSystem.Run.Server.Normal($"grant user {id} {perm}");
                            Puts(LangMsg("CONSOLE_LVL_PERM_GRANTED", null).Replace("{perm}", perm).Replace("{id}", id.ToString()).Replace("{name}", player?.displayName).Replace("{lvl}", level.ToString()));
                            PrintToChat(player, LangMsg("LVL_PERMISSION_GRANTED", id.ToString()).Replace("{perm}", perm).Replace("{lvl}", level.ToString()));

                        }
                    }
                }
            }
        }

      
        /* [ChatCommand("xpp")]
         void cmdXPP(BasePlayer player, string cmd, string[] args)
         {
             if(!permission.UserHasPermission(player.UserIDString, "xppermissions.admin"))
             {
                 player.ChatMessage(LangMsg("CMD_NO_PERMISSION", player.UserIDString));
                 return;
             }

             if (args.Length <= 1)
             {
                 player.ChatMessage(LangMsg("CMD_SYNTAX", player.UserIDString));
                 return;
             }

             int level;

             switch (args[0])
             {
                 case "add":
                     if (!int.TryParse(args[2], out level))
                     {
                         player.ChatMessage(LangMsg("CMD_ADD_SYNTAX", player.UserIDString));
                         return;
                     }

                     if (data.Permissions.ContainsKey(level))
                     {
                         foreach (_Permission sinfo in data.Permissions[level].ToList())
                             if (args[3] == sinfo.Group)
                                 sinfo.Permission.Add(args[1]);
                             else
                             {
                                 _Permission newperm = new _Permission(args[3], args[1]);
                                 data.Permissions[level].Add(newperm);
                             }
                     }
                     else 
                     {
                         _Permission perm = new _Permission(args[3], args[1]);
                         data.Permissions.Add(level, new List<_Permission> { perm });
                     }
                     break;
                 case "remove":
                     if (!int.TryParse(args[1], out level))
                     {
                         player.ChatMessage(LangMsg("CMD_REMOVE_SYNTAX", player.UserIDString));
                         return;
                     }
                     List<_Permission> info = data.Permissions[level];

                     if (args.Length == 4)
                     {   
                         foreach (_Permission lvl in info)
                             foreach (string perm in CopyList(lvl.Permission))
                                 if (args[2] == lvl.Group && args[3] == perm)
                                     lvl.Permission.Remove(perm);
                     }
                     else if(args.Length == 3)
                     {
                         foreach (_Permission lvl in info)
                             if (args[2] == lvl.Group)
                                 data.Permissions[level].Remove(lvl);
                     }
                     else if(args.Length == 2)
                     { 
                         if (data.Permissions.Any(x => x.Key == level))
                             data.Permissions.Remove(level);
                     }
                     break;
             }
             SaveData();
         }*/


        string LangMsg(string msg, string uid = null) => lang.GetMessage(msg, this, uid);
    }
}