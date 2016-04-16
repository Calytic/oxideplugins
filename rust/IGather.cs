using Oxide.Core;
using System;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using ConVar;

namespace Oxide.Plugins
{
    [Info("Individual-Gather-Rate", "DylanSMR", "1.0.6", ResourceId = 1763)]
    [Description("Adds the ability to create gather groups!")]
    class IGather : RustPlugin
    {
        //////////////////////////////////////////////////////////////////////////////////////
        // Configuration File Handler
        //////////////////////////////////////////////////////////////////////////////////////  
        
        void LoadDefaultConfig()
        {
            Config.Clear();
                Config["ChatPrefix"] = "[IG]";
                Config["ChatPrefixColor"] = "#6f60c9";
                Config["ChatColor"] = "#6f60c9";
                Config["DefaultGroupQuarryX"] = 1;
                Config["DefaultGroupResourceX"] = 1;
                Config["DefaultGroupPickUpX"] = 1;
            Config.Save();
        }              
        
        //////////////////////////////////////////////////////////////////////////////////////
        // Data Group Handler
        //////////////////////////////////////////////////////////////////////////////////////        
        
        class StoredData
        {
            public Dictionary<int, GroupData> newGroup = new Dictionary<int, GroupData>();
            public Dictionary<ulong, PlayerData> Player = new Dictionary<ulong, PlayerData>();
            public StoredData()
            {       
            }          
        }
        
        class GroupData
        {
            public string GroupName;
            public int GroupID;
            public List<ulong> Players = new List<ulong>();
            public List<string> PlayerBase = new List<string>();
            public int GroupResourceX;
            public int GroupPickupX;
            public int GroupQuarryX;
            public GroupData()
            {              
            }
        }
        
        void CreateDefaultGroup()
        {
            var newGroupID = 1;
            var newGroupName = "regular";
            var GroupResourceX = Convert.ToInt32(Config["DefaultGroupResourceX"]);
            var GroupPickupX = Convert.ToInt32(Config["DefaultGroupPickUpX"]);
            var GroupQuarryX = Convert.ToInt32(Config["DefaultGroupQuarryX"]);
            try
            {
                if(!storedData.newGroup.ContainsKey(newGroupID))
                {
                    var info = new GroupData();
                    info.GroupName = newGroupName;
                    info.GroupID = newGroupID;
                    info.GroupResourceX = GroupResourceX;
                    info.GroupPickupX = GroupPickupX;
                    info.GroupQuarryX = GroupQuarryX;
                    info.Players = new List<ulong>();
                    info.PlayerBase = new List<string>();
                    storedData.newGroup.Add(newGroupID, info);
                    SaveData();              
                }
                else
                {
                    return;
                }                
            }
            catch (System.Exception)
            {
                PrintWarning("Error creating defaulting group!");
                throw;
            }
        }
        
        class PlayerData
        {
            public string Name;
            public float UserID;
            public int ResourceX;
            public int QuarryX;
            public int PickupX; 
            public string PlayerGroupName;
            public int PlayerGroupID;
            public PlayerData()
            {      
            }           
        }      
        
        void CreatePlayerData(BasePlayer player)
        {
            if(!storedData.Player.ContainsKey(player.userID))
            {
                var info = new PlayerData();
                info.Name = player.displayName;
                info.UserID = player.userID;
                info.ResourceX = Convert.ToInt32(Config["DefaultGroupResourceX"]);
                info.QuarryX = Convert.ToInt32(Config["DefaultGroupQuarryX"]);
                info.PickupX = Convert.ToInt32(Config["DefaultGroupPickUpX"]);
                info.PlayerGroupName = "regular";
                info.PlayerGroupID = 1;
                storedData.Player.Add(player.userID, info);     
                if(!storedData.newGroup[1].Players.Contains(player.userID))
                {
                    storedData.newGroup[1].Players.Add(player.userID);
                    storedData.newGroup[1].PlayerBase.Add(player.displayName.ToString());  
                    SaveData();             
                }
                else SaveData();
            } 
            else
            {
                return;
            }   
        }
        
        //////////////////////////////////////////////////////////////////////////////////////
        // Other Group Stuff
        //////////////////////////////////////////////////////////////////////////////////////           
        
        StoredData storedData;
        public int newcount;   
        public bool QuarryWarning;
        public EntityLink.Gender gender;   
        private const float DefaultMiningQuarryResourceTickRate = 5f;          
        
        //////////////////////////////////////////////////////////////////////////////////////
        // Language Production
        //////////////////////////////////////////////////////////////////////////////////////   
        
