using System.Collections.Generic;
using Oxide.Core;
using Oxide.Core.Configuration;
using System.Linq;
using Oxide.Core.Libraries.Covalence;
using Newtonsoft.Json.Linq;
using System;

namespace Oxide.Plugins
{
    [Info("Clans", "k1lly0u", "0.1.31", ResourceId = 2087)]
    class Clans : CovalencePlugin
    {
        #region Fields        
        StoredData clanData;
        private DynamicConfigFile data;

        public Dictionary<string, string> playerClans;
        public Dictionary<string, Clan> clanCache;

        static Clans clans;
        private bool Initiated = false;
        #endregion
        
        #region Oxide Hooks
        void Loaded()
        {
            data = Interface.Oxide.DataFileSystem.GetFile("clans_data");
            lang.RegisterMessages(Messages, this);
            clans = this;
            clanCache = new Dictionary<string, Clan>();
            playerClans = new Dictionary<string, string>();
        }
        void OnServerInitialized()
        {
            LoadVariables();
            LoadData();
            FillClanList();            
            Initiated = true;
            SaveLoop();
            foreach (var player in players.Connected)
                OnUserConnected(player);
        }
        void Unload() => SaveData();
        void OnUserConnected(IPlayer player)
        {
            if (Initiated)
            {
                timer.Once(3, () =>
                {
                    if (player == null) return;
                    if (playerClans.ContainsKey(player.Id))
                    {
                        var clan = clanCache[playerClans[player.Id]];
                        clan.onlineMembers.Add(player.Id);
                        if (clan.members.ContainsKey(player.Id))
                        {
                            if (clan.members[player.Id] != player.Name)
                                clan.members[player.Id] = player.Name;

                            if (configData.Options.ShowJoinMessage)
                                clan.Broadcast(string.Format(msg("playerCon"), player.Name));
                        }
                        //if (clanData.outstandingMessages.ContainsKey(player.Id))
                            //Reply(player, string.Format(msg("outstandingMsgs", player.Id), clanData.outstandingMessages[player.Id].Count));
                    }
                });
            }                     
        }
        void OnUserDisconnected(IPlayer player)
        {
            if (Initiated)
            {
                if (configData.Options.ShowLeaveMessage)
                {
                    if (player == null) return;
                    if (playerClans.ContainsKey(player.Id))
                    {
                        var clan = clanCache[playerClans[player.Id]];
                        clan.onlineMembers.Remove(player.Id);
                        clan.Broadcast(string.Format(msg("playerDiscon"), player.Name));
                    }
                }
            }
        }
        #endregion

        #region Functions
        void FillClanList()
        {
            foreach(var clan in clanCache)
            {
                clan.Value.onlineMembers.Clear();
                foreach (var member in clan.Value.members)
                {
                    playerClans.Add(member.Key, clan.Key);
                }
            }
        }        
        public bool IsClanMember(string id) => playerClans.ContainsKey(id);
        private Clan FindClanByID(string id)
        {
            if (IsClanMember(id))
            {
                var clanName = playerClans[id];
                var clan = FindClanByTag(clanName);
                if (clan != null)
                    return clan;
            }
            return null;
        }
        public Clan FindClanByTag(string tag)
        {
            Clan clan;
            if (clanCache.TryGetValue(tag, out clan))
                return clan;
            return null;
        }
       
        void Reply(IPlayer player, string message, string message2 = null)
        {
            var formatMsg = $"{configData.Messaging.MSG}{message}</color>";
            if (!string.IsNullOrEmpty(message2))
                formatMsg = $"{configData.Messaging.Main}{message2}</color> {formatMsg}";
            player.Reply(formatMsg);
        }
        void ReplyKey(IPlayer player, string msg, string arg)
        {
            var message = $"{configData.Messaging.MSG}{msg}</color>".Replace("{0}", $"</color>{configData.Messaging.Main}{arg}</color>{configData.Messaging.MSG}");
            if (message.StartsWith("</color>")) message = message.Substring(9).Trim();            
            player.Reply(message);
        }
        private IPlayer FindPlayer(IPlayer player, string arg)
        {
            var targets = from p in players.All
                         where (p.Name.ToLower().Contains(arg.ToLower()) ? true : p.Id == arg)
                         select p;

            if (targets.Count() == 0)
            {
                if (player != null)
                    player.Reply(msg("noPlayers", player.Id));
                return null;
            }
            if (targets.Count() > 1)
            {
                if (player != null)
                    player.Reply(msg("multiPlayers", player.Id));
                return null;
            }
            if (targets.Single() != null)
                return targets.Single();
            else player.Reply(msg("noPlayers", player.Id));
            return null;
        }
        #endregion

