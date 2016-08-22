using System.Collections.Generic;
using Oxide.Core;
using System.Linq;
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
    [Info("XP Backup", "PaiN", "0.2", ResourceId = 2035)]
    class XPBackup : RustPlugin
    {

        [PluginReference]
        Plugin PvXselector;

        class Data { public List<XPInfo> xpinfo = new List<XPInfo>(); }
        static Data data;

        class XPInfo
        {
            public float Level;
            public float XP;
            public ulong steamId;
            public bool OnConnect;

            internal static XPInfo GetInfo(ulong Id) => data.xpinfo.Find(x => x.steamId == Id);
        }

        void Loaded()
        {
            LoadMessages();
            data = Interface.Oxide.DataFileSystem.ReadObject<Data>("XPBackup");
            permission.RegisterPermission("xpbackup.admin", this);
        }

        void OnServerSave() => SaveData();

        void LoadMessages()
        {
            Dictionary<string, string> messages = new Dictionary<string, string>
            {
                {"CMD_NO_PERMISSION", "You do not have permission to use this command!"},
                {"CMD_SAVEALL_SUCCESSFUL", "You have successfully saved everyone's Experience Information" },
                {"CMD_SAVE_SUCCESSFUL", "You have successfully saved {0}'s Experience Information" },
                {"CMD_SYNTAX", "Syntax: \"/xpb <save/remove/give> <player/all> <GiveOnConnect:true/false>\" (ex. /xpb save PaiN true)"},
                {"CMD_PLAYER_NOT_FOUND", "Player not found!" },
                {"CMD_NO_BACKUP", "There are no saved backups for this player." },
                {"CMD_REMOVE_SUCCESSFUL", "You have successfully removed {0}'s Experience Information." },
                {"CMD_REMOVEALL_SUCCESSFUL", "You have successfully removed everyone's Experience Information." },
                {"CMD_GIVE_SUCCESSFUL", "You have successfully given {0}'s Experience Information back." },
                {"CMD_GIVEALL_SUCCESSFUL", "You have successfully given everyone's Experience Information back." },
                {"CONNECT_GIVE_XP", "You have been given your saved XP"}

            };
            lang.RegisterMessages(messages, this);
        }

        void OnPlayerInit(BasePlayer player)
        {
            if (XPInfo.GetInfo(player.userID) != null && XPInfo.GetInfo(player.userID).OnConnect)
            {
                ResetAndAdd(player);
                data.xpinfo.Remove(XPInfo.GetInfo(player.userID));
                player?.ChatMessage(LangMsg("CONNECT_GIVE_XP", player.userID));
            }
        }

        [ChatCommand("xpb")]
        void cmdBackup(BasePlayer player, string cmd, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, "xpbackup.admin"))
            {
                player.ChatMessage(LangMsg("CMD_NO_PERMISSION", player.userID));
                return;
            }
            if (args.Length == 0)
            {
                player.ChatMessage(LangMsg("CMD_SYNTAX", player.userID));
                return;
            }

            BasePlayer target = BasePlayer.Find(args[1]);
            XPInfo pinfo = XPInfo.GetInfo(target?.userID ?? 0);

            switch (args[0])
            {
                case "save":
                    if (args.Length != 3)
                    {
                        player.ChatMessage(LangMsg("CMD_SYNTAX", player.userID));
                        return;
                    }

                    bool result;

                    if (!bool.TryParse(args[2], out result))
                    {
                        player.ChatMessage(LangMsg("CMD_SYNTAX", player.userID));
                        return;
                    }

                    if (args[1] == "all")
                    {
                        data.xpinfo.Clear();
                        foreach (var current in covalence.Players.GetAllPlayers())
                        {
                            ulong currId = System.Convert.ToUInt64(current.Id);

                            if (XPInfo.GetInfo(currId) != null)
                                data.xpinfo.Remove(XPInfo.GetInfo(currId));

                            XPInfo info = new XPInfo()
                            {
                                Level = BasePlayer.FindXpAgent(currId).CurrentLevel,
                                steamId = currId,
                                XP = BasePlayer.FindXpAgent(currId).UnspentXp,
                                OnConnect = result
                            };
                            data.xpinfo.Add(info);
                        }
                        player.ChatMessage(LangMsg("CMD_SAVEALL_SUCCESSFUL", player.userID));
                    }
                    else
                    {
                        if (target == null)
                        {
                            player.ChatMessage(LangMsg("CMD_PLAYER_NOT_FOUND", player.userID));
                            return;
                        }

                        if (XPInfo.GetInfo(target.userID) != null)
                            data.xpinfo.Remove(pinfo);

                        XPInfo info = new XPInfo()
                        {
                            Level = BasePlayer.FindXpAgent(target.userID).CurrentLevel,
                            steamId = target.userID,
                            XP = BasePlayer.FindXpAgent(target.userID).UnspentXp,
                            OnConnect = result
                        };
                        data.xpinfo.Add(info);
                        player.ChatMessage(string.Format(LangMsg("CMD_SAVE_SUCCESSFUL", player.userID), target.displayName));
                    }
                    break;
                case "remove":
                    if (args[1] == "all")
                    {
                        data.xpinfo.Clear();
                        player.ChatMessage(LangMsg("CMD_REMOVEALL_SUCCESSFUL", player.userID));
                    }
                    else
                    {
                        if (target == null)
                        {
                            player.ChatMessage(LangMsg("CMD_PLAYER_NOT_FOUND", player.userID));
                            return;
                        }
                        if (pinfo == null)
                        {
                            player.ChatMessage(LangMsg("CMD_NO_BACKUP", player.userID));
                            return;
                        }
                        data.xpinfo.Remove(pinfo);
                        player.ChatMessage(string.Format(LangMsg("CMD_REMOVE_SUCCESSFUL", player.userID), target.displayName));
                    }
                    break;
                case "give":
                    if (args[1] == "all")
                    {
                        foreach (BasePlayer current in BasePlayer.activePlayerList)
                        {
                            if (data.xpinfo.Any(x => x.steamId == current.userID))
                            {
                                ResetAndAdd(current);
                                data.xpinfo.Remove(XPInfo.GetInfo(current.userID));
                            }
                        }
                        player.ChatMessage(LangMsg("CMD_GIVEALL_SUCCESSFUL", player.userID));
                    }
                    else
                    {
                        if (target == null)
                        {
                            player.ChatMessage(LangMsg("CMD_PLAYER_NOT_FOUND", player.userID));
                            return;
                        }
                        if (pinfo == null)
                        {
                            player.ChatMessage(LangMsg("CMD_NO_BACKUP", player.userID));
                            return;
                        }
                        ResetAndAdd(player);
                        data.xpinfo.Remove(pinfo);
                        player.ChatMessage(string.Format(LangMsg("CMD_GIVE_SUCCESSFUL", player.userID), target.displayName));
                    }
                    break;
            }
            SaveData();
        }

        void ResetAndAdd(BasePlayer player)
        {
            var xpagent = BasePlayer.FindXpAgent(player.userID);
            PvXselector?.Call("disablePvXLogger", player.userID);
            xpagent.Reset();
            xpagent.Add(Rust.Xp.Definitions.Cheat, Rust.Xp.Config.LevelToXp(System.Convert.ToInt32(XPInfo.GetInfo(player.userID).Level)));
            PvXselector?.Call("pvxUpdateXPDataFile", player);
            PvXselector?.Call("enablePvXLogger", player.userID);
        }

        void SaveData() => Interface.Oxide.DataFileSystem.WriteObject("XPBackup", data);
        string LangMsg(string msg, object uid) => lang.GetMessage(msg, this, uid == null ? null : uid.ToString());
    }
}