        void LoadLangauge()
        {
			lang.RegisterMessages(new Dictionary<string,string>{
                //General Messages//
                ["General-IDAlreadyUsed"] = "<color='{0}'>{1}:</color><color='{2}'> You may not use a ID that has already been used!</color>",
                ["General-AddedToGroupTar"] = "<color='{0}'>{1}:</color><color='{2}'> You were added to group {3}, your rates will be trasnfered upon rejoining.</color>",
                ["General-AddedToGroupPla"] = "<color='{0}'>{1}:</color><color='{2}'> You have added {3} to group {4}.</color>",
                ["General-GroupCreated"] = "<color='{0}'>{1}:</color><color='{2}'> You have created a group named {3} with a ID of {4}.</color>",
                ["General-PlayerAlreadyInGroup"] = "<color='{0}'>{1}:</color><color='{2}'> {3} is already in that group!</color>",
                ["General-WrongNumber"] = "<color='{0}'>{1}:</color><color='{2}'> You must use the next number after the last group.</color>",
                ["General-NotInTargetGroup"] = "<color='{0}'>{1}:</color><color='{2}'> You must be in the targets group to preform this command!</color>",
                ["General-NoGroupOfID"] = "<color='{0}'>{1}:</color><color='{2}'> There is no group of ID {3}, please try again with a actual GroupID!</color>",
                ["General-WasRemovedFrom"] = "<color='{0}'>{1}:</color><color='{2}'> You were removed from {3} and were set to group {4}.</color>",
                ["General-YouRemovedX"] = "<color='{0}'>{1}:</color><color='{2}'> You removed {3} from group {4} to group {5} which is default.</color>",
                ["General-AlreadyInDefault"] = "<color='{0}'>{1}:</color><color='{2}'> {3} is already in the default group. You may not remove him from the default group!</color>",
                ["General-NoPermission"] = "<color='{0}'>{1}:</color><color='{2}'> You do not have the correct permissions to preform this command!</color>",
                ["General-TooManyPlayers"] = "<color='{0}'>{1}:</color><color='{2}'> There are too many players in this group. Please look in the data file!</color>",
                ["General-GatherSet"] = "<color='{0}'>{1}:</color><color='{2}'> Group ID with {3} was changed to [{4}, {5}, {6}].</color>",
                //Format Messages//
                ["Format-GroupPlayers"] = "<color='{0}'>{1}:</color><color='{2}'> {3}.</color>",
                ["Format-Gather"] = "<color='{0}'>{1}:</color><color='{2}'> {3}{4}.</color>",
                //Help Messages//
                ["Help-AddPlayer"] = "<color='{0}'>{1}:</color><color='{2}'> /igath addtogroup (player) (groupID) - Adds a player to a group!</color>",
                ["Help-RemovePlayer"] = "<color='{0}'>{1}:</color><color='{2}'> /igath removefromgroup (player) (groupID) - Removes a player from the selected group.</color>",
                ["Help-AddGroup"] = "<color='{0}'>{1}:</color><color='{2}'> /igath creategroup (groupID) (groupName) (ResourceX) (PickupX) (QuarryX) - Creates a group.</color>",
                ["Help-Gather"] = "<color='{0}'>{1}:</color><color='{2}'> /igath gather - Shows your gather rate.</color>",
                ["Help-Gatherp"] = "<color='{0}'>{1}:</color><color='{2}'> /igath gatherp (target) - Shows a targets current gather rate!</color>",
                ["Help-GroupPlayerA"] = "<color='{0}'>{1}:</color><color='{2}'> /igath groupbaseplayers (groupID) - Shows the players in a group if the total players is less then 12.</color>",
                ["Help-SetGroupGather"] = "<color='{0}'>{1}:</color><color='{2}'> /igath setgroupgather (groupID) (resource) (pickup) (quarry) - Sets a gather rate!</color>",
                //Finder Messages//
                ["Finder-NoPlayers"] = "<color='{0}'>{1}:</color><color='{2}'> No players were found under the name of {3}!</color>",
                ["Finder-MultiplePlayers"] = "<color='{0}'>{1}:</color><color='{2}'> They're were multiple players. Please pick one of the following: {3}.</color>",
			}, this);                
            
        } 
        
        private string GetMessage(string name, string sid = null) {
			return lang.GetMessage(name, this, sid);
		}
        
        //////////////////////////////////////////////////////////////////////////////////////
        // Loaded()
        //////////////////////////////////////////////////////////////////////////////////////    
        
        void Loaded()
        {
            storedData = Interface.GetMod().DataFileSystem.ReadObject<StoredData>("IGather");
            LoadLangauge();
            LoadPermissions();
            CreateDefaultGroup();
            CheckGroups();
        }
        
        //////////////////////////////////////////////////////////////////////////////////////
        // CheckGroups
        //////////////////////////////////////////////////////////////////////////////////////            
        
        void CheckGroups()
        {
            if(storedData.newGroup.ContainsKey(newcount))
            {
                if(storedData.newGroup[newcount].GroupID == 1)
                {
                    if(storedData.newGroup[newcount].GroupResourceX != Convert.ToInt32(Config["DefaultGroupResourceX"]))
                    {
                        Puts("Group Resource Set To Correct Rate!");
                        storedData.newGroup[newcount].GroupResourceX = Convert.ToInt32(Config["DefaultGroupResourceX"]);
                        SaveData();
                    }
                    if(storedData.newGroup[newcount].GroupPickupX != Convert.ToInt32(Config["DefaultGroupPickUpX"]))
                    {
                        Puts("Group Pickup Set To Correct Rate!");
                        storedData.newGroup[newcount].GroupPickupX = Convert.ToInt32(Config["DefaultGroupPickUpX"]);
                        SaveData();
                    }
                    if(storedData.newGroup[newcount].GroupQuarryX != Convert.ToInt32(Config["DefaultGroupQuarryX"]))
                    {
                        Puts("Group Quarry Set To Correct Rate!");
                        storedData.newGroup[newcount].GroupQuarryX = Convert.ToInt32(Config["DefaultGroupQuarryX"]);
                        SaveData();
                        return;
                    } 
                    Puts("All groups resource are correct!");
                    return;                                       
                }
                else
                {
                    newcount++;
                    CheckGroups();                       
                }         
            }
            else
            {
                if(newcount < storedData.newGroup.Count)
                {
                    newcount++;
                    CheckGroups();
                }
                else
                {
                    newcount = 0;
                    return;
                }
            }          
        }
        
        //////////////////////////////////////////////////////////////////////////////////////
        // SaveData()
        //////////////////////////////////////////////////////////////////////////////////////          
        
        void SaveData()
        {
            Interface.Oxide.DataFileSystem.WriteObject("IGather", storedData);    
        }
        
        //////////////////////////////////////////////////////////////////////////////////////
        // OnServerSave
        //////////////////////////////////////////////////////////////////////////////////////  
        
        void OnServerSave()
        {
            SaveData();
            PrintWarning("Data Saved!");
        }
        
        //////////////////////////////////////////////////////////////////////////////////////
        // LoadPermissions()
        //////////////////////////////////////////////////////////////////////////////////////              
        
        void LoadPermissions()
        {
            permission.RegisterPermission("igather.admin", this);
            permission.RegisterPermission("igather.groupadmin", this);     
            permission.RegisterPermission("igather.gatheradmin", this);     
        }
        
        //////////////////////////////////////////////////////////////////////////////////////
        // isAuth
        /////////////////////////////////////////////////////////////////////////////////////        
        
        public bool isAuth(BasePlayer player)
        {
            if(player != null)
            {
                if(player.net.connection.authLevel > 1) return true;
                else return false;
            }
            else return false;
        }
        
        //////////////////////////////////////////////////////////////////////////////////////
        // OnPlayerInit()
        //////////////////////////////////////////////////////////////////////////////////////          
        
