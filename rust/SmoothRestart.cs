using System;
using System.Text.RegularExpressions;

namespace Oxide.Plugins
{
    [Info("SmoothRestart", "Visagalis", "0.0.1")]
    public class SmoothRestart : RustPlugin
    {
        private DateTime restartTime = DateTime.MinValue;
        private Timer activeTimer = null;

        void OnServerInitialized()
        {
            if (!permission.PermissionExists("smoothrestart.canrestart"))
                permission.RegisterPermission("smoothrestart.canrestart", this);
        }

        [ConsoleCommand("srestart")]
        private void smoothRestartConsoleCommand(ConsoleSystem.Arg arg)
        {
            if (arg.connection != null)
                return;

            if (arg.Args != null && arg.Args.Length == 1)
            {
                if (arg.Args[0].ToLower() == "stop")
                    StopRestart();
                else
                {
                    uint seconds = 0;
                    if (stringToSeconds(arg.Args[0], ref seconds))
                        DoSmoothRestart(seconds);
                    else
                        Puts("Incorrect <timer[h/m/s]> format! Must be number!");
                }
            }
            else
                Puts("Incorrect syntax! Must use: srestart <timer[h/m/s]>/stop");
        }

        [ChatCommand("srestart")]
        private void smoothRestartCommand(BasePlayer player, string command, string[] args)
        {
            if (permission.UserHasPermission(player.UserIDString, "smoothrestart.canrestart") || player.net.connection.authLevel > 0)
            {
                if (args != null && args.Length == 1)
                {
                    if (args[0].ToLower() == "stop")
                        StopRestart();
                    else
                    {
                        uint seconds = 0;
                        if (stringToSeconds(args[0], ref seconds))
                            DoSmoothRestart(seconds);
                        else
                            player.ChatMessage("Incorrect <timer>[h/m/s] format! Must be number!");
                    }
                }
                else
                    player.ChatMessage("Incorrect syntax! Must use: /srestart <timer[h/m/s]>/stop");
            }
        }

        private void StopRestart()
        {
            if (activeTimer != null)
            {
                activeTimer.Destroy();
                activeTimer = null;


                Puts("Server restart stopped!");
                rust.BroadcastChat("Server restart stopped!");
            }
        }

        private void DoSmoothRestart(uint seconds)
        {
            restartTime = DateTime.UtcNow.AddSeconds(seconds);
            if (activeTimer != null)
            {
                activeTimer.Destroy();
                activeTimer = null;
            }
            RestartTimerCall();
        }

        private void RestartTimerCall()
        {
            TimeSpan timeLeft = restartTime - DateTime.UtcNow;

            Puts("Server will restart in: " + ReadableTimeSpan(timeLeft));
            rust.BroadcastChat("Server will restart in: " + ReadableTimeSpan(timeLeft));

            TimeSpan timeUntilTimer = TimeSpan.Zero;
            if (timeLeft.Hours > 0)
            {
                timeUntilTimer = timeLeft - new TimeSpan(1,0,0);
            }
            else if (timeLeft.Minutes >= 30)
            {
                timeUntilTimer = timeLeft - new TimeSpan(0, 30, 0);
            }
            else if (timeLeft.Minutes >= 10)
            {
                timeUntilTimer = timeLeft - new TimeSpan(0, 10, 0);
            }
            else if (timeLeft.Minutes >= 5)
            {
                timeUntilTimer = timeLeft - new TimeSpan(0, 5, 0);
            }
            else if (timeLeft.Minutes >= 3)
            {
                timeUntilTimer = timeLeft - new TimeSpan(0, 3, 0);
            }
            else if (timeLeft.Minutes >= 2)
            {
                timeUntilTimer = timeLeft - new TimeSpan(0, 2, 0);
            }
            else if (timeLeft.Minutes >= 1)
            {
                timeUntilTimer = timeLeft - new TimeSpan(0, 1, 0);
            }
            else
            {
                rust.RunServerCommand("restart");
                activeTimer.Destroy();
                activeTimer = null;
            }

            if (timeUntilTimer != TimeSpan.Zero)
                activeTimer = timer.Once((float)timeUntilTimer.TotalSeconds, RestartTimerCall);
        }

        private bool stringToSeconds(string timeString, ref uint stamp)
        {
            string patern = @"(\d*)[hms]";
            Regex regex = new Regex(patern, RegexOptions.IgnoreCase);
            Match match = regex.Match(timeString);
            if (match.Success)
            {
                while (match.Success)
                {
                    if (match.ToString().ToLower().Replace(match.Groups[1].Value, string.Empty) == "h")
                    {
                        stamp += uint.Parse(match.Groups[1].Value)*60*60;
                    }
                    else if (match.ToString().ToLower().Replace(match.Groups[1].Value, string.Empty) == "m")
                    {
                        stamp += uint.Parse(match.Groups[1].Value)*60;
                    }
                    else if (match.ToString().ToLower().Replace(match.Groups[1].Value, string.Empty) == "s")
                    {
                        stamp += uint.Parse(match.Groups[1].Value);
                    }
                    match = match.NextMatch();
                }
                return true;
            }
            return false;
        }

        public static string ReadableTimeSpan(TimeSpan span)
        {
            if(span.Seconds == 59) // Fix rounding problem.
                span = span.Add(new TimeSpan(0, 0, 1));

            string formatted = string.Format("{0}{1}{2}{3}{4}",
                (span.Days / 7) > 0 ? string.Format("{0:0} week(s), ", span.Days / 7) : string.Empty,
                span.Days % 7 > 0 ? string.Format("{0:0} day(s), ", span.Days % 7) : string.Empty,
                span.Hours > 0 ? string.Format("{0:0} hour(s), ", span.Hours) : string.Empty,
                span.Minutes > 0 ? string.Format("{0:0} minute(s), ", span.Minutes) : string.Empty,
                span.Seconds > 0 ? string.Format("{0:0} second(s), ", span.Seconds) : string.Empty);

            if (formatted.EndsWith(", ")) formatted = formatted.Substring(0, formatted.Length - 2);

            return formatted;
        }
    }
}