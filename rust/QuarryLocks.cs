//USING INTERFACES//
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System;
using Oxide.Core.Plugins;
using Oxide.Core;
//USING INTERFACES//

//NAMESPACE OXIDE//
namespace Oxide.Plugins
{
    [Info("Quarry-Locks", "DylanSMR", "1.0.4", ResourceId = 1819)]
    [Description("Added customizable locks to a quarry")]
    class QuarryLocks : RustPlugin
    {
        //////////////////////////////////////////////////////////////////////////////////////
        // Configuration File Handler
        //////////////////////////////////////////////////////////////////////////////////////  
        
        void LoadDefaultConfig()
        {
            Config.Clear();
                //General Server Stuff//
                Config["ChatPrefix"] = "QLock";
                Config["ChatPrefixColor"] = "#6f60c9";
                Config["ChatColor"] = "#6f60c9";
                //Lock Stuff//
                Config["HealthWrong"] = 5;
                Config["CodeLocksNeeded"] = 5;
            Config.Save();
        } 
        
        //////////////////////////////////////////////////////////////////////////////////////
        // Data Handler
        ////////////////////////////////////////////////////////////////////////////////////// 
        
        class QuarryData
        {
            public Dictionary<ulong, ExtraData> QD = new Dictionary<ulong, ExtraData>();
            public QuarryData()
            {           
            }    
        }
        
        class MessageData
        {
            public Dictionary<ulong, ExtraMessage> MD = new Dictionary<ulong, ExtraMessage>();
            public MessageData()
            {   
            }
        }        
        
        class ExtraData
        {
            public string Name;
            public float NameID;
            public int Code;
            public bool CodeEnabled;
            public int MaxLogsAllowed;
            public int MaxLogsFromPlayer;
            public Dictionary<ulong, NewLogsFromP> LogsFromPlayer = new Dictionary<ulong, NewLogsFromP>();
            public Dictionary<ulong, string> HasAccess = new Dictionary<ulong, string>();
            public Dictionary<ulong, string> PlayersBlocked = new Dictionary<ulong, string>();      
            public ExtraData()
            {             
            }
        }
          
        class NewLogsFromP
        {
            public int Logs;
            public NewLogsFromP()
            {              
            }
        }
        
        class ExtraMessage
        {
            public string Name;
            public float NameID;
            public List<string> HasAccessed = new List<string>();
            public List<string> AttemptedAccess = new List<string>();
            public int Messages; 
            public ExtraMessage()
            {     
            }           
        }
        
        public List<string> HelpIM = new List<string>();
        public List<string> HelpIM2 = new List<string>();  
        public bool WarningE;      
        QuarryData quarryData;
        MessageData messageData;
        
        void SaveData()
        {
            Interface.Oxide.DataFileSystem.WriteObject("quarrylocks_qdata", quarryData);
            Interface.Oxide.DataFileSystem.WriteObject("quarrylocks_qmessages", messageData);              
        }
        
        void Loaded()
        {
            quarryData = Interface.GetMod().DataFileSystem.ReadObject<QuarryData>("quarrylocks_qdata");
            messageData = Interface.GetMod().DataFileSystem.ReadObject<MessageData>("quarrylocks_qmessages"); 
            LoadLangauge();          
        } 
        
        void OnPlayerInit(BasePlayer player)
        {
            if(!quarryData.QD.ContainsKey(player.userID))
            {
                var info = new ExtraData();
                info.Name = player.displayName;
                info.NameID = player.userID;
                info.Code = 1234;
                info.CodeEnabled = false;
                info.MaxLogsAllowed = 10;
                info.MaxLogsFromPlayer = 2;
                info.HasAccess = new Dictionary<ulong, string>();   
                info.PlayersBlocked = new Dictionary<ulong, string>();   
                info.LogsFromPlayer = new Dictionary<ulong, NewLogsFromP>();
                quarryData.QD.Add(player.userID, info);     
                Interface.Oxide.DataFileSystem.WriteObject("quarrylocks_qdata", quarryData);                
            }
            if(!messageData.MD.ContainsKey(player.userID))
            {
                var info = new ExtraMessage();
                info.Name = player.displayName;
                info.NameID = player.userID;
                info.Messages = 0;
                info.HasAccessed = new List<string>();
                info.AttemptedAccess = new List<string>();
                messageData.MD.Add(player.userID, info);     
                Interface.Oxide.DataFileSystem.WriteObject("quarrylocks_qmessages", messageData);     
                return;
            } 
            return;  
        }
        
        void OnPlayerSleepEnded(BasePlayer player)
        {
            if(!messageData.MD.ContainsKey(player.userID) || !quarryData.QD.ContainsKey(player.userID)) OnPlayerInit(player);
            SaveData();
            PlayerAlertMessages(player);
        }
        
        //////////////////////////////////////////////////////////////////////////////////////
        // Langauge Handler
        //////////////////////////////////////////////////////////////////////////////////////  
        
        void OnServerSave()
        {
            SaveData(); 
        }        
        
