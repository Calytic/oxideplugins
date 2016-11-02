using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;
//using UnityEngine;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Linq;

namespace Oxide.Plugins
{
    [Info("SmoothRestart", "Fujikura/Visagalis", "0.5.0", ResourceId = 1826)]
    public class SmoothRestart : RustPlugin
    {
        bool Changed;
		DateTime restartTime = DateTime.MinValue;
		Dictionary<BasePlayer, Timer> timers = new Dictionary<BasePlayer, Timer>();
        Timer activeTimer = null;
		int lastMinute;
		int lastSecond;
		int currentCountDown;
		bool countDownActive;
		bool secondsActive;
		bool timerActivated;
		
		UIColor deathNoticeShadowColor = new UIColor(0.1, 0.1, 0.1, 0.8);
        UIColor deathNoticeColor = new UIColor(0.85, 0.85, 0.85, 0.1);
		
		Dictionary<string, object> rebootTimes = new Dictionary<string, object>();
		List<object> countDownMinutes = new List<object>();
		List<object> countDownSeconds = new List<object>();
		bool useTimers;

		bool SimpleUI_Enable;
		int SimpleUI_FontSize;
		float SimpleUI_Top;
		float SimpleUI_Left;
		float SimpleUI_MaxWidth;
		float SimpleUI_MaxHeight;
		float SimpleUI_HideTimer;
						
		static Dictionary<string, object> defaultRebootTimes()
		{
			var dp = new Dictionary<string, object>();
			dp.Add("23:30","30");
			dp.Add("11:15","45");
			return dp;
		}
		
		static List<object> defaultCountDownMinutes()
		{
			var dp = new List<object>();
			dp.Add(60);
			dp.Add(45);
			dp.Add(30);
			dp.Add(15);
			dp.Add(10);
			dp.Add(5);
			dp.Add(4);
			dp.Add(3);
			dp.Add(2);
			dp.Add(1);			
			return dp;
		}
		
		static List<object> defaultCountDownSeconds()
		{
			var dp = new List<object>();
			dp.Add(50);
			dp.Add(40);
			dp.Add(30);
			dp.Add(20);
			dp.Add(10);
			dp.Add(5);
			dp.Add(4);
			dp.Add(3);
			dp.Add(2);
			dp.Add(1);
			return dp;
		}
		
		List<Regex> regexTags = new List<Regex>
        {
            new Regex(@"<color=.+?>", RegexOptions.Compiled),
            new Regex(@"<size=.+?>", RegexOptions.Compiled)
        };

        List<string> tags = new List<string>
        {
            "</color>",
            "</size>",
            "<i>",
            "</i>",
            "<b>",
            "</b>"
        };
		
		object GetConfig(string menu, string datavalue, object defaultValue)
		{
			var data = Config[menu] as Dictionary<string, object>;
			if (data == null)
			{
				data = new Dictionary<string, object>();
				Config[menu] = data;
				Changed = true;
			}
			object value;
			if (!data.TryGetValue(datavalue, out value))
			{
				value = defaultValue;
				data[datavalue] = value;
				Changed = true;
			}
			return value;
		}		
		
		void LoadVariables()
		{
			rebootTimes = (Dictionary<string, object>)GetConfig("Timers", "RebootTimes", defaultRebootTimes());
			countDownMinutes = (List<object>)GetConfig("Settings", "ShowMinutes", defaultCountDownMinutes());
			countDownSeconds = (List<object>)GetConfig("Settings", "ShowSeconds", defaultCountDownSeconds());
			useTimers = Convert.ToBoolean(GetConfig("Timers", "useTimers", false));
			
			SimpleUI_Enable = Convert.ToBoolean(GetConfig("SimpleUI", "SimpleUI_Enable", true));
			SimpleUI_FontSize = Convert.ToInt32(GetConfig("SimpleUI", "SimpleUI_FontSize", 30));
			SimpleUI_Top = Convert.ToSingle(GetConfig("SimpleUI", "SimpleUI_Top", 0.1));
			SimpleUI_Left = Convert.ToSingle(GetConfig("SimpleUI", "SimpleUI_Left", 0.1));
			SimpleUI_MaxWidth = Convert.ToSingle(GetConfig("SimpleUI", "SimpleUI_MaxWidth", 0.8));
			SimpleUI_MaxHeight = Convert.ToSingle(GetConfig("SimpleUI", "SimpleUI_MaxHeight", 0.05));
			SimpleUI_HideTimer = Convert.ToSingle(GetConfig("SimpleUI", "SimpleUI_HideTimer", 10));

			if (!Changed) return;
			SaveConfig();
			Changed = false;
		}
		
		void LoadDefaultMessages()
		{
			lang.RegisterMessages(new Dictionary<string, string>
			                      {
									{"RestartInit", "<color=orange>Server will restart in <color=red>{0}</color> minutes!</color>"},
									{"RestartInitSec", "<color=orange>Server will restart in <color=red>{0}</color> seconds!</color>"},
									{"RestartCancel", "<color=green>Server restart stopped!</color>"},
									{"ManualInit", "<color=silver>Manual countdown will start at next full minute</color>"},									
									{"ManualCancel", "<color=silver>Manual Timer was canceled</color>"},	
									},this);
		}

