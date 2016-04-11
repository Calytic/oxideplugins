// Reference: Newtonsoft.Json

using System.Collections.Generic;
using System.Text.RegularExpressions;
using System;
using System.Data;
using System.Reflection;
using Oxide.Core;
using Oxide.Core.Libraries;
using Oxide.Core.Configuration;
using Newtonsoft.Json;

namespace Oxide.Plugins
{
    [Info("PlayerInfo", "UraniumRUST", 1.1)]
    [Description("Get some player's informations")]
    public class PlayerInfo : RustLegacyPlugin
    {
        JsonSerializerSettings jsonsettings;
        
        private Core.Configuration.DynamicConfigFile Data;     
        void LoadData() {Data = Interface.GetMod().DataFileSystem.GetDatafile("PlayerInfo");}
        void SaveData() {Interface.GetMod().DataFileSystem.SaveDatafile("PlayerInfo"); }
        void Unload() { SaveData(); }
		void OnServerSave(){ SaveData(); }
        void LoadDefaultConfig() { }
        
        public static string prefix = "Oxide";
        public static bool logall = true;
		
        void LoadDefaultMessages()
        {
            var messages = new Dictionary<string, string>
            {
                {"NotAllowed", "[color red]You are not allowed to use this command."},
                {"NoPlayer", "[color red]No player found with this name."},
                {"LogPlayers", "[color orange]Logall players is disabled, enable it to use offline option command."},
                {"LocalNetwork", "[color red]You will get erros using this on a local network."},
                {"OnlinePlayerM1", "[color green]Name: [color orange]{0}[color white] | [color green]IP: [color orange]{1}[color white] | [color green]ID: [color orange]{2}"},
                {"OnlinePlayerM2", "[color red][color green]Country: [color orange]{0}[color white] | [color green]Hostname: [color orange]{1}[color white]"},
                {"OnlinePlayerM3", "[color green]Location: [color orange]{0}/{1}[color white]"},
                {"OfflinePlayerM1", "[color orange]The player is currentily [color red]offline[color orange] but we managed to gather some information about him."},
                {"OfflinePlayerM2", "[color green]Name: [color orange]{0}[color white] | [color green]ID:[color orange] {1} [color white]"},// Changed in 1.1
                {"OfflinePlayerM3", "[color green]IP: [color orange]{0}[color white] | [color green]Last Seen:[color orange] {1} [color white]"},  // Added in 1.1
                {"TimeNow", "[color green]Time Now: [color orange]{0}[color white]"}, // Added in 1.1
                {"HelpText1", "[color orange]Use /info <player>"},// Added on Lang API in 1.1
                {"HelpText2", "[color orange]Use /info time => To see the current real time."} // Added in 1.1
            };
            lang.RegisterMessages(messages, this);
        }
        
        void Loaded()
        {			
			LoadData();
            LoadDefaultMessages();
            permission.RegisterPermission("playerinfo.allowed", this);
        }
        
        void Init()
        {
			CheckCfg<string>("Chat Prefix", ref prefix);
            CheckCfg<bool>("Log some player's information on connect", ref logall);
            SaveConfig();
        }
        
        bool hasAccess(NetUser netuser, string permissionname)
        {
            if (netuser.CanAdmin()) return true;
			if (permission.UserHasPermission(netuser.playerClient.userID.ToString(), "playerinfo.allowed")) return true;
            return permission.UserHasPermission(netuser.playerClient.userID.ToString(), permissionname);
        }
        
        private void CheckCfg<T>(string Key, ref T var)
        {
            if (Config[Key] is T)
                var = (T)Config[Key];
            else
                Config[Key] = var;
        }
        
        
        Dictionary<string, object> GetPlayerdata(string userid)
        {
            if (Data[userid] == null)
                Data[userid] = new Dictionary<string, object>();
            return Data[userid] as Dictionary<string, object>;
        }
        
        void OnPlayerConnected(NetUser netuser)
        {
            if(logall)
            {
                var playerlog = GetPlayerdata(netuser.displayName);
                if(playerlog.ContainsKey("ID"))
				{
					playerlog.Remove("ID");
					playerlog.Remove("Nome");
					playerlog.Remove("IP");
                    if(playerlog.ContainsKey("Hora")) { playerlog.Remove("Hora"); } // Added in 1.1
				}
				long x= DateTime.Now.ToBinary();
                DateTime d= DateTime.FromBinary(x);
                var time = d.ToString();
                playerlog.Add("ID", netuser.playerClient.userID.ToString());
				playerlog.Add("Nome", netuser.displayName);
				playerlog.Add("IP", netuser.networkPlayer.externalIP);
                playerlog.Add("Hora", time); // Added in 1.1
            }
        }
              