        //////////////////////////////////////////////////////////////////////////////////////
        // Langauge Handler
        //////////////////////////////////////////////////////////////////////////////////////  
        
        void LoadLangauge()
        {
			lang.RegisterMessages(new Dictionary<string,string>{
                //Regular//
                ["QL_ACCESSEDQUARRY"] = "<color='{0}'>{1}:</color><color='{2}'> {3} accessed your quarry at time | {4}!</color>",
                ["QL_ATTEMPTEDACCESS"] = "<color='{0}'>{1}:</color><color='{2}'> {3} attempted to access your quarry at time | {4}.</color>",
                ["QL_MESSAGES"] = "<color='{0}'>{1}:</color><color='{2}'> You have {3} new messages.</color>",
                ["QL_TRIEDACCESS"] = "<color='{0}'>{1}:</color><color='{2}'>{3} just tried to access your quarry!</color>",
                ["QL_GUESSED"] = "<color='{0}'>{1}:</color><color='{2}'> {3} just attempted to guess your passcode!</color>",
                ["QL_JUSTGUESSED"] = "<color='{0}'>{1}:</color><color='{2}'> {3} has just guessed or entered your pass code!</color>",
                ["QL_NEWPASSCODE"] = "<color='{0}'>{1}:</color><color='{2}'> Your new pass code is {3}. Keep it safe!</color>",
                ["QL_ISACCESSING"] = "<color='{0}'>{1}:</color><color='{2}'> {3} is looting your quarry!</color>",
                ["QL_NOFRIENDS"] = "<color='{0}'>{1}:</color><color='{2}'> No one currently knows your quarry passcode!</color>",
                ["QL_FRIENDS"] = "<color='{0}'>{1}:</color><color='{2}'> {3} knows your passcode.</color>",
                ["QL_NOBLOCKED"] = "<color='{0}'>{1}:</color><color='{2}'> No one is currently blocked!</color>",
                ["QL_ISBLOCKED"] = "<color='{0}'>{1}:</color><color='{2}'> {3} is currently blocked!</color>",
                ["QL_CODE"] = "<color='{0}'>{1}:</color><color='{2}'> {3} is your current code!</color>",
                ["QL_MESSAGESCLEARED"] = "<color='{0}'>{1}:</color><color='{2}'> All of your messages have been cleared!</color>",
                ["QL_INCORRECTCODE"] = "<color='{0}'>{1}:</color><color='{2}'> Incorrect Passcode.</color>",
                ["QL_CORRECTCODE"] = "<color='{0}'>{1}:</color><color='{2}'> Correct Passcode.</color>",
                ["QL_DCODE"] = "<color='{0}'>{1}:</color><color='{2}'> You currently have no code!</color>",
                ["QL_CANNOTBLOCKSELF"] = "<color='{0}'>{1}:</color><color='{2}'> You may not block yourself as that would be silly :).</color>",
                ["QL_CANNOTGUESSOWNCODE"] = "<color='{0}'>{1}:</color><color='{2}'> You cannot guess your own code.</color>",
                ["QL_NOTENABLED"] = "<color='{0}'>{1}:</color><color='{2}'> {3} does not have their code enabled.</color>",
                ["QL_ISENABLED"] = "<color='{0}'>{1}:</color><color='{2}'> Your code is already enabled.</color>",
                ["QL_ENABLED"] = "<color='{0}'>{1}:</color><color='{2}'> Your have enabled your code lock by using {3} code locks.</color>",
                ["QL_MESSAGE"] = "<color='{0}'>{1}:</color><color='{2}'> {3}</color>",
                ["QL_ALERTMESSAGES"] = "<color='{0}'>{1}:</color><color='{2}'> You have {3} new messagse! Do /qlock rmessage to check them!</color>",
                ["QL_MAYNOTOPEN"] = "<color='{0}'>{1}:</color><color='{2}'> You may not open this mans quarry.</color>",
                ["QL_FIXMOUSE"] = "<color='{0}'>{1}:</color><color='{2}'> Open your inventory and close it to fix your mouse.</color>",
                ["QL_NOTENOUGHLOCKS"] = "<color='{0}'>{1}:</color><color='{2}'> You only had {3} out of the {4} needed to create a code lock for your quarry.</color>",
                ["QL_ALREADYBLOCKED"] = "<color='{0}'>{1}:</color><color='{2}'> {3} is already blocked!</color>",
                ["QL_BLOCKED"] = "<color='{0}'>{1}:</color><color='{2}'> {3} is now blocked!</color>",
                ["QL_CODEDISABLED"] = "<color='{0}'>{1}:</color><color='{2}'> Your code system was disabled and you have been refunded your code locks!</color>",
                ["QL_CANNOTSETTOCURRENT"] = "<color='{0}'>{1}:</color><color='{2}'> You may not set {3} to your current code as it is your current code!</color>",
                ["QL_ALREADYFRIEND"] = "<color='{0}'>{1}:</color><color='{2}'> You are already a friend of {3}.</color>",
                ["QL_MAXLOGSET"] = "<color='{0}'>{1}:</color><color='{2}'> Your max logs total is now set too {3}.</color>",
                ["QL_MAXPLAYERLOGSET"] = "<color='{0}'>{1}:</color><color='{2}'> Your max logs per player total is now set too {3}.</color>",
                ["QL_REMOVEDFROMBLOCKED"] = "<color='{0}'>{1}:</color><color='{2}'> {3} was removed from your blocked list.</color>",
                ["QL_NOTINBLOCKED"] = "<color='{0}'>{1}:</color><color='{2}'> {3} is not in your blocked list therefor you may not remove him/her.</color>",
                ["QL_TOOMANYNUMBERS"] = "<color='{0}'>{1}:</color><color='{2}'> You entered {3} numbers while {4} is only allowed. Try again!!!</color>",
                ["QL_CODENENABLED"] = "<color='{0}'>{1}:</color><color='{2}'> You do not have the code lock system enabled!</color>",
                //Help//
                ["QLH_FRIENDHELP"] = "<color='{0}'>{1}:</color><color='{2}'> /qlock friends - Returns anyone who knows your quarry code.</color>",
                ["QLH_BLOCKEDHELP"] = "<color='{0}'>{1}:</color><color='{2}'> /qlock blocked - Returns anyone who is blocked from your code.</color>",
                ["QLH_BLOCKHELP"] = "<color='{0}'>{1}:</color><color='{2}'> /qlock block (player) - Will block a player.</color>",
                ["QLH_UNBLOCKHELP"] = "<color='{0}'>{1}:</color><color='{2}'> /qlock unblock (player) - Will unblock a player.</color>",
                ["QLH_CODEHELP"] = "<color='{0}'>{1}:</color><color='{2}'> /qlock code - Returns your quarry code.</color>",
                ["QLH_CREATECODEHELP"] = "<color='{0}'>{1}:</color><color='{2}'> /qlock createcode - Creates a code for your quarrys if you have the correct locks(must do setcode afterwards).</color>",
                ["QLH_DISABLECODEHELP"] = "<color='{0}'>{1}:</color><color='{2}'> /qlock disablecode - Disables your code and refunds the code locks.</color>",
                ["QLH_SETCODEHELP"] = "<color='{0}'>{1}:</color><color='{2}'> /qlock setcode (newcode) - Sets your code to (NewCode) if your code is enabled.</color>",
                ["QLH_ENTERCODE"] = "<color='{0}'>{1}:</color><color='{2}'> /qlock entercode (player) (code) - Unlocks a quarry if you used the right code.</color>",
                ["QLH_HELP"] = "<color='{0}'>{1}:</color><color='{2}'> /qlock help - Gives you this help page.</color>",
                ["QLH_HELP2"] = "<color='{0}'>{1}:</color><color='{2}'> /qlock help2 - Gives you the second help page.</color>",
                //Help2//
                ["QLH_RMESSAGEHELP"] = "<color='{0}'>{1}:</color><color='{2}'> /qlock rmessage - Returns all of your current messages.</color>",
                ["QLH_CLEARMESSAGESHELP"] = "<color='{0}'>{1}:</color><color='{2}'> /qlock clearmessages - Clears all of your logs|messages.</color>",
                ["QLH_SETMAXLOGSHELP"] = "<color='{0}'>{1}:</color><color='{2}'> /qlock setmaxlogs (#) - Sets your max logs allowed to (#).</color>",
                ["QLH_SETMAXLOGS(P)HELP"] = "<color='{0}'>{1}:</color><color='{2}'> /qlock help - Gives you this help page.</color>",
			}, this);                
        } 
        
