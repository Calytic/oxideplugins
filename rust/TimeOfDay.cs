//
//    The zlib/libpng License
//    ===========================
//
//    Copyright (C) 2015 Sin (sin.oxide.service@gmail.com)
//
//    This software is provided 'as-is', without any express or implied
//    warranty.  In no event will the authors be held liable for any damages
//    arising from the use of this software.
//
//    Permission is granted to anyone to use this software for any purpose,
//    including commercial applications, and to alter it and redistribute it
//    freely, subject to the following restrictions:
//
//    1. The origin of this software must not be misrepresented; you must not
//        claim that you wrote the original software. If you use this software
//        in a product, an acknowledgement in the product documentation would be
//        appreciated but is not required.
//
//    2. Altered source versions must be clearly marked as such, and must not be
//        misrepresented as being the original software.
//
//    3. This notice may not be removed or altered from any source distribution.
//
#region Using Directives
using Oxide.Core.Plugins;
using System;
using System.Text;
#endregion

namespace Oxide.Plugins
{
    /// <summary>
    /// A plugin class which provides tools for managing time. It can also alter day and night duration.
    /// </summary>
    [Info("TimeOfDay", "Sin", "1.0.1", ResourceId = 1355)]
    [Description("Provides tools for managing time. It can also alter day and night duration.")]
    public class TimeOfDay : RustPlugin
    {
        #region Fields
        /// <summary>
        /// The hour at which it is considered day.
        /// </summary>
        public float SunriseHour
        {
            get
            {
                return TOD_Sky.Instance.SunriseTime;
            }
        }

        /// <summary>
        /// The hour at which it is considered night.
        /// </summary>
        public float SunsetHour
        {
            get
            {
                return TOD_Sky.Instance.SunsetTime;
            }
        }

        /// <summary>
        /// The length of the day in  minutes.
        /// </summary>
        public uint Daylength
        {
            get;
            private set;
        }

        /// <summary>
        /// The length of the night in  minutes.
        /// </summary>
        public uint Nightlength
        {
            get;
            private set;
        }

        /// <summary>
        /// Returns true if it is currently day time.
        /// </summary>
        public bool IsDay
        {
            get
            {
                return ((TOD_Sky.Instance.Cycle.Hour > this.SunriseHour) && (TOD_Sky.Instance.Cycle.Hour < this.SunsetHour)) == true ? true : false;
            }
        }

        /// <summary>
        /// Returns true if it is currently night time.
        /// </summary>
        public bool IsNight
        {
            get
            {
                return ((TOD_Sky.Instance.Cycle.Hour > this.SunriseHour) && (TOD_Sky.Instance.Cycle.Hour < this.SunsetHour)) == true ? false : true;
            }
        }

        /// <summary>
        /// Represents the number of restarted timers which try to find the TOD_Time component.
        /// </summary>
        private uint componentSearchAttempts = 0;

        /// <summary>
        /// When found it holds a reference to the TOD_Time component.
        /// </summary>
        private TOD_Time timeComponent = null;
        #endregion

        #region Methods
        /// <summary>
        /// Called when the plugin gets loaded. We are 
        /// starting the timer for the time component fetch function.
        /// </summary>
        private void Loaded()
        {
            timer.Once(3, GetTimeComponent);
        }

        /// <summary>
        /// Called when the plugin gets loaded for the first time.
        /// We are creating the default configuration file here.
        /// </summary>
        private void LoadDefaultConfig()
        {
            this.Daylength = 30;
            this.Nightlength = 30;

            Config.Clear();

            Config["ConfigVersion"] = "1.0.0";
            
            Config["Settings", "Daylength"] = this.Daylength;
            Config["Settings", "Nightlength"] = this.Nightlength;

            SaveConfig();
        }

