
using CodeHatch.Common;
using CodeHatch.Engine;
using CodeHatch.Engine.Core.Commands;
using CodeHatch.Engine.Networking;

namespace Oxide.Plugins
{
    [Info("Permission Command", "Mughisi", 1.0)]
    [Description("Permission command replacement until this is fixed by CodeHatch.")]
    public class PermissionCommand : ReignOfKingsPlugin
    {
        [ChatCommand("permission")]
        private void Permission(Player player, string command, string[] args)
        {
            if (!player.HasPermission("codehatch.command.admin.permissions")) return;
            var cmdinfo = new CommandInfo(player.Id, (command + " " + args.JoinToString(" ")).Trim());
            var handler = UnityEngine.Object.FindObjectOfType<CoreCommandHandler>();
            handler?.Permission(cmdinfo);
            if (args.Length == 0)
            {
                SendReply(player, "[008000]Permissions[-]: Available commands:");
                SendReply(player, "/permission groups - [333333]Shows a list of the defined groups in the config.[-]");
                SendReply(player, "/permission group [groupName] makedefault - [333333]Makes [groupName] the default group.[-]");
                SendReply(player, "/permission group [groupName] add|remove [permission] - [333333]Adds/Removes a [permission] for a [groupName].[-]");
                SendReply(player, "/permission group [groupName] add|remove - [333333]Adds/Removes [groupName] to/from the config.[-]");
                SendReply(player, "/permission group [groupName] - [333333]Gets the permission info for [groupName].[-]");
                SendReply(player, "/permission group [groupName] inherit [parentGroup] - [333333]Makes [groupName] inherit [parentGroup].[-]");
                SendReply(player, "/permission reload - [333333]Reloads the permissions from the config.[-]");
                SendReply(player, "/permission user [userName] removegroup [groupName] - [333333]Removes [userName] from [groupName].[-]");
                SendReply(player, "/permission user [userName] addgroup [groupName] - [333333]Adds [userName] to [groupName].[-]");
                SendReply(player, "/permission group [groupName] chatformat [format].. - [333333]Sets the chat format for [groupName].[-]");
                SendReply(player, "/permission user [userName] - [333333]Shows the permission info for [userName].[-]");
                SendReply(player, "/permission users - [333333]Shows a list of the defined users in the config.[-]");
                SendReply(player, "/permission user [userName] nameformat [format].. - [333333]Sets the name format for [userName].[-]");
                SendReply(player, "/permission user [userName] chatformat [format].. - [333333]Sets the chat format for [userName].[-]");
                SendReply(player, "/permission user [userName] setprimary [groupName] - [333333]Sets the primary group for [userName] to [groupName].[-]");
                SendReply(player, "/permission user [userName] add|remove - [333333]Adds/Removes [userName] to/from the config.[-]");
                SendReply(player, "/permission user [userName] add|remove [permission] - [333333]Adds/Removes a [permission] for a [userName].[-]");
                SendReply(player, "/permission group [groupName] nameformat [format].. - [333333]Sets the name format for [groupName].[-]");
            }
        }
    }
}