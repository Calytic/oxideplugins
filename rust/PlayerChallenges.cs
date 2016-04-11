using System.Collections.Generic;
using System;
using Oxide.Core;
using System.Reflection;


namespace Oxide.Plugins
{
    [Info("Player Challenges", "Smoosher", "1.7.0", ResourceId = 1442 )]
    [Description("Player Challenges, Tracks trackable things and assigns a title.")]

    class PlayerChallenges : RustPlugin
    {

        ulong KillWinSteamID = 0;
        int KillWinCount = 0;
        ulong animalWinSteamID = 0;
        int animalWinCount = 0;
        ulong headshotWinSteamID = 0;
        int headshotWinCount = 0;
        bool IgnoreSleepers = false;
        bool UseBetterChat = false;
        bool IsKillChallangeActive = true;
        bool IsAnimalKillChallangeActive = true;
        bool IsHeadshotChallangeActive = true;
        bool IsIgnoreAdmins = false;
        bool CanAnnounceNewLeader = true;
        string KillTitle = "[Top Gun]";
        string animalKillTitle = "[Hunter]";
        string headshotTitle = "[Scalper]";
        string BCKillsGroup = "";
        string BCAnimalGroup = "";
        string BCheadshotGroup = "";

        FieldInfo displayName = typeof(BasePlayer).GetField("_displayName", (BindingFlags.Instance | BindingFlags.NonPublic));

        #region config
        protected override void LoadDefaultConfig()
        {
            PrintWarning("Creating a new configuration file.");
            Config.Clear();
            Config["0IsKillChallangeActive"] = true;
            Config["0IsAnimalKillChallangeActive"] = true;
            Config["0IsHeadshotChallangeActive"] = true;


            Config["1IgnoreSleepers"] = false;
            Config["1IgnoreAdmins"] = false;
            Config["1BCaUseBetterChat"] = false;
            Config["1AnnounceNewLeader"] = false;

            Config["2BCKillsGroup"] = "Kill Group Name";
            Config["2BCAnimalGroup"] = "Kill Group Name";
            Config["2BCheadshotGroup"] = "Kill Group Name";

            Config["3KillTitle"] = "[TopGun]";
            Config["3animalKillTitle"] = "[Hunter]";
            Config["3headshotTitle"] = "[Scalper]";
            SaveConfig();
            PrintWarning("Config Created.");
        }

        private void SetConfig()
        {
            IsKillChallangeActive = TrueorFalse(Config["0IsKillChallangeActive"].ToString());
            IsAnimalKillChallangeActive = TrueorFalse(Config["0IsAnimalKillChallangeActive"].ToString());
            IsHeadshotChallangeActive = TrueorFalse(Config["0IsHeadshotChallangeActive"].ToString());
            IgnoreSleepers = TrueorFalse(Config["1IgnoreSleepers"].ToString());
            IsIgnoreAdmins= TrueorFalse(Config["1IgnoreAdmins"].ToString());
            UseBetterChat = TrueorFalse(Config["1BCaUseBetterChat"].ToString());
            CanAnnounceNewLeader = TrueorFalse(Config["1AnnounceNewLeader"].ToString());
            BCKillsGroup = Config["2BCKillsGroup"].ToString();
            BCAnimalGroup = Config["2BCAnimalGroup"].ToString();
            BCheadshotGroup = Config["2BCheadshotGroup"].ToString();
            KillTitle = Config["3KillTitle"].ToString();
            animalKillTitle = Config["3animalKillTitle"].ToString();
            headshotTitle = Config["3headshotTitle"].ToString();
        }
        #endregion

        #region Functions

        #region Global Functions

        private void IsPlayerLeader(BasePlayer player)
        {
            if (IsKillChallengeActive())
            {
                if (player.userID == KillWinSteamID)
                {
                    SetChallangeTitleName(player, "kills");
                }
            }
        }

        private void SetChallangeTitleName(BasePlayer player, string type)
        {
            switch (type)
            {
                case "kills":
                    string name = string.Format(KillTitle + player.displayName);
                    displayName.SetValue(player, name);
                    break;

                case "animals":
                    string nameani = string.Format(animalKillTitle + player.displayName);
                    displayName.SetValue(player, nameani);
                    break;


                case "headshots":
                    string namehs = string.Format(headshotTitle + player.displayName);
                    displayName.SetValue(player, namehs);
                    break;
            }
        }