        void OnPlayerInit(BasePlayer player) 
        {
            CreatePlayerData(player);
            timer.Once(2, () =>
            {
                if(storedData.Player[player.userID].PlayerGroupID != 1)
                {
                    if(storedData.newGroup[1].GroupID == storedData.Player[player.userID].PlayerGroupID) return;
                    else
                    {
                        if(storedData.newGroup[newcount].GroupID == storedData.Player[player.userID].PlayerGroupID)
                        {
                            storedData.newGroup[newcount].Players.Add(player.userID);
                            storedData.Player[player.userID].ResourceX = storedData.newGroup[newcount].GroupResourceX;  
                            storedData.Player[player.userID].QuarryX = storedData.newGroup[newcount].GroupQuarryX;   
                            storedData.Player[player.userID].PickupX = storedData.newGroup[newcount].GroupPickupX; 
                            storedData.Player[player.userID].PlayerGroupName = storedData.newGroup[newcount].GroupName;   
                            storedData.Player[player.userID].PlayerGroupID= storedData.newGroup[newcount].GroupID;       
                            SaveData();
                            newcount = 0;        
                        }
                        else
                        {
                            newcount++;
                            OnPlayerInit(player);
                        }
                    }  
                }
            });
        }
        
        //////////////////////////////////////////////////////////////////////////////////////
        // DoCheckGroupTotal()
        //////////////////////////////////////////////////////////////////////////////////////          
        
        [ConsoleCommand("trygroup")]
        void tryGroup() 
        {
            if(storedData.newGroup.ContainsKey(newcount))
            {
                Puts(storedData.newGroup[newcount].GroupID.ToString());
                Puts(storedData.newGroup[newcount].GroupName.ToString());
                newcount++;
                tryGroup();            
            }
            else
            {
                if(newcount < storedData.newGroup.Count)
                {
                    newcount++;
                    tryGroup();
                }
                else
                {
                    newcount = 0;
                    return;
                }
            }
        }
        
        //////////////////////////////////////////////////////////////////////////////////////
        // FindPlayer(BasePlayer)
        //////////////////////////////////////////////////////////////////////////////////////          
        
        private BasePlayer FindPlayer(BasePlayer player, string arg)
        {
            var foundPlayers = new List<BasePlayer>();
            ulong steamid;
            ulong.TryParse(arg, out steamid);
            string lowerarg = arg.ToLower();

            foreach (var p in BasePlayer.activePlayerList)
            {
                if (p != null)
                {
                    if (steamid != 0L)
                        if (p.userID == steamid) return p;
                    string lowername = p.displayName.ToLower();
                    if (lowername.Contains(lowerarg))
                    {
                        foundPlayers.Add(p);
                    }
                }
            }
            if (foundPlayers.Count == 0)
            {
                foreach (var sleeper in BasePlayer.sleepingPlayerList)
                {
                    if (sleeper != null)
                    {
                        if (steamid != 0L)
                            if (sleeper.userID == steamid)
                            {
                                foundPlayers.Clear();
                                foundPlayers.Add(sleeper);
                                return foundPlayers[0];
                            }
                        string lowername = player.displayName.ToLower();
                        if (lowername.Contains(lowerarg))
                        {
                            foundPlayers.Add(sleeper);
                        }
                    }
                }
            }
            if (foundPlayers.Count == 0)
            {
                if (player != null)
                    SendReply(player, string.Format(GetMessage("Finder-NoPlayers", player.UserIDString), Config["ChatPrefixColor"], Config["ChatPrefix"], Config["ChatColor"], arg.ToLower()));
                return null;
            }
            if (foundPlayers.Count > 1)
            {
                if (player != null)
                    SendReply(player, string.Format(GetMessage("Finder-MultiplePlayers", player.UserIDString), Config["ChatPrefixColor"], Config["ChatPrefix"], Config["ChatColor"], foundPlayers[0]));
                return null;
            }

            return foundPlayers[0];
        }          
        
        //////////////////////////////////////////////////////////////////////////////////////
        // FindPlayerConsole
        //////////////////////////////////////////////////////////////////////////////////////          
        
        private BasePlayer FindPlayerC(ConsoleSystem.Arg targer, string arg)
        {
            var foundPlayers = new List<BasePlayer>();
            ulong steamid;
            ulong.TryParse(arg, out steamid);
            string lowerarg = arg.ToLower();

            foreach (var p in BasePlayer.activePlayerList)
            {
                if (p != null)
                {
                    if (steamid != 0L)
                        if (p.userID == steamid) return p;
                    string lowername = p.displayName.ToLower();
                    if (lowername.Contains(lowerarg))
                    {
                        foundPlayers.Add(p);
                    }
                }
            }
            if (foundPlayers.Count == 0)
            {
                foreach (var sleeper in BasePlayer.sleepingPlayerList)
                {
                    if (sleeper != null)
                    {
                        if (steamid != 0L)
                            if (sleeper.userID == steamid)
                            {
                                foundPlayers.Clear();
                                foundPlayers.Add(sleeper);
                                return foundPlayers[0];
                            }
                        string lowername = targer.ToString();
                        if (lowername.Contains(lowerarg))
                        {
                            foundPlayers.Add(sleeper);
                        }
                    }
                }
            }
            if (foundPlayers.Count == 0)
            {
                if (arg != null)
                    SendReply(targer, "No Players Of Name: "+arg+"");
                return null;
            }
            if (foundPlayers.Count > 1)
            {
                if (arg != null)
                    SendReply(targer, "Multiple Players With Name: "+foundPlayers+"");
                return null;
            }

            return foundPlayers[0];
        }          
        
        //////////////////////////////////////////////////////////////////////////////////////
        // CreateGroup
        //////////////////////////////////////////////////////////////////////////////////////  
        
        void CreateGroup(int cID, string cNAME, int cResource, int cPickup, int cQuarry, BasePlayer player)
        {
            if(storedData.newGroup.Count() < cID  && cID < storedData.newGroup.Count() + 2) 
            {
                if(player == null)
                {
                    return;
                }    
                else
                {
                    if(!storedData.newGroup.ContainsKey(cID))
                    {
                        var info = new GroupData();
                        info.GroupName = cNAME;
                        info.GroupID = cID;
                        info.GroupResourceX = cResource;
                        info.GroupPickupX = cPickup;
                        info.GroupQuarryX = cQuarry;
                        info.Players = new List<ulong>();
                        storedData.newGroup.Add(cID, info);
                        SendReply(player, string.Format(GetMessage("General-GroupCreated", player.UserIDString), Config["ChatPrefixColor"], Config["ChatPrefix"], Config["ChatColor"], cNAME, cID));                      
                        SaveData();                       
                    }
                    else
                    {
                        SendReply(player, string.Format(GetMessage("General-IDAlreadyUsed", player.UserIDString), Config["ChatPrefixColor"], Config["ChatPrefix"], Config["ChatColor"]));
                        return;                    
                    }                     
                }                
            }
            else
            {
                SendReply(player, string.Format(GetMessage("General-WrongNumber", player.UserIDString), Config["ChatPrefixColor"], Config["ChatPrefix"], Config["ChatColor"]));
                return;                   
            }
        }
        
