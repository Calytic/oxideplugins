/******************************************************************************
* Version 1.3 Changelog
*** Added /weather auto command to turn weather back to automatic.
******************************************************************************/

using UnityEngine;
using System;

namespace Oxide.Plugins
{
    [Info("Weather Controller", "Waizujin", 1.4)]
    [Description("Allows you to control the weather.")]
    public class WeatherController : RustPlugin
    {
        private void Loaded()
        {
            permission.RegisterPermission("weathercontroller.canUseWeather", this);
        }

        [ChatCommand("weather")]
        private void WeatherCommand(BasePlayer player, string command, string[] args)
        {
            if (!hasPermission(player, "weathercontroller.canUseWeather"))
            {
                SendReply(player, "You do not have access to this command.");

                return;
            }

            if (args.Length == 0)
            {
                SendReply(player, "Syntax Error: Please use /weather [weather_type] [on/off] [Optional: weather duration (in seconds)]");
                SendReply(player, "Or use /weather off to turn all weather off.");
                SendReply(player, "(Weather Types: clouds, fog, wind, rain, mild, average, heavy, max)");

                return;
            }

            if (args[0] == "clouds")
            {
                if (args[1] == "on")
                {
                    weather(player, "clouds", 1);
                }
                else if (args[1] == "off")
                {
                    weather(player, "clouds", 0);
                }
            }
            else if (args[0] == "fog")
            {
                if (args[1] == "on")
                {
                    weather(player, "fog", 1);
                }
                else if (args[1] == "off")
                {
                    weather(player, "fog", 0);
                }
            }
            else if (args[0] == "rain")
            {
                if (args[1] == "on")
                {
                    weather(player, "rain", 1);
                }
                else if (args[1] == "off")
                {
                    weather(player, "rain", 0);
                }
            }
            else if (args[0] == "wind")
            {
                if (args[1] == "on")
                {
                    weather(player, "wind", 1);
                }
                else if (args[1] == "off")
                {
                    weather(player, "wind", 0);
                }
            }
            else if (args[0] == "mild")
            {
                if (args[1] == "on")
                {
                    mild(player, 1);
                }
                else if (args[1] == "off")
                {
                    mild(player, 0);
                }
            }
            else if (args[0] == "average")
            {
                if (args[1] == "on")
                {
                    average(player, 1);
                }
                else if (args[1] == "off")
                {
                    average(player, 0);
                }
            }
            else if (args[0] == "heavy")
            {
                if (args[1] == "on")
                {
                    heavy(player, 1);
                }
                else if (args[1] == "off")
                {
                    heavy(player, 0);
                }
            }
            else if (args[0] == "max")
            {
                if (args[1] == "on")
                {
                    max(player, 1);
                }
                else if (args[1] == "off")
                {
                    max(player, 0);
                }
            }
			else if (args[0] == "off")
			{
				weather(player, "all", 0, true);
			}
			else if (args[0] == "auto")
			{
				weather(player, "auto", -1, true);
			}
			else
            {
                SendReply(player, "Syntax Error: Please use /weather [weather_type] [on/off] [Optional: weather duration (in seconds)]");
                SendReply(player, "Or use /weather off to turn all weather off.");
                SendReply(player, "(Weather Types: clouds, fog, wind, rain, mild, average, heavy, max)");

                return;
            }

            if (args.Length == 3)
            {
                long durationSeconds;

                if (!Int64.TryParse(args[2], out durationSeconds))
                {
                    SendReply(player, "Syntax Error: Please use /weather [weather_type] [on/off] [Optional: weather duration (in seconds)]");
                    SendReply(player, "Or use /weather off to turn all weather off.");
                    SendReply(player, "(Weather Types: clouds, fog, wind, rain, mild, average, heavy, max)");
                }

                timer.Once(durationSeconds, () => weather(player, args[0], 0));
            }
        }

