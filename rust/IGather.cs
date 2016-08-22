using Oxide.Core;
using System;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using ConVar;

namespace Oxide.Plugins
{
    [Info("IGather", "DylanSMR", "1.0.9", ResourceId = 1763)]
    [Description("A GUI timer.")]
    class IGather : RustPlugin
    {  
        // / // / // / //
        //Configuration//
        // / // / // / //

        void LoadDefaultConfig()
        {
            PrintWarning("Creating default configuration");
            Config.Clear();
                Config["DefQuarryGatherRate"] = _defQuarryGatherRate;
                Config["DefResourceGatherRate"] = _defResourceGatherRate;
                Config["DefPickupGatherRate"] = _defPickupGatherRate;
                Config["DefPermission"] = _defPermission;
                Config["DefGroupName"] = _defGroupName;
            Config.Save();
        }

        T GetConfig<T>(string name, T defaultValue)
        {
            if (Config[name] == null)
            {
                return defaultValue;
            }

            return (T)Convert.ChangeType(Config[name], typeof(T));
        }   

        // / // / // / //
        //PVC Variables//
        // / // / // / //

        float _defQuarryGatherRate = 1.0f;
        float _defResourceGatherRate = 1.0f;
        float _defPickupGatherRate = 1.0f;

        string _defPermission = "igather.admin";
        string _defGroupName = "Regular";

        string Permission = "";

        public string GroupName;
        public int newcount = 0;

        public bool WipeConfirmV;

        public List<string> nameli = new List<string>();
        public List<ulong>  newadd = new List<ulong>();

        // / // / // / //
        //Data -- Files//
        // / // / // / //

        class GroupData
        {
            public Dictionary<int, Groups> groupD = new Dictionary<int, Groups>();
            public List<string> perms = new List<string>();
            public GroupData(){}
        }

        class Groups
        {
            public string groupName;
            public int groupID;
            public string groupPerm;
            public string groupTimeCreated;
            public float groupQuarryRate;
            public float groupResourceRate;
            public float groupPickupRate;
            public string groupCreater;
            public List<ulong> groupPlayers = new List<ulong>();
            public Groups(){}
        }

        GroupData groupData;

        class PlayerData
        {
            public Dictionary<ulong, Players> playerD = new Dictionary<ulong, Players>();
            public PlayerData(){}
        }

        class Players
        {
            public string playerName;
            public ulong playerID;
            public float playerPickup;
            public float playerQuarry;
            public float playerResource;
            public int playerGroupID;
            public string playerGroupName;
            public Players(){}
        }

        PlayerData playerData;

        // / // / // / //
        //Loading Data-//
        // / // / // / //

        void Loaded()
        {
            groupData = Interface.GetMod().DataFileSystem.ReadObject<GroupData>("IGather-Groups");
            playerData = Interface.GetMod().DataFileSystem.ReadObject<PlayerData>("IGather-Players");   
            LoadDefaultGroups();
            lang.RegisterMessages(messages, this); 
            GroupName = GetConfig("DefGroupName", "");

            LoadPerms();
        }

        void LoadPerms()
        {
            foreach(var p in groupData.perms){
                permission.RegisterPermission(p, this);}
        }

        void LoadDefaultGroups()
        {
            if(!groupData.groupD.ContainsKey(1))
            {
                PrintWarning("Creating default group...");
                try 
                {
                    timer.Once(1, () =>
                    {
                        var info = new Groups();
                        info.groupName = GroupName;
                        info.groupID = 1;
                        info.groupPerm = "igather."+GroupName;
                        info.groupCreater = "Server";
                        info.groupTimeCreated = DateTime.Now.ToString("h:mm tt").ToString();
                        info.groupPlayers = new List<ulong>();
                        info.groupResourceRate = Convert.ToInt64(Config["DefResourceGatherRate"]);
                        info.groupQuarryRate = Convert.ToInt64(Config["DefQuarryGatherRate"]);
                        info.groupPickupRate = Convert.ToInt64(Config["DefPickupGatherRate"]);
                        groupData.groupD.Add(1, info);

                        groupData.perms.Add("igather."+GroupName);

                        timer.Once(1, () => PrintWarning("Group created..."));
                        timer.Once(3, () => LoadDefaultGroups());
                        SaveData();
                        return;
                    });
                }
                catch(System.Exception)
                {
                    return;
                }
            }
            else
            {
                if(groupData.groupD[1].groupName == null)
                {
                    PrintWarning("Default group has a error, re-creating!!!");
                    groupData.groupD.Remove(1);
                    timer.Once(1, () =>
                    {
                        LoadDefaultGroups();
                    });
                    return;
                }
                Puts("Group 1 is configured.");
                return;
            }
        }

