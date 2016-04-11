using Oxide.Core;
//using Oxide.Core.Plugins;
using System;
using System.Collections.Generic;
//using System.Reflection;
//using System.Linq;
using UnityEngine;
using System.Text.RegularExpressions;


namespace Oxide.Plugins
{
    [Info("AutoReply", "4seti [Lunatiq] for Rust Planet", "1.4.2", ResourceId = 908)]
    public class AutoReply : RustPlugin
    {

        #region Utility Methods

        private void Log(string message)
        {
            Puts("{0}: {1}", Title, message);
        }
		
        private void Warn(string message)
        {
            PrintWarning("{0}: {1}", Title, message);
        }

        private void Error(string message)
        {
            PrintError("{0}: {1}", Title, message);
        }

		void ReplyChat(BasePlayer player, string msg)
		{
			player.ChatMessage(string.Format("<color=#81D600>{0}</color>: {1}", ReplyName, msg));
		}

		#endregion

		#region Default and private params
		private Dictionary<string, Dictionary<ulong, float>> antiSpam = new Dictionary<string, Dictionary<ulong, float>>();
		private int replyInterval = 5;		
		private int minPriveledge = 0;
        private bool forceAdmin = false;
		
		private Dictionary<string, string> messages;		
		private Dictionary<string, string> defMsg = new Dictionary<string, string>()
		{
			{"attribSet", "Attribute {0} set to {1}"},  
			{"newWord", "New word was added to check: <color=#81F23F>{0}</color> for group: <color=#81F23F>{1}</color>"}, 
			{"newGroup", "New word group was added to check: <color=#81F23F>{0}</color> baseword: {1} with reply: <color=#81F23F>{2}</color>"}, 
			{"removedGroup", "Word group \"<color=#81F23F>{0}</color>\" was removed!"}, 			
			{"newChar", "New char replacement was added to check: <color=#F23F3F>{0}</color>-><color=#81F23F>{1}</color>"}, 
			{"charRemoved", "Char replacement was removed from check: <color=#F23F3F>{0}</color>"},
			{"groupParams", "Full matching: <color=#81F23F>{0}</color>, Drop message:  <color=#81F23F>{1}</color>"},
			{"charNotFound", "Char replacement not found in check: <color=#F23F3F>{0}</color>"},			
			{"baseWordExist", "This baseword or part of it (<color=#F23F3F>{0}</color>) already exists in group <color=#81F23F>{1}</color>"},
			{"newCharExists", "Char already persist in the check: <color=#81F23F>{0}</color>"}, 
			{"replyChanged", "Reply changed for word group: <color=#81F23F>{0}</color>"}, 			
			{"replyAdded", "Reply added for word group: <color=#81F23F>{0}</color> with number: <color=#81F23F>{1}</color>"}, 	
			{"replyRemoved", "Reply removed for word group: <color=#81F23F>{0}</color> with number: <color=#81F23F>{1}</color>"}, 
			{"replyNotFound", "Reply â<color=#81F23F>{0}</color> not found for word group: <color=#81F23F>{1}</color>"}, 			
			{"Error", "Something went wrong"},
			{"noGroup", "No groups found!"},
			{"matchChanged", "Match for group: <color=#81F23F>{0}</color>, changed to <color=#81F23F>{1}</color>"},
			{"matchNotFound", "Group: <color=#81F23F>{0}</color> not found"},
			{"dropChanged", "AutoDrop for group: <color=#81F23F>{0}</color>, changed to <color=#81F23F>{1}</color>"},
			{"newWordExists", "Word already persist in the check: <color=#F23F3F>{0}</color>"},
			{"wordGroupExist", "Word group with that name exist: <color=#F23F3F>{0}</color>"},
			{"wordGroupDontExist", "Word group with that name don't exist: <color=#F23F3F>{0}</color> use <color=#F23F3F>/ar_new</color> first"},			
			{"newAttr", "New attribute added! Name: {0}, Text: {1}"},
			{"attrRemoved", "Attribute removed! Name: {0}"},	
			{"attrEdited", "Attribute edited! Name: {0}, New value: {1}"},				
			{"attrNoFound", "Attribute not found! Name: {0}"},	
			{"attrExist", "Attribute \"{0}\" already exist"},
			{"newGroupError", "Error! Should be: <color=#F23F3F>/ar_new groupname baseword replymsg params(optional)</color>"},
			{"changeReplyError", "Error! Should be: <color=#F23F3F>/ar_reply add/del/set (set or del is by nums (check /ar_list)) groupname replymsg attribs</color>"},
			{"attrAdded", "Attrib: <color=#F23F3F>{0}</color> added for word group <color=#F23F3F>{1}</color>"},
			{"attrDeleted", "Attrib: <color=#F23F3F>{0}</color> deleted for word group <color=#F23F3F>{1}</color>"},
			{"attrCleared", "Attributes cleared for word group <color=#F23F3F>{0}</color>"},
			{"attrWordExist", "Attrib: <color=#F23F3F>{0}</color> exists in word group <color=#F23F3F>{1}</color>"},
			{"attrNotExist", "Attrib: <color=#F23F3F>{0}</color> do not exists in word group <color=#F23F3F>{1}</color>"},			
			{"attrUnknown", "UNKNOWN Attrib: <color=#F23F3F>{0}</color>"},		
			{"attrCritError", "Error! Should be like that: <color=#81F23F>/ar_wa add/del/clear groupname ReplyNum attrib</color>"},
			{"wordAdded", "Word: <color=#F23F3F>{0}</color> added for word group <color=#F23F3F>{1}</color>"},
			{"wordDeleted", "Word: <color=#F23F3F>{0}</color> deleted for word group <color=#F23F3F>{1}</color>"},
			{"wordWordExist", "Word: <color=#F23F3F>{0}</color> exists in word group <color=#F23F3F>{1}</color>"},
			{"wordNotExist", "Word: <color=#F23F3F>{0}</color> do not exists in word group <color=#F23F3F>{1}</color>"},				
			{"wordCritError", "Error! Should be like that: <color=#81F23F>/ar_word add/del groupname word</color>"},
			{"listGroupReply", "Group Name: <color=#81F23F>{0}</color> with <color=#81F23F>{1}</color> hits total" },
			{"listWords", "Words to lookup: <color=#81F23F>{0}</color>"},
			{"listAttribs", "Attributes for reply: <color=#81F23F>{0}</color>"},
			{"usageOfExc", "Usage of \"!\" at word start is forbidden use \"?\" instead"},
            {"ForceOn", "Force Admins check was <color=#81F23F>ENABLED</color>"},
            {"ForceOff", "Force Admins check was <color=#F23F3F>DISABLED</color>"}
		};
		Dictionary<string, Wordgroup> Wordgroups = new Dictionary<string, Wordgroup>();

		
		private Dictionary<string, string> attributes;
		private Dictionary<string, string> defAttr = new Dictionary<string, string>()
		{
			{"time", "by_plugin"},
			{"player", "by_plugin"},
			{"online", "by_plugin"},
			{"sleepers", "by_plugin"},
            {"gametime", "by_plugin"},
			{"lastwipe", "???"},
			{"nextwipe", "???"}
		};
		void Unload()
		{
			SaveData();
		}
		private string ReplyName = "AutoReply";
		
