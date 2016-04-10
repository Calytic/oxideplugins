using System.Collections.Generic;
using System;
using System.Reflection;
using System.Data;
using UnityEngine;
using Oxide.Core;
using Oxide.Core.Plugins;
using RustProto;
using System.Linq;
 
namespace Oxide.Plugins
{
    [Info("Warp System", "PaiN", 0.5, ResourceId = 1434)] 
    [Description("Create warp points for players.")]
    class WarpSystem : RustLegacyPlugin 
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
			warplist = Convert.ToString(GetConfig("Messages", "WARP_LIST", "Warp ID: [color cyan]{2}[color white] Warp Name: [color cyan]{0}[color white] Permission:[color orange] {1} [color white] MaxUses Remaining: [color lime]{3}[color white]"));
			therealreadyis = Convert.ToString(GetConfig("Messages", "WARP_EXISTS", "This warp already exists!"));
			warpadded = Convert.ToString(GetConfig("Messages", "WARP_ADDED", "Warp added with Warp Name: [color cyan]{0}[color white]"));
			youhavetowait = Convert.ToString(GetConfig("Messages", "COOLDOWN_MESSAGE", "You have to wait [color cyan]{0}[color white] second(s) before you can teleport again."));
			youhaveteleportedto = Convert.ToString(GetConfig("Messages", "TELEPORTED_TO", "You have teleported to [color cyan]{0}[color white]"));
			teleportingto = Convert.ToString(GetConfig("Messages", "TELEPORTING_IN_TO", "Teleporting in [color orange]{0}[color white] second(s) to [color cyan]{1}[color white]"));
			youhaveremoved = Convert.ToString(GetConfig("Messages", "WARP_REMOVED", "You have removed the warp [color cyan]{0}[color white]"));
			
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
				public Dictionary<ulong, Dictionary<string, int>> maxuses = new Dictionary<ulong, Dictionary<string, int>>();
				
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
				public int WarpMaxUses;
				public string WarpCreatorName;
				public int RandomRange;
				