        void OnPlayerInit(BasePlayer player)
        {
            if(playerData.playerD.ContainsKey(player.userID))
            {
                return;
            }
            else
            {
                PrintWarning("Creating player data for: "+player.displayName);
                try 
                {
                    var info = new Players();
                    info.playerName = player.displayName;
                    info.playerID = player.userID;
                    info.playerQuarry = groupData.groupD[1].groupQuarryRate;
                    info.playerResource = groupData.groupD[1].groupResourceRate;
                    info.playerPickup = groupData.groupD[1].groupPickupRate;
                    info.playerGroupID = 1;
                    info.playerGroupName = GroupName;
                    playerData.playerD.Add(player.userID, info);

                    groupData.groupD[1].groupPlayers.Add(player.userID);
                    SaveData();

                    PrintWarning(player.displayName+"(s) player data has been created (and/or) saved.");
                }
                catch(System.Exception) 
                {
                    return;
                }
            }
        }

        // / // / // / //
        //Permission Ad//
        // / // / // / //

        void OnGroupPermissionGranted(string name, string perm)
        {
                foreach(var entry in groupData.perms)
                {
                    if(entry == perm)
                    {
                        grabPermG(perm, name);
                    }
                }
        }

        void grabPermG(string perma, string name)
        {
            var newperm = perma.Replace("igather.", "");
            if(groupData.groupD.ContainsKey(newcount))
            {
                if(groupData.groupD[newcount].groupName == newperm)
                {     
                    List<string> players = new List<string>();
                    foreach(var sleeper in BasePlayer.sleepingPlayerList)
                    {
                        players.Add(sleeper.displayName);
                    }
                    foreach(var playera in BasePlayer.activePlayerList)
                    {
                        players.Add(playera.displayName);
                    }
                        foreach(var entry in permission.GetUsersInGroup(name))
                        {
                            foreach(var key in players)
                            {
                                if(entry.Contains(key))
                                { 
                                    var newid = groupData.groupD[newcount].groupID;
                                    object addPlayer = FindPlayerU(key);       
                                    BasePlayer newkey = (BasePlayer)addPlayer;     
                                    if(newkey == null) 
                                    {
                                        Puts($"{key} was a null player, not adding to group!");
                                        break;
                                    }
                                    GrantPermission(newid, newkey);
                                }
                            }
                        }
                    players.Clear();
                    return;
                }
                newcount++;
                grabPermG(perma, name);      
                return;    
            }
            else
            {
                if(newcount < groupData.groupD.Count)
                {
                    newcount++;
                    grabPermG(perma, name);  
                    return;
                }
                else
                {
                    newcount = 0;
                    return;
                }
            }         
        }

        [ConsoleCommand("igGrabPermissions")]
        void PermissionGrab(ConsoleSystem.Arg arg)
        {
            if (arg.Player() != null && !arg.Player().IsAdmin())
            {
                SendReply(arg, lang.GetMessage("NoPermission", this));
                return;
            }
            else
            {
                foreach(var perm in groupData.perms)
                {
                    Puts(perm);
                }
            }
        }

        void GrantPermission(int newid, BasePlayer target)
        {
            groupData.groupD[playerData.playerD[target.userID].playerGroupID].groupPlayers.Remove(target.userID);
            groupData.groupD[Convert.ToInt32(newid)].groupPlayers.Add(target.userID);
            playerData.playerD[target.userID].playerGroupName = groupData.groupD[Convert.ToInt32(newid)].groupName;
            playerData.playerD[target.userID].playerGroupID = groupData.groupD[Convert.ToInt32(newid)].groupID;
            playerData.playerD[target.userID].playerPickup = groupData.groupD[Convert.ToInt32(newid)].groupPickupRate;
            playerData.playerD[target.userID].playerResource = groupData.groupD[Convert.ToInt32(newid)].groupResourceRate;
            playerData.playerD[target.userID].playerQuarry = groupData.groupD[Convert.ToInt32(newid)].groupQuarryRate;
            SaveData();
            SendReply(target, lang.GetMessage("UGAddedT", this), groupData.groupD[Convert.ToInt32(newid)].groupName);
        }

