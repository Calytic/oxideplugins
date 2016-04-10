using System.Collections.Generic;
using System;
using System.Reflection;
using System.Data;
using UnityEngine;
using Oxide.Core;
using Oxide.Core.Plugins;
using RustProto;
using Oxide.Core.Libraries; 

namespace Oxide.Plugins  
{


    [Info("Easy Chat", "PaiN", 1.1, ResourceId = 1115)]
    [Description("A chat modification plugin.")]
    class EasyChat : RustLegacyPlugin
    {
		private bool Changed;
		private string oprefix;
		private string omsgcolor;
		private string ogpermission;
		private string mprefix;
		private string mmsgcolor;
		private string mgpermission;
		private string vprefix;
		private string vmsgcolor; 
		private string vgpermission;
		private bool debug = false;
		
		void Loaded()
        {
			LoadVariables();
			foreach (var group in Config)
            {
 
	
                string gname = group.Key.ToString();
 
             
				if(!permission.PermissionExists(Config[gname, "Permission"].ToString())) permission.RegisterPermission(Config[gname, "Permission"].ToString(), this);
				if(debug == true) {Puts("Registered " + Config[gname, "Permission"].ToString());}

			}
			
        }
 
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
			oprefix = Convert.ToString(GetConfig("Owner", "Prefix", "[Owner]"));
			omsgcolor = Convert.ToString(GetConfig("Owner", "MessageColor", "[color red]"));
			ogpermission = Convert.ToString(GetConfig("Owner", "Permission", "owner_chat"));
			mprefix = Convert.ToString(GetConfig("Moderator", "Prefix", "[MOD]"));
			mmsgcolor = Convert.ToString(GetConfig("Moderator", "MessageColor", "[color yellow]"));
			mgpermission = Convert.ToString(GetConfig("Moderator", "Permission", "mod_chat"));
			vprefix = Convert.ToString(GetConfig("VIP", "Prefix", "[VIP]"));
			vmsgcolor = Convert.ToString(GetConfig("VIP", "MessageColor", "[color aqua]"));
			vgpermission = Convert.ToString(GetConfig("VIP", "Permission", "vip_chat"));


            if (Changed)
            {
                SaveConfig();
                Changed = false;

            }

        }

        protected override void LoadDefaultConfig()
        {
            Puts("Creating a new configuration file!");
            Config.Clear();
            LoadVariables();
        }

		
		bool OnPlayerChat(NetUser netuser, string message)
		{
			string name = rust.QuoteSafe(netuser.displayName);
			string msg = rust.QuoteSafe(message); 
			var simplemute = plugins.Find("simplemute");
			
			
			if(simplemute != null)
			{
				bool isMuted = (bool) simplemute.Call("isMuted", netuser);
				if(isMuted) return false;
			}
			
			foreach(var group in Config)
			{ 
				string gname = group.Key;
				if (permission.UserHasPermission(netuser.userID.ToString(), Config[gname, "Permission"].ToString()))
				{
					name = rust.QuoteSafe(Config[gname, "Prefix"].ToString() + netuser.displayName);
					msg = rust.QuoteSafe(Config[gname, "MessageColor"].ToString() + message);
				}

			} 
				Puts(name + ": " + message);
				ConsoleNetworker.Broadcast(string.Concat("chat.add ", name, " ", msg));
				netuser.NoteChatted();
			return false;
		}
	}
}