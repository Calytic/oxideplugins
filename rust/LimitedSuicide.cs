using Oxide.Core.Plugins;
using System;
using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("LimitedSuicide", "playrust.io / dcode", "1.0.2", ResourceId = 835)]
    public class LimitedSuicide : RustPlugin
    {
        // Default configuration: Once per 300 seconds
        private static int defaultLimit = 1;
        private static int defaultTimespan = 300;

        // Loaded configuration
        private int limit = defaultLimit;
        private int timespan = defaultTimespan;

        // Suicide timestamps
        private Dictionary<ulong, List<DateTime>> suicides = new Dictionary<ulong, List<DateTime>>();

        protected override void LoadDefaultConfig() {
            Config["limit"] = defaultLimit;
            Config["timespan"] = defaultTimespan;
            SaveConfig();
        }

        [HookMethod("OnServerInitialized")]
        void OnServerInitialized() {
            try {
                limit = Convert.ToInt32(Config["limit"]);
                if (limit < 1) limit = 1;
            } catch (Exception ex) {
                Puts("[LimitedSuicide] Illegal configuration value for 'limit': {0}", ex.Message);
            }
            try {
                timespan = Convert.ToInt32(Config["timespan"]);
                if (timespan < 1) timespan = 1;
            } catch (Exception ex) {
                Puts("[LimitedSuicide] Illegal configuration value for 'timespan': {0}", ex.Message);
            }
        }

        [HookMethod("OnRunCommand")]
        object OnRunCommand(ConsoleSystem.Arg arg) {
            if (arg.connection == null || arg.connection.player == null || arg.cmd.name != "kill")
                return null;
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return null;
            List<DateTime> times;
            DateTime now = DateTime.UtcNow;
            if (suicides.TryGetValue(player.userID, out times)) {
                int n = 0;
                for (var i = 0; i < times.Count;) {
                    var time = times[i];
                    if (time < now.AddSeconds(-timespan))
                        times.RemoveAt(i);
                    else {
                        ++n; ++i;
                    }
                }
                if (n >= limit) {
                    player.ChatMessage("You may only suicide " + (limit == 1 ? "once" : limit + " times") + " per " + timespan + " seconds.");
                    return true;
                }
                times.Add(now);
            } else {
                times = new List<DateTime>();
                times.Add(now);
                suicides.Add(player.userID, times);
            }
            return null;
        }
    }
}