        void grabPerm(string perma, string name)
        {
            var newperm = perma.Replace("igather.", "");
            if(groupData.groupD.ContainsKey(newcount))
            {
                if(groupData.groupD[newcount].groupName == newperm)
                {
                    var newid = groupData.groupD[newcount].groupID;
                    object addPlayer = FindPlayerU(name);             
                    BasePlayer target = (BasePlayer)addPlayer;        
                    GrantPermission(newid, target);
                    return;
                }
                newcount++;
                grabPerm(perma, name);      
                return;    
            }
            else
            {
                if(newcount < groupData.groupD.Count)
                {
                    newcount++;
                    grabPerm(perma, name);  
                    return;
                }
                else
                {
                    newcount = 0;
                    return;
                }
            }         
        }

        void OnUserPermissionGranted(string name, string str)
        {
            foreach(var entry in groupData.perms)
            {
                if(entry == str)
                {
                    grabPerm(str, name);
                }
            }
        }

        void RevokePermission(int newid, BasePlayer target)
        {
            groupData.groupD[playerData.playerD[target.userID].playerGroupID].groupPlayers.Remove(target.userID);
            groupData.groupD[1].groupPlayers.Add(target.userID);
            playerData.playerD[target.userID].playerGroupName = groupData.groupD[1].groupName;
            playerData.playerD[target.userID].playerGroupID = groupData.groupD[1].groupID;
            playerData.playerD[target.userID].playerPickup = groupData.groupD[1].groupPickupRate;
            playerData.playerD[target.userID].playerResource = groupData.groupD[1].groupResourceRate;
            playerData.playerD[target.userID].playerQuarry = groupData.groupD[1].groupQuarryRate;
            SaveData();
            SendReply(target, lang.GetMessage("UGAddedT", this), groupData.groupD[1].groupName);
        }

        void grabPerm2(string perma, string name)
        {
            var newperm = perma.Replace("igather.", "");
            if(groupData.groupD.ContainsKey(newcount))
            {
                if(groupData.groupD[newcount].groupName == newperm)
                {
                    var newid = groupData.groupD[newcount].groupID;
                    object addPlayer = FindPlayerU(name);             
                    BasePlayer target = (BasePlayer)addPlayer;        
                    RevokePermission(newid, target);
                    return;
                }
                newcount++;
                grabPerm2(perma, name);      
                return;    
            }
            else
            {
                if(newcount < groupData.groupD.Count)
                {
                    newcount++;
                    grabPerm2(perma, name);  
                    return;
                }
                else
                {
                    newcount = 0;
                    return;
                }
            }         
        }

        void OnUserPermissionRevoked(string name, string str)
        {
            foreach(var entry in groupData.perms)
            {
                if(entry == str)
                {
                    grabPerm2(str, name);
                }
            }
        }

        // / // / // / //
        //SaveData File//
        // / // / // / //

        void SaveData()
        {
            Interface.Oxide.DataFileSystem.WriteObject("IGather-Groups", groupData);   
            Interface.Oxide.DataFileSystem.WriteObject("IGather-Players", playerData);     
        }

        // / // / // / //
        //Language-File//
        // / // / // / //