				public WarpInfo(string name, NetUser player, int timerp, string permissionp, int warpnum, int randomr, int maxusess)
				{
					var cachedVector3 = player.playerClient.lastKnownPosition;
					WarpName =  name; 
					WarpId = warpnum;
					WarpX = cachedVector3.x; 
					WarpMaxUses = maxusess;
					WarpY = cachedVector3.y;
					WarpZ = cachedVector3.z;
					WarpCreatorName = player.displayName;
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
						cmd.AddChatCommand(info.WarpId.ToString(), this, "");
						cmd.AddChatCommand(info.WarpName, this, "");
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
			int GetRandomId(NetUser player)
			{
				int randomid = 0;
				foreach(WarpInfo info in storedData.WarpInfo)
				{
					if(permission.UserHasGroup(player.userID.ToString(), info.WarpPermissionGroup) || info.WarpPermissionGroup == "all")
					{
						randomid = UnityEngine.Random.Range(0, Math.Max(0, info.WarpId));
					}
				}
				return randomid + 1;
			}
			[ChatCommand("warp")]
			void cmdWarp(NetUser player, string cmdd, string[] args)
			{  
				if(args.Length == 0)
				{ 
					if(permission.UserHasPermission(player.userID.ToString(), "warp.admin"))
					{
						SendReply(player, "[color cyan]Available Commands[color white]");
						SendReply(player, "[color cyan]-[color white] /warp <add> <WarpName> <WarpTimer> <WarpRange> <WarpMaxUses> <WarpPermissionGroup>");
						SendReply(player, "[color cyan]-[color white] /warp limit");
						SendReply(player, "[color cyan]-[color white] /warp remove <WarpName>");
						SendReply(player, "[color cyan]-[color white] /warp wipe");
						SendReply(player, "[color cyan]-[color white] /warp list");
						SendReply(player, "[color cyan]-[color white] /warp to <WarpName> || /warp list");
						SendReply(player, "[color cyan]Teleport all online players[color white]: \n[color cyan]-[color white] /warp all <WarpName>");
					}
					else
					{
						SendReply(player, "[color cyan]Available Commands[color white]");
						SendReply(player, "[color cyan]-[color white] /warp list");
						SendReply(player, "[color cyan]-[color white] /warp limit");
						SendReply(player, "[color cyan]-[color white] /warp to <WarpName> || /warp list");
					}
					return;
				}
				ulong steamId = player.userID;
				float nextteletime;
				switch(args[0])
				{
					case "limit":
					SendReply(player, "[color cyan]Current Warp Limits[color white]");

					if (storedData.cantele.TryGetValue(steamId, out nextteletime))
					{ 
						int nexttele = Convert.ToInt32(nextteletime - Time.realtimeSinceStartup);
						if(nexttele <= 0)
						{
							nexttele = 0;
						}
						SendReply(player, $"You will be able to warp again in {nexttele.ToString()} seconds");
					}
					SendReply(player, $"Warp Cooldown: [color orange]{cooldown.ToString()}[color white]");
					SendReply(player, $"Warp Cooldown Enabled: [color orange]{enablecooldown.ToString()}[color white]");
					SendReply(player, "[color cyan]*************[color white]");
					break;
					case "back":
					if(permission.UserHasPermission(player.userID.ToString(), "canback"))
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
					
					case "all":
					if(!permission.UserHasPermission(player.userID.ToString(), "warp.admin"))
					{
						SendReply(player, "You do not have permission to use this command!");
						return; 
					}
					if(args.Length == 2)
					{
						foreach(PlayerClient current in PlayerClient.All)
						{
							foreach(WarpInfo info in storedData.WarpInfo)
							{
								if(info.WarpName.ToString().ToLower() == args[1].ToString().ToLower() || info.WarpId.ToString() == args[1].ToString())
								{
									var management = RustServerManagement.Get(); 
									management.TeleportPlayerToWorld(current.netPlayer, new Vector3(info.WarpX, info.WarpY, info.WarpZ));
									PrintToChat("Everyone got teleported to [color cyan]" + info.WarpName + "[color white] by [color orange]" + player.displayName + "[color white]");
								
								}
							}
						}
					}
					else
					{
						SendReply(player, "[color cyan]Teleport all online players[color white]: \n /warp all <WarpName, WarpId>");
						return;
					}
					break;
					case "wipe":
					if(!permission.UserHasPermission(player.userID.ToString(), "warp.admin"))
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
						SendReply(player, "[color cyan]Current Warps[color white]");
						string maxusesrem;
						foreach(WarpInfo info in storedData.WarpInfo)
						{
							if(permission.UserHasGroup(steamId.ToString(), info.WarpPermissionGroup) || info.WarpPermissionGroup == "all")
							{

								if(info.WarpMaxUses == 0)
								{
									maxusesrem = "[color red]UNLIMITED[color white]";
								}
								else if(!storedData.maxuses.ContainsKey(steamId))
								{
									maxusesrem = info.WarpMaxUses.ToString();
								}
								else
								maxusesrem = storedData.maxuses[steamId][info.WarpName].ToString();
								
								SendReply(player, warplist.ToString(), info.WarpName, info.WarpPermissionGroup, info.WarpId, maxusesrem.ToString());
								SendReply(player, "[color cyan]*************[color white]");
							}
							
						}
						SendReply(player, "[color cyan]*************[color white]");
					break;
					 
					case "add":
					
					if(!permission.UserHasPermission(player.userID.ToString(), "warp.admin"))
					{
						SendReply(player, "You do not have permission to use this command!");
						return;
					}
					if(args.Length != 6)
					{
						SendReply(player, "/warp <add> <WarpName> <WarpTimer> <WarpRange> <WarpMaxUses> <WarpPermissionGroup>");
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
					string permissionp = args[5];
					string name = args[1];
					int warpnum;
					int timerp = Convert.ToInt32(args[2]); 
					int randomr = Convert.ToInt32(args[3]);
					int maxusess = Convert.ToInt32(args[4]);
					if(storedData.WarpInfo == null)
					{
						warpnum = 1;
					}
					else
					{
						warpnum = GetNewId();
					}
					var data = new WarpInfo(name, player, timerp, permissionp, warpnum, randomr, maxusess);
					storedData.WarpInfo.Add(data);
					SendReply(player, warpadded, name.ToString());
					Interface.GetMod().DataFileSystem.WriteObject("WarpSystem", storedData);
					if(!permission.GroupExists(args[5])) permission.CreateGroup(args[5], "", 0);
					cmd.AddChatCommand(name.ToString(), this, "");
					cmd.AddChatCommand(warpnum.ToString(), this, "");
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
								if(info.WarpMaxUses > 0)
								{
									if(!storedData.maxuses.ContainsKey(steamId))
									{
										storedData.maxuses.Add(
										steamId,
										new Dictionary<string, int>{
											{info.WarpName, 1}
										}
									);
									}
									if(storedData.maxuses[steamId][info.WarpName] == 5)
									{
										SendReply(player, "You have reached the max uses for this Warp!");
										return;
									}
									if(storedData.maxuses.ContainsKey(steamId))
									{
										storedData.maxuses[steamId][info.WarpName] = storedData.maxuses[steamId][info.WarpName] + 1;
									}
								}
								
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
									var cachedVector3 = player.playerClient.lastKnownPosition;
									float x = cachedVector3.x; 
									float y = cachedVector3.y;
									float z = cachedVector3.z;
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
					if(permission.UserHasPermission(player.userID.ToString(), "warp.admin"))
					{
						SendReply(player, "[color cyan]Available Commands[color white]");
						SendReply(player, "[color cyan]-[color white] /warp <add> <WarpName> <WarpTimer> <WarpRange> <WarpMaxUses> <WarpPermissionGroup>");
						SendReply(player, "[color cyan]-[color white] /warp limit");
						SendReply(player, "[color cyan]-[color white] /warp remove <WarpName>");
						SendReply(player, "[color cyan]-[color white] /warp wipe");
						SendReply(player, "[color cyan]-[color white] /warp list");
						SendReply(player, "[color cyan]-[color white] /warp to <WarpName> || /warp list");
						SendReply(player, "[color cyan]Teleport all online players[color white]: \n[color cyan]-[color white] /warp all <WarpName>");
					}
					else
					{
						SendReply(player, "[color cyan]Available Commands[color white]");
						SendReply(player, "[color cyan]-[color white] /warp list");
						SendReply(player, "[color cyan]-[color white] /warp limit");
						SendReply(player, "[color cyan]-[color white] /warp to <WarpName> || /warp list");
					}
					break;
					case "remove":
					if(!permission.UserHasPermission(player.userID.ToString(), "warp.admin"))
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
			
			void ForcePlayerPos(NetUser player, Vector3 xyz)
			{
				var management = RustServerManagement.Get(); 
				management.TeleportPlayerToWorld(player.playerClient.netPlayer, xyz);
			}
	}
}