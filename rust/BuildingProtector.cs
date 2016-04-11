using System;
using System.Collections.Generic;

using Rust;

namespace Oxide.Plugins
{

    [Info("Building Protector", "Onyx", "1.2.0", ResourceId=1200)]
    class BuildingProtector : RustPlugin
    {

        #region Configuration Data

        bool configChanged;

        // Plugin settings

		string chatPrefix = "Guardian";
        string chatPrefixColor = "#008000ff";
		
        string defaultDaysProtected = "Monday,Tuesday,Wednesday,Thursday,Friday,Saturday,Sunday";
        int defaultStartHours = 18;
		int defaultStartMinutes = 0;
		int defaultStartSeconds = 0;
		int defaultEndHours = 0;
		int defaultEndMinutes = 0;
		int defaultEndSeconds = 0;
		
        string daysProtected;
		int startHours;
		int startMinutes;
		int startSeconds;
		int endHours;
		int endMinutes;
		int endSeconds;
		
        // Plugin options
        bool defaultProtectAllBuildingBlocks = true;
        bool defaultInformPlayer = true;
        float defaultInformInterval = 10;

        bool protectAllBuildingBlocks;
        bool informPlayer;
        float informInterval;

        // Messages
        string defaultInformMessage = "You can't raid between {0} and {1} !";

        string informMessage;

        #endregion
		
		
        class OnlinePlayer
        {
            public BasePlayer Player;
            public float LastInformTime;

            public OnlinePlayer(BasePlayer player)
            {
            }
        }

        [OnlinePlayers] Hash<BasePlayer, OnlinePlayer> onlinePlayers = new Hash<BasePlayer, OnlinePlayer>();
        DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0);
        Protector protector = null;

        protected override void LoadDefaultConfig()
        {
            Log("Created a new default configuration file.");
            Config.Clear();
            LoadVariables();
        }

        void Loaded()
        {
            LoadVariables();
						
            // Save config changes when required
            if (configChanged)
            {
                Log("The configuration file was updated.");
                SaveConfig();
            }

            dailyLoop((float) (86400 - DateTime.Now.TimeOfDay.TotalSeconds));
        }

        void dailyLoop(float timeToWait)
        {
            if(daysProtected.Split(',').Contains(DateTime.Now.DayOfWeek.ToString())) protector = new Protector(this);
            else {
                protector = null;
                PrintToChat($"<color={chatPrefixColor}>{chatPrefix}</color>: Raids are enabled the whole day !");
            }

            timer.Once(timeToWait, () => dailyLoop(86400));
        }

        class Protector : RustPlugin {

            readonly BuildingProtector parent;
            public DateTime startRaid;
		    public DateTime endRaid;
		    public int toStart;
		    public int toEnd;

            public Protector (BuildingProtector parent)
            {
                this.parent = parent;
                startRaid  = new DateTime(1, 1, 1, parent.startHours, parent.startMinutes, parent.startSeconds);
			    endRaid = new DateTime(1, 1, 1, parent.endHours, parent.endMinutes, parent.endSeconds);
                TimeSpan t;
			
			    if(startRaid < endRaid)
			    {
				    t = endRaid - startRaid;
				    toStart = (int)(86400 - t.TotalSeconds);
				    toEnd = (int)(t.TotalSeconds);
			    }
			    else if(startRaid > endRaid)
			    {
				    t = startRaid - endRaid;
				    toStart = (int)(t.TotalSeconds);
				    toEnd = (int)(86400 - t.TotalSeconds);
			    }
			    else return;
			
			    // #######  First interval #######
			
			    TimeSpan initTime = DateTime.Now.TimeOfDay;
			    DateTime current = new DateTime(1, 1, 1, initTime.Hours, initTime.Minutes, initTime.Seconds);
			    TimeSpan currentToStart = startRaid - current;
			    TimeSpan currentToEnd = endRaid - current;
						
			    int timeToWait = 0;
			
			    if(startRaid < endRaid)
			    {
				    if(currentToStart.TotalSeconds > 0)
				    {
					    timeToWait = (int)currentToStart.TotalSeconds;
					    parent.protectAllBuildingBlocks = true;
				    }
				    else if(currentToStart.TotalSeconds < 0 && currentToEnd.TotalSeconds < 0)
				    {
					    t = current - startRaid;
					    timeToWait = (int)(86400 - t.TotalSeconds);
					    parent.protectAllBuildingBlocks = true;
				    }
				    else if(currentToEnd.TotalSeconds > 0)
				    {
					    timeToWait = (int)currentToEnd.TotalSeconds;
					    parent.protectAllBuildingBlocks = false;
				    }
			    }
			    else if(startRaid > endRaid)
			    {
				    if(currentToEnd.TotalSeconds > 0)
				    {
					    timeToWait = (int)currentToEnd.TotalSeconds;
					    parent.protectAllBuildingBlocks = false;
				    }
				    else if(currentToEnd.TotalSeconds < 0 && currentToStart.TotalSeconds< 0)
				    {
					    t = current - endRaid;
					    timeToWait = (int)(86400 - t.TotalSeconds);
					    parent.protectAllBuildingBlocks = false;
				    }
				    else if(currentToStart.TotalSeconds > 0)
				    {
					    timeToWait = (int)currentToStart.TotalSeconds;
					    parent.protectAllBuildingBlocks = true;
				    }
			    }
			    else return;
				
				
			    if(parent.protectAllBuildingBlocks)
			    {
				    PrintToChat($"<color={parent.chatPrefixColor}>{parent.chatPrefix}</color>: Raids are disabled. They'll be enabled again in " + (timeToWait / 3600) + "h " + (timeToWait % 3600) / 60 + "m " + (timeToWait % 3600) % 60 + "s");
			    }
			    else{
				    PrintToChat($"<color={parent.chatPrefixColor}>{parent.chatPrefix}</color>: Raids are enabled. They'll be disabled again in " + (timeToWait / 3600) + "h " + (timeToWait % 3600) / 60 + "m " + (timeToWait % 3600) % 60 + "s");
			    }
			
			    timer.Once((float)timeToWait, () => loop());
             }

