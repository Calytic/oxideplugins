using System.Text.RegularExpressions;
using System.Collections.Generic;
using Oxide.Core.Libraries;
using System.Linq;
using Oxide.Core;
using System;

namespace Oxide.Plugins
{
    [Info("Devblog Announcer", "LaserHydra", "1.2.0", ResourceId = 1340)]
    [Description("Broadcasts to chat when a new Devblog or Community Update was released.")]
    class DevblogAnnouncer : RustPlugin
	{
		private readonly WebRequests webRequests = Interface.GetMod().GetLibrary<WebRequests>("WebRequests");

		class Data
		{
			public int Devblog = 0;
			public int CommunityUpdate = 0;
		}
		
		Data data;
		
		void Loaded()
		{
            LoadConfig();

            data = Interface.GetMod().DataFileSystem.ReadObject<Data>("DevblogAnnouncer");
            CheckForBlogs();
			timer.Repeat(60, 0, () => CheckForBlogs());
		}

        protected override void LoadDefaultConfig()
        {
            PrintWarning("Generating new configfile...");
        }
		
		[ConsoleCommand("getblogs")]
		void GetLatest()
		{
            CheckForBlogs();
            Puts("Latest Devblog: Devblog " + data.Devblog.ToString());
			Puts("Latest Community Update: Community Update " + data.CommunityUpdate.ToString());
		}
		
		void SaveData()
		{
			Interface.GetMod().DataFileSystem.WriteObject("DevblogAnnouncer", data);
		}
		
		void LoadConfig()
		{
			SetConfig("Settings", "Enable Devblog", true);
            SetConfig("Settings", "Enable CommunityUpdate", true);

            SaveConfig();
		}
		
		void CheckForBlogs()
		{
			webRequests.EnqueueGet("http://api.steampowered.com/ISteamNews/GetNewsForApp/v0002/?appid=252490&count=1&maxlength=300&format=json", (code, response) => DataRecieved(code, response), this);
		}
		
		void DataRecieved(int code, string response)
		{
			if (response == null || code != 200)
            {
                Puts("Failed to get data.");
			}
			
			int devblog = 0;
            int community = 0;

			Match dev_match = new Regex(@"\""title\"": \""Devblog (\d+)\""").Match(response);
			Match community_match = new Regex(@"\""title\"": \""Community Update (\d+)\""").Match(response);

			if(dev_match.Success) devblog = Convert.ToInt32(dev_match.Groups[1].ToString());
			if(community_match.Success) community = Convert.ToInt32(community_match.Groups[1].ToString());

			if(GetConfig(true, "Settings", "Enable CommunityUpdate"))
			{
				if(community > data.CommunityUpdate) 
				{
					BroadcastChat($"Community Update {community} was released!");
					Puts($"Community Update {community} was released!");
					Console.Beep();

                    data.CommunityUpdate = community;

					SaveData();
				}
			}
			
			if(GetConfig(true, "Settings", "Enable Devblog"))
			{
				if(devblog > data.Devblog) 
				{
					BroadcastChat($"Devblog {devblog} was released!");
                    Puts($"Devblog {devblog} was released!");
					Console.Beep();

					data.Devblog = devblog;

					SaveData();
				}
			}
		}

        ////////////////////////////////////////
        ///     Converting
        ////////////////////////////////////////

        string ListToString(List<string> list, int first, string seperator)
        {
            return String.Join(seperator, list.Skip(first).ToArray());
        }

        ////////////////////////////////////////
        ///     Config & Message Related
        ////////////////////////////////////////

        void SetConfig(params object[] args)
        {
            List<string> stringArgs = (from arg in args select arg.ToString()).ToList<string>();
            stringArgs.RemoveAt(args.Length - 1);

            if (Config.Get(stringArgs.ToArray()) == null)
                Config.Set(args);
        }

        T GetConfig<T>(T defaultVal, params object[] args)
        {
            List<string> stringArgs = (from arg in args select arg.ToString()).ToList<string>();

            if (Config.Get(stringArgs.ToArray()) == null)
            {
                PrintError($"The plugin failed to read something from the config: {ListToString(stringArgs, 0, "/")}{Environment.NewLine}Please reload the plugin and see if this message is still showing. If so, please post this into the support thread of this plugin.");
                return defaultVal;
            }

            return (T)Convert.ChangeType(Config.Get(stringArgs.ToArray()), typeof(T));
        }

        ////////////////////////////////////////
        ///     Chat Handling
        ////////////////////////////////////////

        void BroadcastChat(string prefix, string msg = null, object userID = null) => rust.BroadcastChat(msg == null ? prefix : "<color=#C4FF00>" + prefix + "</color>: " + msg, null, userID == null ? "0" : userID.ToString());

        void SendChatMessage(BasePlayer player, string prefix, string msg = null) => rust.SendChatMessage(player, msg == null ? prefix : "<color=#C4FF00>" + prefix + "</color>: " + msg);
    }
}