        public void weather(BasePlayer player, string type, int status, bool quiet = false)
        {
            if (status == 0)
            {
                if (type == "clouds") { ConsoleSystem.Run.Server.Normal("weather.clouds " + status); if (!quiet) { SendReply(player, "Clouds have been disabled!"); } }
                if (type == "rain") { ConsoleSystem.Run.Server.Normal("weather.rain " + status); if (!quiet) { SendReply(player, "Rain has been disabled!"); } }
                if (type == "wind") { ConsoleSystem.Run.Server.Normal("weather.wind " + status); if (!quiet) { SendReply(player, "Wind has been disabled!"); } }
                if (type == "fog") { ConsoleSystem.Run.Server.Normal("weather.fog " + status); if (!quiet) { SendReply(player, "Fog has been disabled!"); } }
                if (type == "mild") { mild(player, status); }
                if (type == "average") { average(player, status); }
                if (type == "heavy") { heavy(player, status); }
                if (type == "max") { max(player, status); }
				if (type == "all")
				{
					weather(player, "clouds", status, quiet);
					weather(player, "rain", status, quiet);
					weather(player, "wind", status, quiet);
					weather(player, "fog", status, quiet);

					SendReply(player, "The weather returns to normal.");
				}
			}
            else if (status == -1 && type == "auto")
			{
				weather(player, "clouds", status, quiet);
				weather(player, "rain", status, quiet);
				weather(player, "wind", status, quiet);
				weather(player, "fog", status, quiet);

				SendReply(player, "The weather has been set to automatic.");
			}
			else
			{
                if (type == "clouds") { ConsoleSystem.Run.Server.Normal("weather.clouds " + status); if (!quiet) { SendReply(player, "Clouds have been enabled!"); } }
                if (type == "rain") { ConsoleSystem.Run.Server.Normal("weather.rain " + status); if (!quiet) { SendReply(player, "Rain has been enabled!"); } }
                if (type == "wind") { ConsoleSystem.Run.Server.Normal("weather.wind " + status); if (!quiet) { SendReply(player, "Wind has been enabled!"); } }
                if (type == "fog") { ConsoleSystem.Run.Server.Normal("weather.fog " + status); if (!quiet) { SendReply(player, "Fog has been enabled!"); } }
                if (type == "mild") { mild(player, status); }
                if (type == "average") { average(player, status); }
                if (type == "heavy") { heavy(player, status); }
                if (type == "max") { max(player, status); }
            }
        }

        public void mild(BasePlayer player, int status)
        {
            if (status == 1)
            {
                weather(player, "rain", 1, true);
                SendReply(player, "A mild storm has begun.");
            }
            else if (status == 0)
            {
                weather(player, "rain", 0, true);
                SendReply(player, "A mild storm has ended.");
            }
        }

        public void average(BasePlayer player, int status)
        {
            if (status == 1)
            {
                weather(player, "rain", 1, true);
                weather(player, "wind", 1, true);
                SendReply(player, "A storm has begun.");
            }
            else if (status == 0)
            {
                weather(player, "rain", 0, true);
                weather(player, "wind", 0, true);
                SendReply(player, "The storm has ended.");
            }
        }

        public void heavy(BasePlayer player, int status)
        {
            if (status == 1)
            {
                weather(player, "clouds", 1, true);
                weather(player, "rain", 1, true);
                weather(player, "wind", 1, true);
                SendReply(player, "A heavy storm has begun.");
            }
            else if (status == 0)
            {
                weather(player, "clouds", 0, true);
                weather(player, "rain", 0, true);
                weather(player, "wind", 0, true);
                SendReply(player, "The heavy storm has ended.");
            }
        }

        public void max(BasePlayer player, int status)
        {
            if (status == 1)
            {
                weather(player, "clouds", 1, true);
                weather(player, "rain", 1, true);
                weather(player, "wind", 1, true);
                weather(player, "fog", 1, true);
                SendReply(player, "The weather becomes vicious and visibility decreases.");
            }
            else if (status == 0)
            {
                weather(player, "clouds", 0, true);
                weather(player, "rain", 0, true);
                weather(player, "wind", 0, true);
                weather(player, "fog", 0, true);
                SendReply(player, "The vicious storm comes to an end.");
            }
        }

        bool hasPermission(BasePlayer player, string perm)
        {
            if (player.net.connection.authLevel > 1)
            {
                return true;
            }

            return permission.UserHasPermission(player.userID.ToString(), perm);
        }
    }
}