        //////////////////////////////////////////////////////////////////////////////////////
        // CreateGroupConsole()
        //////////////////////////////////////////////////////////////////////////////////////          
        
        void CreateGroupConsole(int cID, string cNAME, int cResource, int cPickup, int cQuarry, ConsoleSystem.Arg arg)
        {
            try
            {
                if(storedData.newGroup.Count() < cID  && cID < storedData.newGroup.Count() + 2) 
                {
                    if(!storedData.newGroup.ContainsKey(cID))
                    {
                        var info = new GroupData();
                        info.GroupName = cNAME;
                        info.GroupID = cID;
                        info.GroupResourceX = cResource;
                        info.GroupPickupX = cPickup;
                        info.GroupQuarryX = cQuarry;
                        info.Players = new List<ulong>();
                        storedData.newGroup.Add(cID, info);
                        SendReply(arg, "Group Created With The Name: *"+cNAME+"*.");                
                        SaveData();                                      
                    }                
                }
                else
                {
                    var newNumber = storedData.newGroup.Count() + 1;
                    SendReply(arg, "Wrong number: Please use *#"+newNumber+"*.");
                    return;                   
                }                
            }
            catch(System.Exception)
            {
                PrintWarning("Error Creating Group: CreateGroupConsole.");
            }
        }               
        
        //////////////////////////////////////////////////////////////////////////////////////
        // SetGroup
        //////////////////////////////////////////////////////////////////////////////////////         
        
        void PlayerSetGroup(BasePlayer player, BasePlayer target, int GroupID)
        {
            if(target == null)
            {
                return;
            }    
            if(storedData.newGroup[GroupID].Players.Contains(target.userID))
            {
                SendReply(player, string.Format(GetMessage("General-PlayerAlreadyInGroup", player.UserIDString), Config["ChatPrefixColor"], Config["ChatPrefix"], Config["ChatColor"], target.displayName));
                return;
            }
            if(!storedData.newGroup.ContainsKey(GroupID))
            {
                SendReply(player, string.Format(GetMessage("General-NoGroupOfID", player.UserIDString), Config["ChatPrefixColor"], Config["ChatPrefix"], Config["ChatColor"], GroupID));
                return;                
            }
            else
            {
                var newPID = storedData.Player[target.userID].PlayerGroupID;          
                storedData.newGroup[storedData.Player[target.userID].PlayerGroupID].Players.Remove(target.userID);  
                storedData.newGroup[storedData.Player[target.userID].PlayerGroupID].PlayerBase.Remove(target.displayName.ToString());  
                storedData.newGroup[GroupID].Players.Add(target.userID);
                storedData.newGroup[GroupID].PlayerBase.Add(target.displayName.ToString());
                storedData.Player[target.userID].ResourceX = storedData.newGroup[GroupID].GroupResourceX;  
                storedData.Player[target.userID].QuarryX = storedData.newGroup[GroupID].GroupQuarryX;   
                storedData.Player[target.userID].PickupX = storedData.newGroup[GroupID].GroupPickupX; 
                storedData.Player[target.userID].PlayerGroupName = storedData.newGroup[GroupID].GroupName;   
                storedData.Player[target.userID].PlayerGroupID = storedData.newGroup[GroupID].GroupID;  
                SaveData(); 
                SendReply(target, string.Format(GetMessage("General-AddedToGroupTar", target.UserIDString), Config["ChatPrefixColor"], Config["ChatPrefix"], Config["ChatColor"], storedData.newGroup[GroupID].GroupName));
                SendReply(player, string.Format(GetMessage("General-AddedToGroupPla", player.UserIDString), Config["ChatPrefixColor"], Config["ChatPrefix"], Config["ChatColor"], target.displayName, storedData.newGroup[GroupID].GroupName)); 
            }
        }            
        
        //////////////////////////////////////////////////////////////////////////////////////
        // PlayerSetGroupConsole()
        //////////////////////////////////////////////////////////////////////////////////////          
        
        void PlayerSetGroupConsole(ConsoleSystem.Arg player, BasePlayer target, int GroupID)
        {
            if(target == null)
            {
                return;
            }    
            if(storedData.newGroup[GroupID].Players.Contains(target.userID))
            {
                SendReply(player, "*"+target.displayName+"* is already in that group!");
                return;
            }
            if(!storedData.newGroup.ContainsKey(GroupID))
            {
                SendReply(player, "There is no group of ID *#"+GroupID+"*");
                return;                
            }
            else
            {
                var newPID = storedData.Player[target.userID].PlayerGroupID;          
                storedData.newGroup[storedData.Player[target.userID].PlayerGroupID].Players.Remove(target.userID);  
                storedData.newGroup[storedData.Player[target.userID].PlayerGroupID].PlayerBase.Remove(target.displayName.ToString());  
                storedData.newGroup[GroupID].Players.Add(target.userID);
                storedData.newGroup[GroupID].PlayerBase.Add(target.displayName.ToString());
                storedData.Player[target.userID].ResourceX = storedData.newGroup[GroupID].GroupResourceX;  
                storedData.Player[target.userID].QuarryX = storedData.newGroup[GroupID].GroupQuarryX;   
                storedData.Player[target.userID].PickupX = storedData.newGroup[GroupID].GroupPickupX; 
                storedData.Player[target.userID].PlayerGroupName = storedData.newGroup[GroupID].GroupName;   
                storedData.Player[target.userID].PlayerGroupID = storedData.newGroup[GroupID].GroupID;  
                SaveData(); 
                SendReply(target, string.Format(GetMessage("General-AddedToGroupTar", target.UserIDString), Config["ChatPrefixColor"], Config["ChatPrefix"], Config["ChatColor"], storedData.newGroup[GroupID].GroupName));
                SendReply(player, "You added *"+target.displayName+"* to group *"+storedData.newGroup[GroupID].GroupName+"*."); 
            }
        }          
        
        //////////////////////////////////////////////////////////////////////////////////////
        // RemovGroup
        //////////////////////////////////////////////////////////////////////////////////////         
        