        private void StripChallangeTitleName(string type)
        {
            switch (type)
            {
                case "kills":
                    BasePlayer OldLeader = BasePlayer.Find(KillWinSteamID.ToString());
                    displayName.SetValue(OldLeader, OldLeader.displayName.Replace(KillTitle, ""));
                    break;

                case "animals":
                    BasePlayer OldLeaderani = BasePlayer.Find(animalWinSteamID.ToString());
                    displayName.SetValue(OldLeaderani, OldLeaderani.displayName.Replace(animalKillTitle, ""));
                    break;

                case "headshot":
                    BasePlayer OldLeaderhs = BasePlayer.Find(headshotWinSteamID.ToString());
                    displayName.SetValue(OldLeaderhs, OldLeaderhs.displayName.Replace(headshotTitle, ""));
                    break;
            }
        }

        private void BetterChatGrantRevoke(BasePlayer Player, string type, string grantrevoke)
        {
            BasePlayer playerid = null;
            string command = null;
            string playername = "";
           

            switch (grantrevoke)
            {
                case "grant":
                    playerid = Player;
                    command = "oxide.usergroup add";
                    playername = Player.displayName;
                    break;

                case "revoke":
                    switch(type)
                    {
                        case "kills":
                            playername = GetPlayerName(KillWinSteamID, type);
                            break;

                        case "headshot":
                            playername = GetPlayerName(headshotWinSteamID, "hs");
                            break;

                        case "animals":
                            playername = GetPlayerName(animalWinSteamID, type);
                            break;
                    }
                    command = "oxide.usergroup remove";
                    break;
            }
            switch (type)
            {
                case "kills":
                    ConsoleSystem.Run.Server.Normal(command, new String[] { playername, BCKillsGroup });
                    break;

                case "headshot":
                    ConsoleSystem.Run.Server.Normal(command, new String[] { playername, BCheadshotGroup });
                    break;

                case "animals":
                    ConsoleSystem.Run.Server.Normal(command, new String[] { playername, BCAnimalGroup });
                    break;

            }
        }

        private void CheckIfNewLeader(BasePlayer player, string type, int value)
        {
            switch (type)
            {
                case "kills":
                    if (value > KillWinCount)
                    {
                        KillWinCount = value;
                        if (KillWinSteamID != player.userID)
                        {
                            if (KillWinSteamID != 0)
                            {
                                if (UseBetterChat)
                                {
                                    BetterChatGrantRevoke(player, type, "revoke");
                                }
                                else
                                {
                                    StripChallangeTitleName(type);
                                }
                            }
                            KillWinSteamID = player.userID;
                            AddNewChallengeLeader(player, value, type);
                            UpdateWinner(player, type);
                        }
                    }
                    break;

                case "animals":
                    if (value > animalWinCount)
                    {
                        animalWinCount = value;
                        if (animalWinSteamID != player.userID)
                        {
                            if (animalWinSteamID != 0)
                            {
                                if (UseBetterChat)
                                {
                                    BetterChatGrantRevoke(player, type, "revoke");
                                }
                                else
                                {
                                    StripChallangeTitleName(type);
                                }
                            }
                            animalWinSteamID = player.userID;
                            AddNewChallengeLeader(player, value, type);
                            UpdateWinner(player, type);
                        }
                    }
                    break;

                case "headshot":
                    if (value > headshotWinCount)
                    {
                        headshotWinCount = value;
                        if (animalWinSteamID != player.userID)
                        {
                            if (headshotWinSteamID != 0)
                            {
                                if (UseBetterChat)
                                {
                                    BetterChatGrantRevoke(player, type, "revoke");
                                }
                                else
                                {
                                    StripChallangeTitleName(type);
                                }
                            }
                            headshotWinSteamID = player.userID;
                            AddNewChallengeLeader(player, value, type);
                            UpdateWinner(player, type);
                        }
                    }
                    break;
            }
        }