            void loop()
		    {
			    int interval;
			    if(parent.protectAllBuildingBlocks)
			    {
				    parent.protectAllBuildingBlocks = false;
				    interval = toEnd;
				    PrintToChat($"<color={parent.chatPrefixColor}>{parent.chatPrefix}</color>: Warning ! Raids are now enabled !");
				    PrintToChat($"<color={parent.chatPrefixColor}>{parent.chatPrefix}</color>: They'll be disabled in " + (interval / 3600) + "h " + (interval % 3600) / 60 + "m " + (interval % 3600) % 60 + "s");
			    }
			    else
			    {
				    parent.protectAllBuildingBlocks = true;
				    interval = toStart;
				    PrintToChat("Warning ! Raids are now disabled !");
				    PrintToChat("They'll be enabled in " + (interval / 3600) + "h " + (interval % 3600) / 60 + "m " + (interval % 3600) % 60 + "s");
			    }
			    timer.Once((float)interval, () => parent.protector.loop());
		    }

        }
   
        void LoadVariables()
        {
            // Settings
            startHours = Convert.ToInt32(GetConfigValue("Start of raids", "Hours", defaultStartHours));
			startMinutes = Convert.ToInt32(GetConfigValue("Start of raids", "Minutes", defaultStartMinutes));
			startSeconds = Convert.ToInt32(GetConfigValue("Start of raids", "Seconds", defaultStartSeconds));
			endHours = Convert.ToInt32(GetConfigValue("End of raids", "Hours", defaultEndHours));
			endMinutes = Convert.ToInt32(GetConfigValue("End of raids", "Minutes", defaultEndMinutes));
			endSeconds = Convert.ToInt32(GetConfigValue("End of raids", "Seconds", defaultEndSeconds));

            // Options
            daysProtected = Convert.ToString(GetConfigValue("Options", "Days of the week when Protector is enabled", defaultDaysProtected));
            informPlayer = bool.Parse(Convert.ToString(GetConfigValue("Options", "Inform Player", defaultInformPlayer)));
            informInterval = float.Parse(Convert.ToString(GetConfigValue("Options", "Inform Interval", defaultInformInterval)));

            // Messages
            informMessage = Convert.ToString(GetConfigValue("Messages", "InformMessage", defaultInformMessage));
        }

        void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            var block = entity as BuildingBlock;
            if (!block) return;
            if (protectAllBuildingBlocks)
                info.damageTypes = new DamageTypeList();

            if (info.damageTypes.Total() != 0f) return;

            var player = info.Initiator as BasePlayer;
            if (player && informPlayer && onlinePlayers[player].LastInformTime + informInterval < GetTimestamp())
            {
                onlinePlayers[player].LastInformTime = GetTimestamp();
				String startHour = protector.startRaid.Hour + "h " + protector.startRaid.Minute + "m " + protector.startRaid.Second + "s";
				String endHour = protector.endRaid.Hour + "h " + protector.endRaid.Minute + "m " + protector.endRaid.Second + "s";
                SendChatMessage(player, informMessage, startHour, endHour);
            }
        }

        void OnPlayerInit(BasePlayer player) 
            => onlinePlayers[player].LastInformTime = 0f;

        #region Helper Methods

        void Log(string message) 
            => Puts("{0} : {1}", Title, message);

        void SendChatMessage(BasePlayer player, string message, params object[] arguments) 
            => PrintToChat(player, $"<color={chatPrefixColor}>{chatPrefix}</color>: {message}", arguments);
        
        object GetConfigValue(string category, string setting, object defaultValue)
        {
            var data = Config[category] as Dictionary<string, object>;
            object value;

            if (data == null)
            {
                data = new Dictionary<string, object>();
                Config[category] = data;
                configChanged = true;
            }

            if (!data.TryGetValue(setting, out value))
            {
                value = defaultValue;
                data[setting] = value;
                configChanged = true;
            }

            return value;
        }
		
        private long GetTimestamp()
            => Convert.ToInt64((System.DateTime.UtcNow.Subtract(epoch)).TotalSeconds);

		[ChatCommand("raid")]
        private void SaveCommand(BasePlayer player, string command, string[] args)
        {
            if (protector != null)
            {
                String startHour = protector.startRaid.Hour + "h " + protector.startRaid.Minute + "m " + protector.startRaid.Second + "s";
			    String endHour = protector.endRaid.Hour + "h " + protector.endRaid.Minute + "m " + protector.endRaid.Second + "s";
                SendChatMessage(player, "Raids are activated between " + startHour + " and " + endHour);
            }
			else 
            {
                SendChatMessage(player,  "Raids are activated all the day.");
            }
        }
			
        #endregion

    }

}