        private string GetMessage(string name, string sid = null) {
			return lang.GetMessage(name, this, sid);
		}              
        
        //////////////////////////////////////////////////////////////////////////////////////
        // SendReplyHelp
        //////////////////////////////////////////////////////////////////////////////////////        
        
        void SendReplyHelp(BasePlayer player)
        {
            SendReply(player, string.Format(GetMessage("QLH_FRIENDHELP", player.UserIDString), Config["ChatPrefixColor"], Config["ChatPrefix"], Config["ChatColor"]));
            SendReply(player, string.Format(GetMessage("QLH_BLOCKEDHELP", player.UserIDString), Config["ChatPrefixColor"], Config["ChatPrefix"], Config["ChatColor"]));
            SendReply(player, string.Format(GetMessage("QLH_BLOCKHELP", player.UserIDString), Config["ChatPrefixColor"], Config["ChatPrefix"], Config["ChatColor"]));
            SendReply(player, string.Format(GetMessage("QLH_UNBLOCKHELP", player.UserIDString), Config["ChatPrefixColor"], Config["ChatPrefix"], Config["ChatColor"]));
            SendReply(player, string.Format(GetMessage("QLH_CODEHELP", player.UserIDString), Config["ChatPrefixColor"], Config["ChatPrefix"], Config["ChatColor"]));
            SendReply(player, string.Format(GetMessage("QLH_CREATECODEHELP", player.UserIDString), Config["ChatPrefixColor"], Config["ChatPrefix"], Config["ChatColor"]));
            SendReply(player, string.Format(GetMessage("QLH_DISABLECODEHELP", player.UserIDString), Config["ChatPrefixColor"], Config["ChatPrefix"], Config["ChatColor"]));
            SendReply(player, string.Format(GetMessage("QLH_SETCODEHELP", player.UserIDString), Config["ChatPrefixColor"], Config["ChatPrefix"], Config["ChatColor"]));
            SendReply(player, string.Format(GetMessage("QLH_ENTERCODE", player.UserIDString), Config["ChatPrefixColor"], Config["ChatPrefix"], Config["ChatColor"]));
            SendReply(player, string.Format(GetMessage("QLH_HELP", player.UserIDString), Config["ChatPrefixColor"], Config["ChatPrefix"], Config["ChatColor"]));
            SendReply(player, string.Format(GetMessage("QLH_HELP2", player.UserIDString), Config["ChatPrefixColor"], Config["ChatPrefix"], Config["ChatColor"]));
        }
        