		protected override void LoadDefaultConfig()
		{
			Config.Clear();
			LoadVariables();
		}
		
        void OnServerInitialized()
        {
            if (!permission.PermissionExists("smoothrestart.canrestart"))
                permission.RegisterPermission("smoothrestart.canrestart", this);
        }

		void Init()
		{
			LoadVariables();
			LoadDefaultMessages();
			lastMinute = DateTime.Now.Minute;
			lastSecond = DateTime.Now.Second;
			currentCountDown = 0;
		}		
				
		void OnTick()
		{
			if ((!secondsActive && lastMinute == DateTime.Now.Minute)) return;
			if (secondsActive && lastSecond == DateTime.Now.Second) return;
			if (secondsActive)
			{
				lastSecond = DateTime.Now.Second;
				if (currentCountDown == 0)
					ConVar.Global.quit(new ConsoleSystem.Arg(null));
				if (countDownSeconds.Contains(currentCountDown))
					DoSmoothRestart(currentCountDown, false);
				currentCountDown--;
				return;
				// breaks here on times under a minute
			}
			lastMinute = DateTime.Now.Minute;
			if (countDownActive)
			{
				if (countDownMinutes.Contains(currentCountDown))
					DoSmoothRestart(currentCountDown);
				currentCountDown--;
				//switchers over to seconds if timer reached 0
				if (currentCountDown == 0)
				{
					secondsActive = true;
					lastSecond = DateTime.Now.Second;
					currentCountDown = 59;
					return;
				}
				return;
			}
			if ((useTimers && !countDownActive && rebootTimes.ContainsKey(DateTime.Now.ToString("HH:mm"))) || timerActivated)
			{
				if (!timerActivated)
					currentCountDown = Convert.ToInt32(rebootTimes[DateTime.Now.ToString("HH:mm")]);
				countDownActive = true;
				DoSmoothRestart(currentCountDown);
				currentCountDown--;
			}
		}				
		
        [ConsoleCommand("srestart")]
        private void smoothRestartConsoleCommand(ConsoleSystem.Arg arg)
        {
            if(arg.connection != null && arg.connection.authLevel < 2) return;
            if (arg.Args != null && arg.Args.Length == 1)
            {
                if (arg.Args[0].ToLower() == "stop")
				{
					if (countDownActive)
					{
						timerActivated = false;
						StopRestart();
						return;
					}
					if (timerActivated)
					{
						timerActivated = false;
						SendReply(arg, StripTags(lang.GetMessage("ManualCancel", this)));
					}
				}
                else
                {
                    int minutes;
                    if (int.TryParse(arg.Args[0], out minutes))
					{
						currentCountDown = Convert.ToInt32(minutes);
						//countDownActive = true;
						//DoSmoothRestart(currentCountDown);
						lastMinute = DateTime.Now.Minute;
						timerActivated = true;
						SendReply(arg, StripTags(lang.GetMessage("ManualInit", this)));
					}
                    else
                        SendReply(arg, "Incorrect <minutes> format! Must be number!");
                }
            }
            else
                SendReply(arg, "Incorrect syntax! Must use: srestart <minutes>/stop");
        }
		

        [ChatCommand("srestart")]
        private void smoothRestartCommand(BasePlayer player, string command, string[] args)
        {
            if (permission.UserHasPermission(player.UserIDString, "smoothrestart.canrestart") || player.net.connection.authLevel > 1)
            {
                if (args != null && args.Length == 1)
                {
                    if (args[0].ToLower() == "stop")
					{
                        if (countDownActive)
						{
							timerActivated = false;
							StopRestart();
							return;
						}
						if (timerActivated)
						{
							timerActivated = false;
							SendReply(player, lang.GetMessage("ManualCancel", this));
						}
					}
                    else
                    {
							int minutes;
							if (int.TryParse(args[0], out minutes))
							{
								currentCountDown = Convert.ToInt32(minutes);
								//countDownActive = true;
								//DoSmoothRestart(currentCountDown);
								lastMinute = DateTime.Now.Minute;
								timerActivated = true;
								SendReply(player, lang.GetMessage("ManualInit", this));
							}
                        else
							SendReply(player, "Incorrect <minutes> format! Must be number!");
                    }
                }
                else
                    SendReply(player, "Incorrect syntax! Must use: /srestart <minutes>/stop");
            }
        }

