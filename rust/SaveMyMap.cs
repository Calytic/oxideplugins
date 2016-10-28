using System;
using System.Collections.Generic;
using System.Linq;
using Oxide.Core;
using UnityEngine;

using IEnumerator = System.Collections.IEnumerator;

namespace Oxide.Plugins
{
	[Info("SaveMyMap", "Fujikura", "1.0.0", ResourceId = 2111)] 
	class SaveMyMap : RustPlugin
	{
		bool Changed;
		SaveRestore saveRestore = null;
		int Rounds;
		bool Initialized;
		string saveFolder;
		bool loadReload;
		string [] saveFolders;

		int saveInterval;
		int saveCustomAfter;
		bool callOnServerSave;
		float delayCallOnServerSave;
		bool saveAfterLoadFile;
		bool allowOutOfDateSaves;
		bool enableLoadOverride;
		int numberOfSaves;

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
			saveInterval = Convert.ToInt32(GetConfig("Settings", "saveInterval", 1200));
			saveCustomAfter = Convert.ToInt32(GetConfig("Settings", "saveCustomAfter", 1));
			callOnServerSave = Convert.ToBoolean(GetConfig("Settings", "callOnServerSave", true));
			delayCallOnServerSave = Convert.ToInt32(GetConfig("Settings", "delayCallOnServerSave", 3));
			saveAfterLoadFile = Convert.ToBoolean(GetConfig("Settings", "saveAfterLoadFile", true));
			enableLoadOverride = Convert.ToBoolean(GetConfig("Settings", "enableLoadOverride", true));
			allowOutOfDateSaves = Convert.ToBoolean(GetConfig("Settings", "allowOutOfDateSaves", false));
			numberOfSaves = Convert.ToInt32(GetConfig("Settings", "numberOfSaves", 10));

			if (!Changed) return;
			SaveConfig();
			Changed = false;
		}
		