        void SendReplyHelp2(BasePlayer player)
        {
            SendReply(player, string.Format(GetMessage("QLH_RMESSAGEHELP", player.UserIDString), Config["ChatPrefixColor"], Config["ChatPrefix"], Config["ChatColor"]));
            SendReply(player, string.Format(GetMessage("QLH_CLEARMESSAGESHELP", player.UserIDString), Config["ChatPrefixColor"], Config["ChatPrefix"], Config["ChatColor"]));
            SendReply(player, string.Format(GetMessage("QLH_SETMAXLOGSHELP", player.UserIDString), Config["ChatPrefixColor"], Config["ChatPrefix"], Config["ChatColor"]));
            SendReply(player, string.Format(GetMessage("QLH_SETMAXLOGS(P)HELP", player.UserIDString), Config["ChatPrefixColor"], Config["ChatPrefix"], Config["ChatColor"]));            
        }
        
        //////////////////////////////////////////////////////////////////////////////////////
        // FindPlayer(string arg)
        //////////////////////////////////////////////////////////////////////////////////////               
        
        private object FindPlayer(string arg)
        {
            var foundPlayers = new List<BasePlayer>();
            ulong steamid;
            ulong.TryParse(arg, out steamid);
            string lowerarg = arg.ToLower();

            foreach (var p in BasePlayer.activePlayerList)
            {
                if (steamid != 0L)
                    if (p.userID == steamid)
                    {
                        foundPlayers.Clear();
                        foundPlayers.Add(p);
                        return foundPlayers;
                    }
                string lowername = p.displayName.ToLower();
                if (lowername.Contains(lowerarg))
                {
                    foundPlayers.Add(p);
                }
            }
            return foundPlayers[0];
        } 
        
        //////////////////////////////////////////////////////////////////////////////////////
        // OnPlayerJoin alert.
        //////////////////////////////////////////////////////////////////////////////////////         
              
        void PlayerAlertMessages(BasePlayer player)
        {
            var totalalerts = messageData.MD[player.userID].HasAccessed.Count + messageData.MD[player.userID].AttemptedAccess.Count;
            SendReply(player, string.Format(GetMessage("QL_ALERTMESSAGES", player.UserIDString), Config["ChatPrefixColor"], Config["ChatPrefix"], Config["ChatColor"], totalalerts));
            return;    
        }      
               
        //////////////////////////////////////////////////////////////////////////////////////
        // ChatCommands(qlock)
        ////////////////////////////////////////////////////////////////////////////////////// 
        