        #region Commands       
        [Command("clan")]
        void cmdClan(IPlayer player, string command, string[] args)
        {
            if (Initiated)
            {
                if (args == null || args.Length == 0)
                {
                    Reply(player, "", $"{Title}  v {Version}");
                    ReplyKey(player, msg("cMessHelp", player.Id), "/c <message>");
                    ReplyKey(player, msg("aMessHelp", player.Id), "/a <message>");
                    ReplyKey(player, msg("clanHelp", player.Id), "/clanhelp");
                    ReplyKey(player, msg("clanMembers", player.Id), "/clan members");
                    return;
                }
                switch (args[0].ToLower())
                {
                    
                    case "create":
                        CreateClan(player, args);
                        return;  
                    case "join":
                        if (args.Length == 2)                        
                            JoinClan(player, args[1]);                        
                        else Reply(player, "/clan join <tag>");
                        return; 
                    case "leave":
                        LeaveClan(player);
                        return;
                    case "invite":
                        if (args.Length >= 2)                        
                            InviteMember(player, args);                        
                        else Reply(player, "", msg("noName", player.Id));
                        return;
                    case "kick":
                        if (args.Length >= 2)                        
                            KickMember(player, args[1]);                        
                        else Reply(player, "", msg("noID", player.Id));
                        return;
                    case "members":
                        ShowMembers(player);
                        return;
                    case "promote":
                        if (args.Length >= 2)                        
                            PromoteMember(player, args[1]);                        
                        else Reply(player, "", msg("noID", player.Id));
                        return;                    
                    case "demote":
                        if (args.Length >= 2)                        
                            DemoteMember(player, args[1]);                        
                        else Reply(player, "", msg("noID", player.Id));
                        return;                    
                    case "disband":
                        DisbandClan(player);
                        return;                   
                    case "ally":                        
                        Alliance(player, args);                        
                        return;                    
                    default:
                        cmdClanHelp(player, "clanhelp", null);
                        break;
                }
            }
        }
        [Command("c")]
        void cmdClanChat(IPlayer player, string command, string[] args)
        {
            if (Initiated)
            {
                if (IsClanMember(player.Id))
                {
                    var clanName = playerClans[player.Id];
                    if (!string.IsNullOrEmpty(clanName))
                    {
                        var clan = clanCache[clanName];
                        clan.Broadcast($"{player.Name} : {string.Join(" ", args)}");
                        return;
                    }
                }
                Reply(player, "", msg("noClanData", player.Id));
            }
        }
        [Command("a")]
        void cmdAllianceChat(IPlayer player, string command, string[] args)
        {
            if (Initiated)
            {
                if (IsClanMember(player.Id))
                {
                    var clanName = playerClans[player.Id];
                    if (!string.IsNullOrEmpty(clanName))
                    {
                        var clan = clanCache[clanName];
                        if (clan.clanAlliances.Count > 0)
                        {
                            foreach (var clanAllyName in clan.clanAlliances)
                            {
                                var clanAlly = clanCache[clanAllyName];
                                clanAlly.Broadcast($"{player.Name} : {string.Join(" ", args)}", clan.clanTag);
                            }
                            clan.Broadcast($"{player.Name} : {string.Join(" ", args)}", clan.clanTag);
                            return;
                        }
                        else Reply(player, "", msg("noClanAlly", player.Id));
                    }
                }
                Reply(player, "", msg("noClanData", player.Id));
            }
        }
        //[Command("cmessage")]
        //void cmdCMessage(IPlayer player, string command, string[] args)
        //{
        //    if (Initiated)
        //    {
        //        if (clanData.outstandingMessages.ContainsKey(player.Id))
        //        {
        //            if (args.Length > 0 && args[0].ToLower() == "clear")
        //            {
        //                clanData.outstandingMessages.Remove(player.Id);
        //                Reply(player, "", msg("clearedMsg", player.Id));
        //                return;
        //            }
        //            foreach (var msg in clanData.outstandingMessages[player.Id])
        //            {
        //                Reply(player, "", msg);
        //            }
        //            Reply(player, "", msg("clearSyn", player.Id));
        //        }
        //        else Reply(player, "", msg("noOM", player.Id));
        //    }
        //}
        [Command("clanhelp")]
        void cmdClanHelp(IPlayer player, string command, string[] args)
        {
            if (Initiated)
            {
                if (args == null || args.Length == 0)
                {
                    Reply(player, "", msg("comHelp", player.Id));
                    ReplyKey(player, msg("memHelp", player.Id), "/clanhelp member");
                    ReplyKey(player, msg("modHelp", player.Id), "/clanhelp moderator");
                    ReplyKey(player, msg("ownHelp", player.Id), "/clanhelp owner");
                    return;
                }
                switch (args[0].ToLower())
                {
                    case "member":
                        Reply(player, "", msg("memCom", player.Id));
                        ReplyKey(player, msg("cMessHelp", player.Id), "/c <message>");
                        ReplyKey(player, msg("aMessHelp", player.Id), "/a <message>");
                        ReplyKey(player, msg("createHelp", player.Id), "/clan create <tag>");
                        ReplyKey(player, msg("joinHelp", player.Id), "/clan join <tag>");
                        ReplyKey(player, msg("leaveHelp", player.Id), "/clan leave");
                        return;
                    case "moderator":
                        Reply(player, "", msg("modCom", player.Id));
                        ReplyKey(player, msg("inviteHelp", player.Id), "/clan invite <playername>");
                        ReplyKey(player, msg("cancelHelp", player.Id), "/clan invite cancel <partialname/ID>");
                        ReplyKey(player, msg("kickHelp", player.Id), "/clan kick <partialname/ID>");
                        return;
                    case "owner":
                        Reply(player, "", msg("ownerCom", player.Id));
                        ReplyKey(player, msg("promoteHelp", player.Id), "/clan promote <playername>");
                        ReplyKey(player, msg("demoteHelp", player.Id), "/clan demote <playername>");
                        ReplyKey(player, msg("disbandHelp", player.Id), "/clan disband");
                        ReplyKey(player, msg("allyReqHelp", player.Id), "/clan ally request <clantag>");
                        ReplyKey(player, msg("allyAccHelp", player.Id), "/clan ally accept <clantag>");
                        ReplyKey(player, msg("allyDecHelp", player.Id), "/clan ally decline <clantag>");
                        ReplyKey(player, msg("allyCanHelp", player.Id), "/clan ally cancel <clantag>");
                        return;
                    default:
                        break;
                }
            }
        }
        #endregion

