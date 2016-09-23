using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Oxide.Core;
using Oxide.Core.Plugins;
using UnityEngine;
using ProtoBuf;
using Network;

namespace Oxide.Plugins
{
    [Info("BackupExt", "Fujikura", "0.1.0")]
    class BackupExt : RustPlugin
    {
		bool Changed;
		bool _backup;
		bool _startup;
		string [] backupFolders;

		bool backupOnStartup;
		int numberOfBackups;
		bool backupBroadcast;
		int backupDelay;
		bool useBroadcastDelay;
		string prefix;
		string prefixColor;
		bool useTimer;
		int timerInterval;
	
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
			backupOnStartup = Convert.ToBoolean(GetConfig("Settings", "backupOnStartup", false));
			numberOfBackups = Convert.ToInt32(GetConfig("Settings", "numberOfBackups", 4));
			backupBroadcast = Convert.ToBoolean(GetConfig("Notification", "backupBroadcast", false));
			backupDelay = Convert.ToInt32(GetConfig("Notification", "backupDelay", 5));
			useBroadcastDelay = Convert.ToBoolean(GetConfig("Notification", "useBroadcastDelay", true));
			prefix = Convert.ToString(GetConfig("Notification", "prefix", "BACKUP"));
			prefixColor = Convert.ToString(GetConfig("Notification", "prefixColor", "orange"));
			useTimer = Convert.ToBoolean(GetConfig("Timer", "useTimer", false));
			timerInterval = Convert.ToInt32(GetConfig("Timer", "timerInterval", 3600));

			if (!Changed) return;
			SaveConfig();
			Changed = false;
		}
		
		void LoadDefaultMessages()
		{
			lang.RegisterMessages(new Dictionary<string, string>
			                      {
									{"backupfinish", "Backup process finished."},
									{"backupannounce", "Starting server backup in {0} seconds."},
									{"backuprunning", "Running server backup."},
									{"backupautomatic", "Running automated server backup every {0} seconds."},									
			                      },this);
		}

		protected override void LoadDefaultConfig()
		{
			Config.Clear();
			LoadVariables();
		}
		
		void Loaded()
		{
			if (_startup) return;
			LoadVariables();
			LoadDefaultMessages();
			backupFolders = BackupFolders();
			_startup = true;
		}
		
		void OnTerrainInitialized()
        {
			if (_startup) Loaded();
			if (backupOnStartup && !_backup)
			{
				_backup = true;
				BackupCreate();
			}
        }
		
		void OnServerInitialized()
        {
			if (useTimer)
			{
				timer.Every(timerInterval, () => ccmdExtBackup(new ConsoleSystem.Arg(null)));
				Puts(string.Format(lang.GetMessage("backupautomatic", this), timerInterval));
			}
        }		

		void BackupCreate(bool manual = false)
		{
			DirectoryEx.Backup(BackupFolders());
			DirectoryEx.CopyAll(ConVar.Server.rootFolder, backupFolders[0]);
			if (!manual)
				Puts(lang.GetMessage("backupfinish", this));
		}

		[ConsoleCommand("extbackup")]
		void ccmdExtBackup(ConsoleSystem.Arg arg)
		{
			if(arg.connection != null && arg.connection.authLevel < 2) return;
			if (backupBroadcast)
			{
				if (useBroadcastDelay)
				{
					SendReply(arg, string.Format(lang.GetMessage("backupannounce", this, arg.connection != null ? arg.connection.userid.ToString() : null ), backupDelay));
					BroadcastChat(string.Format(lang.GetMessage("backupannounce", this), backupDelay));
					timer.Once(backupDelay, () => BackupRun(arg));
				}
				else
				{
					BackupRun(arg);
				}
			}
			else
				BackupRun(arg);
		}
		
		void BackupRun(ConsoleSystem.Arg arg)
		{
				if (backupBroadcast)
					BroadcastChat(lang.GetMessage("backuprunning", this));
				SendReply(arg, lang.GetMessage("backuprunning", this, arg.connection != null ? arg.connection.userid.ToString() : null ));
				BackupCreate(true);
				SendReply(arg, lang.GetMessage("backupfinish", this, arg.connection != null ? arg.connection.userid.ToString() : null ));
				if (backupBroadcast)
					BroadcastChat(lang.GetMessage("backupfinish", this));
		}
		
		string [] BackupFolders()
		{
			string [] dp = new string[numberOfBackups];
			for (int i = 0; i < numberOfBackups; i++)
			{
				dp[i] = $"backup/{i}/{ConVar.Server.identity}";
			}
			return dp;
		}
		
		void BroadcastChat(string msg = null) => PrintToChat(msg == null ? prefix : "<color=" + prefixColor + ">" + prefix + "</color>: " + msg);
	}
}