        [ChatCommand("qlock")]
        void cmdQLock(BasePlayer player, string command, string[] args)
        {
            if(args.Length == 0 || args.Length >= 5)
            {
                SendReplyHelp(player); 
                return;
            }              
            switch(args[0])
            {                        
                case "friends":
                    if(args.Length >= 2 || args.Length == 0){ SendReplyHelp(player); return; }
                    if(quarryData.QD[player.userID].HasAccess.Count >= 1)
                    {
                        foreach(var friend in quarryData.QD[player.userID].HasAccess)
                        {
                            SendReply(player, string.Format(GetMessage("QL_FRIENDS", player.UserIDString), Config["ChatPrefixColor"], Config["ChatPrefix"], Config["ChatColor"], friend)); 
                            return;   
                        }    
                    }
                    else
                    {
                        SendReply(player, string.Format(GetMessage("QL_NOFRIENDS", player.UserIDString), Config["ChatPrefixColor"], Config["ChatPrefix"], Config["ChatColor"]));
                        return;
                    }
                break;
                
                case "blocked":
                    if(args.Length >= 2 || args.Length == 0){ SendReplyHelp(player); return; }
                    if(quarryData.QD[player.userID].PlayersBlocked.Count >= 1)
                    {
                        foreach(var block in quarryData.QD[player.userID].PlayersBlocked)
                        {
                            SendReply(player, string.Format(GetMessage("QL_ISBLOCKED", player.UserIDString), Config["ChatPrefixColor"], Config["ChatPrefix"], Config["ChatColor"], block)) ;   
                            return;
                        }    
                    }
                    else
                    {
                        SendReply(player, string.Format(GetMessage("QL_NOBLOCKED", player.UserIDString), Config["ChatPrefixColor"], Config["ChatPrefix"], Config["ChatColor"]));
                        return;
                    }
                break;
                
                case "code":
                    if(args.Length >= 2 || args.Length == 0){ SendReplyHelp(player); return; }
                    if(quarryData.QD[player.userID].Code != 1111)
                    {
                        SendReply(player, string.Format(GetMessage("QL_CODE", player.UserIDString), Config["ChatPrefixColor"], Config["ChatPrefix"], Config["ChatColor"], quarryData.QD[player.userID].Code));
                        return;    
                    }
                    else
                    {
                        SendReply(player, string.Format(GetMessage("QL_DCODE", player.UserIDString), Config["ChatPrefixColor"], Config["ChatPrefix"], Config["ChatColor"]));   
                        return;
                    }
                break;
                
                case "setcode": 
                    if(args.Length >= 3 || args.Length == 0 || args.Length == 1)
                    {
                        SendReplyHelp(player);
                        return;
                    }                            
                    if(quarryData.QD[player.userID].CodeEnabled == false)
                    {
                        SendReply(player, string.Format(GetMessage("QL_CODENENABLED", player.UserIDString), Config["ChatPrefixColor"], Config["ChatPrefix"], Config["ChatColor"]));   
                        return;
                    }
                    if(quarryData.QD[player.userID].Code == Convert.ToInt32(args[1]))
                    {
                        SendReply(player, string.Format(GetMessage("QL_CANNOTSETTOCURRENT", player.UserIDString), Config["ChatPrefixColor"], Config["ChatPrefix"], Config["ChatColor"], args[1]));  
                        return;
                    }
                    if(args[1].Length >= 5)
                    {
                        SendReply(player, string.Format(GetMessage("QL_TOOMANYNUMBERS", player.UserIDString), Config["ChatPrefixColor"], Config["ChatPrefix"], Config["ChatColor"], args[1].Length, 4));   
                        return;
                    }
                    var newCode = args[1];                   
                    quarryData.QD[player.userID].Code = Convert.ToInt32(newCode);
                    quarryData.QD[player.userID].HasAccess.Clear();
                    SaveData();
                    SendReply(player, string.Format(GetMessage("QL_NEWPASSCODE", player.UserIDString), Config["ChatPrefixColor"], Config["ChatPrefix"], Config["ChatColor"], newCode));  
                break;
                
                case "entercode":
                    if(args.Length >= 4 || args.Length == 0 || args.Length == 2 || args.Length == 1)
                    {
                        SendReplyHelp(player);
                        return;
                    }
                    object addPlayer = FindPlayer(args[1]);             
                    BasePlayer target = (BasePlayer)addPlayer;                 
                    if(quarryData.QD[target.userID].HasAccess.ContainsKey(player.userID))
                    {
                        SendReply(player, string.Format(GetMessage("QL_ALREADYFRIEND", player.UserIDString), Config["ChatPrefixColor"], Config["ChatPrefix"], Config["ChatColor"], target.displayName)); 
                        return;
                    }                                                              
                    var targetCode = quarryData.QD[target.userID].Code.ToString();
                    var guessCode = args[2];
                    if(target == player || player == target)
                    {
                        Effect.server.Run("assets/prefabs/locks/keypad/effects/lock.code.denied.prefab", player.transform.position);
                        SendReply(player, string.Format(GetMessage("You may not enter your own code!", player.UserIDString), Config["ChatPrefixColor"], Config["ChatPrefix"], Config["ChatColor"]));  
                        return;
                    }                        
                    if(quarryData.QD[target.userID].CodeEnabled)     
                    {
                        if(guessCode == targetCode)
                        {
                            quarryData.QD[target.userID].HasAccess.Add(player.userID, player.displayName);
                            SaveData();
                            SendReply(target, string.Format(GetMessage("QL_JUSTGUESSED", target.UserIDString), Config["ChatPrefixColor"], Config["ChatPrefix"], Config["ChatColor"], target.displayName)); 
                            SendReply(player, string.Format(GetMessage("QL_CORRECTCODE", player.UserIDString), Config["ChatPrefixColor"], Config["ChatPrefix"], Config["ChatColor"])); 
                            Effect.server.Run("assets/prefabs/locks/keypad/effects/lock.code.updated.prefab", player.transform.position);
                            Effect.server.Run("assets/prefabs/locks/keypad/effects/lock.code.updated.prefab", target.transform.position);
                            return;     
                        }
                        else
                        {
                            SendReply(target, string.Format(GetMessage("QL_INCORRECTCODE", target.UserIDString), Config["ChatPrefixColor"], Config["ChatPrefix"], Config["ChatColor"]));
                            timer.Repeat(0.1f, Convert.ToInt32(Config["HealthWrong"]), () => player.health--); 
                            Effect.server.Run("assets/prefabs/locks/keypad/effects/lock.code.denied.prefab", player.transform.position);
                            foreach( var p in BasePlayer.activePlayerList )
                            {
                                try 
                                {
                                    if(target = p)
                                    {
                                        SendReply(target, string.Format(GetMessage("QL_GUESSED", target.UserIDString), Config["ChatPrefixColor"], Config["ChatPrefix"], Config["ChatColor"], player.displayName)); 
                                        return;                                              
                                    }
                                    else 
                                    {
                                        return;
                                    }    
                                }
                                catch
                                {
                                    messageData.MD[target.userID].AttemptedAccess.Add(string.Format(GetMessage("GL_AAADD", target.UserIDString), Config["ChatPrefixColor"], Config["ChatPrefix"], Config["ChatColor"], player.displayName, DateTime.Now.ToString("h:mm tt")));
                                    messageData.MD[target.userID].Messages++;
                                    return;                                     
                                }
                            }  
                        }
                    }
                    else
                    {
                        SendReply(target, string.Format(GetMessage("QL_NOTENABLED", target.UserIDString), Config["ChatPrefixColor"], Config["ChatPrefix"], Config["ChatColor"], target.displayName));
                        return;
                    }                             
                break;
                
                case "createcode":
                    if(args.Length >= 2 || args.Length == 0){ SendReplyHelp(player); return; }
                    if(quarryData.QD[player.userID].CodeEnabled)
                    {
                        SendReply(player, string.Format(GetMessage("QL_ISENABLED", player.UserIDString), Config["ChatPrefixColor"], Config["ChatPrefix"], Config["ChatColor"]));
                        return;                        
                    }
                    else
                    {
                        int codelocks = player.inventory.GetAmount(-975723312);
                        if(codelocks >= Convert.ToInt32(Config["CodeLocksNeeded"]))
                        {
                            player.inventory.Take(null, -975723312, Convert.ToInt32(Config["CodeLocksNeeded"]));
                            SendReply(player, string.Format(GetMessage("QL_ENABLED", player.UserIDString), Config["ChatPrefixColor"], Config["ChatPrefix"], Config["ChatColor"], Config["CodeLocksNeeded"].ToString()));
                            quarryData.QD[player.userID].CodeEnabled = true; 
                            SaveData();
                            return;   
                        }
                        else
                        {
                            SendReply(player, string.Format(GetMessage("QL_NOTENOUGHLOCKS", player.UserIDString), Config["ChatPrefixColor"], Config["ChatPrefix"], Config["ChatColor"], codelocks , Convert.ToInt32(Config["CodeLocksNeeded"])));
                            return;    
                        } 
                    }
                break;
                
                case "block":
                    if(args.Length >= 3 || args.Length == 0 || args.Length == 1)
                    {
                        SendReplyHelp(player);
                        return;
                    }                               
                    object newPlayer = FindPlayer(args[1]);             
                    BasePlayer blocker = (BasePlayer)newPlayer;   
                    if(blocker == player || player == blocker)
                    {
                        Effect.server.Run("assets/prefabs/locks/keypad/effects/lock.code.denied.prefab", player.transform.position);
                        SendReply(player, string.Format(GetMessage("QL_CANNOTBLOCKSELF", player.UserIDString), Config["ChatPrefixColor"], Config["ChatPrefix"], Config["ChatColor"]));  
                        return;
                    }                                             
                    if(quarryData.QD[player.userID].PlayersBlocked.ContainsKey(player.userID))
                    {
                        SendReply(player, string.Format(GetMessage("QL_ALREADYBLOCKED", player.UserIDString), Config["ChatPrefixColor"], Config["ChatPrefix"], Config["ChatColor"], blocker.displayName));
                        return; 
                    }  
                    else
                    {
                        quarryData.QD[player.userID].PlayersBlocked.Add(player.userID, player.displayName);   
                        SendReply(player, string.Format(GetMessage("QL_BLOCKED", player.UserIDString), Config["ChatPrefixColor"], Config["ChatPrefix"], Config["ChatColor"], blocker.displayName));
                        SaveData();
                    }              
                break;
                
                case "rmessage":
                    if(args.Length >= 2 || args.Length == 0){ SendReplyHelp(player); return; }
                    SendReply(player, "<color='#66ff33'>Tried Access:</color>");
                    foreach(var m in messageData.MD[player.userID].AttemptedAccess)
                    {
                        SendReply(player, string.Format(GetMessage("QL_MESSAGE", player.UserIDString), Config["ChatPrefixColor"], Config["ChatPrefix"], Config["ChatColor"], m));
                    }
                    SendReply(player, "<color='#66ff33'>Has Accessed:</color>");
                    foreach(var mm in messageData.MD[player.userID].HasAccessed)
                    {
                        SendReply(player, string.Format(GetMessage("QL_MESSAGE", player.UserIDString), Config["ChatPrefixColor"], Config["ChatPrefix"], Config["ChatColor"], mm));    
                    }
                break;
                
                case "unblock":
                    if(args.Length >= 3 || args.Length == 0 || args.Length == 1){ SendReplyHelp(player); return; }
                    else
                    {
                        object removePlayer = FindPlayer(args[1]);             
                        BasePlayer unblock = (BasePlayer)removePlayer;  
                        
                        if(quarryData.QD[player.userID].PlayersBlocked.ContainsKey(unblock.userID))
                        {
                            SendReply(player, string.Format(GetMessage("QL_REMOVEDFROMBLOCKED", player.UserIDString), Config["ChatPrefixColor"], Config["ChatPrefix"], Config["ChatColor"], unblock.displayName)); 
                            quarryData.QD[player.userID].PlayersBlocked.Remove(unblock.userID);
                            SaveData();
                        }  
                        else
                        {
                            SendReply(player, string.Format(GetMessage("QL_NOTINBLOCKED", player.UserIDString), Config["ChatPrefixColor"], Config["ChatPrefix"], Config["ChatColor"], args[1])); 
                            return;   
                        }                          
                    }
                break;
                
                case "setmaxlogs":
                if(args.Length >= 3 || args.Length == 0 || args.Length == 1){ SendReplyHelp(player); return; }
                    quarryData.QD[player.userID].MaxLogsAllowed = Convert.ToInt32(args[1]);
                    SaveData();
                    SendReply(player, string.Format(GetMessage("QL_MAXLOGSET", player.UserIDString), Config["ChatPrefixColor"], Config["ChatPrefix"], Config["ChatColor"], args[1]));   
                break;
                
                case "setmaxlogs(p)":
                if(args.Length >= 3 || args.Length == 0 || args.Length == 1){ SendReplyHelp(player); return; }
                    quarryData.QD[player.userID].MaxLogsFromPlayer = Convert.ToInt32(args[1]);
                    SaveData();
                    SendReply(player, string.Format(GetMessage("QL_MAXPLAYERLOGSET", player.UserIDString), Config["ChatPrefixColor"], Config["ChatPrefix"], Config["ChatColor"], args[1]));                       
                break;
                
                case "disablecode":
                    if(quarryData.QD[player.userID].CodeEnabled == false)
                    {
                        SendReply(player, string.Format(GetMessage("QL_CODENENABLED", player.UserIDString), Config["ChatPrefixColor"], Config["ChatPrefix"], Config["ChatColor"]));   
                        return;
                    }
                    else
                    {
                        SendReply(player, string.Format(GetMessage("QL_CODEDISABLED", player.UserIDString), Config["ChatPrefixColor"], Config["ChatPrefix"], Config["ChatColor"])); 
                        quarryData.QD[player.userID].CodeEnabled = false;
                        SaveData();
                        player.inventory.GiveItem(ItemManager.CreateByName("lock.code", Convert.ToInt32(Config["CodeLocksNeeded"])), player.inventory.containerMain);
                        return;
                    }                    
                break;
                
                case "clearmessages":
                    messageData.MD[player.userID].AttemptedAccess.Clear();
                    messageData.MD[player.userID].HasAccessed.Clear();
                    quarryData.QD[player.userID].LogsFromPlayer.Clear();
                    SendReply(player, string.Format(GetMessage("QL_MESSAGESCLEARED", player.UserIDString), Config["ChatPrefixColor"], Config["ChatPrefix"], Config["ChatColor"])); 
                    SaveData();
                break;
                
                case "help":
                    SendReplyHelp(player);
                    return;
                break;
                
                case "help2":
                    SendReplyHelp2(player);
                    return;
                break;
                
                default:
                    SendReplyHelp(player);
                    return;
                break;
            }    
        } 
        
        
        