        private void UpdateWinner(BasePlayer player, string type)
        {
            if (UseBetterChat)
            {
                BetterChatGrantRevoke(player, type, "grant");
            }
            else
            {
                SetChallangeTitleName(player, type);
            }
            if (CanAnnounceNewLeader)
            {
                AnnouceNewLeader(player, type);
            }
        }

        private int GetPlayerScore(ulong UID, string type)
        {
            int score = 0;
            foreach ( var item in storedData.Players )
            {
                if (item.SteamID == UID)
                {
                    switch (type)
                    {
                        case "kills":
                            score = item.Kills;
                            break;

                        case "animals":
                            score = item.animalkills;
                            break;

                        case "hs":
                            score = item.Headshots;
                            break;
                    }
                   
                }
            }

            return score;
        }

        private string GetPlayerName(ulong UID, string type)
        {
            string name = "Unkown";
            foreach (var item in storedData.Players)
            {
                if (item.SteamID == UID)
                {
                    switch (type)
                    {
                        case "kills":
                        case "animals":
                        case "hs":
                            name = item.Username;
                            break;
                    }

                }
            }

            return name;
        }

        #endregion

        #region Bool Checks

        private bool TrueorFalse(string input)
        {
            bool output;
            input = input.ToLower();
            switch (input)
            {
                case "true":
                    output = true;
                    return output;
                    break;

                case "false":
                    output = false;
                    return output;
                    break;

                default:
                    output = false;
                    return output;
                    break;
            }
        }

        private bool IsDeathPlayer(BaseCombatEntity entity)
        {
            BasePlayer player = entity.ToPlayer();

            if (player != null)
            {
                if (IgnoreSleepers)
                {
                    if (player.IsSleeping())
                    {
                        return false;
                    }
                }
                return true;
            }
            return false;
        }

        private bool IsKillerPlayer(HitInfo entity)
        {
            BaseEntity player = entity.Initiator;

            if (player != null)
            {
                if (player.ToPlayer() != null)
                {
                    return true;
                }
            }
            return false;
        }

        private bool IsKillChallengeActive()
        {
            return IsKillChallangeActive;
        }

        private bool IsAnimalKillChallengeActive()
        {
            return IsAnimalKillChallangeActive;
        }

        private bool IsHeadshotChallengeActive()
        {
            return IsHeadshotChallangeActive;
        }

        private bool IsDeathAnimal(BaseCombatEntity entity)
        {
            if (entity != null)
            {
                if (entity.ToString().Contains("animals"))
                {
                    return true;
                }
            }
            return false;

        }

        private bool IsDeathHeadshot(HitInfo hitinfo)
        {
            if (hitinfo != null)
            {
                if (hitinfo.HitBone.ToString().Contains("3198432"))
                {
                    return true;
                }
            }
            return false;
        }

        private bool IsIgnoreAdmin()
        {
            return IsIgnoreAdmins;
        }

        #endregion

        #region Challenge Type Checks

        private void ChallengeCheck(BaseCombatEntity victim, HitInfo Attacker)
        {
            if (IsKillChallengeActive())
            {
                if (IsDeathPlayer(victim))
                {
                    if (IsHeadshotChallengeActive())
                    {
                        if (IsDeathHeadshot(Attacker))
                        {
                            PlayerHeadshotKill(victim, Attacker);
                        }
                        else
                        {
                            PlayerKills(victim, Attacker);
                        }
                    }
                    else
                    {
                        PlayerKills(victim, Attacker);
                    }
                }
            }
            if (IsAnimalKillChallengeActive())
            {
                if (IsDeathAnimal(victim))
                {
                    AnimalKills(victim, Attacker);
                }

            }
        }
        #endregion

        #region Hook Functions

        void Init()
        {
            try
            {
                if ((bool)Config["1IgnoreAdmins"])
                {

                }
            }
            catch (Exception e)
            {
                Config["1IgnoreAdmins"] = true;
                Config.Save();
            }
        }

