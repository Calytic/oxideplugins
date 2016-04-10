using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using Oxide.Core;
using System;

namespace Oxide.Plugins
{
    [Info("Admin Toggle", "LaserHydra", "1.0.2", ResourceId = 1371)]
    [Description("Toggle your admin status")]
    class AdminToggle : RustPlugin
    {
		readonly FieldInfo displayname = typeof(BasePlayer).GetField("_displayName", (BindingFlags.Instance | BindingFlags.NonPublic));
		
		class Data
		{
			public Dictionary<string, AdminData> AdminData = new Dictionary<string, AdminData>();
		}
		
		Data data;
		
		class AdminData
		{
			public string PlayerName;
			public string AdminName;
			public bool EnabledPlayerMode;
			
			public AdminData(BasePlayer player)
			{
				PlayerName = "";
				AdminName = player.displayName;
				EnabledPlayerMode = false;
			}
			
			public AdminData()
			{
			}
		}
		
		void Loaded()
		{
			if(!permission.PermissionExists("admin.toggle")) permission.RegisterPermission("admin.toggle", this);
			data = Interface.GetMod().DataFileSystem.ReadObject<Data>("AdminToggle_Data");
			LoadConfig();
		}
		
		void LoadConfig()
		{
			SetConfig("Groups", "Admin Group", "admin");
			SetConfig("Groups", "Player Group", "player");
		}
		
		protected override void LoadDefaultConfig()
		{
			Puts("Generating new config file...");
			LoadConfig();
		}
		
		void SaveData() => Interface.GetMod().DataFileSystem.WriteObject("AdminToggle_Data", data);
		
		void OnPlayerInit(BasePlayer player)
		{
			if(!data.AdminData.ContainsKey(player.userID.ToString())) return;
			
			if(data.AdminData[player.userID.ToString()].EnabledPlayerMode)
			{
				displayname.SetValue(player, data.AdminData[player.userID.ToString()].PlayerName);
			}
		}
		
		bool CheckPermission(BasePlayer player)
		{
			if(permission.UserHasPermission(player.userID.ToString(), "admin.toggle")) return true;
			else SendChatMessage(player, "AdminToggle", "You have no permission to use this command.");
			return false;
		}
		
		[ChatCommand("setplayername")]
		void SetPlayerName(BasePlayer player, string cmd, string[] args)
		{
			if(!CheckPermission(player)) return;
			
			if(args.Length != 1)
			{
				SendChatMessage(player, "AdminToggle", "Syntax: /setplayername <Name>");
				return;
			}
			
			if(!data.AdminData.ContainsKey(player.userID.ToString())) data.AdminData.Add(player.userID.ToString(), new AdminData(player));
			
			data.AdminData[player.userID.ToString()].PlayerName = args[0] ?? "";
			SaveData();
			
			SendChatMessage(player, "AdminToggle", $"You have set your player name to: <color=#00FF8D>{args[0]}</color>");
		}
		
		[ChatCommand("toggleadmin")]
		void ToggleAdmin(BasePlayer player)
		{
			if(!CheckPermission(player)) return;
			
			if(!data.AdminData.ContainsKey(player.userID.ToString())) data.AdminData.Add(player.userID.ToString(), new AdminData(player));
			
			if(data.AdminData[player.userID.ToString()].PlayerName == "")
			{
				SendChatMessage(player, "AdminToggle", "You did not set up your player name yet. Set it up using /setplayername");
				return;
			}
			
			displayname.SetValue(player, data.AdminData[player.userID.ToString()].EnabledPlayerMode ? data.AdminData[player.userID.ToString()].AdminName : data.AdminData[player.userID.ToString()].PlayerName);
			
			if(data.AdminData[player.userID.ToString()].EnabledPlayerMode)
			{
				ServerUsers.Set(player.userID, ServerUsers.UserGroup.Owner, player.displayName, "");
				ConVar.Server.writecfg(new ConsoleSystem.Arg(""));
				
				permission.AddUserGroup(player.userID.ToString(), Config["Groups", "Admin Group"].ToString());
				permission.RemoveUserGroup(player.userID.ToString(), Config["Groups", "Player Group"].ToString());
				
				SendChatMessage(player, "AdminToggle", "You switched to admin mode!");
			}
			else
			{
				ServerUsers.Set(player.userID, ServerUsers.UserGroup.None, player.displayName, "");
				ConVar.Server.writecfg(new ConsoleSystem.Arg(""));
				
				permission.AddUserGroup(player.userID.ToString(), Config["Groups", "Player Group"].ToString());
				permission.RemoveUserGroup(player.userID.ToString(), Config["Groups", "Admin Group"].ToString());
				
				
				SendChatMessage(player, "AdminToggle", "You switched to player mode!");
			}
			
			data.AdminData[player.userID.ToString()].EnabledPlayerMode = !data.AdminData[player.userID.ToString()].EnabledPlayerMode;
			SaveData();
			
			SendChatMessage(player, "AdminToggle", "You will be kicked in 5 seconds to update your status. Please reconnect!");
			timer.Once(5, () => player.SendConsoleCommand("client.disconnect"));
		}
		
        #region UsefulMethods
        //--------------------------->   Player finding   <---------------------------//

		BasePlayer GetPlayer(string searchedPlayer, BasePlayer executer, string prefix)
        {
            BasePlayer targetPlayer = null;
            List<string> foundPlayers = new List<string>();
            string searchedLower = searchedPlayer.ToLower();
            
			foreach(BasePlayer player in BasePlayer.activePlayerList)
			{
				if(player.displayName.ToLower().Contains(searchedLower)) foundPlayers.Add(player.displayName);
			} 
			
			switch(foundPlayers.Count)
			{
				case 0:
					SendChatMessage(executer, prefix, "The Player can not be found.");
					break;
					
				case 1:
					targetPlayer = BasePlayer.Find(foundPlayers[0]);
					break;
				
				default:
					string players = ListToString(foundPlayers, 0, ", ");
					SendChatMessage(executer, prefix, "Multiple matching players found: \n" + players);
					break;
			}
			
            return targetPlayer;
        }
		
		//---------------------------->   Converting   <----------------------------//

        string ListToString(List<string> list, int first, string seperator) 
		{
			return String.Join(seperator, list.Skip(first).ToArray());
		}

        //------------------------------>   Config   <------------------------------//

        void SetConfig(string Arg1, object Arg2, object Arg3 = null, object Arg4 = null)
		{
			if(Arg4 == null) 
			{
				Config[Arg1, Arg2.ToString()] = Config[Arg1, Arg2.ToString()] ?? Arg3;
			}
			else if(Arg3 == null) 
			{
				Config[Arg1] = Config[Arg1] ?? Arg2;
			}
			else
			{
				Config[Arg1, Arg2.ToString(), Arg3.ToString()] = Config[Arg1, Arg2.ToString(), Arg3.ToString()] ?? Arg4;
			} 
		}

        //---------------------------->   Chat Sending   <----------------------------//

        void BroadcastChat(string prefix, string msg = null) => PrintToChat(msg == null ? prefix : "<color=#00FF8D>" + prefix + "</color>: " + msg);

        void SendChatMessage(BasePlayer player, string prefix, string msg = null) => SendReply(player, msg == null ? prefix : "<color=#00FF8D>" + prefix + "</color>: " + msg);

        //---------------------------------------------------------------------------//
        #endregion
    }
}