        #region Command Functions
        public void CreateClan(IPlayer player, string[] args)
        {
            if (args.Length == 2)
            {
                if (IsClanMember(player.Id))
                {
                    Reply(player, "", msg("alreadyMember", player.Id));
                    return;
                }
                string tag = new string(args[1].Where(c => char.IsLetterOrDigit(c) || char.IsWhiteSpace(c)).ToArray());
                if (tag.Length < 2 || tag.Length > 6)
                {
                    Reply(player, "", msg("tagForm", player.Id));
                    return;
                }
                if (!clanCache.ContainsKey(tag))
                {
                    Clan newClan = new Clan().CreateNewClan(tag, player.Id, player.Name);
                    clanCache.Add(tag, newClan);
                    playerClans.Add(player.Id, tag);
                    Reply(player, tag, msg("createSucc", player.Id));
                }
                else Reply(player, tag, msg("clanExists", player.Id));
            }
            else Reply(player, "", "/clan create <tag>");
        }
        public void JoinClan(IPlayer player, string tag)
        {
            if (IsClanMember(player.Id))
            {
                Reply(player, "", msg("alreadyMember", player.Id));
                return;
            }
            if (clanCache.ContainsKey(tag))
            {
                var clan = clanCache[tag];
                if (clan.invitedPlayers.ContainsKey(player.Id))
                {
                    if (configData.ClanLimits.Maximum_TotalMembers != 0 && clan.members.Count >= configData.ClanLimits.Maximum_TotalMembers)
                    {
                        Reply(player, "", msg("memberLimit", player.Id));
                        clan.invitedPlayers.Remove(player.Id);
                        return;
                    }
                    clan.Broadcast(string.Format(msg("hasJoined"), player.Name));
                    clan.members.Add(player.Id, player.Name);
                    clan.invitedPlayers.Remove(player.Id);
                    playerClans.Add(player.Id, clan.clanTag);
                    Reply(player, tag, msg("joinSucc", player.Id));                    
                    ClanUpdate(clan.clanTag);
                }
                else Reply(player, tag, msg("noInvite", player.Id));
            }
            else Reply(player, tag, msg("noFindClan", player.Id));
        }
        public void LeaveClan(IPlayer player)
        {
            if (IsClanMember(player.Id))
            {
                var clan = clanCache[playerClans[player.Id]];
                if (clan.IsOwner(player.Id))
                {
                    clan.RemoveOwner(player);
                    return;
                }
                if (clan.IsModerator(player.Id))
                    clan.moderators.Remove(player.Id);

                if (clan.IsMember(player.Id))
                    clan.RemoveUser(player.Id, ref clan.members);

                Reply(player, clan.clanTag, msg("leaveSucc", player.Id));
                ClanUpdate(clan.clanTag);
            }
            else Reply(player, "", msg("notInClan", player.Id));
        }
        public void InviteMember(IPlayer player, string[] args)
        {
            if (IsClanMember(player.Id))
            {
                var clan = clanCache[playerClans[player.Id]];
                if (!clan.IsOwner(player.Id) && !clan.IsModerator(player.Id))
                {
                    Reply(player, "", msg("noInvPerm", player.Id));
                    return;
                }
                if (configData.ClanLimits.Maximum_TotalMembers != 0 && clan.members.Count >= configData.ClanLimits.Maximum_TotalMembers)
                {
                    Reply(player, "", msg("invMemberLimit", player.Id));
                    return;
                }
                if (args[1].ToLower() == "cancel")
                {
                    var targetName = clan.FindPlayer(args[2], clan.invitedPlayers, false);
                    if (string.IsNullOrEmpty(targetName))
                    {
                        Reply(player, args[2], msg("noPlayerInv", player.Id));
                        return;
                    }
                    else if (clan.RemoveUser(targetName, ref clan.invitedPlayers))
                        ReplyKey(player, msg("invCancelled", player.Id), targetName);
                    return;
                }
                var target = FindPlayer(player, args[1]);
                if (target != null)
                {
                    if (IsClanMember(target.Id))
                    {
                        ReplyKey(player, msg("playerInClan", player.Id), target.Name);
                        return;
                    }
                    clan.invitedPlayers.Add(target.Id, target.Name);
                    Reply(target, clan.clanTag, msg("clanInv"));
                    ReplyKey(player, "You have invited {0} to join your clan", target.Name);
                    ClanUpdate(clan.clanTag);
                    return;
                }                
            }
            else Reply(player, "", msg("notInClan", player.Id));
        }
        public object KickMember(IPlayer player, string targetplayer)
        {
            if (IsClanMember(player.Id))
            {
                var clan = clanCache[playerClans[player.Id]];
                if (!clan.IsOwner(player.Id) && !clan.IsModerator(player.Id))
                {
                    Reply(player, "", msg("noKickPerm", player.Id));
                    return null;
                }
                var target = clan.FindPlayer(targetplayer, clan.members, true);
                var targetName = clan.members[target];
                if (!string.IsNullOrEmpty(target))
                {
                    if (clan.IsOwner(target))
                    {
                        Reply(player, "", msg("noKickOwner", player.Id));
                        return null;
                    }

                    if (!clan.IsOwner(player.Id) && clan.IsModerator(target))
                    {
                        Reply(player, "", msg("noKickMod", player.Id));
                        return null;
                    }

                    if (target == player.Id)
                    {
                        Reply(player, "", msg("noKickSelf", player.Id));
                        return null;
                    }

                    if (clan.RemoveUser(target, ref clan.members))
                    {
                        if (clan.IsModerator(target))
                            clan.moderators.Remove(target);

                        playerClans.Remove(target);


                        Reply(player, string.Format(msg("kickSucc", player.Id), targetName));
                        ClanUpdate(clan.clanTag);

                        var targetPlayer = players.FindPlayer(target);
                        if (targetPlayer != null && targetPlayer.IsConnected)
                            Reply(targetPlayer, clan.clanTag, msg("kicked"));
                        //else AddDelayMessage(target, string.Format(msg("kicked"), clan.clanTag));
                        return true;
                    }
                    else
                    {
                        ReplyKey(player, msg("kickError", player.Id), targetName);                        
                    }
                }
                else
                {
                    Reply(player, "", msg("noClanMember", player.Id));                    
                }
            }
            return null;
        }
        public object PromoteMember(IPlayer player, string targetplayer)
        {
            if (IsClanMember(player.Id))
            {
                var clan = clanCache[playerClans[player.Id]];
                if (clan.moderators.Count != 0 && clan.moderators.Count >= configData.ClanLimits.Maximum_Moderators)
                {
                    Reply(player, "", msg("modLimit", player.Id));
                    return null;
                }
                var target = clan.FindPlayer(targetplayer, clan.members, true);
                var targetName = clan.members[target];
                if (clan.IsModerator(target) || clan.IsOwner(target))
                {
                    ReplyKey(player, msg("alreadyMod", player.Id), targetName);
                    return null;
                }

                clan.moderators.Add(target);

                Reply(player, targetName, msg("promSucc", player.Id));
                ClanUpdate(clan.clanTag);

                var targetPlayer = players.FindPlayer(target);
                if (targetPlayer != null && targetPlayer.IsConnected)
                    ReplyKey(targetPlayer, msg("beenProm", targetPlayer.Id), player.Name);
                //else AddDelayMessage(target, string.Format(msg("beenProm"), player.Name));
                return true;
            }
            else { Reply(player, "", msg("notInClan", player.Id)); return null; }
        }
        public object DemoteMember(IPlayer player, string targetplayer)
        {
            if (IsClanMember(player.Id))
            {
                var clan = clanCache[playerClans[player.Id]];
                var target = clan.FindPlayer(targetplayer, clan.members, true);
                var targetName = clan.members[target];
                if (clan.IsModerator(target) && clan.IsOwner(player.Id))
                {
                    clan.moderators.Remove(target);
                    Reply(player, string.Format(msg("demSucc", player.Id), targetName));
                    ClanUpdate(clan.clanTag);

                    var targetPlayer = players.FindPlayer(target);
                    if (targetPlayer != null && targetPlayer.IsConnected)
                        ReplyKey(targetPlayer, msg("beenDem", targetPlayer.Id), player.Name);
                    //else AddDelayMessage(target, string.Format(msg("beenDem"), player.Name));

                    return true;
                }
                else ReplyKey(player, msg("notMod", player.Id), targetName);
            }
            else Reply(player, "", msg("notInClan", player.Id));
            return null;
        }
        public void DisbandClan(IPlayer player)
        {
            if (IsClanMember(player.Id))
            {
                var clan = clanCache[playerClans[player.Id]];
                if (clan.IsOwner(player.Id))
                {
                    foreach (var member in clan.members)
                    {
                        if (member.Key != player.Id)
                        {
                            var targetPlayer = players.FindPlayer(member.Key);
                            if (targetPlayer != null && targetPlayer.IsConnected)
                                ReplyKey(targetPlayer, msg("beenDisb", targetPlayer.Id), player.Name);
                            //else AddDelayMessage(member.Key, string.Format(msg("beenDisb"), player.Name));
                        }
                        playerClans.Remove(member.Key);
                    }
                    ClanDestroy(clan.clanTag);
                    clanCache.Remove(clan.clanTag);
                    Reply(player, "", msg("disbSucc", player.Id));
                }
                else Reply(player, "", msg("notOwnerDisb", player.Id));
            }
            else Reply(player, "", msg("notInClan", player.Id));
        }
        public void ShowMembers(IPlayer player)
        {
            if (IsClanMember(player.Id))
            {
                var clan = clanCache[playerClans[player.Id]];
                var returnString = "";
                int i = 1;
                foreach (var member in clan.members)
                {
                    if (clan.onlineMembers.Contains(member.Key))
                        returnString += $"<color=green>{member.Value}</color>";
                    else returnString += $"<color=red>{member.Value}</color>";
                    if (i < clan.members.Count)
                        returnString += ", ";
                    i++;
                }
                Reply(player, returnString);
            }
            else Reply(player, "", msg("notInClan", player.Id));
        }
        public void Alliance(IPlayer player, string[] args)
        {
            var clan = clanCache[playerClans[player.Id]];
            if (clan.IsOwner(player.Id))
            {
                if (args.Length == 3)
                {
                    if (clanCache.ContainsKey(args[2]))
                    {
                        switch (args[1].ToLower())
                        {
                            case "request":
                                if (configData.ClanLimits.Maximum_ClanAlliances != 0 && clan.clanAlliances.Count >= configData.ClanLimits.Maximum_ClanAlliances)
                                {
                                    Reply(player, "", msg("allyLimit", player.Id));
                                    return;
                                }
                                if (clanCache.ContainsKey(args[2]))
                                {
                                    if (!clan.invitedAllies.Contains(args[2]))
                                    {
                                        if (clan.clanAlliances.Contains(args[2]))
                                        {
                                            Reply(player, args[2], msg("alreadyAllies", player.Id));
                                            return;
                                        }
                                        var targetClan = clanCache[args[2]];
                                        targetClan.pendingInvites.Add(clan.clanTag);
                                        clan.invitedAllies.Add(targetClan.clanTag);

                                        Reply(player, targetClan.clanTag, msg("allyReq", player.Id));
                                        ClanUpdate(clan.clanTag);

                                        var targetOwner = players.FindPlayer(targetClan.ownerID);
                                        if (targetOwner != null && targetOwner.IsConnected)
                                            ReplyKey(targetOwner, msg("reqAlliance", targetOwner.Id), clan.clanTag);
                                        //else AddDelayMessage(targetClan.ownerID, string.Format(msg("reqAlliance"), clan.clanTag));
                                    }
                                    else Reply(player, args[2], msg("invitePending", player.Id));
                                }
                                else ReplyKey(player, msg("clanNoExist", player.Id), args[2]);
                                return;
                            case "accept":
                                if (clanCache.ContainsKey(args[2]))
                                {
                                    if (clan.pendingInvites.Contains(args[2]))
                                    {
                                        var targetClan = clanCache[args[2]];
                                        if (configData.ClanLimits.Maximum_ClanAlliances != 0 && targetClan.clanAlliances.Count >= configData.ClanLimits.Maximum_ClanAlliances)
                                        {
                                            ReplyKey(player, msg("allyAccLimit", player.Id), targetClan.clanTag);
                                            targetClan.invitedAllies.Remove(clan.clanTag);
                                            clan.pendingInvites.Remove(targetClan.clanTag);
                                            return;
                                        }
                                        targetClan.invitedAllies.Remove(clan.clanTag);
                                        targetClan.clanAlliances.Add(clan.clanTag);
                                        clan.pendingInvites.Remove(targetClan.clanTag);
                                        clan.clanAlliances.Add(targetClan.clanTag);

                                        Reply(player, targetClan.clanTag, msg("allyAcc", player.Id));
                                        ClanUpdate(clan.clanTag);

                                        var targetOwner = players.FindPlayer(targetClan.ownerID);
                                        if (targetOwner != null && targetOwner.IsConnected)
                                            ReplyKey(targetOwner, msg("allyAccSucc", targetOwner.Id), clan.clanTag);
                                        //else AddDelayMessage(targetClan.ownerID, string.Format(msg("allyAccSucc"), clan.clanTag));
                                    }
                                    else Reply(player, args[2], msg("noAllyInv", player.Id));
                                }
                                else ReplyKey(player, msg("clanNoExist", player.Id), args[2]);
                                return;
                            case "decline":
                                if (clanCache.ContainsKey(args[2]))
                                {
                                    if (clan.pendingInvites.Contains(args[2]))
                                    {
                                        var targetClan = clanCache[args[2]];
                                        targetClan.invitedAllies.Remove(clan.clanTag);
                                        Reply(player, targetClan.clanTag, msg("allyDec", player.Id));
                                        ClanUpdate(clan.clanTag);

                                        var targetOwner = players.FindPlayer(targetClan.ownerID);
                                        if (targetOwner != null && targetOwner.IsConnected)
                                            ReplyKey(targetOwner, msg("allyDecSucc", targetOwner.Id), clan.clanTag);
                                        //else AddDelayMessage(targetClan.ownerID, string.Format(msg("allyDecSucc"), clan.clanTag));
                                    }
                                    else Reply(player, args[2], msg("noAllyInv", player.Id));
                                }
                                else ReplyKey(player, msg("clanNoExist", player.Id), args[2]);
                                return;
                            case "cancel":
                                if (clanCache.ContainsKey(args[2]))
                                {
                                    if (clan.clanAlliances.Contains(args[2]))
                                    {
                                        var targetClan = clanCache[args[2]];
                                        targetClan.clanAlliances.Remove(clan.clanTag);
                                        clan.clanAlliances.Remove(clan.clanTag);
                                        Reply(player, targetClan.clanTag, msg("allyCan", player.Id));
                                        ClanUpdate(clan.clanTag);

                                        var targetOwner = players.FindPlayer(targetClan.ownerID);
                                        if (targetOwner != null && targetOwner.IsConnected)
                                            ReplyKey(targetOwner, msg("allyCanSucc", targetOwner.Id), clan.clanTag);
                                        //else AddDelayMessage(targetClan.ownerID, string.Format(msg("allyCanSucc"), clan.clanTag));
                                    }
                                    else Reply(player, args[2], msg("noAlly", player.Id));
                                }
                                else ReplyKey(player, msg("clanNoExist", player.Id), args[2]);
                                return;
                            default:
                                Reply(player, "Syntax:\n/clan ally request <clantag>\n/clan ally accept <clantag>\n/clan ally decline <clantag>\n/clan ally cancel <clantag>");
                                return;
                        }
                    }
                    else ReplyKey(player, msg("clanNoExist", player.Id), args[2]);
                }
                else Reply(player, "Syntax:\n/clan ally request <clantag>\n/clan ally accept <clantag>");
            }
            else Reply(player, "", msg("ownerAlly", player.Id));
        }
        #endregion

