using Oxide.Core;
using Oxide.Core.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;


namespace Oxide.Plugins
{
    [Info("DateCore", "Jordi", 0.3, ResourceId = 1416)]
    [Description("Counts rust days like real life days")]
    public class DateCore : RustPlugin
    {
        public Hash<BasePlayer, Timer> Timers = new Hash<BasePlayer, Timer>();
        public static string json = @"[
            {
                ""name"": ""DateMsg"",
                ""parent"": ""HUD/Overlay"",
                ""components"":
                [
                    {
                         ""type"":""UnityEngine.UI.Image"",
                         ""color"":""0.1 0.1 0.1 0.7"",
                    },
                    {
                        ""type"":""RectTransform"",
                        ""anchormin"": ""0.5 0.95"",
                        ""anchormax"": ""0.980 0.99""
                    }
                ]
            },
            {
                ""parent"": ""DateMsg"",
                ""components"":
                [
                    {
                        ""type"":""UnityEngine.UI.Text"",
                        ""color"":""1 1 1 0.7"",
                        ""text"":""{msg}"",
                        ""fontSize"":15,
                        ""align"": ""MiddleCenter"",
                    },
                    {
                        ""type"":""RectTransform"",
                        ""anchormin"": ""0 0.1"",
                        ""anchormax"": ""1 0.8""
                    }
                ]
            }
        ]
        ";
        public void LoadMsgGui(string Msg, BasePlayer ply)
        {
            Game.Rust.Cui.CuiHelper.DestroyUi(ply, "DateMsg");
            CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo { connection = ply.net.connection }, null, "AddUI", new Facepunch.ObjectList(json.Replace("{msg}", Msg)));
        }

        public void DestroyGui(BasePlayer player)
        {
            Game.Rust.Cui.CuiHelper.DestroyUi(player, "DateMsg");
        }
        public static Hash<String,Action<int>> onday = new Hash<String,Action<int>>();
        public static Hash<String, Action<int>> onweek = new Hash<String, Action<int>>();
        public static Hash<String, Action<int>> onmonth = new Hash<String, Action<int>>();
        public static String[] Months = { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "Nobember", "December" };
        int time = 0;
        public Hash<String, Action<int>> onyear = new Hash<String, Action<int>>();
        class StoredData
        {
            public HashSet<String> td = new HashSet<String>();
            public Dictionary<String, Double> munnie = new Dictionary<String, Double>();
            public int rustday = 1;
            public int day = 1;
            public int rustdayweek = 1;
            public string rustdaystr = "Monday";
            public int rustdayyear = 0;
            public int rustweek = 0;
            public int rustmonth = 0;
            public string rustmonthstring = "January";
            public int rustyear = 0;
            public StoredData()
            {
                
            }

        }
        
        StoredData storedData;
        Boolean addonday(Action<int> act, String str)
        {
            if (!(onday.ContainsKey(str)))
            {
                Puts("onday hook added!");
                onday[str] = (act);
            }
            return (!(onday.ContainsKey(str)));
        }
        Boolean addonweek(Action<int> act, String str)
        {
            if (!(onweek.ContainsKey(str)))
            {
                Puts("onday hook added!");
                onweek[str] = (act);
            }
            return (!(onweek.ContainsKey(str)));
        }
        Boolean addonmonth(Action<int> act, String str)
        {
            if (!(onmonth.ContainsKey(str)))
            {
                Puts("onmonth hook added!");
                onmonth[str] = (act);
            }
            return (!(onmonth.ContainsKey(str)));
        }
        Boolean addonyear(Action<int> act, String str)
        {
            if (!(onyear.ContainsKey(str)))
            {
                Puts("onyear hook added!");
                onyear[str] = (act);
            }
            return (!(onyear.ContainsKey(str)));
        }

        //SKIP

        Boolean removeonday(String str)
        {
            if (!(onday.ContainsKey(str)))
            {
                onday.Remove(str);
                Puts(str+" hook removed!");
                return true;
            }
            return false;
        }
        Boolean removeonweek(String str)
        {
            if (!(onweek.ContainsKey(str)))
            {
                onweek.Remove(str);
                Puts(str + " hook removed!");
                return true;
            }
            return false;
        }
        Boolean removeonmonth(String str)
        {
            if (!(onmonth.ContainsKey(str)))
            {
                onmonth.Remove(str);
                Puts(str + " hook removed!");
                return true;
            }
            return false;
        }
        Boolean removeonyear(String str)
        {
            if (onyear.ContainsKey(str))
            {
                onyear.Remove(str);
                Puts(str + " hook removed!");
                return true;
            }
            return false;
        }

        //SKIP

       void Init()
        {
            storedData = Interface.GetMod().DataFileSystem.ReadObject<StoredData>("DateCore");
            foreach (BasePlayer ply in BasePlayer.activePlayerList)
            {
                if (!Timers.ContainsKey(ply))
                {
                    Timer tm = timer.Repeat(5f, 0, () =>
                    {
                        String msg = "Current serverdate(rust-days): " + storedData.rustdayyear + "(" + storedData.rustdaystr + ") - " + storedData.rustmonth + "(" + storedData.rustmonthstring + ") - " + storedData.rustyear;
                        DestroyGui(ply);
                        LoadMsgGui(msg, ply);
                    });
                    Timers.Add(ply, tm);
                }
            }
            Puts("##DateCore##");
            Puts("Initiating cores and registering timers");
            Load();
            timer.Repeat(1, 0, () =>
            {
                try
                {
                    if (storedData.day == 0)
                    {
                        if (Math.Floor(TOD_Sky.Instance.Cycle.Hour) == 12)
                        {
                            storedData.day = 1;
                            storedData.rustday = storedData.rustday + 1;
                            storedData.rustdayyear = storedData.rustdayyear + 1;
                            storedData.rustdayweek = storedData.rustdayweek + 1;
                            rustday();
                            decimal dec = storedData.rustday / 7;
                            storedData.rustweek = int.Parse(Math.Floor(dec).ToString());
                            if (storedData.rustdayweek == 8)
                            {
                                storedData.rustdayweek = 1;
                            }
                            if (storedData.rustdayweek == 1)
                            {
                                storedData.rustdaystr = "Monday";
                            }
                            if (storedData.rustdayweek == 2)
                            {
                                storedData.rustdaystr = "Tuesday";
                            }
                            if (storedData.rustdayweek == 3)
                            {
                                storedData.rustdaystr = "Wednesday";
                            }
                            if (storedData.rustdayweek == 4)
                            {
                                storedData.rustdaystr = "Thursday";
                            }
                            if (storedData.rustdayweek == 5)
                            {
                                storedData.rustdaystr = "Friday";
                            }
                            if (storedData.rustdayweek == 6)
                            {
                                storedData.rustdaystr = "Saturday";
                            }
                            if (storedData.rustdayweek == 7)
                            {
                                storedData.rustdaystr = "Sunday";
                            }
                            if (storedData.rustdayyear == 365)
                            {
                                storedData.rustdayyear = 0;
                                storedData.rustyear = storedData.rustyear + 1;
                                rustyear();
                            }
                            if ((storedData.rustyear / 4).ToString().Contains(".") == false)
                            {
                                if (storedData.rustdayyear == 0)
                                {
                                    storedData.rustmonthstring = Months[0];
                                    storedData.rustmonth = 1;
                                    rustmonth();
                                }
                                if (storedData.rustdayyear - 31 + 1 == 1)
                                {
                                    storedData.rustmonthstring = Months[1];
                                    storedData.rustmonth = 2;
                                    rustmonth();
                                }
                                if (storedData.rustdayyear - 31 - 28 + 1 == 1)
                                {
                                    storedData.rustmonthstring = Months[2];
                                    storedData.rustmonth = 3;
                                    rustmonth();
                                }
                                if (storedData.rustdayyear - 31 - 28 - 31 + 1 == 1)
                                {
                                    storedData.rustmonthstring = Months[3];
                                    storedData.rustmonth = 4;
                                    rustmonth();
                                }
                                if (storedData.rustdayyear - 31 - 28 - 31 - 30 + 1 == 1)
                                {
                                    storedData.rustmonthstring = Months[4];
                                    storedData.rustmonth = 5;
                                    rustmonth();
                                }
                                if (storedData.rustdayyear - 31 - 28 - 31 - 30 - 31 + 1 == 1)
                                {
                                    storedData.rustmonthstring = Months[5];
                                    storedData.rustmonth = 6;
                                    rustmonth();
                                }
                                if (storedData.rustdayyear - 31 - 28 - 31 - 30 - 31 - 30 + 1 == 1)
                                {
                                    storedData.rustmonthstring = Months[6];
                                    storedData.rustmonth = 7;
                                    rustmonth();
                                }
                                if (storedData.rustdayyear - 31 - 28 - 31 - 30 - 31 - 30 - 31 + 1 == 1)
                                {
                                    storedData.rustmonthstring = Months[7];
                                    storedData.rustmonth = 8;
                                    rustmonth();
                                }
                                if (storedData.rustdayyear - 31 - 28 - 31 - 30 - 31 - 30 - 31 - 31 + 1 == 1)
                                {
                                    storedData.rustmonthstring = Months[8];
                                    storedData.rustmonth = 9;
                                    rustmonth();
                                }
                                if (storedData.rustdayyear - 31 - 28 - 31 - 30 - 31 - 30 - 31 - 31 - 30 + 1 == 1)
                                {
                                    storedData.rustmonthstring = Months[9];
                                    storedData.rustmonth = 10;
                                    rustmonth();
                                }
                                if (storedData.rustdayyear - 31 - 28 - 31 - 30 - 31 - 30 - 31 - 31 - 30 - 31 + 1 == 1)
                                {
                                    storedData.rustmonthstring = Months[10];
                                    storedData.rustmonth = 11;
                                    rustmonth();
                                }
                                if (storedData.rustdayyear - 31 - 28 - 31 - 30 - 31 - 30 - 31 - 31 - 30 - 31 - 30 + 1 == 1)
                                {
                                    storedData.rustmonthstring = Months[11];
                                    storedData.rustmonth = 12;
                                    rustmonth();
                                }
                            }
                            else
                            {
                                if (storedData.rustdayyear == 0)
                                {
                                    storedData.rustmonthstring = Months[0];
                                    storedData.rustmonth = 1;
                                    rustmonth();
                                }
                                if (storedData.rustdayyear - 31 + 1 == 1)
                                {
                                    storedData.rustmonthstring = Months[1];
                                    storedData.rustmonth = 2;
                                    rustmonth();
                                }
                                if (storedData.rustdayyear - 31 - 29 + 1 == 1)
                                {
                                    storedData.rustmonthstring = Months[2];
                                    storedData.rustmonth = 3;
                                    rustmonth();
                                }
                                if (storedData.rustdayyear - 31 - 29 - 31 + 1 == 1)
                                {
                                    storedData.rustmonthstring = Months[3];
                                    storedData.rustmonth = 4;
                                    rustmonth();
                                }
                                if (storedData.rustdayyear - 31 - 29 - 31 - 30 + 1 == 1)
                                {
                                    storedData.rustmonthstring = Months[4];
                                    storedData.rustmonth = 5;
                                    rustmonth();
                                }
                                if (storedData.rustdayyear - 31 - 29 - 31 - 30 - 31 + 1 == 1)
                                {
                                    storedData.rustmonthstring = Months[5];
                                    storedData.rustmonth = 6;
                                    rustmonth();
                                }
                                if (storedData.rustdayyear - 31 - 29 - 31 - 30 - 31 - 30 + 1 == 1)
                                {
                                    storedData.rustmonthstring = Months[6];
                                    storedData.rustmonth = 7;
                                    rustmonth();
                                }
                                if (storedData.rustdayyear - 31 - 29 - 31 - 30 - 31 - 30 - 31 + 1 == 1)
                                {
                                    storedData.rustmonthstring = Months[7];
                                    storedData.rustmonth = 8;
                                    rustmonth();
                                }
                                if (storedData.rustdayyear - 31 - 29 - 31 - 30 - 31 - 30 - 31 - 31 + 1 == 1)
                                {
                                    storedData.rustmonthstring = Months[8];
                                    storedData.rustmonth = 9;
                                    rustmonth();
                                }
                                if (storedData.rustdayyear - 31 - 29 - 31 - 30 - 31 - 30 - 31 - 31 - 30 + 1 == 1)
                                {
                                    storedData.rustmonthstring = Months[9];
                                    storedData.rustmonth = 10;
                                    rustmonth();
                                }
                                if (storedData.rustdayyear - 31 - 29 - 31 - 30 - 31 - 30 - 31 - 31 - 30 - 31 + 1 == 1)
                                {
                                    storedData.rustmonthstring = Months[10];
                                    storedData.rustmonth = 11;
                                    rustmonth();
                                }
                                if (storedData.rustdayyear - 31 - 29 - 31 - 30 - 31 - 30 - 31 - 31 - 30 - 31 - 30 + 1 == 1)
                                {
                                    storedData.rustmonthstring = Months[11];
                                    storedData.rustmonth = 12;
                                    rustmonth();
                                }
                            }
                            Interface.GetMod().DataFileSystem.WriteObject<StoredData>("DateCore", storedData);
                        }
                    }
                    else
                    {
                        if (Math.Floor(TOD_Sky.Instance.Cycle.Hour) == 0)
                        {
                            storedData.day = 0;
                        }
                    }
                    Interface.GetMod().DataFileSystem.WriteObject<StoredData>("DateCore", storedData);
                }
                catch
                {

                }
            });
            Puts("Done bootingup DateCore!");
        }
        [ChatCommand("date")]
        void gd(BasePlayer player, string command, string[] args)
        {
            String msg = "Current serverdate(rust-days): " + storedData.rustdayyear + "(" + storedData.rustdaystr + ") - " + storedData.rustmonth + "(" + storedData.rustmonthstring + ") - " + storedData.rustyear;
            DestroyGui(player);
            LoadMsgGui(msg, player);
            SendReply(player, msg);
            rust.BroadcastChat("Current serverdate(rust-days): " + storedData.rustdayyear + "(" + storedData.rustdaystr + ") - " + storedData.rustmonth + "(" + storedData.rustmonthstring + ") - " + storedData.rustyear);
        }
        void rustday()
        {
            try
            {
                Puts("Current serverdate(rust-days): " + storedData.rustdayyear + "(" + storedData.rustdaystr + ") - " + storedData.rustmonth + "(" + storedData.rustmonthstring + ") - " + storedData.rustyear);
                foreach (String act in onday.Keys)
                {
                    onday[act](storedData.rustday);
                }
            }
            catch
            {
            }
        }

        void rustweek()
        {
            try
            {
                Puts("Current serverdate(rust-days): " + storedData.rustdayyear + "(" + storedData.rustdaystr + ") - " + storedData.rustmonth + "(" + storedData.rustmonthstring + ") - " + storedData.rustyear);
           foreach (String act in onweek.Keys)
                {
                    onweek[act](storedData.rustday);
                }
                }
                catch
                {

                }
        }

        void rustmonth()
        {
            try
            {
                Puts("Current serverdate(rust-days): " + storedData.rustdayyear + "(" + storedData.rustdaystr + ") - " + storedData.rustmonth + "(" + storedData.rustmonthstring + ") - " + storedData.rustyear);
                foreach (String act in onmonth.Keys)
                {
                    onmonth[act](storedData.rustday);
                }
                }
                catch
                {

                }
        }

        void rustyear()
        {
            try
            {
                Puts("Current serverdate(rust-days): " + storedData.rustdayyear + "(" + storedData.rustdaystr + ") - " + storedData.rustmonth + "(" + storedData.rustmonthstring + ") - " + storedData.rustyear);
                foreach (String act in onyear.Keys)
                {
                    onyear[act](storedData.rustday);
                }
            }
            catch
            {

            }
        }
    }
}