        /// <summary>
        /// Fetches the TOD_Time component of the GameObject. It is initialized with the conig values afterwards and loads the config values.
        /// </summary>
        private void GetTimeComponent()
        {
            if (TOD_Sky.Instance == null)
            {
                ++this.componentSearchAttempts;

                if (this.componentSearchAttempts < 10)
                {
                    Puts("Restarting timer for GetTimeComponent(). Attempt " + this.componentSearchAttempts + "/10.");
                    timer.Once(3, GetTimeComponent);
                }
                else
                {
                    RaiseError("Could not find required component after 10 attempts. Plugin will not work without it.");
                }

                return;
            }

            if (TOD_Sky.Instance != null && this.componentSearchAttempts > 0)
            {
                Puts("Found TOD_Time component after attempt " + this.componentSearchAttempts + ".");
            }

            this.timeComponent = TOD_Sky.Instance.Components.Time;

            if (this.timeComponent == null)
            {
                RaiseError("Could not fetch time component. Plugin will not work without it.");

                return;
            }

            this.Daylength = 30;
            this.Nightlength = 30;

            this.Daylength = Convert.ToUInt32(Config["Settings", "Daylength"]);
            this.Nightlength = Convert.ToUInt32(Config["Settings", "Nightlength"]);

            this.timeComponent.ProgressTime = true;
            this.timeComponent.UseTimeCurve = false;

            this.timeComponent.OnSunrise += UpdateTimeOnSunrise;
            this.timeComponent.OnSunset += UpdateTimeOnSunset;

            if (TOD_Sky.Instance.IsDay == true)
            {
                UpdateTimeOnSunrise();
            }
            else
            {
                UpdateTimeOnSunset();
            }
        }

        /// <summary>
        /// Updates the daylength on sunrise.
        /// </summary>
        private void UpdateTimeOnSunrise()
        {
            float num1 = TOD_Sky.Instance.SunsetTime - TOD_Sky.Instance.SunriseTime;
            float num2 = 24.0f / num1;
            float num3 = this.Daylength * num2;

            this.timeComponent.DayLengthInMinutes = num3;
        }

        /// <summary>
        /// Updates the daylength on sunset.
        /// </summary>
        private void UpdateTimeOnSunset()
        {
            float num1 = TOD_Sky.Instance.SunsetTime - TOD_Sky.Instance.SunriseTime;
            float num2 = 24.0f - num1;
            float num3 = 24.0f / num2;
            float num4 = this.Nightlength * num3;

            this.timeComponent.DayLengthInMinutes = num4;
        }
        #endregion