        #region API
        private JObject GetClan(string tag)
        {
            var clan = FindClanByTag(tag);
            if (clan == null)
                return null;
            return clan.ToJObject();
        }
        private JArray GetAllClans() => new JArray(clanCache.Keys); 
        private string GetClanOf(object player)
        {
            if (player == null)            
                throw new ArgumentException("player");
            
            if (player is ulong)            
                player = player.ToString();
                        
            else if (player is IPlayer)            
                player = (player as IPlayer).Id;
            #if RUST
            else if (player is BasePlayer)
            player = (player as BasePlayer).UserIDString;
            #endif            
            if (!(player is string))            
                throw new ArgumentException("player");
            
            var clan = FindClanByID((string)player);
            if (clan == null)            
                return null;
            
            return clan.clanTag;
        }
        private string GetClanTag(string playerid)
        {
            if (playerClans.ContainsKey(playerid))
            {
                var clan = playerClans[playerid];
                if (clan == null)
                    return null;
                return clan;
            }
            return null;
        }

        private void ClanCreate(string tag) => Interface.CallHook("OnClanCreate", tag);
        private void ClanUpdate(string tag) => Interface.CallHook("OnClanUpdate", tag);
        private void ClanDestroy(string tag) => Interface.CallHook("OnClanDestroy", tag);
        #endregion

