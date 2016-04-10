using System.Collections.Generic;
using System;
using UnityEngine;


namespace Oxide.Plugins
{
    [Info("HeliVote", "k1lly0u", "0.1.3", ResourceId = 1665)]
    class HeliVote : RustPlugin
    {
        bool Changed;

        private List<ulong> receivedYes;
        private List<ulong> receivedNo;
        private List<BaseEntity> currentHelis;

        private bool voteOpen;
        private bool helisActive;
        private bool timeBetween;
        private BasePlayer initiator;

        #region oxide hooks
        //////////////////////////////////////////////////////////////////////////////////////
        // Oxide Hooks ///////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////
        void Loaded()
        {
            permission.RegisterPermission("helivote.use", this);
            lang.RegisterMessages(messages, this);
            LoadVariables();            
        }
        void OnServerInitialized()
        {
            voteOpen = false;
            helisActive = false;
            timeBetween = false;
            initiator = null;
            receivedYes = new List<ulong>();
            receivedNo = new List<ulong>();
            currentHelis = new List<BaseEntity>();
        }
        void LoadDefaultConfig()
        {
            Puts("Creating a new config file");
            Config.Clear();
            LoadVariables();
        }
        void Unload()
        {
            receivedNo.Clear();
            receivedYes.Clear();
            foreach(var heli in currentHelis)
            {
                heli.KillMessage();
            }
            currentHelis.Clear();
        }
        void OnEntityDeath(BaseEntity entity, HitInfo hitinfo)
        {
            if (entity == null) return;
            if (helisActive)
            {
                if (currentHelis.Contains(entity))
                {
                    currentHelis.Remove(entity);
                    if (currentHelis.Count == 0)
                        helisActive = false;
                }
            }
        }
        #endregion

        #region methods
        //////////////////////////////////////////////////////////////////////////////////////
        // Vote Methods //////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////
                