        [ChatCommand("info")]
        private void IPCommand(NetUser netuser, string command, string[] args)
        {
         if(!hasAccess(netuser, "playerinfo.allowed")) { rust.SendChatMessage(netuser, prefix, GetMessage("NotAllowed", netuser.userID.ToString())); return; }
            long x= DateTime.Now.ToBinary();
            DateTime d= DateTime.FromBinary(x);
            var hour = d.ToString();
        if(args.Length == 0)
        {
            var semismallline = "[color red]---------------------------------------------------------------";
            rust.SendChatMessage(netuser, prefix, semismallline);
            rust.SendChatMessage(netuser, prefix, GetMessage("HelpText1", netuser.userID.ToString()));
            rust.SendChatMessage(netuser, prefix, GetMessage("HelpText2", netuser.userID.ToString())); // Added in 1.1 
            rust.SendChatMessage(netuser, prefix, semismallline);
            return;
        }
        if (args.Length == 1 && args[0].ToString() == "time") // Added in 1.1
        {
            var smallline = "[color red]---------------------------------------------------";
            rust.SendChatMessage(netuser, prefix, smallline);
            rust.SendChatMessage(netuser, prefix, string.Format(GetMessage("TimeNow", netuser.userID.ToString()), hour));
            rust.SendChatMessage(netuser, prefix, smallline);
            return;
        }
            var line = "[color red]--------------------------------------------------------------------------------------------------------";
            NetUser targetuser = rust.FindPlayer(args[0]);
            //------------------Player off
            if(targetuser == null)
                {
                    if(!logall) { rust.SendChatMessage(netuser, prefix, GetMessage("LogPlayers", netuser.userID.ToString())); return;}
                    var playerdata = GetPlayerdata(args[0]);      
                    if(playerdata.ContainsKey("Nome"))
                    {
                        
                        rust.SendChatMessage(netuser, prefix, line);
                        rust.SendChatMessage(netuser, prefix, GetMessage("OfflinePlayerM1", netuser.userID.ToString()));
                        rust.SendChatMessage(netuser, prefix, string.Format(GetMessage("OfflinePlayerM2", netuser.userID.ToString()), playerdata["Nome"].ToString(), playerdata["ID"].ToString())); // Changed in 1.1
                        if(playerdata.ContainsKey("Hora")) 
                        {

                            rust.SendChatMessage(netuser, prefix, string.Format(GetMessage("OfflinePlayerM3", netuser.userID.ToString()), playerdata["IP"].ToString(), playerdata["Hora"].ToString())); // Added in 1.1
                        }
                        else
                        {
                            var nlast = "We don't have this information! Sorry!";
                            rust.SendChatMessage(netuser, prefix, string.Format(GetMessage("OfflinePlayerM3", netuser.userID.ToString()), playerdata["IP"].ToString(), nlast)); // Added in 1.1
                        }
                        rust.SendChatMessage(netuser, prefix, string.Format(GetMessage("TimeNow", netuser.userID.ToString()), hour)); // Added in 1.1
                        rust.SendChatMessage(netuser, prefix, line);
                        return;
                    }
                rust.SendChatMessage(netuser, prefix, GetMessage("NoPlayer", netuser.userID.ToString()));
                return;
                }
           //-------------------Player off 
            var ip = targetuser.networkPlayer.externalIP;
            if(ip == "127.0.0.1") { rust.SendChatMessage(netuser, prefix, GetMessage("LocalNetwork", netuser.userID.ToString()));  return;}
            var url = string.Format("http://ip-api.com/json/"+ip);
			Interface.GetMod().GetLibrary<WebRequests>("WebRequests").EnqueueGet(url, (code, response) =>
            { 
				var jsonresponse = JsonConvert.DeserializeObject<Dictionary<string, object>>(response, jsonsettings);
				var hostname = (jsonresponse["org"].ToString());
                var country = (jsonresponse["countryCode"].ToString());   
                var region = (jsonresponse["region"].ToString());
                var city = (jsonresponse["city"].ToString());
				var targetip = (jsonresponse["query"].ToString());
				var targetname = targetuser.displayName;
                var targetid = targetuser.playerClient.userID.ToString();
                
                rust.SendChatMessage(netuser, prefix, line);
                rust.SendChatMessage(netuser, prefix, string.Format(GetMessage("OnlinePlayerM1", netuser.userID.ToString()), targetname, targetip, targetid));
                rust.SendChatMessage(netuser, prefix, string.Format(GetMessage("OnlinePlayerM2", netuser.userID.ToString()), country, hostname));
                rust.SendChatMessage(netuser, prefix, string.Format(GetMessage("OnlinePlayerM3", netuser.userID.ToString()), city, region));
                rust.SendChatMessage(netuser, prefix, line);
                     
			}, this);
        }
        string GetMessage(string key, string steamId = null) => lang.GetMessage(key, this, steamId);
    }
}