        void Loaded()
        {
            storedData = Interface.GetMod().DataFileSystem.ReadObject<StoredData>("PlayerChallenges");
            if (!permission.PermissionExists("canResetPlayerChallenges")) permission.RegisterPermission("canResetPlayerChallenges", this);
            SetConfig();
            BasePlayer[] allPlayers = UnityEngine.Object.FindObjectsOfType<BasePlayer>();
            foreach (BasePlayer Player in allPlayers)
            {
                if (Player.isConnected == true)
                {
                    if (IsNewPlayer(Player))
                    {
                        var playerinf = new PlayerInfo();
                        playerinf.Username = Player.displayName;
                        playerinf.SteamID = Player.userID;
                        storedData.Players.Add(playerinf);
                        Interface.GetMod().DataFileSystem.WriteObject("PlayerChallenges", storedData);

                    }
                }
            }
            GetChallengeLeaders();
        }

        void OnPlayerInit(BasePlayer player)
        {
            if (IsNewPlayer(player))
            {
                var playerinf = new PlayerInfo();
                playerinf.Username = player.displayName;
                playerinf.SteamID = player.userID;
                storedData.Players.Add(playerinf);
                Interface.GetMod().DataFileSystem.WriteObject("PlayerChallenges", storedData);
            }
            if (!UseBetterChat)
            {
                IsPlayerLeader(player);
            }
        }

        void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
        {
            if (info != null)
            {
                if (IsKillerPlayer(info))
                {
                    BasePlayer player = info.Initiator.ToPlayer();
                    if (player.net.connection.authLevel > 0 && IsIgnoreAdmin())
                    {
                        return;
                    }
                    else
                    {
                        ChallengeCheck(entity, info);
                    }
                }
            }
        }

        #endregion

        #region Player Kill Challenges Functions

        private void PlayerKills(BaseCombatEntity Victim, HitInfo Attacker)
        {
            var Killer = Attacker.Initiator.ToPlayer();
            var vic = Victim.ToPlayer();
            if (Killer != vic)
            {
                UpdateChallenge(Killer, "kills");
            }
        }
        #endregion

        #region Player Headshot Challenges Functions

        private void PlayerHeadshotKill(BaseCombatEntity Victim, HitInfo Attacker)
        {
            var Killer = Attacker.Initiator.ToPlayer();
            UpdateChallenge(Killer, "headshot");
            UpdateChallenge(Killer, "kills");
        }

        #endregion

        #region Animal Kill Challenges Functions

        private void AnimalKills(BaseCombatEntity Victim, HitInfo Attacker)
        {
            var Killer = Attacker.Initiator.ToPlayer();
            UpdateChallenge(Killer, "animals");
        }

        #endregion

        #region Data File Functions

        private bool IsNewPlayer(BasePlayer player)
        {
            foreach (var item in storedData.Players)
            {
                if (item.SteamID == player.userID)
                { 
                    return false;
                }
            }
            return true;
        }

        private void GetChallengeLeaders()
        {
            if (IsKillChallengeActive())
            {
                try
                {
                    foreach (var item in storedData.ChalWinner)
                    {
                        if (item.Type == "kills")
                        {
                            KillWinSteamID = item.SteamID;
                            KillWinCount = item.Value;
                        }
                        else if (item.Type == "animals")
                        {
                            animalWinSteamID = item.SteamID;
                            animalWinCount = item.Value;
                        }
                        else if (item.Type == "headshot")
                        {
                            headshotWinSteamID = item.SteamID;
                            headshotWinCount = item.Value;
                        }
                    }

                }
                catch (Exception e)
                {
                    Puts(e.Message.ToString());
                }
            }
        }

        private void AddNewChallengeLeader(BasePlayer player, int kills, string Type)
        {
            Winners CurrWinner = null;
            foreach(var item in storedData.ChalWinner)
            {
                if(item.Type == Type)
                {
                    CurrWinner = item;
                }
            }
            if (CurrWinner != null)
            {
                storedData.ChalWinner.Remove(CurrWinner);
            }
            var ChalLeader = new Winners();
            ChalLeader.SteamID = player.userID;
            ChalLeader.Value = kills;
            ChalLeader.Type = Type;


            storedData.ChalWinner.Add(ChalLeader);
            Interface.GetMod().DataFileSystem.WriteObject("PlayerChallenges", storedData);
        }