        void PlayerRemoveGroup(BasePlayer target, BasePlayer player, int GroupID)
        {
            if(target == null)
            {
                return;
            } 
            if(storedData.newGroup[1].Players.Contains(target.userID))
            {
                SendReply(player, string.Format(GetMessage("General-AlreadyInDefault", player.UserIDString), Config["ChatPrefixColor"], Config["ChatPrefix"], Config["ChatColor"], target.displayName));
                return;                  
            }   
            if(!storedData.newGroup[GroupID].Players.Contains(target.userID))
            {
                var GroupNameG = storedData.newGroup[GroupID].GroupName;
                SendReply(player, string.Format(GetMessage("General-DoesNotContainTarget", player.UserIDString), Config["ChatPrefixColor"], Config["ChatPrefix"], Config["ChatColor"], GroupNameG, target.displayName));
                return;
            }
            if(!storedData.newGroup.ContainsKey(GroupID))
            {
                SendReply(player, string.Format(GetMessage("General-NoGroupOfID", player.UserIDString), Config["ChatPrefixColor"], Config["ChatPrefix"], Config["ChatColor"], GroupID));
                return;                
            }
            else
            {
                var gname = storedData.newGroup[1].GroupName;
                var ggname = storedData.newGroup[GroupID].GroupName;
                storedData.newGroup[GroupID].Players.Remove(target.userID);
                storedData.newGroup[GroupID].PlayerBase.Remove(target.displayName.ToString());
                storedData.newGroup[1].Players.Add(target.userID);
                storedData.newGroup[1].PlayerBase.Add(target.displayName.ToString());
                storedData.Player[target.userID].ResourceX = storedData.newGroup[1].GroupResourceX;  
                storedData.Player[target.userID].QuarryX = storedData.newGroup[1].GroupQuarryX;   
                storedData.Player[target.userID].PickupX = storedData.newGroup[1].GroupPickupX; 
                storedData.Player[target.userID].PlayerGroupName = storedData.newGroup[1].GroupName;   
                storedData.Player[target.userID].PlayerGroupID = storedData.newGroup[1].GroupID;     
                SaveData();
                SendReply(target, string.Format(GetMessage("General-WasRemovedFrom", player.UserIDString), Config["ChatPrefixColor"], Config["ChatPrefix"], Config["ChatColor"], ggname, gname));
                SendReply(player, string.Format(GetMessage("General-YouRemovedX", player.UserIDString), Config["ChatPrefixColor"], Config["ChatPrefix"], Config["ChatColor"], target.displayName, ggname, gname));           
            }            
        }
        
        void PlayerRemoveGroupConsole(BasePlayer target, ConsoleSystem.Arg arg, int GroupID)
        {
            if(target == null)
            {
                return;
            } 
            if(storedData.newGroup[1].Players.Contains(target.userID))
            {
                SendReply(arg, "Player: *"+target.displayName+"* is already in the default group.");
                return;                  
            }   
            if(!storedData.newGroup[GroupID].Players.Contains(target.userID))
            {
                var GroupNameG = storedData.newGroup[GroupID].GroupName;
                SendReply(arg, "The group name of *"+GroupNameG+"* does not contain *"+target.userID+"*.");
                return;
            }
            else
            {
                var gname = storedData.newGroup[1].GroupName;
                var ggname = storedData.newGroup[GroupID].GroupName;
                storedData.newGroup[GroupID].Players.Remove(target.userID);
                storedData.newGroup[GroupID].PlayerBase.Remove(target.displayName.ToString());
                storedData.newGroup[1].Players.Add(target.userID);
                storedData.newGroup[1].PlayerBase.Add(target.displayName.ToString());
                storedData.Player[target.userID].ResourceX = storedData.newGroup[1].GroupResourceX;  
                storedData.Player[target.userID].QuarryX = storedData.newGroup[1].GroupQuarryX;   
                storedData.Player[target.userID].PickupX = storedData.newGroup[1].GroupPickupX; 
                storedData.Player[target.userID].PlayerGroupName = storedData.newGroup[1].GroupName;   
                storedData.Player[target.userID].PlayerGroupID = storedData.newGroup[1].GroupID;     
                SaveData();
                SendReply(target, "You were removed from *"+ggname+"* and added to group *"+gname+"*.");
                SendReply(arg, "You removed *"+target.displayName+"* from group *"+ggname+"* to group *"+gname+"*.");  
            }                  
        }
        
        //////////////////////////////////////////////////////////////////////////////////////
        // SetGatherRate(Group)
        //////////////////////////////////////////////////////////////////////////////////////          
        
        void SetGather(int groupID, int cResource, int cPickup, int cQuarry, BasePlayer player)
        { 
            if(player == null) return;
            if(!storedData.newGroup.ContainsKey(groupID))
            {
                SendReply(player, string.Format(GetMessage("General-NoGroupOfID", player.UserIDString), Config["ChatPrefixColor"], Config["ChatPrefix"], Config["ChatColor"], groupID));
                return;                  
            }
            else
            {
                var gID = Convert.ToInt32(groupID);  
                storedData.newGroup[gID].GroupResourceX = cResource;
                storedData.newGroup[gID].GroupPickupX = cPickup;
                storedData.newGroup[gID].GroupQuarryX = cQuarry;   
                SaveData();
                SendReply(player, string.Format(GetMessage("General-GatherSet", player.UserIDString), Config["ChatPrefixColor"], Config["ChatPrefix"], Config["ChatColor"], groupID, cResource, cPickup, cQuarry));
                foreach(var entry in storedData.newGroup[gID].Players)
                {
                    BasePlayer enter = BasePlayer.FindByID(entry);
                    storedData.Player[enter.userID].QuarryX = storedData.newGroup[gID].GroupQuarryX;
                    storedData.Player[enter.userID].PickupX = storedData.newGroup[gID].GroupPickupX;
                    storedData.Player[enter.userID].ResourceX = storedData.newGroup[gID].GroupResourceX;
                    SaveData();
                }                 
            }
        }
        
        //////////////////////////////////////////////////////////////////////////////////////
        // SetGatherConsole()
        //////////////////////////////////////////////////////////////////////////////////////           
        
        void SetGatherConsole(int groupID, int cResource, int cPickup, int cQuarry, ConsoleSystem.Arg arg)
        {
            if(!storedData.newGroup.ContainsKey(groupID))
            {
                SendReply(arg, "No group of ID *#"+groupID+"* exists! Please use a existing group." );   
                return;                  
            }
            else
            {
                var gID = Convert.ToInt32(groupID);  
                storedData.newGroup[gID].GroupResourceX = cResource;
                storedData.newGroup[gID].GroupPickupX = cPickup;
                storedData.newGroup[gID].GroupQuarryX = cQuarry;   
                SaveData();
                SendReply(arg, "The GroupID with *#"+groupID+"* was set to gather rate: ["+cResource+", "+cPickup+", "+cQuarry+"].");
                foreach(var entry in storedData.newGroup[gID].Players)
                {
                    BasePlayer enter = BasePlayer.FindByID(entry);
                    storedData.Player[enter.userID].QuarryX = storedData.newGroup[gID].GroupQuarryX;
                    storedData.Player[enter.userID].PickupX = storedData.newGroup[gID].GroupPickupX;
                    storedData.Player[enter.userID].ResourceX = storedData.newGroup[gID].GroupResourceX;
                    SaveData();
                }
            }            
        }
        