        #region Config        
        private ConfigData configData;
        class Messaging
        {
            public string ClanChat { get; set; }
            public string Main { get; set; }            
            public string MSG { get; set; }
        }
        class ClanLimit
        {
            public int Maximum_TotalMembers { get; set; }
            public int Maximum_Moderators { get; set; }
            public int Maximum_ClanAlliances { get; set; }
        }
        class Options
        {
            public bool ShowJoinMessage { get; set; }
            public bool ShowLeaveMessage { get; set; }
            public int SaveTimer { get; set; }
        }
        class ConfigData
        {
            public ClanLimit ClanLimits { get; set; }
            public Messaging Messaging { get; set; }
            public Options Options { get; set; }
        }
        private void LoadVariables()
        {
            LoadConfigVariables();
            SaveConfig();
        }
        protected override void LoadDefaultConfig()
        {
            var config = new ConfigData
            {
                ClanLimits = new ClanLimit
                {
                    Maximum_TotalMembers = 8,
                    Maximum_Moderators = 2,
                    Maximum_ClanAlliances = 2
                },
                Messaging = new Messaging
                {
                    ClanChat = "<color=#AFFF00>",
                    Main = "<color=#FFBB00>",                    
                    MSG = "<color=#939393>"
                },
                Options = new Options
                {
                    ShowJoinMessage = true,
                    ShowLeaveMessage = true,
                    SaveTimer = 600,
                }
            };
            SaveConfig(config);
        }
        private void LoadConfigVariables() => configData = Config.ReadObject<ConfigData>();
        void SaveConfig(ConfigData config) => Config.WriteObject(config, true);
#endregion

