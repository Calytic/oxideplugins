using System;
using System.Collections.Generic;
using System.Globalization;

namespace Oxide.Plugins
{
    [Info("WipeSchedule", "k1lly0u", "2.0.1", ResourceId = 1451)]
    class WipeSchedule : RustPlugin
    {
        #region Fields
        DateTime NextWipeDate;
        DateTime NextWipeDateXP;
        Timer announceTimer;
        #endregion

        #region Oxide Hooks 
        void Loaded()
        {
            lang.RegisterMessages(messages, this);
            LoadVariables();
        }
        void OnServerInitialized()
        {
            if (!configData.UseManualNextWipe)
                UpdateWipeDates();
            else LoadWipeDates();

            if (configData.AnnounceOnTimer)
            {
                announceTimer = timer.Repeat((configData.AnnounceTimer * 60) * 60, 0, ()=> BroadcastWipe()); 
            }
        }
        void OnPlayerInit(BasePlayer player)
        {
            if (configData.AnnounceOnJoin)
            {
                cmdNextWipe(player, "", new string[0]);
            }
        }
        void Unload() => announceTimer.Destroy();
        #endregion

        #region Functions
        private void UpdateWipeDates()
        {
            NextWipeDate = DateTime.Parse(configData.LastWipe, CultureInfo.CreateSpecificCulture(configData.RegionalDateType));
            NextWipeDate = NextWipeDate.AddDays(configData.DaysBetweenWipes);
            NextWipeDateXP = DateTime.Parse(configData.LastXPWipe, CultureInfo.CreateSpecificCulture(configData.RegionalDateType));
            NextWipeDateXP = NextWipeDateXP.AddDays(configData.DaysBetweenXPWipes);
        }
        private void LoadWipeDates()
        {
            NextWipeDate = DateTime.Parse(configData.NextWipe, CultureInfo.CreateSpecificCulture(configData.RegionalDateType));
            NextWipeDateXP = DateTime.Parse(configData.NextXPWipe, CultureInfo.CreateSpecificCulture(configData.RegionalDateType));
        }
        private string NextWipeDays(DateTime WipeDate, DateTime LastWipevar)
        {
            var TimeNow = DateTime.Parse(DateTime.Now.ToString(configData.DateFormat), CultureInfo.CreateSpecificCulture(configData.RegionalDateType));
            TimeSpan t = WipeDate.Subtract(TimeNow);

            string TimeLeft = string.Format(string.Format("{0:D2} Days",t.Days));

            return TimeLeft;
        }
        private void BroadcastWipe()
        {
            PrintToChat(string.Format(MSG("lastMapWipe", null), configData.LastWipe, NextWipeDays(NextWipeDate, DateTime.Parse(configData.LastWipe))));
            if (configData.ShowXPWipeSchedule)
                PrintToChat(string.Format(MSG("lastXPWipe", null), configData.LastWipe, NextWipeDays(NextWipeDateXP, DateTime.Parse(configData.LastXPWipe))));
        }
        #endregion

        #region ChatCommands
        [ChatCommand("nextwipe")]
        private void cmdNextWipe(BasePlayer player, string command, string[] args)
        {
            SendReply(player, string.Format(MSG("lastMapWipe", player.UserIDString), configData.LastWipe, NextWipeDays(NextWipeDate, DateTime.Parse(configData.LastWipe))));
            if (configData.ShowXPWipeSchedule)
                SendReply(player, string.Format(MSG("lastXPWipe", player.UserIDString), configData.LastXPWipe, NextWipeDays(NextWipeDateXP, DateTime.Parse(configData.LastXPWipe))));
        }
        [ChatCommand("setwipe")]
        private void cmdSetWipe(BasePlayer player, string command, string[] args)
        {
            if (!player.IsAdmin()) return;
            if (args == null || args.Length == 0)
            {
                SendReply(player, $"<color=#ffae1a>/setwipe map</color>{MSG("setWipeMap", player.UserIDString)}");
                SendReply(player, $"<color=#ffae1a>/setwipe xp</color>{MSG("setWipeXP", player.UserIDString)}");
                SendReply(player, $"<color=#ffae1a>/setwipe map <date></color>{MSG("setWipeMapManual", player.UserIDString)}");
                SendReply(player, $"<color=#ffae1a>/setwipe xp <date></color>{MSG("setWipeXPManual", player.UserIDString)}");
                return;
            }
            if (args.Length == 1)
            {
                switch (args[0].ToLower())
                {
                    case "map":
                        {
                            configData.LastWipe = DateTime.Now.Date.ToString("MM/dd/yyyy");
                            SaveConfig(configData);
                            UpdateWipeDates();
                            SendReply(player, string.Format(MSG("savedWipeMap", player.UserIDString), configData.LastWipe));
                        }
                        return;
                    case "xp":
                        {
                            configData.LastXPWipe = DateTime.Now.Date.ToString("MM/dd/yyyy");
                            SaveConfig(configData);
                            UpdateWipeDates();
                            SendReply(player, string.Format(MSG("savedWipeXP", player.UserIDString), configData.LastXPWipe));
                        }
                        return;
                    default:
                        return;
                }
            }
            if (args.Length == 2)
            {
                switch (args[0].ToLower())
                {
                    case "map":
                        {
                            DateTime time;
                            if (DateTime.TryParse(args[1], out time))
                            {
                                configData.LastWipe = time.ToString("MM/dd/yyyy");
                                SaveConfig(configData);
                                UpdateWipeDates();
                                SendReply(player, string.Format(MSG("savedWipeMap", player.UserIDString), configData.LastWipe));
                            }                            
                        }
                        return;
                    case "xp":
                        {
                            DateTime time;
                            if (DateTime.TryParse(args[1], out time))
                            {
                                configData.LastXPWipe = time.ToString("MM/dd/yyyy");
                                SaveConfig(configData);
                                UpdateWipeDates();
                                SendReply(player, string.Format(MSG("savedWipeXP", player.UserIDString), configData.LastXPWipe));
                            }
                        }
                        return;
                    default:
                        return;
                }
            }
        }