        //////////////////////////////////////////////////////////////////////////////////////
        // RemoveGroupConsole()
        //////////////////////////////////////////////////////////////////////////////////////            
        
        void RemoveGroupConsole(ConsoleSystem.Arg arg, int groupID)
        {
            if(!storedData.newGroup.ContainsKey(groupID))
            {
                SendReply(arg, "No group of ID *#"+groupID+"* exists! Please use a existing group." );   
            }
            else
            {
                var gID = Convert.ToInt32(groupID);  
                foreach(var entry in storedData.newGroup[gID].Players)
                {
                    BasePlayer target = BasePlayer.FindByID(entry);
                    PlayerSetGroupConsole(arg, target, 1);
                    SaveData();
                }   
                storedData.newGroup.Remove(gID);
                SaveData();
                SendReply(arg, "The group number of *#"+gID+"* was removed!");
            }
        }        
        
        //////////////////////////////////////////////////////////////////////////////////////
        // SendCommands
        //////////////////////////////////////////////////////////////////////////////////////          
        
        void SendHelp(BasePlayer player)
        {
            SendReply(player, "<color='#66ff66'>"+Config["ChatPrefix"]+"-------------------------Add Commands------------------------</color>");
            SendReply(player, string.Format(GetMessage("Help-AddPlayer", player.UserIDString), Config["ChatPrefixColor"], Config["ChatPrefix"], Config["ChatColor"])); 
            SendReply(player, string.Format(GetMessage("Help-AddGroup", player.UserIDString), Config["ChatPrefixColor"], Config["ChatPrefix"], Config["ChatColor"])); 
            SendReply(player, "<color='#66ff66'>"+Config["ChatPrefix"]+"-----------------------Remove Commands---------------------</color>");
            SendReply(player, string.Format(GetMessage("Help-RemovePlayer", player.UserIDString), Config["ChatPrefixColor"], Config["ChatPrefix"], Config["ChatColor"])); 
            SendReply(player, "<color='#66ff66'>"+Config["ChatPrefix"]+"-----------------------Gather Commands-----------------------</color>");
            SendReply(player, string.Format(GetMessage("Help-Gather", player.UserIDString), Config["ChatPrefixColor"], Config["ChatPrefix"], Config["ChatColor"]));    
            SendReply(player, string.Format(GetMessage("Help-Gatherp", player.UserIDString), Config["ChatPrefixColor"], Config["ChatPrefix"], Config["ChatColor"]));
            SendReply(player, string.Format(GetMessage("Help-SetGroupGather", player.UserIDString), Config["ChatPrefixColor"], Config["ChatPrefix"], Config["ChatColor"])); 
            SendReply(player, "<color='#66ff66'>"+Config["ChatPrefix"]+"-----------------------Group Commands------------------------</color>");
            SendReply(player, string.Format(GetMessage("Help-GroupPlayerA", player.UserIDString), Config["ChatPrefixColor"], Config["ChatPrefix"], Config["ChatColor"]));    
        }
        
        void SendGather(BasePlayer player)
        {
            SendReply(player, string.Format(GetMessage("Format-Gather", player.UserIDString), Config["ChatPrefixColor"], Config["ChatPrefix"], Config["ChatColor"], "PickUp Rate: ", storedData.Player[player.userID].PickupX)); 
            SendReply(player, string.Format(GetMessage("Format-Gather", player.UserIDString), Config["ChatPrefixColor"], Config["ChatPrefix"], Config["ChatColor"], "Quarry Rate: ", storedData.Player[player.userID].QuarryX)); 
            SendReply(player, string.Format(GetMessage("Format-Gather", player.UserIDString), Config["ChatPrefixColor"], Config["ChatPrefix"], Config["ChatColor"], "Resource Rate: ", storedData.Player[player.userID].ResourceX));    
        }
        
        void SendGatherP(BasePlayer player, BasePlayer target)
        {
            SendReply(player, string.Format(GetMessage("Format-Gather", player.UserIDString), Config["ChatPrefixColor"], Config["ChatPrefix"], Config["ChatColor"], "Target PickUp Rate: ", storedData.Player[target.userID].PickupX)); 
            SendReply(player, string.Format(GetMessage("Format-Gather", player.UserIDString), Config["ChatPrefixColor"], Config["ChatPrefix"], Config["ChatColor"], "Target Quarry Rate: ", storedData.Player[target.userID].QuarryX)); 
            SendReply(player, string.Format(GetMessage("Format-Gather", player.UserIDString), Config["ChatPrefixColor"], Config["ChatPrefix"], Config["ChatColor"], "TargetResource Rate: ", storedData.Player[target.userID].ResourceX));               
        }
        
        //////////////////////////////////////////////////////////////////////////////////////
        // ChatCommand(igath)
        //////////////////////////////////////////////////////////////////////////////////////   
        