        #region Data Management
        void SaveLoop() => timer.Once(configData.Options.SaveTimer * 60, () => { SaveData(); SaveLoop(); });
        void SaveData()
        {
            clanData.data = clanCache;
            data.WriteObject(clanData);
        }
        void LoadData()
        {
            try
            {
                clanData = data.ReadObject<StoredData>();
                clanCache = clanData.data;
            }
            catch
            {
                clanData = new StoredData();
            }
        }
        public class Clan
        {
            public string clanTag;

            public string ownerID;
            public List<string> moderators = new List<string>();
            public Dictionary<string, string> members = new Dictionary<string, string>();
            public Dictionary<string, string> invitedPlayers = new Dictionary<string, string>();

            public List<string> clanAlliances = new List<string>();
            public List<string> invitedAllies = new List<string>();
            public List<string> pendingInvites = new List<string>();

            public List<string> onlineMembers = new List<string>();

            public Clan CreateNewClan(string clanTag, string ownerID, string ownerName)
            {
                this.clanTag = clanTag;
                this.ownerID = ownerID;
                members.Add(ownerID, ownerName);
                clans.ClanCreate(clanTag);
                return this;
            }
            public bool IsOwner(string ID) => ownerID == ID;
            public bool IsModerator(string ID) => moderators.Contains(ID);
            public bool IsMember(string ID) => members.ContainsKey(ID);
            public bool IsInvited(string ID) => invitedPlayers.ContainsKey(ID);
            public void RemoveOwner(IPlayer player)
            {
                RemoveUser(ownerID, ref members);
                string newOwner = null;

                if (moderators.Count > 0)
                    newOwner = moderators.First();
                else if (members.Count > 0)
                    newOwner = members.First().Key;

                if (!string.IsNullOrEmpty(newOwner))
                {
                    ownerID = newOwner;
                    var target = clans.players.FindPlayer(newOwner);
                    if (target != null && target.IsConnected)
                        clans.Reply(target, clans.msg("ownerProm", target.Id));
                    //else clans.AddDelayMessage(newOwner, clans.msg("ownerProm"));
                    clans.Reply(player, clanTag, clans.msg("leaveSucc", target.Id));
                }
                else
                {
                    clans.ReplyKey(player, clans.msg("clanDestroy", player.Id), clanTag);
                    clans.clanCache.Remove(clanTag);
                    clans.playerClans.Remove(player.Id);
                }
            }
            public bool RemoveUser(string IDName, ref Dictionary<string, string> targetDict)
            {
                if (targetDict.ContainsKey(IDName))
                {
                    targetDict.Remove(IDName);
                    clans.playerClans.Remove(IDName);
                    return true;
                }
                if (targetDict.ContainsValue(IDName))
                {
                    var player = targetDict.FirstOrDefault(x => x.Value == IDName).Key;
                    targetDict.Remove(player);
                    clans.playerClans.Remove(player);
                    return true;
                }
                else
                {
                    foreach (var player in targetDict)
                    {
                        if (player.Value.Contains(IDName))
                        {
                            targetDict.Remove(player.Key);
                            clans.playerClans.Remove(player.Key);
                            return true;
                        }
                    }
                }
                return false;
            }
            public string FindPlayer(string IDName, Dictionary<string, string> targetDict, bool ID)
            {
                if (targetDict.ContainsKey(IDName))
                    if (ID) return IDName;
                    else return targetDict[IDName];

                else if (targetDict.ContainsValue(IDName))
                    if (ID) return targetDict.FirstOrDefault(x => x.Value == IDName).Key;
                    else return IDName;

                else
                {
                    foreach (var player in targetDict)
                    {
                        if (player.Value.Contains(IDName))
                        {
                            if (ID) return player.Key;
                            else return player.Value;
                        }
                    }
                }
                return null;
            }
            public void Broadcast(string message, string sender = "Clan")
            {
                foreach (var member in members)
                {
                    IPlayer target = clans.players.FindPlayer(member.Value);
                    if (target != null && target.IsConnected)
                        target.Reply($"{clans.configData.Messaging.ClanChat}|{sender}|</color> :{clans.configData.Messaging.MSG} {message}</color>");
                }
            }
            internal JObject ToJObject()
            {
                var obj = new JObject();
                obj["tag"] = clanTag;
                obj["owner"] = ownerID;

                var jmoderators = new JArray();
                foreach (var moderator in moderators)
                    jmoderators.Add(moderator);
                obj["moderators"] = jmoderators;

                var jmembers = new JArray();
                foreach (var member in members)
                    jmembers.Add(member.Key);
                obj["members"] = jmembers;

                var jinvited = new JArray();
                foreach (var invite in invitedPlayers)
                    jinvited.Add(invite.Key);
                obj["invited"] = jinvited;

                var jallies = new JArray();
                foreach (var ally in clanAlliances)
                    jallies.Add(ally);
                obj["allies"] = jallies;

                var jinvallies = new JArray();
                foreach (var ally in invitedAllies)
                    jinvallies.Add(ally);
                obj["invitedallies"] = jinvallies;
                return obj;
            }
        }
        class StoredData
        {
            public Dictionary<string, Clan> data = new Dictionary<string, Clan>();
            //public Dictionary<string, List<string>> outstandingMessages = new Dictionary<string, List<string>>();
        }
        #endregion

