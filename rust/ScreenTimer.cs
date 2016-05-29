using Oxide.Core;
using System;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using ConVar;
using Oxide.Game.Rust.Cui;

namespace Oxide.Plugins
{
    [Info("ScreenTimer", "DylanSMR", "1.0.1", ResourceId = 1918)]
    [Description("A GUI timer.")]
    class ScreenTimer : RustPlugin
    {  
        
        #region Config and Variables
        private string _permission = "screentimer.use";
        
        private int currenttimer;
        private int seconds;
        private int minutes;
        private int hours;
        
        private string format;
        public string timername;
        
        private bool active;
        
        private Timer screent = null;
        private List<ulong> isin = new List<ulong>();
        
        void LoadDefaultConfig()
        {
            Config.Clear();
            Config["Permission"] = _permission;
            Config.Save();
        }   
        
        T GetConfig<T>(string name, T defaultValue)
        {
            if (Config[name] == null)
            {
                return defaultValue;
            }

            return (T)Convert.ChangeType(Config[name], typeof(T));
        }    
        #endregion
           
        #region Language
        void Loaded()
        {
            lang.RegisterMessages(messages, this);
        } 
        
        Dictionary<string, string> messages = new Dictionary<string, string>()
        {
            {"IncorrectCreate", "Correct Syntax: createtimer (seconds) (timer-name)."},   
            {"CurrentTime", "The current time of the timer is: {0}"},
            {"NoTimer", "There is no current timer!"},
            {"NoPermission", "You have no permission to preform that command."},
            {"TimerEnded", "You have ended the current timer!"},
            {"TimerStarted", "You have started a new timer with the stats of {0}."},
        };
        #endregion
        
        #region Timer
        
        void CreateTimer(int seconds)
        {
            active = true;
            currenttimer = seconds;
            seconds = currenttimer;
                    if(seconds >= 60)
                    {
                        minutes = seconds / 60;
                        seconds = seconds - minutes * 60; 
                        if(minutes >= 60)
                        {
                            hours = minutes / 60;
                            minutes = minutes - hours * 60;
                        }
                    }
            screent = timer.Repeat(1f, currenttimer, () => 
            {
                format = hours.ToString()+"h:"+minutes.ToString()+"m:"+seconds.ToString()+"s"; 
                if(hours <= 9)
                {
                    if(minutes <= 9)
                    {
                        format = "0"+hours.ToString()+"h:0"+minutes.ToString()+"m:"+seconds.ToString()+"s";    
                    }
                    if(seconds <= 9)
                    {
                        format = "0"+hours.ToString()+"h:0"+minutes.ToString()+"m:0"+seconds.ToString()+"s";      
                    }  
                    
                }
                if(seconds == 0 && minutes >= 1)
                {
                    minutes = minutes - 1;
                    seconds = seconds + 60;
                } 
                if(seconds == 0 && minutes == 0 && hours >= 1)
                {
                    hours = hours - 1;
                    minutes = minutes + 60 - 1;
                    seconds = seconds + 60;
                }
                if(seconds == 1 && minutes == 0 && hours == 0)
                {
                    seconds = 0;
                    minutes = 0;
                    hours = 0;
                    RefreshUI();
                    timer.Once(1f, () => DestroyUI());
                    format = hours.ToString()+"h:"+minutes.ToString()+"m:"+seconds.ToString()+"s";
                    screent.Destroy();  
                    active = false; 
                    return;
                }
                if(minutes <= 9)
                {
                    if(hours <= 9) format = "0"+hours.ToString()+"h:0"+minutes.ToString()+"m:"+seconds.ToString()+"s";
                    else format = hours.ToString()+"h:0"+minutes.ToString()+"m:"+seconds.ToString()+"s";
                }
                if(hours <= 9)
                {
                    if(hours <= 9) format = "0"+hours.ToString()+"h:"+minutes.ToString()+"m:"+seconds.ToString()+"s"; 
                    if(minutes <= 9 && hours <= 9) format = "0"+hours.ToString()+"h:0"+minutes.ToString()+"m:"+seconds.ToString()+"s";    
                    if(minutes <= 9 && hours <= 9 && seconds <= 9) format = "0"+hours.ToString()+"h:"+minutes.ToString()+"m:0"+seconds.ToString()+"s"; 
                    
                    if(minutes >= 10 && hours <= 9 && seconds <= 9) format = hours.ToString()+"h:"+minutes.ToString()+"m:"+seconds.ToString()+"s";
                    if(minutes >= 10 && hours >= 10 && seconds >= 10) format = hours.ToString()+"h:"+minutes.ToString()+"m:0"+seconds.ToString()+"s"; 
                    if(minutes >= 10 && hours >= 10 && seconds >= 10) format = hours.ToString()+"h:"+minutes.ToString()+"m:"+seconds.ToString()+"s"; 
                    
                    if(minutes >= 10 && hours <= 9 && seconds <= 9) format = "0"+hours.ToString()+"h:"+minutes.ToString()+"m:0"+seconds.ToString()+"s"; 
                    if(minutes <= 9 && hours <= 9 && seconds <= 9) format = "0"+hours.ToString()+"h:0"+minutes.ToString()+"m:0"+seconds.ToString()+"s"; 
                }
                    seconds--; 
                    RefreshUI();
            });
        }
           
        #endregion 
        
