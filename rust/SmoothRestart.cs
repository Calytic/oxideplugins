using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Linq;
using System.Text;
using Oxide.Core.Configuration;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("SmoothRestart", "Fujikura/Visagalis", "1.0.1", ResourceId = 1826)]
    public class SmoothRestart : RustPlugin
    {
        bool Changed;
		JsonSerializerSettings jsonsettings;
		DateTime restartTime = DateTime.MinValue;
		Dictionary<BasePlayer, Timer> timers = new Dictionary<BasePlayer, Timer>();
		Dictionary<string, string> userAgent;
        Timer _blogTimer = null;
		Timer _oxideTimer = null;
		int serverOxideVersion;
		
		bool initCheckDevblog;
		bool initCheckOxideBuild;
		bool simulationActive;
		bool patchRestartRunning;
		
		int lastMinute;
		int lastSecond;
		int currentCountDown;
		bool countDownActive;
		bool secondsActive;
		bool timerActivated;
		bool newDevblogDetected;
		bool newOxideBuildDetected;
		
		UIColor deathNoticeShadowColor = new UIColor(0.1, 0.1, 0.1, 0.8);
        UIColor deathNoticeColor = new UIColor(0.85, 0.85, 0.85, 0.1);
		
		Dictionary<string, object> rebootTimes = new Dictionary<string, object>();
		List<object> countDownMinutes = new List<object>();
		List<object> countDownSeconds = new List<object>();
		bool useTimers;
		
		int currentDevblog;
		int currentOxideBuild;
		int oxideCheckHoursBack;
		int checkIntervalMinutes;
		bool enableAutoChecks;
		bool enableAutoReboot;
		int autoRebootCountDown;
		bool notifyOnlineAdmins;

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
			
			currentDevblog = Convert.ToInt32(GetConfig("Checks", "currentDevblog", 0));
			currentOxideBuild = Convert.ToInt32(GetConfig("Checks", "currentOxideBuild", 0));
			oxideCheckHoursBack = Convert.ToInt32(GetConfig("Checks", "oxideCheckHoursBack", 2));
			checkIntervalMinutes = Convert.ToInt32(GetConfig("Checks", "checkIntervalMinutes", 5));
			enableAutoChecks = Convert.ToBoolean(GetConfig("Checks", "enableAutoChecks", true));
			enableAutoReboot = Convert.ToBoolean(GetConfig("Checks", "enableAutoReboot", false));
			autoRebootCountDown = Convert.ToInt32(GetConfig("Checks", "autoRebootCountDown", 3));
			notifyOnlineAdmins = Convert.ToBoolean(GetConfig("Checks", "notifyOnlineAdmins", true));
			
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
									{"RestartInitPatch", "<color=red>Server Patch</color> <color=orange>restart in <color=red>{0}</color> minutes!</color>"},
									{"RestartInitSec", "<color=orange>Server will restart in <color=red>{0}</color> seconds!</color>"},
									{"RestartCancel", "<color=green>Server restart stopped!</color>"},
									
									{"ManualInit", "<color=silver>Manual countdown will start at next full minute</color>"},									
									{"ManualCancel", "<color=silver>Manual Timer was canceled</color>"},
									{"DevblogDetected", "<color=orange>New DevBlog <color=green>{0}</color> detected !!!</color>"},
									{"OxideBuildDetected", "<color=orange>New OxideBuild <color=green>{0}</color> detected !!!</color>"},									
									},this);
		}

		protected override void LoadDefaultConfig()
		{
			Config.Clear();
			LoadVariables();
		}

		void Init()
		{
			LoadVariables();
			LoadDefaultMessages();
			lastMinute = DateTime.Now.Minute;
			lastSecond = DateTime.Now.Second;
			currentCountDown = 0;
			jsonsettings = new JsonSerializerSettings();
            jsonsettings.Converters.Add(new KeyValuesConverter()); 
			newDevblogDetected = false;
			newOxideBuildDetected = false;
			userAgent = new Dictionary<string, string>();
			userAgent.Add("User-Agent", "OxideMod");
			serverOxideVersion = Convert.ToInt32(Oxide.Core.OxideMod.Version.Patch);
			initCheckDevblog = true;
			initCheckOxideBuild = true;
			simulationActive = false;
			patchRestartRunning = false;
		}	

        void OnServerInitialized()
        {
            if (!permission.PermissionExists("smoothrestart.canrestart"))
                permission.RegisterPermission("smoothrestart.canrestart", this);
				CheckDevBlog(3);
				CheckOxideCommits(168);
			if (enableAutoChecks)
				_blogTimer = timer.Every(checkIntervalMinutes*60, () => CheckDevBlog());
        }	
		
				
		void OnTick()
		{
			if ((!secondsActive && lastMinute == DateTime.Now.Minute)) return;
			if (secondsActive && lastSecond == DateTime.Now.Second) return;
			if (secondsActive)
			{
				lastSecond = DateTime.Now.Second;
				if (currentCountDown == 0)
				{
					if (!simulationActive)
					{
						ConVar.Global.quit(new ConsoleSystem.Arg(null));
						return;
					}
					else
					{
						secondsActive = false;
						timerActivated = false;
						simulationActive = false;
						oxideCheckHoursBack = (int)Config["Checks", "oxideCheckHoursBack"];
						enableAutoReboot = (bool)Config["Checks", "enableAutoReboot"];
						Puts("Patch simulation finished");
						return;
					}
				}
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
		
		void CheckDevBlog(int countNews = 3)
        {
			var url = $"http://api.steampowered.com/ISteamNews/GetNewsForApp/v0002/?appid=252490&count={countNews}&maxlength=1&format=json";
			try { webrequest.EnqueueGet(url, (code, response) => APIResponse(code, response, "devblog", countNews), this); }
			catch { if (countNews != 3) timer.Once(60f, () => CheckDevBlog(6)); }
		}

		void CheckOxideCommits(int hoursBack = 0)
        {
			if (hoursBack == 0)
				hoursBack = oxideCheckHoursBack;
			var sinceDate = DateTime.Now.AddHours(-hoursBack).ToString("yyyy-MM-ddTHH:mm:ssZ");
			var url = $"https://api.github.com/repos/OxideMod/Snapshots/commits?since={sinceDate}";
			Dictionary<string, string> userAgent  = new Dictionary<string, string>();
			userAgent.Add("User-Agent", "OxideMod");
			try { webrequest.EnqueueGet(url, (code, response) => APIResponse(code, response, "oxide", hoursBack), this, userAgent); }
			catch { if (hoursBack == 168) timer.Once(60f, () => CheckOxideCommits(168));}
		}		

		void APIResponse(int code, string response, string apiType, int numberCheck)
        {
			if (!(response == null || code != 200) && apiType == "devblog")
			{
				var jsonresponse1 = JsonConvert.DeserializeObject<Dictionary<string, object>>(response, jsonsettings);
				if (!(jsonresponse1["appnews"] is Dictionary<string, object>)) return;
				var items = ((jsonresponse1["appnews"] as Dictionary<string, object>)["newsitems"] as List<object>).ToArray();
				foreach (Dictionary<string, object> info in items.Where(p => (p as Dictionary<string, object>)["title"].ToString().Contains("Devblog")))
				{
					if (info["title"].ToString().Contains("Devblog"))
					{
						if (currentDevblog == 0 || initCheckDevblog)
						{
							var resultBlog = Convert.ToInt32((info["title"].ToString().Replace("Devblog ", "")));
							if (resultBlog != currentDevblog)
							{
								currentDevblog = resultBlog;
								Config["Checks", "currentDevblog"] = currentDevblog;
								Config.Save();
							}
							if (initCheckDevblog) initCheckDevblog = false;
							break;
						}
						var checkedDevblog = Convert.ToInt32((info["title"].ToString().Replace("Devblog ", "")));
						bool blogChanged = false;
						if (currentDevblog != checkedDevblog)
						{
							blogChanged = true;
							currentDevblog = checkedDevblog;
							Config["Checks", "currentDevblog"] = currentDevblog;
							Config.Save();
						}
						if (blogChanged)
						{
							Puts(StripTags(string.Format(lang.GetMessage("DevblogDetected", this), currentDevblog)));
							if (notifyOnlineAdmins)
								foreach (var player in BasePlayer.activePlayerList.Where(p => p.IsAdmin()).ToList())
									SendReply(player, string.Format(lang.GetMessage("DevblogDetected", this), currentDevblog));
							newDevblogDetected = true;
							if ( _blogTimer == null || _blogTimer.Destroyed)
								return;
							_blogTimer.Destroy();
							_blogTimer = null;
							_oxideTimer = timer.Every(60f, () => CheckOxideCommits());
						}
						break;
					}
				}
			}
			else if (!(response == null || code != 200) && apiType == "oxide")
			{
				var jsonresponse2 = JsonConvert.DeserializeObject<List<object>>(response, jsonsettings);
				if (!(jsonresponse2 is List<object>) || jsonresponse2.Count == 0) return;
				Dictionary <string,object> commitCheck = jsonresponse2.First() as Dictionary <string,object>;
				string message = (commitCheck["commit"] as Dictionary <string,object>)["message"].ToString().Replace("Oxide build ", "");  
				if (!message.Contains("Oxide build") &&  jsonresponse2.Count > 1)
				{
					foreach (Dictionary <string,object> entry in jsonresponse2)
					{
						if ((entry["commit"] as Dictionary <string,object>)["message"].ToString().Contains("Oxide build"))
						{
							message = (entry["commit"] as Dictionary <string,object>)["message"].ToString().Replace("Oxide build ", "");
							break;							
						}
					}
				}
				else
					message = $"Oxide build {Oxide.Core.OxideMod.Version.Patch} from";
				var buildNum = Convert.ToInt32(message.Substring(0, message.LastIndexOf(" from") + 1));
				if (currentOxideBuild == 0 || initCheckOxideBuild)
				{
					if ( buildNum != currentOxideBuild)
					{
						currentOxideBuild = buildNum;
						Config["Checks", "currentOxideBuild"] = currentOxideBuild;
						Config.Save();
					}
					if (initCheckOxideBuild) initCheckOxideBuild = false;
					return;
				}
				bool buildChanged = false;
				if (currentOxideBuild != buildNum)
				{
					buildChanged = true;
					currentOxideBuild = buildNum;
					Config["Checks", "currentOxideBuild"] = currentOxideBuild;
					Config.Save();
				}
				if (buildChanged)
				{
					Puts(StripTags(string.Format(lang.GetMessage("OxideBuildDetected", this), currentOxideBuild)));
					if (notifyOnlineAdmins)
						foreach (var player in BasePlayer.activePlayerList.Where(p => p.IsAdmin()).ToList())
							SendReply(player, string.Format(lang.GetMessage("OxideBuildDetected", this), currentOxideBuild));
					newOxideBuildDetected = true;
					if ( _oxideTimer == null || _oxideTimer.Destroyed)
						return;
					_oxideTimer.Destroy();
					_oxideTimer = null;
					CheckAutoRestart();
				}
			}
		}
				
		void CheckAutoRestart()
		{
			if (!enableAutoReboot) return;
			if (!newDevblogDetected || !newOxideBuildDetected) return;
			patchRestartRunning = true;
			timerActivated = true;
			currentCountDown = autoRebootCountDown;
		}		
		        
		[ConsoleCommand("simulatepatch")]
        void checkOxideCommits(ConsoleSystem.Arg arg)
        {
            if(arg.connection != null && arg.connection.authLevel < 2) return;
			currentDevblog--;
			currentOxideBuild--;
			SendReply(arg, "Changed current Devblog and OxideBuild numbers to simulate successful checks");
			simulationActive = true;
			enableAutoReboot = true;
			oxideCheckHoursBack = 168;
		}

        [ConsoleCommand("srestart")]
        void smoothRestartConsoleCommand(ConsoleSystem.Arg arg)
        {
            if(arg.connection != null && arg.connection.authLevel < 2) return;
            if (arg.Args != null && arg.Args.Length == 1)
            {
                if (arg.Args[0].ToLower() == "stop")
				{
					if (countDownActive)
					{
						timerActivated = false;
						secondsActive = false;
						patchRestartRunning = false;
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
        void smoothRestartCommand(BasePlayer player, string command, string[] args)
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
							secondsActive = false;
							patchRestartRunning = false;
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

        void StopRestart()
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
			{
				if (patchRestartRunning)
					msg = string.Format(lang.GetMessage("RestartInitPatch", this), timer);
				else
					msg = string.Format(lang.GetMessage("RestartInit", this), timer);
			}
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