		private Dictionary<char, char> replaceChars;
		private Dictionary<char, char> defChar = new Dictionary<char, char>()
		{
			{'Ñ', 'c'},
			{'Ð°', 'a'},
			{'Ð¾', 'o'},    
			{'Ðµ', 'e'}, 
			{'Ñ', 'p'}, 					
			{'Ð²', 'b'}			
		};		
		#endregion
		#region Default inits
        void Loaded()
        {
            Log("Loaded");
        }
	
		protected override void LoadDefaultConfig()
        {
            Warn("Creating a new config file");
            Config.Clear();
            LoadVariables();
        }
		
		// Gets a config value of a specific type
        private T GetConfig<T>(string name, T defaultValue)
        {
            if (Config[name] == null)
                return defaultValue;
            return (T)Convert.ChangeType(Config[name], typeof(T));
        }
		private void LoadData()
		{
			try
			{
				Wordgroups = Interface.GetMod().DataFileSystem.ReadObject<Dictionary<string, Wordgroup>>("AutoReply");
				antiSpam = new Dictionary<string, Dictionary<ulong, float>>();
				foreach (var wordgroup in Wordgroups)
				{
                    antiSpam.Add(wordgroup.Key, new Dictionary<ulong, float>());
                }
				Log("Old AutoReply data loaded!");
			}
			catch
			{
				Wordgroups = new Dictionary<string, Wordgroup>();
				antiSpam = new Dictionary<string, Dictionary<ulong, float>>();
				Warn("Old Data corrupted, new AutoReply data file initiated!");
				SaveData();
			}			
		}
		void SaveData()
		{
			Interface.GetMod().DataFileSystem.WriteObject<Dictionary<string, Wordgroup>>("AutoReply", Wordgroups);
			Log("Data saved!");
		}
		#endregion
		
