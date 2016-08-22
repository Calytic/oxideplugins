// Reference: Newtonsoft.Json
// Reference: Rust.Data
using UnityEngine;
using Oxide.Game.Rust.Cui;
using System.Collections.Generic;
using System;

namespace Oxide.Plugins
{
    [Info("ScreenTimer", "DylanSMR", "1.0.6", ResourceId = 1918)]
    [Description("A GUI timer.")]
    class ScreenTimer : RustPlugin
    {  
        
        #region Fields

        private string _permission = "screentimer.admin";

        private int currenttimer;
        private int seconds;
        private int minutes;
        private int hours;
        
        private string format;
        private string reason = "";

        private bool active;
        private bool newactive;
        
        private Timer screent = null;
        private List<ulong> isin = new List<ulong>();
        
        #endregion
           
        #region Oxide

        void LoadDefaultConfig()
        {
            Config.Clear();
            Config["Permission"] = _permission;
            Config.Save();
        }

        void OnPlayerInit(BasePlayer player)
        {
            if(isin.Contains(player.userID))
            isin.Remove(player.userID);
            StartTime(player);
            isin.Add(player.userID);
        }

        void Unload()
        {
            DestroyUI();
        }

        void Loaded()
        {
            if (!permission.PermissionExists(Config["Permission"].ToString())) permission.RegisterPermission(Config["Permission"].ToString(), this);
            lang.RegisterMessages(messages, this);
        } 
        
        #endregion

        #region Language

        Dictionary<string, string> messages = new Dictionary<string, string>()
        {
            {"IncorrectCreate", "Syntax: createtimer <time> <reason> "},   
            {"CurrentTime", "The current time of the timer is: {0}"},
            {"NoTimer", "There is no current timer!"},
            {"NoPermission", "You have no permission to preform that command."},
            {"TimerEnded", "You have ended the current timer!"},
            {"TimerStarted", "You have started a new timer with the stats of 0."},
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
                    reason = "";
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
                try{
                    if (arg.Player() != null && !arg.Player().IsAdmin())
                    {
                        SendReply(arg, lang.GetMessage("NoPermission", this));
                        return;
                    } 
                    if(active) return;
                    CreateTimer(Convert.ToInt32(arg.Args[0]));
                    reason = arg.Args[1].ToString();
                    timer.Once(1.5f, () => SendReply(arg, lang.GetMessage("TimerStarted", this).Replace("0", format)));
                    OpenUIAll();
                }catch(System.Exception){ return; }
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
                try {
                    if(!hasPermission(player))
                    {
                        SendReply(player, lang.GetMessage("NoPermission", this, player.UserIDString));
                        return;    
                    }   
                    if(active) return;
                    CreateTimer(Convert.ToInt32(args[0]));
                    reason = args[1].ToString();
                    timer.Once(1.5f, () => SendReply(player, lang.GetMessage("TimerStarted", this).Replace("0", format)));

                    OpenUIAll();
                }
                catch(System.Exception){ return; }
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
                if (player.net.connection.authLevel >= 1 || permission.UserHasPermission(player.userID.ToString(), Config["Permission"].ToString())) return true;
                else return false;
            }        

        #endregion
        
        #region GUI

            static string MainTimer = "Maintimer"; 
            
            public class UI
            {
                static public CuiElementContainer CreateElementContainer(string panelName, string color, string aMin, string aMax, bool cursor = false)
                {
                    var NewElement = new CuiElementContainer()
                {
                    {
                        new CuiPanel
                        {
                            Image = {Color = color},
                            RectTransform = {AnchorMin = aMin, AnchorMax = aMax},
                            CursorEnabled = cursor
                        },
                        new CuiElement().Parent,
                        panelName
                    }
                };
                    return NewElement;
                }
                static public void CreatePanel(ref CuiElementContainer container, string panel, string color, string aMin, string aMax, bool cursor = false)
                {
                    container.Add(new CuiPanel
                    {
                        Image = { Color = color },
                        RectTransform = { AnchorMin = aMin, AnchorMax = aMax },
                        CursorEnabled = cursor
                    },
                    panel);
                }
                static public void CreateLabel(ref CuiElementContainer container, string panel, string color, string text, int size, string aMin, string aMax, TextAnchor align = TextAnchor.MiddleCenter)
                {
                    container.Add(new CuiLabel
                    {
                        Text = { Color = color, FontSize = size, Align = align, FadeIn = 0.0f, Text = text },
                        RectTransform = { AnchorMin = aMin, AnchorMax = aMax }
                    },
                    panel);
                }
                static public void CreateButton(ref CuiElementContainer container, string panel, string color, string text, int size, string aMin, string aMax, string command, TextAnchor align = TextAnchor.MiddleCenter)
                {
                    container.Add(new CuiButton
                    {
                        Button = { Color = color, Command = command, FadeIn = 0.0f },
                        RectTransform = { AnchorMin = aMin, AnchorMax = aMax },
                        Text = { Text = text, FontSize = size, Align = align }
                    },
                    panel);
                }
            }

            private Dictionary<string, string> UIColors = new Dictionary<string, string>()
            {
                {"dark", "0.1 0.1 0.1 0.98" },
                {"light", ".85 .85 .85 1.0" },
            };


            void DestroyUI()
            {
                foreach(BasePlayer player in BasePlayer.activePlayerList){
                CuiHelper.DestroyUi(player, MainTimer);
                isin.Remove(player.userID);
                newactive = true;}
            }

            void OpenUIAll()
            {
                foreach(BasePlayer player in BasePlayer.activePlayerList){
                StartTime(player);   
                isin.Add(player.userID);}
            }
            
            void RefreshUI()
            {
                foreach(BasePlayer player in BasePlayer.activePlayerList){
                DestroyUI();
                OpenUIAll();}
            }

            void StartTime(BasePlayer player)
            {
                CuiHelper.DestroyUi(player, MainTimer);
                var element = UI.CreateElementContainer(MainTimer, UIColors["dark"], "0.17 0.024", "0.34 0.107", false);
                UI.CreatePanel(ref element, MainTimer, UIColors["light"],  "0.01 0.04", "0.984 0.94", false);  
                if(!newactive){
                timer.Once(1.5f, () =>
                {
                    UI.CreateLabel(ref element, MainTimer, UIColors["dark"], format.ToString(), 20, "0 1", "1 0.4");
                    UI.CreateLabel(ref element, MainTimer, UIColors["dark"], reason.ToString(), 18, " 0 1", "1 1");
                    CuiHelper.AddUi(player, element);    
                    newactive = true;
                });}
                else
                {
                    UI.CreateLabel(ref element, MainTimer, UIColors["dark"], format.ToString(), 20, " 0 1", "1 0.4");
                    UI.CreateLabel(ref element, MainTimer, UIColors["dark"], reason.ToString(), 18, " 0 1", "1 1");
                    CuiHelper.AddUi(player, element);     
                }
            }

        #endregion
    }
}