        #region Messaging
        string msg(string key, string id = null) => lang.GetMessage(key, this, id);

        Dictionary<string, string> Messages = new Dictionary<string, string>
        {
            {"playerCon", "{0} has connected to the server" },
            {"outstandingMsgs", "You have {0} outstanding messages. Type /cmessage to view them" },
            {"playerDiscon", "{0} has disconnected from the server"},
            {"cMessHelp", "{0} - Sends a message to all your clan members"},
            {"aMessHelp", "{0} - Sends a message to all your allied clan members"},
            {"createHelp", "{0} - Creates a new clan"},
            {"joinHelp", "{0} - Joins a clan you have been invited to"},
            {"leaveHelp", "{0} - Leaves your current clan"},
            {"modCom", "Moderator Commands:"},
            {"inviteHelp", "{0} - Invites a player to your clan"},
            {"cancelHelp", "{0} - Cancel a players invite to your clan"},
            {"kickHelp", "{0} - Kicks a player from your clan"},
            {"ownerCom", "Owner Commands:"},
            {"promoteHelp", "{0} - Promotes a member to clan moderator"},
            {"demoteHelp", "{0} - Demotes a clan moderator to member"},
            {"disbandHelp", "{0} - Disbands your clan" },
            {"allyReqHelp", "{0} - Offer an alliance to another clan"},
            {"allyAccHelp", "{0} - Accept an alliance from another clan"},
            {"allyDecHelp", "{0} - Decline an alliance from another clan"},
            {"allyCanHelp", "{0} - Cancel an alliance with another clan"},
            {"tagForm", "You clan tag must be between 2 and 6 characters, and must not contain any symbols"},
            {"createSucc", "You have successfully created a new clan with the tag:"},
            {"clanExists", "A clan already exists with the tag:"},
            {"alreadyMember", "You are already in a clan"},
            {"memberLimit", "You can not join this clan as it has already reached its member limit"},
            {"joinSucc", "You have joined the clan:"},
            {"noInvite", "You do not have a pending invite from the clan:"},
            {"noFindClan", "Unable to find a clan with the tag:"},
            {"leaveSucc", "You have left the clan:"},
            {"notInClan", "You are not currently in a clan"},
            {"noInvPerm", "You do not have permission to invite players"},
            {"invMemberLimit", "You can not invite any more players to join your clan as it has reached its member limit"},
            {"noPlayerInv", "Unable to find a invite for player:"},
            {"invCancelled", "You have cancelled {0}'s clan invite"},
            {"playerInClan", "{0} is already in a clan"},
            {"clanInv", "You have been invited to join the clan:"},
            {"noPlayerName", "Unable to find a player with that name"},
            {"noname", "You must enter a player's name"},
            {"nokickPerm", "You do not have permission to kick players"},
            {"noKickOwner", "You can not kick the clan owner"},
            {"noKickMod", "Only owners can kick moderators"},
            {"noKickSelf", "You can not kick yourself..."},
            {"kickSucc", "You have successfully kicked {0} from your clan"},
            {"kicked", "You have been kicked from the clan:"},
            {"kickError", "Error whilst removing {0} from your clan"},
            {"noClanMember", "Unable to find a clan member with that name"},
            {"noID", "You must enter a player's name or ID"},
            {"modLimit", "You can not assign any more moderators as you already have the maximum allowed amount"},
            {"alreadyMod", "{0} is already promoted"},
            {"promSucc", "You have successfully promoted"},
            {"beenProm", "{0} has promoted you to moderator"},
            {"demSucc", "You have successfully demoted {0}"},
            {"beenDem", "{0} has demoted you to member"},
            {"notMod", "{0} is not a clan moderator"},
            {"beenDisb", "{0} has disbanded your clan"},
            {"disbSucc", "You have successfully disbanded your clan"},
            {"notOwnerDisb", "Only the owner can disband a clan"},
            {"allyLimit", "You can not request any more alliances as you already have the maximum allowed amount"},
            {"alreadyAllies", "You are already allies with"},
            {"allyReq", "You have requested a clan alliance from"},
            {"reqAlliance", "{0} has requested a clan alliance"},
            {"invitePending", "You already have a pending alliance invite for"},
            {"clanNoExist", "The clan {0} does not exist"},
            {"allyAccLimit", "You can not accept this clan alliance as {0} has already have the maximum allowed amount"},
            {"allyAcc", "You have accepted the clan alliance from"},
            {"allyAccSucc", "{0} has accepted your alliance request"},
            {"noAllyInv", "You do not have a alliance invite from"},
            {"allyDec", "You have declined the clan alliance from"},
            {"allyDecSucc", "{0} has declined your alliance request"},
            {"allyCan", "You have cancelled your alliance with"},
            {"allyCanSucc", "{0} has cancelled your clan alliance"},
            {"noAlly", "You do not have a alliance with"},
            {"ownerAlly", "Only the clan owner can form alliances"},
            {"noClanData", "Unable to find your clan data or you are not a clan member"},
            {"noClanAlly", "You do not have any clan alliances"},
            {"cleardMsg", "You have cleared all outstanding messages"},
            {"clearSyn", "You can clear these messages by typing \"/cmessage clear\""},
            {"noOM", "You do not have any outstanding messages"},
            {"ownerProm", "You have been promoted to clan owner"},
            {"clanDestroy", "You have left the clan: {0} and it has been removed due to lack of members"},
            {"memCom", "Member Commands:" },
            {"comHelp", "Clans Help:" },
            {"clanHelp", "{0} - Display Clan commands" },
            {"clanMembers", "{0} - Show online and offline clan members" },
            {"memHelp", "{0} - Display member commands"},
            {"modHelp", "{0} - Display moderator commands"},
            {"ownHelp", "{0} - Display owner commands"},
            {"hasJoined", "{0} has joined the clan" },
            {"noPlayers", "Unable to find a player with the name or ID" },
            {"multiPlayers", "Multiple players found with that name" }
        };
        #endregion
    }
}