        [ChatCommand("igath")]
        void igathcommand(BasePlayer player, string command, string[] args) 
        {
            if(args.Length == 0)
            {
                SendHelp(player);
                return;
            }
            switch(args[0])
            {
                case "addtogroup":
                    if(isAuth(player) == false && !permission.UserHasPermission(player.userID.ToString(), "igather.groupadmin") && !permission.UserHasPermission(player.userID.ToString(), "igather.admin"))
                    {
                        SendReply(player, string.Format(GetMessage("General-NoPermission", player.UserIDString), Config["ChatPrefixColor"], Config["ChatPrefix"], Config["ChatColor"]));  
                        return;  
                    }
                    if(args.Length >= 4 || args.Length == 1 || args.Length == 2)
                    {
                        SendHelp(player);
                        return;
                    }
                    if(player == null)
                    {
                        return;
                    }
                    else
                    {
                        object addPlayer = FindPlayer(player, args[1]);             
                        BasePlayer target = (BasePlayer)addPlayer;  
                        PlayerSetGroup(target, player, Convert.ToInt32(args[2]));                                
                    }
                break;
                
                case "removefromgroup":
                    if(isAuth(player) == false && !permission.UserHasPermission(player.userID.ToString(), "igather.groupadmin") && !permission.UserHasPermission(player.userID.ToString(), "igather.admin"))
                    {
                        SendReply(player, string.Format(GetMessage("General-NoPermission", player.UserIDString), Config["ChatPrefixColor"], Config["ChatPrefix"], Config["ChatColor"]));  
                        return;  
                    }                
                    if(args.Length >= 4 || args.Length == 1 || args.Length == 2)
                    {
                        SendHelp(player);
                        return;
                    }
                    if(player == null)
                    {
                        return;
                    }
                    else
                    {
                        object addPlayer = FindPlayer(player, args[1]);             
                        BasePlayer target = (BasePlayer)addPlayer;  
                        PlayerRemoveGroup(target, player, Convert.ToInt32(args[2]));                              
                    }
                break;
                
                case "creategroup":
                    if(isAuth(player) == false && !permission.UserHasPermission(player.userID.ToString(), "igather.groupadmin") && !permission.UserHasPermission(player.userID.ToString(), "igather.admin"))
                    {
                        SendReply(player, string.Format(GetMessage("General-NoPermission", player.UserIDString), Config["ChatPrefixColor"], Config["ChatPrefix"], Config["ChatColor"]));  
                        return;  
                    }                
                    else
                    {
                        SendHelp(player);
                        return;
                    }
                    if(player == null)
                    {
                        return;
                    }
                    else
                    {
                        CreateGroup(Convert.ToInt32(args[1]), args[2], Convert.ToInt32(args[3]), Convert.ToInt32(args[4]), Convert.ToInt32(args[5]), player); 
                        return;                                
                    }                
                break;      
                             
                case "gather":
                    if(args.Length >= 2 || args.Length == 0)
                    {
                        SendHelp(player);
                        return;
                    }                    
                    SendGather(player);
                    return;
                break;
                
                case "gatherp":
                    if(isAuth(player) == false && !permission.UserHasPermission(player.userID.ToString(), "igather.gatheradmin") && !permission.UserHasPermission(player.userID.ToString(), "igather.admin"))
                    {
                        SendReply(player, string.Format(GetMessage("General-NoPermission", player.UserIDString), Config["ChatPrefixColor"], Config["ChatPrefix"], Config["ChatColor"]));  
                        return;  
                    }                  
                    if(args.Length >= 3 || args.Length == 0 || args.Length == 1)
                    {
                        SendHelp(player);
                        return;
                    }
                    if(player == null)
                    {
                        return;                     
                    }
                    else
                    {
                        object addPlayer = FindPlayer(player, args[1]);             
                        BasePlayer target = (BasePlayer)addPlayer;                        
                        if(!storedData.newGroup[storedData.Player[target.userID].PlayerGroupID].Players.Contains(player.userID))
                        {
                            SendReply(player, string.Format(GetMessage("General-NotInTargetGroup", player.UserIDString), Config["ChatPrefixColor"], Config["ChatPrefix"], Config["ChatColor"]));               
                            return;
                        }
                        else
                        {
                            SendGatherP(player, target);                          
                        }                        
                    }
                break;
                
                case "groupbaseplayers":
                    if(isAuth(player) == false && !permission.UserHasPermission(player.userID.ToString(), "igather.groupadmin") && !permission.UserHasPermission(player.userID.ToString(), "igather.admin"))
                    {
                        SendReply(player, string.Format(GetMessage("General-NoPermission", player.UserIDString), Config["ChatPrefixColor"], Config["ChatPrefix"], Config["ChatColor"]));  
                        return;  
                    }
                    if(args.Length >= 3 || args.Length == 0 || args.Length == 1)
                    {
                        SendHelp(player);
                        return;
                    }
                    else
                    {
                        var groupID = Convert.ToInt32(args[1]);
                        if(storedData.newGroup[groupID].PlayerBase.Count > 12)
                        {
                            SendReply(player, string.Format(GetMessage("General-TooManyPlayers", player.UserIDString), Config["ChatPrefixColor"], Config["ChatPrefix"], Config["ChatColor"]));  
                            return;                            
                        }
                        if(storedData.newGroup[groupID].PlayerBase.Count == 0)
                        {
                            SendReply(player, "No current players in this group!");
                            return;
                        }
                        foreach(var targetplayer in storedData.newGroup[groupID].PlayerBase)
                        SendReply(player, string.Format(GetMessage("Format-GroupPlayers", player.UserIDString), Config["ChatPrefixColor"], Config["ChatPrefix"], Config["ChatColor"], targetplayer));                       
                    }                                       
                break;
                
                case "setgroupgather":
                    if(isAuth(player) == false && !permission.UserHasPermission(player.userID.ToString(), "igather.groupadmin") && !permission.UserHasPermission(player.userID.ToString(), "igather.admin")) 
                    {
                        SendReply(player, string.Format(GetMessage("General-NoPermission", player.UserIDString), Config["ChatPrefixColor"], Config["ChatPrefix"], Config["ChatColor"]));  
                        return;                                                  
                    } 
                    if(args.Length == 0 || args.Length >= 6 || args.Length == 1 || args.Length == 2 || args.Length == 3 || args.Length == 4)
                    {
                        SendHelp(player);
                        return;                        
                    }
                    else
                    {
                        var groupID = Convert.ToInt32(args[1]);
                        if(storedData.newGroup.ContainsKey(groupID))
                        {
                            SetGather(groupID, Convert.ToInt32(args[2]), Convert.ToInt32(args[3]), Convert.ToInt32(args[4]), player); 
                            return;  
                        }   
                        else
                        {
                            SendReply(player, string.Format(GetMessage("General-NoGroupOfID", player.UserIDString), Config["ChatPrefixColor"], Config["ChatPrefix"], Config["ChatColor"], groupID));
                            return;
                        } 
                    }
                break;
                
                default:
                    SendHelp(player);
                break;
            }    
        }  
        
        [ConsoleCommand("igathcreategroup")]
        private void ccmdIGathC(ConsoleSystem.Arg arg)
        {  
            try 
            {
                if (arg.Player() != null && !arg.Player().IsAdmin())
                {
                    SendReply(arg, "You do not have permissions to use this command");
                } 
                if(arg.Args.Length >= 6 || arg.Args.Length == 0 || arg.Args.Length == null) 
                {
                    SendReply(arg, "Syntax: igathcreategroup (groupID) (groupName) (X) (X) (X).");    
                }
                else
                {
                    CreateGroupConsole(Convert.ToInt32(arg.Args[0]), arg.Args[1], Convert.ToInt32(arg.Args[2]), Convert.ToInt32(arg.Args[3]), Convert.ToInt32(arg.Args[4]), arg);                               
                }                 
            } 
            catch(System.Exception)
            {
                return;
            }                
        }
        