        [HookMethod("OnLootEntity")]
        void OnLootEntity(BasePlayer looter, BaseEntity entry)
        {
            try 
            {
                if (entry is ResourceExtractorFuelStorage)
                {
                    List<BaseEntity> nearby = new List<BaseEntity>();
                    Vis.Entities(entry.transform.position, 2, nearby);
                    MiningQuarry quarry = null;
                    foreach (var ent in nearby)               
                        if (ent is MiningQuarry)
                            quarry = ent.GetComponent<MiningQuarry>();
                            
                    BasePlayer owner = BasePlayer.FindByID(quarry.OwnerID);                           
                    if (quarry != null)
                    {
                        if (looter.userID == owner.userID || quarryData.QD[owner.userID].HasAccess.ContainsKey(looter.userID) && !quarryData.QD[owner.userID].PlayersBlocked.ContainsKey(looter.userID))
                        {
                            if(looter.userID != owner.userID)
                            {
                                if(quarryData.QD[owner.userID].LogsFromPlayer.ContainsKey(looter.userID))
                                {
                                    if(quarryData.QD[owner.userID].LogsFromPlayer[looter.userID].Logs < quarryData.QD[owner.userID].MaxLogsFromPlayer && quarryData.QD[owner.userID].LogsFromPlayer[looter.userID].Logs < quarryData.QD[owner.userID].MaxLogsAllowed)
                                    {
                                        quarryData.QD[owner.userID].LogsFromPlayer[looter.userID].Logs++;
                                        SendReply(owner, string.Format(GetMessage("QL_ACCESSEDQUARRY", owner.UserIDString), Config["ChatPrefixColor"], Config["ChatPrefix"], Config["ChatColor"], looter.displayName, DateTime.Now.ToString("h:mm tt")));
                                        messageData.MD[owner.userID].HasAccessed.Add(string.Format(GetMessage("QL_ACCESSEDQUARRY", owner.UserIDString), Config["ChatPrefixColor"], Config["ChatPrefix"], Config["ChatColor"], looter.displayName, DateTime.Now.ToString("h:mm tt"))); 
                                        SaveData();                                        
                                    }
                                    else
                                    {
                                        SendReply(owner, string.Format(GetMessage("QL_ACCESSEDQUARRY", owner.UserIDString), Config["ChatPrefixColor"], Config["ChatPrefix"], Config["ChatColor"], looter.displayName, DateTime.Now.ToString("h:mm tt")));
                                        return;
                                    }    
                                }
                                else
                                {
                                    messageData.MD[owner.userID].HasAccessed.Add(string.Format(GetMessage("QL_ACCESSEDQUARRY", owner.UserIDString), Config["ChatPrefixColor"], Config["ChatPrefix"], Config["ChatColor"], looter.displayName, DateTime.Now.ToString("h:mm tt")));   
                                    quarryData.QD[owner.userID].LogsFromPlayer.Add(looter.userID, new NewLogsFromP());
                                    quarryData.QD[owner.userID].LogsFromPlayer[looter.userID].Logs = 1;
                                    SaveData();
                                }                                                    
                            }                        
                        }
                        else
                        {
                            if (owner != null)
                            {
                                SendReply(looter, string.Format(GetMessage("QL_MAYNOTOPEN", looter.UserIDString), Config["ChatPrefixColor"], Config["ChatPrefix"], Config["ChatColor"]));
                                SendReply(looter, string.Format(GetMessage("QL_FIXMOUSE", looter.UserIDString), Config["ChatPrefixColor"], Config["ChatPrefix"], Config["ChatColor"]));
                                looter.SetPlayerFlag(BasePlayer.PlayerFlags.ReceivingSnapshot, true);
                                looter.UpdateNetworkGroup();
                                looter.SendNetworkUpdateImmediate(false);
                                looter.ClientRPCPlayer(null, looter, "StartLoading", null, null, null, null, null);
                                looter.SendFullSnapshot();
                                if(quarryData.QD[owner.userID].LogsFromPlayer.ContainsKey(looter.userID))
                                {
                                    if(quarryData.QD[owner.userID].LogsFromPlayer[looter.userID].Logs != quarryData.QD[owner.userID].MaxLogsFromPlayer && quarryData.QD[owner.userID].MaxLogsAllowed != quarryData.QD[owner.userID].LogsFromPlayer[looter.userID].Logs)
                                    {
                                        quarryData.QD[owner.userID].LogsFromPlayer[looter.userID].Logs++;
                                        messageData.MD[owner.userID].AttemptedAccess.Add(string.Format(GetMessage("QL_ATTEMPTEDACCESS", owner.UserIDString), Config["ChatPrefixColor"], Config["ChatPrefix"], Config["ChatColor"], looter.displayName, DateTime.Now.ToString("h:mm tt")));   
                                        SaveData();  
                                        return;                                      
                                    }
                                    else
                                    {
                                        SendReply(owner, string.Format(GetMessage("QL_ATTEMPTEDACCESS", owner.UserIDString), Config["ChatPrefixColor"], Config["ChatPrefix"], Config["ChatColor"], looter.displayName, DateTime.Now.ToString("h:mm tt")));
                                        return;
                                    }    
                                }
                                else
                                {
                                    messageData.MD[owner.userID].HasAccessed.Add(string.Format(GetMessage("ATTEMPTEDACCESS", owner.UserIDString), Config["ChatPrefixColor"], Config["ChatPrefix"], Config["ChatColor"], looter.displayName, DateTime.Now.ToString("h:mm tt")));   
                                    quarryData.QD[owner.userID].LogsFromPlayer.Add(looter.userID, new NewLogsFromP());
                                    quarryData.QD[owner.userID].LogsFromPlayer[looter.userID].Logs = 1;
                                    SaveData();
                                    return;
                                }                                                        
                            }
                            return;
                        }
                    }
                }                
            }
            catch(System.Exception)
            {
                if(WarningE == false)
                {
                    PrintWarning("Error with hook: OnLootEntity! Please consider removing all quarrys installed before the plugin.");    
                    WarningE = true;
                    timer.Once(60, () => 
                    {
                       WarningE = false; 
                    });
                    throw;
                }                                 
            }
        }              
    }
}