        Dictionary<string, string> messages = new Dictionary<string, string>()
        {
            //Permissions//
            {"NoPermission", "You do not have the correct permissions to preform this command!"},
            //Groups//
            {"GroupCreated", "You have created a group with the ID of {id} and a name of {name}."},
            {"GroupCreatedStats", "Your new group has the stats of: "+ "\n" +"Resources: {resource}" + "\n" + "Quarry: {quarry}" + "\n" + "Pickup's (pickup)"},
            {"GroupAlreadyExists", "A group with the name of [{name}] already exists!"},
            {"GroupCreating", "Attempting to create a group!"},
            //Add To Group//
            {"UGNoGroupID", "There is no group of the ID: {id}."},
            {"UGAddedA", "You added {0} to the group of {1}."},
            {"UGAddedT", "You were added to group of {0}."},
            //Remove From Group To Group//
            {"RemoveFrom", "You were removed from group: {0} and you are now in group: {1}."},
            {"RemoveFromA", "You moved {0} from {1} and added him to {2}."},
            //Stats of player//
            {"GatherStats", "{player}'s stats: \n Resources: {resource} \n Quarry: {quarry} \n Pickup: {pickup}."},
            //Wipe//
            {"WGConfirmWipe", "Are you sure you wish to wipe the groups? If so do igconfirm!"},
            {"WGInactive", "There is no wipe timer active currently."},
            {"WGTimerExpired", "You ran out of time to confirm your wipe!"},
            {"WGWiping", "Wiping the statistics of groups now!"},
            {"WGWipingWarn", "{warner} is wiping the groups!"},
            //Set Gather//
            {"SGNoGroupID", "There is no group with the ID of: {0}."},
            {"SGSet", "You set {0}'s group stats to: \n Resource Stats: {1} \n Quarry Stats: {2} \n Pickup Stats: {3}."},
        };     

        // / // / // / //
        //Console Comms//
        // / // / // / //    

        [ConsoleCommand("igcreategroup")]
        void CreateGroupC(ConsoleSystem.Arg arg)
        {
            var id = groupData.groupD.Count() + 1;
            var name = arg.Args[0].ToString();

            var resource = float.Parse(arg.Args[1]);
            var quarry = float.Parse(arg.Args[2]);
            var pickup = float.Parse(arg.Args[3]);

            if (arg.Player() != null && !arg.Player().IsAdmin())
            {
                SendReply(arg, lang.GetMessage("NoPermission", this));
                return;
            }

            SendReply(arg, lang.GetMessage("GroupCreating", this));

            if(TryGroup())
            {
                    if(nameli.Contains(arg.Args[0].ToString()))
                    {
                        SendReply(arg, lang.GetMessage("GroupAlreadyExists", this).Replace("{name}", name));
                        nameli.Clear();
                        return;
                    }
                    timer.Once(5, () =>
                    {
                        if(nameli.Contains(arg.Args[0].ToString()))
                        {
                            SendReply(arg, lang.GetMessage("GroupAlreadyExists", this).Replace("{name}", name));
                            nameli.Clear();
                            return;
                        }
                    });
            }

            var info = new Groups();
            info.groupName = name;
            info.groupID = id;
            info.groupPerm = "igather."+name;
            info.groupCreater = "Console";
            info.groupTimeCreated = DateTime.Now.ToString("h:mm tt").ToString();
            info.groupPlayers = new List<ulong>();
            info.groupResourceRate = resource;
            info.groupQuarryRate = quarry;
            info.groupPickupRate = pickup;
            groupData.groupD.Add(id, info);

            groupData.perms.Add("igather."+name);
            SaveData();
            SendReply(arg, lang.GetMessage("GroupCreated", this).Replace("{name}", name).Replace("{id}", id.ToString()));
            SendReply(arg, lang.GetMessage("GroupCreatedStats", this).Replace("resource", resource.ToString()).Replace("quarry", quarry.ToString()).Replace("pickup", pickup.ToString()));      
            nameli.Clear();    
            LoadPerms();  
        } 

        [ConsoleCommand("igcollectstats")]
        void CollectStatsC(ConsoleSystem.Arg arg)  
        {
            if (arg.Player() != null && !arg.Player().IsAdmin())
            {
                SendReply(arg, lang.GetMessage("NoPermission", this));
                return;
            }
            else
            {
                object addPlayer = FindPlayerC(arg, arg.Args[0]);             
                BasePlayer target = (BasePlayer)addPlayer;        
                if(target == null) return;
                SendReply(arg, lang.GetMessage("GatherStats", this).Replace("player", target.ToString()).Replace("resource", playerData.playerD[target.userID].playerResource.ToString()).Replace("quarry", playerData.playerD[target.userID].playerQuarry.ToString()).Replace("pickup", playerData.playerD[target.userID].playerPickup.ToString()));

                return;
            }
        }