        #region ConsoleComm
            [ConsoleCommand("createtimer")]
            void CreateC(ConsoleSystem.Arg arg)
            {
                if (arg.Player() != null && !arg.Player().IsAdmin())
                {
                    SendReply(arg, lang.GetMessage("NoPermission", this));
                    return;
                } 
                if(active) return;
                timername = arg.Args[1];
                CreateTimer(Convert.ToInt32(arg.Args[0]));
                timer.Once(1.0f, () => SendReply(arg, string.Format(lang.GetMessage("TimerStarted", this), format)));
                OpenUIAll();
            } 
            [ConsoleCommand("destroytimer")]
            void DestroyC(ConsoleSystem.Arg arg)
            {
                if (arg.Player() != null && !arg.Player().IsAdmin())
                {
                    SendReply(arg, lang.GetMessage("NoPermission", this));
                    return;
                }
                if(active) screent.Destroy();
                else SendReply(arg, lang.GetMessage("NoTimer", this));
                active = false;
                    seconds = 0;
                    minutes = 0;
                    hours = 0;
                    format = hours.ToString()+"h:"+minutes.ToString()+"m:"+seconds.ToString()+"s";
                SendReply(arg, lang.GetMessage("TimerEnded", this));
                DestroyUI();
            } 
            [ConsoleCommand("currenttime")]
            void CurrentTime(ConsoleSystem.Arg arg)
            {
                if (arg.Player() != null && !arg.Player().IsAdmin())
                {
                    SendReply(arg, lang.GetMessage("NoPermission", this));
                    return;
                }
                if(active)SendReply(arg, string.Format(lang.GetMessage("CurrentTime", this), format));
                else SendReply(arg, lang.GetMessage("NoTimer", this));
            }
        #endregion            
           
        #region ChatComm    
            [ChatCommand("createtimer")]
            void CreateCC(BasePlayer player, string command, string[] args)
            {
                if(!hasPermission(player))
                {
                    SendReply(player, lang.GetMessage("NoPermission", this, player.UserIDString));
                    return;    
                }   
                if(active) return;
                timername = args[1];
                CreateTimer(Convert.ToInt32(args[0]));
                timer.Once(1.0f, () => SendReply(player, string.Format(lang.GetMessage("TimerStarted", this, player.UserIDString), format)));
                OpenUIAll();
            }     
            [ChatCommand("destroytimer")]
            void DestroyCC(BasePlayer player)
            {
                if(!hasPermission(player))
                {
                    SendReply(player, lang.GetMessage("NoPermission", this, player.UserIDString));
                    return;    
                }
                if(active) screent.Destroy();
                else SendReply(player, lang.GetMessage("NoTimer", this, player.UserIDString));
                active = false;
                SendReply(player, lang.GetMessage("TimerEnded", this, player.UserIDString));
                DestroyUI();
            }

            [ChatCommand("currenttime")]
            void CurrentTimeC(BasePlayer player)
            {
                if(!hasPermission(player))
                {
                    SendReply(player, lang.GetMessage("NoPermission", this, player.UserIDString));
                    return;    
                }
                if(active)SendReply(player, string.Format(lang.GetMessage("CurrentTime", this, player.UserIDString), format));
                else SendReply(player, lang.GetMessage("NoTimer", this, player.UserIDString));
            }    
        #endregion
        
        #region Plugin Related
            public bool hasPermission(BasePlayer player)
            {
                if (permission.UserHasPermission(player.UserIDString, Config["Permission"].ToString()) || player.net.connection.authLevel >= 1) return true;
                else return false;
            }        
        #endregion
        
        #region GUI
                void OnPlayerInit(BasePlayer player)
                {
                    if(isin.Contains(player.userID) || active == false) return;
                    else OpenUII(player);
                }
                void OnPlayerDisconnect(BasePlayer player)
                {
                    if(isin.Contains(player.userID)) DestroyUII(player);
                    else return;
                }
                void Unload()
                {
                    DestroyUI();
                }
                void RefreshUI()
                {
                    DestroyUI();
                    OpenUIAll();
                }   
                void OpenUIAll()
                {
                    foreach(var player in BasePlayer.activePlayerList)
                    {
                        OpenUII(player);   
                    }
                }    
                void OpenUII(BasePlayer player)
                {
                    isin.Add(player.userID);  
                    GUICreate(player);  
                }
                void DestroyUI()
                {
                    foreach(var player in BasePlayer.activePlayerList)
                    {
                        DestroyUII(player);    
                    }
                } 
                void DestroyUII(BasePlayer player)
                {
                    isin.Remove(player.userID);    
                    CuiHelper.DestroyUi(player, "GUIBackground");
                }                  
           void GUICreate(BasePlayer player)
            {
                var GUIElement = new CuiElementContainer();
                var GUIBackground = GUIElement.Add(new CuiPanel
                {
                    Image =
                    {
                        Color = "0.0 0.0 0.0 0.90"
                    },
                    RectTransform =
                    {
                        AnchorMin = "0.005 0.04",
                        AnchorMax = "0.110 0.09"
                    },
                    CursorEnabled = false
                }, "HUD/Overlay", "GUIBackground");
                GUIElement.Add(new CuiLabel
                {
                    Text =
                    {
                        Text = ""+timername,
                        FontSize = int.Parse("12"),
                        Align = TextAnchor.UpperCenter,
                        Color = "0 255 0"
                    },
                    RectTransform =
                    {
                    AnchorMin = "0.00 0.1",
                    AnchorMax = "1 0.9"
                    }
                }, GUIBackground);
                GUIElement.Add(new CuiLabel
                {
                    Text =
                    {
                        Text = ""+format,
                        FontSize = int.Parse("12"),
                        Align = TextAnchor.LowerCenter,
                        Color = "0 255 0"
                    },
                    RectTransform =
                    {
                    AnchorMin = "0.00 0.1",
                    AnchorMax = "1 0.5"
                    }
                }, GUIBackground);            
 
                CuiHelper.AddUi(player, GUIElement);
            }
        #endregion
    }
}