        void OnServerInitialized()
        {
            try
            {
                LoadConfig();
                var version = GetConfig<Dictionary<string, object>>("version", null);
                VersionNumber verNum = new VersionNumber(Convert.ToUInt16(version["Major"]), Convert.ToUInt16(version["Minor"]), Convert.ToUInt16(version["Patch"]));
				
				//Get message dictionary for plugin commands (for Admins) from config
				messages = new Dictionary<string, string>();
                var cfgMessages = GetConfig<Dictionary<string, object>>("messages", null);
                if (cfgMessages != null)
                    foreach (var pair in cfgMessages)
                        messages[pair.Key] = Convert.ToString(pair.Value);			
				//Get char replace list from config
				replaceChars = new Dictionary<char, char>();
				var cfgChar = GetConfig<Dictionary<string, object>>("replaceChars", null);
                if (cfgChar != null)
                    foreach (var pair in cfgChar)
                        replaceChars[Convert.ToChar(pair.Key)] = Convert.ToChar(pair.Value);		
								
				//Get attributes list from config
				attributes = new Dictionary<string, string>();
				var cfgAttr = GetConfig<Dictionary<string, object>>("attributes", null);
				if (cfgAttr != null)
                    foreach (var pair in cfgAttr)
                        attributes[pair.Key] = Convert.ToString(pair.Value);
						
				#region version checker
				if (verNum < Version || defMsg.Count > messages.Count)
                {
                    //placeholder for future version updates
					foreach (var pair in defMsg)
                        if (!messages.ContainsKey(pair.Key))
                            messages[pair.Key] = pair.Value;
							
					foreach (var pair in defAttr)
                        if (!attributes.ContainsKey(pair.Key))
                            attributes[pair.Key] = pair.Value;
					Config["attributes"] = attributes;
                    Config["messages"] = messages;
					Config["version"] = Version;					
					SaveConfig();
                    Warn("Config version updated to: " + Version.ToString() + " please check it");
                }
				#endregion
				ReplyName = GetConfig<string>("ReplyName", ReplyName);
				replyInterval = GetConfig<int>("replyInterval", 30);	
				minPriveledge = GetConfig<int>("minPriveledge", 0);
				LoadData();
            }
            catch (Exception ex)
            {
                Error("OnServerInitialized failed: " + ex.Message);
            }
            
        }
		
		private void LoadVariables()
        {
            Config["messages"] = defMsg;
			Config["replaceChars"] = defChar;
			Config["attributes"] = defAttr;
			Config["replyInterval"] = 30;
			Config["minPriveledge"] = 0;
			Config["ReplyName"] = ReplyName;
			Config["version"] = Version;
        }
        private object OnRunCommand(ConsoleSystem.Arg arg)
        {
			BasePlayer player = null;
			string msg = "";
			try
			{
				if (arg == null) return null;
				if (arg.connection.player == null) return null;
				if (arg.cmd.namefull.ToString() != "chat.say") return null;
				
				if (arg.connection.player is BasePlayer)
				{
					player = arg.connection.player as BasePlayer;
					if (player.net.connection.authLevel > minPriveledge && !forceAdmin) return null;
				}
				else return null;
				
				msg = arg.GetString(0, "text").ToLower();		
				
				if (msg == null) return null;
				else if (msg == "") return null;
				else if (msg.Substring(0, 1).Equals("/") || msg.Substring(0, 1).Equals("!")) return null;
				
				if (player == null) return null;
			}
			catch
			{
				return null;
			}