        #region Commands
        /// <summary>
        /// This function is being called when the chat command /tod has been typed in.
        /// It then goes trough a switch statement and acts accordingly.
        /// </summary>
        /// <param name="Player">The player who issued the command.</param>
        /// <param name="Command">The command which should be executed.</param>
        /// <param name="Args">The arguments supplied with ne command.</param>
        [ChatCommand("tod")]
        private void TodCommand(BasePlayer Player, string Command, string[] Args)
        {
            if (Args.Length < 1)
            {
                PrintPluginMessageToChat(Player, FormNeutralMessage("Current Time: ") + TOD_Sky.Instance.Cycle.DateTime.ToString("HH:mm:ss") + ".");

                return;
            }

            StringBuilder stringBuilder = new StringBuilder();

            switch (Args[0])
            {
                // Prints all available commands to the player's chat.
                case "help":
                    stringBuilder.Append(FormNeutralMessage("-------------------- Available Commands --------------------\n"));

                    if (CheckForPermission(Player) == true)
                    {
                        stringBuilder.Append(FormNeutralMessage("/tod") + " - Shows current Time Of Day.\n");
                        //stringBuilder.Append(FormNeutralMessage("/tod sunrise <hour>") + " - Sets the sunrise hour.\n");
                        //stringBuilder.Append(FormNeutralMessage("/tod sunset <hour>") + " - Sets the sunset hour.\n");
                        stringBuilder.Append(FormNeutralMessage("/tod set <hour>") + " - Sets the time to the given hour.\n");
                        stringBuilder.Append(FormNeutralMessage("/tod daylength <length>") + " - Sets the daylength in minutes.\n");
                        stringBuilder.Append(FormNeutralMessage("/tod nightlength <length>") + " - Sets the nightlength in minutes.\n");
                        stringBuilder.Append(FormNeutralMessage("/tod info") + " - Shows all available commands.");
                    }
                    else
                    {
                        stringBuilder.Append(FormNeutralMessage("/tod") + " - Shows current Time Of Day.\n");
                        stringBuilder.Append(FormNeutralMessage("/tod info") + " - Shows all available commands.");
                    }

                    PrintPluginMessageToChat(Player, stringBuilder.ToString());
                    break;

                // Sets the current time of the day to the given value.
                case "set":
                    if (CheckForPermission(Player) == false)
                        return;

                    if (Args.Length < 2)
                    {
                        PrintPluginMessageToChat(Player, FormErrorMessage("Could not change the time. You did not supply a number."));

                        return;
                    }

                    uint newTime = 6;

                    if (uint.TryParse(Args[1], out newTime) == true)
                    {
                        if (newTime < 0 || newTime > 24)
                        {
                            PrintPluginMessageToChat(Player, FormErrorMessage("Could not change the time to " + Args[1] + ". Number must be between 0 and 24."));

                            return;
                        }

                        TOD_Sky.Instance.Cycle.Hour = newTime;
                        Puts("The time has been set to " + TOD_Sky.Instance.Cycle.DateTime.ToString("HH:mm:ss") + ". (By Player: " + Player.displayName + ")");
                        PrintPluginMessageToChat(Player, "The time has been set to " + newTime + ".");

                        if (newTime > TOD_Sky.Instance.SunriseTime && newTime < TOD_Sky.Instance.SunsetTime)
                        {
                            UpdateTimeOnSunrise();
                        }
                        else
                        {
                            UpdateTimeOnSunset();
                        }
                    }
                    else
                    {
                        PrintPluginMessageToChat(Player, FormErrorMessage("Could not change the time to " + Args[1] + "."));
                    }
                    break;

                // Sets the sunrise hour to the given value and alters the TOD_Sky instance.
                //case "sunrise":
                //    if (CheckForPermission(Player) == false)
                //        return;

                //    if (Args.Length < 2)
                //    {
                //        PrintPluginMessageToChat(Player, FormErrorMessage("Could not change sunrise hour. You did not supply a number."));

                //        return;
                //    }

                //    uint newSunriseHour = 6;

                //    if (uint.TryParse(Args[1], out newSunriseHour) == true)
                //    {
                //        if (newSunriseHour >= this.SunsetHour)
                //        {
                //            PrintPluginMessageToChat(Player, FormErrorMessage("Could not change sunrise hour. Number must be less than the sunset hour."));

                //            return;
                //        }

                //        this.SunriseHour = newSunriseHour;
                //        Puts("The sunrise hour has been set to " + this.SunriseHour + ". (By Player: " + Player.displayName + ")");
                //        PrintPluginMessageToChat(Player, "The sunrise time has been set to " + this.SunriseHour + ".");

                //        Config["Settings", "SunriseHour"] = this.SunriseHour;
                //        SaveConfig();
                //    }
                //    else
                //    {
                //        PrintPluginMessageToChat(Player, FormErrorMessage("Could not set the sunrise hour to " + Args[1] + "."));
                //    }
                //    break;

                // Sets the sunset hour to the given value and alters the TOD_Sky instance.
                //case "sunset":
                //    if (CheckForPermission(Player) == false)
                //        return;

                //    if (Args.Length < 2)
                //    {
                //        PrintPluginMessageToChat(Player, FormErrorMessage("Could not change sunset hour. You did not supply a number."));

                //        return;
                //    }

                //    uint newSunsetHour = 6;

                //    if (uint.TryParse(Args[1], out newSunsetHour) == true)
                //    {
                //        if (newSunsetHour <= this.SunriseHour)
                //        {
                //            PrintPluginMessageToChat(Player, FormErrorMessage("Could not change sunset hour. Number must be greater than the sunrise hour."));

                //            return;
                //        }

                //        if (newSunsetHour > 24)
                //        {
                //            PrintPluginMessageToChat(Player, FormErrorMessage("Could not change sunset hour. Number must be less than or equal to 24."));

                //            return;
                //        }

                //        this.SunsetHour = newSunsetHour;
                //        Puts("The sunset hour has been set to " + this.SunsetHour + ". (By Player: " + Player.displayName + ")");
                //        PrintPluginMessageToChat(Player, "The sunset time has been set to " + this.SunsetHour + ".");

                //        Config["Settings", "SunsetHour"] = this.SunsetHour;
                //        SaveConfig();
                //    }
                //    else
                //    {
                //        PrintPluginMessageToChat(Player, FormErrorMessage("Could not set the sunset hour to " + Args[1] + "."));
                //    }
                //    break;

                // Sets the daylength to the given value and alters the TOD_Sky instance.
                case "daylength":
                    if (CheckForPermission(Player) == false)
                        return;

                    if (Args.Length < 2)
                    {
                        PrintPluginMessageToChat(Player, FormErrorMessage("Incorrect syntax!") + "\n/tod daylength <length>");

                        return;
                    }

                    uint newDaylength = 30;

                    if (uint.TryParse(Args[1], out newDaylength) == true)
                    {
                        if (newDaylength <= 0)
                        {
                            PrintPluginMessageToChat(Player, FormErrorMessage("Could not change daylength. Length must be greater than 0."));

                            return;
                        }

                        this.Daylength = newDaylength;
                        Puts("The daylength has been set to " + this.Daylength + ". (By Player: " + Player.displayName + ")");
                        PrintPluginMessageToChat(Player, "The daylength has been set to " + this.Daylength + ".");

                        if (TOD_Sky.Instance.IsDay == true)
                        {
                            UpdateTimeOnSunrise();
                        }
                        else
                        {
                            UpdateTimeOnSunset();
                        }

                        Config["Settings", "Daylength"] = this.Daylength;
                        SaveConfig();
                    }
                    else
                    {
                        PrintPluginMessageToChat(Player, FormErrorMessage("Could not set the daylength to " + Args[1] + "."));
                    }
                    break;

                // Sets the nightlength to the given value and alters the TOD_Sky instance.
                case "nightlength":
                    if (CheckForPermission(Player) == false)
                        return;

                    if (Args.Length < 2)
                    {
                        PrintPluginMessageToChat(Player, FormErrorMessage("Incorrect syntax!") + "\n/tod nightlength <length>");

                        return;
                    }

                    uint newNightlength = 30;

                    if (uint.TryParse(Args[1], out newNightlength) == true)
                    {
                        if (newNightlength <= 0)
                        {
                            PrintPluginMessageToChat(Player, FormErrorMessage("Could not change nightlength. Length must be greater than 0."));

                            return;
                        }

                        this.Nightlength = newNightlength;
                        Puts("The nightlength has been set to " + this.Nightlength + ". (By Player: " + Player.displayName + ")");
                        PrintPluginMessageToChat(Player, "The nightlength has been set to " + this.Nightlength + ".");

                        if (TOD_Sky.Instance.IsDay == true)
                        {
                            UpdateTimeOnSunrise();
                        }
                        else
                        {
                            UpdateTimeOnSunset();
                        }

                        Config["Settings", "Nightlength"] = this.Nightlength;
                        SaveConfig();
                    }
                    else
                    {
                        PrintPluginMessageToChat(Player, FormErrorMessage("Could not set the nightlength to " + Args[1] + "."));
                    }
                    break;

                // Display the current settings of the plugin to the player's chat.
                case "info":
                    stringBuilder.Append(FormNeutralMessage("-------- Settings --------\n"));
                    stringBuilder.Append(FormNeutralMessage("Current Time") + ":\t" + TOD_Sky.Instance.Cycle.DateTime.ToString("HH:mm:ss") + "\n");
                    stringBuilder.Append(FormNeutralMessage("Sunrise Hour") + ":\t" + TOD_Sky.Instance.SunriseTime.ToString("0.0") + "\n");
                    stringBuilder.Append(FormNeutralMessage("Sunset Hour") + ":\t" + TOD_Sky.Instance.SunsetTime.ToString("0.0") + "\n");
                    stringBuilder.Append(FormNeutralMessage("Daylength") + ":\t\t" + this.Daylength.ToString() + " minutes\n");
                    stringBuilder.Append(FormNeutralMessage("Nightlength") + ":\t\t" + this.Nightlength.ToString() + " minutes");

                    PrintPluginMessageToChat(Player, stringBuilder.ToString());
                    break;

                // No command has been found. Print an error message.
                default:
                    stringBuilder.Append(FormErrorMessage("This command does not exist.") + " (Command: " + Args[0] + ")\n");
                    stringBuilder.Append("Type /tod help for a list of availbale commands.");

                    PrintPluginMessageToChat(Player, stringBuilder.ToString());
                    break;
            }
        }

