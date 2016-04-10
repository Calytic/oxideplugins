using System.Collections.Generic;
using System;
using System.Reflection;
using System.Data;
using UnityEngine;
using Oxide.Core;
using Oxide.Core.Plugins;

using CodeHatch.Networking.Events;
using CodeHatch.Networking.Events.Entities;
using CodeHatch.Engine.Networking;
using CodeHatch.Common;
using CodeHatch.Permissions;
 
namespace Oxide.Plugins
{
    [Info("Warp System", "PaiN", 0.2, ResourceId = 1398)] 
    [Description("Create warp points for players.")]
    class WarpSystem : ReignOfKingsPlugin 
    { 
		private bool Changed;
		private int cooldown;
		private int warpbacktimer;
		private bool enablecooldown;
		private string backtolastloc;
		private string warplist;
		private string therealreadyis;
		private string warpadded;
		private string youhavetowait; 
		private string youhaveteleportedto;
		private string teleportingto;
		private string youhaveremoved;
			
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
			warpbacktimer = Convert.ToInt32(GetConfig("Settings", "WarpBackTimer", 5));
			cooldown = Convert.ToInt32(GetConfig("Settings", "Cooldown", 120));
			enablecooldown = Convert.ToBoolean(GetConfig("Settings", "EnableCooldown", true));
			backtolastloc = Convert.ToString(GetConfig("Messages", "TELEPORTED_TO_LAST_LOCATION", "You have teleported back to your last location!"));
			warplist = Convert.ToString(GetConfig("Messages", "WARP_LIST", "Warp ID: [91FFB5]{2}[FFFFFF] Warp Name: [00FFFF]{0}[FFFFFF] for Permission:[FF8C00] {1} [FFFFFF]"));
			therealreadyis = Convert.ToString(GetConfig("Messages", "WARP_EXISTS", "This warp already exists!"));
			warpadded = Convert.ToString(GetConfig("Messages", "WARP_ADDED", "Warp added with Warp Name: [91FFB5]{0}[FFFFFF]"));
			youhavetowait = Convert.ToString(GetConfig("Messages", "COOLDOWN_MESSAGE", "You have to wait [91FFB5]{0}[FFFFFF] second(s) before you can teleport again."));
			youhaveteleportedto = Convert.ToString(GetConfig("Messages", "TELEPORTED_TO", "You have teleported to [91FFB5]{0}[FFFFFF]"));
			teleportingto = Convert.ToString(GetConfig("Messages", "TELEPORTING_IN_TO", "Teleporting in [FF8C00]{0}[FFFFFF] second(s) to [91FFB5]{1}[FFFFFF]"));
			youhaveremoved = Convert.ToString(GetConfig("Messages", "WARP_REMOVED", "You have removed the warp [91FFB5]{0}[FFFFFF]"));
			
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
		
			class StoredData
			{
				public List<WarpInfo> WarpInfo = new List<WarpInfo>{};
				public Dictionary<ulong, float> cantele = new Dictionary<ulong, float>();
				public Dictionary<ulong, OldPosInfo> lastposition = new Dictionary<ulong, OldPosInfo>();
				
			} 
			
			class OldPosInfo
			{
				public float OldX;
				public float OldY;
				public float OldZ;
				
				public OldPosInfo(float x, float y, float z)
				{
					OldX = x;
					OldY = y; 
					OldZ = z;
				}
				
				public OldPosInfo()
				{
				}
			}
			class WarpInfo
			{
				public string WarpName;
				public int WarpId;
				public float WarpX;
				public float WarpY; 
				public float WarpZ; 
				public string WarpPermissionGroup;
				public int WarpTimer;
				public string WarpCreatorName;
				public int RandomRange;
				
				public WarpInfo(string name, Player player, int timerp, string permissionp, int warpnum, int randomr)
				{
					WarpName =  name; 
					WarpId = warpnum;
					WarpX = player.Entity.Position.x;
					WarpY = player.Entity.Position.y;
					WarpZ = player.Entity.Position.z;
					WarpCreatorName = player.DisplayName;
					WarpTimer = timerp;
					WarpPermissionGroup = permissionp;
					RandomRange = randomr;
				}
				
				public WarpInfo()
				{ 
				}
			}
			
			StoredData storedData;
			
			void Loaded()
			{
				storedData = Interface.GetMod().DataFileSystem.ReadObject<StoredData>("WarpSystem"); 
				if (!permission.PermissionExists("warp.admin")) permission.RegisterPermission("warp.admin", this);
				if (!permission.PermissionExists("canback")) permission.RegisterPermission("canback", this);
				LoadVariables();
				foreach(WarpInfo info in storedData.WarpInfo)
				{
						if(!permission.GroupExists(info.WarpPermissionGroup)) permission.CreateGroup(info.WarpPermissionGroup, "", 0);
				} 
			}
			
			
			
