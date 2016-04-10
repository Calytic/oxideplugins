// Reference: UnityEngine.UI

using System;
using System.Collections.Generic;
using uLink;

namespace Oxide.Plugins 
{
    [Info("Timed Execute", "PaiN", 0.1, ResourceId = 919)]
    [Description("Execute commands every (x) seconds.")]
    class TimedExecute : CovalencePlugin
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
					ConsoleManager.Instance?.ExecuteCommand(cmd.Key);
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
						ConsoleManager.Instance?.ExecuteCommand(cmd.Value.ToString());
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
					ConsoleManager.Instance?.ExecuteCommand(cmdc.Key);
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
			repeatcmds.Add("saveserver", 300);
            Puts("Creating a new configuration file!");
			if(Config["TimerRepeat"] == null) Config["TimerRepeat"] = repeatcmds;

			 
			chaincmds.Add("adminmessage 'Dont forget to like our fanpage!'", 60);
			chaincmds.Add("adminmessage 'Follow us on Twitter!'", 120);
			chaincmds.Add("adminmessage 'You can donate via PayPal!'", 180);
			if(Config["TimerOnce"] == null) Config["TimerOnce"] = chaincmds;
			if(Config["EnableTimerRepeat"] == null) Config["EnableTimerRepeat"] = true;
			if(Config["EnableTimerOnce"] == null) Config["EnableTimerOnce"] = true;
			if(Config["EnabledRealTime-Timer"] == null) Config["EnabledRealTime-Timer"] = true;
			///
			realtimecmds.Add("16:00:00", "adminmessage 'The gate for the event is open!'");
			realtimecmds.Add("16:00:10", "settime 10");
			realtimecmds.Add("16:30:00", "adminmessage 'The gate for the event just closed'");
			realtimecmds.Add("17:00:00", "adminmessage 'Restart in 1 HOUR'");
			realtimecmds.Add("18:00:00", "adminmessage 'The server is restarting NOW.'");
			if(Config["RealTime-Timer"] == null) Config["RealTime-Timer"] = realtimecmds;

        }
		/*[ConsoleCommand("reset.oncetimer")]
		void cmdResOnceTimer(ConsoleSystem.Arg arg)
		{
			RunOnce();
		}*/
	}
}