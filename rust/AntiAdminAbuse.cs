using System;
namespace Oxide.Plugins
{
    [Info("AntiAdminAbuse", "Norn", 0.2, ResourceId = 12693)]
    [Description("Prevent moderator abuse.")]
    public class AntiAdminAbuse : RustPlugin
    {
        object OnRunCommand(ConsoleSystem.Arg arg)
        {
            try
            {
                if (arg.cmd.isCommand && arg.cmd.namefull == "global.setinfo")
                {
                    if(arg.ArgsStr.Contains("global.god") && arg.ArgsStr.Contains("True"))
                    {
                        if (!Convert.ToBoolean(Config["Admin", "GodEnabled"]))
                        {
                            if ((BasePlayer.FindByID(arg.connection.userid).IsConnected()))
                            {
                                BasePlayer player = BasePlayer.FindByID(arg.connection.userid);
                                if (player != null)
                                {
                                    if (player.net.connection.authLevel >= Convert.ToInt32(Config["Admin", "MinLevel"]))
                                    {
                                        PrintToChat(player, Config["Messages", "NoGod"].ToString());
                                        if (Convert.ToBoolean(Config["Admin", "KickAdmin"])) { player.Kick(Config["Messages", "NoGodAllowed"].ToString()); }
                                        if (Convert.ToBoolean(Config["Admin", "PrintToConsole"])) { Puts(arg.connection.username + " [ " + arg.connection.userid + " ] has tried to enable GodMode via F1. [GOD]"); }
                                    }
                                }
                            }
                        }
                        return false;
                    }
                }
                if (arg.cmd.isAdmin && arg.cmd.isCommand)
                {
                    string command = arg.cmd.name.ToString().ToLower();
                    int authlevel = arg.connection.authLevel;
                    switch (command)
                    {

                            case "givearm":
                            {
                                if (authlevel >= Convert.ToInt32(Config["Admin", "MinLevel"]) && authlevel <= Convert.ToInt32(Config["Admin", "MaxLevel"]))
                                {
                                    if (Convert.ToBoolean(Config["Admin", "OnlyMaxCanSpawn"]))
                                    {
                                        if (authlevel != Convert.ToInt32(Config["Admin", "MaxLevel"]))
                                        {
                                            PrintToChat(BasePlayer.FindByID(arg.connection.userid), Config["Messages", "Disabled"].ToString());
                                            if (Convert.ToBoolean(Config["Admin", "PrintToConsole"]))
                                            {
                                                Puts(arg.connection.username + " [ " + arg.connection.userid + " ] has tried to spawn an item via F1. [ARM]");
                                            }
                                            return false;
                                        }
                                    }
                                }
                                break;
                            }
                            case "giveid":
                            {
                                if (authlevel >= Convert.ToInt32(Config["Admin", "MinLevel"]) && authlevel <= Convert.ToInt32(Config["Admin", "MaxLevel"]))
                                {
                                    if (Convert.ToBoolean(Config["Admin", "OnlyMaxCanSpawn"]))
                                    {
                                        if (authlevel != Convert.ToInt32(Config["Admin", "MaxLevel"]))
                                        {
                                            PrintToChat(BasePlayer.FindByID(arg.connection.userid), Config["Messages", "Disabled"].ToString());
                                            if (Convert.ToBoolean(Config["Admin", "PrintToConsole"]))
                                            {
                                                Puts(arg.connection.username + " [ " + arg.connection.userid + " ] has tried to spawn an item via F1. [ID]");
                                            }
                                            return false;
                                        }
                                    }
                                }
                                break;
                            }
                            case "givebp":
                            {
                                if (authlevel >= Convert.ToInt32(Config["Admin", "MinLevel"]) && authlevel <= Convert.ToInt32(Config["Admin", "MaxLevel"]))
                                {
                                    if (Convert.ToBoolean(Config["Admin", "OnlyMaxCanSpawn"]))
                                    {
                                        if (authlevel != Convert.ToInt32(Config["Admin", "MaxLevel"]))
                                        {
                                            PrintToChat(BasePlayer.FindByID(arg.connection.userid), Config["Messages", "Disabled"].ToString());
                                            if (Convert.ToBoolean(Config["Admin", "PrintToConsole"]))
                                            {
                                                Puts(arg.connection.username + " [ " + arg.connection.userid + " ] has tried to spawn an item via F1. [BP]");
                                            }
                                            return false;
                                        }
                                    }
                                }
                                break;
                        }
                    }
                }
            }
            catch { }
            return null;
        }
        protected override void LoadDefaultConfig()
        {
            Puts("No configuration file found, generating...");
            Config.Clear();

            // --- [ MESSAGES ] ---

            Config["Messages", "Disabled"] = "Spawning items has been <color=red>disabled</color> by the server owner.";
            Config["Messages", "NoGod"] = "God Mode has been <color=red>disabled</color> by the server owner.";
            Config["Messages", "NoGodAllowed"] = "God Mode is not allowed, even for administrators.";

            // --- [ ADMIN ] ---
            Config["Admin", "KickAdmin"] = false;
            Config["Admin", "PrintToConsole"] = true;
            Config["Admin", "MinLevel"] = 1;
            Config["Admin", "MaxLevel"] = 2;
            Config["Admin", "OnlyMaxCanSpawn"] = true;
            Config["Admin", "GodEnabled"] = false;
        }
    }
}