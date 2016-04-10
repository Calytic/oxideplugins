using Oxide.Core;
using Oxide.Core.Plugins;
using Steamworks;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;

namespace Oxide.Plugins
{
    /*
    [B]Changelog 1.1.4[/B]
    [LIST]
    [*] Added top bounty hunters /bounties top
    [*] Simplified code.
    [*] Changed all CSteamID to Ulong for readability in files.
    [/LIST]
    */
    [Info("BountyHunters", "Pho3niX90", "1.1.4", ResourceId = 1656)]
    class BountyHunters : HurtworldPlugin
    {
        public List<Bounty> Bounties = new List<Bounty>();
        public List<Hunters> BountiesClaimed = new List<Hunters>();
        [PluginReference]
        Plugin EconomyBanks;
        string MoneySym;
        //bool isEcoBanksLoaded = false;

        void Loaded()
        {

            //EconomyBanks = (Plugin)plugins.Find("EconomyBanks");
            //if (EconomyBanks != null)
            //{
            //    isEcoBanksLoaded = true;
            //    MoneySym = (string)EconomyBanks.Call("GetMsg", "moneySymbol");
            //    isEcoBanksLoaded = true;
            //    Puts("EcconomyBanks has now loaded, and " + this.Title + " will now function");
            //}
            //else
            //{
            //    isEcoBanksLoaded = false;
            //}
            LoadMessages();
            LoadBounties();
        }
        //void OnPluginLoaded(Plugin name)
        //{
        //    if (name.Title.Equals("EconomyBanks"))
        //    {
        //        EconomyBanks = (Plugin)plugins.Find("EconomyBanks");
        //        MoneySym = (string)EconomyBanks.Call("GetMsg", "moneySymbol");
        //        isEcoBanksLoaded = true;
        //        Puts("EcconomyBanks has now loaded, and " + this.Title + " will now function");
        //    }
        //}
        //void OnPluginUnloaded(Plugin name)
        //{
        //    if (name.Title.Equals("EconomyBanks"))
        //    {
        //        isEcoBanksLoaded = false;
        //    }
        //}
        void LoadMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                {"msg_UserNotFound", "Cannot find the user"},
                {"msg_BountyAdded", "Your bounty has been placed on {target} for {moneySymbol}{amount}."},
                {"msg_BountyRewarded", "You have received a bounty reward of {moneySymbol}{amount} for killing {target}"},
                {"err_RewardMustBePositiveNumber", "The reward must be a positive numerical"},
                {"err_NeedMoreCash", "You do not have enough cash"},
                {"msg_NoTopPlayers", "There are no bounty claims"},
                {"msg_Bounties", "Target: {Color:Bad}{target}{/Color} | Bounty: {Color:Bad}{moneySymbol}{bounty}{/Color} | Set By: {Color:Bad}{submitter}{/Color}"},
                {"msg_BountyOnYou", "A bounty of {moneySymbol}{amount} has been placed on your head"},
                {"msg_noBounties", "There are no bounties"},
                {"msg_TopPlayersList", "{rank}: {playername} has {totalwins} total bounties claimed"},
                {"broadcast_BountyAdded", "A bounty has been place on {target} for {moneySymbol}{amount}"},
                {"broadcast_BountyClaimed", "A bounty has been claimed on {target} for {moneySymbol}{amount}"}
            }, this);
        }
        string GetMsg(string key, object userID = null)
        {
            return (userID == null) ? lang.GetMessage(key, this) : lang.GetMessage(key, this, userID.ToString());
        }
        public class Bounty
        {
            public Bounty(ulong target, ulong submitter, double reward, string targetName, string submitterName)
            {
                this.target = target;
                this.submitter = submitter;
                this.reward = reward;
                this.targetName = targetName;
                this.submitterName = submitterName;
            }

            public ulong target { get; set; }
            public ulong submitter { get; set; }
            public double reward { get; set; }
            public string targetName { get; set; }
            public string submitterName { get; set; }
        }
        public class Hunters
        {
            public Hunters(ulong hunter, string hunterName, ulong target, string targetName, ulong submitter, string submitterName, double reward)
            {
                this.hunter = hunter;
                this.hunterName = hunterName;
                this.target = target;
                this.reward = reward;
                this.targetName = targetName;
                this.submitter = submitter;
                this.submitterName = submitterName;
            }

            public ulong hunter { get; set; }
            public string hunterName { get; set; }
            public ulong target { get; set; }
            public string targetName { get; set; }
            public ulong submitter { get; set; }
            public string submitterName { get; set; }
            public double reward { get; set; }
        }

        [ChatCommand("bounties")]
        private void ChatCmd_ListBounties(PlayerSession player, string command, string[] args)
        {
            if (!isEcoLoaded()) { PrintError("Economybanks not loaded. BountyHunters will not function"); return; }

            if (args.Length == 1)
            {

                var TopHunters = from d in BountiesClaimed
                                 group d by d.hunter
                 into g
                                 select new
                                 {
                                     winner = g.Key,
                                     hunterName = g.Max(a => a.hunterName),
                                     totalWins = g.Sum(a => (a.reward > 0) ? 1 : 0)
                                 };
                TopHunters.OrderByDescending(d => d.totalWins).Take(5);
                int i = 1;
                if (TopHunters.Count() == 0)
                {
                    hurt.SendChatMessage(player, GetMsg("msg_NoTopPlayers", player));
                }
                foreach (var topHunters in TopHunters)
                {
                    hurt.SendChatMessage(player, GetMsg("msg_TopPlayersList", player)
                                     .Replace("{rank}", i.ToString())
                                     .Replace("{playername}", topHunters.hunterName)
                                     .Replace("{totalwins}", topHunters.totalWins.ToString()));

                    i++;
                }

                return;
            }

            if (Bounties.Count == 0)
            {
                hurt.SendChatMessage(player, Color(GetMsg("msg_noBounties", player), "bad"));
                return;
            }
            foreach (var bounty in Bounties)
            {
                hurt.SendChatMessage(player, GetMsg("msg_Bounties", player)
                    .Replace("{target}", bounty.targetName)
                    .Replace("{submitter}", bounty.submitterName)
                    .Replace("{bounty}", bounty.reward.ToString())
                    .Replace("{moneySymbol}", MoneySym)
                    .Replace("{Color:Good}", "<color=#00ff00ff>")
                    .Replace("{Color:Bad}", "<color=#ff0000ff>")
                    .Replace("{/Color}", "</color>"));
            }
        }
        [ChatCommand("addbountyall")]
        private void ChatCmd_AddBountyAll(PlayerSession player, string command, string[] args)
        {
            if (!player.IsAdmin) { return; }
            var online = GameManager.Instance.GetSessions();
            foreach (var i in online)
            {
                ChatCmd_AddBounty(player, "addbounty", new string[] { i.Value.Name, args[0] });
            }
        }
        [ChatCommand("addbounty")]
        private void ChatCmd_AddBounty(PlayerSession player, string command, string[] args)
        {
            if (!isEcoLoaded()) { PrintError("Economybanks not loaded. BountyHunters will not function"); return; }
            var bountyTarget = args[0];
            var bountySumiter = player;
            var bountyRewardtmp = args[1];

            if (getSession(bountyTarget) != null)
            {
                double bountyReward;
                if (!double.TryParse(bountyRewardtmp, out bountyReward))
                {
                    hurt.SendChatMessage(player, GetMsg("err_RewardMustBePositiveNumber", player));
                    return;
                }
                if (CashBalance(player) >= bountyReward && bountyReward > 0)
                {
                    PlayerSession bountyTargetSession = getSession(bountyTarget);
                    Bounties.Add(new Bounty((ulong)bountyTargetSession.SteamId, (ulong)bountySumiter.SteamId, bountyReward, bountyTargetSession.Name, player.Name));
                    hurt.SendChatMessage(player, GetMsg("msg_BountyAdded", player)
                        .Replace("{target}", bountyTarget)
                        .Replace("{moneySymbol}", MoneySym)
                        .Replace("{amount}", bountyReward.ToString()));
                    RemoveCash(player, bountyReward);
                    SaveBounties();
                    hurt.SendChatMessage(bountyTargetSession, Color(GetMsg("msg_BountyOnYou", bountyTargetSession)
                        .Replace("{moneySymbol}", MoneySym)
                        .Replace("{amount}", bountyReward.ToString()), "bad"));
                    hurt.BroadcastChat("Bounty", Color(GetMsg("broadcast_BountyAdded")
                        .Replace("{moneySymbol}", MoneySym)
                        .Replace("{amount}", bountyReward.ToString())
                        .Replace("{target}", bountyTarget), "bad"));
                }
                else
                {
                    if (CashBalance(player) >= bountyReward)
                    {
                        hurt.SendChatMessage(player, GetMsg("err_RewardMustBePositiveNumber", player));
                    }
                    else
                    {
                        hurt.SendChatMessage(player, GetMsg("err_NeedMoreCash", player));
                    }
                }
            }
            else
            {
                hurt.SendChatMessage(player, GetMsg("msg_UserNotFound", player));
            }
        }
        void OnPlayerDeath(PlayerSession player, EntityEffectSourceData source)
        {
            PrintWarning("1");
            if (!isEcoLoaded()) { PrintError("Economybanks not loaded. BountyHunters will not function"); return; }
            PrintWarning("2");
            var tmpName = GetNameOfObject(source.EntitySource);
            PrintWarning("3");
            if (tmpName.Length < 3) return;
            PrintWarning("4");
            var murdererName = tmpName.Remove(tmpName.Length - 3);
            PrintWarning("Murderer was " + murdererName);
            var murderer = getSession(murdererName);
            PrintWarning("5");
            var isPlayer = (murderer != null) ? true : false;
            PrintWarning("6");

            if (source.EntitySource.name == null || !isPlayer || murderer == null) return;

            PrintWarning("7");
            var deceased = player;
            PrintWarning("8");
            var deceasedId = (ulong)player.SteamId;

            PrintWarning(player.Name.ToString() + " was killed by " + murdererName + " isPlayer? " + isPlayer);

            if (isPlayer)
            {
                PlayerSession session = murderer;
                var stat = session.WorldPlayerEntity.GetComponent<EntityStats>();

                if (SameStake(murderer.SteamId, deceased.SteamId))
                {
                    hurt.SendChatMessage(murderer, "Can't claim bounties from your friends.");
                    //If they are of the same stake, make them wanted. 
                    //stat.GetFluidEffect(EEntityFluidEffectType.Infamy).AddValue(100f, source);
                    return;
                }

                for (int i = Bounties.Count - 1; i >= 0; i--)
                {
                    Bounty Reward = Bounties[i];
                    if (Reward.target == deceasedId && !SameStake(murderer.SteamId, deceased.SteamId))
                    {
                        AddCash(murderer, Reward.reward);
                        BountiesClaimed.Add(new Hunters((ulong)murderer.SteamId, murdererName, Reward.target, Reward.targetName, Reward.submitter, Reward.submitterName, Reward.reward));
                        hurt.BroadcastChat("Bounty", Color(GetMsg("broadcast_BountyClaimed")
                        .Replace("{moneySymbol}", MoneySym)
                        .Replace("{amount}", Reward.reward.ToString())
                        .Replace("{target}", Reward.targetName), "bad"));
                        hurt.SendChatMessage(murderer, GetMsg("msg_BountyRewarded", player)
                        .Replace("{target}", deceased.Name)
                        .Replace("{moneySymbol}", MoneySym)
                        .Replace("{amount}", Reward.reward.ToString()));
                        //stat.GetFluidEffect(EEntityFluidEffectType.Infamy).AddValue(-100f, EntityEffectSourceData.None);
                        Bounties.RemoveAt(i);
                    }
                }
                SaveBounties();

            }
        }

        double CashBalance(PlayerSession player)
        {
            return double.Parse(EconomyBanks.Call("Wallet", player).ToString());
        }
        double AccountBalance(PlayerSession player)
        {
            return double.Parse(EconomyBanks.Call("Balance", player).ToString());
        }
        void AddCash(PlayerSession player, double Amount)
        {
            EconomyBanks.Call("AddCash", player, Amount);
        }
        void RemoveCash(PlayerSession player, double Amount)
        {
            EconomyBanks.Call("RemoveCash", player, Amount);
        }
        string GetNameOfObject(GameObject obj)
        {
            var ManagerInstance = GameManager.Instance;
            return ManagerInstance.GetDescriptionKey(obj);
        }
        private PlayerSession getSession(string identifier)
        {

            var sessions = GameManager.Instance.GetSessions();
            PlayerSession session = null;
            foreach (var i in sessions)
            {

                if (i.Value.Name.ToLower().Contains(identifier.ToLower()) || identifier.Equals(i.Value.SteamId.ToString()))
                {
                    session = i.Value;
                    break;
                }
            }

            return session;
        }
        private void LoadBounties()
        {
            var _Bounties = Interface.GetMod().DataFileSystem.ReadObject<Collection<Bounty>>("Bounties");
            foreach (var item in _Bounties)
            {
                var target = item.target;
                var submitter = item.submitter;
                var submitterName = item.submitterName;
                var reward = item.reward;
                var targetName = item.targetName;
                Bounties.Add(new Bounty(target, submitter, reward, targetName, submitterName));
            }
        }
        private void LoadBountiesClaimed()
        {
            var _Bounties = Interface.GetMod().DataFileSystem.ReadObject<Collection<Hunters>>("BountiesClaimed");
            foreach (var item in _Bounties)
            {
                BountiesClaimed.Add(new Hunters(item.hunter, item.hunterName, item.target, item.targetName, item.submitter, item.submitterName, item.reward));
            }
        }
        private void SaveBounties()
        {
            Interface.GetMod().DataFileSystem.WriteObject("Bounties", Bounties);
        }
        private void SaveBountiesClaimed()
        {
            Interface.GetMod().DataFileSystem.WriteObject("BountiesClaimed", BountiesClaimed);
        }
        bool SameStake(CSteamID murderer, CSteamID victim)
        {
            List<OwnershipStakeServer> entities = new List<OwnershipStakeServer>();

            foreach (OwnershipStakeServer entity in Resources.FindObjectsOfTypeAll<OwnershipStakeServer>())
            {
                bool isMurdererFound = false;
                bool isVictimFound = false;
                foreach (PlayerIdentity player in entity.AuthorizedPlayers)
                {
                    if (player.SteamId == murderer) { isMurdererFound = true; } else if (player.SteamId == victim) { isVictimFound = true; }
                    if (isMurdererFound && isVictimFound) { return true; }
                }

            }

            return false;
        }
        string Color(string text, string color)
        {
            switch (color)
            {
                case "bad":
                    return "<color=#ff0000ff>" + text + "</color>";

                case "good":
                    return "<color=#00ff00ff>" + text + "</color>";

                case "header":
                    return "<color=#00ffffff>" + text + "</color>";

                default:
                    return "<color=#" + color + ">" + text + "</color>";
            }
        }
        bool isEcoLoaded()
        {
            if (EconomyBanks != null)
                return true;
            else
                return false;
        }
    }
}
