using System;
using System.Collections.Generic;
using RustNative;

namespace Oxide.Plugins {

	[Info("SystemInformation", "cone", "0.1.3", ResourceId = 2036)]
	[Description("Rcon commands to display current server memory usage and uptime.")]
	class SystemInformation : RustPlugin {

		#region member fields

		//DateTime the server "starts" (see OnServerInitialized)
		private DateTime startTime;

		//True if we should print a fake "unknown command" message when a player does not have permission to use a command, otherwise false.
		private bool fakeUnknownCommand;

		#endregion

		#region utility delegates

		//Displays a double in the form of ?.?? with leading zeros
		private string toDisplay(double d) => d.ToString("F2");

		//Checks if a console command was sent by an authorized party.
		private bool isAuthorized(ConsoleSystem.Arg console, string permission) => (console.isAdmin || (console.connection != null && console.connection.authLevel >= 2) || this.permission.UserHasPermission(rust.UserIDFromConnection(console.connection), permission));
		private bool isAuthorized(BasePlayer player, string permission) => (player.net.connection.authLevel >= 1 || this.permission.UserHasPermission(player.UserIDString, permission));

		//Localization utilities
		private string GetMessage(string key, string steamid = null) => lang.GetMessage(key, this, steamid);
		private string GetSystemMessage(string key) => lang.GetMessage(key, this);
		private string GetConsoleDeniedKey() => (fakeUnknownCommand ? "FakeUnknownConsoleCommand" : "NoConsolePermission");
		private string GetChatDeniedKey() => (fakeUnknownCommand ? "FakeUnknownChatCommand" : "NoChatPermission");

		#endregion

		#region plugin hooks

		//Called when all plugins are loaded and the server is accepting connections.
		void OnServerInitialized() {

			//Save current time when the server is accepting connections.
			startTime = DateTime.Now;

			//Register permissions
			permission.RegisterPermission("systeminformation.uptime", this);
			permission.RegisterPermission("systeminformation.mem", this);

			//Register default english locale
			lang.RegisterMessages(new Dictionary<string, string>() {
				{"FakeUnknownConsoleCommand", "Invalid command: {0}"},
				{"FakeUnknownChatCommand", "Unknown command: {0}"},
				{"NoConsolePermission", "You do not have permission to use this command."},
				{"NoChatPermission", "You do not have permission to use this command."},
				{ "Uptime", "Current Uptime: {0} hours, {1} minutes, {2} seconds." },
				{ "MemoryUsage", "Current memory usage: {0} bytes/{1} GB/{2} GiB (what the TCAdmin panel shows)." }
			}, this);

			//Try and load config values.
			try {

				//Parse configuration
				fakeUnknownCommand = Convert.ToBoolean(Config["fakeUnknownCommand"]);

			} catch {

				//Alert admin to config malformation
				Puts("Configuration file is malformed. Re-creating.");

				//Re-create the config
				LoadDefaultConfig();

			}

		}

		//called when the configuration file SystemInformation.json doesn't exist
		protected override void LoadDefaultConfig() {

			//Alert admin to config creation
			Puts("Re-creating configuration.");

			//Used as a security measure to fake "unknown command" when a user does not have permission to use a command.
			Config["fakeUnknownCommand"] = fakeUnknownCommand = true;

			//save the configuration
			SaveConfig();

		}

		#endregion

		#region utility methods

		//Takes a the arguments to a console command and returns a SteamID string if the command came from an active player, or null if it came from RCON.
		private string GetConsoleSteamID(ConsoleSystem.Arg console) {

			//Steam ID as a string, or null if the console arguments are from RCON.
			string sSteamID = null;

			//Check if the console has a connetion, if not, it is an RCON connection.
			if (console.connection != null) {

				//If so, convert the steam ID to a string.
				sSteamID = console.connection.userid.ToString();

			}

			//Return steamID as a string, or null
			return sSteamID;

		}

		#endregion

		#region output methods

		//Display the current server uptime
		string GetUptimeDisplay(string userID) {

			//Get elapsed time since OnServerInitialized()
			TimeSpan uptime = DateTime.Now - startTime;

			//Output uptime
			return string.Format(lang.GetMessage("Uptime", this, userID), uptime.Hours, uptime.Minutes, uptime.Seconds);

		}

		//Display current server memory usage.
		string GetMemoryUsageDisplay(string userID) {

			//Get current working set in bytes
			ulong workingSet = SystemInfo.MemoryUsedWorkingSet();

			//Get current working set in gigabytes
			double workingSetGB = workingSet / 1e+9;

			//Get current working set in gibibytes
			double workingSetGiB = workingSet / 1.074e+9;

			//Output memory usage
			return string.Format(lang.GetMessage("MemoryUsage", this, userID), workingSet, toDisplay(workingSetGB), toDisplay(workingSetGiB));

		}

		#endregion

		#region console commands

		[ConsoleCommand("sysinfo.uptime")]
		void ConsoleDisplayUptime(ConsoleSystem.Arg args) {

			//Check if the user is authorized for uptime display.
			if (isAuthorized(args, "systeminformation.uptime")) {

				//Output uptime
				SendReply(args, GetUptimeDisplay(GetConsoleSteamID(args)));

			} else {

				//Output fake error message
				SendReply(args, string.Format(GetMessage(GetConsoleDeniedKey(), GetConsoleSteamID(args)), "mem"));

			}

		}

		[ConsoleCommand("sysinfo.mem")]
		void ConsoleDisplayMemoryUsage(ConsoleSystem.Arg args) {

			//Check if the user is authorized for memory usage display.
			if (isAuthorized(args, "systeminformation.mem")) {

				//Output memory usage
				SendReply(args, GetMemoryUsageDisplay(GetConsoleSteamID(args)));

			} else {

				//Output fake error message
				SendReply(args, string.Format(GetMessage(GetConsoleDeniedKey(), GetConsoleSteamID(args)), "uptime"));

			}

		}

		#endregion

		#region chat commands

		[ChatCommand("uptime")]
		void ChatDisplayUptime(BasePlayer player, string command, string[] args) {

			//Check if the user is authorized for uptime display.
			if (isAuthorized(player, "systeminformation.uptime")) {

				//Output uptime
				SendReply(player, GetUptimeDisplay(player.UserIDString));

			} else {

				//Output fake error message
				SendReply(player, string.Format(GetMessage(GetChatDeniedKey(), player.UserIDString), "uptime"));

			}

		}

		[ChatCommand("mem")]
		void ChatDisplayMemoryUsage(BasePlayer player, string command, string[] args) {

			//Check if the user is authorized for memory usage display.
			if (isAuthorized(player, "systeminformation.mem")) {

				//Output memory usage
				SendReply(player, GetMemoryUsageDisplay(player.UserIDString));

			} else {

				//Output fake error message
				SendReply(player, string.Format(GetMessage(GetChatDeniedKey(), player.UserIDString), "mem"));

			}

		}

		#endregion

	}

}