			//Fixing alphabets abuse			
			foreach(var pair in replaceChars)
			{
				msg = msg.Replace(pair.Key, pair.Value);				
			}				
			List<string> foundGroup = new List<string>();
			foreach(var pair in Wordgroups)
			{	
				foreach(var item in pair.Value.Words)
				{
					if(!pair.Value.FullMatch)
					{
						if (msg.Contains(item))
						{
							foundGroup.Add(pair.Key);
							break;
						}
					}
					else
					{
						if (msg == item)
						{
							foundGroup.Add(pair.Key);
							break;
						}					
					}
				}
			}
            if (foundGroup.Count > 0)
			{
				bool blocked = false;
				foreach (var fgroup in foundGroup)
				{
					if (antiSpam[fgroup].ContainsKey(player.userID))
					{
						if ((Time.realtimeSinceStartup - antiSpam[fgroup][player.userID]) > replyInterval)
						{

							replyToPlayer(player, fgroup);
							antiSpam[fgroup][player.userID] = Time.realtimeSinceStartup;
						}
					}
					else
					{
						antiSpam[fgroup].Add(player.userID, Time.realtimeSinceStartup);
						replyToPlayer(player, fgroup);
					}
                    Wordgroups[fgroup].Hits++;
					if (Wordgroups[fgroup].Hits % 20 == 0) SaveData();
                    if (Wordgroups[fgroup].Drop)
                            blocked = true;
                }
				if (blocked)
					return false;
			}			
			return null;
		}
		
		private void replyToPlayer(BasePlayer player, string group)
		{		
			foreach(var v in Wordgroups[group].Replies)
			{
				ReplyChat(player, replyBuilder(v.Value, player.displayName));
			}								
		}
		
		private string replyBuilder(string text, string playerName)
		{
			Regex regex = new Regex(@"{(\w+)}", RegexOptions.IgnoreCase);
			string attrText;
			string curMatch;
			foreach (Match match in regex.Matches(text))
			{
				attrText = string.Empty;
				curMatch = match.Groups[1].Value.ToLower();
                if (attributes.ContainsKey(curMatch))
				{
					if (attributes[curMatch] == "by_plugin")
					{
						switch (curMatch)
						{
							case "time":
								attrText = DateTime.Now.ToString("HH:mm:ss");
								break;
							case "player":
								attrText = playerName;
								break;
							case "online":
								attrText = BasePlayer.activePlayerList.Count.ToString();
								break;
							case "sleepers":
								attrText = BasePlayer.sleepingPlayerList.Count.ToString();
								break;
							case "gametime":
								attrText = TOD_Sky.Instance.Cycle.DateTime.ToString("dd/MM/yy HH:mm");
								break;
							default:
								attrText = "";
								break;
						}
					}
					else
						attrText = attributes[curMatch];
					text = text.Replace("{" + curMatch + "}", attrText, StringComparison.OrdinalIgnoreCase);
				}
			}	
			return text;			
		}
		
		[ChatCommand("ar")]
		void cmdAr(BasePlayer player, string cmd, string[] args)
		{
			if (player.net.connection.authLevel == 0) return;			
			if (args.Length > 2)
			{
				if (args[0] == "c") //adding/deleting new char to replace
				{
					try
					{
						if(args[1] == "add") 
						{
							if (!replaceChars.ContainsKey(Convert.ToChar(args[2])))
							{
								replaceChars.Add(Convert.ToChar(args[2].ToLower()), Convert.ToChar(args[3].ToLower()));
								Config["replaceChars"] = replaceChars;
								ReplyChat(player, string.Format(messages["newChar"], args[2], args[3]));	
							}	
							else	
							{
								ReplyChat(player, string.Format(messages["newCharExists"], args[2]));	
							}
						}	
						else if(args[1] == "del")
						{	
							if (replaceChars.ContainsKey(Convert.ToChar(args[2])))
							{
								replaceChars.Remove(Convert.ToChar(args[2].ToLower()));								
								Config["replaceChars"] = replaceChars;		
								ReplyChat(player, string.Format(messages["charRemoved"], args[2]));
							}
							else
							{
								ReplyChat(player, string.Format(messages["charNotFound"], args[2]));
							}							
						}
					}		
					catch		
					{
						ReplyChat(player, messages["Error"]);	
					}					
				}
				else if(args[0] == "a") //adding/deleting new attribute for word list
				{
					try
					{
						if(args[1] == "add") 
						{
							if (!attributes.ContainsKey(args[2]))
							{
								attributes.Add(args[2].ToLower(), args[3]);
								Config["attributes"] = attributes;
								ReplyChat(player, string.Format(messages["newAttr"], args[2], args[3]));	
							}	
							else	
							{
								ReplyChat(player, string.Format(messages["attrExist"], args[2]));	
							}	
						}
						else if(args[1] == "del") 
						{
							if (attributes.ContainsKey(args[2]))
							{
								if (attributes[args[2]] == "by_plugin")
								{
									ReplyChat(player, string.Format(messages["Error"]));
									return;
								}
								//removeAttr(args[2].ToLower());
								attributes.Remove(args[2].ToLower());
								Config["attributes"] = attributes;
								ReplyChat(player, string.Format(messages["attrRemoved"], args[2]));	
							}	
							else	
							{
								ReplyChat(player, string.Format(messages["attrNotFound"], args[2]));	
							}	
						}
						else if(args[1] == "set") 
						{
							if (attributes.ContainsKey(args[2]))
							{
								if (attributes[args[2]] == "by_plugin")
								{
									ReplyChat(player, string.Format(messages["Error"]));
									return;
								}
								attributes[args[2].ToLower()] = args[3];
								Config["attributes"] = attributes;
								ReplyChat(player, string.Format(messages["attrEdited"], args[2], args[3]));	
							}	
							else	
							{
								ReplyChat(player, string.Format(messages["attrNotFound"], args[2]));	
							}	
						}
					}		
					catch		
					{
						ReplyChat(player, messages["Error"]);	
					}				
				}
				SaveConfig();
			}
		}

