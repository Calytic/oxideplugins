This is a simple static (non-file saving) variable system for devs. Useful for plugins intended without file usage.


(More to come, cross plugin temporary variable communication etc etc. This is the bones for something special I hope.)

**Hooks:**

````
// Main Hooks

InitPlugin(Plugin plugin, bool debug = false)

PluginExists(Plugin plugin)

DestroyPlugin(Plugin plugin, bool debug = false)


// General Variable Hooks

SetStaticVariable(Plugin plugin, string variable, string value, bool debug = false)

GetStaticVariable(Plugin plugin, string variable, bool debug = false)

RemoveStaticVariable(Plugin plugin, string variable, bool debug = false)


// Player Variable Hooks

InitPlayer(Plugin plugin, BasePlayer player, bool debug = false)

RemovePlayer(Plugin plugin, BasePlayer player, bool debug = false)

PlayerExists(Plugin plugin, BasePlayer player)


SetStaticPlayerVariable(Plugin plugin, BasePlayer player, string variable, string value, bool debug = false)

GetStaticPlayerVariable(Plugin plugin, BasePlayer player, string variable, bool debug = false)

RemoveStaticPlayerVariable(Plugin plugin, BasePlayer player, string variable, bool debug = false)

PlayerHasVariables(Plugin plugin, BasePlayer player)
````


**Test Plugin (Plugin & Player Uptime):
**
Code (C#):
````
using System;
using Oxide.Core.Plugins;
namespace Oxide.Plugins
{

    [Info("TestPlugin", "Norn", 0.1, ResourceId = 1419)]

    [Description("Demonstration of MagicVariables.")]

    public class TestPlugin : RustPlugin

    {

        [PluginReference]

        Plugin MagicVariables;

        Timer uptime_timer;

        void OnServerInitialized()

        {

            if (MagicVariables)

            {

                if (!Convert.ToBoolean(MagicVariables.Call("PluginExists", this)))

                {

                    MagicVariables.Call("InitPlugin", this, true);

                }

                uptime_timer = timer.Repeat(1, 0, () => UptimeTimer());

            }

        }

        void OnPlayerInit(BasePlayer player)

        {

            if (!Convert.ToBoolean(MagicVariables.Call("PlayerExists", this, player)))

            {

                MagicVariables.Call("InitPlayer", this, player, true);

                MagicVariables.Call("SetStaticPlayerVariable", this, player, "connectionTime", "0");

            }

        }

        void OnPlayerDisconnected(BasePlayer player, string reason)

        {

            if (Convert.ToBoolean(MagicVariables.Call("PlayerExists", this, player)))

            {

                MagicVariables.Call("RemovePlayer", this, player, true);

            }

        }

        private void Unload()

        {

            if (uptime_timer != null)

            {

                uptime_timer.Destroy();

            }

            MagicVariables.Call("DestroyPlugin", this, true);

        }

        [ChatCommand("uptime")]

        void cmdUptime(BasePlayer player, string cmd, string[] args)

        {

            if (!Convert.ToBoolean(MagicVariables.Call("PlayerExists", this, player)))

            {

                MagicVariables.Call("InitPlayer", this, player, true);

                MagicVariables.Call("SetStaticPlayerVariable", this, player, "connectionTime", "1");

            }

            int uptime = Convert.ToInt32(MagicVariables.Call("GetStaticVariable", this, "upTime"));

            TimeSpan t = TimeSpan.FromSeconds(uptime);

            string answer = string.Format("{0:D2}h:{1:D2}m:{2:D2}s:{3:D3}ms", t.Hours, t.Minutes, t.Seconds, t.Milliseconds);

            PrintToChat(player, "<color=yellow>" + this.Title + "</color>: " + answer + " <color=green>plugin uptime</color>.");

            int player_uptime = Convert.ToInt32(MagicVariables.Call("GetStaticPlayerVariable", this, player, "connectionTime"));

            t = TimeSpan.FromSeconds(player_uptime);

            answer = string.Format("{0:D2}h:{1:D2}m:{2:D2}s:{3:D3}ms", t.Hours, t.Minutes, t.Seconds, t.Milliseconds);

            PrintToChat(player, "<color=yellow>" + this.Title + "</color>: " + answer + " <color=green>player uptime</color>.");

        }

        private void UptimeTimer()

        {

            int uptime = Convert.ToInt32(MagicVariables.Call("GetStaticVariable", this, "upTime"));

            int new_uptime = uptime + 1;

            MagicVariables.Call("SetStaticVariable", this, "upTime", new_uptime.ToString());

            foreach (BasePlayer player in BasePlayer.activePlayerList)

            {

                if (player != null)

                {

                    int player_uptime = Convert.ToInt32(MagicVariables.Call("GetStaticPlayerVariable", this, player, "connectionTime"));

                    player_uptime = player_uptime + 1;

                    MagicVariables.Call("SetStaticPlayerVariable", this, player, "connectionTime", player_uptime.ToString());

                }

            }

        }

    }
}
````