using System.Collections.Generic;
using Oxide.Core;

namespace Oxide.Plugins
{
    [Info("ColouredNames", "PsychoTea", "1.0.0")]
    internal class ColouredNames : RustPlugin
    {
        class StoredData
        {
            public Dictionary<ulong, string> colour = new Dictionary<ulong, string>();
        }
        StoredData storedData;

        void Init() => permission.RegisterPermission("colourednames.colouredName", this);

        void Loaded() => storedData = Interface.GetMod().DataFileSystem.ReadObject<StoredData>("ColouredNames");

        object OnPlayerChat(ConsoleSystem.Arg arg)
        {
            BasePlayer player = (BasePlayer)arg.connection.player;

            if (storedData.colour.ContainsKey(player.userID))
            {
                if (storedData.colour[player.userID] == "clear") return null;
                //ConsoleSystem.Broadcast("chat.add", player.userID.ToString(), string.Format("<color=" + storedData.colour[player.userID] + ">" + player.displayName + "</color>: " + arg.GetString(0, "text")), 1.0);
                string message = string.Format("<color=" + storedData.colour[player.userID] + ">" + player.displayName + "</color>: " + arg.GetString(0, "text"));
                foreach (BasePlayer bp in BasePlayer.activePlayerList)
                    rust.SendChatMessage(bp, message, null, player.userID.ToString());
                Interface.Oxide.ServerConsole.AddMessage("[CHAT] " + player.displayName + ": " + arg.GetString(0, "text"));
                return true;
            }
            else return null;
        }

        [ChatCommand("colour")]
        private void ColourCmd(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.userID.ToString(), "colourednames.colouredName")) SendReply(player, "<color=red>You do not have permission!</color>");
            else if (args.Length == 0) SendReply(player, "<color=aqua>Incorrect syntax!</color><color=orange> /colour {colour}.\nFor a more information do /colours.</color>");
            else
            {
                if (!storedData.colour.ContainsKey(player.userID)) storedData.colour.Add(player.userID, args[0]);
                else if (storedData.colour.ContainsKey(player.userID)) storedData.colour[player.userID] = args[0];

                if (args[0] == "clear") SendReply(player, "<color=aqua>ColouredNames: </color><color=orange>Name colour removed!</color>");
                else SendReply(player, "<color=aqua>ColouredNames: </color><color=orange>Name colour changed to </color><color={0}>{0}</color><color=orange>!</color>", args[0]);

                Interface.GetMod().DataFileSystem.WriteObject("ColouredNames", storedData);
            }
        }

        [ChatCommand("colours")]
        private void ColoursCmd(BasePlayer player, string command, string[] args) =>  
            SendReply(player, @"<color=aqua>ColouredNames:</color><color=orange> You may use any colour used in HTML.
                                Eg: ""</color><color=red>red</color><color=orange>"", ""</color><color=blue>blue</color><color=orange>"", ""</color><color=green>green</color><color=orange>"" etc.
                                Or you may use any hex code, eg ""</color><color=#FFFF00>#FFFF00</color><color=orange>"".
                                To remove your colour, use ""clear"".
                                An invalid colour will default to </color>white<color=orange>.</color>");
    }

}