		[ChatCommand("ar_save")]
		void cmdSave(BasePlayer player, string cmd, string[] args)
		{
			if (player.net.connection.authLevel == 0) return;
			SaveData();
			ReplyChat(player, "Data Saved!");
		}

		[ChatCommand("ar_load")]
		void cmdLoad(BasePlayer player, string cmd, string[] args)
		{
			if (player.net.connection.authLevel == 0) return;
			LoadData();
			ReplyChat(player, "Data Loaded!");
		}


		private bool checkWord(BasePlayer player, string baseWord)
		{
			bool found = true;
			string foundGroup = "";
			foreach(var pair in Wordgroups)
			{	
				foreach(var item in pair.Value.Words)
				{					
					if(!pair.Value.FullMatch)
					{
						if (baseWord.Contains(item))
						{
							found = false;
							foundGroup = pair.Key;					
							break;
						}
					}
					else
					{
						if (baseWord == item)
						{
							found = false;
							foundGroup = pair.Key;			
							break;
						}					
					}					
				}
			}
			if (!found)
			{
				ReplyChat(player, string.Format(messages["baseWordExist"], baseWord, foundGroup));				
			}
			return found;
		}
		
		//Adding new word group
		[ChatCommand("ar_new")]
		void cmdArNew(BasePlayer player, string cmd, string[] args)
		{
			if (player.net.connection.authLevel == 0) return;			
			if (args.Length > 2)
			{
				if (!Wordgroups.ContainsKey(args[0]))
				{
					string groupName = args[0].ToLower();
					string baseWord = args[1].ToLower();
					if (baseWord.Substring(0, 1).Equals("!"))
					{
						ReplyChat(player, messages["usageOfExc"]);	
						return;
					}
					string reply = args[2];
					
					if (!checkWord(player, baseWord))
						return;
					List<string> attrCheck = checkAttributes(reply);
					if (attrCheck.Count > 0)
					{
						ReplyChat(player, string.Format(messages["attrUnknown"], string.Join(", ", attrCheck.ToArray())));
						return;
					}
                    Wordgroups.Add(groupName, new Wordgroup(baseWord, reply));
					ReplyChat(player, string.Format(messages["newGroup"], groupName, baseWord, reply));
					antiSpam.Add(groupName, new Dictionary<ulong, float>());
					SaveData();					
				}
				else
				{
					ReplyChat(player, string.Format(messages["wordGroupExist"], args[0]));
					return;
				}
			
			}
			else
				ReplyChat(player, messages["newGroupError"]);	
		}
		