        /// <summary>
        /// Hook for the HelpText plugin. (ResourceID: 676)
        /// It will display help information when typing /help
        /// </summary>
        /// <param name="Player">The player who called for help.</param>
        [HookMethod("SendHelpText")]
        private void SendHelpText(BasePlayer Player)
        {
            StringBuilder stringBuilder = new StringBuilder();

            stringBuilder.Append(FormNeutralMessage("-------------------- Available Commands --------------------\n"));

            if (CheckForPermission(Player) == true)
            {
                stringBuilder.Append(FormNeutralMessage("/tod") + " - Shows current Time Of Day.\n");
                //stringBuilder.Append(FormNeutralMessage("/tod sunrise <hour>") + " - Sets the sunrise hour.\n");
                //stringBuilder.Append(FormNeutralMessage("/tod sunset <hour>") + " - Sets the sunset hour.\n");
                stringBuilder.Append(FormNeutralMessage("/tod set <hour>") + " - Sets the time to the given hour.\n");
                stringBuilder.Append(FormNeutralMessage("/tod daylength <length>") + " - Sets the daylength in minutes.\n");
                stringBuilder.Append(FormNeutralMessage("/tod nightlength <length>") + " - Sets the nightlength in minutes.\n");
                stringBuilder.Append(FormNeutralMessage("/tod info") + " - Shows all available commands.");
            }
            else
            {
                stringBuilder.Append(FormNeutralMessage("/tod") + " - Shows current Time Of Day.\n");
                stringBuilder.Append(FormNeutralMessage("/tod info") + " - Shows all available commands.");
            }

            PrintPluginMessageToChat(Player, stringBuilder.ToString());
        }

