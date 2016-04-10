using System.Collections.Generic;
using System.Reflection;
using System;
using System.Linq;
using System.Data;
using UnityEngine;
using Oxide.Core; 

namespace Oxide.Plugins 
{
    [Info("Timed Execute", "PaiN", 2.6, ResourceId = 919)]
    [Description("Execute commands every (x) seconds.")]
    class TimedExecute : RustPlugin
    {
		private Timer repeater;  
		private Timer chaintimer; 
		private Timer checkreal;
		
		void Loaded()    
		{  
			checkreal = timer.Repeat(1, 0, () => RealTime());
			RunRepeater();
			RunOnce(); 
			if(Convert.ToBoolean(Config["EnableTimerOnce"]) == true)
			{
				Puts("Timer-Once is ON");
			}
			else
			{
				Puts("Timer-Once is OFF");
			}
			if(Convert.ToBoolean(Config["EnableTimerRepeat"]) == true)
			{
				Puts("Timer-Repeat is ON");
			}
			else
			{
				Puts("Timer-Repeat is OFF");
			}
			if(Convert.ToBoolean(Config["EnabledRealTime-Timer"]) == true)
			{
				Puts("RealTime-Timer is ON");
			}
			else
			{
				Puts("RealTime-Timer is OFF");
			}
		}
		
		void RunRepeater()
		{
			if(repeater != null) 
			{
				repeater.Destroy(); 
			}
			if(Convert.ToBoolean(Config["EnableTimerRepeat"]) == true)
			{
				foreach(var cmd in Config["TimerRepeat"] as Dictionary<string, object>)
				{ 
					repeater = timer.Repeat(Convert.ToSingle(cmd.Value), 0, () =>{ 
					ConsoleSystem.Run.Server.Normal(cmd.Key);
					Puts($"ran the command || " + cmd.Key.ToString());
					});
				}
			}
		}   
		void RealTime()
		{
			if(Convert.ToBoolean(Config["EnabledRealTime-Timer"]) == true)
			{
				foreach(var cmd in Config["RealTime-Timer"] as Dictionary<string, object>)
				{
					if(System.DateTime.Now.ToString("HH:mm:ss") == cmd.Key.ToString())
					{ 
						ConsoleSystem.Run.Server.Normal(cmd.Value.ToString());
						Puts("ran the command || " + cmd.Value.ToString() + " at: " + cmd.Key);
					}
				}
			}
		}
		void RunOnce()
		{
			if(chaintimer != null)  
			{   
				chaintimer.Destroy(); 
			}   
			if(Convert.ToBoolean(Config["EnableTimerOnce"]) == true)
			{
				foreach(var cmdc in Config["TimerOnce"] as Dictionary<string, object>)
				{ 
					chaintimer = timer.Once(Convert.ToSingle(cmdc.Value), () =>{
					ConsoleSystem.Run.Server.Normal(cmdc.Key);
					Puts($"ran the command || " + cmdc.Key.ToString());
					});
				} 
			}
		}
		 
		void Unloaded()  
		{
			if(repeater != null) 
			{
				repeater.Destroy(); 
				Puts("Destroyed the *Repeater* timer!");
			}
			if(chaintimer != null)  
			{   
				chaintimer.Destroy();
				Puts("Destroyed the *Timer-Once* timer!");
			}  
			if(checkreal != null)  
			{   
				checkreal.Destroy();
				Puts("Destroyed the *RealTime* timer!");
			} 
		}  

 			Dictionary<string, object> repeatcmds = new Dictionary<string, object>();
			Dictionary<string, object> chaincmds = new Dictionary<string, object>();
			Dictionary<string, object> realtimecmds = new Dictionary<string, object>();
			 
        protected override void LoadDefaultConfig() 
        {  
			repeatcmds.Add("server.save", 300);
			repeatcmds.Add("event.run", 300);
            Puts("Creating a new configuration file!");
			if(Config["TimerRepeat"] == null) Config["TimerRepeat"] = repeatcmds;

			 
			chaincmds.Add("say 'Dont forget to like our fanpage!'", 60);
			chaincmds.Add("say 'Follow us on Twitter!'", 120);
			chaincmds.Add("say 'You can donate via PayPal!'", 180);
			chaincmds.Add("reset.oncetimer", 181);
			if(Config["TimerOnce"] == null) Config["TimerOnce"] = chaincmds;
			if(Config["EnableTimerRepeat"] == null) Config["EnableTimerRepeat"] = true;
			if(Config["EnableTimerOnce"] == null) Config["EnableTimerOnce"] = true;
			if(Config["EnabledRealTime-Timer"] == null) Config["EnabledRealTime-Timer"] = true;
			///
			realtimecmds.Add("16:00:00", "say 'The gate for the event is open!'");
			realtimecmds.Add("16:30:00", "say 'The gate for the event just closed'");
			realtimecmds.Add("17:00:00", "say 'Restart in 1 HOUR'");
			realtimecmds.Add("18:00:00", "say 'The server is restarting NOW.'");
			if(Config["RealTime-Timer"] == null) Config["RealTime-Timer"] = realtimecmds;

        }
		[ConsoleCommand("reset.oncetimer")]
		void cmdResOnceTimer(ConsoleSystem.Arg arg)
		{
			RunOnce();
		}
	}
}