        private bool alreadyVoted(BasePlayer player)
        {
            if (receivedNo.Contains(player.userID) || receivedYes.Contains(player.userID))
                return true;
            return false;
        }
        private bool TallyVotes()
        {
            var Yes = receivedYes.Count;
            var No = receivedNo.Count;
            float requiredVotes = BasePlayer.activePlayerList.Count * requiredVotesPercentage;
            if (useMajorityRules)
                if (Yes >= No)
                    return true;
            if (Yes > No && Yes >= requiredVotes) return true;
            return false;
        }
        private void voteEnd(int amount)
        {
            bool success = TallyVotes();
            if (success)
            {
                msgAll(string.Format(lang.GetMessage("voteSuccess", this), amount));
                helisActive = true;
                CallHeli(amount);                        
            }
            else
            {                
                msgAll(string.Format(lang.GetMessage("voteFail", this), minBetween));               
            }
            voteOpen = false;
            initiator = null;
            clearData();
            timeBetween = true;
            timer.Once(minBetween * 60, () => timeBetween = false);
        }
        private void clearData()
        {
            receivedYes.Clear();
            receivedNo.Clear();
        }
        private void CallHeli(int amount)
        {
            int i = 0;
            while (i < amount)
            {
                BaseEntity entity = GameManager.server.CreateEntity("assets/prefabs/npc/patrol helicopter/patrolhelicopter.prefab", new Vector3(), new Quaternion(), true);
                if (!entity) return;
                PatrolHelicopterAI heliAI = entity.GetComponent<PatrolHelicopterAI>();
                entity.Spawn(true);
                currentHelis.Add(entity);

                float mapSize = (TerrainMeta.Size.x / 2) - 50f;
                entity.transform.position = new Vector3(-mapSize, 30, mapSize);
                if (heliToInit)
                {
                    if (initiator != null)
                    entity.GetComponent<PatrolHelicopterAI>().State_Move_Enter(initiator.transform.position + new Vector3(0.0f, 20f, 0.0f));
                }
                i++;
            }
        }
        private void VoteTimer(int amount)
        {
            var time = voteOpenTimer * 60;
            timer.Repeat(1, time, () =>
            {                
                time--;
                if (time == 0)
                {
                    voteEnd(amount);
                    return;
                }
                if (time == 180)
                {
                    msgAll(string.Format(lang.GetMessage("timeLeft", this), 2, "Minutes"));
                }
                if (time == 120)
                {
                    msgAll(string.Format(lang.GetMessage("timeLeft", this), 2, "Minutes"));
                }
                if (time == 60)
                {
                    msgAll(string.Format(lang.GetMessage("timeLeft", this), 1, "Minute"));
                }
                if (time == 30)
                {
                    msgAll(string.Format(lang.GetMessage("timeLeft", this), 30, "Seconds"));
                }
                if (time == 10)
                {
                    msgAll(string.Format(lang.GetMessage("timeLeft", this), 10, "Seconds"));
                }
            });
        }
        private void msgAll(string left)
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                if (usePerms)
                    if (!canVote(player)) return;
                SendReply(player, lang.GetMessage("title", this, player.UserIDString) + left);
            }
        }
        private bool CheckIfStillExist()
        {
            int i = 0;
            var allobjects = UnityEngine.Object.FindObjectsOfType<BaseHelicopter>();
            foreach (var gobject in allobjects)
            {
                if (gobject.prefabID == 3703982321)
                {
                    if (currentHelis.Contains(gobject))
                        i++;
                }
            }
            if (i != 0) return true;

            helisActive = false;
            currentHelis.Clear();
            return false;

        }
        #endregion

        #region chat/console commands

        [ChatCommand("helivote")]
        private void cmdHeiVote(BasePlayer player, string command, string[] args)
        {
            if (usePerms)
                if (!canVote(player)) return;
            if (args.Length == 0)
            {
                SendReply(player, string.Format(lang.GetMessage("title", this, player.UserIDString) + lang.GetMessage("badSyn", this, player.UserIDString), maxAmount));
                return;
            }

            if (args.Length >= 1)
            {
                if (args[0].ToLower() == "open")
                {
                    if (voteOpen)
                    {
                        SendReply(player, lang.GetMessage("title", this, player.UserIDString) + lang.GetMessage("voteOpen", this, player.UserIDString));
                        return;
                    }
                    if (timeBetween)
                    {
                        SendReply(player, lang.GetMessage("title", this, player.UserIDString) + lang.GetMessage("cooldown", this, player.UserIDString));
                        return;
                    }                   
                    if (CheckIfStillExist())
                    {
                        SendReply(player, lang.GetMessage("title", this, player.UserIDString) + lang.GetMessage("heliActive", this, player.UserIDString));
                        return;
                    }

                    int amount = 1;
                    if (args.Length == 2)
                    {
                        if (!int.TryParse(args[1], out amount))
                        {
                            SendReply(player, lang.GetMessage("title", this, player.UserIDString) + lang.GetMessage("invAmount", this, player.UserIDString));
                            return;
                        }
                    }
                    if (amount > maxAmount)
                        amount = maxAmount;

                    msgAll(string.Format(lang.GetMessage("opened", this, player.UserIDString), amount));

                    float required = BasePlayer.activePlayerList.Count * requiredVotesPercentage;
                    if (required < 1) required = 1;

                    msgAll(string.Format(lang.GetMessage("required", this, player.UserIDString), (int)required));
                    voteOpen = true;
                    receivedYes.Add(player.userID);
                    initiator = player;
                    VoteTimer(amount);
                    return;
                }
                else if (args[0].ToLower() == "yes")
                {
                    if (!voteOpen)
                    {
                        SendReply(player, lang.GetMessage("title", this, player.UserIDString) + lang.GetMessage("noOpen", this, player.UserIDString));
                        return;
                    }
                    if (!alreadyVoted(player))
                    {
                        receivedYes.Add(player.userID);
                        SendReply(player, lang.GetMessage("title", this, player.UserIDString) + lang.GetMessage("yesVote", this, player.UserIDString));
                        if (displayProgress)
                            msgAll(string.Format(lang.GetMessage("totalVotes", this, player.UserIDString), receivedYes.Count, receivedNo.Count));
                        return;
                    }
                    SendReply(player, lang.GetMessage("title", this, player.UserIDString) + lang.GetMessage("alreadyVoted", this, player.UserIDString));
                    return;
                }
                else if (args[0].ToLower() == "no")
                {
                    if (!voteOpen)
                    {
                        SendReply(player, lang.GetMessage("title", this, player.UserIDString) + lang.GetMessage("noOpen", this, player.UserIDString));
                        return;
                    }
                    if (!alreadyVoted(player))
                    {
                        receivedNo.Add(player.userID);
                        SendReply(player, lang.GetMessage("title", this, player.UserIDString) + lang.GetMessage("noVote", this, player.UserIDString));
                        if (displayProgress)
                            msgAll(string.Format(lang.GetMessage("totalVotes", this, player.UserIDString), receivedYes.Count, receivedNo.Count));
                        return;
                    }
                    SendReply(player, lang.GetMessage("title", this, player.UserIDString) + lang.GetMessage("alreadyVoted", this, player.UserIDString));
                    return;
                }
            }
        }
        [ConsoleCommand("helivote")]
        private void ccmdVote(ConsoleSystem.Arg arg)
        {
            if (!isAuthCon(arg)) return;
            int amount = 1;
            if (arg.Args != null)
                if (arg.Args.Length == 1)
                    int.TryParse(arg.Args[0], out amount);

            msgAll(string.Format(lang.GetMessage("opened", this), amount));

            float required = BasePlayer.activePlayerList.Count * requiredVotesPercentage;
            if (required < 1) required = 1;

            msgAll(string.Format(lang.GetMessage("required", this), (int)required));
            voteOpen = true;
            VoteTimer(amount);
        }

        private bool canVote(BasePlayer player)
        {
            if (permission.UserHasPermission(player.userID.ToString(), "helivote.use")) return true;
            else if (isAuth(player)) return true;
            SendReply(player, lang.GetMessage("title", this, player.UserIDString) + lang.GetMessage("noPerms", this, player.UserIDString));
            return false;
        }
        bool isAuth(BasePlayer player)
        {
            if (player.net.connection != null)
            {
                if (player.net.connection.authLevel < 1)
                {
                    SendReply(player, lang.GetMessage("noPerms", this));
                    return false;
                }
            }
            return true;
        }
        bool isAuthCon(ConsoleSystem.Arg arg)
        {
            if (arg.connection != null)
            {
                if (arg.connection.authLevel < 1)
                {
                    SendReply(arg, lang.GetMessage("noPerms", this));
                    return false;
                }
            }
            return true;
        }
       
        #endregion

        #region config
        //////////////////////////////////////////////////////////////////////////////////////
        // Configuration /////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////

        static float requiredVotesPercentage = 0.5f;
        static bool useMajorityRules = true;
        static int voteOpenTimer = 4;
        static bool displayProgress = true;
        static int auth = 1;
        static int minBetween = 5;
        static int maxAmount = 4;
        static bool heliToInit = false;
        static bool usePerms = false;     

        private void LoadVariables()
        {
            LoadConfigVariables();
            SaveConfig();
        }
        private void LoadConfigVariables()
        {
            CheckCfgFloat("Options - Required yes vote percentage", ref requiredVotesPercentage);
            CheckCfg("Options - Timers - Open vote timer (minutes)", ref voteOpenTimer);
            CheckCfg("Options - Timers - Minimum time between votes (minutes)", ref minBetween);
            CheckCfg("Options - Display vote progress", ref displayProgress);
            CheckCfg("Options - Maximum helicopters to call", ref maxAmount);
            CheckCfg("Options - Send helicopters to the initiator", ref heliToInit);
            CheckCfg("Options - Use permission system only", ref usePerms);
            CheckCfg("Options - Use majority rules", ref useMajorityRules);
        }
        private void CheckCfg<T>(string Key, ref T var)
        {
            if (Config[Key] is T)
                var = (T)Config[Key];
            else
                Config[Key] = var;
        }
        private void CheckCfgFloat(string Key, ref float var)
        {

            if (Config[Key] != null)
                var = Convert.ToSingle(Config[Key]);
            else
                Config[Key] = var;
        }
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
        #endregion

        #region messages
        //////////////////////////////////////////////////////////////////////////////////////
        // Messages //////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////

        Dictionary<string, string> messages = new Dictionary<string, string>()
        {
            {"title", "<color=#cc2900>HeliVote</color> : " },
            {"noPerms", "You do not have permission to use this command" },
            {"badSyn", "<color=#ff704d>/helivote open ##</color> - Open a vote to spawn ## amount of helicopters. Maximum is <color=#ff704d>{0}</color>" },
            {"voteOpen", "There is already a vote open" },
            {"noOpen", "There isn't a vote open right now" },
            {"yesVote", "You have voted yes"},
            {"noVote", "You have voted no" },
            {"alreadyVoted", "You have already voted" },
            {"opened", "A vote to call <color=#ff704d>{0}</color> helicopter(s) is now open! Use <color=#ff704d>/helivote yes</color> or <color=#ff704d>/helivote no</color>" },
            {"required", "Minimum yes votes required is <color=#ff704d>{0}</color>" },
            {"invAmount", "You have entered a invalid number" },
            {"timeLeft", "Voting ends in {0} {1}, use <color=#ff704d>/helivote yes</color> or <color=#ff704d>/helivote no</color>" },
            {"cooldown", "You must wait for the cooldown period to end before opening a vote" },
            {"voteSuccess", "The vote was successful, spawning <color=#ff704d>{0}</color> helicopters!" },
            {"voteFail", "The vote failed to meet the requirements, try again in <color=#ff704d>{0}</color> minutes" },
            {"heliActive", "There are still active helicopters from the last vote. Can not open a new vote until they are destroyed" },
            {"totalVotes", "<color=#ff704d>{0}</color> vote(s) for Yes, <color=#ff704d>{1}</color> vote(s) for No" }
        };
        #endregion

    }
}
