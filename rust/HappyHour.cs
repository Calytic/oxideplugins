// Reference: Oxide.Ext.Rust
// Reference: Oxide.Ext.Unity
// Reference: Newtonsoft.Json

/*
 * The MIT License (MIT)
 * Copyright (c) 2015 feramor@computer.org
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:

 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.

 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 */

//Microsoft NameSpaces
using System;
using System.Collections.Generic;

//Oxide NameSpaces
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Core.Libraries;

//External NameSpaces
using Newtonsoft.Json;

namespace Oxide.Plugins
{

    [Info("Happy Hour Plugin", "Feramor", "1.0.4", ResourceId = 807)]
    public class HappyHour : RustPlugin
    {
        public Core.Configuration.DynamicConfigFile mySave;
        Dictionary<string, object> Users = new Dictionary<string, object>();
        static List<Oxide.Core.Libraries.Timer.TimerInstance> Timers = new List<Oxide.Core.Libraries.Timer.TimerInstance>();
        Time MainTime;
        Oxide.Core.Libraries.Timer MainTimer;
        void Init()
        {
            LoadConfig();
            mySave = Interface.GetMod().DataFileSystem.GetDatafile("HappyHour");
            if (mySave["Users"] != null)
                if (((Dictionary<string, object>)mySave["Users"]).Count != 0)
                    Users = (Dictionary<string, object>)mySave["Users"];
            mySaveData();
            MainTimer = Interface.GetMod().GetLibrary<Oxide.Core.Libraries.Timer>("Timer");
            MainTime = Interface.GetMod().GetLibrary<Time>("Time");
        }

        [HookMethod("OnServerInitialized")]
        void OnServerInitialized()
        {
            MainTimer.Once(0, () => WriteConsole(string.Format("Happy Hour : Adding Happy hours.")), this);
            MainTimer.Once(0, () => CalculateTimers(), this);
        }