			int GetNewId()
			{
				
				int id = 0;
				foreach(WarpInfo info in storedData.WarpInfo)
				{
					id = Math.Max(0, info.WarpId);
				}
				return id + 1;
			}
			/*int GetRandomId(Player player)
			{
				int randomid = 0;
				foreach(WarpInfo info in storedData.WarpInfo)
				{
					if(permission.UserHasGroup(player.Id.ToString(), info.WarpPermissionGroup) || info.WarpPermissionGroup == "all")
					{
						randomid = UnityEngine.Random.Range(0, Math.Max(0, info.WarpId));
					}
				}
				return randomid + 1;
			}*/
			[ChatCommand("warp")]
			void cmdWarp(Player player, string cmdd, string[] args)
			{  
				if(args.Length == 0)
				{
					if(permission.UserHasPermission(player.Id.ToString(), "warp.admin"))
					{
						SendReply(player, "[91FFB5]Available Commands[FFFFFF]");
						SendReply(player, "[91FFB5]-[FFFFFF] /warp <add> <WarpName> <WarpTimer> <WarpRange> <WarpPermissionGroup>");
						SendReply(player, "[91FFB5]-[FFFFFF] /warp limit");
						//SendReply(player, "[91FFB5]-[FFFFFF] /warp random");
						SendReply(player, "[91FFB5]-[FFFFFF] /warp remove <WarpName>"); 
						SendReply(player, "[91FFB5]-[FFFFFF] /warp wipe");
						SendReply(player, "[91FFB5]-[FFFFFF] /warp list");
						SendReply(player, "[91FFB5]-[FFFFFF] /warp to <WarpName> || /warp list");
						/*SendReply(player, "[91FFB5]-[FFFFFF] /<WarpName> => A shorter version of /warp to <WarpName> || /warp list");
						SendReply(player, "[91FFB5]Teleport all online players[FFFFFF]: \n[91FFB5]-[FFFFFF] /warp all <WarpName>");
						SendReply(player, "[91FFB5]Teleport all sleepers[FFFFFF]: \n[91FFB5]-[FFFFFF] /warp all sleepers <WarpName>");*/
					}
					else 
					{
						SendReply(player, "[91FFB5]Available Commands[FFFFFF]");
						SendReply(player, "[91FFB5]-[FFFFFF] /warp list");
						SendReply(player, "[91FFB5]-[FFFFFF] /warp limit");
						//SendReply(player, "[91FFB5]-[FFFFFF] /warp random");
						SendReply(player, "[91FFB5]-[FFFFFF] /warp to <WarpName> || /warp list");
						//SendReply(player, "[91FFB5]-[FFFFFF] /<WarpName> => A shorter version of /warp to <WarpName> || /warp list");
					}
					return;
				}
				ulong steamId = player.Id;
				float nextteletime;
				switch(args[0])
				{
					case "limit":
					SendReply(player, "[91FFB5]Current Warp Limits[FFFFFF]");

					if (storedData.cantele.TryGetValue(steamId, out nextteletime))
					{ 
						int nexttele = Convert.ToInt32(nextteletime - Time.realtimeSinceStartup);
						if(nexttele <= 0)
						{
							nexttele = 0;
						}
						SendReply(player, $"You will be able to warp again in {nexttele.ToString()} seconds");
					}
					SendReply(player, $"Warp Cooldown: <color=orage>{cooldown.ToString()}[FFFFFF]");
					SendReply(player, $"Warp Cooldown Enabled: <color=orage>{enablecooldown.ToString()}[FFFFFF]");
					SendReply(player, "[91FFB5]*************[FFFFFF]");
					break;
					case "back":
					if(permission.UserHasPermission(player.Id.ToString(), "canback"))
					{
						SendReply(player, "Teleporting to you last saved locations in {0} seconds.", warpbacktimer.ToString());
						timer.Once(warpbacktimer, () => {
						ForcePlayerPos(player, new Vector3(storedData.lastposition[steamId].OldX, storedData.lastposition[steamId].OldY, storedData.lastposition[steamId].OldZ)); 
						SendReply(player, backtolastloc);
						storedData.lastposition.Remove(steamId);
						Interface.GetMod().DataFileSystem.WriteObject("WarpSystem", storedData);
						});
					}
					break;
					
					/*case "random":
					player.SendConsoleCommand($"chat.say \"/warp to {GetRandomId(player).ToString()}\" ");
					break;*/
					
					/*case "all":
					if(!permission.UserHasPermission(player.userID.ToString(), "warp.admin"))
					{
						SendReply(player, "You do not have permission to use this command!");
						return; 
					}
					if(args.Length == 2)
					{
						foreach(BasePlayer current in BasePlayer.activePlayerList)
						{
							foreach(WarpInfo info in storedData.WarpInfo)
							{
								if(info.WarpName.ToString().ToLower() == args[1].ToString().ToLower() || info.WarpId.ToString() == args[1].ToString())
								{
									ForcePlayerPos(current, new Vector3(info.WarpX, info.WarpY, info.WarpZ)); 
									SendReply(current, "You got teleported to [91FFB5]" + info.WarpName + "[FFFFFF] by [FF8C00]" + player.displayName + "[FFFFFF]");
								
								}
							}
						}
					}
					else if(args.Length == 3 && args[1].ToString() == "sleepers")
					{
						foreach(Player sleepers in BasePlayer.sleepingPlayerList)
						{
							foreach(WarpInfo info in storedData.WarpInfo)
							{
								if(info.WarpName.ToString().ToLower() == args[2].ToString().ToLower() || info.WarpId.ToString() == args[2].ToString())
								{
									ForcePlayerPos(sleepers, new Vector3(info.WarpX, info.WarpY, info.WarpZ)); 
									//SendReply(player, "You got teleported to [91FFB5]" + info.WarpName + "[FFFFFF] by [FF8C00]" + player.displayName + "[FFFFFF]");
								
								}
							}
						}
					}
					else
					{
						SendReply(player, "[91FFB5]Teleport all online players[FFFFFF]: \n /warp all <WarpName>");
						SendReply(player, "[91FFB5]Teleport all sleepers[FFFFFF]: \n /warp all sleepers <WarpName>");
						return;
					}
					break;*/
					case "wipe":
					if(!permission.UserHasPermission(player.Id.ToString(), "warp.admin"))
					{
						SendReply(player, "You do not have permission to use this command!");
						return;
					}
						storedData.WarpInfo.Clear();
						storedData.cantele.Clear();
						Interface.GetMod().DataFileSystem.WriteObject("WarpSystem", storedData);
					SendReply(player, "You have wiped all the teleports!");
					break;
					
					case "list":
						SendReply(player, "[91FFB5]Current Warps[FFFFFF]");
						foreach(WarpInfo info in storedData.WarpInfo)
						{
							if(permission.UserHasGroup(steamId.ToString(), info.WarpPermissionGroup) || info.WarpPermissionGroup == "all")
							{
								SendReply(player, warplist.ToString(), info.WarpName, info.WarpPermissionGroup, info.WarpId);
							}
							
						}
						SendReply(player, "[91FFB5]*************[FFFFFF]");
					break;
					
					case "add":
					
					if(!permission.UserHasPermission(player.Id.ToString(), "warp.admin"))
					{
						SendReply(player, "You do not have permission to use this command!");
						return;
					}
					if(args.Length != 5)
					{
						SendReply(player, "/warp <add> <WarpName> <WarpTimer> <WarpRange> <WarpPermissionGroup>");
						return;
					}   
					foreach(WarpInfo info in storedData.WarpInfo)
					{ 
						if(args[1].ToString().ToLower() == info.WarpName.ToString().ToLower())
						{
							SendReply(player, therealreadyis.ToString());
							return;
						}
					} 
					string permissionp = args[4];
					string name = args[1];
					int warpnum;
					int timerp = Convert.ToInt32(args[2]); 
					int randomr = Convert.ToInt32(args[3]);
					if(storedData.WarpInfo == null)
					{
						warpnum = 1;
					}
					else
					{
						warpnum = GetNewId();
					}
					var data = new WarpInfo(name, player, timerp, permissionp, warpnum, randomr);
					storedData.WarpInfo.Add(data);
					SendReply(player, warpadded, name.ToString());
					Interface.GetMod().DataFileSystem.WriteObject("WarpSystem", storedData);
					if(!permission.GroupExists(args[3])) permission.CreateGroup(args[3], "", 0);
					break;
					
					case "to":
					if(args.Length != 2)
					{
						SendReply(player, "/warp to <WarpName> || /warplist");
						return;
					} 
					foreach(WarpInfo info in storedData.WarpInfo)
					{ 
						if(info.WarpName.ToString().ToLower() == args[1].ToString().ToLower() || info.WarpId.ToString() == args[1].ToString())
						{
							if(info.WarpPermissionGroup == "all" || permission.UserHasGroup(steamId.ToString(), info.WarpPermissionGroup))
							{
								
								if(enablecooldown == true) 
								{
									if (storedData.cantele.TryGetValue(steamId, out nextteletime))
									{  
										if(Time.realtimeSinceStartup >= nextteletime)
										{
											
											storedData.cantele[steamId] = Time.realtimeSinceStartup + cooldown;
											Interface.GetMod().DataFileSystem.WriteObject("WarpSystem", storedData);
											goto Finish;
										} 
										else
										{
											int nexttele = Convert.ToInt32(nextteletime - Time.realtimeSinceStartup);
											SendReply(player, youhavetowait, nexttele.ToString());
											return;
										}
									}
									else
									{
										storedData.cantele.Add(steamId, Time.realtimeSinceStartup + cooldown);
										Interface.GetMod().DataFileSystem.WriteObject("WarpSystem", storedData);
										goto Finish;
									}
								}
								Finish: 
								if(storedData.lastposition.ContainsKey(steamId) |! storedData.lastposition.ContainsKey(steamId))
								{
									storedData.lastposition.Remove(steamId);
									Interface.GetMod().DataFileSystem.WriteObject("WarpSystem", storedData);
									float x = player.Entity.Position.x; 
									float y = player.Entity.Position.y;
									float z = player.Entity.Position.z;
									var oldinfo = new OldPosInfo(x, y, z);
									storedData.lastposition.Add(steamId, oldinfo);
									Interface.GetMod().DataFileSystem.WriteObject("WarpSystem", storedData);
									
								}
									
								SendReply(player, teleportingto,info.WarpTimer, info.WarpName);
								timer.Once(info.WarpTimer, () => { 
								int posx = UnityEngine.Random.Range(Convert.ToInt32(info.WarpX), info.RandomRange);
								int posz = UnityEngine.Random.Range(Convert.ToInt32(info.WarpZ), info.RandomRange);
								if(info.RandomRange == 0)
								{
									ForcePlayerPos(player, new Vector3(info.WarpX, info.WarpY, info.WarpZ));
								}
								else
									ForcePlayerPos(player, new Vector3(posx, info.WarpY, posz)); 
									SendReply(player, youhaveteleportedto, info.WarpName);
								});												 
							}
							else
							{
								SendReply(player, "You are not allowed to use this warp!");
								return; 
							}
						}
					}
					break;
					case "help":
					if(permission.UserHasPermission(player.Id.ToString(), "warp.admin"))
					{
						SendReply(player, "[91FFB5]Available Commands[FFFFFF]");
						SendReply(player, "[91FFB5]-[FFFFFF] /warp <add> <WarpName> <WarpTimer> <WarpRange> <WarpPermissionGroup>");
						SendReply(player, "[91FFB5]-[FFFFFF] /warp limit");
						//SendReply(player, "[91FFB5]-[FFFFFF] /warp random");
						SendReply(player, "[91FFB5]-[FFFFFF] /warp remove <WarpName>"); 
						SendReply(player, "[91FFB5]-[FFFFFF] /warp wipe");
						SendReply(player, "[91FFB5]-[FFFFFF] /warp list");
						SendReply(player, "[91FFB5]-[FFFFFF] /warp to <WarpName> || /warp list");
						/*SendReply(player, "[91FFB5]-[FFFFFF] /<WarpName> => A shorter version of /warp to <WarpName> || /warp list");
						SendReply(player, "[91FFB5]Teleport all online players[FFFFFF]: \n[91FFB5]-[FFFFFF] /warp all <WarpName>");
						SendReply(player, "[91FFB5]Teleport all sleepers[FFFFFF]: \n[91FFB5]-[FFFFFF] /warp all sleepers <WarpName>");*/
					}
					else 
					{
						SendReply(player, "[91FFB5]Available Commands[FFFFFF]");
						SendReply(player, "[91FFB5]-[FFFFFF] /warp list");
						SendReply(player, "[91FFB5]-[FFFFFF] /warp limit");
						//SendReply(player, "[91FFB5]-[FFFFFF] /warp random");
						SendReply(player, "[91FFB5]-[FFFFFF] /warp to <WarpName> || /warp list");
						//SendReply(player, "[91FFB5]-[FFFFFF] /<WarpName> => A shorter version of /warp to <WarpName> || /warp list");
					}
					break;
					case "remove":
					if(!permission.UserHasPermission(player.Id.ToString(), "warp.admin"))
					{
						SendReply(player, "You do not have permission to use this command!");
						return;
					}
					if(args.Length != 2) 
					{
						SendReply(player, "/warp remove <WarpName>");
						return;
					}
					foreach(WarpInfo info in storedData.WarpInfo)
					{
						if(info.WarpName.ToString() == args[1].ToString())
						{
							storedData.WarpInfo.Remove(info);
							SendReply(player, youhaveremoved, info.WarpName);
							Interface.GetMod().DataFileSystem.WriteObject("WarpSystem", storedData);
							break;
						}
					}
					break;
					
				}
			}
			void Unloaded() 
			{
				storedData.cantele.Clear();
				Interface.GetMod().DataFileSystem.WriteObject("WarpSystem", storedData); 
			}
			void ForcePlayerPos(Player player, Vector3 xyz)
			{
				EventManager.CallEvent((BaseEvent)new TeleportEvent(player.Entity, xyz));
			}
	}
}