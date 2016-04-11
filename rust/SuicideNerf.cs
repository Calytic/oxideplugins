using System;
using UnityEngine;
using Oxide.Core.Plugins;
using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("SuicideNerf", "Kyrah Abattoir", "0.1", ResourceId = 1873)]
    [Description("Forces you to bleedout when using 'kill' and adds a cooldown.")]
    class SuicideNerf : RustPlugin
    {
        //CONFIGURATION AREA
        //Cause i'm too dumb to write my own configuration code
        //And too proud to copy someone else's
        private int     cfgMinTimeBetweenSuicideAttempts = 300; //How many seconds between suicide attempts.
        private bool    cfgDoBleedout = true;                   //Set this to false to use the normal rust suicide method.

        private Dictionary<ulong, float> nextSuicideTime = new Dictionary<ulong, float>();

        [HookMethod("OnRunCommand")]
        private object OnRunCommand(ConsoleSystem.Arg arg)
        {
            if (arg?.cmd == null || arg.Player() == null)
                return null;

            BasePlayer ply = arg.Player();
            if (arg.cmd.namefull == "global.kill")
            {
                if (ply.IsWounded())
                    PrintToConsole(ply, "You have to wait until you bleed out or are rescued.");
                else
                {
                    ulong steamID = ply.userID;
                    float next_time;
                    if (nextSuicideTime.TryGetValue(steamID, out next_time))
                    {
                        if(Time.realtimeSinceStartup >= next_time)
                        {
                            nextSuicideTime[steamID] = Time.realtimeSinceStartup + cfgMinTimeBetweenSuicideAttempts;
                            return DoSuicide(ply);
                        }
                        else
                            PrintToConsole(ply,$"You have to wait {(int)(next_time - Time.realtimeSinceStartup)} second(s) before you can suicide again.");
                    }
                    else
                    {
                        nextSuicideTime.Add(steamID, Time.realtimeSinceStartup + cfgMinTimeBetweenSuicideAttempts);
                        return DoSuicide(ply);
                    }
                }
                return true;
            }
            return null;
        }

        private object DoSuicide(BasePlayer ply)
        {
            if (cfgDoBleedout)
            {
                ply.StartWounded();
                return true;
            }
            return null;
        }
    }
    
}