        void CalculateTimers()
        {
            foreach (KeyValuePair<string, object> pair in (Dictionary<string, object>)Config["HappyHours"])
            {
                string[] Hour = pair.Key.Split(':');
                long CurrentTime = MainTime.GetUnixTimestamp();
                DateTime EventTimeData = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, Convert.ToInt32(Hour[0]), Convert.ToInt32(Hour[1]), Convert.ToInt32(Hour[2]), DateTimeKind.Utc);
                long EventTime = Convert.ToInt64((EventTimeData - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds);

                if (CurrentTime > EventTime)
                {
                    EventTimeData = EventTimeData.AddDays(1);
                    EventTime = Convert.ToInt64((EventTimeData - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds);
                }

                Dictionary<string, object> Event = (Dictionary<string, object>)pair.Value;
                Event["NextEvent"] = EventTime.ToString();

                List<ItemDefinition> AllItems = ItemManager.GetItemDefinitions();
                Dictionary<string, object> Items = (Dictionary<string, object>)Event["Items"];
                foreach (KeyValuePair<string, object> zItem in (Dictionary<string, object>)Items)
                {
                    Dictionary<string, object> CurrentItem = (Dictionary<string, object>)zItem.Value;
                    foreach (ItemDefinition SearchItem in AllItems)
                    {
                        if (SearchItem.displayName.english == zItem.Key.ToString())
                            CurrentItem["ID"] = SearchItem.shortname.ToString();
                    }
                }
                Oxide.Core.Libraries.Timer.TimerInstance newTimer = MainTimer.Once(EventTime - CurrentTime, () => HappyHours(this, pair), this);
                Timers.Add(newTimer);
                WriteConsole(string.Format("Happy Hour : Added happy hour @ UTC {0} : Next occurrence with server timezone {1}.", pair.Key.ToString(), EventTimeData.ToLocalTime().ToString()));
            }
            SaveConfig();
        }
        private void HappyHours(object sender , object PairObj)
        {
            HappyHour myPlugin = (HappyHour)sender;
            KeyValuePair<string, object> pair = (KeyValuePair<string, object>)PairObj;
            long CurrentTime = MainTime.GetUnixTimestamp();
            Dictionary<string, object> Event = (Dictionary<string, object>)pair.Value;
            if (CurrentTime < (Convert.ToInt64(Event["NextEvent"].ToString()) + Convert.ToInt64(myPlugin.Config["Time"].ToString())))
            {
                foreach (BasePlayer Player in BasePlayer.activePlayerList)
                {
                    if (myPlugin.Users.ContainsKey(Player.userID.ToString()) == false)
                    {
                        myPlugin.myPrintToChat(Player, Event["Message"].ToString());
                        Dictionary<string, object> Items = (Dictionary<string, object>)Event["Items"];
                        foreach (KeyValuePair<string, object> zItem in (Dictionary<string, object>)Items)
                        {
                            Dictionary<string, object> ItemVars = (Dictionary<string, object>)zItem.Value;
                            Item newItem = ItemManager.CreateByName(ItemVars["ID"].ToString(), Convert.ToInt32(ItemVars["Amount"].ToString()));
                            ItemContainer Cont = null;
                            switch (ItemVars["Amount"].ToString())
                            {
                                case "Belt":
                                    Cont = Player.inventory.containerBelt;
                                    break;
                                case "Wear":
                                    Cont = Player.inventory.containerWear;
                                    break;
                                default:
                                    Cont = Player.inventory.containerMain;
                                    break;
                            }
                            Player.inventory.GiveItem(newItem, Cont);
                        }
                        myPlugin.Users.Add(Player.userID.ToString(), CurrentTime.ToString());
                    }
                }
                myPlugin.mySaveData();
                Oxide.Core.Libraries.Timer.TimerInstance newTimer = MainTimer.Once(1, () => HappyHours(sender, PairObj), (Plugin)sender);
                Timers.Add(newTimer);
            }
            else
            {
                myPlugin.WriteConsole(string.Format("Happy Hour : Happy Hour ended @ {0}", DateTime.Now.ToString()));
                string[] Hour = pair.Key.Split(':');
                DateTime EventTimeData = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, Convert.ToInt32(Hour[0]), Convert.ToInt32(Hour[1]), Convert.ToInt32(Hour[2]), DateTimeKind.Utc);
                long EventTime = Convert.ToInt64((EventTimeData - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds);
                if (CurrentTime > EventTime)
                {
                    EventTimeData = EventTimeData.AddDays(1);
                    EventTime = Convert.ToInt64((EventTimeData - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds);
                }
                Event["NextEvent"] = EventTime.ToString();
                myPlugin.WriteConsole(string.Format("Happy Hour : User List cleared @ {0}", DateTime.Now.ToString()));
                myPlugin.WriteConsole(string.Format("Happy Hour : Same event will occur again @ {0}", EventTimeData.ToLocalTime().ToString()));
                myPlugin.Users.Clear();
                myPlugin.mySaveData();
                Oxide.Core.Libraries.Timer.TimerInstance newTimer = MainTimer.Once((EventTime - CurrentTime), () => HappyHours(sender, PairObj), (Plugin)sender);
                Timers.Add(newTimer);
            }
        }

        [HookMethod("Unload")]
        void myUnload()
        {
            EndTimers();
            Users.Clear();
            mySaveData();
        }

        [HookMethod("LoadDefaultConfig")]
        void myLoadDefaultConfig()
        {
            Dictionary<string, object> ConfigFile = new Dictionary<string, object>();
            Dictionary<string, object> NewHappyHour = new Dictionary<string, object>();
            Dictionary<string, object> Items = new Dictionary<string, object>();
            Dictionary<string, object> Item = new Dictionary<string, object>();
            Item.Add("Amount", 1);
            Item.Add("Type", "Belt");
            Items.Add("Stone Hatchet", Item);
            Items.Add("Building Plan", Item);
            NewHappyHour.Add("Message", "Its time to be happy...");
            NewHappyHour.Add("Items", Items);
            ConfigFile.Add("22:00:00", NewHappyHour);
            ConfigFile.Add("23:00:00", NewHappyHour);
            Config.Clear();
            Config["Time"] = "600";
            Config["ChatTag"] = "Happy Hour";
            Config["HappyHours"] = ConfigFile;
            SaveConfig();

        }
        void OnTick()
        {

        }
        void EndTimers()
        {
            foreach (Oxide.Core.Libraries.Timer.TimerInstance CurrentTimer in Timers)
            {
                if (CurrentTimer != null)
                    if (CurrentTimer.Destroyed == false)
                        CurrentTimer.Destroy();
            }
            Timers.Clear();
        }
        public void WriteConsole(string myText)
        {
            this.Puts(myText);
        }
        public void mySaveData()
        {
            mySave["Users"] = Users;
            Interface.GetMod().DataFileSystem.SaveDatafile("HappyHour");
        }
        public void myPrintToChat(BasePlayer Player, string format, params object[] Args)
        {
            Player.SendConsoleCommand("chat.add", 0, string.Format("<color=orange>{0}</color>  {1}", Config["ChatTag"].ToString(), string.Format(format, Args)), 1.0);
        }
    }
}