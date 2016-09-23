using Oxide.Core.Libraries.Covalence;
using System.Collections.Generic;
using System.Linq;
using System;

namespace Oxide.Plugins
{
    [Info("Timed Execute", "PaiN", 0.6, ResourceId = 1937)]
    [Description("Execute commands every (x) seconds.")]
    class TimedExecute : CovalencePlugin
    {
        private Timer repeater;
        private Timer chaintimer;
        private Timer checkreal;

        void Loaded()
        {
            checkreal = timer.Repeat(1, 0, () => RealTime());
            RunRepeater();
            RunOnce();

            Puts($"Timer-Once is {(Convert.ToBoolean(Config["EnableTimerOnce"]) == true ? "ON" : "OFF")}");
            Puts($"Timer-Repeat is {(Convert.ToBoolean(Config["EnableTimerRepeat"]) == true ? "ON" : "OFF")}");
            Puts($"Timer-RealTime is {(Convert.ToBoolean(Config["EnabledRealTime-Timer"]) == true ? "ON" : "OFF")}");
        }

        void RunRepeater()
        {
            if (repeater != null)
            {
                repeater.Destroy();
            }
            if (Convert.ToBoolean(Config["EnableTimerRepeat"]) == true)
            {
                foreach (var cmd in Config["TimerRepeat"] as Dictionary<string, object>)
                {
                    repeater = timer.Repeat(Convert.ToSingle(cmd.Value), 0, () => {
                        if (SplitCommand(cmd.Key).Value.Length == 0)
                            server.Command(SplitCommand(cmd.Key).Key, null);
                        else
                            server.Command(SplitCommand(cmd.Key).Key, string.Join(" ", SplitCommand(cmd.Key).Value));

                        Puts(string.Format("ran CMD: {0} || ARGS: {1}", SplitCommand(cmd.Key).Key, string.Join(" ", SplitCommand(cmd.Key).Value)));
                    });
                }
            }
        }

        void RealTime()
        {
            if (Convert.ToBoolean(Config["EnabledRealTime-Timer"]) == true)
            {
                foreach (var cmd in Config["RealTime-Timer"] as Dictionary<string, object>)
                {
                    if (System.DateTime.Now.ToString("HH:mm:ss") == cmd.Key.ToString())
                    {
                        if (SplitCommand(cmd.Value.ToString()).Value.Length == 0)
                            server.Command(SplitCommand(cmd.Value.ToString()).Key, null);
                        else
                            server.Command(SplitCommand(cmd.Value.ToString()).Key, string.Join(" ", SplitCommand(cmd.Value.ToString()).Value));

                        Puts(string.Format("ran CMD: {0} || ARGS: {1}", SplitCommand(cmd.Value.ToString()).Key, string.Join(" ", SplitCommand(cmd.Value.ToString()).Value)));
                    }
                }
            }
        }
        void RunOnce()
        {
            if (chaintimer != null)
            {
                chaintimer.Destroy();
            }
            if (Convert.ToBoolean(Config["EnableTimerOnce"]) == true)
            {
                foreach (var cmdc in Config["TimerOnce"] as Dictionary<string, object>)
                {
                    chaintimer = timer.Once(Convert.ToSingle(cmdc.Value), () => {
                        if (SplitCommand(cmdc.Key).Value.Length == 0)
                            server.Command(SplitCommand(cmdc.Key).Key, null);
                        else
                            server.Command(SplitCommand(cmdc.Key).Key, string.Join(" ", SplitCommand(cmdc.Key).Value));

                        Puts(string.Format("ran CMD: {0} || ARGS: {1}", SplitCommand(cmdc.Key).Key, string.Join(" ", SplitCommand(cmdc.Key).Value)));
                    });
                }
            }
        }

        void Unloaded()
        {
            if (repeater != null)
            {
                repeater.Destroy();
                Puts("Destroyed the *Repeater* timer!");
            }
            if (chaintimer != null)
            {
                chaintimer.Destroy();
                Puts("Destroyed the *Timer-Once* timer!");
            }
            if (checkreal != null)
            {
                checkreal.Destroy();
                Puts("Destroyed the *RealTime* timer!");
            }
        }

        Dictionary<string, object> repeatcmds = new Dictionary<string, object>();
        Dictionary<string, object> chaincmds = new Dictionary<string, object>();
        Dictionary<string, object> realtimecmds = new Dictionary<string, object>();

        protected override void LoadDefaultConfig()
        {
            repeatcmds.Add("command1 arg", 300);
            repeatcmds.Add("command2 'msg'", 300);
            Puts("Creating a new configuration file!");
            if (Config["TimerRepeat"] == null) Config["TimerRepeat"] = repeatcmds;


            chaincmds.Add("command1 'msg'", 60);
            chaincmds.Add("command2 'msg'", 120);
            chaincmds.Add("command3 arg", 180);
            chaincmds.Add("command4 arg", 181);
            if (Config["TimerOnce"] == null) Config["TimerOnce"] = chaincmds;

            if (Config["EnableTimerRepeat"] == null) Config["EnableTimerRepeat"] = true;
            if (Config["EnableTimerOnce"] == null) Config["EnableTimerOnce"] = true;
            if (Config["EnabledRealTime-Timer"] == null) Config["EnabledRealTime-Timer"] = true;

            realtimecmds.Add("16:00:00", "command1 arg");
            realtimecmds.Add("16:30:00", "command2 arg");
            realtimecmds.Add("17:00:00", "command3 arg");
            realtimecmds.Add("18:00:00", "command4 arg");
            if (Config["RealTime-Timer"] == null) Config["RealTime-Timer"] = realtimecmds;

        }

        [Command("resetoncetimer", "global.resetoncetimer")]
        void cmdResOnceTimer(IPlayer player, string cmd, string[] args)
        {
            RunOnce();
        }

        KeyValuePair<string, string[]> SplitCommand(string cmd)
        {
            string[] CmdSplit = cmd.Split(' ');
            string command = CmdSplit[0];
            var args = CmdSplit.Skip(1);

            return new KeyValuePair<string, string[]>(command, args.ToArray());
        }

        public static class StringExtensions
        {
            public static bool IsNullOrWhiteSpace(string value)
            {
                if (value != null)
                {
                    for (int i = 0; i < value.Length; i++)
                    {
                        if (!char.IsWhiteSpace(value[i]))
                        {
                            return false;
                        }
                    }
                }
                return true;
            }
        }
    }
}