		//Change reply
		[ChatCommand("ar_reply")]
		void cmdArReply(BasePlayer player, string cmd, string[] args)
		{
			if (player.net.connection.authLevel == 0) return;			
			if (args.Length > 2)
			{
				string groupName = args[1];
				if (Wordgroups.ContainsKey(groupName))
				{
					string mode = args[0];					
					if (mode == "add")
					{
						string reply = args[2];
						int newReply = Wordgroups[groupName].Replies.Count;
						List<string> attrCheck = checkAttributes(reply);
						if (attrCheck.Count > 0)
						{
							ReplyChat(player, string.Format(messages["attrUnknown"], string.Join(", ", attrCheck.ToArray())));
							return;
						}
						Wordgroups[groupName].Replies.Add(newReply, reply);
						ReplyChat(player, string.Format(messages["replyAdded"], groupName, newReply));
						SaveData();
						
					}	
					else if(mode == "del")
					{
						int removeKey = Convert.ToInt32(args[2]);
						if(Wordgroups[groupName].Replies.ContainsKey(removeKey))
						{
							Wordgroups[groupName].Replies.Remove(removeKey);
							ReplyChat(player, string.Format(messages["replyRemoved"], groupName, removeKey));					
							SaveData();
						}
						else
							ReplyChat(player, string.Format(messages["replyNotFound"], removeKey, groupName));				
					}
					else if(mode == "set")
					{
						int setKey = Convert.ToInt32(args[2]);
						string reply = args[3];
						if(Wordgroups[groupName].Replies.ContainsKey(setKey))
						{
							List<string> attrCheck = checkAttributes(reply);
							if (attrCheck.Count > 0)
							{
								ReplyChat(player, string.Format(messages["attrUnknown"], string.Join(", ", attrCheck.ToArray())));
								return;
							}
							Wordgroups[groupName].Replies[setKey] = reply;
							ReplyChat(player, string.Format(messages["replyChanged"], groupName, setKey));
							SaveData();
						}
						else
							ReplyChat(player, string.Format(messages["replyNotFound"], setKey, groupName));		
					}
					else
						ReplyChat(player, messages["Error"]);
				}
				else
				{
					ReplyChat(player, string.Format(messages["wordGroupDontExist"], args[0]));
					return;
				}
			
			}
			else
				ReplyChat(player, messages["changeReplyError"]);	
		}

		private List<string> checkAttributes(string text)
		{
			Regex regex = new Regex(@"{(\w+)}", RegexOptions.IgnoreCase);
			List<string> missedAttribs = new List<string>();

			foreach (Match match in regex.Matches(text))
			{
				if (!attributes.ContainsKey(match.Groups[1].Value.ToLower()))
					missedAttribs.Add(match.Groups[1].Value.ToLower());
            }
			return missedAttribs;
		} 
		
		//Change autodrop
		[ChatCommand("ar_drop")]
		void cmdArDrop(BasePlayer player, string cmd, string[] args)
		{
			if (player.net.connection.authLevel == 0) return;
			if (args.Length > 1)
			{
				string group = args[0];
				bool drop = Convert.ToBoolean(args[1]);
				if (Wordgroups.ContainsKey(group))
				{
					Wordgroups[group].Drop = drop;
					ReplyChat(player, string.Format(messages["dropChanged"], group, drop.ToString()));
					SaveData();
				}
				else
					ReplyChat(player, string.Format(messages["matchNotFound"], group));
			}
			else
				ReplyChat(player, messages["Error"]);
		}

		//Change matching
		[ChatCommand("ar_match")]
		void cmdArMatch(BasePlayer player, string cmd, string[] args)
		{
			if (player.net.connection.authLevel == 0) return;		
			if (args.Length > 1)
			{
				string group = args[0];
				bool match = Convert.ToBoolean(args[1]);
				if (Wordgroups.ContainsKey(group))
				{
					Wordgroups[group].FullMatch = match;
					ReplyChat(player, string.Format(messages["matchChanged"], group, match.ToString()));
					SaveData();
				}
				else
					ReplyChat(player, string.Format(messages["matchNotFound"], group));							
			}
			else
				ReplyChat(player, messages["Error"]);	
		}
		//Remove whole group
		[ChatCommand("ar_remove")]
		void cmdArRemove(BasePlayer player, string cmd, string[] args)
		{
			if (player.net.connection.authLevel == 0) return;
			if (args.Length == 0) return;
			string groupname = args[0];
				
			if (Wordgroups.ContainsKey(groupname))
			{
				Wordgroups.Remove(groupname);
				antiSpam.Remove(groupname);
                SaveData();
				ReplyChat(player, string.Format(messages["removedGroup"], groupname));
			}
		}
		//Foce check admins
		[ChatCommand("ar_force")]
        void cmdArForce(BasePlayer player, string cmd, string[] args)
        {
            if (player.net.connection.authLevel == 0) return;
            if (forceAdmin)
                ReplyChat(player, messages["ForceOff"]);
            else
                ReplyChat(player, messages["ForceOn"]);
            forceAdmin = !forceAdmin;
        }
		//Add word for word group
		[ChatCommand("ar_word")]
		void cmdArWord(BasePlayer player, string cmd, string[] args)
		{
			if (player.net.connection.authLevel == 0) return;			
			if (args.Length > 2)
			{
				if (Wordgroups.ContainsKey(args[1]))
				{
					string groupName = args[1].ToLower();
					string mode = args[0].ToLower();
					string word = args[2].ToLower();

					if (mode == "add")
					{
						if (!checkWord(player, word))
							return;
						if (!Wordgroups[groupName].Words.Contains(word))
						{
							if (word.Substring(0, 1).Equals("!"))
							{
								ReplyChat(player, messages["usageOfExc"]);	
								return;
							}
							Wordgroups[groupName].Words.Add(word);
							ReplyChat(player, string.Format(messages["wordAdded"], word, groupName));
							SaveData();
							return;							
						}
						else
							ReplyChat(player, string.Format(messages["wordWordExist"], word, groupName));	
						return;
					}
					else if (mode == "del")
					{
						if (Wordgroups[groupName].Words.Contains(word))
						{
							Wordgroups[groupName].Words.Remove(word);
							ReplyChat(player, string.Format(messages["wordDeleted"], word, groupName));
							SaveData();
							return;							
						}
						else
							ReplyChat(player, string.Format(messages["wordNotExist"], word, groupName));	
						return;			
					}
					else
					{
						ReplyChat(player, messages["wordCritError"]);	
					}
				}
				else
				{
					ReplyChat(player, string.Format(messages["wordGroupDontExist"], args[0]));
					return;
				}
			
			}
			else
				ReplyChat(player, messages["wordCritError"]);	
		}