        private void StopRestart()
        {
            if (!countDownActive) return;
			countDownActive = false;
			currentCountDown = 0;
			string msg = string.Format(lang.GetMessage("RestartCancel", this));
			Puts(StripTags(msg));
			if(SimpleUI_Enable)
				foreach (BasePlayer player in BasePlayer.activePlayerList)
				{
					UIMessage(player, msg);
				}
			else
				rust.BroadcastChat(msg);
        }		

		
		void DoSmoothRestart(int timer, bool unitMinutes = true)
        {
			string msg = "";
			if (unitMinutes)
				msg = string.Format(lang.GetMessage("RestartInit", this), timer);
			else
				msg = string.Format(lang.GetMessage("RestartInitSec", this), timer);
			Puts(StripTags(msg));
			if(SimpleUI_Enable)
				foreach (BasePlayer player in BasePlayer.activePlayerList)
				{
					UIMessage(player, msg);
				}
			else
				rust.BroadcastChat(msg);
        }
		
		class UIColor
        {
            string color;

            public UIColor(double red, double green, double blue, double alpha)
            {
                color = $"{red} {green} {blue} {alpha}";
            }

            public override string ToString() => color;
        }
		
		class UIObject
        {
            List<object> ui = new List<object>();
            List<string> objectList = new List<string>();

            public UIObject()
            {
            }

            string RandomString()
            {
                const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
                List<char> charList = chars.ToList();

                string random = "";

                for (int i = 0; i <= UnityEngine.Random.Range(5, 10); i++)
                    random = random + charList[UnityEngine.Random.Range(0, charList.Count - 1)];

                return random;
            }

            public void Draw(BasePlayer player)
            {
                CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "AddUI", new Facepunch.ObjectList(JsonConvert.SerializeObject(ui).Replace("{NEWLINE}", Environment.NewLine)));
            }

            public void Destroy(BasePlayer player)
            {
                foreach (string uiName in objectList)
                    CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "DestroyUI", new Facepunch.ObjectList(uiName));
            }

            public string AddText(string name, double left, double top, double width, double height, UIColor color, string text, int textsize = 15, string parent = "Hud.Under", int alignmode = 0, float fadeIn = 0f, float fadeOut = 0f)
            {
                //name = name + RandomString();
                text = text.Replace("\n", "{NEWLINE}");
                string align = "";

                switch (alignmode)
                {
                    case 0: { align = "LowerCenter"; break; };
                    case 1: { align = "LowerLeft"; break; };
                    case 2: { align = "LowerRight"; break; };
                    case 3: { align = "MiddleCenter"; break; };
                    case 4: { align = "MiddleLeft"; break; };
                    case 5: { align = "MiddleRight"; break; };
                    case 6: { align = "UpperCenter"; break; };
                    case 7: { align = "UpperLeft"; break; };
                    case 8: { align = "UpperRight"; break; };
                }

                ui.Add(new Dictionary<string, object> {
                    {"name", name},
                    {"parent", parent},
                    {"fadeOut", fadeOut.ToString()},
                    {"components",
                        new List<object> {
                            new Dictionary<string, string> {
                                {"type", "UnityEngine.UI.Text"},
                                {"text", text},
                                {"fontSize", textsize.ToString()},
                                {"color", color.ToString()},
                                {"align", align},
                                {"fadeIn", fadeIn.ToString()}
                            },
                            new Dictionary<string, string> {
                                {"type", "RectTransform"},
                                {"anchormin", $"{left} {((1 - top) - height)}"},
                                {"anchormax", $"{(left + width)} {(1 - top)}"}
                            }
                        }
                    }
                });

                objectList.Add(name);
                return name;
            }
        }
		
		void UIMessage(BasePlayer player, string message)
        {
            bool replaced = false;
            float fadeIn = 0.2f;

            Timer playerTimer;

            timers.TryGetValue(player, out playerTimer);

            if (playerTimer != null && !playerTimer.Destroyed)
            {
                playerTimer.Destroy();
                fadeIn = 0.1f;

                replaced = true;
            }

            UIObject ui = new UIObject();

            ui.AddText("DeathNotice_DropShadow", SimpleUI_Left + 0.001, SimpleUI_Top + 0.001, SimpleUI_MaxWidth, SimpleUI_MaxHeight, deathNoticeShadowColor, StripTags(message), SimpleUI_FontSize, "Hud.Under", 3, fadeIn, 0.2f);
            ui.AddText("DeathNotice", SimpleUI_Left, SimpleUI_Top, SimpleUI_MaxWidth, SimpleUI_MaxHeight, deathNoticeColor, message, SimpleUI_FontSize, "Hud.Under", 3, fadeIn, 0.2f);

            ui.Destroy(player);

            if(replaced)
            {
                timer.Once(0.1f, () =>
                {
                    ui.Draw(player);

                    timers[player] = timer.Once(SimpleUI_HideTimer, () => ui.Destroy(player));
                });
            }
            else
            {
                ui.Draw(player);

                timers[player] = timer.Once(SimpleUI_HideTimer, () => ui.Destroy(player));
            }
        }
		
		string StripTags(string original)
        {
            foreach (string tag in tags)
                original = original.Replace(tag, "");

            foreach (Regex regexTag in regexTags)
                original = regexTag.Replace(original, "");

            return original;
        }
	
    }
}