        [ConsoleCommand("igwipegroups")]
        void WipeGroupsC(ConsoleSystem.Arg arg)  
        {
            if (arg.Player() != null && !arg.Player().IsAdmin())
            {
                SendReply(arg, lang.GetMessage("NoPermission", this));
                return;
            }
            else
            {
                SendReply(arg, lang.GetMessage("WGConfirmWipe", this));
                WipeConfirmV = true;
                timer.Once(60, () =>
                {
                    if(WipeConfirmV == false) return;
                    SendReply(arg, lang.GetMessage("WGTimerExpired", this));
                    WipeConfirmV = false;
                });
            }   
        }

        [ConsoleCommand("igconfirm")]
        void ConfirmC(ConsoleSystem.Arg arg)  
        {
            if (arg.Player() != null && !arg.Player().IsAdmin())
            {
                SendReply(arg, lang.GetMessage("NoPermission", this));
                return;
            }
            else if(WipeConfirmV == false)
            {
                SendReply(arg, lang.GetMessage("WGInactive", this));  
                return;
            }
            else
            {
                WipeConfirmV = false;
                SendReply(arg, lang.GetMessage("WGWiping", this));  
                PrintWarning(lang.GetMessage("WGWipingWarn", this).Replace("warner", arg.ToString()));
                AddPlayers();
                groupData.groupD.Clear();
                LoadDefaultGroups();

                foreach(var target in newadd)
                {
                    groupData.groupD[1].groupPlayers.Add(target);
                    playerData.playerD[target].playerGroupName = groupData.groupD[1].groupName;
                    playerData.playerD[target].playerGroupID = groupData.groupD[1].groupID;
                    playerData.playerD[target].playerPickup = groupData.groupD[1].groupPickupRate;
                    playerData.playerD[target].playerResource = groupData.groupD[1].groupResourceRate;
                    playerData.playerD[target].playerQuarry = groupData.groupD[1].groupQuarryRate;
                    SaveData();
                }
            }  
        }

        // / // / // / //
        //Chat Commands//
        // / // / // / //   

