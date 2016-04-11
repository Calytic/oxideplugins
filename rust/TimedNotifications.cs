using Oxide.Core;
using Oxide.Core.Plugins;
using System.Collections.Generic;
using System;
using UnityEngine;
using Oxide.Game.Rust.Libraries;


namespace Oxide.Plugins
{
    [Info("Timed Notifications", "Hirsty", "0.0.4", ResourceId = 1277)]
    [Description("Depending on Preset Times, this plugin will display a popup with a notification.")]
    class TimedNotifications : RustPlugin
    {
        public static string version = "0.0.4";
        public bool PopUpNotifier = true;
        public string[] days;
        public string TZ;
        public int hour;
        public DateTime setDate;
        public int min;
        char sep = '/';
        char sep2 = ':';

[PluginReference]
        Plugin PopupNotifications;
        class StoredData
        {
            public HashSet<EventData> Events = new HashSet<EventData>();

            public StoredData()
            {
            }
        }
        class EventData
        {
            public DateTime EventDate;
            public string EventInfo;
            public string CommandLine;
            public bool broadcast;
            public string Schedule;
            public EventData()
            {

            }
            public EventData(DateTime Date, string info, string EventType = null)
            {
                this.EventDate = Date;
                this.broadcast = false;
                switch (EventType.ToLower())
                {
                    case "add":
                        this.EventInfo = info;
                        this.CommandLine = "";
                        this.Schedule = "none";
                        break;
                    case "cmd":
                        this.EventInfo = "";
                        this.CommandLine = info;
                        this.Schedule = "none";
                        break;
                    case "hourly":
                        this.EventInfo = info;
                        this.CommandLine = "";
                        this.Schedule = "hourly";
                        break;
                    case "daily":
                        this.EventInfo = info;
                        this.CommandLine = "";
                        this.Schedule = "daily";
                        break;
                    case "weekly":
                        this.EventInfo = info;
                        this.CommandLine = "";
                        this.Schedule = "weekly";
                        break;
                    case "monthly":
                        this.EventInfo = info;
                        this.CommandLine = "";
                        this.Schedule = "monthly";
                        break;
                    case "yearly":
                        this.EventInfo = info;
                        this.CommandLine = "";
                        this.Schedule = "yearly";
                        break;
                    case "hourlycmd":
                        this.EventInfo = "";
                        this.CommandLine = info;
                        this.Schedule = "hourly";
                        break;
                    case "dailycmd":
                        this.EventInfo = "";
                        this.CommandLine = info;
                        this.Schedule = "daily";
                        break;
                    case "weeklycmd":
                        this.EventInfo = "";
                        this.CommandLine = info;
                        this.Schedule = "weekly"; 
                        break;
                    case "monthlycmd":
                        this.EventInfo = "";
                        this.CommandLine = info;
                        this.Schedule = "monthly";
                        break;
                    case "yearlycmd":
                        this.EventInfo = "";
                        this.CommandLine = info;
                        this.Schedule = "yearly";
                        break;
                }
            }      
        }
        StoredData storedData;
        protected override void LoadDefaultConfig() {
            PrintWarning("Whoops! No config file, lets create a new one!"); // Runs when no configuration file has been found
            Config.Clear();
            Config["Plugin","Version"] = version;
            //Config["Events", "TimeZone"] = "GMT";
            Config["Events", "AllowedDays"] = "Mon,Tue,Wed,Thu,Fri,Sat,Sun";
            Config["Plugin", "PopUpTime"] = 20;
            Config["Plugin", "TimeCheck"] = 10;
            Config["Plugin", "EnablePopUps"]=true;
            SaveConfig();
        }
        private void Loaded() => LoadData(); // What to do when plugin loaded

