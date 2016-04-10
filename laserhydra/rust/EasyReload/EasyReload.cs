using System.Collections.Generic;
using System.Reflection;
using System;
using System.Data;
using UnityEngine;
using Oxide.Core;
using Rust;

namespace Oxide.Plugins
{
    [Info("Easy Reload", "LaserHydra", "2.0.0", ResourceId = 854)]
    [Description("Reload your plugins easily")]
    class EasyReload : RustPlugin
    {
        void Loaded()
        {
            if (!permission.PermissionExists("canReload")) permission.RegisterPermission("canReload", this);
        }
        [ChatCommand("reload")]
        void cmdReload(BasePlayer player, string cmd, string[] args)
        {
            string uid = Convert.ToString(player.userID);
            if (!permission.UserHasPermission(uid, "canReload"))
            {
                SendChatMessage(player, "RELOAD", "You have no permission to use this command!");
                return;
            }
            if (args.Length < 1)
            {
                SendChatMessage(player, "RELOAD", "Reloading plugins...");
                ConsoleSystem.Run.Server.Normal("oxide.reload *");
                SendChatMessage(player, "RELOAD", "All plugins reloaded!");

            }
            if (args.Length == 1)
            {
                string pluginname = GetPlugin(args[0], player, "RELOAD");
                string filename = GetPluginFile(pluginname);
                if (pluginname != "")
                {
                    ConsoleSystem.Run.Server.Normal("oxide.reload " + filename);
                    SendChatMessage(player, "RELOAD", "Reloaded plugin <color=orange>" + pluginname + "</color>!");
                }
            }
        }
        //--------------------------->   Player finding   <---------------------------//

        string GetPlugin(string searchedPlugin, BasePlayer executer, string prefix)
        {
            string targetPlugin = "";
            List<string> foundPlugins = new List<string>();
            string searchedLower = searchedPlugin.ToLower();
            foreach (var plugin in plugins.GetAll())
            {
                if (plugin.Author == "Oxide Team")
                {
                    continue;
                }

                string display = plugin.Title;
                string displayLower = display.ToLower();

                if (!displayLower.Contains(searchedLower))
                {
                    continue;
                }
                if (displayLower.Contains(searchedLower))
                {
                    foundPlugins.Add(display);
                }
            }
            var matchingPlugins = foundPlugins.ToArray();

            if (matchingPlugins.Length == 0)
            {
                SendChatMessage(executer, prefix, "No matching plugins found!");
            }

            if (matchingPlugins.Length > 1)
            {
                SendChatMessage(executer, prefix, "Multiple plugins found:");
                string multiplePlugins = "";
                foreach (string matchingplugin in matchingPlugins)
                {
                    if (multiplePlugins == "")
                    {
                        multiplePlugins = "<color=yellow>" + matchingplugin + "</color>";
                        continue;
                    }

                    if (multiplePlugins != "")
                    {
                        multiplePlugins = multiplePlugins + ", " + "<color=yellow>" + matchingplugin + "</color>";
                    }

                }
                SendChatMessage(executer, prefix, multiplePlugins);
            }

            if (matchingPlugins.Length == 1)
            {
                targetPlugin = matchingPlugins[0];
            }
            return targetPlugin;
        }

        string GetPluginFile(string pluginname)
        {
            string filename = "";
            foreach (var plugin in plugins.GetAll())
            {
                if (plugin.Author == "Oxide Team")
                {
                    continue;
                }

                string display = plugin.Title;

                if (display != pluginname)
                {
                    continue;
                }
                if (display == pluginname)
                {
                    filename = plugin.Name;
                    break;
                }
            }
            return filename;
        }

        //---------------------------->   Chat Sending   <----------------------------//

        void BroadcastChat(string prefix, string msg)
        {
            PrintToChat("<color=orange>" + prefix + "</color>: " + msg);
        }

        void SendChatMessage(BasePlayer player, string prefix, string msg)
        {
            SendReply(player, "<color=orange>" + prefix + "</color>: " + msg);
        }

        //---------------------------------------------------------------------------//
    }
}