		void LoadDefaultMessages()
		{
			lang.RegisterMessages(new Dictionary<string, string>
			                      {
									{"kickreason", "Map restore was initiated. Please wait a momemt"},
									{"loadedinfo", "Saveinverval set to: {0} sec. | Custom save after every '{1}' saves"},
									{"alreadysaving", "Server already saving"},
									{"customsavecomplete", "Custom saving complete"},
									{"needconfirm", "You need to confirm with 'force'"},
									{"definefilename", "You need to define a filename to load"},
									{"lastfilename", "You can load the last file by typing 'load' as name"},
									{"filenotfound", "The given filename was not found."},
									{"dirnotfound", "Save Directory not found. Will be recreated for next save."},									
									{"loadoverride", "Loadfile override succesful."},										

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
			Rounds = 0;
			saveFolders = SaveFolders();
		}

		void Unload()
		{
			if (saveRestore != null)
				saveRestore.timedSave = true;
		}

		void OnServerInitialized()
		{
			saveRestore = SingletonComponent<SaveRestore>.Instance;
			saveRestore.timedSave = false;
			saveFolder = $"{ConVar.Server.rootFolder}/saves/{0}/";
			timer.Every(saveInterval, SaveLoop);
			Initialized = true;
			Puts(lang.GetMessage("loadedinfo", this), saveInterval, saveCustomAfter);
		}		
		
		void SaveLoop()
		{
			if (!Initialized) return;
			if (Rounds < saveCustomAfter && saveCustomAfter > 0) {
				foreach (BaseEntity current in BaseEntity.saveList)
					current.InvalidateNetworkCache();
				if (callOnServerSave)
					timer.Once(delayCallOnServerSave, () => Interface.CallHook("OnServerSave", null));
				IEnumerator original = SaveRestore.Save(ConVar.Server.rootFolder+"/"+SaveRestore.SaveFileName, true);					
				while (original.MoveNext()) {} 
				if (!callOnServerSave) Interface.Oxide.DataFileSystem.WriteObject(this.Title, new List<object>(new object[] { ConVar.Server.rootFolder+"/"+SaveRestore.SaveFileName, "default" }) );
				Rounds++;
			} else {
				string file = saveFolder + SaveRestore.SaveFileName;
				if (callOnServerSave)
					timer.Once(delayCallOnServerSave, () => Interface.CallHook("OnServerSave", file));
				try {
					SaveBackupCreate();
					foreach (BaseEntity current in BaseEntity.saveList)
						current.InvalidateNetworkCache();
					IEnumerator custom = SaveRestore.Save(file, true);					
					while (custom.MoveNext()) {}
					if (!callOnServerSave) Interface.Oxide.DataFileSystem.WriteObject(this.Title, new List<object>(new object[] { file, "custom" }) );}
				catch { PrintWarning(lang.GetMessage("dirnotfound", this)); }
				Rounds = 0;
			}
		}
		
		void OnServerSave(object file = null)
		{
			string type;
			if (file == null)
			{
				file = ConVar.Server.rootFolder+"/"+SaveRestore.SaveFileName;
				type = "default";
			}
			else
				type = "custom";
			Interface.Oxide.DataFileSystem.WriteObject(this.Title, new List<object>(new object[] { file, type }) );
		}
		
		object OnSaveLoad(Dictionary<BaseEntity, ProtoBuf.Entity> dictionary)
		{
			if (Initialized || loadReload) return null;
			if (!enableLoadOverride) return null;
			if (!loadReload)
			{
			List<string> filename = Interface.Oxide.DataFileSystem.ReadObject<List<string>>(this.Title);
			if (filename != null && filename.Count == 2)
				if (filename[1] == "custom")
				{
					loadReload = true;
					if (SaveRestore.Load(filename[0], allowOutOfDateSaves))
					{
						dictionary.Clear();
						Puts(lang.GetMessage("loadoverride", this));
						return true;
					}
				}
			}
			return null;
		}
		
		[ConsoleCommand("smm.save")]
		void cMapSave(ConsoleSystem.Arg arg)
		{
			if(arg.connection != null && arg.connection.authLevel < 2) return;
			if (SaveRestore.IsSaving) {
				SendReply(arg, lang.GetMessage("alreadysaving", this, arg.connection != null ? arg.connection.userid.ToString() : null ));
				return;
			}
			SaveBackupCreate();
			string saveName;
			saveName = saveFolder + SaveRestore.SaveFileName;
			try {
				foreach (BaseEntity current in BaseEntity.saveList)
					current.InvalidateNetworkCache();
				IEnumerator enumerator = SaveRestore.Save(saveName, true);
				while (enumerator.MoveNext()) {}
				Interface.Oxide.DataFileSystem.WriteObject(this.Title, new List<object>(new object[] { saveName, "custom" }) );
				arg.ReplyWith(lang.GetMessage("customsavecomplete", this, arg.connection != null ? arg.connection.userid.ToString() : null )); }
			catch { PrintWarning(lang.GetMessage("dirnotfound", this)); }
		}
		
		[ConsoleCommand("smm.loadmap")]
		void cLoadMap(ConsoleSystem.Arg arg)
		{
			if(arg.connection != null && arg.connection.authLevel < 2) return;
			if (arg.Args == null || arg.Args.Length != 1 || arg.Args[0] != "force")
			{
				SendReply(arg, lang.GetMessage("needconfirm", this, arg.connection != null ? arg.connection.userid.ToString() : null ));
				return;
			}
			foreach (var player in BasePlayer.activePlayerList.ToList())
				player.Kick(lang.GetMessage("kickreason", this, player.UserIDString));
			SaveRestore.Load(ConVar.Server.rootFolder+"/"+SaveRestore.SaveFileName, allowOutOfDateSaves);
		}

		[ConsoleCommand("smm.loadfile")]
		void cLoadFile(ConsoleSystem.Arg arg)
		{
			if(arg.connection != null && arg.connection.authLevel < 2) return;
			if (arg.Args == null || arg.Args.Length < 1 )
			{
					SendReply(arg, lang.GetMessage("definefilename", this, arg.connection != null ? arg.connection.userid.ToString() : null ));
					return;
			}
			int folderNumber = -1;
			if (arg.Args[0].Length <= 4 && arg.Args[0] != "last" && !int.TryParse(arg.Args[0], out folderNumber))
			{
					SendReply(arg, lang.GetMessage("lastfilename", this, arg.connection != null ? arg.connection.userid.ToString() : null ));
					return;
			}			
			string file = "";
			if (arg.Args[0] == "last")
			{
				List<string> filename = Interface.Oxide.DataFileSystem.ReadObject<List<string>>(this.Title);
				if (filename != null)
					file = filename.First();
			}
			else if (int.TryParse(arg.Args[0], out folderNumber))
			{
				file = $"{ConVar.Server.rootFolder}/saves/{folderNumber}/{SaveRestore.SaveFileName}";
			}
			if (file == "")
				file = saveFolder + arg.Args[0];

			foreach (var player in BasePlayer.activePlayerList.ToList())
				player.Kick(lang.GetMessage("kickreason", this));

			if (SaveRestore.Load(file, allowOutOfDateSaves))
			{
				if (saveAfterLoadFile)
				{
					foreach (BaseEntity current in BaseEntity.saveList)
						current.InvalidateNetworkCache();
					SaveRestore.Save(true);
				}
			}
			else
			{
				SendReply(arg, lang.GetMessage("filenotfound", this, arg.connection != null ? arg.connection.userid.ToString() : null ));
				return;
			}
		}

		Int32 UnixTimeStampUTC()
		{
			Int32 unixTimeStamp;
			DateTime currentTime = DateTime.Now;
			DateTime zuluTime = currentTime.ToUniversalTime();
			DateTime unixEpoch = new DateTime(1970, 1, 1);
			unixTimeStamp = (Int32)(zuluTime.Subtract(unixEpoch)).TotalSeconds;
			return unixTimeStamp;
		}
		
		string [] SaveFolders()
		{
			string [] dp = new string[numberOfSaves];
			for (int i = 0; i < numberOfSaves; i++)
			{
				dp[i] = $"{ConVar.Server.rootFolder}/saves/{i}/";
			}
			return dp;
		}
		
		void SaveBackupCreate()
		{
			DirectoryEx.Backup(SaveFolders());
			ConVar.Server.GetServerFolder("saves/0/");
		}
	}
}