        [ChatCommand("igath")]
        void cmdChat(BasePlayer player, string command, string[] args) 
        {     
            if(player == null) return;

            switch(args[0])
            {
                case "creategroup":
                    if(player.net.connection.authLevel > 1)
                    {
                        if(args.Length >= 5 || args.Length == 0) SendReply(player, "Syntax: igath creategroup (group name) (resource rate) (quarry rate) (pickup rate).");
                        var id = groupData.groupD.Count() + 1;
                        var name = args[1].ToString();

                        var resource = float.Parse(args[2]);
                        var quarry = float.Parse(args[3]);
                        var pickup = float.Parse(args[4]);

                        SendReply(player, lang.GetMessage("GroupCreating", this));

                        if(TryGroup())
                        {
                                if(nameli.Contains(args[1].ToString()))
                                {
                                    SendReply(player, lang.GetMessage("GroupAlreadyExists", this).Replace("{name}", name));
                                    nameli.Clear();
                                    return;
                                }
                                timer.Once(5, () =>
                                {
                                    if(nameli.Contains(args[1].ToString()))
                                    {
                                        SendReply(player, lang.GetMessage("GroupAlreadyExists", this).Replace("{name}", name));
                                        nameli.Clear();
                                        return;
                                    }
                                });
                        }

                        var info = new Groups();
                        info.groupName = name;
                        info.groupID = id;
                        info.groupPerm = "igather."+name;
                        info.groupCreater = player.displayName;
                        info.groupTimeCreated = DateTime.Now.ToString("h:mm tt").ToString();
                        info.groupPlayers = new List<ulong>();
                        info.groupResourceRate = resource;
                        info.groupQuarryRate = quarry;
                        info.groupPickupRate = pickup;
                        groupData.groupD.Add(id, info);

                        groupData.perms.Add("igather."+name);
                        SaveData();

                        SendReply(player, lang.GetMessage("GroupCreated", this).Replace("{name}", name).Replace("{id}", id.ToString()));
                        SendReply(player, lang.GetMessage("GroupCreatedStats", this).Replace("resource", resource.ToString()).Replace("quarry", quarry.ToString()).Replace("pickup", pickup.ToString()));      
                        nameli.Clear(); 
                        LoadPerms();      
                    }  
                    else
                    {
                        SendReply(player, lang.GetMessage("NoPermission", this));
                        return;
                    }
                break;

                case "collectstats":
                    if(player.net.connection.authLevel > 1)
                    {
                        if(args.Length >= 2 || args.Length == 0) SendReply(player, "Syntax: igath collectstats (User Name).");
                        object addPlayer = FindPlayer(player, args[1]);             
                        BasePlayer target = (BasePlayer)addPlayer;        
                        if(target == null) return;
                        SendReply(player, lang.GetMessage("GatherStats", this).Replace("player", target.ToString()).Replace("resource", playerData.playerD[target.userID].playerResource.ToString()).Replace("quarry", playerData.playerD[target.userID].playerQuarry.ToString()).Replace("pickup", playerData.playerD[target.userID].playerPickup.ToString()));             
                    }  
                    else
                    {
                        SendReply(player, lang.GetMessage("NoPermission", this));
                        return;
                    }
                break;

                case "wipegroups":
                    if(player.net.connection.authLevel > 1)
                    {
                        if(args.Length >= 2) SendReply(player, "Syntax: igath wipegroups.");
                        SendReply(player, lang.GetMessage("WGConfirmWipe", this));
                        WipeConfirmV = true;
                        timer.Once(60, () =>
                        {
                            if(WipeConfirmV == false) return;
                            SendReply(player, lang.GetMessage("WGTimerExpired", this));
                            WipeConfirmV = false;
                        });
                    }  
                    else
                    {
                        SendReply(player, lang.GetMessage("NoPermission", this));
                        return;
                    }
                break; 

                case "confirmwipe":
                    if(player.net.connection.authLevel != 2)
                    {
                        SendReply(player, lang.GetMessage("NoPermission", this));
                        return;           
                    }  
                    else if(WipeConfirmV == false)
                    {
                        SendReply(player, lang.GetMessage("WGInactive", this));  
                        return;
                    }
                    else
                    {
                        if(args.Length >= 2) SendReply(player, "Syntax: igath confirmwipe.");
                        WipeConfirmV = false;
                        SendReply(player, lang.GetMessage("WGWiping", this));  
                        PrintWarning(lang.GetMessage("WGWipingWarn", this).Replace("warner", player.displayName));
                        AddPlayers();
                        groupData.groupD.Clear();
                        LoadDefaultGroups();

                        foreach(var target in newadd)
                        {
                            groupData.groupD[1].groupPlayers.Add(target);
                            playerData.playerD[target].playerGroupName = groupData.groupD[1].groupName;
                            playerData.playerD[target].playerGroupID = groupData.groupD[1].groupID;
                            playerData.playerD[target].playerPickup = groupData.groupD[1].groupPickupRate;
                            playerData.playerD[target].playerResource = groupData.groupD[1].groupResourceRate;
                            playerData.playerD[target].playerQuarry = groupData.groupD[1].groupQuarryRate;
                            SaveData();
                        }
                    } 
                break;

                case "gather":
                    if(args.Length >= 2) SendReply(player, "Syntax: igath gather.");
                    SendReply(player, "Gather stats:\n Resource Rate: "+ playerData.playerD[player.userID].playerResource +"\n  Quarry Rate: "+ playerData.playerD[player.userID].playerQuarry +"\n  Pickup Rate: "+ playerData.playerD[player.userID].playerPickup);
                break;

                case "setgather":
                    if(player.net.connection.authLevel > 1)
                    {
                        var groupid = Convert.ToInt32(args[1]);
                        var resource = float.Parse(args[2]);
                        var quarry = float.Parse(args[3]);
                        var pickup = float.Parse(args[4]);    
                        if(args.Length >= 5 || args.Length == 0) SendReply(player, "Syntax: igath setgather (Group ID) (Resource Rate) (Quarry Rate) (Pickup Rate).");
                        if(!groupData.groupD.ContainsKey(groupid))
                        {
                            SendReply(player, lang.GetMessage("SGNoGroupID", this), groupid);
                            return;
                        }
                        else
                        {
                            groupData.groupD[groupid].groupResourceRate = resource;
                            groupData.groupD[groupid].groupQuarryRate = quarry;
                            groupData.groupD[groupid].groupPickupRate = pickup;
                            SaveData();
                            SendReply(player, lang.GetMessage("SGSet", this), groupData.groupD[groupid].groupName, resource, quarry, pickup);
                        }
                    }  
                    else
                    {
                        SendReply(player, lang.GetMessage("NoPermission", this));
                        return;
                    }
                break;

                default:
                   SendReply(player, "Syntax: /igath creategroup (group name) (resource rate) (quarry rate) (pickup rate)."); 
                   SendReply(player, "Syntax: /igath addusertogroup (User Name) (GroupID).");
                   SendReply(player, "Syntax: /igath removeuserfromgroup (User Name) (Group to be placed in).");
                   SendReply(player, "Syntax: /igath collectstats (User Name).");
                   SendReply(player, "Syntax: /igath wipegroups.");
                   SendReply(player, "Syntax: /igath gather.");
                   SendReply(player, "Syntax: /igath setgather (Group ID) (Resource Rate) (Quarry Rate) (Pickup Rate).");
                break;
            }
        }

