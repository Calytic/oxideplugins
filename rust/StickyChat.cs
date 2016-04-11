using System;
using Oxide.Core;
using Oxide.Core.Libraries;
using Oxide.Core.Plugins;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Oxide.Plugins
{
    [Info("StickyChat", "Visagalis", "0.0.3")]
    public class StickyChat : RustPlugin
    { 
        public enum ChatType
        {
            GENERAL = 0,
            CLAN = 1,
            PRIVATE = 2,
            REPLY = 3
        }

        public class StickyInfo
        {
            public string details = "";
            public ChatType type = ChatType.GENERAL;
        }

        Dictionary<BasePlayer, StickyInfo> stickies = new Dictionary<BasePlayer, StickyInfo>();

        [HookMethod("OnPlayerDisconnected")]
        void OnPlayerDisconnected(BasePlayer player)
        {
            stickies.Remove(player);
        }

        [PluginReference("Clans")]
        Plugin clanLib;
        [PluginReference("PM")]
        Plugin pmLib;

        [ChatCommand("ct")]
        private void clanChat(BasePlayer player, string command, string[] args)
        {
            if (!stickies.Any(x => x.Key.userID == player.userID))
            {
                stickies.Add(player, new StickyInfo() { type = ChatType.CLAN });
                SendReply(player, "You are currently chatting in {0} chat. Type /{1} to switch to {2}.", "CLAN", "gt", "GENERAL");
            }
        }

        [ChatCommand("gt")]
        private void generalChat(BasePlayer player, string command, string[] args)
        {
            if (stickies.Any(x => x.Key.userID == player.userID))
            {
                stickies.Remove(player);
                SendReply(player, "You are currently chatting in {0} chat.", "GENERAL");
            }
        }

        [ChatCommand("rt")]
        private void replyChat(BasePlayer player, string command, string[] args)
        {
            if (!stickies.Any(x => x.Key.userID == player.userID))
            {
                stickies.Add(player, new StickyInfo() { type = ChatType.REPLY });
                SendReply(player, "You are currently chatting in {0} chat. Type /{1} to switch to {2}.", "REPLY", "gt", "GENERAL");
            }
        }

        [ChatCommand("pt")]
        private void privateChat(BasePlayer player, string command, string[] args)
        {
            if(args.Length != 1)
            {
                SendReply(player, "You need to specify player name you want to sticky message to. /pt [name]");
            }
            if (!stickies.Any(x => x.Key.userID == player.userID))
            {
                stickies.Add(player, new StickyInfo() { type = ChatType.PRIVATE, details = args[0] });
                SendReply(player, "You are currently chatting in {0} chat with {3}. Type /{1} to switch to {2}.", "PRIVATE", "gt", "GENERAL", args[0]);
            }
        }

        object OnPlayerChat(ConsoleSystem.Arg arg)
        {
            BasePlayer player = (BasePlayer)arg.connection.player;
            string message = arg.GetString(0, "text");
            if (stickies.Any(x => x.Key.userID == player.userID && x.Value.type == ChatType.CLAN))
            {
                clanLib.Call("cmdChatClanchat", player, "", new string[] { message });
                return false;
            }
            else if (stickies.Any(x => x.Key.userID == player.userID && x.Value.type == ChatType.REPLY))
            {
                pmLib.Call("cmdPmReply", player, "", new string[] { message });
                return false;
            }
            else if (stickies.Any(x => x.Key.userID == player.userID && x.Value.type == ChatType.PRIVATE))
            {
                StickyInfo result = (from el in stickies
                             where el.Key.userID == player.userID && el.Value.type == ChatType.PRIVATE
                             select el.Value).First();
                pmLib.Call("cmdPm", player, "", new string[] { result.details, message });
                return false;
            }

            return null;
        }

        private void SendHelpText(BasePlayer player)
        {
            PrintToChat(player, "You can use following sticky chat commands:"
                + "\n<color=green>/gt</color> - Stick to general chat."
                + "\n<color=green>/ct</color> - Stick to clan chat."
                + "\n<color=green>/pt [name]</color> - Stick to [name]'s chat."
                + "\n<color=green>/rt</color> - Stick to reply chat.");
        }

        private int PlayerStickyState(BasePlayer player)
        {
            if (stickies.Any(x => x.Key.userID == player.userID))
            {
                StickyInfo result = (from el in stickies
                                     where el.Key.userID == player.userID
                                     select el.Value).First();
                return Convert.ToInt32(result.type);
            }
            return 0;
        }
    }
}