        private void UpdateChallenge(BasePlayer player, string type)
        {

        foreach (var item in storedData.Players)
            {
                var count = 0;
                if (item.SteamID == player.userID)
                {
                    if (count == 0)
                    {
                        switch (type)
                        {
                            case "kills":
                                item.Kills = item.Kills + 1;
                                item.Username = player.displayName;
                                CheckIfNewLeader(player, type, item.Kills);
                                count++;
                                break;

                            case "animals":
                                item.animalkills = item.animalkills + 1;
                                item.Username = player.displayName;
                                CheckIfNewLeader(player, type, item.animalkills);
                                count++;
                                break;

                            case "headshot":
                                item.Headshots = item.Headshots + 1;
                                item.Username = player.displayName;
                                CheckIfNewLeader(player, type, item.Headshots);
                                count++;
                                break;
                        }
                    }
                }
            }
            Interface.GetMod().DataFileSystem.WriteObject("PlayerChallenges", storedData);
           
        }
        #endregion

        #region Chat Broadcast Messages

        private void AnnouceNewLeader(BasePlayer player, string type)
        {
            string typestring = "";
            string Playername = player.displayName;

            switch(type)
            {
                case "kills":
                    typestring = "Most Kills";
                    break;

                case "animals":
                    typestring = "Most Animal Kills";
                    break;

                case "headshot":
                    typestring = "Most Headshots";
                    break;
            }

        ConsoleSystem.Broadcast("chat.add", 0, "<color=blue>[PlayerChallenges]</color> "+Playername+" has become the new leader and now has the "+typestring+"", 1);
        }

        #endregion

        #region Player Chat Message

        private void ChatMessageType(BasePlayer player, string type, string messagetype)
        {
            string message = "";
            int myscore = 0;
            int leaderscore = 0;
            string leadername = "";
            switch (type)
            {
                case "toppc":
                    switch(messagetype)
                    {
                        case "kills":
                            myscore = GetPlayerScore(player.userID, messagetype);
                            if (KillWinSteamID != 0)
                            {
                                leaderscore = GetPlayerScore(KillWinSteamID, messagetype);
                                leadername = GetPlayerName(KillWinSteamID, messagetype);
                                message = "The current leader for Kills is " + leadername + " on: " + leaderscore + ". Your Score : " + myscore;
                                ChatMessage(player, message, "standard");
                            }
                            else
                            {
                                message = "There is no current leader for Kills, and your score is currently: " + myscore;
                            }
                            break;

                        case "animals":
                            myscore = GetPlayerScore(player.userID, messagetype);
                            if (animalWinSteamID != 0)
                            {
                                leaderscore = GetPlayerScore(animalWinSteamID, messagetype);
                                leadername = GetPlayerName(animalWinSteamID, messagetype);

                                message = "The current leader for Animal Kills is " + leadername + " on: " + leaderscore + ". Your Score : " + myscore;
                                ChatMessage(player, message, "standard");
                            }
                            else
                            {
                                message = "There is no current leader for Animal Kills, and your score is currently: " + myscore;
                            }
                            break;

                        case "hs":
                            myscore = GetPlayerScore(player.userID, messagetype);
                            if (headshotWinSteamID != 0)
                            {
                                leaderscore = GetPlayerScore(headshotWinSteamID, messagetype);
                                leadername = GetPlayerName(headshotWinSteamID, messagetype);
                                message = "The current leader for Headshot Kills is " + leadername + " on: " + leaderscore + ". Your Score : " + myscore;
                                ChatMessage(player, message, "standard");
                            }
                            else
                            {
                                message = "There is no current leader for Headshot Kills, and your score is currently: " + myscore;
                            }

                            break;

                        case "standard":
                            message = "The options for this command are kills, animals or hs eg /TopPC kills";
                            ChatMessage(player, message, "standard");
                            break;
                    }
                break;
            }

        }

        private void ChatMessage(BasePlayer player, string Message, string type)
        {
            switch (type)
            {
                case "standard":
                    SendReply(player, Message);
                    break;
            }
        }

        #endregion

        #endregion

        #region Console Commands

        #endregion