        [ConsoleCommand("setwipe")]
        private void ccmdSetWipe(ConsoleSystem.Arg arg)
        {
            if (arg.connection == null)
            {
                if (arg.Args == null || arg.Args.Length == 0)
                {
                    SendReply(arg, $"setwipe map{MSG("setWipeMap", null)}");
                    SendReply(arg, $"setwipe xp{MSG("setWipeXP", null)}");
                    
                    return;
                }
                if (arg.Args.Length == 1)
                {
                    switch (arg.Args[0].ToLower())
                    {
                        case "map":
                            {
                                configData.LastWipe = DateTime.Now.Date.ToString("MM/dd/yyyy");
                                SaveConfig(configData);
                                UpdateWipeDates();
                                SendReply(arg, string.Format(MSG("savedWipeMap", null), configData.LastWipe));
                            }
                            return;
                        case "xp":
                            {
                                configData.LastXPWipe = DateTime.Now.Date.ToString("MM/dd/yyyy");
                                SaveConfig(configData);
                                UpdateWipeDates();
                                SendReply(arg, string.Format(MSG("savedWipeXP", null), configData.LastXPWipe));
                            }
                            return;
                        default:
                            return;
                    }
                }                
            }
        }
        [ChatCommand("setnextwipe")]
        private void cmdSetNextWipe(BasePlayer player, string command, string[] args)
        {
            if (!player.IsAdmin()) return;
            if (args == null || args.Length == 0)
            {                
                SendReply(player, $"<color=#ffae1a>/setnextwipe map <date></color>{MSG("setNextWipeMapManual", player.UserIDString)}");
                SendReply(player, $"<color=#ffae1a>/setnextwipe xp <date></color>{MSG("setNextWipeXPManual", player.UserIDString)}");
                return;
            }            
            if (args.Length == 2)
            {
                switch (args[0].ToLower())
                {
                    case "map":
                        {
                            DateTime time;
                            if (DateTime.TryParse(args[1], out time))
                            {
                                configData.NextWipe = time.ToString("MM/dd/yyyy");
                                SaveConfig(configData);
                                LoadWipeDates();
                                SendReply(player, string.Format(MSG("savedNextWipeMap", player.UserIDString), configData.NextWipe));
                            }
                        }
                        return;
                    case "xp":
                        {
                            DateTime time;
                            if (DateTime.TryParse(args[1], out time))
                            {
                                configData.NextXPWipe = time.ToString("MM/dd/yyyy");
                                SaveConfig(configData);
                                LoadWipeDates();
                                SendReply(player, string.Format(MSG("savedNextWipeXP", player.UserIDString), configData.NextXPWipe));
                            }
                        }
                        return;
                    default:
                        return;
                }
            }
        }

