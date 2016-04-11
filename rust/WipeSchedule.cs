using System.Collections.Generic;
using System;
using System.Globalization;
using Oxide.Core;
using System.Reflection;

namespace Oxide.Plugins
{
    [Info("Wipe Schedule", "Smoosher", "1.0.0")]
    [Description("Simple plugin to help players know when you plan to wipe next")]

    class WipeSchedule : RustPlugin
    {
        int DBW = 0;
        string TOW = "00:00:00";
        DateTime LastWipe = DateTime.Now.Date;
        int DBWBP = 0;
        string TOWBP = "00:00:00";
        string DateFormat = "";
        string RegionalDate = "en-US";
        DateTime LastWipeBP = DateTime.Now.Date;
        DateTime NextWipeDate;
        DateTime NextWipeDateBP;
        bool AnnounceToServer = false;
        bool AnnounceToPlayer = true;
        bool UseBPWipes = true;

        #region Config
        protected override void LoadDefaultConfig()
        {
            PrintWarning("Creating a new configuration file.");
            Config.Clear();

            Config["DaysBetweenWipes"] = "14";
            Config["RegionDateType"] = "en-US";
            Config["DatePattern"] = "MM/dd/yyyy";
            Config["UseBPWipes"] = true;
            Config["LastWipe"] = DateTime.Now.Date.ToString();
            Config["DaysBetweenWipesBP"] = "14";
            Config["LastWipeBP"] = DateTime.Now.Date.ToString();
            Config["AnnounceToServer"] = false;
            Config["AnnounceToPlayer"] = true;
            Config["UseBPWipes"] = true;

            PrintWarning("Config Created.");
        }

        private void SetConfig()
        {
            DateFormat = Config["DatePattern"].ToString();
            RegionalDate = Config["RegionDateType"].ToString();
            LastWipe = DateTime.Parse(Config["LastWipe"].ToString(), CultureInfo.CreateSpecificCulture(RegionalDate));
            DBW = Convert.ToInt32(Config["DaysBetweenWipes"]);
            DBWBP = Convert.ToInt32(Config["DaysBetweenWipesBP"]);
            LastWipeBP = DateTime.Parse(Config["LastWipeBP"].ToString(), CultureInfo.CreateSpecificCulture(RegionalDate));
            AnnounceToServer = TrueorFalse(Config["AnnounceToServer"].ToString());
            AnnounceToPlayer = TrueorFalse(Config["AnnounceToPlayer"].ToString());
            UseBPWipes = TrueorFalse(Config["UseBPWipes"].ToString());
            RegionalDate = Config["RegionDateType"].ToString();

        }
        #endregion

        #region Functions

        #region hooks

        void Loaded()
        {
            SetConfig();
            CultureInfo ci = new CultureInfo(RegionalDate);
            NextWipeDate = DateTime.Parse(LastWipe.ToString(DateFormat), CultureInfo.CreateSpecificCulture(RegionalDate));
            NextWipeDate = NextWipeDate.AddDays(DBW);
            //NextWipeDate = DateTime.Parse(NextWipeDate.ToString(DateFormat), CultureInfo.CreateSpecificCulture(RegionalDate));

            NextWipeDateBP = DateTime.Parse(LastWipeBP.ToString(DateFormat), CultureInfo.CreateSpecificCulture(RegionalDate));
            NextWipeDateBP = LastWipeBP.AddDays(DBWBP);
            //NextWipeDateBP = DateTime.Parse(NextWipeDateBP.ToString(DateFormat), CultureInfo.CreateSpecificCulture(RegionalDate));
        }

        void OnPlayerChat(ConsoleSystem.Arg arg)
        {
           foreach (var Item in arg.Args)
           {
                if (Item.ToString().ToLower().Contains("next wipe"))
                {
                    BasePlayer player = (BasePlayer)arg.connection.player;

                    if (player != null)
                    {
                        SendReply(player, "Last Map Wipe: " + LastWipe.ToString() + " Time Untill Next Map Wipe: " + NextWipeDays(NextWipeDate, LastWipe));
                        if(UseBPWipes)
                        SendReply(player, "Last BP Wipe: " + LastWipeBP.ToString() + " Time Untill Next BP Wipe: " + NextWipeDays(NextWipeDateBP, LastWipeBP));
                    }
                }
            }
        }


        #endregion

        #region Bool Functions
        private bool TrueorFalse(string input)
        {
            bool output;
            input = input.ToLower();
            switch (input)
            {
                case "true":
                    output = true;
                    return output;
                    break;

                case "false":
                    output = false;
                    return output;
                    break;

                default:
                    output = false;
                    return output;
                    break;
            }
        }

        #endregion

        #region Wipe Calcuations

        private string NextWipeDays(DateTime WipeDate, DateTime LastWipevar)
        {
            var TimeNow = DateTime.Parse(DateTime.Now.ToString(DateFormat), CultureInfo.CreateSpecificCulture(RegionalDate));
            Puts(TimeNow.ToString());
            TimeSpan t = WipeDate.Subtract(TimeNow);

            string TimeLeft = string.Format(string.Format("{0:D2} Days",
                t.Days));

            return TimeLeft;

        }
        #endregion

        #endregion

        #region Console Commands
        [ConsoleCommand("chat.test")]
        private void chattest(ConsoleSystem.Arg arg)
        {
            Puts("Last Map Wipe: "+LastWipe.ToString(DateFormat) +" Time Untill Next Map Wipe: "+NextWipeDays(NextWipeDate, LastWipe));
            if(UseBPWipes)
            Puts("Last BP Wipe: " + LastWipeBP.ToString(DateFormat) + " Time Untill Next BP Wipe: " + NextWipeDays(NextWipeDateBP, LastWipeBP));
        }
        #endregion

        #region ChatCommands
        [ChatCommand("NextWipe")]
        private void NextWipe(BasePlayer player, string command, string[] args)
        {
            Puts(NextWipeDate.ToString());
            Puts(NextWipeDateBP.ToString());

            SendReply(player, "Last Map Wipe: " + LastWipe.ToString() + " Time Untill Next Map Wipe: " + NextWipeDays(NextWipeDate, LastWipe));
            if(UseBPWipes)
            SendReply(player, "Last BP Wipe: " + LastWipeBP.ToString() + " Time Untill Next BP Wipe: " + NextWipeDays(NextWipeDateBP, LastWipeBP));

        }

        #endregion

    }
}