        [ConsoleCommand("igathsetgather")]
        private void ccmdISetGather(ConsoleSystem.Arg arg)
        {  
            try 
            {
                if (arg.Player() != null && !arg.Player().IsAdmin())
                {
                    SendReply(arg, "You do not have permissions to use this command");
                    return;
                } 
                if(arg.Args.Length == 0 || arg.Args.Length >= 5 || arg.Args == null) 
                {
                    SendReply(arg, "Syntax: igathsetgather (groupID) (x) (x) (x).");    
                    return;
                }
                else
                {
                    SetGatherConsole(Convert.ToInt32(arg.Args[0]), Convert.ToInt32(arg.Args[1]), Convert.ToInt32(arg.Args[2]), Convert.ToInt32(arg.Args[3]), arg);                          
                }                  
            } 
            catch(System.Exception)
            {
                return;
            }               
        }   
        
        [ConsoleCommand("igathaddtogroup")]
        private void ccmdAddtoGroup(ConsoleSystem.Arg arg)
        {
            try 
            {
                object addPlayer = FindPlayerC(arg, arg.Args[1]);             
                BasePlayer target = (BasePlayer)addPlayer;                  
                if (arg.Player() != null && !arg.Player().IsAdmin())
                {
                    SendReply(arg, "You do not have permissions to use this command.");
                    return;
                } 
                if(arg.Args.Length == 0 || arg.Args.Length == null || arg.Args.Length >= 3) 
                {
                    SendReply(arg, "Syntax: igathaddtogroup (target) (groupID).");    
                    return;
                }
                else
                {
                    PlayerSetGroupConsole(arg, target, Convert.ToInt32(arg.Args[0]));                 
                }                 
            }
            catch(System.Exception)
            {
                return;
            }                
        }     
        
        [ConsoleCommand("igathremovegroup")]
        private void ccmdRemoveGroup(ConsoleSystem.Arg arg)
        {
            try 
            {
                if (arg.Player() != null && !arg.Player().IsAdmin())
                {
                    SendReply(arg, "You do not have permissions to use this command.");
                    return;
                } 
                if(arg.Args.Length == 0 || arg.Args.Length == null || arg.Args.Length >= 2) 
                {
                    SendReply(arg, "Syntax: igathremovegroup (GroupID).");    
                    return;
                }
                else
                {
                    RemoveGroupConsole(arg, Convert.ToInt32(arg.Args[0]));             
                }                 
            }
            catch(System.Exception)
            {
                return;
            }
        }
        
        [ConsoleCommand("igathremovefromgroup")]
        private void ccmdRemoveFromGroup(ConsoleSystem.Arg arg)
        {
            try 
            {
                object addPlayer = FindPlayerC(arg, arg.Args[0]);             
                BasePlayer target = (BasePlayer)addPlayer;                       
                if (arg.Player() != null && !arg.Player().IsAdmin())
                {
                    SendReply(arg, "You do not have permissions to use this command.");
                    return;
                } 
                if(arg.Args.Length == 0 || arg.Args.Length == null || arg.Args.Length >= 3) 
                {
                    SendReply(arg, "Syntax: igathremovefromgroup (Target) (GroupID).");    
                    return;
                }
                else
                {
                    PlayerRemoveGroupConsole(target, arg, Convert.ToInt32(arg.Args[1]));             
                }                 
            }
            catch(System.Exception)
            {
                return;
            }            
        }
        
        [ConsoleCommand("igathgatherp")]
        private void ccmdGatherP(ConsoleSystem.Arg arg)
        {
            try 
            {
                object addPlayer = FindPlayerC(arg, arg.Args[0]);             
                BasePlayer target = (BasePlayer)addPlayer;                       
                if (arg.Player() != null && !arg.Player().IsAdmin())
                {
                    SendReply(arg, "You do not have permissions to use this command.");
                    return;
                } 
                if(arg.Args.Length == 0 || arg.Args.Length == null || arg.Args.Length >= 3) 
                {
                    SendReply(arg, "Syntax: igathgatherp (target).");    
                    return;
                }
                else
                {
                    SendReply(arg, "Target Quarry Gather: #"+storedData.Player[target.userID].QuarryX+".");    
                    SendReply(arg, "Target Pickup Gather: #"+storedData.Player[target.userID].PickupX+".");   
                    SendReply(arg, "Target Resource Gather: #"+storedData.Player[target.userID].ResourceX+".");   
                    return;       
                }                 
            }
            catch(System.Exception)
            {
                return;
            }             
        }
        //////////////////////////////////////////////////////////////////////////////////////
        // Gather Rate Changes
        //////////////////////////////////////////////////////////////////////////////////////   
        
        private void OnDispenserGather(ResourceDispenser dispenser, BaseEntity entity, Item item)
        {
            BasePlayer player = entity.ToPlayer();
            if(player == null) return;
            if(!storedData.Player.ContainsKey(player.userID))
            {
                CreatePlayerData(player);    
            }            
            else
            {
                item.amount = (int)(item.amount * storedData.Player[player.userID].ResourceX);                
            }
        }

        private void OnQuarryGather(MiningQuarry quarry, Item item)
        {
            BasePlayer player = BasePlayer.FindByID(quarry.OwnerID) ?? BasePlayer.FindSleeping(quarry.OwnerID);           
            if(player == null) return; 
            try 
            {
                if(!storedData.Player.ContainsKey(player.userID))
                {
                    CreatePlayerData(player);    
                }
                else
                {
                    item.amount = (int)(item.amount * storedData.Player[player.userID].QuarryX);                   
                }                  
            }
            catch(System.Exception)
            {
                if(QuarryWarning == false)
                {
                    
                    PrintWarning("Hook Error: OnQuarryGather");
                    QuarryWarning = true;
                    timer.Once(60, () => QuarryWarning = false);
                }
                else
                {
                    return;                    
                }
            }
        }

        private void OnCollectiblePickup(Item item, BasePlayer player)
        {
            if(player == null) return;   
            if(!storedData.Player.ContainsKey(player.userID))
            {
                CreatePlayerData(player);    
            }            
            else
            {
                item.amount = (int)(item.amount * storedData.Player[player.userID].PickupX);                     
            }       
        }                                              
    }
}