        [ConsoleCommand("setnextwipe")]
        private void ccmdSetNextWipe(ConsoleSystem.Arg arg)
        {
            if (arg.connection == null)
            {
                if (arg.Args == null || arg.Args.Length == 0)
                {
                    SendReply(arg, $"setnextwipe map <date>{MSG("setNextWipeMapManual", null)}");
                    SendReply(arg, $"setnextwipe xp <date>{MSG("setNextWipeXPManual", null)}");

                    return;
                }
                if (arg.Args.Length == 2)
                {
                    DateTime time;
                    switch (arg.Args[0].ToLower())
                    {
                        case "map":                           
                            if (DateTime.TryParse(arg.Args[1], out time))
                            {
                                configData.NextWipe = time.ToString("MM/dd/yyyy");
                                SaveConfig(configData);
                                LoadWipeDates();
                                SendReply(arg, string.Format(MSG("savedNextWipeMap"), configData.NextWipe));
                            }
                            return;
                        case "xp":
                            if (DateTime.TryParse(arg.Args[1], out time))
                            {
                                configData.NextXPWipe = time.ToString("MM/dd/yyyy");
                                SaveConfig(configData);
                                LoadWipeDates();
                                SendReply(arg, string.Format(MSG("savedNextWipeXP"), configData.NextXPWipe));
                            }
                            return;
                        default:
                            return;
                    }
                }
            }
        }
        #endregion

        #region Config        
        private ConfigData configData;
        class ConfigData
        {
            public string RegionalDateType { get; set; }
            public string DateFormat { get; set; }
            public int DaysBetweenWipes { get; set; }
            public int DaysBetweenXPWipes { get; set; }
            public string LastWipe { get; set; }
            public string LastXPWipe { get; set; }
            public string NextWipe { get; set; }
            public string NextXPWipe { get; set; }
            public bool ShowXPWipeSchedule { get; set; }           
            public bool AnnounceOnJoin { get; set; }
            public bool UseManualNextWipe { get; set; }
            public bool AnnounceOnTimer { get; set; }
            public int AnnounceTimer { get; set; }
        }
        private void LoadVariables()
        {
            LoadConfigVariables();
            SaveConfig();
        }
        protected override void LoadDefaultConfig()
        {
            var config = new ConfigData
            {
                AnnounceOnJoin = true,
                AnnounceOnTimer = true,
                AnnounceTimer = 3,
                DateFormat = "MM/dd/yyyy",
                DaysBetweenWipes = 14,
                DaysBetweenXPWipes = 14,
                LastWipe = DateTime.Now.Date.ToString("MM/dd/yyyy"),
                LastXPWipe = DateTime.Now.Date.ToString("MM/dd/yyyy"),
                UseManualNextWipe = false,
                NextWipe = DateTime.Now.Date.ToString("MM/dd/yyyy"),
                NextXPWipe = DateTime.Now.Date.ToString("MM/dd/yyyy"),
                RegionalDateType = "en-US",
                ShowXPWipeSchedule = true
        };
            SaveConfig(config);
        }
        private void LoadConfigVariables() => configData = Config.ReadObject<ConfigData>();
        void SaveConfig(ConfigData config) => Config.WriteObject(config, true);
        #endregion

        #region Messaging
        private string MSG(string key, string playerid = null) => lang.GetMessage(key, this, playerid);

        Dictionary<string, string> messages = new Dictionary<string, string>()
        {            
            {"lastMapWipe", "<color=#b3b3b3>Last Map Wipe:</color> <color=#ffae1a>{0}</color> <color=#b3b3b3>Time Until Next Map Wipe:</color> <color=#ffae1a>{1}</color>" },
            {"lastXPWipe", "<color=#b3b3b3>Last XP Wipe:</color> <color=#ffae1a>{0}</color> <color=#b3b3b3>Time Until Next XP Wipe:</color> <color=#ffae1a>{1}</color>" } ,
            {"setWipeMap", "<color=#b3b3b3> - Sets the current time as last map wipe</color>" },
            {"setWipeXP", "<color=#b3b3b3> - Sets the current time as last XP wipe</color>" },
            {"savedWipeMap", "<color=#b3b3b3>Successfully set last map wipe to:</color> <color=#ffae1a>{0}</color>" },
            {"savedWipeXP", "<color=#b3b3b3>Successfully set last XP wipe to:</color> <color=#ffae1a>{0}</color>" },
            {"setWipeMapManual", "<color=#b3b3b3> - Set the time of last map wipe. Format: MM/dd/yyyy</color>" },
            {"setWipeXPManual", "<color=#b3b3b3> - Set the time as last XP wipe. Format: MM/dd/yyyy</color>" },            
            {"savedNextWipeMap", "<color=#b3b3b3>Successfully set next map wipe to:</color> <color=#ffae1a>{0}</color>" },
            {"savedNextWipeXP", "<color=#b3b3b3>Successfully set next XP wipe to:</color> <color=#ffae1a>{0}</color>" },
            {"setNextWipeMapManual", "<color=#b3b3b3> - Set the time of next map wipe. Format: MM/dd/yyyy</color>" },
            {"setNextWipeXPManual", "<color=#b3b3b3> - Set the time as next XP wipe. Format: MM/dd/yyyy</color>" }
        };
        #endregion
    }
}
