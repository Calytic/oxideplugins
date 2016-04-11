using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Text.RegularExpressions;
using Oxide.Core;
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
    [Info("ChatAPI", "Prefix", "0.3.0")]
    public class ChatAPI : RustLegacyPlugin
    {
		
		public string ChatTag = "[ChatAPI]";
		static string FormatTag = "{0} ({1}) {2}";
		static string FormatMessage = "{0}{1}";
		
		class ChatPerson : ChatAPI
		{
			public string displayName;
			public string prefix;
			public int prefix_pr = 0; // Prefix priority
			public string suffix;
			public int suffix_pr = 0; // Suffix priority
			public string chatcolor = "[color #ffffff]";
			public int chatcolor_pr = 0; // Suffix priority
			public Plugin listenersplugin;
			public List<NetUser> listoflisteners;
			public string tag = "";

			public ChatPerson(string _displayName, string _prefix = "", string _suffix = "", string _chatcolor = "[color #ffffff]")
			{
				displayName = _displayName;
				prefix = _prefix;
				suffix = _suffix;
				chatcolor = _chatcolor;
				UpdateTag();
			}
			
			public void UpdateTag() {
				tag = string.Format(FormatTag, prefix, displayName, suffix).Trim();
			}
		}
		
		Dictionary<NetUser, ChatPerson> ChatPersonData = new Dictionary<NetUser, ChatPerson>();
		
		void LoadDefaultConfig() { }

        private void CheckCfg<T>(string Key, ref T var)
        {
            if (Config[Key] is T)
                var = (T)Config[Key];
            else
                Config[Key] = var;
        } 
		
        void Init()
        {
            CheckCfg<string>("Chat: Player tag", ref FormatTag);
			CheckCfg<string>("Chat: Player message", ref FormatMessage);
			SaveConfig();
			
			// If loaded mannualy
			if(PlayerClient.All.Count > 0) {
				var netusers = PlayerClient.All.Select(pc => pc.netUser).ToList();
				for (int i = 0; i < netusers.Count; i++)
				{
					TryToAdd(netusers[i]);
					i++;
				}
			}
		
        }	
		
		bool OnPlayerChat(NetUser netuser, string message)
		{
			object obj = Interface.CallHook("ChatAPIPlayerChat", netuser, message);
			bool strip = true;
			if (obj is bool) {
				if((bool)obj == false) {
					return false;
				}
			}
			if(obj is string) {
				message = (string)obj;
				strip = false;
			} else {
				message = StripBBCode(message);
			}
			object cl = getCP(netuser);
			if(cl is bool) 
				return false;
			
			ChatPerson cp = (ChatPerson)cl;
			if(cp == null)
				return false;
			
			string msg = string.Format(FormatMessage, cp.chatcolor, message).Trim();
			string ctag = cp.tag;
			
			if(ctag == null) {
				cp.UpdateTag();
				ctag = cp.tag;
			}
			
			if ( cp.listenersplugin != null && cp.listoflisteners.Any()) {
				foreach (NetUser listener in cp.listoflisteners.ToList())
				{
					if(listener == null)
						continue;
					
					rust.SendChatMessage(listener, ctag, msg);
				}
			} else {
				rust.BroadcastChat(ctag, msg);
			}
			
			Puts(ctag + " " + StripBBCode(message));
			
			return true;
		}
		
		string StripBBCode(string bbCode)
		{
			string r = Regex.Replace(bbCode,
			@"\[(.*?)\]",
			String.Empty, RegexOptions.IgnoreCase);

			return r;
		}
		
		void OnPlayerConnected(NetUser netuser)
		{
			if (!(ChatPersonData.ContainsKey(netuser))) {
				ChatPerson cp = new ChatPerson(netuser.displayName);
				ChatPersonData.Add(netuser, cp);
			}
		}
		
		void TryToAdd(NetUser netuser) {
			if (!(ChatPersonData.ContainsKey(netuser))) {
				ChatPerson cp = new ChatPerson(netuser.displayName);
				ChatPersonData.Add(netuser, cp);
				Interface.CallHook("onChatApiPlayerLoad", netuser);
			}
		}
		
		void OnPlayerDisconected(uLink.NetworkPlayer networkPlayer)
		{
			NetUser netuser = (NetUser)networkPlayer.GetLocalData();
			if(ChatPersonData.ContainsKey(netuser)) {
				ChatPersonData.Remove(netuser);
			}
		}
		
		object getCP(NetUser netuser) {
			if (ChatPersonData.ContainsKey(netuser)) {
				return ChatPersonData[netuser];
			}
			ChatPerson cp = new ChatPerson(netuser.displayName);
			ChatPersonData.Add(netuser, cp);
			return ChatPersonData[netuser];
		}
		
		object getPrefix(NetUser netuser) {
			if(getCP(netuser) is ChatPerson) {
				ChatPerson cp = (ChatPerson)getCP(netuser);
				return cp.prefix;
			}
			return false;
		}
		
		bool setPrefix(NetUser netuser, string prefix, int priority = 0) {
			if(getCP(netuser) is ChatPerson) {
				ChatPerson cp = (ChatPerson)getCP(netuser);
				if(priority >= cp.prefix_pr) {
					cp.prefix = prefix;
					cp.prefix_pr = priority;
					cp.UpdateTag();
					return true;
				}
			}
			return false;
		}
		
		bool resetPrefix(NetUser netuser) {
			if(getCP(netuser) is ChatPerson) {
				ChatPerson cp = (ChatPerson)getCP(netuser);
				cp.prefix = "";
				cp.prefix_pr = 0;
				cp.UpdateTag();
				return true;
			}
			return false;
		}
		
		object getSuffix(NetUser netuser) {
			if(getCP(netuser) is ChatPerson) {
				ChatPerson cp = (ChatPerson)getCP(netuser);
				return cp.suffix;
			}
			return false;
		}
		
		bool setSuffix(NetUser netuser, string suffix, int priority = 0) {
			if(getCP(netuser) is ChatPerson) {
				ChatPerson cp = (ChatPerson)getCP(netuser);
				if(priority >= cp.suffix_pr) {
					cp.suffix = suffix;
					cp.suffix_pr = priority;
					cp.UpdateTag();
					return true;
				}
			}
			return false;
		}
		
		bool resetSuffix(NetUser netuser) {
			if(getCP(netuser) is ChatPerson) {
				ChatPerson cp = (ChatPerson)getCP(netuser);
				cp.suffix = "";
				cp.suffix_pr = 0;
				cp.UpdateTag();
				return true;
			}
			return false;
		}
		
		object getDisplayName(NetUser netuser) {
			if(getCP(netuser) is ChatPerson) {
				ChatPerson cp = (ChatPerson)getCP(netuser);
				return cp.displayName;
			}
			return false;
		}
		
		bool setDisplayName(NetUser netuser, string DisplayName) {
			if(getCP(netuser) is ChatPerson) {
				ChatPerson cp = (ChatPerson)getCP(netuser);
				cp.displayName = DisplayName;
				return true;
			}
			return false;
		}
		
		bool resetDisplayName(NetUser netuser) {
			if(getCP(netuser) is ChatPerson) {
				ChatPerson cp = (ChatPerson)getCP(netuser);
				cp.displayName = netuser.displayName;
				cp.UpdateTag();
				return true;
			}
			return false;
		}
		
		object getChatColor(NetUser netuser) {
			if(getCP(netuser) is ChatPerson) {
				ChatPerson cp = (ChatPerson)getCP(netuser);
				return cp.chatcolor;
			}
			return false;
		}
		
		bool setChatColor(NetUser netuser, string chatcolor, int priority = 0) {
			if(getCP(netuser) is ChatPerson) {
				ChatPerson cp = (ChatPerson)getCP(netuser);
				if(priority >= cp.suffix_pr) {
					cp.chatcolor = chatcolor;
					cp.chatcolor_pr = priority;
					return true;
				}
			}
			return false;
		}
		
		bool resetChatColor(NetUser netuser) {
			if(getCP(netuser) is ChatPerson) {
				ChatPerson cp = (ChatPerson)getCP(netuser);
				cp.chatcolor = "[color white]";
				cp.chatcolor_pr = 0;
				return true;
			}
			return false;
		}
		bool setCustomTag(NetUser netuser, string tag) {
			if(getCP(netuser) is ChatPerson) {
				ChatPerson cp = (ChatPerson)getCP(netuser);
				cp.tag = tag;
				
				return true;
			}
			return false;
		}
		
		bool resetCustomTag(NetUser netuser) {
			if(getCP(netuser) is ChatPerson) {
				ChatPerson cp = (ChatPerson)getCP(netuser);
				cp.UpdateTag();
				return true;
			}
			return false;
		}
		
		bool setListenersList(NetUser netuser, List<NetUser> list, Plugin plugin) {
			if(getCP(netuser) is ChatPerson) {
				
				ChatPerson cp = (ChatPerson)getCP(netuser);
				if(cp.listenersplugin == null) {
					cp.listenersplugin = plugin;
					cp.listoflisteners = list;
					return false;
				}
			}
			return false;
		}
		
		bool resetListenersList(NetUser netuser, Plugin plugin) {
			if(getCP(netuser) is ChatPerson) {
				ChatPerson cp = (ChatPerson)getCP(netuser);
				if(cp.listenersplugin == null) {
					cp.listoflisteners.Clear();
					return true;
				} else {
					if(plugin.Name.Equals(cp.listenersplugin.Name)) {
						cp.listoflisteners.Clear();
						return true;
					}
				}
				
			}
			return false;
		}
		
	}
}