		[ChatCommand("ar_list")]
		void cmdArList(BasePlayer player, string cmd, string[] args)
		{
			if (player.net.connection.authLevel == 0) return;

			if (args.Length == 0)
				if (Wordgroups.Count > 0)
				{
					foreach (var group in Wordgroups)
					{
						ReplyChat(player, string.Format(messages["listGroupReply"], group.Key, group.Value.Hits));
					}
				}
				else
				{
					ReplyChat(player, messages["noGroup"]);
				}
			else
				if (args[0] == "attr")
				foreach (var pair in attributes)
				{
					ReplyChat(player, string.Format("{0} -> {1}", pair.Key, pair.Value.QuoteSafe()));
				}
		}

		[ChatCommand("ar_info")]
		void cmdArInfo(BasePlayer player, string cmd, string[] args)
		{
			if (player.net.connection.authLevel == 0) return;

			if (args.Length == 0) return;

			string groupName = args[0];
			if (Wordgroups.ContainsKey(groupName))
			{
				ReplyChat(player, string.Format(messages["listGroupReply"], groupName, Wordgroups[groupName].Hits));
				ReplyChat(player, string.Format(messages["groupParams"], Wordgroups[groupName].FullMatch, Wordgroups[groupName].Drop));
				ReplyChat(player, string.Format(messages["listWords"], string.Join(", ", Wordgroups[groupName].Words.ToArray())));
				foreach (var v in Wordgroups[groupName].Replies)
				{
					ReplyChat(player, string.Format("<color=#F5D400>[{0}]</color> - {1}", v.Key, v.Value.QuoteSafe()));
					//if (v.Value.ReplyAttr.Count > 0)
					//	ReplyChat(player, string.Format("A:{0}", string.Join(", ", v.Value.ReplyAttr.ToArray())));
				}
			}
			else
			{
				ReplyChat(player, messages["noGroup"]);
			}
		}

		public class Wordgroup
		{
			public List<string> Words;
			public Dictionary<int, string> Replies;
			public bool FullMatch;
			public bool Drop;
			public int Hits;

			public Wordgroup(string baseword, string baseReply)
			{
				Words = new List<string>();
				Words.Add(baseword);
				Replies = new Dictionary<int, string>();
				Replies.Add(0, baseReply);
                FullMatch = false;
				Drop = true;
				Hits = 0;
            }
		}
		public class ReplyEntry
		{
			public string Reply;
			public List<string> ReplyAttr;
			public ReplyEntry()
			{
				Reply = string.Empty;
				ReplyAttr = new List<string>();
            }
			public ReplyEntry(string reply, List<string> attrs)
			{
				Reply = reply;
                if (attrs == null)
					ReplyAttr = new List<string>();
				else
					ReplyAttr = attrs;
            }
		}	
    }
}