        #region Chat Commands
        [ChatCommand("TopPC")]
        private void Topcheck(BasePlayer player, string command, string[] args)
        {
            if (args == null || args.Length == 0)
            {
                ChatMessageType(player, command.ToLower(), "standard");
            }
            else
            {
                switch (args[0].ToLower())
                {
                    case "kills":
                    case "animals":
                    case "hs":
                        ChatMessageType(player, command.ToLower(), args[0].ToLower());
                        break;

                    default:
                        ChatMessageType(player, command.ToLower(), "standard");
                        break;

                }
            }
        }

        [ChatCommand("ResetPC")]
        private void ResetPlayerChallenges(BasePlayer player, string commdn, string[] args)
        {
            var perm = new Oxide.Core.Libraries.Permission();
            if (perm.UserHasPermission(player.userID.ToString(), "canResetPlayerChallenges"))
            {
                if(UseBetterChat)
                {
                    var command = "oxide.usergroup remove";

                    var playernamekill = GetPlayerName(KillWinSteamID, "kills");

                    ConsoleSystem.Run.Server.Normal(command, new String[] { playernamekill, BCKillsGroup });

                    var playernamehs = GetPlayerName(headshotWinSteamID, "hs");

                    ConsoleSystem.Run.Server.Normal(command, new String[] { playernamehs, BCheadshotGroup });

                    var playernameanimal = GetPlayerName(animalWinSteamID, "animals");
                    ConsoleSystem.Run.Server.Normal(command, new String[] { playernameanimal, BCAnimalGroup });

                 }
                else
                {
                    StripChallangeTitleName("kills");
                    StripChallangeTitleName("animals");
                    StripChallangeTitleName("headshot");

                }
                animalWinSteamID = 0;
                animalWinCount = 0;
                KillWinSteamID = 0;
                KillWinCount = 0;
                headshotWinSteamID = 0;
                headshotWinSteamID = 0;

                storedData = new StoredData();
                    Interface.GetMod().DataFileSystem.WriteObject("PlayerChallenges", storedData);

                BasePlayer[] allPlayers = UnityEngine.Object.FindObjectsOfType<BasePlayer>();
                foreach (BasePlayer Player in allPlayers)
                {
                    if (Player.isConnected == true)
                    {
                        if (IsNewPlayer(Player))
                        {
                                       var playerinf = new PlayerInfo();
                                       playerinf.Username = Player.displayName;
                                       playerinf.SteamID = Player.userID;
                                       storedData.Players.Add(playerinf);
                                       Interface.GetMod().DataFileSystem.WriteObject("PlayerChallenges", storedData);

                        }
                    }
                }

                SendReply(player, "PlayerChallenges Data Reset");
            }
            else
            {
                SendReply(player, "You do not have permission to do this");
            }
        }
        #endregion

        #region Data File

        #region Player Stats File
        class StoredData
        {
            public HashSet<PlayerInfo> Players = new HashSet<PlayerInfo>();
            public HashSet<Winners> ChalWinner = new HashSet<Winners>();

            public StoredData()
            {
            }
        }

        class PlayerInfo
        {
            public ulong SteamID;
            public string Username;
            public int Kills = 0;
            public int Headshots = 0;
            public int animalkills = 0;

            public PlayerInfo()
            {
            }

            public PlayerInfo(BasePlayer player)
            {
                SteamID = player.userID;
                Username = player.displayName;
            }


            public PlayerInfo(BasePlayer player, int killed, string type)
            {
                switch(type)
                {
                    case "kills":
                        Username = player.displayName;
                        SteamID = player.userID;
                        Kills = Kills + killed;
                        break;

                    case "animals":
                        Username = player.displayName;
                        SteamID = player.userID;
                        animalkills = animalkills + killed;
                        break;

                    case "headshot":
                        Username = player.displayName;
                        SteamID = player.userID;
                        Kills = Kills + killed;
                        Headshots = Headshots + 1;
                        break;
                }

            }


        }
        #endregion

        #region Winners Stats File
        class Winners
            {
                public ulong SteamID;
                public int Value = 0;
                public string Type = "";

                public Winners()
                {
                }

                public Winners(BasePlayer player, int WinNo, string ChalType)
                {
                    SteamID = player.userID;
                    Value = WinNo;
                    Type = ChalType;
                }

            }
        #endregion
        StoredData storedData;
            #endregion

        }
    }