        bool hasPermission(BasePlayer player, string permname)
        {
            if (player.net.connection.authLevel > 1)
                return true;
            return permission.UserHasPermission(player.userID.ToString(), permname);
        }
        private void LoadData()
        {
            storedData = Interface.GetMod().DataFileSystem.ReadObject<StoredData>("MyEvents");
            if (Convert.ToString(Config["Plugin", "Version"]) != version)
            {
                Puts("Uh oh! Not up to date! No Worries, lets update you!");
                switch(version)
                {
                    case "0.0.1":
                        Config["Plugin", "Version"] = version;
                        break;
                    case "0.0.2":
                        Config["Plugin", "Version"] = version;
                        Puts("Updating your Data File! Hang On!");
                        foreach (var storedEvent in storedData.Events)
                        {

                            if (storedEvent.CommandLine == null)
                            {
                                storedEvent.CommandLine = "";
                            }
                        }
                        Interface.GetMod().DataFileSystem.WriteObject("MyEvents", storedData);
                        break;
                    case "0.0.3":
                        Config["Plugin", "Version"] = version;
                        Puts("Updating your Data File! Hang On!");
                        foreach (var storedEvent in storedData.Events)
                        {

                            if (storedEvent.Schedule == null)
                            {
                                storedEvent.Schedule = "none";
                            }
                        }
                        Interface.GetMod().DataFileSystem.WriteObject("MyEvents", storedData);
                        break;
                    default:
                        Config["Plugin", "Version"] = version;
                        break;
                }
                SaveConfig();

            }
            permission.RegisterPermission("canplanevent", this);
            // Load Imformation from a Config file
            if (!PopupNotifications)
            {
                Puts("PopupNotifications not found! Using text based notifications!");
                PopUpNotifier = false;
            } else
            {
                
                if (Convert.ToBoolean(Config["Plugin", "EnablePopUps"]).ToString() != null)
                {
                    PopUpNotifier = Convert.ToBoolean(Config["Plugin", "EnablePopUps"]);
                }
            }
            string[] seperator = new string[] { "," };
            // Build array of events
            int tick;
            if (Convert.ToInt16(Config["Plugin", "TimeCheck"]) > 0)
            {
                tick = Convert.ToInt16(Config["Plugin", "TimeCheck"]);
            } else
            {
                tick = 10;
            }
            string daystring = Convert.ToString(Config["Events", "AllowedDays"]);
            days = daystring.Split(seperator, StringSplitOptions.RemoveEmptyEntries);
            timer.Repeat(tick, 0, () => EventCheck());
        }