        // / // / // / //
        //Config Gather//
        // / // / // / //   

        private void OnDispenserGather(ResourceDispenser dispenser, BaseEntity entity, Item item)
        {
            BasePlayer player = entity.ToPlayer();
            if(player == null) return;
            if(!playerData.playerD.ContainsKey(player.userID))
            {
                OnPlayerInit(player);  
            }            
            else
            {
                item.amount = (int)(item.amount * playerData.playerD[player.userID].playerResource);                
            }
        }

        private void OnQuarryGather(MiningQuarry quarry, Item item)
        {
            BasePlayer player = BasePlayer.FindByID(quarry.OwnerID) ?? BasePlayer.FindSleeping(quarry.OwnerID);           
            if(player == null) return; 
            try 
            {
                if(!playerData.playerD.ContainsKey(player.userID))
                {
                    OnPlayerInit(player);  
                }
                else
                {
                    item.amount = (int)(item.amount * playerData.playerD[player.userID].playerQuarry);                   
                }                  
            }
            catch(System.Exception)
            {
                return;
            }
        }

        private void OnCollectiblePickup(Item item, BasePlayer player)
        {
            if(player == null) return;   
            if(!playerData.playerD.ContainsKey(player.userID))
            {
                OnPlayerInit(player);  
            }            
            else
            {
                item.amount = (int)(item.amount * playerData.playerD[player.userID].playerPickup);                  
            }       
        }

        // / // / // / //
        //Find Name Gro//
        // / // / // / //   

        bool TryGroupSe()
        {
            if(groupData.groupD.ContainsKey(newcount))
            {
                nameli.Add(groupData.groupD[newcount].groupName.ToString());
                newcount++;
                TryGroupSe();      
                return true;      
            }
            else
            {
                if(newcount < groupData.groupD.Count)
                {
                    newcount++;
                    TryGroupSe();
                    return true;
                }
                else
                {
                    newcount = 0;
                    return true;
                }
            }
        }

        bool TryGroup()
        {
            if(TryGroupSe()) return true;
            else return true;
        }

        void AddPlayers()
        {
            if(groupData.groupD.ContainsKey(newcount))
            {
                foreach(var uid in groupData.groupD[newcount].groupPlayers)
                {
                    if(newadd.Contains(uid)) break;
                    newadd.Add(uid);
                }
                newcount++;
                AddPlayers();      
                return;    
            }
            else
            {
                if(newcount < groupData.groupD.Count)
                {
                    newcount++;
                    AddPlayers();
                    return;
                }
                else
                {
                    newcount = 0;
                    return;
                }
            }          
        }
        // / // / // / //
        //Find Player -//
        // / // / // / // 

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
                    SendReply(player, "Could not find a player with the name of "+ arg);
                return null;
            }
            if (foundPlayers.Count > 1)
            {
                if (player != null)
                    SendReply(player, "Found "+ foundPlayers.Count + " with the name of "+ arg);
                return null;
            }

            return foundPlayers[0];
        }   

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
                    SendReply(targer, "Could not find a player with the name of "+ arg);
                return null;
            }
            if (foundPlayers.Count > 1)
            {
                if (arg != null)
                    SendReply(targer, "Could not find a player with the name of "+ arg);
                return null;
            }

            return foundPlayers[0];
        }  

        private BasePlayer FindPlayerU(string arg)
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
                    }
                }
            }
            if (foundPlayers.Count == 0)
            {
                if (arg != null)
                return null;
            }
            if (foundPlayers.Count > 1)
            {
                if (arg != null)
                return null;
            }

            return foundPlayers[0];
        }        
    }
}