        /// <summary>
        /// Helper function for printing a message to specified player's chat with additional plugin information.
        /// </summary>
        /// <param name="Player">The target player.</param>
        /// <param name="Message">The message to send.</param>
        private void PrintPluginMessageToChat(BasePlayer Player, string Message)
        {
            PrintToChat(Player, "<b><size=16>[<color=#ffa500ff>" + this.Name + "</color>] [<color=#ffa500ff>" + this.Version.ToString() + "</color>]</size></b>\n" + Message);
        }

        /// <summary>
        /// Helper function for printing a message to the chat with additional plugin information.
        /// </summary>
        /// <param name="Message">The message to send.</param>
        private void PrintPluginMessageToChat(string Message)
        {
            PrintToChat("<b><size=16>[<color=#ffa500ff>" + this.Name + "</color>] [<color=#ffa500ff>" + this.Version.ToString() + "</color>]</size></b>\n" + Message);
        }

        /// <summary>
        /// Helper function for coloring neutral messages.
        /// </summary>
        /// <param name="Message">The message to alter.</param>
        /// <returns>The message with the neutral color code.</returns>
        private string FormNeutralMessage(string Message)
        {
            return "<color=#c0c0c0ff>" + Message + "</color>";
        }

        /// <summary>
        /// Helper function for coloring warning messages.
        /// </summary>
        /// <param name="Message">The message to alter.</param>
        /// <returns>The message with the warning color code.</returns>
        private string FormWarningMessage(string Message)
        {
            return "<color=#ffff00ff>" + Message + "</color>";
        }

        /// <summary>
        /// Helper function for coloring error messages.
        /// </summary>
        /// <param name="Message">The message to alter.</param>
        /// <returns>The message with the error color code.</returns>
        private string FormErrorMessage(string Message)
        {
            return "<color=#ff0000ff>" + Message + "</color>";
        }

        /// <summary>
        /// Checks if a player is considered admin on the server.
        /// </summary>
        /// <param name="Player">The player to check.</param>
        /// <returns>True if the authentication level is above one. (Admin)</returns>
        private bool CheckForPermission(BasePlayer Player)
        {
            if (Player.net.connection.authLevel > 1)
            {
                return true;
            }

            return false;
        }
        #endregion
    }
}