        [ChatCommand("notification")] // Whatever cammand you want the player to type
        private void notification(BasePlayer player, string command, string[] args)
        {
            if(!hasPermission(player, "canplanevent")) { SendChatMessage(player, "Timed Notifications", "You don't have access to this command"); return; }
            switch (args.Length)
            {
                case 0:
                    SendHelp(player);
                    break;
                case 1:
                    switch (args[0])
                    {
                        case "list":
                            // List Notifications
                            int count = 0;
                            foreach (var storedEvent in storedData.Events)
                            {
                                if (!storedEvent.broadcast)
                                {
                                    count++;
                                    SendChatMessage(player, "", "ID: " +count + " - (D: " + storedEvent.EventDate.Day + "/" + storedEvent.EventDate.Month + "/" + storedEvent.EventDate.Year + " T: " + storedEvent.EventDate.Hour + ":" + storedEvent.EventDate.Minute + ") " + storedEvent.EventInfo);
                                }
                            }
                            if(count == 0)
                            {
                                SendChatMessage(player, "Timed Notifications", "No notifications planned! Time for some Planning!");
                            }
                                break;
                        case "reset":
                            // Remove all notifications
                            storedData.Events.Clear();
                            Interface.GetMod().DataFileSystem.WriteObject("MyEvents", storedData);
                            SendChatMessage(player, "Timed Notifications", "Events have been removed!");
                            break;
                        default:
                            SendHelp(player);
                            break;
                    }
                    break;
                case 2:
                case 3:
                case 4:
                    switch (args[0].ToLower())
                    {
                        case "add":
                        case "hourly":
                        case "daily":
                        case "weekly":
                        case "monthly":
                        case "yearly":
                        case "addcmd":
                        case "hourlycmd":
                        case "dailycmd":
                        case "weeklycmd":
                        case "monthlycmd":
                        case "yearlycmd":
                            string[] datepart2 = args[1].Split(sep);
                            string[] timepart2 = args[2].Split(sep2);
                            try
                            {
                                setDate = new DateTime(datepart2[2].ToInt(), datepart2[1].ToInt(), datepart2[0].ToInt(), timepart2[0].ToInt(), timepart2[1].ToInt(), 0);
                            }
                            catch
                            {
                                SendHelp(player);
                            }

                            string cmdinfo = args[3];
                            EventData infocmd = new EventData(setDate, cmdinfo, args[0]);
                            storedData.Events.Add(infocmd);
                            SendChatMessage(player, "Timed Notifications", "Event Saved");
                            Interface.GetMod().DataFileSystem.WriteObject("MyEvents", storedData);
                            break;
                        case "remove":

// List Notifications
                            int count = 0;
                            foreach (EventData storedEvent in storedData.Events)
                            {
                                if (!storedEvent.broadcast)
                                {
                                    count++;
                                    if(count.ToString() == args[1])
                                    {
                                        storedEvent.broadcast = true;
                                        Interface.GetMod().DataFileSystem.WriteObject("MyEvents", storedData);
                                    }
                                }
                            }
                            SendChatMessage(player, "Timed Notifications", "Event Removed");
                            break;
                        default:
                            SendHelp(player);
                            break;
                    }
                    break;
                default:
                    SendHelp(player);
                    break;
            }
            // Function for the chat command
        }
        void EventCheck()
        {
            if (!days.Contains(DateTime.Now.ToUniversalTime().ToString("ddd")))
            {
                return;
            } else {
                foreach(var storedEvent in storedData.Events)
                {
                    if(storedEvent.EventDate.Date <= DateTime.UtcNow && !storedEvent.broadcast)
                    {
                        Puts(storedEvent.EventDate.Date.ToString() + " <= " + DateTime.UtcNow.ToString());
                        if (storedEvent.CommandLine == "")
                        {
                            SendNotification(storedEvent.EventInfo, Convert.ToInt16(Config["Plugin", "PopUpTime"]));
                        } else
                        {
                            var rust = new Oxide.Game.Rust.Libraries.Rust();
                            
                            string args = storedEvent.CommandLine.Remove(0,storedEvent.CommandLine.IndexOf(" ") +1);

                            string command = storedEvent.CommandLine.Substring(0, storedEvent.CommandLine.IndexOf(" "));
                            rust.RunServerCommand(command, args);
                        }
                        switch (storedEvent.Schedule.ToLower())
                        {
                            case "hourly":
                                while (storedEvent.EventDate.Date <= DateTime.UtcNow)
                                {
                                    storedEvent.EventDate = storedEvent.EventDate.AddHours(1);
                                }
                                break;
                            case "daily":
                                while (storedEvent.EventDate.Date <= DateTime.UtcNow)
                                {
                                    storedEvent.EventDate = storedEvent.EventDate.AddDays(1);
                                }
                                break;
                            case "weekly":
                                while (storedEvent.EventDate.Date <= DateTime.UtcNow)
                                {
                                    storedEvent.EventDate = storedEvent.EventDate.AddDays(7);
                                }
                                break;
                            case "monthly":
                                while (storedEvent.EventDate.Date <= DateTime.UtcNow)
                                {
                                    storedEvent.EventDate = storedEvent.EventDate.AddMonths(1);
                                }
                                break;
                            case "yearly":
                                while (storedEvent.EventDate.Date <= DateTime.UtcNow)
                                {
                                    storedEvent.EventDate = storedEvent.EventDate.AddYears(1);
                                }
                                break;
                            default:
                                storedEvent.broadcast = true;
                                break;
                        }
                        Interface.GetMod().DataFileSystem.WriteObject("MyEvents", storedData);
                    }
                }
                // Check for Events on that day
                Interface.GetMod().DataFileSystem.WriteObject("MyEvents", storedData);

            }
        }
        void SendHelp(BasePlayer player)
        {
            SendChatMessage(player, "", "/notification (add|hourly|daily|weekly|monthly|yearly) <DD/MM/YY> <HH:MM> \"<MESSAGE>\" - To schedule a notification for the specified time");
            SendChatMessage(player, "", "/notification (addcmd|hourlycmd|dailycmd|weeklycmd|monthlycmd|yearlycmd) <DD/MM/YY> <HH:MM> \"<COMMAND>\" - To schedule a console command for the specified time");
            SendChatMessage(player, "", "/notification list - List Future Events");
            SendChatMessage(player, "", "/notification reset - Remove Current and Past Events");
            SendChatMessage(player, "", "/notification remove <ID> - Removes specified event - IDs will alter on removal");
        }
        //---------------------------->   Chat Sending   <----------------------------//
        void SendNotification(string message, int delay=5,BasePlayer player=null)
        {
            if (PopUpNotifier && PopupNotifications)
            {
                
                PopupNotifications.Call("CreatePopupNotification", "<color=orange>Notification:</color> " + message, player, delay);
               
            }
            else
            {
                if (player != null)
                {
                    SendChatMessage(player, "Notification", message);
                } else
                {
                    BroadcastChat("Notification", message);
                }
            }
        }
        void BroadcastChat(string prefix, string msg)
        {
            PrintToChat("<color=orange>" + prefix + "</color>: " + msg);
        }
        void SendChatMessage(BasePlayer player, string prefix, string msg)
        {
            if (prefix != "")
            {
                prefix = "<color=orange>" + prefix + "</color>: ";
            } else
            {
                prefix = "";
            }
            SendReply(player, prefix + msg);
        }
        //---------------------------------------------------------------------------//
    }
}
